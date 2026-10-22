using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Mods;

/// <summary>
/// Entry point of the mod system (see docs/MODDING.md). Mods live in a "Mods" folder next
/// to the executable: one folder per mod, each holding a compiled assembly plus optional
/// assets. The loader is transport agnostic; multiplayer compatibility is enforced through
/// <see cref="Signature"/>, which is published in the session properties so clients can
/// detect a mismatch before joining.
/// </summary>
internal static class ModLoader
{
	/// <summary>Settings.ini: [Mods] Enabled.</summary>
	public static bool Enabled = true;

	/// <summary>Settings.ini: [Mods] RequireMatchingMods.</summary>
	public static bool RequireMatchingMods = true;

	/// <summary>File inside the Mods folder that records the load order.</summary>
	public const string LoadOrderFile = "loadorder.txt";

	private static readonly List<Mod> LoadedMods = new List<Mod>();

	private static readonly List<string> Errors = new List<string>();

	private static readonly HashSet<Mod> BrokenMods = new HashSet<Mod>();

	/// <summary>Mod ids we have received packets from but do not know; logged once each.</summary>
	private static readonly List<string> UnknownPacketSenders = new List<string>();

	/// <summary>Graphics device handed to mods for texture loading.</summary>
	public static GraphicsDevice GraphicsDevice { get; set; }

	/// <summary>True once <see cref="Initialize"/> has run.</summary>
	public static bool IsInitialized { get; private set; }

	/// <summary>Deterministic signature of the loaded mod set (FNV-1a over names + versions).</summary>
	public static string Signature { get; private set; } = "vanilla";

	public static int SignatureHash1 { get; private set; }

	public static int SignatureHash2 { get; private set; }

	public static int ModCount => LoadedMods.Count;

	public static string ModsDirectory => Terraria.Storage.AppPaths.Mods;

	public static IList<Mod> Mods => LoadedMods;

	public static IList<string> LoadErrors => Errors;

	/// <summary>
	/// True when a peer with this signature may join.
	///
	/// The comparison is symmetric on purpose. It used to return true whenever the local set was
	/// empty, which meant a clean install could join a modded server: the one case that breaks
	/// first was the one case that was not checked. A player with no mods saw different item
	/// stats from everyone else and nothing said why.
	///
	/// <see cref="RequireMatchingMods"/> is still the escape hatch, and it is a deliberate choice
	/// by whoever turns it off - the host's setting is what governs, because the host is the one
	/// whose world everyone else has to agree with.
	/// </summary>
	public static bool IsCompatible(string signature)
	{
		return IsCompatible(Signature, signature);
	}

