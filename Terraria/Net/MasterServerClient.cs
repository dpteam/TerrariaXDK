using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace Terraria.Net;

/// <summary>One server as advertised by the master server.</summary>
internal sealed class MasterServerEntry
{
	public string Name = string.Empty;

	public string Host = string.Empty;

	public int Port;

	public int Players;

	public int MaxPlayers;

	public int ProtocolVersion;

	public bool Passworded;

	public string WorldName = string.Empty;

	public string Motd = string.Empty;

	/// <summary>Mod set hash, so the list can mark incompatible servers before connecting.</summary>
	public string ModSignature = string.Empty;

	public string Address
	{
		get
		{
			return Host + ":" + Port.ToString(CultureInfo.InvariantCulture);
		}
	}
}

/// <summary>
/// Client for the master server that publishes and lists multiplayer worlds.
///
/// The master is a single point of failure, so nothing here may ever block the game:
/// every call is meant for a background thread, has a hard timeout, falls back to a cached
/// response and backs off exponentially after failures. An unreachable master means "you
/// see LAN only", never an error dialog.
///
/// The protocol is plain HTTP/1.1 with JSON bodies (see docs/MULTIPLAYER.md):
/// <code>
/// GET  /v1/servers     -> {"servers":[{...}]}
/// POST /v1/announce    &lt;- {"name":..,"host":..,"port":..,...}
/// POST /v1/remove      &lt;- {"host":..,"port":..}
/// </code>
/// </summary>
internal static class MasterServerClient
{
	private const string CacheFileName = "servers.json";

	private const int MaxResponseBytes = Json.MaxDocumentSize;

	private static readonly List<string> endpoints = new List<string>(2);

	private static readonly object sync = new object();

	private static int consecutiveFailures;

	private static DateTime nextAttemptUtc = DateTime.MinValue;

	/// <summary>Publish this host to the master server. Off by default: publishing is opt-in.</summary>
	public static bool Publish;

	/// <summary>Hard timeout for one request. The master is never allowed to stall the game.</summary>
	public static int TimeoutMs = 4000;

	/// <summary>Endpoint list as "host:port"; the first one that answers wins.</summary>
	public static string[] Endpoints => endpoints.ToArray();

	public static string CachePath => Path.Combine(Terraria.Storage.AppPaths.Cache, CacheFileName);

