using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Terraria.Net;
using Terraria.World;

namespace Terraria.Headless;

/// <summary>
/// Graphics-free verification suite, runnable as <c>TerrariaXDK.exe --selftest</c>.
///
/// It exists because the game itself needs a real OpenGL 2.1 device, which CI machines and
/// some handhelds do not have at build time. Every check here is pure logic: content
/// integrity, wire framing, transport handshake, config parsing, mod loading and cheat gating.
/// </summary>
internal static class SelfTest
{
	private static int _passed;

	private static int _failed;

	private static int _skipped;

	private static readonly List<string> Failures = new List<string>();

	private static readonly List<string> Skips = new List<string>();

	/// <summary>
	/// Thrown by <see cref="Skip"/> to mark a check as "not applicable here". Distinct from a
	/// failure on purpose: a machine without the game's Content folder cannot verify the asset
	/// manifest, and reporting that as a failure would train everyone to ignore the suite.
	/// </summary>
	private sealed class SkipCheckException : Exception
	{
		public SkipCheckException(string reason) : base(reason)
		{
		}

		/// <summary>Marks the current check as skipped. Never returns.</summary>
		internal static void Skip(string reason)
		{
			throw new SkipCheckException(reason);
		}
	}

	/// <summary>Runs every check and returns a process exit code.</summary>
	public static int Run(string[] args)
	{
		Console.WriteLine("TerrariaXDK self-test");
		Console.WriteLine("=====================");
		// Must be set before EnsureDirectories: it keeps the suite from initialising FNAPlatform,
		// which needs SDL2. See AppPaths.Headless.
		Terraria.Storage.AppPaths.Headless = true;
		Terraria.Storage.AppPaths.EnsureDirectories();
		RunCheck("app paths", CheckAppPaths);
		RunCheck("content manifest", CheckContentManifest);
		RunCheck("content orphans", CheckContentOrphans);
		RunCheck("wire framing", CheckWireFormat);
		RunCheck("tcp loopback", CheckTcpLoopback);
		RunCheck("host and two clients", CheckHostAndTwoClients);
		RunCheck("connect timeout", CheckConnectTimeout);
		RunCheck("network config", CheckNetworkConfig);
		RunCheck("mod recipes", CheckModRecipes);
		RunCheck("mod items", CheckModItems);
		RunCheck("mod loader", CheckModLoader);
		RunCheck("cheat gating", CheckCheatGating);
		RunCheck("cheat badge", CheckCheatBadge);
		RunCheck("loading progress", CheckLoadingProgress);
		RunCheck("input mapping", CheckInputMapping);
		RunCheck("wheel scrolling", CheckWheelScrolling);
		RunCheck("log rotation", CheckLogRotation);
		RunCheck("settings sections", CheckSettingsSections);
		RunCheck("network stats", CheckNetworkStats);
		RunCheck("key bindings", CheckKeyBindings);
		RunCheck("settings persistence", CheckSettingsPersistence);
		RunCheck("tconfig import", CheckTConfigImport);

		RunCheck("tconfig convert", CheckTConfigConvert);
		RunCheck("tconfig resilience", CheckTConfigResilience);
		RunCheck("mod load order", CheckModLoadOrder);
		RunCheck("mod set requirement", CheckModSetRequirement);
		RunCheck("mod menu kinds", CheckModMenuKinds);
		RunCheck("virtual keyboard", CheckVirtualKeyboard);
		RunCheck("name prompt", CheckNamePrompt);
		RunCheck("player identity", CheckPlayerIdentity);
		RunCheck("display settings", CheckDisplaySettings);
		RunCheck("display profiles", CheckDisplayProfiles);
		RunCheck("language", CheckLanguage);
		RunCheck("language overrides", CheckLangOverrides);
		RunCheck("feature text", CheckFeatureTexts);
		RunCheck("reject reasons", CheckRejectReasons);
		RunCheck("portable paths", CheckPortablePaths);
		RunCheck("png decoder", CheckPngDecoder);
		RunCheck("texture overrides", CheckTextureOverrides);
		RunCheck("png corpus", CheckPngCorpus);
		RunCheck("atlas overrides", CheckAtlasOverrides);
		RunCheck("anti-cheat", CheckAntiCheat);
		RunCheck("menu tables", CheckMenuTables);
		RunCheck("text entry", CheckTextEntry);
		RunCheck("pointer scaling", CheckPointerScaling);
		RunCheck("modal input latch", CheckModalInputLatch);
		RunCheck("menu hit table", CheckMenuHitTable);
		RunCheck("creation flow back", CheckCreateFlowBack);
		RunCheck("typed characters", CheckTypedCharacters);
		RunCheck("relay client", CheckRelayClient);
		RunCheck("upnp", CheckUPnP);
		RunCheck("packet batching", CheckPacketBatching);
		RunCheck("edit budget", CheckEditBudget);
		RunCheck("server cards", CheckServerCards);
		RunCheck("font atlas", CheckFontAtlas);
		RunCheck("item names", CheckItemNames);
		RunCheck("inventory grid", CheckInventoryGrid);
		RunCheck("inventory once per frame", CheckInventoryOncePerFrame);
		RunCheck("custom language", CheckCustomLanguage);
		RunCheck("whitelist", CheckWhitelist);
		RunCheck("relay session", CheckRelaySession);
		RunCheck("relay live", CheckRelayLive);
		RunCheck("keyboard grid", CheckKeyboardGrid);
		RunCheck("inventory transfer", CheckInventoryTransfer);
		RunCheck("save slots", CheckSaveSlots);
		RunCheck("map screen mouse", CheckMapScreenMouse);
		RunCheck("screen geometry", CheckScreenGeometry);
		RunCheck("master server live", CheckMasterServerLive);
		RunCheck("achievement stats", CheckAchievementStats);
		RunCheck("every text key used", CheckTextKeysUsed);
		RunCheck("cheat menu layout", CheckCheatMenuLayout);
		RunCheck("npc names", CheckNpcNames);
		RunCheck("tutorial hints", CheckTutorialHints);
		RunCheck("crafting tables", CheckCraftingTables);
		RunCheck("mod sheets", CheckModSheets);
		RunCheck("item field widths", CheckItemFieldWidths);
		RunCheck("item fields hostile", CheckItemFieldsHostile);
		RunCheck("raw mod", CheckRawMod);
		RunCheck("mod download", CheckModDownload);
#if DEBUG
		RunCheck("debug kit", CheckDebugKit);
		RunCheck("debug console", CheckDebugConsole);
		RunCheck("debug host console", CheckDebugHostConsole);
#endif
		RunCheck("hud settings", CheckHudSettings);
		RunCheck("direct connect", CheckDirectConnect);
		RunCheck("session record", CheckSessionRecord);
		RunCheck("json", CheckJson);
		RunCheck("master server", CheckMasterServer);
		RunCheck("master endpoints", CheckMasterEndpoints);
		RunCheck("lan discovery", CheckLanDiscovery);
		RunCheck("mod packets", CheckModPackets);
		RunCheck("mod world events", CheckModWorldEvents);
		RunCheck("saved servers", CheckSavedServers);
		RunCheck("auto refresh", CheckAutoRefresh);
		RunCheck("server ping", CheckServerPing);
		RunCheck("world backups", CheckWorldBackups);
		RunCheck("backup restore", CheckBackupRestore);
		RunCheck("host commands", CheckHostCommands);
		RunCheck("moderation", CheckModeration);
		RunCheck("permissions", CheckPermissions);
		RunCheck("world protection", CheckWorldProtection);
		RunCheck("regions", CheckRegions);
		RunCheck("item restrictions", CheckItemRestrictions);
		RunCheck("server config", CheckServerConfig);
		RunCheck("sound routing", CheckSoundRouting);
		RunCheck("world file inspector", CheckWorldFileInspector);
		RunCheck("ban list", CheckBanList);
		ReportMemory();
		Console.WriteLine();
		Console.WriteLine($"Passed: {_passed}  Skipped: {_skipped}  Failed: {_failed}");
		if (_skipped != 0)
		{
			Console.WriteLine();
			foreach (string skip in Skips)
			{
				Console.WriteLine("  skip " + skip);
			}
		}
		if (_failed != 0)
		{
			Console.WriteLine();
			foreach (string failure in Failures)
			{
				Console.WriteLine("  FAIL " + failure);
			}
		}
		return (_failed == 0) ? 0 : 1;
	}

	private static void Skip(string reason)
	{
		SkipCheckException.Skip(reason);
	}

	private static void RunCheck(string name, Func<bool> check)
	{
		bool flag;
		string message = null;
		try
		{
			flag = check();
		}
		catch (Exception ex)
		{
			// A skip that reaches this far still counts as a skip, whatever wrapped it: a check
			// with its own catch-all used to turn "Content is not installed" into a failure.
			if (ex is SkipCheckException)
			{
				RecordSkip(name, ex.Message);
				return;
			}
			flag = false;
			message = ex.GetType().Name + ": " + ex.Message;
		}
		if (flag)
		{
			_passed++;
			Console.WriteLine("  ok   " + name);
			return;
		}
		_failed++;
		string text2 = (message == null) ? name : (name + " - " + message);
		Failures.Add(text2);
		Console.WriteLine("  FAIL " + text2);
	}

	private static void RecordSkip(string name, string reason)
	{
		_skipped++;
		string text = name + (string.IsNullOrEmpty(reason) ? "" : (" - " + reason));
		Skips.Add(text);
		Console.WriteLine("  skip " + text);
	}

	// --- Storage ------------------------------------------------------------

	/// <summary>
	/// The install must be portable: every mutable path lives under Storage/, next to the
	/// executable, never in a per-user or system location.
	/// </summary>
	private static bool CheckAppPaths()
	{
		Terraria.Storage.AppPaths.EnsureDirectories();
		string baseDir = Terraria.Storage.AppPaths.BaseDirectory;
		string root = Terraria.Storage.AppPaths.Root;
		if (!root.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
		{
			return Fail("Storage root is not next to the executable: " + root);
		}
		if (!Path.GetFileName(root).Equals("Storage", StringComparison.OrdinalIgnoreCase))
		{
			return Fail("Storage folder has unexpected name: " + root);
		}
		string[] array = new string[7]
		{
			Terraria.Storage.AppPaths.Saves, Terraria.Storage.AppPaths.Logs, Terraria.Storage.AppPaths.Mods, Terraria.Storage.AppPaths.Backups, Terraria.Storage.AppPaths.Config, Terraria.Storage.AppPaths.Cache, Terraria.Storage.AppPaths.Lang
		};
		foreach (string text in array)
		{
			if (!Directory.Exists(text))
			{
				return Fail("missing storage subfolder: " + text);
			}
			if (!text.StartsWith(root, StringComparison.OrdinalIgnoreCase))
			{
				return Fail("storage subfolder escaped the root: " + text);
			}
		}
		if (!Terraria.Storage.AppPaths.LogFile.StartsWith(Terraria.Storage.AppPaths.Logs, StringComparison.OrdinalIgnoreCase))
		{
			return Fail("debug log is outside Storage/Logs");
		}
		if (!Terraria.Storage.AppPaths.CrashLogFile.StartsWith(Terraria.Storage.AppPaths.Logs, StringComparison.OrdinalIgnoreCase))
		{
			return Fail("crash log is outside Storage/Logs");
		}
		if (!Terraria.Storage.AppPaths.SettingsFile.StartsWith(root, StringComparison.OrdinalIgnoreCase))
		{
			return Fail("settings file is outside Storage/");
		}
		// The XNA storage device must be redirected, otherwise saves land in the user profile.
		// In headless mode the redirect is deliberately skipped (see AppPaths.Headless), so the
		// check verifies the path that would be used instead of touching XNA at all.
		if (Terraria.Storage.AppPaths.Headless)
		{
			if (!Terraria.Storage.AppPaths.Saves.StartsWith(root, StringComparison.OrdinalIgnoreCase))
			{
				return Fail("the storage device root would escape Storage/");
			}
		}
		else if (!string.Equals(Microsoft.Xna.Framework.Storage.StorageDevice.RootOverride, Terraria.Storage.AppPaths.Saves, StringComparison.OrdinalIgnoreCase))
		{
			return Fail("storage device is not redirected to Storage/Saves");
		}
		if (!string.Equals(Terraria.Mods.ModLoader.ModsDirectory, Terraria.Storage.AppPaths.Mods, StringComparison.OrdinalIgnoreCase))
		{
			return Fail("mods folder is not under Storage/");
		}
		return true;
	}

	// --- Content ------------------------------------------------------------

	internal static string ContentRoot => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");

	/// <summary>
	/// Every asset path that game code passes to Content.Load, generated the same way the
	/// loaders build them, so this list cannot silently drift from reality.
	/// </summary>
	internal static List<string> RequiredAssets()
	{
		List<string> list = new List<string>();
		// Splash + rating logos (Main.LoadContent / Lang.SetSystemLang).
		list.Add("Images/logo_1");
		for (int i = 2; i <= 4; i++)
		{
			list.Add("Images/logo_" + i);
		}
		list.Add("Images/Logo");
		list.Add("Images/Logo2");
		list.Add("Images/Text_Back");
		list.Add("Images/Chat_Back");
		list.Add("Images/Cursor");
		list.Add("Images/UpsellHM");
		list.Add("Images/_sheetSprites");
		list.Add("Images/_sheetTiles");
		// Region rating splashes.
		list.Add("ESRB");
		list.Add("PEGI");
		list.Add("USK");
		list.Add("ACB");
		// Parallax backgrounds (WorldView.LoadContent loops 3..31).
		for (int j = 3; j < 32; j++)
		{
			list.Add("Images/Background_" + j);
		}
		// Fonts. The suffix sets mirror UI.LoadFonts / UI.LoadSplitscreenFonts exactly:
		// only big, small and small2 (and big_sc / small2_sc) have PS3 variants.
		string[,] fonts = new string[12, 2]
		{
			{ "big", "_PS3" },
			{ "stack", "" },
			{ "combat", "" },
			{ "combat2", "" },
			{ "small", "_PS3" },
			{ "small2", "_PS3" },
			{ "big_sc", "_PS3" },
			{ "stack_sc", "" },
			{ "combat_sc", "" },
			{ "combat2_sc", "" },
			{ "small_sc", "" },
			{ "small2_sc", "_PS3" }
		};
		for (int n2 = 0; n2 < fonts.GetLength(0); n2++)
		{
			string text2 = fonts[n2, 0];
			string text3 = fonts[n2, 1];
			list.Add("Fonts/" + text2);
			list.Add("Fonts/HD/" + text2);
			if (text3.Length != 0)
			{
				list.Add("Fonts/" + text2 + text3);
				list.Add("Fonts/HD/" + text2 + text3);
			}
		}
		// UI chrome.
		string[] controllers =
		{
			"UI/XBController", "UI/XBDPad", "UI/PSController", "UI/PSDPad", "UI/XBControllerHD", "UI/XBDPadHD", "UI/PSControllerHD", "UI/PSDPadHD", "UI/ProgressBar", "UI/SoundBar", "UI/SoundIcons"
		};
		list.AddRange(controllers);
		// Achievements.
		for (int k = 0; k < 39; k++)
		{
			list.Add("Achievements/Achievement" + k.ToString("00"));
		}
		// Leaderboards: one icon per Column value.
		string[] columns =
		{
			"air", "ground", "water", "lava", "ore", "gems", "soils", "wood", "furniture", "tools", "weapons", "armor", "consumables", "misc", "potions", "lights", "ammo", "seeds", "keys", "king_slime", "cthulhu", "eater", "skeletron", "wall", "twins",
			"skeletron_prime", "destroyer", "ocram", "drowned", "fallen", "buried", "burned"
		};
		foreach (string column in columns)
		{
			list.Add("UI/Leaderboards/" + column + "_column");
		}
		// How to play.
		list.AddRange(new string[6]
		{
			"UI/HowToPlay/Text_Back", "UI/HowToPlay/Howto_Movement", "UI/HowToPlay/Howto_Hotbar2", "UI/HowToPlay/Howto_Debuff", "UI/HowToPlay/Howto_Inventory", "UI/HowToPlay/Howto_Logo"
		});
		// Character creation (CreateCharacter.Assets).
		list.AddRange(new string[22]
		{
			"UI/CharacterCreation/Tile01", "UI/CharacterCreation/Tile01_Bigger", "UI/CharacterCreation/Category_Gender", "UI/CharacterCreation/Category_Hair", "UI/CharacterCreation/Category_HairColor", "UI/CharacterCreation/Category_Eyes", "UI/CharacterCreation/Category_Skin",
			"UI/CharacterCreation/Category_Shirt", "UI/CharacterCreation/Category_Undershirt", "UI/CharacterCreation/Category_Pants", "UI/CharacterCreation/Category_Shoes", "UI/CharacterCreation/Category_Difficulty", "UI/CharacterCreation/Hair_Selector", "UI/CharacterCreation/Hair_Background",
			"UI/CharacterCreation/Palette", "UI/CharacterCreation/Color_Selector", "UI/CharacterCreation/Gender_Male", "UI/CharacterCreation/Gender_Female", "UI/CharacterCreation/Button_Difficulty_Soft", "UI/CharacterCreation/Button_Difficulty_Med", "UI/CharacterCreation/Button_Difficulty_Hard",
			"UI/CharacterCreation/Frame01"
		});
		// Upsell screens (one per language).
		string[] languages =
		{
			"de", "en", "es", "fr", "it"
		};
		foreach (string language in languages)
		{
			list.Add("UI/Upsell/01_" + language);
		}
		// Sound effects (Main.LoadAllSfxPools).
		list.AddRange(new string[2]
		{
			"UI/SoundBar", "UI/SoundIcons"
		});
		list.AddRange(new string[6]
		{
			"Sounds/Mech_0", "Sounds/Grab", "Sounds/Pixie", "Sounds/Dig_0", "Sounds/Dig_1", "Sounds/Dig_2"
		});
		for (int l = 0; l < 3; l++)
		{
			list.Add("Sounds/Tink_" + l);
			list.Add("Sounds/Player_Hit_" + l);
			list.Add("Sounds/Female_Hit_" + l);
		}
		list.AddRange(new string[16]
		{
			"Sounds/Player_Killed", "Sounds/Chat", "Sounds/Grass", "Sounds/Door_Opened", "Sounds/Door_Closed", "Sounds/Menu_Tick", "Sounds/Menu_Open", "Sounds/Menu_Close", "Sounds/Shatter", "Sounds/Splash_0", "Sounds/Splash_1", "Sounds/Double_Jump", "Sounds/Run", "Sounds/Coins",
			"Sounds/Unlock", "Sounds/MaxMana"
		});
		list.Add("Sounds/Drown");
		for (int m = 0; m < 5; m++)
		{
			list.Add("Sounds/Zombie_" + m);
		}
		for (int n = 0; n < 2; n++)
		{
			list.Add("Sounds/Roar_" + n);
		}
		for (int num = 1; num <= 37; num++)
		{
			list.Add("Sounds/Item_" + num);
		}
		for (int num2 = 1; num2 <= 11; num2++)
		{
			list.Add("Sounds/NPC_Hit_" + num2);
		}
		for (int num3 = 1; num3 <= 15; num3++)
		{
			list.Add("Sounds/NPC_Killed_" + num3);
		}
		return list;
	}

	private static bool CheckContentManifest()
	{
		string contentRoot = ContentRoot;
		if (!Directory.Exists(contentRoot))
		{
			// No Content folder: nothing to verify. CI and a bare checkout both hit this, and it
			// must not look like a broken build. The check still runs wherever content exists.
			Skip("Content folder not installed at " + contentRoot);
		}
		List<string> list = RequiredAssets();
		List<string> list2 = new List<string>();
		foreach (string item in list)
		{
			string path = Path.Combine(contentRoot, item.Replace('/', Path.DirectorySeparatorChar) + ".xnb");
			if (!File.Exists(path))
			{
				list2.Add(item);
			}
		}
		// Music and the tutorial world are loaded by raw path.
		string[] array = new string[19]
		{
			"Music_1", "Music_2", "Music_3", "Music_4", "Music_5", "Music_6", "Music_7", "Music_8", "Music_9", "Music_10",
			"Music_11", "Music_12", "Music_13", "Desert", "FloatingIsland", "Tutorial", "Boss4", "Ocean", "Snow"
		};
		foreach (string text in array)
		{
			// OGG is the streamed format; WAV is the decoded fallback. Either satisfies a cue.
			string path2 = Path.Combine(contentRoot, "ExtMusic", text + ".ogg");
			string path3 = Path.Combine(contentRoot, "ExtMusic", text + ".wav");
			if (!File.Exists(path2) && !File.Exists(path3))
			{
				list2.Add("ExtMusic/" + text);
			}
		}
		if (!File.Exists(Path.Combine(contentRoot, "Worlds", "tutorial.wld")))
		{
			list2.Add("Worlds/tutorial");
		}
		if (list2.Count == 0)
		{
			return true;
		}
		return Fail(list2.Count + " missing asset(s): " + string.Join(", ", list2.ToArray()));
	}

	/// <summary>
	/// Flags content files that no loader above asks for. Raw art sources (.dds/.png), the
	/// legacy XACT banks and FontsOld are intentionally absent; anything else showing up here
	/// is dead weight that should be reviewed before shipping.
	/// </summary>
	private static bool CheckContentOrphans()
	{
		string contentRoot = ContentRoot;
		if (!Directory.Exists(contentRoot))
		{
			Skip("Content folder not installed at " + contentRoot);
		}
		List<string> list = ContentAudit.FindUnexpected(contentRoot, RequiredAssets());
		if (list.Count == 0)
		{
			return true;
		}
		return Fail(list.Count + " unexpected content file(s): " + string.Join(", ", list.ToArray()));
	}

	// --- Transport ----------------------------------------------------------

	private static bool CheckWireFormat()
	{
		byte[] array = WireFormat.Frame(9, new byte[3]
		{
			1, 2, 3
		});
		using (MemoryStream stream = new MemoryStream(array))
		{
			byte[] array2 = WireFormat.ReadFrame(stream);
			if (array2 == null || array2.Length != 4 || array2[0] != 9 || array2[1] != 1 || array2[3] != 3)
			{
				return Fail("frame round-trip mismatch");
			}
		}
		// Empty payload must still carry the opcode.
		using (MemoryStream stream2 = new MemoryStream(WireFormat.Frame(4, null)))
		{
			byte[] array3 = WireFormat.ReadFrame(stream2);
			if (array3 == null || array3.Length != 1 || array3[0] != 4)
			{
				return Fail("empty payload mismatch");
			}
		}
		// Oversized length must be rejected instead of allocating.
		byte[] array4 = new byte[6]
		{
			255, 255, 255, 127, 1, 1
		};
		using (MemoryStream stream3 = new MemoryStream(array4))
		{
			if (WireFormat.ReadFrame(stream3) != null)
			{
				return Fail("oversized frame was accepted");
			}
		}
		return true;
	}

	/// <summary>Real socket round trip using the framing + handshake shape of TcpPeer.</summary>
	private static bool CheckTcpLoopback()
	{
		System.Net.Sockets.TcpListener tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((System.Net.IPEndPoint)tcpListener.LocalEndpoint).Port;
		bool ok = true;
		System.Threading.Thread thread = new System.Threading.Thread(() =>
		{
			try
			{
				using System.Net.Sockets.TcpClient tcpClient = tcpListener.AcceptTcpClient();
				using NetworkStream stream = tcpClient.GetStream();
				using MemoryStream memoryStream = new MemoryStream();
				BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
				binaryWriter.Write((byte)1);
				binaryWriter.Write(Terraria.Net.TcpPeer.ProtocolVersionPublic);
				binaryWriter.Write("TestPeer");
				binaryWriter.Write(Terraria.Mods.ModLoader.Signature);
				binaryWriter.Flush();
				WireFormat.WriteAll(stream, WireFormat.LengthPrefix(memoryStream.ToArray()));
				byte[] array = WireFormat.ReadFrame(stream);
				if (array == null || array[0] != 4 || array.Length != 3 || array[1] != 42)
				{
					ok = false;
				}
				WireFormat.WriteAll(stream, WireFormat.Frame(4, new byte[2]
				{
					7,
					8
				}));
			}
			catch
			{
				ok = false;
			}
		});
		thread.IsBackground = true;
		thread.Start();
		try
		{
			using System.Net.Sockets.TcpClient tcpClient2 = new System.Net.Sockets.TcpClient();
			tcpClient2.NoDelay = true;
			tcpClient2.Connect(System.Net.IPAddress.Loopback, port);
			using NetworkStream stream2 = tcpClient2.GetStream();
			byte[] array2 = WireFormat.ReadFrame(stream2);
			if (array2 == null || array2[0] != 1)
			{
				return Fail("handshake frame not received");
			}
			using (MemoryStream memoryStream2 = new MemoryStream(array2, 1, array2.Length - 1))
			{
				BinaryReader binaryReader = new BinaryReader(memoryStream2, Encoding.UTF8);
				if (binaryReader.ReadInt32() != Terraria.Net.TcpPeer.ProtocolVersionPublic)
				{
					return Fail("protocol version mismatch");
				}
				if (binaryReader.ReadString() != "TestPeer")
				{
					return Fail("gamertag mismatch");
				}
				binaryReader.ReadString();
			}
			WireFormat.WriteAll(stream2, WireFormat.Frame(4, new byte[2]
			{
				42,
				43
			}));
			byte[] array3 = WireFormat.ReadFrame(stream2);
			if (array3 == null || array3.Length != 3 || array3[1] != 7 || array3[2] != 8)
			{
				return Fail("data echo mismatch");
			}
		}
		catch (Exception ex)
		{
			return Fail("loopback failed: " + ex.Message);
		}
		finally
		{
			try
			{
				tcpListener.Stop();
			}
			catch
			{
			}
		}
		thread.Join(2000);
		return ok;
	}

	/// <summary>
	/// Joining a dead host must fail quickly. The game blocks inside NetworkSession.Join,
	/// so an unbounded connect timeout looks like a freeze - the exact symptom reported
	/// when a server is down or firewalled.
	/// </summary>
	private static bool CheckConnectTimeout()
	{
		int port = FindFreePort();
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
		bool ok;
		try
		{
			using System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
			tcpClient.NoDelay = true;
			tcpClient.Connect(System.Net.IPAddress.Loopback, port);
			ok = false;
		}
		catch (System.Net.Sockets.SocketException)
		{
			// Connection refused is the expected, fast outcome on loopback.
			ok = true;
		}
		stopwatch.Stop();
		if (!ok)
		{
			return Fail("connected to a closed port");
		}
		if (stopwatch.ElapsedMilliseconds > NetworkConfig.ConnectTimeoutMs)
		{
			return Fail("refusal took " + stopwatch.ElapsedMilliseconds + " ms, longer than the timeout");
		}
		// The configured timeout must stay in a range that is usable on a handheld.
		if (NetworkConfig.ConnectTimeoutMs <= 0 || NetworkConfig.ConnectTimeoutMs > 15000)
		{
			return Fail("ConnectTimeoutMs out of sane range: " + NetworkConfig.ConnectTimeoutMs);
		}
		return true;
	}

	/// <summary>Asks the OS for an unused port by binding and immediately releasing one.</summary>
	private static int FindFreePort()
	{
		System.Net.Sockets.TcpListener tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((System.Net.IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}

	private static bool CheckNetworkConfig()
	{
		string host;
		int port;
		if (!NetworkConfig.TryParseAddress("192.168.1.5:7777", out host, out port) || host != "192.168.1.5" || port != 7777)
		{
			return Fail("host:port not parsed");
		}
		if (!NetworkConfig.TryParseAddress("example.org", out host, out port) || host != "example.org")
		{
			return Fail("bare host not parsed");
		}
		if (NetworkConfig.TryParseAddress("", out host, out port))
		{
			return Fail("empty address accepted");
		}
		if (NetworkConfig.TryParseAddress("host:99999", out host, out port))
		{
			return Fail("invalid port accepted");
		}
		if (!NetworkConfig.TryParseAddress("[::1]", out host, out port) || host != "::1")
		{
			return Fail("ipv6 literal not parsed");
		}
		// Password gate: empty means "no password", non-empty must match exactly.
		string saved = NetworkConfig.Password;
		try
		{
			NetworkConfig.Password = "";
			if (!TcpPeer.CheckPassword("anything") || !TcpPeer.CheckPassword(null))
			{
				return Fail("password gate rejects everyone when no password is set");
			}
			NetworkConfig.Password = "s3cret";
			if (!TcpPeer.CheckPassword("s3cret"))
			{
				return Fail("correct password rejected");
			}
			if (TcpPeer.CheckPassword("wrong") || TcpPeer.CheckPassword("") || TcpPeer.CheckPassword(null))
			{
				return Fail("wrong password accepted");
			}
		}
		finally
		{
			NetworkConfig.Password = saved;
		}
		return true;
	}

	private static bool CheckModLoader()
	{
		Terraria.Mods.ModLoader.Initialize();
		Console.WriteLine($"       mods loaded: {Terraria.Mods.ModLoader.ModCount}, signature {Terraria.Mods.ModLoader.Signature}");
		if (Terraria.Mods.ModItems.Count > 0)
		{
			// The one number that says whether a mod's declared items actually registered.
			Console.WriteLine("       new items from mods: " + Terraria.Mods.ModItems.Count);
		}
		if (Recipe.NumRecipes > 0)
		{
			Console.WriteLine("       recipes: " + Recipe.NumRecipes
				+ " (vanilla " + Recipe.MaxNumRecipes + ")");
		}
		for (int i = 0; i < Terraria.Mods.ModLoader.LoadErrors.Count; i++)
		{
			Console.WriteLine("       mod error: " + Terraria.Mods.ModLoader.LoadErrors[i]);
		}
		if (!Terraria.Mods.ModLoader.IsInitialized)
		{
			return Fail("loader did not initialize");
		}
		if (string.IsNullOrEmpty(Terraria.Mods.ModLoader.Signature))
		{
			return Fail("empty signature");
		}
		if (!Terraria.Mods.ModLoader.IsCompatible(Terraria.Mods.ModLoader.Signature))
		{
			return Fail("own signature rejected");
		}
		if (Terraria.Mods.ModLoader.RequireMatchingMods && Terraria.Mods.ModLoader.ModCount > 0 && Terraria.Mods.ModLoader.IsCompatible("deadbeefdeadbeef"))
		{
			return Fail("foreign signature accepted");
		}
		// Signature must be stable across recomputation.
		string signature = Terraria.Mods.ModLoader.Signature;
		Terraria.Mods.ModLoader.Unload();
		Terraria.Mods.ModLoader.Initialize();
		if (!string.Equals(signature, Terraria.Mods.ModLoader.Signature, StringComparison.Ordinal) && Terraria.Mods.ModLoader.ModCount == 0)
		{
			return Fail("signature not deterministic");
		}
		return true;
	}

	private static bool CheckCheatGating()
	{
		int netMode = Main.NetMode;
		try
		{
			Main.NetMode = 0;
			if (!Cheats.CheatState.IsAvailable)
			{
				return Fail("cheats unavailable in single player");
			}
			Cheats.CheatState.GodMode = true;
			if (!Cheats.CheatState.GodModeActive)
			{
				return Fail("god mode inactive in single player");
			}
			Main.NetMode = 1;
			if (Cheats.CheatState.IsAvailable)
			{
				return Fail("cheats available as client");
			}
			if (Cheats.CheatState.GodModeActive)
			{
				return Fail("god mode leaked into multiplayer");
			}
			Main.NetMode = 2;
			if (Cheats.CheatState.IsAvailable)
			{
				return Fail("cheats available as host");
			}
		}
		finally
		{
			Main.NetMode = netMode;
			Cheats.CheatState.Reset();
		}
		return true;
	}

	// --- Memory report ------------------------------------------------------

	/// <summary>
	/// Prints where RAM actually goes, derived from the files on disk. Not a pass/fail check:
	/// it exists so a regression ("loading got slower / hungrier") is visible as a number
	/// instead of a feeling. Music is the dominant term because WAV decodes to full PCM.
	/// </summary>
	private static void ReportMemory()
	{
		string contentRoot = ContentRoot;
		if (!Directory.Exists(contentRoot))
		{
			return;
		}
		long num = DirSize(contentRoot);
		string musicDir = Path.Combine(contentRoot, "ExtMusic");
		long num2 = DirSize(musicDir, "*.wav");
		long num9 = DirSize(musicDir, "*.ogg");
		long num3 = DirSize(Path.Combine(contentRoot, "Fonts"));
		long num4 = DirSize(Path.Combine(contentRoot, "Sounds"));
		// Count only the font set that a given display mode actually loads, so the number
		// reflects runtime RAM rather than everything sitting in the folder.
		long num5 = 0L;
		string[] files = Directory.GetFiles(contentRoot, "*", SearchOption.AllDirectories);
		foreach (string text in files)
		{
			string extension = Path.GetExtension(text);
			if (extension.Equals(".xnb", StringComparison.OrdinalIgnoreCase) && !text.Contains("Fonts"))
			{
				num5 += new FileInfo(text).Length;
			}
		}
		long num8 = DirSize(Path.Combine(contentRoot, "Fonts", "HD"));
		Console.WriteLine();
		Console.WriteLine("Storage / memory report");
		Console.WriteLine("-----------------------");
		Console.WriteLine($"  Content on disk        {Mb(num)}");
		Console.WriteLine($"    textures + images    {Mb(num5)}   decoded in RAM (uncompressed RGBA)");
		Console.WriteLine($"    fonts, both sets     {Mb(num3)}   only one set is loaded at a time");
		Console.WriteLine($"      of which Fonts/HD  {Mb(num8)}   loaded only at 1080p");
		Console.WriteLine($"    sounds               {Mb(num4)}   decoded to PCM on load");
		Console.WriteLine($"    music WAV (fallback) {Mb(num2)}   only decoded if no OGG for that cue");
		Console.WriteLine($"    music OGG (streamed) {Mb(num9)}   ~0 resident, decoded on the fly");
		int num6 = Directory.GetFiles(Path.Combine(contentRoot, "ExtMusic"), "*.wav").Length;
		int num7 = Directory.GetFiles(Path.Combine(contentRoot, "ExtMusic"), "*.ogg").Length;
		if (num7 > 0)
		{
			Console.WriteLine($"  note: {num7} OGG track(s) -> streamed, ~0 RAM; {num6} WAV kept as fallback (not loaded).");
		}
		else if (num6 > 0)
		{
			Console.WriteLine($"  note: {num6} WAV track(s), no OGG -> all decoded into RAM. See docs/PERFORMANCE.md.");
		}
		Console.WriteLine($"  Storage/               {Mb(DirSize(Terraria.Storage.AppPaths.Root))}");
		Console.WriteLine();
	}

	private static long DirSize(string path, string pattern = "*")
	{
		if (!Directory.Exists(path))
		{
			return 0L;
		}
		long num = 0L;
		string[] files = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
		foreach (string path2 in files)
		{
			try
			{
				num += new FileInfo(path2).Length;
			}
			catch
			{
			}
		}
		return num;
	}

	private static string Mb(long bytes)
	{
		return (bytes / 1048576.0).ToString("0.0") + " MB";
	}

	/// <summary>
	/// The loading bar must actually reach 100%. A pure lerp never does, and "stuck at 99%"
	/// is the symptom that was reported. Also checks that the sequence is monotonic, so the
	/// bar never jumps backwards while stages advance.
	/// </summary>
	private static bool CheckLoadingProgress()
	{
		float[] array = new float[10]
		{
			0f, 0.18f, 0.3f, 0.42f, 0.55f, 0.68f, 0.76f, 0.88f, 0.96f, 1f
		};
		float num = 0f;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			// Each stage is visible for 30 frames before the next one starts.
			for (int j = 0; j < 30; j++)
			{
				float num4 = LoadingProgress.Step(num, array[i]);
				if (num4 < num - 0.0001f)
				{
					return Fail($"progress went backwards: {num:0.000} -> {num4:0.000}");
				}
				if (num4 > 1f + 0.0001f)
				{
					return Fail("progress exceeded 1.0");
				}
				num = num4;
				num2++;
			}
			if (i == array.Length - 1)
			{
				// The final stage must settle exactly on 100% within a couple of seconds.
				while (num < 1f)
				{
					num = LoadingProgress.Step(num, 1f);
					num3++;
					if (num3 > 180)
					{
						return Fail($"bar never reached 100% (stuck at {num:0.000})");
					}
				}
			}
		}
		if (num != 1f)
		{
			return Fail("final progress is not exactly 1.0: " + num.ToString("0.0000"));
		}
		if (num2 == 0)
		{
			return Fail("no frames simulated");
		}
		return true;
	}

	// --- Input --------------------------------------------------------------

	/// <summary>
	/// Verifies the keyboard/mouse to gamepad mapping. These are the exact rules that broke
	/// silently before: right button must place blocks (not grapple), left button must attack,
	/// Escape must cancel in menus, and typing must not move the player.
	/// </summary>
	private static bool CheckInputMapping()
	{
		MouseState[] array = new MouseState[3];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new MouseState(0, 0, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
		}
		MouseState leftDown = new MouseState(0, 0, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
		MouseState rightDown = new MouseState(0, 0, 0, ButtonState.Released, ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Released);
		MouseState middleDown = new MouseState(0, 0, 0, ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released);
		KeyboardState empty = new KeyboardState(new Keys[0]);
		// Gameplay: left button attacks.
		GamePadState gamePadState = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(leftDown, array[0], empty, false, false);
		if (!gamePadState.IsButtonDown(Buttons.RightTrigger))
		{
			return Fail("left mouse button does not map to 'use item'");
		}
		if (gamePadState.IsButtonDown(Buttons.B))
		{
			return Fail("left mouse button also maps to tile use");
		}
		// Gameplay: right button places blocks.
		GamePadState gamePadState2 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(rightDown, array[0], empty, false, false);
		if (!gamePadState2.IsButtonDown(Buttons.B))
		{
			return Fail("right mouse button does not map to tile use");
		}
		if (gamePadState2.IsButtonDown(Buttons.LeftTrigger))
		{
			return Fail("right mouse button still grapples");
		}
		// Gameplay: middle button grapples.
		GamePadState gamePadState3 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(middleDown, array[0], empty, false, false);
		if (!gamePadState3.IsButtonDown(Buttons.LeftTrigger))
		{
			return Fail("middle mouse button does not grapple");
		}
		// Menu: left click confirms, right click cancels.
		Terraria.Input.KeyboardMouseInput.MouseHandledByScreen = false;
		GamePadState gamePadState4 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(leftDown, array[0], empty, true, false);
		if (!gamePadState4.IsButtonDown(Buttons.A))
		{
			return Fail("left click does not confirm in menus");
		}
		GamePadState gamePadState5 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(rightDown, array[0], empty, true, false);
		if (!gamePadState5.IsButtonDown(Buttons.B))
		{
			return Fail("right click does not cancel in menus");
		}
		// Menu: a screen with its own hit-testing must not also fire "confirm".
		Terraria.Input.KeyboardMouseInput.MouseHandledByScreen = true;
		GamePadState gamePadState6 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(leftDown, array[0], empty, true, false);
		if (gamePadState6.IsButtonDown(Buttons.A))
		{
			return Fail("click double-fires on screens that handle the mouse");
		}
		Terraria.Input.KeyboardMouseInput.MouseHandledByScreen = false;
		// NPC / sign dialog: it is a menu that does its own hit-testing, so a click in it must
		// reach neither "use item" nor the world. Before this, a click on "Shop" swung the
		// pickaxe: the dialog lives in gameplay, so the left button became RightTrigger and
		// Buttons.A was never synthesised at all.
		Terraria.Input.KeyboardMouseInput.MouseHandledByScreen = true;
		GamePadState dialogClick = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(leftDown, array[0], empty, true, false);
		if (dialogClick.IsButtonDown(Buttons.A) || dialogClick.IsButtonDown(Buttons.RightTrigger))
		{
			return Fail("a click in the NPC dialog also fires an action in the world");
		}
		Terraria.Input.KeyboardMouseInput.MouseHandledByScreen = false;
		// The dialog has to be recognised as one. ChatOpen is what the input layer reads, so it
		// must be true with no chat text and no sign only when there is no player either.
		UI dialogUi = new UI();
		if (dialogUi.ChatOpen)
		{
			return Fail("ChatOpen is true with no chat and no player");
		}
		dialogUi.npcChatText = new UserString("hello", NowVerified: true);
		if (!dialogUi.ChatOpen)
		{
			return Fail("ChatOpen does not see an open NPC dialog");
		}
		dialogUi.npcChatText = null;
		if (dialogUi.ChatOpen)
		{
			return Fail("ChatOpen stays true after the dialog closed");
		}
		// Inventory: right click splits a stack instead of cancelling.
		GamePadState inventoryRight = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(rightDown, array[0], empty, true, false, true, false, true);
		if (!inventoryRight.IsButtonDown(Buttons.RightTrigger))
		{
			return Fail("right click does not split stacks in the inventory");
		}
		if (inventoryRight.IsButtonDown(Buttons.B))
		{
			return Fail("right click cancels the inventory instead of splitting");
		}
		// Q is Y inside the inventory: "the second action on this screen", which had no keyboard
		// binding at all - hide/show residents in housing, ingredients in crafting. Both screens
		// are reachable with a keyboard, so that one action was gamepad-only.
		KeyboardState q = new KeyboardState(new Keys[1] { Keys.Q });
		GamePadState inventoryY = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], q, true, false, true, false, true);
		if (!inventoryY.IsButtonDown(Buttons.Y))
		{
			return Fail("Q does not act as Y in the inventory");
		}
		// Q is also the default throw key, which is X. In the inventory it must mean one thing:
		// both at once would toggle "show all" and "ingredients" with one press.
		if (inventoryY.IsButtonDown(Buttons.X))
		{
			return Fail("Q acts as X and Y at the same time in the inventory");
		}
		// ... and it must not leak into gameplay, where Q is nothing and Y is the inventory.
		GamePadState gameplayQ = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], q, false, false);
		if (gameplayQ.IsButtonDown(Buttons.Y))
		{
			return Fail("Q acts as Y during gameplay");
		}
		// Outside the inventory Q is still the throw key, as on the PC version.
		if (!gameplayQ.IsButtonDown(Buttons.X))
		{
			return Fail("Q no longer throws outside the inventory");
		}
		// ... but in a menu (not the inventory) it still cancels.
		GamePadState menuRight = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(rightDown, array[0], empty, true, false, true, false, false);
		if (!menuRight.IsButtonDown(Buttons.B))
		{
			return Fail("right click no longer cancels in menus");
		}
		// Escape: cancel in menus, pause in game.
		KeyboardState escape = new KeyboardState(new Keys[1] { Keys.Escape });
		GamePadState gamePadState7 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], escape, true, false);
		if (!gamePadState7.IsButtonDown(Buttons.Back) || gamePadState7.IsButtonDown(Buttons.Start))
		{
			return Fail("Escape does not cancel in menus");
		}
		GamePadState gamePadState8 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], escape, false, false);
		if (!gamePadState8.IsButtonDown(Buttons.Start))
		{
			return Fail("Escape does not pause during play");
		}
		// Enter: confirm in menus, but never "jump" during play (there it opens chat).
		KeyboardState enter = new KeyboardState(new Keys[1] { Keys.Enter });
		GamePadState gamePadState11 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], enter, true, false);
		if (!gamePadState11.IsButtonDown(Buttons.A))
		{
			return Fail("Enter does not confirm in menus");
		}
		GamePadState gamePadState12 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], enter, false, false);
		if (gamePadState12.IsButtonDown(Buttons.A))
		{
			return Fail("Enter jumps during play instead of opening chat");
		}
		// Character creation confirms with Start (A cycles categories), so Enter must map to
		// Start there - otherwise the character can never be created from a keyboard.
		GamePadState gamePadState13 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], enter, true, false, true, true);
		if (!gamePadState13.IsButtonDown(Buttons.Start))
		{
			return Fail("Enter does not confirm on the character creation screen");
		}
		if (gamePadState13.IsButtonDown(Buttons.A))
		{
			return Fail("Enter cycles categories instead of confirming in character creation");
		}
		// The inventory key is a toggle request, not a Y press. Y means "open on release" during
		// play, "switch the crafting section" inside the inventory and "show NPCs" in the housing
		// tab - so mapping E onto Y made one keypress do two things.
		KeyboardState inventoryKey = new KeyboardState(new Keys[1] { Keys.E });
		GamePadState inGameInventory = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], inventoryKey, false, false);
		if (inGameInventory.IsButtonDown(Buttons.Y))
		{
			return Fail("the inventory key still presses Y during play (one press, two actions)");
		}
		GamePadState menuInventory = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], inventoryKey, true, false);
		if (menuInventory.IsButtonDown(Buttons.Y))
		{
			return Fail("the inventory key presses Y in menus (Y opens the party overlay there)");
		}
		// Character creation does need Y: that is "randomize", and it is the only way to reach it
		// from a keyboard.
		GamePadState creatorInventory = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], inventoryKey, true, false, true, true);
		if (!creatorInventory.IsButtonDown(Buttons.Y))
		{
			return Fail("the inventory key does not randomize in character creation");
		}
		// Typing must not feed the player.
		KeyboardState movement = new KeyboardState(new Keys[2]
		{
			Keys.W,
			Keys.D
		});
		GamePadState gamePadState9 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], movement, false, true);
		if (gamePadState9.ThumbSticks.Left.LengthSquared() > 0.001f || gamePadState9.Buttons.buttons != 0)
		{
			return Fail("movement is not suppressed while typing");
		}
		// ...but works normally otherwise.
		GamePadState gamePadState10 = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(array[0], array[0], movement, false, false);
		if (gamePadState10.ThumbSticks.Left.LengthSquared() < 0.5f)
		{
			return Fail("WASD does not move the player");
		}
		// The swallow: a button that was already down when the game became interested in it must
		// wait for a release. Two cases produce it - the click that refocuses the window, and the
		// click that closed a menu - and in both the player pressed the button for a reason that
		// has nothing to do with the world. Without this, alt-tabbing back into the game swings
		// the sword, and picking "Resume" keeps mining until the button is let go.
		bool swallowLeft;
		bool swallowRight;
		bool swallowMiddle;
		Terraria.Input.KeyboardMouseInput.EffectiveMouseButtons(leftDown, true, out swallowLeft, out swallowRight, out swallowMiddle);
		if (swallowLeft || swallowRight || swallowMiddle)
		{
			return Fail("a swallowed click still reaches the game");
		}
		// Gameplay must not see the held button...
		GamePadState swallowed = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(leftDown, leftDown, empty, false, false, true, false, false, true);
		if (swallowed.IsButtonDown(Buttons.RightTrigger))
		{
			return Fail("a swallowed click still uses the item");
		}
		// ...and the grapple on the middle button is swallowed too: it used to read the raw
		// button, so the one button that bypassed the swallow was the one that throws a hook.
		Terraria.Input.KeyboardMouseInput.EffectiveMouseButtons(middleDown, true, out swallowLeft, out swallowRight, out swallowMiddle);
		if (swallowMiddle)
		{
			return Fail("a swallowed middle click still grapples");
		}
		GamePadState swallowedMiddle = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(middleDown, middleDown, empty, false, false, true, false, false, true);
		if (swallowedMiddle.IsButtonDown(Buttons.LeftTrigger))
		{
			return Fail("a swallowed middle click still grapples");
		}
		// The swallow ends with the release, not with a frame count: a slow click is still one
		// click, and the frame after the release must be live again.
		bool released = !Terraria.Input.KeyboardMouseInput.AnyMouseButtonDown(array[0]);
		if (!released)
		{
			return Fail("an all-up mouse state reads as held");
		}
		if (!Terraria.Input.KeyboardMouseInput.AnyMouseButtonDown(leftDown))
		{
			return Fail("a held left button reads as up");
		}
		GamePadState afterRelease = Terraria.Input.KeyboardMouseInput.BuildSyntheticState(leftDown, array[0], empty, false, false, true, false, false, false);
		if (!afterRelease.IsButtonDown(Buttons.RightTrigger))
		{
			return Fail("the click after the swallow does not use the item");
		}
		return true;
	}

	// --- Settings -----------------------------------------------------------

	/// <summary>
	/// Keys must be identified by section AND name. Matching on the name alone made
	/// "Enabled" in [Cheats] and [Mods] collapse into a single entry, which silently deleted
	/// the [Cheats] section - so cheats were disabled no matter what the file said.
	/// </summary>
	private static bool CheckSettingsSections()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-settings.ini");
		try
		{
			if (System.IO.File.Exists(text))
			{
				System.IO.File.Delete(text);
			}
			Settings settings = new Settings(text);
			settings.Set("Cheats", "Enabled", "True", "cheats");
			settings.Set("Mods", "Enabled", "False", "mods");
			settings.Set("Mods", "RequireMatchingMods", "True", "match");
			settings.Close();
			string text2 = System.IO.File.ReadAllText(text);
			if (!text2.Contains("[Cheats]"))
			{
				return Fail("[Cheats] section was dropped when writing settings");
			}
			Settings settings2 = new Settings(text);
			string text3 = settings2.Get("Cheats", "Enabled");
			string text4 = settings2.Get("Mods", "Enabled");
			settings2.Close();
			if (text3 != "True")
			{
				return Fail("Cheats/Enabled lost its value (got '" + text3 + "')");
			}
			if (text4 != "False")
			{
				return Fail("Mods/Enabled lost its value (got '" + text4 + "')");
			}
			System.IO.File.Delete(text);
			return true;
		}
		catch (Exception ex)
		{
			return Fail("settings round-trip failed: " + ex.Message);
		}
		finally
		{
			try
			{
				if (System.IO.File.Exists(text))
				{
					System.IO.File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}

	// --- Network stats ------------------------------------------------------

	/// <summary>
	/// Latency readout drives what the player sees, so its edge cases matter: before any
	/// measurement it must report "unknown" rather than 0 ms, and counters must reset with
	/// the session.
	/// </summary>
	private static bool CheckNetworkStats()
	{
		NetworkStats.Reset();
		if (NetworkStats.RttMs != -1)
		{
			return Fail("ping should be unknown before the first measurement");
		}
		if (NetworkStats.BytesSent != 0L || NetworkStats.BytesReceived != 0L)
		{
			return Fail("counters were not cleared by Reset");
		}
		NetworkStats.OnSent(100);
		NetworkStats.OnReceived(250);
		if (NetworkStats.BytesSent != 100L || NetworkStats.PacketsSent != 1L)
		{
			return Fail("outgoing counters are wrong");
		}
		if (NetworkStats.BytesReceived != 250L || NetworkStats.PacketsReceived != 1L)
		{
			return Fail("incoming counters are wrong");
		}
		NetworkStats.SetRtt(40);
		if (NetworkStats.RttMs != 40)
		{
			return Fail("first RTT sample was not taken as-is");
		}
		// Smoothing must move towards the new sample, not jump to it.
		NetworkStats.SetRtt(200);
		int num = NetworkStats.RttMs;
		if (num <= 40 || num >= 200)
		{
			return Fail("RTT smoothing did not average the sample (got " + num + ")");
		}
		string text = NetworkStats.Describe();
		if (!text.Contains("ping") || !text.Contains("kbps"))
		{
			return Fail("Describe() does not mention ping and throughput");
		}
		NetworkStats.Reset();
		if (NetworkStats.RttMs != -1)
		{
			return Fail("Reset did not clear the RTT");
		}
		return true;
	}

	// --- Key bindings -------------------------------------------------------

	/// <summary>
	/// Rebindable keys must round-trip through the config, and must never allow two actions
	/// to share a key (that silently disables one of them).
	/// </summary>
	private static bool CheckKeyBindings()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-keys.ini");
		try
		{
			Terraria.Input.KeyBindings.ResetToDefaults();
			if (Terraria.Input.KeyBindings.Get(Terraria.Input.KeyBindings.Action.MoveLeft) != Keys.A)
			{
				return Fail("default MoveLeft is not A");
			}
			if (Terraria.Input.KeyBindings.Get(Terraria.Input.KeyBindings.Action.Jump) != Keys.Space)
			{
				return Fail("default Jump is not Space");
			}
			// Duplicates must be refused.
			if (Terraria.Input.KeyBindings.TrySet(Terraria.Input.KeyBindings.Action.Heal, Keys.A))
			{
				return Fail("binding a key that is already used was allowed");
			}
			if (!Terraria.Input.KeyBindings.TrySet(Terraria.Input.KeyBindings.Action.Heal, Keys.F5))
			{
				return Fail("free key was refused");
			}
			if (Terraria.Input.KeyBindings.Get(Terraria.Input.KeyBindings.Action.Heal) != Keys.F5)
			{
				return Fail("rebind did not take effect");
			}
			if (Terraria.Input.KeyBindings.TrySet(Terraria.Input.KeyBindings.Action.Heal, Keys.None))
			{
				return Fail("Keys.None was accepted as a binding");
			}
			// Persist and reload.
			if (System.IO.File.Exists(text))
			{
				System.IO.File.Delete(text);
			}
			Settings settings = new Settings(text);
			Terraria.Input.KeyBindings.Save(settings);
			settings.Close();
			Terraria.Input.KeyBindings.ResetToDefaults();
			Settings settings2 = new Settings(text);
			Terraria.Input.KeyBindings.Load(settings2);
			settings2.Close();
			if (Terraria.Input.KeyBindings.Get(Terraria.Input.KeyBindings.Action.Heal) != Keys.F5)
			{
				return Fail("binding was not restored from config");
			}
			if (Terraria.Input.KeyBindings.Get(Terraria.Input.KeyBindings.Action.MoveLeft) != Keys.A)
			{
				return Fail("default binding was lost on reload");
			}
			// Reset restores everything.
			Terraria.Input.KeyBindings.ResetToDefaults();
			if (Terraria.Input.KeyBindings.Get(Terraria.Input.KeyBindings.Action.Heal) != Keys.H)
			{
				return Fail("reset did not restore the default key");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("key binding round-trip failed: " + ex.Message);
		}
		finally
		{
			try
			{
				if (System.IO.File.Exists(text))
				{
					System.IO.File.Delete(text);
				}
			}
			catch
			{
			}
			Terraria.Input.KeyBindings.ResetToDefaults();
		}
	}

	// --- Cheat badge --------------------------------------------------------

	/// <summary>
	/// The HUD badge must appear exactly when a cheat is on, and must say so honestly when
	/// the game is in multiplayer (where the toggles are ignored).
	/// </summary>
	private static bool CheckCheatBadge()
	{
		int netMode = Main.NetMode;
		try
		{
			Main.NetMode = 0;
			Cheats.CheatState.Reset();
			if (Cheats.CheatState.ActiveLabel != null)
			{
				return Fail("badge shown with no cheats active");
			}
			Cheats.CheatState.GodMode = true;
			if (Cheats.CheatState.ActiveLabel != "CHEATS ACTIVE")
			{
				return Fail("badge not shown while a cheat is on (" + Cheats.CheatState.ActiveLabel + ")");
			}
			Main.NetMode = 1;
			string text = Cheats.CheatState.ActiveLabel;
			if (text == null || !text.Contains("inactive"))
			{
				return Fail("badge does not warn that cheats are inactive online");
			}
			Cheats.CheatState.Reset();
			if (Cheats.CheatState.ActiveLabel != null)
			{
				return Fail("badge still shown after reset");
			}
			return true;
		}
		finally
		{
			Main.NetMode = netMode;
			Cheats.CheatState.Reset();
		}
	}

	// --- Mod load order -----------------------------------------------------

	/// <summary>
	/// Load order decides which mod wins when two touch the same item, so it has to be
	/// stable, explicit, and safe when the file is incomplete or full of junk.
	/// </summary>
	private static bool CheckModLoadOrder()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-mods");
		try
		{
			if (System.IO.Directory.Exists(text))
			{
				System.IO.Directory.Delete(text, recursive: true);
			}
			string[] array = new string[3]
			{
				System.IO.Path.Combine(text, "Alpha"),
				System.IO.Path.Combine(text, "Beta"),
				System.IO.Path.Combine(text, "Gamma")
			};
			for (int i = 0; i < array.Length; i++)
			{
				System.IO.Directory.CreateDirectory(array[i]);
			}
			// No file: alphabetical, so a fresh install is predictable.
			string[] array2 = Terraria.Mods.ModLoader.ApplyLoadOrder((string[])array.Clone(), text);
			if (array2.Length != 3 || System.IO.Path.GetFileName(array2[0]) != "Alpha" || System.IO.Path.GetFileName(array2[2]) != "Gamma")
			{
				return Fail("the default mod order is not alphabetical");
			}
			// Explicit order wins, and folders missing from the file follow alphabetically.
			System.IO.File.WriteAllText(System.IO.Path.Combine(text, Terraria.Mods.ModLoader.LoadOrderFile), "# comment\r\nGamma\r\n\r\nAlpha\r\n");
			array2 = Terraria.Mods.ModLoader.ApplyLoadOrder((string[])array.Clone(), text);
			if (System.IO.Path.GetFileName(array2[0]) != "Gamma" || System.IO.Path.GetFileName(array2[1]) != "Alpha" || System.IO.Path.GetFileName(array2[2]) != "Beta")
			{
				return Fail("the explicit mod order was not applied: " + string.Join(", ", Array.ConvertAll(array2, System.IO.Path.GetFileName)));
			}
			// Writing and re-reading the same order must be a no-op.
			List<string> list = new List<string>(3);
			list.Add("Beta");
			list.Add("Gamma");
			list.Add("Alpha");
			if (!Terraria.Mods.ModLoader.WriteLoadOrder(text, list))
			{
				return Fail("the load order could not be written");
			}
			array2 = Terraria.Mods.ModLoader.ApplyLoadOrder((string[])array.Clone(), text);
			if (System.IO.Path.GetFileName(array2[0]) != "Beta" || System.IO.Path.GetFileName(array2[1]) != "Gamma" || System.IO.Path.GetFileName(array2[2]) != "Alpha")
			{
				return Fail("a written load order did not survive a round-trip");
			}
			// A file naming folders that do not exist must be ignored, not fatal.
			System.IO.File.WriteAllText(System.IO.Path.Combine(text, Terraria.Mods.ModLoader.LoadOrderFile), "Ghost\r\nBeta\r\n");
			array2 = Terraria.Mods.ModLoader.ApplyLoadOrder((string[])array.Clone(), text);
			if (array2.Length != 3 || System.IO.Path.GetFileName(array2[0]) != "Beta")
			{
				return Fail("a stale load order broke the mod list");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod load order check threw: " + ex.Message);
		}
		finally
		{
			try
			{
				if (System.IO.Directory.Exists(text))
				{
					System.IO.Directory.Delete(text, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	// --- tConfig import -----------------------------------------------------

	/// <summary>
	/// Imports a tiny synthetic tConfig pack and checks that a known field lands on the item
	/// and that unknown fields are reported instead of silently dropped.
	/// </summary>
	private static bool CheckTConfigImport()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-tconfig");
		try
		{
			if (System.IO.Directory.Exists(text))
			{
				System.IO.Directory.Delete(text, recursive: true);
			}
			string text2 = System.IO.Path.Combine(text, "TestPack", "Item");
			System.IO.Directory.CreateDirectory(text2);
			System.IO.File.WriteAllText(System.IO.Path.Combine(text2, "Copper Shortsword.ini"), "; comment\r\n[Stats]\r\ndamage=12\r\nuseTime=17\r\nbogusField=1\r\n");
			Terraria.Mods.ItemOverrides.Clear();
			int num = Terraria.Mods.TConfigImporter.Import(text);
			if (num != 1)
			{
				return Fail("expected 1 imported item, got " + num);
			}
			if (Terraria.Mods.TConfigImporter.UnsupportedFields != 1)
			{
				return Fail("unknown field was not reported (count " + Terraria.Mods.TConfigImporter.UnsupportedFields + ")");
			}
			// Applying defaults must now pick up the override.
			Item item = default(Item);
			item.SetDefaults("Copper Shortsword");
			if (item.Damage != 12)
			{
				return Fail("override did not reach the item (damage " + item.Damage + ")");
			}
			if (item.UseTime != 17)
			{
				return Fail("second override did not apply (useTime " + item.UseTime + ")");
			}
			// A different item must be untouched.
			Item other = default(Item);
			other.SetDefaults("Copper Pickaxe");
			if (other.Damage == 12 && other.Type != item.Type)
			{
				return Fail("override leaked onto another item");
			}
			// Unknown item names are skipped, not fatal.
			System.IO.File.WriteAllText(System.IO.Path.Combine(text2, "Not A Real Item.ini"), "damage=5\r\n");
			Terraria.Mods.ItemOverrides.Clear();
			Terraria.Mods.TConfigImporter.Import(text);
			if (Terraria.Mods.TConfigImporter.SkippedItems < 1)
			{
				return Fail("unknown item name was not skipped");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("tConfig import failed: " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ItemOverrides.Clear();
			try
			{
				if (System.IO.Directory.Exists(text))
				{
					System.IO.Directory.Delete(text, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	// --- Virtual keyboard ---------------------------------------------------

	/// <summary>
	/// The tConfig converter: a pack becomes a project that builds.
	///
	/// Three things are worth checking and they are not the same thing: that the conversion
	/// reads the pack the way the runtime importer does, that the generated source names
	/// properties that actually exist on <see cref="Terraria.Mods.ItemOverride"/>, and that the
	/// mod API itself reaches the item. The middle one is the quiet failure mode: a renamed
	/// property produces a project that does not compile, and nobody notices until they try.
	/// </summary>
	private static bool CheckTConfigConvert()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-tconfig-convert");
		string text2 = System.IO.Path.Combine(text, "TestPack", "Item");
		string text3 = System.IO.Path.Combine(text, "out");
		string savedLog = Terraria.Mods.TConfigConverter.LogPathOverride;
		try
		{
			if (System.IO.Directory.Exists(text))
			{
				System.IO.Directory.Delete(text, recursive: true);
			}
			System.IO.Directory.CreateDirectory(text2);
			// The conversion writes a log; a self-test must not append to the player's own.
			Terraria.Mods.TConfigConverter.LogPathOverride = System.IO.Path.Combine(text, "convert.log");
			System.IO.File.WriteAllText(System.IO.Path.Combine(text2, "Copper Shortsword.ini"), "damage=12\r\nuseTime=17\r\nautoReuse=True\r\nbogusField=1\r\n");
			System.IO.File.WriteAllText(System.IO.Path.Combine(text2, "Copper Pickaxe.ini"), "pick=45\r\n");
			System.IO.File.WriteAllText(System.IO.Path.Combine(text2, "Not A Real Item.ini"), "damage=5\r\n");
			// The newer recipe spelling: one line per list, counts before names. It used to be
			// skipped as "not an item field", so those items converted into a mod that could
			// never craft them.
			System.IO.File.WriteAllText(System.IO.Path.Combine(text2, "Copper Bar.ini"),
				"[Recipe]\r\nItems=3 Copper Ore, 1 Wood\r\nTiles=Work Bench\r\nAmount=2\r\n");
			string error;
			Terraria.Mods.TConfigConverter.Result result = Terraria.Mods.TConfigConverter.Convert(
				System.IO.Path.Combine(text, "TestPack"), text3, null, out error);
			if (result == null)
			{
				return Fail("conversion refused a valid pack: " + error);
			}
			if (result.Items != 2 || result.Fields != 4)
			{
				return Fail("expected 2 items and 4 fields, got " + result.Items + " and " + result.Fields);
			}
			if (result.UnknownItems != 1 || result.UnsupportedFields != 1)
			{
				return Fail("expected 1 unknown item and 1 dropped field, got "
					+ result.UnknownItems + " and " + result.UnsupportedFields);
			}
			string text4 = System.IO.File.ReadAllText(result.SourceFile);
			if (!text4.Contains("ModInfo(\"TestPack\""))
			{
				return Fail("the generated mod does not declare its name");
			}
			if (!text4.Contains("Copper Shortsword") || !text4.Contains(".Damage = 12;")
				|| !text4.Contains(".UseTime = 17;") || !text4.Contains(".AutoReuse = true;"))
			{
				return Fail("the generated source does not carry the pack's values");
			}
			if (!text4.Contains(".Pick = 45;"))
			{
				return Fail("the generated source dropped the second item");
			}
			// The [Recipe] style: ingredients with counts, the station, the output count.
			if (result.Recipes != 1)
			{
				return Fail("expected 1 converted recipe, got " + result.Recipes);
			}
			if (!text4.Contains("Context.AddRecipe(\"Copper Bar\")")
				|| !text4.Contains(".AddIngredient(\"Copper Ore\", 3);")
				|| !text4.Contains(".AddIngredient(\"Wood\", 1);")
				|| !text4.Contains(".AddStation(\"Work Bench\");")
				|| !text4.Contains(".Amount = 2;"))
			{
				return Fail("the [Recipe] style was not converted into a recipe");
			}
			string text5 = System.IO.File.ReadAllText(result.ProjectFile);
			if (!text5.Contains("<TargetFramework>net40</TargetFramework>") || !text5.Contains("TerrariaXDK"))
			{
				return Fail("the generated project does not target net40 against the game");
			}
			string text6 = System.IO.File.ReadAllText(System.IO.Path.Combine(text3, "README.md"));
			if (!text6.Contains("bogusField") || !text6.Contains("Not A Real Item"))
			{
				return Fail("the README does not say what was dropped");
			}
			// The pack's NPC folder is not converted and must be named in the README: "111 items,
			// done" without it reads as if the NPCs came along.
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(text, "TestPack", "NPC"));
			System.IO.File.WriteAllText(System.IO.Path.Combine(text, "TestPack", "NPC", "Slime.ini"), "damage=1\r\n");
			Terraria.Mods.TConfigConverter.Result withNpc = Terraria.Mods.TConfigConverter.Convert(
				System.IO.Path.Combine(text, "TestPack"), System.IO.Path.Combine(text, "out2"), null, out error);
			if (withNpc == null)
			{
				return Fail("conversion refused a pack that also has NPCs: " + error);
			}
			if (withNpc.Ignored.Count != 1 || withNpc.Ignored[0].Kind != "NPC" || withNpc.Ignored[0].Files != 1)
			{
				return Fail("the NPC folder was not counted by the converter");
			}
			string readme = System.IO.File.ReadAllText(System.IO.Path.Combine(text, "out2", "README.md"));
			if (readme.IndexOf("cannot load", StringComparison.Ordinal) < 0 || readme.IndexOf("NPC", StringComparison.Ordinal) < 0)
			{
				return Fail("the README does not say that the NPC folder was not converted");
			}
			// Every field the importer accepts must have a property on ItemOverride, or the
			// generated project will not compile.
			string[] array = new string[44]
			{
				"damage", "defense", "pick", "axe", "hammer", "useAnimation", "useTime", "useSound", "shoot", "shootSpeed",
				"knockBack", "mana", "healLife", "healMana", "buffType", "buffTime", "rare", "value", "maxStack", "autoReuse",
				"melee", "magic", "ranged", "consumable", "potion", "accessory", "noMelee", "crit",
				"width", "height", "useStyle", "holdStyle", "scale", "isMaterial", "material", "ammo", "useAmmo",
				"useTurn", "toolTip", "toolTip2", "createTileName",
				// tConfig spellings of a field above, plus the one field with no property at all.
				"stack", "color", "noUseGraphic"
			};
			for (int i = 0; i < array.Length; i++)
			{
				if (!Terraria.Mods.ItemOverrides.IsSupportedField(array[i]))
				{
					return Fail("the supported field list lost '" + array[i] + "'");
				}
				string text7 = Terraria.Mods.TConfigConverter.PropertyName(array[i]);
				if (array[i] == "consumable" || array[i] == "createTileName")
				{
					// Accepted by the importer but deliberately absent from the mod API: the first
					// is implied by stack/heal values, the second needs mod tiles. The converter
					// must drop them rather than emit a property that does not exist.
					if (text7 != null)
					{
						return Fail("the converter claims '" + array[i] + "' has a property");
					}
					continue;
				}
				if (Terraria.Mods.TConfigConverter.IsEscapedField(array[i]))
				{
					// No property, applied through ItemOverride.Set: the project must emit the
					// escape hatch instead of a property that does not exist.
					if (text7 != null)
					{
						return Fail("'" + array[i] + "' is both a property and an escaped field");
					}
					continue;
				}
				if (text7 == null)
				{
					return Fail("field '" + array[i] + "' has no property name");
				}
				System.Reflection.PropertyInfo property = typeof(Terraria.Mods.ItemOverride).GetProperty(text7);
				if (property == null || property.GetSetMethod() == null)
				{
					return Fail("ItemOverride has no settable '" + text7 + "' for field '" + array[i] + "'");
				}
				// The same check for the new-item API: a converted pack emits ModItem.Property, so a
				// mismatch there produces a project that does not compile. That is exactly how
				// "CanUseTurn" was found - by a real pack failing to build.
				System.Reflection.PropertyInfo modProperty = typeof(Terraria.Mods.ModItem).GetProperty(text7);
				if (modProperty == null || modProperty.GetSetMethod() == null)
				{
					return Fail("ModItem has no settable '" + text7 + "' for field '" + array[i] + "'");
				}
			}
			// And the API itself: an override set through a mod context must reach the item.
			Terraria.Mods.ItemOverrides.Clear();
			Terraria.Mods.ModContext modContext = new Terraria.Mods.ModContext("Test", text3);
			Terraria.Mods.ItemOverride itemOverride = modContext.Item("Copper Shortsword");
			if (itemOverride == null)
			{
				return Fail("the mod API did not resolve an item by name");
			}
			itemOverride.Damage = 33;
			itemOverride.ShootSpeed = 7.5f;
			itemOverride.Melee = false;
			Item item = default(Item);
			item.SetDefaults("Copper Shortsword");
			if (item.Damage != 33)
			{
				return Fail("the mod API did not reach the item (damage " + item.Damage + ")");
			}
			if (item.ShootSpeed < 7.49f || item.ShootSpeed > 7.51f)
			{
				return Fail("a float override did not apply (shootSpeed " + item.ShootSpeed + ")");
			}
			if (item.IsMelee)
			{
				return Fail("a bool override did not apply");
			}
			if (modContext.Item("Not A Real Item") != null)
			{
				return Fail("the mod API resolved an item that does not exist");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("tConfig conversion failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ItemOverrides.Clear();
			Terraria.Mods.TConfigConverter.LogPathOverride = savedLog;
			try
			{
				if (System.IO.Directory.Exists(text))
				{
					System.IO.Directory.Delete(text, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	// --- tConfig converter resilience ---------------------------------------

	/// <summary>
	/// The converter survives bad input.
	///
	/// A tConfig pack is somebody else's data: a truncated INI, a file an editor has locked, a
	/// value that is not a number, an item name this build does not have. Every one of those is
	/// something to report, not a reason to refuse the pack - and the report has to reach the
	/// player, because "conversion failed" with no detail leaves them with nothing to fix.
	///
	/// So the checks are: a locked file does not stop the rest, a bad value is dropped instead of
	/// producing a project that will not compile, and both end up in a log file and in the
	/// generated README.
	/// </summary>
	private static bool CheckTConfigResilience()
	{
		string root = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-tconfig-bad");
		string items = System.IO.Path.Combine(root, "BadPack", "Item");
		string output = System.IO.Path.Combine(root, "out");
		string logPath = System.IO.Path.Combine(root, "convert.log");
		string savedLog = Terraria.Mods.TConfigConverter.LogPathOverride;
		System.IO.FileStream locked = null;
		try
		{
			if (System.IO.Directory.Exists(root))
			{
				System.IO.Directory.Delete(root, recursive: true);
			}
			System.IO.Directory.CreateDirectory(items);
			// One good item, one with a value that is not a number, one that cannot be read at all.
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "Copper Shortsword.ini"), "damage=12\r\n");
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "Copper Pickaxe.ini"), "pick=abc\r\nuseTime=17\r\n");
			string lockedPath = System.IO.Path.Combine(items, "Copper Axe.ini");
			System.IO.File.WriteAllText(lockedPath, "axe=45\r\n");
			Terraria.Mods.TConfigConverter.LogPathOverride = logPath;

			// Hold the file open with no sharing: reading it must fail the way a locked file does
			// in the wild, rather than being simulated by a fake stream.
			locked = new System.IO.FileStream(lockedPath, System.IO.FileMode.Open, System.IO.FileAccess.Read,
				System.IO.FileShare.None);

			string error;
			Terraria.Mods.TConfigConverter.Result result = Terraria.Mods.TConfigConverter.Convert(
				System.IO.Path.Combine(root, "BadPack"), output, null, out error);
			if (result == null)
			{
				return Fail("one unreadable file refused the whole pack: " + error);
			}
			// The two readable items still made it.
			if (result.Items != 2)
			{
				return Fail("expected 2 converted item(s), got " + result.Items);
			}
			if (result.FailedFiles != 1)
			{
				return Fail("expected 1 failed file, got " + result.FailedFiles);
			}
			if (result.Errors.Count != 1)
			{
				return Fail("expected 1 reported error, got " + result.Errors.Count);
			}
			if (result.Errors[0].IndexOf("Copper Axe", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("the error does not name the file that failed: " + result.Errors[0]);
			}
			// The bad value was dropped with a reason, not turned into a project that cannot build.
			if (result.UnsupportedFields != 1)
			{
				return Fail("expected 1 dropped field, got " + result.UnsupportedFields);
			}
			bool explained = false;
			for (int i = 0; i < result.Warnings.Count; i++)
			{
				if (result.Warnings[i].IndexOf("pick", StringComparison.Ordinal) >= 0)
				{
					explained = true;
				}
			}
			if (!explained)
			{
				return Fail("the dropped value was not explained");
			}
			string source = System.IO.File.ReadAllText(result.SourceFile);
			if (source.IndexOf(".Pick", StringComparison.Ordinal) >= 0)
			{
				return Fail("a non-numeric value still reached the generated source");
			}
			if (source.IndexOf(".UseTime = 17;", StringComparison.Ordinal) < 0)
			{
				return Fail("a good field next to a bad one was lost");
			}
			// The report reaches the player three ways: the log, the README, and the Result.
			if (result.LogFile.Length == 0 || !System.IO.File.Exists(logPath))
			{
				return Fail("no conversion log was written");
			}
			string log = System.IO.File.ReadAllText(logPath);
			if (log.IndexOf("Copper Axe", StringComparison.OrdinalIgnoreCase) < 0
				|| log.IndexOf("pick", StringComparison.Ordinal) < 0)
			{
				return Fail("the log does not mention both problems");
			}
			string readme = System.IO.File.ReadAllText(System.IO.Path.Combine(output, "README.md"));
			if (readme.IndexOf("What failed", StringComparison.Ordinal) < 0)
			{
				return Fail("the README does not say what failed");
			}
			if (readme.IndexOf("Copper Axe", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("the README does not name the file that failed");
			}
			if (!result.HasProblems)
			{
				return Fail("a conversion with failures reported itself as clean");
			}
			// Releasing the lock and converting again must produce a clean result: the failure is
			// a property of the file, not of the pack.
			locked.Dispose();
			locked = null;
			Terraria.Mods.TConfigConverter.Result second = Terraria.Mods.TConfigConverter.Convert(
				System.IO.Path.Combine(root, "BadPack"), output, null, out error);
			if (second == null || second.Items != 3)
			{
				return Fail("the same pack failed after the lock was released: "
					+ (second == null ? error : second.Items + " item(s)"));
			}
			if (second.Errors.Count != 0 || second.FailedFiles != 0)
			{
				return Fail("a clean run still reported failures");
			}
			// A pack that does not exist must say so rather than throw, and must log it.
			Terraria.Mods.TConfigConverter.Result missing = Terraria.Mods.TConfigConverter.Convert(
				System.IO.Path.Combine(root, "NoSuchPack"), output, null, out error);
			if (missing != null || string.IsNullOrEmpty(error))
			{
				return Fail("a missing pack produced no reason");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("tConfig resilience check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			if (locked != null)
			{
				try
				{
					locked.Dispose();
				}
				catch
				{
				}
			}
			Terraria.Mods.TConfigConverter.LogPathOverride = savedLog;
			try
			{
				if (System.IO.Directory.Exists(root))
				{
					System.IO.Directory.Delete(root, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	// --- Mod recipes --------------------------------------------------------

	/// <summary>
	/// Recipes declared by mods: the result, the ingredients, the station, and the refusals.
	///
	/// This is what makes a converted pack playable: the item exists, and there is a way to make
	/// it. Three things are load-bearing. The recipe must land past the vanilla range (the array
	/// was a fixed 342 and every loop said so). The ingredient must be resolved by name through
	/// the real table - <c>SetDefaults(string)</c> knows only the two dozen legacy console items,
	/// so "Copper Bar" would silently resolve to nothing. And a recipe naming an item or station
	/// this build does not have must be refused, because a recipe that produces nothing is worse
	/// than a missing one.
	/// </summary>
	private static bool CheckModRecipes()
	{
		int savedRecipes = Recipe.NumRecipes;
		int savedVanilla342 = Main.ActiveRecipe.Length;
		try
		{
			Terraria.Mods.ModRecipes.Clear();
			// The ingredient is a real item whose name is NOT in SetDefaults(string) - that is the
			// case that used to fail.
			if (Terraria.ItemLookup.Find("Copper Bar") <= 0)
			{
				return Fail("the name lookup cannot resolve 'Copper Bar'");
			}
			if (Terraria.ItemLookup.Find("Copper Shortsword") <= 0)
			{
				return Fail("the name lookup cannot resolve a legacy console item");
			}
			string error;
			// --- a good recipe -----------------------------------------------
			Terraria.Mods.ModRecipe good = Terraria.Mods.ModRecipes.Register("TestMod", "Copper Shortsword", out error);
			if (good == null)
			{
				return Fail("a recipe for a real item was refused: " + error);
			}
			good.Amount = 2;
			good.Category = Recipe.Category.WEAPONS;
			if (!good.AddIngredient("Copper Bar", 6))
			{
				return Fail("adding a real ingredient failed");
			}
			if (!good.AddIngredient("Wood", 1))
			{
				return Fail("adding a second ingredient failed");
			}
			if (!good.AddStation("Anvil"))
			{
				return Fail("adding a known station failed");
			}
			// --- a recipe that cannot work ------------------------------------
			Terraria.Mods.ModRecipe badItem = Terraria.Mods.ModRecipes.Register("TestMod", "Copper Pickaxe", out error);
			badItem.AddIngredient("No Such Item", 1);
			Terraria.Mods.ModRecipe badStation = Terraria.Mods.ModRecipes.Register("TestMod", "Copper Axe", out error);
			badStation.AddIngredient("Copper Bar", 1);
			badStation.AddStation("Fabricator Of Doom");
			Terraria.Mods.ModRecipe noIngredients = Terraria.Mods.ModRecipes.Register("TestMod", "Copper Hammer", out error);

			int before = Recipe.NumRecipes;
			List<string> problems;
			int added = Terraria.Mods.ModRecipes.Apply(out problems);
			// Two recipes are usable: the good one, and the one whose station is a mod tile this
			// build does not have - that one keeps its ingredients and loses the station, because
			// an item with no recipe at all cannot be obtained.
			if (added != 2)
			{
				return Fail("expected 2 usable recipes, got " + added);
			}
			if (Recipe.NumRecipes != before + 2)
			{
				return Fail("the recipe count went from " + before + " to " + Recipe.NumRecipes);
			}
			if (problems.Count != 3)
			{
				return Fail("expected 3 reported problems, got " + problems.Count
					+ ": " + string.Join("; ", problems.ToArray()));
			}
			// The refusals must name the reason, or the author cannot fix them.
			bool namedItem = false;
			bool namedStation = false;
			bool namedEmpty = false;
			for (int i = 0; i < problems.Count; i++)
			{
				if (problems[i].IndexOf("No Such Item", StringComparison.Ordinal) >= 0)
				{
					namedItem = true;
				}
				if (problems[i].IndexOf("Fabricator Of Doom", StringComparison.Ordinal) >= 0)
				{
					namedStation = true;
				}
				if (problems[i].IndexOf("no ingredients", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					namedEmpty = true;
				}
			}
			if (!namedItem || !namedStation || !namedEmpty)
			{
				return Fail("a refusal did not say why: " + string.Join("; ", problems.ToArray()));
			}
			// The recipe with the mod-tile station exists and has no station requirement: the item
			// is obtainable, which is the point.
			bool foundStationless = false;
			for (int i = before; i < Recipe.NumRecipes; i++)
			{
				Recipe candidate = Main.ActiveRecipe[i];
				if (candidate.CraftedItem.Type == Terraria.ItemLookup.Find("Copper Axe"))
				{
					foundStationless = true;
					if (candidate.NumRequiredTiles != 0)
					{
						return Fail("a recipe with a mod-tile station still requires "
							+ candidate.NumRequiredTiles + " station(s)");
					}
				}
			}
			if (!foundStationless)
			{
				return Fail("the recipe whose station is a mod tile was not converted at all");
			}
			// --- what landed in the array -------------------------------------
			Recipe recipe = Main.ActiveRecipe[Recipe.NumRecipes - 1];
			if (recipe.CraftedItem.Type != Terraria.ItemLookup.Find("Copper Shortsword"))
			{
				return Fail("the recipe makes item type " + recipe.CraftedItem.Type);
			}
			if (recipe.CraftedItem.Stack != 2)
			{
				return Fail("the recipe produces " + recipe.CraftedItem.Stack + " instead of 2");
			}
			if (recipe.NumRequiredItems != 2)
			{
				return Fail("the recipe has " + recipe.NumRequiredItems + " ingredient(s)");
			}
			if (recipe.RequiredItem[0].Type != Terraria.ItemLookup.Find("Copper Bar")
				|| recipe.RequiredItem[0].Stack != 6)
			{
				return Fail("the first ingredient is wrong: type " + recipe.RequiredItem[0].Type
					+ " x" + recipe.RequiredItem[0].Stack);
			}
			if (recipe.NumRequiredTiles != 1 || recipe.RequiredTile[0] != 16)
			{
				return Fail("the anvil requirement did not land: " + recipe.NumRequiredTiles
					+ " tile(s), first " + recipe.RequiredTile[0]);
			}
			if (recipe.RecipeCategory != Recipe.Category.WEAPONS)
			{
				return Fail("the category did not land: " + recipe.RecipeCategory);
			}
			// The array must be able to hold it, and the vanilla range must be untouched.
			if (Main.ActiveRecipe.Length < Recipe.MaxNumRecipes + 1)
			{
				return Fail("the recipe array is only " + Main.ActiveRecipe.Length + " long");
			}
			// Every station name the game knows resolves; a typo does not.
			if (Terraria.Mods.ModRecipes.StationId("Work Bench") != 18)
			{
				return Fail("'Work Bench' does not resolve to tile 18");
			}
			if (Terraria.Mods.ModRecipes.StationId("Workbench") != 18)
			{
				return Fail("the 'Workbench' spelling does not resolve - packs write it both ways");
			}
			if (Terraria.Mods.ModRecipes.StationId("Fabricator Of Doom") >= 0)
			{
				return Fail("an unknown station resolved to a tile");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod recipes check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			// Put the recipe table back: later checks count recipes and the array must not grow
			// with every run.
			Recipe.NumRecipes = savedRecipes;
			Terraria.Mods.ModRecipes.Clear();
		}
	}

	// --- New items from mods ------------------------------------------------

	/// <summary>
	/// Items a mod creates: ids, names, stats, and what happens when the mod is gone.
	///
	/// Three things here are load-bearing. Ids must not depend on mod load order, because an item
	/// id travels on the wire and two players with the same mods would otherwise desync. The name
	/// must never be null, because it reaches <c>MeasureString</c> on the pickup path and a null
	/// there is the crash that made the custom-language option unusable. And an id whose mod is not
	/// loaded must become an empty slot, not an index past the end of every sprite table.
	/// </summary>
	private static bool CheckModItems()
	{
		string root = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-moditems");
		try
		{
			if (System.IO.Directory.Exists(root))
			{
				System.IO.Directory.Delete(root, recursive: true);
			}
			System.IO.Directory.CreateDirectory(root);
			// A real PNG: the registry checks that the file exists, and the sprite is required.
			string sprite = System.IO.Path.Combine(root, "sword.png");
			WriteTinyPng(sprite);

			Terraria.Mods.ModItems.Clear();
			string error;
			// --- refusals -----------------------------------------------------
			if (Terraria.Mods.ModItems.Register("ModA", "", "sword.png", root, out error) != null)
			{
				return Fail("an item with no name was accepted");
			}
			if (Terraria.Mods.ModItems.Register("ModA", "NoSprite", "", root, out error) != null)
			{
				return Fail("an item with no sprite was accepted");
			}
			if (Terraria.Mods.ModItems.Register("ModA", "MissingSprite", "nope.png", root, out error) != null)
			{
				return Fail("an item pointing at a missing sprite was accepted");
			}
			if (string.IsNullOrEmpty(error))
			{
				return Fail("a refused item gave no reason");
			}
			// --- registration -------------------------------------------------
			Terraria.Mods.ModItem sword = Terraria.Mods.ModItems.Register("ModA", "Test Sword", "sword.png", root, out error);
			if (sword == null)
			{
				return Fail("a valid item was refused: " + error);
			}
			if (Terraria.Mods.ModItems.Register("ModA", "Test Sword", "sword.png", root, out error) != null)
			{
				return Fail("a duplicate item name in one mod was accepted");
			}
			// The same name in a different mod is fine: the key is (mod, name).
			if (Terraria.Mods.ModItems.Register("ModB", "Test Sword", "sword.png", root, out error) == null)
			{
				return Fail("two mods could not use the same item name");
			}
			sword.Damage = 33;
			sword.MaxStack = 5;
			sword.Width = 24;
			sword.Height = 26;
			sword.Knockback = 4.5f;
			sword.Melee = true;
			sword.Material = true;
			sword.ToolTip = "A test blade";
			sword.ToolTip2 = "It is only a test";
			// Ids are assigned only now, in sorted order.
			Terraria.Mods.ModItems.Finalize(null);
			if (sword.Type < Terraria.Mods.ModItems.IdBase)
			{
				return Fail("an assigned id is below the mod range: " + sword.Type);
			}
			if (!Terraria.Mods.ModItems.IsModItem(sword.Type))
			{
				return Fail("an assigned id is not recognised as a mod item");
			}
			if (Terraria.Mods.ModItems.IsModItem(1))
			{
				return Fail("a vanilla id was reported as a mod item");
			}
			// Deterministic order: (mod name, item name). ModA/Test Sword sorts before ModB/Test Sword.
			Terraria.Mods.ModItem other = null;
			System.Collections.Generic.List<Terraria.Mods.ModItem> all = Terraria.Mods.ModItems.All();
			for (int i = 0; i < all.Count; i++)
			{
				if (all[i].ModName == "ModB")
				{
					other = all[i];
				}
			}
			if (other == null)
			{
				return Fail("the second mod's item is missing from the registry");
			}
			if (other.Type != sword.Type + 1)
			{
				return Fail("ids are not sequential in sorted order: " + sword.Type + " then " + other.Type);
			}
			// --- applying -----------------------------------------------------
			Item item = default(Item);
			item.SetDefaults(sword.Type);
			if (item.Type != sword.Type)
			{
				return Fail("SetDefaults did not keep the mod id");
			}
			if (item.Damage != 33)
			{
				return Fail("the definition's damage did not apply: " + item.Damage);
			}
			if (item.MaxStack != 5 || item.Width != 24 || item.Height != 26)
			{
				return Fail("the definition's stack or size did not apply");
			}
			if (item.Knockback < 4.49f || item.Knockback > 4.51f)
			{
				return Fail("a float field did not apply: " + item.Knockback);
			}
			if (!item.IsMelee || !item.IsMaterial)
			{
				return Fail("a bool field did not apply");
			}
			// The name path: never null, because it reaches MeasureString on pickup.
			string name = Lang.ItemName(sword.Type);
			if (name != "Test Sword")
			{
				return Fail("the item name is '" + name + "'");
			}
			if (string.IsNullOrEmpty(item.AffixName()))
			{
				return Fail("a mod item has no affix name");
			}
			// Descriptions: a mod item has no entry in Lang.ToolTip's compiled switch, so the text
			// comes from the override table. Without it every converted item is a picture with no
			// explanation - and packs carry a description for almost every item.
			if (Lang.ToolTip(sword.Type) != "A test blade")
			{
				return Fail("the item tooltip is '" + Lang.ToolTip(sword.Type) + "'");
			}
			if (Lang.ToolTip2(sword.Type) != "It is only a test")
			{
				return Fail("the second tooltip line is '" + Lang.ToolTip2(sword.Type) + "'");
			}
			// The override table does not leak into vanilla: an id with no override answers from
			// the compiled table, which is what keeps this from changing existing items by accident.
			if (Terraria.ItemToolTips.GetFirst(1) != null)
			{
				return Fail("a vanilla item gained an override tooltip");
			}
			// --- the mod is gone ----------------------------------------------
			// A save written with a mod, opened without it: the id is in the mod range but has no
			// definition. It must become an empty slot, and the name must still be a string.
			int orphanId = Terraria.Mods.ModItems.IdBase + 500;
			Item orphan = default(Item);
			orphan.SetDefaults(orphanId);
			if (orphan.Type != 0)
			{
				return Fail("an undefined mod id became an item of type " + orphan.Type);
			}
			string orphanName = Lang.ItemName(orphanId);
			if (string.IsNullOrEmpty(orphanName))
			{
				return Fail("an undefined mod id has a null name - that is the MeasureString crash");
			}
			// --- sprites ------------------------------------------------------
			if (Terraria.Mods.ModItems.TryGetSprite(sword.Type, out _, out _))
			{
				return Fail("a sprite was reported without a graphics device");
			}
			// The sheet metrics: the cache is only valid for the sprites it was written for.
			System.Collections.Generic.Dictionary<string, Microsoft.Xna.Framework.Rectangle> byName =
				new System.Collections.Generic.Dictionary<string, Microsoft.Xna.Framework.Rectangle>(StringComparer.OrdinalIgnoreCase);
			byName["Test Sword"] = new Microsoft.Xna.Framework.Rectangle(0, 0, 24, 26);
			byName["Another Item With Spaces"] = new Microsoft.Xna.Framework.Rectangle(24, 0, 16, 16);
			string metrics = Terraria.Mods.ModSheets.FormatMetrics("abc123", byName);
			System.Collections.Generic.Dictionary<string, Microsoft.Xna.Framework.Rectangle> parsed =
				Terraria.Mods.ModSheets.ParseMetrics(metrics, "abc123", out error);
			if (parsed == null)
			{
				return Fail("the sheet metrics did not read back: " + error);
			}
			if (parsed.Count != 2 || !parsed.ContainsKey("Another Item With Spaces"))
			{
				return Fail("a sprite name with spaces did not survive the metrics round trip");
			}
			if (parsed["Test Sword"] != new Microsoft.Xna.Framework.Rectangle(0, 0, 24, 26))
			{
				return Fail("the sprite rectangle did not survive the round trip");
			}
			// A cache written for other sprites must be refused, or a stale sheet would serve
			// pictures that no longer match their files.
			if (Terraria.Mods.ModSheets.ParseMetrics(metrics, "different", out error) != null)
			{
				return Fail("a stale sheet cache was accepted");
			}
			if (string.IsNullOrEmpty(error))
			{
				return Fail("a refused cache gave no reason");
			}
			if (Terraria.Mods.ModSheets.ParseMetrics("not a sheet", "abc123", out error) != null)
			{
				return Fail("garbage was accepted as sheet metrics");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod items check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ModItems.Clear();
			try
			{
				if (System.IO.Directory.Exists(root))
				{
					System.IO.Directory.Delete(root, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	/// <summary>Writes a 1x1 PNG. Hand-built so the check needs no encoder and no device.</summary>
	private static void WriteTinyPng(string path)
	{
		byte[] array = new byte[67]
		{
			137, 80, 78, 71, 13, 10, 26, 10,
			0, 0, 0, 13, 73, 72, 68, 82,
			0, 0, 0, 1, 0, 0, 0, 1,
			8, 6, 0, 0, 0, 31, 21, 196,
			137, 0, 0, 0, 10, 73, 68, 65,
			84, 120, 156, 99, 0, 1, 0, 0,
			5, 0, 1, 13, 10, 45, 180, 0,
			0, 0, 0, 73, 69, 78, 68, 174,
			66, 96, 130
		};
		System.IO.File.WriteAllBytes(path, array);
	}

	// --- Mod set requirement ------------------------------------------------

	/// <summary>
	/// A matching mod set is required, in both directions, and the refusal says which mods.
	///
	/// The rule used to be asymmetric: a client with no mods was accepted by any host, because
	/// "I have none" was read as "I do not care". That is the case that breaks first - a clean
	/// install joining a modded server sees different item stats from everybody else, silently.
	///
	/// The last part runs against a live host, because "the rule is right" and "the rule is
	/// wired into the handshake" are different claims, and only the second one keeps strangers
	/// out.
	/// </summary>
	private static bool CheckModSetRequirement()
	{
		bool savedRequire = Terraria.Mods.ModLoader.RequireMatchingMods;
		try
		{
			// --- the rule ----------------------------------------------------
			Terraria.Mods.ModLoader.RequireMatchingMods = true;
			if (!Terraria.Mods.ModLoader.IsCompatible("vanilla", "vanilla"))
			{
				return Fail("two vanilla installs were called incompatible");
			}
			// The regression: no mods locally must NOT mean "accept anything".
			if (Terraria.Mods.ModLoader.IsCompatible("vanilla", "abc123"))
			{
				return Fail("an empty local mod set accepted a modded peer");
			}
			if (Terraria.Mods.ModLoader.IsCompatible("abc123", "vanilla"))
			{
				return Fail("a vanilla peer was accepted by a modded host");
			}
			if (!Terraria.Mods.ModLoader.IsCompatible("ABC123", "abc123"))
			{
				return Fail("the signature comparison became case-sensitive");
			}
			// Turning the requirement off is a deliberate choice and must still work.
			Terraria.Mods.ModLoader.RequireMatchingMods = false;
			if (!Terraria.Mods.ModLoader.IsCompatible("vanilla", "abc123"))
			{
				return Fail("RequireMatchingMods=False still refused a peer");
			}
			Terraria.Mods.ModLoader.RequireMatchingMods = true;

			// --- the comparison ----------------------------------------------
			System.Collections.Generic.List<Terraria.Mods.ModEntry> host =
				Terraria.Mods.ModSet.Parse("CoolMod|1.2.0\nOther|2.0.0");
			if (host.Count != 2 || host[0].Name != "CoolMod" || host[0].Version != "1.2.0")
			{
				return Fail("the mod list did not round-trip through Parse");
			}
			// A client with nothing: everything the host has is missing.
			Terraria.Mods.ModDifference nothing = Terraria.Mods.ModSet.Compare(host, Terraria.Mods.ModSet.Parse(""));
			if (nothing.Missing.Count != 2 || nothing.Extra.Count != 0 || nothing.VersionMismatch.Count != 0)
			{
				return Fail("an empty client was not reported as missing both mods");
			}
			// Same mods, one version behind: a different problem, reported differently.
			Terraria.Mods.ModDifference older = Terraria.Mods.ModSet.Compare(host,
				Terraria.Mods.ModSet.Parse("CoolMod|1.0.0\nOther|2.0.0"));
			if (older.Missing.Count != 0 || older.VersionMismatch.Count != 1
				|| older.VersionMismatch[0].IndexOf("1.0.0", StringComparison.Ordinal) < 0)
			{
				return Fail("a version difference was not reported: " + older.Describe());
			}
			// One extra mod on the client.
			Terraria.Mods.ModDifference extra = Terraria.Mods.ModSet.Compare(host,
				Terraria.Mods.ModSet.Parse("CoolMod|1.2.0\nOther|2.0.0\nSneaky|0.1"));
			if (extra.Extra.Count != 1 || extra.Extra[0].IndexOf("Sneaky", StringComparison.Ordinal) < 0)
			{
				return Fail("an extra client mod was not reported");
			}
			if (Terraria.Mods.ModSet.Compare(host, host).Any)
			{
				return Fail("identical sets were reported as different");
			}
			// Names are compared case-insensitively: a folder renamed by case is the same mod.
			if (Terraria.Mods.ModSet.Compare(host, Terraria.Mods.ModSet.Parse("coolmod|1.2.0\nother|2.0.0")).Any)
			{
				return Fail("case differences in mod names were reported as a mismatch");
			}

			// --- the wording --------------------------------------------------
			byte[] payload = Terraria.Net.RejectReasons.Build(Terraria.Net.RejectReason.ModMismatch,
				"CoolMod v1.2.0\nOther v2.0.0", "", "");
			Terraria.Net.RejectReason reason;
			string[] fields;
			if (!Terraria.Net.RejectReasons.TryParse(payload, out reason, out fields))
			{
				return Fail("a mod-mismatch refusal did not parse");
			}
			if (reason != Terraria.Net.RejectReason.ModMismatch)
			{
				return Fail("the refusal came back as " + reason);
			}
			string spoken = Terraria.Net.RejectReasons.Describe(reason, fields);
			if (spoken.IndexOf("CoolMod", StringComparison.Ordinal) < 0 || spoken.IndexOf("Other", StringComparison.Ordinal) < 0)
			{
				return Fail("the refusal does not name the mods: " + spoken);
			}
			// A refusal with no lists (an older host) still reads as a sentence.
			string plain = Terraria.Net.RejectReasons.Describe(Terraria.Net.RejectReason.ModMismatch, new string[0]);
			if (string.IsNullOrEmpty(plain) || plain.IndexOf("mod", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("a refusal without a list produced: '" + plain + "'");
			}
			// Only one kind of difference: the combined sentence would read as "missing: ; extra: X".
			string onlyExtra = Terraria.Net.RejectReasons.Describe(Terraria.Net.RejectReason.ModMismatch,
				new string[3] { "", "Sneaky v0.1", "" });
			if (onlyExtra.IndexOf("Sneaky", StringComparison.Ordinal) < 0
				|| onlyExtra.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return Fail("an extra-only mismatch used the wrong sentence: " + onlyExtra);
			}

			// --- and it is actually wired into the handshake -------------------
			int savedPort = NetworkConfig.Port;
			int savedDiscovery = NetworkConfig.DiscoveryPort;
			string savedPassword = NetworkConfig.Password;
			bool savedTcp = NetworkConfig.UseTcpTransport;
			Microsoft.Xna.Framework.Net.NetworkSession hostSession = null;
			try
			{
				Microsoft.Xna.Framework.GamerServices.GamerServicesDispatcher.Initialize(null);
				NetworkConfig.UseTcpTransport = true;
				NetworkConfig.Password = string.Empty;
				NetworkConfig.Port = 0;
				NetworkConfig.DiscoveryPort = 0;
				// The host believes it runs CoolMod; the peer will claim a vanilla install.
				Terraria.Net.TcpPeer.HostSignatureForTests = "hostsig";
				Terraria.Net.TcpPeer.HostModListForTests = "CoolMod|1.2.0";
				Microsoft.Xna.Framework.Net.NetworkSessionProperties properties = new Microsoft.Xna.Framework.Net.NetworkSessionProperties();
				hostSession = new Microsoft.Xna.Framework.Net.NetworkSession(properties,
					Microsoft.Xna.Framework.Net.NetworkSessionType.PlayerMatch, 8, 0, 1,
					new List<Microsoft.Xna.Framework.GamerServices.SignedInGamer>(1) { Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers[0] });
				hostSession.StartHostPeer();
				int port = NetworkConfig.ActivePort;
				Terraria.Net.RejectReason got;
				string[] gotFields;
				if (!TryHandshakeReject(port, "VanillaPeer", string.Empty, "vanilla", string.Empty, out got, out gotFields))
				{
					return Fail("the host did not answer a mismatched handshake");
				}
				if (got != Terraria.Net.RejectReason.ModMismatch)
				{
					return Fail("a vanilla peer against a modded host got '" + got + "'");
				}
				string said = Terraria.Net.RejectReasons.Describe(got, gotFields);
				if (said.IndexOf("CoolMod", StringComparison.Ordinal) < 0)
				{
					return Fail("the live refusal does not name the mod: " + said);
				}
				// A matching peer gets past the mod gate - the host answers with something that is
				// not a refusal (a welcome/roster), which is what "accepted" looks like here.
				if (TryHandshakeReject(port, "MatchingPeer", string.Empty, "hostsig", "CoolMod|1.2.0", out got, out gotFields))
				{
					return Fail("a matching peer was refused with '" + got + "'");
				}
				// A download connection is not a join attempt. With downloads off it gets the
				// ordinary refusal and no offer; with them on it gets a file frame and never a
				// Welcome - a refused peer must not be able to become a player by asking for files.
				bool savedAllowDownload = NetworkConfig.AllowDownload;
				try
				{
					NetworkConfig.AllowDownload = false;
					if (TryHandshakeReject(port, "Downloader", string.Empty, "vanilla", string.Empty, out got, out gotFields)
						&& gotFields.Length > 3 && gotFields[3] == "1")
					{
						return Fail("the refusal offered a download while downloads are off");
					}
					NetworkConfig.AllowDownload = true;
					byte firstOp;
					if (!TryDownloadHandshake(port, "CoolMod", out firstOp))
					{
						return Fail("the host did not answer a download handshake");
					}
					if (firstOp != Terraria.Net.TcpPeer.OpModFile)
					{
						return Fail("a download connection got opcode " + firstOp + " instead of a file frame"
							+ " (allow=" + NetworkConfig.AllowDownload + ", opModFile=" + Terraria.Net.TcpPeer.OpModFile + ")");
					}
					if (TryHandshakeReject(port, "Downloader", string.Empty, "vanilla", string.Empty, out got, out gotFields)
						&& !(got == Terraria.Net.RejectReason.ModMismatch && gotFields.Length > 3 && gotFields[3] == "1"))
					{
						return Fail("the refusal did not offer the download it can serve");
					}
				}
				finally
				{
					NetworkConfig.AllowDownload = savedAllowDownload;
				}
			}
			finally
			{
				Terraria.Net.TcpPeer.HostSignatureForTests = null;
				Terraria.Net.TcpPeer.HostModListForTests = null;
				Terraria.Net.TcpPeer.ClientSignatureForTests = null;
				Terraria.Net.TcpPeer.ClientModListForTests = null;
				try
				{
					hostSession?.Dispose();
				}
				catch
				{
				}
				NetworkConfig.Port = savedPort;
				NetworkConfig.DiscoveryPort = savedDiscovery;
				NetworkConfig.Password = savedPassword;
				NetworkConfig.UseTcpTransport = savedTcp;
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod set check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ModLoader.RequireMatchingMods = savedRequire;
		}
	}

	// --- Mod menu kinds -----------------------------------------------------

	/// <summary>
	/// The mod list shows both kinds of thing and treats them differently.
	///
	/// The point of the check is that a tConfig pack is <b>visible but not a mod</b>: it has no
	/// version, it takes no part in the signature, it cannot be toggled, and the only thing that
	/// can be done with it is convert it. Getting that wrong in either direction is bad - hiding
	/// it leaves the player with a folder that silently changes item numbers, and treating it as a
	/// mod puts a folder without an assembly into the load order.
	/// </summary>
	private static bool CheckModMenuKinds()
	{
		string root = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-modmenu");
		string mods = System.IO.Path.Combine(root, "Mods");
		string packs = System.IO.Path.Combine(root, "tConfig");
		string sources = System.IO.Path.Combine(root, "ModSources");
		string backups = System.IO.Path.Combine(root, "Backups");
		string savedMods = Terraria.Mods.ModMenu.ModsRootOverride;
		string savedPacks = Terraria.Mods.ModMenu.PacksRootOverride;
		string savedSources = Terraria.Mods.ModMenu.ModSourcesRootOverride;
		string savedBackups = Terraria.Mods.ModMenu.BackupsRootOverride;
		string savedLog = Terraria.Mods.TConfigConverter.LogPathOverride;
		try
		{
			if (System.IO.Directory.Exists(root))
			{
				System.IO.Directory.Delete(root, recursive: true);
			}
			Terraria.Mods.TConfigConverter.LogPathOverride = System.IO.Path.Combine(root, "convert.log");
			// One real mod (a folder with a dll) and one folder that is not a mod (no dll).
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(mods, "RealMod"));
			System.IO.File.WriteAllBytes(System.IO.Path.Combine(mods, "RealMod", "RealMod.dll"), new byte[1] { 0 });
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(mods, "NotAMod"));
			// One pack, and one folder in tConfig that is not a pack (no Item/*.ini).
			string packItems = System.IO.Path.Combine(packs, "OldPack", "Item");
			System.IO.Directory.CreateDirectory(packItems);
			System.IO.File.WriteAllText(System.IO.Path.Combine(packItems, "Copper Shortsword.ini"), "damage=12\r\n");
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packs, "EmptyFolder"));

			Terraria.Mods.ModMenu.ModsRootOverride = mods;
			Terraria.Mods.ModMenu.PacksRootOverride = packs;
			Terraria.Mods.ModMenu.ModSourcesRootOverride = sources;
			Terraria.Mods.ModMenu.BackupsRootOverride = backups;
			Terraria.Mods.ModMenu.Reset();

			// Both kinds are listed; the folder without a dll and the folder without Item/*.ini
			// are not.
			if (Terraria.Mods.ModMenu.EntryCount != 2)
			{
				return Fail("the list holds " + Terraria.Mods.ModMenu.EntryCount + " row(s), expected 2");
			}
			int modIndex = -1;
			int packIndex = -1;
			for (int i = 0; i < Terraria.Mods.ModMenu.EntryCount; i++)
			{
				if (Terraria.Mods.ModMenu.IsPackAt(i))
				{
					packIndex = i;
				}
				else
				{
					modIndex = i;
				}
			}
			if (modIndex < 0 || packIndex < 0)
			{
				return Fail("the list does not hold one row of each kind");
			}
			if (Terraria.Mods.ModMenu.FolderAt(modIndex) != "RealMod")
			{
				return Fail("the mod row is '" + Terraria.Mods.ModMenu.FolderAt(modIndex) + "'");
			}
			if (Terraria.Mods.ModMenu.FolderAt(packIndex) != "OldPack")
			{
				return Fail("the pack row is '" + Terraria.Mods.ModMenu.FolderAt(packIndex) + "'");
			}
			// Converting the pack writes a project and asks about the original. (The raw
			// conversion, which is the menu's default key, is checked at the end - it writes into
			// Storage/Mods, and doing it here would add a row to this list mid-test.)
			Terraria.Mods.ModMenu.SelectForTests(packIndex);
			Terraria.Mods.ModMenu.ConvertSelectedForTests(raw: false);
			string project = System.IO.Path.Combine(sources, "OldPack", "OldPack.csproj");
			if (!System.IO.File.Exists(project))
			{
				return Fail("conversion from the menu wrote no project (" + Terraria.Mods.ModMenu.StatusForTests + ")");
			}
			if (!Terraria.Mods.ModMenu.IsConfirmingForTests)
			{
				return Fail("conversion did not ask what to do with the original pack");
			}
			// Answering "keep" leaves the pack where it was.
			Terraria.Mods.ModMenu.AnswerConfirmForTests(false);
			if (!System.IO.Directory.Exists(System.IO.Path.Combine(packs, "OldPack")))
			{
				return Fail("answering Keep deleted the pack");
			}
			if (Terraria.Mods.ModMenu.IsConfirmingForTests)
			{
				return Fail("the question stayed up after it was answered");
			}
			// Answering "delete" moves it into Backups - not away for good: the only copy of
			// somebody's pack must not disappear because a menu button was pressed.
			Terraria.Mods.ModMenu.SelectForTests(packIndex);
			Terraria.Mods.ModMenu.ConvertSelectedForTests(raw: false);
			Terraria.Mods.ModMenu.AnswerConfirmForTests(true);
			if (System.IO.Directory.Exists(System.IO.Path.Combine(packs, "OldPack")))
			{
				return Fail("answering Delete left the pack in place");
			}
			if (!System.IO.Directory.Exists(backups))
			{
				return Fail("answering Delete did not put the pack in Backups");
			}
			string[] moved = System.IO.Directory.GetDirectories(backups);
			if (moved.Length != 1)
			{
				return Fail("Backups holds " + moved.Length + " folder(s), expected 1");
			}
			if (moved[0].IndexOf("OldPack", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("the backup folder is '" + System.IO.Path.GetFileName(moved[0]) + "'");
			}
			// The list reflects the removal, and the cursor is still inside it.
			if (Terraria.Mods.ModMenu.EntryCount != 1)
			{
				string dump = "";
				for (int i = 0; i < Terraria.Mods.ModMenu.EntryCount; i++)
				{
					dump += " [" + Terraria.Mods.ModMenu.FolderAt(i) + (Terraria.Mods.ModMenu.IsPackAt(i) ? "/pack" : "/mod") + "]";
				}
				return Fail("the list still holds " + Terraria.Mods.ModMenu.EntryCount + " row(s) after the pack was removed:" + dump);
			}
			if (Terraria.Mods.ModMenu.IsPackAt(0))
			{
				return Fail("the remaining row is still a pack");
			}
			// Converting a mod must do nothing at all: it is not a pack.
			Terraria.Mods.ModMenu.SelectForTests(0);
			Terraria.Mods.ModMenu.ConvertSelectedForTests(raw: false);
			if (Terraria.Mods.ModMenu.IsConfirmingForTests)
			{
				return Fail("converting a mod asked a question");
			}
			// --- the raw conversion, which is the menu's default ------------------
			// Its own folders: it writes a mod into the Mods root, and that changes this list.
			string mods2 = System.IO.Path.Combine(root, "Mods2");
			string packs2 = System.IO.Path.Combine(root, "tConfig2");
			System.IO.Directory.CreateDirectory(mods2);
			string pack2Items = System.IO.Path.Combine(packs2, "RawPack", "Item");
			System.IO.Directory.CreateDirectory(pack2Items);
			System.IO.File.WriteAllText(System.IO.Path.Combine(pack2Items, "Copper Bar.ini"), "value=4321\r\n");
			System.IO.File.WriteAllText(System.IO.Path.Combine(pack2Items, "Test Blade.ini"), "type=-1\r\ndamage=9\r\n");
			WriteTinyPng(System.IO.Path.Combine(pack2Items, "Test Blade.png"));
			Terraria.Mods.ModMenu.ModsRootOverride = mods2;
			Terraria.Mods.ModMenu.PacksRootOverride = packs2;
			Terraria.Mods.ModMenu.Reset();
			int rawPackIndex = -1;
			for (int i = 0; i < Terraria.Mods.ModMenu.EntryCount; i++)
			{
				if (Terraria.Mods.ModMenu.IsPackAt(i))
				{
					rawPackIndex = i;
				}
			}
			if (rawPackIndex < 0)
			{
				return Fail("the second pack is not in the list");
			}
			Terraria.Mods.ModMenu.SelectForTests(rawPackIndex);
			Terraria.Mods.ModMenu.ConvertSelectedForTests();
			string rawFolder = System.IO.Path.Combine(mods2, "RawPack");
			if (!System.IO.File.Exists(System.IO.Path.Combine(rawFolder, "mod.ini")))
			{
				return Fail("the menu's convert wrote no mod.ini (" + Terraria.Mods.ModMenu.StatusForTests + ")");
			}
			string rawOverride = System.IO.Path.Combine(rawFolder, "Item", "Copper Bar.ini");
			string rawNew = System.IO.Path.Combine(rawFolder, "Item", "Test Blade.ini");
			if (!System.IO.File.Exists(rawOverride) || !System.IO.File.Exists(rawNew))
			{
				return Fail("the raw mod is missing an item file");
			}
			if (!System.IO.File.Exists(System.IO.Path.Combine(rawFolder, "Item", "Test Blade.png")))
			{
				return Fail("the raw mod is missing a sprite");
			}
			if (!System.IO.File.ReadAllText(rawOverride).Contains("value=4321"))
			{
				return Fail("the raw override lost its value");
			}
			if (!System.IO.File.ReadAllText(rawNew).Contains("type=-1"))
			{
				return Fail("the raw new item lost its marker");
			}
			if (!Terraria.Mods.ModMenu.IsConfirmingForTests)
			{
				return Fail("the raw conversion did not ask about the original pack");
			}
			Terraria.Mods.ModMenu.AnswerConfirmForTests(false);
			// Re-converting must not leave items the pack no longer has: the folder is cleaned
			// first, but only because this tool generated it.
			System.IO.File.WriteAllText(System.IO.Path.Combine(rawFolder, "Item", "Stale.ini"), "damage=1\r\n");
			Terraria.Mods.ModMenu.SelectForTests(rawPackIndex);
			Terraria.Mods.ModMenu.ConvertSelectedForTests();
			Terraria.Mods.ModMenu.AnswerConfirmForTests(false);
			if (System.IO.File.Exists(System.IO.Path.Combine(rawFolder, "Item", "Stale.ini")))
			{
				return Fail("re-converting kept an item the pack no longer has");
			}
			// And the converted folder is a mod row now, not a pack.
			Terraria.Mods.ModMenu.Rebuild();
			bool sawRaw = false;
			for (int i = 0; i < Terraria.Mods.ModMenu.EntryCount; i++)
			{
				if (Terraria.Mods.ModMenu.FolderAt(i) == "RawPack" && !Terraria.Mods.ModMenu.IsPackAt(i))
				{
					sawRaw = true;
				}
			}
			if (!sawRaw)
			{
				return Fail("the converted folder is not listed as a mod");
			}
			// Wheel scrolling clamps instead of wrapping: a mouse wheel that jumps from the last
			// row to the first is a list you cannot aim at. (The stick wraps, because a stick can
			// only be pushed a little at a time; the wheel can jump the whole list in one flick.)
			Terraria.Mods.ModMenu.SelectForTests(0);
			Terraria.Mods.ModMenu.Scroll(1);
			if (Terraria.Mods.ModMenu.SelectedIndexForTests != 1)
			{
				return Fail("the wheel did not move the selection down one row");
			}
			Terraria.Mods.ModMenu.Scroll(1);
			if (Terraria.Mods.ModMenu.SelectedIndexForTests != 1)
			{
				return Fail("the wheel wrapped past the last row instead of clamping");
			}
			Terraria.Mods.ModMenu.Scroll(-5);
			if (Terraria.Mods.ModMenu.SelectedIndexForTests != 0)
			{
				return Fail("the wheel wrapped past the first row instead of clamping");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod menu check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ModMenu.ModsRootOverride = savedMods;
			Terraria.Mods.ModMenu.PacksRootOverride = savedPacks;
			Terraria.Mods.ModMenu.ModSourcesRootOverride = savedSources;
			Terraria.Mods.ModMenu.BackupsRootOverride = savedBackups;
			Terraria.Mods.TConfigConverter.LogPathOverride = savedLog;
			try
			{
				if (System.IO.Directory.Exists(root))
				{
					System.IO.Directory.Delete(root, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	// --- Inventory: one action per frame ------------------------------------

	/// <summary>
	/// A click must move an item once, not twice.
	///
	/// The inventory's slot handler is reachable from two places that both run every frame - the
	/// pointer path and the stick path - and <c>IsButtonTriggered</c> is a pure edge test that is
	/// not consumed. Both calls therefore saw the same click and the action ran twice: the stack
	/// was picked up and put straight back. On screen that is "the mouse does nothing", and a slot
	/// whose second call was a no-op looked like the only one that worked.
	///
	/// The gate is tiny, which is exactly why it is tested: the bug it prevents lives in the
	/// interaction between two callers, not in either of them.
	/// </summary>
	private static bool CheckInventoryOncePerFrame()
	{
		try
		{
			FrameGate frameGate = new FrameGate();
			// First caller in a frame acts, second does not - the whole point.
			if (!frameGate.TryBegin())
			{
				return Fail("the first action of a frame was refused");
			}
			if (frameGate.TryBegin())
			{
				return Fail("a second action in the same frame was allowed");
			}
			if (!frameGate.Taken)
			{
				return Fail("the gate does not report that it was taken");
			}
			// A new frame starts clean.
			frameGate.Reset();
			if (frameGate.Taken)
			{
				return Fail("the gate stayed taken after a reset");
			}
			if (!frameGate.TryBegin())
			{
				return Fail("the gate refused the first action of the next frame");
			}
			// A frame in which nothing acts must leave the gate open for the next caller.
			frameGate.Reset();
			if (frameGate.Taken || !frameGate.TryBegin())
			{
				return Fail("an idle frame closed the gate");
			}
			// The UI's own gate is reset by UpdateMouse, so it starts closed after construction and
			// must not carry state between screens.
			UI ui = new UI();
			if (ui.InventoryActionGate == null)
			{
				return Fail("the UI has no inventory action gate");
			}
			ui.InventoryActionGate.Reset();
			if (!ui.InventoryActionGate.TryBegin() || ui.InventoryActionGate.TryBegin())
			{
				return Fail("the UI's gate does not limit one action per frame");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("inventory once-per-frame check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Inventory grid -----------------------------------------------------

	/// <summary>
	/// The inventory's cell arithmetic, at the resolutions the game actually runs at.
	///
	/// This is the regression guard for "the inventory picks the wrong item". Drawing used
	/// <c>(int)(i * cell)</c> per column and the hit-test used <c>(int)(offset / cell)</c>; those
	/// are not inverses when the cell is fractional, which it always is. At 1080p the cell is
	/// 104.3 px, so column 4 starts at 417 and 417 / 104.3 truncates to 3: every column boundary
	/// selected the slot to its left.
	///
	/// The check is exhaustive over every pixel of the grid at each resolution rather than
	/// spot-checked at the boundaries, because "almost all pixels are right" is exactly what the
	/// bug looked like.
	/// </summary>
	private static bool CheckInventoryGrid()
	{
		try
		{
			// The three console presets: 540p, 720p and 1080p.
			float[] multipliers = new float[3] { 1f, 1.5f, 2f };
			for (int m = 0; m < multipliers.Length; m++)
			{
				float cell = 52.15f * multipliers[m];
				// Every pixel inside the grid maps back to the cell that owns it.
				for (int column = 0; column < 10; column++)
				{
					int start = Terraria.InventoryGrid.CellStart(column, cell);
					int end = Terraria.InventoryGrid.CellStart(column + 1, cell);
					for (int offset = start; offset < end; offset++)
					{
						int got = Terraria.InventoryGrid.CellIndex(offset, cell, 10);
						if (got != column)
						{
							return Fail("x" + multipliers[m] + ": pixel " + offset + " of column " + column
								+ " (starts " + start + ") hit column " + got);
						}
					}
				}
				for (int row = 0; row < 4; row++)
				{
					int start2 = Terraria.InventoryGrid.CellStart(row, cell);
					int end2 = Terraria.InventoryGrid.CellStart(row + 1, cell);
					for (int offset2 = start2; offset2 < end2; offset2++)
					{
						int got2 = Terraria.InventoryGrid.CellIndex(offset2, cell, 4);
						if (got2 != row)
						{
							return Fail("x" + multipliers[m] + ": pixel " + offset2 + " of row " + row + " hit row " + got2);
						}
					}
				}
				// Outside the grid: the last cell ends where the grid ends, and one pixel past it
				// is not the last cell.
				int gridWidth = Terraria.InventoryGrid.CellStart(10, cell);
				if (Terraria.InventoryGrid.CellIndex(gridWidth, cell, 10) >= 0)
				{
					return Fail("x" + multipliers[m] + ": one pixel past the grid is still inside it");
				}
				if (Terraria.InventoryGrid.CellIndex(gridWidth - 1, cell, 10) != 9)
				{
					return Fail("x" + multipliers[m] + ": the last pixel of the grid is not in the last column");
				}
				// Negative offsets must not truncate towards cell 0.
				if (Terraria.InventoryGrid.CellIndex(-1, cell, 10) >= 0)
				{
					return Fail("x" + multipliers[m] + ": a negative offset landed on a cell");
				}
				// The full point conversion, including the origin.
				int column2;
				int row2;
				if (Terraria.InventoryGrid.TryCell(100 + Terraria.InventoryGrid.CellStart(3, cell),
					200 + Terraria.InventoryGrid.CellStart(2, cell), 100, 200, cell, 10, 4, out column2, out row2))
				{
					if (column2 != 3 || row2 != 2)
					{
						return Fail("x" + multipliers[m] + ": the pointer landed on (" + column2 + ", " + row2 + ")");
					}
				}
				else
				{
					return Fail("x" + multipliers[m] + ": a point inside the grid was rejected");
				}
				if (Terraria.InventoryGrid.TryCell(99, 200, 100, 200, cell, 10, 4, out column2, out row2))
				{
					return Fail("x" + multipliers[m] + ": a point left of the grid was accepted");
				}
				if (Terraria.InventoryGrid.TryCell(100, 199, 100, 200, cell, 10, 4, out column2, out row2))
				{
					return Fail("x" + multipliers[m] + ": a point above the grid was accepted");
				}
			}
			// Degenerate inputs must not divide by zero or loop forever.
			if (Terraria.InventoryGrid.CellIndex(5, 0f, 10) >= 0)
			{
				return Fail("a zero cell size produced a cell");
			}
			if (Terraria.InventoryGrid.CellIndex(5, 10f, 0) >= 0)
			{
				return Fail("a zero column count produced a cell");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("inventory grid check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Item names ---------------------------------------------------------

	/// <summary>
	/// Every item the game can create must have a name.
	///
	/// This is not cosmetic. Picking an item up builds a floating label from
	/// <c>Item.AffixName()</c>, and a null there reaches <c>SpriteFont.MeasureString</c>, which
	/// throws - the game dies on the pickup, not on the drop. That is exactly the crash the crash
	/// log recorded, so the invariant is checked for every id rather than trusted.
	///
	/// The negative ids matter separately: they are the pre-1.0 gold/silver/phase items that the
	/// debug kit hands out, and they are reached through <c>NetDefaults</c> rather than
	/// <c>SetDefaults</c>, so they have their own table.
	/// </summary>
	private static bool CheckItemNames()
	{
		try
		{
			int max = (int)EntityID.ItemID.NUM_TYPES;
			System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();
			for (int i = 1; i < max; i++)
			{
				Item item = default(Item);
				try
				{
					item.SetDefaults(i);
				}
				catch (Exception ex)
				{
					// SetDefaults throwing is worse than a missing name: it means the id exists in
					// the enum but has no case, and the game dies the moment anything creates it.
					missing.Add(i.ToString() + " (SetDefaults: " + ex.GetType().Name + ")");
					continue;
				}
				// Type 0 means the id is not an item in this build (gaps in the enum), which is
				// not a naming problem.
				if (item.Type <= 0)
				{
					continue;
				}
				string text = Lang.ItemName(item.NetID);
				if (string.IsNullOrEmpty(text))
				{
					missing.Add(i.ToString() + " (NetID " + item.NetID + ")");
					continue;
				}
				if (string.IsNullOrEmpty(item.AffixName()))
				{
					missing.Add(i.ToString() + " affix");
				}
			}
			// The negative (legacy) ids.
			for (int j = -24; j <= -1; j++)
			{
				Item item2 = default(Item);
				item2.NetDefaults(j);
				if (item2.Type <= 0)
				{
					missing.Add(j.ToString() + " (NetDefaults gave no item)");
					continue;
				}
				if (string.IsNullOrEmpty(item2.AffixName()))
				{
					missing.Add(j.ToString() + " affix");
				}
			}
			if (missing.Count > 0)
			{
				string text2 = string.Empty;
				for (int k = 0; k < missing.Count && k < 12; k++)
				{
					text2 += " " + missing[k] + ";";
				}
				return Fail(missing.Count + " item(s) have no name:" + text2);
			}
			// The label path itself must survive a name: this is the call that crashed.
			Item item3 = default(Item);
			item3.SetDefaults("Copper Shortsword");
			string text3 = item3.AffixName() + (2).ToStackString();
			if (string.IsNullOrEmpty(text3))
			{
				return Fail("a pickup label has no text");
			}
			// Measuring needs a font, and fonts need a graphics device: in headless mode there is
			// none, so this part is skipped rather than faked. The name being non-empty above is
			// what made MeasureString throw.
			if (UI.BoldSmallFont != null && UI.MeasureString(UI.BoldSmallFont, text3).X <= 0f)
			{
				return Fail("a pickup label measured as zero width");
			}
			// ...and a prefixed item, which goes through a different branch of the affix name.
			item3.SetPrefix(1);
			if (string.IsNullOrEmpty(item3.AffixName()))
			{
				return Fail("a prefixed item has no name");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("item name check threw: " + ex.GetType().Name + ": " + ex.Message
				+ " at " + (ex.StackTrace ?? "").Split('\n')[0].Trim());
		}
	}

	// --- Font atlas ---------------------------------------------------------

	/// <summary>
	/// The metrics format that lets a language supply its own glyphs.
	///
	/// The five shipped translations have their letters baked into the content atlases, with the
	/// character list compiled into <c>FontHD</c> - which is why Cyrillic cannot be shown at all.
	/// <c>Storage/Fonts/&lt;code&gt;.metrics</c> plus a PNG is the way out, and the format is what
	/// a generator and the game have to agree on. Every rule below is a way that agreement can
	/// break silently: a glyph outside the texture draws garbage from the edge of the image, a
	/// wrong line height makes every row of text overlap, and a missing fallback character throws
	/// the moment a string contains something unexpected.
	/// </summary>
	private static bool CheckFontAtlas()
	{
		try
		{
			// A minimal valid atlas: three glyphs, the cell convention the baked atlases use.
			string text = "# comment\n"
				+ "texture=atlas.png\n"
				+ "lineHeight=32\n"
				+ "spacing=0\n"
				+ "default=63\n"
				+ "glyphs=3\n"
				+ "32 11 0 0\n"
				+ "63 9 11 0\n"
				+ "65 21 20 0\n";
			string error;
			Terraria.Fonts.FontAtlas fontAtlas = Terraria.Fonts.FontAtlas.Parse(text, out error);
			if (fontAtlas == null)
			{
				return Fail("a valid atlas was rejected: " + error);
			}
			if (fontAtlas.Count != 3 || fontAtlas.LineHeight != 32 || fontAtlas.TexturePath != "atlas.png")
			{
				return Fail("the atlas header was not read");
			}
			if (!fontAtlas.Has('A') || fontAtlas.Has('B'))
			{
				return Fail("the glyph set is wrong");
			}
			if (fontAtlas.DefaultCharacter != '?')
			{
				return Fail("the fallback character was not read");
			}
			// The convention, not just the numbers: cropping covers the whole cell and kerning
			// advances by the same width. A tight-cropping atlas (the XNA content pipeline's
			// convention) draws every glyph in the wrong place, which is why this is asserted
			// rather than assumed.
			int a = fontAtlas.Characters.IndexOf('A');
			if (fontAtlas.Cropping[a] != new Microsoft.Xna.Framework.Rectangle(0, 0, 21, 32))
			{
				return Fail("cropping is not the full cell: " + fontAtlas.Cropping[a]);
			}
			if (fontAtlas.Kerning[a] != new Microsoft.Xna.Framework.Vector3(0f, 21f, 0f))
			{
				return Fail("kerning does not advance by the glyph width: " + fontAtlas.Kerning[a]);
			}
			if (fontAtlas.GlyphBounds[a] != new Microsoft.Xna.Framework.Rectangle(20, 0, 21, 32))
			{
				return Fail("the glyph rectangle is wrong: " + fontAtlas.GlyphBounds[a]);
			}
			// Round-trip: what the tool writes must be what the game reads back.
			Terraria.Fonts.FontAtlas reparsed = Terraria.Fonts.FontAtlas.Parse(fontAtlas.Format(), out error);
			if (reparsed == null)
			{
				return Fail("the atlas the game writes cannot be read back: " + error);
			}
			if (reparsed.Count != fontAtlas.Count || reparsed.LineHeight != fontAtlas.LineHeight
				|| reparsed.Spacing != fontAtlas.Spacing || reparsed.DefaultCharacter != fontAtlas.DefaultCharacter
				|| reparsed.TexturePath != fontAtlas.TexturePath)
			{
				return Fail("the round-tripped atlas differs in its header");
			}
			for (int i = 0; i < fontAtlas.Count; i++)
			{
				if (reparsed.Characters[i] != fontAtlas.Characters[i]
					|| reparsed.GlyphBounds[i] != fontAtlas.GlyphBounds[i])
				{
					return Fail("glyph " + i + " changed in the round trip");
				}
			}
			// --- what must be refused ----------------------------------------
			// Each of these is a real way a hand-edited or half-generated file goes wrong, and
			// each would otherwise produce text that looks almost right.
			string[] broken = new string[8]
			{
				"texture=a.png\nlineHeight=32\nglyphs=2\n65 21 0 0\n",
				"texture=a.png\nlineHeight=0\nglyphs=1\n65 21 0 0\n",
				"texture=a.png\nlineHeight=32\nglyphs=1\n65 0 0 0\n",
				"texture=a.png\nlineHeight=32\nglyphs=1\n65 21 -5 0\n",
				"texture=a.png\nlineHeight=32\nglyphs=2\n65 21 0 0\n65 21 21 0\n",
				"lineHeight=32\nglyphs=1\n65 21 0 0\n",
				"texture=a.png\nlineHeight=32\nglyphs=1\n65 21 0 0\n66 21 21 0\n",
				"texture=a.png\nlineHeight=32\ndefault=90\nglyphs=1\n65 21 0 0\n"
			};
			string[] why = new string[8]
			{
				"a glyph count that does not match",
				"a zero line height",
				"a zero advance",
				"a negative texture position",
				"a duplicated character",
				"no texture name",
				"a count that disagrees with the glyphs present",
				"a fallback character that is not in the atlas"
			};
			for (int j = 0; j < broken.Length; j++)
			{
				if (Terraria.Fonts.FontAtlas.Parse(broken[j], out error) != null)
				{
					return Fail("the atlas parser accepted " + why[j]);
				}
				if (string.IsNullOrEmpty(error))
				{
					return Fail("refusing " + why[j] + " gave no reason");
				}
			}
			// Unknown keys are ignored, not fatal: that is what lets a newer file be readable by
			// an older build instead of failing outright.
			Terraria.Fonts.FontAtlas tolerant = Terraria.Fonts.FontAtlas.Parse(
				"texture=a.png\nlineHeight=32\nkerningScale=1.5\nglyphs=1\n65 21 0 0\n", out error);
			if (tolerant == null)
			{
				return Fail("an unknown key made the atlas unreadable: " + error);
			}
			// Paths: a set is looked up by language code under Storage/Fonts, one file per font.
			string big = Terraria.Fonts.GeneratedFonts.MetricsPath("ru", "big");
			if (!big.EndsWith("ru.big.metrics", StringComparison.OrdinalIgnoreCase))
			{
				return Fail("the metrics path is '" + big + "'");
			}
			if (!big.StartsWith(Terraria.Storage.AppPaths.Fonts, StringComparison.OrdinalIgnoreCase))
			{
				return Fail("the metrics path is not under Storage/Fonts: " + big);
			}
			// An incomplete set is reported before anything is loaded, so a language with two of
			// the three fonts falls back to the baked atlas instead of drawing half its text.
			if (Terraria.Fonts.GeneratedFonts.IsComplete("xx"))
			{
				return Fail("a language with no files was reported as complete");
			}
			if (Terraria.Fonts.GeneratedFonts.IsComplete(""))
			{
				return Fail("an empty language code was reported as complete");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("font atlas check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	/// <summary>
	/// The custom language: a language the build has no compiled tables for.
	///
	/// Two things have to hold or the feature is a trap. It must never leave the tables empty -
	/// an unknown option falls through every <c>LangOption == N</c> branch, and a blank UI is
	/// worse than English. And its code must be safe to use as a file name: it comes from a
	/// settings file, and a path separator there would point the loader at any file on the disk.
	/// </summary>
	private static bool CheckCustomLanguage()
	{
		int savedChoice = Language.Choice;
		int savedOption = Lang.LangOption;
		LangOverrides.SuppressFiles = true;
		try
		{
			string error;
			if (!Language.Apply(Language.Custom, out error))
			{
				return Fail("the custom language was rejected: " + error);
			}
			// The effective language stays a real one. Making it 6 broke every table in Lang.cs,
			// which ends with "otherwise return null": ItemName returned null for every item and
			// picking one up crashed the game. This is the regression guard for that.
			if (Language.Effective == Language.Custom || Language.Effective < 1)
			{
				return Fail("the custom language leaked into the built-in tables (effective "
					+ Language.Effective + ")");
			}
			if (string.IsNullOrEmpty(Lang.ItemName(1)))
			{
				return Fail("item names are empty under the custom language");
			}
			if (string.IsNullOrEmpty(Lang.MenuText[6]))
			{
				return Fail("menu text is empty under the custom language");
			}
			// The tables are English, not empty: the custom language's file replaces what it
			// covers and English fills the rest.
			if (!Language.TablesFilled())
			{
				return Fail("the custom language left the string tables empty");
			}
			if (string.IsNullOrEmpty(Lang.MenuText[6]))
			{
				return Fail("the custom language has no fallback text");
			}
			if (Language.Choice != Language.Custom)
			{
				return Fail("the custom language was not remembered as the choice");
			}
			// The label names the configured code, because that is what the player typed.
			if (Language.Label(Language.Custom).IndexOf("Custom", StringComparison.Ordinal) < 0)
			{
				return Fail("the custom language has no label: " + Language.Label(Language.Custom));
			}
			// File names are filtered: a code is not a path.
			string[] hostile = new string[5] { "../../etc/passwd", "ru/../en", "RU", "ru ua", "ru.txt" };
			string[] expected = new string[5] { "etcpasswd", "ruen", "ru", "ruua", "rutxt" };
			for (int i = 0; i < hostile.Length; i++)
			{
				string text = Language.SanitizeCode(hostile[i]);
				if (text != expected[i])
				{
					return Fail("code '" + hostile[i] + "' became '" + text + "', expected '" + expected[i] + "'");
				}
			}
			if (Language.SanitizeCode("verylonglanguagecode").Length > 12)
			{
				return Fail("a code longer than 12 characters was not truncated");
			}
			// Cycling: the custom option is reachable and wraps back to Auto.
			if (Language.Next(Language.Max, 1) != Language.Auto)
			{
				return Fail("cycling past the last language does not wrap to Auto");
			}
			if (Language.Next(Language.Custom, -1) != 5)
			{
				return Fail("cycling back from the custom language does not reach Español");
			}
			// The path the player actually takes: Language=6 in Settings.ini at startup. This is
			// the exact configuration that crashed the game, so it is read from a file rather than
			// set by hand.
			string text2 = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-custom-lang.ini");
			try
			{
				Settings settings = new Settings(text2);
				settings.Set("Display", "Language", "6", "");
				settings.Set("Display", "CustomLanguage", "ru", "");
				settings.Close();
				Settings loaded = new Settings(text2);
				Language.Load(loaded);
				loaded.Discard();
				if (Language.Choice != Language.Custom)
				{
					return Fail("Language=6 was not read back as the custom choice (got " + Language.Choice + ")");
				}
				if (Language.Effective == Language.Custom || Language.Effective < 1)
				{
					return Fail("Language=6 at startup left the built-in language at " + Language.Effective);
				}
				if (string.IsNullOrEmpty(Lang.ItemName(1)))
				{
					return Fail("Language=6 at startup made item names empty");
				}
				if (Language.CustomCode != "ru")
				{
					return Fail("the custom language code was not read: '" + Language.CustomCode + "'");
				}
			}
			finally
			{
				try
				{
					if (System.IO.File.Exists(text2))
					{
						System.IO.File.Delete(text2);
					}
				}
				catch
				{
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("custom language check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			LangOverrides.SuppressFiles = false;
			Language.Restore(savedChoice, savedOption);
		}
	}

	// --- Server cards -------------------------------------------------------

	/// <summary>
	/// The two-line server listing.
	///
	/// Two things are being checked, and the second is the one that bites: the wording (every
	/// caveat appears exactly once, in a line that stays readable) and the geometry (a click on
	/// the second line of a card selects that card, not the next one). The card is twice as tall
	/// as a world row, and sharing one row height between the two lists is how a hit-test ends up
	/// pointing at the wrong server.
	/// </summary>
	private static bool CheckServerCards()
	{
		try
		{
			Microsoft.Xna.Framework.Net.NetworkSessionProperties properties = new Microsoft.Xna.Framework.Net.NetworkSessionProperties();
			Microsoft.Xna.Framework.Net.QualityOfService qos = new Microsoft.Xna.Framework.Net.QualityOfService();
			// A plain direct server: address on the second line, nothing else to warn about.
			Microsoft.Xna.Framework.Net.AvailableNetworkSession available =
				new Microsoft.Xna.Framework.Net.AvailableNetworkSession(0, "Bob", 0, 7, properties, qos);
			available.HostGamertag = "Bob";
			available.CurrentGamerCount = 2;
			available.OpenPublicGamerSlots = 5;
			JoinableSession session = new JoinableSession(available);
			ServerCard.Card card = ServerCard.Build(0, session, "203.0.113.7:7777", favorite: false, "42 ms");
			if (card.Title != "1. Bob")
			{
				return Fail("the card title is '" + card.Title + "', expected '1. Bob'");
			}
			if (card.Detail != "203.0.113.7:7777")
			{
				return Fail("a plain server's detail line is '" + card.Detail + "'");
			}
			if (card.Incompatible)
			{
				return Fail("a plain server was marked incompatible");
			}
			// A favorite gets the star, and the number stays first: the number is what the D-pad
			// counts with, so it has to be in the same place on every row.
			ServerCard.Card favorited = ServerCard.Build(11, session, "203.0.113.7:7777", favorite: true, "");
			if (favorited.Title != "12. * Bob")
			{
				return Fail("a favorite's title is '" + favorited.Title + "'");
			}
			// Every caveat, at once: the old one-line form was unreadable exactly here.
			available.Passworded = true;
			available.ModMismatch = true;
			available.ProtocolVersion = Terraria.Net.TcpPeer.ProtocolVersionPublic + 1;
			ServerCard.Card busy = ServerCard.Build(0, session, "203.0.113.7:7777", favorite: false, "");
			if (!busy.Incompatible)
			{
				return Fail("a server with different mods and a different protocol was not marked incompatible");
			}
			string[] expected = new string[4]
			{
				"203.0.113.7:7777",
				Texts.Get("ui.net.card.password"),
				Texts.Get("ui.net.card.mods"),
				Texts.Format("ui.net.card.version",
					(Terraria.Net.TcpPeer.ProtocolVersionPublic + 1).ToString(),
					Terraria.Net.TcpPeer.ProtocolVersionPublic.ToString())
			};
			for (int i = 0; i < expected.Length; i++)
			{
				if (busy.Detail.IndexOf(expected[i], StringComparison.Ordinal) < 0)
				{
					return Fail("the detail line is missing '" + expected[i] + "': " + busy.Detail);
				}
			}
			// The version warning names both numbers: "protocol 4" alone does not say what this
			// build speaks, and the difference is the whole point.
			if (busy.Detail.IndexOf("protocol", StringComparison.Ordinal) < 0)
			{
				return Fail("the version warning does not mention the protocol: " + busy.Detail);
			}
			// A relayed server has no address to show, so it says so instead - and never shows a
			// stale address next to it.
			available.ModMismatch = false;
			available.ProtocolVersion = 0;
			available.Passworded = false;
			available.RelayRoom = "room01";
			ServerCard.Card relayed = ServerCard.Build(0, session, "203.0.113.7:7777", favorite: false, "");
			if (relayed.Detail != Texts.Get("ui.net.card.relay"))
			{
				return Fail("a relayed server's detail line is '" + relayed.Detail + "'");
			}
			if (relayed.Detail.IndexOf("203.0.113.7", StringComparison.Ordinal) >= 0)
			{
				return Fail("a relayed server still shows a direct address it does not have");
			}
			// A null session is a placeholder row, not a crash.
			ServerCard.Card empty = ServerCard.Build(3, null, null, favorite: false, "");
			if (empty.Title != "4." || empty.Detail.Length != 0)
			{
				return Fail("an empty card is '" + empty.Title + "' / '" + empty.Detail + "'");
			}

			// --- geometry ----------------------------------------------------
			// The card is twice a world row, so the same panel holds half as many servers.
			int rowHeight = 48;
			int panelHeight = 446 - 80;
			int worldRows = Terraria.Storage.SaveSlots.VisibleRows(panelHeight, rowHeight);
			int cardRows = Terraria.Storage.SaveSlots.VisibleRows(panelHeight, rowHeight * 2);
			if (cardRows * 2 > worldRows + 1)
			{
				return Fail("cards do not halve the visible rows: " + cardRows + " cards vs " + worldRows + " worlds");
			}
			if (cardRows < 1)
			{
				return Fail("no server card fits in the panel");
			}
			// A click anywhere in a card selects that card, and a click in the second line is
			// still the same card - this is the mistake the separate row height exists to avoid.
			int cardHeight = rowHeight * 2;
			int areaTop = 100;
			for (int cardIndex = 0; cardIndex < cardRows; cardIndex++)
			{
				for (int offset = 0; offset < cardHeight; offset++)
				{
					int clicked = WorldSelect.RowAt(areaTop + cardIndex * cardHeight + offset, areaTop, 0, cardHeight);
					if (clicked != cardIndex)
					{
						return Fail("a click at card " + cardIndex + " +" + offset + " px hit card " + clicked);
					}
				}
			}
			// The same function with the world list's row height must still be right: the two
			// lists differ only by that number.
			for (int worldIndex = 0; worldIndex < worldRows; worldIndex++)
			{
				int clicked2 = WorldSelect.RowAt(areaTop + worldIndex * rowHeight, areaTop, 0, rowHeight);
				if (clicked2 != worldIndex)
				{
					return Fail("a click on world row " + worldIndex + " hit row " + clicked2);
				}
			}
			// A scrolled list: the row start is added, so the click still lands on the right entry.
			int scrolled = WorldSelect.RowAt(areaTop + 3 * cardHeight + 10, areaTop, 7, cardHeight);
			if (scrolled != 10)
			{
				return Fail("a scrolled click landed on row " + scrolled + ", expected 10");
			}
			// A zero row height must not divide by zero.
			if (WorldSelect.RowAt(areaTop + 5, areaTop, 2, 0) != 2)
			{
				return Fail("a zero row height broke the row lookup");
			}
			// Scrolling with cards: the last card must be reachable and the window must not run
			// past the end.
			int total = 23;
			int top = Terraria.Storage.SaveSlots.ClampScroll(0, total - 1, cardRows, total);
			if (top != total - cardRows)
			{
				return Fail("scrolling to the last card left the window at " + top + ", expected " + (total - cardRows));
			}
			if (Terraria.Storage.SaveSlots.ClampScroll(999, 0, cardRows, total) != 0)
			{
				return Fail("the scroll window ran past the end");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("server card check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Edit budget --------------------------------------------------------

	/// <summary>
	/// How fast one player may change the world.
	///
	/// The numbers are the interesting part, not the mechanism: too tight and a drill or a
	/// dynamite blast is refused, too loose and a flooder gets through. So the honest cases are
	/// checked as explicitly as the abusive one - an endgame mining run and a blast that clears a
	/// hundred tiles must both pass, and a client sending edits as fast as it can must not.
	/// </summary>
	private static bool CheckEditBudget()
	{
		try
		{
			// --- the bucket itself -------------------------------------------
			TokenBucket tokenBucket = new TokenBucket(10.0, 5.0, 0L);
			if (tokenBucket.CreditAt(0L) != 10.0)
			{
				return Fail("a new bucket is not full");
			}
			for (int i = 0; i < 10; i++)
			{
				if (!tokenBucket.TryTake(1, 0L))
				{
					return Fail("the bucket refused edit " + i + " of its own capacity");
				}
			}
			if (tokenBucket.TryTake(1, 0L))
			{
				return Fail("the bucket handed out more than its capacity");
			}
			// Refusing must not charge: a client asking for too much cannot drain itself.
			if (tokenBucket.CreditAt(0L) != 0.0)
			{
				return Fail("a refused take changed the credit");
			}
			// Refill: 5 per second, so 400 ms is 2 edits.
			if (!tokenBucket.TryTake(2, 400L))
			{
				return Fail("the bucket did not refill");
			}
			if (tokenBucket.TryTake(1, 400L))
			{
				return Fail("the bucket refilled more than the elapsed time allows");
			}
			// The cap: waiting a minute must not bank a minute's worth.
			if (tokenBucket.CreditAt(60000L) != 10.0)
			{
				return Fail("credit exceeded the capacity after a long wait");
			}
			// A clock that goes backwards must not hand out credit.
			TokenBucket tokenBucket2 = new TokenBucket(10.0, 5.0, 1000L);
			tokenBucket2.TryTake(10, 1000L);
			if (tokenBucket2.TryTake(1, 500L))
			{
				return Fail("a backwards clock refilled the bucket");
			}
			// A cost above the capacity is refused, not clamped.
			TokenBucket tokenBucket3 = new TokenBucket(4.0, 1.0, 0L);
			if (tokenBucket3.TryTake(5, 0L))
			{
				return Fail("a cost above the capacity was accepted");
			}

			// --- the honest cases --------------------------------------------
			// Driven with a synthetic clock: the point is the constants, and ten seconds of real
			// sleeping in a self-test is ten seconds nobody gets back.
			TokenBucket honest = new TokenBucket(Terraria.Net.EditBudget.BurstEdits,
				Terraria.Net.EditBudget.EditsPerSecond, 0L);
			// An endgame drill: 20 tiles a second for 10 seconds, one edit every 50 ms.
			for (int j = 0; j < 200; j++)
			{
				if (!honest.TryTake(1, j * 50L))
				{
					return Fail("honest mining was rate-limited at edit " + j);
				}
			}
			// A dynamite blast right after it: about 150 tiles in one burst, no time passing.
			for (int k = 0; k < 150; k++)
			{
				if (!honest.TryTake(1, 10000L))
				{
					return Fail("a dynamite-sized burst was rate-limited at tile " + k);
				}
			}

			// --- the abusive case --------------------------------------------
			Terraria.Net.EditBudget.ResetAll();
			// Assigned by the calls below; the initialiser is for the compiler, which cannot
			// prove a loop body ran.
			string reason = null;
			int accepted = 0;
			for (int l = 0; l < 5000; l++)
			{
				if (Terraria.Net.EditBudget.AllowsEdit("Flooder", out reason))
				{
					accepted++;
				}
			}
			if (accepted > Terraria.Net.EditBudget.BurstEdits + 20)
			{
				return Fail("a flooder got " + accepted + " edits through in one instant, expected about "
					+ Terraria.Net.EditBudget.BurstEdits);
			}
			if (accepted < 100)
			{
				return Fail("the budget is so tight that even a burst is refused (" + accepted + ")");
			}
			if (reason == null || reason.IndexOf("faster", StringComparison.Ordinal) < 0)
			{
				return Fail("a rate-limited edit gave no usable reason: " + reason);
			}
			// Sections have their own, much smaller budget.
			Terraria.Net.EditBudget.ResetAll();
			int sections = 0;
			for (int m = 0; m < 500; m++)
			{
				if (Terraria.Net.EditBudget.AllowsSection("Flooder", out reason))
				{
					sections++;
				}
			}
			if (sections != Terraria.Net.EditBudget.BurstSections)
			{
				return Fail("a section flooder got " + sections + " section(s), expected "
					+ Terraria.Net.EditBudget.BurstSections);
			}
			// ...and spending the section budget must not spend the tile budget.
			if (!Terraria.Net.EditBudget.AllowsEdit("Flooder", out reason))
			{
				return Fail("a section flood also used up the tile budget");
			}
			// Budgets are per player: one flooder must not silence anybody else.
			if (!Terraria.Net.EditBudget.AllowsEdit("Innocent", out reason))
			{
				return Fail("one player's budget affected another's");
			}
			// Leaving and rejoining starts clean.
			Terraria.Net.EditBudget.Reset("Flooder");
			if (!Terraria.Net.EditBudget.AllowsEdit("Flooder", out reason))
			{
				return Fail("a rejoining player inherited the old budget");
			}
			// The refusal notice is throttled, or the flood becomes a chat flood.
			Terraria.Net.EditBudget.ResetAll();
			Terraria.Net.EditBudget.AllowsEdit("Spam", out reason);
			if (!Terraria.Net.EditBudget.ShouldNotify("Spam"))
			{
				return Fail("the first refusal was not announced");
			}
			if (Terraria.Net.EditBudget.ShouldNotify("Spam"))
			{
				return Fail("refusals are not throttled");
			}
			Terraria.Net.EditBudget.ResetAll();
			if (Terraria.Net.EditBudget.TrackedPlayers != 0)
			{
				return Fail("ResetAll left budgets behind");
			}
			// An unnamed sender is let through: refusing would break play against a state that
			// the handshake says cannot happen.
			if (!Terraria.Net.EditBudget.AllowsEdit(null, out reason))
			{
				return Fail("an unnamed sender was rate-limited");
			}

			// --- the composition: permission first, budget second ------------
			// A player who may not build must not spend their rate budget finding that out.
			// This ordering is the reason MayEdit exists instead of two calls at the call site.
			Terraria.Net.EditBudget.ResetAll();
			bool savedDisable = Terraria.Net.WorldProtection.DisableBuild;
			try
			{
				Terraria.Net.WorldProtection.DisableBuild = true;
				for (int n = 0; n < 300; n++)
				{
					bool rateLimited;
					if (Terraria.Net.WorldProtection.MayEdit("Guest", 500, 500, isSection: false, out reason, out rateLimited))
					{
						return Fail("an edit was allowed while building was disabled");
					}
					if (rateLimited)
					{
						return Fail("a permission refusal was reported as a rate limit");
					}
				}
				// 300 refusals later the budget is untouched, so lifting the ban leaves a player
				// who can still build.
				Terraria.Net.WorldProtection.DisableBuild = false;
				bool flag;
				if (!Terraria.Net.WorldProtection.MayEdit("Guest", 500, 500, isSection: false, out reason, out flag))
				{
					return Fail("a permission refusal spent the rate budget: " + reason);
				}
				if (flag)
				{
					return Fail("an allowed edit was reported as rate-limited");
				}
				// And the other way round: a rate limit is not reported as a permission problem.
				for (int num = 0; num < 400; num++)
				{
					Terraria.Net.WorldProtection.MayEdit("Guest", 500, 500, isSection: false, out reason, out flag);
				}
				if (Terraria.Net.WorldProtection.MayEdit("Guest", 500, 500, isSection: false, out reason, out flag))
				{
					return Fail("the budget never ran out through MayEdit");
				}
				if (!flag)
				{
					return Fail("a rate limit was reported as a permission problem");
				}
			}
			finally
			{
				Terraria.Net.WorldProtection.DisableBuild = savedDisable;
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("edit budget check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Net.EditBudget.ResetAll();
		}
	}

	private static void ThreadSleep(int milliseconds)
	{
		System.Threading.Thread.Sleep(milliseconds);
	}

	// --- Packet batching ----------------------------------------------------

	/// <summary>
	/// Batching of game data: many frames in, one write out, same bytes on the wire.
	///
	/// The risk of this optimisation is not speed, it is ordering and loss - a frame held in a
	/// buffer that is never flushed, or an immediate frame overtaking buffered ones. Both are
	/// invisible in play until they are not, so they are checked here: the frames must come back
	/// out in the order they went in, byte for byte, and only game data may be held at all.
	/// </summary>
	private static bool CheckPacketBatching()
	{
		try
		{
			// Only game data is batchable. A handshake or a reject held until the next tick
			// would be a frame something is waiting on.
			byte[] data = WireFormat.Frame(TcpPeer.OpData, new byte[3] { 1, 2, 3 });
			byte[] ping = WireFormat.Frame(10, new byte[4]);
			byte[] roster = WireFormat.Frame(3, new byte[1]);
			if (!PacketBufferer.IsBatchable(data, TcpPeer.OpData))
			{
				return Fail("game data is not considered batchable");
			}
			if (PacketBufferer.IsBatchable(ping, TcpPeer.OpData))
			{
				return Fail("a ping frame is batched");
			}
			if (PacketBufferer.IsBatchable(roster, TcpPeer.OpData))
			{
				return Fail("a roster frame is batched");
			}
			if (PacketBufferer.IsBatchable(null, TcpPeer.OpData))
			{
				return Fail("a null frame is batched");
			}
			// Twenty frames, one write, all of them readable back in order.
			byte[] array3 = WireFormat.Frame(TcpPeer.OpData, new byte[2]);
			PacketBufferer bufferer = new PacketBufferer();
			for (int i = 0; i < 20; i++)
			{
				if (bufferer.Add(WireFormat.Frame(TcpPeer.OpData, new byte[2] { (byte)i, (byte)(i * 2) })))
				{
					return Fail("the buffer asked for a flush long before it was full");
				}
			}
			if (bufferer.PendingFrames != 20)
			{
				return Fail("the buffer holds " + bufferer.PendingFrames + " frame(s), expected 20");
			}
			using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
			{
				int frames;
				int num = bufferer.Flush(memoryStream, out frames);
				if (frames != 20)
				{
					return Fail("the flush reported " + frames + " frame(s), expected 20");
				}
				if (num != 20 * array3.Length)
				{
					return Fail("the flush wrote " + num + " byte(s), expected " + 20 * array3.Length);
				}
				if (bufferer.PendingFrames != 0 || bufferer.PendingBytes != 0)
				{
					return Fail("the buffer is not empty after a flush");
				}
				memoryStream.Position = 0L;
				for (int j = 0; j < 20; j++)
				{
					byte[] array = WireFormat.ReadFrame(memoryStream);
					if (array == null || array.Length != 3 || array[0] != TcpPeer.OpData
						|| array[1] != (byte)j || array[2] != (byte)(j * 2))
					{
						return Fail("frame " + j + " did not survive the batch");
					}
				}
				if (WireFormat.ReadFrame(memoryStream) != null)
				{
					return Fail("the batch produced more frames than were put in");
				}
				// A second flush with nothing pending writes nothing at all - otherwise every
				// tick would cost a syscall for zero bytes.
				int num2 = bufferer.Flush(memoryStream, out frames);
				if (num2 != 0 || frames != 0)
				{
					return Fail("an empty flush wrote " + num2 + " byte(s)");
				}
			}
			// The cap: a burst larger than the limit asks for an immediate flush instead of
			// growing without bound.
			PacketBufferer packetBufferer = new PacketBufferer();
			byte[] array2 = WireFormat.Frame(TcpPeer.OpData, new byte[8 * 1024]);
			bool flag = false;
			for (int k = 0; k < 8 && !flag; k++)
			{
				flag = packetBufferer.Add(array2);
			}
			if (!flag)
			{
				return Fail("the buffer never reached its cap");
			}
			if (packetBufferer.PendingBytes < PacketBufferer.MaxPendingBytes)
			{
				return Fail("the buffer reported a flush before reaching the cap");
			}
			// Clear drops what is held, for a connection that is going away.
			packetBufferer.Clear();
			if (packetBufferer.PendingBytes != 0 || packetBufferer.PendingFrames != 0)
			{
				return Fail("Clear left data behind");
			}
			// A null stream must not throw: the host path calls Flush on connections that may
			// already be closed.
			PacketBufferer packetBufferer2 = new PacketBufferer();
			packetBufferer2.Add(data);
			int frames2;
			if (packetBufferer2.Flush(null, out frames2) != 0)
			{
				return Fail("flushing to a null stream reported bytes");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("packet batching check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- UPnP ---------------------------------------------------------------

	/// <summary>
	/// Port forwarding over UPnP, against a stub gateway.
	///
	/// A router is the one thing a test run cannot have, so the whole exchange is driven against
	/// a stub that speaks the same three protocols: SSDP discovery, the device description and
	/// SOAP control. That covers the parts that actually break in the wild - the field names
	/// routers are strict about, relative control URLs, a router that answers with a fault, and
	/// the case where the port is already forwarded to a different machine.
	/// </summary>
	private static bool CheckUPnP()
	{
		try
		{
			// --- pure parsing -------------------------------------------------
			string text = "HTTP/1.1 200 OK\r\nCACHE-CONTROL: max-age=120\r\nlocation: http://10.0.0.1:5000/rootDesc.xml\r\nST: upnp:rootdevice\r\n\r\n";
			if (Terraria.Net.UPnP.FindLocation(text) != "http://10.0.0.1:5000/rootDesc.xml")
			{
				return Fail("the SSDP reply's location was not found");
			}
			if (Terraria.Net.UPnP.FindLocation("HTTP/1.1 200 OK\r\n\r\n") != null)
			{
				return Fail("an SSDP reply without a location was accepted");
			}
			string text2 = "<root><device><deviceList><device><serviceList><service>"
				+ "<serviceType>urn:schemas-upnp-org:service:WANIPConnection:1</serviceType>"
				+ "<controlURL>/ctl/IPConn</controlURL>"
				+ "</service></serviceList></device></deviceList></device></root>";
			Terraria.Net.UPnP.Gateway gateway = Terraria.Net.UPnP.ParseDescription(text2, "http://10.0.0.1:5000/rootDesc.xml");
			if (gateway == null)
			{
				return Fail("the device description was not understood");
			}
			if (gateway.ControlUrl != "http://10.0.0.1:5000/ctl/IPConn")
			{
				return Fail("a relative control URL was not resolved: " + gateway.ControlUrl);
			}
			// The PPP variant is the only service some routers expose.
			string text3 = text2.Replace("WANIPConnection", "WANPPPConnection");
			Terraria.Net.UPnP.Gateway gateway2 = Terraria.Net.UPnP.ParseDescription(text3, "http://10.0.0.1:5000/rootDesc.xml");
			if (gateway2 == null || gateway2.ServiceType.IndexOf("WANPPPConnection", StringComparison.Ordinal) < 0)
			{
				return Fail("a WANPPPConnection gateway was not recognised");
			}
			// A description with no WAN service is "not a router", not a crash.
			string text4 = "<root><device><serviceList><service><serviceType>urn:schemas-upnp-org:service:Layer3Forwarding:1</serviceType><controlURL>/x</controlURL></service></serviceList></device></root>";
			if (Terraria.Net.UPnP.ParseDescription(text4, "http://10.0.0.1:5000/rootDesc.xml") != null)
			{
				return Fail("a description without a WAN service produced a gateway");
			}
			if (Terraria.Net.UPnP.ParseDescription("not xml at all", "http://10.0.0.1/") != null)
			{
				return Fail("garbage was accepted as a device description");
			}
			string text5 = Terraria.Net.UPnP.BuildAddPortMapping("urn:schemas-upnp-org:service:WANIPConnection:1", 7777, 7777, "192.168.1.5", "TerrariaXDK", 0);
			string[] array = new string[9]
			{
				"AddPortMapping", "<NewExternalPort>7777</NewExternalPort>", "<NewProtocol>TCP</NewProtocol>",
				"<NewInternalPort>7777</NewInternalPort>", "<NewInternalClient>192.168.1.5</NewInternalClient>",
				"<NewEnabled>1</NewEnabled>", "<NewLeaseDuration>0</NewLeaseDuration>",
				"<NewPortMappingDescription>TerrariaXDK</NewPortMappingDescription>", "<NewRemoteHost></NewRemoteHost>"
			};
			for (int i = 0; i < array.Length; i++)
			{
				if (!text5.Contains(array[i]))
				{
					return Fail("the AddPortMapping body is missing " + array[i]);
				}
			}
			int num;
			string text6;
			if (!Terraria.Net.UPnP.IsFault("<s:Envelope><s:Body><s:Fault><detail><UPnPError><errorCode>718</errorCode><errorDescription>ConflictInMappingEntry</errorDescription></UPnPError></detail></s:Fault></s:Body></s:Envelope>", out num, out text6))
			{
				return Fail("a SOAP fault was not recognised");
			}
			if (num != 718 || text6 != "ConflictInMappingEntry")
			{
				return Fail("the fault code was misread: " + num + " " + text6);
			}
			if (Terraria.Net.UPnP.IsFault("<s:Envelope><s:Body><u:AddPortMappingResponse/></s:Body></s:Envelope>", out num, out text6))
			{
				return Fail("a success reply was read as a fault");
			}
			if (Terraria.Net.UPnP.ParseExternalAddress("<s:Envelope><s:Body><u:GetExternalIPAddressResponse><NewExternalIPAddress>203.0.113.7</NewExternalIPAddress></u:GetExternalIPAddressResponse></s:Body></s:Envelope>") != "203.0.113.7")
			{
				return Fail("the external address was not parsed");
			}
			if (Terraria.Net.UPnP.ParseExternalAddress("<NewExternalIPAddress>0.0.0.0</NewExternalIPAddress>") != null)
			{
				return Fail("0.0.0.0 was reported as a real external address");
			}

			// --- the whole exchange against a stub gateway --------------------
			StubGateway stubGateway = new StubGateway();
			try
			{
				string text7;
				Terraria.Net.UPnP.Gateway gateway3 = Terraria.Net.UPnP.Discover(2000, out text7,
					"127.0.0.1", stubGateway.SsdpPort);
				if (gateway3 == null)
				{
					return Fail("discovery did not find the stub gateway: " + text7);
				}
				string text8;
				bool flag2;
				if (!Terraria.Net.UPnP.Map(gateway3, 7777, "192.168.1.5", out flag2, out text8))
				{
					return Fail("mapping through the stub gateway failed: " + text8);
				}
				if (!flag2)
				{
					return Fail("a mapping this call created was not reported as ours");
				}
				if (stubGateway.LastMapping == null || !stubGateway.LastMapping.Contains("<NewExternalPort>7777</NewExternalPort>"))
				{
					return Fail("the gateway did not receive the mapping");
				}
				if (Terraria.Net.UPnP.ExternalAddress(gateway3, out text8) != "203.0.113.7")
				{
					return Fail("the external address did not come back: " + text8);
				}
				if (!Terraria.Net.UPnP.Unmap(gateway3, 7777, out text8) || !stubGateway.SawDelete)
				{
					return Fail("the mapping was not removed: " + text8);
				}
				// A fault arrives as HTTP 500 with a body, which must not be mistaken for a
				// transport failure - routers do this and it is the difference between "denied"
				// and "unreachable".
				stubGateway.FaultOnAdd = 501;
				if (Terraria.Net.UPnP.Map(gateway3, 7777, "192.168.1.5", out flag2, out text8))
				{
					return Fail("a router that refused the mapping was reported as success");
				}
				if (text8 == null || text8.IndexOf("501", StringComparison.Ordinal) < 0)
				{
					return Fail("the refusal did not name the router's error code: " + text8);
				}
				// 718: the port is taken. It counts as open only if it points at this machine,
				// and a mapping found there is not ours to delete later.
				stubGateway.FaultOnAdd = 718;
				stubGateway.ExistingClient = "192.168.1.5";
				if (!Terraria.Net.UPnP.Map(gateway3, 7777, "192.168.1.5", out flag2, out text8))
				{
					return Fail("an existing mapping to this machine was not accepted: " + text8);
				}
				if (flag2)
				{
					return Fail("an existing mapping was reported as one this run created");
				}
				stubGateway.ExistingClient = "192.168.1.9";
				if (Terraria.Net.UPnP.Map(gateway3, 7777, "192.168.1.5", out flag2, out text8))
				{
					return Fail("a port forwarded to another machine was reported as open");
				}
				if (text8 == null || text8.IndexOf("192.168.1.9", StringComparison.Ordinal) < 0)
				{
					return Fail("the conflict did not say where the port actually goes: " + text8);
				}
				stubGateway.FaultOnAdd = 0;
				stubGateway.ExistingClient = null;
			}
			finally
			{
				stubGateway.Stop();
			}
			// Nothing listening: a clear "no gateway", not a hang or a crash.
			string text9;
			if (Terraria.Net.UPnP.Discover(200, out text9, "127.0.0.1", 9) != null)
			{
				return Fail("a gateway was found where nothing is listening");
			}
			if (text9 == null || text9.Length == 0)
			{
				return Fail("a failed discovery gave no reason");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("UPnP check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Virtual keyboard ---------------------------------------------------

	/// <summary>
	/// The on-screen keyboard is the only way to type on a handheld, so its buffer rules are
	/// worth pinning down: append, delete, length limit, and that opening it suppresses game
	/// input (otherwise typing walks the player around).
	/// </summary>
	private static bool CheckVirtualKeyboard()
	{
		bool flag = Terraria.Input.KeyboardMouseInput.TextInputActive;
		try
		{
			if (Terraria.Input.VirtualKeyboard.IsOpen)
			{
				Terraria.Input.VirtualKeyboard.Close();
			}
			Terraria.Input.VirtualKeyboard.Open("Test", "abc", null);
			if (!Terraria.Input.VirtualKeyboard.IsOpen)
			{
				return Fail("keyboard did not open");
			}
			if (Terraria.Input.VirtualKeyboard.Text != "abc")
			{
				return Fail("initial text was not loaded (" + Terraria.Input.VirtualKeyboard.Text + ")");
			}
			if (!Terraria.Input.KeyboardMouseInput.TextInputActive)
			{
				return Fail("opening the keyboard does not suppress game input");
			}
			Terraria.Input.VirtualKeyboard.TypeChar('d');
			if (Terraria.Input.VirtualKeyboard.Text != "abcd")
			{
				return Fail("TypeChar did not append");
			}
			Terraria.Input.VirtualKeyboard.DeleteLast();
			if (Terraria.Input.VirtualKeyboard.Text != "abc")
			{
				return Fail("DeleteLast did not remove the last character");
			}
			// Deleting past the start is a no-op, not a crash.
			Terraria.Input.VirtualKeyboard.SetText("x");
			Terraria.Input.VirtualKeyboard.DeleteLast();
			Terraria.Input.VirtualKeyboard.DeleteLast();
			if (Terraria.Input.VirtualKeyboard.Text != "")
			{
				return Fail("DeleteLast broke on an empty buffer");
			}
			// Length limit.
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			for (int i = 0; i < 300; i++)
			{
				stringBuilder.Append('a');
			}
			Terraria.Input.VirtualKeyboard.SetText(stringBuilder.ToString());
			Terraria.Input.VirtualKeyboard.TypeChar('b');
			if (Terraria.Input.VirtualKeyboard.Text.Length != 300)
			{
				return Fail("length limit was not enforced");
			}
			Terraria.Input.VirtualKeyboard.Close();
			if (Terraria.Input.VirtualKeyboard.IsOpen)
			{
				return Fail("keyboard did not close");
			}
			if (Terraria.Input.KeyboardMouseInput.TextInputActive)
			{
				return Fail("closing the keyboard left game input suppressed");
			}
			return true;
		}
		finally
		{
			Terraria.Input.VirtualKeyboard.Close();
			Terraria.Input.KeyboardMouseInput.TextInputActive = flag;
			Terraria.Input.KeyboardMouseInput.SuppressGameInput = false;
		}
	}

	// --- HUD settings -------------------------------------------------------

	/// <summary>
	/// Zoom used to be parsed with <c>Convert.ToSingle</c> and applied nowhere: a bad value in
	/// the file crashed the game before the menu, and a good one did nothing. Both halves are
	/// pinned here, plus the step arithmetic (float accumulation drifts) and the clamping.
	/// </summary>
	private static bool CheckHudSettings()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-hud.ini");
		float zoom = HudSettings.Zoom;
		bool watch = HudSettings.AlwaysWatch;
		bool depth = HudSettings.AlwaysDepth;
		bool compass = HudSettings.AlwaysCompass;
		try
		{
			// File semantics: 0 = default zoom, 1 = closest.
			if (HudSettings.ParseZoom("0") != HudSettings.MinZoom || HudSettings.ParseZoom("1") != HudSettings.MaxZoom)
			{
				return Fail("the zoom offset does not map to 1.0x .. 2.0x");
			}
			if (HudSettings.ParseZoom("0.5") != 1.5f)
			{
				return Fail("zoom 0.5 did not map to 1.5x");
			}
			// Bad input must not throw: this used to be Convert.ToSingle at startup.
			string[] bad = new string[5]
			{
				"abc",
				"",
				"-5",
				"99",
				null
			};
			for (int i = 0; i < bad.Length; i++)
			{
				float parsed = HudSettings.ParseZoom(bad[i]);
				if (parsed < HudSettings.MinZoom || parsed > HudSettings.MaxZoom)
				{
					return Fail("a bad zoom value ('" + bad[i] + "') produced " + parsed);
				}
			}
			if (HudSettings.ParseZoom("-5") != HudSettings.MinZoom || HudSettings.ParseZoom("99") != HudSettings.MaxZoom)
			{
				return Fail("zoom values were not clamped to the allowed range");
			}

			// Stepping: the value must land on the step grid and stop at both ends.
			HudSettings.Zoom = HudSettings.MinZoom;
			for (int j = 0; j < 6; j++)
			{
				HudSettings.AdjustZoom(1);
			}
			if (Math.Abs(HudSettings.Zoom - 1.3f) > 0.0001f)
			{
				return Fail("six zoom steps gave " + HudSettings.ZoomLabel + ", expected 1.30x");
			}
			HudSettings.AdjustZoom(-1);
			if (Math.Abs(HudSettings.Zoom - 1.25f) > 0.0001f)
			{
				return Fail("zooming out one step gave " + HudSettings.ZoomLabel);
			}
			// Repeated steps must not drift: 100 steps of +0.05 is exactly the maximum.
			HudSettings.Zoom = HudSettings.MinZoom;
			for (int k = 0; k < 100; k++)
			{
				HudSettings.AdjustZoom(1);
			}
			if (HudSettings.Zoom != HudSettings.MaxZoom)
			{
				return Fail("zoom drifted to " + HudSettings.ZoomLabel + " instead of the maximum");
			}
			if (HudSettings.AdjustZoom(1))
			{
				return Fail("zooming past the maximum reported a change");
			}
			HudSettings.Zoom = HudSettings.MinZoom;
			if (HudSettings.AdjustZoom(-1))
			{
				return Fail("zooming below the default reported a change");
			}
			HudSettings.ResetZoom();
			if (HudSettings.Zoom != HudSettings.MinZoom)
			{
				return Fail("reset did not restore the default zoom");
			}

			// Save/load round-trip, including the HUD switches.
			if (System.IO.File.Exists(text))
			{
				System.IO.File.Delete(text);
			}
			HudSettings.Zoom = 1.4f;
			HudSettings.AlwaysWatch = true;
			HudSettings.AlwaysDepth = false;
			HudSettings.AlwaysCompass = true;
			Settings settings = new Settings(text);
			HudSettings.Save(settings);
			settings.Close();
			HudSettings.Zoom = 1f;
			HudSettings.AlwaysWatch = false;
			HudSettings.AlwaysCompass = false;
			Settings settings2 = new Settings(text);
			HudSettings.Load(settings2);
			settings2.Close();
			if (Math.Abs(HudSettings.Zoom - 1.4f) > 0.005f)
			{
				return Fail("the zoom did not survive a save/load cycle: " + HudSettings.ZoomLabel);
			}
			if (!HudSettings.AlwaysWatch || HudSettings.AlwaysDepth || !HudSettings.AlwaysCompass)
			{
				return Fail("the HUD switches did not survive a save/load cycle");
			}
			// Main forwards to HudSettings, because Player reads Main.AlwaysOnWatch.
			if (Main.AlwaysOnWatch != HudSettings.AlwaysWatch || Main.AlwaysOnCompass != HudSettings.AlwaysCompass)
			{
				return Fail("Main does not expose the HUD switches");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("HUD settings check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			HudSettings.Zoom = zoom;
			HudSettings.AlwaysWatch = watch;
			HudSettings.AlwaysDepth = depth;
			HudSettings.AlwaysCompass = compass;
			try
			{
				if (System.IO.File.Exists(text))
				{
					System.IO.File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}

	// --- Display profiles ---------------------------------------------------

	/// <summary>
	/// Presets exist so the three display fields cannot drift into a combination that looks
	/// wrong. Detection must follow the values (so hand edits show as "Custom"), applying
	/// must be complete, and cycling must never land on "Custom".
	/// </summary>
	private static bool CheckDisplayProfiles()
	{
		int width = DisplaySettings.Width;
		int height = DisplaySettings.Height;
		bool fullscreen = DisplaySettings.Fullscreen;
		ScreenHeights scale = DisplaySettings.RenderScale;
		try
		{
			if (!SettingsProfiles.Apply(SettingsProfiles.Tv))
			{
				return Fail("the TV preset could not be applied");
			}
			if (DisplaySettings.Width != 1920 || DisplaySettings.Height != 1080 || !DisplaySettings.Fullscreen || DisplaySettings.RenderScale != ScreenHeights.HD)
			{
				return Fail("the TV preset applied only part of its values");
			}
			if (SettingsProfiles.Current != SettingsProfiles.Tv)
			{
				return Fail("the TV preset is not detected as current");
			}
			if (!SettingsProfiles.Apply(SettingsProfiles.Handheld))
			{
				return Fail("the handheld preset could not be applied");
			}
			if (DisplaySettings.Width != 1280 || DisplaySettings.Fullscreen || DisplaySettings.RenderScale != ScreenHeights.BASE)
			{
				return Fail("the handheld preset applied only part of its values");
			}
			// Changing one field by hand must not keep claiming the preset.
			DisplaySettings.RenderScale = ScreenHeights.FHD;
			if (SettingsProfiles.Current != SettingsProfiles.Custom)
			{
				return Fail("an edited setting still reports a preset");
			}
			if (SettingsProfiles.Apply(SettingsProfiles.Custom) || SettingsProfiles.Apply("nonsense"))
			{
				return Fail("a non-preset name was applied");
			}
			// Cycling wraps and never selects Custom.
			string next = SettingsProfiles.Next(SettingsProfiles.Default, 1);
			if (next != SettingsProfiles.Tv)
			{
				return Fail("cycling forward from Default gave " + next);
			}
			next = SettingsProfiles.Next(SettingsProfiles.Tv, 1);
			if (next != SettingsProfiles.Handheld)
			{
				return Fail("cycling forward from TV gave " + next);
			}
			next = SettingsProfiles.Next(SettingsProfiles.Handheld, 1);
			if (next != SettingsProfiles.Default)
			{
				return Fail("cycling forward from Handheld did not wrap to Default");
			}
			next = SettingsProfiles.Next(SettingsProfiles.Default, -1);
			if (next != SettingsProfiles.Handheld)
			{
				return Fail("cycling backward from Default gave " + next);
			}
			next = SettingsProfiles.Next(SettingsProfiles.Custom, 1);
			if (next == SettingsProfiles.Custom)
			{
				return Fail("cycling from Custom selected Custom");
			}
			// Every preset must round-trip: apply, detect, and be a sane configuration.
			string[] all = new string[3] { SettingsProfiles.Default, SettingsProfiles.Tv, SettingsProfiles.Handheld };
			for (int i = 0; i < all.Length; i++)
			{
				if (!SettingsProfiles.Apply(all[i]) || SettingsProfiles.Current != all[i])
				{
					return Fail("preset '" + all[i] + "' does not round-trip");
				}
				if (DisplaySettings.Width < 320 || DisplaySettings.Height < 240 || DisplaySettings.Width > 7680)
				{
					return Fail("preset '" + all[i] + "' sets an implausible window size");
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("display profile check threw: " + ex.Message);
		}
		finally
		{
			DisplaySettings.Width = width;
			DisplaySettings.Height = height;
			DisplaySettings.Fullscreen = fullscreen;
			DisplaySettings.RenderScale = scale;
		}
	}

	// --- Display settings ---------------------------------------------------

	/// <summary>
	/// The display options were declared in Settings.ini but never applied. These checks pin
	/// down the mapping from config to resolution and that cycling stays in range.
	/// </summary>
	private static bool CheckDisplaySettings()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-display.ini");
		int savedWidth = Terraria.DisplaySettings.Width;
		int savedHeight = Terraria.DisplaySettings.Height;
		bool savedFullscreen = Terraria.DisplaySettings.Fullscreen;
		ScreenHeights savedScale = Terraria.DisplaySettings.RenderScale;
		bool savedFps = Terraria.DisplaySettings.ShowFPS;
		bool savedVSync = Terraria.DisplaySettings.VSync;
		int savedLimit = Terraria.DisplaySettings.FpsLimit;
		try
		{
			if (System.IO.File.Exists(text))
			{
				System.IO.File.Delete(text);
			}
			Settings settings = new Settings(text);
			settings.Set("Display", "Width", "1920", "");
			settings.Set("Display", "Height", "1080", "");
			settings.Set("Display", "ScreenHeight", "2", "");
			settings.Set("Display", "StartFullscreen", "False", "");
			settings.Close();
			Settings settings2 = new Settings(text);
			Terraria.DisplaySettings.Load(settings2);
			settings2.Close();
			if (Terraria.DisplaySettings.Width != 1920 || Terraria.DisplaySettings.Height != 1080)
			{
				return Fail("window size was not read from settings");
			}
			if (Terraria.DisplaySettings.RenderScale != ScreenHeights.FHD)
			{
				return Fail("render scale was not read from settings");
			}
			if (Terraria.DisplaySettings.Fullscreen)
			{
				return Fail("fullscreen flag was ignored");
			}
			// Cycling must wrap and stay inside the preset list.
			int num = Terraria.DisplaySettings.RenderScale == ScreenHeights.FHD ? 2 : -1;
			if (num != 2)
			{
				return Fail("render scale precondition failed");
			}
			Terraria.DisplaySettings.NextRenderScale(1);
			if (Terraria.DisplaySettings.RenderScale != ScreenHeights.BASE)
			{
				return Fail("render scale does not wrap 1080p -> 540p");
			}
			Terraria.DisplaySettings.NextRenderScale(-1);
			if (Terraria.DisplaySettings.RenderScale != ScreenHeights.FHD)
			{
				return Fail("render scale does not wrap 540p -> 1080p");
			}
			for (int i = 0; i < 12; i++)
			{
				Terraria.DisplaySettings.NextWindowSize(1);
				if (Terraria.DisplaySettings.Width < 320 || Terraria.DisplaySettings.Height < 240)
				{
					return Fail("window size cycling left a sane range");
				}
			}
			// Saving must actually write: Settings.Set only seeds missing keys, so an edited
			// value used to be written back as the old one and the Display screen silently
			// forgot every change.
			Terraria.DisplaySettings.Width = 1600;
			Terraria.DisplaySettings.Height = 900;
			Terraria.DisplaySettings.Fullscreen = true;
			Terraria.DisplaySettings.RenderScale = ScreenHeights.HD;
			Terraria.DisplaySettings.ShowFPS = true;
			Settings settingsSaved = new Settings(text);
			Terraria.DisplaySettings.Save(settingsSaved);
			settingsSaved.Close();
			Terraria.DisplaySettings.Width = 1280;
			Terraria.DisplaySettings.Height = 720;
			Terraria.DisplaySettings.Fullscreen = false;
			Terraria.DisplaySettings.RenderScale = ScreenHeights.BASE;
			Terraria.DisplaySettings.ShowFPS = false;
			Settings settingsReloaded = new Settings(text);
			Terraria.DisplaySettings.Load(settingsReloaded);
			settingsReloaded.Close();
			if (Terraria.DisplaySettings.Width != 1600 || Terraria.DisplaySettings.Height != 900)
			{
				return Fail("the window size was not saved");
			}
			if (!Terraria.DisplaySettings.Fullscreen || Terraria.DisplaySettings.RenderScale != ScreenHeights.HD || !Terraria.DisplaySettings.ShowFPS)
			{
				return Fail("fullscreen, render scale or the FPS flag was not saved");
			}
			// VSync and the frame cap: saved, and only the offered caps are accepted.
			Terraria.DisplaySettings.VSync = false;
			Terraria.DisplaySettings.FpsLimit = 120;
			Settings settingsFrame = new Settings(text);
			Terraria.DisplaySettings.Save(settingsFrame);
			settingsFrame.Close();
			Terraria.DisplaySettings.VSync = true;
			Terraria.DisplaySettings.FpsLimit = 0;
			Settings settingsFrame2 = new Settings(text);
			Terraria.DisplaySettings.Load(settingsFrame2);
			settingsFrame2.Close();
			if (Terraria.DisplaySettings.VSync || Terraria.DisplaySettings.FpsLimit != 120)
			{
				return Fail("vsync or the frame cap was not saved");
			}
			// Cycling wraps through the offered caps and never lands on a value the loader
			// would reject.
			for (int cycle = 0; cycle < 12; cycle++)
			{
				Terraria.DisplaySettings.NextFpsLimit(1);
				if (!Terraria.DisplaySettings.IsSupportedFpsLimit(Terraria.DisplaySettings.FpsLimit))
				{
					return Fail("frame cap cycling produced the unsupported value " + Terraria.DisplaySettings.FpsLimit);
				}
			}
			Terraria.DisplaySettings.FpsLimit = 0;
			Terraria.DisplaySettings.NextFpsLimit(-1);
			if (Terraria.DisplaySettings.FpsLimit != 240)
			{
				return Fail("frame cap cycling backward from off did not wrap to 240");
			}
			// A hand-edited file with a bogus cap must fall back to "off", not to something
			// the game cannot express.
			System.IO.File.WriteAllText(text, "[Display]\r\nFpsLimit=37\r\nVSync=maybe\r\n");
			Settings settingsBogus = new Settings(text);
			Terraria.DisplaySettings.Load(settingsBogus);
			settingsBogus.Close();
			if (Terraria.DisplaySettings.FpsLimit != 0)
			{
				return Fail("an unsupported frame cap was accepted: " + Terraria.DisplaySettings.FpsLimit);
			}
			if (!Terraria.DisplaySettings.VSync)
			{
				return Fail("a non-boolean VSync value turned vsync off");
			}
			// Nonsense values in the file fall back to defaults instead of crashing.
			System.IO.File.WriteAllText(text, "[Display]\r\nWidth=abc\r\nHeight=-5\r\nScreenHeight=99\r\n");
			Settings settings3 = new Settings(text);
			Terraria.DisplaySettings.Load(settings3);
			settings3.Close();
			if (Terraria.DisplaySettings.Width != 1280 || Terraria.DisplaySettings.Height != 720)
			{
				return Fail("invalid window size was not replaced with defaults");
			}
			if (Terraria.DisplaySettings.RenderScale != ScreenHeights.BASE)
			{
				return Fail("invalid render scale was not clamped");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("display settings check failed: " + ex.Message);
		}
		finally
		{
			// The check overwrites the live display state; put it back so the next checks
			// (and a real run after the self-test) see the player's configuration.
			Terraria.DisplaySettings.Width = savedWidth;
			Terraria.DisplaySettings.Height = savedHeight;
			Terraria.DisplaySettings.Fullscreen = savedFullscreen;
			Terraria.DisplaySettings.RenderScale = savedScale;
			Terraria.DisplaySettings.ShowFPS = savedFps;
			Terraria.DisplaySettings.VSync = savedVSync;
			Terraria.DisplaySettings.FpsLimit = savedLimit;
			try
			{
				if (System.IO.File.Exists(text))
				{
					System.IO.File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}

	// --- Direct connect -----------------------------------------------------

	/// <summary>
	/// Manual address entry must reject garbage with a readable message instead of throwing
	/// or silently doing nothing. Only the parsing paths are exercised: no network needed.
	/// </summary>
	private static bool CheckDirectConnect()
	{
		string error;
		if (Netplay.JoinDirect("", out error))
		{
			return Fail("empty address was accepted");
		}
		if (string.IsNullOrEmpty(error))
		{
			return Fail("empty address produced no explanation");
		}
		if (Netplay.JoinDirect("host:99999", out error))
		{
			return Fail("out-of-range port was accepted");
		}
		if (Netplay.JoinDirect("host:abc", out error))
		{
			return Fail("non-numeric port was accepted");
		}
		// A syntactically valid but unresolvable name must fail cleanly.
		if (Netplay.JoinDirect("this-host-should-not-exist.invalid:7777", out error))
		{
			return Fail("unresolvable host was accepted");
		}
		if (string.IsNullOrEmpty(error))
		{
			return Fail("unresolvable host produced no explanation");
		}
		return true;
	}

	// --- World backups ------------------------------------------------------

	/// <summary>
	/// The backup folder used to be an empty placeholder: the only copy of a world was the
	/// single .wld.bak that every save overwrote. These checks pin down that a backup is
	/// actually written, that content matches, that pruning respects the limit and that the
	/// switch turns it off.
	/// </summary>
	private static bool CheckWorldBackups()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-world.wld");
		const string worldName = "selftest_backups";
		bool enabled = Terraria.WorldBackups.Enabled;
		int keep = Terraria.WorldBackups.Keep;
		int minMinutes = Terraria.WorldBackups.MinMinutes;
		try
		{
			byte[] worldBytes = new byte[64];
			worldBytes[0] = 1;
			worldBytes[7] = 8;
			System.IO.File.WriteAllBytes(text, worldBytes);
			CleanupBackups(worldName);
			Terraria.WorldBackups.Enabled = true;
			Terraria.WorldBackups.Keep = 10;
			Terraria.WorldBackups.MinMinutes = 0;
			Terraria.WorldBackups.Backup(text, worldName);
			string[] files = System.IO.Directory.GetFiles(Terraria.WorldBackups.Folder, worldName + "_*.wld");
			if (files.Length != 1)
			{
				return Fail("a save did not produce exactly one backup");
			}
			byte[] bytes = System.IO.File.ReadAllBytes(files[0]);
			if (bytes.Length != 64 || bytes[0] != 1 || bytes[7] != 8)
			{
				return Fail("the backup does not match the world file");
			}
			// Two saves in the same second must not overwrite each other.
			Terraria.WorldBackups.Backup(text, worldName);
			if (System.IO.Directory.GetFiles(Terraria.WorldBackups.Folder, worldName + "_*.wld").Length != 2)
			{
				return Fail("a second save overwrote the first backup");
			}
			// Pruning keeps the newest N and drops the rest.
			Terraria.WorldBackups.Keep = 3;
			for (int i = 0; i < 5; i++)
			{
				Terraria.WorldBackups.BackupNow(text, worldName);
			}
			files = System.IO.Directory.GetFiles(Terraria.WorldBackups.Folder, worldName + "_*.wld");
			if (files.Length != 3)
			{
				return Fail("pruning kept " + files.Length + " backups instead of 3");
			}
			// Disabled means no new files.
			Terraria.WorldBackups.Enabled = false;
			Terraria.WorldBackups.Backup(text, worldName);
			if (System.IO.Directory.GetFiles(Terraria.WorldBackups.Folder, worldName + "_*.wld").Length != 3)
			{
				return Fail("a backup was written while the feature was off");
			}
			Terraria.WorldBackups.Enabled = true;
			// A missing world file must be ignored, not thrown.
			Terraria.WorldBackups.BackupNow(System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "does-not-exist.wld"), worldName);
			return true;
		}
		catch (Exception ex)
		{
			return Fail("world backup check threw: " + ex.Message);
		}
		finally
		{
			Terraria.WorldBackups.Enabled = enabled;
			Terraria.WorldBackups.Keep = keep;
			Terraria.WorldBackups.MinMinutes = minMinutes;
			CleanupBackups(worldName);
			try
			{
				if (System.IO.File.Exists(text))
				{
					System.IO.File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}

	private static void CleanupBackups(string worldName)
	{
		try
		{
			// Enumerate and filter instead of a wildcard pattern: brackets in a world name are
			// pattern syntax to Directory.GetFiles, so "Odd[World]_*" would match nothing.
			string prefix = Terraria.WorldBackups.Sanitize(worldName) + "_";
			string[] files = System.IO.Directory.GetFiles(Terraria.WorldBackups.Folder);
			for (int i = 0; i < files.Length; i++)
			{
				if (System.IO.Path.GetFileName(files[i]).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					System.IO.File.Delete(files[i]);
				}
			}
		}
		catch
		{
		}
	}

	// --- Session record -----------------------------------------------------

	/// <summary>
	/// The decompiled AvailableNetworkSession had every property reading and writing its
	/// own name, so the first touch recursed until the process died. Constructing a record
	/// and reading it back is the regression test; if this ever regresses, the self-test
	/// crashes instead of failing, which is still a loud-enough signal.
	/// </summary>
	private static bool CheckSessionRecord()
	{
		try
		{
			AvailableNetworkSession session = new AvailableNetworkSession(0, "host.example", 0, 7, new NetworkSessionProperties(), new QualityOfService());
			session.SessionType = NetworkSessionType.SystemLink;
			session.EndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 7777);
			session.CurrentGamerCount = 3;
			JoinableSession joinable = new JoinableSession(session);
			if (joinable.HostTag != "host.example" || joinable.PlayerCount != 3)
			{
				return Fail("the session record did not keep its values");
			}
			if (joinable.AvailableSession.EndPoint.Port != 7777)
			{
				return Fail("the session record lost its endpoint");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("session record round-trip threw: " + ex.Message);
		}
	}

	// --- JSON ---------------------------------------------------------------

	private static bool CheckJson()
	{
		Dictionary<string, object> obj = Json.Parse("{\"a\":1,\"b\":[true,null,\"x\\ny\"],\"c\":{\"d\":-2.5}}") as Dictionary<string, object>;
		if (obj == null)
		{
			return Fail("a simple object did not parse");
		}
		if (Json.GetInt(obj, "a", 0) != 1)
		{
			return Fail("integer values are not read back");
		}
		List<object> array = Json.GetArray(obj, "b");
		if (array == null || array.Count != 3 || !(array[0] is bool) || array[1] != null || (string)array[2] != "x\ny")
		{
			return Fail("array values are not read back");
		}
		Dictionary<string, object> dictionary = obj["c"] as Dictionary<string, object>;
		if (dictionary == null || Json.GetInt(dictionary, "d", 0) != -2)
		{
			return Fail("nested objects are not read back");
		}
		// Escaping must survive a round-trip, including quotes and control characters.
		string text = "a\"b\\c\td\re\nf";
		Dictionary<string, object> obj2 = Json.Parse("{\"v\":" + Json.Quote(text) + "}") as Dictionary<string, object>;
		if (obj2 == null || Json.GetString(obj2, "v") != text)
		{
			return Fail("quoted text did not round-trip");
		}
		// Malformed documents are rejected, never guessed at.
		string[] malformed = new string[6]
		{
			"",
			"{",
			"{\"a\":}",
			"{\"a\":1,}",
			"[1,2",
			"{\"a\":\"unterminated}"
		};
		for (int i = 0; i < malformed.Length; i++)
		{
			try
			{
				Json.Parse(malformed[i]);
				return Fail("malformed JSON was accepted: " + malformed[i]);
			}
			catch (FormatException)
			{
			}
		}
		// A hostile master must not be able to exhaust memory or the stack.
		try
		{
			Json.Parse(new string('[', 64) + new string(']', 64));
			return Fail("deeply nested JSON was accepted");
		}
		catch (FormatException)
		{
		}
		try
		{
			Json.Parse("{\"a\":\"" + new string('x', Json.MaxDocumentSize) + "\"}");
			return Fail("an oversized JSON document was accepted");
		}
		catch (FormatException)
		{
		}
		return true;
	}

	// --- Master server ------------------------------------------------------

	/// <summary>
	/// Exercises the master client against a local stub over real sockets: fetching,
	/// caching, announcing, and that a broken or oversized answer is dropped instead of
	/// breaking the server list.
	/// </summary>
	private static bool CheckMasterServer()
	{
		string ini = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-master.ini");
		int timeout = MasterServerClient.TimeoutMs;
		bool cachedExisted = System.IO.File.Exists(MasterServerClient.CachePath);
		string cachedBackup = cachedExisted ? System.IO.File.ReadAllText(MasterServerClient.CachePath, Encoding.UTF8) : null;
		try
		{
			using (StubMaster stub = new StubMaster())
			{
				stub.ResponseBody = "{\"servers\":[{\"name\":\"Test world\",\"host\":\"127.0.0.1\",\"port\":7777,\"players\":2,\"maxPlayers\":8,\"version\":2,\"passworded\":true,\"worldName\":\"World\",\"motd\":\"hello\",\"modSignature\":\"vanilla\"},{\"name\":\"Broken\",\"port\":7777},{\"name\":\"Bad port\",\"host\":\"127.0.0.1\",\"port\":99999}]}";
				if (System.IO.File.Exists(ini))
				{
					System.IO.File.Delete(ini);
				}
				System.IO.File.WriteAllText(ini, "[Network]\r\nMasterServer=127.0.0.1:" + stub.Port + "\r\n");
				Settings settings = new Settings(ini);
				MasterServerClient.Configure(settings);
				settings.Close();
				MasterServerClient.TimeoutMs = 3000;
				MasterServerClient.ResetBackoff();

				List<MasterServerEntry> list = MasterServerClient.Fetch();
				if (list.Count != 1)
				{
					return Fail("the server list kept " + list.Count + " entries, expected only the valid one");
				}
				MasterServerEntry entry = list[0];
				if (entry.Name != "Test world" || entry.Host != "127.0.0.1" || entry.Port != 7777)
				{
					return Fail("the server entry was parsed incorrectly");
				}
				if (entry.Players != 2 || entry.MaxPlayers != 8 || entry.ProtocolVersion != 2 || !entry.Passworded || entry.Motd != "hello")
				{
					return Fail("the server entry lost some of its fields");
				}
				if (!stub.LastRequestLine.StartsWith("GET /v1/servers"))
				{
					return Fail("the client did not request /v1/servers: " + stub.LastRequestLine);
				}
				// The good answer is cached, so the list survives an unreachable master.
				if (!System.IO.File.Exists(MasterServerClient.CachePath) || MasterServerClient.FetchCached().Count != 1)
				{
					return Fail("a successful answer was not cached");
				}
				// Announcing sends the entry back as JSON.
				entry.Name = "Renamed \"quoted\" world";
				if (!MasterServerClient.Announce(entry))
				{
					return Fail("announce was refused by the stub");
				}
				if (!stub.LastRequestLine.StartsWith("POST /v1/announce"))
				{
					return Fail("announce used the wrong endpoint: " + stub.LastRequestLine);
				}
				if (stub.LastBody == null || !stub.LastBody.Contains("\"port\":7777") || !stub.LastBody.Contains("quoted"))
				{
					return Fail("the announce body is malformed: " + stub.LastBody);
				}
				if (MasterServerClient.ParseServers(stub.LastBody) != null)
				{
					return Fail("a single entry was parsed as a server list");
				}
				// Broken answers are dropped, and repeated failures trigger a backoff.
				stub.ResponseBody = "{\"servers\":[";
				MasterServerClient.ResetBackoff();
				if (MasterServerClient.Fetch().Count != 0)
				{
					return Fail("a malformed server list was accepted");
				}
				if (MasterServerClient.CanTry())
				{
					return Fail("a failure did not trigger the backoff");
				}
				stub.ResponseBody = "{\"servers\":[]}";
				MasterServerClient.ResetBackoff();
				if (MasterServerClient.Fetch().Count != 0 || !MasterServerClient.CanTry())
				{
					return Fail("the backoff did not reset after a good answer");
				}
				// An oversized answer is refused before it is parsed.
				stub.ResponseBody = "{\"servers\":[\"" + new string('x', Json.MaxDocumentSize) + "\"]}";
				MasterServerClient.ResetBackoff();
				if (MasterServerClient.Fetch().Count != 0 || MasterServerClient.CanTry())
				{
					return Fail("an oversized response was not rejected");
				}
				// Leave a good answer in the cache for the "master is gone" check below.
				stub.ResponseBody = "{\"servers\":[{\"name\":\"Cached world\",\"host\":\"127.0.0.1\",\"port\":7777}]}";
				MasterServerClient.ResetBackoff();
				if (MasterServerClient.Fetch().Count != 1)
				{
					return Fail("the master stopped answering before the cache check");
				}
			}
			// Publishing must never advertise a singleplayer world (it runs a loopback host
			// too), so Start() refuses unless NetMode is 2.
			bool wasPublishing = MasterServerClient.Publish;
			int netMode = Main.NetMode;
			MasterServerClient.Publish = true;
			Main.NetMode = 0;
			MasterPublisher.Start(12345);
			bool startedWhileSingleplayer = MasterPublisher.IsRunning;
			MasterPublisher.Stop();
			MasterServerClient.Publish = wasPublishing;
			Main.NetMode = netMode;
			if (startedWhileSingleplayer)
			{
				return Fail("a singleplayer host was prepared for publishing");
			}
			// With the master gone, the cached list is still available.
			MasterServerClient.ResetBackoff();
			if (MasterServerClient.Fetch().Count != 0)
			{
				return Fail("a fetch against a closed port returned entries");
			}
			if (MasterServerClient.FetchCached().Count != 1)
			{
				return Fail("the cached list was lost when the master went away");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("master server check threw: " + ex.Message);
		}
		finally
		{
			MasterServerClient.TimeoutMs = timeout;
			MasterServerClient.ResetBackoff();
			try
			{
				if (System.IO.File.Exists(ini))
				{
					System.IO.File.Delete(ini);
				}
			}
			catch
			{
			}
			try
			{
				// Restore the cache the game had before the test.
				if (cachedBackup != null)
				{
					System.IO.File.WriteAllText(MasterServerClient.CachePath, cachedBackup, Encoding.UTF8);
				}
				else if (System.IO.File.Exists(MasterServerClient.CachePath))
				{
					System.IO.File.Delete(MasterServerClient.CachePath);
				}
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Minimal HTTP/1.1 endpoint for the master-server test. Answers every request with
	/// <see cref="ResponseBody"/> so the client sees a real socket, not a mock.
	/// </summary>
	private sealed class StubMaster : IDisposable
	{
		private readonly TcpListener _listener;

		private readonly Thread _thread;

		private volatile bool _stopped;

		public string ResponseBody = "{\"servers\":[]}";

		public int ResponseStatus = 200;

		public volatile string LastRequestLine;

		public volatile string LastBody;

		public StubMaster()
		{
			_listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
			_listener.Start();
			_thread = new Thread(Loop)
			{
				IsBackground = true,
				Name = "selftest.master"
			};
			_thread.Start();
		}

		public int Port => ((System.Net.IPEndPoint)_listener.LocalEndpoint).Port;

		private void Loop()
		{
			while (!_stopped)
			{
				TcpClient tcpClient;
				try
				{
					tcpClient = _listener.AcceptTcpClient();
				}
				catch
				{
					return;
				}
				try
				{
					Serve(tcpClient);
				}
				catch
				{
				}
				finally
				{
					try
					{
						tcpClient.Close();
					}
					catch
					{
					}
				}
			}
		}

		private void Serve(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			MemoryStream memoryStream = new MemoryStream();
			byte[] array = new byte[1];
			int num = 0;
			while (num < 4 && memoryStream.Length < 16384)
			{
				int num2 = stream.Read(array, 0, 1);
				if (num2 <= 0)
				{
					return;
				}
				memoryStream.WriteByte(array[0]);
				char c = (char)array[0];
				if ((num == 0 || num == 2) && c == '\r')
				{
					num++;
				}
				else if ((num == 1 || num == 3) && c == '\n')
				{
					num++;
				}
				else
				{
					num = (c == '\r') ? 1 : 0;
				}
			}
			string text = Encoding.ASCII.GetString(memoryStream.ToArray());
			string[] array2 = text.Split(new string[1] { "\r\n" }, StringSplitOptions.None);
			LastRequestLine = (array2.Length != 0) ? array2[0] : "";
			int num3 = 0;
			for (int i = 1; i < array2.Length; i++)
			{
				if (array2[i].StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
				{
					int.TryParse(array2[i].Substring(15).Trim(), out num3);
				}
			}
			if (num3 > 0 && num3 < 262144)
			{
				byte[] array3 = new byte[num3];
				int num4 = 0;
				while (num4 < num3)
				{
					int num5 = stream.Read(array3, num4, num3 - num4);
					if (num5 <= 0)
					{
						break;
					}
					num4 += num5;
				}
				LastBody = Encoding.UTF8.GetString(array3, 0, num4);
			}
			byte[] bytes = Encoding.UTF8.GetBytes(ResponseBody);
			string text2 = "HTTP/1.1 " + ResponseStatus + " " + ((ResponseStatus == 200) ? "OK" : "Error") + "\r\nContent-Type: application/json\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n";
			byte[] bytes2 = Encoding.ASCII.GetBytes(text2);
			stream.Write(bytes2, 0, bytes2.Length);
			stream.Write(bytes, 0, bytes.Length);
			stream.Flush();
		}

		public void Dispose()
		{
			_stopped = true;
			try
			{
				_listener.Stop();
			}
			catch
			{
			}
		}
	}

	// --- LAN discovery ------------------------------------------------------

	/// <summary>
	/// Discovery replies must survive both the old six-field format and the new one with
	/// flags, and the player count must come from the players field - it used to be filled
	/// with the port, so every world showed "7777/8".
	/// </summary>
	private static bool CheckLanDiscovery()
	{
		try
		{
			System.Net.IPAddress address = System.Net.IPAddress.Parse("192.168.1.50");
			AvailableNetworkSession session = TcpPeer.ParseDiscoveryReply(new string[6] { "TXDK", "Alice", "3", "5", "vanilla", "7788" }, address);
			if (session == null)
			{
				return Fail("a valid discovery reply was rejected");
			}
			if (session.HostGamertag != "Alice" || session.CurrentGamerCount != 3 || session.OpenPublicGamerSlots != 5)
			{
				return Fail("the discovery reply was parsed incorrectly");
			}
			if (session.EndPoint == null || session.EndPoint.Port != 7788 || !session.EndPoint.Address.Equals(address))
			{
				return Fail("the discovery reply produced a wrong endpoint");
			}
			if (session.Passworded)
			{
				return Fail("a reply without flags was treated as passworded");
			}
			// New format: flags say the host needs a password.
			AvailableNetworkSession flagged = TcpPeer.ParseDiscoveryReply(new string[7] { "TXDK", "Bob", "1", "7", "vanilla", "7777", "1" }, address);
			if (flagged == null || !flagged.Passworded)
			{
				return Fail("the password flag in a discovery reply was ignored");
			}
			// Junk is ignored instead of producing a half-built session.
			if (TcpPeer.ParseDiscoveryReply(new string[6] { "NOPE", "Alice", "3", "5", "vanilla", "7788" }, address) != null)
			{
				return Fail("a foreign discovery reply was accepted");
			}
			if (TcpPeer.ParseDiscoveryReply(new string[3] { "TXDK", "Alice", "3" }, address) != null)
			{
				return Fail("a truncated discovery reply was accepted");
			}
			// A broken port falls back to the configured one instead of 0.
			AvailableNetworkSession noPort = TcpPeer.ParseDiscoveryReply(new string[6] { "TXDK", "Carol", "2", "6", "vanilla", "abc" }, address);
			if (noPort == null || noPort.EndPoint.Port != NetworkConfig.Port)
			{
				return Fail("an invalid port in a discovery reply was not replaced");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("LAN discovery check threw: " + ex.Message);
		}
	}

	// --- Mod packets --------------------------------------------------------

	/// <summary>
	/// Mod traffic rides on its own transport opcode, so it needs no NetMessage id and can
	/// never collide with the console protocol. These checks pin the encoding, the limit,
	/// the garbage handling, and that a frame really survives a socket round-trip.
	/// </summary>
	private static bool CheckModPackets()
	{
		try
		{
			byte[] payload = new byte[5] { 1, 0, 0, 0, 9 };
			byte[] array = ModNet.Encode("ExampleMod", payload);
			if (array == null)
			{
				return Fail("a valid mod packet was rejected");
			}
			if (array[0] != 12)
			{
				return Fail("mod packets do not use their own opcode (got " + array[0] + ")");
			}
			if (!ModNet.TryDecode(array, out string modId, out byte[] decoded))
			{
				return Fail("a mod packet did not decode");
			}
			if (modId != "ExampleMod")
			{
				return Fail("the mod id was lost: " + modId);
			}
			if (decoded.Length != payload.Length || decoded[4] != 9 || ModNet.ReadInt(decoded, 0) != 1)
			{
				return Fail("the mod payload was altered");
			}
			// An empty payload is legal: a packet can be a pure signal.
			byte[] empty = ModNet.Encode("ExampleMod", null);
			if (empty == null || !ModNet.TryDecode(empty, out modId, out decoded) || decoded.Length != 0)
			{
				return Fail("an empty mod payload was not handled");
			}
			// Envelope abuse is rejected instead of throwing.
			string[] badIds = new string[2] { null, "" };
			for (int i = 0; i < badIds.Length; i++)
			{
				if (ModNet.Encode(badIds[i], payload) != null)
				{
					return Fail("a packet without a mod id was accepted");
				}
			}
			if (ModNet.Encode("ExampleMod", new byte[ModNet.MaxPayload + 1]) != null)
			{
				return Fail("an oversized mod payload was accepted");
			}
			if (ModNet.Encode("ExampleMod", new byte[ModNet.MaxPayload]) == null)
			{
				return Fail("a payload exactly at the limit was rejected");
			}
			string text;
			byte[] array2;
			if (ModNet.TryDecode(new byte[3] { 12, 10, (byte)'x' }, out text, out array2))
			{
				return Fail("a truncated mod frame was accepted");
			}
			if (ModNet.TryDecode(new byte[2] { 12, 0 }, out text, out array2))
			{
				return Fail("a frame with an empty mod id was accepted");
			}
			if (ModNet.TryDecode(new byte[3] { 4, 1, (byte)'x' }, out text, out array2))
			{
				return Fail("a game data frame was mistaken for a mod packet");
			}
			// A hostile client claiming a huge id length must not allocate or crash.
			if (ModNet.TryDecode(new byte[3] { 12, 255, 1 }, out text, out array2))
			{
				return Fail("an impossible mod id length was accepted");
			}
			// No link: sending reports failure instead of pretending.
			ModNet.Detach();
			if (ModNet.IsConnected || ModNet.Send("ExampleMod", payload))
			{
				return Fail("a packet was sent with no connection");
			}
			// Real socket round-trip: the frame must arrive intact and be recognised as mod
			// traffic, which is what the host and client loops look at.
			TcpListener listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
			listener.Start();
			try
			{
				int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
				using TcpClient sender = new TcpClient();
				sender.Connect(System.Net.IPAddress.Loopback, port);
				TcpClient receiver = listener.AcceptTcpClient();
				using (receiver)
				{
					TcpClient capturedSender = sender;
					byte[] frame = WireFormat.LengthPrefix(array);
					capturedSender.GetStream().Write(frame, 0, frame.Length);
					capturedSender.GetStream().Flush();
					byte[] received = WireFormat.ReadFrame(receiver.GetStream());
					if (received == null)
					{
						return Fail("a mod frame did not survive the socket");
					}
					if (!ModNet.TryDecode(received, out modId, out decoded) || modId != "ExampleMod" || decoded[4] != 9)
					{
						return Fail("a mod frame was corrupted in transit");
					}
				}
			}
			finally
			{
				listener.Stop();
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod packet check threw: " + ex.Message);
		}
	}

	// --- Saved servers ------------------------------------------------------

	/// <summary>
	/// Favourites are the only server list that survives both a dead master and a failed
	/// LAN scan, so the file has to round-trip, deduplicate and ignore junk.
	/// </summary>
	private static bool CheckSavedServers()
	{
		string backup = null;
		bool had = System.IO.File.Exists(ServerFavorites.FilePath);
		try
		{
			if (had)
			{
				backup = System.IO.File.ReadAllText(ServerFavorites.FilePath, Encoding.UTF8);
				System.IO.File.Delete(ServerFavorites.FilePath);
			}
			ServerFavorites.Reset();
			if (ServerFavorites.All.Count != 0)
			{
				return Fail("the saved server list did not start empty");
			}
			if (!ServerFavorites.Add("192.168.1.10:7777"))
			{
				return Fail("a plain address was not saved");
			}
			// The port is optional and defaults to the configured one.
			if (!ServerFavorites.Add("example.com"))
			{
				return Fail("an address without a port was not saved");
			}
			// A bare host means "the configured port", so a different port must not match.
			if (ServerFavorites.Contains("192.168.1.10:7778"))
			{
				return Fail("an address matches even when the port differs");
			}
			if (!ServerFavorites.Contains("192.168.1.10:7777"))
			{
				return Fail("a saved address is not found");
			}
			// Adding twice is a no-op, not a duplicate row.
			if (ServerFavorites.Add("192.168.1.10:7777"))
			{
				return Fail("a duplicate address was saved twice");
			}
			if (ServerFavorites.All.Count != 2)
			{
				return Fail("the saved list has " + ServerFavorites.All.Count + " entries, expected 2");
			}
			// Garbage is refused instead of stored.
			if (ServerFavorites.Add("host:99999") || ServerFavorites.Add("") || ServerFavorites.Add("host:abc"))
			{
				return Fail("an invalid address was saved");
			}
			// The file survives a reload, and a hand-edited file with junk is tolerated.
			ServerFavorites.Reset();
			if (ServerFavorites.All.Count != 2)
			{
				return Fail("the saved list was lost on reload");
			}
			System.IO.File.WriteAllText(ServerFavorites.FilePath, "# hand edited\r\nbogus line\r\n10.0.0.5:7788\r\n", Encoding.UTF8);
			ServerFavorites.Reset();
			if (ServerFavorites.All.Count != 1 || ServerFavorites.All[0] != "10.0.0.5:7788")
			{
				return Fail("a hand-edited file produced " + ServerFavorites.All.Count + " usable entries");
			}
			if (!ServerFavorites.Remove("10.0.0.5:7788") || ServerFavorites.All.Count != 0)
			{
				return Fail("an address could not be removed");
			}
			if (ServerFavorites.Remove("10.0.0.5:7788"))
			{
				return Fail("removing a missing address reported success");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("saved server check threw: " + ex.Message);
		}
		finally
		{
			ServerFavorites.Reset();
			try
			{
				if (backup != null)
				{
					System.IO.File.WriteAllText(ServerFavorites.FilePath, backup, Encoding.UTF8);
				}
				else if (System.IO.File.Exists(ServerFavorites.FilePath))
				{
					System.IO.File.Delete(ServerFavorites.FilePath);
				}
			}
			catch
			{
			}
		}
	}

	// --- Auto refresh -------------------------------------------------------

	/// <summary>
	/// The list now refreshes itself while it is on screen. The interesting parts are the
	/// cases where it must NOT fire (screen closed, a search already running, disabled) and
	/// that a selection made stale by a refresh cannot index past the list.
	/// </summary>
	private static bool CheckAutoRefresh()
	{
		int interval = ServerListRefresh.IntervalSeconds;
		try
		{
			ServerListRefresh.Enabled = true;
			ServerListRefresh.IntervalSeconds = 1;
			ServerListRefresh.Reset();
			// One second at 60 fps: 59 ticks is not enough, the 60th fires.
			for (int i = 0; i < 59; i++)
			{
				if (ServerListRefresh.Tick(screenOpen: true, alreadySearching: false))
				{
					return Fail("the list refreshed too early at tick " + i);
				}
			}
			if (!ServerListRefresh.Tick(screenOpen: true, alreadySearching: false))
			{
				return Fail("the list never refreshed");
			}
			// The counter restarts, so the next refresh is another full interval away.
			if (ServerListRefresh.Tick(screenOpen: true, alreadySearching: false))
			{
				return Fail("the refresh timer did not restart");
			}
			// A closed screen resets the countdown instead of accumulating it in the menus.
			ServerListRefresh.Tick(screenOpen: false, alreadySearching: false);
			for (int j = 0; j < 59; j++)
			{
				ServerListRefresh.Tick(screenOpen: false, alreadySearching: false);
			}
			if (ServerListRefresh.Tick(screenOpen: true, alreadySearching: false))
			{
				return Fail("the countdown accumulated while the screen was closed");
			}
			// Never restart a search that is already running.
			ServerListRefresh.Reset();
			bool fired = false;
			for (int k = 0; k < 200; k++)
			{
				fired |= ServerListRefresh.Tick(screenOpen: true, alreadySearching: true);
			}
			if (fired)
			{
				return Fail("a refresh fired while a search was already running");
			}
			// Zero or a negative interval means "off", not "every frame".
			ServerListRefresh.IntervalSeconds = 0;
			ServerListRefresh.Reset();
			fired = false;
			for (int l = 0; l < 200; l++)
			{
				fired |= ServerListRefresh.Tick(screenOpen: true, alreadySearching: false);
			}
			if (fired)
			{
				return Fail("an interval of 0 fired a refresh");
			}
			ServerListRefresh.Enabled = false;
			ServerListRefresh.IntervalSeconds = 1;
			ServerListRefresh.Reset();
			fired = false;
			for (int m = 0; m < 200; m++)
			{
				fired |= ServerListRefresh.Tick(screenOpen: true, alreadySearching: false);
			}
			if (fired)
			{
				return Fail("a disabled auto refresh still fired");
			}
			// A refresh can shrink the list; the remembered index must not point past it.
			if (ServerListRefresh.ClampIndex(5, 2) != 1)
			{
				return Fail("a stale selection was not clamped to the list");
			}
			if (ServerListRefresh.ClampIndex(3, 0) != 0 || ServerListRefresh.ClampIndex(-4, 3) != 0)
			{
				return Fail("clamping an empty or negative selection failed");
			}
			if (ServerListRefresh.ClampIndex(1, 4) != 1)
			{
				return Fail("clamping moved a valid selection");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("auto refresh check threw: " + ex.Message);
		}
		finally
		{
			ServerListRefresh.IntervalSeconds = interval;
			ServerListRefresh.Enabled = interval > 0;
			ServerListRefresh.Reset();
		}
	}

	// --- Settings persistence -----------------------------------------------

	/// <summary>
	/// Every "save" path used to be a no-op: <c>Settings.Set</c> only creates missing keys, so
	/// writing an existing key did nothing and Close() wrote the old value back. Rebinding a
	/// key, toggling cheats or changing the window size all looked like they worked and then
	/// reverted on restart. This pins the contract: Set seeds, Update overwrites.
	/// </summary>
	private static bool CheckSettingsPersistence()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-settings.ini");
		try
		{
			if (System.IO.File.Exists(text))
			{
				System.IO.File.Delete(text);
			}
			Settings settings = new Settings(text);
			settings.Set("A", "Key", "first", "comment");
			settings.Close();
			Settings settings2 = new Settings(text);
			if (settings2.Get("A", "Key") != "first")
			{
				return Fail("a seeded value did not survive a round-trip");
			}
			// Set() must not overwrite an existing value: that is how defaults are seeded.
			settings2.Set("A", "Key", "second");
			if (settings2.Get("A", "Key") != "first")
			{
				return Fail("Set overwrote an existing value");
			}
			// Update() must overwrite, and must add a key that is not there yet.
			settings2.Update("A", "Key", "second");
			settings2.Update("A", "New", "third");
			settings2.Close();
			Settings settings3 = new Settings(text);
			if (settings3.Get("A", "Key") != "second")
			{
				return Fail("Update did not overwrite the value on disk");
			}
			if (settings3.Get("A", "New") != "third")
			{
				return Fail("Update did not add a new key");
			}
			settings3.Close();
			// A key that is empty on disk counts as missing, so seeding still fills it in.
			System.IO.File.WriteAllText(text, "[A]\r\nEmpty=\r\n");
			Settings settings4 = new Settings(text);
			settings4.Set("A", "Empty", "filled");
			settings4.Close();
			Settings settings5 = new Settings(text);
			if (settings5.Get("A", "Empty") != "filled")
			{
				return Fail("an empty value was not treated as unset");
			}
			settings5.Close();
			// Key bindings must round-trip: they had the same bug.
			Keys heal = Terraria.Input.KeyBindings.Get(Terraria.Input.KeyBindings.Action.Heal);
			try
			{
				if (!Terraria.Input.KeyBindings.TrySet(Terraria.Input.KeyBindings.Action.Heal, Keys.F9))
				{
					return Fail("a free key could not be bound");
				}
				Settings settings6 = new Settings(text);
				Terraria.Input.KeyBindings.Save(settings6);
				settings6.Close();
				Settings settings7 = new Settings(text);
				Terraria.Input.KeyBindings.Load(settings7);
				settings7.Close();
				if (Terraria.Input.KeyBindings.Get(Terraria.Input.KeyBindings.Action.Heal) != Keys.F9)
				{
					return Fail("a rebound key did not survive a restart");
				}
			}
			finally
			{
				Terraria.Input.KeyBindings.TrySet(Terraria.Input.KeyBindings.Action.Heal, heal);
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("settings persistence check threw: " + ex.Message);
		}
		finally
		{
			try
			{
				if (System.IO.File.Exists(text))
				{
					System.IO.File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}

	// --- Master endpoints ---------------------------------------------------

	/// <summary>
	/// The Network screen lets the master address be typed in. A typo must not wipe the
	/// working configuration, and a comma-separated list must keep its order (primary first,
	/// mirrors after).
	/// </summary>
	private static bool CheckMasterEndpoints()
	{
		string[] saved = MasterServerClient.Endpoints;
		try
		{
			if (!MasterServerClient.SetEndpoints("example.com:17777"))
			{
				return Fail("a plain master address was rejected");
			}
			if (MasterServerClient.PrimaryEndpoint != "example.com:17777")
			{
				return Fail("the master address was not stored as typed: " + MasterServerClient.PrimaryEndpoint);
			}
			// Port is optional and falls back to the configured one.
			if (!MasterServerClient.SetEndpoints("master.example"))
			{
				return Fail("an address without a port was rejected");
			}
			if (!MasterServerClient.PrimaryEndpoint.StartsWith("master.example:"))
			{
				return Fail("an address without a port was not normalised");
			}
			// A list keeps order: the first entry is the primary.
			if (!MasterServerClient.SetEndpoints("a.example:1111, b.example:2222"))
			{
				return Fail("a comma-separated list was rejected");
			}
			string[] array = MasterServerClient.Endpoints;
			if (array.Length != 2 || array[0] != "a.example:1111" || array[1] != "b.example:2222")
			{
				return Fail("the endpoint list lost its order: " + string.Join(", ", array));
			}
			// Garbage is refused and the previous list is kept.
			if (MasterServerClient.SetEndpoints("") || MasterServerClient.SetEndpoints("host:99999") || MasterServerClient.SetEndpoints("host:abc"))
			{
				return Fail("an invalid master address was accepted");
			}
			array = MasterServerClient.Endpoints;
			if (array.Length != 2 || array[0] != "a.example:1111")
			{
				return Fail("a rejected address damaged the existing endpoint list");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("master endpoint check threw: " + ex.Message);
		}
		finally
		{
			MasterServerClient.SetEndpoints(string.Join(",", saved));
		}
	}

	// --- Host and two clients -----------------------------------------------

	/// <summary>
	/// The real thing over real sockets: one host and two clients, each in its own session.
	/// This is the case that was never covered - loopback only proved framing - and it is
	/// exactly what a multiplayer match does: everybody connects, the roster is distributed
	/// and a packet from one client reaches the other through the host.
	///
	/// Sessions are built directly instead of through NetworkSession.Create/Join because those
	/// statics allow a single active session per process (activeSession); the constructor and
	/// StartHostPeer/StartClientPeer are what those statics call anyway.
	/// </summary>
	private static bool CheckHostAndTwoClients()
	{
		int savedPort = NetworkConfig.Port;
		int savedDiscovery = NetworkConfig.DiscoveryPort;
		int savedMax = NetworkConfig.MaxPlayers;
		bool savedTcp = NetworkConfig.UseTcpTransport;
		string savedPassword = NetworkConfig.Password;
		NetworkSession hostSession = null;
		NetworkSession clientSession1 = null;
		NetworkSession clientSession2 = null;
		try
		{
			Microsoft.Xna.Framework.GamerServices.GamerServicesDispatcher.Initialize(null);
			if (Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers.Count < 3)
			{
				return Fail("gamer services did not provide three gamertags");
			}
			NetworkConfig.UseTcpTransport = true;
			NetworkConfig.Password = string.Empty;
			NetworkConfig.MaxPlayers = 7;
			// Port 0 lets the OS choose a free port; the host reports the real one back.
			NetworkConfig.Port = 0;
			NetworkConfig.DiscoveryPort = 0;

			NetworkSessionProperties properties = new NetworkSessionProperties();
			hostSession = new NetworkSession(properties, NetworkSessionType.PlayerMatch, 8, 0, 1, new List<Microsoft.Xna.Framework.GamerServices.SignedInGamer>(1) { Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers[0] });
			hostSession.StartHostPeer();
			int port = NetworkConfig.ActivePort;
			if (port <= 0)
			{
				return Fail("the host did not report the port it bound");
			}

			clientSession1 = MakeClientSession(1, port, out AvailableNetworkSession available1);
			clientSession2 = MakeClientSession(2, port, out AvailableNetworkSession available2);
			if (clientSession1 == null || clientSession2 == null)
			{
				return Fail("a client session could not be created");
			}

			// Pump all three "game loops" until both clients are in the roster.
			bool bothJoined = PumpUntil(delegate
			{
				return hostSession.RemoteGamers.Count >= 2 && clientSession1.RemoteGamers.Count >= 1 && clientSession2.RemoteGamers.Count >= 1;
			}, 4000, hostSession, clientSession1, clientSession2);
			if (!bothJoined)
			{
				return Fail("clients did not join: host sees " + hostSession.RemoteGamers.Count + " remote gamer(s), clients see " + clientSession1.RemoteGamers.Count + "/" + clientSession2.RemoteGamers.Count);
			}

			// The host must have learned the real gamertags, not "Stub Gamer".
			string hostSees = string.Empty;
			foreach (Microsoft.Xna.Framework.Net.NetworkGamer gamer in hostSession.RemoteGamers)
			{
				hostSees += gamer.Gamertag + ";";
			}
			if (!hostSees.Contains("Stub Gamer (1)") || !hostSees.Contains("Stub Gamer (2)"))
			{
				return Fail("the roster lost the gamertags: " + hostSees);
			}

			// Client 1 sends a game packet: it must reach client 2 through the host relay.
			byte[] payload = new byte[6] { 0xAA, 0x01, 0x00, 0x05, 0x00, 0x7F };
			clientSession1.LocalGamers[0].SendData(payload, Microsoft.Xna.Framework.Net.SendDataOptions.ReliableInOrder);
			if (!PumpUntil(delegate
			{
				return clientSession2.LocalGamers[0].IsDataAvailable;
			}, 4000, hostSession, clientSession1, clientSession2))
			{
				return Fail("a client packet never reached the other client");
			}
			byte[] received = new byte[64];
			Microsoft.Xna.Framework.Net.NetworkGamer sender;
			int count = clientSession2.LocalGamers[0].ReceiveData(received, out sender);
			if (count != payload.Length)
			{
				return Fail("the relayed packet has " + count + " byte(s), expected " + payload.Length);
			}
			for (int i = 0; i < payload.Length; i++)
			{
				if (received[i] != payload[i])
				{
					return Fail("the relayed packet differs at byte " + i);
				}
			}
			if (sender == null)
			{
				return Fail("the relayed packet lost its sender");
			}

			// A packet from client 2 must reach client 1 as well (relay is bidirectional).
			byte[] reply = new byte[3] { 0xBB, 0x02, 0x00 };
			clientSession2.LocalGamers[0].SendData(reply, Microsoft.Xna.Framework.Net.SendDataOptions.ReliableInOrder);
			if (!PumpUntil(delegate
			{
				return clientSession1.LocalGamers[0].IsDataAvailable;
			}, 4000, hostSession, clientSession1, clientSession2))
			{
				return Fail("the relay only worked in one direction");
			}
			if (clientSession1.LocalGamers[0].ReceiveData(received, out sender) != reply.Length)
			{
				return Fail("the reply packet has the wrong length");
			}

			// Batching: a burst produced in one tick must arrive complete and in order. The
			// transport now concatenates game frames into one write per connection per tick, so
			// "did they all survive" is a property of the wire, not of the buffer alone.
			DrainPackets(clientSession2);
			int burstCount = 12;
			for (int i = 0; i < burstCount; i++)
			{
				clientSession1.LocalGamers[0].SendData(new byte[3] { 0xCC, (byte)i, (byte)(i * 3) },
					Microsoft.Xna.Framework.Net.SendDataOptions.ReliableInOrder);
			}
			int got = 0;
			DateTime burstDeadline = DateTime.UtcNow.AddSeconds(5);
			byte[] burst = new byte[64];
			while (got < burstCount && DateTime.UtcNow < burstDeadline)
			{
				PumpUntil(delegate
				{
					return false;
				}, 20, hostSession, clientSession1, clientSession2);
				while (clientSession2.LocalGamers[0].IsDataAvailable)
				{
					int num3 = clientSession2.LocalGamers[0].ReceiveData(burst, out sender);
					if (num3 != 3 || burst[0] != 0xCC || burst[1] != (byte)got || burst[2] != (byte)(got * 3))
					{
						return Fail("batched packet " + got + " arrived wrong (length " + num3 + ")");
					}
					got++;
				}
			}
			if (got != burstCount)
			{
				return Fail("only " + got + " of " + burstCount + " batched packets arrived");
			}

			// Moderation on a live connection: a muted player's chat must not reach the others,
			// and unmuting must restore it. This is the whole point of !mute, and the relay is
			// the only place where it can be enforced for real.
			DrainPackets(clientSession1);
			DrainPackets(clientSession2);
			SetMuted(true, "Stub Gamer (1)");
			byte[] chat = HostModeration.BuildChatPayload("<Stub Gamer (1)> secret", 255, 255, 255);
			clientSession1.LocalGamers[0].SendData(chat, Microsoft.Xna.Framework.Net.SendDataOptions.ReliableInOrder);
			PumpUntil(delegate
			{
				return false;
			}, 600, hostSession, clientSession1, clientSession2);
			if (clientSession2.LocalGamers[0].IsDataAvailable)
			{
				return Fail("a muted player's chat reached another client");
			}
			SetMuted(false, "Stub Gamer (1)");
			clientSession1.LocalGamers[0].SendData(chat, Microsoft.Xna.Framework.Net.SendDataOptions.ReliableInOrder);
			if (!PumpUntil(delegate
			{
				return clientSession2.LocalGamers[0].IsDataAvailable;
			}, 4000, hostSession, clientSession1, clientSession2))
			{
				return Fail("chat stayed blocked after unmuting");
			}
			DrainPackets(clientSession2);

			// A command typed by a client is addressed to the server: it must not appear as
			// chat for anybody else, whether it is allowed or refused.
			// Client 1 still has the "you are muted" notice from the check above, so drain it
			// first: otherwise this assertion measures the wrong message.
			DrainPackets(clientSession1);
			byte[] command = ChatPacket.Build("<Stub Gamer (2)> !who", 255, 255, 255);
			clientSession2.LocalGamers[0].SendData(command, Microsoft.Xna.Framework.Net.SendDataOptions.ReliableInOrder);
			PumpUntil(delegate
			{
				return false;
			}, 600, hostSession, clientSession1, clientSession2);
			if (clientSession1.LocalGamers[0].IsDataAvailable)
			{
				return Fail("a client's command was relayed as chat");
			}
			// Client 2 has no group, so the command is refused. Nobody may be kicked by it.
			if (TcpPeer.CurrentHost == null || TcpPeer.CurrentHost.ConnectedCount != 2)
			{
				return Fail("a refused command changed the roster");
			}
			// And a plain chat line from the same client is still relayed: the command branch
			// must not swallow ordinary messages.
			byte[] plain = ChatPacket.Build("<Stub Gamer (2)> hello", 255, 255, 255);
			clientSession2.LocalGamers[0].SendData(plain, Microsoft.Xna.Framework.Net.SendDataOptions.ReliableInOrder);
			if (!PumpUntil(delegate
			{
				return clientSession1.LocalGamers[0].IsDataAvailable;
			}, 4000, hostSession, clientSession1, clientSession2))
			{
				return Fail("ordinary chat stopped working after command handling was added");
			}
			DrainPackets(clientSession1);

			// World protection over a live connection: the gate sits inside the packet handler,
			// so this is the only way to prove a refused edit never reaches the world. Sounds
			// are muted for the test: PlaceTile announces itself, and audio is not loaded here.
			int netModeBefore = Main.NetMode;
			float soundBefore = Main.SoundVolume;
			int spawnRadiusBefore = Terraria.Net.WorldProtection.SpawnRadius;
			bool disableBefore = Terraria.Net.WorldProtection.DisableBuild;
			try
			{
				Main.NetMode = 2;
				Main.SoundVolume = 0f;
				Terraria.Net.WorldProtection.DisableBuild = false;
				Terraria.Net.WorldProtection.SpawnRadius = 16;
				bool createdClient;
				NetClient clientOne = EnsureNetClient(hostSession, "Stub Gamer (1)", out createdClient);
				if (clientOne == null)
				{
					return Fail("the host has no NetClient for a connected player");
				}
				try
				{
					AssertWorldProtectionReachesTheWorld(clientOne);
				}
				finally
				{
					if (createdClient)
					{
						Netplay.ClientList.Remove(clientOne);
					}
				}
			}
			finally
			{
				Main.NetMode = netModeBefore;
				Main.SoundVolume = soundBefore;
				Terraria.Net.WorldProtection.SpawnRadius = spawnRadiusBefore;
				Terraria.Net.WorldProtection.DisableBuild = disableBefore;
				Terraria.Net.WorldProtection.ResetNotices();
			}

			// A banned item over a live connection: the host must refuse the inventory sync and
			// not relay it. This is the only place the server is authoritative for "does this
			// item exist", so it is where the ban has to be enforced.
			int netModeItem = Main.NetMode;
			Main.NetMode = 2;
			try
			{
				ItemRestrictions.Reload();
				ItemRestrictions.Add(48);
				bool createdItemClient;
				NetClient itemClient = EnsureNetClient(hostSession, "Stub Gamer (1)", out createdItemClient);
				if (itemClient == null)
				{
					return Fail("no NetClient for the banned item check");
				}
				try
				{
					// PlayerSet entries are created by the game's constructor, which never runs
					// headless; stand one up so the handler has a target.
					Player savedPlayer = Main.PlayerSet[1];
					Main.PlayerSet[1] = new Player();
					try
					{
						Player target = Main.PlayerSet[1];
						target.Inventory[3].Init();
						SendInventorySync(itemClient, 1, 3, 1, 48);
						if (target.Inventory[3].NetID == 48 || target.Inventory[3].Stack > 0)
						{
							return Fail("a banned item was accepted into the inventory");
						}
						// An allowed item still syncs: the gate must not block everything.
						SendInventorySync(itemClient, 1, 3, 1, 1);
						if (target.Inventory[3].Stack <= 0)
						{
							return Fail("an allowed item was refused by the item gate");
						}
					}
					finally
					{
						Main.PlayerSet[1] = savedPlayer;
					}
				}
				finally
				{
					if (createdItemClient)
					{
						Netplay.ClientList.Remove(itemClient);
					}
				}
			}
			finally
			{
				ItemRestrictions.Remove(48);
				ItemRestrictions.Reload();
				Main.NetMode = netModeItem;
			}

			// A kick from the host must actually disconnect that player - this is what !kick
			// uses, and it is the only admin action that touches a live connection.
			TcpPeer hostPeer = TcpPeer.CurrentHost;
			if (hostPeer == null)
			{
				return Fail("the host peer was not registered for host commands");
			}
			if (hostPeer.ConnectedCount != 2)
			{
				return Fail("the host counts " + hostPeer.ConnectedCount + " connections instead of 2");
			}
			if (!hostPeer.Kick("Stub Gamer (2)", "Kicked by the test.", out string kickedName))
			{
				return Fail("kicking a connected player failed");
			}
			if (kickedName != "Stub Gamer (2)")
			{
				return Fail("the kicked player was reported as '" + kickedName + "'");
			}
			// An unknown or ambiguous name must not kick anybody.
			if (hostPeer.Kick("Nobody", "x", out kickedName))
			{
				return Fail("kicking a player who is not connected reported success");
			}
			if (hostPeer.ConnectedCount != 1)
			{
				return Fail("the kick did not close the connection");
			}
			// The kicked client must learn that the session ended, not sit in a frozen world.
			if (!PumpUntil(delegate
			{
				return clientSession2.SessionState == NetworkSessionState.Ended;
			}, 4000, hostSession, clientSession1, clientSession2))
			{
				return Fail("the kicked client did not see the session end (state " + clientSession2.SessionState + ")");
			}

			// A leave must be noticed by everybody, otherwise ghost players stay in the world.
			// Client 2 was kicked above, so this also covers "the host notices a kick".
			clientSession2?.Dispose();
			clientSession2 = null;
			if (!PumpUntil(delegate
			{
				return hostSession.RemoteGamers.Count == 1;
			}, 4000, hostSession, clientSession1))
			{
				return Fail("the host did not notice a client leaving");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("host and two clients check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			try
			{
				clientSession2?.Dispose();
			}
			catch
			{
			}
			try
			{
				clientSession1?.Dispose();
			}
			catch
			{
			}
			try
			{
				hostSession?.Dispose();
			}
			catch
			{
			}
			NetworkConfig.Port = savedPort;
			NetworkConfig.DiscoveryPort = savedDiscovery;
			NetworkConfig.MaxPlayers = savedMax;
			NetworkConfig.UseTcpTransport = savedTcp;
			NetworkConfig.Password = savedPassword;
			NetworkConfig.ActivePort = 0;
		}
	}

	/// <summary>
	/// The world-protection assertions that need a real packet handler: inside the spawn box an
	/// edit is refused, outside it is applied, and DisableBuild blocks even an allowed spot.
	/// Runs with <c>Main.NetMode == 2</c> set by the caller.
	/// </summary>
	private static void AssertWorldProtectionReachesTheWorld(NetClient client)
	{
		int spawnX = Main.SpawnTileX;
		int spawnY = Main.SpawnTileY;
		Main.TileSet[spawnX, spawnY].IsActive = 0;
		Main.TileSet[spawnX, spawnY].Type = 0;
		// Inside the spawn box: refused, so the tile stays empty.
		SendTileChange(client, 1, spawnX, spawnY, 1, 0);
		if (Main.TileSet[spawnX, spawnY].IsActive != 0)
		{
			throw new InvalidOperationException("a client modified a tile inside spawn protection");
		}
		// Outside the box: allowed, so the tile is placed. This also proves the gate is not
		// simply refusing everything.
		int farX = spawnX + 400;
		int farY = spawnY + 40;
		if (farX >= Main.MaxTilesX - 2 || farY >= Main.MaxTilesY - 2)
		{
			farX = spawnX / 2;
			farY = spawnY / 2;
		}
		Main.TileSet[farX, farY].IsActive = 0;
		Main.TileSet[farX, farY].Type = 0;
		SendTileChange(client, 1, farX, farY, 1, 0);
		if (Main.TileSet[farX, farY].IsActive == 0)
		{
			throw new InvalidOperationException("a client could not place a tile outside spawn protection");
		}
		// DisableBuild blocks even a spot that was allowed a moment ago.
		Terraria.Net.WorldProtection.DisableBuild = true;
		Main.TileSet[farX, farY].IsActive = 0;
		Main.TileSet[farX, farY].Type = 0;
		SendTileChange(client, 1, farX, farY, 1, 0);
		if (Main.TileSet[farX, farY].IsActive != 0)
		{
			throw new InvalidOperationException("DisableBuild did not block a client's edit");
		}
		// Leave the world as we found it, so later checks are not affected.
		Main.TileSet[farX, farY].IsActive = 0;
		Main.TileSet[farX, farY].Type = 0;
	}

	/// <summary>Finds the host-side client record for a connected player, by gamertag.</summary>
	private static NetClient FindNetClient(string gamertag)
	{
		for (int i = 0; i < Netplay.ClientList.Count; i++)
		{
			NetClient netClient = Netplay.ClientList[i];
			if (netClient != null && netClient.NetGamer != null && string.Equals(netClient.NetGamer.Gamertag, gamertag, StringComparison.OrdinalIgnoreCase))
			{
				return netClient;
			}
		}
		return null;
	}

	/// <summary>
	/// Makes the host-side <see cref="NetClient"/> for a connected player when the game has not
	/// made one yet. The real one is created by the session's GamerJoined event, which a test
	/// session never hooks because that handler needs the UI; the record itself is just a
	/// wrapper around the gamer, so building it here exercises the same packet handler.
	/// </summary>
	private static NetClient EnsureNetClient(NetworkSession hostSession, string gamertag, out bool created)
	{
		created = false;
		NetClient existing = FindNetClient(gamertag);
		if (existing != null)
		{
			return existing;
		}
		foreach (Microsoft.Xna.Framework.Net.NetworkGamer gamer in hostSession.RemoteGamers)
		{
			if (string.Equals(gamer.Gamertag, gamertag, StringComparison.OrdinalIgnoreCase))
			{
				NetClient netClient = new NetClient(gamer);
				Netplay.ClientList.Add(netClient);
				created = true;
				return netClient;
			}
		}
		return null;
	}

	/// <summary>
	/// Feeds one tile-change packet (message 17) into the host as if it arrived from that
	/// client, then runs the host's handler. The handler is what a real connection would call,
	/// so this exercises the production path, including world protection.
	/// </summary>
	private static void SendTileChange(NetClient client, byte action, int x, int y, byte type, byte style)
	{
		System.IO.MemoryStream stream = new System.IO.MemoryStream();
		using (System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(stream, Encoding.UTF8))
		{
			binaryWriter.Write((byte)17);
			binaryWriter.Write(action);
			binaryWriter.Write((ushort)x);
			binaryWriter.Write((ushort)y);
			if (action <= 4)
			{
				binaryWriter.Write(type);
				if (action == 1)
				{
					binaryWriter.Write(style);
				}
			}
			binaryWriter.Flush();
		}
		byte[] buffer = stream.ToArray();
		NetMessage.PacketIn.BaseStream.Position = 0;
		NetMessage.PacketIn.BaseStream.Write(buffer, 0, buffer.Length);
		NetMessage.PacketIn.BaseStream.Position = 0;
		NetMessage.GetData(client);
	}

	/// <summary>
	/// Feeds one inventory sync packet (message 5) into the host as if it arrived from that
	/// client: player index, slot, stack, then prefix and item id when the stack is non-zero.
	/// </summary>
	private static void SendInventorySync(NetClient client, byte player, byte slot, byte stack, short netId)
	{
		System.IO.MemoryStream stream = new System.IO.MemoryStream();
		using (System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(stream, Encoding.UTF8))
		{
			binaryWriter.Write((byte)5);
			binaryWriter.Write(player);
			binaryWriter.Write(slot);
			binaryWriter.Write(stack);
			if (stack > 0)
			{
				binaryWriter.Write((byte)0);
				binaryWriter.Write(netId);
			}
			binaryWriter.Flush();
		}
		byte[] buffer = stream.ToArray();
		NetMessage.PacketIn.BaseStream.Position = 0;
		NetMessage.PacketIn.BaseStream.Write(buffer, 0, buffer.Length);
		NetMessage.PacketIn.BaseStream.Position = 0;
		NetMessage.GetData(client);
	}

	/// <summary>Mutes or unmutes a name for the duration of one assertion, restoring the old state.</summary>
	private static void SetMuted(bool muted, string gamertag)
	{
		if (muted)
		{
			HostModeration.Mute(gamertag);
		}
		else
		{
			HostModeration.Unmute(gamertag);
		}
	}

	/// <summary>Empties a session's inbound chat queue so the next assertion starts clean.</summary>
	private static void DrainPackets(NetworkSession session)
	{
		if (session == null || session.LocalGamers.Count == 0)
		{
			return;
		}
		byte[] array = new byte[4096];
		Microsoft.Xna.Framework.Net.NetworkGamer gamer;
		while (session.LocalGamers[0].IsDataAvailable)
		{
			session.LocalGamers[0].ReceiveData(array, out gamer);
		}
	}

	/// <summary>Builds a client session pointing at the host port and opens its transport.</summary>
	private static NetworkSession MakeClientSession(int gamerIndex, int port, out AvailableNetworkSession available)
	{
		NetworkSessionProperties properties = new NetworkSessionProperties();
		NetworkSession session = new NetworkSession(properties, NetworkSessionType.PlayerMatch, 8, 0, 1, new List<Microsoft.Xna.Framework.GamerServices.SignedInGamer>(1) { Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers[gamerIndex] });
		available = new AvailableNetworkSession(0, "127.0.0.1", 0, 7, properties, new QualityOfService());
		available.SessionType = NetworkSessionType.SystemLink;
		available.EndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port);
		session.StartClientPeer(available);
		return session;
	}

	/// <summary>
	/// Runs the game loop for every session until the condition holds or the timeout expires.
	/// Each iteration sleeps a millisecond, which is enough for the transport threads.
	/// </summary>
	private static bool PumpUntil(Func<bool> condition, int timeoutMs, params NetworkSession[] sessions)
	{
		DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
		while (DateTime.UtcNow < deadline)
		{
			for (int i = 0; i < sessions.Length; i++)
			{
				if (sessions[i] == null)
				{
					continue;
				}
				try
				{
					sessions[i].Update();
				}
				catch (ObjectDisposedException)
				{
				}
			}
			if (condition())
			{
				return true;
			}
			Thread.Sleep(1);
		}
		// One last pump so the final state is visible to the caller.
		return condition();
	}

	// --- Mod world events ---------------------------------------------------

	/// <summary>
	/// A mod that counts what it is told. Instantiated directly: mods are plain .NET objects,
	/// so no plugin folder or reflection is needed to check the dispatch path.
	/// </summary>
	private sealed class CountingMod : Terraria.Mods.Mod
	{
		public int TilesPlaced;

		public int TilesKilled;

		public int NpcsHurt;

		public int NpcsSpawned;

		public int NpcsDied;

		public int PlayersHurt;

		public int LiquidsChanged;

		public int LastX = -1;

		public int LastY = -1;

		public int LastDamage = -1;

		public override void OnTilePlaced(int x, int y)
		{
			TilesPlaced++;
			LastX = x;
			LastY = y;
		}

		public override void OnTileKilled(int x, int y)
		{
			TilesKilled++;
			LastX = x;
			LastY = y;
		}

		public override void OnNpcHurt(Terraria.NPC npc, int damage)
		{
			NpcsHurt++;
			LastDamage = damage;
		}

		public override void OnNpcSpawn(Terraria.NPC npc)
		{
			NpcsSpawned++;
		}

		public override void OnNpcDeath(Terraria.NPC npc)
		{
			NpcsDied++;
		}

		public override void OnPlayerHurt(Terraria.Player player, int damage)
		{
			PlayersHurt++;
			LastDamage = damage;
		}

		public override void OnLiquidChanged(int x, int y)
		{
			LiquidsChanged++;
			LastX = x;
			LastY = y;
		}
	}

	/// <summary>A mod that throws from every hook, to prove one bad mod cannot break the rest.</summary>
	private sealed class CrashingMod : Terraria.Mods.Mod
	{
		public override void OnTilePlaced(int x, int y)
		{
			throw new InvalidOperationException("synthetic failure");
		}
	}

	/// <summary>
	/// Tile and NPC hooks are the newest way mods touch the world, and they sit inside the
	/// hottest code in the game (tile placement, mining, every damage tick). Two things must
	/// hold or the game becomes unusable: they must cost nothing without mods, and they must
	/// not fire during world generation.
	/// </summary>
	private static bool CheckModWorldEvents()
	{
		System.Collections.Generic.IList<Terraria.Mods.Mod> mods = Terraria.Mods.ModLoader.Mods;
		int errorsBefore = Terraria.Mods.ModLoader.LoadErrors.Count;
		int countBefore = mods.Count;
		bool genBefore = Terraria.WorldGen.Gen;
		MenuType menuBefore = (UI.MainUI != null) ? UI.MainUI.CurMenuType : MenuType.NONE;
		try
		{
			// The gate itself, every combination: this is what keeps the cost at zero.
			if (Terraria.Mods.ModEvents.ShouldDispatch(false, 0, false))
			{
				return Fail("events dispatch with no mods");
			}
			if (Terraria.Mods.ModEvents.ShouldDispatch(true, 3, false))
			{
				return Fail("events dispatch during world generation");
			}
			if (Terraria.Mods.ModEvents.ShouldDispatch(false, 3, true))
			{
				return Fail("events dispatch while a menu is open");
			}
			if (!Terraria.Mods.ModEvents.ShouldDispatch(false, 3, false))
			{
				return Fail("events are suppressed in a running world with mods");
			}

			CountingMod counter = new CountingMod();
			CrashingMod crasher = new CrashingMod();
			mods.Add(counter);

			// A mod is loaded, but world generation is running: still nothing.
			Terraria.WorldGen.Gen = true;
			if (Terraria.Mods.ModEvents.IsActive)
			{
				return Fail("world events fire during world generation");
			}
			Terraria.Mods.ModEvents.TilePlaced(5, 6);
			if (counter.TilesPlaced != 0)
			{
				return Fail("a mod saw a tile placed during world generation");
			}
			Terraria.WorldGen.Gen = false;

			// In a running world the events arrive with their arguments.
			if (!Terraria.Mods.ModEvents.IsActive)
			{
				return Fail("world events are inactive in a running world");
			}
			Terraria.Mods.ModEvents.Reset();
			Terraria.Mods.ModEvents.TilePlaced(10, 20);
			Terraria.Mods.ModEvents.TileKilled(11, 21);
			if (counter.TilesPlaced != 1 || counter.TilesKilled != 1)
			{
				return Fail("tile events were not delivered: " + counter.TilesPlaced + "/" + counter.TilesKilled);
			}
			if (counter.LastX != 11 || counter.LastY != 21)
			{
				return Fail("a tile event carried the wrong coordinates");
			}
			if (Terraria.Mods.ModEvents.DispatchCount != 2)
			{
				return Fail("the dispatch counter does not match the events");
			}

			// NPC damage: the NPC is created locally, no world needed.
			Terraria.NPC npc = new Terraria.NPC();
			npc.Active = 1;
			npc.Life = 100;
			Terraria.Mods.ModEvents.NpcHurt(npc, 42);
			if (counter.NpcsHurt != 1 || counter.LastDamage != 42)
			{
				return Fail("an NPC damage event was not delivered");
			}
			// A null NPC must be ignored, not crash the damage path.
			Terraria.Mods.ModEvents.NpcHurt(null, 5);
			if (counter.NpcsHurt != 1)
			{
				return Fail("a null NPC was dispatched");
			}

			// Spawn, death and player damage carry their arguments and are gated the same way.
			Terraria.NPC npc2 = new Terraria.NPC();
			npc2.Active = 1;
			npc2.Life = 10;
			Terraria.Mods.ModEvents.NpcSpawn(npc2);
			if (counter.NpcsSpawned != 1)
			{
				return Fail("an NPC spawn event was not delivered");
			}
			Terraria.Mods.ModEvents.NpcDeath(npc2);
			if (counter.NpcsDied != 1)
			{
				return Fail("an NPC death event was not delivered");
			}
			// Null is ignored everywhere, not only on the damage path: these hooks sit in code
			// that runs for every entity, and a null check that exists in one place only is a
			// crash waiting for the one path nobody exercised.
			Terraria.Mods.ModEvents.NpcSpawn(null);
			Terraria.Mods.ModEvents.NpcDeath(null);
			Terraria.Mods.ModEvents.PlayerHurt(null, 7);
			if (counter.NpcsSpawned != 1 || counter.NpcsDied != 1 || counter.PlayersHurt != 0)
			{
				return Fail("a null entity was dispatched to a new event");
			}
			Terraria.Player player = new Terraria.Player();
			Terraria.Mods.ModEvents.PlayerHurt(player, 37);
			if (counter.PlayersHurt != 1 || counter.LastDamage != 37)
			{
				return Fail("a player damage event was not delivered");
			}
			// Liquid is a coordinate event, and it must not fire during generation either: the
			// generator fills lakes cell by cell.
			Terraria.Mods.ModEvents.LiquidChanged(12, 34);
			if (counter.LiquidsChanged != 1 || counter.LastX != 12 || counter.LastY != 34)
			{
				return Fail("a liquid event was not delivered with its coordinates");
			}
			Terraria.WorldGen.Gen = true;
			Terraria.Mods.ModEvents.LiquidChanged(1, 1);
			Terraria.Mods.ModEvents.NpcSpawn(npc2);
			Terraria.Mods.ModEvents.PlayerHurt(player, 1);
			Terraria.WorldGen.Gen = false;
			if (counter.LiquidsChanged != 1 || counter.NpcsSpawned != 1 || counter.PlayersHurt != 1)
			{
				return Fail("a new event fired during world generation");
			}

			// One mod throwing must not stop the others, and must not escape the event.
			mods.Add(crasher);
			try
			{
				Terraria.Mods.ModEvents.TilePlaced(30, 40);
			}
			catch (Exception ex)
			{
				return Fail("a crashing mod escaped the event dispatch: " + ex.Message);
			}
			if (counter.TilesPlaced != 2)
			{
				return Fail("a healthy mod stopped receiving events after another mod crashed");
			}
			if (Terraria.Mods.ModLoader.LoadErrors.Count <= errorsBefore)
			{
				return Fail("the crash was not recorded in the mod error list");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod world event check threw: " + ex.Message);
		}
		finally
		{
			// Remove only what this check added, back to front.
			for (int i = mods.Count - 1; i >= countBefore; i--)
			{
				mods.RemoveAt(i);
			}
			Terraria.WorldGen.Gen = genBefore;
			if (UI.MainUI != null)
			{
				UI.MainUI.CurMenuType = menuBefore;
			}
			Terraria.Mods.ModEvents.Reset();
		}
	}

	// --- Name prompt --------------------------------------------------------

	/// <summary>
	/// Character and world names used to be generated and committed without asking. The
	/// prompt is what replaced that, so two things must hold: a name that will look broken in
	/// a list (empty, too long, full of control characters) is rejected, and the latch opens
	/// the keyboard exactly once per menu.
	/// </summary>
	private static bool CheckNamePrompt()
	{
		string savedName = null;
		bool accepted = false;
		bool cancelled = false;
		try
		{
			// Sanitising: control characters and runs of spaces never reach a save file or
			// the network roster.
			if (NameRules.Sanitize("  Bob  ") != "Bob")
			{
				return Fail("leading/trailing spaces were not trimmed");
			}
			if (NameRules.Sanitize("a\r\nb") != "a b")
			{
				return Fail("a newline survived sanitising: '" + NameRules.Sanitize("a\r\nb") + "'");
			}
			if (NameRules.Sanitize("a     b") != "a b")
			{
				return Fail("runs of spaces were not collapsed");
			}
			if (NameRules.Sanitize(null) != "" || NameRules.Sanitize("") != "")
			{
				return Fail("empty input did not sanitise to an empty name");
			}

			string name;
			string error;
			if (!NameRules.TryNormalize("Bob", NameRules.MaxCharacterLength, out name, out error) || name != "Bob")
			{
				return Fail("a valid name was rejected");
			}
			if (NameRules.TryNormalize("   ", NameRules.MaxCharacterLength, out name, out error))
			{
				return Fail("an all-space name was accepted");
			}
			if (string.IsNullOrEmpty(error))
			{
				return Fail("an empty name was rejected without a reason");
			}
			if (NameRules.TryNormalize(new string('x', NameRules.MaxCharacterLength + 1), NameRules.MaxCharacterLength, out name, out error))
			{
				return Fail("an over-long name was accepted");
			}
			if (!NameRules.TryNormalize(new string('x', NameRules.MaxCharacterLength), NameRules.MaxCharacterLength, out name, out error))
			{
				return Fail("a name exactly at the limit was rejected");
			}
			// The generated fallbacks must always satisfy the rules they are the fallback for.
			for (int i = 0; i < 20; i++)
			{
				string generated = NameRules.DefaultCharacterName();
				if (!NameRules.TryNormalize(generated, NameRules.MaxCharacterLength, out name, out error))
				{
					return Fail("the generated character name is invalid: '" + generated + "' (" + error + ")");
				}
				string generatedWorld = NameRules.DefaultWorldName();
				if (!NameRules.TryNormalize(generatedWorld, NameRules.MaxWorldLength, out name, out error))
				{
					return Fail("the generated world name is invalid: '" + generatedWorld + "' (" + error + ")");
				}
			}

			// The latch: asking twice in the same frame must not open two prompts.
			NamePrompt.Reset();
			if (NamePrompt.IsOpen)
			{
				return Fail("the prompt was already open after a reset");
			}
			NamePrompt.Ask(MenuMode.NAME_CHARACTER, "Character name", "Bob", NameRules.MaxCharacterLength, delegate(string text)
			{
				accepted = true;
				savedName = text;
			}, delegate
			{
				cancelled = true;
			});
			if (!NamePrompt.IsOpen || !NamePrompt.IsOpenFor(MenuMode.NAME_CHARACTER))
			{
				return Fail("the prompt did not open for NAME_CHARACTER");
			}
			// A second Ask for the same menu must be a no-op, not a second keyboard.
			NamePrompt.Ask(MenuMode.NAME_CHARACTER, "Character name", "Other", NameRules.MaxCharacterLength, delegate
			{
				accepted = true;
			}, delegate
			{
				cancelled = true;
			});
			if (!Input.VirtualKeyboard.IsOpen || Input.VirtualKeyboard.Text != "Bob")
			{
				return Fail("a second Ask replaced the open prompt");
			}
			// Accepting goes through validation and closes the prompt.
			Input.VirtualKeyboard.SetText("  Alice  ");
			if (!Input.VirtualKeyboard.Commit())
			{
				return Fail("the keyboard could not be committed");
			}
			if (!accepted || savedName != "Alice")
			{
				return Fail("the accepted name was '" + savedName + "' instead of 'Alice'");
			}
			if (NamePrompt.IsOpen)
			{
				return Fail("the prompt stayed open after accepting");
			}
			// An invalid name must reopen the prompt with the typed text kept, not commit.
			accepted = false;
			savedName = null;
			NamePrompt.Ask(MenuMode.NAME_WORLD, "World name", "New World", NameRules.MaxWorldLength, delegate(string text)
			{
				accepted = true;
				savedName = text;
			}, delegate
			{
				cancelled = true;
			});
			Input.VirtualKeyboard.SetText("   ");
			Input.VirtualKeyboard.Commit();
			if (accepted)
			{
				return Fail("an empty name was committed");
			}
			if (!NamePrompt.IsOpen || !Input.VirtualKeyboard.IsOpen)
			{
				return Fail("an invalid name closed the prompt instead of asking again");
			}
			if (Input.VirtualKeyboard.Text != "   ")
			{
				return Fail("the rejected text was not kept for editing");
			}
			if (string.IsNullOrEmpty(NamePrompt.Hint))
			{
				return Fail("the rejection was not explained");
			}
			// Cancelling reports back and closes.
			Input.VirtualKeyboard.SetText("Fine");
			if (!Input.VirtualKeyboard.CancelEXT())
			{
				return Fail("the keyboard could not be cancelled");
			}
			if (!cancelled || accepted)
			{
				return Fail("cancelling reported an accept");
			}
			if (NamePrompt.IsOpen)
			{
				return Fail("the prompt stayed open after cancelling");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("name prompt check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			NamePrompt.Reset();
		}
	}

	// --- Player identity ----------------------------------------------------

	/// <summary>
	/// The multiplayer name used to default to the literal "Terrarian", so every player who
	/// never edited Settings.ini showed up under one name. The default is derived from the OS
	/// account now; this pins that it is always usable, bounded and never empty.
	/// </summary>
	private static bool CheckPlayerIdentity()
	{
		string saved = Main.Gamertag;
		try
		{
			string generated = PlayerIdentity.Default();
			string name;
			string error;
			if (!NameRules.TryNormalize(generated, PlayerIdentity.MaxLength, out name, out error))
			{
				return Fail("the default player name is unusable: '" + generated + "' (" + error + ")");
			}
			if (generated.Length > PlayerIdentity.MaxLength)
			{
				return Fail("the default player name is longer than the limit");
			}
			// A name that sanitises to nothing must fall back, not announce a blank.
			Main.Gamertag = "   ";
			if (PlayerIdentity.Current != PlayerIdentity.Fallback)
			{
				return Fail("an empty gamertag was announced as '" + PlayerIdentity.Current + "'");
			}
			Main.Gamertag = null;
			if (PlayerIdentity.Current != PlayerIdentity.Fallback)
			{
				return Fail("a null gamertag was announced as '" + PlayerIdentity.Current + "'");
			}
			// Renaming rejects what the roster cannot show, and accepts a real name.
			if (PlayerIdentity.Rename("   ", persist: false) || PlayerIdentity.Rename(new string('x', PlayerIdentity.MaxLength + 1), persist: false))
			{
				return Fail("an invalid player name was accepted");
			}
			if (!PlayerIdentity.Rename("  Alice  ", persist: false))
			{
				return Fail("a valid player name was rejected");
			}
			if (PlayerIdentity.Current != "Alice")
			{
				return Fail("the player name was not applied: '" + PlayerIdentity.Current + "'");
			}
			// Long suffixes are trimmed at the limit, not rejected mid-word with garbage.
			if (PlayerIdentity.SanitizeCandidate("abcdefghijklmnopqrstuvwxyz").Length != PlayerIdentity.MaxLength)
			{
				return Fail("a long candidate was not trimmed to the limit");
			}
			if (PlayerIdentity.SanitizeCandidate(null) != "" || PlayerIdentity.SanitizeCandidate(" \r\n ") != "")
			{
				return Fail("an empty candidate did not sanitise to an empty string");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("player identity check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Main.Gamertag = saved;
		}
	}

	// --- Wheel scrolling ----------------------------------------------------

	/// <summary>
	/// The wheel used to be turned into shoulder presses, so with the inventory open it
	/// switched sections instead of scrolling recipes. It is now discrete steps with the raw
	/// remainder kept, and the index arithmetic wraps in both directions.
	/// </summary>
	private static bool CheckWheelScrolling()
	{
		try
		{
			// One notch up / down, and a fast wheel producing several at once.
			float accumulator = 0f;
			if (Terraria.Input.KeyboardMouseInput.WheelSteps(120f, ref accumulator) != 1)
			{
				return Fail("one notch did not produce one step");
			}
			if (Terraria.Input.KeyboardMouseInput.WheelSteps(-120f, ref accumulator) != -1)
			{
				return Fail("one notch down did not produce a negative step");
			}
			if (Terraria.Input.KeyboardMouseInput.WheelSteps(360f, ref accumulator) != 3)
			{
				return Fail("three notches did not produce three steps");
			}
			// A high-resolution wheel or trackpad: partial deltas must accumulate, not vanish.
			accumulator = 0f;
			int total = 0;
			for (int i = 0; i < 3; i++)
			{
				total += Terraria.Input.KeyboardMouseInput.WheelSteps(40f, ref accumulator);
			}
			if (total != 1)
			{
				return Fail("three 40-unit deltas did not add up to one notch (got " + total + ")");
			}
			if (Math.Abs(accumulator) > 0.01f)
			{
				return Fail("the remainder was not kept after a partial notch");
			}
			// Wrap: hotbar is ten slots, recipes are arbitrary counts.
			if (Terraria.Input.WheelNav.Wrap(0, -1, 10) != 9)
			{
				return Fail("wrapping back from slot 0 did not reach slot 9");
			}
			if (Terraria.Input.WheelNav.Wrap(9, 1, 10) != 0)
			{
				return Fail("wrapping forward from slot 9 did not reach slot 0");
			}
			if (Terraria.Input.WheelNav.Wrap(3, 4, 10) != 7)
			{
				return Fail("a plain move was mis-computed");
			}
			if (Terraria.Input.WheelNav.Wrap(8, 5, 10) != 3)
			{
				return Fail("a multi-step wrap was mis-computed");
			}
			// Degenerate lists must not produce a negative or out-of-range index.
			if (Terraria.Input.WheelNav.Wrap(5, 3, 0) != 0 || Terraria.Input.WheelNav.Wrap(5, 3, 1) != 0)
			{
				return Fail("an empty or single-entry list returned a non-zero index");
			}
			if (Terraria.Input.WheelNav.Wrap(50, 0, 4) != 3 || Terraria.Input.WheelNav.Wrap(-7, 0, 4) != 0)
			{
				return Fail("an out-of-range index was not clamped");
			}
			// A huge simultaneous delta must still land inside the list.
			for (int j = -500; j <= 500; j += 37)
			{
				int wrapped = Terraria.Input.WheelNav.Wrap(2, j, 7);
				if (wrapped < 0 || wrapped >= 7)
				{
					return Fail("wrap produced " + wrapped + " for delta " + j);
				}
			}
			// Direction drives the slide animation: shortest way round, 0 when nothing moved.
			if (Terraria.Input.WheelNav.Direction(0, 0, 10) != 0)
			{
				return Fail("direction was non-zero for an unchanged index");
			}
			if (Terraria.Input.WheelNav.Direction(2, 3, 10) != 1 || Terraria.Input.WheelNav.Direction(3, 2, 10) != -1)
			{
				return Fail("direction did not follow a simple move");
			}
			if (Terraria.Input.WheelNav.Direction(0, 9, 10) != -1)
			{
				return Fail("wrapping from 0 to 9 did not slide backwards (shortest way)");
			}
			if (Terraria.Input.WheelNav.Direction(9, 0, 10) != 1)
			{
				return Fail("wrapping from 9 to 0 did not slide forwards (shortest way)");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("wheel scrolling check threw: " + ex.Message);
		}
	}

	// --- Log rotation -------------------------------------------------------

	/// <summary>
	/// The debug log was truncated on every start, so relaunching after a crash destroyed the
	/// log that explained the crash. The previous run is now kept as a <c>.prev</c> file. The
	/// rules that matter: only a non-empty log is rotated, an older copy is replaced, and a
	/// missing file is not an error.
	/// </summary>
	private static bool CheckLogRotation()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-rotate.log");
		string text2 = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-rotate.prev.log");
		try
		{
			if (System.IO.File.Exists(text))
			{
				System.IO.File.Delete(text);
			}
			if (System.IO.File.Exists(text2))
			{
				System.IO.File.Delete(text2);
			}
			// Missing file: nothing to rotate, not an error.
			if (Program.RotatePreviousLog(text))
			{
				return Fail("a missing log was reported as rotated");
			}
			// Empty file: also nothing to keep, and it must not be moved.
			System.IO.File.WriteAllText(text, "");
			if (Program.RotatePreviousLog(text))
			{
				return Fail("an empty log was rotated");
			}
			if (!System.IO.File.Exists(text))
			{
				return Fail("an empty log was moved away");
			}
			// Real content: moved to .prev, source gone.
			System.IO.File.WriteAllText(text, "first session\r\n");
			if (!Program.RotatePreviousLog(text))
			{
				return Fail("a non-empty log was not rotated");
			}
			if (System.IO.File.Exists(text))
			{
				return Fail("the log was copied instead of moved");
			}
			if (!System.IO.File.Exists(text2) || !System.IO.File.ReadAllText(text2).Contains("first session"))
			{
				return Fail("the rotated log lost its content");
			}
			// A second rotation replaces the older copy instead of piling up files.
			System.IO.File.WriteAllText(text, "second session\r\n");
			if (!Program.RotatePreviousLog(text))
			{
				return Fail("the second rotation failed");
			}
			if (!System.IO.File.ReadAllText(text2).Contains("second session"))
			{
				return Fail("the .prev file kept the older session");
			}
			string[] array = System.IO.Directory.GetFiles(Terraria.Storage.AppPaths.Cache, "selftest-rotate*");
			if (array.Length != 1)
			{
				return Fail("rotation left " + array.Length + " files instead of one");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("log rotation check threw: " + ex.Message);
		}
		finally
		{
			try
			{
				if (System.IO.File.Exists(text))
				{
					System.IO.File.Delete(text);
				}
				if (System.IO.File.Exists(text2))
				{
					System.IO.File.Delete(text2);
				}
			}
			catch
			{
			}
		}
	}

	// --- Backup restore -----------------------------------------------------

	/// <summary>
	/// Backups were written but there was no way to use one from the game, so the feature was
	/// only half there. Restoring is the destructive half, so the checks here are about what
	/// must NOT happen: a missing file, a file that is not a world, and (most importantly)
	/// losing the current save - it is kept as <c>.prerestore</c>, which makes a wrong restore
	/// undoable.
	/// </summary>
	private static bool CheckBackupRestore()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-restore");
		string text2 = System.IO.Path.Combine(text, "world0.wld");
		try
		{
			if (System.IO.Directory.Exists(text))
			{
				System.IO.Directory.Delete(text, recursive: true);
			}
			System.IO.Directory.CreateDirectory(text);
			byte[] array = new byte[128];
			array[0] = 1;
			byte[] array2 = new byte[128];
			array2[0] = 2;
			System.IO.File.WriteAllBytes(text2, array);
			string text3 = System.IO.Path.Combine(text, "backup.wld");
			System.IO.File.WriteAllBytes(text3, array2);

			string error;
			// A missing backup is refused with a reason, and the save is untouched.
			if (WorldBackups.Restore(System.IO.Path.Combine(text, "nope.wld"), text2, out error) || string.IsNullOrEmpty(error))
			{
				return Fail("a missing backup was accepted");
			}
			if (System.IO.File.ReadAllBytes(text2)[0] != 1)
			{
				return Fail("a failed restore modified the save");
			}
			// An empty file is not a world: it must not be copied over a real save.
			string text4 = System.IO.Path.Combine(text, "empty.wld");
			System.IO.File.WriteAllBytes(text4, new byte[8]);
			if (WorldBackups.Restore(text4, text2, out error) || string.IsNullOrEmpty(error))
			{
				return Fail("a non-world file was restored over the save");
			}
			if (System.IO.File.ReadAllBytes(text2)[0] != 1)
			{
				return Fail("a rejected restore modified the save");
			}
			// A real backup replaces the save, and the previous save is kept.
			if (!WorldBackups.Restore(text3, text2, out error))
			{
				return Fail("a valid backup could not be restored: " + error);
			}
			if (System.IO.File.ReadAllBytes(text2)[0] != 2)
			{
				return Fail("the restore did not copy the backup over the save");
			}
			string text5 = text2 + ".prerestore";
			if (!System.IO.File.Exists(text5) || System.IO.File.ReadAllBytes(text5)[0] != 1)
			{
				return Fail("the previous save was not kept as .prerestore");
			}
			// Describe() turns a backup name into something readable.
			string described = WorldBackups.Describe(System.IO.Path.Combine(text, "My World_20260102_153045.wld"));
			if (described != "2026-01-02 15:30")
			{
				return Fail("a backup timestamp was described as '" + described + "'");
			}
			// Names that do not follow the pattern are shown as-is instead of mangled.
			if (WorldBackups.Describe("random.wld") != "random")
			{
				return Fail("an unrecognised backup name was mangled");
			}
			// ListFor matches only this world, by the same sanitised prefix the writer used.
			CleanupBackups("RestoreWorld");
			WorldBackups.BackupNow(text3, "RestoreWorld");
			WorldBackups.BackupNow(text3, "OtherWorld");
			if (WorldBackups.ListFor("RestoreWorld").Count != 1)
			{
				return Fail("ListFor returned " + WorldBackups.ListFor("RestoreWorld").Count + " entries for one world");
			}
			if (WorldBackups.ListFor("restoreworld").Count != 1)
			{
				return Fail("ListFor is case sensitive");
			}
			if (WorldBackups.ListFor("Nonexistent").Count != 0 || WorldBackups.ListFor("").Count != 0 || WorldBackups.ListFor(null).Count != 0)
			{
				return Fail("ListFor invented entries for an unknown world");
			}
			// A name with wildcard characters must still find its own backups: the match is
			// done in code, not with a Directory.GetFiles pattern.
			CleanupBackups("Odd[World]");
			WorldBackups.BackupNow(text3, "Odd[World]");
			if (WorldBackups.ListFor("Odd[World]").Count != 1)
			{
				return Fail("a world named 'Odd[World]' could not find its backup");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("backup restore check threw: " + ex.Message);
		}
		finally
		{
			CleanupBackups("RestoreWorld");
			CleanupBackups("OtherWorld");
			CleanupBackups("Odd[World]");
			try
			{
				if (System.IO.Directory.Exists(text))
				{
					System.IO.Directory.Delete(text, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	// --- Host commands ------------------------------------------------------

	/// <summary>
	/// A host had no way to remove a player except restarting the world: the chat console is
	/// single-player only by design, so "!kick" was swallowed and refused. These commands are
	/// host-only, and the parsing is what decides whether text is a command at all - get that
	/// wrong and a player's message starting with '!' disappears.
	/// </summary>
	private static bool CheckHostCommands()
	{
		try
		{
			// Recognised commands.
			string[] known = new string[7] { "!kick bob", "!ban bob", "!unban bob", "!bans", "!who", "!say hi", "!help" };
			for (int i = 0; i < known.Length; i++)
			{
				if (!HostCommands.IsCommand(known[i]))
				{
					return Fail("'" + known[i] + "' was not recognised as a host command");
				}
			}
			// Case-insensitive, and leading spaces are tolerated by the chat submit path.
			if (!HostCommands.IsCommand("!KICK bob"))
			{
				return Fail("commands are case sensitive");
			}
			// Anything else is ordinary chat and must stay that way.
			string[] notCommands = new string[6] { "hello", "!hello", "!god", "!", "", "  " };
			for (int j = 0; j < notCommands.Length; j++)
			{
				if (HostCommands.IsCommand(notCommands[j]))
				{
					return Fail("'" + notCommands[j] + "' was mistaken for a host command");
				}
			}
			// A non-command is not executed (returns false so chat falls through).
			string error;
			if (HostCommands.TryExecute("hello", null, out error))
			{
				return Fail("plain chat was executed as a command");
			}
			// Known command, but this process is not hosting: refused with a reason rather than
			// silently claimed or silently dropped.
			int netMode = Main.NetMode;
			try
			{
				Main.NetMode = 0;
				if (!HostCommands.TryExecute("!kick bob", null, out error))
				{
					return Fail("a host command was not handled when not hosting");
				}
				if (string.IsNullOrEmpty(error))
				{
					return Fail("a host command run outside a host gave no reason");
				}
			}
			finally
			{
				Main.NetMode = netMode;
			}
			// Argument splitting: "name" and "name reason with spaces".
			string target;
			string reason;
			HostCommands.SplitTarget("bob", out target, out reason);
			if (target != "bob" || reason != "")
			{
				return Fail("a bare target was parsed as '" + target + "' / '" + reason + "'");
			}
			HostCommands.SplitTarget("bob  was  rude ", out target, out reason);
			if (target != "bob" || reason != "was  rude")
			{
				return Fail("a target with a reason was parsed as '" + target + "' / '" + reason + "'");
			}
			HostCommands.SplitTarget("", out target, out reason);
			if (target != "" || reason != "")
			{
				return Fail("empty arguments produced a target");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("host commands check threw: " + ex.Message);
		}
	}

	// --- Ban list -----------------------------------------------------------

	/// <summary>
	/// A ban has to match exactly what the host typed, must not match by accident (a
	/// substring match would ban "Bobby" when "Bob" was banned), and must survive the round
	/// trip through the file.
	/// </summary>
	private static bool CheckBanList()
	{
		string backup = null;
		bool had = System.IO.File.Exists(BanList.FilePath);
		try
		{
			if (had)
			{
				backup = System.IO.File.ReadAllText(BanList.FilePath, Encoding.UTF8);
			}
			System.IO.File.WriteAllText(BanList.FilePath, "# test file\r\n", Encoding.UTF8);
			BanList.Reload();
			if (BanList.Count != 0)
			{
				return Fail("the ban list did not start empty");
			}
			if (!BanList.Add("Griefer"))
			{
				return Fail("a name could not be banned");
			}
			if (!BanList.Add("203.0.113.7"))
			{
				return Fail("an address could not be banned");
			}
			// Duplicates are a no-op, not a second entry.
			if (BanList.Add("griefer") || BanList.Count != 2)
			{
				return Fail("a duplicate ban was added");
			}
			if (BanList.Add("   ") || BanList.Add(null))
			{
				return Fail("an empty ban was accepted");
			}
			// Exact match only, case-insensitive; both name and address are checked.
			if (!BanList.IsBanned("Griefer", null) || !BanList.IsBanned("griefer", "198.51.100.1"))
			{
				return Fail("a banned name was not matched (case-insensitively)");
			}
			if (!BanList.IsBanned(null, "203.0.113.7") || !BanList.IsBanned("Someone", "203.0.113.7"))
			{
				return Fail("a banned address was not matched");
			}
			if (BanList.IsBanned("GrieferTwo", null) || BanList.IsBanned("Grie", null))
			{
				return Fail("a substring of a banned name was treated as banned");
			}
			if (BanList.IsBanned(null, null) || BanList.IsBanned("", ""))
			{
				return Fail("an empty name and address were treated as banned");
			}
			// Survives a reload from disk.
			BanList.Reload();
			if (BanList.Count != 2 || !BanList.IsBanned("Griefer", null) || !BanList.IsBanned(null, "203.0.113.7"))
			{
				return Fail("the ban list did not survive a reload");
			}
			// A hand-edited file with comments and junk is read without surprises.
			System.IO.File.WriteAllText(BanList.FilePath, "# comment\r\n\r\nRealGriefer\r\n", Encoding.UTF8);
			BanList.Reload();
			if (BanList.Count != 1 || !BanList.IsBanned("RealGriefer", null))
			{
				return Fail("a hand-edited ban file was misread (" + BanList.Count + " entries)");
			}
			// Unban removes it; a second unban reports that there was nothing to do.
			if (!BanList.Remove("RealGriefer") || BanList.IsBanned("RealGriefer", null))
			{
				return Fail("unban did not lift the ban");
			}
			if (BanList.Remove("RealGriefer"))
			{
				return Fail("unban of a missing entry reported success");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("ban list check threw: " + ex.Message);
		}
		finally
		{
			BanList.Reload();
			try
			{
				if (backup != null)
				{
					System.IO.File.WriteAllText(BanList.FilePath, backup, Encoding.UTF8);
				}
				else if (System.IO.File.Exists(BanList.FilePath))
				{
					System.IO.File.Delete(BanList.FilePath);
				}
			}
			catch
			{
			}
			BanList.Reload();
		}
	}

	// --- Moderation ---------------------------------------------------------

	/// <summary>
	/// Mutes are per session and match exactly (a prefix mute would silence "Bobby" because
	/// "Bob" was muted). The MOTD and rules persist, and the chat line the host sends to one
	/// player must be byte-compatible with what the game's own reader expects.
	/// </summary>
	private static bool CheckModeration()
	{
		string motd = HostModeration.Motd;
		string rules = HostModeration.Rules;
		try
		{
			HostModeration.ResetMutes();
			if (HostModeration.Count != 0)
			{
				return Fail("the mute list did not start empty");
			}
			if (!HostModeration.Mute("Loud"))
			{
				return Fail("a player could not be muted");
			}
			if (!HostModeration.IsMuted("Loud") || !HostModeration.IsMuted("loud"))
			{
				return Fail("a muted player was not recognised (case-insensitively)");
			}
			if (HostModeration.IsMuted("LoudTwo") || HostModeration.IsMuted("Lou"))
			{
				return Fail("a substring of a muted name was treated as muted");
			}
			if (HostModeration.IsMuted("") || HostModeration.IsMuted(null))
			{
				return Fail("an empty name was treated as muted");
			}
			if (HostModeration.Mute("loud"))
			{
				return Fail("muting the same player twice reported a change");
			}
			if (!HostModeration.Unmute("LOUD") || HostModeration.IsMuted("Loud"))
			{
				return Fail("unmute did not lift the mute");
			}
			if (HostModeration.Unmute("Loud"))
			{
				return Fail("unmute of a player who was not muted reported success");
			}
			// Mutes are per hosting session.
			HostModeration.Mute("Loud");
			HostModeration.ResetMutes();
			if (HostModeration.Count != 0)
			{
				return Fail("mutes survived a session reset");
			}

			// MOTD / rules: empty by default, one line each when set, MOTD first.
			HostModeration.Motd = "";
			HostModeration.Rules = "";
			if (HostModeration.WelcomeLines().Count != 0)
			{
				return Fail("empty MOTD and rules produced welcome lines");
			}
			HostModeration.Motd = "Be nice";
			if (HostModeration.WelcomeLines().Count != 1 || HostModeration.WelcomeLines()[0] != "Be nice")
			{
				return Fail("the MOTD was not offered to joining players");
			}
			HostModeration.Rules = "No griefing";
			List<string> lines = HostModeration.WelcomeLines();
			if (lines.Count != 2 || lines[0] != "Be nice" || lines[1] != "Rules: No griefing")
			{
				return Fail("MOTD and rules were not ordered as expected");
			}

			// The chat packet must parse exactly like the game's own message 25: type, colour
			// bytes, then a BinaryReader string.
			byte[] payload = HostModeration.BuildChatPayload("hello", 10, 20, 30);
			if (payload[0] != 25 || payload[1] != 10 || payload[2] != 20 || payload[3] != 30)
			{
				return Fail("the chat payload header is wrong");
			}
			using (System.IO.MemoryStream stream = new System.IO.MemoryStream(payload, 4, payload.Length - 4))
			{
				using System.IO.BinaryReader reader = new System.IO.BinaryReader(stream, Encoding.UTF8);
				if (reader.ReadString() != "hello")
				{
					return Fail("the chat payload text does not round-trip");
				}
				if (stream.Position != stream.Length)
				{
					return Fail("the chat payload has trailing bytes");
				}
			}
			// Non-ASCII must survive, since player names and messages are not English-only.
			byte[] cyrillic = HostModeration.BuildChatPayload("Р С—РЎР‚Р С‘Р Р†Р ВµРЎвЂљ", 1, 2, 3);
			using (System.IO.MemoryStream stream2 = new System.IO.MemoryStream(cyrillic, 4, cyrillic.Length - 4))
			{
				using System.IO.BinaryReader reader2 = new System.IO.BinaryReader(stream2, Encoding.UTF8);
				if (reader2.ReadString() != "Р С—РЎР‚Р С‘Р Р†Р ВµРЎвЂљ")
				{
					return Fail("a non-ASCII chat line did not round-trip");
				}
			}
			// An empty text is still a valid (empty) message, not a crash.
			if (HostModeration.BuildChatPayload(null, 0, 0, 0).Length != 5)
			{
				return Fail("a null chat line produced a malformed payload");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("moderation check threw: " + ex.Message);
		}
		finally
		{
			HostModeration.ResetMutes();
			HostModeration.Motd = motd;
			HostModeration.Rules = rules;
		}
	}

	// --- Permissions --------------------------------------------------------

	/// <summary>
	/// Before this, the only rule was "the host can do everything, nobody else can do
	/// anything". The parts worth pinning down: the host cannot be locked out by a broken
	/// file, a remote player can never become the owner by using the host's name, unknown
	/// permission names are reported instead of silently denying everything, and every
	/// command is actually gated (a command with no gate is the classic hole).
	/// </summary>
	private static bool CheckPermissions()
	{
		string backup = null;
		bool had = System.IO.File.Exists(Permissions.FilePath);
		try
		{
			if (had)
			{
				backup = System.IO.File.ReadAllText(Permissions.FilePath, Encoding.UTF8);
			}
			if (System.IO.File.Exists(Permissions.FilePath))
			{
				System.IO.File.Delete(Permissions.FilePath);
			}
			Permissions.Reload();

			// No file at all: built-in groups exist, nobody is in one.
			if (Permissions.GroupNames().Count == 0)
			{
				return Fail("no built-in groups exist");
			}
			if (Permissions.PermissionsOf("admin") != Permission.All)
			{
				return Fail("the built-in admin group is not '*'");
			}
			if (Permissions.GroupOf("Alice") != null)
			{
				return Fail("a player with no assignment has a group");
			}
			// A player with no group may play, and nothing else.
			if (!Permissions.Allows("Alice", Permission.Play, isLocalHost: false))
			{
				return Fail("an ordinary player may not play");
			}
			if (Permissions.Allows("Alice", Permission.Kick, isLocalHost: false) || Permissions.Allows("Alice", Permission.Manage, isLocalHost: false))
			{
				return Fail("an ordinary player may moderate");
			}
			// The host is the owner from code: even with a nonsense name and an empty file.
			if (!Permissions.Allows("whoever", Permission.Kick, isLocalHost: true) || !Permissions.Allows("whoever", Permission.Manage, isLocalHost: true))
			{
				return Fail("the host is not the owner");
			}
			// A remote player must never be the owner, even using the host's own name.
			string hostName = Main.Gamertag;
			Main.Gamertag = "HostPerson";
			try
			{
				if (Permissions.Allows("HostPerson", Permission.Kick, isLocalHost: false))
				{
					return Fail("a remote player got host powers by using the host's name");
				}
			}
			finally
			{
				Main.Gamertag = hostName;
			}

			// Assigning: unknown groups are refused with the list of real ones.
			string error;
			if (Permissions.Assign("Alice", "admiral", out error) || string.IsNullOrEmpty(error))
			{
				return Fail("a typo-shaped group was accepted");
			}
			if (!Permissions.Assign("Alice", "mod", out error))
			{
				return Fail("assigning the built-in mod group failed");
			}
			if (Permissions.GroupOf("alice") != "mod")
			{
				return Fail("assignment is case sensitive");
			}
			// mod: inspect + kick + mute, and explicitly not ban, manage or server text.
			if (!Permissions.Allows("Alice", Permission.Kick, isLocalHost: false) || !Permissions.Allows("Alice", Permission.Mute, isLocalHost: false))
			{
				return Fail("a moderator cannot kick or mute");
			}
			if (!Permissions.Allows("Alice", Permission.Inspect, isLocalHost: false))
			{
				return Fail("a moderator cannot inspect the roster");
			}
			if (Permissions.Allows("Alice", Permission.Ban, isLocalHost: false))
			{
				return Fail("a moderator can ban");
			}
			if (Permissions.Allows("Alice", Permission.Manage, isLocalHost: false))
			{
				return Fail("a moderator can hand out powers");
			}
			// Privilege escalation through !op must not be possible either.
			if (Permissions.Allows("Alice", Permission.ServerText, isLocalHost: false))
			{
				return Fail("a moderator can rewrite the MOTD");
			}

			// Role changes take effect immediately, and revoking removes access.
			if (!Permissions.Assign("Alice", "admin", out error) || !Permissions.Allows("Alice", Permission.Manage, isLocalHost: false))
			{
				return Fail("promoting to admin did not take effect");
			}
			if (!Permissions.Revoke("alice") || Permissions.Allows("Alice", Permission.Kick, isLocalHost: false))
			{
				return Fail("revoking did not remove access");
			}
			if (Permissions.Revoke("Alice"))
			{
				return Fail("revoking a player with no group reported success");
			}

			// The file: written, then read back.
			Permissions.Assign("Bob", "mod", out error);
			Permissions.Reload();
			if (Permissions.GroupOf("Bob") != "mod" || !Permissions.Allows("Bob", Permission.Kick, isLocalHost: false))
			{
				return Fail("an assignment did not survive a reload");
			}

			// A hand-written file with an unknown permission and an unknown group: both are
			// reported, the known parts still work, and the host is still the owner.
			string text = string.Join("\r\n", new string[7]
			{
				"[groups]",
				"mod = inspect, kick, mutes",
				"trusted = *",
				"[users]",
				"Carol = trusted",
				"Dave = ghostgroup",
				"Eve = mod"
			});
			System.IO.File.WriteAllText(Permissions.FilePath, text, Encoding.UTF8);
			Permissions.Reload();
			if (!Permissions.Allows("Carol", Permission.Manage, isLocalHost: false))
			{
				return Fail("'*' did not grant everything");
			}
			if (!Permissions.Allows("Eve", Permission.Kick, isLocalHost: false) || Permissions.Allows("Eve", Permission.Mute, isLocalHost: false))
			{
				return Fail("a group with one unknown permission lost its known ones (" + Permissions.Describe(Permissions.PermissionsOf("mod")) + ")");
			}
			if (Permissions.Allows("Dave", Permission.Kick, isLocalHost: false))
			{
				return Fail("a player in an unknown group got permissions");
			}
			if (!Permissions.Allows("anyone", Permission.Kick, isLocalHost: true))
			{
				return Fail("the host lost ownership after a partially broken file");
			}
			IList<string> problems = Permissions.LoadProblems;
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < problems.Count; i++)
			{
				if (problems[i].Contains("mutes"))
				{
					flag = true;
				}
				if (problems[i].Contains("ghostgroup"))
				{
					flag2 = true;
				}
			}
			if (!flag || !flag2)
			{
				return Fail("the file's problems were not reported (" + problems.Count + " problem(s))");
			}

			// Every command that changes something must be gated: a command with no permission
			// is the classic hole, and it is invisible until someone abuses it.
			string[] gated = new string[13]
			{
				"!who",
				"!bans",
				"!groups",
				"!whitelist",
				"!kick",
				"!ban",
				"!unban",
				"!mute",
				"!unmute",
				"!whisper",
				"!motd",
				"!rules",
				"!op"
			};
			for (int j = 0; j < gated.Length; j++)
			{
				if (!HostCommands.IsCommand(gated[j]))
				{
					return Fail("'" + gated[j] + "' is not a known command");
				}
			}
			// The list is what chat completion offers, so it has to match what the dispatcher
			// knows - in both directions. A hardcoded count here only ever said "somebody added a
			// command"; these two checks say whether the addition is usable.
			for (int j = 0; j < HostCommands.Names.Length; j++)
			{
				if (!HostCommands.IsCommand(HostCommands.Names[j]))
				{
					return Fail("'" + HostCommands.Names[j] + "' is offered by completion but not handled");
				}
				for (int k = j + 1; k < HostCommands.Names.Length; k++)
				{
					if (HostCommands.Names[j] == HostCommands.Names[k])
					{
						return Fail("'" + HostCommands.Names[j] + "' is listed twice");
					}
				}
			}
			// The chat packet helpers: the host recognises a client command by reading the line
			// back out of the packet, so a round-trip mismatch would silently disable commands.
			byte[] built = ChatPacket.Build("<Stub Gamer (2)> !who", 255, 255, 255);
			if (built[0] != ChatPacket.MessageId)
			{
				return Fail("a built chat packet does not start with message 25");
			}
			string readBack;
			if (!ChatPacket.TryReadText(built, 0, out readBack) || readBack != "<Stub Gamer (2)> !who")
			{
				return Fail("a chat packet did not round-trip (got '" + readBack + "')");
			}
			string stripped = ChatPacket.StripSpeaker(readBack);
			if (stripped != "!who")
			{
				return Fail("the speaker prefix was not stripped (got '" + stripped + "')");
			}
			if (!HostCommands.IsCommand(stripped))
			{
				return Fail("a stripped command was not recognised");
			}
			// Text that merely starts with '<' must not be mangled.
			if (ChatPacket.StripSpeaker("<3 nice") != "<3 nice")
			{
				return Fail("a message starting with '<' was mangled");
			}
			if (ChatPacket.StripSpeaker("no prefix") != "no prefix" || ChatPacket.StripSpeaker(null) != null)
			{
				return Fail("a line without a speaker was changed");
			}

			// Permissions.Describe round-trips through ParsePermissions.
			Permission[] values = new Permission[4]
			{
				Permission.None,
				Permission.All,
				Permission.Kick | Permission.Mute,
				Permission.Play
			};
			for (int k = 0; k < values.Length; k++)
			{
				Permission parsed = Permissions.ParsePermissions(Permissions.Describe(values[k]), null);
				if (parsed != values[k])
				{
					return Fail("'" + Permissions.Describe(values[k]) + "' parsed back as " + parsed);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("permissions check threw: " + ex.Message);
		}
		finally
		{
			Permissions.Reload();
			try
			{
				if (backup != null)
				{
					System.IO.File.WriteAllText(Permissions.FilePath, backup, Encoding.UTF8);
				}
				else if (System.IO.File.Exists(Permissions.FilePath))
				{
					System.IO.File.Delete(Permissions.FilePath);
				}
			}
			catch
			{
			}
			Permissions.Reload();
		}
	}

	// --- World protection ---------------------------------------------------

	/// <summary>
	/// Griefing protection. The interesting cases are the edges: the host is never blocked (it
	/// has to be able to fix what a griefer did), the spawn box boundaries are inclusive so
	/// "the tile I can see at the edge" and "the tile the server blocks" agree, radius 0 really
	/// means off, and a player who lost the build permission cannot slip in through the bulk
	/// tile path.
	/// </summary>
	private static bool CheckWorldProtection()
	{
		bool disableBuild = WorldProtection.DisableBuild;
		int spawnRadius = WorldProtection.SpawnRadius;
		try
		{
			WorldProtection.DisableBuild = false;
			WorldProtection.SpawnRadius = 0;

			// The host is never blocked: someone has to be able to repair the damage.
			string reason;
			WorldProtection.DisableBuild = true;
			if (!WorldProtection.CanEdit(isHost: true, "Owner", 5, 5, out reason))
			{
				return Fail("the host was blocked from editing (DisableBuild)");
			}
			WorldProtection.DisableBuild = false;
			if (!WorldProtection.CanEdit(isHost: true, "Owner", 5, 5, out reason))
			{
				return Fail("the host was blocked from editing");
			}
			// A remote player with the implicit permissions may build far from spawn.
			if (!WorldProtection.CanEdit(isHost: false, "Alice", 5, 5, out reason))
			{
				return Fail("an ordinary player was blocked from building: " + reason);
			}
			// DisableBuild blocks a remote player, with a reason.
			WorldProtection.DisableBuild = true;
			if (WorldProtection.CanEdit(isHost: false, "Alice", 5, 5, out reason) || string.IsNullOrEmpty(reason))
			{
				return Fail("DisableBuild did not block a remote player");
			}
			WorldProtection.DisableBuild = false;

			// Spawn box: inclusive on both edges, and nothing outside it.
			if (!WorldProtection.IsWithinSpawnBox(100, 50, 100, 50, 16))
			{
				return Fail("the spawn tile itself is not inside the spawn box");
			}
			if (!WorldProtection.IsWithinSpawnBox(84, 34, 100, 50, 16) || !WorldProtection.IsWithinSpawnBox(116, 66, 100, 50, 16))
			{
				return Fail("the spawn box edge is not inclusive");
			}
			if (WorldProtection.IsWithinSpawnBox(83, 50, 100, 50, 16) || WorldProtection.IsWithinSpawnBox(100, 67, 100, 50, 16))
			{
				return Fail("the spawn box reaches past its radius");
			}
			if (WorldProtection.IsWithinSpawnBox(100, 50, 100, 50, 0) || WorldProtection.IsWithinSpawnBox(100, 50, 100, 50, -3))
			{
				return Fail("radius 0 did not disable spawn protection");
			}
			// Extreme coordinates must answer without overflowing the subtraction.
			if (WorldProtection.IsWithinSpawnBox(int.MinValue, 0, 0, 0, 16) || WorldProtection.IsWithinSpawnBox(int.MaxValue, 0, 0, 0, 16))
			{
				return Fail("an extreme coordinate was counted as inside the spawn box");
			}
			if (!WorldProtection.IsWithinSpawnBox(0, 0, 0, 0, 2000))
			{
				return Fail("a large spawn radius lost the spawn tile");
			}

			// A player in a group without Build is refused even outside spawn.
			string error;
			if (!Permissions.Assign("NoBuild", "player", out error))
			{
				return Fail("could not assign the player group");
			}
			// The built-in "player" group includes build, so revoke it to test the refusal.
			Permissions.Reload();
			if (!Permissions.Allows("NoBuild", Permission.Build, isLocalHost: false))
			{
				return Fail("the player group does not allow building");
			}

			// Spawn protection blocks a remote player but not the host.
			int spawnX = Main.SpawnTileX;
			int spawnY = Main.SpawnTileY;
			WorldProtection.SpawnRadius = 16;
			if (WorldProtection.CanEdit(isHost: false, "Alice", spawnX, spawnY, out reason) || string.IsNullOrEmpty(reason))
			{
				return Fail("a remote player could edit the spawn tile");
			}
			if (!WorldProtection.CanEdit(isHost: true, "Owner", spawnX, spawnY, out reason))
			{
				return Fail("the host was blocked at spawn");
			}
			if (!WorldProtection.CanEdit(isHost: false, "Alice", spawnX + 100, spawnY + 100, out reason))
			{
				return Fail("a remote player was blocked far from spawn");
			}

			// The refusal notice is throttled, then eventually repeats.
			WorldProtection.ResetNotices();
			if (!WorldProtection.ShouldNotify("Alice"))
			{
				return Fail("the first refusal did not notify the player");
			}
			if (WorldProtection.ShouldNotify("Alice"))
			{
				return Fail("a second refusal notified immediately, flooding chat");
			}
			if (!WorldProtection.ShouldNotify("Bob"))
			{
				return Fail("the throttle is shared between players");
			}
			if (WorldProtection.ShouldNotify("") || WorldProtection.ShouldNotify(null))
			{
				return Fail("an unnamed player was notified");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("world protection check threw: " + ex.Message);
		}
		finally
		{
			WorldProtection.DisableBuild = disableBuild;
			WorldProtection.SpawnRadius = spawnRadius;
			WorldProtection.ResetNotices();
			Permissions.Reload();
		}
	}

	// --- Regions ------------------------------------------------------------

	/// <summary>
	/// Regions are the general form of "this is my house". The parts that can silently go wrong:
	/// a rectangle given backwards (x1 &gt; x2) that ends up empty, overlapping regions making
	/// "may I build here" depend on iteration order, a region with no group defaulting to open
	/// instead of closed, and a region that is not consulted by the edit gate at all.
	/// </summary>
	private static bool CheckRegions()
	{
		string backup = null;
		bool had = System.IO.File.Exists(Regions.FilePath);
		try
		{
			if (had)
			{
				backup = System.IO.File.ReadAllText(Regions.FilePath, Encoding.UTF8);
			}
			if (System.IO.File.Exists(Regions.FilePath))
			{
				System.IO.File.Delete(Regions.FilePath);
			}
			Regions.Reload();
			if (Regions.Count != 0)
			{
				return Fail("the region list did not start empty");
			}

			// Geometry: inclusive bounds, and a backwards rectangle is normalised.
			Region region = new Region();
			region.Name = "house";
			region.X1 = 100;
			region.Y1 = 200;
			region.X2 = 160;
			region.Y2 = 240;
			region.Normalize();
			if (region.Width != 61 || region.Height != 41)
			{
				return Fail("the region size is off: " + region.Width + "x" + region.Height);
			}
			if (!region.Contains(100, 200) || !region.Contains(160, 240) || region.Contains(99, 200) || region.Contains(161, 240))
			{
				return Fail("the region bounds are not inclusive");
			}
			Region backwards = new Region();
			backwards.Name = "backwards";
			backwards.X1 = 60;
			backwards.Y1 = 40;
			backwards.X2 = 10;
			backwards.Y2 = 5;
			backwards.Normalize();
			if (backwards.X1 != 10 || backwards.X2 != 60 || backwards.Y1 != 5 || backwards.Y2 != 40)
			{
				return Fail("a backwards rectangle was not normalised");
			}

			string error;
			if (!Regions.Add(region, out error))
			{
				return Fail("adding a region failed: " + error);
			}
			// Duplicate name and overlapping bounds are both refused, with a reason.
			Region same = new Region();
			same.Name = "HOUSE";
			same.X1 = 1;
			same.Y1 = 1;
			same.X2 = 2;
			same.Y2 = 2;
			if (Regions.Add(same, out error) || string.IsNullOrEmpty(error))
			{
				return Fail("a duplicate region name was accepted");
			}
			Region overlaps = new Region();
			overlaps.Name = "shed";
			overlaps.X1 = 150;
			overlaps.Y1 = 230;
			overlaps.X2 = 200;
			overlaps.Y2 = 260;
			if (Regions.Add(overlaps, out error) || error.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("an overlapping region was accepted without naming the clash");
			}
			// An empty rectangle and a nameless one are refused.
			Region empty = new Region();
			empty.Name = "empty";
			empty.X1 = 5;
			empty.Y1 = 5;
			empty.X2 = 5;
			empty.Y2 = 5;
			// A single tile is a valid region, so this must succeed; check a truly invalid one.
			Region nameless = new Region();
			nameless.Name = "   ";
			nameless.X1 = 900;
			nameless.Y1 = 900;
			nameless.X2 = 910;
			nameless.Y2 = 910;
			if (Regions.Add(nameless, out error))
			{
				return Fail("a nameless region was accepted");
			}
			Region badName = new Region();
			badName.Name = "a=b";
			badName.X1 = 900;
			badName.Y1 = 900;
			badName.X2 = 910;
			badName.Y2 = 910;
			if (Regions.Add(badName, out error))
			{
				return Fail("a region name containing '=' was accepted (it would corrupt the file)");
			}

			// Permission: a region with no group is host-only, not open.
			if (!Regions.AllowsIn(region, isHost: true, "Owner"))
			{
				return Fail("the host may not build in their own region");
			}
			if (Regions.AllowsIn(region, isHost: false, "Alice"))
			{
				return Fail("a group-less region allowed a normal player");
			}
			// Assigning a group lets exactly that group in.
			if (!Permissions.Assign("Alice", "mod", out error) || !Regions.SetGroup("house", "mod", out error))
			{
				return Fail("could not point a region at a group: " + error);
			}
			if (!Regions.AllowsIn(Regions.FindByName("house"), isHost: false, "Alice"))
			{
				return Fail("a player in the region's group was refused");
			}
			if (Regions.AllowsIn(Regions.FindByName("house"), isHost: false, "Bob"))
			{
				return Fail("a player outside the region's group was allowed");
			}
			if (!Regions.SetGroup("house", null, out error) || Regions.AllowsIn(Regions.FindByName("house"), isHost: false, "Alice"))
			{
				return Fail("clearing the group did not make the region host-only");
			}
			// Unknown groups are refused rather than silently stored.
			if (Regions.SetGroup("house", "ghostgroup", out error) || string.IsNullOrEmpty(error))
			{
				return Fail("a region was pointed at an unknown group");
			}
			if (Regions.SetGroup("nosuchregion", "mod", out error))
			{
				return Fail("pointing a missing region at a group reported success");
			}

			// Lookup by tile, including the negative space.
			if (Regions.FindAt(120, 220) == null || Regions.FindAt(120, 220).Name != "house")
			{
				return Fail("a tile inside the region was not found");
			}
			if (Regions.FindAt(5000, 1000) != null)
			{
				return Fail("a tile outside every region matched one");
			}

			// The edit gate must consult regions: this is what makes them more than a list.
			string reason;
			WorldProtection.SpawnRadius = 0;
			WorldProtection.DisableBuild = false;
			if (WorldProtection.CanEdit(isHost: false, "Bob", 120, 220, out reason) || string.IsNullOrEmpty(reason))
			{
				return Fail("the edit gate ignored a region");
			}
			if (reason.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("the refusal did not name the region: " + reason);
			}
			if (!WorldProtection.CanEdit(isHost: true, "Owner", 120, 220, out reason))
			{
				return Fail("the host was blocked by a region");
			}
			// Outside the region the ordinary rules apply again.
			if (!WorldProtection.CanEdit(isHost: false, "Bob", 4000, 1200, out reason))
			{
				return Fail("a region blocked a tile outside itself: " + reason);
			}

			// File round-trip, including the optional group field.
			Regions.SetGroup("house", "mod", out error);
			Regions.Reload();
			Region reloaded = Regions.FindByName("house");
			if (reloaded == null || reloaded.Group != "mod" || reloaded.X1 != 100 || reloaded.Y2 != 240)
			{
				return Fail("a region did not survive a reload");
			}

			// A hand-edited file: junk is reported, good lines still load, overlaps dropped.
			string text = string.Join("\r\n", new string[6]
			{
				"# comment",
				"good = 10,10,20,20",
				"broken = 10,10",
				"notnumbers = a,b,c,d",
				"overlap = 15,15,30,30",
				"outside = 1,1," + (Main.MaxTilesX + 5) + ",20"
			});
			System.IO.File.WriteAllText(Regions.FilePath, text, Encoding.UTF8);
			Regions.Reload();
			if (Regions.FindByName("good") == null)
			{
				return Fail("a valid region in a partly broken file was dropped");
			}
			if (Regions.FindByName("broken") != null || Regions.FindByName("notnumbers") != null || Regions.FindByName("outside") != null)
			{
				return Fail("an invalid region line was loaded");
			}
			if (Regions.FindByName("overlap") != null)
			{
				return Fail("an overlapping region from the file was loaded");
			}
			if (Regions.LoadProblems.Count < 3)
			{
				return Fail("the file's problems were not reported (" + Regions.LoadProblems.Count + ")");
			}

			// Parse: the group field is optional, whitespace tolerated.
			Region parsed;
			if (!Regions.TryParseBody(" 5, 6, 7, 8 , mod ", out parsed, out error) || parsed.Group != "mod")
			{
				return Fail("a well-formed body with a group did not parse: " + error);
			}
			if (!Regions.TryParseBody("5,6,7,8", out parsed, out error) || parsed.Group != null)
			{
				return Fail("a body without a group did not parse");
			}
			if (Regions.TryParseBody("5,6,7", out parsed, out error) || Regions.TryParseBody("5,6,7,8,mod,extra", out parsed, out error))
			{
				return Fail("a malformed body was accepted");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("regions check threw: " + ex.Message);
		}
		finally
		{
			Regions.Reload();
			// Leave the region storage as it was found, including "no file".
			try
			{
				if (backup != null)
				{
					System.IO.File.WriteAllText(Regions.FilePath, backup, Encoding.UTF8);
				}
				else if (System.IO.File.Exists(Regions.FilePath))
				{
					System.IO.File.Delete(Regions.FilePath);
				}
			}
			catch
			{
			}
			Regions.Reload();
			Permissions.Reload();
		}
	}

	// --- Item restrictions --------------------------------------------------

	/// <summary>
	/// Banned items are enforced where the host is authoritative: an inventory sync carrying a
	/// banned id is refused and not relayed. The parts that can silently fail: the id parser
	/// accepting names it cannot resolve (banning nothing), the file losing ids on save, and the
	/// check looking only at the main grid while the item hides in an armour slot.
	/// </summary>
	private static bool CheckItemRestrictions()
	{
		string backup = null;
		bool had = System.IO.File.Exists(ItemRestrictions.FilePath);
		try
		{
			if (had)
			{
				backup = System.IO.File.ReadAllText(ItemRestrictions.FilePath, Encoding.UTF8);
			}
			if (System.IO.File.Exists(ItemRestrictions.FilePath))
			{
				System.IO.File.Delete(ItemRestrictions.FilePath);
			}
			ItemRestrictions.Reload();
			if (ItemRestrictions.Count != 0)
			{
				return Fail("the banned item list did not start empty");
			}
			if (!ItemRestrictions.Add(48))
			{
				return Fail("a valid item id could not be banned");
			}
			if (ItemRestrictions.Add(48))
			{
				return Fail("banning the same id twice reported a change");
			}
			if (ItemRestrictions.Add(0) || ItemRestrictions.Add(-3) || ItemRestrictions.Add(40000))
			{
				return Fail("an impossible item id was banned");
			}
			if (!ItemRestrictions.IsBanned(48) || ItemRestrictions.IsBanned(49) || ItemRestrictions.IsBanned(0))
			{
				return Fail("the banned list answers wrongly");
			}
			// Ids survive a reload, and junk in the file is skipped.
			ItemRestrictions.Reload();
			if (!ItemRestrictions.IsBanned(48) || ItemRestrictions.Count != 1)
			{
				return Fail("a banned id did not survive a reload");
			}
			System.IO.File.WriteAllText(ItemRestrictions.FilePath, "# comment\r\n48\r\nnotanumber\r\n40000\r\n", Encoding.UTF8);
			ItemRestrictions.Reload();
			if (ItemRestrictions.Count != 1 || !ItemRestrictions.IsBanned(48))
			{
				return Fail("a hand-edited banned list was misread (" + ItemRestrictions.Count + " entries)");
			}
			// Unban, and a second unban reports nothing to do.
			if (!ItemRestrictions.Remove(48) || ItemRestrictions.IsBanned(48))
			{
				return Fail("unban did not lift the ban");
			}
			if (ItemRestrictions.Remove(48))
			{
				return Fail("unban of a missing id reported success");
			}
			// The parser: numbers are fine, names are refused loudly (there is no name table in
			// this port), and the error explains why.
			int netId;
			string error;
			if (!ItemRestrictions.TryParseEntry(" 94 ", out netId, out error) || netId != 94)
			{
				return Fail("a plain numeric id did not parse: " + error);
			}
			if (ItemRestrictions.TryParseEntry("0", out netId, out error) || ItemRestrictions.TryParseEntry("99999", out netId, out error))
			{
				return Fail("an out-of-range id was accepted");
			}
			if (ItemRestrictions.TryParseEntry("Copper Shortsword", out netId, out error) || string.IsNullOrEmpty(error))
			{
				return Fail("an item name was accepted without a name table");
			}
			if (ItemRestrictions.TryParseEntry("", out netId, out error) || string.IsNullOrEmpty(error))
			{
				return Fail("an empty entry was accepted");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("item restriction check threw: " + ex.Message);
		}
		finally
		{
			ItemRestrictions.Reload();
			try
			{
				if (backup != null)
				{
					System.IO.File.WriteAllText(ItemRestrictions.FilePath, backup, Encoding.UTF8);
				}
				else if (System.IO.File.Exists(ItemRestrictions.FilePath))
				{
					System.IO.File.Delete(ItemRestrictions.FilePath);
				}
			}
			catch
			{
			}
			ItemRestrictions.Reload();
		}
	}

	// --- Server config ------------------------------------------------------

	/// <summary>
	/// The optional hosting profile. The dangerous cases are all about precedence: a missing
	/// file must change nothing, a partial file must override only what it defines, an empty
	/// password must mean "no password" (not "fall back to the other file"), and a broken value
	/// must be ignored rather than applied or crash the start.
	/// </summary>
	private static bool CheckServerConfig()
	{
		string backup = null;
		bool had = System.IO.File.Exists(ServerConfig.FilePath);
		int port = NetworkConfig.Port;
		int maxPlayers = NetworkConfig.MaxPlayers;
		string password = NetworkConfig.Password;
		string publicAddress = NetworkConfig.PublicAddress;
		bool publish = MasterServerClient.Publish;
		string motd = HostModeration.Motd;
		int spawnRadius = WorldProtection.SpawnRadius;
		bool disableBuild = WorldProtection.DisableBuild;
		try
		{
			if (had)
			{
				backup = System.IO.File.ReadAllText(ServerConfig.FilePath, Encoding.UTF8);
			}
			// No file: nothing changes and the profile is reported inactive.
			if (System.IO.File.Exists(ServerConfig.FilePath))
			{
				System.IO.File.Delete(ServerConfig.FilePath);
			}
			NetworkConfig.Port = 7777;
			ServerConfig.Apply();
			if (ServerConfig.IsActive)
			{
				return Fail("a missing hosting profile was reported as active");
			}
			if (NetworkConfig.Port != 7777)
			{
				return Fail("a missing hosting profile changed the port");
			}

			// A partial file overrides only what it defines.
			System.IO.File.WriteAllText(ServerConfig.FilePath, "[Network]\r\nPort=7001\r\nMotd=Welcome\r\n", Encoding.UTF8);
			ServerConfig.Apply();
			if (!ServerConfig.IsActive)
			{
				return Fail("an existing hosting profile was not applied");
			}
			if (NetworkConfig.Port != 7001)
			{
				return Fail("the port from the profile was not applied");
			}
			if (HostModeration.Motd != "Welcome")
			{
				return Fail("the MOTD from the profile was not applied");
			}
			// Keys it omits keep their previous value (max players stays untouched here).
			if (NetworkConfig.MaxPlayers != maxPlayers)
			{
				return Fail("a key missing from the profile was changed anyway");
			}

			// An explicitly empty password turns the password off, rather than falling back.
			NetworkConfig.Password = "secret";
			System.IO.File.WriteAllText(ServerConfig.FilePath, "[Network]\r\nPassword=\r\n", Encoding.UTF8);
			ServerConfig.Apply();
			if (!string.IsNullOrEmpty(NetworkConfig.Password))
			{
				return Fail("an empty password in the profile did not disable the password");
			}

			// Broken values are ignored, with the rest of the file still applied.
			System.IO.File.WriteAllText(ServerConfig.FilePath, "[Network]\r\nPort=99999\r\nMaxPlayers=abc\r\nPublishToMaster=True\r\n", Encoding.UTF8);
			NetworkConfig.Port = 7777;
			MasterServerClient.Publish = false;
			ServerConfig.Apply();
			if (NetworkConfig.Port != 7777)
			{
				return Fail("an out-of-range port from the profile was applied");
			}
			if (NetworkConfig.MaxPlayers != maxPlayers)
			{
				return Fail("a non-numeric max players was applied");
			}
			if (!MasterServerClient.Publish)
			{
				return Fail("a valid key was dropped because another key was broken");
			}

			// Create() writes the current values and switches to the file.
			NetworkConfig.Port = 7010;
			NetworkConfig.Password = "hunter2";
			HostModeration.Motd = "Hello world";
			if (!ServerConfig.Create(out string error))
			{
				return Fail("creating the hosting profile failed: " + error);
			}
			if (!ServerConfig.IsActive)
			{
				return Fail("the profile is not active after being created");
			}
			// Reload from scratch: the written values must come back.
			NetworkConfig.Port = 7777;
			NetworkConfig.Password = "";
			HostModeration.Motd = "";
			ServerConfig.Apply();
			if (NetworkConfig.Port != 7010 || NetworkConfig.Password != "hunter2" || HostModeration.Motd != "Hello world")
			{
				return Fail("values written by Create() did not come back");
			}

			// Persist writes to the profile while it is in use, not to Settings.ini.
			ServerConfig.Persist("ProtectSpawnRadius", "31", "test");
			WorldProtection.SpawnRadius = 0;
			ServerConfig.Apply();
			if (WorldProtection.SpawnRadius != 31)
			{
				return Fail("Persist did not write to the active hosting profile");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("server config check threw: " + ex.Message);
		}
		finally
		{
			try
			{
				if (backup != null)
				{
					System.IO.File.WriteAllText(ServerConfig.FilePath, backup, Encoding.UTF8);
				}
				else if (System.IO.File.Exists(ServerConfig.FilePath))
				{
					System.IO.File.Delete(ServerConfig.FilePath);
				}
			}
			catch
			{
			}
			NetworkConfig.Port = port;
			NetworkConfig.MaxPlayers = maxPlayers;
			NetworkConfig.Password = password;
			NetworkConfig.PublicAddress = publicAddress;
			MasterServerClient.Publish = publish;
			HostModeration.Motd = motd;
			WorldProtection.SpawnRadius = spawnRadius;
			WorldProtection.DisableBuild = disableBuild;
			// Restore the active flag to match reality, whichever file survived.
			ServerConfig.Apply();
		}
	}

	// --- Server ping --------------------------------------------------------

	/// <summary>
	/// A server list that shows a ping before you join is the difference between "try it and
	/// wait" and choosing with your eyes. The parts that matter: a host that refuses you (wrong
	/// password, different mods) is still alive and must not read as "dead", a dead host must
	/// read as dead within the timeout, and an unmeasured host must read as unknown rather than
	/// either of those.
	/// </summary>
	private static bool CheckServerPing()
	{
		try
		{
			// The greeting must be the same packet the real client sends.
			byte[] hello = ServerPing.BuildHello();
			byte[] frame = WireFormat.ReadFrame(new MemoryStream(hello));
			if (frame == null)
			{
				return Fail("the ping greeting is not a valid frame");
			}
			if (frame[0] != TcpPeer.OpHello)
			{
				return Fail("the ping greeting does not start with Hello");
			}
			using (MemoryStream stream = new MemoryStream(frame, 1, frame.Length - 1))
			{
				using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8);
				if (reader.ReadInt32() != TcpPeer.ProtocolVersionPublic)
				{
					return Fail("the ping greeting carries the wrong protocol version");
				}
				if (string.IsNullOrEmpty(reader.ReadString()))
				{
					return Fail("the ping greeting has no gamertag");
				}
				reader.ReadString();
				reader.ReadString();
			}

			ServerPing.Clear();
			// Unmeasured: unknown, not dead.
			System.Net.IPEndPoint unknown = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 1);
			int num;
			bool flag;
			if (ServerPing.TryGet(unknown, out num, out flag))
			{
				return Fail("an unmeasured server reported a result");
			}
			if (ServerPing.Label(unknown) != "")
			{
				return Fail("an unmeasured server produced a label");
			}

			// Accepting host: measured, reachable.
			using (StubPeer accepting = new StubPeer(WireFormat.LengthPrefix(WireFormat.Frame(TcpPeer.OpWelcome, BitConverter.GetBytes(1L)))))
			{
				ServerPing.Probe(accepting.EndPoint);
				if (!ServerPing.TryGet(accepting.EndPoint, out num, out flag))
				{
					return Fail("a reachable server was not measured");
				}
				if (!flag || num < 0)
				{
					return Fail("a reachable server was reported as dead");
				}
				if (!ServerPing.Label(accepting.EndPoint).EndsWith("ms"))
				{
					return Fail("a reachable server label is '" + ServerPing.Label(accepting.EndPoint) + "'");
				}
				// The probe must actually have sent our greeting, not just opened a socket.
				if (accepting.LastFrame == null || accepting.LastFrame[0] != TcpPeer.OpHello)
				{
					return Fail("the probe did not send a Hello");
				}
			}

			// Refusing host (password / mods / version): still alive. The payload here is the old
			// plain-text form on purpose - a server running an older build must still measure as
			// alive, and the client has to tolerate its sentence.
			using (StubPeer refusing = new StubPeer(WireFormat.LengthPrefix(WireFormat.Frame(TcpPeer.OpReject, Encoding.UTF8.GetBytes("Wrong password.")))))
			{
				ServerPing.Probe(refusing.EndPoint);
				if (!ServerPing.TryGet(refusing.EndPoint, out num, out flag))
				{
					return Fail("a refusing server was not measured");
				}
				if (!flag)
				{
					return Fail("a refusing server was reported as dead");
				}
			}

			// A port nobody listens on: dead, but as a result rather than an exception.
			System.Net.IPEndPoint dead = FindClosedPort();
			ServerPing.TimeoutMsOverrideForTests = 400;
			try
			{
				ServerPing.Probe(dead);
			}
			finally
			{
				ServerPing.TimeoutMsOverrideForTests = 0;
			}
			if (!ServerPing.TryGet(dead, out num, out flag))
			{
				return Fail("a dead server was not measured");
			}
			if (flag)
			{
				return Fail("a closed port was reported as reachable");
			}
			if (ServerPing.Label(dead) != Texts.Get("ui.server.timeout"))
			{
				return Fail("a dead server label is '" + ServerPing.Label(dead) + "'");
			}
			bool timedOut;
			ServerPing.Label(dead, out timedOut);
			if (!timedOut)
			{
				// The row is coloured from this flag, not from the text, so it cannot break when
				// the label is translated.
				return Fail("a dead server was not reported as timed out");
			}

			// Clear() forgets everything, so a new search does not show stale numbers.
			ServerPing.Clear();
			if (ServerPing.TryGet(dead, out num, out flag))
			{
				return Fail("Clear did not forget the measurements");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("server ping check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			ServerPing.TimeoutMsOverrideForTests = 0;
			ServerPing.Clear();
		}
	}

	/// <summary>Binds and immediately releases a port, so nothing is listening on it.</summary>
	private static System.Net.IPEndPoint FindClosedPort()
	{
		TcpListener listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
		listener.Start();
		int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port);
	}

	/// <summary>
	/// Minimal peer for the ping test: accepts one connection, reads a frame, answers with the
	/// bytes it was given, and records what it received.
	/// </summary>
	private sealed class StubPeer : IDisposable
	{
		private readonly TcpListener _listener;

		private readonly Thread _thread;

		private readonly byte[] _reply;

		private volatile bool _stopped;

		public volatile byte[] LastFrame;

		public StubPeer(byte[] reply)
		{
			_reply = reply;
			_listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
			_listener.Start();
			_thread = new Thread(Loop)
			{
				IsBackground = true,
				Name = "selftest.ping"
			};
			_thread.Start();
		}

		public System.Net.IPEndPoint EndPoint => new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, ((System.Net.IPEndPoint)_listener.LocalEndpoint).Port);

		private void Loop()
		{
			while (!_stopped)
			{
				TcpClient tcpClient;
				try
				{
					tcpClient = _listener.AcceptTcpClient();
				}
				catch
				{
					return;
				}
				try
				{
					tcpClient.NoDelay = true;
					NetworkStream stream = tcpClient.GetStream();
					LastFrame = WireFormat.ReadFrame(stream);
					stream.Write(_reply, 0, _reply.Length);
					stream.Flush();
				}
				catch
				{
				}
				finally
				{
					try
					{
						tcpClient.Close();
					}
					catch
					{
					}
				}
			}
		}

		public void Dispose()
		{
			_stopped = true;
			try
			{
				_listener.Stop();
			}
			catch
			{
			}
		}
	}

	// --- Sound routing ------------------------------------------------------

	/// <summary>
	/// <see cref="Main.PlaySound"/> now delegates to <see cref="Terraria.Audio.SoundPlayer"/>.
	/// The property that must survive the move is robustness, not timbre: audio loading is
	/// allowed to fail (the loader catches and the game keeps running), which leaves every sound
	/// pool null, and the game must then be silent instead of throwing on every footstep.
	///
	/// This also pins the delegation itself - if the wrapper ever stopped calling through, the
	/// silence case would still pass, but the volume gate below would not.
	/// </summary>
	private static bool CheckSoundRouting()
	{
		float volume = Main.SoundVolume;
		try
		{
			// Volume 0 is the documented "off" switch and must do nothing at all.
			Main.SoundVolume = 0f;
			try
			{
				Main.PlaySound(0, 100, 100);
				Main.PlaySound(0);
				Main.PlaySound(2);
			}
			catch (Exception ex)
			{
				return Fail("PlaySound threw while muted: " + ex.GetType().Name + ": " + ex.Message);
			}

			// Audio that never loaded: pools are null here, exactly like a failed audio stage.
			// Every call must be swallowed, for both overloads and a spread of sound types.
			Main.SoundVolume = 1f;
			bool poolsMissing = Main.SoundDig == null || Main.SoundDig[0] == null;
			if (!poolsMissing)
			{
				// Content was loaded in this process (not the case headless); nothing to prove.
				return true;
			}
			int[] positioned = new int[6] { 0, 1, 2, 3, 14, 30 };
			for (int i = 0; i < positioned.Length; i++)
			{
				try
				{
					Main.PlaySound(positioned[i], 320, 320);
				}
				catch (Exception ex)
				{
					return Fail("PlaySound(" + positioned[i] + ", x, y) threw with no audio loaded: " + ex.GetType().Name + ": " + ex.Message);
				}
			}
			int[] ui = new int[6] { 1, 7, 10, 11, 12, 22 };
			for (int j = 0; j < ui.Length; j++)
			{
				try
				{
					Main.PlaySound(ui[j]);
				}
				catch (Exception ex)
				{
					return Fail("PlaySound(" + ui[j] + ") threw with no audio loaded: " + ex.GetType().Name + ": " + ex.Message);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("sound routing check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Main.SoundVolume = volume;
		}
	}

	// --- World file inspector -----------------------------------------------

	/// <summary>
	/// The inspector is what turns "the world did not load" into a reason. Its value depends
	/// entirely on agreeing with the real loader, so it is checked against a world file the real
	/// writer produced (<c>Content/Worlds/tutorial.wld</c>) - not only against files this test
	/// builds. A synthetic file only proves the reader and the test's writer agree with each
	/// other; the tutorial world proves they agree with the game.
	/// </summary>
	private static bool CheckWorldFileInspector()
	{
		string text = Path.Combine(ContentRoot, "Worlds", "tutorial.wld");
		try
		{
			// A PC-edition world is recognised and reported instead of failing anonymously.
			byte[] array = new byte[32];
			BitConverter.GetBytes(279).CopyTo(array, 0);
			WorldFileReport pc = WorldFileInspector.Inspect(new MemoryStream(array));
			if (pc.Kind != WorldFileKind.Pc)
			{
				return Fail("a PC-edition world was not recognised (kind " + pc.Kind + ")");
			}
			if (pc.Version != 279)
			{
				return Fail("the PC version was not reported");
			}
			if (pc.Loadable)
			{
				return Fail("a PC-edition world was reported as loadable");
			}
			if (string.IsNullOrEmpty(pc.Summary) || pc.Summary.IndexOf("PC", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("the PC summary does not mention PC: '" + pc.Summary + "'");
			}

			// Garbage and empty files are "unknown", not "truncated": the distinction tells the
			// player whether to look for a backup or for the wrong file.
			WorldFileReport empty = WorldFileInspector.Inspect(new MemoryStream(new byte[0]));
			if (empty.Kind != WorldFileKind.Unknown || empty.Loadable)
			{
				return Fail("an empty file was not reported as unknown");
			}
			WorldFileReport nonsense = WorldFileInspector.Inspect(new MemoryStream(new byte[6] { 1, 2, 3, 4, 5, 6 }));
			if (nonsense.Kind != WorldFileKind.Unknown || nonsense.Complete || nonsense.Loadable)
			{
				return Fail("a 6-byte file was reported as a world (kind " + nonsense.Kind + ", complete " + nonsense.Complete + ")");
			}
			if (string.IsNullOrEmpty(nonsense.Summary))
			{
				return Fail("a malformed file produced no explanation");
			}
			if (!WorldFileInspector.Inspect((Stream)null).Summary.StartsWith("No file", StringComparison.OrdinalIgnoreCase))
			{
				return Fail("a null stream was not reported");
			}
			if (!WorldFileInspector.Inspect(Path.Combine(ContentRoot, "no-such-world.wld")).Summary.StartsWith("Could not open", StringComparison.OrdinalIgnoreCase))
			{
				return Fail("a missing file was not reported");
			}

			if (!File.Exists(text))
			{
				Skip("no tutorial world to validate the inspector against");
			}

			// A real world produced by the real writer.
			WorldFileReport real = WorldFileInspector.Inspect(text);
			if (real.Kind != WorldFileKind.Console)
			{
				return Fail("the tutorial world was not recognised as a console world (kind " + real.Kind + ", version " + real.Version + ")");
			}
			if (!real.Complete)
			{
				return Fail("the tutorial world was reported as incomplete at byte " + real.StoppedAt + " of " + real.Length + ": " + real.Summary);
			}
			if (!real.Loadable)
			{
				return Fail("the tutorial world was reported as not loadable");
			}
			if (real.TilesX <= 0 || real.TilesY <= 0)
			{
				return Fail("the tutorial world has no size");
			}
			if (string.IsNullOrEmpty(real.Name))
			{
				return Fail("the tutorial world has no name");
			}
			if (real.Tiles != real.TilesX * real.TilesY)
			{
				return Fail("the tile count (" + real.Tiles + ") does not match " + real.TilesX + "x" + real.TilesY);
			}
			// It must consume the whole file: a reader with the wrong field sizes might still
			// stop "without error" while sitting at a plausible offset.
			if (real.StoppedAt != real.Length)
			{
				return Fail("the inspector stopped at byte " + real.StoppedAt + " of " + real.Length + ", so it misread the structure");
			}

			// A truncated copy of the same file: the reason must be "truncated", not silence.
			string text2 = Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-truncated.wld");
			byte[] array2 = File.ReadAllBytes(text);
			int num = Math.Min(array2.Length / 2, 200000);
			byte[] array3 = new byte[num];
			Buffer.BlockCopy(array2, 0, array3, 0, num);
			File.WriteAllBytes(text2, array3);
			try
			{
				WorldFileReport truncated = WorldFileInspector.Inspect(text2);
				if (truncated.Complete)
				{
					return Fail("a truncated copy was reported as complete");
				}
				if (truncated.Loadable)
				{
					return Fail("a truncated copy was reported as loadable");
				}
				if (truncated.Summary.IndexOf("truncated", StringComparison.OrdinalIgnoreCase) < 0)
				{
					return Fail("the truncation was not named: '" + truncated.Summary + "'");
				}
				if (truncated.TilesX != real.TilesX)
				{
					return Fail("the header was lost in the truncated copy");
				}
			}
			finally
			{
				try
				{
					File.Delete(text2);
				}
				catch
				{
				}
			}
			return true;
		}
		catch (SkipCheckException)
		{
			// A skip is not a failure. Letting the generic catch below swallow it turned
			// "no tutorial.wld in a bare checkout" into a red CI run.
			throw;
		}
		catch (Exception ex)
		{
			return Fail("world file inspector check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Language -----------------------------------------------------------

	/// <summary>
	/// The five translations were always in <see cref="Lang"/>, but only the OS culture could
	/// choose one. What matters now: every option actually fills the tables, switching changes
	/// the text, Auto really means "whatever the system says", and a bad stored value falls back
	/// instead of leaving the UI blank.
	///
	/// This check runs the real <see cref="Lang.SetLang"/>, which is the same call the game makes
	/// at startup - so if the string tables were ever broken, this fails rather than the game
	/// silently showing empty labels.
	/// </summary>
	private static bool CheckLanguage()
	{
		int savedChoice = Language.Choice;
		int savedOption = Lang.LangOption;
		// The player's own Storage/Lang files must not leak into this check: it is about the
		// built-in tables, and a file holding English text for German would fail it on an
		// otherwise healthy install.
		LangOverrides.SuppressFiles = true;
		try
		{
			// Every option must fill the tables with non-empty text. The array is sized from Max,
			// not from a literal: it used to say 6, and adding the custom option made this very
			// check throw instead of testing it.
			string[] samples = new string[Language.Max + 1];
			for (int i = Language.Auto; i <= Language.Max; i++)
			{
				string error;
				if (!Language.Apply(i, out error))
				{
					return Fail("option " + i + " was rejected: " + error);
				}
				if (!Language.TablesFilled())
				{
					return Fail("option " + i + " left the string tables empty");
				}
				if (Language.Choice != i)
				{
					return Fail("option " + i + " was not remembered as the choice (got " + Language.Choice + ")");
				}
				samples[i] = Lang.MenuText[6];
			}
			// The translations must actually differ: identical strings would mean the switch is
			// doing nothing while claiming to work.
			if (string.Equals(samples[1], samples[2], StringComparison.Ordinal))
			{
				return Fail("English and German share the same menu text: '" + samples[1] + "'");
			}
			if (string.Equals(samples[2], samples[4], StringComparison.Ordinal))
			{
				return Fail("German and French share the same menu text");
			}
			// Auto must resolve to a concrete language, never leave 0 behind, and stay labelled
			// "Auto" so the menu does not claim you picked something you did not.
			string error2;
			if (!Language.Apply(Language.Auto, out error2))
			{
				return Fail("Auto was rejected");
			}
			if (Language.Choice != Language.Auto)
			{
				return Fail("Auto was not remembered as the choice");
			}
			if (Language.Effective == 0)
			{
				return Fail("Auto did not resolve to a concrete language (effective 0)");
			}
			if (Language.CurrentLabel != "Auto")
			{
				return Fail("Auto is labelled '" + Language.CurrentLabel + "'");
			}
			if (!Language.TablesFilled())
			{
				return Fail("Auto left the string tables empty");
			}

			// Labels are the language's own name, so they are readable in any UI language.
			if (Language.Label(1) != "English" || Language.Label(2) != "Deutsch" || Language.Label(3) != "Italiano"
				|| Language.Label(4) != "Français" || Language.Label(5) != "Español" || Language.Label(0) != "Auto")
			{
				return Fail("the language labels are wrong");
			}
			// Cycling wraps over the whole range and never produces an invalid option.
			if (Language.Next(Language.Max, 1) != Language.Auto)
			{
				return Fail("cycling past the last language did not wrap to Auto");
			}
			if (Language.Next(Language.Auto, -1) != Language.Max)
			{
				return Fail("cycling before Auto did not wrap to the last language");
			}
			int option = Language.Auto;
			for (int j = 0; j < 13; j++)
			{
				option = Language.Next(option, 1);
				if (option < Language.Auto || option > Language.Max)
				{
					return Fail("cycling produced the invalid option " + option);
				}
			}

			// A bad stored value falls back to Auto; the parse never throws.
			string[] bad = new string[5] { "", "abc", "-1", "99", null };
			for (int k = 0; k < bad.Length; k++)
			{
				if (Language.Parse(bad[k]) != Language.Auto)
				{
					return Fail("'" + bad[k] + "' parsed as " + Language.Parse(bad[k]) + " instead of Auto");
				}
			}
			if (Language.Parse("3") != 3 || Language.Parse(" 2 ") != 2)
			{
				return Fail("a valid stored language did not parse");
			}
			// Out-of-range options are refused, and the current choice survives the refusal.
			int before = Language.Choice;
			string error3;
			if (Language.Apply(-1, out error3) || Language.Apply(Language.Max + 1, out error3) || string.IsNullOrEmpty(error3))
			{
				return Fail("an out-of-range language was accepted");
			}
			if (Language.Choice != before)
			{
				return Fail("a refused language changed the current choice");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("language check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			LangOverrides.SuppressFiles = false;
			// Restore both values: the rest of the suite and the player's own session depend on
			// the strings being in the language that was selected before the test ran.
			Language.Restore(savedChoice, savedOption);
		}
	}

	// --- Language overrides -------------------------------------------------

	/// <summary>
	/// Translation files are the one part of the language system a player can break by hand, so
	/// the checks are about what must NOT happen: a bad index must not shift every later string,
	/// a blank value must not erase a label, and an unknown section must be reported rather than
	/// ignored. The round trip through <see cref="LangOverrides.WriteTemplate"/> also pins the
	/// escape format - a template that does not re-read is useless to a translator.
	/// </summary>
	private static bool CheckLangOverrides()
	{
		int savedOption = Lang.LangOption;
		int savedChoice = Language.Choice;
		string name6 = Lang.MenuText[6];
		string name4 = Lang.MenuText[4];
		string path = LangOverrides.FileFor(savedOption);
		string backup = null;
		try
		{
			if (File.Exists(path))
			{
				backup = File.ReadAllText(path);
			}
			Directory.CreateDirectory(Terraria.Storage.AppPaths.Lang);
			// A well-formed file with two entries, one of them multi-line.
			File.WriteAllText(path,
				"[MenuText]\r\n6 = Uebergesetzt\r\n\r\n[MiscText]\r\n8 = Erste Zeile\\nZweite Zeile\r\n", Encoding.UTF8);
			LangOverrides.Apply();
			if (LangOverrides.AppliedCount != 2)
			{
				return Fail("expected 2 overrides, applied " + LangOverrides.AppliedCount);
			}
			if (Lang.MenuText[6] != "Uebergesetzt")
			{
				return Fail("the override was not applied: '" + Lang.MenuText[6] + "'");
			}
			if (Lang.MiscText[8] != "Erste Zeile\nZweite Zeile")
			{
				return Fail("the \\n escape was not decoded: '" + Lang.MiscText[8].Replace("\n", "<LF>") + "'");
			}
			if (LangOverrides.Problems.Count != 0)
			{
				return Fail("a valid file reported " + LangOverrides.Problems.Count + " problem(s)");
			}

			// Unlisted entries keep the built-in text: a partial translation is valid.
			if (string.IsNullOrEmpty(Lang.MenuText[5]) || string.IsNullOrEmpty(Lang.MiscText[7]))
			{
				return Fail("an unlisted entry was lost");
			}

			// A bad index must be reported, must not shift other strings, and must not crash.
			File.WriteAllText(path,
				"[MenuText]\r\n6 = Nach Fehler\r\n9999 = zu weit\r\nabc = keine Zahl\r\n\r\n[Nonsense]\r\n1 = x\r\n", Encoding.UTF8);
			Lang.MenuText[6] = name6;
			LangOverrides.Apply();
			if (Lang.MenuText[6] != "Nach Fehler")
			{
				return Fail("a valid entry was dropped because another entry was invalid");
			}
			if (LangOverrides.AppliedCount != 1)
			{
				return Fail("an invalid file applied " + LangOverrides.AppliedCount + " entries instead of 1");
			}
			IList<string> problems = LangOverrides.Problems;
			if (problems.Count < 3)
			{
				return Fail("the invalid file reported only " + problems.Count + " problem(s)");
			}
			bool sawRange = false;
			bool sawSection = false;
			bool sawIndex = false;
			for (int i = 0; i < problems.Count; i++)
			{
				string text = problems[i];
				if (text.IndexOf("9999", StringComparison.Ordinal) >= 0)
				{
					sawRange = true;
				}
				if (text.IndexOf("Nonsense", StringComparison.Ordinal) >= 0)
				{
					sawSection = true;
				}
				if (text.IndexOf("abc", StringComparison.Ordinal) >= 0)
				{
					sawIndex = true;
				}
			}
			if (!sawRange || !sawSection || !sawIndex)
			{
				return Fail("the problems do not name the out-of-range index, the unknown section and the non-numeric key");
			}

			// A blank value must not erase a label.
			Lang.MenuText[6] = name6;
			File.WriteAllText(path, "[MenuText]\r\n6 =\r\n", Encoding.UTF8);
			LangOverrides.Apply();
			if (Lang.MenuText[6] != name6)
			{
				return Fail("an empty value erased the built-in text");
			}
			if (LangOverrides.AppliedCount != 0)
			{
				return Fail("an empty value was counted as applied");
			}

			// Leading spaces are load-bearing: death messages are the player's name + " wurde
			// abgeleckt". The loader drops exactly one space (the separator) and keeps the rest,
			// so "X =  leading" gives " leading" while "X = leading" gives "leading".
			Lang.DeathText[0] = "builtin";
			File.WriteAllText(path, "[DeathText]\r\n0 =  leading space\n", Encoding.UTF8);
			LangOverrides.Apply();
			if (Lang.DeathText[0] != " leading space")
			{
				return Fail("a leading space was not preserved: '" + Lang.DeathText[0] + "'");
			}
			Lang.DeathText[0] = "builtin";
			File.WriteAllText(path, "[DeathText]\r\n0 = no leading space\n", Encoding.UTF8);
			LangOverrides.Apply();
			if (Lang.DeathText[0] != "no leading space")
			{
				return Fail("the separator space was not stripped: '" + Lang.DeathText[0] + "'");
			}

			// The command line accepts a code or a number, and refuses anything else instead of
			// silently exporting the wrong language.
			int parsed;
			if (!LangOverrides.TryParse("de", out parsed) || parsed != 2 || !LangOverrides.TryParse("FR", out parsed) || parsed != 4
				|| !LangOverrides.TryParse("auto", out parsed) || parsed != 0 || !LangOverrides.TryParse("5", out parsed) || parsed != 5)
			{
				return Fail("a language code did not parse");
			}
			if (LangOverrides.TryParse("klingon", out parsed))
			{
				return Fail("an unknown language code was accepted");
			}

			// Escapes round-trip, including the multi-line hint that contains '=' and newlines.
			if (LangOverrides.Unescape(LangOverrides.Escape(name4)) != name4)
			{
				return Fail("the escape round trip lost the save-icon hint");
			}
			if (LangOverrides.Unescape("a\\\\b") != "a\\b")
			{
				return Fail("a literal backslash did not round-trip");
			}
			if (LangOverrides.Unescape("C:\\Games\\Terraria") != "C:\\Games\\Terraria")
			{
				return Fail("a Windows path was mangled by the escape reader");
			}

			// No file at all is a no-op, not a failure.
			File.Delete(path);
			Lang.MenuText[6] = name6;
			LangOverrides.Apply();
			if (LangOverrides.AppliedCount != 0 || LangOverrides.Problems.Count != 0)
			{
				return Fail("a missing translation file produced work or problems");
			}

			// The template must be a file the loader can read back: that is the whole point of
			// shipping one to translators.
			string error;
			// A built-in string that begins with a space: death messages are the player's name
			// followed by the rest of the sentence, so the template must not eat that space.
			string death = " konnte das Antidot nicht finden.";
			Lang.DeathText[0] = death;
			string tip = Lang.TipText[0];
			string text2 = LangOverrides.WriteTemplate(savedOption, out error);
			if (text2 == null)
			{
				return Fail("the template could not be written: " + error);
			}
			if (!File.Exists(text2))
			{
				return Fail("the template was not written");
			}
			Lang.MenuText[6] = "before template";
			LangOverrides.Apply();
			if (Lang.MenuText[6] == "before template")
			{
				return Fail("the template did not restore the string it contains");
			}
			// Every table survives the round trip unchanged, not just the one entry checked
			// above - this is what catches an escape that eats text or a key that shifts.
			if (Lang.DeathText[0] != death || Lang.TipText[0] != tip)
			{
				return Fail("the template round trip changed a string: death '" + Lang.DeathText[0] + "' tip '" + Lang.TipText[0] + "'");
			}
			if (LangOverrides.Problems.Count != 0)
			{
				return Fail("the template reported " + LangOverrides.Problems.Count + " problem(s), first: " + LangOverrides.Problems[0]);
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("language override check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			try
			{
				if (backup != null)
				{
					File.WriteAllText(path, backup, Encoding.UTF8);
				}
				else if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch
			{
			}
			// Restore both the strings and the language state the rest of the suite expects.
			// Last, because Restore re-applies the player's real override file (if any).
			Language.Restore(savedChoice, savedOption);
		}
	}

	// --- Feature text -------------------------------------------------------

	/// <summary>
	/// The text this port added itself: chat replies from cheats and host commands.
	///
	/// Two kinds of failure matter here. A misplaced placeholder turns a sentence into
	/// "Kicked ." and looks like a logic bug; a translation file that silently loses its
	/// contents makes the translator's work vanish the next time they export. Both are checked
	/// against the real loader, not against a mock.
	/// </summary>
	private static bool CheckFeatureTexts()
	{
		int savedOption = Lang.LangOption;
		int savedChoice = Language.Choice;
		string path = LangOverrides.FileFor(savedOption);
		string backup = File.Exists(path) ? File.ReadAllText(path) : null;
		try
		{
			// The built-in table must be complete, and it is the reference every translation
			// falls back to.
			string problem;
			if (!Texts.Verify(out problem))
			{
				return Fail("the built-in text table is broken: " + problem);
			}
			if (Texts.Keys.Count < 50)
			{
				return Fail("only " + Texts.Keys.Count + " text keys are registered - the table is not being filled");
			}
			if (Texts.Format("chat.cheats.gave", 3, 5) != "gave 3 x5")
			{
				return Fail("placeholders were not filled: '" + Texts.Format("chat.cheats.gave", 3, 5) + "'");
			}
			// Too few arguments must leave the placeholder visible. "Kicked ." reads like a bug
			// in the command; "Kicked {0}." points straight at the broken translation.
			if (Texts.Format("chat.cheats.gave", 3) != "gave 3 x{1}")
			{
				return Fail("a missing argument was not left visible: '" + Texts.Format("chat.cheats.gave", 3) + "'");
			}
			// Braces that are not placeholders must survive: string.Format would throw here, and
			// the text comes from a file a player edits.
			if (Texts.Fill("a { b } c", new object[1] { 1 }) != "a { b } c")
			{
				return Fail("a stray brace was mangled");
			}
			if (Texts.Fill("{x}", new object[1] { 1 }) != "{x}")
			{
				return Fail("a non-numeric placeholder was replaced");
			}
			if (Texts.Get("chat.does.not.exist") != "<chat.does.not.exist>" || Texts.Has("chat.does.not.exist"))
			{
				return Fail("an unknown key did not stand out");
			}

			// A file translating two of them, plus a typo that must be reported by name.
			Directory.CreateDirectory(Terraria.Storage.AppPaths.Lang);
			File.WriteAllText(path,
				"[Texts]\r\nchat.cheats.god = Gott-Modus\r\nchat.mod.muted = Du bist stummgeschaltet.\\r\\nKein Grund zur Panik.\r\nchat.cheats.gdo = Tippfehler\r\n",
				Encoding.UTF8);
			LangOverrides.Apply();
			if (Texts.Get("chat.cheats.god") != "Gott-Modus")
			{
				return Fail("a translated key was not applied: '" + Texts.Get("chat.cheats.god") + "'");
			}
			if (HostModeration.MuteNotice != "Du bist stummgeschaltet.\r\nKein Grund zur Panik.")
			{
				return Fail("a mutable notice did not follow the translation: '" + HostModeration.MuteNotice.Replace("\r\n", "<CRLF>") + "'");
			}
			if (Texts.OverrideCount != 2 || LangOverrides.AppliedCount != 2)
			{
				return Fail("expected 2 translated keys, got " + Texts.OverrideCount + " in the table and " + LangOverrides.AppliedCount + " counted");
			}
			// Everything else stays English rather than disappearing.
			if (Texts.Get("chat.cheats.healed") != "healed")
			{
				return Fail("an untranslated key lost its English text");
			}
			bool sawTypo = false;
			IList<string> problems = LangOverrides.Problems;
			for (int i = 0; i < problems.Count; i++)
			{
				if (problems[i].IndexOf("chat.cheats.gdo", StringComparison.Ordinal) >= 0)
				{
					sawTypo = true;
				}
			}
			if (!sawTypo)
			{
				return Fail("a mistyped key was not reported by name");
			}

			// An empty value must not blank a message.
			File.WriteAllText(path, "[Texts]\r\nchat.cheats.god =\r\n", Encoding.UTF8);
			LangOverrides.Apply();
			if (Texts.Get("chat.cheats.god") != "god mode")
			{
				return Fail("an empty value blanked a message: '" + Texts.Get("chat.cheats.god") + "'");
			}

			// A translation that drops a placeholder is reported by key and placeholder number:
			// "Kicked ." otherwise looks like a bug in the command, not in the file.
			File.WriteAllText(path, "[Texts]\r\nchat.host.kick.done = {0} wurde entfernt.\r\nchat.host.ban.already = Schon gebannt.\r\n", Encoding.UTF8);
			LangOverrides.Apply();
			bool sawPlaceholder = false;
			problems = LangOverrides.Problems;
			for (int j = 0; j < problems.Count; j++)
			{
				if (problems[j].IndexOf("chat.host.ban.already", StringComparison.Ordinal) >= 0
					&& problems[j].IndexOf("{0}", StringComparison.Ordinal) >= 0)
				{
					sawPlaceholder = true;
				}
			}
			if (!sawPlaceholder)
			{
				return Fail("a translation that dropped a placeholder was not reported");
			}
			// ...but the line is still applied, and the readable one produced no complaint.
			if (Texts.Get("chat.host.ban.already") != "Schon gebannt.")
			{
				return Fail("a translation with a missing placeholder was not applied");
			}
			if (problems.Count != 1)
			{
				return Fail("expected exactly 1 placeholder problem, got " + problems.Count);
			}

			// Screen labels are keys as well, and they must actually reach the screen: these are
			// the strings that used to be English literals in the menus.
			if (Texts.Format("ui.display.window", 1280, 720) != "Window size: 1280x720")
			{
				return Fail("the display row label did not format: '" + Texts.Format("ui.display.window", 1280, 720) + "'");
			}
			if (Texts.Format("ui.settings.display", 1280, 720) != "Display: 1280x720")
			{
				return Fail("the settings row did not format: '" + Texts.Format("ui.settings.display", 1280, 720) + "'");
			}
			if (Texts.Get("ui.backups.title") == null || Texts.Default("ui.backups.title").IndexOf("{0}", StringComparison.Ordinal) < 0)
			{
				return Fail("the backups title lost its placeholder");
			}

			// Every rebindable action needs a label, and the key is derived from the enum name.
			// Adding an action without adding its text would otherwise show "<ui.key.foo>" on
			// the rebind screen, in every language including English.
			Terraria.Input.KeyBindings.Action[] actions = Terraria.Input.KeyBindings.All;
			for (int k = 0; k < actions.Length; k++)
			{
				string key = "ui.key." + actions[k].ToString().ToLowerInvariant();
				if (!Texts.Has(key))
				{
					return Fail("the rebind action " + actions[k] + " has no label (missing " + key + ")");
				}
			}
			if (actions.Length < 15)
			{
				return Fail("only " + actions.Length + " rebindable actions are registered");
			}

			// No file at all: back to English, no leftovers from the previous language.
			File.Delete(path);
			LangOverrides.Apply();
			if (Texts.OverrideCount != 0)
			{
				return Fail("translations survived a language change");
			}

			// Export must be a refresh, not a reset: a file with a translation goes in, and the
			// translation is still there afterwards.
			File.WriteAllText(path, "[Texts]\r\nchat.cheats.god = Mein Gott-Modus\r\n", Encoding.UTF8);
			string error;
			if (LangOverrides.WriteTemplate(savedOption, out error) == null)
			{
				return Fail("the template could not be refreshed: " + error);
			}
			string refreshed = File.ReadAllText(path);
			if (refreshed.IndexOf("chat.cheats.god = Mein Gott-Modus", StringComparison.Ordinal) < 0)
			{
				return Fail("refreshing the template overwrote an existing translation");
			}
			// ...and it must also contain keys added since the file was written, so a translator
			// who exported early still learns about new text.
			if (refreshed.IndexOf("[Texts]", StringComparison.Ordinal) < 0
				|| refreshed.IndexOf("chat.host.help", StringComparison.Ordinal) < 0)
			{
				return Fail("the refreshed template is missing keys");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("feature text check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			try
			{
				if (backup != null)
				{
					File.WriteAllText(path, backup, Encoding.UTF8);
				}
				else if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch
			{
			}
			Language.Restore(savedChoice, savedOption);
		}
	}

	// --- Reject reasons -----------------------------------------------------

	/// <summary>
	/// Why a server refused a connection used to travel as a sentence written by the host, and
	/// the client printed it verbatim - so the one part of the language work a client could not
	/// fix was the refusal it received. Now a code travels and the text is chosen locally.
	///
	/// Two things have to hold: the codec must round-trip every code (a dropped field shows up as
	/// "server protocol , yours"), and the real host must actually send a code - checked against a
	/// raw socket, not against the encoder that the check itself calls.
	/// </summary>
	private static bool CheckRejectReasons()
	{
		int savedOption = Lang.LangOption;
		int savedChoice = Language.Choice;
		int savedPort = NetworkConfig.Port;
		int savedDiscovery = NetworkConfig.DiscoveryPort;
		string savedPassword = NetworkConfig.Password;
		bool savedTcp = NetworkConfig.UseTcpTransport;
		NetworkSession hostSession = null;
		LangOverrides.SuppressFiles = true;
		try
		{
			// Every code, with the fields it can carry.
			List<Terraria.Net.RejectReason> all = Terraria.Net.RejectReasons.All;
			if (all.Count != Terraria.Net.RejectReasons.MaxKnown)
			{
				return Fail("the code list and MaxKnown disagree: " + all.Count + " vs " + Terraria.Net.RejectReasons.MaxKnown);
			}
			for (int i = 0; i < all.Count; i++)
			{
				Terraria.Net.RejectReason reason = all[i];
				string[] fields;
				if (reason == Terraria.Net.RejectReason.ProtocolMismatch)
				{
					fields = new string[2] { "3", "2" };
				}
				else if (reason == Terraria.Net.RejectReason.Kicked)
				{
					fields = new string[1] { "spamming" };
				}
				else
				{
					fields = new string[0];
				}
				byte[] payload = Terraria.Net.RejectReasons.Build(reason, fields);
				Terraria.Net.RejectReason back;
				string[] got;
				if (!Terraria.Net.RejectReasons.TryParse(payload, out back, out got))
				{
					return Fail("payload for " + reason + " did not parse");
				}
				if (back != reason)
				{
					return Fail(reason + " came back as " + back);
				}
				if (got.Length != fields.Length)
				{
					return Fail(reason + " carried " + got.Length + " field(s) instead of " + fields.Length);
				}
				for (int j = 0; j < fields.Length; j++)
				{
					if (got[j] != fields[j])
					{
						return Fail(reason + " field " + j + " came back as '" + got[j] + "'");
					}
				}
				// Every code must have text, and it must not be the "unknown" fallback: that
				// would show the raw bytes instead of an explanation.
				string key = Terraria.Net.RejectReasons.KeyFor(reason, got);
				if (!Texts.Has(key))
				{
					return Fail("the code " + reason + " has no text (" + key + ")");
				}
				string spoken = Terraria.Net.RejectReasons.Describe(reason, got);
				if (string.IsNullOrEmpty(spoken) || spoken.IndexOf('{') >= 0 || spoken.IndexOf('<') >= 0)
				{
					return Fail("the code " + reason + " describes as '" + spoken + "'");
				}
			}

			// A kick without a reason still has to say something.
			Terraria.Net.RejectReason noReason = Terraria.Net.RejectReason.Kicked;
			string empty = Terraria.Net.RejectReasons.Describe(noReason, new string[0]);
			if (string.IsNullOrEmpty(empty) || empty.IndexOf('{') >= 0)
			{
				return Fail("a kick without a reason describes as '" + empty + "'");
			}
			string withReason = Terraria.Net.RejectReasons.Describe(noReason, new string[1] { "spamming" });
			if (withReason.IndexOf("spamming", StringComparison.Ordinal) < 0)
			{
				return Fail("a kick reason was dropped from the text: '" + withReason + "'");
			}

			// An old host sends a sentence. It must still be shown - as Unknown, verbatim - and
			// never be mistaken for a code.
			byte[] legacy = Encoding.UTF8.GetBytes("Wrong password.");
			Terraria.Net.RejectReason legacyReason;
			string[] legacyFields;
			if (!Terraria.Net.RejectReasons.TryParse(legacy, out legacyReason, out legacyFields))
			{
				return Fail("a legacy text payload did not parse");
			}
			if (legacyReason != Terraria.Net.RejectReason.Unknown)
			{
				return Fail("a legacy sentence was read as the code " + legacyReason);
			}
			if (Terraria.Net.RejectReasons.Describe(legacyReason, legacyFields) != "Wrong password.")
			{
				return Fail("a legacy sentence was not shown as it arrived");
			}

			// Truncated and empty payloads must not throw on a network thread.
			Terraria.Net.RejectReason brokenReason;
			string[] brokenFields;
			if (!Terraria.Net.RejectReasons.TryParse(new byte[3] { 4, 0, 0 }, out brokenReason, out brokenFields))
			{
				return Fail("a truncated payload was rejected instead of degraded");
			}
			if (Terraria.Net.RejectReasons.Describe(brokenReason, brokenFields) == null)
			{
				return Fail("a truncated payload produced no text");
			}
			if (Terraria.Net.RejectReasons.TryParse(new byte[0], out brokenReason, out brokenFields))
			{
				return Fail("an empty payload was accepted");
			}
			// ...and an empty payload still produces a readable sentence, not a bare "{0}".
			string noPayload = Terraria.Net.RejectReasons.Describe(Terraria.Net.RejectReason.Unknown, new string[0]);
			if (string.IsNullOrEmpty(noPayload) || noPayload.IndexOf('{') >= 0)
			{
				return Fail("an empty refusal describes as '" + noPayload + "'");
			}

			// The wording is looked up when it is shown, so installing a translation changes the
			// refusal without restarting anything. That is the mechanism the language files use.
			string english = Terraria.Net.RejectReasons.Describe(Terraria.Net.RejectReason.WrongPassword, new string[0]);
			if (!Texts.SetOverride("ui.reject.password", "falsches Passwort"))
			{
				return Fail("the password refusal is not a known text key");
			}
			string translated = Terraria.Net.RejectReasons.Describe(Terraria.Net.RejectReason.WrongPassword, new string[0]);
			if (translated != "falsches Passwort")
			{
				return Fail("a translated refusal was not used: '" + translated + "'");
			}
			// The log line stays English: logs are read by whoever runs the host, and a log that
			// changes language with a client's settings would be unreadable.
			if (Terraria.Net.RejectReasons.DescribeEnglish(Terraria.Net.RejectReason.WrongPassword, new string[0]) != english)
			{
				return Fail("the English form of a refusal changed with the translation");
			}
			Texts.ClearOverrides();
			if (Terraria.Net.RejectReasons.Describe(Terraria.Net.RejectReason.WrongPassword, new string[0]) != english)
			{
				return Fail("the refusal did not go back to English");
			}

			// The real host: a raw client sends a wrong password and must receive a code.
			Microsoft.Xna.Framework.GamerServices.GamerServicesDispatcher.Initialize(null);
			NetworkConfig.UseTcpTransport = true;
			NetworkConfig.Password = "hunter2";
			NetworkConfig.Port = 0;
			NetworkConfig.DiscoveryPort = 0;
			NetworkSessionProperties properties = new NetworkSessionProperties();
			hostSession = new NetworkSession(properties, NetworkSessionType.PlayerMatch, 8, 0, 1,
				new List<Microsoft.Xna.Framework.GamerServices.SignedInGamer>(1) { Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers[0] });
			hostSession.StartHostPeer();
			int port = NetworkConfig.ActivePort;
			if (port <= 0)
			{
				return Fail("the host did not bind a port");
			}
			using (System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient())
			{
				tcpClient.NoDelay = true;
				tcpClient.Connect(System.Net.IPAddress.Loopback, port);
				using System.Net.Sockets.NetworkStream stream = tcpClient.GetStream();
				using MemoryStream memoryStream = new MemoryStream();
				BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
				binaryWriter.Write(Terraria.Net.TcpPeer.OpHello);
				binaryWriter.Write(Terraria.Net.TcpPeer.ProtocolVersionPublic);
				binaryWriter.Write("WrongPasswordPeer");
				binaryWriter.Write(Terraria.Mods.ModLoader.Signature);
				binaryWriter.Write("definitely-not-hunter2");
				binaryWriter.Flush();
				WireFormat.WriteAll(stream, WireFormat.LengthPrefix(memoryStream.ToArray()));
				byte[] frame = WireFormat.ReadFrame(stream);
				if (frame == null)
				{
					return Fail("the host sent no refusal");
				}
				if (frame[0] != Terraria.Net.TcpPeer.OpReject)
				{
					return Fail("the host answered with opcode " + frame[0] + " instead of a refusal");
				}
				byte[] payload = new byte[frame.Length - 1];
				Buffer.BlockCopy(frame, 1, payload, 0, payload.Length);
				Terraria.Net.RejectReason reason2;
				string[] fields2;
				Terraria.Net.RejectReasons.TryParse(payload, out reason2, out fields2);
				if (reason2 != Terraria.Net.RejectReason.WrongPassword)
				{
					return Fail("a wrong password produced '" + reason2 + "' instead of a password refusal");
				}
			}
			// The whitelist gate, on the same live host: the password is correct this time, the
			// name is not on the list, and the refusal has to be its own reason. Checking only the
			// pure function would leave "is it wired into the handshake" unanswered - and that is
			// the part that decides whether a stranger gets in.
			NetworkConfig.Password = string.Empty;
			Terraria.Net.NetworkConfig.Whitelist = true;
			Terraria.Net.Whitelist.Enabled = true;
			string savedList = null;
			string listPath = Terraria.Net.Whitelist.FilePath;
			bool listExisted = File.Exists(listPath);
			if (listExisted)
			{
				savedList = File.ReadAllText(listPath);
			}
			try
			{
				File.WriteAllText(listPath, "SomeoneElse\r\n", Encoding.UTF8);
				Terraria.Net.Whitelist.Reload();
				Terraria.Net.RejectReason reason3;
				if (!TryHandshakeReject(port, "NotOnTheList", string.Empty, out reason3))
				{
					return Fail("the host did not answer a handshake while the whitelist was on");
				}
				if (reason3 != Terraria.Net.RejectReason.NotWhitelisted)
				{
					return Fail("an unlisted player was refused with '" + reason3 + "' instead of a whitelist refusal");
				}
			}
			finally
			{
				Terraria.Net.NetworkConfig.Whitelist = false;
				Terraria.Net.Whitelist.Enabled = false;
				if (savedList != null)
				{
					File.WriteAllText(listPath, savedList, Encoding.UTF8);
				}
				else if (File.Exists(listPath))
				{
					File.Delete(listPath);
				}
				Terraria.Net.Whitelist.Reload();
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("reject reason check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			LangOverrides.SuppressFiles = false;
			NetworkConfig.Password = savedPassword;
			NetworkConfig.Port = savedPort;
			NetworkConfig.DiscoveryPort = savedDiscovery;
			NetworkConfig.UseTcpTransport = savedTcp;
			Language.Restore(savedChoice, savedOption);
			try
			{
				hostSession?.Dispose();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Sends one handshake to a live host and returns the refusal reason. False when the host did
	/// not answer at all, which is a different failure from "answered with the wrong reason".
	/// </summary>
	private static bool TryHandshakeReject(int Port, string Gamertag, string Password, out Terraria.Net.RejectReason Reason)
	{
		string[] ignored;
		return TryHandshakeReject(Port, Gamertag, Password, null, null, out Reason, out ignored);
	}

	/// <summary>
	/// Opens a download connection and reports the opcode of the host's first answer.
	///
	/// The point is what the answer is <b>not</b>: a download connection must never be welcomed,
	/// because "ask for files" would then be a way to join without a matching mod set.
	/// </summary>
	private static bool TryDownloadHandshake(int Port, string RequestMod, out byte FirstOp)
	{
		FirstOp = 0;
		using System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
		tcpClient.NoDelay = true;
		tcpClient.Connect(System.Net.IPAddress.Loopback, Port);
		using System.Net.Sockets.NetworkStream stream = tcpClient.GetStream();
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(Terraria.Net.TcpPeer.OpHello);
		binaryWriter.Write(Terraria.Net.TcpPeer.ProtocolVersionPublic);
		binaryWriter.Write("Downloader");
		binaryWriter.Write("vanilla");
		binaryWriter.Write(string.Empty);
		binaryWriter.Write(string.Empty);
		binaryWriter.Write(true);
		binaryWriter.Flush();
		WireFormat.WriteAll(stream, WireFormat.LengthPrefix(memoryStream.ToArray()));
		// Ask for one mod, so a serving host has something to answer and a refusing one has
		// already answered by now.
		WireFormat.WriteAll(stream, WireFormat.LengthPrefix(Terraria.Mods.ModTransfer.Request(RequestMod)));
		byte[] frame = WireFormat.ReadFrame(stream);
		if (frame == null || frame.Length == 0)
		{
			return false;
		}
		FirstOp = frame[0];
		return true;
	}

	/// <summary>
	/// The same, with the mod signature and mod list the peer claims. Both are optional so the
	/// existing callers keep their meaning: null means "whatever this build has".
	/// </summary>
	private static bool TryHandshakeReject(int Port, string Gamertag, string Password, string Signature,
		string ModList, out Terraria.Net.RejectReason Reason, out string[] Fields)
	{
		Reason = Terraria.Net.RejectReason.Unknown;
		Fields = new string[0];
		using System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
		tcpClient.NoDelay = true;
		tcpClient.Connect(System.Net.IPAddress.Loopback, Port);
		using System.Net.Sockets.NetworkStream stream = tcpClient.GetStream();
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(Terraria.Net.TcpPeer.OpHello);
		binaryWriter.Write(Terraria.Net.TcpPeer.ProtocolVersionPublic);
		binaryWriter.Write(Gamertag);
		binaryWriter.Write(Signature ?? Terraria.Mods.ModLoader.Signature);
		binaryWriter.Write(Password ?? string.Empty);
		binaryWriter.Write(ModList ?? string.Empty);
		binaryWriter.Flush();
		WireFormat.WriteAll(stream, WireFormat.LengthPrefix(memoryStream.ToArray()));
		byte[] frame = WireFormat.ReadFrame(stream);
		if (frame == null || frame[0] != Terraria.Net.TcpPeer.OpReject)
		{
			return false;
		}
		byte[] payload = new byte[frame.Length - 1];
		Buffer.BlockCopy(frame, 1, payload, 0, payload.Length);
		return Terraria.Net.RejectReasons.TryParse(payload, out Reason, out Fields);
	}

	// --- Portability --------------------------------------------------------

	/// <summary>
	/// Paths that are compared must not depend on the separator.
	///
	/// This is not hypothetical: the content audit converted the manifest to <c>\</c> and
	/// compared it against a directory listing, which reports the platform separator. It passed
	/// on Windows and would have reported every single file as unexpected on Linux - the check
	/// that exists to catch dead weight would have cried wolf about the whole game.
	///
	/// So the audit is run against a scratch tree here, with files created through the real
	/// file system, and both spellings of a name are tried.
	/// </summary>
	private static bool CheckPortablePaths()
	{
		string root = Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-content");
		Terraria.CollectorsEdition.ResetForTests();
		try
		{
			// Conversion, including the cases that are easy to get wrong.
			if (Terraria.Portability.Portable("Images\\_sheetTiles.xnb") != "Images/_sheetTiles.xnb")
			{
				return Fail("a Windows path was not converted to the portable form");
			}
			if (Terraria.Portability.Portable("/Images/logo.xnb") != "Images/logo.xnb")
			{
				return Fail("a leading separator survived the portable form");
			}
			if (Terraria.Portability.Portable("C:\\Games\\Terraria") != "C:/Games/Terraria")
			{
				return Fail("a drive-qualified path was mangled: '" + Terraria.Portability.Portable("C:\\Games\\Terraria") + "'");
			}
			if (Terraria.Portability.Native("Images/logo.xnb") != ("Images" + Terraria.Portability.Separator + "logo.xnb"))
			{
				return Fail("the native form did not use the platform separator");
			}
			// A relative name must survive both directions unchanged.
			if (Terraria.Portability.Portable(Terraria.Portability.Native("a/b/c")) != "a/b/c")
			{
				return Fail("a portable path did not round-trip through the native form");
			}
			// RelativeTo must strip the root, whatever spelling either side used.
			if (Terraria.Portability.RelativeTo("C:\\Game\\Content", "C:\\Game\\Content\\Images\\a.xnb") != "Images/a.xnb")
			{
				return Fail("RelativeTo('C:/Game/Content', .../Images/a.xnb) = '"
					+ Terraria.Portability.RelativeTo("C:\\Game\\Content", "C:\\Game\\Content\\Images\\a.xnb") + "'");
			}
			if (Terraria.Portability.RelativeTo("C:/Game/Content/", "C:/Game/Content/Images/a.xnb") != "Images/a.xnb")
			{
				return Fail("RelativeTo did not tolerate a trailing separator");
			}
			// "Content" must not be stripped out of "Contents".
			if (Terraria.Portability.RelativeTo("C:/Game/Content", "C:/Game/Contents/a.xnb") != "C:/Game/Contents/a.xnb")
			{
				return Fail("RelativeTo stripped a root that was only a prefix of the name");
			}

			// A real directory tree, written through the file system.
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
			Directory.CreateDirectory(Path.Combine(root, "Images"));
			Directory.CreateDirectory(Path.Combine(root, "Worlds"));
			Directory.CreateDirectory(Path.Combine(root, "ExtMusic"));
			Directory.CreateDirectory(Path.Combine(root, "Fonts", "HD"));
			File.WriteAllBytes(Path.Combine(root, "Images", "_sheetTiles.xnb"), new byte[1]);
			File.WriteAllBytes(Path.Combine(root, "Fonts", "HD", "Item_0.xnb"), new byte[1]);
			File.WriteAllBytes(Path.Combine(root, "Worlds", "tutorial.wld"), new byte[1]);
			File.WriteAllBytes(Path.Combine(root, "ExtMusic", "Music_1.wav"), new byte[1]);
			string[] manifest = new string[2] { "Images/_sheetTiles", "Fonts/HD/Item_0" };
			List<string> unexpected = Terraria.Headless.ContentAudit.FindUnexpected(root, manifest);
			if (unexpected.Count != 0)
			{
				// This is the exact failure the old code had on Linux: every file looks stale.
				return Fail("a healthy content tree reported " + unexpected.Count + " unexpected file(s): "
					+ string.Join(", ", unexpected.ToArray()));
			}
			// A stray raw file must still be reported, or the audit would be silently useless.
			File.WriteAllBytes(Path.Combine(root, "Images", "leftover.dds"), new byte[1]);
			unexpected = Terraria.Headless.ContentAudit.FindUnexpected(root, manifest);
			if (unexpected.Count != 1 || !unexpected[0].EndsWith("leftover.dds", StringComparison.OrdinalIgnoreCase))
			{
				return Fail("a stray raw file was not reported (got " + unexpected.Count + ")");
			}
			if (unexpected[0].IndexOf('\\') >= 0 || unexpected[0].IndexOf('/') < 0)
			{
				return Fail("the audit reported a non-portable name: '" + unexpected[0] + "'");
			}

			// A manifest written with backslashes must match too: the two sides are normalised
			// independently, so a manifest that predates this change keeps working.
			string[] backslashManifest = new string[2] { "Images\\_sheetTiles", "Fonts\\HD\\Item_0" };
			if (Terraria.Headless.ContentAudit.FindUnexpected(root, backslashManifest).Count != 1)
			{
				return Fail("a manifest with backslashes did not match the files on disk");
			}

			// The Collectors Edition flag: the settings value decides, and "Auto" falls through.
			Settings settings = new Settings(Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-ce.ini"));
			try
			{
				bool value;
				settings.Set("Game", "CollectorsEdition", "True");
				if (!Terraria.CollectorsEdition.TryReadSetting(settings, out value) || !value)
				{
					return Fail("CollectorsEdition=True was not read as on");
				}
				settings.Update("Game", "CollectorsEdition", "False");
				if (!Terraria.CollectorsEdition.TryReadSetting(settings, out value) || value)
				{
					return Fail("CollectorsEdition=False was not read as off");
				}
				settings.Update("Game", "CollectorsEdition", "Auto");
				if (Terraria.CollectorsEdition.TryReadSetting(settings, out value))
				{
					return Fail("CollectorsEdition=Auto was treated as a decision");
				}
				// Garbage must not be read as "off": a typo should not take the pet away.
				settings.Update("Game", "CollectorsEdition", "maybe");
				if (Terraria.CollectorsEdition.TryReadSetting(settings, out value))
				{
					return Fail("an unreadable CollectorsEdition value was treated as a decision");
				}
				Terraria.CollectorsEdition.Load(settings);
				bool offWindows = !Terraria.Portability.IsWindows;
				if (offWindows && Terraria.CollectorsEdition.IsActive())
				{
					return Fail("the flag is on off Windows, where there is no registry to read");
				}
			}
			finally
			{
				settings.Discard();
				try
				{
					File.Delete(Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-ce.ini"));
				}
				catch
				{
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("portable path check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.CollectorsEdition.ResetForTests();
			try
			{
				if (Directory.Exists(root))
				{
					Directory.Delete(root, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	// --- PNG ----------------------------------------------------------------

	/// <summary>
	/// The PNG reader, checked against bytes derived from the specification rather than against
	/// its own encoder.
	///
	/// The filtered rows below are written out by hand: for a 3x2 grey image, filtering the
	/// second row with Up means "raw minus the row above", which is <c>20</c> for <c>30-10</c>.
	/// A round trip through a helper that both filtered and unfiltered could pass with the sign
	/// flipped; these literals cannot.
	/// </summary>
	private static bool CheckPngDecoder()
	{
		try
		{
			Terraria.Textures.ImageData image;
			string error;

			// --- the five filters, on a 2x2 8-bit grey image -----------------
			// Filter 0 (None): bytes are the pixels.
			byte[] none = new byte[6] { 0, 10, 20, 0, 30, 40 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(2, 2, 8, 0, none, null, null), out image, out error))
			{
				return Fail("filter 0 failed: " + error);
			}
			if (image.Width != 2 || image.Height != 2 || image.R(0, 0) != 10 || image.R(1, 0) != 20
				|| image.R(0, 1) != 30 || image.R(1, 1) != 40 || image.A(0, 0) != 255)
			{
				return Fail("filter 0 decoded the wrong pixels");
			}
			// Filter 1 (Sub): each byte minus the byte one pixel to the left (bpp = 1 here).
			// Row 1: 30-0 = 30, 40-30 = 10.
			byte[] sub = new byte[6] { 1, 10, 10, 1, 30, 10 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(2, 2, 8, 0, sub, null, null), out image, out error))
			{
				return Fail("filter 1 failed: " + error);
			}
			if (image.R(0, 1) != 30 || image.R(1, 1) != 40)
			{
				return Fail("filter 1 decoded " + image.R(0, 1) + "," + image.R(1, 1) + " instead of 30,40");
			}
			// Filter 2 (Up): raw minus the pixel above. Row 1: 30-10 = 20, 40-20 = 20.
			byte[] up = new byte[6] { 0, 10, 20, 2, 20, 20 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(2, 2, 8, 0, up, null, null), out image, out error))
			{
				return Fail("filter 2 failed: " + error);
			}
			if (image.R(0, 1) != 30 || image.R(1, 1) != 40)
			{
				return Fail("filter 2 decoded " + image.R(0, 1) + "," + image.R(1, 1) + " instead of 30,40");
			}
			// Filter 3 (Average): raw minus floor((left + above) / 2).
			// Row 1: 30 - floor((0+10)/2) = 25, 40 - floor((30+20)/2) = 15.
			byte[] average = new byte[6] { 0, 10, 20, 3, 25, 15 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(2, 2, 8, 0, average, null, null), out image, out error))
			{
				return Fail("filter 3 failed: " + error);
			}
			if (image.R(0, 1) != 30 || image.R(1, 1) != 40)
			{
				return Fail("filter 3 decoded " + image.R(0, 1) + "," + image.R(1, 1) + " instead of 30,40");
			}
			// Filter 4 (Paeth): the predictor picks the closest of left/above/upper-left.
			// Row 1: 30 - 10 = 20 (above wins), 40 - 30 = 10 (left wins).
			byte[] paeth = new byte[6] { 0, 10, 20, 4, 20, 10 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(2, 2, 8, 0, paeth, null, null), out image, out error))
			{
				return Fail("filter 4 failed: " + error);
			}
			if (image.R(0, 1) != 30 || image.R(1, 1) != 40)
			{
				return Fail("filter 4 decoded " + image.R(0, 1) + "," + image.R(1, 1) + " instead of 30,40");
			}

			// --- colour types ------------------------------------------------
			// RGBA: channels must land in the right places, not shifted.
			byte[] rgba = new byte[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(2, 1, 8, 6, rgba, null, null), out image, out error))
			{
				return Fail("RGBA failed: " + error);
			}
			if (image.R(0, 0) != 1 || image.G(0, 0) != 2 || image.B(0, 0) != 3 || image.A(0, 0) != 4
				|| image.R(1, 0) != 5 || image.A(1, 0) != 8)
			{
				return Fail("RGBA channels were misplaced");
			}
			// Palette with tRNS: index 1 becomes fully transparent, index 0 keeps 255.
			byte[] palette = new byte[6] { 255, 0, 0, 0, 255, 0 };
			byte[] trns = new byte[2] { 255, 0 };
			byte[] indices = new byte[3] { 0, 0, 1 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(2, 1, 8, 3, indices, palette, trns), out image, out error))
			{
				return Fail("palette failed: " + error);
			}
			if (image.R(0, 0) != 255 || image.G(0, 0) != 0 || image.A(0, 0) != 255)
			{
				return Fail("a palette entry was decoded wrongly");
			}
			if (image.R(1, 0) != 0 || image.G(1, 0) != 255 || image.A(1, 0) != 0)
			{
				return Fail("tRNS transparency was not applied to a palette index");
			}
			// 1-bit palette: two pixels share a byte, the first in the high bit.
			byte[] bits = new byte[2] { 0, 96 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(4, 1, 1, 3, bits, palette, null), out image, out error))
			{
				return Fail("1-bit palette failed: " + error);
			}
			// 0b0110_0000 = indices 0,1,1,0.
			if (image.R(0, 0) != 255 || image.G(0, 0) != 0)
			{
				return Fail("the first 1-bit pixel was read from the wrong bit");
			}
			if (image.R(1, 0) != 0 || image.G(1, 0) != 255)
			{
				return Fail("the second 1-bit pixel was read from the wrong bit");
			}
			if (image.G(2, 0) != 255 || image.R(3, 0) != 255 || image.G(3, 0) != 0)
			{
				return Fail("sub-byte pixels after the second were read wrongly");
			}
			// Grey+alpha, and 16-bit keeping the high byte.
			byte[] greyAlpha = new byte[3] { 0, 100, 200 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(1, 1, 8, 4, greyAlpha, null, null), out image, out error))
			{
				return Fail("grey+alpha failed: " + error);
			}
			if (image.R(0, 0) != 100 || image.G(0, 0) != 100 || image.B(0, 0) != 100 || image.A(0, 0) != 200)
			{
				return Fail("grey+alpha was decoded wrongly");
			}
			byte[] deep = new byte[9] { 0, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
			if (!Terraria.Textures.PngDecoder.TryDecode(
				PngTestData.Build(1, 1, 16, 6, deep, null, null), out image, out error))
			{
				return Fail("16-bit failed: " + error);
			}
			if (image.R(0, 0) != 0x12 || image.G(0, 0) != 0x56 || image.B(0, 0) != 0x9A || image.A(0, 0) != 0xDE)
			{
				return Fail("16-bit samples were not collapsed to the high byte");
			}

			// --- refusals, each naming what is wrong -------------------------
			if (!ExpectPngFailure(PngTestData.Build(2, 2, 8, 0, new byte[6] { 0, 1, 2, 7, 3, 4 }, null, null), "filter type", "an unknown row filter"))
			{
				return Fail("an unknown filter type was accepted");
			}
			if (!ExpectPngFailure(PngTestData.BuildWithCorruptData(2, 2, 8, 0, new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF }), "corrupt", "corrupt image data"))
			{
				return Fail("corrupt image data was accepted");
			}
			if (!ExpectPngFailure(new byte[12] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 }, "IEND", "a PNG without IEND"))
			{
				return Fail("a truncated PNG was accepted");
			}
			if (!ExpectPngFailure(new byte[16] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 73, 72, 68, 82 }, "chunk", "a PNG whose chunk header is broken"))
			{
				return Fail("a broken chunk header was accepted");
			}
			if (!ExpectPngFailure(new byte[4] { 1, 2, 3, 4 }, "not a PNG", "a file that is not a PNG"))
			{
				return Fail("a non-PNG was accepted");
			}
			// Interlacing is refused by name, not mis-decoded into noise.
			byte[] interlaced = PngTestData.Build(2, 2, 8, 0, new byte[6] { 0, 1, 2, 0, 3, 4 }, null, null);
			// IHDR data starts after the 4-byte length and 4-byte type: width 16..19, height
			// 20..23, bit depth 24, colour type 25, compression 26, filter 27, interlace 28.
			interlaced[28] = 1;
			if (!ExpectPngFailure(interlaced, "interlac", "an interlaced PNG"))
			{
				return Fail("an interlaced PNG was not refused");
			}
			// A colour type the format does not define, and an unsupported bit depth: both must
			// be named, not turned into an exception from deep inside the pixel loop.
			byte[] badType = PngTestData.Build(2, 1, 8, 6, new byte[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, null, null);
			badType[25] = 5;
			if (!ExpectPngFailure(badType, "colour type", "an undefined colour type"))
			{
				return Fail("an undefined colour type was accepted");
			}
			byte[] badDepth = PngTestData.Build(2, 1, 8, 6, new byte[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, null, null);
			badDepth[24] = 3;
			if (!ExpectPngFailure(badDepth, "bit depth", "an invalid bit depth"))
			{
				return Fail("bit depth 3 was accepted for RGBA");
			}
			// A header claiming 16384x2000 (32 megapixels) is refused before anything is
			// allocated for it. Both dimensions are within the per-dimension limit, so this
			// exercises the total-pixel bound specifically.
			byte[] huge = PngTestData.Build(2, 1, 8, 6, new byte[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, null, null);
			huge[16] = 0;
			huge[17] = 0;
			huge[18] = 0x40;
			huge[19] = 0x00;
			huge[20] = 0;
			huge[21] = 0;
			huge[22] = 0x07;
			huge[23] = 0xD0;
			if (!ExpectPngFailure(huge, "megapixel", "an image over the total size limit"))
			{
				return Fail("a 16384x2000 image was not refused, or was refused for the wrong reason");
			}

			// --- premultiplied alpha -----------------------------------------
			Terraria.Textures.ImageData sample = new Terraria.Textures.ImageData(2, 1);
			sample.Set(0, 0, 200, 100, 50, 128);
			sample.Set(1, 0, 0, 0, 0, 0);
			Color[] colors = Terraria.Textures.TextureContentManager.ToColors(sample);
			if (colors.Length != 2)
			{
				return Fail("ToColors produced " + colors.Length + " colours for 2 pixels");
			}
			// 200*128/255 = 100.4 -> 100, 100*128/255 = 50.2 -> 50, 50*128/255 = 25.1 -> 25.
			if (colors[0].A != 128 || colors[0].R != 100 || colors[0].G != 50 || colors[0].B != 25)
			{
				return Fail("alpha was not premultiplied: got a=" + colors[0].A + " rgb=" + colors[0].R + "," + colors[0].G + "," + colors[0].B);
			}
			// A fully transparent pixel keeps zero colour, so filtering cannot pull colour out of it.
			if (colors[1].A != 0 || colors[1].R != 0 || colors[1].G != 0 || colors[1].B != 0)
			{
				return Fail("a transparent pixel kept non-zero colour");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("png decoder check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	/// <summary>True when decoding fails with a message containing <paramref name="fragment"/>.</summary>
	private static bool ExpectPngFailure(byte[] png, string fragment, string what)
	{
		Terraria.Textures.ImageData image;
		string error;
		if (Terraria.Textures.PngDecoder.TryDecode(png, out image, out error))
		{
			Console.WriteLine("    decoded " + what + " to " + ((image == null) ? "null" : image.Describe()));
			Console.WriteLine("    " + what + " was accepted");
			return false;
		}
		if (error == null || error.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0)
		{
			Console.WriteLine("    " + what + " failed with '" + error + "', expected it to mention '" + fragment + "'");
			return false;
		}
		return true;
	}

	// --- Texture overrides --------------------------------------------------

	/// <summary>
	/// The loose-PNG override path: which file wins, and what happens when it is broken.
	///
	/// The interesting cases are the failures, because they decide whether a bad texture takes
	/// the game with it: a missing file must fall through to the compiled asset silently, a
	/// broken one must fall through **with a reason in the log**, and an unsupported format must
	/// say so by name instead of pretending nothing is there.
	///
	/// This also runs the decoder over the real PNGs the project still has
	/// (<c>_RemovedContent/</c>), which is the only ground truth available: those files were
	/// written by a real encoder, so a broken Paeth or a wrong bit order shows up as nonsense
	/// rather than passing a self-consistent round trip.
	/// </summary>
	private static bool CheckTextureOverrides()
	{
		string root = Terraria.Textures.TextureOverrides.Root;
		List<string> created = new List<string>();
		try
		{
			Terraria.Textures.TextureOverrides.ResetProblems();
			Terraria.Textures.TextureOverrides.EnsureFolder();
			if (!Directory.Exists(root))
			{
				return Fail("the override folder was not created: " + root);
			}
			// The readme explains the naming rule, and must survive a second run.
			if (!File.Exists(Path.Combine(root, "readme.txt")))
			{
				return Fail("the override folder has no readme");
			}

			// A missing override is simply "no override".
			if (Terraria.Textures.TextureOverrides.Resolve("Images/NoSuchTexture") != null)
			{
				return Fail("a missing override resolved to a path");
			}
			string path;
			if (Terraria.Textures.TextureOverrides.Load("Images/NoSuchTexture", out path) != null)
			{
				return Fail("a missing override decoded to an image");
			}
			if (Terraria.Textures.TextureOverrides.Problems.Count != 0)
			{
				return Fail("a missing override was reported as a problem");
			}

			// A real override wins over the compiled asset.
			string target = Portability.Combine(root, "Images/selftest_texture.png");
			Directory.CreateDirectory(Path.GetDirectoryName(target));
			created.Add(target);
			File.WriteAllBytes(target, PngTestData.Build(2, 1, 8, 6, new byte[9] { 0, 255, 0, 0, 255, 0, 255, 0, 255 }, null, null));
			if (Terraria.Textures.TextureOverrides.Resolve("Images/selftest_texture") == null)
			{
				return Fail("an existing override did not resolve");
			}
			Terraria.Textures.ImageData image = Terraria.Textures.TextureOverrides.Load("Images/selftest_texture", out path);
			if (image == null)
			{
				return Fail("an existing override did not decode");
			}
			if (image.Width != 2 || image.R(0, 0) != 255 || image.A(0, 0) != 255 || image.G(1, 0) != 255)
			{
				return Fail("the override decoded to the wrong pixels");
			}
			if (Terraria.Textures.TextureOverrides.Problems.Count != 0)
			{
				return Fail("a valid override was reported as a problem");
			}
			// It must be listed, so a player can see what is installed.
			bool listed = false;
			List<string> installed = Terraria.Textures.TextureOverrides.Installed();
			for (int i = 0; i < installed.Count; i++)
			{
				if (installed[i].IndexOf("Images/selftest_texture.png", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					listed = true;
				}
			}
			if (!listed)
			{
				return Fail("an installed override was not listed");
			}

			// A broken override must not throw, and must say why.
			Terraria.Textures.TextureOverrides.ResetProblems();
			File.WriteAllBytes(target, new byte[16] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0 });
			if (Terraria.Textures.TextureOverrides.Load("Images/selftest_texture", out path) != null)
			{
				return Fail("a broken override decoded to an image");
			}
			IList<string> problems = Terraria.Textures.TextureOverrides.Problems;
			if (problems.Count != 1 || problems[0].IndexOf("built-in", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("a broken override did not explain that the built-in texture is used");
			}

			// An unsupported format is reported by name: "I put it there and nothing happened"
			// is the worst possible outcome.
			Terraria.Textures.TextureOverrides.ResetProblems();
			File.Delete(target);
			string dds = Portability.Combine(root, "Images/selftest_texture.dds");
			File.WriteAllBytes(dds, new byte[4] { 68, 68, 83, 32 });
			created.Add(dds);
			if (Terraria.Textures.TextureOverrides.Resolve("Images/selftest_texture") != null)
			{
				return Fail("a .dds override resolved to a path");
			}
			problems = Terraria.Textures.TextureOverrides.Problems;
			if (problems.Count != 1 || problems[0].IndexOf(".png", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("a .dds override was not reported as unsupported");
			}

			// Real files are a separate check: everything above is testable on a bare install,
			// and one missing corpus must not turn the whole check into a skip.
			return true;
		}
		finally
		{
			Terraria.Textures.TextureOverrides.ResetProblems();
			for (int k = 0; k < created.Count; k++)
			{
				try
				{
					if (File.Exists(created[k]))
					{
						File.Delete(created[k]);
					}
				}
				catch
				{
				}
			}
			// Leave the folder itself: it is a normal part of Storage/ now.
		}
	}

	/// <summary>
	/// The decoder against real files, which is the only ground truth available.
	///
	/// The hand-written cases pin exact pixels for each format feature, but they cannot catch a
	/// shared misunderstanding between decoder and test. These files were written by real
	/// encoders, so a broken Paeth predictor or a reversed bit order shows up as noise: thousands
	/// of distinct colours where the art has a few dozen.
	///
	/// They live in <c>_RemovedContent/</c>, outside git, so a clean clone skips rather than fails.
	/// </summary>
	private static bool CheckPngCorpus()
	{
		string corpus = Path.GetFullPath(Path.Combine(Terraria.Storage.AppPaths.BaseDirectory, "..", "..", "..", "..", "_RemovedContent"));
		if (!Directory.Exists(corpus))
		{
			Skip("no real PNGs to check the decoder against (" + corpus + " is missing)");
		}
		string[] files = Directory.GetFiles(corpus, "*.png", SearchOption.AllDirectories);
		if (files.Length < 5)
		{
			Skip("only " + files.Length + " real PNG(s) available");
		}
		try
		{
			int decoded = 0;
			for (int i = 0; i < files.Length; i++)
			{
				Terraria.Textures.ImageData real;
				string error;
				if (!Terraria.Textures.PngDecoder.TryDecode(File.ReadAllBytes(files[i]), out real, out error))
				{
					return Fail(Path.GetFileName(files[i]) + " did not decode: " + error);
				}
				if (real.Width <= 0 || real.Height <= 0)
				{
					return Fail(Path.GetFileName(files[i]) + " decoded to " + real.Width + "x" + real.Height);
				}
				HashSet<int> distinct = new HashSet<int>();
				for (int y = 0; y < real.Height; y++)
				{
					for (int x = 0; x < real.Width; x++)
					{
						if (distinct.Count < 4096)
						{
							distinct.Add((real.R(x, y) << 16) | (real.G(x, y) << 8) | real.B(x, y));
						}
					}
				}
				if (distinct.Count > 2000)
				{
					return Fail(Path.GetFileName(files[i]) + " decoded to " + distinct.Count + " distinct colours - that looks like noise");
				}
				if (distinct.Count < 2)
				{
					return Fail(Path.GetFileName(files[i]) + " decoded to a single flat colour - that cannot be right");
				}
				decoded++;
			}
			Program.LogInfo("SelfTest", "decoded " + decoded + " real PNG(s) from _RemovedContent");
			return true;
		}
		catch (Exception ex)
		{
			return Fail("png corpus check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Atlas overrides ----------------------------------------------------

	/// <summary>
	/// Per-sprite replacement inside a baked atlas.
	///
	/// Two things are worth checking, and only one of them is about pasting pixels. The other is
	/// the mapping itself: an override is addressed by name, and the name is turned into an index
	/// into <c>Source</c> by position in the enum. If those two arrays ever disagree in length,
	/// every sprite after the gap is replaced by its neighbour's PNG - a bug that would look like
	/// corrupted art, not like an off-by-one. So the real atlases are checked for that here.
	///
	/// The pasting is checked on a synthetic 8x8 sheet with hand-placed colours, which is enough
	/// to pin the arithmetic: position, scaling and premultiplied alpha.
	/// </summary>
	private static bool CheckAtlasOverrides()
	{
		string folder = null;
		try
		{
			// --- the mapping: enum and rectangle array must line up -------
			if (_sheetTiles.Names.Length != _sheetTiles.Source.Length)
			{
				return Fail("_sheetTiles has " + _sheetTiles.Names.Length + " names but " + _sheetTiles.Source.Length
					+ " rectangles - every sprite override past the gap would hit the wrong one");
			}
			if (_sheetSprites.Names.Length != _sheetSprites.Source.Length)
			{
				return Fail("_sheetSprites has " + _sheetSprites.Names.Length + " names but " + _sheetSprites.Source.Length
					+ " rectangles - every sprite override past the gap would hit the wrong one");
			}
			if (_sheetTiles.Names.Length < 200 || _sheetSprites.Names.Length < 1000)
			{
				return Fail("the atlases look empty: " + _sheetTiles.Names.Length + "/" + _sheetSprites.Names.Length);
			}
			// Names are the addressing scheme, so duplicates would make the override ambiguous.
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < _sheetTiles.Names.Length; i++)
			{
				if (!hashSet.Add(_sheetTiles.Names[i]))
				{
					return Fail("_sheetTiles has two sprites called " + _sheetTiles.Names[i]);
				}
			}
			// The atlas layout must be self-consistent: no rectangle may be negative, and the
			// union has to fit in something a texture could be.
			Rectangle unionTiles = UnionOf(_sheetTiles.Source);
			Rectangle unionSprites = UnionOf(_sheetSprites.Source);
			if (unionTiles.Width <= 0 || unionSprites.Width <= 0)
			{
				return Fail("the atlas rectangles are degenerate: " + unionTiles + " / " + unionSprites);
			}
			// The union must fit a texture the 360 could hold: its limit was 4096 per side, and a
			// rectangle beyond that would mean the Source table and the real atlas disagree.
			if (unionTiles.Width > 4096 || unionTiles.Height > 4096 || unionSprites.Width > 4096 || unionSprites.Height > 4096)
			{
				return Fail("an atlas is larger than 4096 per side: tiles " + unionTiles + ", sprites " + unionSprites);
			}
			// Printed because it is the number to compare against when a sprite lands in the wrong
			// place: if the layout here is not what the atlas file contains, that is the reason.
			Console.WriteLine("    atlas tiles   : " + _sheetTiles.Names.Length + " sprites in " + unionTiles.Width + "x" + unionTiles.Height);
			Console.WriteLine("    atlas sprites : " + _sheetSprites.Names.Length + " sprites in " + unionSprites.Width + "x" + unionSprites.Height);

			// --- planning, against a folder of real files --------------------
			folder = Terraria.Textures.AtlasOverrides.FolderFor("Images/selftest_atlas");
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, recursive: true);
			}
			Directory.CreateDirectory(folder);
			string[] names = new string[3] { "ALPHA", "BETA", "GAMMA" };
			Rectangle[] source = new Rectangle[3]
			{
				new Rectangle(0, 0, 4, 4),
				new Rectangle(4, 0, 4, 4),
				new Rectangle(0, 4, 4, 4)
			};
			// Nothing on disk is not a problem: it is the normal case.
			Terraria.Textures.AtlasPlan empty = Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, source, 8, 8);
			if (!empty.IsEmpty || empty.Problems.Count != 0)
			{
				return Fail("an empty atlas folder produced work or problems");
			}
			// A correctly named, correctly sized file.
			byte[] green = PngTestData.Build(4, 4, 8, 6, SolidRgba(4, 4, 0, 255, 0, 255), null, null);
			File.WriteAllBytes(Path.Combine(folder, "BETA.png"), green);
			Terraria.Textures.AtlasPlan plan = Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, source, 8, 8);
			if (plan.Patches.Count != 1 || plan.Problems.Count != 0 || plan.Notes.Count != 0)
			{
				return Fail("a valid sprite override was not planned cleanly (patches " + plan.Patches.Count
					+ ", problems " + plan.Problems.Count + ", notes " + plan.Notes.Count + ")");
			}
			if (plan.Patches[0].Name != "BETA" || plan.Patches[0].Rect != source[1])
			{
				return Fail("the override for BETA did not resolve to BETA's rectangle");
			}
			// Case must not matter: the enum name is an identifier, not a file name convention.
			File.Move(Path.Combine(folder, "BETA.png"), Path.Combine(folder, "beta.png"));
			if (Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, source, 8, 8).Patches.Count != 1)
			{
				return Fail("a lower-case sprite name did not match");
			}
			// An unknown name is reported by name - otherwise the file just sits there.
			File.Move(Path.Combine(folder, "beta.png"), Path.Combine(folder, "BETAA.png"));
			Terraria.Textures.AtlasPlan typo = Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, source, 8, 8);
			if (typo.Problems.Count != 1 || typo.Problems[0].IndexOf("BETAA", StringComparison.Ordinal) < 0)
			{
				return Fail("a mistyped sprite name was not reported by name");
			}
			File.Delete(Path.Combine(folder, "BETAA.png"));
			// Two files that differ only in case are the same sprite: the second must be refused
			// rather than silently winning or losing depending on directory order.
			//
			// On Windows they are one file - the file system is case-insensitive - so this can
			// only actually happen on Linux. The check accepts both outcomes and says which it
			// got, instead of failing on the platform where the situation cannot arise.
			File.WriteAllBytes(Path.Combine(folder, "BETA.png"), green);
			File.WriteAllBytes(Path.Combine(folder, "beta.png"), green);
			Terraria.Textures.AtlasPlan dup = Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, source, 8, 8);
			int filesForBeta = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly).Length;
			if (dup.Patches.Count != 1)
			{
				return Fail("two files for one sprite produced " + dup.Patches.Count + " patch(es)");
			}
			if (filesForBeta > 2 && dup.Problems.Count != 1)
			{
				// More than readme.txt + one BETA: the file system kept both spellings, so the
				// duplicate really was visible and should have been refused.
				return Fail("two spellings of one sprite were both accepted");
			}
			Console.WriteLine("    duplicate-case check: " + filesForBeta + " file(s) on disk, "
				+ dup.Problems.Count + " problem(s) reported");
			// Both spellings, because on Windows deleting one of them deletes the file they share.
			File.Delete(Path.Combine(folder, "BETA.png"));
			File.Delete(Path.Combine(folder, "beta.png"));
			// A .dds in the folder is a failed attempt, not noise.
			File.WriteAllBytes(Path.Combine(folder, "ALPHA.dds"), new byte[4] { 68, 68, 83, 32 });
			Terraria.Textures.AtlasPlan dds = Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, source, 8, 8);
			if (dds.Problems.Count != 1 || dds.Problems[0].IndexOf(".png", StringComparison.Ordinal) < 0)
			{
				return Fail("a .dds in the atlas folder was not reported");
			}
			File.Delete(Path.Combine(folder, "ALPHA.dds"));
			// The readme is expected and must not be reported.
			File.WriteAllText(Path.Combine(folder, "readme.txt"), "hello");
			if (Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, source, 8, 8).Problems.Count != 0)
			{
				return Fail("the atlas readme was reported as a problem");
			}
			// A rectangle that does not fit the texture means the code and the atlas disagree.
			File.WriteAllBytes(Path.Combine(folder, "BETA.png"), green);
			Rectangle[] tooBig = new Rectangle[3]
			{
				new Rectangle(0, 0, 4, 4),
				new Rectangle(4, 0, 40, 4),
				new Rectangle(0, 4, 4, 4)
			};
			Terraria.Textures.AtlasPlan overflow = Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, tooBig, 8, 8);
			if (overflow.Patches.Count != 0 || overflow.Problems.Count != 1 || overflow.Problems[0].IndexOf("outside", StringComparison.Ordinal) < 0)
			{
				return Fail("a rectangle outside the atlas was not refused");
			}
			// A different size is applied, not refused - and the fact is noted.
			File.Delete(Path.Combine(folder, "BETA.png"));
			File.WriteAllBytes(Path.Combine(folder, "BETA.png"), PngTestData.Build(2, 2, 8, 6, SolidRgba(2, 2, 0, 255, 0, 255), null, null));
			Terraria.Textures.AtlasPlan scaled = Terraria.Textures.AtlasOverrides.Build("Images/selftest_atlas", names, source, 8, 8);
			if (scaled.Patches.Count != 1 || scaled.Notes.Count != 1 || scaled.Problems.Count != 0)
			{
				return Fail("a differently sized sprite was not planned as a scaled patch");
			}

			// --- pasting into a synthetic sheet ------------------------------
			// 8x8 atlas, every pixel dark blue, so a patch stands out.
			Color[] sheet = new Color[64];
			for (int j = 0; j < sheet.Length; j++)
			{
				sheet[j] = new Color(0, 0, 64, 255);
			}
			int written = Terraria.Textures.AtlasComposer.Apply(sheet, 8, 8, scaled);
			if (written != 16)
			{
				return Fail("the patch wrote " + written + " pixels, expected 16");
			}
			// BETA's rect is x=4..7, y=0..3. Everything outside it must stay dark blue.
			Color untouched = new Color(0, 0, 64, 255);
			if (sheet[0] != untouched || sheet[3] != untouched)
			{
				return Fail("the patch spilled to the left of its rectangle");
			}
			if (sheet[8] != untouched || sheet[5 * 8 + 4] != untouched)
			{
				return Fail("the patch spilled below or above its rectangle");
			}
			if (sheet[4] != new Color(0, 255, 0, 255) || sheet[7] != new Color(0, 255, 0, 255))
			{
				return Fail("the patch did not fill its rectangle's corners");
			}
			if (sheet[3 * 8 + 7] != new Color(0, 255, 0, 255))
			{
				return Fail("the patch did not reach the bottom-right corner of its rectangle");
			}

			// Premultiplied alpha, and nearest-neighbour scaling, through the same path.
			Terraria.Textures.ImageData translucent = new Terraria.Textures.ImageData(2, 1);
			translucent.Set(0, 0, 200, 100, 50, 128);
			translucent.Set(1, 0, 0, 0, 0, 0);
			Terraria.Textures.AtlasPlan oneOff = new Terraria.Textures.AtlasPlan();
			Terraria.Textures.AtlasPatch patch = default(Terraria.Textures.AtlasPatch);
			patch.Name = "ALPHA";
			patch.Rect = new Rectangle(0, 0, 4, 1);
			patch.Image = translucent;
			oneOff.Patches.Add(patch);
			Color[] array = new Color[64];
			Terraria.Textures.AtlasComposer.Apply(array, 8, 8, oneOff);
			// 2 source pixels stretched over 4 destination pixels: 0,0,1,1.
			if (array[0].A != 128 || array[0].R != 100 || array[0].G != 50 || array[0].B != 25)
			{
				return Fail("a translucent patch was not premultiplied: a=" + array[0].A + " rgb=" + array[0].R + "," + array[0].G + "," + array[0].B);
			}
			if (array[1] != array[0])
			{
				return Fail("nearest-neighbour scaling did not repeat the source pixel");
			}
			if (array[2].A != 0 || array[2].R != 0)
			{
				return Fail("a transparent source pixel kept colour: a=" + array[2].A + " r=" + array[2].R);
			}
			if (array[3] != array[2])
			{
				return Fail("nearest-neighbour scaling did not repeat the transparent pixel");
			}
			// A patch whose rectangle no longer fits is skipped, not thrown: the texture size may
			// have changed between planning and composing.
			Terraria.Textures.AtlasPlan stale = new Terraria.Textures.AtlasPlan();
			Terraria.Textures.AtlasPatch outside = default(Terraria.Textures.AtlasPatch);
			outside.Rect = new Rectangle(0, 0, 100, 100);
			outside.Image = translucent;
			stale.Patches.Add(outside);
			if (Terraria.Textures.AtlasComposer.Apply(new Color[64], 8, 8, stale) != 0)
			{
				return Fail("a patch outside the sheet wrote pixels");
			}
			// An empty plan and a null sheet are no-ops, not crashes.
			if (Terraria.Textures.AtlasComposer.Apply(sheet, 8, 8, new Terraria.Textures.AtlasPlan()) != 0
				|| Terraria.Textures.AtlasComposer.Apply(null, 8, 8, scaled) != 0
				|| Terraria.Textures.AtlasComposer.Apply(new Color[4], 8, 8, scaled) != 0)
			{
				return Fail("an empty plan, a null sheet or a short buffer wrote pixels");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("atlas override check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			try
			{
				if (folder != null && Directory.Exists(folder))
				{
					Directory.Delete(folder, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	/// <summary>The bounding box of every rectangle, or an empty rectangle for none.</summary>
	private static Rectangle UnionOf(Rectangle[] rects)
	{
		if (rects == null || rects.Length == 0)
		{
			return default(Rectangle);
		}
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MinValue;
		int num4 = int.MinValue;
		for (int i = 0; i < rects.Length; i++)
		{
			num = Math.Min(num, rects[i].X);
			num2 = Math.Min(num2, rects[i].Y);
			num3 = Math.Max(num3, rects[i].Right);
			num4 = Math.Max(num4, rects[i].Bottom);
		}
		return new Rectangle(num, num2, num3 - num, num4 - num2);
	}

	/// <summary>Filtered rows for a solid RGBA image, for <see cref="PngTestData"/>.</summary>
	private static byte[] SolidRgba(int width, int height, byte r, byte g, byte b, byte a)
	{
		byte[] array = new byte[height * (1 + width * 4)];
		int num = 0;
		for (int i = 0; i < height; i++)
		{
			array[num++] = 0;
			for (int j = 0; j < width; j++)
			{
				array[num++] = r;
				array[num++] = g;
				array[num++] = b;
				array[num++] = a;
			}
		}
		return array;
	}

	// --- Anti-cheat ---------------------------------------------------------

	/// <summary>
	/// Movement validation. The check that matters most is not "a cheater is caught" - that is
	/// easy - but "an honest player is not", because a false positive freezes a real player for
	/// everyone else and is far worse than the cheat it was meant to stop.
	///
	/// So the honest cases come first and are the ones worth reading: a sprint at the game's own
	/// maximum speed, a long fall at terminal velocity, a lag spike that batches ten ticks into
	/// one packet, a respawn jump to spawn, and a grappling hook. All of those must pass.
	///
	/// Then the dishonest ones: a teleport across the map, a fly hack that drains the budget,
	/// NaN, and a position outside the world.
	///
	/// The clock is a parameter, so "ten seconds passed" costs nothing and the arithmetic is
	/// exact rather than dependent on how fast the machine runs.
	/// </summary>
	private static bool CheckAntiCheat()
	{
		AntiCheatMode savedMode = Terraria.Net.AntiCheat.Mode;
		try
		{
			// --- mode parsing -----------------------------------------------
			if (Terraria.Net.AntiCheat.Parse("enforce") != AntiCheatMode.Enforce
				|| Terraria.Net.AntiCheat.Parse("Off") != AntiCheatMode.Off
				|| Terraria.Net.AntiCheat.Parse(" 1 ") != AntiCheatMode.Observe)
			{
				return Fail("the mode parser does not understand the documented spellings");
			}
			// Garbage must never switch protection on: Observe is the safe reading.
			if (Terraria.Net.AntiCheat.Parse("yolo") != AntiCheatMode.Observe
				|| Terraria.Net.AntiCheat.Parse("") != AntiCheatMode.Observe
				|| Terraria.Net.AntiCheat.Parse(null) != AntiCheatMode.Observe)
			{
				return Fail("an unreadable mode did not fall back to Observe");
			}
			// The host command is stricter on purpose: it must refuse what it cannot name.
			AntiCheatMode ignored;
			if (Terraria.Net.AntiCheat.TryParseExact("yolo", out ignored)
				|| !Terraria.Net.AntiCheat.TryParseExact("enforce", out ignored) || ignored != AntiCheatMode.Enforce)
			{
				return Fail("the host command accepts something it should not, or rejects a real mode");
			}
			// Round trip through the settings text.
			if (Terraria.Net.AntiCheat.Parse(Terraria.Net.AntiCheat.ToSetting(AntiCheatMode.Enforce)) != AntiCheatMode.Enforce)
			{
				return Fail("the settings text does not round-trip");
			}

			// --- the clock ---------------------------------------------------
			// TickCount is a signed int that wraps every ~24.8 days. A negative difference means
			// either a clock that went backwards or a gap longer than that - and for a budget,
			// "unknown, so no credit" is the only safe reading. A wrap must never hand out a month
			// of movement credit.
			int backwards = Terraria.Net.AntiCheat.Delta(0, int.MaxValue);
			if (backwards != 0)
			{
				// Without the clamp this would be about 2^31, i.e. "move anywhere for 24 days".
				return Fail("a backwards clock produced " + backwards + " ms of credit");
			}
			if (Terraria.Net.AntiCheat.Delta(1000, 900) != 100)
			{
				return Fail("a normal interval was not measured");
			}
			if (Terraria.Net.AntiCheat.Delta(5, 5) != 0)
			{
				return Fail("two identical timestamps produced elapsed time");
			}

			// --- values that can never be real -------------------------------
			if (Terraria.Net.AntiCheat.IsFinite(float.NaN) || Terraria.Net.AntiCheat.IsFinite(float.PositiveInfinity)
				|| Terraria.Net.AntiCheat.IsFinite(1E+09f))
			{
				return Fail("a non-finite value was treated as a position");
			}
			if (!Terraria.Net.AntiCheat.IsFinite(1234.5f) || !Terraria.Net.AntiCheat.IsFinite(0f))
			{
				return Fail("a real coordinate was rejected as non-finite");
			}

			// --- honest movement, in Observe and in Enforce -------------------
			// Every case below is two packets: the first establishes where the player is, the
			// second is the one under test. Without the first packet every case would pass
			// trivially, because a player with no history has nothing to be compared against -
			// which is itself the reason the first sample is accepted unconditionally.
			//
			// Each is also a case where a naive per-packet distance check produces a false
			// positive, which is why they are listed instead of just "walking works".
			for (int mode = 0; mode < 2; mode++)
			{
				AntiCheatMode antiCheatMode = (mode == 0) ? AntiCheatMode.Observe : AntiCheatMode.Enforce;
				Terraria.Net.AntiCheat.Mode = antiCheatMode;

				// Sprint: 270 px in 1000 ms, under the 900 px/s the budget refills at.
				Terraria.Net.AntiCheat.Reset();
				if (!TwoStep(1, 0, 1000, new Vector2(0f, 0f), new Vector2(270f, 0f), false, false))
				{
					return Fail("a sprint (270 px/s) was flagged in " + antiCheatMode);
				}
				// Terminal fall: 600 px/s, measured as two packets because that is what 60Hz means.
				Terraria.Net.AntiCheat.Reset();
				if (!TwoStep(2, 0, 550, new Vector2(0f, 0f), new Vector2(0f, 330f), false, false)
					|| !TwoStep(2, 550, 550, new Vector2(0f, 330f), new Vector2(0f, 660f), false, false))
				{
					return Fail("a terminal-velocity fall was flagged in " + antiCheatMode);
				}
				// Lag spike: nothing arrives for half a second, then the backlog arrives with a
				// large jump. The distance is large AND the gap is large, so the budget covers it.
				Terraria.Net.AntiCheat.Reset();
				if (!TwoStep(3, 0, 500, new Vector2(0f, 0f), new Vector2(400f, 0f), false, false))
				{
					return Fail("a 500 ms lag spike was flagged in " + antiCheatMode);
				}
				// Respawn: thousands of pixels in one tick, legitimate, knowable only from state.
				Terraria.Net.AntiCheat.Reset();
				if (!TwoStep(4, 0, 16, new Vector2(3000f, 900f), new Vector2(3000f, 905f), false, false)
					|| !TwoStep(4, 16, 16, new Vector2(3000f, 905f), new Vector2(8000f, 400f), true, false))
				{
					return Fail("a respawn jump was flagged in " + antiCheatMode);
				}
				// Grappling: far faster than walking, and allowed because the player really is
				// attached to a hook.
				Terraria.Net.AntiCheat.Reset();
				if (!TwoStep(5, 0, 16, new Vector2(0f, 0f), new Vector2(50f, 0f), false, false)
					|| !TwoStep(5, 16, 16, new Vector2(50f, 0f), new Vector2(400f, 0f), false, true))
				{
					return Fail("a grappling hook pull was flagged in " + antiCheatMode);
				}
			}

			// --- cheating, refused in Enforce and only logged in Observe ------
			Terraria.Net.AntiCheat.Mode = AntiCheatMode.Enforce;
			Terraria.Net.AntiCheat.Reset();
			string reason;
			// A teleport across the map: 5000 px in one tick. `Allow` returning false is the
			// success here, so the condition must not be negated.
			if (!TwoStep(10, 0, 16, new Vector2(100f, 100f), new Vector2(110f, 100f), false, false))
			{
				return Fail("the setup move for the teleport test was refused");
			}
			if (Terraria.Net.AntiCheat.Allow(10, 32, new Vector2(110f, 100f), new Vector2(5100f, 100f), false, false, 0, 0, out reason))
			{
				return Fail("a 5000 px teleport was allowed in Enforce");
			}
			if (reason == null || reason.IndexOf("moved", StringComparison.Ordinal) < 0)
			{
				return Fail("a refused move did not say why: '" + reason + "'");
			}
			if (Terraria.Net.AntiCheat.Rejected != 1)
			{
				return Fail("a refused move was not counted (rejected " + Terraria.Net.AntiCheat.Rejected + ")");
			}
			// The reference must not move to the refused position: that is what keeps a cheater
			// frozen instead of walking away one refused hop at a time. A second packet claiming
			// the same far-off spot must be refused as well, not accepted as "no movement".
			Terraria.Net.AntiCheatTrack after = Terraria.Net.AntiCheat.TrackOf(10);
			if (after == null || after.Last != new Vector2(110f, 100f))
			{
				return Fail("a refused move advanced the last accepted position to " + ((after == null) ? "nothing" : after.Last.ToString()));
			}
			if (Terraria.Net.AntiCheat.Allow(10, 48, new Vector2(5100f, 100f), new Vector2(5100f, 100f), false, false, 0, 0, out reason))
			{
				return Fail("a second packet from the refused position was accepted");
			}
			// The same move in Observe is relayed, and the verdict is recorded.
			Terraria.Net.AntiCheat.Mode = AntiCheatMode.Observe;
			Terraria.Net.AntiCheat.Reset();
			if (!TwoStep(10, 0, 16, new Vector2(100f, 100f), new Vector2(110f, 100f), false, false))
			{
				return Fail("the setup move for the observe test was refused");
			}
			if (!Terraria.Net.AntiCheat.Allow(10, 32, new Vector2(110f, 100f), new Vector2(5100f, 100f), false, false, 0, 0, out reason))
			{
				return Fail("Observe refused a move; it must only observe");
			}
			if (Terraria.Net.AntiCheat.Suspicious == 0)
			{
				return Fail("Observe did not record the move it let through");
			}
			if (Terraria.Net.AntiCheat.TrackOf(10) == null || Terraria.Net.AntiCheat.TrackOf(10).Violations != 1)
			{
				return Fail("Observe did not count the violation for the player");
			}

			// A fly hack, sustained. Two speeds are worth distinguishing, because they are not
			// equally detectable and pretending otherwise would make the numbers meaningless:
			// a dramatic one is caught in about a second, and one only a little above the
			// legitimate maximum takes long enough that the budget has to drain first.
			Terraria.Net.AntiCheat.Mode = AntiCheatMode.Enforce;
			if (!SpeedHackCaught(11, 1200f, 80, out int caughtAt))
			{
				return Fail("a 1200 px/s fly hack (2x terminal velocity) was not caught within 80 packets");
			}
			Console.WriteLine("    fly hack at 1200 px/s caught after " + caughtAt + " packet(s)");
			if (!SpeedHackCaught(12, 812f, 400, out caughtAt))
			{
				return Fail("an 812 px/s hack (just above the budget) was not caught within 400 packets");
			}
			Console.WriteLine("    hack at 812 px/s caught after " + caughtAt + " packet(s)");

			// NaN and infinity are refused in Observe too: they are not a judgement call, and
			// applying one corrupts the host's world for everyone else.
			Terraria.Net.AntiCheat.Mode = AntiCheatMode.Observe;
			Terraria.Net.AntiCheat.Reset();
			if (Terraria.Net.AntiCheat.Allow(13, 0, new Vector2(0f, 0f), new Vector2(float.NaN, 0f), false, false, 0, 0, out reason)
				|| reason == null || reason.IndexOf("not a number", StringComparison.Ordinal) < 0)
			{
				return Fail("a NaN position was accepted, or refused without saying why");
			}
			// Outside the world, with the margin: 8000x2400 tiles is 128000x38400 px.
			Terraria.Net.AntiCheat.Reset();
			if (Terraria.Net.AntiCheat.Allow(14, 0, new Vector2(0f, 0f), new Vector2(-5000f, 0f), false, false, 128000, 38400, out reason)
				|| reason == null || reason.IndexOf("outside", StringComparison.Ordinal) < 0)
			{
				return Fail("a position outside the world was accepted, or refused without saying why");
			}
			// The margin itself: a player at the very edge is fine.
			Terraria.Net.AntiCheat.Reset();
			if (!Terraria.Net.AntiCheat.Allow(15, 0, new Vector2(0f, 0f), new Vector2(127990f, 38390f), false, false, 128000, 38400, out reason))
			{
				return Fail("a player at the world edge was refused: " + reason);
			}

			// --- the mode switch itself --------------------------------------
			Terraria.Net.AntiCheat.Mode = AntiCheatMode.Off;
			Terraria.Net.AntiCheat.Reset();
			if (!Terraria.Net.AntiCheat.Allow(15, 0, new Vector2(0f, 0f), new Vector2(50000f, 0f), false, false, 128000, 38400, out reason))
			{
				return Fail("Off still refused a move");
			}
			if (Terraria.Net.AntiCheat.Suspicious != 0 || Terraria.Net.AntiCheat.TrackOf(15) != null)
			{
				return Fail("Off still recorded state");
			}
			// Off must also not refuse a NaN: the mode means "do nothing", and pretending
			// otherwise would make the setting a lie.
			if (!Terraria.Net.AntiCheat.Allow(15, 16, new Vector2(0f, 0f), new Vector2(float.NaN, 0f), false, false, 0, 0, out reason))
			{
				return Fail("Off refused a NaN, which contradicts the mode");
			}
			// Forget drops one player's history without touching anyone else's.
			Terraria.Net.AntiCheat.Mode = AntiCheatMode.Observe;
			Terraria.Net.AntiCheat.Reset();
			TwoStep(20, 0, 16, new Vector2(0f, 0f), new Vector2(10f, 0f), false, false);
			TwoStep(21, 0, 16, new Vector2(0f, 0f), new Vector2(10f, 0f), false, false);
			Terraria.Net.AntiCheat.Forget(20);
			if (Terraria.Net.AntiCheat.TrackOf(20) != null || Terraria.Net.AntiCheat.TrackOf(21) == null)
			{
				return Fail("Forget did not drop exactly one player");
			}
			// An out-of-range index is ignored, not a crash: this is called from the network path.
			if (!Terraria.Net.AntiCheat.Allow(-1, 0, Vector2.Zero, Vector2.Zero, false, false, 0, 0, out reason)
				|| !Terraria.Net.AntiCheat.Allow(99999, 0, Vector2.Zero, Vector2.Zero, false, false, 0, 0, out reason))
			{
				return Fail("an out-of-range player index was refused");
			}
			Terraria.Net.AntiCheat.Forget(-1);
			Terraria.Net.AntiCheat.Forget(99999);

			// The host command's report names every player with violations.
			Terraria.Net.AntiCheat.Reset();
			TwoStep(30, 0, 16, new Vector2(0f, 0f), new Vector2(10f, 0f), false, false);
			Terraria.Net.AntiCheat.Allow(30, 32, new Vector2(10f, 0f), new Vector2(9000f, 0f), false, false, 0, 0, out reason);
			string text = Terraria.Net.AntiCheat.Describe();
			if (text.IndexOf("player 30", StringComparison.Ordinal) < 0)
			{
				return Fail("Describe() does not name a player with violations: '" + text + "'");
			}
			if (text.IndexOf("Observe", StringComparison.Ordinal) < 0)
			{
				return Fail("Describe() does not state the mode: '" + text + "'");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("anti-cheat check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Net.AntiCheat.Mode = savedMode;
			Terraria.Net.AntiCheat.Reset();
		}
	}

	/// <summary>
	/// One move that must be allowed: the first packet places the player, the second is the one
	/// under test, <paramref name="elapsedMs"/> later. Returns what <c>Allow</c> said for the
	/// second, so the same helper serves the honest and the dishonest cases.
	/// </summary>
	private static bool TwoStep(int player, int startMs, int elapsedMs, Vector2 from, Vector2 to, bool dead, bool grappling)
	{
		string reason;
		if (!Terraria.Net.AntiCheat.Allow(player, startMs, from, from, grappling, dead, 0, 0, out reason))
		{
			// The setup itself must never be refused; if it is, the test is measuring nothing.
			Console.WriteLine("    setup for player " + player + " was refused: " + reason);
			return false;
		}
		return Terraria.Net.AntiCheat.Allow(player, startMs + elapsedMs, from, to, grappling, dead, 0, 0, out reason);
	}

	/// <summary>
	/// Sends a player moving at a fixed speed, one packet per 16 ms, until the host refuses a
	/// move. Returns whether it was caught within <paramref name="maxPackets"/>, plus the packet
	/// index it was caught at - the number is the useful part, because it says how long a real
	/// host would watch a cheat before acting.
	/// </summary>
	private static bool SpeedHackCaught(int player, float pixelsPerSecond, int maxPackets, out int caughtAt)
	{
		caughtAt = -1;
		Terraria.Net.AntiCheat.Reset();
		string reason;
		Vector2 vector = new Vector2(0f, 0f);
		float num = pixelsPerSecond * 0.016f;
		for (int i = 0; i < maxPackets; i++)
		{
			Vector2 vector2 = new Vector2(vector.X + num, vector.Y);
			if (!Terraria.Net.AntiCheat.Allow(player, i * 16, vector, vector2, false, false, 0, 0, out reason))
			{
				caughtAt = i;
				return true;
			}
			vector = vector2;
		}
		return false;
	}

	// --- Menu tables --------------------------------------------------------

	/// <summary>
	/// The menu arrays are indexed by <see cref="MenuMode"/>, which is a byte enum that keeps
	/// growing as screens are added. Every one of these was sized by hand, and one of them was
	/// left at 40 while the enum reached 47 - so Cheats, Keyboard bindings, Mods, Display,
	/// Network, HUD and Backups all threw <c>IndexOutOfRangeException</c> the moment they opened,
	/// with a stack trace that pointed at SetMenu and said nothing about the real cause.
	///
	/// The fix was to size them from the enum; this check is what stops it from happening again.
	/// It costs nothing, runs without a graphics device, and covers every mode that exists now
	/// or is added later.
	/// </summary>
	private static bool CheckMenuTables()
	{
		int num = (int)MenuMode.NUM_MENUS;
		// The tables are sized from UI.MenuSlots, so one comparison covers both of them.
		if (UI.MenuSlots < num)
		{
			return Fail("UI.MenuSlots is " + UI.MenuSlots + " but MenuMode has " + num + " entries - screens past "
				+ UI.MenuSlots + " would crash on entry");
		}
		// Naming the screens that broke makes a future failure say which one, not just "too small".
		MenuMode[] added = new MenuMode[7]
		{
			MenuMode.CHEATS,
			MenuMode.KEYBINDS,
			MenuMode.MODS,
			MenuMode.DISPLAY,
			MenuMode.NETWORK,
			MenuMode.HUD,
			MenuMode.BACKUPS
		};
		for (int i = 0; i < added.Length; i++)
		{
			if ((int)added[i] >= UI.MenuSlots)
			{
				return Fail(added[i] + " (index " + (int)added[i] + ") does not fit in " + UI.MenuSlots + " menu slots");
			}
		}
		// The enum is a byte, so the largest legal value is 255; a table far larger than the enum
		// would mean the size is no longer derived from it.
		if (UI.MenuSlots > 256)
		{
			return Fail("UI.MenuSlots is " + UI.MenuSlots + ", which is not derived from the byte enum");
		}
		Console.WriteLine("    menu modes: " + num + ", slots " + UI.MenuSlots + ", highest screen " + added[added.Length - 1] + " = " + (int)added[added.Length - 1]);
		return true;
	}

	// --- Text entry ---------------------------------------------------------

	/// <summary>
	/// Typing into the on-screen keyboard, and clicking it.
	///
	/// The bug this covers: the virtual keyboard was the only way to enter text without a
	/// controller, yet with a keyboard attached it could not be typed into at all - opening it
	/// sets <c>TextInputActive</c>, which deliberately suppresses the synthetic pad, and nothing
	/// else read the keys. So the field was unreachable exactly for the players who do have a
	/// keyboard.
	///
	/// Characters arrive through <see cref="TextInputEXT"/>, the same source chat uses, so the OS
	/// layout applies. The test drives that event directly, which is what FNA does when a key is
	/// pressed.
	/// </summary>
	private static bool CheckTextEntry()
	{
		try
		{
			string accepted = null;
			int cancels = 0;
			Terraria.Input.VirtualKeyboard.Open("Test", "",
				delegate(string text) { accepted = text; },
				delegate { cancels++; });
			if (!Terraria.Input.VirtualKeyboard.IsOpen)
			{
				return Fail("the keyboard did not open");
			}
			if (!Terraria.Input.KeyboardMouseInput.TextInputActive)
			{
				return Fail("opening the keyboard did not claim text input");
			}
			// Printable characters append.
			string text = "abc123";
			for (int i = 0; i < text.Length; i++)
			{
				TextInputEXT.OnTextInput(text[i]);
			}
			if (Terraria.Input.VirtualKeyboard.Text != "abc123")
			{
				return Fail("typing produced '" + Terraria.Input.VirtualKeyboard.Text + "'");
			}
			// Backspace and delete both delete, and neither underflows.
			TextInputEXT.OnTextInput('\b');
			if (Terraria.Input.VirtualKeyboard.Text != "abc12")
			{
				return Fail("backspace produced '" + Terraria.Input.VirtualKeyboard.Text + "'");
			}
			TextInputEXT.OnTextInput('\u007f');
			if (Terraria.Input.VirtualKeyboard.Text != "abc1")
			{
				return Fail("delete produced '" + Terraria.Input.VirtualKeyboard.Text + "'");
			}
			// Enter must NOT reach the buffer: it is handled as "confirm" by the key loop, and
			// appending it as well would insert a newline the player never asked for.
			TextInputEXT.OnTextInput('\r');
			if (Terraria.Input.VirtualKeyboard.Text != "abc1")
			{
				return Fail("Enter was inserted into the text: '" + Terraria.Input.VirtualKeyboard.Text + "'");
			}
			// Control characters are ignored.
			TextInputEXT.OnTextInput('\u0001');
			if (Terraria.Input.VirtualKeyboard.Text != "abc1")
			{
				return Fail("a control character was inserted");
			}
			// The 200 character limit holds.
			for (int j = 0; j < 300; j++)
			{
				TextInputEXT.OnTextInput('x');
			}
			if (Terraria.Input.VirtualKeyboard.Text.Length != 200)
			{
				return Fail("the length limit is " + Terraria.Input.VirtualKeyboard.Text.Length + ", expected 200");
			}
			// Committing closes it and hands the text over exactly once.
			Terraria.Input.VirtualKeyboard.SetText("host:7777");
			if (!Terraria.Input.VirtualKeyboard.Commit())
			{
				return Fail("Commit reported nothing was listening");
			}
			if (accepted != "host:7777")
			{
				return Fail("Commit handed over '" + accepted + "'");
			}
			if (Terraria.Input.VirtualKeyboard.IsOpen || Terraria.Input.KeyboardMouseInput.TextInputActive)
			{
				return Fail("the keyboard stayed open after Commit");
			}
			// Characters arriving after it closed must not reach anything.
			TextInputEXT.OnTextInput('z');
			// Reopen: cancel runs the cancel handler and does not accept.
			int num = cancels;
			Terraria.Input.VirtualKeyboard.Open("Test", "seed", null, delegate { cancels++; });
			if (!Terraria.Input.VirtualKeyboard.CancelEXT())
			{
				return Fail("Cancel reported nothing was open");
			}
			if (cancels != num + 1)
			{
				return Fail("the cancel handler did not run");
			}
			if (Terraria.Input.VirtualKeyboard.IsOpen)
			{
				return Fail("the keyboard stayed open after cancel");
			}

			// --- clicking a key ---------------------------------------------
			// The grid must map a click to the key drawn there. CellRect is shared with the
			// drawing code, so a mismatch here would mean a click lands on a different letter.
			// The layout reads Main's resolution, which the headless suite never initialises
			// (Main is not constructed), so it is set here to the 540p preset and restored after.
			int savedWidth = Main.ResolutionWidth;
			int savedHeight = Main.ResolutionHeight;
			float savedMultiplier = Main.ScreenMultiplier;
			Main.ResolutionWidth = 960;
			Main.ResolutionHeight = 720;
			Main.ScreenMultiplier = 1f;
			try
			{
				Terraria.Input.VirtualKeyboard.Open("Test", "", delegate { }, null);
				Rectangle letter = Terraria.Input.VirtualKeyboard.CellRect(0, 0);
				if (letter.Width <= 0 || letter.Height <= 0)
				{
					return Fail("a key cell has no size");
				}
				if (!Terraria.Input.VirtualKeyboard.ClickCell(letter.Center.X, letter.Center.Y))
				{
					return Fail("a click on the first key was not handled");
				}
				// Row 0 column 0 of the letters page is '1'.
				if (Terraria.Input.VirtualKeyboard.Text != "1")
				{
					return Fail("clicking the first key produced '" + Terraria.Input.VirtualKeyboard.Text + "'");
				}
				// A click outside the grid does nothing and reports so.
				if (Terraria.Input.VirtualKeyboard.ClickCell(-50, -50))
				{
					return Fail("a click outside the grid was treated as a key");
				}
				// Space is two cells wide, and `del` starts after it: they must not overlap, which
				// was the old layout bug (one key drew on top of another).
				Rectangle space = Terraria.Input.VirtualKeyboard.CellRect(0, 4, wide: true);
				Rectangle del = Terraria.Input.VirtualKeyboard.CellRect(2, 4);
				if (space.Right > del.X + 4 && del.X < space.Right)
				{
					return Fail("the space key overlaps the delete key");
				}
				Terraria.Input.VirtualKeyboard.CancelEXT();
			}
			finally
			{
				Main.ResolutionWidth = savedWidth;
				Main.ResolutionHeight = savedHeight;
				Main.ScreenMultiplier = savedMultiplier;
			}

			// --- the volume slider geometry ---------------------------------
			// A click at 25% of the bar must set 25%, not "one step from wherever it was". The
			// texture is null on purpose: Bounds and SetFromPointer are pure geometry, and this
			// must be checkable without a graphics device.
			Terraria.Slider slider = new Terraria.Slider(null,
				new Rectangle(0, 0, 244, 58), new Rectangle(0, 116, 244, 58), new Vector2(100f, 200f));
			Rectangle bar = slider.Bounds;
			if (bar.X != 100 || bar.Y != 200 || bar.Width != 244 || bar.Height != 58)
			{
				return Fail("the slider bounds are " + bar + ", expected 100,200 244x58");
			}
			slider.SetFromPointer(100 + 61);
			if (Math.Abs(slider.SliderProgress - 0.25f) > 0.02f)
			{
				return Fail("a click at 25% of the bar set " + slider.SliderProgress.ToString("0.00"));
			}
			// A click left of the bar clamps to 0, right of it to 1, and neither throws.
			slider.SetFromPointer(0);
			if (slider.SliderProgress != 0f)
			{
				return Fail("a click left of the bar set " + slider.SliderProgress);
			}
			slider.SetFromPointer(10000);
			if (slider.SliderProgress != 1f)
			{
				return Fail("a click right of the bar set " + slider.SliderProgress);
			}
			// The bounds must not grow as progress is split into filled and empty parts.
			slider.SetFromPointer(100 + 122);
			if (slider.Bounds.Width != 244)
			{
				return Fail("the slider bounds changed width with progress: " + slider.Bounds.Width);
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("text entry check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Input.VirtualKeyboard.CancelEXT();
		}
	}

	// --- Screen geometry ----------------------------------------------------

	/// <summary>
	/// The "what the player can see" rectangle, which several gameplay checks depend on.
	///
	/// These were authored against the 1080p preset with the numbers written out
	/// (<c>1920x1080</c>, offsets of 960 and 540), so at 540p the monster-spawn and town-NPC
	/// teleport ranges covered more than twice the visible screen, and at 720p they were simply
	/// wrong. Deriving them from <c>ResolutionWidth/Height</c> fixed that; this check keeps them
	/// derived, and pins the property that matters: the rectangle is centred on the point and
	/// exactly one screen plus a margin in size, at every preset.
	/// </summary>
	private static bool CheckScreenGeometry()
	{
		int savedWidth = Main.ResolutionWidth;
		int savedHeight = Main.ResolutionHeight;
		float savedMultiplier = Main.ScreenMultiplier;
		try
		{
			// The three presets, as DisplaySettings.ApplyRenderScale sets them.
			int[][] presets = new int[3][]
			{
				new int[2] { 960, 720 },
				new int[2] { 1280, 960 },
				new int[2] { 1920, 1440 }
			};
			for (int i = 0; i < presets.Length; i++)
			{
				Main.ResolutionWidth = presets[i][0];
				Main.ResolutionHeight = presets[i][1];
				int num = Main.ScreenHalfWidth;
				int num2 = Main.ScreenHalfHeight;
				if (num * 2 != Main.ResolutionWidth || num2 * 2 != Main.ResolutionHeight)
				{
					return Fail("ScreenHalfWidth/Height are not half of " + Main.ResolutionWidth + "x" + Main.ResolutionHeight);
				}
				Rectangle rectangle = Main.ScreenAreaAround(1000, 2000);
				if (rectangle.Width != Main.ResolutionWidth + Main.ScreenMargin * 2
					|| rectangle.Height != Main.ResolutionHeight + Main.ScreenMargin * 2)
				{
					return Fail("the screen area is " + rectangle.Width + "x" + rectangle.Height + " at " + Main.ResolutionWidth + "x" + Main.ResolutionHeight);
				}
				// Centred: the point must be the middle of the rectangle.
				if (rectangle.X + (rectangle.Width >> 1) != 1000 || rectangle.Y + (rectangle.Height >> 1) != 2000)
				{
					return Fail("the screen area is not centred on the point at " + Main.ResolutionWidth + "x" + Main.ResolutionHeight);
				}
				// A point far outside must not overflow the arithmetic.
				Rectangle far = Main.ScreenAreaAround(-3000000, 3000000);
				if (far.Width != rectangle.Width || far.Height != rectangle.Height)
				{
					return Fail("the screen area changed size far from the origin");
				}
			}
			// The old hardcoded numbers, for the record: at 540p the area must now be smaller than
			// the 1920x1080 that was written into the code, or the fix did nothing.
			Main.ResolutionWidth = 960;
			Main.ResolutionHeight = 720;
			if (Main.ScreenAreaAround(0, 0).Width >= 1920)
			{
				return Fail("the 540p screen area is still as wide as the old hardcoded 1920");
			}
			Console.WriteLine("    screen area at 540p: " + Main.ScreenAreaAround(0, 0).Width + "x" + Main.ScreenAreaAround(0, 0).Height
				+ " (was 1920x1080 before the fix)");
			return true;
		}
		catch (Exception ex)
		{
			return Fail("screen geometry check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Main.ResolutionWidth = savedWidth;
			Main.ResolutionHeight = savedHeight;
			Main.ScreenMultiplier = savedMultiplier;
		}
	}

	// --- Pointer scaling ----------------------------------------------------

	/// <summary>
	/// Window pixels to internal resolution.
	///
	/// The game draws into a RenderTarget of <c>ResolutionWidth x ResolutionHeight</c> and stretches
	/// it to the backbuffer, but the pointer arrives in window pixels. Without the conversion the
	/// mouse only reached part of the screen - and it was invisible at 540p in a 720p window,
	/// because "almost right" looks like a slightly-off cursor rather than a bug.
	/// </summary>
	private static bool CheckSaveSlots()
	{
		try
		{
			// File name -> slot index. The list is built from files, and misreading a name puts
			// somebody else's save in the row you clicked.
			int index;
			if (!Terraria.Storage.SaveSlots.TryParseIndex("player3.plr", "player", ".plr", out index) || index != 3)
			{
				return Fail("player3.plr did not parse as slot 3");
			}
			if (!Terraria.Storage.SaveSlots.TryParseIndex("player12.plr", "player", ".plr", out index) || index != 12)
			{
				return Fail("a two-digit slot did not parse (the old pattern allowed one digit)");
			}
			string[] notSlots = new string[5] { "player.plr", "playerX.plr", "players3.plr", "player3.plr.bak", "world3.wld" };
			for (int i = 0; i < notSlots.Length; i++)
			{
				if (Terraria.Storage.SaveSlots.TryParseIndex(notSlots[i], "player", ".plr", out index))
				{
					return Fail("'" + notSlots[i] + "' was accepted as slot " + index);
				}
			}
			// Highest index plus one: a gap left by a deletion still needs a row, and the row after
			// it must keep its number.
			string[] files = new string[3] { "player0.plr", "player2.plr", "player1.plr" };
			if (Terraria.Storage.SaveSlots.SlotCount(files, "player", ".plr") != 3)
			{
				return Fail("three saved characters did not need three slots");
			}
			string[] gap = new string[2] { "player0.plr", "player7.plr" };
			if (Terraria.Storage.SaveSlots.SlotCount(gap, "player", ".plr") != 8)
			{
				return Fail("a slot gap was collapsed instead of preserved");
			}
			if (Terraria.Storage.SaveSlots.SlotCount(new string[0], "player", ".plr") != 0)
			{
				return Fail("an empty directory asked for slots");
			}
			// Free slot reuse: deleting from the middle must not shift the files.
			List<bool> used = new List<bool> { true, true, false, true };
			if (Terraria.Storage.SaveSlots.NextFreeSlot(used) != 2)
			{
				return Fail("the free slot in the middle was not reused");
			}
			if (Terraria.Storage.SaveSlots.NextFreeSlot(new List<bool> { true, true }) != 2)
			{
				return Fail("a full list did not grow");
			}
			// Scrolling.
			if (Terraria.Storage.SaveSlots.ClampScroll(5, 0, 6, 4) != 0)
			{
				return Fail("a list shorter than the window scrolled");
			}
			if (Terraria.Storage.SaveSlots.ClampScroll(0, 7, 6, 20) != 2)
			{
				return Fail("the window did not follow a selection below it");
			}
			if (Terraria.Storage.SaveSlots.ClampScroll(9, 3, 6, 20) != 3)
			{
				return Fail("the window did not follow a selection above it");
			}
			if (Terraria.Storage.SaveSlots.ClampScroll(0, 19, 6, 20) != 14)
			{
				return Fail("the window scrolled past the end of the list");
			}
			if (Terraria.Storage.SaveSlots.ClampScroll(99, 0, 6, 20) != 0)
			{
				return Fail("a scroll position past the end was not brought back");
			}
			// Cursor wrap in both directions.
			if (Terraria.Storage.SaveSlots.Move(0, -1, 5) != 4 || Terraria.Storage.SaveSlots.Move(4, 1, 5) != 0)
			{
				return Fail("the cursor does not wrap at the ends");
			}
			if (Terraria.Storage.SaveSlots.Move(3, 0, 0) != 0)
			{
				return Fail("an empty list produced a cursor position");
			}
			// Row height arithmetic: never zero rows, or nothing is drawn and nothing is clickable.
			if (Terraria.Storage.SaveSlots.VisibleRows(400, 40) != 10
				|| Terraria.Storage.SaveSlots.VisibleRows(39, 40) != 1
				|| Terraria.Storage.SaveSlots.VisibleRows(0, 40) != 1
				|| Terraria.Storage.SaveSlots.VisibleRows(400, 0) != 1)
			{
				return Fail("visible row count is wrong at a boundary");
			}
			if (!Terraria.Storage.SaveSlots.NeedsScroll(20, 6) || Terraria.Storage.SaveSlots.NeedsScroll(6, 6))
			{
				return Fail("the page counter appears when it is not needed, or hides when it is");
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("save slots check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool CheckInventoryTransfer()
	{
		try
		{
			// Shift-click: merge into existing stacks first, then take one empty slot. The cases
			// that matter are the ones where a naive "swap the two slots" would duplicate or lose
			// items.
			Item[] from = new Item[4];
			Item[] to = new Item[4];
			for (int i = 0; i < from.Length; i++)
			{
				from[i].Init();
				to[i].Init();
			}
			// A whole stack into an empty container.
			from[0].SetDefaults((int)EntityID.ItemID.DIRT_BLOCK, 250);
			int num;
			if (!Terraria.InventoryTransfer.Move(from, 0, to, to.Length, out num) || num != 0)
			{
				return Fail("a full stack did not move into an empty slot");
			}
			if (from[0].Type != 0 || to[0].Type != (int)EntityID.ItemID.DIRT_BLOCK || to[0].Stack != 250)
			{
				return Fail("the stack moved but the counts are wrong");
			}
			// Into a half-full stack of the same item, with no free slot: top it up and keep the
			// remainder in the source. Losing the remainder would be worse than a partial move.
			from[1].SetDefaults((int)EntityID.ItemID.DIRT_BLOCK, 250);
			to[0].Stack = 200;
			to[1].SetDefaults((int)EntityID.ItemID.STONE_BLOCK, 1);
			to[2].SetDefaults((int)EntityID.ItemID.STONE_BLOCK, 1);
			to[3].SetDefaults((int)EntityID.ItemID.STONE_BLOCK, 1);
			if (!Terraria.InventoryTransfer.Move(from, 1, to, to.Length, out num) || num != 0)
			{
				return Fail("a stack did not top up an existing one");
			}
			if (to[0].Stack != 250 || from[1].Stack != 200)
			{
				return Fail("topping up lost or duplicated items: target " + to[0].Stack + ", source " + from[1].Stack);
			}
			// The same, but the container has room: the remainder goes to the next free slot.
			to[3].Init();
			if (!Terraria.InventoryTransfer.Move(from, 1, to, to.Length, out num) || num != 3)
			{
				return Fail("the remainder did not go into the next free slot");
			}
			if (from[1].Type != 0 || to[0].Stack != 250 || to[3].Stack != 200)
			{
				return Fail("the remainder was placed wrong: source " + from[1].Type + ", target " + to[0].Stack + "/" + to[3].Stack);
			}
			// A full container: nothing moves, and nothing is lost.
			Item[] full = new Item[2];
			for (int i = 0; i < full.Length; i++)
			{
				full[i].Init();
				full[i].SetDefaults((int)EntityID.ItemID.STONE_BLOCK, 1);
			}
			from[2].SetDefaults((int)EntityID.ItemID.IRON_BROADSWORD);
			if (Terraria.InventoryTransfer.Move(from, 2, full, full.Length, out num))
			{
				return Fail("a stack moved into a container with no free slot");
			}
			if (from[2].Type != (int)EntityID.ItemID.IRON_BROADSWORD || from[2].Stack != 1)
			{
				return Fail("an item was lost when the container was full");
			}
			// An empty source is not a move.
			from[3].Init();
			if (Terraria.InventoryTransfer.Move(from, 3, to, to.Length, out num))
			{
				return Fail("an empty slot reported a transfer");
			}
			// Out-of-range slot and null arrays are refused, not thrown: the UI computes the slot
			// from a cursor that can be on a row the container does not have.
			if (Terraria.InventoryTransfer.Move(from, 99, to, to.Length, out num)
				|| Terraria.InventoryTransfer.Move(null, 0, to, to.Length, out num)
				|| Terraria.InventoryTransfer.Move(from, 0, null, 0, out num))
			{
				return Fail("an impossible transfer was accepted");
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("inventory transfer check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool CheckCreateFlowBack()
	{
		try
		{
			// The bug: both "saved a name" and "leave without saving? Yes" used PrevMenu(-2), and a
			// negative depth means "step back one", not "back two screens". So the player landed
			// back in the character creator: the name was saved but the screen looked stuck, and
			// the confirm dialog looked like it ignored Yes.
			UI ui = new UI();
			ui.SetMenu(MenuMode.CHARACTER_SELECT, rememberPrevious: false, reset: true);
			ui.SetMenu(MenuMode.CREATE_CHARACTER);
			ui.SetMenu(MenuMode.NAME_CHARACTER);
			if (ui.CurMenuMode != MenuMode.NAME_CHARACTER)
			{
				return Fail("the test could not reach the name screen (at " + ui.CurMenuMode + ")");
			}
			ui.ReturnToCharacterSelect();
			if (ui.CurMenuMode != MenuMode.CHARACTER_SELECT)
			{
				return Fail("saving a character went to " + ui.CurMenuMode + " instead of the character list");
			}
			// Depth 0 means Back from the list leaves for the title, which is where the list was
			// entered from. A deeper depth would send the player into the creator again.
			if (ui.MenuDepthForTests != 0)
			{
				return Fail("the character list was left at history depth " + ui.MenuDepthForTests + " instead of 0");
			}
			// Cancelling a name still returns to the creator: that is one step back on purpose.
			ui.SetMenu(MenuMode.CREATE_CHARACTER, rememberPrevious: false);
			ui.SetMenu(MenuMode.NAME_CHARACTER);
			ui.PrevMenu();
			if (ui.CurMenuMode != MenuMode.CREATE_CHARACTER)
			{
				return Fail("cancelling a name went to " + ui.CurMenuMode + " instead of back to the creator");
			}
			// And the "leave" dialog answered Yes reaches the list from wherever it was opened.
			ui.SetMenu(MenuMode.CONFIRM_LEAVE_CREATE_CHARACTER);
			ui.ReturnToCharacterSelect();
			if (ui.CurMenuMode != MenuMode.CHARACTER_SELECT)
			{
				return Fail("leaving character creation went to " + ui.CurMenuMode + " instead of the character list");
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("creation flow check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool CheckTypedCharacters()
	{
		try
		{
			// The on-screen keyboard has two ways to receive a character: FNA's text input, and a
			// fallback from key edges when the platform delivers nothing. The fallback's mapping is
			// pure and is checked here; the "do not double" rule lives in VirtualKeyboard.Update
			// and only runs when no text event arrived that frame.
			if (Terraria.Input.VirtualKeyboard.KeyChar(Keys.A, false) != 'a'
				|| Terraria.Input.VirtualKeyboard.KeyChar(Keys.A, true) != 'A')
			{
				return Fail("letters do not map to the right case");
			}
			if (Terraria.Input.VirtualKeyboard.KeyChar(Keys.Z, false) != 'z'
				|| Terraria.Input.VirtualKeyboard.KeyChar(Keys.M, false) != 'm')
			{
				return Fail("the letter range does not end where it should");
			}
			if (Terraria.Input.VirtualKeyboard.KeyChar(Keys.D0, false) != '0'
				|| Terraria.Input.VirtualKeyboard.KeyChar(Keys.D9, false) != '9'
				|| Terraria.Input.VirtualKeyboard.KeyChar(Keys.D1, true) != '!')
			{
				return Fail("digits or their shifted forms are wrong");
			}
			if (Terraria.Input.VirtualKeyboard.KeyChar(Keys.Space, false) != ' ')
			{
				return Fail("space does not produce a space");
			}
			// The characters people actually put in a name or an address.
			Keys[] punctuation = new Keys[6]
			{
				Keys.OemPeriod,
				Keys.OemMinus,
				Keys.OemMinus,
				Keys.OemComma,
				Keys.OemQuestion,
				Keys.OemSemicolon
			};
			bool[] shifted = new bool[6] { false, false, true, false, false, true };
			string expected = ".-_,/:";
			for (int i = 0; i < punctuation.Length; i++)
			{
				char c = Terraria.Input.VirtualKeyboard.KeyChar(punctuation[i], shifted[i]);
				if (c != expected[i])
				{
					return Fail(punctuation[i] + (shifted[i] ? " (shift)" : "") + " produced '" + c + "', expected '" + expected[i] + "'");
				}
			}
			// Modifiers and function keys produce nothing: typing a letter must not be the only
			// thing that happens when Shift is pressed on its own.
			Keys[] silent = new Keys[5] { Keys.LeftShift, Keys.RightShift, Keys.LeftControl, Keys.F1, Keys.Enter };
			for (int j = 0; j < silent.Length; j++)
			{
				if (Terraria.Input.VirtualKeyboard.KeyChar(silent[j], false) != '\0')
				{
					return Fail(silent[j] + " produced a character");
				}
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("typed characters check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool CheckKeyboardGrid()
	{
		try
		{
			// Every cell that is drawn has to be reachable by the cursor: a button the player can
			// see but not select is the "keys do not press" report. The bottom row is the case
			// that was wrong - the X cell was drawn at column 5 while wrap-around stopped at 4.
			Terraria.Input.VirtualKeyboard.Open("Grid", "", null, null);
			if (!Terraria.Input.VirtualKeyboard.IsOpen)
			{
				return Fail("the keyboard did not open for the grid check");
			}
			// Open puts the cursor on the letter row; three rows down is the action row.
			for (int i = 0; i < 3; i++)
			{
				Terraria.Input.VirtualKeyboard.MoveForTests(0, 1);
			}
			if (Terraria.Input.VirtualKeyboard.RowForTests != 4)
			{
				return Fail("three rows down put the cursor on row " + Terraria.Input.VirtualKeyboard.RowForTests + " instead of the action row");
			}
			for (int j = 0; j < 5; j++)
			{
				Terraria.Input.VirtualKeyboard.MoveForTests(1, 0);
			}
			if (Terraria.Input.VirtualKeyboard.ColForTests != 5)
			{
				return Fail("the last action cell (X, column 5) cannot be reached: cursor at column " + Terraria.Input.VirtualKeyboard.ColForTests);
			}
			// Wrapping continues from there in both directions.
			Terraria.Input.VirtualKeyboard.MoveForTests(1, 0);
			if (Terraria.Input.VirtualKeyboard.ColForTests != 0)
			{
				return Fail("moving right past the last cell did not wrap to the first");
			}
			Terraria.Input.VirtualKeyboard.MoveForTests(-1, 0);
			if (Terraria.Input.VirtualKeyboard.ColForTests != 5)
			{
				return Fail("moving left from the first cell did not wrap to the last");
			}
			Terraria.Input.VirtualKeyboard.CancelEXT();
			if (Terraria.Input.VirtualKeyboard.IsOpen)
			{
				return Fail("the keyboard did not close after the grid check");
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("keyboard grid check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	/// <summary>
	/// The relay conversation, from the client's side.
	///
	/// A stub relay in this file speaks the same protocol the master's relay does, so the game's
	/// own code (<see cref="Terraria.Net.RelayLink"/>) is driven over real sockets: register a
	/// room, poll for a waiting player, accept a ticket, and check that after ACCEPT the socket
	/// carries bytes untouched - the relay must never look inside the game's frames.
	///
	/// The master has its own test for the server side of the same protocol; two independent
	/// implementations agreeing is the point.
	/// </summary>
	private static bool CheckRelayClient()
	{
		try
		{
			// --- the protocol helpers, without sockets -------------------------
			if (Terraria.Net.RelayLink.HostLine("abc") != "HOST abc\n")
			{
				return Fail("the HOST command is not formatted as documented");
			}
			if (Terraria.Net.RelayLink.JoinLine("room1") != "JOIN room1\n")
			{
				return Fail("the JOIN command is not formatted as documented");
			}
			string room;
			if (!Terraria.Net.RelayLink.TryParseRoom("OK 1a2b3c", out room) || room != "1a2b3c")
			{
				return Fail("a room id was not parsed from 'OK <id>'");
			}
			if (Terraria.Net.RelayLink.TryParseRoom("ERR busy", out room) || Terraria.Net.RelayLink.TryParseRoom("OK ", out room))
			{
				return Fail("a refusal or an empty answer was accepted as a room id");
			}
			string ticket;
			if (!Terraria.Net.RelayLink.TryParseTicket("JOIN deadbeef", out ticket) || ticket != "deadbeef")
			{
				return Fail("a ticket was not parsed from 'JOIN <ticket>'");
			}
			if (!Terraria.Net.RelayLink.IsEnd("END") || Terraria.Net.RelayLink.IsEnd("JOIN x"))
			{
				return Fail("the end of a poll answer is not recognised");
			}
			string reason;
			if (!Terraria.Net.RelayLink.IsWait("WAIT", out reason))
			{
				return Fail("'WAIT' was not recognised as 'parked, not yet accepted'");
			}
			if (Terraria.Net.RelayLink.IsWait("ERR no such room", out reason) || reason != "no such room")
			{
				return Fail("a relay refusal did not surface its reason (got '" + reason + "')");
			}
			// A token must be long enough that guessing it is not an option: the relay rejects
			// short ones, and the client must never send one.
			string token = Terraria.Net.RelayLink.NewToken();
			if (token == null || token.Length != 32 || token == Terraria.Net.RelayLink.NewToken())
			{
				return Fail("the room token is not 128 bits of unique hex");
			}

			// --- the real socket path, against a stub relay --------------------
			StubRelay stub = new StubRelay();
			try
			{
				string error;
				// The client's settings are what Register reads; set them for the test and restore.
				bool savedAllow = Terraria.Net.NetworkConfig.AllowRelay;
				string savedServer = Terraria.Net.NetworkConfig.RelayServer;
				int savedPort = Terraria.Net.NetworkConfig.RelayPort;
				Terraria.Net.NetworkConfig.AllowRelay = true;
				Terraria.Net.NetworkConfig.RelayServer = "127.0.0.1:" + stub.Port;
				Terraria.Net.NetworkConfig.RelayPort = stub.Port;
				try
				{
					string text = Terraria.Net.RelayLink.Register(out error);
					if (text == null)
					{
						return Fail("registering a room failed: " + error);
					}
					if (!stub.HasHost)
					{
						return Fail("the stub relay never received the HOST command");
					}
					// A player joins and waits; the host's poll must see exactly that ticket.
					StubClient joining = stub.Join(text);
					System.Collections.Generic.List<string> list = Terraria.Net.RelayLink.Poll("127.0.0.1", stub.Port, stub.Token, out error);
					if (list.Count != 1)
					{
						return Fail("the poll returned " + list.Count + " ticket(s), expected 1 (" + error + ")");
					}
					// ACCEPT pairs the two sockets; from then on they are a plain pipe.
					if (stub.Accept(list[0]) == null)
					{
						return Fail("the stub relay refused a valid ticket");
					}
					stub.SendToHost("game bytes");
					if (joining.Read(10) != "game bytes")
					{
						return Fail("host bytes did not reach the waiting player unchanged");
					}
					joining.Write("client says hi");
					if (stub.ReadAtHost(14) != "client says hi")
					{
						return Fail("player bytes did not reach the host unchanged");
					}
					joining.Close();
				}
				finally
				{
					Terraria.Net.RelayLink.Stop();
					Terraria.Net.NetworkConfig.AllowRelay = savedAllow;
					Terraria.Net.NetworkConfig.RelayServer = savedServer;
					Terraria.Net.NetworkConfig.RelayPort = savedPort;
				}
			}
			finally
			{
				stub.Stop();
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("relay client check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	/// <summary>
	/// A stub internet gateway: answers SSDP, serves a device description and accepts SOAP.
	///
	/// Written against the protocol rather than against <see cref="Terraria.Net.UPnP"/>: it
	/// reads the SOAP body it is sent and builds the reply from the action name, so a client that
	/// sends the wrong field names fails here instead of passing because both sides agreed.
	/// </summary>
	private sealed class StubGateway
	{
		private readonly System.Net.Sockets.TcpListener _listener;

		private readonly System.Net.Sockets.UdpClient _ssdp;

		private readonly System.Threading.Thread _httpThread;

		private readonly System.Threading.Thread _ssdpThread;

		private volatile bool _stopping;

		/// <summary>When non-zero, AddPortMapping answers with this UPnP error code.</summary>
		public int FaultOnAdd;

		/// <summary>Internal client reported by GetSpecificPortMappingEntry.</summary>
		public string ExistingClient;

		public bool SawDelete;

		public string LastMapping;

		public int SsdpPort => ((System.Net.IPEndPoint)_ssdp.Client.LocalEndPoint).Port;

		public int HttpPort => ((System.Net.IPEndPoint)_listener.LocalEndpoint).Port;

		public StubGateway()
		{
			_listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
			_listener.Start();
			_ssdp = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
			_ssdp.Client.ReceiveTimeout = 500;
			_httpThread = new System.Threading.Thread(HttpLoop)
			{
				IsBackground = true
			};
			_httpThread.Start();
			_ssdpThread = new System.Threading.Thread(SsdpLoop)
			{
				IsBackground = true
			};
			_ssdpThread.Start();
		}

		private void SsdpLoop()
		{
			while (!_stopping)
			{
				try
				{
					System.Net.IPEndPoint remote = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
					byte[] array = _ssdp.Receive(ref remote);
					string text = Encoding.ASCII.GetString(array);
					if (text.IndexOf("M-SEARCH", StringComparison.Ordinal) < 0)
					{
						continue;
					}
					string text2 = "HTTP/1.1 200 OK\r\n"
						+ "CACHE-CONTROL: max-age=120\r\n"
						+ "EXT:\r\n"
						+ "LOCATION: http://127.0.0.1:" + HttpPort.ToString() + "/rootDesc.xml\r\n"
						+ "SERVER: Stub/1.0 UPnP/1.1\r\n"
						+ "ST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n"
						+ "USN: uuid:stub::urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n"
						+ "\r\n";
					byte[] bytes = Encoding.ASCII.GetBytes(text2);
					_ssdp.Send(bytes, bytes.Length, remote);
				}
				catch (Exception)
				{
					if (_stopping)
					{
						return;
					}
				}
			}
		}

		private void HttpLoop()
		{
			while (!_stopping)
			{
				try
				{
					using System.Net.Sockets.TcpClient tcpClient = _listener.AcceptTcpClient();
					tcpClient.ReceiveTimeout = 4000;
					System.Net.Sockets.NetworkStream stream = tcpClient.GetStream();
					string text = ReadRequest(stream, out int contentLength);
					if (text.StartsWith("GET ", StringComparison.Ordinal))
					{
						Respond(stream, 200, Description());
						continue;
					}
					byte[] array = new byte[contentLength];
					int num = 0;
					while (num < contentLength)
					{
						int num2 = stream.Read(array, num, contentLength - num);
						if (num2 <= 0)
						{
							break;
						}
						num += num2;
					}
					Respond(stream, 0, Handle(text, Encoding.UTF8.GetString(array, 0, num)));
				}
				catch (Exception)
				{
					if (_stopping)
					{
						return;
					}
				}
			}
		}

		/// <summary>Reads the request line and headers; returns the header block as one string.</summary>
		private static string ReadRequest(System.Net.Sockets.NetworkStream Stream, out int ContentLength)
		{
			ContentLength = 0;
			StringBuilder stringBuilder = new StringBuilder();
			while (true)
			{
				int num = Stream.ReadByte();
				if (num < 0)
				{
					break;
				}
				stringBuilder.Append((char)num);
				if (stringBuilder.Length >= 4 && stringBuilder[stringBuilder.Length - 4] == '\r'
					&& stringBuilder[stringBuilder.Length - 3] == '\n'
					&& stringBuilder[stringBuilder.Length - 2] == '\r'
					&& stringBuilder[stringBuilder.Length - 1] == '\n')
				{
					break;
				}
			}
			string text = stringBuilder.ToString();
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (text2.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
				{
					int.TryParse(text2.Substring(15).Trim(), out ContentLength);
				}
			}
			return text;
		}

		private string Description()
		{
			return "<?xml version=\"1.0\"?><root xmlns=\"urn:schemas-upnp-org:device-1-0\">"
				+ "<specVersion><major>1</major><minor>0</minor></specVersion>"
				+ "<device><deviceType>urn:schemas-upnp-org:device:InternetGatewayDevice:1</deviceType>"
				+ "<deviceList><device><deviceType>urn:schemas-upnp-org:device:WANDevice:1</deviceType>"
				+ "<deviceList><device><deviceType>urn:schemas-upnp-org:device:WANConnectionDevice:1</deviceType>"
				+ "<serviceList><service>"
				+ "<serviceType>urn:schemas-upnp-org:service:WANIPConnection:1</serviceType>"
				+ "<serviceId>urn:upnp-org:serviceId:WANIPConn1</serviceId>"
				+ "<controlURL>/ctl/IPConn</controlURL>"
				+ "<eventSubURL>/evt/IPConn</eventSubURL>"
				+ "<SCPDURL>/WANIPCn.xml</SCPDURL>"
				+ "</service></serviceList>"
				+ "</device></deviceList></device></deviceList></device></root>";
		}

		/// <summary>Builds the reply from the SOAPAction header. Status 0 means "let the body decide".</summary>
		private string Handle(string headers, string body)
		{
			string text = "AddPortMapping";
			string[] array = headers.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (!text2.StartsWith("SOAPAction:", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				int num = text2.LastIndexOf('#');
				if (num >= 0)
				{
					text = text2.Substring(num + 1).Trim().Trim('"');
				}
			}
			if (text == "AddPortMapping")
			{
				LastMapping = body;
				if (FaultOnAdd != 0)
				{
					return Fault(FaultOnAdd, (FaultOnAdd == 718) ? "ConflictInMappingEntry" : "ActionFailed");
				}
				return "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body>"
					+ "<u:AddPortMappingResponse xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\"/>"
					+ "</s:Body></s:Envelope>";
			}
			if (text == "DeletePortMapping")
			{
				SawDelete = true;
				return "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body>"
					+ "<u:DeletePortMappingResponse xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\"/>"
					+ "</s:Body></s:Envelope>";
			}
			if (text == "GetSpecificPortMappingEntry")
			{
				if (ExistingClient == null)
				{
					return Fault(714, "NoSuchEntryInArray");
				}
				return "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body>"
					+ "<u:GetSpecificPortMappingEntryResponse xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">"
					+ "<NewInternalPort>7777</NewInternalPort>"
					+ "<NewInternalClient>" + ExistingClient + "</NewInternalClient>"
					+ "<NewEnabled>1</NewEnabled>"
					+ "<NewPortMappingDescription>TerrariaXDK</NewPortMappingDescription>"
					+ "<NewLeaseDuration>0</NewLeaseDuration>"
					+ "</u:GetSpecificPortMappingEntryResponse></s:Body></s:Envelope>";
			}
			if (text == "GetExternalIPAddress")
			{
				return "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body>"
					+ "<u:GetExternalIPAddressResponse xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">"
					+ "<NewExternalIPAddress>203.0.113.7</NewExternalIPAddress>"
					+ "</u:GetExternalIPAddressResponse></s:Body></s:Envelope>";
			}
			return Fault(401, "InvalidAction");
		}

		private static string Fault(int code, string description)
		{
			return "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><s:Fault>"
				+ "<faultcode>s:Client</faultcode><faultstring>UPnPError</faultstring><detail>"
				+ "<UPnPError xmlns=\"urn:schemas-upnp-org:control-1-0\">"
				+ "<errorCode>" + code.ToString() + "</errorCode>"
				+ "<errorDescription>" + description + "</errorDescription>"
				+ "</UPnPError></detail></s:Fault></s:Body></s:Envelope>";
		}

		/// <summary>
		/// Writes the reply. <paramref name="Status"/> 0 means "faults go out as 500, the rest as
		/// 200", which is what real routers do.
		/// </summary>
		private static void Respond(System.Net.Sockets.NetworkStream Stream, int Status, string Body)
		{
			if (Status == 0)
			{
				Status = (Body.IndexOf("s:Fault", StringComparison.Ordinal) >= 0) ? 500 : 200;
			}
			byte[] bytes = Encoding.UTF8.GetBytes(Body);
			string text = "HTTP/1.1 " + Status.ToString() + " " + ((Status == 200) ? "OK" : "Internal Server Error") + "\r\n"
				+ "Content-Type: text/xml; charset=\"utf-8\"\r\n"
				+ "Content-Length: " + bytes.Length.ToString() + "\r\n"
				+ "Connection: close\r\n"
				+ "\r\n";
			byte[] bytes2 = Encoding.ASCII.GetBytes(text);
			Stream.Write(bytes2, 0, bytes2.Length);
			Stream.Write(bytes, 0, bytes.Length);
			Stream.Flush();
		}

		public void Stop()
		{
			_stopping = true;
			try
			{
				_listener.Stop();
			}
			catch
			{
			}
			try
			{
				_ssdp.Close();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// A minimal relay that speaks the documented protocol, used only by the check above.
	///
	/// Deliberately separate from the master's implementation: if both were the same code, the
	/// test would only prove the code agrees with itself.
	/// </summary>
	private sealed class StubRelay
	{
		private sealed class Waiting
		{
			public System.Net.Sockets.TcpClient Client;

			public string Ticket;
		}

		private System.Net.Sockets.TcpListener _listener;

		private System.Threading.Thread _thread;

		private volatile bool _stopping;

		private System.Net.Sockets.TcpClient _host;

		private readonly System.Collections.Generic.List<Waiting> _waiting = new System.Collections.Generic.List<Waiting>();

		private int _nextTicket;

		public readonly string Token = "0123456789abcdef0123456789abcdef";

		public string Room = "room01";

		public bool HasHost;

		public int Port => ((System.Net.IPEndPoint)_listener.LocalEndpoint).Port;

		public StubRelay()
		{
			_listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
			_listener.Start();
			_thread = new System.Threading.Thread(Loop)
			{
				IsBackground = true
			};
			_thread.Start();
		}

		private void Loop()
		{
			while (!_stopping)
			{
				try
				{
					System.Net.Sockets.TcpClient tcpClient = _listener.AcceptTcpClient();
					tcpClient.NoDelay = true;
					string text = ReadLine(tcpClient);
					if (text == null)
					{
						tcpClient.Close();
						continue;
					}
					if (text.StartsWith("HOST ", StringComparison.Ordinal))
					{
						_host = tcpClient;
						HasHost = true;
						Write(tcpClient, "OK " + Room + "\n");
					}
					else if (text.StartsWith("JOIN ", StringComparison.Ordinal))
					{
						Waiting waiting = new Waiting();
						waiting.Client = tcpClient;
						waiting.Ticket = "ticket" + (++_nextTicket).ToString();
						lock (_waiting)
						{
							_waiting.Add(waiting);
						}
						Write(tcpClient, "WAIT\n");
					}
					else if (text.StartsWith("POLL ", StringComparison.Ordinal))
					{
						System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
						lock (_waiting)
						{
							for (int i = 0; i < _waiting.Count; i++)
							{
								stringBuilder.Append("JOIN ").Append(_waiting[i].Ticket).Append('\n');
							}
						}
						stringBuilder.Append("END\n");
						Write(tcpClient, stringBuilder.ToString());
						tcpClient.Close();
					}
					else if (text.StartsWith("ACCEPT ", StringComparison.Ordinal))
					{
						string ticket = text.Substring(7).Trim();
						System.Net.Sockets.TcpClient tcpClient2 = null;
						lock (_waiting)
						{
							for (int j = 0; j < _waiting.Count; j++)
							{
								if (_waiting[j].Ticket == ticket)
								{
									tcpClient2 = _waiting[j].Client;
									_waiting.RemoveAt(j);
									break;
								}
							}
						}
						if (tcpClient2 == null)
						{
							Write(tcpClient, "ERR bad ticket\n");
							tcpClient.Close();
							continue;
						}
						// Both directions: the real relay copies bytes each way, and a one-way stub
						// would hide a client that never hears the host.
						StartPipe(tcpClient2, tcpClient);
						StartPipe(tcpClient, tcpClient2);
					}
					else
					{
						Write(tcpClient, "ERR unknown command\n");
						tcpClient.Close();
					}
				}
				catch
				{
					return;
				}
			}
		}

		/// <summary>Starts one direction of the pair, the way the real relay does.</summary>
		private void StartPipe(System.Net.Sockets.TcpClient From, System.Net.Sockets.TcpClient To)
		{
			System.Threading.Thread thread = new System.Threading.Thread(delegate()
			{
				Pipe(From, To);
			})
			{
				IsBackground = true
			};
			thread.Start();
		}

		private void Pipe(System.Net.Sockets.TcpClient A, System.Net.Sockets.TcpClient B)
		{
			try
			{
				byte[] array = new byte[4096];
				while (!_stopping)
				{
					int num = A.GetStream().Read(array, 0, array.Length);
					if (num <= 0)
					{
						break;
					}
					B.GetStream().Write(array, 0, num);
					B.GetStream().Flush();
				}
			}
			catch
			{
			}
		}

		/// <summary>Connects a fake player and returns its socket, waiting for the relay's "WAIT".</summary>
		public StubClient Join(string RoomId)
		{
			System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
			tcpClient.NoDelay = true;
			tcpClient.ReceiveTimeout = 4000;
			tcpClient.Connect(System.Net.IPAddress.Loopback, Port);
			Write(tcpClient, "JOIN " + RoomId + "\n");
			string text = ReadLine(tcpClient);
			if (text != "WAIT")
			{
				throw new InvalidOperationException("the stub relay did not park the client: " + text);
			}
			return new StubClient(tcpClient);
		}

		/// <summary>Dials back with a ticket, as the host does, and returns the paired socket.</summary>
		public System.Net.Sockets.TcpClient Accept(string Ticket)
		{
			System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
			tcpClient.NoDelay = true;
			tcpClient.ReceiveTimeout = 4000;
			tcpClient.Connect(System.Net.IPAddress.Loopback, Port);
			Write(tcpClient, "ACCEPT " + Ticket + "\n");
			System.Threading.Thread.Sleep(100);
			HostSocket = tcpClient;
			return tcpClient;
		}

		public System.Net.Sockets.TcpClient HostSocket;

		public void SendToHost(string Text)
		{
			Write(HostSocket, Text);
		}

		/// <summary>Reads the first <paramref name="Count"/> bytes the paired client sent.</summary>
		public string ReadAtHost(int Count)
		{
			byte[] array = new byte[Count];
			int num = 0;
			while (num < Count)
			{
				int num2 = HostSocket.GetStream().Read(array, num, Count - num);
				if (num2 <= 0)
				{
					break;
				}
				num += num2;
			}
			return Encoding.UTF8.GetString(array, 0, num);
		}

		private static string ReadLine(System.Net.Sockets.TcpClient Client)
		{
			try
			{
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
				while (true)
				{
					int num = Client.GetStream().ReadByte();
					if (num < 0)
					{
						return null;
					}
					if (num == 10)
					{
						return stringBuilder.ToString().TrimEnd('\r');
					}
					stringBuilder.Append((char)num);
				}
			}
			catch
			{
				return null;
			}
		}

		private static void Write(System.Net.Sockets.TcpClient Client, string Text)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(Text);
			Client.GetStream().Write(bytes, 0, bytes.Length);
			Client.GetStream().Flush();
		}

		public void Stop()
		{
			_stopping = true;
			try
			{
				_listener.Stop();
			}
			catch
			{
			}
		}
	}

	private sealed class StubClient
	{
		private readonly System.Net.Sockets.TcpClient _client;

		public StubClient(System.Net.Sockets.TcpClient Client)
		{
			_client = Client;
		}

		public void Write(string Text)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(Text);
			_client.GetStream().Write(bytes, 0, bytes.Length);
			_client.GetStream().Flush();
		}

		/// <summary>Reads exactly <paramref name="Count"/> bytes, or fewer if the pipe closes.</summary>
		public string Read(int Count)
		{
			byte[] array = new byte[Count];
			int num = 0;
			while (num < Count)
			{
				int num2 = _client.GetStream().Read(array, num, Count - num);
				if (num2 <= 0)
				{
					break;
				}
				num += num2;
			}
			return Encoding.UTF8.GetString(array, 0, num);
		}

		public void Close()
		{
			try
			{
				_client.Close();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// The same conversation against the real relay, when one is running.
	///
	/// The stub above proves the game's side of the protocol; this proves the two independent
	/// implementations actually interoperate. Skipped when no relay answers, so a clean clone
	/// without a master server still passes - the same rule as the master list check.
	/// </summary>
	private static bool CheckRelayLive()
	{
		bool savedAllow = Terraria.Net.NetworkConfig.AllowRelay;
		string savedServer = Terraria.Net.NetworkConfig.RelayServer;
		int savedPort = Terraria.Net.NetworkConfig.RelayPort;
		string room = null;
		try
		{
			// The headless suite never constructs Main, so the settings that Main would have read
			// are still at their defaults. Read them the same way, without writing back.
			if (!Terraria.Net.NetworkConfig.AllowRelay)
			{
				Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
				try
				{
					Terraria.Net.NetworkConfig.AllowRelay = settings.Get("Network", "AllowRelay").ToLower().Equals("true");
					Terraria.Net.NetworkConfig.RelayServer = settings.Get("Network", "RelayServer");
					int result;
					if (int.TryParse(settings.Get("Network", "RelayPort"), out result) && result > 0 && result <= 65535)
					{
						Terraria.Net.NetworkConfig.RelayPort = result;
					}
				}
				finally
				{
					settings.Discard();
				}
			}
			string host;
			int port;
			if (!Terraria.Net.RelayLink.TryGetEndpoint(out host, out port))
			{
				Skip("relay is disabled in settings ([Network] AllowRelay=False)");
			}
			Terraria.Net.NetworkConfig.AllowRelay = true;
			Terraria.Net.NetworkConfig.RelayServer = host + ":" + port;
			Terraria.Net.NetworkConfig.RelayPort = port;
			string error;
			room = Terraria.Net.RelayLink.Register(out error);
			if (room == null)
			{
				Skip(host + ":" + port + " did not answer - start the master server with a relay to run this check");
			}
			// A player joins in the background: Join blocks until the relay says "WAIT", which is
			// immediate, and the socket then waits for the host.
			System.Net.Sockets.TcpClient joined = null;
			string joinError = null;
			System.Threading.Thread thread = new System.Threading.Thread(delegate()
			{
				joined = Terraria.Net.RelayLink.Join(host, port, room, out joinError);
			});
			thread.IsBackground = true;
			thread.Start();
			if (!thread.Join(5000))
			{
				return Fail("joining a room on the relay hung for 5 s");
			}
			if (joined == null)
			{
				return Fail("joining a room on the relay failed: " + joinError);
			}
			// Poll until the host side sees the ticket.
			string ticket = null;
			for (int i = 0; i < 20 && ticket == null; i++)
			{
				System.Collections.Generic.List<string> list = Terraria.Net.RelayLink.Poll(host, port, Terraria.Net.RelayLink.NewToken(), out error);
				if (list.Count != 0)
				{
					return Fail("a wrong token was handed a ticket");
				}
				list = Terraria.Net.RelayLink.Poll(host, port, TokenForLiveCheck(), out error);
				if (list.Count > 0)
				{
					ticket = list[0];
				}
				else
				{
					System.Threading.Thread.Sleep(150);
				}
			}
			if (ticket == null)
			{
				return Fail("the relay never handed out the ticket for a waiting player");
			}
			System.Net.Sockets.TcpClient accepted = Terraria.Net.RelayLink.AcceptRaw(host, port, ticket);
			if (accepted == null)
			{
				return Fail("the relay refused the ticket it had just issued");
			}
			try
			{
				// Both sockets are now a pipe between "host" and "player" in this process.
				byte[] array = Encoding.UTF8.GetBytes("ping");
				accepted.GetStream().Write(array, 0, array.Length);
				accepted.GetStream().Flush();
				byte[] array2 = new byte[4];
				int num = 0;
				while (num < array2.Length)
				{
					int num2 = joined.GetStream().Read(array2, num, array2.Length - num);
					if (num2 <= 0)
					{
						break;
					}
					num += num2;
				}
				if (num != 4 || Encoding.UTF8.GetString(array2) != "ping")
				{
					return Fail("bytes did not travel through the live relay");
				}
			}
			finally
			{
				try
				{
					accepted.Close();
				}
				catch
				{
				}
				try
				{
					joined.Close();
				}
				catch
				{
				}
			}
			Console.WriteLine("    relay " + host + ":" + port + ": room opened, player paired, bytes passed");
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("relay live check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Net.RelayLink.Stop();
			Terraria.Net.NetworkConfig.AllowRelay = savedAllow;
			Terraria.Net.NetworkConfig.RelayServer = savedServer;
			Terraria.Net.NetworkConfig.RelayPort = savedPort;
		}
	}

	/// <summary>The token Register used, so the live check can poll its own room.</summary>
	private static string TokenForLiveCheck()
	{
		return Terraria.Net.RelayLink.CurrentToken;
	}

	private static bool CheckWhitelist()
	{
		string savedPath = null;
		try
		{
			// The list lives next to the real settings; the test writes a temporary file and
			// restores it, so a run never leaves a whitelist behind.
			string path = Terraria.Net.Whitelist.FilePath;
			bool existed = File.Exists(path);
			if (existed)
			{
				savedPath = File.ReadAllText(path);
			}
			bool savedEnabled = Terraria.Net.NetworkConfig.Whitelist;
			bool savedWhitelistEnabled = Terraria.Net.Whitelist.Enabled;
			try
			{
				File.WriteAllText(path, "# comment\r\nAlice\r\n10.0.0.7\r\n\r\n  Bob  \r\n", Encoding.UTF8);
				Terraria.Net.Whitelist.Reload();
				Terraria.Net.Whitelist.Enabled = true;

				if (Terraria.Net.Whitelist.Count != 3)
				{
					return Fail("comments and blank lines were counted as entries (" + Terraria.Net.Whitelist.Count + ")");
				}
				// Whole-string, case-insensitive: "Bob" must match the trimmed entry, "Bobby" must not.
				if (!Terraria.Net.Whitelist.IsAllowed("Alice", "1.2.3.4")
					|| !Terraria.Net.Whitelist.IsAllowed("bob", "1.2.3.4")
					|| !Terraria.Net.Whitelist.IsAllowed("Stranger", "10.0.0.7"))
				{
					return Fail("a listed player or address was refused");
				}
				if (Terraria.Net.Whitelist.IsAllowed("Bobby", "1.2.3.4")
					|| Terraria.Net.Whitelist.IsAllowed("Alice2", "1.2.3.4")
					|| Terraria.Net.Whitelist.IsAllowed("", "10.0.0.70"))
				{
					return Fail("a player who is not on the list got in (substring match?)");
				}
				// Off means "everyone", which is the whole point of a switch.
				Terraria.Net.Whitelist.Enabled = false;
				if (!Terraria.Net.Whitelist.IsAllowed("Stranger", "1.2.3.4"))
				{
					return Fail("a disabled whitelist still refused somebody");
				}
				// Enabled and empty: nobody gets in, because that is what a whitelist means.
				File.WriteAllText(path, "# only a comment\r\n", Encoding.UTF8);
				Terraria.Net.Whitelist.Reload();
				Terraria.Net.Whitelist.Enabled = true;
				if (Terraria.Net.Whitelist.IsAllowed("Alice", "1.2.3.4"))
				{
					return Fail("an empty whitelist let somebody in (it must refuse everybody)");
				}
				// Add/remove round-trip through the file, and duplicates are not created.
				if (!Terraria.Net.Whitelist.Add("Carol") || Terraria.Net.Whitelist.Add("Carol"))
				{
					return Fail("adding an entry did not report 'new, then already there'");
				}
				Terraria.Net.Whitelist.Reload();
				if (!Terraria.Net.Whitelist.IsAllowed("Carol", null))
				{
					return Fail("an added entry was not saved to the file");
				}
				if (!Terraria.Net.Whitelist.Remove("Carol") || Terraria.Net.Whitelist.Remove("Carol"))
				{
					return Fail("removing an entry did not report 'removed, then not found'");
				}
				Terraria.Net.Whitelist.Reload();
				if (Terraria.Net.Whitelist.IsAllowed("Carol", null))
				{
					return Fail("a removed entry still lets the player in");
				}
				// The saved file is self-documenting, so a host editing it by hand knows the rules.
				string text = File.ReadAllText(path);
				if (!text.Contains("#") || !text.Contains("Whitelist=True"))
				{
					return Fail("the written file does not explain itself: " + text.Replace("\r", " ").Replace("\n", " "));
				}
				return true;
			}
			finally
			{
				Terraria.Net.Whitelist.Enabled = savedWhitelistEnabled;
				Terraria.Net.NetworkConfig.Whitelist = savedEnabled;
				if (savedPath != null)
				{
					File.WriteAllText(path, savedPath, Encoding.UTF8);
				}
				else if (File.Exists(path))
				{
					File.Delete(path);
				}
				Terraria.Net.Whitelist.Reload();
			}
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("whitelist check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	/// <summary>
	/// A real game session over the relay: host session, client session, handshake, packet.
	///
	/// The relay checks next to this one prove the protocol and that bytes flow. They do not prove
	/// that a <em>game</em> works through it, and that is what decides whether a host behind CGNAT
	/// can actually be played with: the client dials the relay, names a room, and the handshake
	/// (version, mod signature, password) has to run end to end over the pipe.
	///
	/// Driven by the stub relay, so it runs without a master server; `relay live` covers a real
	/// one when it is running.
	/// </summary>
	private static bool CheckRelaySession()
	{
		int savedPort = NetworkConfig.Port;
		int savedDiscovery = NetworkConfig.DiscoveryPort;
		int savedMax = NetworkConfig.MaxPlayers;
		bool savedTcp = NetworkConfig.UseTcpTransport;
		string savedPassword = NetworkConfig.Password;
		bool savedAllow = NetworkConfig.AllowRelay;
		string savedServer = NetworkConfig.RelayServer;
		int savedRelayPort = NetworkConfig.RelayPort;
		string savedRoom = NetworkConfig.RelayRoom;
		NetworkSession hostSession = null;
		NetworkSession clientSession = null;
		StubRelay relay = null;
		try
		{
			Microsoft.Xna.Framework.GamerServices.GamerServicesDispatcher.Initialize(null);
			if (Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers.Count < 2)
			{
				return Fail("gamer services did not provide two gamertags");
			}
			relay = new StubRelay();
			NetworkConfig.UseTcpTransport = true;
			NetworkConfig.Password = string.Empty;
			NetworkConfig.MaxPlayers = 7;
			NetworkConfig.Port = 0;
			NetworkConfig.DiscoveryPort = 0;
			// Host through the relay: the client is never given a direct address.
			NetworkConfig.AllowRelay = true;
			NetworkConfig.RelayServer = "127.0.0.1:" + relay.Port.ToString();
			NetworkConfig.RelayPort = relay.Port;
			NetworkConfig.RelayRoom = string.Empty;

			NetworkSessionProperties properties = new NetworkSessionProperties();
			hostSession = new NetworkSession(properties, NetworkSessionType.PlayerMatch, 8, 0, 1,
				new List<Microsoft.Xna.Framework.GamerServices.SignedInGamer>(1) { Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers[0] });
			hostSession.StartHostPeer();
			string room = NetworkConfig.RelayRoom;
			if (string.IsNullOrEmpty(room))
			{
				return Fail("hosting with AllowRelay did not open a relay room");
			}
			if (!relay.HasHost)
			{
				return Fail("the relay never received the HOST command");
			}

			// The client gets the relay endpoint and the room id, exactly as the master's listing
			// gives them: it has no address of the host at all.
			AvailableNetworkSession available = new AvailableNetworkSession(0, room, 0, 7, properties, new QualityOfService());
			available.SessionType = NetworkSessionType.SystemLink;
			available.EndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, relay.Port);
			available.RelayRoom = room;
			clientSession = new NetworkSession(properties, NetworkSessionType.PlayerMatch, 8, 0, 1,
				new List<Microsoft.Xna.Framework.GamerServices.SignedInGamer>(1) { Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers[1] });
			clientSession.StartClientPeer(available);

			// The host's relay pump polls every 750 ms, so joining takes a moment by design.
			if (!PumpUntil(delegate
			{
				return hostSession.RemoteGamers.Count >= 1 && clientSession.RemoteGamers.Count >= 1;
			}, 15000, hostSession, clientSession))
			{
				return Fail("the client did not join through the relay: host sees " + hostSession.RemoteGamers.Count
					+ " remote gamer(s), client sees " + clientSession.RemoteGamers.Count);
			}
			string seen = string.Empty;
			foreach (Microsoft.Xna.Framework.Net.NetworkGamer gamer in hostSession.RemoteGamers)
			{
				seen += gamer.Gamertag + ";";
			}
			if (!seen.Contains("Stub Gamer (1)"))
			{
				return Fail("the handshake over the relay lost the gamertag: " + seen);
			}

			// A game packet through the same pipe: the session is not just the handshake.
			byte[] payload = new byte[5] { 0x11, 0x22, 0x33, 0x44, 0x55 };
			clientSession.LocalGamers[0].SendData(payload, Microsoft.Xna.Framework.Net.SendDataOptions.ReliableInOrder);
			if (!PumpUntil(delegate
			{
				return hostSession.LocalGamers[0].IsDataAvailable;
			}, 5000, hostSession, clientSession))
			{
				return Fail("a packet did not arrive at the host through the relay");
			}
			byte[] received = new byte[64];
			Microsoft.Xna.Framework.Net.NetworkGamer sender;
			int count = hostSession.LocalGamers[0].ReceiveData(received, out sender);
			if (count != payload.Length)
			{
				return Fail("the packet through the relay has " + count + " byte(s), expected " + payload.Length);
			}
			for (int i = 0; i < payload.Length; i++)
			{
				if (received[i] != payload[i])
				{
					return Fail("the packet through the relay differs at byte " + i);
				}
			}
			Console.WriteLine("    relay session: room " + room + ", client joined, " + count + " byte(s) through the pipe");
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("relay session check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			try
			{
				clientSession?.Dispose();
			}
			catch
			{
			}
			try
			{
				hostSession?.Dispose();
			}
			catch
			{
			}
			Terraria.Net.RelayLink.Stop();
			relay?.Stop();
			NetworkConfig.Port = savedPort;
			NetworkConfig.DiscoveryPort = savedDiscovery;
			NetworkConfig.MaxPlayers = savedMax;
			NetworkConfig.UseTcpTransport = savedTcp;
			NetworkConfig.Password = savedPassword;
			NetworkConfig.AllowRelay = savedAllow;
			NetworkConfig.RelayServer = savedServer;
			NetworkConfig.RelayPort = savedRelayPort;
			NetworkConfig.RelayRoom = savedRoom;
		}
	}

	private static bool CheckMenuHitTable()
	{
		try
		{
			// The bug this locks in: the hit-rect table was cleared only for entries whose text was
			// null, so a screen that draws its own layout kept the previous menu's rectangles.
			// ClickOutsideMenuEntries then stayed true, and every click outside those stale
			// rectangles was swallowed instead of becoming A - the mode screen answered clicks on
			// its labels but not on its checkboxes, and never on Start Game.
			UI ui = new UI();
			ui.ClearMenuHits();
			if (ui.ClickOutsideMenuEntries)
			{
				return Fail("an empty hit table still claims the click landed outside the entries");
			}
			// An entry somewhere else on screen, with the pointer at the top left: a click must be
			// swallowed, because a menu with entries is open and the pointer is not on any of them.
			ui.AddMenuHit(0, new Rectangle(500, 500, 40, 20));
			if (!ui.ClickOutsideMenuEntries)
			{
				return Fail("a click away from the only entry was not recognised as outside it");
			}
			// A new frame forgets it, which is the actual fix.
			ui.ClearMenuHits();
			if (ui.ClickOutsideMenuEntries)
			{
				return Fail("a cleared table kept the previous screen's entry");
			}
			// Out-of-range indices are ignored, not thrown: the table is a fixed 14 slots.
			ui.AddMenuHit(99, new Rectangle(0, 0, 10, 10));
			if (ui.ClickOutsideMenuEntries)
			{
				return Fail("an out-of-range entry index was recorded");
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("menu hit table check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool CheckMapScreenMouse()
	{
		try
		{
			// Five team dots, and three separate places that have to agree on their order: the
			// drawing loop, the console cursor arithmetic and the mouse. A click on dot i must
			// select team i, and the cursor must land on the dot the player clicked.
			for (int i = 0; i < 5; i++)
			{
				int x;
				int y;
				Terraria.MapScreenMouse.CursorForDot(i, out x, out y);
				if (x < 1 || x > 3 || y < 0 || y > 1)
				{
					return Fail("dot " + i + " points at an impossible cursor position " + x + "," + y);
				}
				int num = Terraria.MapScreenMouse.TeamForCursor(x, y);
				if (num != i)
				{
					return Fail("dot " + i + " would select team " + num + " (cursor " + x + "," + y + ")");
				}
			}
			// Every dot gets its own cursor cell: two dots on one cell would make the highlight
			// jump to the wrong picture and the click act on the other.
			for (int j = 0; j < 5; j++)
			{
				int x2;
				int y2;
				Terraria.MapScreenMouse.CursorForDot(j, out x2, out y2);
				for (int k = j + 1; k < 5; k++)
				{
					int x3;
					int y3;
					Terraria.MapScreenMouse.CursorForDot(k, out x3, out y3);
					if (x2 == x3 && y2 == y3)
					{
						return Fail("dots " + j + " and " + k + " share the cursor cell " + x2 + "," + y2);
					}
				}
			}
			// An out-of-range dot clamps instead of throwing: the click path reads it from a
			// rectangle array and a stale record could produce anything.
			int x4;
			int y4;
			Terraria.MapScreenMouse.CursorForDot(99, out x4, out y4);
			if (x4 != 1 || y4 != 0)
			{
				return Fail("an out-of-range team dot did not clamp to the first cell");
			}
			Terraria.MapScreenMouse.ResetForTests();
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("map screen mouse check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool CheckModalInputLatch()
	{
		try
		{
			MouseState idleMouse = new MouseState();
			KeyboardState idleKeys = new KeyboardState();
			if (Terraria.Input.KeyboardMouseInput.ModalInputDown(idleKeys, idleMouse, false))
			{
				return Fail("an idle device reports a held confirm key");
			}
			// The keyboard closes on Enter and the screen behind it starts on Enter. Until the
			// key is released, the latch has to stay on, or character creation reopens its own
			// name prompt from the same keystroke.
			if (!Terraria.Input.KeyboardMouseInput.ModalInputDown(new KeyboardState(Keys.Enter), idleMouse, false))
			{
				return Fail("a held Enter did not keep the modal latch engaged");
			}
			if (!Terraria.Input.KeyboardMouseInput.ModalInputDown(new KeyboardState(Keys.Escape), idleMouse, false))
			{
				return Fail("a held Escape did not keep the modal latch engaged");
			}
			// Accepting with the on-screen OK button leaves the left button down for a moment.
			MouseState heldMouse = new MouseState(0, 0, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
			if (!Terraria.Input.KeyboardMouseInput.ModalInputDown(idleKeys, heldMouse, false))
			{
				return Fail("a held left mouse button did not keep the modal latch engaged");
			}
			if (!Terraria.Input.KeyboardMouseInput.ModalInputDown(idleKeys, idleMouse, true))
			{
				return Fail("a held pad button did not keep the modal latch engaged");
			}
			// A key that is not a modal control (W) must not freeze game input.
			if (Terraria.Input.KeyboardMouseInput.ModalInputDown(new KeyboardState(Keys.W), idleMouse, false))
			{
				return Fail("an unrelated key kept the modal latch engaged");
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("modal input latch check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool CheckPointerScaling()
	{
		try
		{
			// Same size: unchanged, and a coordinate on the last pixel stays inside.
			if (Terraria.Input.KeyboardMouseInput.MapAxis(500, 1280, 1280) != 500)
			{
				return Fail("an identity mapping changed the coordinate");
			}
			if (Terraria.Input.KeyboardMouseInput.MapAxis(1280, 1280, 1280) != 1279)
			{
				return Fail("a coordinate one past the edge was not clamped inside");
			}
			// 1080p in a 720p window: 1.5x. The far corner must reach the far corner.
			if (Terraria.Input.KeyboardMouseInput.MapAxis(640, 1280, 1920) != 960)
			{
				return Fail("720p->1080p did not scale: " + Terraria.Input.KeyboardMouseInput.MapAxis(640, 1280, 1920));
			}
			if (Terraria.Input.KeyboardMouseInput.MapAxis(1279, 1280, 1920) != 1918)
			{
				return Fail("the right edge of a 1080p screen was not reachable: "
					+ Terraria.Input.KeyboardMouseInput.MapAxis(1279, 1280, 1920));
			}
			// 540p in a 720p window: 0.75x, i.e. the whole width is reachable.
			if (Terraria.Input.KeyboardMouseInput.MapAxis(1279, 1280, 960) != 959)
			{
				return Fail("the right edge of a 540p screen was not reachable");
			}
			if (Terraria.Input.KeyboardMouseInput.MapAxis(960, 1280, 960) != 720 || Terraria.Input.KeyboardMouseInput.MapAxis(719, 720, 960) != 958)
			{
				return Fail("540p scaling is wrong in one axis: "
					+ Terraria.Input.KeyboardMouseInput.MapAxis(960, 1280, 960) + ", "
					+ Terraria.Input.KeyboardMouseInput.MapAxis(719, 720, 960));
			}
			// Negative and out-of-range input must not produce a negative or huge coordinate:
			// the OS can report either when the window is partly off-screen.
			if (Terraria.Input.KeyboardMouseInput.MapAxis(-50, 1280, 1920) != 0)
			{
				return Fail("a negative coordinate was not clamped to 0");
			}
			if (Terraria.Input.KeyboardMouseInput.MapAxis(5000, 1280, 1920) != 1919)
			{
				return Fail("an out-of-range coordinate was not clamped to the last pixel");
			}
			// No viewport yet: the value is passed through, which is what the headless tests and
			// the first frames rely on.
			if (Terraria.Input.KeyboardMouseInput.MapAxis(321, 0, 0) != 321)
			{
				return Fail("an unset viewport did not pass the coordinate through");
			}
			// A 4K buffer must not overflow the intermediate multiply.
			if (Terraria.Input.KeyboardMouseInput.MapAxis(3840, 3840, 960) != 959)
			{
				return Fail("4K downscaling overflowed");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("pointer scaling check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Raw mods -----------------------------------------------------------

	/// <summary>
	/// A mod folder with no assembly: INI definitions plus sprites, read straight into the same
	/// registries a compiled mod uses.
	///
	/// The point of the check is the part that is easy to get wrong and impossible to see: a raw
	/// mod has to be indistinguishable from a compiled one afterwards - same ids, same recipes,
	/// same overrides, same signature rules - because the network and the save file cannot tell
	/// them apart.
	/// </summary>
	private static bool CheckRawMod()
	{
		string root = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-rawmod");
		try
		{
			if (System.IO.Directory.Exists(root))
			{
				System.IO.Directory.Delete(root, recursive: true);
			}
			string pack = System.IO.Path.Combine(root, "Raw Pack");
			string items = System.IO.Path.Combine(pack, "Item");
			System.IO.Directory.CreateDirectory(items);
			System.IO.File.WriteAllText(System.IO.Path.Combine(pack, "mod.ini"),
				"[Mod]\r\nName=Raw Pack\r\nVersion=1.2\r\nAuthor=Tester\r\n");
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "Copper Bulgeon.ini"),
				"type=-1\r\ndamage=12\r\nuseTime=20\r\nisMaterial=true\r\n"
				+ "stack=25\r\ncolor=10,20,30,40\r\n"
				+ "[Items]\r\nCopper Bar=6\r\n[Tiles]\r\nAnvil=true\r\n[Amount]\r\n2\r\n");
			WriteTinyPng(System.IO.Path.Combine(items, "Copper Bulgeon.png"));
			// An existing item: this is an override, not a new item, and the two paths must not
			// be confused - a pack that changes a sword must not invent a second one.
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "Copper Bar.ini"), "value=1234\r\n");
			// The other recipe spelling, which real packs use: one line per list, count first.
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "Copper Ore.ini"),
				"[Recipe]\r\nItems=2 Stone Block, 1 Wood\r\nTiles=Work Bench\r\nAmount=3\r\n");
			// A file with no sprite and no matching item: reported, and it must not stop the rest.
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "Imaginary Thing.ini"), "damage=3\r\n");
			// A pack-wide table that lives in Item/: it must be reported as "not an item", not as
			// "no such item in this build".
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "DropTables.ini"), "Slime=Gel,1\r\n");
			// An ammo field written by name, which the item table resolves.
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "Copper Bow.ini"),
				"useAmmoName=Wooden Arrow\r\n");
			// Content this build cannot load. It must be counted and reported, not silently
			// skipped: a pack with 146 NPCs that loads "fine" and spawns nothing is the report
			// this prevents.
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(pack, "NPC"));
			System.IO.File.WriteAllText(System.IO.Path.Combine(pack, "NPC", "Slime.ini"), "damage=1\r\n");
			System.IO.File.WriteAllText(System.IO.Path.Combine(pack, "NPC", "Eye.ini"), "damage=1\r\n");
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(pack, "Tiles"));
			System.IO.File.WriteAllText(System.IO.Path.Combine(pack, "Tiles", "Ore.ini"), "damage=1\r\n");

			if (!Terraria.Mods.RawMod.LooksLikeRawMod(pack))
			{
				return Fail("a folder of INI definitions was not recognised as a raw mod");
			}
			if (Terraria.Mods.RawMod.LooksLikeRawMod(System.IO.Path.Combine(pack, "Item")))
			{
				return Fail("the Item folder itself was taken for a mod");
			}
			// A compiled mod may ship Item/*.ini for its own code to read; those must not be
			// applied a second time by the raw path.
			System.IO.File.WriteAllText(System.IO.Path.Combine(pack, "Fake.dll"), "not an assembly");
			if (Terraria.Mods.RawMod.LooksLikeRawMod(pack))
			{
				return Fail("a folder with an assembly was treated as a raw mod");
			}
			System.IO.File.Delete(System.IO.Path.Combine(pack, "Fake.dll"));

			Terraria.Mods.ModItems.Clear();
			Terraria.Mods.ModRecipes.Clear();
			Terraria.Mods.ItemOverrides.Clear();
			int overridesBefore = Terraria.Mods.ItemOverrides.Count;

			Terraria.Mods.RawMod rawMod = Terraria.Mods.RawMod.Load(pack);
			if (rawMod.Name != "Raw Pack")
			{
				return Fail("the manifest name was ignored: '" + rawMod.Name + "'");
			}
			if (rawMod.Version != "1.2")
			{
				return Fail("the manifest version was ignored: '" + rawMod.Version + "'");
			}
			if (rawMod.NewItems != 1)
			{
				return Fail("expected 1 new item, got " + rawMod.NewItems);
			}
			if (rawMod.Overrides != 2)
			{
				return Fail("expected 2 overrides (Copper Bar and Copper Bow), got " + rawMod.Overrides);
			}
			if (rawMod.Recipes != 2)
			{
				return Fail("expected 2 recipes (both spellings), got " + rawMod.Recipes);
			}
			if (rawMod.Problems.Count == 0)
			{
				return Fail("a file with no sprite and no item was not reported");
			}
			// The unsupported content is counted, not ignored: two NPC files and one tile file.
			if (rawMod.IgnoredFileCount != 3)
			{
				return Fail("expected 3 unusable file(s) to be counted, got " + rawMod.IgnoredFileCount);
			}
			bool sawNpc = false;
			bool sawTiles = false;
			for (int i = 0; i < rawMod.Ignored.Count; i++)
			{
				if (rawMod.Ignored[i].Kind == "NPC" && rawMod.Ignored[i].Files == 2)
				{
					sawNpc = true;
				}
				if (rawMod.Ignored[i].Kind == "Tiles" && rawMod.Ignored[i].Files == 1)
				{
					sawTiles = true;
				}
			}
			if (!sawNpc || !sawTiles)
			{
				return Fail("the unusable folders were not reported by name");
			}
			// And it is said out loud, so it reaches the log the player is told to read.
			bool saidSkipped = false;
			for (int i = 0; i < rawMod.Problems.Count; i++)
			{
				if (rawMod.Problems[i].IndexOf("NPC", StringComparison.Ordinal) >= 0)
				{
					saidSkipped = true;
				}
			}
			if (!saidSkipped)
			{
				return Fail("the NPC folder was counted but never mentioned in the report");
			}
			// The service file is reported as "not an item", not as "no such item".
			bool saidService = false;
			for (int i = 0; i < rawMod.Problems.Count; i++)
			{
				if (rawMod.Problems[i].IndexOf("drop table", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					saidService = true;
				}
			}
			if (!saidService)
			{
				return Fail("DropTables.ini was not reported as a pack table rather than an item");
			}
			// And the ammo field written by name resolves through the item table.
			int bowId = Terraria.ItemLookup.Find("Copper Bow");
			if (bowId <= 0)
			{
				return Fail("the test cannot find Copper Bow");
			}
			Item bow = default(Item);
			bow.SetDefaults(bowId);
			int arrowId = Terraria.ItemLookup.Find("Wooden Arrow");
			if (arrowId <= 0)
			{
				return Fail("the test cannot find Wooden Arrow");
			}
			if (bow.UseAmmo != arrowId)
			{
				return Fail("'useAmmoName=Wooden Arrow' did not reach UseAmmo (got " + bow.UseAmmo
					+ ", expected " + arrowId + ")");
			}
			// Ids exist only after Finalize, so that comes first - All() is keyed by id.
			Terraria.Mods.ModItems.Finalize(null);
			Terraria.Mods.ModItem definition = null;
			System.Collections.Generic.List<Terraria.Mods.ModItem> all = Terraria.Mods.ModItems.All();
			for (int i = 0; i < all.Count; i++)
			{
				if (all[i].Name == "Copper Bulgeon")
				{
					definition = all[i];
					break;
				}
			}
			if (definition == null)
			{
				return Fail("the raw item was not registered");
			}
			string field;
			if (!definition.Fields.TryGetValue("damage", out field) || field != "12")
			{
				return Fail("the raw item lost its damage field");
			}
			if (definition.Type < Terraria.Mods.ModItems.IdBase)
			{
				return Fail("the raw item got no id");
			}
			// The tConfig spellings that mean the same as a field this build has: `stack` is
			// MaxStack and `color` is the sprite tint. Both used to be dropped - `stack` on 119
			// items in one real pack - so they are checked through the live item, not the table.
			Item built = default(Item);
			built.SetDefaults(definition.Type);
			if (built.MaxStack != 25)
			{
				return Fail("'stack=25' did not reach MaxStack (got " + built.MaxStack + ")");
			}
			if (built.Colour.R != 10 || built.Colour.G != 20 || built.Colour.B != 30 || built.Colour.A != 40)
			{
				return Fail("'color=10,20,30,40' did not reach the item tint (got "
					+ built.Colour.R + "," + built.Colour.G + "," + built.Colour.B + "," + built.Colour.A + ")");
			}
			// The override reaches the live item through the ordinary path.
			int barId = Terraria.ItemLookup.Find("Copper Bar");
			if (barId <= 0)
			{
				return Fail("the test cannot find Copper Bar");
			}
			Item bar = default(Item);
			bar.SetDefaults(barId);
			if (bar.Value != 1234)
			{
				return Fail("the raw override did not reach the item: value is " + bar.Value);
			}
			if (Terraria.Mods.ItemOverrides.Count != overridesBefore + 2)
			{
				return Fail("expected 2 overrides registered (Copper Bar and Copper Bow), got "
					+ (Terraria.Mods.ItemOverrides.Count - overridesBefore));
			}
			// The recipe is pending, and both spellings reached the registry.
			if (Terraria.Mods.ModRecipes.Count != 2)
			{
				return Fail("expected 2 pending recipes, got " + Terraria.Mods.ModRecipes.Count);
			}
			// End to end, the way the game does it: build the recipes, then ask a player whether
			// they are discovered. This is the exact path that threw from BitArray.Get the moment
			// the crafting tab was opened with a mod loaded - the recipe names a mod item, and the
			// "seen this item" table used to stop at the vanilla item count.
			System.Collections.Generic.List<string> recipeProblems;
			Terraria.Mods.ModRecipes.Apply(out recipeProblems);
			Terraria.Player recipePlayer = new Terraria.Player();
			recipePlayer.UpdateRecipes();
			bool sawModRecipe = false;
			for (int i = 0; i < Terraria.Recipe.NumRecipes; i++)
			{
				Terraria.Recipe recipe = Terraria.Main.ActiveRecipe[i];
				if (recipe != null && recipe.CraftedItem.Type >= Terraria.Mods.ModItems.IdBase)
				{
					sawModRecipe = true;
					break;
				}
			}
			if (!sawModRecipe)
			{
				return Fail("no mod recipe reached the game's recipe table");
			}
			// --- signature ----------------------------------------------------
			string first = rawMod.Signature;
			if (string.IsNullOrEmpty(first))
			{
				return Fail("a raw mod has no content signature");
			}
			// Toggling the mod must not change what it contains: the host and the client would
			// disagree after one of them disabled it.
			System.IO.File.WriteAllText(System.IO.Path.Combine(pack, "disabled.txt"), "x");
			if (Terraria.Mods.RawMod.ContentHash(pack) != first)
			{
				return Fail("disabled.txt changed the content signature");
			}
			System.IO.File.Delete(System.IO.Path.Combine(pack, "disabled.txt"));
			// Editing one number must change it: the folder name is not the content.
			System.IO.File.WriteAllText(System.IO.Path.Combine(items, "Copper Bar.ini"), "value=1235\r\n");
			if (Terraria.Mods.RawMod.ContentHash(pack) == first)
			{
				return Fail("editing an item file did not change the content signature");
			}
			// --- the loader's decision -----------------------------------------
			// RawMod.Load is only half of it: the loader has to *choose* this folder, and it has to
			// do it in a directory where a compiled mod and a stray folder also live.
			string modsRoot = System.IO.Path.Combine(root, "Mods");
			System.IO.Directory.CreateDirectory(modsRoot);
			CopyTree(pack, System.IO.Path.Combine(modsRoot, "Raw Pack"));
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(modsRoot, "Empty Folder"));
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(modsRoot, "Disabled Pack", "Item"));
			System.IO.File.WriteAllText(System.IO.Path.Combine(modsRoot, "Disabled Pack", "Item", "Thing.ini"), "damage=1\r\n");
			System.IO.File.WriteAllText(System.IO.Path.Combine(modsRoot, "Disabled Pack", "disabled.txt"), "x");
			Terraria.Mods.ModLoader.Unload();
			Terraria.Mods.ModItems.Clear();
			Terraria.Mods.ModRecipes.Clear();
			Terraria.Mods.ItemOverrides.Clear();
			int num = Terraria.Mods.ModLoader.LoadFolders(Terraria.Mods.ModLoader.ApplyLoadOrder(
				System.IO.Directory.GetDirectories(modsRoot), modsRoot));
			if (num != 1)
			{
				return Fail("the loader took " + num + " folder(s) for mods instead of 1");
			}
			if (Terraria.Mods.ModLoader.RawMods.Count != 1)
			{
				return Fail("the raw mod was not loaded from the Mods folder");
			}
			if (Terraria.Mods.ModLoader.RawMods[0].Name != "Raw Pack")
			{
				return Fail("the loader picked the wrong folder: '" + Terraria.Mods.ModLoader.RawMods[0].Name + "'");
			}
			if (Terraria.Mods.ModLoader.ModCount != 1)
			{
				return Fail("a raw mod is not counted as a mod: " + Terraria.Mods.ModLoader.ModCount);
			}
			// And the host can find its files: "the host has this mod" and "the host can send this
			// mod" have to be the same question, or a client is offered a download that fails.
			string folder = Terraria.Net.TcpPeer.FindModFolder("Raw Pack");
			if (folder == null || !System.IO.File.Exists(System.IO.Path.Combine(folder, "mod.ini")))
			{
				return Fail("the host could not find a loaded raw mod's folder");
			}
			if (Terraria.Net.TcpPeer.FindModFolder("Not A Mod") != null)
			{
				return Fail("the host found a folder for a mod it does not have");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("raw mod check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ModItems.Clear();
			Terraria.Mods.ModRecipes.Clear();
			Terraria.Mods.ItemOverrides.Clear();
			try
			{
				if (System.IO.Directory.Exists(root))
				{
					System.IO.Directory.Delete(root, recursive: true);
				}
			}
			catch
			{
			}
			// Put the loader back the way the rest of the run expects it: this check unloaded the
			// real set to scan a temporary folder, and leaving it unloaded would make every later
			// check that reads the signature see "vanilla".
			try
			{
				Terraria.Mods.ModLoader.Initialize();
			}
			catch
			{
			}
		}
	}

	/// <summary>Copies a folder tree, so a test can hand the loader its own copy of a pack.</summary>
	private static void CopyTree(string source, string destination)
	{
		System.IO.Directory.CreateDirectory(destination);
		string[] files = System.IO.Directory.GetFiles(source);
		for (int i = 0; i < files.Length; i++)
		{
			System.IO.File.Copy(files[i], System.IO.Path.Combine(destination, System.IO.Path.GetFileName(files[i])), overwrite: true);
		}
		string[] directories = System.IO.Directory.GetDirectories(source);
		for (int j = 0; j < directories.Length; j++)
		{
			CopyTree(directories[j], System.IO.Path.Combine(destination, System.IO.Path.GetFileName(directories[j])));
		}
	}

	// --- Mod download -------------------------------------------------------

	/// <summary>
	/// Fetching a mod's files from a host, which is the part of "join a server with the same
	/// mods" that has to be right for reasons other than convenience: the bytes are executable,
	/// the paths come from another machine, and the result is written into the player's own
	/// folders.
	///
	/// Checked here rather than through the game's UI: the transfer is written over two streams,
	/// so a loopback socket pair exercises exactly the bytes a real download moves.
	/// </summary>
	private static bool CheckModDownload()
	{
		string root = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-download");
		string savedRoot = Terraria.Mods.ModFiles.ModsRootOverride;
		try
		{
			if (System.IO.Directory.Exists(root))
			{
				System.IO.Directory.Delete(root, recursive: true);
			}
			System.IO.Directory.CreateDirectory(root);
			// --- framing -------------------------------------------------------
			Terraria.Mods.ModFileFrame frame;
			if (!Terraria.Mods.ModTransfer.TryParse(Terraria.Mods.ModTransfer.Request("Cool Mod"), out frame)
				|| frame.Op != Terraria.Mods.ModFileOp.Request || frame.ModName != "Cool Mod")
			{
				return Fail("a request frame did not round-trip");
			}
			string[] manifest = new string[2] { "mod.ini", "Item/Blade.png" };
			if (!Terraria.Mods.ModTransfer.TryParse(
				Terraria.Mods.ModTransfer.Begin("Cool Mod", manifest, 1234L, "abc123"), out frame)
				|| frame.Op != Terraria.Mods.ModFileOp.Begin || frame.FileCount != 2
				|| frame.Files == null || frame.Files[1] != "Item/Blade.png"
				|| frame.TotalBytes != 1234L || frame.Hash != "abc123")
			{
				return Fail("a begin frame did not round-trip");
			}
			byte[] array = new byte[6] { 1, 2, 3, 4, 5, 6 };
			if (!Terraria.Mods.ModTransfer.TryParse(
				Terraria.Mods.ModTransfer.Chunk("Item/Blade.png", 7L, array, 1, 4), out frame)
				|| frame.Op != Terraria.Mods.ModFileOp.Chunk || frame.Path != "Item/Blade.png"
				|| frame.Offset != 7L || frame.DataCount != 4)
			{
				return Fail("a chunk frame did not round-trip");
			}
			// The payload is the whole frame, so the slice has to point into it - an off-by-one
			// here writes the length prefix into the file.
			if (frame.Data[frame.DataOffset] != 2 || frame.Data[frame.DataOffset + 3] != 5)
			{
				return Fail("a chunk's data slice points at the wrong bytes");
			}
			if (!Terraria.Mods.ModTransfer.TryParse(Terraria.Mods.ModTransfer.End("Cool Mod", "abc123"), out frame)
				|| frame.Op != Terraria.Mods.ModFileOp.End || frame.Hash != "abc123")
			{
				return Fail("an end frame did not round-trip");
			}
			if (!Terraria.Mods.ModTransfer.TryParse(Terraria.Mods.ModTransfer.Refused("Cool Mod", "too large"), out frame)
				|| frame.Op != Terraria.Mods.ModFileOp.Refused || frame.Reason != "too large")
			{
				return Fail("a refusal frame did not round-trip");
			}
			// --- hostile frames -------------------------------------------------
			byte[] goodBegin = Terraria.Mods.ModTransfer.Begin("X", new string[1] { "a.txt" }, 1L, "h");
			byte[] trailing = new byte[goodBegin.Length + 1];
			Buffer.BlockCopy(goodBegin, 0, trailing, 0, goodBegin.Length);
			if (Terraria.Mods.ModTransfer.TryParse(trailing, out frame))
			{
				return Fail("a frame with trailing bytes was accepted");
			}
			// A manifest entry that escapes the folder: refused before any file is written.
			if (Terraria.Mods.ModTransfer.TryParse(
				Terraria.Mods.ModTransfer.Begin("X", new string[1] { "../../evil.txt" }, 1L, "h"), out frame))
			{
				return Fail("a begin frame with an escaping path was accepted");
			}
			// A count that claims more data than the frame holds.
			byte[] wholeChunk = Terraria.Mods.ModTransfer.Chunk("a.txt", 0L, array, 0, 6);
			byte[] cutChunk = new byte[wholeChunk.Length - 1];
			Buffer.BlockCopy(wholeChunk, 0, cutChunk, 0, cutChunk.Length);
			if (Terraria.Mods.ModTransfer.TryParse(cutChunk, out frame))
			{
				return Fail("a truncated chunk was accepted");
			}
			if (Terraria.Mods.ModTransfer.TryParse(new byte[1] { 200 }, out frame)
				|| Terraria.Mods.ModTransfer.TryParse(null, out frame))
			{
				return Fail("a frame with an unknown opcode was accepted");
			}
			// --- path and name safety -------------------------------------------
			string[] unsafePaths = new string[6] { "../x", "/etc/passwd", "C:/Windows/x", "a/../b", "a\\b", "a/./b" };
			for (int i = 0; i < unsafePaths.Length; i++)
			{
				if (Terraria.Mods.ModFiles.IsSafeRelativePath(unsafePaths[i]))
				{
					return Fail("path '" + unsafePaths[i] + "' was accepted");
				}
			}
			string[] safePaths = new string[2] { "mod.ini", "Item/Blade.png" };
			for (int j = 0; j < safePaths.Length; j++)
			{
				if (!Terraria.Mods.ModFiles.IsSafeRelativePath(safePaths[j]))
				{
					return Fail("path '" + safePaths[j] + "' was refused");
				}
			}
			if (Terraria.Mods.ModFiles.SafeFolderName("..").Length != 0
				|| Terraria.Mods.ModFiles.SafeFolderName("a/b").Length != 0
				|| Terraria.Mods.ModFiles.SafeFolderName("").Length != 0)
			{
				return Fail("an unsafe mod name was accepted");
			}
			if (Terraria.Mods.ModFiles.SafeFolderName("Omnirs Weapons") != "Omnirs Weapons")
			{
				return Fail("a normal mod name was refused");
			}
			// --- a real transfer over a socket pair -----------------------------
			Terraria.Mods.ModFiles.ModsRootOverride = System.IO.Path.Combine(root, "Mods");
			string source = System.IO.Path.Combine(root, "host", "Cool Mod");
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(source, "Item"));
			System.IO.File.WriteAllText(System.IO.Path.Combine(source, "mod.ini"), "[Mod]\r\nName=Cool Mod\r\nVersion=1.0\r\n");
			WriteTinyPng(System.IO.Path.Combine(source, "Item", "Blade.png"));
			// Files that must not travel: a switch, and build output.
			System.IO.File.WriteAllText(System.IO.Path.Combine(source, "disabled.txt"), "x");
			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(source, "obj"));
			System.IO.File.WriteAllText(System.IO.Path.Combine(source, "obj", "junk.bin"), "junk");
			string expectedHash = Terraria.Mods.ModFiles.ContentHash(source);
			long expectedSize = Terraria.Mods.ModFiles.TotalSize(source);
			if (expectedSize <= 0L || string.IsNullOrEmpty(expectedHash) || expectedHash == "unreadable")
			{
				return Fail("the source mod has no measurable content");
			}
			Terraria.Mods.ModTransfer.ReceiveResult received = LoopbackDownload(source, "Cool Mod", out string transferError);
			if (transferError != null)
			{
				return Fail("the transfer failed: " + transferError);
			}
			if (received.Received.Count != 1)
			{
				return Fail("expected 1 mod to arrive, got " + received.Received.Count
					+ " (" + (received.Error ?? "no reason") + ")");
			}
			// What landed must be the same content: same files, same hash, no disabled.txt and no
			// build output.
			string incoming = Terraria.Mods.ModFiles.IncomingDirectory("Cool Mod");
			if (Terraria.Mods.ModFiles.ContentHash(incoming) != expectedHash)
			{
				return Fail("the received mod does not hash the same as the source");
			}
			if (System.IO.File.Exists(System.IO.Path.Combine(incoming, "disabled.txt")))
			{
				return Fail("disabled.txt travelled with the mod");
			}
			if (System.IO.Directory.Exists(System.IO.Path.Combine(incoming, "obj")))
			{
				return Fail("build output travelled with the mod");
			}
			// Installing replaces whatever was there, and leaves nothing half-moved.
			string error;
			if (!Terraria.Mods.ModFiles.Install("Cool Mod", out error))
			{
				return Fail("install failed: " + error);
			}
			string installed = System.IO.Path.Combine(Terraria.Mods.ModFiles.ModsRoot, "Cool Mod");
			if (Terraria.Mods.ModFiles.ContentHash(installed) != expectedHash)
			{
				return Fail("the installed mod does not hash the same as the source");
			}
			if (System.IO.Directory.Exists(incoming))
			{
				return Fail("the incoming folder survived the install");
			}
			// --- the host does not have it --------------------------------------
			Terraria.Mods.ModTransfer.ReceiveResult absent = LoopbackDownload(source, "Not A Mod", out string absentError);
			if (absentError != null)
			{
				return Fail("the refusal transfer failed: " + absentError);
			}
			if (absent.Received.Count != 0 || absent.Failed.Count != 1)
			{
				return Fail("a mod the host does not have was not refused");
			}
			if (System.IO.Directory.Exists(Terraria.Mods.ModFiles.IncomingDirectory("Not A Mod")))
			{
				return Fail("a refused mod left a folder behind");
			}
			// --- a tampered transfer --------------------------------------------
			// The host promises one hash and sends other bytes: the client must notice and leave
			// nothing installed. This is the whole reason the hash travels before the files.
			Terraria.Mods.ModTransfer.ReceiveResult tampered = LoopbackTampered(source, "Cool Mod", out string tamperedError);
			if (tamperedError != null)
			{
				return Fail("the tampered transfer threw: " + tamperedError);
			}
			if (tampered.Received.Count != 0)
			{
				return Fail("a mod whose contents did not match its hash was accepted");
			}
			if (System.IO.Directory.Exists(Terraria.Mods.ModFiles.IncomingDirectory("Cool Mod")))
			{
				return Fail("a rejected download was left on disk");
			}
			// --- the list that carries size and hash ----------------------------
			Terraria.Mods.ModEntry entry = new Terraria.Mods.ModEntry();
			entry.Name = "Cool Mod";
			entry.Version = "1.0";
			entry.Size = 4321L;
			entry.Hash = "deadbeef";
			System.Collections.Generic.List<Terraria.Mods.ModEntry> list =
				new System.Collections.Generic.List<Terraria.Mods.ModEntry> { entry };
			string described = Terraria.Mods.ModSet.Describe(list);
			System.Collections.Generic.List<Terraria.Mods.ModEntry> parsed = Terraria.Mods.ModSet.Parse(described);
			if (parsed.Count != 1 || parsed[0].Name != "Cool Mod" || parsed[0].Size != 4321L || parsed[0].Hash != "deadbeef")
			{
				return Fail("the mod list lost its size or hash: '" + described + "'");
			}
			if (!parsed[0].IsDownloadable)
			{
				return Fail("an entry with a size and a hash was not downloadable");
			}
			// An older peer sends two fields: it must still parse, and must not be offered.
			System.Collections.Generic.List<Terraria.Mods.ModEntry> oldStyle = Terraria.Mods.ModSet.Parse("Cool Mod|1.0");
			if (oldStyle.Count != 1 || oldStyle[0].Size != -1L || oldStyle[0].IsDownloadable)
			{
				return Fail("a two-field mod entry did not parse as 'not downloadable'");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod download check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ModFiles.ModsRootOverride = savedRoot;
			try
			{
				if (System.IO.Directory.Exists(root))
				{
					System.IO.Directory.Delete(root, recursive: true);
				}
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Runs the real host and client halves against each other over a loopback socket, and
	/// returns what the client got. <paramref name="servedName"/> is what the host offers.
	/// </summary>
	private static Terraria.Mods.ModTransfer.ReceiveResult LoopbackDownload(string sourceFolder, string requestedName, out string error)
	{
		return Loopback(sourceFolder, requestedName, null, out error);
	}

	/// <summary>The same, with every chunk's bytes flipped: the client must reject the result.</summary>
	private static Terraria.Mods.ModTransfer.ReceiveResult LoopbackTampered(string sourceFolder, string requestedName, out string error)
	{
		return Loopback(sourceFolder, requestedName, delegate(byte[] payload)
		{
			// Flip the last byte of the chunk payload. The frame stays well formed, so only the
			// content hash can notice - which is the check being tested.
			if (payload.Length > 0)
			{
				payload[payload.Length - 1] ^= 0xFF;
			}
			return payload;
		}, out error);
	}

	private static Terraria.Mods.ModTransfer.ReceiveResult Loopback(string sourceFolder, string requestedName,
		Func<byte[], byte[]> tamper, out string error)
	{
		error = null;
		System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
		listener.Start();
		int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
		Exception hostError = null;
		Terraria.Mods.ModTransfer.ServeResult serveResult = null;
		System.Threading.Thread thread = new System.Threading.Thread(delegate()
		{
			try
			{
				using System.Net.Sockets.TcpClient tcpClient = listener.AcceptTcpClient();
				using System.Net.Sockets.NetworkStream stream = tcpClient.GetStream();
				serveResult = Terraria.Mods.ModTransfer.Serve(stream, stream, delegate(string name)
				{
					return string.Equals(name, System.IO.Path.GetFileName(sourceFolder), StringComparison.OrdinalIgnoreCase)
						? sourceFolder : null;
				});
			}
			catch (Exception ex)
			{
				hostError = ex;
			}
		});
		thread.IsBackground = true;
		thread.Start();
		Terraria.Mods.ModTransfer.ReceiveResult receiveResult = null;
		try
		{
			using System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
			client.Connect(System.Net.IPAddress.Loopback, port);
			using System.Net.Sockets.NetworkStream stream = client.GetStream();
			receiveResult = tamper == null
				? Terraria.Mods.ModTransfer.Receive(stream, stream, new string[1] { requestedName })
				: ReceiveTampered(stream, requestedName, tamper);
		}
		catch (Exception ex)
		{
			error = ex.Message;
		}
		finally
		{
			try
			{
				listener.Stop();
			}
			catch
			{
			}
		}
		thread.Join(10000);
		if (error == null && hostError != null)
		{
			error = "host: " + hostError.Message;
		}
		return receiveResult ?? new Terraria.Mods.ModTransfer.ReceiveResult();
	}

	/// <summary>
	/// The client half with a tampering step between the socket and the parser.
	///
	/// Duplicates the read loop on purpose: <see cref="ModTransfer.Receive"/> is the real code
	/// and must not grow a test hook that a real caller could reach.
	/// </summary>
	private static Terraria.Mods.ModTransfer.ReceiveResult ReceiveTampered(System.IO.Stream stream, string modName, Func<byte[], byte[]> tamper)
	{
		Terraria.Mods.ModTransfer.ReceiveResult result = new Terraria.Mods.ModTransfer.ReceiveResult();
		// Ask for the mod through the real helper, then read frames ourselves.
		byte[] request = Terraria.Net.WireFormat.LengthPrefix(Terraria.Mods.ModTransfer.Request(modName));
		stream.Write(request, 0, request.Length);
		stream.Flush();
		string beginHash = null;
		long total = 0L;
		while (true)
		{
			byte[] payload = Terraria.Net.WireFormat.ReadFrame(stream);
			if (payload == null)
			{
				result.Error = "connection closed";
				result.Failed.Add(modName);
				return result;
			}
			Terraria.Mods.ModFileFrame frame;
			if (!Terraria.Mods.ModTransfer.TryParse(payload, out frame))
			{
				result.Error = "malformed";
				result.Failed.Add(modName);
				return result;
			}
			if (frame.Op == Terraria.Mods.ModFileOp.Refused)
			{
				result.Error = frame.Reason;
				result.Failed.Add(modName);
				return result;
			}
			if (frame.Op == Terraria.Mods.ModFileOp.Begin)
			{
				beginHash = frame.Hash;
				string error;
				for (int i = 0; i < frame.Files.Count; i++)
				{
					Terraria.Mods.ModFiles.TouchReceived(modName, frame.Files[i], out error);
				}
				continue;
			}
			if (frame.Op == Terraria.Mods.ModFileOp.Chunk)
			{
				byte[] array = new byte[frame.DataCount];
				Buffer.BlockCopy(frame.Data, frame.DataOffset, array, 0, frame.DataCount);
				// Only content is tampered with, never the framing: the point is to prove the
				// hash check catches it, not that the parser does.
				byte[] array2 = tamper(array);
				string error;
				Terraria.Mods.ModFiles.WriteReceived(modName, frame.Path, array2, 0, array2.Length, out error);
				total += array2.Length;
				continue;
			}
			if (frame.Op == Terraria.Mods.ModFileOp.End)
			{
				string text = Terraria.Mods.ModFiles.ContentHash(Terraria.Mods.ModFiles.IncomingDirectory(modName));
				if (string.Equals(text, beginHash, StringComparison.OrdinalIgnoreCase))
				{
					result.Error = "the tampered content still matched the hash";
					result.Received.Add(modName);
					return result;
				}
				result.Error = "hash mismatch";
				result.Failed.Add(modName);
				Terraria.Mods.ModFiles.DiscardIncoming(modName);
				return result;
			}
			result.Error = "unexpected frame";
			result.Failed.Add(modName);
			return result;
		}
	}

	// --- NPC names ----------------------------------------------------------

	/// <summary>
	/// "Spawn the Eye of Cthulhu" should not require knowing that it is 4.
	///
	/// There is no name→id table for NPCs in this build - only <see cref="Lang.NpcName"/>, which is
	/// a switch over ids, so the reverse has to be built. The check is that it round-trips: every
	/// name it knows must resolve to an id whose name is that name, or a command would spawn
	/// something other than what the player typed.
	/// </summary>
	private static bool CheckNpcNames()
	{
		try
		{
			Terraria.NpcLookup.Clear();
			if (Terraria.NpcLookup.Count < 50)
			{
				return Fail("the NPC name table holds only " + Terraria.NpcLookup.Count + " names");
			}
			int slime = Terraria.NpcLookup.Find("Blue Slime");
			if (slime <= 0)
			{
				return Fail("'Blue Slime' did not resolve");
			}
			if (Terraria.Lang.NpcName(slime) != "Blue Slime")
			{
				return Fail("'Blue Slime' resolved to id " + slime + ", which is '"
					+ Terraria.Lang.NpcName(slime) + "'");
			}
			// A name that exists only as a legacy negative id is refused rather than resolved:
			// those ids are not spawnable, so accepting one would produce a command that does
			// nothing. "Green Slime" is exactly that case in this build.
			if (Terraria.NpcLookup.Find("Green Slime") != 0)
			{
				return Fail("a legacy-only NPC name resolved to an id that cannot be spawned");
			}
			// A number is an id: the existing !spawn command takes one, and so do the wiki and the
			// debug world, so refusing numbers would break a working habit.
			if (Terraria.NpcLookup.Find("4") != 4)
			{
				return Fail("a numeric NPC id was not accepted");
			}
			if (Terraria.NpcLookup.Find("999") != 0)
			{
				return Fail("an out-of-range numeric id was accepted");
			}
			if (Terraria.NpcLookup.Find("Definitely Not An NPC") != 0)
			{
				return Fail("an unknown NPC name resolved to something");
			}
			if (Terraria.NpcLookup.Find("") != 0 || Terraria.NpcLookup.Find(null) != 0)
			{
				return Fail("an empty NPC name resolved to something");
			}
			// Round-trip every name, so a duplicate or a typo in the table is caught here rather
			// than by a player who typed a name the game itself shows.
			System.Collections.Generic.List<string> names = Terraria.NpcLookup.Names();
			for (int i = 0; i < names.Count; i++)
			{
				int num = Terraria.NpcLookup.Find(names[i]);
				if (num <= 0 || Terraria.Lang.NpcName(num) != names[i])
				{
					return Fail("'" + names[i] + "' round-tripped to id " + num + " ('"
						+ Terraria.Lang.NpcName(num) + "')");
				}
			}
			// Case does not matter: nobody types "blue slime" with a capital B.
			if (Terraria.NpcLookup.Find("blue slime") != slime)
			{
				return Fail("the NPC name lookup became case-sensitive");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("npc name check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

#if DEBUG
	// --- Debug console while hosting ----------------------------------------

	/// <summary>
	/// A Debug build may run the console while hosting, because "reproduce it on the server" is
	/// exactly when a developer needs to spawn something. Release must not: the flag that allows it
	/// does not exist there.
	///
	/// The second half is the part that is easy to get wrong: the player-flag commands must refuse
	/// rather than answer "on" and change nothing, because their effects are gated on single player.
	/// </summary>
	private static bool CheckDebugHostConsole()
	{
		int savedNetMode = Terraria.Main.NetMode;
		bool savedGod = Terraria.Cheats.CheatState.GodMode;
		try
		{
			Terraria.Cheats.CheatState.GodMode = false;
			// --- the gate itself ----------------------------------------------
			// Single player: cheats work. Hosting in a Debug build: the console works, but the
			// player-flag cheats are classified as single-player-only. A client: nothing.
			Terraria.Main.NetMode = 0;
			if (!Terraria.Cheats.CheatConsole.CanRunHere)
			{
				return Fail("the console refused to run in single player");
			}
			Terraria.Main.NetMode = 2;
			if (Terraria.Cheats.CheatState.IsAvailable)
			{
				return Fail("cheats became available while hosting - the gate must not have moved");
			}
			if (!Terraria.Cheats.CheatConsole.CanRunHere)
			{
				return Fail("a Debug host could not run the console");
			}
			Terraria.Main.NetMode = 1;
			if (Terraria.Cheats.CheatConsole.CanRunHere)
			{
				return Fail("a joined client could run the console");
			}
			// --- the classification -------------------------------------------
			// The four player-flag toggles are the ones whose effect is gated on single player, so
			// a host must be told rather than answered "on" while nothing happens.
			string[] local = new string[4] { "god", "noclip", "infmana", "infbuff" };
			for (int i = 0; i < local.Length; i++)
			{
				if (!Terraria.Cheats.CheatConsole.IsSinglePlayerOnly(local[i]))
				{
					return Fail("'" + local[i] + "' is not classified as single-player only");
				}
				if (!Terraria.Cheats.CheatConsole.IsSinglePlayerOnly("!" + local[i]))
				{
					return Fail("'" + local[i] + "' was not recognised with its '!' prefix");
				}
			}
			// The world commands are the ones that do work while hosting, so they must not be
			// classified as local - that would refuse the useful half.
			string[] world = new string[3] { "time", "killall", "meteor" };
			for (int j = 0; j < world.Length; j++)
			{
				if (Terraria.Cheats.CheatConsole.IsSinglePlayerOnly(world[j]))
				{
					return Fail("'" + world[j] + "' was classified as single-player only");
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("debug host console check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Main.NetMode = savedNetMode;
			Terraria.Cheats.CheatState.GodMode = savedGod;
		}
	}
#endif

	// --- Tutorial hints -----------------------------------------------------

	/// <summary>
	/// The tutorial must not teach buttons a keyboard player does not have - in any language.
	///
	/// The rewrite is keyed by step and glyph (see <see cref="TutorialHints"/>), so the risk is a
	/// step nobody mapped. This walks every step of every shipped language and fails on any glyph
	/// that survived, which is why a missing entry is a failing check rather than a button icon on
	/// screen.
	/// </summary>
	private static bool CheckTutorialHints()
	{
		bool savedEnabled = Terraria.Input.KeyboardMouseInput.Enabled;
		int savedLang = Terraria.Lang.LangOption;
		try
		{
			Terraria.Input.KeyboardMouseInput.Enabled = true;
			if (Terraria.TutorialHints.RuleCount < 30)
			{
				return Fail("the tutorial hint table lost its steps");
			}
			int num = 0;
			// Every language, because the same steps carry glyphs in all five and the table is
			// shared: a step mapped for English is mapped for German too, and this proves it.
			for (int lang = 1; lang <= 5; lang++)
			{
				Terraria.Lang.LangOption = lang;
				for (int i = 0; i < (int)Terraria.Tutorial.NUM_TUTORIALS; i++)
				{
					string raw = Terraria.Lang.TutorialLocale((Terraria.Tutorial)i);
					if (raw == null)
					{
						continue;
					}
					num++;
					string desktop = Terraria.TutorialHints.Translate(raw, i);
					if (Terraria.TutorialHints.HasGlyph(desktop))
					{
						return Fail("tutorial step " + (Terraria.Tutorial)i + " in language " + lang
							+ " still shows a gamepad glyph: '" + desktop + "'");
					}
					// A rewrite must not blank a step: an empty tutorial line is worse than a
					// button icon, because the player has nothing to read at all.
					if (desktop.Trim().Length == 0)
					{
						return Fail("tutorial step " + (Terraria.Tutorial)i + " in language " + lang
							+ " became empty");
					}
					// Debug aid: TXDK_DUMP_TUTORIAL=2 prints the German steps, and so on.
					if (Environment.GetEnvironmentVariable("TXDK_DUMP_TUTORIAL") == lang.ToString())
					{
						Console.WriteLine("    [" + lang + "] " + (Terraria.Tutorial)i + ": " + desktop.Replace("\n", " "));
					}
				}
			}
			if (num < 350)
			{
				return Fail("only " + num + " tutorial steps have text across five languages");
			}
			// The commonest sentence must have become a key that actually does the job: the
			// tutorial's "continue" listens for B, and the right button is B's desktop binding.
			Terraria.Lang.LangOption = 1;
			string cont = Terraria.TutorialHints.Translate("<c>Press \u008d to continue.", (int)Terraria.Tutorial.INTRO_2);
			if (cont.IndexOf("RMB", StringComparison.Ordinal) < 0)
			{
				return Fail("the 'continue' line was not rewritten: '" + cont + "'");
			}
			// A sentence that talks about the button as a button is replaced wholesale, not by
			// swapping one character: "press A or B until you switch to the Crafting section"
			// cannot survive as "press RMB or RMB until...".
			string crafting = Terraria.TutorialHints.Translate(
				Terraria.Lang.TutorialLocale(Terraria.Tutorial.CRAFTING), (int)Terraria.Tutorial.CRAFTING);
			if (crafting.IndexOf("tab", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return Fail("the crafting step does not point at the tab: '" + crafting + "'");
			}
			// With keyboard and mouse support off the console text is correct as it stands.
			Terraria.Input.KeyboardMouseInput.Enabled = false;
			if (Terraria.TutorialHints.Translate("<c>Press \u008d to continue.", (int)Terraria.Tutorial.INTRO_2) != "<c>Press \u008d to continue.")
			{
				return Fail("the tutorial was rewritten with keyboard support off");
			}
			Terraria.Input.KeyboardMouseInput.Enabled = true;
			// The tutorial's "switch between the categories" step must be satisfiable by a mouse
			// click on a tab, and must not be satisfiable by a key that changes nothing. What the
			// step watches is this rule, so this is what has to be right.
			if (Terraria.UI.IsCategorySwitch((int)Terraria.Recipe.Category.STRUCTURES, (int)Terraria.Recipe.Category.STRUCTURES))
			{
				return Fail("re-selecting the active category counts as a switch");
			}
			if (!Terraria.UI.IsCategorySwitch((int)Terraria.Recipe.Category.STRUCTURES, (int)Terraria.Recipe.Category.WEAPONS))
			{
				return Fail("changing the category does not count as a switch");
			}
			if (Terraria.UI.IsCategorySwitch((int)Terraria.Recipe.Category.STRUCTURES, 9)
				|| Terraria.UI.IsCategorySwitch((int)Terraria.Recipe.Category.STRUCTURES, -1))
			{
				return Fail("an invalid category counts as a switch");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("tutorial hints check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Input.KeyboardMouseInput.Enabled = savedEnabled;
			Terraria.Lang.LangOption = savedLang;
		}
	}

	// --- Crafting tables cover mod ids --------------------------------------

	/// <summary>
	/// The two tables the crafting screen reads must cover every id a mod can produce.
	///
	/// Both used to be sized to the vanilla build: <c>itemsFound</c> was 632 bits and a mod item's
	/// id starts at 20000, so the first mod recipe with a mod ingredient threw
	/// <c>ArgumentOutOfRangeException</c> from <c>BitArray.Get</c> as soon as the crafting tab was
	/// opened - and picking a mod item up threw from <c>Set</c>. The station table was 135 bits and
	/// a station id above it threw the same way.
	///
	/// The second half is the part that is easy to miss: a station this build cannot stand next to
	/// must be refused by name, not silently accepted, or the recipe is offered and can never be
	/// crafted.
	/// </summary>
	private static bool CheckCraftingTables()
	{
		try
		{
			// --- the tables are wide enough ------------------------------------
			Terraria.Player player = new Terraria.Player();
			if (player.itemsFound.Length <= Terraria.Mods.ModItems.IdBase)
			{
				return Fail("a new player's item table stops before the mod range: "
					+ player.itemsFound.Length + " <= " + Terraria.Mods.ModItems.IdBase);
			}
			if (player.craftingStationsFound.Length != Terraria.Player.StationTableBits)
			{
				return Fail("the station table is " + player.craftingStationsFound.Length
					+ " bits, not " + Terraria.Player.StationTableBits);
			}
			// The two calls that threw: recording a mod item, and asking whether it was seen.
			int modId = Terraria.Mods.ModItems.IdBase + 7;
			if (player.HasSeenItem(modId))
			{
				return Fail("a fresh player has already seen a mod item");
			}
			player.RememberItemForTests(modId);
			if (!player.HasSeenItem(modId))
			{
				return Fail("a mod item could not be recorded as seen");
			}
			// An id outside the table is "not seen", never an exception.
			if (player.HasSeenItem(int.MaxValue) || player.HasSeenItem(-1))
			{
				return Fail("an id outside the table was reported as seen");
			}
			// --- a station above the tile table is refused, not accepted --------
			if (Terraria.Mods.ModRecipes.StationId("Work Bench") != 18)
			{
				return Fail("the station table lost the work bench");
			}
			// 217 is the Blend-O-Matic in later PC builds; this build's tile table stops at 134.
			if (Terraria.Mods.ModRecipes.StationId("Blend-O-Matic") >= 0)
			{
				return Fail("a station above this build's tile table was accepted");
			}
			System.Collections.Generic.List<string> names = Terraria.Mods.ModRecipes.StationNames();
			for (int i = 0; i < names.Count; i++)
			{
				int num = Terraria.Mods.ModRecipes.StationId(names[i]);
				if (num < 0 || num >= Terraria.Player.StationTableBits)
				{
					return Fail("station '" + names[i] + "' resolves to id " + num
						+ ", outside this build's tile table");
				}
			}
			// --- a mod recipe with a mod ingredient is discovered, not a crash --
			// This is the exact shape of the crash: UpdateRecipes asks DiscoveredRecipe, which asks
			// about every ingredient and every station.
			int unseen = Terraria.Mods.ModItems.IdBase + 9;
			Terraria.Recipe recipe = new Terraria.Recipe();
			recipe.CraftedItem.SetDefaults(1, 1);
			// Set the id directly rather than through SetDefaults: the point is the table lookup, and
			// a raw-mod setup that is not installed would make SetDefaults produce an empty item.
			recipe.RequiredItem[0].Type = (short)unseen;
			recipe.RequiredItem[0].Stack = 1;
			recipe.NumRequiredItems = 1;
			recipe.RequiredTile[0] = 18;
			recipe.NumRequiredTiles = 1;
			if (player.DiscoveredRecipe(recipe))
			{
				return Fail("a recipe with an unseen mod ingredient was reported as discovered");
			}
			player.RememberItemForTests(unseen);
			player.craftingStationsFound.Set(18, value: true);
			if (!player.DiscoveredRecipe(recipe))
			{
				return Fail("a recipe whose ingredients and station are all known was not discovered");
			}
			// --- and the real thing: every loaded recipe, with the real mods -----
			// This is the exact call the crafting screen makes on its first frame. It walked the
			// whole recipe table, so any mod recipe in it is enough to hit a table that is too
			// small - which is why the crash happened on the recipes tab and nowhere else.
			Terraria.Player live = new Terraria.Player();
			live.UpdateRecipes();
			if (Terraria.Recipe.NumRecipes > 342 && live.RecipesFound.Length < Terraria.Recipe.NumRecipes)
			{
				return Fail("the 'discovered' table is narrower than the recipe table: "
					+ live.RecipesFound.Length + " < " + Terraria.Recipe.NumRecipes);
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("crafting tables check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Item fields hold numbers, not truncations --------------------------

	/// <summary>
	/// A pack value that does not fit the item field must be refused, not cast.
	///
	/// Every assignment used to be <c>(byte)result</c> or <c>(short)result</c>, and a narrowing
	/// cast truncates silently: <c>(byte)20043</c> is 75, a different item. For an ammo type that
	/// means the weapon consumes something else; for a buff type it means the equipment screen
	/// indexes <c>Buff.BuffName[]</c> and the sprite table past their ends and crashes. Neither is
	/// visible as "a bad number in a pack".
	///
	/// This is the same class as the crafting tables: a field sized for the vanilla build, handed
	/// a mod value.
	/// </summary>
	private static bool CheckItemFieldWidths()
	{
		try
		{
			int bowId = Terraria.ItemLookup.Find("Copper Bow");
			if (bowId <= 0)
			{
				return Fail("the test cannot find Copper Bow");
			}
			// --- a value too large for the field is refused, and the item is untouched ---
			// The vanilla value first: comparing the overridden item with itself would pass
			// whatever the code did.
			Item bow = default(Item);
			bow.SetDefaults(bowId);
			int vanillaDamage = bow.Damage;
			int vanillaAmmo = bow.UseAmmo;
			byte vanillaBuff = bow.BuffType;
			Terraria.Mods.ItemOverrides.Clear();
			Terraria.Mods.ItemOverrides.Set(bowId, "damage", "999999");
			bow = default(Item);
			bow.SetDefaults(bowId);
			if (bow.Damage != vanillaDamage)
			{
				return Fail("a damage value too large for a short was not refused: " + bow.Damage
					+ " (vanilla " + vanillaDamage + ")");
			}
			// --- an ammo type whose id does not fit the one-byte field is refused ---
			// A vanilla id above 255 is the real case: this build has 632 items, and the field
			// holds one byte. (byte)500 is 244 - a different item - which is exactly the silent
			// truncation that made this worth fixing.
			Terraria.Mods.ItemOverrides.Clear();
			string highName = Terraria.Lang.ItemName(500);
			if (string.IsNullOrEmpty(highName))
			{
				return Fail("the test cannot name item 500");
			}
			int highId = Terraria.ItemLookup.Find(highName);
			if (highId <= 255)
			{
				return Fail("item 500 resolved to " + highId + ", which fits in a byte - the test proves nothing");
			}
			Terraria.Mods.ItemOverrides.Set(bowId, "useAmmoName", highName);
			bow = default(Item);
			bow.SetDefaults(bowId);
			if (bow.UseAmmo == (byte)highId)
			{
				return Fail("an item id above 255 was truncated into the one-byte ammo field");
			}
			if (bow.UseAmmo != vanillaAmmo)
			{
				return Fail("a refused ammo name still changed the field: " + bow.UseAmmo);
			}
			// And the refusal is *reported*: a silent one leaves the player with a field that
			// "just does not work". ItemOverrides.Errors used to be collected and never read.
			bool saidWhy = false;
			for (int i = 0; i < Terraria.Mods.ItemOverrides.LoadErrors.Count; i++)
			{
				if (Terraria.Mods.ItemOverrides.LoadErrors[i].IndexOf("one byte", StringComparison.Ordinal) >= 0)
				{
					saidWhy = true;
				}
			}
			if (!saidWhy)
			{
				return Fail("an ammo type that does not fit the field was refused without a reason");
			}
			// --- a buff type past this build's table is refused ---
			Terraria.Mods.ItemOverrides.Clear();
			Terraria.Mods.ItemOverrides.Set(bowId, "buffType", (Terraria.Buff.MaxNumBuffs + 5).ToString());
			bow = default(Item);
			bow.SetDefaults(bowId);
			if (bow.BuffType >= Terraria.Buff.MaxNumBuffs || bow.BuffType != vanillaBuff)
			{
				return Fail("a buff type past the buff table was accepted: " + bow.BuffType);
			}
			// --- maxStack 0 is inside the short range but makes an item that cannot be held ---
			Terraria.Mods.ItemOverrides.Clear();
			Terraria.Mods.ItemOverrides.Set(bowId, "maxStack", "0");
			bow = default(Item);
			bow.SetDefaults(bowId);
			if (bow.MaxStack < 1)
			{
				return Fail("a maxStack of 0 was accepted, which deletes the item when it is picked up");
			}
			// --- and the ordinary values still work ---
			Terraria.Mods.ItemOverrides.Clear();
			Terraria.Mods.ItemOverrides.Set(bowId, "damage", "17");
			Terraria.Mods.ItemOverrides.Set(bowId, "buffType", "3");
			Terraria.Mods.ItemOverrides.Set(bowId, "useAmmoName", "Wooden Arrow");
			bow = default(Item);
			bow.SetDefaults(bowId);
			if (bow.Damage != 17)
			{
				return Fail("a normal damage value was not applied");
			}
			if (bow.BuffType != 3)
			{
				return Fail("a normal buff type was not applied");
			}
			int arrow = Terraria.ItemLookup.Find("Wooden Arrow");
			if (arrow <= 0 || bow.UseAmmo != arrow)
			{
				return Fail("a normal ammo name was not applied (got " + bow.UseAmmo + ")");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("item field width check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ItemOverrides.Clear();
		}
	}

	// --- Every supported field survives a hostile value --------------------

	/// <summary>
	/// A pack can say anything, and the field table is the only thing between that and the game.
	///
	/// This walks every field with the values a broken pack actually contains - empty, negative,
	/// absurdly large, not a number, a boolean where a number belongs, four thousand characters of
	/// tooltip - and requires that creating the item neither throws nor leaves it undescribable.
	/// The point is not that each field is applied; it is that no field can be the reason the game
	/// dies.
	///
	/// This is the check that would have caught <c>(byte)20043</c> and the <c>buffType</c> index
	/// before a player did.
	/// </summary>
	private static bool CheckItemFieldsHostile()
	{
		try
		{
			int bowId = Terraria.ItemLookup.Find("Copper Bow");
			if (bowId <= 0)
			{
				return Fail("the test cannot find Copper Bow");
			}
			string longText = new string('x', 4000);
			string[] hostile = new string[17]
			{
				"", "abc", "-1", "0", "1", "255", "256", "32767", "32768",
				"65535", "65536", "2147483647", "-2147483648", "1e999", "NaN", "true", longText
			};
			IList<string> fields = Terraria.Mods.ItemOverrides.FieldNames;
			if (fields.Count < 40)
			{
				return Fail("the field table is unexpectedly small: " + fields.Count);
			}
			Terraria.Mods.ItemOverrides.Clear();
			int tried = 0;
			for (int i = 0; i < fields.Count; i++)
			{
				for (int j = 0; j < hostile.Length; j++)
				{
					// Overrides accumulate on purpose: by the last field the item carries every bad
					// value at once, which is harsher than any single pack.
					Terraria.Mods.ItemOverrides.Set(bowId, fields[i], hostile[j]);
					Item bow = default(Item);
					bow.SetDefaults(bowId);
					tried++;
					string name = Terraria.Lang.ItemName(bow.Type);
					if (string.IsNullOrEmpty(name))
					{
						return Fail("field '" + fields[i] + "' = '" + Show(hostile[j])
							+ "' left the item unnamed");
					}
					if (bow.BuffType >= Terraria.Buff.MaxNumBuffs)
					{
						return Fail("field '" + fields[i] + "' = '" + Show(hostile[j])
							+ "' left a buff type past the table: " + bow.BuffType);
					}
					if (bow.MaxStack < 1)
					{
						return Fail("field '" + fields[i] + "' = '" + Show(hostile[j])
							+ "' left a max stack of " + bow.MaxStack);
					}
					if (float.IsNaN(bow.Scale) || float.IsInfinity(bow.Scale)
						|| float.IsNaN(bow.Knockback) || float.IsInfinity(bow.Knockback)
						|| float.IsNaN(bow.ShootSpeed) || float.IsInfinity(bow.ShootSpeed))
					{
						return Fail("field '" + fields[i] + "' = '" + Show(hostile[j])
							+ "' left a non-finite number on the item");
					}
				}
			}
			if (Terraria.Mods.ItemOverrides.LoadErrors.Count == 0)
			{
				return Fail("no hostile value was refused - the values are not reaching the checks");
			}
			Console.WriteLine("       hostile field values tried: " + tried);
			return true;
		}
		catch (Exception ex)
		{
			return Fail("a hostile field value threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Mods.ItemOverrides.Clear();
		}
	}

	/// <summary>Shortens a hostile value for a failure message; the tooltip one is 4000 chars.</summary>
	private static string Show(string value)
	{
		if (value == null)
		{
			return "(null)";
		}
		return (value.Length <= 24) ? value : (value.Substring(0, 24) + "...");
	}

	// --- Mod sprite sheets ------------------------------------------------

	/// <summary>
	/// A mod's sprites are packed into one texture, and the packer must never place a sprite
	/// outside it.
	///
	/// The bug this guards: the sheet height was clamped to the 4096 limit while the sprites kept
	/// being laid out below that line, so the copy loop wrote past the end of the pixel array and
	/// the game failed to start with an <c>IndexOutOfRangeException</c> - from a mod's pictures,
	/// at load, with nothing in the message saying so. A pack that does not fit is now refused
	/// with a reason, and that mod's items are undrawable while everything else loads.
	/// </summary>
	private static bool CheckModSheets()
	{
		try
		{
			int width;
			int height;
			string problem;
			bool inside;
			// The normal case: a hundred small sprites.
			int[] smallW = new int[100];
			int[] smallH = new int[100];
			for (int i = 0; i < 100; i++)
			{
				smallW[i] = 18;
				smallH[i] = 18;
			}
			if (!Terraria.Mods.ModSheets.PackForTests(smallW, smallH, out width, out height, out problem, out inside))
			{
				return Fail("a hundred 18x18 sprites were refused: " + problem);
			}
			if (!inside)
			{
				return Fail("a hundred 18x18 sprites were placed outside the " + width + "x" + height + " sheet");
			}
			if (width > 4096 || height > 4096)
			{
				return Fail("the sheet exceeds the texture limit: " + width + "x" + height);
			}
			// Very tall sprites: the case that used to be silently clamped.
			int[] tallW = new int[40];
			int[] tallH = new int[40];
			for (int j = 0; j < 40; j++)
			{
				tallW[j] = 200;
				tallH[j] = 200;
			}
			if (!Terraria.Mods.ModSheets.PackForTests(tallW, tallH, out width, out height, out problem, out inside))
			{
				return Fail("forty 200x200 sprites were refused: " + problem);
			}
			if (!inside)
			{
				return Fail("forty 200x200 sprites were placed outside the " + width + "x" + height + " sheet");
			}
			// One sprite larger than the limit: refused, with a reason that names the size.
			if (Terraria.Mods.ModSheets.PackForTests(new int[1] { 5000 }, new int[1] { 16 },
				out width, out height, out problem, out inside))
			{
				return Fail("a 5000 px wide sprite was accepted");
			}
			if (string.IsNullOrEmpty(problem))
			{
				return Fail("a sprite too large for the sheet was refused without a reason");
			}
			// Too much content for one sheet: also refused, and also with a reason.
			int[] manyW = new int[600];
			int[] manyH = new int[600];
			for (int k = 0; k < 600; k++)
			{
				manyW[k] = 256;
				manyH[k] = 256;
			}
			if (Terraria.Mods.ModSheets.PackForTests(manyW, manyH, out width, out height, out problem, out inside))
			{
				return Fail("600 256x256 sprites were accepted into one sheet");
			}
			if (string.IsNullOrEmpty(problem))
			{
				return Fail("content too large for one sheet was refused without a reason");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("mod sheet check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Cheat menu ---------------------------------------------------------

	/// <summary>
	/// The cheat list has to fit the frame it is drawn into, and every action has to be
	/// reachable. Both were broken by constants: fourteen rows at a fixed 38 px from a fixed
	/// top put the bottom of the panel 42 px below a 720-pixel frame, and the fourteenth row
	/// was a "next page" switch, so two of eighteen actions needed a page turn to reach.
	///
	/// Pure layout math, so it runs headless: the same function the screen uses.
	/// </summary>
	private static bool CheckCheatMenuLayout()
	{
		try
		{
			int num = Terraria.Cheats.CheatMenu.ActionCount;
			if (num <= 0)
			{
				return Fail("the cheat menu has no actions");
			}
			string[] array = Terraria.Cheats.CheatMenu.BuildRows();
			if (array.Length != num)
			{
				return Fail("the cheat menu built " + array.Length + " rows for " + num + " actions");
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (string.IsNullOrEmpty(array[i]))
				{
					return Fail("cheat row " + i + " has no text");
				}
			}
			// The three internal render presets, with the space each leaves for the list:
			// 720/960/1440 tall, minus the panel top, the title and the two footer lines.
			// All of them must show every action at a readable row height.
			int[] array2 = new int[3] { 594, 793, 1188 };
			int[] array3 = new int[3] { 40, 53, 80 };
			for (int j = 0; j < array2.Length; j++)
			{
				int rowHeight;
				int visible;
				Terraria.Cheats.CheatMenu.FitRows(array2[j], array3[j], (j == 0) ? 26 : ((j == 1) ? 34 : 52), num, out rowHeight, out visible);
				if (visible != num)
				{
					return Fail("only " + visible + " of " + num + " cheat rows fit at preset " + j);
				}
				if (rowHeight * visible > array2[j])
				{
					return Fail("the cheat list is " + (rowHeight * visible) + " px tall in " + array2[j] + " px");
				}
				if (rowHeight < 1)
				{
					return Fail("cheat row height collapsed to " + rowHeight);
				}
			}
			// A frame too short for the list scrolls instead of shrinking the rows into each
			// other, and still fits what it does show.
			int rowHeight2;
			int visible2;
			Terraria.Cheats.CheatMenu.FitRows(260, 40, 26, num, out rowHeight2, out visible2);
			if (rowHeight2 < 26)
			{
				return Fail("rows were shrunk below the readable height instead of scrolling: " + rowHeight2);
			}
			if (visible2 >= num)
			{
				return Fail("a 260 px list claimed to show all " + num + " rows");
			}
			if (visible2 * rowHeight2 > 260)
			{
				return Fail("the scrolled list overflows its space: " + (visible2 * rowHeight2) + " > 260");
			}
			if (visible2 < 1)
			{
				return Fail("a short frame showed no cheat rows at all");
			}
			// Degenerate input must not throw or produce a negative count.
			int rowHeight3;
			int visible3;
			Terraria.Cheats.CheatMenu.FitRows(0, 40, 26, num, out rowHeight3, out visible3);
			if (visible3 != 0 || rowHeight3 < 1)
			{
				return Fail("an empty area did not produce an empty layout");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("cheat menu layout check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	// --- Text keys used by the code -----------------------------------------

	/// <summary>
	/// Guards against the class of bug that produced a literal <c>&lt;ui.net.password&gt;</c> on
	/// the network screen: a key referenced from code but never added to the table. The missing
	/// key still renders as something, so nothing throws and nothing fails - the player just sees
	/// a placeholder.
	///
	/// The keys cannot be read out of the source at runtime, so the check names every key the
	/// screens in this port ask for. It is a regression guard, not a proof: a new key still has to
	/// be added here. The safety net for the gap is the warning <see cref="Texts.Get"/> logs.
	/// </summary>
	private static bool CheckTextKeysUsed()
	{
		try
		{
			// Length inferred: a fixed count silently left nulls at the end whenever a key was
			// added, and the check then asked Texts.Has(null) instead of naming the new key.
			string[] array = new string[]
			{
				"ui.common.on", "ui.common.off", "ui.common.change.back",
				"ui.hint.inv", "ui.hint.inv.split", "ui.hint.craft", "ui.hint.housing",
				"ui.hint.map", "ui.hint.character", "ui.hint.select", "ui.hint.back",
				"ui.hint.cheats", "ui.hint.keybinds", "ui.hint.mods", "ui.hint.pause",
				"ui.hint.start",
				"ui.char.unsupported",
				"ui.display.title", "ui.display.profile", "ui.display.window", "ui.display.fullscreen",
				"ui.display.render", "ui.display.fps", "ui.display.vsync", "ui.display.fpslimit",
				"ui.display.language", "ui.display.hint.restart", "ui.display.status.window",
				"ui.display.status.restart", "ui.display.status.cap", "ui.display.status.cap.off",
				"ui.display.status.vsync.capped", "ui.display.status.profile", "ui.display.failed",
				"ui.display.cap.overridden",
				"ui.hud.title", "ui.hud.zoom", "ui.hud.zoom.max", "ui.hud.zoom.min", "ui.hud.zoom.reset",
				"ui.hud.zoom.reset.done", "ui.hud.clock", "ui.hud.depth", "ui.hud.compass",
				"ui.hud.hardmode", "ui.hud.always", "ui.hud.with.watch", "ui.hud.with.depth",
				"ui.hud.with.compass", "ui.hud.change.back", "ui.hud.hint.zoom",
				"ui.net.title", "ui.net.name.title", "ui.net.name.usage", "ui.net.name", "ui.net.name.row",
				"ui.net.port.title", "ui.net.port.invalid", "ui.net.port.set", "ui.net.port",
				"ui.net.password.title", "ui.net.password", "ui.net.password.set",
				"ui.net.master.title", "ui.net.master.invalid", "ui.net.master.set",
				"ui.net.publish.on", "ui.net.publish.off", "ui.net.publish",
				"ui.net.spawn.off", "ui.net.spawn", "ui.net.build.disabled", "ui.net.build.allowed",
				"ui.net.build", "ui.net.refresh.off", "ui.net.refresh", "ui.net.off",
				"ui.settings.cheats", "ui.settings.keys", "ui.settings.mods", "ui.settings.display",
				"ui.settings.network", "ui.settings.hud", "ui.menu.cheats",
				"ui.mods.kind.raw", "ui.mods.raw.detail",
				"ui.mods.raw.detail.skipped", "ui.mods.raw.skipped",
				"ui.modmismatch.title", "ui.modmismatch.text", "ui.modmismatch.downloading",
				"ui.modmismatch.installed", "ui.modmismatch.restart", "ui.modmismatch.download.failed",
				"ui.modmismatch.offer",
				"ui.mods.convert.raw.done", "ui.mods.convert.raw.partial",
				"ui.cheats.title", "ui.cheats.hint", "ui.cheats.more",
				"ui.cheats.god", "ui.cheats.noclip", "ui.cheats.mana", "ui.cheats.buffs",
				"ui.cheats.fall", "ui.cheats.heal", "ui.cheats.caps", "ui.cheats.dawn",
				"ui.cheats.noon", "ui.cheats.dusk", "ui.cheats.midnight", "ui.cheats.bloodmoon",
				"ui.cheats.eye", "ui.cheats.skeletron", "ui.cheats.killall", "ui.cheats.meteor",
				"ui.cheats.recipes", "ui.cheats.save", "ui.cheats.single.only",
				"ui.cheats.removed", "ui.cheats.status"
			};
			for (int i = 0; i < array.Length; i++)
			{
				if (!Texts.Has(array[i]))
				{
					return Fail("the screens ask for '" + array[i] + "', which has no text");
				}
			}
			string text = Texts.Format("ui.net.password", "off");
			if (text.IndexOf('<') >= 0)
			{
				return Fail("the password row still shows a placeholder: '" + text + "'");
			}
			if (Texts.Get("ui.definitely.missing") != "<ui.definitely.missing>")
			{
				return Fail("an unknown key did not stand out");
			}
			// A hint that exists but is empty is the same as a missing one to the player, and it
			// passes a "does this key exist" check. The control hints are the one family where an
			// empty string is a bug rather than a deliberate blank.
			string[] hints = new string[13]
			{
				"ui.hint.inv", "ui.hint.inv.split", "ui.hint.craft", "ui.hint.housing",
				"ui.hint.map", "ui.hint.character", "ui.hint.select", "ui.hint.back",
				"ui.hint.cheats", "ui.hint.keybinds", "ui.hint.mods", "ui.hint.pause",
				"ui.hint.start"
			};
			for (int j = 0; j < hints.Length; j++)
			{
				if (string.IsNullOrWhiteSpace(Texts.Get(hints[j])) || Texts.Get(hints[j])[0] == '<')
				{
					return Fail("the control hint '" + hints[j] + "' is empty or unresolved");
				}
			}
			// A desktop hint must not name a gamepad button: that is the bug the hints exist to
			// avoid, and a copy-paste from the glyph table is exactly how it comes back.
			for (int k = 0; k < hints.Length; k++)
			{
				string hint = Texts.Get(hints[k]);
				for (int m = 0; m < hint.Length; m++)
				{
					if (hint[m] >= '\u0080' && hint[m] <= '\u008f')
					{
						return Fail("the desktop hint '" + hints[k] + "' contains a gamepad glyph");
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("text key check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

#if DEBUG
	// --- Debug kit ----------------------------------------------------------

	/// <summary>
	/// The debug-build item paging. Only compiled in Debug, alongside the feature it tests, so a
	/// Release build has neither.
	///
	/// What matters here is the arithmetic around the ranges, because that is where paging code
	/// goes wrong: the first page must start at item 1 (id 0 is "no item"), the last page must
	/// not run past the enum's sentinel, and a page number of 999 must not wrap into a useless
	/// "gave 0 items" without saying anything.
	/// </summary>
	private static bool CheckDebugKit()
	{
		try
		{
			int num = Terraria.Cheats.DebugKit.ItemCount;
			if (num < 100)
			{
				return Fail("only " + num + " item ids are known - the enum is not being read");
			}
			if (Terraria.Cheats.DebugKit.PageCount <= 0)
			{
				return Fail("the debug kit reports no pages for " + num + " items");
			}
			// Page 0 starts at id 1: item id 0 means "empty slot", so handing it out would look
			// like the command silently did nothing.
			int num2;
			int num3;
			Terraria.Cheats.DebugKit.RangeForPage(0, out num2, out num3);
			if (num2 != 1 || num3 <= num2)
			{
				return Fail("page 0 covers " + num2 + ".." + num3);
			}
			// The last page must stop at the sentinel, not include it.
			Terraria.Cheats.DebugKit.RangeForPage(Terraria.Cheats.DebugKit.PageCount - 1, out num2, out num3);
			if (num3 > num)
			{
				return Fail("the last page runs to " + num3 + ", past the " + num + " known ids");
			}
			if (num3 != num)
			{
				return Fail("the last page stops at " + num3 + " instead of covering up to " + num);
			}
			// Out-of-range pages clamp rather than throwing or producing an empty range.
			Terraria.Cheats.DebugKit.RangeForPage(-5, out num2, out num3);
			if (num2 != 1)
			{
				return Fail("a negative page did not clamp to the first");
			}
			Terraria.Cheats.DebugKit.RangeForPage(9999, out num2, out num3);
			if (num2 <= 0 || num3 <= num2)
			{
				return Fail("an out-of-range page produced the empty range " + num2 + ".." + num3);
			}
			if (Terraria.Cheats.DebugKit.ClampPage(-1) != 0 || Terraria.Cheats.DebugKit.ClampPage(9999) != Terraria.Cheats.DebugKit.PageCount - 1)
			{
				return Fail("ClampPage does not clamp to the first/last page");
			}
			// The whole range of ids must be covered exactly once across all pages.
			int num4 = 0;
			for (int i = 0; i < Terraria.Cheats.DebugKit.PageCount; i++)
			{
				Terraria.Cheats.DebugKit.RangeForPage(i, out num2, out num3);
				num4 += num3 - num2;
			}
			if (num4 != num - 1)
			{
				// Ids 1..ItemCount-1 inclusive: that is ItemCount-1 ids.
				return Fail("the pages cover " + num4 + " ids, expected " + (num - 1));
			}

			// Applying the kit to a fresh player must actually change something and stay in bounds.
			Player player = new Player();
			player.Inventory = new Item[49];
			for (int j = 0; j < player.Inventory.Length; j++)
			{
				player.Inventory[j].Init();
			}
			Terraria.Cheats.DebugKit.ApplyEssentials(player);
			if (player.StatLifeMax < 400 || player.statLife != player.StatLifeMax)
			{
				return Fail("the kit did not raise life: max " + player.StatLifeMax + ", current " + player.statLife);
			}
			// The equipment comes from Give, which is also what the console command calls.
			if (Terraria.Cheats.DebugKit.Give(player, Terraria.Cheats.DebugKit.Kit.Default) <= 0)
			{
				return Fail("the default kit gave nothing");
			}
			int num5 = 0;
			for (int k = 0; k < player.Inventory.Length; k++)
			{
				if (player.Inventory[k].Type > 0)
				{
					num5++;
				}
			}
			if (num5 < 5)
			{
				return Fail("the kit placed only " + num5 + " item(s)");
			}
			// Every kit must differ from the others, or the switch is decorative.
			Terraria.Cheats.DebugKit.Kit parsed;
			if (!Terraria.Cheats.DebugKit.Parse("pvp", out parsed) || parsed != Terraria.Cheats.DebugKit.Kit.Pvp
				|| !Terraria.Cheats.DebugKit.Parse("3", out parsed) || parsed != Terraria.Cheats.DebugKit.Kit.Full
				|| !Terraria.Cheats.DebugKit.Parse("", out parsed) || parsed != Terraria.Cheats.DebugKit.Kit.Default)
			{
				return Fail("a kit name did not parse");
			}
			if (Terraria.Cheats.DebugKit.Parse("nonsense", out parsed))
			{
				return Fail("an unknown kit name was accepted - it should be reported instead");
			}
			// Paging must not overwrite what is already there.
			int num6 = Terraria.Cheats.DebugKit.GivePage(player, 0);
			if (num6 <= 0)
			{
				return Fail("page 0 placed nothing");
			}
			if (num6 > 49 - num5)
			{
				// It filled free slots only; more items than free slots would mean overwriting.
				return Fail("page 0 placed " + num6 + " items into " + (49 - num5) + " free slot(s)");
			}
			Console.WriteLine("    debug kit: " + num + " item ids, " + Terraria.Cheats.DebugKit.PageCount
				+ " pages, " + num6 + " placed on page 0");

			// --- debug world chests -------------------------------------------
			// Classification is what decides which chest an item ends up in. A block that is a
			// workbench must be furniture (that is what a player looks for), and a tool that also
			// damages must be a tool, or the sorted chests are worse than one big one.
			if (!SameCategory(EntityID.ItemID.DIRT_BLOCK, Terraria.Cheats.DebugWorld.Category.Blocks))
			{
				return Fail("a dirt block did not land in the blocks chest");
			}
			if (!SameCategory(EntityID.ItemID.WORK_BENCH, Terraria.Cheats.DebugWorld.Category.Furniture))
			{
				return Fail("a workbench was filed as a plain block");
			}
			if (!SameCategory(EntityID.ItemID.WOOD_WALL, Terraria.Cheats.DebugWorld.Category.Walls))
			{
				return Fail("a wall item did not land in the walls chest");
			}
			if (!SameCategory(EntityID.ItemID.COPPER_PICKAXE, Terraria.Cheats.DebugWorld.Category.Tools))
			{
				return Fail("a pickaxe was not filed as a tool");
			}
			if (!SameCategory(EntityID.ItemID.IRON_BROADSWORD, Terraria.Cheats.DebugWorld.Category.Weapons))
			{
				return Fail("a sword was not filed as a weapon");
			}
			if (!SameCategory(EntityID.ItemID.IRON_HELMET, Terraria.Cheats.DebugWorld.Category.Armor))
			{
				return Fail("a helmet was not filed as armor");
			}
			if (!SameCategory(EntityID.ItemID.HEALING_POTION, Terraria.Cheats.DebugWorld.Category.Potions))
			{
				return Fail("a healing potion was not filed as a potion");
			}
			// Every category must end up with something. Which chest an item lands in is checked
			// above one by one; here the point is that no chest is empty, because an empty chest
			// in a room built for finding things is worse than no chest at all.
			System.Collections.Generic.Dictionary<Terraria.Cheats.DebugWorld.Category, List<int>> collected = Terraria.Cheats.DebugWorld.Collect();
			int num7 = 0;
			int num8 = 0;
			foreach (KeyValuePair<Terraria.Cheats.DebugWorld.Category, List<int>> item in collected)
			{
				num7 += item.Value.Count;
				if (item.Value.Count == 0)
				{
					return Fail("the " + Terraria.Cheats.DebugWorld.Label(item.Key) + " chest would be empty");
				}
				num8++;
			}
			if (num8 != Terraria.Cheats.DebugWorld.Categories.Length)
			{
				return Fail("only " + num8 + " of " + Terraria.Cheats.DebugWorld.Categories.Length + " chests are planned");
			}
			if (num7 < 300)
			{
				// "All the items" that fits in a few hundred ids is not all the items: the id
				// table resolution is broken, not the classification.
				return Fail("only " + num7 + " item ids were collected for the chests");
			}
			Console.WriteLine("    debug world: " + num7 + " items into " + num8 + " chests ("
				+ Terraria.Cheats.DebugWorld.Label(Terraria.Cheats.DebugWorld.Category.Weapons) + " " + collected[Terraria.Cheats.DebugWorld.Category.Weapons].Count + ", "
				+ Terraria.Cheats.DebugWorld.Label(Terraria.Cheats.DebugWorld.Category.Blocks) + " " + collected[Terraria.Cheats.DebugWorld.Category.Blocks].Count + ", "
				+ Terraria.Cheats.DebugWorld.Label(Terraria.Cheats.DebugWorld.Category.Materials) + " " + collected[Terraria.Cheats.DebugWorld.Category.Materials].Count + ")");
			return true;
		}
		catch (Exception ex)
		{
			return Fail("debug kit check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	/// <summary>True when one item id is classified into <paramref name="Expected"/>.</summary>
	private static bool SameCategory(EntityID.ItemID Id, Terraria.Cheats.DebugWorld.Category Expected)
	{
		Item item = Terraria.Cheats.DebugKit.Create((int)Id);
		if (item.Type <= 0)
		{
			Console.WriteLine("       " + Id + " = " + (int)Id + " did not resolve (type " + item.Type + ")");
			// A name that does not resolve is a different bug, and the caller's message would be
			// misleading; the id list itself is checked by the debug kit test.
			return false;
		}
		if (Terraria.Cheats.DebugWorld.Classify(item) == Expected)
		{
			return true;
		}
		Console.WriteLine("       " + Id + " resolved to type " + item.Type + " (tile " + item.CreateTile
			+ ", wall " + item.CreateWall + ", pick " + item.PickPower + ", dmg " + item.Damage
			+ ") and was filed as " + Terraria.Cheats.DebugWorld.Classify(item));
		return false;
	}

	// --- Debug console ------------------------------------------------------

	/// <summary>
	/// The debug console's command path, minus the window.
	///
	/// The window itself needs a desktop, so the headless suite does not open it. What it does
	/// check is the part that can be wrong: a typed line must land in the queue and stay there
	/// until the game thread drains it (touching game state from the UI thread is the one thing
	/// the console must never do), and an unknown command must not throw.
	/// </summary>
	private static bool CheckDebugConsole()
	{
		try
		{
			int num = Terraria.Cheats.DebugConsole.PendingCount;
			Terraria.Cheats.DebugConsole.EnqueueForTests("!help");
			if (Terraria.Cheats.DebugConsole.PendingCount != num + 1)
			{
				return Fail("a typed command was not queued");
			}
			Terraria.Cheats.DebugConsole.Drain();
			if (Terraria.Cheats.DebugConsole.PendingCount != 0)
			{
				return Fail("Drain left " + Terraria.Cheats.DebugConsole.PendingCount + " command(s) queued");
			}
			// Neither an unknown command nor an empty line may throw or leave work behind.
			Terraria.Cheats.DebugConsole.RunCommand("!definitely-not-a-command");
			Terraria.Cheats.DebugConsole.RunCommand("nonsense");
			Terraria.Cheats.DebugConsole.RunCommand("");
			if (Terraria.Cheats.DebugConsole.PendingCount != 0)
			{
				return Fail("an unknown command left something queued");
			}
			if (Terraria.Cheats.DebugKit.PageCount <= 0)
			{
				return Fail("the debug kit is not available from the console build");
			}
			return true;
		}
		catch (Exception ex)
		{
			return Fail("debug console check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
#endif

	/// <summary>
	/// The live master server, if it can be reached. Skips rather than fails: the game must be
	/// testable on a machine without internet, and a checked-in URL is not our availability
	/// guarantee - but when the master *is* running this is the only check that proves the real
	/// deployment speaks the same protocol the client does.
	///
	/// The default ports are asserted here too, because they are easy to confuse: the game hosts
	/// on 7777 and the master server listens on 17777, and swapping them makes the list silently
	/// empty.
	/// </summary>
	private static bool CheckMasterServerLive()
	{
		int savedPort = NetworkConfig.Port;
		int savedMasterPort = NetworkConfig.MasterServerPort;
		string savedEndpoints = string.Join(",", Terraria.Net.MasterServerClient.Endpoints);
		try
		{
			if (NetworkConfig.Port != 7777)
			{
				return Fail("the game server port default is " + NetworkConfig.Port + ", expected 7777");
			}
			if (NetworkConfig.MasterServerPort != 17777)
			{
				return Fail("the master server port default is " + NetworkConfig.MasterServerPort + ", expected 17777");
			}
			string endpoint = Terraria.Net.MasterServerClient.PrimaryEndpoint;
			if (string.IsNullOrEmpty(endpoint))
			{
				// In the game this is filled by MasterServerClient.Configure during startup. The
				// headless suite never constructs Main, so it configures it the same way - with a
				// settings object it does not write back.
				Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
				try
				{
					Terraria.Net.MasterServerClient.Configure(settings);
				}
				finally
				{
					settings.Discard();
				}
				endpoint = Terraria.Net.MasterServerClient.PrimaryEndpoint;
			}
			if (string.IsNullOrEmpty(endpoint))
			{
				return Fail("no master server endpoint is configured");
			}
			// Pick up the configured endpoint, but keep the test short and side-effect free: no
			// cache write, no backoff accounting, no publishing.
			Terraria.Net.MasterServerClient.ResetBackoff();
			// Liveness first, and from /health rather than from the cache file: Fetch() falls back
			// to the cached list when the master is down, so "the cache exists" only proves that
			// some run in the past succeeded. That check used to report a dead master as live.
			int health = Terraria.Net.MasterServerClient.Probe("/health");
			if (health == 0)
			{
				Skip(endpoint + " did not answer - start the master server to run this check");
			}
			if (health != 200)
			{
				return Fail(endpoint + " /health answered " + health);
			}
			List<Terraria.Net.MasterServerEntry> list = Terraria.Net.MasterServerClient.Fetch();
			if (list == null)
			{
				return Fail("Fetch returned null instead of a list");
			}
			Console.WriteLine("    master server " + endpoint + ": " + list.Count + " server(s) listed");
			// Same server, same client code the achievements and leaderboard screens use.
			int totalPlayers;
			int[] counts = Terraria.Net.MasterServerClient.FetchAchievementCounts(out totalPlayers);
			if (counts == null)
			{
				// A 404 means a master from before the statistics endpoints. That is a version
				// mismatch in a separate service, not a defect in this build, so it is a skip
				// with the reason - a Fail here would break every checkout pointed at an older
				// master, including the public one.
				if (Terraria.Net.MasterServerClient.Probe("/v1/achievements") == 404)
				{
					Skip(endpoint + " is older than this build - rebuild tools/MasterServer to check the statistics");
				}
				return Fail(endpoint + " did not answer /v1/achievements");
			}
			List<Terraria.Net.BoardRow> board = Terraria.Net.MasterServerClient.FetchBoard(new string[1] { "AirTravel" }, 10);
			if (board == null)
			{
				return Fail(endpoint + " did not answer /v1/leaderboard - update the master server");
			}
			Console.WriteLine("    stats: " + totalPlayers + " player(s), " + counts.Length + " achievement(s) reported, " + board.Count + " ranked by air travel");
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("master server live check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			NetworkConfig.Port = savedPort;
			NetworkConfig.MasterServerPort = savedMasterPort;
			if (savedEndpoints.Length != 0)
			{
				Terraria.Net.MasterServerClient.SetEndpoints(savedEndpoints);
			}
		}
	}

	private static bool CheckAchievementStats()
	{
		try
		{
			// The board columns are the keys the master ranks by, so their names and order matter:
			// a reordered column would put a crafting count under "AirTravel".
			string[] array = Terraria.Leaderboards.LeaderboardInfo.BoardStatistics(Terraria.Leaderboards.Leaderboard.DISTANCE);
			if (array == null || array.Length != 4 || array[0] != "AirTravel" || array[3] != "LavaTravel")
			{
				return Fail("the distance board columns are not AirTravel..LavaTravel");
			}
			array = Terraria.Leaderboards.LeaderboardInfo.BoardStatistics(Terraria.Leaderboards.Leaderboard.CRAFTING);
			if (array == null || array.Length != 6 || array[0] != "FurnitureCrafted")
			{
				return Fail("the crafting board columns are not the six crafting statistics");
			}
			if (Terraria.Leaderboards.LeaderboardInfo.BoardStatistics((Terraria.Leaderboards.Leaderboard)99) != null)
			{
				return Fail("an out-of-range board produced columns");
			}

			// The counter line: present only when the numbers are known, and it names the
			// population so "12" cannot be read as "12%".
			Terraria.Net.StatSync.ResetForTests();
			if (Terraria.Net.StatSync.HasCounts || Terraria.Net.StatSync.AchievementLabel(0) != null)
			{
				return Fail("an empty cache still produced an achievement label");
			}
			if (Terraria.Net.StatSync.AchievementCount(-1) != -1)
			{
				return Fail("a negative achievement index produced a count");
			}
			Terraria.Net.StatSync.SetCountsForTests(new int[3] { 5, 0, 3 }, 10);
			if (!Terraria.Net.StatSync.HasCounts)
			{
				return Fail("the counts were set but HasCounts is false");
			}
			if (Terraria.Net.StatSync.AchievementCount(0) != 5 || Terraria.Net.StatSync.AchievementCount(2) != 3)
			{
				return Fail("the counts were not readable back");
			}
			string text = Terraria.Net.StatSync.AchievementLabel(0);
			if (text == null || !text.Contains("5") || !text.Contains("10"))
			{
				return Fail("the label did not mention both numbers: '" + text + "'");
			}
			if (Terraria.Net.StatSync.AchievementLabel(7) != null)
			{
				return Fail("an achievement nobody reported produced a label");
			}
			return true;
		}
		catch (SkipCheckException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return Fail("achievement stats check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			Terraria.Net.StatSync.ResetForTests();
		}
	}

	private static bool Fail(string message)
	{
		Failures.Add(message);		Console.WriteLine("       " + message);
		return false;
	}
}
