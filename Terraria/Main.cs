using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Terraria.Achievements;

namespace Terraria;

public sealed class Main : Game
{
	public enum Music : byte
	{
		OVERWORLD_DAY = 0,
		EERIE = 1,
		OVERWORLD_NIGHT = 2,
		UNDERGROUND = 3,
		BOSS1 = 4,
		TITLE = 5,
		JUNGLE = 6,
		CORRUPTION = 7,
		THE_HALLOW = 8,
		UNDERGROUND_CORRUPTION = 9,
		UNDERGROUND_HALLOW = 10,
		BOSS2 = 11,
		BOSS3 = 12,
		DESERT = 13,
		FLOATING_ISLAND = 14,
		TUTORIAL = 15,
		BOSS4 = 16,
		OCEAN = 17,
		SNOW = 18,
		NUM_SONGS = 19,
		NONE = NUM_SONGS
	}

	private RenderTarget2D _renderTarget;

	public static int ResolutionWidth;

	public static int ResolutionHeight;

	internal const string OGCVersion = "TerrariaXDK v1.03";

	public const int SmallWorldW = 4200;

	public const int SmallWorldH = 1200;

	public const int LargeWorldW = 5040;

	public const int LargeWorldH = 1440;

	public const int MediumWorldW = 4620;

	public const int MediumWorldH = 1320;

	public const bool NET_VERBOSE = false;

	public const int MaxNumChests = 1000;

	public const int MaxNumItems = 200;

	public const int MaxNumItemSounds = 37;

	public const int MaxNumNPCHitSounds = 11;

	public const int MaxNumNPCKillSounds = 15;

	public const int MaxNumLiquidTypes = 2;

	public const int MaxNumMusic = 19;

	public const int OldWorldDataVersion = 46;

	public const int PlayerDataVersion = 6;

	public const int SettingsDataVersion = 5;

	public const int NewWorldDataVersion = 49;

	public const int NetworkVersion = 2;

	public const string VersionNumber = "Xbox360 v0.7.8";

	public const int WorldRate = 1;

	public const bool IgnoreErrors = false;

	public const int ZoneX = 99;

	public const int ZoneY = 87;

	public const bool ShowFrameRate = false;

	public const int LeftWorld = 0;

	public const int TopWorld = 0;

	public const int SectionWidth = 40;

	public const int SectionHeight = 30;

	public const int MaxNumTilesets = 150;

	public const int MaxNumWallTypes = 32;

	public const int MaxNumTilenames = 135;

	public const int MaxNumCombatText = 32;

	public const int ChatLength = 600;

	public const int MaxNumChatLines = 7;

	private const int MaxNumItemUpdates = 5;

	public const int SaveIconSprite = 642;

	public const int SaveIconMessageTime = 480;

	private const int SaveIconMinTime = 180;

	private const int NumSplashLogos = 4;

	private const int SplashFadeIn = 16;

	private const int SplashFadeOut = 16;

	private const int SplashDelayBase = 120;

	private const int SplashDelayRating = 240;

	private const int UpsellNumScreens = 1;

	private const int UpsellDelay = 600;

	private static readonly string[] MusicCueNames = new string[19]
	{
		"Music_1", "Music_2", "Music_3", "Music_4", "Music_5", "Music_6", "Music_7", "Music_8", "Music_9", "Music_10",
		"Music_11", "Music_12", "Music_13", "Desert", "FloatingIsland", "Tutorial", "Boss4", "Ocean", "Snow"
	};

	private static readonly Music[] MusicBoxToSong = new Music[19]
	{
		Music.OVERWORLD_DAY,
		Music.EERIE,
		Music.OVERWORLD_NIGHT,
		Music.TITLE,
		Music.UNDERGROUND,
		Music.BOSS1,
		Music.JUNGLE,
		Music.CORRUPTION,
		Music.UNDERGROUND_CORRUPTION,
		Music.THE_HALLOW,
		Music.BOSS2,
		Music.UNDERGROUND_HALLOW,
		Music.BOSS3,
		Music.DESERT,
		Music.FLOATING_ISLAND,
		Music.TUTORIAL,
		Music.BOSS4,
		Music.OCEAN,
		Music.SNOW
	};

	public static readonly EntityID.ItemID[] SongToMusicBox = new EntityID.ItemID[19]
	{
		EntityID.ItemID.MUSIC_BOX_OVERWORLD_DAY,
		EntityID.ItemID.MUSIC_BOX_EERIE,
		EntityID.ItemID.MUSIC_BOX_NIGHT,
		EntityID.ItemID.MUSIC_BOX_UNDERGROUND,
		EntityID.ItemID.MUSIC_BOX_BOSS1,
		EntityID.ItemID.MUSIC_BOX_TITLE,
		EntityID.ItemID.MUSIC_BOX_JUNGLE,
		EntityID.ItemID.MUSIC_BOX_CORRUPTION,
		EntityID.ItemID.MUSIC_BOX_THE_HALLOW,
		EntityID.ItemID.MUSIC_BOX_UNDERGROUND_CORRUPTION,
		EntityID.ItemID.MUSIC_BOX_UNDERGROUND_HALLOW,
		EntityID.ItemID.MUSIC_BOX_BOSS2,
		EntityID.ItemID.MUSIC_BOX_BOSS3,
		EntityID.ItemID.MUSIC_BOX_DESERT,
		EntityID.ItemID.MUSIC_BOX_SPACE,
		EntityID.ItemID.MUSIC_BOX_TUTORIAL,
		EntityID.ItemID.MUSIC_BOX_BOSS4,
		EntityID.ItemID.MUSIC_BOX_OCEAN,
		EntityID.ItemID.MUSIC_BOX_SNOW
	};

	public static UI[] UIInstance = new UI[4];

	public static AchievementSystem AchievementSystem = new AchievementSystem();

	public static Thread WorldGenThread;

	public static int MusicBox = -1;

	public static float MusicVolume = 0.75f;

	public static float SoundVolume = 1f;

	private Thread ContentThread;

	public static float HarpNote = 0f;

	public static bool[] ProjHostile = new bool[120];

	public static Recipe[] ActiveRecipe = new Recipe[Recipe.Capacity];

	public static Chest[] Shop = new Chest[10];

	public static int RenderCount = 0;

	private static GraphicsDeviceManager GraphicsGame;

	public static SpriteBatch SpriteBatch;

	public static StringBuilder StrBuilder = new StringBuilder(4096, 4096);

	public static bool IsGameStarted = false;

	public static bool IsGamePaused = false;

	public static bool InHardMode = false;

	public static int DiscoStyle = 0;

	public static Vector3 DiscoRGB = new Vector3(1f, 0f, 0f);

	public static uint FrameCounter = 0u;

	public static int MagmaBGFrame = 0;

	public static int RightWorld = 80640;

	public static int BottomWorld = 23040;

	public static short MaxTilesX = (short)(RightWorld / 16);

	public static short MaxTilesY = (short)(BottomWorld / 16);

	public static int MaxSectionsX = MaxTilesX / 40;

	public static int MaxSectionsY = MaxTilesY / 30;

	public static string[] TileNames = new string[135];

	public static short DungeonX;

	public static short DungeonY;

	public static Liquid[] Liquid = new Liquid[4096];

	public static LiquidBuffer[] LiquidBuffer = new LiquidBuffer[8192];

	public static Music CurrentMusic = Music.NUM_SONGS;

	public static Music NewMusic = Music.NUM_SONGS;

	public static string WorldName;

	public static int WorldID;

	public static int WorldTimestamp;

	public static bool CheckWorldId = false;

	public static bool CheckUserGeneratedContent = false;

	public static int WorldSurface = 360;

	public static int WorldSurfacePixels = WorldSurface << 4;

	public static int RockLayer;

	public static int RockLayerPixels;

	public static int MagmaLayer;

	public static int MagmaLayerPixels;

	public static Color[] TeamColors = new Color[5];

	public static Time GameTime = default;

	public static Time MenuTime = default;

	public static float DemonTorch = 1f;

	public static float DemonTorchDir = -0.01f;

	public static FastRandom Rand = new FastRandom();

	public static Texture2D WhiteTexture;

	public static SfxInstancePool SoundMech;

	public static SfxInstancePool[] SoundDig = new SfxInstancePool[3];

	public static SfxInstancePool[] SoundTink = new SfxInstancePool[3];

	public static SfxInstancePool[] SoundPlayerHit = new SfxInstancePool[3];

	public static SfxInstancePool[] SoundFemaleHit = new SfxInstancePool[3];

	public static SfxInstancePool SoundPlayerKilled;

	public static SfxInstancePool SoundGrass;

	public static SfxInstancePool SoundGrab;

	public static SfxInstancePool SoundPixie;

	public static SfxInstancePool[] SoundItem = new SfxInstancePool[37];

	public static SfxInstancePool[] SoundNPCHit = new SfxInstancePool[11];

	public static SfxInstancePool[] SoundNPCKilled = new SfxInstancePool[15];

	public static SfxInstancePool SoundDoorOpen;

	public static SfxInstancePool SoundDoorClosed;

	public static SfxInstancePool SoundMenuOpen;

	public static SfxInstancePool SoundMenuClose;

	public static SfxInstancePool SoundMenuTick;

	public static SfxInstancePool SoundShatter;

	public static SfxInstancePool[] SoundZombie = new SfxInstancePool[5];

	public static SfxInstancePool[] SoundRoar = new SfxInstancePool[2];

	public static SfxInstancePool[] SoundSplash = new SfxInstancePool[2];

	public static SfxInstancePool SoundDoubleJump;

	public static SfxInstancePool SoundRun;

	public static SfxInstancePool SoundCoins;

	public static SfxInstancePool SoundUnlock;

	public static SfxInstancePool SoundChat;

	public static SfxInstancePool SoundMaxMana;

	public static SfxInstancePool SoundDrown;

	private static SoundManager UsedManager;

	public static float[] MusicFade = new float[19];

	public static bool[] TileLighted = new bool[150];

	public static bool[] TileMergeDirt = new bool[150];

	public static bool[] TileCut = new bool[150];

	public static short[] TileShine = new short[150];

	public static bool[] TileShine2 = new bool[150];

	public static bool[] WallHouse = new bool[32];

	public static bool[] TileStone = new bool[150];

	public static bool[] TileAxe = new bool[150];

	public static bool[] TileHammer = new bool[150];

	public static bool[] TileWaterDeath = new bool[150];

	public static bool[] TileLavaDeath = new bool[150];

	public static bool[] TileTable = new bool[150];

	public static bool[] TileBlockLight = new bool[150];

	public static bool[] TileNoSunLight = new bool[150];

	public static bool[] TileDungeon = new bool[150];

	public static bool[] TileSolidTop = new bool[150];

	public static bool[] TileSolid = new bool[150];

	public static bool[] TileSolidNotSolidTop = new bool[150];

	public static bool[] TileSolidAndAttach = new bool[150];

	public static bool[] TileNoAttach = new bool[150];

	public static bool[] TileNoFail = new bool[150];

	public static bool[] TileFrameImportant = new bool[150];

	public static DustPool DustSet = new DustPool(null, 256);

	public static Tile[,] TileSet = new Tile[5040, 1440];

	public static Item[] ItemSet = new Item[201];

	public static NPC[] NPCSet = new NPC[197];

	public static Gore[] GoreSet = new Gore[128];

	public static Projectile[] ProjectileSet = new Projectile[512];

	public static CombatText[] CombatTextSet = new CombatText[32];

	public static Chest[] ChestSet = new Chest[1000];

	public static Sign[] SignSet = new Sign[1000];

	public static ChatLine[] ChatLineSet = new ChatLine[7];

	public static Player[] PlayerSet = new Player[9];

	public static short SpawnTileX;

	public static short SpawnTileY;

	public static bool HasFocus = true;

	public static int InvasionType = 0;

	public static float InvasionX = 0f;

	public static int InvasionSize = 0;

	public static int InvasionDelay = 0;

	public static int InvasionWarn = 0;

	public static int NetMode = 0;

	public static int NetPlayCounter;

	public static int LastItemUpdate;

	private static int SaveIconCounter = 0;

	private static int ActiveSaves = 0;

	public static bool SaveOnExit;

	private static readonly Texture2D[] SplashTextures = new Texture2D[5];

	private static short ShowSplash = 0;

	private short SplashDelay = 240;

	private short SplashCounter;

	private short SplashLogo;

	private bool HasUpsellLoaded;

	public static bool IsRunningSlowly;

	public static bool IsTrial = true;

	public static bool IsHDTV;

	public static Tutorial TutorialState = Tutorial.NUM_TUTORIALS;

	public static bool TutorialMaskLS;

	public static bool TutorialMaskRS;

	public static bool TutorialMaskRSpress;

	public static bool TutorialMaskA;

	public static bool TutorialMaskB;

	public static bool TutorialMaskX;

	public static bool TutorialMaskY;

	public static bool TutorialMaskLB;

	public static bool TutorialMaskRB;

	public static bool TutorialMaskLT;

	public static bool TutorialMaskRT;

	public static bool TutorialMaskBack;

	public static CompiledText TutorialText = null;

	private static int TutorialInputDelay;

	private static uint TutorialVar;

	private static uint TutorialVar2;

	private static Location TutorialHouse;

	public const string SettingsFile = "Settings.ini";

	public static string Gamertag;

	public static bool PSMode;

	public static bool TouchpadButton;

	public static bool UnlockAllRecipes;

	public static bool ShowFPS;

	/// <summary>Forwarded to <see cref="HudSettings"/>; <see cref="Player"/> reads these.</summary>
	public static bool AlwaysOnWatch
	{
		get
		{
			return Terraria.HudSettings.AlwaysWatch;
		}
		set
		{
			Terraria.HudSettings.AlwaysWatch = value;
		}
	}

	public static bool AlwaysOnDepth
	{
		get
		{
			return Terraria.HudSettings.AlwaysDepth;
		}
		set
		{
			Terraria.HudSettings.AlwaysDepth = value;
		}
	}

	public static bool AlwaysOnCompass
	{
		get
		{
			return Terraria.HudSettings.AlwaysCompass;
		}
		set
		{
			Terraria.HudSettings.AlwaysCompass = value;
		}
	}

	public static ScreenHeights ScreenHeightPtr;

	public static float ScreenMultiplier;

	/// <summary>
	/// Half of the internal render resolution, in pixels.
	///
	/// The game's logic builds "the area the screen covers, plus a margin" rectangles in several
	/// places (monster spawn range, town-NPC teleport checks, meteor placement). Those were written
	/// as <c>1920x1080</c>, which is the FHD preset: at 540p the area was more than twice the
	/// screen, so behaviour depended on the render-scale setting. Deriving the numbers keeps them
	/// honest at every preset.
	/// </summary>
	public static int ScreenHalfWidth => ResolutionWidth >> 1;

	public static int ScreenHalfHeight => ResolutionHeight >> 1;

	/// <summary>
	/// The area a player can see, as a world-space rectangle around a point, with a small margin.
	///
	/// Used for the "is anything on screen?" checks: monster spawn range, town-NPC teleport, meteor
	/// placement. Those were written against the 1080p preset (<c>1920x1080</c> as constants), so
	/// at 540p they covered more than twice the visible area and at any other preset the numbers
	/// were simply wrong. One function, so the seven callers cannot drift apart again, and so the
	/// geometry is testable without a world.
	/// </summary>
	public static Rectangle ScreenAreaAround(int centerX, int centerY)
	{
		return new Rectangle(
			centerX - ScreenHalfWidth - ScreenMargin,
			centerY - ScreenHalfHeight - ScreenMargin,
			ResolutionWidth + ScreenMargin * 2,
			ResolutionHeight + ScreenMargin * 2);
	}

	/// <summary>Margin around the visible area, in pixels. Two tiles.</summary>
	public const int ScreenMargin = 34;

	public static bool HardmodeAlert;

	private bool SoundResponse;

	public static bool CollectorsEditionPC = false;

	private readonly bool StartFullscreen;

	private float ZoomLevel = Terraria.HudSettings.MinZoom;

	public static Stopwatch FPSTimer = new Stopwatch();

	public static short FPS;

	public static short FPSCount;

	internal static KeyboardState CurrentKeyState;

	internal static KeyboardState PrevKeyState;

	public static Stopwatch RunTimer = new Stopwatch();

	// Written by the audio loader thread, read by the main thread every frame: volatile is
	// required, otherwise the loop can observe a stale value and wait out the full timeout.
	public static volatile string _loadingStatusText = "Loading...";

	public static volatile float _loadingProgress = 0f;

	private float _displayProgress = 0f;

	private Texture2D _pixelTexture;

	private int _updateCount = 0;

	private int _drawCount = 0;

	private int _drawSplashCount = 0;

	private int _drawGameCount = 0;

	public void CheckKeyStates()
	{
		PrevKeyState = CurrentKeyState;
		CurrentKeyState = Keyboard.GetState();
	}

	public bool HasKeyBeenPressed(Keys key)
	{
		if (CurrentKeyState.IsKeyDown(key))
		{
			return !PrevKeyState.IsKeyDown(key);
		}
		return false;
	}

