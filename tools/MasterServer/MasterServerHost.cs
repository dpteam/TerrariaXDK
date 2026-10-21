using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MasterServer;

/// <summary>One advertised world.</summary>
internal sealed class Entry
{
	public string Name = string.Empty;
	public string Host = string.Empty;
	public int Port;
	public int Players;
	public int MaxPlayers;
	public int Version;
	public bool Passworded;
	public string WorldName = string.Empty;
	public string Motd = string.Empty;
	public string ModSignature = string.Empty;
	public string Platform = string.Empty;

	/// <summary>
	/// Relay room this world can be reached through, or empty. Clients that see it dial the
	/// relay instead of <see cref="Host"/>/<see cref="Port"/>.
	/// </summary>
	public string RelayRoom = string.Empty;

	public DateTime LastSeenUtc;

	public string Key
	{
		get
		{
			return (Host + ":" + Port.ToString(CultureInfo.InvariantCulture)).ToLowerInvariant();
		}
	}

	public string Address
	{
		get
		{
			return Host + ":" + Port.ToString(CultureInfo.InvariantCulture);
		}
	}
}

/// <summary>
/// The master server the game talks to: a plain HTTP/1.1 endpoint that keeps a list of worlds.
///
/// Why it lives here and not in the game: it is one process for many players, and putting it in
/// the client would have made "hosted" and "listed" the same machine. See
/// <c>docs/MULTIPLAYER.md</c> section 7 for the protocol, and <c>Terraria/Net/MasterServerClient.cs</c>
/// for the side that has to agree with it.
///
/// Deliberately dependency-free: <c>TcpListener</c> instead of <c>HttpListener</c> (which needs an
/// administrative URL reservation on Windows), hand-written JSON (<c>System.Text.Json</c> is not
/// in net40), and no database - the list is a few dozen entries that expire in two minutes.
/// </summary>
internal sealed class MasterServerHost
{
	/// <summary>A listing older than this is dropped. Hosts re-announce every 45 s.</summary>
	public const int DefaultTtlSeconds = 120;

	/// <summary>Largest announce body accepted, so a stray POST cannot exhaust memory.</summary>
	private const int MaxBodyBytes = 64 * 1024;

	/// <summary>Upper bound on listed worlds. The client shows a list, not a directory service.</summary>
	private const int MaxEntries = 1000;

	private readonly int _requestedPort;
	private readonly int _ttlSeconds;
	private readonly string _dataPath;
	private readonly object _sync = new object();
	private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
	private int _totalAnnounces;
	private TcpListener _listener;
	private Thread _acceptThread;
	private volatile bool _stopping;
	private DateTime _startedUtc;

	/// <summary>Achievement and leaderboard aggregation. Same process, same console.</summary>
	private readonly PlayerStats _stats;

	/// <summary>Raised for every request and every change to the list, so the console can show it.</summary>
	public event Action<string> Log;

	/// <summary>Raised when the stored list changed, so the caller can persist it.</summary>
	public event Action Changed;

	/// <summary>Raised when the statistics changed (not the world list).</summary>
	public event Action StatsChangedEvent;

	public MasterServerHost(int port, int ttlSeconds, string dataPath)
	{
		_requestedPort = port;
		_ttlSeconds = (ttlSeconds > 0) ? ttlSeconds : DefaultTtlSeconds;
		_dataPath = dataPath;
		// Statistics live in their own file: the world list is disposable (every entry expires in
		// two minutes), while achievement counts are the accumulated result of everybody who ever
		// played. Keeping them together meant one cleanup could lose both.
		_stats = new PlayerStats(string.IsNullOrEmpty(dataPath) ? null : Path.ChangeExtension(dataPath, ".stats.json"));
	}

	/// <summary>The statistics store, for tests and for the console's shutdown line.</summary>
	public PlayerStats Stats => _stats;

	/// <summary>The port actually bound; differs from the request when 0 was asked for.</summary>
	public int Port { get; private set; }

	public int EntryCount
	{
		get
		{
			int count;
			List<Entry> expired;
			lock (_sync)
			{
				expired = PruneLocked();
				count = _entries.Count;
			}
			ReportExpired(expired);
			return count;
		}
	}

