using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MasterServer;

/// <summary>
/// Protocol checks against a temporary instance on a free port.
///
/// The point is not to test the HTTP plumbing but the contract the game depends on: that an
/// announce is stored and listed, that a re-announce updates instead of duplicating, that an
/// empty host is filled from the connection (the only correct answer behind NAT), that a stale
/// listing expires, and that the error paths answer with a status instead of dropping the socket.
/// A client that gets its expectations wrong here shows up as an empty server list in game, which
/// is almost impossible to diagnose from the outside.
/// </summary>
internal static class SelfTest
{
	private static int _passed;

	private static int _failed;

	public static int Run()
	{
		Console.WriteLine("master server self-test");
		Console.WriteLine(new string('-', 23));
		MasterServerHost host = new MasterServerHost(0, 2, null);
		try
		{
			host.Start();
		}
		catch (Exception ex)
		{
			Console.WriteLine("FAIL could not start: " + ex.Message);
			return 1;
		}
		try
		{
			string text = "http://127.0.0.1:" + host.Port;
			int num;
			string text2;
			Request(text + "/v1/servers", "GET", null, out num, out text2);
			Check("empty list", num == 200 && text2.Contains("\"servers\":[]"), "status " + num + ": " + Trim(text2));

			Request(text + "/v1/announce", "POST",
				"{\"name\":\"Test world\",\"host\":\"1.2.3.4\",\"port\":7777,\"players\":2,\"maxPlayers\":8,"
				+ "\"version\":3,\"passworded\":true,\"worldName\":\"World\",\"motd\":\"hi\",\"modSignature\":\"abc\",\"platform\":\"windows\"}",
				out num, out text2);
			Check("announce accepted", num == 200 && text2.Contains("1.2.3.4:7777"), "status " + num + ": " + Trim(text2));

			Request(text + "/v1/servers", "GET", null, out num, out text2);
			Check("announce listed", num == 200 && text2.Contains("\"host\":\"1.2.3.4\"") && text2.Contains("\"players\":2"),
				Trim(text2));
			Check("fields preserved", text2.Contains("\"passworded\":true") && text2.Contains("\"version\":3")
				&& text2.Contains("\"modSignature\":\"abc\"") && text2.Contains("\"worldName\":\"World\""), Trim(text2));

			// Re-announce: one entry, updated player count, not two entries.
			Request(text + "/v1/announce", "POST",
				"{\"name\":\"Test world\",\"host\":\"1.2.3.4\",\"port\":7777,\"players\":5,\"maxPlayers\":8,\"version\":3}",
				out num, out text2);
			Request(text + "/v1/servers", "GET", null, out num, out text2);
			Check("re-announce updates in place", Count(text2, "\"host\":") == 1 && text2.Contains("\"players\":5"), Trim(text2));

			// Empty host: the master must use the address it sees.
			Request(text + "/v1/announce", "POST", "{\"name\":\"NAT world\",\"host\":\"\",\"port\":7777,\"players\":1}",
				out num, out text2);
			Request(text + "/v1/servers", "GET", null, out num, out text2);
			Check("empty host filled from the connection", text2.Contains("\"host\":\"127.0.0.1\""), Trim(text2));

			// Two entries now; the port is part of the key, so the same host on another port is a
			// different world.
			Request(text + "/v1/announce", "POST", "{\"name\":\"Second\",\"host\":\"127.0.0.1\",\"port\":7778,\"players\":1}",
				out num, out text2);
			Request(text + "/v1/servers", "GET", null, out num, out text2);
			Check("a different port is a different world", Count(text2, "\"host\":") == 3, Trim(text2));

			Request(text + "/v1/remove", "POST", "{\"host\":\"127.0.0.1\",\"port\":7778}", out num, out text2);
			Check("remove works", num == 200 && text2.Contains("\"removed\":true"), "status " + num + ": " + Trim(text2));
			Request(text + "/v1/servers", "GET", null, out num, out text2);
			Check("removed entry is gone", Count(text2, "\"host\":") == 2, Trim(text2));

			// Errors answer with a status and a reason, not a dropped connection.
			Request(text + "/v1/announce", "POST", "{\"host\":\"1.2.3.4\",\"port\":0}", out num, out text2);
			Check("port 0 refused", num == 400 && text2.Contains("port"), "status " + num + ": " + Trim(text2));
			Request(text + "/v1/announce", "POST", "not json", out num, out text2);
			Check("bad json refused", num == 400, "status " + num + ": " + Trim(text2));
			Request(text + "/v1/announce", "GET", null, out num, out text2);
			Check("wrong method refused", num == 405, "status " + num + ": " + Trim(text2));
			Request(text + "/nope", "GET", null, out num, out text2);
			Check("unknown path is 404", num == 404, "status " + num + ": " + Trim(text2));

			Request(text + "/health", "GET", null, out num, out text2);
			Check("health reports the count", num == 200 && text2.Contains("\"servers\":2") && text2.Contains("\"ttlSeconds\":2"),
				Trim(text2));
			Request(text + "/", "GET", null, out num, out text2);
			Check("status page is html", num == 200 && text2.Contains("<table>") && text2.Contains("Test world"), Trim(text2));

			// TTL: the instance was started with 2 seconds, so both listings must expire.
			Thread.Sleep(2200);
			Request(text + "/v1/servers", "GET", null, out num, out text2);
			Check("stale listings expire", text2.Contains("\"servers\":[]"), Trim(text2));

			// --- achievements and leaderboards -------------------------------
			Request(text + "/v1/achievements", "GET", null, out num, out text2);
			Check("no players, no counts", num == 200 && text2.Contains("\"totalPlayers\":0") && text2.Contains("\"counts\":[]"), Trim(text2));

			Request(text + "/v1/stats", "POST",
				"{\"gamertag\":\"Alice\",\"achievements\":[0,2,5],\"stats\":{\"AirTravel\":1200,\"Deaths\":3}}",
				out num, out text2);
			Check("stats accepted", num == 200 && text2.Contains("\"newAchievements\":3"), "status " + num + ": " + Trim(text2));

			Request(text + "/v1/stats", "POST",
				"{\"gamertag\":\"Bob\",\"achievements\":[2],\"stats\":{\"AirTravel\":900}}", out num, out text2);
			Check("second player accepted", num == 200 && text2.Contains("\"newAchievements\":1"), Trim(text2));

			Request(text + "/v1/achievements", "GET", null, out num, out text2);
			// Achievement 2 is held by both, 0 and 5 by Alice only.
			Check("counts per achievement", text2.Contains("\"totalPlayers\":2") && text2.Contains("[1,0,2,0,0,1]"), Trim(text2));

			// Re-reporting the same achievements adds nothing; a lower stat does not lower the record.
			Request(text + "/v1/stats", "POST",
				"{\"gamertag\":\"Alice\",\"achievements\":[0,2,5],\"stats\":{\"AirTravel\":50}}", out num, out text2);
			Check("re-report adds nothing", text2.Contains("\"newAchievements\":0"), Trim(text2));
			Request(text + "/v1/leaderboard?stat=AirTravel", "GET", null, out num, out text2);
			Check("stats keep the maximum",
				text2.Contains("\"gamertag\":\"Alice\",\"values\":[1200]") && text2.Contains("\"rank\":1"), Trim(text2));
			Check("ranking is sorted", text2.IndexOf("Alice", StringComparison.Ordinal) < text2.IndexOf("Bob", StringComparison.Ordinal), Trim(text2));

			Request(text + "/v1/leaderboard?stat=Deaths", "GET", null, out num, out text2);
			Check("a stat only some players have", text2.Contains("{\"rank\":1,\"gamertag\":\"Alice\",\"values\":[3]}") && !text2.Contains("Bob"), Trim(text2));

			// A board is a table: every column at once, ranked by their sum. Bob has 900 AirTravel
			// and 0 Deaths, Alice 1200 and 3, so Alice leads on the sum as well - but the columns
			// come back in the order that was asked for, not in ranking order.
			Request(text + "/v1/leaderboard?stats=AirTravel,Deaths", "GET", null, out num, out text2);
			Check("a board returns every column",
				text2.Contains("\"stats\":[\"AirTravel\",\"Deaths\"]") && text2.Contains("\"values\":[1200,3]"), Trim(text2));
			Check("a board ranks by the sum",
				text2.IndexOf("Alice", StringComparison.Ordinal) < text2.IndexOf("Bob", StringComparison.Ordinal)
				&& text2.Contains("\"values\":[900,0]"), Trim(text2));

			Request(text + "/v1/leaderboard", "GET", null, out num, out text2);
			Check("leaderboard needs a stat name", num == 400, "status " + num + ": " + Trim(text2));
			Request(text + "/v1/leaderboard?stat=NoSuchStat", "GET", null, out num, out text2);
			Check("an unknown stat is an empty ranking", num == 200 && text2.Contains("\"rows\":[]"), Trim(text2));

			Request(text + "/v1/stats", "POST", "{\"achievements\":[1]}", out num, out text2);
			Check("stats need a gamertag", num == 400, "status " + num + ": " + Trim(text2));
			Request(text + "/v1/stats", "GET", null, out num, out text2);
			Check("stats refuse GET", num == 405, "status " + num + ": " + Trim(text2));

			Request(text + "/health", "GET", null, out num, out text2);
			Check("health counts players", text2.Contains("\"players\":2"), Trim(text2));

			// --- relay ---------------------------------------------------------
			// The relay is what lets somebody behind CGNAT host at all. These checks drive the
			// real protocol over real sockets: a room opens, a client waits, the host collects the
			// ticket, and after ACCEPT the two sockets must be a transparent pipe - the game's own
			// framing is the only framing, so arbitrary bytes have to come back unchanged.
			RelayChecks();
		}
		catch (Exception ex)
		{
			Check("self-test did not throw", false, ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			host.Stop();
		}
		Console.WriteLine();
		Console.WriteLine("Passed: " + _passed + "  Failed: " + _failed);
		return (_failed == 0) ? 0 : 1;
	}

	/// <summary>
	/// The relay, driven the way the game will drive it. A fake host registers a room, a fake
	/// client joins and waits, the host polls for the ticket and accepts it, and then both sides
	/// exchange bytes that must arrive unchanged in both directions.
	/// </summary>
	private static void RelayChecks()
	{
		RelayHost relay = new RelayHost(0);
		try
		{
			relay.Start();
		}
		catch (Exception ex)
		{
			Check("relay starts", false, ex.Message);
			return;
		}
		try
		{
			string token = "0123456789abcdef0123456789abcdef";
			TcpClient tcpClient = RelayConnect(relay.Port);
			RelaySend(tcpClient, "HOST short");
			string text = RelayLine(tcpClient);
			Check("relay refuses a short token", text == "ERR token too short", text);

			// A room, and its id comes back.
			TcpClient tcpClient2 = RelayConnect(relay.Port);
			RelaySend(tcpClient2, "HOST " + token);
			string text2 = RelayLine(tcpClient2);
			Check("relay opens a room", text2 != null && text2.StartsWith("OK ", StringComparison.Ordinal), text2);
			string roomId = ((text2 != null && text2.Length > 3) ? text2.Substring(3).Trim() : "");
			Check("relay counts the room", relay.HasRoom(roomId) && relay.RoomCount == 1, "rooms " + relay.RoomCount);

			// A client joins and is parked.
			TcpClient tcpClient3 = RelayConnect(relay.Port);
			RelaySend(tcpClient3, "JOIN " + roomId);
			string text3 = RelayLine(tcpClient3);
			Check("relay parks a client", text3 == "WAIT", text3);
			Check("relay reports one waiting client", relay.WaitingCount(roomId) == 1, "waiting " + relay.WaitingCount(roomId));

			// Unknown room: refused before anything is parked.
			TcpClient tcpClient4 = RelayConnect(relay.Port);
			RelaySend(tcpClient4, "JOIN nosuchroom");
			string text4 = RelayLine(tcpClient4);
			Check("relay refuses an unknown room", text4 == "ERR no such room", text4);
			tcpClient4.Close();

			// The host asks for waiting clients and gets a ticket.
			TcpClient tcpClient5 = RelayConnect(relay.Port);
			RelaySend(tcpClient5, "POLL " + token);
			string text5 = RelayLine(tcpClient5);
			Check("relay hands out a ticket", text5 != null && text5.StartsWith("JOIN ", StringComparison.Ordinal), text5);
			string text6 = RelayLine(tcpClient5);
			Check("relay ends the poll", text6 == "END", text6);
			string ticket = ((text5 != null && text5.Length > 5) ? text5.Substring(5).Trim() : "");

			// A wrong token cannot poll somebody else's room.
			TcpClient tcpClient6 = RelayConnect(relay.Port);
			RelaySend(tcpClient6, "POLL deadbeefdeadbeefdeadbeefdeadbeef");
			string text7 = RelayLine(tcpClient6);
			Check("relay refuses a wrong token", text7 == "ERR no such room", text7);
			tcpClient6.Close();

			// Accept: after this the two sockets are a raw pipe.
			TcpClient tcpClient7 = RelayConnect(relay.Port);
			RelaySend(tcpClient7, "ACCEPT " + ticket);
			// Host -> client.
			RelayWrite(tcpClient7, "hello from the host");
			Check("relay carries host bytes", RelayRead(tcpClient3, "hello from the host"), "nothing arrived");
			// Client -> host.
			RelayWrite(tcpClient3, "hello from the client");
			Check("relay carries client bytes", RelayRead(tcpClient7, "hello from the client"), "nothing arrived");

			// A ticket is one shot: a second ACCEPT must not attach to the same session.
			TcpClient tcpClient8 = RelayConnect(relay.Port);
			RelaySend(tcpClient8, "ACCEPT " + ticket);
			string text8 = RelayLine(tcpClient8);
			Check("relay refuses a reused ticket", text8 == "ERR bad ticket", text8);
			tcpClient8.Close();

			// Closing the host's presence ends the room: a relay that keeps the room alive would
			// let clients wait forever for a host that is gone.
			tcpClient7.Close();
			tcpClient3.Close();
			tcpClient2.Close();
			Thread.Sleep(300);
			Check("relay closes the room with its host", !relay.HasRoom(roomId) && relay.RoomCount == 0, "rooms " + relay.RoomCount);
			TcpClient tcpClient9 = RelayConnect(relay.Port);
			RelaySend(tcpClient9, "JOIN " + roomId);
			string text9 = RelayLine(tcpClient9);
			Check("relay refuses a closed room", text9 == "ERR no such room", text9);
			tcpClient9.Close();
			tcpClient.Close();
		}
		catch (Exception ex)
		{
			Check("relay checks did not throw", false, ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			relay.Stop();
		}
	}

	/// <summary>One socket to the relay, with a read timeout so a hung pipe fails a check instead of the test.</summary>
	private static TcpClient RelayConnect(int Port)
	{
		TcpClient tcpClient = new TcpClient();
		tcpClient.NoDelay = true;
		tcpClient.ReceiveTimeout = 4000;
		tcpClient.Connect(IPAddress.Loopback, Port);
		return tcpClient;
	}

	private static void RelaySend(TcpClient Client, string Line)
	{
		RelayWrite(Client, Line + "\n");
	}

	private static void RelayWrite(TcpClient Client, string Text)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(Text);
		Client.GetStream().Write(bytes, 0, bytes.Length);
		Client.GetStream().Flush();
	}

	/// <summary>Reads one line, or null on timeout/close.</summary>
	private static string RelayLine(TcpClient Client)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			NetworkStream stream = Client.GetStream();
			while (stringBuilder.Length < 512)
			{
				int num = stream.ReadByte();
				if (num < 0)
				{
					return (stringBuilder.Length == 0) ? null : stringBuilder.ToString();
				}
				if (num == 10)
				{
					return stringBuilder.ToString().TrimEnd('\r');
				}
				stringBuilder.Append((char)num);
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return null;
		}
	}

