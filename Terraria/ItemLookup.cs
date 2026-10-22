using System;
using System.Collections.Generic;

namespace Terraria;

/// <summary>
/// Finds an item id by the name the game shows.
///
/// This exists because <c>Item.SetDefaults(string)</c> is not a lookup: it is a switch over the
/// two dozen legacy console items (the gold/silver/copper tools), and any other name falls through
/// doing nothing. So <c>SetDefaults("Copper Bar")</c> silently produces an empty item - which meant
/// every name-based path in the port worked only for those two dozen names:
/// <c>Context.Item</c>, the tConfig importer, recipe ingredients and the converter.
///
/// The real table is <see cref="Lang.ItemName"/>, which covers every id; this builds the reverse of
/// it once and caches it.
/// </summary>
internal static class ItemLookup
{
	private static Dictionary<string, int> _byName;

	private static readonly object Sync = new object();

	/// <summary>Resolves a name to an id. Returns 0 when the name is unknown.</summary>
	internal static int Find(string name)
	{
		int result;
		return TryFind(name, out result) ? result : 0;
	}

	/// <summary>
	/// The same lookup, but only among the items this build ships - never a mod's.
	///
	/// Exists for <c>ModItems.Register</c>: a new item may not take a name that already belongs to
	/// a vanilla item, and deciding that needs the vanilla table on its own. Asking <see cref="Find"/>
	/// would answer with whichever mod item happened to be registered first, which is the very
	/// ambiguity being prevented.
	/// </summary>
	internal static int FindVanilla(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return 0;
		}
		int result;
		return Map().TryGetValue(name.Trim(), out result) ? result : 0;
	}

	internal static bool TryFind(string name, out int type)
	{
		type = 0;
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		// Mod items first: their names live in the registry, and the vanilla map below was built
		// before any mod loaded.
		int num = Terraria.Mods.ModItems.FindByName(name);
		if (num > 0)
		{
			type = num;
			return true;
		}
		Dictionary<string, int> map = Map();
		return map.TryGetValue(name.Trim(), out type) && type > 0;
	}

	/// <summary>
	/// The name→id map, built once.
	///
	/// Built with <see cref="Lang.LangOption"/> forced to English: mod files, tConfig packs and
	/// saved recipes are written in English, while <c>Lang.ItemName</c> answers in the player's
	/// language. A German client would otherwise build a German-only map and fail to resolve
	/// "Copper Bar" from its own mod files. Only a table is read here - nothing refills - so
	/// restoring the option immediately is safe.
	/// </summary>
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
				// Negative ids first: they are the legacy console items (gold/silver/copper tools),
				// and their names exist nowhere else. "Copper Shortsword" is one of them, which is
				// why looking only at positive ids left the most common test item unresolvable.
				for (int i = -64; i < Item.MaxNumItemTypes; i++)
				{
					if (i == 0)
					{
						continue;
					}
					Item item = default(Item);
					try
					{
						// Negative ids are filled by NetDefaults, positive ones by SetDefaults -
						// the debug kit hit the same split.
						if (i < 0)
						{
							item.NetDefaults(i);
						}
						else
						{
							item.SetDefaults(i);
						}
					}
					catch
					{
						// An id that throws is not worth a failed lookup for the rest.
						continue;
					}
					if (item.Type <= 0)
					{
						continue;
					}
					string text = null;
					try
					{
						// The name comes from NetID: for a legacy item the Type is positive but the
						// name table is keyed on the negative id.
						text = Lang.ItemName(item.NetID);
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text) || dictionary.ContainsKey(text))
					{
						continue;
					}
					// Store the positive Type: that is what SetDefaults takes.
					dictionary[text] = item.Type;
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

	/// <summary>Drops the cached map. For a reload and for the tests.</summary>
	internal static void Clear()
	{
		lock (Sync)
		{
			_byName = null;
		}
	}

	/// <summary>How many names are known. For the log and the tests.</summary>
	internal static int Count
	{
		get
		{
			return Map().Count;
		}
	}

	/// <summary>How long the table took to build, in milliseconds. For the log.</summary>
	internal static long BuildMs { get; private set; }

	/// <summary>
	/// Builds the table now, on purpose.
	///
	/// It is built by calling <c>Item.SetDefaults</c> for every id in the build, which is tens of
	/// milliseconds of work. Doing that on first use means the first hover over the crafting tab,
	/// or the first file of a pack conversion, pays for it in one frame - the hitch. Called from
	/// the loading screen instead, where the game already shows a progress bar and a pause is
	/// invisible.
	///
	/// Safe to call more than once: the map is cached, and a second call is a no-op.
	/// </summary>
	internal static void Warm()
	{
		long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
		int num = Count;
		BuildMs = (System.Diagnostics.Stopwatch.GetTimestamp() - timestamp) * 1000L
			/ System.Diagnostics.Stopwatch.Frequency;
		Program.Log("[ItemLookup] " + num + " item names indexed in " + BuildMs + " ms");
	}
}