	public static void Configure(Settings settings)
	{
		Publish = settings.Get("Network", "PublishToMaster").ToLower().Equals("true");
		int result;
		if (int.TryParse(settings.Get("Network", "MasterServerPort"), out result) && result > 0 && result <= 65535)
		{
			NetworkConfig.MasterServerPort = result;
		}
		string text = settings.Get("Network", "MasterServer");
		if (string.IsNullOrEmpty(text))
		{
			text = NetworkConfig.MasterServer;
		}
		lock (sync)
		{
			endpoints.Clear();
			// Comma-separated list: primary first, then mirrors.
			string[] array = text.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (text2.Length == 0)
				{
					continue;
				}
				string host;
				int port;
				if (NetworkConfig.TryParseAddress(text2, NetworkConfig.MasterServerPort, out host, out port))
				{
					endpoints.Add(host + ":" + port.ToString(CultureInfo.InvariantCulture));
				}
			}
			if (endpoints.Count == 0)
			{
				endpoints.Add(NetworkConfig.MasterServer + ":" + NetworkConfig.MasterServerPort.ToString(CultureInfo.InvariantCulture));
			}
			// Repair a config written before the port bug: "host" without a port was resolved with
			// the *game* port, so every existing install saved "play.dartpower.xyz:7777" and the
			// master was never reached. Narrow on purpose - only the built-in host, only the game
			// port, and only when the master port is something else - so a deliberately configured
			// server is never rewritten.
			for (int j = 0; j < endpoints.Count; j++)
			{
				string text3 = endpoints[j];
				string text4 = NetworkConfig.MasterServer + ":" + NetworkConfig.Port.ToString(CultureInfo.InvariantCulture);
				if (NetworkConfig.MasterServerPort != NetworkConfig.Port
					&& text3.Equals(text4, StringComparison.OrdinalIgnoreCase))
				{
					endpoints[j] = NetworkConfig.MasterServer + ":" + NetworkConfig.MasterServerPort.ToString(CultureInfo.InvariantCulture);
					Program.LogWarn("Master", "corrected the master server port in the settings: " + text3 + " -> " + endpoints[j]);
					// Written back, not just fixed in memory: otherwise every start repeats the
					// warning and the file keeps claiming a port that was never right.
					if (settings != null)
					{
						settings.Update("Network", "MasterServer", string.Join(",", endpoints.ToArray()),
							"Master server 'host:port'; a comma-separated list adds fallbacks.");
					}
				}
			}
		}
	}

	/// <summary>
	/// Replaces the endpoint list from a user-typed "host:port[,host:port]" string. Returns
	/// false when nothing in the text parses, so the settings screen can keep the old value
	/// instead of silently wiping the configuration.
	/// </summary>
	public static bool SetEndpoints(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		List<string> list = new List<string>(2);
		string[] array = text.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length == 0)
			{
				continue;
			}
			string host;
			int port;
			if (NetworkConfig.TryParseAddress(text2, NetworkConfig.MasterServerPort, out host, out port))
			{
				list.Add(host + ":" + port.ToString(CultureInfo.InvariantCulture));
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		lock (sync)
		{
			endpoints.Clear();
			endpoints.AddRange(list);
		}
		ResetBackoff();
		return true;
	}

	/// <summary>The primary endpoint as typed by the user, for the settings screen.</summary>
	public static string PrimaryEndpoint
	{
		get
		{
			string[] array = Endpoints;
			return (array.Length == 0) ? string.Empty : array[0];
		}
	}

	public static void Save(Settings settings)
	{
		settings.Update("Network", "MasterServer", string.Join(",", endpoints.ToArray()), "Master server 'host:port'; a comma-separated list adds fallbacks.");
		settings.Update("Network", "MasterServerPort", NetworkConfig.MasterServerPort.ToString(CultureInfo.InvariantCulture), "Dummy port used when MasterServer has no explicit port.");
		settings.Update("Network", "PublishToMaster", Publish ? "True" : "False", "List this world on the master server while hosting.\n; Off by default: publishing your address is a choice.");
	}

	// --- Backoff ------------------------------------------------------------

	/// <summary>False while backing off after a run of failures.</summary>
	public static bool CanTry()
	{
		lock (sync)
		{
			return DateTime.UtcNow >= nextAttemptUtc;
		}
	}

	private static void NoteSuccess()
	{
		lock (sync)
		{
			consecutiveFailures = 0;
			nextAttemptUtc = DateTime.MinValue;
		}
	}

	private static void NoteFailure()
	{
		lock (sync)
		{
			consecutiveFailures++;
			// 5s, 10s, 20s ... capped at five minutes.
			int num = 5 << Math.Min(consecutiveFailures - 1, 6);
			if (num > 300)
			{
				num = 300;
			}
			nextAttemptUtc = DateTime.UtcNow.AddSeconds(num);
		}
	}

	/// <summary>Clears the failure counter. Used when the player asks for a manual refresh.</summary>
	public static void ResetBackoff()
	{
		lock (sync)
		{
			consecutiveFailures = 0;
			nextAttemptUtc = DateTime.MinValue;
		}
	}

	// --- Public API ---------------------------------------------------------

	/// <summary>
	/// Fetches the public server list. Blocking: call it from a background thread only.
	/// Returns an empty list when the master is unreachable; the caller falls back to LAN.
	/// </summary>
	public static List<MasterServerEntry> Fetch()
	{
		List<MasterServerEntry> result = new List<MasterServerEntry>();
		if (!CanTry())
		{
			return result;
		}
		string[] array = Endpoints;
		for (int i = 0; i < array.Length; i++)
		{
			string body;
			if (!TryRequest(array[i], "GET", "/v1/servers", null, out body))
			{
				continue;
			}
			List<MasterServerEntry> list = ParseServers(body);
			if (list == null)
			{
				Program.LogWarn("Master", "ignoring an unreadable server list from " + array[i]);
				continue;
			}
			NoteSuccess();
			SaveCache(body);
			return list;
		}
		NoteFailure();
		return result;
	}

	/// <summary>The last good response, so the list is not empty when the master is down.</summary>
	public static List<MasterServerEntry> FetchCached()
	{
		try
		{
			if (!File.Exists(CachePath))
			{
				return new List<MasterServerEntry>();
			}
			string text = File.ReadAllText(CachePath, Encoding.UTF8);
			List<MasterServerEntry> list = ParseServers(text);
			return list ?? new List<MasterServerEntry>();
		}
		catch (Exception ex)
		{
			Program.LogWarn("Master", "could not read the cached server list: " + ex.Message);
			return new List<MasterServerEntry>();
		}
	}

	/// <summary>Announces this host. Blocking; the host calls it from its own timer thread.</summary>
	public static bool Announce(MasterServerEntry entry)
	{
		if (entry == null || string.IsNullOrEmpty(entry.Host))
		{
			return false;
		}
		string text = EntryToJson(entry);
		string[] array = Endpoints;
		for (int i = 0; i < array.Length; i++)
		{
			string body;
			if (TryRequest(array[i], "POST", "/v1/announce", text, out body))
			{
				NoteSuccess();
				return true;
			}
		}
		NoteFailure();
		return false;
	}

	/// <summary>Best-effort removal when hosting stops; a stale entry expires on its own anyway.</summary>
	public static void Remove(string host, int port)
	{
		if (string.IsNullOrEmpty(host))
		{
			return;
		}
		string text = "{\"host\":" + Json.Quote(host) + ",\"port\":" + port.ToString(CultureInfo.InvariantCulture) + "}";
		string[] array = Endpoints;
		for (int i = 0; i < array.Length; i++)
		{
			string body;
			if (TryRequest(array[i], "POST", "/v1/remove", text, out body))
			{
				return;
			}
		}
	}

	// --- Parsing ------------------------------------------------------------

	/// <summary>Returns null when the document is not a usable server list.</summary>
	internal static List<MasterServerEntry> ParseServers(string body)
	{
		List<MasterServerEntry> list = new List<MasterServerEntry>();
		try
		{
			Dictionary<string, object> dictionary = Json.Parse(body) as Dictionary<string, object>;
			if (dictionary == null)
			{
				return null;
			}
			List<object> array = Json.GetArray(dictionary, "servers");
			if (array == null)
			{
				return null;
			}
			for (int i = 0; i < array.Count; i++)
			{
				Dictionary<string, object> dictionary2 = array[i] as Dictionary<string, object>;
				if (dictionary2 == null)
				{
					continue;
				}
				string text = Json.GetString(dictionary2, "host");
				int num = Json.GetInt(dictionary2, "port", 0);
				// A record without an address is useless: drop it instead of failing the list.
				if (string.IsNullOrEmpty(text) || num <= 0 || num > 65535)
				{
					continue;
				}
				MasterServerEntry masterServerEntry = new MasterServerEntry();
				masterServerEntry.Host = text.Trim();
				masterServerEntry.Port = num;
				masterServerEntry.Name = Json.GetString(dictionary2, "name") ?? (masterServerEntry.Host + ":" + num);
				masterServerEntry.Players = Json.GetInt(dictionary2, "players", 0);
				masterServerEntry.MaxPlayers = Json.GetInt(dictionary2, "maxPlayers", NetworkConfig.MaxPlayers);
				masterServerEntry.ProtocolVersion = Json.GetInt(dictionary2, "version", 0);
				masterServerEntry.Passworded = Json.GetBool(dictionary2, "passworded");
				masterServerEntry.WorldName = Json.GetString(dictionary2, "worldName") ?? string.Empty;
				masterServerEntry.Motd = Json.GetString(dictionary2, "motd") ?? string.Empty;
				masterServerEntry.ModSignature = Json.GetString(dictionary2, "modSignature") ?? string.Empty;
				list.Add(masterServerEntry);
			}
			return list;
		}
		catch (FormatException ex)
		{
			Program.LogWarn("Master", "malformed server list: " + ex.Message);
			return null;
		}
	}

	internal static string EntryToJson(MasterServerEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		stringBuilder.Append('{');
		stringBuilder.Append("\"name\":").Append(Json.Quote(entry.Name)).Append(',');
		stringBuilder.Append("\"host\":").Append(Json.Quote(entry.Host)).Append(',');
		stringBuilder.Append("\"port\":").Append(entry.Port.ToString(CultureInfo.InvariantCulture)).Append(',');
		stringBuilder.Append("\"players\":").Append(entry.Players.ToString(CultureInfo.InvariantCulture)).Append(',');
		stringBuilder.Append("\"maxPlayers\":").Append(entry.MaxPlayers.ToString(CultureInfo.InvariantCulture)).Append(',');
		stringBuilder.Append("\"version\":").Append(entry.ProtocolVersion.ToString(CultureInfo.InvariantCulture)).Append(',');
		stringBuilder.Append("\"passworded\":").Append(entry.Passworded ? "true" : "false").Append(',');
		stringBuilder.Append("\"worldName\":").Append(Json.Quote(entry.WorldName)).Append(',');
		stringBuilder.Append("\"motd\":").Append(Json.Quote(entry.Motd)).Append(',');
		stringBuilder.Append("\"modSignature\":").Append(Json.Quote(entry.ModSignature)).Append(',');
		stringBuilder.Append("\"platform\":\"windows\"");
		stringBuilder.Append('}');
		return stringBuilder.ToString();
	}

	// --- HTTP ---------------------------------------------------------------

	/// <summary>
	/// One request against one endpoint. Everything that can go wrong (DNS, refused
	/// connection, timeout, non-200, oversized body) becomes a plain false.
	/// </summary>
	private static bool TryRequest(string endpoint, string method, string path, string payload, out string body)
	{
		body = null;
		string url = "http://" + endpoint + path;
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = method;
			httpWebRequest.Timeout = TimeoutMs;
			httpWebRequest.ReadWriteTimeout = TimeoutMs;
			httpWebRequest.KeepAlive = false;
			httpWebRequest.UserAgent = "TerrariaXDK/" + Main.OGCVersion;
			httpWebRequest.Accept = "application/json";
			// No system proxy: a master server is a direct endpoint, and a stale proxy
			// setting would turn every request into a five second timeout.
			httpWebRequest.Proxy = null;
			if (payload != null)
			{
				byte[] bytes = Encoding.UTF8.GetBytes(payload);
				httpWebRequest.ContentType = "application/json; charset=utf-8";
				httpWebRequest.ContentLength = bytes.Length;
				using Stream stream = httpWebRequest.GetRequestStream();
				stream.Write(bytes, 0, bytes.Length);
			}
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			if (httpWebResponse.StatusCode != HttpStatusCode.OK)
			{
				Program.LogWarn("Master", endpoint + " answered " + (int)httpWebResponse.StatusCode);
				return false;
			}
			using Stream stream2 = httpWebResponse.GetResponseStream();
			body = ReadLimited(stream2);
			return body != null;
		}
		catch (Exception ex)
		{
			Program.LogInfo("Master", Describe(ex, url));
			return false;
		}
	}

	// --- Statistics and leaderboards ----------------------------------------

	/// <summary>
	/// Posts this player's achievements and counters. Returns the number of newly recorded
	/// achievements, or -1 with a reason.
	///
	/// Synchronous, like the rest of this class: the caller owns the background thread
	/// (<see cref="StatSync"/>). Keeping the thread out of here is what lets the self-test drive
	/// these methods directly against a local master.
	/// </summary>
	public static int ReportStats(string gamertag, int[] achievements, Dictionary<string, long> stats, out string error)
	{
		error = null;
		if (string.IsNullOrEmpty(gamertag))
		{
			error = "no gamertag";
			return -1;
		}
		StringBuilder stringBuilder = new StringBuilder(256);
		stringBuilder.Append("{\"gamertag\":").Append(Json.Quote(gamertag));
		stringBuilder.Append(",\"achievements\":[");
		if (achievements != null)
		{
			for (int i = 0; i < achievements.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(achievements[i].ToString(CultureInfo.InvariantCulture));
			}
		}
		stringBuilder.Append("],\"stats\":{");
		bool flag = true;
		if (stats != null)
		{
			foreach (KeyValuePair<string, long> stat in stats)
			{
				if (!flag)
				{
					stringBuilder.Append(',');
				}
				flag = false;
				stringBuilder.Append(Json.Quote(stat.Key)).Append(':').Append(stat.Value.ToString(CultureInfo.InvariantCulture));
			}
		}
		stringBuilder.Append("}}");
		int num;
		string text;
		if (!TryRequestWithStatus(Endpoints, "POST", "/v1/stats", stringBuilder.ToString(), out num, out text))
		{
			error = "the master server is unreachable";
			return -1;
		}
		Dictionary<string, object> dictionary = Json.Parse(text) as Dictionary<string, object>;
		if (num != 200)
		{
			error = (dictionary == null) ? ("status " + num) : (Json.GetString(dictionary, "error") ?? ("status " + num));
			return -1;
		}
		if (dictionary == null)
		{
			error = "the master answered with something that is not JSON";
			return -1;
		}
		return Json.GetInt(dictionary, "newAchievements", 0);
	}

	/// <summary>
	/// How many players earned each achievement, as a dense array by index, plus the population.
	/// Returns null when the master cannot be reached, so the caller can keep showing "unknown"
	/// instead of "0 of 0 players".
	/// </summary>
	public static int[] FetchAchievementCounts(out int totalPlayers)
	{
		totalPlayers = 0;
		int num;
		string text;
		if (!TryRequestWithStatus(Endpoints, "GET", "/v1/achievements", null, out num, out text) || num != 200)
		{
			return null;
		}
		Dictionary<string, object> dictionary = Json.Parse(text) as Dictionary<string, object>;
		if (dictionary == null)
		{
			return null;
		}
		totalPlayers = Json.GetInt(dictionary, "totalPlayers", 0);
		List<object> array = Json.GetArray(dictionary, "counts");
		if (array == null)
		{
			return null;
		}
		int[] array2 = new int[array.Count];
		for (int i = 0; i < array.Count; i++)
		{
			string text2 = (array[i] == null) ? null : Convert.ToString(array[i], CultureInfo.InvariantCulture);
			int result;
			array2[i] = (text2 != null && int.TryParse(text2, out result)) ? result : 0;
		}
		return array2;
	}

	/// <summary>
	/// One board from the master: every column of every row, ranked by the sum of the columns.
	/// Returns null when the master is unreachable, so the screen can fall back to its own
	/// message instead of drawing an empty table.
	/// </summary>
	public static List<BoardRow> FetchBoard(string[] statistics, int limit)
	{
		if (statistics == null || statistics.Length == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder("/v1/leaderboard?stats=", 48);
		for (int i = 0; i < statistics.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(Uri.EscapeDataString(statistics[i]));
		}
		stringBuilder.Append("&limit=").Append(limit.ToString(CultureInfo.InvariantCulture));
		int num;
		string text;
		if (!TryRequestWithStatus(Endpoints, "GET", stringBuilder.ToString(), null, out num, out text) || num != 200)
		{
			return null;
		}
		Dictionary<string, object> dictionary = Json.Parse(text) as Dictionary<string, object>;
		List<object> array = (dictionary == null) ? null : Json.GetArray(dictionary, "rows");
		if (array == null)
		{
			return null;
		}
		List<BoardRow> list = new List<BoardRow>(array.Count);
		for (int j = 0; j < array.Count; j++)
		{
			Dictionary<string, object> dictionary2 = array[j] as Dictionary<string, object>;
			if (dictionary2 == null)
			{
				continue;
			}
			string text2 = Json.GetString(dictionary2, "gamertag");
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			List<object> array2 = Json.GetArray(dictionary2, "values");
			long[] array3 = new long[statistics.Length];
			if (array2 != null)
			{
				for (int k = 0; k < array3.Length && k < array2.Count; k++)
				{
					string text3 = (array2[k] == null) ? null : Convert.ToString(array2[k], CultureInfo.InvariantCulture);
					long result;
					array3[k] = (text3 != null && long.TryParse(text3, out result)) ? result : 0L;
				}
			}
			BoardRow boardRow = new BoardRow();
			boardRow.Gamertag = text2;
			boardRow.Values = array3;
			list.Add(boardRow);
		}
		return list;
	}

	/// <summary>
	/// Like <see cref="TryRequest"/>, but also reports a non-200 status together with its body.
	/// The failure detail matters for the statistics endpoint: "400 gamertag is required" is a bug
	/// report, while "unreachable" is a network problem, and the two need different fixes.
	/// </summary>
	private static bool TryRequestWithStatus(string[] candidates, string method, string path, string payload, out int status, out string body)
	{
		status = 0;
		body = null;
		if (candidates == null || candidates.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < candidates.Length; i++)
		{
			string url = "http://" + candidates[i] + path;
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
				httpWebRequest.Method = method;
				httpWebRequest.Timeout = TimeoutMs;
				httpWebRequest.ReadWriteTimeout = TimeoutMs;
				httpWebRequest.KeepAlive = false;
				httpWebRequest.UserAgent = "TerrariaXDK/" + Main.OGCVersion;
				httpWebRequest.Accept = "application/json";
				httpWebRequest.Proxy = null;
				if (payload != null)
				{
					byte[] bytes = Encoding.UTF8.GetBytes(payload);
					httpWebRequest.ContentType = "application/json; charset=utf-8";
					httpWebRequest.ContentLength = bytes.Length;
					using Stream stream = httpWebRequest.GetRequestStream();
					stream.Write(bytes, 0, bytes.Length);
				}
				using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				status = (int)httpWebResponse.StatusCode;
				using Stream stream2 = httpWebResponse.GetResponseStream();
				body = ReadLimited(stream2);
				return true;
			}
			catch (WebException ex)
			{
				// A 4xx/5xx arrives as an exception, but the response still carries the reason.
				HttpWebResponse httpWebResponse2 = ex.Response as HttpWebResponse;
				if (httpWebResponse2 != null)
				{
					using (httpWebResponse2)
					{
						status = (int)httpWebResponse2.StatusCode;
						body = ReadLimited(httpWebResponse2.GetResponseStream());
					}
					return true;
				}
				Program.LogDebug("Master", Describe(ex, url));
			}
			catch (Exception ex2)
			{
				Program.LogDebug("Master", Describe(ex2, url));
			}
		}
		return false;
	}

	/// <summary>
	/// HTTP status for one path on the master, or 0 when nothing answered. Diagnostics only:
	/// the self-test uses it to tell "the master is old" (404) from "the master is broken" (0).
	/// </summary>
	internal static int Probe(string path)
	{
		int num;
		string text;
		if (!TryRequestWithStatus(Endpoints, "GET", path, null, out num, out text))
		{
			return 0;
		}
		return num;
	}

	/// <summary>Reads at most <see cref="MaxResponseBytes"/>; null when the body is too large.</summary>
	private static string ReadLimited(Stream stream)	{
		if (stream == null)
		{
			return string.Empty;
		}
		byte[] array = new byte[8192];
		MemoryStream memoryStream = new MemoryStream();
		while (true)
		{
			int num = stream.Read(array, 0, array.Length);
			if (num <= 0)
			{
				break;
			}
			if (memoryStream.Length + num > MaxResponseBytes)
			{
				Program.LogWarn("Master", "response exceeds " + MaxResponseBytes + " bytes, dropped");
				return null;
			}
			memoryStream.Write(array, 0, num);
		}
		return Encoding.UTF8.GetString(memoryStream.ToArray());
	}

	private static string Describe(Exception ex, string url)
	{
		WebException ex2 = ex as WebException;
		if (ex2 != null)
		{
			return url + " failed: " + ex2.Status;
		}
		return url + " failed: " + ex.Message;
	}

	private static void SaveCache(string body)
	{
		try
		{
			string text = Path.GetDirectoryName(CachePath);
			if (!string.IsNullOrEmpty(text) && !Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			File.WriteAllText(CachePath, body, Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Master", "could not cache the server list: " + ex.Message);
		}
	}
}
