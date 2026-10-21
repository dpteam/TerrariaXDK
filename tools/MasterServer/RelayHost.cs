using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MasterServer;

/// <summary>
/// TCP relay: lets a host accept players without port forwarding.
///
/// The problem it solves is not "the router is not configured" but "from the outside there is no
/// address to configure": behind carrier-grade NAT, a mobile connection or a guest network, the
/// host simply cannot be reached, and UPnP has nothing to forward. An <b>outbound</b> connection
/// always works, so both sides dial the relay and it copies bytes between them.
///
/// Why it never parses a packet: the game's transport already frames its stream
/// (<c>TcpPeer</c>/<c>WireFormat</c>), so the relay only has to move bytes. That keeps it small,
/// independent of the protocol version, and unable to impersonate either side - the handshake
/// (version, mod signature, password, ban list) stays end to end between host and client.
///
/// Protocol (one line of UTF-8 text per command, '\n' terminated):
/// <code>
/// HOST &lt;token&gt;      host registers a room; the socket becomes the room's presence
///                      -> "OK &lt;roomId&gt;"
/// JOIN &lt;roomId&gt;     client asks for the room; the socket waits for the host
///                      -> "WAIT" (or "ERR &lt;reason&gt;")
/// POLL &lt;token&gt;      host asks for waiting clients
///                      -> "JOIN &lt;ticket&gt;" repeated, then "END"
/// ACCEPT &lt;ticket&gt;    host dials back with a one-shot ticket; the relay joins the two
///                      sockets and both become a raw pipe
/// </code>
///
/// A separate port rather than an HTTP endpoint: a long-lived bidirectional stream does not fit
/// the request/response parser in <see cref="MasterServerHost"/> without a CONNECT tunnel, and
/// rewriting that parser would put the world list at risk for a feature that needs none of it.
/// </summary>
internal sealed class RelayHost
{
	/// <summary>How many rooms may exist at once. One room is one hosted world.</summary>
	public const int MaxRooms = 64;

	/// <summary>How many clients may wait for the host in one room.</summary>
	public const int MaxWaitingPerRoom = 16;

	/// <summary>How long a join ticket stays valid, in seconds.</summary>
	public const int TicketSeconds = 30;

	/// <summary>
	/// Per-room bandwidth cap, in bytes per second, counted in both directions together.
	///
	/// A cap rather than a total quota: a long session must not be cut off, but one host pushing a
	/// large world must not take the relay down for everybody else. 512 KiB/s is far above what a
	/// few players generate and far below a saturated link.
	/// </summary>
	public const int MaxBytesPerSecondPerRoom = 512 * 1024;

	/// <summary>One parked client: connected, told to wait, not yet paired.</summary>
	private sealed class PendingClient
	{
		public TcpClient Client;

		public string Ticket;

		public DateTime CreatedUtc;

		/// <summary>
		/// Set when the host pairs this client, or when the room closes under it. The waiting
		/// thread blocks on it and must never read the socket: the pipe owns the stream once the
		/// pairing happens, and two readers on one socket would steal each other's bytes.
		/// </summary>
		public readonly ManualResetEvent Done = new ManualResetEvent(initialState: false);

		public bool Paired;
	}

	private sealed class Room
	{
		public string Id;

		public string Token;

		public TcpClient Presence;

		public readonly Queue<PendingClient> Waiting = new Queue<PendingClient>();

		public readonly Dictionary<string, PendingClient> Tickets = new Dictionary<string, PendingClient>(StringComparer.Ordinal);

		public DateTime CreatedUtc;

		public long Bytes;

		public int PipeCount;

		public readonly object Sync = new object();
	}

	private TcpListener _listener;

	private Thread _acceptThread;

	private Thread _reaperThread;

	private volatile bool _stopping;

	private readonly int _requestedPort;

	private readonly object _sync = new object();

	private readonly Dictionary<string, Room> _rooms = new Dictionary<string, Room>(StringComparer.Ordinal);

	/// <summary>Raised for every event worth putting in the console.</summary>
	public event Action<string> Log;