	public int TtlSeconds
	{
		get
		{
			return _ttlSeconds;
		}
	}

	public void Start()
	{
		_listener = new TcpListener(IPAddress.Any, _requestedPort);
		_listener.Start();
		Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
		_startedUtc = DateTime.UtcNow;
		_stopping = false;
		_acceptThread = new Thread(AcceptLoop)
		{
			IsBackground = true,
			Name = "MasterServer.Accept"
		};
		_acceptThread.Start();
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
		Save();
		_stats.Save();
	}

	private void AcceptLoop()
	{
		while (!_stopping)
		{
			TcpClient client;
			try
			{
				client = _listener.AcceptTcpClient();
			}
			catch
			{
				return;
			}
			// One thread per connection: the traffic is a handful of requests per minute from a
			// handful of hosts, and a pool would be more machinery than the problem needs.
			Thread thread = new Thread(delegate()
			{
				try
				{
					Serve(client);
				}
				catch (Exception ex)
				{
					Say("error: " + ex.GetType().Name + ": " + ex.Message);
				}
				finally
				{
					try
					{
						client.Close();
					}
					catch
					{
					}
				}
			});
			thread.IsBackground = true;
			thread.Start();
		}
	}

	/// <summary>Serves one HTTP request. Reads the request line and headers, then the body.</summary>
	private void Serve(TcpClient client)	{
		client.NoDelay = true;
		client.ReceiveTimeout = 10000;
		client.SendTimeout = 10000;
		using NetworkStream stream = client.GetStream();
		string remote = "?";
		try
		{
			remote = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
		}
		catch
		{
		}
		string requestLine;
		Dictionary<string, string> headers;
		if (!ReadHead(stream, out requestLine, out headers))
		{
			return;
		}
		string[] parts = requestLine.Split(' ');
		if (parts.Length < 2)
		{
			Respond(stream, 400, "application/json", "{\"error\":\"bad request line\"}");
			Say(remote + "  " + requestLine + "  -> 400");
			return;
		}
		string method = parts[0].ToUpperInvariant();
		string path = parts[1];
		string query = string.Empty;
		int num = path.IndexOf('?');
		if (num >= 0)
		{
			query = path.Substring(num + 1);
			path = path.Substring(0, num);
		}
		byte[] body;
		int contentLength;
		if (headers.TryGetValue("content-length", out string text) && int.TryParse(text, out contentLength) && contentLength > 0)
		{
			if (contentLength > MaxBodyBytes)
			{
				Respond(stream, 413, "application/json", "{\"error\":\"body too large\"}");
				Say(remote + "  " + method + " " + path + "  -> 413 (body " + contentLength + " bytes)");
				return;
			}
			body = new byte[contentLength];
			if (!ReadExactly(stream, body, contentLength))
			{
				return;
			}
		}
		else
		{
			body = new byte[0];
		}
		int num2;
		string text2;
		Route(method, path, query, body, remote, out num2, out text2, out string contentType);
		Respond(stream, num2, contentType, text2);
		if (query.Length != 0)
		{
			Say(remote + "  " + method + " " + path + "?" + query + "  -> " + num2);
		}
		else
		{
			Say(remote + "  " + method + " " + path + "  -> " + num2);
		}
	}

	private void Route(string method, string path, string query, byte[] body, string remote, out int status, out string response, out string contentType)
	{
		contentType = "application/json";
		string text = Encoding.UTF8.GetString(body);
		switch (path)
		{
		case "/v1/servers":
			if (method != "GET")
			{
				status = 405;
				response = "{\"error\":\"use GET\"}";
				return;
			}
			status = 200;
			response = ListJson();
			return;
		case "/v1/announce":
			if (method != "POST")
			{
				status = 405;
				response = "{\"error\":\"use POST\"}";
				return;
			}
			status = Announce(text, remote, out response);
			return;
		case "/v1/remove":
			if (method != "POST")
			{
				status = 405;
				response = "{\"error\":\"use POST\"}";
				return;
			}
			status = Remove(text, out response);
			return;
		case "/health":
			status = 200;
			response = HealthJson();
			return;
		case "/v1/stats":
			if (method != "POST")
			{
				status = 405;
				response = "{\"error\":\"use POST\"}";
				return;
			}
			status = ReportStats(text, out response);
			return;
		case "/v1/achievements":
			if (method != "GET")
			{
				status = 405;
				response = "{\"error\":\"use GET\"}";
				return;
			}
			status = 200;
			response = AchievementsJson();
			return;
		case "/v1/leaderboard":
			if (method != "GET")
			{
				status = 405;
				response = "{\"error\":\"use GET\"}";
				return;
			}
			status = LeaderboardJson(query, out response);
			return;
		case "/":
			status = 200;
			contentType = "text/html; charset=utf-8";
			response = StatusPage();
			return;
		default:
			status = 404;
			response = "{\"error\":\"no such endpoint\"}";
			return;
		}
	}