	private void SpecialCheck()
	{
		// Was a bare registry read: Windows-only, and its exception was swallowed, so the flag
		// was simply always false elsewhere. See CollectorsEdition for the two sources now.
		CollectorsEditionPC = CollectorsEdition.IsActive();
	}

	private static int ParsePort(string text, int fallback)
	{
		if (int.TryParse(text, out var result) && result > 0 && result <= 65535)
		{
			return result;
		}
		return fallback;
	}

	public static ulong GetWorldId()
	{
		return (ulong)(((long)WorldID << 32) | (uint)WorldTimestamp);
	}

	public Main()
	{
		Program.Log("[MAIN] ⚡ Constructor entered");
		GraphicsGame = new GraphicsDeviceManager(this);
		GraphicsGame.SynchronizeWithVerticalRetrace = true;
		IsFixedTimeStep = true;
		Content.RootDirectory = "Content";
		// A content manager that lets a loose PNG replace a compiled texture. Set here, before
		// anything loads, so every Content.Load<Texture2D> in the game goes through it.
		Content = new Terraria.Textures.TextureContentManager(Services);
		Content.RootDirectory = "Content";
		Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
		// The console got this from Xbox Live. Defaulting to a literal "Terrarian" made every
		// unconfigured player share one name in LAN lists and rosters, so the OS account is a
		// better default; the player can change it in the network settings.
		settings.Set("Game", "Gamertag", Terraria.PlayerIdentity.Default(), "Name other players see in multiplayer and in LAN lists.");
		settings.Set("Game", "TrialActive", "False", "Determines if trial mode is active; Included for archival/legal purposes, as this is not the full version by default.");
		settings.Set("Game", "PSMode", "False", "Determines whether the game will run in 'PlayStation 3' mode, rather than 'Xbox 360' mode.\n; This will change the font, controller, D-Pad, and button sprites used in game.");
		settings.Set("Game", "TouchpadButton", "False", "If you want the game to account for a touchpad button, enable this setting. This will not change the controller labels in-game.\n; This will only function if PSMode is enabled and is akin to the PS4 control scheme.");
		settings.Set("Game", "UnlockAllRecipes", "False");
		// The PC Collectors Edition flag used to come only from the registry, so it was false on
		// every install where the PC edition was never installed - and always false off Windows.
		settings.Set("Game", "CollectorsEdition", "Auto", "Grants the Collectors Edition bunny pet. Auto = ask the Windows registry (HKCU\\Software\\Terraria), True/False = decide here.");
		settings.Set("Display", "ScreenHeight", "0", "Internal render resolution: 0 = 540p, 1 = 720p, 2 = 1080p.\n; Higher values look sharper and cost more.");
		settings.Set("Display", "Language", "0", "0 = follow the system, 1 = English, 2 = Deutsch, 3 = Italiano, 4 = Français, 5 = Español.");
		settings.Set("Display", "Width", "1280", "Window width in pixels.");
		settings.Set("Display", "Height", "720", "Window height in pixels.");
		settings.Set("Display", "ShowFPS", "False");
		settings.Set("Display", "StartFullscreen", "True", "Determines if the game is started in full-screen mode; This will not upscale, but stretch the current resolution to the display's max.\n; You can also toggle full-screen mode in-game by pressing 'F'.");
		settings.Set("Display", "ZoomLevel", "0", "Camera zoom: 0 = default, 1 = closest. Adjust in game with + and -.");
		settings.Set("Display", "AlwaysOnWatch", "False", "Always show the clock, without wearing a watch.");
		settings.Set("Display", "AlwaysOnDepth", "False", "Always show the depth, without wearing a depth meter.");
		settings.Set("Display", "AlwaysOnCompass", "False", "Always show the horizontal position, without wearing a compass.");
		settings.Set("Additions", "HardmodeAlert", "False", "Implements a pop-up alert once hardmode is activated within a world а la Pre-1.3 Terraria Mobile.\n; This basically just shows an introductory message about what hardmode is and what you can do from there.");
		settings.Set("Input", "KeyboardMouse", "True", "Enables desktop keyboard and mouse support for player one (WASD, space, mouse aim/use).\n; Set to False to get the original gamepad-only behaviour.");
		settings.Set("Cheats", "Enabled", "True", "Enables the single-player cheat menu (TerrariaXDK addition). Has no effect in multiplayer.");
		settings.Set("Network", "Port", "7777", "TCP/UDP port used when this game hosts a multiplayer world in-process.");
		settings.Set("Network", "Password", "", "Password clients must send to join this host.\n; Leave empty to accept anyone.");
		settings.Set("Network", "JoinAddress", "", "Optional 'host:port' to offer as a one-click join target in the netplay menu.\n; Leave empty to only browse LAN worlds.");
		// Observe by default: see AntiCheatMode for why refusing is not the default.
		settings.Set("Network", "AntiCheat", "Observe", "Movement validation for remote players while hosting: Off, Observe (log what would be refused) or Enforce (refuse it too).");
		settings.Set("Mods", "Enabled", "True", "Loads mods from the Mods folder next to the executable.");
		settings.Set("Mods", "RequireMatchingMods", "True", "Rejects multiplayer clients whose mod set differs from the host's.");
		settings.Set("Mods", "AllowDownload", "False", "Lets clients whose mod set differs fetch this host's mods automatically.\n; A mod is executable code: turn this on only for mods you are willing to give away.");
		settings.Set("Mods", "DownloadFromHost", "False", "Accepts mods from a host whose mod set differs, instead of being refused.\n; Joining a server is not agreement to run its code, so this is off by default.");
		Gamertag = settings.Get("Game", "Gamertag");
		IsTrial = settings.Get("Game", "TrialActive").ToLower().Equals("true");
		PSMode = settings.Get("Game", "PSMode").ToLower().Equals("true");
		TouchpadButton = PSMode && settings.Get("Game", "TouchpadButton").ToLower().Equals("true");
		UnlockAllRecipes = settings.Get("Game", "UnlockAllRecipes").ToLower().Equals("true");
		// Resolved now, applied after the splash: the settings file is closed by then.
		Terraria.CollectorsEdition.Load(settings);
		ScreenMultiplier = 1f;
		ResolutionHeight = 720;
		ResolutionWidth = 960;
		// Display configuration used to be declared in Settings.ini but never read: the game
		// was hard-wired to the 540p preset and a 640x480 backbuffer.
		Terraria.DisplaySettings.Load(settings);
		Terraria.DisplaySettings.ApplyRenderScale();
		// Language is read here and applied later, in LoadContent: SetLang fills the string
		// tables, and the culture-based default must have been installed first.
		Terraria.Language.Load(settings);
		settings.Set("Backups", "Enabled", "True", "Keep dated copies of the world in Storage/Backups.");
		settings.Set("Backups", "Keep", "10", "How many backups to keep per world (0-100).\n; Oldest are removed first; 0 disables automatic backups.");
		settings.Set("Backups", "MinMinutes", "15", "Minutes between automatic backups of the same world (0 = every save).");
		Terraria.WorldBackups.Load(settings);
		ShowFPS = settings.Get("Display", "ShowFPS").ToLower().Equals("true");
		StartFullscreen = settings.Get("Display", "StartFullscreen").ToLower().Equals("true");
		// Zoom and the HUD widgets live in HudSettings: the values used to be parsed into
		// fields that nothing applied, and the parse itself could throw on a bad value.
		Terraria.HudSettings.Load(settings);
		settings.Set("Display", "ZoomLevel", "0", "Camera zoom: 0 = default, 1 = closest. Adjust in game with + and -.");
		settings.Set("Display", "AlwaysOnWatch", "False", "Always show the clock, without wearing a watch.");
		settings.Set("Display", "AlwaysOnDepth", "False", "Always show the depth, without wearing a depth meter.");
		settings.Set("Display", "AlwaysOnCompass", "False", "Always show the horizontal position, without wearing a compass.");
		HardmodeAlert = settings.Get("Additions", "HardmodeAlert").ToLower().Equals("true");
		Terraria.Input.KeyboardMouseInput.Enabled = settings.Get("Input", "KeyboardMouse").ToLower().Equals("true");
		Terraria.Cheats.CheatState.Enabled = settings.Get("Cheats", "Enabled").ToLower().Equals("true");
		Terraria.Net.NetworkConfig.Port = ParsePort(settings.Get("Network", "Port"), 7777);
		Terraria.Net.NetworkConfig.JoinAddress = settings.Get("Network", "JoinAddress");
		Terraria.Net.NetworkConfig.Password = settings.Get("Network", "Password");
		Terraria.Net.NetworkConfig.PublicAddress = settings.Get("Network", "PublicAddress");
		Terraria.Net.NetworkConfig.Whitelist = settings.Get("Network", "Whitelist").ToLower().Equals("true");
		Terraria.Net.NetworkConfig.UseUPnP = settings.Get("Network", "UPnP").ToLower().Equals("true");
		Terraria.Net.NetworkConfig.AllowRelay = settings.Get("Network", "AllowRelay").ToLower().Equals("true");
		Terraria.Net.NetworkConfig.RelayServer = settings.Get("Network", "RelayServer");
		int relayPort;
		if (int.TryParse(settings.Get("Network", "RelayPort"), out relayPort) && relayPort > 0 && relayPort <= 65535)
		{
			Terraria.Net.NetworkConfig.RelayPort = relayPort;
		}
		Terraria.Net.MasterServerClient.Configure(settings);
		settings.Set("Network", "AllowRelay", Terraria.Net.NetworkConfig.AllowRelay ? "True" : "False", "Host through the master server's relay.\n; For players who cannot be reached from the outside at all (CGNAT, mobile, guest Wi-Fi).\n; Adds a hop of latency and uses the relay's bandwidth, so it is a fallback, not a mode.");
		settings.Set("Network", "RelayServer", Terraria.Net.NetworkConfig.RelayServer, "Relay address 'host:port'. Empty means the master's host on RelayPort.");
		settings.Set("Network", "RelayPort", Terraria.Net.NetworkConfig.RelayPort.ToString(), "Relay port used when RelayServer has no port of its own.");
		settings.Set("Network", "PublishToMaster", Terraria.Net.MasterServerClient.Publish ? "True" : "False", "List this world on the master server while hosting.\n; Off by default: publishing your address is a choice.");
		settings.Set("Network", "PublicAddress", Terraria.Net.NetworkConfig.PublicAddress, "Address advertised to the master server, e.g. 'myserver.example.com'.\n; Leave empty to let the master use the address it sees.");
		settings.Set("Network", "Whitelist", Terraria.Net.NetworkConfig.Whitelist ? "True" : "False", "Only players listed in Storage/Config/whitelist.txt may join.\n; An empty list while enabled refuses everybody - that is what a whitelist is.");
		settings.Set("Network", "UPnP", Terraria.Net.NetworkConfig.UseUPnP ? "True" : "False", "Ask the router to forward the game port over UPnP while hosting.\n; Without it, players outside your network can only join through the relay or a manual\n; port forward. Only the game's own port is touched, and the rule is removed when you stop.");
		Terraria.Net.MasterServerClient.Save(settings);
		settings.Set("Network", "Motd", "", "Message shown to every player who joins this host.");
		settings.Set("Network", "Rules", "", "Rules shown to every player who joins this host.");
		settings.Set("Network", "DisableBuild", "False", "While set, only the host can change the world.\n; An emergency switch for when you step away from a public server.");
		settings.Set("Network", "ProtectSpawnRadius", "16", "Tiles around the world spawn that remote players cannot modify (0 = no protection).");
		Terraria.Net.HostModeration.Load(settings);
		Terraria.Net.WorldProtection.Load(settings);
		// Read before the hosting profile so the profile can override it below.
		Terraria.Net.AntiCheat.Load(settings);
		// The optional hosting profile wins over Settings.ini for the keys it defines. Applied
		// after everything it can override, so a partial file is enough.
		settings.Set("Network", "MaxPlayers", "7", "How many remote players the host accepts (1-255).");
		Terraria.Net.ServerConfig.Apply();
		settings.Set("Network", "AutoRefreshSeconds", "20", "Seconds between automatic server list refreshes while the list is open.\n; 0 disables it; manual refresh is F5 / LB.");
		Terraria.Net.ServerListRefresh.Load(settings);
		Terraria.Input.KeyBindings.Load(settings);
		Terraria.Mods.ModLoader.Enabled = settings.Get("Mods", "Enabled").ToLower().Equals("true");
		Terraria.Mods.ModLoader.RequireMatchingMods = settings.Get("Mods", "RequireMatchingMods").ToLower().Equals("true");
		Terraria.Net.NetworkConfig.AllowDownload = settings.Get("Mods", "AllowDownload").ToLower().Equals("true");
		Terraria.Net.NetworkConfig.DownloadFromHost = settings.Get("Mods", "DownloadFromHost").ToLower().Equals("true");
		// Writes every rebindable action, so the file documents them even when untouched.
		Terraria.Input.KeyBindings.Save(settings);
		settings.Close();
		Program.Log("[MAIN] ⚡ Constructor exiting, about to be Initialize()'d");
	}

	protected override void Initialize()
	{
		Program.Log("[MAIN.Initialize()] ⬇\ufe0f START");
		try
		{
			Program.Log("[MAIN.Initialize()] Calling base.Initialize()...");
			base.Initialize();
			Program.Log("[MAIN.Initialize()] ✅ base.Initialize() DONE - Window created!");
		}
		catch (Exception ex)
		{
			Program.Log("[MAIN.Initialize()] \ud83d\udca5 base.Initialize() CRASHED: " + ex.Message);
			throw;
		}
		MenuTime.reset(86.4f);
		GameTime.reset(1f);
		NPC.ClearNames();
		NPC.SetNames();
		EntityID.TileID[] shine2Tiles = EntityID.Shine2Tiles;
		EntityID.TileID[] array = shine2Tiles;
		foreach (EntityID.TileID tileID in array)
		{
			TileShine2[(short)tileID] = true;
		}
		TileShine[6] = 1150;
		TileShine[7] = 1100;
		TileShine[8] = 1000;
		TileShine[9] = 1050;
		TileShine[12] = 1000;
		TileShine[21] = 1200;
		TileShine[22] = 1150;
		TileShine[45] = 1900;
		TileShine[46] = 2000;
		TileShine[47] = 2100;
		TileShine[63] = 900;
		TileShine[64] = 900;
		TileShine[65] = 900;
		TileShine[66] = 900;
		TileShine[67] = 900;
		TileShine[68] = 900;
		TileShine[107] = 950;
		TileShine[108] = 900;
		TileShine[109] = 9000;
		TileShine[110] = 9000;
		TileShine[111] = 850;
		TileShine[116] = 9000;
		TileShine[117] = 9000;
		TileShine[118] = 8000;
		TileShine[121] = 1850;
		TileShine[122] = 1800;
		TileShine[125] = 600;
		TileShine[129] = 300;
		shine2Tiles = EntityID.HammerTiles;
		EntityID.TileID[] array2 = shine2Tiles;
		foreach (EntityID.TileID tileID2 in array2)
		{
			TileHammer[(short)tileID2] = true;
		}
		shine2Tiles = EntityID.FrameImportantTiles;
		EntityID.TileID[] array3 = shine2Tiles;
		foreach (EntityID.TileID tileID3 in array3)
		{
			TileFrameImportant[(short)tileID3] = true;
		}
		shine2Tiles = EntityID.LightedTiles;
		EntityID.TileID[] array4 = shine2Tiles;
		foreach (EntityID.TileID tileID4 in array4)
		{
			TileLighted[(short)tileID4] = true;
		}
		shine2Tiles = EntityID.StoneTiles;
		EntityID.TileID[] array5 = shine2Tiles;
		foreach (EntityID.TileID tileID5 in array5)
		{
			TileStone[(short)tileID5] = true;
		}
		shine2Tiles = EntityID.BlockLightTiles;
		EntityID.TileID[] array6 = shine2Tiles;
		foreach (EntityID.TileID tileID6 in array6)
		{
			TileBlockLight[(short)tileID6] = true;
		}
		shine2Tiles = EntityID.SolidTiles;
		EntityID.TileID[] array7 = shine2Tiles;
		foreach (EntityID.TileID tileID7 in array7)
		{
			TileSolid[(short)tileID7] = true;
		}
		TileSolid[3] = false;
		TileSolid[4] = false;
		TileSolid[5] = false;
		TileSolid[11] = false;
		TileSolid[110] = false;
		shine2Tiles = EntityID.MergeDirtTiles;
		EntityID.TileID[] array8 = shine2Tiles;
		foreach (EntityID.TileID tileID8 in array8)
		{
			TileMergeDirt[(short)tileID8] = true;
		}
		shine2Tiles = EntityID.AxeTiles;
		EntityID.TileID[] array9 = shine2Tiles;
		foreach (EntityID.TileID tileID9 in array9)
		{
			TileAxe[(short)tileID9] = true;
		}
		shine2Tiles = EntityID.CutTiles;
		EntityID.TileID[] array10 = shine2Tiles;
		foreach (EntityID.TileID tileID10 in array10)
		{
			TileCut[(short)tileID10] = true;
		}
		shine2Tiles = EntityID.LavaDeathTiles;
		EntityID.TileID[] array11 = shine2Tiles;
		foreach (EntityID.TileID tileID11 in array11)
		{
			TileLavaDeath[(short)tileID11] = true;
		}
		shine2Tiles = EntityID.WaterDeathTiles;
		EntityID.TileID[] array12 = shine2Tiles;
		foreach (EntityID.TileID tileID12 in array12)
		{
			TileWaterDeath[(short)tileID12] = true;
		}
		shine2Tiles = EntityID.NoAttachTiles;
		EntityID.TileID[] array13 = shine2Tiles;
		foreach (EntityID.TileID tileID13 in array13)
		{
			TileNoAttach[(short)tileID13] = true;
		}
		shine2Tiles = EntityID.NoFailTiles;
		EntityID.TileID[] array14 = shine2Tiles;
		foreach (EntityID.TileID tileID14 in array14)
		{
			TileNoFail[(short)tileID14] = true;
		}
		shine2Tiles = EntityID.DungeonTiles;
		EntityID.TileID[] array15 = shine2Tiles;
		foreach (EntityID.TileID tileID15 in array15)
		{
			TileDungeon[(short)tileID15] = true;
		}
		shine2Tiles = EntityID.SolidTopTiles;
		EntityID.TileID[] array16 = shine2Tiles;
		foreach (EntityID.TileID tileID16 in array16)
		{
			TileSolidTop[(short)tileID16] = true;
		}
		shine2Tiles = EntityID.TableTiles;
		EntityID.TileID[] array17 = shine2Tiles;
		foreach (EntityID.TileID tileID17 in array17)
		{
			TileTable[(short)tileID17] = true;
		}
		EntityID.WallID[] houseWalls = EntityID.HouseWalls;
		EntityID.WallID[] array18 = houseWalls;
		foreach (EntityID.WallID wallID in array18)
		{
			WallHouse[(short)wallID] = true;
		}
		for (int num13 = 149; num13 >= 0; num13--)
		{
			TileSolidNotSolidTop[num13] = TileSolid[num13] & !TileSolidTop[num13];
			TileSolidAndAttach[num13] = TileSolid[num13] & !TileNoAttach[num13];
		}
		for (int num14 = 0; num14 < 150; num14++)
		{
			if (TileSolid[num14])
			{
				TileNoSunLight[num14] = true;
			}
		}
		TileNoSunLight[19] = false;
		TileNoSunLight[11] = true;
		DustSet.Init();
		for (int num15 = 0; num15 < 201; num15++)
		{
			ItemSet[num15].Init();
		}
		for (int num16 = 0; num16 < 197; num16++)
		{
			NPCSet[num16] = new NPC();
			NPCSet[num16].WhoAmI = (short)num16;
		}
		for (int num17 = 0; num17 < 9; num17++)
		{
			PlayerSet[num17] = new Player();
			PlayerSet[num17].WhoAmI = (byte)num17;
		}
		for (int num18 = 0; num18 < 512; num18++)
		{
			ProjectileSet[num18].Init();
		}
		for (int num19 = 0; num19 < 128; num19++)
		{
			GoreSet[num19].Init();
		}
		Cloud.Initialize();
		for (int num20 = 0; num20 < 32; num20++)
		{
			CombatTextSet[num20].Init();
		}
		Recipe.SetupRecipes();
		for (int num21 = 0; num21 < 7; num21++)
		{
			ChatLineSet[num21].Init();
		}
		ref Color reference = ref TeamColors[0];
		reference = Color.White;
		ref Color reference2 = ref TeamColors[1];
		reference2 = new Color(230, 40, 20);
		ref Color reference3 = ref TeamColors[2];
		reference3 = new Color(20, 200, 30);
		ref Color reference4 = ref TeamColors[3];
		reference4 = new Color(75, 90, 255);
		ref Color reference5 = ref TeamColors[4];
		reference5 = new Color(200, 180, 0);
		Projectile projectile = default;
		for (int num22 = 1; num22 < 120; num22++)
		{
			projectile.SetDefaults(num22);
			ProjHostile[num22] = projectile.hostile;
		}
		Program.Log("[MAIN.Initialize()] ⬆\ufe0f COMPLETE");
	}