	public RelayHost(int port)
	{
		_requestedPort = port;
	}

	/// <summary>The port actually bound. Port 0 means "let the OS pick".</summary>
	public int Port { get; private set; }

	/// <summary>Rooms currently registered. Used by the console and by tests.</summary>
	public int RoomCount
	{
		get
		{
			lock (_sync)
			{
				return _rooms.Count;
			}
		}
	}

	/// <summary>Total bytes moved through the relay since start. Diagnostics only.</summary>
	public long TotalBytes => Interlocked.Read(ref _totalBytes);

	/// <summary>Pipes currently open. Diagnostics only.</summary>
	public int ActivePipes => _activePipes;

	public void Start()
	{
		_listener = new TcpListener(IPAddress.Any, _requestedPort);
		_listener.Start();
		Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
		_acceptThread = new Thread(AcceptLoop)
		{
			IsBackground = true,
			Name = "Relay.Accept"
		};
		_acceptThread.Start();
		_reaperThread = new Thread(ReaperLoop)
		{
			IsBackground = true,
			Name = "Relay.Reaper"
		};
		_reaperThread.Start();
	}

	public void Stop()
	{
		_stopping = true;
		try
		{
			_listener?.Stop();
		}
		catch
		{
		}
		List<TcpClient> list = new List<TcpClient>();
		lock (_sync)
		{
			foreach (Room value in _rooms.Values)
			{
				if (value.Presence != null)
				{
					list.Add(value.Presence);
				}
				lock (value.Sync)
				{
					while (value.Waiting.Count > 0)
					{
						PendingClient pendingClient = value.Waiting.Dequeue();
						if (pendingClient.Client != null)
						{
							list.Add(pendingClient.Client);
						}
					}
				}
			}
			_rooms.Clear();
		}
		for (int i = 0; i < list.Count; i++)
		{
			try
			{
				list[i].Close();
			}
			catch
			{
			}
		}
	}

	/// <summary>Number of clients waiting in a room, for the console and for tests.</summary>
	public int WaitingCount(string RoomId)
	{
		lock (_sync)
		{
			Room value;
			if (!_rooms.TryGetValue(RoomId, out value))
			{
				return -1;
			}
			lock (value.Sync)
			{
				return value.Waiting.Count;
			}
		}
	}

	/// <summary>True when a room exists. For tests: a room must die with its host.</summary>
	public bool HasRoom(string RoomId)
	{
		lock (_sync)
		{
			return _rooms.ContainsKey(RoomId);
		}
	}

	private void Say(string message)
	{
		Action<string> log = Log;
		if (log != null)
		{
			log(message);
		}
	}

	private void AcceptLoop()
	{
		while (!_stopping)
		{
			TcpClient tcpClient;
			try
			{
				tcpClient = _listener.AcceptTcpClient();
			}
			catch
			{
				return;
			}
			try
			{
				tcpClient.NoDelay = true;
				Thread thread = new Thread(delegate(object state)
				{
					Serve((TcpClient)state);
				});
				thread.IsBackground = true;
				thread.Name = "Relay.Client";
				thread.Start(tcpClient);
			}
			catch
			{
				try
				{
					tcpClient.Close();
				}
				catch
				{
				}
			}
		}
	}

	/// <summary>
	/// Reads one command line and dispatches. Everything except HOST/POLL/ACCEPT becomes a pipe,
	/// so this method is the whole protocol surface.
	/// </summary>
	private void Serve(TcpClient Client)
	{
		try
		{
			string text = ReadLine(Client.GetStream());
			if (text == null)
			{
				Client.Close();
				return;
			}
			string[] array = text.Split(new char[1] { ' ' }, 2);
			string text2 = array[0].ToUpperInvariant();
			string text3 = ((array.Length > 1) ? array[1].Trim() : "");
			switch (text2)
			{
			case "HOST":
				RegisterHost(Client, text3);
				return;
			case "JOIN":
				JoinRoom(Client, text3);
				return;
			case "POLL":
				Poll(Client, text3);
				return;
			case "ACCEPT":
				Accept(Client, text3);
				return;
			default:
				SendLine(Client, "ERR unknown command");
				Client.Close();
				return;
			}
		}
		catch
		{
			try
			{
				Client.Close();
			}
			catch
			{
			}
		}
	}