	/// <summary>
	/// The rule itself, with the expected signature passed in.
	///
	/// Separate so a test can exercise the real comparison without having to install mods: the
	/// thing being checked is "the host's set and the client's set must agree", and that has
	/// nothing to do with which mods happen to be on disk.
	/// </summary>
	internal static bool IsCompatible(string expected, string actual)
	{
		if (!RequireMatchingMods)
		{
			return true;
		}
		return string.Equals(actual ?? string.Empty, expected ?? string.Empty, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Loads every mod once. Safe to call multiple times.</summary>
	public static void Initialize()
	{
		if (IsInitialized)
		{
			return;
		}
		IsInitialized = true;
		// One subscription for the whole run: the loader fans packets out to mods by name.
		Terraria.Net.ModNet.Received -= HandleModPacket;
		Terraria.Net.ModNet.Received += HandleModPacket;
		if (!Enabled)
		{
			Log("Mods disabled by settings");
			RecomputeSignature();
			return;
		}
		string text = ModsDirectory;
		if (!System.IO.Directory.Exists(text))
		{
			try
			{
				System.IO.Directory.CreateDirectory(text);
			}
			catch
			{
			}
			Log("No Mods folder yet, created " + text);
			RecomputeSignature();
			return;
		}
		string[] directories = System.IO.Directory.GetDirectories(text);
		directories = ApplyLoadOrder(directories, text);
		string[] array = directories;
		foreach (string text2 in array)
		{
			string[] files = System.IO.Directory.GetFiles(text2, "*.dll", SearchOption.TopDirectoryOnly);
			if (files.Length == 0)
			{
				continue;
			}
			if (File.Exists(Path.Combine(text2, "disabled.txt")))
			{
				Log("Skipping disabled mod folder " + Path.GetFileName(text2));
				continue;
			}
			foreach (string path in files)
			{
				LoadAssembly(path, text2);
			}
		}
		RecomputeSignature();
		Log($"Loaded {LoadedMods.Count} mod(s), signature {Signature}");
	}

	/// <summary>
	/// Orders mod folders by <c>Mods/loadorder.txt</c>, then everything not listed
	/// alphabetically. Load order matters as soon as two mods touch the same item: the
	/// last writer wins, so the player has to be able to say who that is.
	/// </summary>
	internal static string[] ApplyLoadOrder(string[] directories, string modsDirectory)
	{
		Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
		List<string> list = ReadLoadOrder(modsDirectory);
		if (list.Count == 0)
		{
			return directories;
		}
		List<string> list2 = new List<string>(directories.Length);
		// Listed folders first, in the listed order.
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = 0; j < directories.Length; j++)
			{
				if (string.Equals(Path.GetFileName(directories[j]), list[i], StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(directories[j]);
					break;
				}
			}
		}
		// Then the rest, alphabetically, so an incomplete file is not a trap.
		for (int k = 0; k < directories.Length; k++)
		{
			if (!list2.Contains(directories[k]))
			{
				list2.Add(directories[k]);
			}
		}
		return list2.ToArray();
	}

	/// <summary>Reads <c>Mods/loadorder.txt</c>; blank lines and <c>#</c> comments are ignored.</summary>
	internal static List<string> ReadLoadOrder(string modsDirectory)
	{
		List<string> list = new List<string>();
		try
		{
			string path = Path.Combine(modsDirectory, LoadOrderFile);
			if (!File.Exists(path))
			{
				return list;
			}
			string[] array = File.ReadAllLines(path);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length != 0 && !text.StartsWith("#"))
				{
					list.Add(text);
				}
			}
		}
		catch (Exception ex)
		{
			Log("Could not read " + LoadOrderFile + ": " + ex.Message);
		}
		return list;
	}

	/// <summary>Writes the load order. Best effort: failing to save must not break the game.</summary>
	internal static bool WriteLoadOrder(string modsDirectory, List<string> order)
	{
		try
		{
			if (!Directory.Exists(modsDirectory))
			{
				Directory.CreateDirectory(modsDirectory);
			}
			List<string> list = new List<string>(order.Count + 2);
			list.Add("# Mod load order, top to bottom. Later entries load last and win conflicts.");
			list.Add("# Folders missing from this file load after the listed ones, alphabetically.");
			list.AddRange(order);
			File.WriteAllLines(Path.Combine(modsDirectory, LoadOrderFile), list.ToArray());
			return true;
		}
		catch (Exception ex)
		{
			Log("Could not write " + LoadOrderFile + ": " + ex.Message);
			return false;
		}
	}

	// --- Mod networking -----------------------------------------------------

	/// <summary>
	/// Routes an inbound mod packet to the mod that sent it. Subscribed once in
	/// <see cref="Initialize"/>; runs on the network thread, which is why
	/// <see cref="Mod.OnPacket"/> forbids touching the world there.
	/// </summary>
	private static void HandleModPacket(string modId, byte[] payload)
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			if (!string.Equals(mod.Name, modId, StringComparison.Ordinal))
			{
				continue;
			}
			try
			{
				mod.OnPacket(payload);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
			return;
		}
		// A peer runs a mod we do not have. The handshake already refuses mismatched sets,
		// so this only happens when matching is disabled - log it once per id.
		if (!UnknownPacketSenders.Contains(modId))
		{
			UnknownPacketSenders.Add(modId);
			Log("Ignoring packet from a mod that is not loaded here: " + modId);
		}
	}

	private static void LoadAssembly(string assemblyPath, string modDirectory)
	{
		Assembly assembly;
		try
		{
			assembly = Assembly.LoadFrom(assemblyPath);
		}
		catch (Exception ex)
		{
			RecordError("Could not load " + assemblyPath + ": " + ex.Message);
			return;
		}
		ModInfoAttribute modInfoAttribute = null;
		try
		{
			object[] customAttributes = assembly.GetCustomAttributes(typeof(ModInfoAttribute), inherit: false);
			if (customAttributes.Length != 0)
			{
				modInfoAttribute = (ModInfoAttribute)customAttributes[0];
			}
		}
		catch
		{
		}
		Type[] types;
		try
		{
			types = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex2)
		{
			types = ex2.Types;
		}
		catch (Exception ex3)
		{
			RecordError("Could not inspect " + assemblyPath + ": " + ex3.Message);
			return;
		}
		Type[] array = types;
		foreach (Type type in array)
		{
			if (type == null || type.IsAbstract || !typeof(Mod).IsAssignableFrom(type))
			{
				continue;
			}
			try
			{
				Mod mod = (Mod)Activator.CreateInstance(type);
				mod.Info = modInfoAttribute ?? new ModInfoAttribute(type.Name, "0.0.0");
				mod.Context = new ModContext(mod.Info.Name, modDirectory);
				mod.Load();
				LoadedMods.Add(mod);
				Log("Loaded " + mod.Info.Name + " v" + mod.Info.Version + " (" + type.FullName + ")");
			}
			catch (Exception ex4)
			{
				RecordError("Failed to initialize " + type.FullName + ": " + ex4.Message);
			}
		}
	}

	private static void RecomputeSignature()
	{
		if (LoadedMods.Count == 0)
		{
			Signature = "vanilla";
			SignatureHash1 = 0;
			SignatureHash2 = 0;
			return;
		}
		List<string> list = new List<string>();
		foreach (Mod loadedMod in LoadedMods)
		{
			list.Add(loadedMod.Name + "|" + loadedMod.Version);
		}
		list.Sort(StringComparer.Ordinal);
		ulong num = 14695981039346656037uL;
		foreach (string item in list)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(item + "\n");
			byte[] array = bytes;
			foreach (byte b in array)
			{
				num ^= b;
				num *= 1099511628211;
			}
		}
		Signature = num.ToString("x16");
		SignatureHash1 = (int)(uint)num;
		SignatureHash2 = (int)(uint)(num >> 32);
	}

	// --- Hook dispatch ------------------------------------------------------

	public static void GameUpdate()
	{
		if (LoadedMods.Count == 0)
		{
			return;
		}
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnGameUpdate();
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	public static void PlayerUpdate(Player player)
	{
		if (LoadedMods.Count == 0)
		{
			return;
		}
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnPlayerUpdate(player);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	public static void WorldLoaded()
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnWorldLoaded();
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	public static void WorldSaving()
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnWorldSaving();
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	// --- World events -------------------------------------------------------

	/// <summary>
	/// Fans a tile event out to every healthy mod, in load order. A mod that throws is
	/// disabled exactly like it is for the other hooks: one bad mod must not stop the rest.
	/// </summary>
	internal static void DispatchTilePlaced(int x, int y)
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnTilePlaced(x, y);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	internal static void DispatchTileKilled(int x, int y)
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnTileKilled(x, y);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	internal static void DispatchNpcHurt(NPC npc, int damage)
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnNpcHurt(npc, damage);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	internal static void DispatchNpcSpawn(NPC npc)
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnNpcSpawn(npc);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	internal static void DispatchNpcDeath(NPC npc)
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnNpcDeath(npc);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	internal static void DispatchPlayerHurt(Player player, int damage)
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnPlayerHurt(player, damage);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	internal static void DispatchLiquidChanged(int x, int y)
	{
		for (int i = 0; i < LoadedMods.Count; i++)
		{
			Mod mod = LoadedMods[i];
			if (BrokenMods.Contains(mod))
			{
				continue;
			}
			try
			{
				mod.OnLiquidChanged(x, y);
			}
			catch (Exception ex)
			{
				OnModCrash(mod, ex);
			}
		}
	}

	public static void Unload()
	{
		for (int i = LoadedMods.Count - 1; i >= 0; i--)
		{
			try
			{
				LoadedMods[i].Unload();
			}
			catch
			{
			}
		}
		LoadedMods.Clear();
		BrokenMods.Clear();
		UnknownPacketSenders.Clear();
		Terraria.Net.ModNet.Received -= HandleModPacket;
		IsInitialized = false;
		Signature = "vanilla";
	}

	private static void OnModCrash(Mod mod, Exception ex)
	{
		BrokenMods.Add(mod);
		RecordError("Mod '" + mod.Name + "' crashed and was disabled: " + ex);
	}

	private static void RecordError(string message)
	{
		Errors.Add(message);
		Log("ERROR: " + message);
	}

	public static void Log(string message)
	{
		try
		{
			Program.Log("[Mods] " + message);
		}
		catch
		{
		}
	}
}