	/// <summary>
	/// Adds or refreshes one listing. Returns 200 with the stored address, 400 with the reason.
	///
	/// An empty <c>host</c> means "use the address I see", which is the only correct answer behind
	/// NAT: the host does not know its own public address. That is what the docs promise, and it is
	/// why the remote address is a parameter rather than something the body must carry.
	/// </summary>
	private int Announce(string body, string remote, out string response)
	{
		string error;
		Dictionary<string, object> dictionary = Json.ParseObject(body, out error);
		if (dictionary == null)
		{
			response = "{\"error\":" + Json.Quote(error) + "}";
			return 400;
		}
		int num = Json.GetInt(dictionary, "port", 0);
		if (num <= 0 || num > 65535)
		{
			response = "{\"error\":\"port must be 1-65535\"}";
			return 400;
		}
		string text = (Json.GetString(dictionary, "host") ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			// Behind NAT the host cannot know this; the address the request came from is the truth.
			text = remote;
		}
		Entry entry = new Entry();
		entry.Host = text;
		entry.Port = num;
		entry.Name = Clean(Json.GetString(dictionary, "name"), 64, text + ":" + num);
		entry.Players = Clamp(Json.GetInt(dictionary, "players", 1), 0, 255);
		entry.MaxPlayers = Clamp(Json.GetInt(dictionary, "maxPlayers", 8), 1, 255);
		entry.Version = Clamp(Json.GetInt(dictionary, "version", 0), 0, 99);
		entry.Passworded = Json.GetBool(dictionary, "passworded");
		entry.WorldName = Clean(Json.GetString(dictionary, "worldName"), 64, string.Empty);
		entry.Motd = Clean(Json.GetString(dictionary, "motd"), 200, string.Empty);
		entry.ModSignature = Clean(Json.GetString(dictionary, "modSignature"), 64, string.Empty);
		entry.Platform = Clean(Json.GetString(dictionary, "platform"), 16, string.Empty);
		// A relayed host publishes the room it can be reached through. The id is opaque and the
		// relay itself validates it, so there is nothing to check beyond its length.
		entry.RelayRoom = Clean(Json.GetString(dictionary, "relayRoom"), 64, string.Empty);
		entry.LastSeenUtc = DateTime.UtcNow;
		bool flag;
		string announceMessage = null;
		List<Entry> expired;
		lock (_sync)
		{
			expired = PruneLocked();
			flag = !_entries.ContainsKey(entry.Key);
			if (flag && _entries.Count >= MaxEntries)
			{
				response = "{\"error\":\"the list is full\"}";
				ReportExpired(expired);
				return 503;
			}
			bool flag2 = false;
			Entry existing;
			if (!flag && _entries.TryGetValue(entry.Key, out existing))
			{
				// A host that re-announces with a different player count is an update, not a new
				// world; keeping the first-seen order is what stops the list from reshuffling.
				flag2 = existing.Players != entry.Players || existing.MaxPlayers != entry.MaxPlayers
					|| !existing.Name.Equals(entry.Name, StringComparison.Ordinal) || existing.Passworded != entry.Passworded;
				entry.LastSeenUtc = DateTime.UtcNow;
			}
			_entries[entry.Key] = entry;
			_totalAnnounces++;
			if (flag || flag2)
			{
				announceMessage = (flag ? "listed   " : "updated  ") + entry.Address + "  '" + entry.Name + "'  "
					+ entry.Players + "/" + entry.MaxPlayers + (entry.Passworded ? "  [pw]" : string.Empty);
			}
		}
		// Logged outside the lock: the console writer must never run while the list is held.
		ReportExpired(expired);
		if (announceMessage != null)
		{
			Say(announceMessage);
		}
		RaiseChanged();
		response = "{\"ok\":true,\"address\":" + Json.Quote(entry.Address) + "}";
		return 200;
	}

