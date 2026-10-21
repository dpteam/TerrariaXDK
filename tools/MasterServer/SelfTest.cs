using System;
using System.IO;
using System.Net;
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

	private static void Check(string name, bool ok, string detail)
	{
		if (ok)
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
