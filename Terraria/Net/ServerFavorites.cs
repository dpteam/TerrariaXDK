using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Saved servers ("favourites"), stored as <c>host:port</c> lines in
/// <c>Storage/Cache/favorites.txt</c>.
///
/// They are also re-added to the joinable list on every search, so a server you have saved
/// stays reachable when discovery and the master server are both unavailable - which is
/// exactly when you still want to be able to type/join the address you know works.
/// </summary>
internal static class ServerFavorites
{
	private static readonly List<string> Entries = new List<string>();

	private static bool loaded;

	public static string FilePath => Path.Combine(Terraria.Storage.AppPaths.Cache, "favorites.txt");

	/// <summary>
	/// A snapshot of the saved addresses. A copy, not a live view: the network finder reads
	/// this from its own thread while the UI can add or remove entries.
	/// </summary>
	public static IList<string> All
	{
		get
		{
			Load();
			return new List<string>(Entries);
		}
	}

	/// <summary>Adds an address if it is not already saved. Returns false for a bad address.</summary>
	public static bool Add(string address)
	{
		string host;
		int port;
		if (!NetworkConfig.TryParseAddress(address, out host, out port))
		{
			return false;
		}
		string text = host + ":" + port;
		Load();
		for (int i = 0; i < Entries.Count; i++)
		{
			if (string.Equals(Entries[i], text, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		Entries.Add(text);
		Save();
		return true;
	}

	public static bool Remove(string address)
	{
		string host;
		int port;
		if (!NetworkConfig.TryParseAddress(address, out host, out port))
		{
			return false;
		}
		string text = host + ":" + port;
		Load();
		for (int i = 0; i < Entries.Count; i++)
		{
			if (string.Equals(Entries[i], text, StringComparison.OrdinalIgnoreCase))
			{
				Entries.RemoveAt(i);
				Save();
				return true;
			}
		}
		return false;
	}

	public static bool Contains(string address)
	{
		string host;
		int port;
		if (!NetworkConfig.TryParseAddress(address, out host, out port))
		{
			return false;
		}
		string text = host + ":" + port;
		Load();
		for (int i = 0; i < Entries.Count; i++)
		{
			if (string.Equals(Entries[i], text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Turns a saved address into "host:port", or null when it is unusable.</summary>
	public static string Normalize(string address)
	{
		string host;
		int port;
		if (!NetworkConfig.TryParseAddress(address, out host, out port))
		{
			return null;
		}
		return host + ":" + port;
	}

	/// <summary>Formats a session endpoint the same way the file does, for lookups.</summary>
	public static string AddressOf(System.Net.IPEndPoint endPoint)
	{
		if (endPoint == null)
		{
			return null;
		}
		return endPoint.Address + ":" + endPoint.Port;
	}

	private static void Load()
	{
		if (loaded)
		{
			return;
		}
		loaded = true;
		try
		{
			if (!File.Exists(FilePath))
			{
				return;
			}
			string[] array = File.ReadAllLines(FilePath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0 || text.StartsWith("#"))
				{
					continue;
				}
				string text2 = Normalize(text);
				// Keep only entries that still parse: a hand-edited file must not produce
				// a phantom row the player cannot remove.
				if (text2 != null && !Entries.Contains(text2))
				{
					Entries.Add(text2);
				}
			}
		}
		catch (Exception ex)
		{
			Program.LogWarn("Favorites", "could not read the saved servers: " + ex.Message);
		}
	}

	private static void Save()
	{
		try
		{
			string text = Path.GetDirectoryName(FilePath);
			if (!string.IsNullOrEmpty(text) && !Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# Saved servers, one 'host:port' per line. F in the world list adds or removes one.");
			for (int i = 0; i < Entries.Count; i++)
			{
				stringBuilder.AppendLine(Entries[i]);
			}
			File.WriteAllText(FilePath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Favorites", "could not save the server list: " + ex.Message);
		}
	}

	/// <summary>Drops the in-memory copy; the self-test uses it between cases.</summary>
	internal static void Reset()
	{
		Entries.Clear();
		loaded = false;
	}
}