	private int Remove(string body, out string response)
	{
		string error;
		Dictionary<string, object> dictionary = Json.ParseObject(body, out error);
		if (dictionary == null)
		{
			response = "{\"error\":" + Json.Quote(error) + "}";
			return 400;
		}
		string text = (Json.GetString(dictionary, "host") ?? string.Empty).Trim();
		int num = Json.GetInt(dictionary, "port", 0);
		if (text.Length == 0 || num <= 0)
		{
			response = "{\"error\":\"host and port are required\"}";
			return 400;
		}
		string key = (text + ":" + num.ToString(CultureInfo.InvariantCulture)).ToLowerInvariant();
		bool flag;
		lock (_sync)
		{
			flag = _entries.Remove(key);
		}
		if (flag)
		{
			Say("removed  " + text + ":" + num);
			RaiseChanged();
		}
		response = "{\"ok\":true,\"removed\":" + (flag ? "true" : "false") + "}";
		return 200;
	}

	/// <summary>The list as the client expects it. Expired entries are pruned first.</summary>
	private string ListJson()
	{
		List<Entry> list;
		List<Entry> expired;
		lock (_sync)
		{
			expired = PruneLocked();
			list = new List<Entry>(_entries.Values);
		}
		ReportExpired(expired);
		// Oldest announcement first: a stable order, so the client list does not jump between two
		// requests.
		list.Sort(delegate(Entry a, Entry b)
		{
			int num = a.LastSeenUtc.CompareTo(b.LastSeenUtc);
			return (num != 0) ? num : string.CompareOrdinal(a.Key, b.Key);
		});
		StringBuilder stringBuilder = new StringBuilder(256 + list.Count * 200);
		stringBuilder.Append("{\"servers\":[");
		for (int i = 0; i < list.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			Entry entry = list[i];
			int num = (int)Math.Max(0.0, (DateTime.UtcNow - entry.LastSeenUtc).TotalSeconds);
			stringBuilder.Append('{');
			stringBuilder.Append("\"name\":").Append(Json.Quote(entry.Name)).Append(',');
			stringBuilder.Append("\"host\":").Append(Json.Quote(entry.Host)).Append(',');
			stringBuilder.Append("\"port\":").Append(entry.Port.ToString(CultureInfo.InvariantCulture)).Append(',');
			stringBuilder.Append("\"players\":").Append(entry.Players.ToString(CultureInfo.InvariantCulture)).Append(',');
			stringBuilder.Append("\"maxPlayers\":").Append(entry.MaxPlayers.ToString(CultureInfo.InvariantCulture)).Append(',');
			stringBuilder.Append("\"version\":").Append(entry.Version.ToString(CultureInfo.InvariantCulture)).Append(',');
			stringBuilder.Append("\"passworded\":").Append(entry.Passworded ? "true" : "false").Append(',');
			stringBuilder.Append("\"worldName\":").Append(Json.Quote(entry.WorldName)).Append(',');
			stringBuilder.Append("\"motd\":").Append(Json.Quote(entry.Motd)).Append(',');
			stringBuilder.Append("\"modSignature\":").Append(Json.Quote(entry.ModSignature));
			// The relay room is not "extra": a client that ignores it cannot reach a relayed
			// host at all, because there is no address to dial.
			stringBuilder.Append(",\"relayRoom\":").Append(Json.Quote(entry.RelayRoom));
			// Extra fields, ignored by the client: useful when reading the list by hand.
			stringBuilder.Append(",\"platform\":").Append(Json.Quote(entry.Platform));
			stringBuilder.Append(",\"ageSeconds\":").Append(num.ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append('}');
		}
		stringBuilder.Append("]}");
		return stringBuilder.ToString();
	}

	private string HealthJson()
	{
		int num = EntryCount;
		return "{\"ok\":true,\"servers\":" + num.ToString(CultureInfo.InvariantCulture)
			+ ",\"ttlSeconds\":" + _ttlSeconds.ToString(CultureInfo.InvariantCulture)
			+ ",\"announces\":" + _totalAnnounces.ToString(CultureInfo.InvariantCulture)
			+ ",\"players\":" + _stats.PlayerCount.ToString(CultureInfo.InvariantCulture)
			+ ",\"uptimeSeconds\":" + ((int)Math.Max(0.0, (DateTime.UtcNow - _startedUtc).TotalSeconds)).ToString(CultureInfo.InvariantCulture)
			+ "}";
	}

	/// <summary>
	/// <c>POST /v1/stats</c>: <c>{"gamertag":..,"achievements":[0,5],"stats":{"AirTravel":1234}}</c>.
	///
	/// Achievements as an array of indices rather than a map of flags: the client has a fixed list
	/// and only ever needs to say which entries it has, so the shorter form is also the honest one.
	/// </summary>
	private int ReportStats(string body, out string response)
	{
		string error;
		Dictionary<string, object> dictionary = Json.ParseObject(body, out error);
		if (dictionary == null)
		{
			response = "{\"error\":" + Json.Quote(error) + "}";
			return 400;
		}
		string text = Json.GetString(dictionary, "gamertag");
		if (string.IsNullOrEmpty(text))
		{
			response = "{\"error\":\"gamertag is required\"}";
			return 400;
		}
		List<int> list = new List<int>();
		List<object> array = Json.GetArray(dictionary, "achievements");
		if (array != null)
		{
			for (int i = 0; i < array.Count; i++)
			{
				string text2 = (array[i] == null) ? null : Convert.ToString(array[i], CultureInfo.InvariantCulture);
				int result;
				if (text2 != null && int.TryParse(text2, out result))
				{
					list.Add(result);
				}
			}
		}
		Dictionary<string, long> dictionary2 = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, object> obj = Json.GetObject(dictionary, "stats");
		if (obj != null)
		{
			foreach (KeyValuePair<string, object> item in obj)
			{
				string text3 = (item.Value == null) ? null : Convert.ToString(item.Value, CultureInfo.InvariantCulture);
				long result2;
				if (text3 != null && long.TryParse(text3, out result2))
				{
					dictionary2[item.Key] = result2;
				}
			}
		}
		string text4;
		int num = _stats.Report(text, list.ToArray(), dictionary2, out text4);
		if (num < 0)
		{
			response = "{\"error\":" + Json.Quote(text4) + "}";
			return 400;
		}
		StatsChanged();
		if (num > 0)
		{
			// Only when something changed: this endpoint is called on every session end, and a line
			// per call would bury the server list.
			Say("stats    " + text.Trim() + ": +" + num + " achievement(s), " + dictionary2.Count + " stat(s)");
		}
		response = "{\"ok\":true,\"newAchievements\":" + num.ToString(CultureInfo.InvariantCulture) + "}";
		return 200;
	}

	private string AchievementsJson()
	{
		int num;
		int[] array = _stats.AchievementCounts(out num);
		StringBuilder stringBuilder = new StringBuilder(64 + array.Length * 12);
		stringBuilder.Append("{\"totalPlayers\":").Append(num.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append(",\"counts\":[");
		for (int i = 0; i < array.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(array[i].ToString(CultureInfo.InvariantCulture));
		}
		stringBuilder.Append("]}");
		return stringBuilder.ToString();
	}

	/// <summary>
	/// <c>GET /v1/leaderboard?stats=Ore,Gems&amp;limit=50</c>: the ranking for a board.
	///
	/// Named statistics rather than a board number: the board list belongs to the game, and
	/// numbering it in the server would mean changing the server every time a column is added.
	/// Several names at once because a board is a table - the screen needs every column, and
	/// asking for them one at a time would put four requests on one refresh.
	///
	/// Rows are ranked by the sum of the requested statistics, which is the same rating the
	/// console build submitted as a board entry.
	/// </summary>
	private int LeaderboardJson(string query, out string response)
	{
		string text = QueryValue(query, "stats");
		if (text == null)
		{
			text = QueryValue(query, "stat");
		}
		int result;
		if (!int.TryParse(QueryValue(query, "limit"), out result) || result <= 0)
		{
			result = 50;
		}
		if (result > 500)
		{
			result = 500;
		}
		if (string.IsNullOrEmpty(text))
		{
			response = "{\"error\":\"stats is required, e.g. ?stats=Ore,Gems\"}";
			return 400;
		}
		string[] array = text.Split(',');
		List<string> list = new List<string>(array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length != 0)
			{
				list.Add(text2);
			}
		}
		if (list.Count == 0)
		{
			response = "{\"error\":\"stats is required, e.g. ?stats=Ore,Gems\"}";
			return 400;
		}
		List<PlayerRecord> rows = _stats.RankBySum(list, result);
		StringBuilder stringBuilder = new StringBuilder(128 + rows.Count * (64 + list.Count * 12));
		stringBuilder.Append("{\"stats\":[");
		for (int j = 0; j < list.Count; j++)
		{
			if (j > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(Json.Quote(list[j]));
		}
		stringBuilder.Append("],\"rows\":[");
		for (int k = 0; k < rows.Count; k++)
		{
			PlayerRecord playerRecord = rows[k];
			if (k > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append("{\"rank\":").Append((k + 1).ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append(",\"gamertag\":").Append(Json.Quote(playerRecord.Gamertag));
			stringBuilder.Append(",\"values\":[");
			for (int l = 0; l < list.Count; l++)
			{
				if (l > 0)
				{
					stringBuilder.Append(',');
				}
				long num;
				playerRecord.Stats.TryGetValue(list[l], out num);
				stringBuilder.Append(num.ToString(CultureInfo.InvariantCulture));
			}
			stringBuilder.Append("]}");
		}
		stringBuilder.Append("]}");
		response = stringBuilder.ToString();
		return 200;
	}

	/// <summary>One value from a query string, url-decoded enough for names and numbers.</summary>
	internal static string QueryValue(string query, string key)
	{
		if (string.IsNullOrEmpty(query))
		{
			return null;
		}
		string[] array = query.Split('&');
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i].IndexOf('=');
			if (num > 0 && array[i].Substring(0, num).Equals(key, StringComparison.OrdinalIgnoreCase))
			{
				return Uri.UnescapeDataString(array[i].Substring(num + 1));
			}
		}
		return null;
	}

	/// <summary>A human page, so "is it running?" does not need a JSON client.</summary>
	private string StatusPage()
	{
		List<Entry> list;
		List<Entry> expired;
		lock (_sync)
		{
			expired = PruneLocked();
			list = new List<Entry>(_entries.Values);
		}
		ReportExpired(expired);
		list.Sort(delegate(Entry a, Entry b)
		{
			return a.LastSeenUtc.CompareTo(b.LastSeenUtc);
		});
		StringBuilder stringBuilder = new StringBuilder(2048);
		stringBuilder.Append("<!doctype html><meta charset=\"utf-8\"><title>TerrariaXDK master server</title>");
		// Refresh so the page shows the truth while it is open, without a script.
		stringBuilder.Append("<meta http-equiv=\"refresh\" content=\"5\">");
		stringBuilder.Append("<style>body{background:#0e121a;color:#d6deea;font:14px Consolas,monospace;padding:24px}");
		stringBuilder.Append("table{border-collapse:collapse}td,th{padding:4px 14px 4px 0;text-align:left}");
		stringBuilder.Append("th{color:#7f93b0;font-weight:400}.pw{color:#ffbe78}.ok{color:#7ee08a}</style>");
		stringBuilder.Append("<h2>TerrariaXDK master server</h2>");
		stringBuilder.Append("<p class=\"ok\">running</p>");
		stringBuilder.Append("<p>port <b>").Append(Port).Append("</b> &middot; ttl <b>").Append(_ttlSeconds)
			.Append("s</b> &middot; uptime <b>").Append((int)Math.Max(0.0, (DateTime.UtcNow - _startedUtc).TotalSeconds)).Append("s</b></p>");
		stringBuilder.Append("<p>").Append(list.Count).Append(" server(s) listed</p>");
		if (list.Count == 0)
		{
			stringBuilder.Append("<p>Nobody is hosting right now. A world appears here within 45 seconds of a host enabling &quot;Publish to master&quot;.</p>");
		}
		else
		{
			stringBuilder.Append("<table><tr><th>name</th><th>address</th><th>players</th><th>world</th><th>version</th><th>age</th></tr>");
			for (int i = 0; i < list.Count; i++)
			{
				Entry entry = list[i];
				stringBuilder.Append("<tr><td>").Append(Html(entry.Name)).Append("</td><td>").Append(Html(entry.Address)).Append("</td><td>")
					.Append(entry.Players).Append('/').Append(entry.MaxPlayers).Append("</td><td>").Append(Html(entry.WorldName))
					.Append("</td><td>").Append(entry.Version);
				if (entry.Passworded)
				{
					stringBuilder.Append(" <span class=\"pw\">[pw]</span>");
				}
				stringBuilder.Append("</td><td>").Append((int)Math.Max(0.0, (DateTime.UtcNow - entry.LastSeenUtc).TotalSeconds)).Append("s ago</td></tr>");
			}
			stringBuilder.Append("</table>");
		}
		stringBuilder.Append("<p>/v1/servers &middot; /v1/announce &middot; /v1/remove &middot; /health</p>");
		return stringBuilder.ToString();
	}

	private static string Html(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}

	/// <summary>
	/// Drops listings that stopped announcing, and returns them so the caller can log after the
	/// lock is released. Must be called under <c>_sync</c>.
	/// </summary>
	private List<Entry> PruneLocked()
	{
		List<Entry> list = new List<Entry>();
		List<string> list2 = new List<string>();
		DateTime utcNow = DateTime.UtcNow;
		foreach (KeyValuePair<string, Entry> entry in _entries)
		{
			if ((utcNow - entry.Value.LastSeenUtc).TotalSeconds > _ttlSeconds)
			{
				list2.Add(entry.Key);
			}
		}
		for (int i = 0; i < list2.Count; i++)
		{
			list.Add(_entries[list2[i]]);
			_entries.Remove(list2[i]);
		}
		return list;
	}

	/// <summary>Logs expired listings. Called outside the lock, because the log handler is foreign code.</summary>
	private void ReportExpired(List<Entry> expired)
	{
		if (expired == null)
		{
			return;
		}
		for (int i = 0; i < expired.Count; i++)
		{
			Say("expired  " + expired[i].Address + "  '" + expired[i].Name + "'  (no announce for " + _ttlSeconds + "s)");
		}
		if (expired.Count > 0)
		{
			RaiseChanged();
		}
	}

	// --- persistence --------------------------------------------------------

	/// <summary>
	/// Writes the list next to the executable. Not required for correctness - every entry is
	/// refreshed within 45 seconds - but it means a restart during a long session does not blank
	/// the list for two minutes.
	/// </summary>
	public void Save()
	{
		if (string.IsNullOrEmpty(_dataPath))
		{
			return;
		}
		try
		{
			List<Entry> list;
			lock (_sync)
			{
				list = new List<Entry>(_entries.Values);
			}
			StringBuilder stringBuilder = new StringBuilder(1024);
			stringBuilder.Append("{\"savedUtc\":").Append(Json.Quote(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
			stringBuilder.Append(",\"servers\":[");
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				Entry entry = list[i];
				stringBuilder.Append("{\"name\":").Append(Json.Quote(entry.Name));
				stringBuilder.Append(",\"host\":").Append(Json.Quote(entry.Host));
				stringBuilder.Append(",\"port\":").Append(entry.Port.ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(",\"players\":").Append(entry.Players.ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(",\"maxPlayers\":").Append(entry.MaxPlayers.ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(",\"version\":").Append(entry.Version.ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(",\"passworded\":").Append(entry.Passworded ? "true" : "false");
				stringBuilder.Append(",\"worldName\":").Append(Json.Quote(entry.WorldName));
				stringBuilder.Append(",\"motd\":").Append(Json.Quote(entry.Motd));
				stringBuilder.Append(",\"modSignature\":").Append(Json.Quote(entry.ModSignature));
				stringBuilder.Append(",\"platform\":").Append(Json.Quote(entry.Platform));
				stringBuilder.Append(",\"lastSeenUtc\":").Append(Json.Quote(entry.LastSeenUtc.ToString("o", CultureInfo.InvariantCulture)));
				stringBuilder.Append('}');
			}
			stringBuilder.Append("]}");
			File.WriteAllText(_dataPath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Say("could not save " + _dataPath + ": " + ex.Message);
		}
	}

	// --- helpers ------------------------------------------------------------

	private static string Clean(string text, int maxLength, string fallback)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return fallback ?? string.Empty;
		}
		text = text.Trim();
		// Control characters would corrupt the JSON list page and the console; the length cap
		// keeps one hostile announce from filling the list response.
		StringBuilder stringBuilder = new StringBuilder(Math.Min(text.Length, maxLength));
		for (int i = 0; i < text.Length && stringBuilder.Length < maxLength; i++)
		{
			stringBuilder.Append((text[i] < ' ') ? ' ' : text[i]);
		}
		return stringBuilder.ToString();
	}

	private static int Clamp(int value, int min, int max)
	{
		return (value < min) ? min : ((value > max) ? max : value);
	}

	private void Say(string message)
	{
		Action<string> log = Log;
		if (log != null)
		{
			log(message);
		}
	}

	private void RaiseChanged()
	{
		Action changed = Changed;
		if (changed != null)
		{
			changed();
		}
	}

	private void StatsChanged()
	{
		Action action = StatsChangedEvent;
		if (action != null)
		{
			action();
		}
	}

	// --- HTTP plumbing ------------------------------------------------------

	private static bool ReadHead(NetworkStream stream, out string requestLine, out Dictionary<string, string> headers)
	{
		requestLine = null;
		headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		StringBuilder stringBuilder = new StringBuilder(256);
		int num = 0;
		int num2 = stream.ReadByte();
		while (num2 >= 0)
		{
			stringBuilder.Append((char)num2);
			if (stringBuilder.Length >= 2 && stringBuilder[stringBuilder.Length - 1] == '\n' && stringBuilder[stringBuilder.Length - 2] == '\r')
			{
				string text = stringBuilder.ToString(0, stringBuilder.Length - 2);
				stringBuilder.Length = 0;
				if (num == 0)
				{
					requestLine = text;
				}
				else
				{
					int num3 = text.IndexOf(':');
					if (num3 > 0)
					{
						headers[text.Substring(0, num3).Trim()] = text.Substring(num3 + 1).Trim();
					}
				}
				num++;
				if (text.Length == 0)
				{
					return requestLine != null;
				}
				if (num > 64 || stringBuilder.Length > 8192)
				{
					return false;
				}
			}
			num2 = stream.ReadByte();
		}
		return false;
	}

	private static bool ReadExactly(NetworkStream stream, byte[] buffer, int count)
	{
		int num = 0;
		while (num < count)
		{
			int num2 = stream.Read(buffer, num, count - num);
			if (num2 <= 0)
			{
				return false;
			}
			num += num2;
		}
		return true;
	}

	private static void Respond(NetworkStream stream, int status, string contentType, string body)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
		string text = (status == 200) ? "200 OK" : ((status == 400) ? "400 Bad Request"
			: ((status == 404) ? "404 Not Found" : ((status == 405) ? "405 Method Not Allowed"
			: ((status == 413) ? "413 Payload Too Large" : "503 Service Unavailable"))));
		StringBuilder stringBuilder = new StringBuilder(160);
		stringBuilder.Append("HTTP/1.1 ").Append(text).Append("\r\n");
		stringBuilder.Append("Content-Type: ").Append(contentType).Append("\r\n");
		stringBuilder.Append("Content-Length: ").Append(bytes.Length).Append("\r\n");
		stringBuilder.Append("Cache-Control: no-store\r\n");
		stringBuilder.Append("Connection: close\r\n\r\n");
		byte[] bytes2 = Encoding.ASCII.GetBytes(stringBuilder.ToString());
		try
		{
			stream.Write(bytes2, 0, bytes2.Length);
			stream.Write(bytes, 0, bytes.Length);
			stream.Flush();
		}
		catch
		{
		}
	}
}