	private void InitializePostSplash()
	{
		UI.Initialize(this);
		WorldView.Initialize(GraphicsDevice);
		for (int i = 0; i < UIInstance.Length; i++)
		{
			UIInstance[i] = new UI();
			if (i == 0)
			{
				UIInstance[i].SetUIView(WorldView.Type.FULLSCREEN, NoAutoFullScreen: true);
			}
			UIInstance[i].Initialize((PlayerIndex)i);
		}
		Item item = default;
		for (int num = 631; num > 0; num--)
		{
			item.SetDefaults(num);
			if (item.HeadSlot > 0)
			{
				Item.HeadType[item.HeadSlot] = item.Type;
			}
			else if (item.BodySlot > 0)
			{
				Item.BodyType[item.BodySlot] = item.Type;
			}
			else if (item.LegSlot > 0)
			{
				Item.LegType[item.LegSlot] = item.Type;
			}
		}
		for (int j = 0; j < 10; j++)
		{
			Shop[j] = new Chest();
			if (j > 0)
			{
				Shop[j].SetupShop(j);
			}
		}
		Star.SpawnStars();
		Projectile.Initialize();
		Components.Add(new GamerServicesComponent(this));
		SpecialCheck();
		if (ShowFPS)
		{
			FPSTimer.Start();
		}
		SignedInGamer.SignedIn += SignedInGamer_SignedIn;
		SignedInGamer.SignedOut += SignedInGamer_SignedOut;
		NetworkSession.InviteAccepted += Netplay.NetworkSession_InviteAccepted;
		Terraria.Input.ChatInput.Initialize();
		// Old tConfig packs dropped into Storage/tConfig are picked up first, as item overrides.
		//
		// Order matters: both a pack and a mod write into the same field table, and the last
		// writer wins. A pack is a legacy folder that takes no part in the mod signature and
		// cannot be required by a host; a mod is explicit, versioned and shared. So the pack is
		// applied first and the mod gets the final word on any field they both touch.
		Terraria.Mods.TConfigImporter.ImportIfPresent();
		Terraria.Mods.ModLoader.GraphicsDevice = GraphicsDevice;
		Terraria.Mods.ModLoader.Initialize();
	}