	private void RegisterHost(TcpClient Client, string Token)
	{
		if (Token.Length < 16)
		{
			// A guessable token would let anyone take over somebody's room.
			SendLine(Client, "ERR token too short");
			Client.Close();
			return;
		}
		Room room = new Room();
		room.Id = NewSecret(8);
		room.Token = Token;
		room.Presence = Client;
		room.CreatedUtc = DateTime.UtcNow;
		lock (_sync)
		{
			if (_rooms.Count >= MaxRooms)
			{
				SendLine(Client, "ERR busy");
				Client.Close();
				return;
			}
			_rooms[room.Id] = room;
		}
		SendLine(Client, "OK " + room.Id);
		Say("relay    room " + room.Id + " opened (" + RoomCount + " of " + MaxRooms + ")");
		// The presence socket is what says "this room still exists": when the host goes away the
		// connection ends and the room is dropped, which is the only reliable signal a relay has.
		WaitForHostToLeave(room);
	}

	private void WaitForHostToLeave(Room Room)
	{
		try
		{
			NetworkStream stream = Room.Presence.GetStream();
			byte[] array = new byte[64];
			while (!_stopping)
			{
				int num = stream.Read(array, 0, array.Length);
				if (num <= 0)
				{
					break;
				}
			}
		}
		catch
		{
		}
		CloseRoom(Room, "host left");
	}

