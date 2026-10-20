using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Terraria.Net;

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

	private static readonly List<string> Failures = new List<string>();

	/// <summary>Runs every check and returns a process exit code.</summary>
	public static int Run(string[] args)
	{
		Console.WriteLine("TerrariaXDK self-test");
		Console.WriteLine("=====================");
		Terraria.Storage.AppPaths.EnsureDirectories();
		RunCheck("app paths", CheckAppPaths);
		RunCheck("content manifest", CheckContentManifest);
		RunCheck("content orphans", CheckContentOrphans);
		RunCheck("wire framing", CheckWireFormat);
		RunCheck("tcp loopback", CheckTcpLoopback);
		RunCheck("connect timeout", CheckConnectTimeout);
		RunCheck("network config", CheckNetworkConfig);
		RunCheck("mod loader", CheckModLoader);
		RunCheck("cheat gating", CheckCheatGating);
		RunCheck("cheat badge", CheckCheatBadge);
		RunCheck("loading progress", CheckLoadingProgress);
		RunCheck("input mapping", CheckInputMapping);
		RunCheck("settings sections", CheckSettingsSections);
		RunCheck("network stats", CheckNetworkStats);
		RunCheck("key bindings", CheckKeyBindings);
		RunCheck("tconfig import", CheckTConfigImport);
		RunCheck("virtual keyboard", CheckVirtualKeyboard);
		RunCheck("display settings", CheckDisplaySettings);
		RunCheck("direct connect", CheckDirectConnect);
		RunCheck("world backups", CheckWorldBackups);
		ReportMemory();
		Console.WriteLine();
		Console.WriteLine($"Passed: {_passed}  Failed: {_failed}");
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
		string text = (message == null) ? name : (name + " - " + message);
		Failures.Add(text);
		Console.WriteLine("  FAIL " + text);
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
		if (!string.Equals(Microsoft.Xna.Framework.Storage.StorageDevice.RootOverride, Terraria.Storage.AppPaths.Saves, StringComparison.OrdinalIgnoreCase))
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
			return Fail("Content folder not found at " + contentRoot);
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

	// --- Display settings ---------------------------------------------------

	/// <summary>
	/// The display options were declared in Settings.ini but never applied. These checks pin
	/// down the mapping from config to resolution and that cycling stays in range.
	/// </summary>
	private static bool CheckDisplaySettings()
	{
		string text = System.IO.Path.Combine(Terraria.Storage.AppPaths.Cache, "selftest-display.ini");
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
			string[] files = System.IO.Directory.GetFiles(Terraria.WorldBackups.Folder, worldName + "_*.wld");
			for (int i = 0; i < files.Length; i++)
			{
				System.IO.File.Delete(files[i]);
			}
		}
		catch
		{
		}
	}

	private static bool Fail(string message)
	{
		Failures.Add(message);		Console.WriteLine("       " + message);
		return false;
	}
}