	protected override void LoadContent()
	{
		Program.LogSection("LOADING CONTENT");
		Program.Log("[MAIN.LoadContent()] Setting up GraphicsDevice states...");
		try
		{
			GraphicsDevice.DepthStencilState = DepthStencilState.None;
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
			Program.Log("[MAIN.LoadContent()] ✅ DepthStencil + Rasterizer set");
		}
		catch (Exception ex)
		{
			Program.Log("[MAIN.LoadContent()] \ud83d\udca5 GraphicsDevice state error: " + ex.Message);
		}
		Program.Log("[MAIN.LoadContent()] Setting resolution...");
		try
		{
			GraphicsGame.PreferredBackBufferWidth = Terraria.DisplaySettings.Width;
			GraphicsGame.PreferredBackBufferHeight = Terraria.DisplaySettings.Height;
			Program.Log($"[MAIN.LoadContent()] BackBuffer: {Terraria.DisplaySettings.Width}x{Terraria.DisplaySettings.Height}, FullScreen={Terraria.DisplaySettings.Fullscreen}");
			if (Terraria.DisplaySettings.Fullscreen)
			{
				GraphicsGame.IsFullScreen = true;
				Program.Log("[MAIN.LoadContent()] Fullscreen mode enabled");
			}
			GraphicsGame.ApplyChanges();
			Program.Log("[MAIN.LoadContent()] ✅ ApplyChanges() done");
		}
		catch (Exception ex2)
		{
			Program.Log("[MAIN.LoadContent()] \ud83d\udca5 Resolution/ApplyChanges error: " + ex2.Message);
		}
		try
		{
			// Vsync / frame cap: a handheld at 30 fps runs much cooler than one at 120.
			Terraria.DisplaySettings.ApplyFrameLimit(this, GraphicsGame);
			GraphicsGame.ApplyChanges();
			Program.Log($"[MAIN.LoadContent()] Frame limit: vsync={Terraria.DisplaySettings.VSync}, cap={(Terraria.DisplaySettings.FpsLimit > 0 ? Terraria.DisplaySettings.FpsLimit + " fps" : "off")}");
		}
		catch (Exception ex2b)
		{
			Program.LogWarn("Display", "could not apply the frame limit: " + ex2b.Message);
		}
		Program.Log($"[MAIN.LoadContent()] Creating RenderTarget {ResolutionWidth}x{ResolutionHeight}...");
		try
		{
			_renderTarget = new RenderTarget2D(GraphicsDevice, ResolutionWidth, ResolutionHeight, mipMap: false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
			Program.Log($"[MAIN.LoadContent()] ✅ RenderTarget created! Format={GraphicsDevice.PresentationParameters.BackBufferFormat}, Depth=Depth24");
		}
		catch (Exception ex3)
		{
			Program.Log("[MAIN.LoadContent()] \ud83d\udca5\ud83d\udca5\ud83d\udca5 RenderTarget CREATION FAILED: " + ex3.Message);
			Program.Log("[MAIN.LoadContent()] \ud83d\udca5 This is likely the HANG cause on Mali-G31!");
			throw;
		}
		Program.Log("[MAIN.LoadContent()] Detecting display mode...");
		try
		{
			IsHDTV = GraphicsGame.GraphicsDevice.Adapter.CurrentDisplayMode.Height >= 720;
			Program.Log($"[MAIN.LoadContent()] IsHDTV={IsHDTV} (display height={GraphicsGame.GraphicsDevice.Adapter.CurrentDisplayMode.Height})");
		}
		catch (Exception ex4)
		{
			Program.Log("[MAIN.LoadContent()] ⚠\ufe0f Display mode detection failed: " + ex4.Message);
			IsHDTV = false;
		}
		Program.Log("[MAIN.LoadContent()] Creating SpriteBatch...");
		try
		{
			SpriteBatch = new SpriteBatch(GraphicsDevice);
			Program.Log("[MAIN.LoadContent()] ✅ SpriteBatch created");
		}
		catch (Exception ex5)
		{
			Program.Log("[MAIN.LoadContent()] \ud83d\udca5 SpriteBatch failed: " + ex5.Message);
			throw;
		}
		Program.Log("[MAIN.LoadContent()] Loading splash screens...");
		try
		{
			string text = Lang.SetSystemLang();
			// A language chosen in the menu wins over the OS culture. Applied right after the
			// system default so the splash rating logos below are unaffected (they follow the
			// region, not the language), and so the tables are filled either way.
			Terraria.Language.ApplySaved();
			if (text != null)
			{
				SplashTextures[0] = Content.Load<Texture2D>(text);
				Program.Log("[MAIN.LoadContent()] ✅ Rating texture loaded: " + text);
			}
			else
			{
				SplashLogo = 1;
				SplashDelay = 120;
				Program.Log("[MAIN.LoadContent()] No rating, using default splash");
			}
			SplashTextures[1] = Content.Load<Texture2D>("Images/logo_1");
			Program.Log("[MAIN.LoadContent()] ✅ Logo_1 loaded");
		}
		catch (Exception ex6)
		{
			Program.Log("[MAIN.LoadContent()] \ud83d\udca5 Splash loading error: " + ex6.Message);
		}
		// The remaining splash logos are tiny (a few KB each) but they are GPU resources.
		// Loading them here, on the main thread, removes the race where the splash advances
		// to a logo that the loader thread has not produced yet.
		for (int i = 2; i <= 4; i++)
		{
			try
			{
				SplashTextures[i] = Content.Load<Texture2D>("Images/logo_" + i);
			}
			catch (Exception ex7)
			{
				Program.Log("[MAIN.LoadContent()] Splash logo_" + i + " failed: " + ex7.Message);
			}
		}
		Program.Log("[MAIN.LoadContent()] === STARTING LOADING THREAD (audio only) ===");
		ContentThread = new Thread(LoadingThread);
		ContentThread.Name = "ContentLoader";
		ContentThread.IsBackground = true;
		ContentThread.Start();
		Program.Log("[MAIN.LoadContent()] ✅ ContentThread started - GPU phases will run on the main thread");
		Program.Log("[MAIN.LoadContent()] ⬆\ufe0f === LOAD CONTENT COMPLETE ===");
	}

	/// <summary>
	/// Tells the input layer the current window/render sizes. Wrapped because
	/// <see cref="Game.GraphicsDevice"/> throws before the device exists, and a failure here must
	/// not take down the update loop: the pointer then keeps its previous mapping.
	/// </summary>
	private void UpdateInputViewport()
	{
		try
		{
			PresentationParameters presentationParameters = GraphicsDevice.PresentationParameters;
			Terraria.Input.KeyboardMouseInput.SetViewport(
				presentationParameters.BackBufferWidth,
				presentationParameters.BackBufferHeight,
				ResolutionWidth,
				ResolutionHeight);
		}
		catch
		{
			// No device yet (loading, shutdown): DisplaySettings still knows the window we asked
			// for, which is the right mapping for the frames that do have a device.
			Terraria.Input.KeyboardMouseInput.SetViewport(
				Terraria.DisplaySettings.Width, Terraria.DisplaySettings.Height,
				ResolutionWidth, ResolutionHeight);
		}
	}

	/// <summary>Queue of GPU loading steps, executed one per frame.</summary>
	private bool _loadTasksReady;

	private readonly System.Collections.Generic.List<LoadTask> _loadTasks = new System.Collections.Generic.List<LoadTask>();

	private int _loadTaskIndex;

	private float _loadTotalWeight = 1f;

	private float _loadDoneWeight;

	/// <summary>
	/// Builds the loading queue. Must be called on the main thread: every task creates GPU
	/// resources, and FNA's GL context is current only here.
	/// </summary>
	private void BuildLoadTasks()
	{
		_loadTasks.Clear();
		_loadTaskIndex = 0;
		_loadDoneWeight = 0f;
		_loadTasks.Add(new LoadTask("tiles", 30f, delegate { _sheetTiles.LoadContent(Content); }));
		_loadTasks.Add(new LoadTask("sprites", 24f, delegate { _sheetSprites.LoadContent(Content); }));
		// Backgrounds are separate files: one task each keeps the bar moving.
		for (int i = 3; i < 32; i++)
		{
			int index = i;
			_loadTasks.Add(new LoadTask("background " + index, 0.8f, delegate
			{
				WorldView.LoadBackground(Content, index);
			}));
		}
		_loadTasks.Add(new LoadTask("buffers", 0.2f, delegate
		{
			WhiteTexture = new Texture2D(GraphicsDevice, 1, 1, mipMap: false, SurfaceFormat.Color);
			WhiteTexture.SetData(new Color[1] { Color.White });
		}));
		UI.AppendLoadTasks(_loadTasks, Content);
		_loadTasks.Add(new LoadTask("finalizing", 0.5f, delegate { CRC32.Initialize(); }));
		_loadTotalWeight = 0f;
		for (int j = 0; j < _loadTasks.Count; j++)
		{
			_loadTotalWeight += _loadTasks[j].Weight;
		}
		Program.LogInfo("Loading", $"{_loadTasks.Count} load step(s), weight {_loadTotalWeight:0.0}");
	}

	/// <summary>
	/// Runs the next loading step. Returns true when the queue is finished, so the caller can
	/// move on to <see cref="InitializePostSplash"/>.
	/// </summary>
	private bool RunNextLoadTask()
	{
		if (_loadTaskIndex >= _loadTasks.Count)
		{
			return true;
		}
		LoadTask loadTask = _loadTasks[_loadTaskIndex];
		_loadTaskIndex++;
		_loadingStatusText = loadTask.Label;
		try
		{
			loadTask.Run();
		}
		catch (Exception ex)
		{
			// One missing asset must not abort the whole loading screen; report and continue.
			Program.LogError("Loading", "step '" + loadTask.Label + "' failed: " + ex.Message);
		}
		_loadDoneWeight += loadTask.Weight;
		float num = _loadDoneWeight / Math.Max(1f, _loadTotalWeight);
		if (num > 1f)
		{
			num = 1f;
		}
		float num2 = 0.3f + 0.66f * num;
		if (num2 > _loadingProgress)
		{
			_loadingProgress = num2;
		}
		return false;
	}

	/// <summary>
	/// Background part of loading: audio only. Everything that touches the GraphicsDevice is
	/// done by <see cref="LoadGpuContent"/> on the main thread.
	/// Running one half off-thread keeps the splash responsive while ~80 MB of music and
	/// ~100 sound effects are decoded.
	/// </summary>
	private void LoadingThread()
	{
		Program.LogSection("LOADING AUDIO");
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		try
		{
			_loadingStatusText = "Loading music...";
			// Music is the first 20% of the bar; report per track so it advances smoothly.
			SoundManager.Progress = delegate(int done, int total)
			{
				_loadingProgress = 0.02f + 0.2f * ((float)done / (float)Math.Max(1, total));
			};
			UsedManager = new SoundManager(MusicCueNames);
			SoundManager.Progress = null;
			_loadingProgress = 0.22f;
			Program.LogInfo("Loading", $"music ready ({stopwatch.ElapsedMilliseconds} ms)");
			_loadingStatusText = "Loading sounds...";
			LoadAllSfxPools(stopwatch);
			_loadingProgress = 0.3f;
			Program.LogInfo("Loading", $"sound effects ready ({stopwatch.ElapsedMilliseconds} ms)");
		}
		catch (Exception ex)
		{
			Program.LogError("Loading", "audio loading failed: " + ex.Message);
		}
		stopwatch.Stop();
		Program.LogInfo("Loading", $"audio stage finished in {stopwatch.ElapsedMilliseconds} ms");
	}

	private void LoadAllSfxPools(Stopwatch sw)
	{
		SoundMech = new SfxInstancePool(Content, "Sounds/Mech_0", 3);
		SoundGrab = new SfxInstancePool(Content, "Sounds/Grab", 3);
		SoundPixie = new SfxInstancePool(Content, "Sounds/Pixie", 2);
		SoundDig[0] = new SfxInstancePool(Content, "Sounds/Dig_0", 3);
		SoundDig[1] = new SfxInstancePool(Content, "Sounds/Dig_1", 3);
		SoundDig[2] = new SfxInstancePool(Content, "Sounds/Dig_2", 3);
		SoundTink[0] = new SfxInstancePool(Content, "Sounds/Tink_0", 3);
		SoundTink[1] = new SfxInstancePool(Content, "Sounds/Tink_1", 3);
		SoundTink[2] = new SfxInstancePool(Content, "Sounds/Tink_2", 3);
		SoundPlayerHit[0] = new SfxInstancePool(Content, "Sounds/Player_Hit_0", 3);
		SoundPlayerHit[1] = new SfxInstancePool(Content, "Sounds/Player_Hit_1", 3);
		SoundPlayerHit[2] = new SfxInstancePool(Content, "Sounds/Player_Hit_2", 3);
		SoundFemaleHit[0] = new SfxInstancePool(Content, "Sounds/Female_Hit_0", 3);
		SoundFemaleHit[1] = new SfxInstancePool(Content, "Sounds/Female_Hit_1", 3);
		SoundFemaleHit[2] = new SfxInstancePool(Content, "Sounds/Female_Hit_2", 3);
		SoundPlayerKilled = new SfxInstancePool(Content, "Sounds/Player_Killed", 3);
		SoundChat = new SfxInstancePool(Content, "Sounds/Chat", 2);
		SoundGrass = new SfxInstancePool(Content, "Sounds/Grass", 6);
		SoundDoorOpen = new SfxInstancePool(Content, "Sounds/Door_Opened", 3);
		SoundDoorClosed = new SfxInstancePool(Content, "Sounds/Door_Closed", 3);
		SoundMenuTick = new SfxInstancePool(Content, "Sounds/Menu_Tick", 3);
		SoundMenuOpen = new SfxInstancePool(Content, "Sounds/Menu_Open", 3);
		SoundMenuClose = new SfxInstancePool(Content, "Sounds/Menu_Close", 3);
		SoundShatter = new SfxInstancePool(Content, "Sounds/Shatter", 4);
		SoundZombie[0] = new SfxInstancePool(Content, "Sounds/Zombie_0", 4);
		SoundZombie[1] = new SfxInstancePool(Content, "Sounds/Zombie_1", 4);
		SoundZombie[2] = new SfxInstancePool(Content, "Sounds/Zombie_2", 4);
		SoundZombie[3] = new SfxInstancePool(Content, "Sounds/Zombie_3", 4);
		SoundZombie[4] = new SfxInstancePool(Content, "Sounds/Zombie_4", 4);
		SoundRoar[0] = new SfxInstancePool(Content, "Sounds/Roar_0", 2);
		SoundRoar[1] = new SfxInstancePool(Content, "Sounds/Roar_1", 2);
		SoundSplash[0] = new SfxInstancePool(Content, "Sounds/Splash_0", 4);
		SoundSplash[1] = new SfxInstancePool(Content, "Sounds/Splash_1", 4);
		SoundDoubleJump = new SfxInstancePool(Content, "Sounds/Double_Jump", 3);
		SoundRun = new SfxInstancePool(Content, "Sounds/Run", 7);
		SoundCoins = new SfxInstancePool(Content, "Sounds/Coins", 4);
		SoundUnlock = new SfxInstancePool(Content, "Sounds/Unlock", 4);
		SoundMaxMana = new SfxInstancePool(Content, "Sounds/MaxMana", 4);
		SoundDrown = new SfxInstancePool(Content, "Sounds/Drown", 4);
		for (int i = 0; i < 37; i++)
		{
			int num = i + 1;
			int maxInstances = 3;
			if (num != 9 && num != 10 && num != 24 && num != 26 && num != 34)
			{
				maxInstances = 2;
			}
			if (i % 10 == 0)
			{
				Program.Log($"[LoadingThread]     Item {i + 1}/37...");
			}
			SoundItem[i] = new SfxInstancePool(Content, "Sounds/Item_" + num, maxInstances);
		}
		for (int j = 0; j < 11; j++)
		{
			SoundNPCHit[j] = new SfxInstancePool(Content, "Sounds/NPC_Hit_" + (j + 1), 4);
		}
		for (int k = 0; k < 15; k++)
		{
			SoundNPCKilled[k] = new SfxInstancePool(Content, "Sounds/NPC_Killed_" + (k + 1), 3);
		}
	}

	protected override void UnloadContent()
	{
	}

	private void UpdateMusic(Player MainPlayer)
	{
		try
		{
			if (CurrentMusic != Music.NUM_SONGS && UsedManager.CheckState(MusicCueNames[(uint)CurrentMusic], SoundState.Paused))
			{
				UsedManager.Resume(MusicCueNames[(uint)CurrentMusic]);
			}
			if (MusicVolume == 0f)
			{
				NewMusic = Music.NUM_SONGS;
			}
			else
			{
				bool flag = true;
				for (int i = 0; i < UIInstance.Length; i++)
				{
					if (UIInstance[i].CurMenuType != MenuType.MAIN)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					if (UI.MainUI.CurMenuMode == MenuMode.CREDITS)
					{
						NewMusic = Music.TUTORIAL;
					}
					else
					{
						NewMusic = Music.TITLE;
					}
				}
				else if (IsTutorial())
				{
					NewMusic = Music.TUTORIAL;
				}
				else
				{
					int num = 0;
					int num2 = 10000;
					bool result = false;
					Rectangle value = new Rectangle
					{
						Width = num2,
						Height = num2
					};
					WorldView currentView = MainPlayer.CurrentView;
					for (int j = 0; j < 196; j++)
					{
						if (NPCSet[j].Active == 0)
						{
							continue;
						}
						int type = NPCSet[j].Type;
						switch (type)
						{
						case 134:
						case 143:
						case 144:
						case 145:
							value.X = NPCSet[j].XYWH.X + (NPCSet[j].Width >> 1) - num2 / 2;
							value.Y = NPCSet[j].XYWH.Y + (NPCSet[j].Height >> 1) - num2 / 2;
							currentView.ViewArea.Intersects(ref value, out result);
							if (!result)
							{
								continue;
							}
							num = 3;
							break;
						case 113:
						case 114:
						case 125:
						case 126:
							value.X = NPCSet[j].XYWH.X + (NPCSet[j].Width >> 1) - num2 / 2;
							value.Y = NPCSet[j].XYWH.Y + (NPCSet[j].Height >> 1) - num2 / 2;
							currentView.ViewArea.Intersects(ref value, out result);
							if (!result)
							{
								continue;
							}
							num = 2;
							break;
						case 4:
						case 166:
							value.X = NPCSet[j].XYWH.X + (NPCSet[j].Width >> 1) - num2 / 2;
							value.Y = NPCSet[j].XYWH.Y + (NPCSet[j].Height >> 1) - num2 / 2;
							currentView.ViewArea.Intersects(ref value, out result);
							if (!result)
							{
								continue;
							}
							num = 4;
							break;
						default:
							if (!NPCSet[j].IsBoss && (type < 13 || type > 15) && (type < 26 || type > 29) && type != 111)
							{
								continue;
							}
							value.X = NPCSet[j].XYWH.X + (NPCSet[j].Width >> 1) - num2 / 2;
							value.Y = NPCSet[j].XYWH.Y + (NPCSet[j].Height >> 1) - num2 / 2;
							currentView.ViewArea.Intersects(ref value, out result);
							if (!result)
							{
								continue;
							}
							num = 1;
							break;
						}
						break;
					}
					if (num > 0)
					{
						switch (num)
						{
						case 1:
							NewMusic = Music.BOSS1;
							break;
						case 2:
							NewMusic = Music.BOSS2;
							break;
						case 3:
							NewMusic = Music.BOSS3;
							break;
						case 4:
							NewMusic = Music.BOSS4;
							break;
						}
					}
					else if (currentView.ScreenPosition.Y > MaxTilesY - 200 << 4)
					{
						NewMusic = Music.EERIE;
					}
					else if (MainPlayer.ZoneEvil)
					{
						if (currentView.ScreenPosition.Y > WorldSurfacePixels + ResolutionHeight)
						{
							NewMusic = Music.UNDERGROUND_CORRUPTION;
						}
						else
						{
							NewMusic = Music.CORRUPTION;
						}
					}
					else if (currentView.AtmoOpacity < 1f)
					{
						NewMusic = Music.FLOATING_ISLAND;
					}
					else if ((currentView.ScreenPosition.X < 3200 || currentView.ScreenPosition.X > (MaxTilesX - 200 << 4) - ResolutionWidth) && currentView.ScreenPosition.Y <= WorldSurfacePixels)
					{
						NewMusic = Music.OCEAN;
					}
					else if (MainPlayer.ZoneMeteor || MainPlayer.ZoneDungeon)
					{
						NewMusic = Music.EERIE;
					}
					else if (MainPlayer.ZoneJungle)
					{
						NewMusic = Music.JUNGLE;
					}
					else if (MainPlayer.CurrentView.SandTiles > 1000)
					{
						NewMusic = Music.DESERT;
					}
					else if (MainPlayer.CurrentView.SnowTiles > 80)
					{
						NewMusic = Music.SNOW;
					}
					else if (currentView.ScreenPosition.Y > WorldSurfacePixels)
					{
						if (MainPlayer.zoneHoly)
						{
							NewMusic = Music.UNDERGROUND_HALLOW;
						}
						else
						{
							NewMusic = Music.UNDERGROUND;
						}
					}
					else if (GameTime.DayTime)
					{
						if (MainPlayer.zoneHoly)
						{
							NewMusic = Music.THE_HALLOW;
						}
						else
						{
							NewMusic = Music.OVERWORLD_DAY;
						}
					}
					else if (!GameTime.DayTime)
					{
						if (GameTime.IsBloodMoon)
						{
							NewMusic = Music.EERIE;
						}
						else
						{
							NewMusic = Music.OVERWORLD_NIGHT;
						}
					}
					int num3 = 0;
					while (MusicBox < 0 && num3 < UIInstance.Length)
					{
						if (UIInstance[num3].CurrentView != null)
						{
							MusicBox = UIInstance[num3].CurrentView.MusicBox;
						}
						num3++;
					}
					if (MusicBox >= 0)
					{
						NewMusic = MusicBoxToSong[MusicBox];
					}
				}
			}
			CurrentMusic = NewMusic;
			for (int k = 0; k < 19; k++)
			{
				if (k == (int)CurrentMusic)
				{
					if (!UsedManager.CheckState(MusicCueNames[k], SoundState.Playing))
					{
						UsedManager.Play(MusicCueNames[k]);
					}
					else
					{
						MusicFade[k] += 0.005f;
						if (MusicFade[k] > 1f)
						{
							MusicFade[k] = 1f;
						}
					}
					UsedManager.SetVolume(MusicCueNames[k], MusicFade[k] * MusicVolume);
				}
				else if (UsedManager.CheckState(MusicCueNames[k], SoundState.Playing))
				{
					if (CurrentMusic == Music.NUM_SONGS)
					{
						MusicFade[k] = 0f;
						UsedManager.Stop(MusicCueNames[k]);
					}
					else if (MusicFade[(uint)CurrentMusic] > 0.25f)
					{
						MusicFade[k] -= 0.005f;
						if (MusicFade[k] <= 0f)
						{
							MusicFade[k] = 0f;
							UsedManager.Stop(MusicCueNames[k]);
						}
						else
						{
							UsedManager.SetVolume(MusicCueNames[k], MusicFade[k] * MusicVolume);
						}
					}
				}
				else
				{
					MusicFade[k] = 0f;
				}
			}
		}
		catch
		{
			MusicVolume = 0f;
		}
	}

	private static void UpdateMenuTime()
	{
		MenuTime.Update();
		if (NetMode == 1)
		{
			UpdateTime();
		}
	}

	private void DrawSplash(GameTime GTime)
	{
		GraphicsDevice.Clear(default);
		base.Draw(GTime);
		// A missing logo means the asset failed to load (or is still being produced).
		// Hold on the bar instead of dereferencing null - on slow storage the old code
		// crashed here, which looked like "does not start sometimes".
		if (SplashLogo < 0 || SplashLogo >= SplashTextures.Length || SplashTextures[SplashLogo] == null)
		{
			while (SplashLogo < SplashTextures.Length && SplashTextures[SplashLogo] == null)
			{
				SplashLogo++;
			}
			if (SplashLogo >= SplashTextures.Length)
			{
				ShowSplash = 1;
			}
			return;
		}
		if (SplashCounter == SplashDelay + 16 + 16)
		{
			SplashTextures[SplashLogo].Dispose();
			SplashTextures[SplashLogo] = null;
			SplashDelay = 120;
			SplashCounter = 0;
			if (++SplashLogo == 5)
			{
				ShowSplash = 1;
				return;
			}
		}
		SplashCounter++;
		SpriteBatch.Begin();
		int num;
		if (SplashCounter < 16)
		{
			num = SplashCounter * 255 / 16;
		}
		else if (SplashCounter <= 16 + SplashDelay)
		{
			num = 255;
		}
		else
		{
			num = SplashCounter - SplashDelay - 16;
			num = 255 - num * 255 / 16;
		}
		Vector2 position = new Vector2
		{
			X = ResolutionWidth - SplashTextures[SplashLogo].Width >> 1,
			Y = ResolutionHeight - SplashTextures[SplashLogo].Height >> 1
		};
		SpriteBatch.Draw(SplashTextures[SplashLogo], position, new Color(num, num, num, num));
		SpriteBatch.End();
		DrawLoadingBar();
	}

	/// <summary>
	/// Progress bar shown while content is still being loaded.
	/// Visual: blue fill on a dark track, white outline and a drop shadow offset by 6px -
	/// kept exactly as designed. Only the animation maths changed, so the fill reaches 100%.
	/// </summary>
	private void DrawLoadingBar()
	{
		_displayProgress = LoadingProgress.Step(_displayProgress, _loadingProgress);
		if (_pixelTexture == null)
		{
			_pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
			_pixelTexture.SetData(new Color[1] { Color.White });
		}
		int num = 300;
		int num2 = 16;
		int num3 = (ResolutionWidth - num) / 2;
		int num4 = ResolutionHeight - 60;
		Color color = new Color(0, 136, 255);
		Color color2 = new Color(40, 40, 40, 255);
		Color color3 = new Color(20, 20, 20);
		int num5 = num3 - 2;
		int num6 = num4 - 2;
		int width = num + 4;
		int height = num2 + 4;
		SpriteBatch.Begin();
		// Drop shadow, then white outline, then the dark track.
		SpriteBatch.Draw(_pixelTexture, new Rectangle(num5 + 6, num6 + 6, width, height), color2);
		SpriteBatch.Draw(_pixelTexture, new Rectangle(num5, num6, width, height), Color.White);
		SpriteBatch.Draw(_pixelTexture, new Rectangle(num3, num4, num, num2), color3);
		int num7 = (int)((float)num * _displayProgress);
		if (num7 > 0)
		{
			SpriteBatch.Draw(_pixelTexture, new Rectangle(num3, num4, num7, num2), color);
		}
		SpriteBatch.End();
	}

	public void LoadUpsell()
	{
		if (!HasUpsellLoaded)
		{
			HasUpsellLoaded = true;
			for (int i = 0; i < 1; i++)
			{
				SplashTextures[i] = Content.Load<Texture2D>("UI/Upsell/0" + (i + 1) + "_" + Lang.LanguageId);
			}
		}
		SplashLogo = 0;
		SplashCounter = 0;
	}

	public void DrawUpsell()
	{
		SplashCounter++;
		int num = ((SplashCounter >= 16) ? 255 : (SplashCounter * 255 / 16));
		SpriteBatch.Draw(SplashTextures[SplashLogo], new Rectangle(0, 0, ResolutionWidth, ResolutionHeight), new Color(num, num, num, num));
	}

	public void DrawFPS()
	{
		bool flag = NetMode != 0 && UI.CurrentUI != null && UI.CurrentUI.CurrentView != null;
		string activeLabel = Terraria.Cheats.CheatState.ActiveLabel;
		if (!ShowFPS && !flag && activeLabel == null)
		{
			return;
		}
		if (UI.CurrentUI == null || UI.CurrentUI.CurrentView == null)
		{
			return;
		}
		int num = ResolutionWidth - UI.CurrentUI.CurrentView.SafeAreaOffsetLeft;
		int safeAreaOffsetTop = UI.CurrentUI.CurrentView.SafeAreaOffsetTop;
		float num2 = (UI.NumActiveViews <= 1) ? 1 : 2;
		float num3 = 0f;
		if (ShowFPS)
		{
			Vector2 vector = UI.BoldSmallFont.MeasureString("FPS: " + FPS);
			SpriteBatch.DrawString(UI.BoldSmallFont, "FPS: " + FPS, new Vector2((float)num - vector.X, safeAreaOffsetTop), Color.White, 0f, new Vector2(0f, vector.Y), num2, SpriteEffects.None, 0f);
			num3 = UI.BoldSmallFont.MeasureString("FPS").Y * num2;
		}
		if (flag)
		{
			// Connection diagnostics: latency and throughput, always visible while online so
			// a bad connection is obvious instead of just "feels laggy".
			string text = Terraria.Net.NetworkStats.Describe();
			Vector2 vector2 = UI.BoldSmallFont.MeasureString(text);
			int rtt = Terraria.Net.NetworkStats.RttMs;
			Color color = Color.White;
			if (rtt >= 0 && rtt > 150)
			{
				color = new Color(255, 180, 80);
			}
			if (rtt >= 0 && rtt > 300)
			{
				color = new Color(255, 110, 110);
			}
			SpriteBatch.DrawString(UI.BoldSmallFont, text, new Vector2((float)num - vector2.X * num2, safeAreaOffsetTop + num3), color, 0f, new Vector2(0f, vector2.Y), num2, SpriteEffects.None, 0f);
			num3 += UI.BoldSmallFont.MeasureString(text).Y * num2;
		}
		// Cheat badge: a world played with cheats should never look like a fair one,
		// especially in screenshots.
		if (activeLabel != null)
		{
			Vector2 vector3 = UI.BoldSmallFont.MeasureString(activeLabel);
			Color color2 = Terraria.Cheats.CheatState.IsAvailable ? new Color(255, 210, 90) : new Color(160, 160, 160);
			SpriteBatch.DrawString(UI.BoldSmallFont, activeLabel, new Vector2((float)num - vector3.X * num2, safeAreaOffsetTop + num3), color2, 0f, new Vector2(0f, vector3.Y), num2, SpriteEffects.None, 0f);
		}
	}

	protected override void Update(GameTime GTime)
	{
		_updateCount++;
		if (_updateCount <= 20 || _updateCount % 500 == 0)
		{
			Program.Log($"[Update] #{_updateCount} time={GTime.TotalGameTime.TotalSeconds:F3}s slow={GTime.IsRunningSlowly} splash={ShowSplash}");
		}
		if (_updateCount % 60 == 0)
		{
			long num = GC.GetTotalMemory(forceFullCollection: false) / 1024 / 1024;
			Program.LogDebug("Memory", $"managed heap {num} MB");
		}
		if (_updateCount == 1)
		{
			Program.LogInfo("Update", $"first frame: splash={ShowSplash}, audioThread={ContentThread != null}");
		}
		try
		{
			base.Update(GTime);
		}
		catch (Exception ex)
		{
			Program.Log("[Update] \ud83d\udca5 base.Update() exception: " + ex.Message);
		}
		IsRunningSlowly = GTime.IsRunningSlowly;
		FrameCounter++;
		switch (ShowSplash)
		{
		case 0:
			if (_updateCount <= 5 || _updateCount % 100 == 0)
			{
				Program.Log("[Update] ShowSplash=0 (showing splash screen)");
			}
			return;
		case 1:
			try
			{
				// Phase A: wait for the audio loader. Poll instead of blocking so the splash
				// keeps animating; Join(0) is a non-blocking "has it finished?" check.
				if (ContentThread != null)
				{
					if (!ContentThread.Join(0))
					{
						return;
					}
					ContentThread = null;
					_loadingProgress = 0.32f;
					_loadingStatusText = "Loading graphics...";
					// One collection before the largest allocation burst of the run.
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}
				// Phase B: one loading step per frame, so the bar advances continuously.
				if (!_loadTasksReady)
				{
					BuildLoadTasks();
					_loadTasksReady = true;
					return;
				}
				if (!RunNextLoadTask())
				{
					return;
				}
				// Phase C: hold at 100% until the bar visually catches up, then start.
				_loadingProgress = 1f;
				_loadingStatusText = "Starting...";
				if (_displayProgress < 0.999f)
				{
					// DrawLoadingBar advances the animation; just wait for it this frame.
					return;
				}
				GC.Collect();
				ShowSplash = 2;
				Program.LogSection("CONTENT READY");
				InitializePostSplash();
				return;
			}
			catch (Exception ex2)
			{
				Program.LogError("Loading", "Splash transition failed: " + ex2.Message);
				ShowSplash = 2;
				return;
			}
		}
		if (_updateCount == 0)
		{
			Program.Log("[Update] \ud83c\udfae Entered normal game loop for first time!");
		}
		// Focus must be current before input is read; it used to be assigned further down,
		// so the first frame after focusing the window dropped all input.
		HasFocus = IsActive;
		// Window pixels -> internal resolution, before anything reads the pointer. The game draws
		// into a RenderTarget of ResolutionWidth x ResolutionHeight and stretches it to the
		// backbuffer; without this the pointer only reached part of the screen at other render
		// scales (1080p pointed at two thirds, 540p could not reach the right edge).
		UpdateInputViewport();
		Terraria.Input.KeyboardMouseInput.Update();
		// Pad state is refreshed before anything reads it. Chat and the on-screen keyboard both
		// look at buttons, and they ran before this loop, so they were reading last frame's
		// state - one frame of lag on every button press, and a press that could be missed
		// entirely when a screen changed in between.
		for (int i = 0; i < UIInstance.Length; i++)
		{
			UIInstance[i].UpdateGamePad();
		}
		// Chat runs before anything reads input, so typing never moves the player.
		Terraria.Input.ChatInput.Update();
		// On-screen keyboard for gamepad-only setups; swallows input while open.
		Terraria.Input.VirtualKeyboard.Update();
		if (TutorialState < Tutorial.THE_END)
		{
			UpdateTutorial();
		}
		Terraria.Cheats.CheatState.Update();
		Terraria.Mods.ModLoader.GameUpdate();
		for (int j = 0; j < UIInstance.Length; j++)
		{
			UIInstance[j].Update();
		}
		DustSet.UpdateDust();
		if (UI.HasQuit)
		{
			Program.Log("[Update] Quit requested");
			Quit();
			return;
		}
		UI.UpdateOnce();
		AchievementSystem.Update();
		if (!SoundResponse)
		{
			SoundResponse = UsedManager.MatchCounted(Window.Handle);
		}
		CheckKeyStates();
		WorldGen.ToDestroyObject = false;
		UpdateMusic(UI.MainUI.ActivePlayer);
		HasFocus = IsActive;
		IsGamePaused = !HasFocus && NetMode == 0;
		if (IsGamePaused)
		{
			return;
		}
		if (ShowFPS && FPSTimer.ElapsedMilliseconds >= 1000)
		{
			FPS = FPSCount;
			FPSCount = 0;
			FPSTimer.Restart();
		}
		if (HasKeyBeenPressed(Keys.F))
		{
			GraphicsGame.ToggleFullScreen();
		}
#if DEBUG
		// Debug-only developer console. F12 rather than a printable key so it cannot collide with
		// typing in chat, and it is not in Release at all (the call itself is compiled out).
		if (HasKeyBeenPressed(Keys.F12))
		{
			if (Terraria.Cheats.DebugConsole.IsOpen)
			{
				Terraria.Cheats.DebugConsole.Close();
			}
			else
			{
				Terraria.Cheats.DebugConsole.Open();
			}
		}
		// Commands typed in the console run here, on the game thread.
		Terraria.Cheats.DebugConsole.Drain();
#endif
		if (IsGameStarted)
		{
			// Zoom lives in HudSettings, so the value shown in the menu, the keys and what the
			// camera actually does can never drift apart.
			if (CurrentKeyState.IsKeyDown(Keys.OemPlus))
			{
				Terraria.HudSettings.AdjustZoom(1);
			}
			if (CurrentKeyState.IsKeyDown(Keys.OemMinus))
			{
				Terraria.HudSettings.AdjustZoom(-1);
			}
			ZoomLevel = Terraria.HudSettings.Zoom;
			if (UI.MainUI.CurMenuMode == MenuMode.PAUSE)
			{
				if (UI.MainUI.PadState.IsButtonDown(Buttons.RightTrigger))
				{
					Terraria.HudSettings.AdjustZoom(1);
				}
				if (UI.MainUI.PadState.IsButtonDown(Buttons.LeftTrigger))
				{
					Terraria.HudSettings.AdjustZoom(-1);
				}
			}
		}
		if (Netplay.Session != null)
		{
			if (!Netplay.PlayDisconnect)
			{
				if (Netplay.HasHookEvents)
				{
					Netplay.HookSessionEvents();
				}
				if (NetMode == 1)
				{
					UpdateClient();
				}
				else
				{
					UpdateServer();
				}
			}
			else if (Netplay.ToStopSession)
			{
				Netplay.Disconnect();
			}
		}
		if (Netplay.CurInvite != null)
		{
			Netplay.InviteAccepted();
		}
		if (NetMode == 0)
		{
			if (UI.MainUI.CurMenuType == MenuType.PAUSE)
			{
				bool flag2 = true;
				for (int k = 0; k < UIInstance.Length; k++)
				{
					if (UIInstance[k].CurMenuType == MenuType.NONE)
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					IsGamePaused = true;
					return;
				}
			}
		}
		else if (CheckWorldId)
		{
			for (int l = 0; l < UIInstance.Length; l++)
			{
				UI uI = UIInstance[l];
				if (uI.CurrentView != null && uI.CurMenuType == MenuType.NONE)
				{
					CheckWorldId = false;
					if (uI.CheckBlacklist())
					{
						break;
					}
				}
			}
		}
		Star.UpdateStars();
		Cloud.UpdateClouds();
		for (int m = 0; m < 7; m++)
		{
			if (ChatLineSet[m].ShowTime > 0)
			{
				ChatLineSet[m].ShowTime--;
			}
		}
		for (int n = 0; n < UIInstance.Length; n++)
		{
			if (UIInstance[n].CurrentView != null && UIInstance[n].CurMenuType == MenuType.MAIN)
			{
				UpdateMenuTime();
				break;
			}
		}
		if (!IsGameStarted)
		{
			return;
		}
		if (NetMode != 0 && CheckUserGeneratedContent)
		{
			CheckUserGeneratedContent = false;
			UI.MainUI.CheckUserGeneratedContent();
		}
		if (DiscoStyle == 0)
		{
			DiscoRGB.Y += 7f / 255f;
			if (DiscoRGB.Y >= 1f)
			{
				DiscoRGB.Y = 1f;
				DiscoStyle = 1;
			}
			DiscoRGB.X -= 7f / 255f;
			if (DiscoRGB.X < 0f)
			{
				DiscoRGB.X = 0f;
			}
		}
		else if (DiscoStyle == 1)
		{
			DiscoRGB.Z += 7f / 255f;
			if (DiscoRGB.Z >= 1f)
			{
				DiscoRGB.Z = 1f;
				DiscoStyle = 2;
			}
			DiscoRGB.Y -= 7f / 255f;
			if (DiscoRGB.Y < 0f)
			{
				DiscoRGB.Y = 0f;
			}
		}
		else
		{
			DiscoRGB.X += 7f / 255f;
			if (DiscoRGB.X >= 1f)
			{
				DiscoRGB.X = 1f;
				DiscoStyle = 0;
			}
			DiscoRGB.Z -= 7f / 255f;
			if (DiscoRGB.Z < 0f)
			{
				DiscoRGB.Z = 0f;
			}
		}
		if ((FrameCounter & 7) == 0 && ++MagmaBGFrame >= 3)
		{
			MagmaBGFrame = 0;
		}
		DemonTorch += DemonTorchDir;
		if (DemonTorch > 1f)
		{
			DemonTorch = 1f;
			DemonTorchDir = 0f - DemonTorchDir;
		}
		else if (DemonTorch < 0f)
		{
			DemonTorch = 0f;
			DemonTorchDir = 0f - DemonTorchDir;
		}
		if (NetMode != 1)
		{
			if (_updateCount == 1000)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
			WorldGen.UpdateWorld();
			UpdateInvasion();
		}
		MusicBox = -1;
		for (int num2 = 0; num2 < 8; num2++)
		{
			if (PlayerSet[num2].Active != 0)
			{
				PlayerSet[num2].UpdatePlayer(num2);
			}
		}
		if (NetMode != 1 && TutorialState >= Tutorial.THE_END)
		{
			NPC.SpawnNPC();
		}
		for (int num3 = 0; num3 < 8; num3++)
		{
			PlayerSet[num3].ActiveNPCs = 0f;
			PlayerSet[num3].TownNPCs = 0f;
		}
		if (NPC.WoF >= 0 && NPCSet[NPC.WoF].Active == 0)
		{
			NPC.WoF = -1;
		}
		for (int num4 = 195; num4 >= 0; num4--)
		{
			if (NPCSet[num4].Active != 0)
			{
				NPCSet[num4].UpdateNPC(num4);
			}
		}
		for (int num5 = 0; num5 < 128; num5++)
		{
			if (GoreSet[num5].Active != 0)
			{
				GoreSet[num5].Update();
			}
		}
		for (int num6 = 0; num6 < 512; num6++)
		{
			if (ProjectileSet[num6].active != 0)
			{
				ProjectileSet[num6].Update(num6);
			}
		}
		for (int num7 = 0; num7 < 200; num7++)
		{
			if (ItemSet[num7].Active != 0)
			{
				ItemSet[num7].UpdateItem(num7);
			}
		}
		CombatText.UpdateCombatText();
		UpdateTime();
	}

	protected override void Draw(GameTime GTime)
	{
		_drawCount++;
		// If the previous frame left a batch open - one screen's Begin without a matching End -
		// the very next Begin would throw and kill the session, with a message that does not say
		// which screen did it. Closing it here makes the worst case "one sprite skipped" and the
		// warning in the log names the count, so the culprit is still findable.
		SpriteBatch.ForceEnd();
		if (_drawCount <= 5)
		{
			Program.Log($"[Draw] #{_drawCount} time={GTime.TotalGameTime.TotalSeconds:F3}s splash={ShowSplash}");
			if (_drawCount == 1)
			{
				Program.Log("[Draw] \ud83d\uddbc\ufe0f\ud83d\uddbc\ufe0f\ud83d\uddbc\ufe0f FIRST DRAW CALL! Rendering pipeline active! \ud83d\uddbc\ufe0f\ud83d\uddbc\ufe0f\ud83d\uddbc\ufe0f");
			}
		}
		try
		{
			GraphicsDevice.SetRenderTarget(_renderTarget);
			GraphicsDevice.Clear(Color.Black);
		}
		catch (Exception ex)
		{
			Program.Log("[Draw] \ud83d\udca5 SetRenderTarget/Clear failed: " + ex.Message);
			return;
		}
		if (ShowSplash == 0)
		{
			_drawSplashCount++;
			if (_drawSplashCount == 1)
			{
				Program.LogDebug("Draw", "splash screen started");
			}
			DrawSplash(GTime);
			DrawRenderTargetToScreen();
			return;
		}
		if (ShowSplash == 1)
		{
			// Content is still loading: keep showing the bar. Falling through here would
			// touch UIInstance[], which is only created by InitializePostSplash().
			DrawLoadingBar();
			DrawRenderTargetToScreen();
			return;
		}
		_drawGameCount++;
		FPSCount++;
		RenderCount++;
		for (int i = 0; i < UIInstance.Length; i++)
		{
			UIInstance[i].PrepareDraw(RenderCount);
		}
		if (RenderCount > UIInstance.Length)
		{
			RenderCount = 0;
			Lighting.TempLightCount = 0;
		}
		try
		{
			GraphicsDevice.SetRenderTarget(_renderTarget);
			GraphicsDevice.Clear(default);
			base.Draw(GTime);
		}
		catch (Exception ex2)
		{
			Program.Log("[Draw] \ud83d\udca5 Game render failed: " + ex2.Message);
		}
		for (int j = 0; j < UIInstance.Length; j++)
		{
			try
			{
				UIInstance[j].Draw();
			}
			catch (Exception ex3)
			{
				if (_drawGameCount <= 3)
				{
					Program.Log($"[Draw] \ud83d\udca5 UI[{j}] Draw error: {ex3.Message}");
				}
			}
		}
		WorldView.RestoreViewport();
		try
		{
			SpriteBatch.Begin();
			for (int k = 0; k < UIInstance.Length; k++)
			{
				if (UIInstance[k].CurMenuType != MenuType.MAIN)
				{
					DrawChat();
					break;
				}
			}
			DrawFPS();
			if (SaveIconCounter > 0 || ActiveSaves > 0)
			{
				SaveIconCounter--;
				int x = 878;
				int y = 479;
				Color white = Color.White;
				switch (ScreenHeightPtr)
				{
				case ScreenHeights.HD:
					x = 1182;
					y = 650;
					break;
				case ScreenHeights.FHD:
					x = 1790;
					y = 992;
					break;
				}
				SpriteSheet<_sheetSprites>.Draw(642, x, y, white, (float)((double)SaveIconCounter * (Math.PI / 60.0)), 1f);
			}
			SpriteBatch.End();
		}
		catch (Exception ex4)
		{
			Program.Log("[Draw] \ud83d\udca5 SpriteBatch rendering error: " + ex4.Message);
		}
		DrawRenderTargetToScreen();
		if (_drawGameCount == 1)
		{
			Program.Log("[Draw] ✅ First full game frame rendered successfully!");
		}
	}

	private void DrawRenderTargetToScreen()
	{
		try
		{
			GraphicsDevice.SetRenderTarget(null);
			GraphicsDevice.Clear(Color.Black);
			SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
			SpriteBatch.Draw(_renderTarget, new Rectangle(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight), Color.White);
			SpriteBatch.End();
		}
		catch (Exception ex)
		{
			Program.Log("[DrawRenderTargetToScreen] \ud83d\udca5 Error: " + ex.Message);
		}
	}

	private static void UpdateInvasion()
	{
		if (InvasionType <= 0)
		{
			return;
		}
		if (InvasionSize <= 0)
		{
			if (InvasionType == 1)
			{
				NPC.HasDownedGoblins = true;
				UI.SetTriggerStateForAll(Trigger.KilledGoblinArmy);
				NetMessage.CreateMessage0(7);
				NetMessage.SendMessage();
			}
			else if (InvasionType == 2)
			{
				NPC.HasDownedFrost = true;
			}
			InvasionWarning();
			InvasionType = 0;
			InvasionDelay = 7;
		}
		if (InvasionX == (float)SpawnTileX)
		{
			return;
		}
		float num = 1f;
		if (InvasionX > (float)SpawnTileX)
		{
			InvasionX -= num;
			if (InvasionX <= (float)SpawnTileX)
			{
				InvasionX = SpawnTileX;
				InvasionWarning();
			}
			else
			{
				InvasionWarn--;
			}
		}
		else if (InvasionX < (float)SpawnTileX)
		{
			InvasionX += num;
			if (InvasionX >= (float)SpawnTileX)
			{
				InvasionX = SpawnTileX;
				InvasionWarning();
			}
			else
			{
				InvasionWarn--;
			}
		}
		if (InvasionWarn <= 0)
		{
			InvasionWarn = 3600;
			InvasionWarning();
		}
	}

	private static void InvasionWarning()
	{
		int textId;
		if (InvasionSize <= 0)
		{
			textId = ((InvasionType == 2) ? 4 : 0);
		}
		else if (InvasionX < (float)SpawnTileX)
		{
			textId = ((InvasionType != 2) ? 1 : 5);
		}
		else if (InvasionX > (float)SpawnTileX)
		{
			textId = ((InvasionType != 2) ? 2 : 6);
		}
		else
		{
			textId = ((InvasionType != 2) ? 3 : 7);
		}
		NetMessage.SendText(textId, 175, 75, 255, -1);
	}

	public static void StartInvasion(int InvadeType = 1)
	{
		if (InvasionType != 0 || InvasionDelay != 0)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			if (PlayerSet[i].Active != 0 && PlayerSet[i].StatLifeMax >= 200)
			{
				num++;
			}
		}
		if (num > 0)
		{
			InvasionType = InvadeType;
			InvasionSize = 80 + 40 * num;
			InvasionWarn = 0;
			if (Rand.Next(2) == 0)
			{
				InvasionX = 0f;
			}
			else
			{
				InvasionX = MaxTilesX;
			}
		}
	}

