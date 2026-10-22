using System;
using System.Collections.Generic;
using System.Text;

namespace Terraria.Mods;

/// <summary>
/// One mod as it travels in the handshake: a name, a version, how big it is and what its
/// contents hash to.
///
/// The signature (<see cref="ModLoader.Signature"/>) answers "same set or not" with one number.
/// That is enough to refuse a connection but useless for telling the player what to do about it -
/// and "your mods differ" leaves them comparing folder names by hand. This list exists so the
/// refusal can name the mods.
///
/// Size and hash are what turn the refusal into an offer: they are sent *before* anybody agrees
/// to download anything, so the client can say "3 mods, 240 KB" and, once the files arrive, check
/// them against the hash the host promised. Neither is executable content - the files are, and
/// those only move after an explicit yes.
/// </summary>
internal sealed class ModEntry
{
	public string Name = "";

	public string Version = "";

	/// <summary>Total bytes of the mod's files, or -1 when the host could not measure them.</summary>
	public long Size = -1L;

	/// <summary>Content hash of the mod folder; empty when the host could not compute one.</summary>
	public string Hash = "";

	/// <summary>"Name v1.2.3", for messages.</summary>
	public string Label => Name + " v" + Version;

	/// <summary>True when this entry carries enough to be downloaded and verified.</summary>
	public bool IsDownloadable => Size >= 0L && !string.IsNullOrEmpty(Hash) && Hash != "unreadable";
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

	/// <summary>
	/// The host's entries the client does not have, and the ones whose version differs. Kept
	/// next to the human labels because a download needs names and sizes, and re-deriving them
	/// from "Name v1.2.0" would be parsing a sentence that was only ever meant to be read.
	/// </summary>
	public readonly List<ModEntry> MissingEntries = new List<ModEntry>();

	public readonly List<ModEntry> VersionMismatchEntries = new List<ModEntry>();

	/// <summary>
	/// The names a client could fetch to fix the mismatch: missing mods and wrong versions, in
	/// that order. Only entries whose size and hash are known - the rest cannot be verified
	/// after transfer, so offering them would be offering a download that fails its own check.
	/// </summary>
	public List<string> DownloadableNames()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < MissingEntries.Count; i++)
		{
			if (MissingEntries[i].IsDownloadable)
			{
				list.Add(MissingEntries[i].Name);
			}
		}
		for (int j = 0; j < VersionMismatchEntries.Count; j++)
		{
			if (VersionMismatchEntries[j].IsDownloadable)
			{
				list.Add(VersionMismatchEntries[j].Name);
			}
		}
		return list;
	}

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
		return Describe(Loaded());
	}

	/// <summary>Serialises a mod list: one line per mod, <c>name|version|size|hash</c>.</summary>
	internal static string Describe(List<ModEntry> entries)
	{
		if (entries == null || entries.Count == 0)
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
			ModEntry modEntry = entries[i];
			stringBuilder.Append(modEntry.Name).Append(FieldSeparator).Append(modEntry.Version);
			// Size and hash are optional on the wire: a peer that predates them sends two fields,
			// and the refusal then cannot offer a download - still correct, just less helpful.
			if (modEntry.Size >= 0L || !string.IsNullOrEmpty(modEntry.Hash))
			{
				stringBuilder.Append(FieldSeparator).Append(modEntry.Size.ToString(System.Globalization.CultureInfo.InvariantCulture))
					.Append(FieldSeparator).Append(modEntry.Hash ?? "");
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// The loaded mods as entries, sorted by name.
	///
	/// The folder of each mod is found from the loaded instance, so the size and hash describe
	/// exactly the folder a download would copy.
	/// </summary>
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
			Measure(modEntry, ModLoader.DirectoryOf(mod));
			list.Add(modEntry);
		}
		IList<RawMod> rawMods = ModLoader.RawMods;
		for (int j = 0; j < rawMods.Count; j++)
		{
			RawMod rawMod = rawMods[j];
			ModEntry modEntry2 = new ModEntry();
			modEntry2.Name = rawMod.Name;
			modEntry2.Version = rawMod.Version;
			Measure(modEntry2, rawMod.Directory);
			list.Add(modEntry2);
		}
		list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		return list;
	}

	/// <summary>Fills in size and hash, or leaves them "unknown" when the folder is gone.</summary>
	private static void Measure(ModEntry entry, string directory)
	{
		if (string.IsNullOrEmpty(directory))
		{
			return;
		}
		long num = ModFiles.TotalSize(directory);
		if (num < 0L || num > ModFiles.MaxBytes)
		{
			// Too big to send: report it as unknown rather than promising a download that will be
			// refused halfway through.
			return;
		}
		entry.Size = num;
		entry.Hash = ModFiles.ContentHash(directory);
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
			// name|version|size|hash - the last two optional, so an older peer's two-field entry
			// still parses and simply cannot be downloaded.
			string[] array2 = text2.Split(FieldSeparator);
			ModEntry modEntry = new ModEntry();
			modEntry.Name = array2[0].Trim();
			modEntry.Version = (array2.Length > 1) ? array2[1].Trim() : "?";
			if (array2.Length > 2)
			{
				long result;
				if (long.TryParse(array2[2].Trim(), System.Globalization.NumberStyles.Integer,
					System.Globalization.CultureInfo.InvariantCulture, out result) && result >= 0L)
				{
					modEntry.Size = result;
				}
			}
			if (array2.Length > 3)
			{
				modEntry.Hash = array2[3].Trim();
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
				modDifference.MissingEntries.Add(modEntry);
				continue;
			}
			if (!string.Equals(value.Version, modEntry.Version, StringComparison.OrdinalIgnoreCase))
			{
				modDifference.VersionMismatch.Add(modEntry.Name + " (host " + modEntry.Version + ", you " + value.Version + ")");
				modDifference.VersionMismatchEntries.Add(modEntry);
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