	/// <summary>Reads until the expected text arrives, or gives up. Returns whether it did.</summary>
	private static bool RelayRead(TcpClient Client, string Expected)
	{
		try
		{
			byte[] array = new byte[Expected.Length];
			NetworkStream stream = Client.GetStream();
			int num = 0;
			while (num < array.Length)
			{
				int num2 = stream.Read(array, num, array.Length - num);
				if (num2 <= 0)
				{
					return false;
				}
				num += num2;
			}
			return Encoding.UTF8.GetString(array) == Expected;
		}
		catch
		{
			return false;
		}
	}

	private static void Check(string name, bool ok, string detail)
	{		if (ok)
		{
			_passed++;
			Console.WriteLine("  ok   " + name);
			return;
		}
		_failed++;
		Console.WriteLine("  FAIL " + name + " - " + detail);
	}

	private static int Count(string text, string needle)
	{
		int num = 0;
		int num2 = 0;
		while ((num2 = text.IndexOf(needle, num2, StringComparison.Ordinal)) >= 0)
		{
			num++;
			num2 += needle.Length;
		}
		return num;
	}

	private static string Trim(string text)
	{
		if (text == null)
		{
			return "(no body)";
		}
		text = text.Replace("\r", " ").Replace("\n", " ");
		return (text.Length <= 200) ? text : (text.Substring(0, 200) + "...");
	}

	private static void Request(string url, string method, string body, out int status, out string response)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		httpWebRequest.Method = method;
		httpWebRequest.Timeout = 5000;
		httpWebRequest.ReadWriteTimeout = 5000;
		httpWebRequest.KeepAlive = false;
		if (body != null)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(body);
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.ContentLength = bytes.Length;
			using Stream stream = httpWebRequest.GetRequestStream();
			stream.Write(bytes, 0, bytes.Length);
		}
		try
		{
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			status = (int)httpWebResponse.StatusCode;
			using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
			response = streamReader.ReadToEnd();
		}
		catch (WebException ex)
		{
			HttpWebResponse httpWebResponse = ex.Response as HttpWebResponse;
			if (httpWebResponse == null)
			{
				throw;
			}
			using (httpWebResponse)
			{
				status = (int)httpWebResponse.StatusCode;
				using StreamReader streamReader2 = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
				response = streamReader2.ReadToEnd();
			}
		}
	}
}