	private static void UpdateClient()
	{
		if (IsGameStarted)
		{
			NetPlayCounter++;
		}
		try
		{
			Netplay.Session.Update();
		}
		catch (Exception ex)
		{
			Program.Log("[Net] session update failed: " + ex.Message);
		}
		// Inbound packets are attacker-controlled data. A single malformed message must not
		// terminate the game, so a failed parse is logged and the batch is dropped.
		try
		{
			NetMessage.CheckBytesClient();
		}
		catch (Exception ex2)
		{
			Program.Log("[Net] client packet handling failed: " + ex2.Message);
		}
	}

	private static void UpdateServer()
	{
		if (IsGameStarted)
		{
			NetPlayCounter++;
			for (int num = Netplay.ClientList.Count - 1; num >= 0; num--)
			{
				NetClient netClient = Netplay.ClientList[num];
				for (int num2 = netClient.Machine.Gamers.Count - 1; num2 >= 0; num2--)
				{
					Player player = netClient.Machine.Gamers[num2].Tag as Player;
					if (player.Active != 0)
					{
						int num3 = (player.XYWH.X >> 4) / 40;
						int num4 = (player.XYWH.Y >> 4) / 30;
						NetMessage.SendSection(netClient, num3, num4);
						if (player.velocity.X > 0f)
						{
							if (!NetMessage.SendSection(netClient, num3 + 1, num4) && !NetMessage.SendSection(netClient, num3 + 1, num4 - 1) && !NetMessage.SendSection(netClient, num3 + 1, num4 + 1) && !NetMessage.SendSection(netClient, num3 + 2, num4) && !NetMessage.SendSection(netClient, num3 + 2, num4 - 1) && !NetMessage.SendSection(netClient, num3 + 2, num4 + 1))
							{
							}
						}
						else if (player.velocity.X < 0f && !NetMessage.SendSection(netClient, num3 - 1, num4) && !NetMessage.SendSection(netClient, num3 - 1, num4 - 1) && !NetMessage.SendSection(netClient, num3 - 1, num4 + 1) && !NetMessage.SendSection(netClient, num3 - 2, num4) && !NetMessage.SendSection(netClient, num3 - 2, num4 - 1))
						{
							NetMessage.SendSection(netClient, num3 - 2, num4 + 1);
						}
						if (player.velocity.Y > 0f)
						{
							if (!NetMessage.SendSection(netClient, num3, num4 + 1) && !NetMessage.SendSection(netClient, num3 + 1, num4 + 1) && !NetMessage.SendSection(netClient, num3 - 1, num4 + 1) && !NetMessage.SendSection(netClient, num3 + 2, num4 + 1) && !NetMessage.SendSection(netClient, num3 - 2, num4 + 1))
							{
							}
						}
						else if (player.velocity.Y < 0f && !NetMessage.SendSection(netClient, num3, num4 - 1) && !NetMessage.SendSection(netClient, num3 + 1, num4 - 1) && !NetMessage.SendSection(netClient, num3 - 1, num4 - 1) && !NetMessage.SendSection(netClient, num3 + 2, num4 - 1))
						{
							NetMessage.SendSection(netClient, num3 - 2, num4 - 1);
						}
					}
				}
			}
		}
		try
		{
			Netplay.Session.Update();
		}
		catch (Exception)
		{
		}
		if (NetMode == 0)
		{
			Netplay.CheckOfflineSession();
			return;
		}
		// Host-side parse of client packets: same reasoning as UpdateClient, and here a crash
		// would take down every connected player at once.
		try
		{
			NetMessage.CheckBytesServer();
		}
		catch (Exception ex)
		{
			Program.Log("[Net] server packet handling failed: " + ex.Message);
		}
		foreach (NetworkGamer remoteGamer in Netplay.Session.RemoteGamers)
		{
			Player player2 = remoteGamer.Tag as Player;
			if (player2.ToKill)
			{
				player2.ToKill = false;
				player2.Active = 0;
				remoteGamer.Machine.RemoveFromSession();
			}
		}
	}