	private void CloseRoom(Room Room, string Reason)
	{
		List<PendingClient> list;
		lock (_sync)
		{
			Room value;
			if (!_rooms.TryGetValue(Room.Id, out value) || value != Room)
			{
				return;
			}
			_rooms.Remove(Room.Id);
			lock (Room.Sync)
			{
				list = new List<PendingClient>(Room.Waiting);
				Room.Waiting.Clear();
				Room.Tickets.Clear();
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			try
			{
				SendLine(list[i].Client, "ERR host gone");
				list[i].Client.Close();
			}
			catch
			{
			}
			// Wake the waiting thread so it does not sit on its timeout; it sees Paired == false
			// and closes without sending a second error.
			list[i].Done.Set();
		}
		try
		{
			Room.Presence.Close();
		}
		catch
		{
		}
		Say("relay    room " + Room.Id + " closed (" + Reason + ")");
	}

	private void JoinRoom(TcpClient Client, string RoomId)
	{
		Room room;
		lock (_sync)
		{
			if (!_rooms.TryGetValue(RoomId, out room))
			{
				SendLine(Client, "ERR no such room");
				Client.Close();
				return;
			}
		}
		PendingClient pendingClient = new PendingClient();
		pendingClient.Client = Client;
		pendingClient.CreatedUtc = DateTime.UtcNow;
		pendingClient.Ticket = NewSecret(16);
		lock (room.Sync)
		{
			if (room.Waiting.Count >= MaxWaitingPerRoom)
			{
				SendLine(Client, "ERR room busy");
				Client.Close();
				return;
			}
			room.Waiting.Enqueue(pendingClient);
			room.Tickets[pendingClient.Ticket] = pendingClient;
		}
		SendLine(Client, "WAIT");
		// Nothing else to do on this thread: the socket stays parked until the host accepts the
		// ticket. Waiting on an event (rather than reading the socket) is what keeps the client's
		// connection alive without consuming bytes the pipe will need.
		try
		{
			pendingClient.Done.WaitOne((TicketSeconds + 5) * 1000);
		}
		catch
		{
		}
		bool flag;
		lock (room.Sync)
		{
			flag = pendingClient.Paired;
			if (!flag)
			{
				room.Tickets.Remove(pendingClient.Ticket);
			}
		}
		if (!flag)
		{
			// Not paired: the host never took it (or the room closed and the closer already told
			// the client why). Closing here is safe because no pipe owns the socket.
			try
			{
				SendLine(Client, "ERR timed out");
				Client.Close();
			}
			catch
			{
			}
		}
	}

	private void Poll(TcpClient Client, string Token)
	{
		Room room;
		lock (_sync)
		{
			room = FindByToken(Token);
		}
		if (room == null)
		{
			SendLine(Client, "ERR no such room");
			Client.Close();
			return;
		}
		List<string> list = new List<string>();
		lock (room.Sync)
		{
			foreach (PendingClient value in room.Waiting)
			{
				list.Add(value.Ticket);
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < list.Count; i++)
		{
			stringBuilder.Append("JOIN ").Append(list[i]).Append('\n');
		}
		stringBuilder.Append("END\n");
		Send(Client, stringBuilder.ToString());
		Client.Close();
	}

	private void Accept(TcpClient Client, string Ticket)
	{
		Room room = null;
		PendingClient pendingClient = null;
		lock (_sync)
		{
			foreach (Room value in _rooms.Values)
			{
				lock (value.Sync)
				{
					if (value.Tickets.TryGetValue(Ticket, out pendingClient))
					{
						// One shot: a ticket that worked twice would let a second connection
						// attach to somebody else's session.
						value.Tickets.Remove(Ticket);
						room = value;
						break;
					}
				}
			}
		}
		if (room == null || pendingClient == null)
		{
			SendLine(Client, "ERR bad ticket");
			Client.Close();
			return;
		}
		lock (room.Sync)
		{
			// The waiting queue is a queue: remove the exact client, not "the next one".
			Queue<PendingClient> queue = new Queue<PendingClient>(room.Waiting.Count);
			while (room.Waiting.Count > 0)
			{
				PendingClient pendingClient2 = room.Waiting.Dequeue();
				if (pendingClient2 != pendingClient)
				{
					queue.Enqueue(pendingClient2);
				}
			}
			while (queue.Count > 0)
			{
				room.Waiting.Enqueue(queue.Dequeue());
			}
			pendingClient.Paired = true;
			room.PipeCount++;
		}
		pendingClient.Done.Set();
		Interlocked.Increment(ref _activePipes);
		Say("relay    room " + room.Id + " paired a client");
		Pipe(room, Client, pendingClient.Client);
		Interlocked.Decrement(ref _activePipes);
		lock (room.Sync)
		{
			room.PipeCount--;
		}
	}

	private int _activePipes;

	private Room FindByToken(string Token)
	{
		if (string.IsNullOrEmpty(Token))
		{
			return null;
		}
		foreach (Room value in _rooms.Values)
		{
			if (string.Equals(value.Token, Token, StringComparison.Ordinal))
			{
				return value;
			}
		}
		return null;
	}

	/// <summary>
	/// Copies bytes both ways until either side closes. No framing, no parsing: the game's own
	/// transport does that, and a relay that understood packets would have to be updated with it.
	/// </summary>
	private void Pipe(Room Room, TcpClient Host, TcpClient Client)
	{
		Thread thread = new Thread(delegate()
		{
			Copy(Room, Host, Client);
		});
		thread.IsBackground = true;
		thread.Name = "Relay.PipeDown";
		thread.Start();
		Copy(Room, Client, Host);
		try
		{
			thread.Join(2000);
		}
		catch
		{
		}
		try
		{
			Host.Close();
		}
		catch
		{
		}
		try
		{
			Client.Close();
		}
		catch
		{
		}
	}

	/// <summary>
	/// One direction of a pipe, with the per-room rate limit. The limit is applied by sleeping
	/// when a room goes over its budget for the current second, not by dropping bytes: a relay
	/// that silently loses data would corrupt a stream that has no way to recover.
	/// </summary>
	private void Copy(Room Room, TcpClient From, TcpClient To)
	{
		byte[] array = new byte[16384];
		try
		{
			NetworkStream stream = From.GetStream();
			NetworkStream stream2 = To.GetStream();
			DateTime utcNow = DateTime.UtcNow;
			long num = 0L;
			while (!_stopping)
			{
				int num2 = stream.Read(array, 0, array.Length);
				if (num2 <= 0)
				{
					break;
				}
				stream2.Write(array, 0, num2);
				Interlocked.Add(ref _totalBytes, num2);
				lock (Room.Sync)
				{
					Room.Bytes += num2;
				}
				num += num2;
				DateTime utcNow2 = DateTime.UtcNow;
				double totalSeconds = (utcNow2 - utcNow).TotalSeconds;
				if (totalSeconds >= 1.0)
				{
					if (num > MaxBytesPerSecondPerRoom)
					{
						// Throttle the rest of this second instead of dropping it.
						int num3 = (int)Math.Min(2000.0, (num - MaxBytesPerSecondPerRoom) * 1000.0 / MaxBytesPerSecondPerRoom);
						if (num3 > 0)
						{
							Thread.Sleep(num3);
						}
					}
					utcNow = utcNow2;
					num = 0L;
				}
			}
		}
		catch
		{
		}
		try
		{
			From.Close();
		}
		catch
		{
		}
		try
		{
			To.Close();
		}
		catch
		{
		}
	}

	private long _totalBytes;

	/// <summary>
	/// Drops rooms whose presence socket died without the read noticing, and tickets nobody used.
	/// A relay that leaks rooms eventually refuses everybody, and the failure looks like "the
	/// master is down" rather than "the relay is full".
	/// </summary>
	private void ReaperLoop()
	{
		while (!_stopping)
		{
			Thread.Sleep(5000);
			List<Room> list = new List<Room>();
			DateTime utcNow = DateTime.UtcNow;
			lock (_sync)
			{
				foreach (Room value in _rooms.Values)
				{
					lock (value.Sync)
					{
						List<string> list2 = new List<string>();
						foreach (KeyValuePair<string, PendingClient> ticket in value.Tickets)
						{
							if ((utcNow - ticket.Value.CreatedUtc).TotalSeconds > TicketSeconds)
							{
								list2.Add(ticket.Key);
							}
						}
						for (int i = 0; i < list2.Count; i++)
						{
							PendingClient pendingClient = value.Tickets[list2[i]];
							value.Tickets.Remove(list2[i]);
							try
							{
								SendLine(pendingClient.Client, "ERR timed out");
								pendingClient.Client.Close();
							}
							catch
							{
							}
							pendingClient.Done.Set();
						}
					}
					bool flag;
					try
					{
						flag = value.Presence == null || !value.Presence.Connected;
					}
					catch
					{
						flag = true;
					}
					if (flag)
					{
						list.Add(value);
					}
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				CloseRoom(list[j], "presence lost");
			}
		}
	}

	// --- text protocol helpers ----------------------------------------------

	private static string ReadLine(NetworkStream Stream)
	{
		StringBuilder stringBuilder = new StringBuilder(64);
		while (true)
		{
			int num = Stream.ReadByte();
			if (num < 0)
			{
				return (stringBuilder.Length == 0) ? null : stringBuilder.ToString();
			}
			if (num == 10)
			{
				return stringBuilder.ToString().TrimEnd('\r');
			}
			if (stringBuilder.Length > 512)
			{
				// A command longer than this is not ours; refusing it keeps a hostile client from
				// growing the buffer without limit.
				return null;
			}
			stringBuilder.Append((char)num);
		}
	}

	private static void SendLine(TcpClient Client, string Line)
	{
		Send(Client, Line + "\n");
	}

	private static void Send(TcpClient Client, string Text)
	{
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(Text);
			Client.GetStream().Write(bytes, 0, bytes.Length);
			Client.GetStream().Flush();
		}
		catch
		{
		}
	}

	/// <summary>Random hex, from the cryptographic generator: a guessable room or ticket is a takeover.</summary>
	private static string NewSecret(int Bytes)
	{
		byte[] array = new byte[Bytes];
		using (System.Security.Cryptography.RandomNumberGenerator randomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(array);
		}
		StringBuilder stringBuilder = new StringBuilder(Bytes * 2);
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(array[i].ToString("x2", CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}
}
