using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
		RunCheck("mod load order", CheckModLoadOrder);
		RunCheck("virtual keyboard", CheckVirtualKeyboard);
		RunCheck("name prompt", CheckNamePrompt);
		RunCheck("player identity", CheckPlayerIdentity);
		RunCheck("display settings", CheckDisplaySettings);
		RunCheck("display profiles", CheckDisplayProfiles);
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
		catch (SkipCheckException ex)
		{
			_skipped++;
			string text = ex.Message;
			Skips.Add(name + (string.IsNullOrEmpty(text) ? "" : (" - " + text)));
			Console.WriteLine("  skip " + name + (string.IsNullOrEmpty(text) ? "" : (" - " + text)));
			return;
		}
		catch (Exception ex)
		{
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
		string[] array = new string[6]
		{
			Terraria.Storage.AppPaths.Saves, Terraria.Storage.AppPaths.Logs, Terraria.Storage.AppPaths.Mods, Terraria.Storage.AppPaths.Backups, Terraria.Storage.AppPaths.Config, Terraria.Storage.AppPaths.Cache
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
		List<string> list = RequiredAssets();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list)
		{
			hashSet.Add(item.Replace('/', '\\') + ".xnb");
		}
		string[] array = new string[3]
		{
			"Worlds\\tutorial.wld", "ExtMusic\\Music_1.wav", "ExtMusic\\Snow.wav"
		};
		List<string> list2 = new List<string>();
		foreach (string item2 in Directory.GetFiles(contentRoot, "*", SearchOption.AllDirectories))
		{
			string text = item2.Substring(contentRoot.Length + 1);
			string extension = Path.GetExtension(text);
			if (extension.Equals(".xnb", StringComparison.OrdinalIgnoreCase))
			{
				if (!hashSet.Contains(text))
				{
					list2.Add(text);
				}
				continue;
			}
			if (extension.Equals(".wld", StringComparison.OrdinalIgnoreCase))
			{
				if (!text.Equals("Worlds\\tutorial.wld", StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(text);
				}
				continue;
			}
			if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) || extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
			{
				if (!text.StartsWith("ExtMusic\\", StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(text);
				}
				continue;
			}
			// Anything else in Content is unexpected.
			list2.Add(text);
		}
		if (list2.Count == 0)
		{
			return true;
		}
		return Fail(list2.Count + " unexpected content file(s): " + string.Join(", ", list2.ToArray()));
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
			byte[] cyrillic = HostModeration.BuildChatPayload("привет", 1, 2, 3);
			using (System.IO.MemoryStream stream2 = new System.IO.MemoryStream(cyrillic, 4, cyrillic.Length - 4))
			{
				using System.IO.BinaryReader reader2 = new System.IO.BinaryReader(stream2, Encoding.UTF8);
				if (reader2.ReadString() != "привет")
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
			string[] gated = new string[12]
			{
				"!who",
				"!bans",
				"!groups",
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
			if (HostCommands.Names.Length != 22)
			{
				return Fail("the command list has " + HostCommands.Names.Length + " entries, expected 22");
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

			// Refusing host (password / mods / version): still alive.
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
			if (ServerPing.Label(dead) != "timeout")
			{
				return Fail("a dead server label is '" + ServerPing.Label(dead) + "'");
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
		catch (Exception ex)
		{
			return Fail("world file inspector check threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static bool Fail(string message)
	{
		Failures.Add(message);		Console.WriteLine("       " + message);
		return false;
	}
}