	public static void NewText(string Text, int R, int G, int B)
	{
		for (int num = 6; num > 0; num--)
		{
			ref ChatLine reference = ref ChatLineSet[num];
			reference = ChatLineSet[num - 1];
		}
		ChatLineSet[0].Color = new Color(R, G, B);
		ChatLineSet[0].Text = Text;
		ChatLineSet[0].Size = UI.BoldSmallFont.MeasureString(Text);
		ChatLineSet[0].ShowTime = 600;
		PlaySound(12);
	}

	public static void DrawChat()
	{
		int num = 0;
		float num2 = 0f;
		int num3 = 0;
		int num4 = 48;
		int num5 = 440;
		switch (ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num4 = 66;
			num5 = 620;
			break;
		case ScreenHeights.FHD:
			num4 = 98;
			num5 = 980;
			break;
		}
		float x = num4 + 6;
		int num6 = num5 + 6;
		for (int i = 0; i < 7; i++)
		{
			if (ChatLineSet[i].ShowTime > 0)
			{
				num3 += (int)ChatLineSet[i].Size.Y;
				if (ChatLineSet[i].Size.X > num2)
				{
					num2 = ChatLineSet[i].Size.X;
				}
				num++;
			}
		}
		if (num == 0 || num2 == 0f)
		{
			return;
		}
		DrawRect(new Rectangle(num4, num5 - num3, (int)num2 + 12, num3 + 12), new Color(32, 32, 32, 32));
		float num7 = 0f;
		for (int j = 0; j < 7; j++)
		{
			if (ChatLineSet[j].ShowTime > 0)
			{
				num7 += ChatLineSet[j].Size.Y;
				float num8 = (float)(int)UI.MouseTextBrightness * (1f / 255f);
				SpriteBatch.DrawString(UI.BoldSmallFont, ChatLineSet[j].Text, new Vector2(x, (float)num6 - num7), new Color((byte)((float)(int)ChatLineSet[j].Color.R * num8), (byte)((float)(int)ChatLineSet[j].Color.G * num8), (byte)((float)(int)ChatLineSet[j].Color.B * num8), UI.MouseTextBrightness));
			}
		}
	}

	private static void UpdateTime()
	{
		bool isBloodMoon = GameTime.IsBloodMoon;
		if (GameTime.Update())
		{
			WorldGen.ToSpawnNPC = 0;
			NPC.CheckForSpawnsTimer = 0;
			if (GameTime.DayTime)
			{
				Time.CheckXMas();
				if (InvasionDelay > 0)
				{
					InvasionDelay--;
				}
				if (NetMode != 1)
				{
					if (WorldGen.HasShadowOrbSmashed && Rand.Next(NPC.HasDownedGoblins ? 15 : 3) == 0)
					{
						StartInvasion();
					}
					for (int i = 0; i < 8; i++)
					{
						if (PlayerSet[i].Active != 0)
						{
							PlayerSet[i].SunMoonTransition(isBloodMoon);
						}
					}
				}
			}
			else if (NetMode != 1)
			{
				if (WorldGen.HasShadowOrbSmashed && Rand.Next(50) == 0)
				{
					WorldGen.ToSpawnMeteor = true;
				}
				if (!NPC.HasDownedBoss1)
				{
					for (int j = 0; j < 8; j++)
					{
						if (PlayerSet[j].Active == 0 || PlayerSet[j].StatLifeMax < 200 || PlayerSet[j].StatDefense <= 10)
						{
							continue;
						}
						if (Rand.Next(3) != 0)
						{
							break;
						}
						int num = 0;
						for (int k = 0; k < 196; k++)
						{
							if (NPCSet[k].IsTownNPC && NPCSet[k].Active != 0 && ++num >= 4)
							{
								WorldGen.ToSpawnEye = true;
								NetMessage.SendText(9, 50, 255, 130, -1);
								break;
							}
						}
						break;
					}
				}
				if (!WorldGen.ToSpawnEye && GameTime.MoonPhase != 4 && Rand.Next(9) == 0)
				{
					for (int l = 0; l < 8; l++)
					{
						if (PlayerSet[l].Active != 0 && PlayerSet[l].StatLifeMax > 120)
						{
							GameTime.IsBloodMoon = true;
							NetMessage.SendText(8, 50, 255, 130, -1);
							break;
						}
					}
				}
				for (int m = 0; m < 8; m++)
				{
					if (PlayerSet[m].Active != 0)
					{
						PlayerSet[m].SunMoonTransition(WasBloodMoon: false);
					}
				}
			}
			if (NetMode == 2)
			{
				NetMessage.CreateMessage0(7);
				NetMessage.SendMessage();
			}
		}
		else if (GameTime.DayTime)
		{
			if (NetMode != 1)
			{
				NPC.CheckForTownSpawns();
			}
		}
		else if (GameTime.WorldTime > 16200f)
		{
			if (WorldGen.ToSpawnMeteor)
			{
				WorldGen.ToSpawnMeteor = false;
				WorldGen.DropMeteor();
			}
		}
		else
		{
			if (!(GameTime.WorldTime > 4860f) || !WorldGen.ToSpawnEye || NetMode == 1)
			{
				return;
			}
			for (int n = 0; n < 8; n++)
			{
				Player player = PlayerSet[n];
				if (player.Active != 0 && !player.IsDead && player.XYWH.Y < WorldSurfacePixels)
				{
					NPC.SpawnOnPlayer(player, 4);
					WorldGen.ToSpawnEye = false;
					break;
				}
			}
		}
	}

	public static int DamageVar(int InitialDamage)
	{
		return (int)Math.Round((double)InitialDamage * (1.0 + (double)Rand.Next(-15, 16) * 0.01));
	}

	public static double CalculateDamage(int Damage, int Defense)
	{
		float num = (float)Damage - (float)Defense * 0.5f;
		if (num < 1f)
		{
			num = 1f;
		}
		return num;
	}

	/// <summary>
	/// Plays one of the game's sounds at a world position. Delegates to <see cref="Audio.SoundPlayer"/>:
	/// the body lives there so this file stops growing, and this signature stays so the ~300 call
	/// sites keep working unchanged.
	/// </summary>
	public static void PlaySound(int SoundType, int X, int Y, int Style = 1)
	{
		Terraria.Audio.SoundPlayer.Play(SoundType, X, Y, Style);
	}

	/// <summary>Plays a UI sound that has no world position.</summary>
	public static void PlaySound(int SoundType)
	{
		Terraria.Audio.SoundPlayer.Play(SoundType);
	}

