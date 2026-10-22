using System;
using System.Collections.Generic;

namespace Terraria;

/// <summary>
/// Finds an NPC type id by the name the game shows.
///
/// The same gap <see cref="ItemLookup"/> fills for items: there is no name→id table anywhere, only
/// <see cref="Lang.NpcName"/>, which is a switch over ids. Anything that takes an NPC "by name" -
/// a debug command, a future content file - would otherwise have to hardcode numbers, and the
/// numbers are exactly what a person cannot remember.
///
/// Built once, in English: the names come from <see cref="Lang.NpcName"/> in the player's language,
/// while commands and files are written in English. Only a table is read while the language is
/// forced, so restoring it immediately is safe.
/// </summary>
internal static class NpcLookup
{
	private static Dictionary<string, int> _byName;

	private static readonly object Sync = new object();

	/// <summary>Resolves a name to a type id. Returns 0 when the name is unknown.</summary>
	internal static int Find(string name)
	{
		int result;
		return TryFind(name, out result) ? result : 0;
	}

	internal static bool TryFind(string name, out int type)
	{
		type = 0;
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		// A number is accepted as-is: "!spawnnpc 4" has to work, because ids are what the
		// existing command takes and what the debug world and the wiki use.
		int result;
		if (int.TryParse(name.Trim(), out result))
		{
			if (result > 0 && result < NPC.MaxNumNPCTypes)
			{
				type = result;
				return true;
			}
			return false;
		}
		return Map().TryGetValue(name.Trim(), out type) && type > 0;
	}

	/// <summary>Every name this build knows, for the error message.</summary>
	internal static List<string> Names()
	{
		List<string> list = new List<string>(Map().Keys);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	/// <summary>How many names are known. For the log and the tests.</summary>
	internal static int Count => Map().Count;

	/// <summary>Drops the cached map. For a language change and for the tests.</summary>
	internal static void Clear()
	{
		lock (Sync)
		{
			_byName = null;
		}
	}

	private static Dictionary<string, int> Map()
	{
		lock (Sync)
		{
			if (_byName != null)
			{
				return _byName;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			int langOption = Lang.LangOption;
			try
			{
				Lang.LangOption = 1;
				for (int i = 1; i < NPC.MaxNumNPCTypes; i++)
				{
					string text;
					try
					{
						text = Lang.NpcName(i);
					}
					catch
					{
						// A type whose name switch throws is not worth a failed lookup for the rest.
						continue;
					}
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					// First name wins: several ids share a name (slime variants), and the lower id
					// is the one a person means when they type "Green Slime".
					if (!dictionary.ContainsKey(text))
					{
						dictionary[text] = i;
					}
				}
			}
			finally
			{
				Lang.LangOption = langOption;
			}
			_byName = dictionary;
			return dictionary;
		}
	}
}
