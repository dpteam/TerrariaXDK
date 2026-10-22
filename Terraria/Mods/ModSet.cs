using System;
using System.Collections.Generic;
using System.Text;

namespace Terraria.Mods;

/// <summary>
/// One mod as it travels in the handshake: a name and a version, nothing else.
///
/// The signature (<see cref="ModLoader.Signature"/>) answers "same set or not" with one number.
/// That is enough to refuse a connection but useless for telling the player what to do about it -
/// and "your mods differ" leaves them comparing folder names by hand. This list exists so the
/// refusal can name the mods.
///
/// Deliberately small: a name and a version. Sending the whole manifest would mean sending
/// executable-adjacent data before anybody has agreed to trust it.
/// </summary>
internal sealed class ModEntry
{
	public string Name = "";

	public string Version = "";

	/// <summary>"Name v1.2.3", for messages.</summary>
	public string Label => Name + " v" + Version;
}

/// <summary>What differs between the host's mod set and a client's.</summary>
internal sealed class ModDifference
{
	/// <summary>The host has these, the client does not.</summary>
	public readonly List<string> Missing = new List<string>();

	/// <summary>The client has these, the host does not.</summary>
	public readonly List<string> Extra = new List<string>();

	/// <summary>Same name, different version: "Name (host 1.0, you 2.0)".</summary>
	public readonly List<string> VersionMismatch = new List<string>();

	public bool Any => Missing.Count > 0 || Extra.Count > 0 || VersionMismatch.Count > 0;

	/// <summary>
	/// A short human list for the refusal. Capped: a client with fifty extra mods does not need
	/// all fifty named, and the message has to fit on a screen.
	/// </summary>
	public string Describe(int max = 6)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Append(stringBuilder, Missing, "+", max);
		Append(stringBuilder, Extra, "-", max);
		Append(stringBuilder, VersionMismatch, "~", max);
		return stringBuilder.ToString();
	}

	private static void Append(StringBuilder builder, List<string> items, string sign, int max)
	{
		for (int i = 0; i < items.Count && i < max; i++)
		{
			if (builder.Length > 0)
			{
				builder.Append(", ");
			}
			builder.Append(sign).Append(items[i]);
		}
		if (items.Count > max)
		{
			if (builder.Length > 0)
			{
				builder.Append(", ");
			}
			builder.Append('+').Append(items.Count - max).Append(" more");
		}
	}
}

/// <summary>
/// Comparing and describing mod sets across the wire.
///
/// The rule this replaces was asymmetric and wrong: a client with no mods was accepted by any
/// host, because "I have none" was treated as "I do not care". A player with a clean install
/// could join a modded server, see different item stats from everyone else, and there was no
/// warning at all - the signature check existed but was skipped exactly for the case that
/// breaks first.
/// </summary>
internal static class ModSet
{
	/// <summary>Separator inside the handshake field. A mod name cannot contain a newline.</summary>
	internal const char ItemSeparator = '\n';

	internal const char FieldSeparator = '|';

	/// <summary>Serialises the loaded mods for the handshake, sorted so it is deterministic.</summary>
	internal static string Describe()
	{
		List<ModEntry> entries = Loaded();
		if (entries.Count == 0)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < entries.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(ItemSeparator);
			}
			stringBuilder.Append(entries[i].Name).Append(FieldSeparator).Append(entries[i].Version);
		}
		return stringBuilder.ToString();
	}

	/// <summary>The loaded mods as entries, sorted by name.</summary>
	internal static List<ModEntry> Loaded()
	{
		List<ModEntry> list = new List<ModEntry>();
		IList<Mod> mods = ModLoader.Mods;
		for (int i = 0; i < mods.Count; i++)
		{
			Mod mod = mods[i];
			ModEntry modEntry = new ModEntry();
			modEntry.Name = mod.Name ?? mod.GetType().Name;
			modEntry.Version = mod.Version ?? "0.0.0";
			list.Add(modEntry);
		}
		list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		return list;
	}

	/// <summary>
	/// Parses the handshake field. Unreadable entries are dropped rather than throwing: this runs
	/// on the network thread, on data from another machine.
	/// </summary>
	internal static List<ModEntry> Parse(string text)
	{
		List<ModEntry> list = new List<ModEntry>();
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		string[] array = text.Split(ItemSeparator);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length == 0)
			{
				continue;
			}
			int num = text2.IndexOf(FieldSeparator);
			ModEntry modEntry = new ModEntry();
			if (num < 0)
			{
				modEntry.Name = text2;
				modEntry.Version = "?";
			}
			else
			{
				modEntry.Name = text2.Substring(0, num).Trim();
				modEntry.Version = text2.Substring(num + 1).Trim();
			}
			if (modEntry.Name.Length > 0)
			{
				list.Add(modEntry);
			}
		}
		return list;
	}

	/// <summary>
	/// What the client is missing or has to spare, from the host's point of view. Empty when the
	/// sets match.
	///
	/// Comparison is by name, and a version difference is reported separately: "you have 1.0, the
	/// host has 1.1" is a different problem from "you do not have it", and the fix differs.
	/// </summary>
	internal static ModDifference Compare(List<ModEntry> host, List<ModEntry> client)
	{
		ModDifference modDifference = new ModDifference();
		Dictionary<string, ModEntry> dictionary = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < client.Count; i++)
		{
			dictionary[client[i].Name] = client[i];
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int j = 0; j < host.Count; j++)
		{
			ModEntry modEntry = host[j];
			hashSet.Add(modEntry.Name);
			ModEntry value;
			if (!dictionary.TryGetValue(modEntry.Name, out value))
			{
				modDifference.Missing.Add(modEntry.Label);
				continue;
			}
			if (!string.Equals(value.Version, modEntry.Version, StringComparison.OrdinalIgnoreCase))
			{
				modDifference.VersionMismatch.Add(modEntry.Name + " (host " + modEntry.Version + ", you " + value.Version + ")");
			}
		}
		for (int k = 0; k < client.Count; k++)
		{
			if (!hashSet.Contains(client[k].Name))
			{
				modDifference.Extra.Add(client[k].Label);
			}
		}
		return modDifference;
	}
}