	private static void UpdateTutorial()
	{
		if (UI.MainUI.CurMenuType != MenuType.NONE)
		{
			return;
		}
		if (TutorialInputDelay > 0)
		{
			TutorialInputDelay--;
		}
		Vector2 leftThumbStick = (TutorialMaskLS ? default(Vector2) : UI.MainUI.PadState.ThumbSticks.Left);
		Vector2 rightThumbStick = (TutorialMaskRS ? default(Vector2) : UI.MainUI.PadState.ThumbSticks.Right);
		float leftTrigger = (TutorialMaskLT ? 0f : UI.MainUI.PadState.Triggers.Left);
		float rightTrigger = (TutorialMaskRT ? 0f : UI.MainUI.PadState.Triggers.Right);
		Buttons buttons = (Buttons)0;
		if (!TutorialMaskA && UI.MainUI.PadState.IsButtonDown(Buttons.A))
		{
			buttons |= Buttons.A;
		}
		if (!TutorialMaskB && UI.MainUI.PadState.IsButtonDown(Buttons.B))
		{
			buttons |= Buttons.B;
		}
		if (!TutorialMaskX && UI.MainUI.PadState.IsButtonDown(Buttons.X))
		{
			buttons |= Buttons.X;
		}
		if (!TutorialMaskY && UI.MainUI.PadState.IsButtonDown(Buttons.Y))
		{
			buttons |= Buttons.Y;
		}
		if (!TutorialMaskBack && UI.MainUI.PadState.IsButtonDown(Buttons.Back))
		{
			buttons |= Buttons.Back;
		}
		if (!TutorialMaskLB && UI.MainUI.PadState.IsButtonDown(Buttons.LeftShoulder))
		{
			buttons |= Buttons.LeftShoulder;
		}
		if (!TutorialMaskRB && UI.MainUI.PadState.IsButtonDown(Buttons.RightShoulder))
		{
			buttons |= Buttons.RightShoulder;
		}
		if (!TutorialMaskLS && UI.MainUI.PadState.IsButtonDown(Buttons.DPadLeft))
		{
			buttons |= Buttons.DPadLeft;
		}
		if (!TutorialMaskLS && UI.MainUI.PadState.IsButtonDown(Buttons.DPadRight))
		{
			buttons |= Buttons.DPadRight;
		}
		if (!TutorialMaskLS && UI.MainUI.PadState.IsButtonDown(Buttons.DPadUp))
		{
			buttons |= Buttons.DPadUp;
		}
		if (!TutorialMaskLS && UI.MainUI.PadState.IsButtonDown(Buttons.DPadDown))
		{
			buttons |= Buttons.DPadDown;
		}
		if (!TutorialMaskLS && UI.MainUI.PadState.IsButtonDown(Buttons.LeftStick))
		{
			buttons |= Buttons.LeftStick;
		}
		if (!TutorialMaskRSpress && UI.MainUI.PadState.IsButtonDown(Buttons.RightStick))
		{
			buttons |= Buttons.RightStick;
		}
		if (UI.MainUI.PadState.IsButtonDown(Buttons.Start))
		{
			buttons |= Buttons.Start;
		}
		UI.MainUI.PadState = new GamePadState(leftThumbStick, rightThumbStick, leftTrigger, rightTrigger, buttons);
		switch (TutorialState)
		{
		case Tutorial.INTRO:
		case Tutorial.MONSTER_INFO_1:
		case Tutorial.POTIONS_1:
		case Tutorial.TORCH_1:
		case Tutorial.INVENTORY_2:
		case Tutorial.MOVEMENT_1:
		case Tutorial.DROP_1:
		case Tutorial.EQUIPSCREEN_1:
		case Tutorial.CHEST_1:
		case Tutorial.CRAFTSCREEN_1:
		case Tutorial.MINED_ORE_1:
		case Tutorial.CURSOR_SWITCH_1:
		case Tutorial.DAY_NIGHT_1:
		case Tutorial.USE_BENCH_1:
		case Tutorial.BACK_WALL_INFO_1:
		case Tutorial.HOUSE_INFO_1:
		case Tutorial.HOUSE_DONE_1:
		case Tutorial.THE_GUIDE_1:
		case Tutorial.HAMMER_1:
		case Tutorial.CONGRATS_1:
			if (TutorialInputDelay == 0)
			{
				NextTutorial();
			}
			break;
		}
		switch (TutorialState)
		{
		case Tutorial.INTRO_2:
		case Tutorial.MONSTER_INFO_2:
		case Tutorial.POTIONS_2:
		case Tutorial.TORCH_2:
		case Tutorial.INVENTORY_3:
		case Tutorial.MOVEMENT_2:
		case Tutorial.DROP_2:
		case Tutorial.EQUIPSCREEN_2:
		case Tutorial.CHEST_2:
		case Tutorial.CRAFTSCREEN_2:
		case Tutorial.MINED_ORE_2:
		case Tutorial.CURSOR_SWITCH_2:
		case Tutorial.DAY_NIGHT_2:
		case Tutorial.USE_BENCH_2:
		case Tutorial.BACK_WALL_INFO_2:
		case Tutorial.HOUSE_INFO_2:
		case Tutorial.HOUSE_DONE_2:
		case Tutorial.THE_GUIDE_2:
		case Tutorial.HAMMER_2:
		case Tutorial.CONGRATS_2:
			if (UI.MainUI.IsButtonTriggered(Buttons.B))
			{
				UI.MainUI.ClearButtonTriggers();
				TutorialMaskB = TutorialVar != 0;
				NextTutorial();
			}
			return;
		}
		Player activePlayer = UI.MainUI.ActivePlayer;
		switch (TutorialState)
		{
		case Tutorial.MOVE:
			if (UI.MainUI.PadState.ThumbSticks.Left.LengthSquared() > 1f / 64f)
			{
				TutorialVar++;
			}
			if (TutorialVar > 4 && TutorialInputDelay == 0)
			{
				UI.MainUI.AchievementTriggers.SetState(Trigger.FirstTutorialTaskCompleted, State: true);
				NextTutorial();
			}
			break;
		case Tutorial.JUMP:
			if (UI.MainUI.TotalJumps - TutorialVar >= 1)
			{
				NextTutorial();
			}
			break;
		case Tutorial.FALL_DOWN:
			if (activePlayer.IsControlDown)
			{
				int num = (int)activePlayer.Position.X + 10 >> 4;
				int num2 = (int)activePlayer.Position.Y + 42 >> 4;
				if (TileSet[num, num2 - 1].Type == 19 || TileSet[num - 1, num2 - 1].Type == 19 || TileSet[num + 1, num2 - 1].Type == 19)
				{
					NextTutorial();
				}
			}
			break;
		case Tutorial.JUMP_OUT:
			if (UI.MainUI.TotalJumps - TutorialVar >= 1)
			{
				int num3 = (int)activePlayer.Position.X + 10 >> 4;
				int num4 = (int)activePlayer.Position.Y + 42 >> 4;
				if (TileSet[num3, num4 + 1].Type == 19 || TileSet[num3 - 1, num4 + 1].Type == 19 || TileSet[num3 + 1, num4 + 1].Type == 19)
				{
					NextTutorial();
				}
			}
			break;
		case Tutorial.CURSOR:
			if (UI.MainUI.PadState.ThumbSticks.Right.LengthSquared() > 1f / 64f)
			{
				TutorialVar++;
			}
			if (UI.MainUI.IsButtonTriggered(Buttons.RightTrigger))
			{
				TutorialVar2++;
			}
			if (TutorialInputDelay == 0 && TutorialVar != 0 && TutorialVar2 != 0)
			{
				NextTutorial();
			}
			break;
		case Tutorial.HOTBAR:
			if (TutorialInputDelay == 0 && activePlayer.SelectedItem == 0)
			{
				NextTutorial();
			}
			break;
		case Tutorial.SWORD_ATTACK:
			if (UI.MainUI.TotalDeadSlimes > TutorialVar)
			{
				Item.NewItem((int)activePlayer.Position.X, (int)activePlayer.Position.Y, 1, 1, 23);
				NextTutorial();
			}
			else if (TutorialVar2 != 0 && --TutorialVar2 == 0)
			{
				TutorialVar2 = 900u;
				int num7 = NPC.NewNPC(activePlayer.XYWH.X - ResolutionWidth / 2, activePlayer.XYWH.Y - ResolutionHeight, 1);
				NPCSet[num7].SetDefaults("Green Slime");
			}
			break;
		case Tutorial.SELECT_AXE:
			if (TutorialInputDelay == 0 && activePlayer.Inventory[activePlayer.SelectedItem].AxePower > 0)
			{
				NextTutorial();
			}
			break;
		case Tutorial.USE_AXE:
			if (UI.MainUI.TotalChopsTaken > TutorialVar)
			{
				NextTutorial();
			}
			break;
		case Tutorial.INVENTORY:
			if (UI.MainUI.InventoryMode == 1)
			{
				NextTutorial();
			}
			break;
		case Tutorial.EQUIPMENT:
			if (UI.MainUI.ActiveInvSection == UI.InventorySection.EQUIP)
			{
				NextTutorial();
			}
			break;
		case Tutorial.CRAFTING:
			if (UI.MainUI.ActiveInvSection == UI.InventorySection.CRAFTING)
			{
				NextTutorial();
			}
			break;
		case Tutorial.CRAFT_TORCH:
			if (UI.MainUI.TotalTorchesCrafted > TutorialVar)
			{
				NextTutorial();
			}
			break;
		case Tutorial.CRAFT_CATEGORIES:
			if (UI.MainUI.IsButtonTriggered(Buttons.LeftTrigger) || UI.MainUI.IsButtonTriggered(Buttons.RightTrigger))
			{
				TutorialVar++;
			}
			if (TutorialVar != 0 && TutorialInputDelay == 0)
			{
				NextTutorial();
			}
			break;
		case Tutorial.CRAFTING_EXIT:
			if (UI.MainUI.InventoryMode == 0)
			{
				NextTutorial();
			}
			break;
		case Tutorial.SELECT_PICK:
			if (TutorialInputDelay == 0 && activePlayer.Inventory[activePlayer.SelectedItem].PickPower > 0)
			{
				NextTutorial();
			}
			break;
		case Tutorial.USE_PICK:
			if (UI.MainUI.TotalCopperObtained - TutorialVar >= 55)
			{
				NextTutorial();
			}
			break;
		case Tutorial.WOOD_PLATFORM:
			if (UI.MainUI.TotalWoodPlatformsCrafted - TutorialVar >= 5)
			{
				NextTutorial(2);
			}
			else if (--TutorialVar2 == 0)
			{
				NextTutorial();
			}
			break;
		case Tutorial.WOOD_PLATFORM_TIME_OUT:
			if (UI.MainUI.TotalWoodPlatformsCrafted - TutorialVar >= 5)
			{
				NextTutorial();
			}
			break;
		case Tutorial.SELECT_PLATFORM:
			if (TutorialInputDelay == 0 && activePlayer.Inventory[activePlayer.SelectedItem].Type == 94)
			{
				NextTutorial();
			}
			break;
		case Tutorial.BUILD_CURSOR:
			if (TutorialInputDelay == 0 && !UI.MainUI.UsingSmartCursor)
			{
				TutorialMaskRSpress = true;
				NextTutorial();
			}
			break;
		case Tutorial.PLACING_1:
			if (UI.MainUI.TotalWoodPlatformsPlaced > TutorialVar)
			{
				NextTutorial();
			}
			break;
		case Tutorial.PLACING_2:
			if (activePlayer.XYWH.Y < 3360)
			{
				NextTutorial();
			}
			break;
		case Tutorial.BUILD_HOUSE:
			if (TutorialInputDelay == 0)
			{
				NextTutorial();
			}
			goto case Tutorial.BUILD_HOUSE_EXTRA_INFO;
		case Tutorial.BUILD_HOUSE_EXTRA_INFO:
		{
			if ((FrameCounter & 0x1F) != 0)
			{
				break;
			}
			int num5 = UI.MainUI.MouseX + UI.MainUI.CurrentView.ScreenPosition.X >> 4;
			int num6 = UI.MainUI.MouseY + UI.MainUI.CurrentView.ScreenPosition.Y >> 4;
			bool flag = WorldGen.StartSpaceCheck(num5, num6);
			if (!flag)
			{
				num5--;
				flag = WorldGen.StartSpaceCheck(num5, num6);
				if (!flag)
				{
					num5 += 2;
					flag = WorldGen.StartSpaceCheck(num5, num6);
				}
				if (!flag)
				{
					num5--;
					num6++;
					flag = WorldGen.StartSpaceCheck(num5, num6);
				}
			}
			if (flag)
			{
				TutorialHouse.X = (short)num5;
				TutorialHouse.Y = (short)num6;
				NextTutorial((TutorialState != Tutorial.BUILD_HOUSE) ? 1 : 2);
			}
			break;
		}
		case Tutorial.BUILD_HOUSE_2:
			if (TutorialInputDelay == 0)
			{
				NextTutorial();
			}
			goto case Tutorial.BUILD_HOUSE_2_EXTRA_INFO;
		case Tutorial.BUILD_HOUSE_2_EXTRA_INFO:
			if ((FrameCounter & 0x1F) == 0 && (UI.MainUI.TotalWoodAxed - TutorialVar >= 3 || UI.MainUI.TotalOrePicked - TutorialVar2 >= 3) && !WorldGen.StartSpaceCheck(TutorialHouse.X, TutorialHouse.Y))
			{
				NextTutorial((TutorialState != Tutorial.BUILD_HOUSE_2) ? 1 : 2);
			}
			break;
		case Tutorial.CRAFT_WORKBENCH:
		case Tutorial.CRAFT_WORKBENCH_EXTRA_INFO:
		{
			if ((FrameCounter & 0x1F) != 0)
			{
				break;
			}
			int num8 = (int)activePlayer.Position.X + 10 >> 4;
			int num9 = ((int)activePlayer.Position.Y + 42 >> 4) - 1;
			for (int i = num8 - 5; i < num8 + 5; i++)
			{
				for (int j = num9 - 5; j < num9 + 5; j++)
				{
					if (TileSet[i, j].Type == 18)
					{
						NextTutorial((TutorialState != Tutorial.CRAFT_WORKBENCH) ? 1 : 2);
						return;
					}
				}
			}
			break;
		}
		case Tutorial.CRAFT_DOOR:
		case Tutorial.CRAFT_DOOR_EXTRA_INFO:
			if (TutorialInputDelay == 0 && activePlayer.HasItemInInventory(25))
			{
				NextTutorial((TutorialState != Tutorial.CRAFT_DOOR) ? 1 : 2);
			}
			break;
		case Tutorial.PLACE_DOOR:
			if ((FrameCounter & 0x1F) == 0 && WorldGen.StartSpaceCheck(TutorialHouse.X, TutorialHouse.Y) && (WorldGen.HouseTile[10] || WorldGen.HouseTile[11]))
			{
				NextTutorial();
			}
			break;
		case Tutorial.USE_DOOR:
			if (TutorialInputDelay == 0 && (UI.MainUI.TotalDoorsOpened > TutorialVar || UI.MainUI.TotalDoorsClosed > TutorialVar2))
			{
				NextTutorial();
			}
			break;
		case Tutorial.CRAFT_WALL:
		case Tutorial.CRAFT_WALL_EXTRA_INFO:
			if (TutorialInputDelay == 0 && UI.MainUI.TotalWallsCrafted - TutorialVar >= 32)
			{
				NextTutorial((TutorialState != Tutorial.CRAFT_WALL) ? 1 : 2);
			}
			break;
		case Tutorial.PLACE_WALL:
			if (TutorialInputDelay == 0 && UI.MainUI.TotalWallsPlaced - TutorialVar >= 8)
			{
				NextTutorial();
			}
			break;
		case Tutorial.BACK_WALL:
			if ((FrameCounter & 0x1F) == 0 && WorldGen.StartRoomCheck(TutorialHouse.X, TutorialHouse.Y))
			{
				NextTutorial();
			}
			break;
		case Tutorial.PLACE_CHAIR:
			if ((FrameCounter & 0x1F) == 0 && WorldGen.StartRoomCheck(TutorialHouse.X, TutorialHouse.Y))
			{
				WorldGen.RoomNeeds();
				if (WorldGen.roomChair)
				{
					NextTutorial();
				}
			}
			break;
		case Tutorial.PLACE_TORCH:
			if ((FrameCounter & 0x1F) == 0 && WorldGen.StartRoomCheck(TutorialHouse.X, TutorialHouse.Y) && WorldGen.RoomNeeds())
			{
				NextTutorial();
			}
			break;
		case Tutorial.MONSTER_INFO_1:
		case Tutorial.MONSTER_INFO_2:
		case Tutorial.POTIONS_1:
		case Tutorial.POTIONS_2:
		case Tutorial.TORCH_1:
		case Tutorial.TORCH_2:
		case Tutorial.INVENTORY_2:
		case Tutorial.INVENTORY_3:
		case Tutorial.MOVEMENT_1:
		case Tutorial.MOVEMENT_2:
		case Tutorial.DROP_1:
		case Tutorial.DROP_2:
		case Tutorial.EQUIPSCREEN_1:
		case Tutorial.EQUIPSCREEN_2:
		case Tutorial.CHEST_1:
		case Tutorial.CHEST_2:
		case Tutorial.CRAFTSCREEN_1:
		case Tutorial.CRAFTSCREEN_2:
		case Tutorial.MINED_ORE_1:
		case Tutorial.MINED_ORE_2:
		case Tutorial.CURSOR_SWITCH_1:
		case Tutorial.CURSOR_SWITCH_2:
		case Tutorial.DAY_NIGHT_1:
		case Tutorial.DAY_NIGHT_2:
		case Tutorial.USE_BENCH_1:
		case Tutorial.USE_BENCH_2:
		case Tutorial.BACK_WALL_INFO_1:
		case Tutorial.BACK_WALL_INFO_2:
		case Tutorial.HOUSE_INFO_1:
		case Tutorial.HOUSE_INFO_2:
			break;
		}
	}

	private static void NextTutorial(int Increment = 1)
	{
		if (TutorialState < Tutorial.THE_END)
		{
			SetTutorial(TutorialState + Increment);
		}
	}

	public static void SetTutorial(Tutorial State)
	{
		TutorialState = State;
		if (State == Tutorial.NUM_TUTORIALS)
		{
			TutorialMaskLS = false;
			TutorialMaskRS = false;
			TutorialMaskRSpress = false;
			TutorialMaskA = false;
			TutorialMaskB = false;
			TutorialMaskX = false;
			TutorialMaskY = false;
			TutorialMaskLB = false;
			TutorialMaskRB = false;
			TutorialMaskLT = false;
			TutorialMaskRT = false;
			TutorialMaskBack = false;
			return;
		}
		TutorialInputDelay = 180;
		string text = Lang.TutorialLocale(State);
		Player activePlayer = UI.MainUI.ActivePlayer;
		switch (State)
		{
		case Tutorial.INTRO:
			TutorialMaskLS = true;
			TutorialMaskRS = true;
			TutorialMaskRSpress = true;
			TutorialMaskA = true;
			TutorialMaskB = true;
			TutorialMaskX = true;
			TutorialMaskY = true;
			TutorialMaskLB = true;
			TutorialMaskRB = true;
			TutorialMaskLT = true;
			TutorialMaskRT = true;
			TutorialMaskBack = true;
			UI.MainUI.UsingSmartCursor = true;
			break;
		case Tutorial.INTRO_2:
		case Tutorial.MONSTER_INFO_2:
		case Tutorial.POTIONS_2:
		case Tutorial.TORCH_2:
		case Tutorial.INVENTORY_3:
		case Tutorial.MOVEMENT_2:
		case Tutorial.DROP_2:
		case Tutorial.EQUIPSCREEN_2:
		case Tutorial.CHEST_2:
		case Tutorial.CRAFTSCREEN_2:
		case Tutorial.MINED_ORE_2:
		case Tutorial.CURSOR_SWITCH_2:
		case Tutorial.DAY_NIGHT_2:
		case Tutorial.USE_BENCH_2:
		case Tutorial.BACK_WALL_INFO_2:
		case Tutorial.HOUSE_INFO_2:
		case Tutorial.HOUSE_DONE_2:
		case Tutorial.THE_GUIDE_2:
		case Tutorial.HAMMER_2:
		case Tutorial.CONGRATS_2:
			text = Lang.TutorialLocale(State - 1) + text;
			TutorialVar = (TutorialMaskB ? 1u : 0u);
			TutorialMaskB = false;
			break;
		case Tutorial.MOVE:
			TutorialMaskLS = false;
			TutorialVar = 0u;
			break;
		case Tutorial.JUMP:
			TutorialMaskA = false;
			TutorialVar = UI.MainUI.TotalJumps;
			break;
		case Tutorial.JUMP_OUT:
			TutorialVar = UI.MainUI.TotalJumps;
			break;
		case Tutorial.CURSOR:
			TutorialMaskRT = false;
			TutorialMaskRS = false;
			TutorialVar = 0u;
			TutorialVar2 = 0u;
			break;
		case Tutorial.HOTBAR:
			TutorialMaskLB = false;
			TutorialMaskRB = false;
			break;
		case Tutorial.SWORD_ATTACK:
			TutorialVar = UI.MainUI.TotalDeadSlimes;
			TutorialVar2 = 120u;
			break;
		case Tutorial.MONSTER_INFO_1:
		{
			for (int num = 195; num >= 0; num--)
			{
				if (NPCSet[num].Type == 1 && NPCSet[num].Active != 0)
				{
					NPCSet[num].HitEffect(0, 999.0);
					NPCSet[num].Active = 0;
				}
			}
			break;
		}
		case Tutorial.USE_AXE:
			TutorialVar = UI.MainUI.TotalChopsTaken;
			break;
		case Tutorial.INVENTORY:
			TutorialMaskY = false;
			TutorialMaskB = true;
			break;
		case Tutorial.INVENTORY_2:
			TutorialMaskLB = true;
			TutorialMaskRB = true;
			break;
		case Tutorial.EQUIPMENT:
			TutorialMaskRB = false;
			TutorialMaskLB = false;
			TutorialMaskRT = false;
			TutorialMaskLT = false;
			break;
		case Tutorial.CRAFT_TORCH:
			TutorialMaskX = false;
			TutorialVar = UI.MainUI.TotalTorchesCrafted;
			break;
		case Tutorial.CRAFT_CATEGORIES:
			TutorialVar = 0u;
			break;
		case Tutorial.CRAFTING_EXIT:
			TutorialMaskB = false;
			break;
		case Tutorial.USE_PICK:
			TutorialVar = UI.MainUI.TotalCopperObtained;
			break;
		case Tutorial.WOOD_PLATFORM:
			TutorialVar = UI.MainUI.TotalWoodPlatformsCrafted;
			TutorialVar2 = 600u;
			break;
		case Tutorial.BUILD_CURSOR:
			TutorialMaskRSpress = false;
			break;
		case Tutorial.PLACING_1:
			TutorialVar = UI.MainUI.TotalWoodPlatformsPlaced;
			break;
		case Tutorial.PLACING_2:
			text = Lang.TutorialLocale(State - 1) + text;
			break;
		case Tutorial.CURSOR_SWITCH_1:
			TutorialMaskRSpress = false;
			break;
		case Tutorial.BUILD_HOUSE:
			TutorialInputDelay = 1800;
			break;
		case Tutorial.BUILD_HOUSE_2:
			TutorialVar = UI.MainUI.TotalWoodAxed;
			TutorialVar2 = UI.MainUI.TotalOrePicked;
			TutorialInputDelay = 600;
			break;
		case Tutorial.BUILD_HOUSE_EXTRA_INFO:
		case Tutorial.BUILD_HOUSE_2_EXTRA_INFO:
			text = Lang.TutorialLocale(State - 1) + text;
			break;
		case Tutorial.CRAFT_WORKBENCH:
			if (activePlayer.CountInventory(9) < 10)
			{
				TutorialState = State + 1;
				text += Lang.TutorialLocale(TutorialState);
			}
			break;
		case Tutorial.CRAFT_DOOR:
			if (activePlayer.CountInventory(9) < 6)
			{
				TutorialState = State + 1;
				text += Lang.TutorialLocale(TutorialState);
			}
			break;
		case Tutorial.USE_DOOR:
			TutorialVar = UI.MainUI.TotalDoorsOpened;
			TutorialVar2 = UI.MainUI.TotalDoorsClosed;
			break;
		case Tutorial.CRAFT_WALL:
			TutorialVar = UI.MainUI.TotalWallsCrafted;
			if (activePlayer.CountInventory(9) < 6)
			{
				TutorialState = State + 1;
				text += Lang.TutorialLocale(TutorialState);
			}
			break;
		case Tutorial.PLACE_WALL:
			TutorialVar = UI.MainUI.TotalWallsPlaced;
			break;
		case Tutorial.THE_END:
			GameTime.DayRate = 1f;
			UI.MainUI.AchievementTriggers.SetState(Trigger.AllTutorialTasksCompleted, State: true);
			break;
		}
		if (text != null)
		{
			int wrapWidth = 470;
			switch (ScreenHeightPtr)
			{
			case ScreenHeights.HD:
				wrapWidth = 548;
				break;
			case ScreenHeights.FHD:
				wrapWidth = 705;
				break;
			}
			TutorialText = new CompiledText(text, wrapWidth, UI.BoldSmallTextStyle);
		}
		else
		{
			TutorialText = null;
		}
	}

	public static bool IsTutorial()
	{
		return TutorialState != Tutorial.NUM_TUTORIALS;
	}

	public static void StartTutorial()
	{
		Player player = new Player();
		player.Name = UI.MainUI.SignedInGamer.Gamertag;
		player.SelectedItem = 1;
		UI.MainUI.CreateCharacterGUI.Randomize(player);
		UI.MainUI.SetPlayer(player);
		SetTutorial(Tutorial.INTRO);
		WorldGen.PlayWorld();
	}

	/// <summary>
	/// Applies window size / fullscreen changes immediately. A restart is not needed for these;
	/// the render scale is the only setting that requires one.
	/// </summary>
	internal void ApplyDisplayChange()
	{
		try
		{
			if (GraphicsGame == null)
			{
				return;
			}
			GraphicsGame.PreferredBackBufferWidth = Terraria.DisplaySettings.Width;
			GraphicsGame.PreferredBackBufferHeight = Terraria.DisplaySettings.Height;
			GraphicsGame.IsFullScreen = Terraria.DisplaySettings.Fullscreen;
			Terraria.DisplaySettings.ApplyFrameLimit(this, GraphicsGame);
			GraphicsGame.ApplyChanges();
			Program.LogInfo("Display", $"window changed to {Terraria.DisplaySettings.Width}x{Terraria.DisplaySettings.Height} (fullscreen={Terraria.DisplaySettings.Fullscreen}, vsync={Terraria.DisplaySettings.VSync}, cap={Terraria.DisplaySettings.FpsLimit})");
		}
		catch (Exception ex)
		{
			Program.LogWarn("Display", "ApplyChanges failed: " + ex.Message);
		}
	}

	public static void StartGame()
	{
		UI mainUI = UI.MainUI;
		PlaySound(11);
		for (int i = 0; i < 7; i++)
		{
			ChatLineSet[i].Init();
		}
		for (int j = 0; j < 8; j++)
		{
			if (j != mainUI.MyPlayer)
			{
				PlayerSet[j].Active = 0;
			}
			PlayerSet[j].IsAnnounced = false;
		}
		if (IsTutorial())
		{
			UI.MainUI.SignedInGamer.Presence.SetPresenceModeStringEXT("Tutorial");
		}
		else if (mainUI.IsOnline)
		{
			NetMode = 2;
			mainUI.SignedInGamer.Presence.SetPresenceModeStringEXT("Online");
		}
		else
		{
			mainUI.SignedInGamer.Presence.SetPresenceModeStringEXT("Offline");
		}
		Netplay.StartServer();
		MusicBox = -1;
		GameTime = WorldGen.tempTime;
		mainUI.InitGame();
		Netplay.SessionReadyEvent.WaitOne();
		mainUI.ActivePlayer.Spawn();
		mainUI.CurMenuType = MenuType.NONE;
		mainUI.CurrentView.OnStartGame();
		MiniMap.OnStartGame();
		// The saved zoom used to be parsed and then never applied anywhere, so the setting
		// did nothing until the player pressed +/-. Apply it now that the view exists.
		Terraria.HudSettings.Apply();
		GC.Collect();
		IsGameStarted = true;
		Terraria.Mods.ModLoader.WorldLoaded();
	}

	public static void JoinGame(UI StartUI)
	{
		StartUI.SignedInGamer.Presence.SetPresenceModeStringEXT((NetMode == 0) ? "Offline" : "Online");
		PlaySound(11);
		StartUI.InitGame();
		StartUI.ActivePlayer.Spawn();
		if (NetMode == 2)
		{
			NetMessage.SyncPlayer(StartUI.MyPlayer);
		}
		StartUI.CurMenuType = MenuType.NONE;
		StartUI.CurrentView.OnStartGame();
		MiniMap.OnStartGame();
		Terraria.HudSettings.Apply();
		IsGameStarted = true;
		Terraria.Mods.ModLoader.WorldLoaded();
	}

	public static void DrawSolidRect(Rectangle DestRect, Color Colour)
	{
		SpriteBatch.Draw(WhiteTexture, DestRect, Colour);
	}

	public static void DrawSolidRect(ref Rectangle DestRect, Color Colour)
	{
		SpriteBatch.Draw(WhiteTexture, DestRect, Colour);
	}

	public static void DrawRect(int TexId, Rectangle DestRect, Color Colour)
	{
		Rectangle s = default;
		Vector2 pos = default;
		Rectangle dest = DestRect;
		s.X = (s.Y = 8);
		s.Width = (s.Height = 36);
		pos.X = DestRect.X - 8;
		pos.Y = DestRect.Y - 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, Colour);
		s.X = 9;
		s.Y = 0;
		s.Width = 34;
		s.Height = 8;
		dest.Y -= 8;
		dest.Height = 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, Colour);
		s.Y = 44;
		dest.Y = DestRect.Y + DestRect.Height;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, Colour);
		s.Width = 8;
		s.Y = 0;
		s.X = 0;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, Colour);
		s.Y = 44;
		pos.Y = DestRect.Y + DestRect.Height;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, Colour);
		pos.X = DestRect.X + DestRect.Width;
		s.X = 44;
		s.Y = 44;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, Colour);
		pos.Y = DestRect.Y - 8;
		s.Y = 0;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, Colour);
		s.Height = 34;
		s.Y = 9;
		dest.Width = 8;
		dest.Height = DestRect.Height;
		dest.X += DestRect.Width;
		dest.Y = DestRect.Y;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, Colour);
		s.X = 0;
		dest.X = DestRect.X - 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, Colour);
	}

	public static void DrawRect(int TexId, Rectangle DestRect, int Alpha, int Shift = 0)
	{
		Color colour = new Color(Alpha >> Shift, Alpha >> Shift, Alpha >> Shift, Alpha);
		DrawRect(TexId, DestRect, colour);
	}

	public static void DrawRect(int TexId, Vector2 Position, Rectangle Source, Color Colour)
	{
		Rectangle destRect = new Rectangle((int)Position.X, (int)Position.Y, Source.Width, Source.Height);
		DrawRect(TexId, destRect, Colour);
	}

	public static void DrawRectStraightBottom(int TexId, Rectangle DestRect, int Alpha, int Shift = 0)
	{
		Rectangle s = default;
		Rectangle dest = DestRect;
		Vector2 pos = default;
		Color c = new Color(Alpha >> Shift, Alpha >> Shift, Alpha >> Shift, Alpha);
		s.X = (s.Y = 8);
		s.Width = (s.Height = 36);
		pos.X = DestRect.X - 8;
		pos.Y = DestRect.Y - 8;
		dest.Height += 7;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.X = 9;
		s.Y = 0;
		s.Width = 34;
		s.Height = 8;
		dest.Y -= 8;
		dest.Height = 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.Y = 51;
		s.Height = 1;
		dest.X -= 7;
		dest.Y = DestRect.Y + DestRect.Height + 7;
		dest.Width += 14;
		dest.Height = 1;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.Width = 8;
		s.Height = 8;
		s.Y = 0;
		s.X = 0;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, c);
		pos.X = DestRect.X + DestRect.Width;
		pos.Y = DestRect.Y - 8;
		s.X = 44;
		s.Y = 0;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, c);
		s.Height = 34;
		s.Y = 9;
		dest.Width = 8;
		dest.Height = DestRect.Height + 8;
		dest.X = DestRect.X + DestRect.Width;
		dest.Y = DestRect.Y;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.X = 0;
		dest.X = DestRect.X - 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
	}

	public static void DrawRectOpenAtBottom(int TexId, Rectangle DestRect, int Alpha, int Shift = 0)
	{
		Rectangle s = default;
		Rectangle dest = DestRect;
		Vector2 pos = default;
		Color c = new Color(Alpha >> Shift, Alpha >> Shift, Alpha >> Shift, Alpha);
		s.X = (s.Y = 8);
		s.Width = 36;
		s.Height = 36;
		dest.Height += 8;
		pos.X = DestRect.X - 8;
		pos.Y = DestRect.Y - 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.X = 9;
		s.Y = 0;
		s.Width = 34;
		s.Height = 8;
		dest.Y -= 8;
		dest.Height = 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.Width = 8;
		s.Y = 0;
		s.X = 0;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, c);
		pos.X = DestRect.X + DestRect.Width;
		pos.Y = DestRect.Y - 8;
		s.X = 44;
		s.Y = 0;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, c);
		s.Height = 34;
		s.Y = 9;
		dest.Width = 8;
		dest.Height = DestRect.Height + 8;
		dest.X += DestRect.Width;
		dest.Y = DestRect.Y;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.X = 0;
		dest.X = DestRect.X - 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
	}

	public static void DrawRectOpenAtTop(int TexId, Rectangle DestRect, int Alpha, int Shift = 0)
	{
		Rectangle s = default;
		Rectangle dest = DestRect;
		Vector2 pos = default;
		Color c = new Color(Alpha >> Shift, Alpha >> Shift, Alpha >> Shift, Alpha);
		s.X = (s.Y = 8);
		s.Width = (s.Height = 36);
		pos.X = DestRect.X - 8;
		pos.Y = DestRect.Y - 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.X = 9;
		s.Width = 34;
		s.Height = 8;
		dest.Height = 8;
		s.Y = 44;
		dest.Y = DestRect.Y + DestRect.Height;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.Width = 8;
		s.X = 0;
		s.Y = 44;
		pos.Y = DestRect.Y + DestRect.Height;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, c);
		pos.X = DestRect.X + DestRect.Width;
		s.X = 44;
		s.Y = 44;
		SpriteSheet<_sheetSprites>.Draw(TexId, ref pos, ref s, c);
		s.Height = 34;
		s.Y = 9;
		dest.Width = 8;
		dest.Height = DestRect.Height;
		dest.X += DestRect.Width;
		dest.Y = DestRect.Y;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
		s.X = 0;
		dest.X = DestRect.X - 8;
		SpriteSheet<_sheetSprites>.DrawStretched(TexId, s, ref dest, c);
	}

	public static void DrawRect(Rectangle DestRect, Color Colour, bool ToCenter = true)
	{
		DestRect.X += 2;
		DestRect.Y += 2;
		DestRect.Width -= 4;
		DestRect.Height -= 4;
		if (ToCenter)
		{
			SpriteBatch.Draw(WhiteTexture, DestRect, Colour);
			Colour.A >>= 3;
		}
		Rectangle destinationRectangle = DestRect;
		destinationRectangle.Y -= 2;
		destinationRectangle.Height = 2;
		SpriteBatch.Draw(WhiteTexture, destinationRectangle, Colour);
		destinationRectangle.X -= 2;
		destinationRectangle.Y += 2;
		destinationRectangle.Height = DestRect.Height;
		destinationRectangle.Width = 2;
		SpriteBatch.Draw(WhiteTexture, destinationRectangle, Colour);
		destinationRectangle.X += DestRect.Width + 2;
		SpriteBatch.Draw(WhiteTexture, destinationRectangle, Colour);
		destinationRectangle.X = DestRect.X;
		destinationRectangle.Y += DestRect.Height;
		destinationRectangle.Width = DestRect.Width;
		destinationRectangle.Height = 2;
		SpriteBatch.Draw(WhiteTexture, destinationRectangle, Colour);
	}

	public static void ShowSaveIcon()
	{
		if (SaveIconCounter <= 0)
		{
			SaveIconCounter = 180;
		}
		ActiveSaves++;
	}

	public static void HideSaveIcon()
	{
		ActiveSaves--;
	}

	public static bool IsSaveIconVisible()
	{
		if (SaveIconCounter <= 0)
		{
			return ActiveSaves > 0;
		}
		return true;
	}

	private void Quit()
	{
		Netplay.PlayDisconnect = true;
		Exit();
	}

	private static void SignedInGamer_SignedIn(object Sender, SignedInEventArgs Event)
	{
		IsTrial = Guide.IsTrialMode;
		CheckUserGeneratedContent = true;
	}

	private static void SignedInGamer_SignedOut(object Sender, SignedOutEventArgs Event)
	{
		PlayerIndex playerIndex = Event.Gamer.PlayerIndex;
		UIInstance[(int)playerIndex].SignOut();
	}
}
