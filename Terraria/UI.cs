using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using Terraria.Achievements;
using Terraria.CreateCharacter;
using Terraria.Hardmode;
using Terraria.HowToPlay;
using Terraria.Leaderboards;
using Terraria.SoundUI;
using Terraria.Storage;

namespace Terraria;

public sealed class UI
{
	public enum InventorySection : byte
	{
		CRAFTING,
		ITEMS,
		CHEST,
		EQUIP,
		HOUSING,
		NUM_SECTIONS
	}

	public enum CraftingSection : byte
	{
		RECIPES,
		INGREDIENTS
	}

	private enum StackType
	{
		NONE,
		INGREDIENT,
		INVENTORY,
		HOTBAR
	}

	private const ulong MARKETPLACE_OFFER_ID = 6359384554213474305uL;

	public static int FULLSCREEN_SAFE_AREA_OFFSET_X = Main.ResolutionWidth / 20;

	public static int FULLSCREEN_SAFE_AREA_OFFSET_Y = Main.ResolutionHeight / 20;

	public static int UsableWidth = (int)(864f * Main.ScreenMultiplier);

	public static int USABLE_HEIGHT = (int)(454f * Main.ScreenMultiplier);

	public const int CONTROLS_HUD_X = 0;

	public const int UI_DELAY = 12;

	public const int MOUSE_DELAY = 4;

	public const float GpDeadZone = 0.125f;

	public const float SquaredDeadZone = 1f / 64f;

	public const float WORLD_FADE_START = -0.25f;

	private const float WORLD_FADE_SPEED = 1f / 30f;

	private const float UI_FADE_SPEED = 1f / 30f;

	private const int MAX_ITEMS = 14;

	private const int MAX_DEPTH = 16;

	private const int MAX_LOAD_PLAYERS = 5;

	private const int MAX_LOAD_WORLDS = 5;

	public const int mcColorR = 125;

	public const int mcColorG = 125;

	public const int mcColorB = 255;

	public const int hcColorR = 200;

	public const int hcColorG = 125;

	public const int hcColorB = 255;

	public const float FONT_STACK_EXTRA_SCALE = 0.1f;

	public const Buttons BTN_JUMP = Buttons.A;

	public const Buttons BTN_INTERACT = Buttons.B;

	public const Buttons BTN_USE = Buttons.RightTrigger;

	public const Buttons BTN_DROP = Buttons.X;

	public const Buttons BTN_RESPAWN = Buttons.A;

	public const Buttons BTN_CURSOR_MODE = Buttons.RightStick;

	public const Buttons BTN_PREV_ITEM = Buttons.LeftShoulder;

	public const Buttons BTN_NEXT_ITEM = Buttons.RightShoulder;

	public const Buttons BTN_INVENTORY_SELL_OR_TRASH = Buttons.X;

	public const Buttons BTN_INVENTORY_SELECT = Buttons.A;

	public const Buttons BTN_INVENTORY_ACTION = Buttons.RightTrigger;

	public const Buttons BTN_INVENTORY_DROP = Buttons.X;

	public const Buttons BTN_INVENTORY_HOUSING = Buttons.LeftTrigger;

	public const Buttons BTN_INVENTORY_OPEN = Buttons.Y;

	public const Buttons BTN_INVENTORY_CLOSE = Buttons.B;

	public const Buttons BTN_NPC_CHAT_SELECT = Buttons.A;

	public const Buttons BTN_NPC_CHAT_CLOSE = Buttons.B;

	public const float LEFT_STICK_VERTICAL_THRESHOLD = 0.5f;

	public const int HOTBAR_ITEMNAME_DISPLAYTIME = 210;

	public const int QUICK_ACCESS_DISPLAYTIME = 120;

	private const int CRAFTING_INGREDIENT_COLS = 4;

	private const int CRAFTING_INGREDIENT_ROWS = 3;

	private const float CRAFTING_DEFAULT_SCROLL_MUL = 0.8125f;

	private const float CRAFTING_MIN_SCROLL_MUL = 0.25f;

	private const float CRAFTING_SCROLL_MUL_DECREMENT = 0.075f;

	private const int cooldownLen = 180;

	private const int MENU_TITLE_W = 1;

	private const int MENU_TITLE_H = 7;

	private const int MENU_PAUSE_W = 1;

	private const int MENU_PAUSE_H = 7;

	private const int MENU_SELECT_W = 1;

	private const int MENU_SELECT_H = 6;

	private const int MENU_CONFIRM_DELETE_W = 1;

	private const int MENU_CONFIRM_DELETE_H = 2;

	private const int MENU_WORLD_SIZE_W = 1;

	private const int MENU_WORLD_SIZE_H = 3;

	private const int MENU_OPTIONS_W = 1;

	private const int MENU_OPTIONS_H = 4;

	private const int MENU_SETTINGS_W = 1;

	private const int MENU_SETTINGS_H = 3;

	private static readonly int INVENTORY_W = UsableWidth;

	private static int INVENTORY_H = USABLE_HEIGHT - 8;

	private static readonly int INVENTORY_CLIENT_Y_OFFSET = (int)(80f * Main.ScreenMultiplier);

	private static readonly int INVENTORY_CLIENT_H = (int)(366f * Main.ScreenMultiplier);

	private static readonly int TOOLTIP_W = (int)(322f * Main.ScreenMultiplier);

	private const float INVENTORY_FADE = 0.5f;

	public static Color DISABLED_COLOR = new Color(16, 16, 16, 128);

	public static Color WINDOW_OUTLINE = new Color(12, 24, 24, 255);

	public static Color DefaultDialogColor = new Color(42, 43, 101, 192);

	private static int FONT_STACK_X_OFFSET = 0;

	private static int FONT_STACK_Y_OFFSET = 0;

	public static UI MainUI;

	public static UI CurrentUI;

	public static int NumActiveViews;

	public static WorldView[] activeView = new WorldView[4];

	public WorldView CurrentView;

	public LocalNetworkGamer localGamer;

	public SignedInGamer SignedInGamer;

	public PlayerIndex controller;

	public Player ActivePlayer;

	public byte MyPlayer = 8;

	private byte privateSlots;

	public NetPlayer netPlayer = new NetPlayer();

	public bool wasRemovedFromSessionWithoutOurConsent;

	public MenuType CurMenuType;

	public bool isStopping;

	public float worldFade;

	public float worldFadeTarget = 1f;

	public float uiFade = 1f;

	public float uiFadeTarget = 1f;

	public short oldMouseX;

	public short oldMouseY;

	public short MouseX;

	public short MouseY;

	public bool UsingSmartCursor = true;

	public bool alternateGrappleControls;

	public Buttons BTN_JUMP2 = Buttons.LeftStick;

	public Buttons BTN_GRAPPLE = Buttons.LeftTrigger;

	private sbyte quickAccessUp = -1;

	private sbyte quickAccessDown = -1;

	private sbyte quickAccessLeft = -1;

	private sbyte quickAccessRight = -1;

	public GamePadState gpPrevState;

	public GamePadState PadState;

	public StorageDeviceManager playerStorage;

	private List<string> transferredPlayerStorage = new List<string>(3);

	/// <summary>Number of saved characters (used slots), not the size of the arrays.</summary>
	public int numLoadPlayers;

	public string playerPathName;

	/// <summary>
	/// One entry per save slot. Grown as needed and never shrunk, so slot indices stay equal to
	/// the file numbers (<c>player3.plr</c>) - a deleted slot keeps its index and is reused by the
	/// next character. The old fixed five meant the sixth character simply had nowhere to go.
	/// </summary>
	public Player[] loadPlayer = new Player[0];

	/// <summary>File name per slot; null marks a free slot.</summary>
	public string[] loadPlayerPath = new string[0];

	/// <summary>Slot indices of the rows currently drawn (saved characters only, in file order).</summary>
	private readonly List<int> charRows = new List<int>();

	/// <summary>First visible row, so a list longer than the screen can be scrolled.</summary>
	private int charScroll;

	/// <summary>Rows that fit on screen this frame.</summary>
	private int charVisible = 6;

	/// <summary>Coordinates for the gamepad cursor, rebuilt to match the visible rows.</summary>
	private Location[] charCoords = new Location[0];

	/// <summary>
	/// Save slot the character currently being created will be written into. Chosen when "New"
	/// is pressed, because the name prompt runs one frame later and by then the free slot could
	/// have changed if the list were rescanned.
	/// </summary>
	private int creatingPlayerSlot = -1;

	private float logoRotation;

	private float logoRotationDirection = 1f;

	private float logoRotationSpeed = 1f;

	private float logoScale = 1f;

	private float logoScaleDirection = 1f;

	private float logoScaleSpeed = 1f;

	private short LogoA = 255;

	private short LogoB;

	public string statusText;

	private static string errorDescription;

	private static string errorCaption;

	private static CompiledText errorCompiledText;

	private static CompiledText saveIconMessage = null;

	private static int saveIconMessageTime = 0;

	public float MusicVolume = 0.75f;

	public float SoundVolume = 1f;

	public bool autoSave;

	public bool ShowItemText = true;

	public bool IsOnline;

	public bool IsInviteOnly;

	public bool SettingsDirty;

	private Stopwatch saveTime = new Stopwatch();

	private Color selColor = Color.White;

	private bool[] noFocus = new bool[14];

	private bool[] blockFocus = new bool[14];

	private short[] menuY = new short[14];

	private byte[] menuHC = new byte[14];

	private float[] menuScale = new float[14];

	private float[] menuItemScale = new float[14];

	private sbyte focusMenu = -1;

	private sbyte selectedMenu = -1;

	public sbyte selectedPlayer;

	public int menuDepth;

	public MenuMode CurMenuMode = MenuMode.WELCOME;

	/// <summary>
	/// How many slots the per-mode tables need. One place, derived from the enum, so a screen
	/// added later cannot leave an array behind - which is exactly what happened: the tables were
	/// hand-sized at 40, the enum grew to 47, and every screen added after CHEATS (Cheats,
	/// Keyboard bindings, Mods, Display, Network, HUD, Backups) crashed on entry with
	/// IndexOutOfRangeException pointing at SetMenu.
	///
	/// Declared before its uses on purpose: static field initialisers run in declaration order.
	/// </summary>
	internal static readonly int MenuSlots = (int)MenuMode.NUM_MENUS;

	/// <summary>
	/// Back-navigation stack. Sized from the enum as well: a deep path through the settings
	/// screens could overflow a fixed 16, and the failure was the same crash.
	/// </summary>
	public MenuMode[] prevMenuMode = new MenuMode[MenuSlots];

	public float Progress;

	private float progressTotal;

	private float numProgressStepsInv;

	private short uiX;

	private short uiY;

	private sbyte uiWidth;

	private sbyte uiHeight;

	private sbyte uiDelayValue;

	public sbyte uiDelay;

	public Location[] uiCoords;

	// One slot per MenuMode, indexed by the mode's own value.
	private Location[] uiPos = new Location[MenuSlots];

	public int cursorHighlight;

	public Terraria.CreateCharacter.UI CreateCharacterGUI;

	private Terraria.SoundUI.UI soundUI;

	private Terraria.HowToPlay.UI howtoUI;

	private Terraria.Hardmode.UI hardmodeUpsell;

	private AchievementsUI Achievements;

	private TextSequenceBlock tips;

	private LeaderboardsUI Leaderboards;

	public int hotbarItemNameTime = 210;

	public int quickAccessDisplayTime;

	public float[] hotbarScale = new float[10] { 1f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f };

	private static float inventoryScale;

	/// <summary>
	/// True when the pointer is over an interface element, so a click belongs to the UI and
	/// not to the player. Mirrors the PC version's <c>mouseInterface</c>.
	/// </summary>
	public bool CursorOverInterface => InventoryMode > 0 || CurMenuType != MenuType.NONE || ChatOpen;

	/// <summary>
	/// True while the NPC / sign dialog is up. It is a screen, but it lives in gameplay, so
	/// <see cref="CurMenuType"/> says NONE and the input layer treated a click in it as "use
	/// item": the pickaxe swung and the option was never activated. The dialog handles the
	/// click itself, through rectangles recorded while drawing, so the input layer has to know
	/// it is looking at a menu.
	/// </summary>
	public bool ChatOpen => npcChatText != null || (ActivePlayer != null && ActivePlayer.sign != -1);

	/// <summary>
	/// True while the mouse is the active pointing device. Screens use it to suppress the
	/// console selection cursor: drawing both leaves a sprite "stuck" on one menu row while
	/// the real pointer moves elsewhere.
	/// </summary>
	public static bool PointerActive => Terraria.Input.KeyboardMouseInput.Enabled && Terraria.Input.KeyboardMouseInput.CursorOverride;

	// Bounds of the visible menu entries, recorded while drawing. Used for real hit-testing
	// so a click activates the entry under the pointer and nothing else.
	private readonly Rectangle[] menuHitRects = new Rectangle[14];

	private readonly bool[] menuHitValid = new bool[14];

	/// <summary>True when the last drawn frame contained at least one clickable entry.</summary>
	private bool menuHitAny;

	/// <summary>Entry index under the pointer, or -1. Valid for the last drawn frame.</summary>
	public int MenuEntryUnderPointer
	{
		get
		{
			for (int i = 0; i < menuHitValid.Length; i++)
			{
				if (menuHitValid[i] && menuHitRects[i].Contains(MouseX, MouseY))
				{
					return i;
				}
			}
			return -1;
		}
	}

	/// <summary>
	/// True when a click must be swallowed: a menu with real entries is open, but the pointer
	/// is not over any of them. Without this a click anywhere activated whichever entry
	/// happened to be highlighted - the "only PvP is clickable" symptom.
	/// </summary>
	public bool ClickOutsideMenuEntries => menuHitAny && MenuEntryUnderPointer < 0;

	/// <summary>
	/// Moves the map screen's console cursor from the pointer, so the highlight follows the mouse
	/// even though the cursor itself is only drawn once per frame.
	/// </summary>
	internal void SetMapCursor(int X, int Y)
	{
		mapScreenCursorX = X;
		mapScreenCursorY = Y;
	}

	/// <summary>
	/// Opens the player card for one row of the map screen's player list (0 = first row), for the
	/// mouse. The console path does the same thing from the cursor position; both go through the
	/// checks here so a click cannot reach a gamer card the gamepad could not.
	/// </summary>
	internal void OpenMapGamerCard(int Row)
	{
		if (!CanViewGamerCard() || Netplay.Session == null)
		{
			return;
		}
		GamerCollection<NetworkGamer> allGamers = Netplay.Session.AllGamers;
		if (Row < 0 || Row >= allGamers.Count)
		{
			return;
		}
		NetworkGamer gamer = allGamers[Row];
		if (gamer != null)
		{
			ShowGamerCard(gamer);
		}
	}

	/// <summary>
	/// Forgets every recorded entry. Called once per draw, before the entries of the current
	/// screen are recorded, so no screen can inherit another screen's hit rectangles.
	/// </summary>
	internal void ClearMenuHits()
	{
		for (int i = 0; i < menuHitValid.Length; i++)
		{
			menuHitValid[i] = false;
		}
		menuHitAny = false;
	}

	/// <summary>Test hook: records one entry rectangle as if it had just been drawn.</summary>
	internal void AddMenuHit(int index, Rectangle rect)
	{
		if (index < 0 || index >= menuHitRects.Length)
		{
			return;
		}
		menuHitRects[index] = rect;
		menuHitValid[index] = true;
		menuHitAny = true;
	}

	// Inventory slot grid, captured while drawing so mouse hit-testing matches the layout.
	private Rectangle invMouseGrid;

	private float invMouseCell;

	/// <summary>
	/// True on the frame the pointer clicked on an inventory slot.
	///
	/// Slot actions must not depend on <c>Buttons.A</c>: that button is only synthesised when the
	/// pointer's click is not "handled by the screen", and the inventory is exactly such a screen.
	/// So a click inside the inventory produced no <c>A</c> at all, and every slot action was
	/// unreachable by mouse while the tabs (which read the pointer directly) worked.
	/// </summary>
	private bool pointerActivate;

	/// <summary>
	/// True for the rest of the frame in which the pointer already acted on a slot.
	///
	/// The same handler is reachable from two places - the pointer path and
	/// <see cref="PositionInventoryCursor"/> - and both run every frame while the inventory is
	/// open. <see cref="IsButtonTriggered"/> is a pure edge test that is not consumed, so the
	/// action ran twice: the item was picked up and immediately put back, which looks exactly like
	/// "the mouse does nothing" and is why only a slot whose second call was a no-op appeared to
	/// work.
	/// </summary>
	private readonly FrameGate inventoryAction = new FrameGate();

	/// <summary>Test hook: the gate the inventory uses to act once per frame.</summary>
	internal FrameGate InventoryActionGate => inventoryAction;

	private bool invMouseLayoutValid;

	/// <summary>True when the player asked to act on the slot under the pointer.</summary>
	private bool ActivatePressed()
	{
		return pointerActivate || IsButtonTriggered(Buttons.A);
	}

	/// <summary>Equip-section slots, rebuilt while drawing so a click lands on the drawn square.</summary>
	private readonly Rectangle[] equipSlotRects = new Rectangle[16];

	private readonly sbyte[] equipSlotX = new sbyte[16];

	private readonly sbyte[] equipSlotY = new sbyte[16];

	private readonly bool[] equipSlotValid = new bool[16];

	/// <summary>Housing-section slots, same idea.</summary>
	private readonly Rectangle[] housingSlotRects = new Rectangle[11];

	private readonly sbyte[] housingSlotX = new sbyte[11];

	private readonly sbyte[] housingSlotY = new sbyte[11];

	private readonly bool[] housingSlotValid = new bool[11];

	/// <summary>Chest, bank and safe grid, recorded while drawing.</summary>
	private readonly Rectangle[] chestSlotRects = new Rectangle[20];

	private readonly sbyte[] chestSlotX = new sbyte[20];

	private readonly sbyte[] chestSlotY = new sbyte[20];

	private readonly bool[] chestSlotValid = new bool[20];

	/// <summary>
	/// Loot all / deposit / quick stack, drawn in their own column beside the chest grid.
	/// Indexed by the same 1..3 value the cursor uses, so a click can set the cursor exactly
	/// the way the stick does.
	/// </summary>
	private readonly Rectangle[] chestButtonRects = new Rectangle[4];

	private readonly bool[] chestButtonValid = new bool[4];

	/// <summary>Shop rows, recorded while drawing.</summary>
	private readonly Rectangle[] shopSlotRects = new Rectangle[20];

	private readonly bool[] shopSlotValid = new bool[20];

	/// <summary>Coin row, ammo row and trash slot, for the pointer hit-test.</summary>
	private Rectangle invMouseCoin;

	private bool invMouseCoinValid;

	private Rectangle invMouseAmmo;

	private bool invMouseAmmoValid;

	private Rectangle invMouseTrash;

	private bool invMouseTrashValid;

	/// <summary>
	/// On-screen rectangles of the five inventory tabs, recorded while drawing. Clicking a tab
	/// is the most obvious thing a mouse user will try, and it was the one part of the inventory
	/// that had no hit-testing at all.
	/// </summary>
	private readonly Rectangle[] inventorySectionRects = new Rectangle[5];

	private readonly bool[] inventorySectionRectValid = new bool[5];

	/// <summary>
	/// The six crafting category tabs, recorded while drawing. They had no hit-testing at all,
	/// so on a mouse the only way to change category was the shoulder buttons.
	/// </summary>
	private readonly Rectangle[] craftingCategoryRects = new Rectangle[6];

	private readonly bool[] craftingCategoryRectValid = new bool[6];

	public float[] inventoryMenuSectionScale = new float[5] { 0.75f, 0.75f, 1f, 0.75f, 0.75f };

	public byte InventoryMode;

	private bool restoreOldInventorySection;

	/// <summary>
	/// The cheat screen was opened straight from play with F1, so closing it resumes the game
	/// instead of returning to the pause menu it was never opened from.
	/// </summary>
	private bool cheatsFromPlay;

	private InventorySection oldInventorySection;

	public InventorySection ActiveInvSection = InventorySection.ITEMS;

	private sbyte inventoryItemX;

	private sbyte inventoryItemY;

	private sbyte inventoryChestX;

	private sbyte inventoryChestY;

	private sbyte inventoryEquipX;

	private sbyte inventoryEquipY = 1;

	private sbyte inventoryBuffX;

	private sbyte inventoryHousingX;

	private sbyte inventoryHousingY;

	private short inventoryHousingNpc = -1;

	public Recipe.Category craftingCategory;

	private CraftingSection craftingSection;

	public bool craftingShowCraftable;

	public sbyte CraftingRecipeX;

	/// <summary>
	/// Index of the recipe drawn in the leftmost cell of the selected row.
	///
	/// This used to be <see cref="CraftingRecipeX"/> itself, which works for a stick - the row
	/// slides and the cursor stays on the first cell - but not for a pointer. Hovering cell 3
	/// of a row whose window started at 0 selected recipe 3, which moved the window to 3, so
	/// cell 3 now showed recipe 6, and the next frame selected that: the grid scrolled by
	/// itself for as long as the pointer rested on it. The window is what the stick and the
	/// wheel move; the pointer moves only the cursor inside it.
	/// </summary>
	public sbyte CraftingRecipeWindowX;

	public sbyte CraftingRecipeY;

	private sbyte craftingIngredientX;

	private sbyte craftingIngredientY;

	private float craftingRecipeScrollX;

	private float craftingRecipeScrollY;

	public Recipe CraftingRecipe;

	private float craftingRecipeScrollMul = 0.8125f;

	public short stackSplit;

	public short stackCounter;

	public short stackDelay = 7;

	private sbyte mouseItemSrcX;

	private sbyte mouseItemSrcY;

	private InventorySection mouseItemSrcSection = InventorySection.NUM_SECTIONS;

	public Item mouseItem;

	public Item trashItem;

	public Item GuideItem;

	private Item toolTip;

	public List<Recipe.SubCategoryList> CurrentRecipeCategory = new List<Recipe.SubCategoryList>();

	public byte npcShop;

	public bool CraftGuide;

	public bool reforge;

	public string chestText;

	public bool editSign;

	public bool signBubble;

	public int signX;

	public int signY;

	public UserString npcChatText;

	public string npcCompiledChatText;

	private CompiledText npcChatCompiledText;

	public short helpText;

	public sbyte npcChatSelectedItem;

	public bool showNPCs;

	public MiniMap miniMap = new MiniMap();

	private int mapScreenCursorX;

	private int mapScreenCursorY;

	public byte teamSelected;

	public bool pvpSelected;

	public short teamCooldown;

	public short pvpCooldown;

	public Statistics Statistics;

	public TriggerSystem AchievementTriggers;

	public uint TotalJumps;

	public uint TotalChopsTaken;

	public uint TotalDeadSlimes;

	public uint TotalWoodAxed;

	public uint TotalCopperObtained;

	public uint TotalDoorsOpened;

	public uint TotalDoorsClosed;

	public uint TotalWoodPlatformsPlaced;

	public uint TotalWallsPlaced;

	public uint TotalTorchesCrafted;

	public uint TotalWoodPlatformsCrafted;

	public uint TotalWallsCrafted;

	public uint totalSteps;

	public uint TotalBarsCrafted;

	public uint TotalOrePicked;

	public uint TotalAnvilCrafting;

	public uint totalWires;

	public uint totalAirTime;

	public uint currentAirTime;

	public uint RunningTimer;

	public float airTravel;

	public byte petSpawnMask;

	private BitArray armorFound = new BitArray(632);

	private readonly List<ulong> blacklist = new List<ulong>();

	private static Main theGame;

	/// <summary>The game instance, exposed so the display menu can apply window changes.</summary>
	internal static Main Game => theGame;

	public static byte MouseTextBrightness = 175;

	private static sbyte mouseTextColorChange = 2;

	public static Color mouseTextColor = new Color(175, 175, 175, 175);

	public static Color mouseColor = new Color(255, 95, 180);

	public static Color CursorColour = Color.White;

	public static float cursorAlpha = 0f;

	public static float cursorScale = 0f;

	public static byte invAlpha = 180;

	private static sbyte invDir = 1;

	public static float PulseScale = 1f;

	private static float essDir = -0.01f;

	public static float BlueWave = 1f;

	private static float blueDelta = -0.0005f;

	public static bool HasQuit = false;

	public static Texture2D logoTexture;

	public static Texture2D logo2Texture;

	public static Texture2D controlsTexture;

	public static Texture2D dpadTexture;

	public static Texture2D progressBarTexture;

	public static Texture2D textBackTexture;

	public static Texture2D chatBackTexture;

	public static Texture2D cursorTexture;

	public static SpriteFont BigFont;

	public static SpriteFont SmallFont;

	public static SpriteFont BoldSmallFont;

	public static SpriteFont ItemStackFont;

	public static SpriteFont[] CombatTextFont = new SpriteFont[2];

	public static CompiledText.Style BoldSmallTextStyle;

	private static readonly short ViewportHalfWidth = (short)(Main.ResolutionWidth / 2);

	private static readonly Location[] MENU_TITLE_COORDS = new Location[8]
	{
		new Location(Main.ResolutionWidth / 2, 199),
		new Location(Main.ResolutionWidth / 2, 237),
		new Location(Main.ResolutionWidth / 2, 275),
		new Location(Main.ResolutionWidth / 2, 313),
		new Location(Main.ResolutionWidth / 2, 351),
		new Location(Main.ResolutionWidth / 2, 399),
		new Location(Main.ResolutionWidth / 2, 437),
		new Location(Main.ResolutionWidth / 2, 475)
	};

	private static readonly Location[] MENU_PAUSE_COORDS = new Location[8]
	{
		new Location(Main.ResolutionWidth / 2, 221),
		new Location(Main.ResolutionWidth / 2, 259),
		new Location(Main.ResolutionWidth / 2, 297),
		new Location(Main.ResolutionWidth / 2, 334),
		new Location(Main.ResolutionWidth / 2, 372),
		new Location(Main.ResolutionWidth / 2, 421),
		new Location(Main.ResolutionWidth / 2, 459),
		// Eighth slot: the trial build inserts one extra row, which pushes the cheat entry to
		// index 7. It used to write past the end of this array.
		new Location(Main.ResolutionWidth / 2, 497)
	};

	private static Location[] MENU_CONFIRM_DELETE_COORDS = new Location[2]
	{
		new Location(Main.ResolutionWidth / 2, 356),
		new Location(Main.ResolutionWidth / 2, 437)
	};

	private static readonly Location[] MENU_WORLD_SIZE_COORDS = new Location[3]
	{
		new Location(Main.ResolutionWidth / 2, 307),
		new Location(Main.ResolutionWidth / 2, 367),
		new Location(Main.ResolutionWidth / 2, 415)
	};

	private static readonly Location[] MENU_OPTIONS_COORDS = new Location[4]
	{
		new Location(Main.ResolutionWidth / 2, 253),
		new Location(Main.ResolutionWidth / 2, 313),
		new Location(Main.ResolutionWidth / 2, 372),
		new Location(Main.ResolutionWidth / 2, 432)
	};

	private static readonly Location[] MENU_SETTINGS_COORDS = new Location[9]
	{
		new Location(Main.ResolutionWidth / 2, 200),
		new Location(Main.ResolutionWidth / 2, 244),
		new Location(Main.ResolutionWidth / 2, 288),
		new Location(Main.ResolutionWidth / 2, 332),
		new Location(Main.ResolutionWidth / 2, 376),
		new Location(Main.ResolutionWidth / 2, 420),
		new Location(Main.ResolutionWidth / 2, 464),
		new Location(Main.ResolutionWidth / 2, 508),
		new Location(Main.ResolutionWidth / 2, 552)
	};

	private string[] menuString = new string[14];

	private byte numMenuItems;

	private sbyte oldMenu;

	private sbyte showPlayer = -1;

	private byte menuSpace = 80;

	private short menuTop = 250;

	private short menuLeft = 480;

	public bool inputTextEnter;

	public bool inputTextCanceled;

	private IAsyncResult kbResult;

	private string focusText;

	private string focusText3;

	private Color focusColor;

	private static CompiledText compiledToolTipText;

	private static string toolTipText;

	private static Item cpItem = default;

	private static readonly float TextScale = 1f;

	public static void Error(string caption, string desc, bool rememberPreviousMenu = false)
	{
		errorCompiledText = null;
		errorCaption = caption;
		errorDescription = desc;
		MainUI.SetMenu(MenuMode.ERROR, rememberPreviousMenu);
	}

	public void InitGame()
	{
		wasRemovedFromSessionWithoutOurConsent = false;
		restoreOldInventorySection = false;
		InventoryMode = 0;
		ActiveInvSection = InventorySection.ITEMS;
		inventoryItemX = 0;
		inventoryItemY = 0;
		inventoryChestX = 0;
		inventoryChestY = 0;
		inventoryEquipX = 0;
		inventoryEquipY = 1;
		inventoryBuffX = 0;
		inventoryHousingX = 0;
		inventoryHousingY = 0;
		inventoryHousingNpc = -1;
		craftingCategory = Recipe.Category.STRUCTURES;
		craftingSection = CraftingSection.RECIPES;
		craftingShowCraftable = false;
		CraftingRecipeX = 0;
		CraftingRecipeWindowX = 0;
		CraftingRecipeY = 0;
		craftingIngredientX = 0;
		craftingIngredientY = 0;
		craftingRecipeScrollX = 0f;
		craftingRecipeScrollY = 0f;
		mouseItem.Init();
		trashItem.Init();
		GuideItem.Init();
		toolTip.Init();
		helpText = 0;
		showNPCs = false;
		mapScreenCursorX = 0;
		mapScreenCursorY = 0;
		teamSelected = 0;
		pvpSelected = false;
		teamCooldown = 0;
		pvpCooldown = 0;
		npcShop = 0;
		CraftGuide = false;
		reforge = false;
		editSign = false;
		signBubble = false;
		npcChatText = null;
		npcChatSelectedItem = 0;
		ActivePlayer.hostile = false;
		ActivePlayer.NetClone(netPlayer);
		if (!Main.IsTrial)
		{
			InitializeAchievementTriggers();
		}
	}

	private void InitializeAchievementTriggers()
	{
		AchievementTriggers.ReadProfile(SignedInGamer);
	}

	public bool TriggerCheckEnabled(Trigger trigger)
	{
		return AchievementTriggers.CheckEnabled(trigger);
	}

	public void SetTriggerState(Trigger trigger)
	{
		AchievementTriggers.SetState(trigger, State: true);
	}

	public static void SetTriggerStateForAll(Trigger trigger)
	{
		for (int i = 0; i < 8; i++)
		{
			Player player = Main.PlayerSet[i];
			if (player.Active != 0)
			{
				player.AchievementTrigger(trigger);
			}
		}
	}

	public static void IncreaseStatisticForAll(StatisticEntry entry)
	{
		if (entry == StatisticEntry.Unknown)
		{
			return;
		}
		for (int i = 0; i < 8; i++)
		{
			Player player = Main.PlayerSet[i];
			if (player.Active != 0)
			{
				player.IncreaseStatistic(entry);
			}
		}
	}

	private void UpdateAchievements()
	{
		if (Statistics.AllSlimeTypesKilled)
		{
			SetTriggerState(Trigger.AllSlimesKilled);
		}
		if (Statistics.AllBossesKilled)
		{
			SetTriggerState(Trigger.AllBossesKilled);
		}
		if (Statistics.MechBossesKilledTwice)
		{
			SetTriggerState(Trigger.BackForSeconds);
		}
		AchievementTriggers.UpdateAchievements(SignedInGamer);
		// Publish this player's achievements and counters so the master server can answer "how
		// many players earned this" and rank the leaderboards. Throttled inside StatSync, and the
		// snapshot is taken here, on the game thread - the background thread only owns the HTTP.
		if (SignedInGamer != null)
		{
			Terraria.Net.StatSync.Report(Statistics, SignedInGamer.Gamertag);
		}
	}

	public static void Initialize(Main game)
	{
		theGame = game;
		Terraria.HowToPlay.UI.GenerateCache(game.GraphicsDevice);
		TextSequenceBlock.GenerateCache(game.GraphicsDevice);
		if (Main.HardmodeAlert)
		{
			Terraria.Hardmode.UI.GenerateCache(theGame.GraphicsDevice);
		}
	}

	/// <summary>
	/// Grows the save-slot arrays to at least <paramref name="Count"/> entries, filling new slots
	/// with empty players. Never shrinks: a slot index is a file number, and renumbering it would
	/// point at somebody else's save.
	/// </summary>
	private void EnsurePlayerSlots(int Count)
	{
		if (Count <= loadPlayer.Length)
		{
			return;
		}
		int num = loadPlayer.Length;
		Array.Resize(ref loadPlayer, Count);
		Array.Resize(ref loadPlayerPath, Count);
		for (int i = num; i < Count; i++)
		{
			loadPlayer[i] = new Player();
			loadPlayerPath[i] = null;
		}
	}

	/// <summary>Number of rows on the character screen: saved characters plus "New".</summary>
	private int CharacterRowCount()
	{
		return charRows.Count + 1;
	}

	/// <summary>
	/// First free save slot: the lowest slot with no file, or one past the end so the list can
	/// grow. Gaps are reused, which is what makes deleting a character and creating a new one
	/// leave the same number of files rather than ever-higher numbers.
	/// </summary>
	private int NextFreePlayerSlot()
	{
		for (int i = 0; i < loadPlayerPath.Length; i++)
		{
			if (string.IsNullOrEmpty(loadPlayerPath[i]))
			{
				return i;
			}
		}
		return loadPlayerPath.Length;
	}

	/// <summary>
	/// How many character rows fit between the top of the list and the control hints. The list
	/// used to be six fixed rows, so the fifth character filled it and the sixth was impossible.
	/// </summary>
	private int CharacterVisibleRows()
	{
		int num = (int)(menuSpace * Main.ScreenMultiplier);
		int num2 = Main.ResolutionHeight - (int)(menuTop * Main.ScreenMultiplier) - (int)(90f * Main.ScreenMultiplier);
		// The menu row arrays hold 14 entries; drawing more would need a wider table, and 14 rows
		// is already more than fits on any preset.
		int num3 = SaveSlots.VisibleRows(num2, num);
		return Math.Min(num3, 14);
	}

	/// <summary>
	/// Rebuilds the gamepad cursor positions for the character rows. Same numbers as the drawing
	/// loop (menuTop/menuSpace), because the stick path moves the pointer to these and the mouse
	/// path hit-tests the drawn rectangles - if the two disagree, the cursor lands between rows.
	///
	/// One entry per row, not per visible row: the cursor is an absolute row index and the window
	/// follows it, so a list longer than the screen can still be walked with the D-pad. The rows
	/// that are scrolled out have coordinates outside the screen, which is harmless - they are
	/// only read when the cursor is on them, and then the window has already moved.
	/// </summary>
	private void BuildCharacterCoords(int Rows)
	{
		if (charCoords.Length != Rows)
		{
			charCoords = new Location[Rows];
		}
		for (int i = 0; i < Rows; i++)
		{
			charCoords[i] = new Location(ViewportHalfWidth,
				(short)((menuTop + menuSpace * i) * Main.ScreenMultiplier + (int)(16f * Main.ScreenMultiplier)));
		}
	}

	public void Initialize(PlayerIndex controller)	{
		this.controller = controller;
		CurrentUI = this;
		if (MainUI == null)
		{
			MainUI = this;
		}
		for (int num = 13; num >= 0; num--)
		{
			menuItemScale[num] = 0.8f;
		}
		// Five empty slots up front: the console build always showed six rows (five characters and
		// "New"), and an empty list should look the same now.
		EnsurePlayerSlots(5);
		CreateCharacterGUI = Terraria.CreateCharacter.UI.Create(this);
		soundUI = Terraria.SoundUI.UI.Create(this);
		howtoUI = Terraria.HowToPlay.UI.Create(this);
		tips = TextSequenceBlock.CreateTips();
		Statistics = Statistics.Create();
		AchievementTriggers = new TriggerSystem();
		Leaderboards = new LeaderboardsUI(this);
		Achievements = new AchievementsUI(this);
		if (Main.HardmodeAlert)
		{
			hardmodeUpsell = Terraria.Hardmode.UI.Create(this);
		}
	}

	private void SetDefaultSettings()
	{
		SoundVolume = 1f;
		MusicVolume = 0.75f;
		if (this == MainUI)
		{
			Main.MusicVolume = MusicVolume;
			Main.SoundVolume = SoundVolume;
		}
		autoSave = true;
		ShowItemText = true;
		alternateGrappleControls = false;
		UpdateAlternateGrappleControls();
		Statistics.Init();
		totalSteps = 0u;
		TotalOrePicked = 0u;
		TotalBarsCrafted = 0u;
		TotalAnvilCrafting = 0u;
		totalWires = 0u;
		totalAirTime = 0u;
		petSpawnMask = 0;
		armorFound.SetAll(value: false);
		IsOnline = false;
		IsInviteOnly = false;
		blacklist.Clear();
		SettingsDirty = false;
	}

	private void InitPlayerStorage()
	{
		SetDefaultSettings();
		numLoadPlayers = 0;
		if (SignedInGamer.IsGuest || Main.IsTrial)
		{
			playerStorage = null;
			return;
		}
		if (playerStorage == null)
		{
			playerStorage = new StorageDeviceManager(theGame, controller, 196608);
			playerStorage.DeviceSelectorCanceled += DeviceSelectorCanceled;
			playerStorage.DeviceDisconnected += DeviceDisconnected;
			playerStorage.DeviceSelected += DeviceSelected;
			theGame.Components.Add(playerStorage);
		}
		if (playerStorage.Device == null)
		{
			playerStorage.PromptForDevice();
		}
	}

	public bool HasPlayerStorage()
	{
		if (playerStorage != null)
		{
			return playerStorage.Device != null;
		}
		return false;
	}

	public bool CanViewGamerCard()
	{
		if (SignedInGamer.IsSignedInToLive)
		{
			_ = SignedInGamer.Privileges.AllowProfileViewing;
			return false;
		}
		return false;
	}

	public bool HasOnline()
	{
		if (!Main.IsTrial)
		{
			_ = SignedInGamer.IsSignedInToLive;
			return false;
		}
		return false;
	}

	public bool HasOnlineWithPrivileges()
	{
		if (!Main.IsTrial && SignedInGamer.IsSignedInToLive)
		{
			_ = SignedInGamer.Privileges.AllowOnlineSessions;
			return false;
		}
		return false;
	}

	public static bool IsUserGeneratedContentAllowed()
	{
		GamerCollection<NetworkGamer> gamerCollection = ((Netplay.Session != null) ? Netplay.Session.RemoteGamers : null);
		SignedInGamerCollection signedInGamers = Gamer.SignedInGamers;
		for (int num = signedInGamers.Count - 1; num >= 0; num--)
		{
			SignedInGamer signedInGamer = signedInGamers[num];
			if (!signedInGamer.IsGuest && signedInGamer.IsSignedInToLive)
			{
				if (signedInGamer.Privileges.AllowUserCreatedContent == GamerPrivilegeSetting.Blocked)
				{
					return false;
				}
				if (gamerCollection != null && signedInGamer.Privileges.AllowUserCreatedContent == GamerPrivilegeSetting.FriendsOnly)
				{
					for (int num2 = gamerCollection.Count - 1; num2 >= 0; num2--)
					{
						NetworkGamer gamer = gamerCollection[num2];
						if (!signedInGamer.IsFriend(gamer))
						{
							return false;
						}
					}
				}
			}
		}
		return true;
	}

	public bool CanPlayOnline()
	{
		// The Xbox Live gate is gone with the console service; the built-in TCP transport
		// provides LAN/Internet play without any sign-in, so multiplayer is always available.
		if (Terraria.Net.NetworkConfig.UseTcpTransport)
		{
			return IsUserGeneratedContentAllowed();
		}
		if (HasOnlineWithPrivileges())
		{
			return IsUserGeneratedContentAllowed();
		}
		return false;
	}

	/// <summary>
	/// True when multiplayer can be started at all. Xbox Live titles required a Live account;
	/// the built-in transport only needs a network stack, which is always present on desktop
	/// and on handheld consoles running this build.
	/// </summary>
	public bool IsNetworkAvailable()
	{
		return Terraria.Net.NetworkConfig.UseTcpTransport || HasOnline();
	}

	public bool CanCommunicate()
	{
		return SignedInGamer.Privileges.AllowCommunication != GamerPrivilegeSetting.Blocked;
	}

	public static bool AllPlayersCanPlayOnline()
	{
		if (IsUserGeneratedContentAllowed())
		{
			for (int i = 0; i < 4; i++)
			{
				UI uI = Main.UIInstance[i];
				if (uI.SignedInGamer != null && !uI.HasOnlineWithPrivileges())
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public static void LoadContent(ContentManager Content)
	{
		// Sequential version, kept for callers that need everything at once. The loading
		// screen uses AppendLoadTasks instead, one task per frame.
		System.Collections.Generic.List<LoadTask> list = new System.Collections.Generic.List<LoadTask>();
		AppendLoadTasks(list, Content);
		for (int i = 0; i < list.Count; i++)
		{
			list[i].Run();
		}
	}

	/// <summary>
	/// Registers the interface assets as individual loading tasks so the progress bar can
	/// advance per asset. Weights approximate real cost (font atlases dominate).
	/// </summary>
	public static void AppendLoadTasks(System.Collections.Generic.List<LoadTask> tasks, ContentManager Content)
	{
		tasks.Add(new LoadTask("character creation", 4f, delegate { Terraria.CreateCharacter.Assets.LoadContent(Content); }));
		tasks.Add(new LoadTask("sound ui", 1f, delegate { Terraria.SoundUI.Assets.LoadContent(Content); }));
		tasks.Add(new LoadTask("how to play", 2f, delegate { Terraria.HowToPlay.Assets.LoadContent(Content); }));
		tasks.Add(new LoadTask("leaderboards", 3f, delegate { Terraria.Leaderboards.Assets.LoadContent(Content); }));
		tasks.Add(new LoadTask("logo", 1f, delegate { logoTexture = Content.Load<Texture2D>("Images/Logo"); }));
		tasks.Add(new LoadTask("logo 2", 1f, delegate { logo2Texture = Content.Load<Texture2D>("Images/Logo2"); }));
		tasks.Add(new LoadTask("achievements", 4f, delegate { Terraria.Achievements.Assets.LoadContent(Content); }));
		tasks.Add(new LoadTask("hardmode upsell", 1f, delegate
		{
			if (Main.HardmodeAlert)
			{
				Terraria.Hardmode.Assets.LoadContent(Content);
			}
		}));
		tasks.Add(new LoadTask("controller art", 3f, delegate
		{
			string text = "UI/XBController";
			string text2 = "UI/XBDPad";
			if (Main.PSMode)
			{
				text = "UI/PSController";
				text2 = "UI/PSDPad";
			}
			if (Main.ScreenHeightPtr == ScreenHeights.FHD)
			{
				text += "HD";
				text2 += "HD";
			}
			controlsTexture = Content.Load<Texture2D>(text);
			dpadTexture = Content.Load<Texture2D>(text2);
		}));
		tasks.Add(new LoadTask("interface chrome", 1f, delegate
		{
			progressBarTexture = Content.Load<Texture2D>("UI/ProgressBar");
			textBackTexture = Content.Load<Texture2D>("Images/Text_Back");
			chatBackTexture = Content.Load<Texture2D>("Images/Chat_Back");
			cursorTexture = Content.Load<Texture2D>("Images/Cursor");
		}));
		AppendFontTasks(tasks, Content);
	}

	/// <summary>
	/// Font atlases are the single most expensive asset group (tens of megabytes), so each
	/// texture gets its own task and a weight proportional to that cost.
	/// </summary>
	private static void AppendFontTasks(System.Collections.Generic.List<LoadTask> tasks, ContentManager Content)
	{
		string prefix = "Fonts/";
		string big = "big";
		string stack = "stack";
		string combat = "combat";
		string combat2 = "combat2";
		string small = "small";
		string small2 = "small2";
		if (Main.PSMode)
		{
			big += "_PS3";
			small += "_PS3";
			small2 += "_PS3";
		}
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			prefix += "HD/";
		}
		// Capture locally so each task closure reads its own name.
		string[] names = new string[6] { big, stack, combat, combat2, small, small2 };
		float[] weights = new float[6] { 12f, 1f, 1f, 1f, 4f, 6f };
		Texture2D[] textures = new Texture2D[6];
		for (int i = 0; i < 6; i++)
		{
			int index = i;
			tasks.Add(new LoadTask("font " + names[i], weights[i], delegate
			{
				textures[index] = Content.Load<Texture2D>(prefix + names[index]);
			}));
		}
		tasks.Add(new LoadTask("build fonts", 2f, delegate
		{
			BuildFonts(textures[0], textures[1], textures[2], textures[3], textures[4], textures[5]);
		}));
	}

	public static void LoadFonts(ContentManager Content)
	{
		string text = "Fonts/";
		string text2 = "big";
		string text3 = "stack";
		string text4 = "combat";
		string text5 = "combat2";
		string text6 = "small";
		string text7 = "small2";
		if (Main.PSMode)
		{
			text2 += "_PS3";
			text6 += "_PS3";
			text7 += "_PS3";
		}
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			text += "HD/";
		}
		BuildFonts(
			Content.Load<Texture2D>(text + text2),
			Content.Load<Texture2D>(text + text3),
			Content.Load<Texture2D>(text + text4),
			Content.Load<Texture2D>(text + text5),
			Content.Load<Texture2D>(text + text6),
			Content.Load<Texture2D>(text + text7));
	}

	/// <summary>
	/// Builds the SpriteFont objects from already-loaded atlas textures. Split out from
	/// loading so the atlases can be fetched one per frame by the loading screen.
	/// </summary>
	private static void BuildFonts(Texture2D texture2D, Texture2D texture2D2, Texture2D texture2D3, Texture2D texture2D4, Texture2D texture2D5, Texture2D texture2D6)
	{
		ConstructorInfo constructor = typeof(SpriteFont).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[8]
		{
			typeof(Texture2D),
			typeof(List<Rectangle>),
			typeof(List<Rectangle>),
			typeof(List<char>),
			typeof(int),
			typeof(float),
			typeof(List<Vector3>),
			typeof(char?)
		}, null);
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			FontHD fontHD = new FontHD();
			FontHDBig fontHDBig = new FontHDBig();
			FontHDSmall fontHDSmall = new FontHDSmall();
			if (!Main.PSMode)
			{
				fontHDBig.BigBounds.RemoveRange(99, 14);
				fontHDBig.BigBounds.InsertRange(99, fontHD.XBBigBoundPart);
				fontHDBig.BigCrops.RemoveRange(99, 14);
				fontHDBig.BigCrops.InsertRange(99, fontHD.XBBigCropPart);
				fontHDBig.BigKerning.RemoveRange(99, 14);
				fontHDBig.BigKerning.InsertRange(99, fontHD.XBBigKerningPart);
				fontHDSmall.SmlBounds.RemoveRange(98, 80);
				fontHDSmall.SmlBounds.InsertRange(98, fontHD.XBSmlBoundPart);
				fontHDSmall.SmlCrops.RemoveRange(98, 17);
				fontHDSmall.SmlCrops.InsertRange(98, fontHD.XBSmlCropPart);
				fontHDSmall.SmlKerning.RemoveRange(98, 17);
				fontHDSmall.SmlKerning.InsertRange(98, fontHD.XBSmlKerningPart);
				fontHDSmall.Sml2Bounds.RemoveAt(0);
				fontHDSmall.Sml2Bounds.Insert(0, new Rectangle(954, 200, 16, 49));
				fontHDSmall.Sml2Bounds.AddRange(fontHD.XBSml2Bounds);
				fontHDSmall.Sml2Crops.RemoveRange(97, 16);
				fontHDSmall.Sml2Crops.InsertRange(97, fontHD.XBSml2CropPart);
				fontHDSmall.Sml2Kerning.RemoveRange(97, 16);
				fontHDSmall.Sml2Kerning.InsertRange(97, fontHD.XBSml2KerningPart);
			}
			else
			{
				fontHDSmall.Sml2Bounds.AddRange(fontHD.PSSml2Bounds);
			}
			object[] parameters = new object[8]
			{
				texture2D,
				fontHDBig.BigBounds,
				fontHDBig.BigCrops,
				fontHD.BigSmlChars,
				152,
				-22f,
				fontHDBig.BigKerning,
				Convert.ToChar(" ")
			};
			BigFont = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D2,
				fontHD.StackBounds,
				fontHD.StackCrops,
				fontHD.StackChars,
				28,
				-2f,
				fontHD.StackKerning,
				Convert.ToChar("/")
			};
			ItemStackFont = (SpriteFont)constructor.Invoke(parameters);
			FONT_STACK_Y_OFFSET = 7;
			parameters = new object[8]
			{
				texture2D3,
				fontHD.CombatBounds,
				fontHD.CombatCrops,
				fontHD.CombatChars,
				21,
				-3f,
				fontHD.CombatKerning,
				Convert.ToChar("0")
			};
			CombatTextFont[0] = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D4,
				fontHD.Combat2Bounds,
				fontHD.Combat2Crops,
				fontHD.CombatChars,
				22,
				-4f,
				fontHD.Combat2Kerning,
				Convert.ToChar("0")
			};
			CombatTextFont[1] = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D5,
				fontHDSmall.SmlBounds,
				fontHDSmall.SmlCrops,
				fontHD.BigSmlChars,
				30,
				-1f,
				fontHDSmall.SmlKerning,
				Convert.ToChar(" ")
			};
			SmallFont = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D6,
				fontHDSmall.Sml2Bounds,
				fontHDSmall.Sml2Crops,
				fontHD.BigSmlChars,
				33,
				-4f,
				fontHDSmall.Sml2Kerning,
				Convert.ToChar(" ")
			};
			BoldSmallFont = (SpriteFont)constructor.Invoke(parameters);
		}
		else
		{
			Font font = new Font();
			FontBig fontBig = new FontBig();
			FontSmall fontSmall = new FontSmall();
			if (!Main.PSMode)
			{
				fontSmall.SmlBounds.RemoveRange(96, 16);
				fontSmall.SmlBounds.InsertRange(96, font.XBSmlBoundPart);
				fontSmall.SmlCrops.RemoveRange(96, 16);
				fontSmall.SmlCrops.InsertRange(96, font.XBSmlCroPart);
				fontSmall.SmlKerning.RemoveRange(96, 16);
				fontSmall.SmlKerning.InsertRange(96, font.XBSmlKerningPart);
				fontSmall.Sml2Bounds.RemoveRange(96, 16);
				fontSmall.Sml2Bounds.InsertRange(96, font.XBSml2BoundPart);
				fontSmall.Sml2Crops.RemoveRange(96, 16);
				fontSmall.Sml2Crops.InsertRange(96, font.XBSml2CropPart);
				fontSmall.Sml2Kerning.RemoveRange(96, 16);
				fontSmall.Sml2Kerning.InsertRange(96, font.XBSml2KerningPart);
			}
			object[] parameters2 = new object[8]
			{
				texture2D,
				fontBig.BigBounds,
				fontBig.BigCrops,
				font.BigSmlChars,
				109,
				-24f,
				fontBig.BigKerning,
				Convert.ToChar(" ")
			};
			BigFont = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D2,
				font.StackBounds,
				font.StackCrops,
				font.StackChars,
				20,
				-4f,
				font.StackKerning,
				Convert.ToChar("/")
			};
			ItemStackFont = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D3,
				font.CombatBounds,
				font.CombatCrops,
				font.CombatChars,
				21,
				-3f,
				font.CombatKerning,
				Convert.ToChar("0")
			};
			CombatTextFont[0] = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D4,
				font.Combat2Bounds,
				font.Combat2Crops,
				font.CombatChars,
				22,
				-4f,
				font.Combat2Kerning,
				Convert.ToChar("0")
			};
			CombatTextFont[1] = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D5,
				fontSmall.SmlBounds,
				fontSmall.SmlCrops,
				font.BigSmlChars,
				20,
				-2f,
				fontSmall.SmlKerning,
				Convert.ToChar(" ")
			};
			SmallFont = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D6,
				fontSmall.Sml2Bounds,
				fontSmall.Sml2Crops,
				font.BigSmlChars,
				22,
				-5f,
				fontSmall.Sml2Kerning,
				Convert.ToChar(" ")
			};
			BoldSmallFont = (SpriteFont)constructor.Invoke(parameters2);
		}
		FONT_STACK_X_OFFSET = -5;
		// A generated atlas replaces the baked one for languages whose letters the baked atlases
		// do not have. Deliberately last and all-or-nothing: if the set is missing or broken the
		// game keeps exactly what it built above, which is why this cannot make things worse.
		ApplyGeneratedFonts();
		BoldSmallTextStyle = new CompiledText.Style(BoldSmallFont);
	}

	/// <summary>
	/// Swaps in <c>Storage/Fonts/&lt;code&gt;.*</c> for the current language when it exists.
	///
	/// The combat, stack and split-screen fonts stay baked: they are digits and a slash, and no
	/// language needs different ones. Only the three text fonts have letters.
	/// </summary>
	private static void ApplyGeneratedFonts()
	{
		if (theGame == null || theGame.GraphicsDevice == null)
		{
			return;
		}
		string code = Terraria.LangOverrides.Code(Terraria.Language.Choice);
		SpriteFont spriteFont;
		SpriteFont spriteFont2;
		SpriteFont spriteFont3;
		if (!Terraria.Fonts.GeneratedFonts.TryBuildAll(theGame.GraphicsDevice, code, out spriteFont, out spriteFont2, out spriteFont3))
		{
			if (Terraria.Fonts.GeneratedFonts.LastProblem.Length != 0)
			{
				// A set that exists but is broken must be loud: the player installed it on purpose
				// and would otherwise only see the text stay in the old letters.
				Program.LogWarn("Fonts", "generated font set for '" + code + "' was ignored: "
					+ Terraria.Fonts.GeneratedFonts.LastProblem);
			}
			return;
		}
		BigFont = spriteFont;
		SmallFont = spriteFont2;
		BoldSmallFont = spriteFont3;
		Program.LogInfo("Fonts", "using generated fonts from Storage/Fonts for language '" + code + "'");
	}

	public static void LoadSplitscreenFonts(ContentManager Content)
	{
		string text = "Fonts/";
		string text2 = "big_sc";
		string text3 = "stack_sc";
		string text4 = "combat_sc";
		string text5 = "combat2_sc";
		string text6 = "small_sc";
		string text7 = "small2_sc";
		ConstructorInfo constructor = typeof(SpriteFont).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[8]
		{
			typeof(Texture2D),
			typeof(List<Rectangle>),
			typeof(List<Rectangle>),
			typeof(List<char>),
			typeof(int),
			typeof(float),
			typeof(List<Vector3>),
			typeof(char?)
		}, null);
		if (Main.PSMode)
		{
			text2 += "_PS3";
			text7 += "_PS3";
		}
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			text += "HD/";
		}
		Texture2D texture2D = Content.Load<Texture2D>(text + text2);
		Texture2D texture2D2 = Content.Load<Texture2D>(text + text3);
		Texture2D texture2D3 = Content.Load<Texture2D>(text + text4);
		Texture2D texture2D4 = Content.Load<Texture2D>(text + text5);
		Texture2D texture2D5 = Content.Load<Texture2D>(text + text6);
		Texture2D texture2D6 = Content.Load<Texture2D>(text + text7);
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			FontHD fontHD = new FontHD();
			FontHDBig fontHDBig = new FontHDBig();
			FontHDSmall fontHDSmall = new FontHDSmall();
			if (!Main.PSMode)
			{
				fontHDBig.BigSCBounds.RemoveRange(98, 15);
				fontHDBig.BigSCBounds.InsertRange(98, fontHD.XBBigSCBoundPart);
				fontHDBig.BigSCCrops.RemoveRange(98, 15);
				fontHDBig.BigSCCrops.InsertRange(100, fontHD.XBBigSCCropPart);
				fontHDBig.BigSCKerning.RemoveRange(98, 15);
				fontHDBig.BigSCKerning.InsertRange(100, fontHD.XBBigSCKerningPart);
				fontHDSmall.SmlSCBounds.RemoveRange(96, 65);
				fontHDSmall.SmlSCBounds.InsertRange(96, fontHD.XBSmlSCBoundPart);
				fontHDSmall.SmlSCCrops.RemoveRange(96, 17);
				fontHDSmall.SmlSCCrops.InsertRange(96, fontHD.XBSmlSCCropPart);
				fontHDSmall.SmlSCKerning.RemoveRange(96, 17);
				fontHDSmall.SmlSCKerning.InsertRange(96, fontHD.XBSmlSCKerningPart);
				fontHDSmall.Sml2SCBounds.RemoveRange(96, 18);
				fontHDSmall.Sml2SCBounds.InsertRange(96, fontHD.XBSml2SCBoundPart);
				fontHDSmall.Sml2SCCrops.RemoveRange(96, 18);
				fontHDSmall.Sml2SCCrops.InsertRange(96, fontHD.XBSml2SCCropPart);
				fontHDSmall.Sml2SCKerning.RemoveRange(96, 18);
				fontHDSmall.Sml2SCKerning.InsertRange(96, fontHD.XBSml2SCKerningPart);
			}
			object[] parameters = new object[8]
			{
				texture2D,
				fontHDBig.BigSCBounds,
				fontHDBig.BigSCCrops,
				fontHD.BigSmlChars,
				28,
				-11f,
				fontHDBig.BigSCKerning,
				Convert.ToChar(" ")
			};
			BigFont = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D2,
				fontHD.StackSCBounds,
				fontHD.StackSCCrops,
				fontHD.StackChars,
				21,
				-2f,
				fontHD.StackSCKerning,
				Convert.ToChar("/")
			};
			ItemStackFont = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D3,
				fontHD.CombatBothSCBounds,
				fontHD.CombatBothSCCrops,
				fontHD.CombatChars,
				17,
				-2f,
				fontHD.CombatBothSCKerning,
				Convert.ToChar("0")
			};
			CombatTextFont[0] = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D4,
				fontHD.CombatBothSCBounds,
				fontHD.CombatBothSCCrops,
				fontHD.CombatChars,
				17,
				-3f,
				fontHD.CombatBothSCKerning,
				Convert.ToChar("0")
			};
			CombatTextFont[1] = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D5,
				fontHDSmall.SmlSCBounds,
				fontHDSmall.SmlSCCrops,
				fontHD.BigSmlChars,
				15,
				-1f,
				fontHDSmall.SmlSCKerning,
				Convert.ToChar(" ")
			};
			SmallFont = (SpriteFont)constructor.Invoke(parameters);
			parameters = new object[8]
			{
				texture2D6,
				fontHDSmall.Sml2SCBounds,
				fontHDSmall.Sml2SCCrops,
				fontHD.BigSmlChars,
				22,
				-4f,
				fontHDSmall.Sml2SCKerning,
				Convert.ToChar(" ")
			};
			BoldSmallFont = (SpriteFont)constructor.Invoke(parameters);
		}
		else
		{
			Font font = new Font();
			FontBig fontBig = new FontBig();
			FontSmall fontSmall = new FontSmall();
			if (!Main.PSMode)
			{
				fontSmall.Sml2SCBounds.RemoveRange(95, 16);
				fontSmall.Sml2SCBounds.InsertRange(95, font.XBSml2SCBoundPart);
				fontSmall.Sml2SCCrops.RemoveRange(95, 16);
				fontSmall.Sml2SCCrops.InsertRange(95, font.XBSml2SCCropPart);
				fontSmall.Sml2SCKerning.RemoveRange(95, 16);
				fontSmall.Sml2SCKerning.InsertRange(95, font.XBSml2SCKerningPart);
			}
			object[] parameters2 = new object[8]
			{
				texture2D,
				fontBig.BigSCBounds,
				fontBig.BigSCCrops,
				font.BigSmlChars,
				19,
				-13f,
				fontBig.BigSCKerning,
				Convert.ToChar(" ")
			};
			BigFont = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D2,
				font.StackSCBounds,
				font.StackSCCrops,
				font.StackChars,
				15,
				-4f,
				font.StackSCKerning,
				Convert.ToChar("/")
			};
			ItemStackFont = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D3,
				font.CombatBothSCBounds,
				font.CombatBothSCCrops,
				font.CombatChars,
				17,
				-2f,
				font.CombatBothSCKerning,
				Convert.ToChar("0")
			};
			CombatTextFont[0] = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D4,
				font.CombatBothSCBounds,
				font.CombatBothSCCrops,
				font.CombatChars,
				17,
				-3f,
				font.CombatBothSCKerning,
				Convert.ToChar("0")
			};
			CombatTextFont[1] = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D5,
				fontSmall.SmlSCBounds,
				fontSmall.SmlSCCrops,
				font.BigSmlChars,
				10,
				-2f,
				fontSmall.SmlSCKerning,
				Convert.ToChar(" ")
			};
			SmallFont = (SpriteFont)constructor.Invoke(parameters2);
			parameters2 = new object[8]
			{
				texture2D6,
				fontSmall.Sml2SCBounds,
				fontSmall.Sml2SCCrops,
				font.BigSmlChars,
				13,
				-2f,
				fontSmall.Sml2SCKerning,
				Convert.ToChar(" ")
			};
			BoldSmallFont = (SpriteFont)constructor.Invoke(parameters2);
		}
		FONT_STACK_X_OFFSET = -8;
		BoldSmallTextStyle = new CompiledText.Style(BoldSmallFont);
	}

	private void InvalidateCachedText()
	{
		toolTipText = null;
		npcCompiledChatText = null;
		errorCompiledText = null;
		Terraria.HowToPlay.UI.GenerateCache(theGame.GraphicsDevice);
		TextSequenceBlock.GenerateCache(theGame.GraphicsDevice);
		if (Main.HardmodeAlert)
		{
			Terraria.Hardmode.UI.GenerateCache(theGame.GraphicsDevice);
		}
	}

	public static float Spacing(SpriteFont font)
	{
		float num = font.Spacing;
		if (NumActiveViews > 1)
		{
			num *= 2f;
		}
		return num;
	}

	public static int LineSpacing(SpriteFont font)
	{
		int num = font.LineSpacing;
		if (NumActiveViews > 1)
		{
			num <<= 1;
		}
		return num;
	}

	public static Vector2 MeasureString(SpriteFont font)
	{
		Vector2 result = font.MeasureString(Main.StrBuilder);
		if (NumActiveViews > 1)
		{
			result.X *= 2f;
			result.Y *= 2f;
		}
		return result;
	}

	public static float MeasureStringX(SpriteFont font)
	{
		float num = font.MeasureString(Main.StrBuilder).X;
		if (NumActiveViews > 1)
		{
			num *= 2f;
		}
		return num;
	}

	public static Vector2 MeasureString(SpriteFont font, string text)
	{
		Vector2 result = font.MeasureString(text);
		if (NumActiveViews > 1)
		{
			result.X *= 2f;
			result.Y *= 2f;
		}
		return result;
	}

	public static void DrawStringLB(SpriteFont font, int x, int y)
	{
		float scale = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
		Vector2 vector = font.MeasureString(Main.StrBuilder);
		Main.SpriteBatch.DrawString(font, Main.StrBuilder, new Vector2(x, Main.ResolutionHeight - y), Color.White, 0f, new Vector2(0f, vector.Y), scale, SpriteEffects.None, 0f);
	}

	public static void DrawStringLT(SpriteFont font, int x, int y, Color c)
	{
		float scale = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
		Main.SpriteBatch.DrawString(font, Main.StrBuilder, new Vector2(x, y), c, 0f, default, scale, SpriteEffects.None, 0f);
	}

	public static void DrawStringLT(SpriteFont font, string s, int x, int y, Color c)
	{
		float scale = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
		Main.SpriteBatch.DrawString(font, s, new Vector2(x, y), c, 0f, default, scale, SpriteEffects.None, 0f);
	}

	public static void DrawStringScaled(SpriteFont font, string s, Vector2 pos, Color c, Vector2 pivot, float scale)
	{
		scale *= TextScale;
		if (NumActiveViews > 1)
		{
			scale *= 2f;
			pivot.X *= 0.5f;
			pivot.Y *= 0.5f;
		}
		Main.SpriteBatch.DrawString(font, s, pos, c, 0f, pivot, scale, SpriteEffects.None, 0f);
	}

	public static void DrawString(SpriteFont font, string s, Vector2 pos, Color c, float rot, Vector2 pivot, float scale)
	{
		scale *= TextScale;
		if (NumActiveViews > 1)
		{
			scale *= 2f;
			pivot.X *= 0.5f;
			pivot.Y *= 0.5f;
		}
		Main.SpriteBatch.DrawString(font, s, pos, c, rot, pivot, scale, SpriteEffects.None, 0f);
	}

	public static void DrawStringScaled(SpriteFont font, Vector2 pos, Color c, Vector2 pivot, float scale)
	{
		scale *= TextScale;
		if (NumActiveViews > 1)
		{
			scale *= 2f;
			pivot.X *= 0.5f;
			pivot.Y *= 0.5f;
		}
		Main.SpriteBatch.DrawString(font, Main.StrBuilder, pos, c, 0f, pivot, scale, SpriteEffects.None, 0f);
	}

	public static void DrawStringCC(SpriteFont font, string s, int x, int y, Color c)
	{
		float scale = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
		Vector2 origin = font.MeasureString(s);
		origin.X = (float)Math.Round((double)origin.X * 0.5);
		origin.Y = (float)Math.Round((double)origin.Y * 0.5);
		Main.SpriteBatch.DrawString(font, s, new Vector2(x, y), c, 0f, origin, scale, SpriteEffects.None, 0f);
	}

	public static void DrawStringLC(SpriteFont font, string s, int x, int y, Color c)
	{
		float scale = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
		Vector2 origin = font.MeasureString(s);
		origin.X = 0f;
		origin.Y = (float)Math.Round((double)origin.Y * 0.5);
		Main.SpriteBatch.DrawString(font, s, new Vector2(x, y), c, 0f, origin, scale, SpriteEffects.None, 0f);
	}

	public static void DrawStringRC(SpriteFont font, string s, int x, int y, Color c)
	{
		float scale = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
		Vector2 origin = font.MeasureString(s);
		origin.Y = (float)Math.Round((double)origin.Y * 0.5);
		Main.SpriteBatch.DrawString(font, s, new Vector2(x, y), c, 0f, origin, scale, SpriteEffects.None, 0f);
	}

	public static float DrawStringCT(SpriteFont font, string s, int x, int y, Color c)
	{
		float num = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
		Vector2 origin = font.MeasureString(s);
		origin.X = (float)Math.Round((double)origin.X * 0.5);
		float result = origin.Y * num;
		origin.Y = 0f;
		Main.SpriteBatch.DrawString(font, s, new Vector2(x, y), c, 0f, origin, num, SpriteEffects.None, 0f);
		return result;
	}

	public static float DrawStringCT(SpriteFont font, int x, int y, Color c)
	{
		float num = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
		Vector2 origin = font.MeasureString(Main.StrBuilder);
		origin.X *= 0.5f;
		float result = origin.Y * num;
		origin.Y = 0f;
		Main.SpriteBatch.DrawString(font, Main.StrBuilder, new Vector2(x, y), c, 0f, origin, num, SpriteEffects.None, 0f);
		return result;
	}

	/// <summary>
	/// Steps back through the menu history.
	///
	/// The parameter is an absolute history depth, not a count: a negative value means "step back
	/// one screen". <c>PrevMenu(-2)</c> therefore does <b>not</b> go back two screens, and using it
	/// that way left the character-creation flow stuck in the creator (see
	/// <see cref="ReturnToCharacterSelect"/>).
	/// </summary>
	public void PrevMenu(int depth = -1)
	{
		Main.PlaySound(11);
		if (depth < 0)
		{
			// Step back past entries that name the screen we are already on. A stale entry used
			// to make Back a no-op - which is exactly how the settings screens became impossible
			// to leave: every return re-pushed the screen being left, so the history filled up
			// with the screen the player was trying to escape.
			do
			{
				menuDepth--;
			}
			while (menuDepth >= 0 && prevMenuMode[menuDepth] == CurMenuMode);
			if (menuDepth >= 0)
			{
				SetMenu(prevMenuMode[menuDepth], rememberPrevious: false);
			}
			else
			{
				SetMenu(MenuMode.TITLE, rememberPrevious: false, reset: true);
			}
		}
		else if (depth < menuDepth)
		{
			menuDepth = depth;
			SetMenu(prevMenuMode[depth], rememberPrevious: false);
		}
		else
		{
			SetMenu(MenuMode.TITLE, rememberPrevious: false, reset: true);
		}
	}

	/// <summary>Number of screens on the back stack. Exposed for the self-test.</summary>
	internal int MenuDepthForTests => menuDepth;

	/// <summary>
	/// Test hook: runs one frame of the menu logic for the current mode.
	///
	/// The self-test uses this to open every screen without a GPU. <c>UpdateMenu</c> is where the
	/// "IndexOutOfRangeException in UpdateMenu" class of bug lives: it is one method driving every
	/// screen in the game, so a crash there says nothing about which screen, and the screen is
	/// only reachable by opening it.
	/// </summary>
	internal void UpdateMenuForTests()
	{
		UpdateMenu();
	}

	/// <summary>Rows the current screen asked for. Used by the menu smoke test.</summary>
	internal int MenuItemCountForTests => numMenuItems;

	/// <summary>
	/// Returns to the character list from anywhere in the creation flow.
	///
	/// This exists because "back two screens" is a destination, not a number: <see cref="PrevMenu(int)"/>
	/// takes an absolute history depth, and a negative argument means "step back one". The creation
	/// flow used <c>PrevMenu(-2)</c> in two places, so both did a single step:
	/// <list type="bullet">
	/// <item>after saving a name the player landed back in the character creator instead of the
	/// list - the character was saved and the screen looked stuck;</item>
	/// <item>"Leave without creating a character?" answered Yes also landed back in the creator,
	/// so the dialog looked like it ignored the answer.</item>
	/// </list>
	/// The click that caused the step is cleared too: the callback runs before the next screen's
	/// update, and a stale <c>selectedMenu</c> would immediately pick a row there.
	/// </summary>
	internal void ReturnToCharacterSelect()
	{
		selectedMenu = -1;
		focusMenu = -1;
		numLoadPlayers = CountSavedPlayers();
		for (int i = menuDepth - 1; i >= 0; i--)
		{
			if (prevMenuMode[i] == MenuMode.CHARACTER_SELECT)
			{
				menuDepth = i;
				SetMenu(MenuMode.CHARACTER_SELECT, rememberPrevious: false);
				// Ignore input briefly: the key or click that got us here is often still held, and
				// the list must not act on it.
				ClearMenuHits();
				uiDelay = 12;
				return;
			}
		}
		// The list is not in the history (a direct entry, an invite): go there anyway rather than
		// staying in a screen the player just asked to leave.
		menuDepth = 0;
		SetMenu(MenuMode.CHARACTER_SELECT, rememberPrevious: false);
		ClearMenuHits();
		uiDelay = 12;
	}

	private void ResetPlayerMenuSelection()
	{
		uiX = 0;
		// Row 0 is the first saved character, or "New" when there are none.
		uiY = 0;
		charScroll = 0;
		selectedMenu = -1;
		focusMenu = -1;
	}

	/// <summary>
	/// Per-mode UI position, indexed by the MenuMode value. Exposed so the self-test can assert
	/// it still covers every mode: it was a fixed 40 while the enum grew to 47, and every screen
	/// added after CHEATS crashed on entry with IndexOutOfRangeException.
	/// </summary>
	public void SetMenu(MenuMode mode, bool rememberPrevious = true, bool reset = false)
	{
		if (SettingsDirty)
		{
			SaveSettings();
		}
		// The name prompt is bound to its menu: leaving that menu must never leave a keyboard
		// floating over whatever comes next.
		if (mode != MenuMode.NAME_CHARACTER && mode != MenuMode.NAME_WORLD)
		{
			NamePrompt.Reset();
		}
		numMenuItems = 0;
		if (reset)
		{
			menuDepth = 0;
		}
		if (mode == MenuMode.TITLE)
		{
			Main.SetTutorial(Tutorial.NUM_TUTORIALS);
			if (!Main.IsTrial && saveIconMessage == null)
			{
				int wrapWidth = 470;
				switch (Main.ScreenHeightPtr)
				{
				case ScreenHeights.HD:
					wrapWidth = 548;
					break;
				case ScreenHeights.FHD:
					wrapWidth = 705;
					break;
				}
				saveIconMessage = new CompiledText(Lang.MenuText[4], wrapWidth, BoldSmallTextStyle);
				saveIconMessageTime = 480;
			}
			if (SignedInGamer != null)
			{
				SignedInGamer.Presence.SetPresenceModeStringEXT("Menu");
			}
			if (Netplay.isJoiningRemoteInvite)
			{
				if (!Netplay.gamersWaitingToJoinInvite.Contains(SignedInGamer))
				{
					Exit();
					return;
				}
				mode = MenuMode.CHARACTER_SELECT;
				if (this == MainUI)
				{
					for (int num = Netplay.gamersWaitingToJoinInvite.Count - 1; num >= 0; num--)
					{
						SignedInGamer signedInGamer = Netplay.gamersWaitingToJoinInvite[num];
						if (signedInGamer != SignedInGamer)
						{
							UI uI = Main.UIInstance[(int)signedInGamer.PlayerIndex];
							uI.SetMenu(MenuMode.CHARACTER_SELECT, rememberPrevious: false, reset: true);
							uI.OpenView();
						}
					}
				}
			}
		}
		if (CurMenuMode != MenuMode.NONE)
		{
			for (int num2 = menuHC.Length - 1; num2 >= 0; num2--)
			{
				menuHC[num2] = 0;
			}
			if (rememberPrevious)
			{
				// Guarded: the table is sized from MenuMode, so this should never trigger, but a
				// menu that pushes without ever popping must not be able to crash the game.
				if (menuDepth < prevMenuMode.Length)
				{
					prevMenuMode[menuDepth++] = CurMenuMode;
				}
			}
			uiPos[(uint)CurMenuMode].X = uiX;
			uiPos[(uint)CurMenuMode].Y = uiY;
		}
		CurMenuMode = mode;
		uiX = uiPos[(uint)mode].X;
		uiY = uiPos[(uint)mode].Y;
		uiDelay = 0;
		uiDelayValue = 12;
		switch (mode)
		{
		case MenuMode.PAUSE:
			worldFadeTarget = 0.375f;
			uiWidth = 1;
			uiHeight = 7;
			uiCoords = MENU_PAUSE_COORDS;
			return;
		case MenuMode.TITLE:
			uiWidth = 1;
			uiHeight = 7;
			uiCoords = MENU_TITLE_COORDS;
			return;
		case MenuMode.CHARACTER_SELECT:
			uiWidth = 1;
			// Row count and coordinates come from the character list itself, which is built in
			// UpdateMenu because the number of rows depends on how many fit on screen. This is the
			// first frame's value; the branch rebuilds both before any input is read.
			uiHeight = (sbyte)Math.Max(1, Math.Min(127, Math.Max(CharacterRowCount(), charCoords.Length)));
			BuildCharacterCoords(uiHeight);
			uiCoords = charCoords;
			initCharacterSelectCoordinates();
			return;
		case MenuMode.CONFIRM_LEAVE_CREATE_CHARACTER:
		case MenuMode.CONFIRM_DELETE_CHARACTER:
		case MenuMode.CONFIRM_DELETE_WORLD:
			uiWidth = 1;
			uiHeight = 2;
			uiCoords = MENU_CONFIRM_DELETE_COORDS;
			return;
		case MenuMode.VOLUME:
			soundUI.UpdateVolumes();
			break;
		case MenuMode.WORLD_SIZE:
			uiWidth = 1;
			uiHeight = 3;
			uiCoords = MENU_WORLD_SIZE_COORDS;
			return;
		case MenuMode.OPTIONS:
			uiWidth = 1;
			uiHeight = 4;
			uiCoords = MENU_OPTIONS_COORDS;
			return;
		case MenuMode.SETTINGS:
			uiWidth = 1;
			uiHeight = 3;
			uiCoords = MENU_SETTINGS_COORDS;
			return;
		case MenuMode.CHEATS:
			// Own screen, like Display / HUD / Network: the cheat list needs a panel, a
			// highlight and a row height that follows the screen, none of which the generic
			// menu renderer does.
			uiWidth = 0;
			uiHeight = 1;
			uiCoords = null;
			Terraria.Cheats.CheatMenu.Reset();
			return;
		case MenuMode.KEYBINDS:
			// Own screen: no generic menu rows, no console cursor.
			uiWidth = 0;
			uiHeight = 1;
			uiCoords = null;
			Terraria.Input.KeyBindScreen.Reset();
			return;
		case MenuMode.MODS:
			uiWidth = 0;
			uiHeight = 1;
			uiCoords = null;
			Terraria.Mods.ModMenu.Reset();
			return;
		case MenuMode.DISPLAY:
			uiWidth = 0;
			uiHeight = 1;
			uiCoords = null;
			Terraria.DisplayMenu.Reset();
			return;
		case MenuMode.NETWORK:
			uiWidth = 0;
			uiHeight = 1;
			uiCoords = null;
			Terraria.NetworkMenu.Reset();
			return;
		case MenuMode.HUD:
			uiWidth = 0;
			uiHeight = 1;
			uiCoords = null;
			Terraria.HudMenu.Reset();
			return;
		case MenuMode.BACKUPS:
			uiWidth = 0;
			uiHeight = 1;
			uiCoords = null;
			return;
		case MenuMode.STATUS_SCREEN:
		case MenuMode.NETPLAY:
			Progress = 0f;
			progressTotal = 0f;
			uiWidth = 0;
			uiHeight = 1;
			uiCoords = null;
			statusText = null;
			return;
		case MenuMode.WORLD_SELECT:
			Terraria.Net.ServerListRefresh.Reset();
			if (Netplay.availableSessions.Count == 0 && !Netplay.IsFindingSessions())
			{
				Netplay.FindSessions();
			}
			break;
		case MenuMode.ERROR:
			if (Main.WorldGenThread != null)
			{
				Main.WorldGenThread.Abort();
				Main.WorldGenThread = null;
				WorldGen.Gen = false;
			}
			break;
		case MenuMode.MAP:
			worldFadeTarget = 0.375f;
			uiFade = 0f;
			uiFadeTarget = 1f;
			break;
		case MenuMode.CREDITS:
			Credits.Init();
			break;
		case MenuMode.UPSELL:
			theGame.LoadUpsell();
			break;
		}
		uiWidth = 0;
		uiHeight = 0;
		uiCoords = null;
	}

	private void Exit()
	{
		if (NumActiveViews == 1)
		{
			if (Main.IsTrial)
			{
				SetMenu(MenuMode.UPSELL, rememberPrevious: false, reset: true);
			}
			else
			{
				HasQuit = true;
			}
			return;
		}
		SetPlayer(null);
		SignedInGamer = null;
		CurMenuType = MenuType.MAIN;
		CurMenuMode = MenuMode.WELCOME;
		selectedMenu = -1;
		focusMenu = -1;
		uiX = 0;
		uiY = 0;
		uiPos = new Location[MenuSlots];
		worldFade = 0f;
		worldFadeTarget = 1f;
		isStopping = false;
		if (playerStorage != null && !Netplay.isJoiningRemoteInvite)
		{
			theGame.Components.Remove(playerStorage);
			playerStorage.Dispose();
			playerStorage = null;
		}
	}

	public void ExitGame()
	{
		Main.IsGameStarted = false;
		for (int i = 0; i < 4; i++)
		{
			UI uI = Main.UIInstance[i];
			if (uI.CurrentView != null && uI.CurMenuType != MenuType.MAIN)
			{
				uI.CurrentView.onStopGame();
			}
		}
		for (int j = 0; j < 4; j++)
		{
			UI uI2 = Main.UIInstance[j];
			if (uI2.CurrentView != null && uI2 != MainUI)
			{
				if (uI2.CurMenuType == MenuType.MAIN)
				{
					uI2.Exit();
				}
				else
				{
					uI2.StopGame();
				}
			}
		}
		MainUI.StopGame();
	}

	public void StopGame()
	{
		CloseInventory();
		ActiveInvSection = InventorySection.ITEMS;
		hotbarItemNameTime = 210;
		quickAccessDisplayTime = 0;
		quickAccessUp = -1;
		quickAccessDown = -1;
		quickAccessLeft = -1;
		quickAccessRight = -1;
		isStopping = true;
		worldFadeTarget = 1f;
		if (Main.SaveOnExit)
		{
			statusText = Lang.MenuText[54];
		}
		else
		{
			statusText = "";
		}
		if (CurMenuMode != MenuMode.ERROR)
		{
			SetMenu(MenuMode.STATUS_SCREEN, rememberPrevious: false);
		}
		CurMenuType = MenuType.MAIN;
		if (this == MainUI)
		{
			if (Main.SaveOnExit)
			{
				Main.SaveOnExit = false;
				WorldGen.SaveAndQuit();
				return;
			}
			Netplay.PlayDisconnect = true;
			LoadPlayers();
			if (CurMenuMode != MenuMode.ERROR)
			{
				SetMenu(MenuMode.TITLE, rememberPrevious: false, reset: true);
			}
		}
		else if (!Main.SaveOnExit)
		{
			Exit();
		}
	}

	public void PrepareDraw(int pass)
	{
		if (CurrentView != null && CurMenuType != MenuType.MAIN)
		{
			CurrentUI = this;
			CurrentView.PrepareDraw(pass);
		}
	}

	public void Draw()
	{
		if (CurrentView != null)
		{
			CurrentUI = this;
			CurrentView.DrawBg(this);
			if (CurMenuType != MenuType.MAIN)
			{
				CurrentView.DrawWorld();
				DrawCursor();
				CurrentView.SetScreenView();
			}
			DrawTopLayer();
			if (CurMenuType != MenuType.NONE)
			{
				DrawMenu();
			}
			else if (Main.TutorialState < Tutorial.THE_END)
			{
				DrawTutorial();
			}
			else
			{
				// Chat prompt lives above the HUD, below menus.
				Terraria.Input.ChatInput.Draw();
			}
			// Pointer on top of everything, for menus and for the inventory (which keeps
			// CurMenuType == NONE, so DrawCursor returns early and would leave no cursor).
			if (PointerActive && (CurMenuType != MenuType.NONE || InventoryMode > 0))
			{
				DrawMenuCursor();
			}
			Terraria.Input.VirtualKeyboard.Draw();
			Main.SpriteBatch.End();
		}
	}

	private void DrawTopLayer()
	{
		if (worldFade < 1f)
		{
			Main.DrawSolidRect(new Rectangle(0, 0, CurrentView.ViewWidth, Main.ResolutionHeight), new Color(0f, 0f, 0f, 1f - worldFade));
		}
		if (worldFade != worldFadeTarget)
		{
			if (worldFadeTarget < worldFade)
			{
				worldFade -= 1f / 30f;
				if (worldFadeTarget > worldFade)
				{
					worldFade = worldFadeTarget;
				}
			}
			else
			{
				worldFade += 1f / 30f;
				if (worldFadeTarget < worldFade)
				{
					worldFade = worldFadeTarget;
				}
			}
		}
		if (CurMenuType == MenuType.NONE)
		{
			DrawInterface();
			if (InventoryMode == 0 && !ActivePlayer.ghost)
			{
				DrawHud();
			}
		}
	}

	private void DrawTutorial()
	{
		int num = 500;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num = (int)((float)num * 1.15f);
			break;
		case ScreenHeights.FHD:
			num = (int)((float)num * 1.534f);
			break;
		}
		DrawDialog(new Vector2(CurrentView.ViewWidth - num >> 1, Main.ResolutionHeight - CurrentView.SafeAreaOffsetBottom - (int)(36f * Main.ScreenMultiplier)), new Color(128, 128, 128, 64), new Color(255, 255, 255, 255), Main.TutorialText, null, anchorBottom: true);
	}

	private void UpdateMenu()
	{
		MenuMode curMenuMode = CurMenuMode;
		numMenuItems = 0;
		menuTop = 250;
		menuLeft = (short)((CurrentView != null) ? (CurrentView.ViewWidth >> 1) : (Main.ResolutionWidth / 2));
		menuSpace = 80;
		showPlayer = -1;
		for (int i = 0; i < 14; i++)
		{
			noFocus[i] = false;
			blockFocus[i] = false;
			menuY[i] = 0;
			menuScale[i] = 1f;
		}
		// Move the selection to whatever the pointer is on BEFORE any branch below consumes a
		// click. UpdateMouse (which also does this) runs after UpdateMenu in UI.Update, so the
		// click used to be handled against last frame's highlight: clicking "Start Game" then hit
		// whichever row had been highlighted a frame earlier, and a menu looked like it was stuck
		// to the previous entry. Hit rectangles come from the last Draw, which is exactly what the
		// pointer was looking at.
		if (Terraria.Input.KeyboardMouseInput.Enabled && Terraria.Input.KeyboardMouseInput.CursorOverride)
		{
			SelectMenuEntryUnderPointer();
		}
		if (CurMenuMode == MenuMode.ERROR)
		{
			numMenuItems = 0;
			if (IsBackButtonTriggered())
			{
				SetMenu(MenuMode.TITLE, rememberPrevious: false, reset: true);
			}
		}
		else if (CurMenuMode == MenuMode.LOAD_FAILED_NO_BACKUP)
		{
			numMenuItems = 2;
			menuString[0] = Lang.MenuText[9];
			noFocus[0] = true;
			// The reason, when the file itself explains it. Saves a support round-trip: without
			// this, a truncated save and a PC-edition world looked exactly the same.
			menuString[1] = WorldSelect.LastLoadError;
			noFocus[1] = true;
			menuTop = (short)((WorldSelect.LastLoadError != null) ? 280 : 300);
			if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.STATUS_SCREEN)
		{
			numMenuItems = 1;
			menuString[0] = statusText;
			noFocus[0] = true;
			menuTop = 175;
			tips.Update();
		}
		else if (CurMenuMode == MenuMode.NETPLAY)
		{
			numMenuItems = 1;
			menuString[0] = statusText;
			noFocus[0] = true;
			menuTop = 175;
			tips.Update();
			if (IsBackButtonTriggered())
			{
				if (Netplay.sessionThread != null)
				{
					Netplay.PlayDisconnect = true;
				}
				PrevMenu();
			}
			else if (Netplay.sessionThread == null)
			{
				bool flag = true;
				for (int j = 0; j < 4; j++)
				{
					UI uI = Main.UIInstance[j];
					if (uI.CurrentView != null && uI.CurMenuMode != MenuMode.NETPLAY)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					Netplay.StartClient();
				}
			}
		}
		else if (CurMenuMode == MenuMode.WAITING_SCREEN)
		{
			numMenuItems = 1;
			menuString[0] = Lang.MenuText[51];
			noFocus[0] = true;
			menuTop = 300;
			if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
			else if (Main.IsGameStarted && !Main.IsGamePaused)
			{
				if (Netplay.Session == null)
				{
					Exit();
					return;
				}
				if (Netplay.Session.AllGamers.Count < 8)
				{
					int privateGamerSlots = Netplay.Session.PrivateGamerSlots;
					if (privateGamerSlots > 0)
					{
						if (Main.NetMode == 1)
						{
							NetMessage.CreateMessage0(66);
							NetMessage.SendMessage();
							privateSlots = (byte)privateGamerSlots;
							SetMenu(MenuMode.WAITING_FOR_PUBLIC_SLOT, rememberPrevious: false);
							return;
						}
						Netplay.Session.PrivateGamerSlots--;
					}
					try
					{
						Netplay.Session.AddLocalGamer(SignedInGamer);
						SetMenu(MenuMode.WAITING_FOR_PLAYER_ID, rememberPrevious: false);
					}
					catch
					{
						Exit();
						return;
					}
				}
				else
				{
					menuString[0] = Lang.WorldGenText[57];
				}
			}
		}
		else if (CurMenuMode == MenuMode.WAITING_FOR_PUBLIC_SLOT)
		{
			numMenuItems = 1;
			menuString[0] = Lang.MenuText[51];
			noFocus[0] = true;
			menuTop = 300;
			if (Netplay.Session.PrivateGamerSlots < privateSlots)
			{
				try
				{
					Netplay.Session.AddLocalGamer(SignedInGamer);
					SetMenu(MenuMode.WAITING_FOR_PLAYER_ID, rememberPrevious: false);
				}
				catch
				{
					Exit();
					return;
				}
			}
			else if (IsBackButtonTriggered())
			{
				NetMessage.CreateMessage0(67);
				NetMessage.SendMessage();
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.WAITING_FOR_PLAYER_ID)
		{
			numMenuItems = 1;
			menuString[0] = Lang.MenuText[51];
			noFocus[0] = true;
			menuTop = 300;
			if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.LEADERBOARDS)
		{
			if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
			else
			{
				Leaderboards.Update();
			}
		}
		else if (CurMenuMode == MenuMode.ACHIEVEMENTS)
		{
			if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
			else
			{
				Achievements.Update();
			}
		}
		else if (CurMenuMode == MenuMode.TITLE)
		{
			if (MainUI != this)
			{
				Exit();
				return;
			}
			menuTop = 172;
			menuSpace = 42;
			menuString[0] = Lang.MenuText[13];
			menuString[1] = Lang.MenuText[89];
			menuString[2] = Lang.MenuText[106];
			menuString[3] = Lang.MenuText[107];
			menuString[4] = Lang.MenuText[108];
			bool flag2 = false;
			if (!Main.IsTrial)
			{
				MENU_TITLE_COORDS[0].X = ViewportHalfWidth;
				menuHC[0] = 0;
			}
			else
			{
				MENU_TITLE_COORDS[0].X = 0;
				menuHC[0] = 3;
				if (uiY == 0)
				{
					uiY = 1;
				}
			}
			if (IsNetworkAvailable())
			{
				MENU_TITLE_COORDS[2].X = ViewportHalfWidth;
				menuHC[2] = 0;
			}
			else
			{
				MENU_TITLE_COORDS[2].X = 0;
				menuHC[2] = 3;
				if (uiY == 2)
				{
					uiY = 3;
				}
			}
			if (!SignedInGamer.IsGuest && !Main.IsTrial)
			{
				MENU_TITLE_COORDS[3].X = ViewportHalfWidth;
				menuHC[3] = 0;
			}
			else
			{
				MENU_TITLE_COORDS[3].X = 0;
				menuHC[3] = 3;
				if (uiY == 3)
				{
					uiY = 4;
				}
			}
			if (Main.IsTrial)
			{
				menuString[5] = Lang.MenuText[109];
				if (SignedInGamer.Privileges.AllowPurchaseContent)
				{
					MENU_TITLE_COORDS[5].X = ViewportHalfWidth;
					menuHC[5] = 0;
				}
				else
				{
					MENU_TITLE_COORDS[5].X = 0;
					menuHC[5] = 3;
					if (uiY == 5)
					{
						uiY = 6;
					}
				}
				menuString[6] = Lang.MenuText[15];
				numMenuItems = 7;
				MENU_TITLE_COORDS[6].X = ViewportHalfWidth;
			}
			else
			{
				menuString[5] = Lang.MenuText[15];
				numMenuItems = 6;
				MENU_TITLE_COORDS[6].X = 0;
			}
			for (int num = numMenuItems - 1; num >= 0; num--)
			{
				menuScale[num] = 0.75f;
			}
			if (selectedMenu == 0)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.CHARACTER_SELECT);
				ResetPlayerMenuSelection();
			}
			else if (selectedMenu == 1)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.STATUS_SCREEN);
				Main.StartTutorial();
			}
			else if (selectedMenu == 2)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.LEADERBOARDS);
				Leaderboards.InitializeData();
			}
			else if (selectedMenu == 3)
			{
				ShowAchievements();
			}
			else if (selectedMenu == 4)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.OPTIONS);
			}
			else if (selectedMenu == 5)
			{
				if (Main.IsTrial)
				{
					if (!Guide.IsVisible)
					{
						if (!flag2)
						{
							flag2 = true;
							Main.PlaySound(12);
							Process.Start("http://www.terraria.org");
						}
						else
						{
							Main.PlaySound(11);
						}
					}
				}
				else
				{
					SetMenu(MenuMode.QUIT);
				}
			}
			else if (selectedMenu == 6)
			{
				SetMenu(MenuMode.QUIT);
			}
			else if (IsButtonTriggered(Buttons.X))
			{
				if (playerStorage != null)
				{
					playerStorage.PromptForDevice();
				}
			}
			else if (IsButtonTriggered(Buttons.Y))
			{
				ShowParty();
			}
		}
		else if (CurMenuMode == MenuMode.PAUSE)
		{
			menuString[0] = Lang.MenuText[112];
			int num2 = 1;
			bool flag3 = false;
			if (Main.IsTrial)
			{
				menuString[num2] = Lang.MenuText[109];
				if (SignedInGamer.Privileges.AllowPurchaseContent)
				{
					MENU_PAUSE_COORDS[num2].X = ViewportHalfWidth;
					menuHC[num2] = 0;
				}
				else
				{
					MENU_PAUSE_COORDS[num2].X = 0;
					menuHC[num2] = 3;
					if (uiY == num2)
					{
						uiY = 0;
					}
				}
				num2++;
				numMenuItems = 7;
				MENU_PAUSE_COORDS[6].X = ViewportHalfWidth;
			}
			else
			{
				numMenuItems = 6;
				MENU_PAUSE_COORDS[6].X = 0;
			}
			if (!IsNetworkAvailable())
			{
				MENU_PAUSE_COORDS[num2 + 1].X = 0;
				menuHC[num2 + 1] = 3;
				if (uiY == num2 + 1)
				{
					uiY = 0;
				}
			}
			else
			{
				MENU_PAUSE_COORDS[num2 + 1].X = ViewportHalfWidth;
				menuHC[num2 + 1] = 0;
			}
			if (SignedInGamer.IsGuest || Main.IsTrial)
			{
				MENU_PAUSE_COORDS[num2 + 2].X = 0;
				menuHC[num2 + 2] = 3;
				if (uiY == num2 + 2)
				{
					uiY = 0;
				}
			}
			else
			{
				MENU_PAUSE_COORDS[num2 + 2].X = ViewportHalfWidth;
				menuHC[num2 + 2] = 0;
			}
			if (WorldGen.saveLock || Main.IsTutorial() || !HasPlayerStorage())
			{
				MENU_PAUSE_COORDS[num2 + 3].X = 0;
				menuHC[num2 + 3] = 3;
				if (uiY == num2 + 3)
				{
					uiY = 0;
				}
			}
			else
			{
				MENU_PAUSE_COORDS[num2 + 3].X = ViewportHalfWidth;
				menuHC[num2 + 3] = 0;
			}
			if (WorldGen.saveLock)
			{
				MENU_PAUSE_COORDS[num2 + 4].X = 0;
				menuHC[num2 + 4] = 3;
				if (uiY == num2 + 4)
				{
					uiY = 0;
				}
			}
			else
			{
				MENU_PAUSE_COORDS[num2 + 4].X = ViewportHalfWidth;
				menuHC[num2 + 4] = 0;
			}
			menuString[num2] = Lang.MenuText[108];
			menuString[num2 + 1] = Lang.MenuText[106];
			menuString[num2 + 2] = Lang.MenuText[107];
			menuString[num2 + 3] = Lang.MenuText[99];
			menuString[num2 + 4] = Lang.MenuText[101];
			if (!Main.IsTrial && Terraria.Cheats.CheatState.IsAvailable)
			{
				// Extra single-player-only entry appended after the stock pause options.
				menuString[num2 + 5] = Texts.Get("ui.menu.cheats");
				MENU_PAUSE_COORDS[num2 + 5].X = ViewportHalfWidth;
				menuHC[num2 + 5] = 0;
				numMenuItems = (byte)(num2 + 6);
			}
			for (int k = 0; k < numMenuItems; k++)
			{
				menuScale[k] = 0.75f;
			}
			menuTop = 200;
			menuSpace = 40;
			if (selectedMenu == 0 || IsButtonUntriggered(Buttons.Start) || IsBackButtonTriggered())
			{
				ResumeGame();
			}
			else if (Main.NetMode == 1 && IsButtonTriggered(Buttons.RightShoulder))
			{
				MessageBox.Show(controller, Lang.InterfaceText[72], Lang.InterfaceText[73], new string[2]
				{
					Lang.MenuText[105],
					Lang.MenuText[104]
				}, ShouldAutoUpdate: false);
				SetMenu(MenuMode.BLACKLIST);
			}
			else if (num2 > 1 && selectedMenu == 1)
			{
				if (!Guide.IsVisible)
				{
					if (!flag3)
					{
						flag3 = true;
						Main.PlaySound(12);
						Process.Start("http://www.terraria.org");
					}
					else
					{
						Main.PlaySound(11);
					}
				}
			}
			else if (selectedMenu == num2)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.OPTIONS);
			}
			else if (selectedMenu == num2 + 1)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.LEADERBOARDS);
				Leaderboards.InitializeData();
			}
			else if (selectedMenu == num2 + 2)
			{
				ShowAchievements();
			}
			else if (selectedMenu == num2 + 3)
			{
				Main.PlaySound(10);
				WorldGen.saveAllWhilePlaying();
			}
			else if (selectedMenu == num2 + 4)
			{
				Main.PlaySound(10);
				Main.SaveOnExit = !Main.IsTutorial() && autoSave && IsStorageEnabledForAnyPlayer();
				if (!Main.SaveOnExit && !Main.IsTutorial())
				{
					string[] boxOptions = ((!IsStorageEnabledForAnyPlayer()) ? new string[2]
					{
						Lang.InterfaceText[0],
						Lang.InterfaceText[1]
					} : new string[3]
					{
						Lang.InterfaceText[0],
						Lang.InterfaceText[1],
						Lang.InterfaceText[2]
					});
					MessageBox.Show(controller, Lang.MenuText[100], Lang.InterfaceText[5], boxOptions, ShouldAutoUpdate: false);
				}
				SetMenu(MenuMode.EXIT);
			}
			else if (!Main.IsTrial && Terraria.Cheats.CheatState.IsAvailable && selectedMenu == num2 + 5)
			{
				Main.PlaySound(10);
				cheatsFromPlay = false;
				SetMenu(MenuMode.CHEATS);
			}
		}
		else if (CurMenuMode == MenuMode.KEYBINDS)
		{
			if (Terraria.Input.KeyBindScreen.Update(this))
			{
				// PrevMenu, not SetMenu(SETTINGS): SetMenu remembers the screen it is leaving, so
				// returning this way pushed KEYBINDS onto the history every time. Back in SETTINGS
				// then stepped to KEYBINDS again - the "cannot leave the settings screen" loop.
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.MODS)
		{
			if (Terraria.Mods.ModMenu.Update())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.DISPLAY)
		{
			if (Terraria.DisplayMenu.Update())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.NETWORK)
		{
			if (Terraria.NetworkMenu.Update())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.HUD)
		{
			if (Terraria.HudMenu.Update())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.BACKUPS)
		{
			if (Terraria.BackupsMenu.Update())
			{
				// Same reason as the settings screens: returning through SetMenu would push
				// BACKUPS onto the history, and Back in the world list would reopen it.
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.CHEATS)
		{
			// PrevMenu, not SetMenu(PAUSE): SetMenu remembers the screen it is leaving, so
			// closing this way pushed CHEATS onto the history and Back in the pause menu
			// reopened it.
			if (Terraria.Cheats.CheatMenu.Update(this))
			{
				// Opened straight from play with F1 there is no pause menu underneath, so the
				// way back is the same one the pause menu uses to resume: PrevMenu plus
				// CurMenuType = NONE. With only PrevMenu the game would sit frozen with no
				// screen to press anything on.
				if (cheatsFromPlay)
				{
					cheatsFromPlay = false;
					ResumeGame();
				}
				else
				{
					PrevMenu();
				}
			}
		}
		else if (CurMenuMode == MenuMode.EXIT)
		{
			if (MessageBox.IsVisible())
			{
				if (!MessageBox.IsAutoUpdate() && MessageBox.Update())
				{
					int result = MessageBox.GetResult();
					if (result <= 0)
					{
						PrevMenu();
					}
					else
					{
						Main.SaveOnExit = result == 2;
					}
				}
			}
			else
			{
				ExitGame();
			}
		}
		else if (CurMenuMode == MenuMode.EXIT_UGC_BLOCKED)
		{
			if (MessageBox.IsVisible())
			{
				if (!MessageBox.IsAutoUpdate() && MessageBox.Update())
				{
					Main.SaveOnExit = MessageBox.GetResult() != 1;
				}
			}
			else
			{
				ExitGame();
			}
		}
		else if (CurMenuMode == MenuMode.BLACKLIST)
		{
			if (MessageBox.IsVisible() && !MessageBox.IsAutoUpdate() && MessageBox.Update())
			{
				if (MessageBox.GetResult() <= 0)
				{
					PrevMenu();
				}
				else
				{
					blacklist.Add(Main.GetWorldId());
					SaveSettings();
					ExitGame();
				}
			}
		}
		else if (CurMenuMode == MenuMode.BLACKLIST_REMOVE)
		{
			if (MessageBox.IsVisible() && !MessageBox.IsAutoUpdate() && MessageBox.Update())
			{
				if (MessageBox.GetResult() <= 0)
				{
					if (CurMenuType == MenuType.MAIN)
					{
						Exit();
						return;
					}
					ExitGame();
				}
				else
				{
					blacklist.Remove(Main.GetWorldId());
					SettingsDirty = true;
					PrevMenu();
					if (CurMenuType != MenuType.MAIN)
					{
						CurMenuType = MenuType.NONE;
					}
					Main.CheckWorldId = true;
				}
			}
		}
		else if (CurMenuMode == MenuMode.MAP)
		{
			// Back (Escape/Tab) always exits, even while the map is still being generated.
			// Previously the whole block was gated on IsThreadDone, so during generation the
			// key did nothing and the screen felt impossible to leave.
			if (IsBackButtonTriggered())
			{
				miniMap.DestroyMap();
				ResumeGame();
			}
			else if (miniMap.IsThreadDone)
			{
				// Pointer first: dragging pans and the wheel zooms, which the stick path cannot
				// express. Stick-driven movement still runs afterwards - it is zero when the
				// sticks are at rest, which is the usual case with a mouse in hand.
				MapScreenMouse.Update(this);
				miniMap.UpdateMap(this);
			}
			else
			{
				// Still building: nothing can be clicked yet, but the wheel counter is shared and
				// must not survive into the game as a hotbar scroll.
				Terraria.Input.KeyboardMouseInput.ConsumeWheelSteps();
			}
		}
		else if (CurMenuMode == MenuMode.CHARACTER_SELECT)
		{
			menuLeft += 80;
			menuTop = 200;
			menuSpace = 40;
			// Rows: one per saved character, then "New character". Built from the slots rather
			// than from a fixed array, so the list has no ceiling and a deleted character leaves
			// a reusable slot instead of a hole nobody can fill.
			charRows.Clear();
			for (int slot = 0; slot < loadPlayerPath.Length; slot++)
			{
				if (!string.IsNullOrEmpty(loadPlayerPath[slot]) && loadPlayer[slot] != null)
				{
					charRows.Add(slot);
				}
			}
			int numRows = CharacterRowCount();
			charVisible = CharacterVisibleRows();
			// The cursor is an absolute row; the window follows it.
			if (uiY < 0)
			{
				uiY = 0;
			}
			if (uiY >= numRows)
			{
				uiY = (short)(numRows - 1);
			}
			charScroll = SaveSlots.ClampScroll(charScroll, uiY, charVisible, numRows);
			for (int i = 0; i < charVisible; i++)
			{
				int num = charScroll + i;
				if (num >= numRows)
				{
					menuString[i] = null;
					menuHC[i] = 0;
					continue;
				}
				menuScale[i] = 0.8f;
				if (num == charRows.Count)
				{
					menuString[i] = Lang.MenuText[16];
					menuHC[i] = 0;
					continue;
				}
				int num2 = charRows[num];
				menuString[i] = loadPlayer[num2].CharacterName;
				menuHC[i] = loadPlayer[num2].difficulty;
				if (loadPlayer[num2].difficulty == 67)
				{
					menuString[i] = "Unsupported";
					menuHC[i] = 3;
				}
			}
			numMenuItems = (byte)Math.Min(charVisible, numRows);
			// The cursor walks all rows (so the D-pad can leave the window); only the visible ones
			// are drawn. Height is a byte in the legacy navigation code, so very long lists stop
			// being reachable by stick at 127 - the pointer still reaches them.
			int num4 = Math.Min(numRows, 127);
			BuildCharacterCoords(num4);
			uiWidth = 1;
			uiHeight = (sbyte)num4;
			uiCoords = charCoords;
			// Visible row -> absolute row -> save slot. Everything below works on the slot; the
			// menu indices only mean "row on screen".
			int selectedRow = ((selectedMenu >= 0) ? (charScroll + selectedMenu) : -1);
			int focusedRow = ((focusMenu >= 0) ? (charScroll + focusMenu) : -1);
			int selectedSlot = ((selectedRow >= 0 && selectedRow < charRows.Count) ? charRows[selectedRow] : -1);
			int focusedSlot = ((focusedRow >= 0 && focusedRow < charRows.Count) ? charRows[focusedRow] : -1);
			if (IsBackButtonTriggered() && (Netplay.gamersWhoReceivedInvite.Count < 2 || !Netplay.gamersWhoReceivedInvite.Contains(SignedInGamer)))
			{
				CancelInvite(SignedInGamer);
				SetMenu(MenuMode.TITLE, rememberPrevious: false, reset: true);
			}
			else if (selectedRow == charRows.Count)
			{
				Main.PlaySound(10);
				int num3 = NextFreePlayerSlot();
				EnsurePlayerSlots(num3 + 1);
				loadPlayer[num3] = new Player();
				loadPlayer[num3].CharacterName = SignedInGamer.Gamertag;
				loadPlayer[num3].Inventory[0].SetDefaults("Copper Shortsword");
				loadPlayer[num3].Inventory[0].SetPrefix(-1);
				loadPlayer[num3].Inventory[1].SetDefaults("Copper Pickaxe");
				loadPlayer[num3].Inventory[1].SetPrefix(-1);
				loadPlayer[num3].Inventory[2].SetDefaults("Copper Axe");
				loadPlayer[num3].Inventory[2].SetPrefix(-1);
				CreateCharacterGUI.ApplyDefaultAttributes(loadPlayer[num3]);
				// numLoadPlayers+1 counts the character being created: the name prompt saves it
				// into this slot and only then does it become part of the list.
				numLoadPlayers = Math.Max(numLoadPlayers, num3 + 1);
				creatingPlayerSlot = num3;
				SetMenu(MenuMode.CREATE_CHARACTER);
			}
			else if (selectedSlot >= 0)
			{
				if (loadPlayer[selectedSlot].difficulty != 67)
				{
					selectedPlayer = (sbyte)selectedSlot;
					Main.PlaySound(10);
					SetPlayer(loadPlayer[selectedSlot].DeepCopy());
					playerPathName = loadPlayerPath[selectedSlot];
					if (Netplay.isJoiningRemoteInvite)
					{
						SetMenu(MenuMode.NETPLAY);
						statusText = Lang.MenuText[75];
					}
					else if (this != MainUI)
					{
						SetMenu(MenuMode.WAITING_SCREEN);
					}
					else
					{
						SetMenu(MenuMode.WORLD_SELECT);
					}
				}
			}
			else if (focusedSlot >= 0)
			{
				if (IsButtonTriggered(Buttons.X))
				{
					selectedPlayer = (sbyte)focusedSlot;
					Main.PlaySound(10);
					SetMenu(MenuMode.CONFIRM_DELETE_CHARACTER);
				}
				else
				{
					showPlayer = (sbyte)focusedSlot;
					if (loadPlayer[focusedSlot].difficulty == 67)
					{
						showPlayer = -1;
					}
				}
			}
		}
		else if (CurMenuMode == MenuMode.CREATE_CHARACTER)
		{
			if (creatingPlayerSlot < 0 || creatingPlayerSlot >= loadPlayer.Length)
			{
				// Reached without a chosen slot (a save file that vanished, a menu re-entry).
				// Falling back to a fresh slot is better than indexing past the array.
				int num = NextFreePlayerSlot();
				EnsurePlayerSlots(num + 1);
				loadPlayer[num] = new Player();
				CreateCharacterGUI.ApplyDefaultAttributes(loadPlayer[num]);
				creatingPlayerSlot = num;
			}
			CreateCharacterGUI.Update(loadPlayer[creatingPlayerSlot]);
		}
		else if (CurMenuMode == MenuMode.CONFIRM_LEAVE_CREATE_CHARACTER)
		{
			menuString[0] = Lang.MenuText[49];
			noFocus[0] = true;
			menuString[1] = Lang.MenuText[104];
			menuString[2] = Lang.MenuText[105];
			numMenuItems = 3;
			if (selectedMenu == 1)
			{
				// Yes: leave creation and go back to the character list. This used to be
				// PrevMenu(-2), which is a single step (a negative argument means "back one"),
				// so the answer landed right back in the creator and looked ignored.
				ReturnToCharacterSelect();
			}
			else if (selectedMenu == 2 || IsBackButtonTriggered())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.NAME_CHARACTER)
		{
			// Used to generate a name and commit it without asking: pressing Start in
			// character creation decided your character's name forever. Now the on-screen
			// keyboard asks, pre-filled with the same generated default.
			if (NamePrompt.IsOpen)
			{
				// The keyboard owns the input; nothing to do until it answers.
				return;
			}
			numMenuItems = 0;
			string defaultName = NameRules.DefaultCharacterName();
			NamePrompt.Ask(MenuMode.NAME_CHARACTER, "Character name", defaultName, NameRules.MaxCharacterLength, delegate(string name)
			{
				Main.PlaySound(10);
				int num = creatingPlayerSlot;
				if (num < 0 || num >= loadPlayer.Length || num >= loadPlayerPath.Length || loadPlayer[num] == null)
				{
					Program.LogWarn("UI", "no slot to save the new character into");
					ReadError();
					PrevMenu();
					return;
				}
				Player player = loadPlayer[num];
				player.CharacterName = name;
				player.ui = this;
				// Only advance once the file is actually on disk. This used to ignore the result,
				// so a failed save still took the player to the character list with a character
				// that existed in memory only - and the keyboard re-opened on the next frame,
				// which read as "the name prompt is broken".
				string path = nextLoadPlayer();
				if (path == null || !player.Save(path))
				{
					Program.LogWarn("UI", "could not save the new character");
					ReadError();
					PrevMenu();
					return;
				}
				loadPlayerPath[num] = path;
				// Back to the list, not to the creator: PrevMenu(-2) meant "one step back", so the
				// character was saved and the screen stayed in creation - it looked as if Enter
				// saved nothing and could not be left.
				ReturnToCharacterSelect();
				selectedPlayer = (sbyte)num;
				showPlayer = (sbyte)num;
				// Put the cursor on the new character's row. Its row index is "how many saved slots
				// come before it", which is what the list will show on the next frame.
				int num2 = 0;
				for (int i = 0; i < num; i++)
				{
					if (!string.IsNullOrEmpty(loadPlayerPath[i]))
					{
						num2++;
					}
				}
				charScroll = 0;
				uiY = (short)num2;
				numLoadPlayers = CountSavedPlayers();
			}, delegate
			{
				// Cancelled: back to character creation, character not saved.
				PrevMenu();
			});
		}
		else if (CurMenuMode == MenuMode.CONFIRM_DELETE_CHARACTER)
		{
			menuString[0] = Lang.MenuText[46] + loadPlayer[selectedPlayer].Name + "?";
			noFocus[0] = true;
			menuString[1] = Lang.MenuText[104];
			menuString[2] = Lang.MenuText[105];
			numMenuItems = 3;
			if (selectedMenu == 2 || IsBackButtonTriggered())
			{
				PrevMenu();
			}
			else if (selectedMenu == 1)
			{
				ErasePlayer(selectedPlayer);
				Main.PlaySound(10);
				PrevMenu();
				ResetPlayerMenuSelection();
			}
		}
		else if (CurMenuMode == MenuMode.WORLD_SELECT)
		{
			WorldSelect.Update();
		}
		else if (CurMenuMode == MenuMode.GAME_MODE)
		{
			GameMode.Update();
		}
		else if (CurMenuMode == MenuMode.NAME_WORLD)
		{
			// Same story as the character name: the world used to be created and named
			// without asking, so every world was called something like
			// "WORLD_A3F19C07_123456" until the player renamed the save file.
			if (NamePrompt.IsOpen)
			{
				return;
			}
			numMenuItems = 0;
			string defaultWorld = NameRules.DefaultWorldName();
			NamePrompt.Ask(MenuMode.NAME_WORLD, "World name", defaultWorld, NameRules.MaxWorldLength, delegate(string name)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.STATUS_SCREEN, rememberPrevious: false);
				WorldSelect.CreateWorld(name);
			}, delegate
			{
				// Cancelled: back to the world size choice, nothing created.
				PrevMenu();
			});
		}
		else if (CurMenuMode == MenuMode.CONFIRM_DELETE_WORLD)
		{
			menuString[0] = Lang.MenuText[46] + WorldSelect.WorldName() + "?";
			noFocus[0] = true;
			menuString[1] = Lang.MenuText[104];
			menuString[2] = Lang.MenuText[105];
			numMenuItems = 3;
			if (selectedMenu == 2 || IsBackButtonTriggered())
			{
				PrevMenu();
			}
			else if (selectedMenu == 1)
			{
				WorldSelect.EraseWorld();
				Main.PlaySound(10);
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.OPTIONS)
		{
			menuTop = 220;
			menuSpace = 57;
			menuString[0] = Lang.MenuText[110];
			menuString[1] = Lang.MenuText[111];
			menuString[2] = Lang.MenuText[14];
			if (CurMenuType == MenuType.MAIN)
			{
				menuString[3] = Lang.MenuText[47];
				MENU_OPTIONS_COORDS[3].X = ViewportHalfWidth;
				numMenuItems = 4;
			}
			else
			{
				MENU_OPTIONS_COORDS[3].X = 0;
				if (uiY == 3)
				{
					uiY = 0;
				}
				numMenuItems = 3;
			}
			if (selectedMenu == 0)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.HOW_TO_PLAY);
			}
			else if (selectedMenu == 1)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.CONTROLS);
			}
			else if (selectedMenu == 2)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.SETTINGS);
			}
			else if (selectedMenu == 3)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.CREDITS);
			}
			else if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.HOW_TO_PLAY)
		{
			howtoUI.Update();
			if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.SHOW_SIGN_IN)
		{
			if (!Guide.IsVisible)
			{
				try
				{
					Guide.ShowSignIn(1, this != MainUI && Main.NetMode != 0);
					CurMenuMode = MenuMode.SIGN_IN;
				}
				catch (GuideAlreadyVisibleException)
				{
				}
			}
		}
		else if (CurMenuMode == MenuMode.SIGN_IN)
		{
			if (!Guide.IsVisible)
			{
				if (SignedInGamer == null)
				{
					foreach (SignedInGamer signedInGamer in Gamer.SignedInGamers)
					{
						if (signedInGamer.PlayerIndex == controller)
						{
							SignedInGamer = signedInGamer;
							SignedInGamer.Presence.SetPresenceModeStringEXT("Menu");
							InitPlayerStorage();
							break;
						}
					}
				}
				if (SignedInGamer != null)
				{
					if (!IsGamerValid())
					{
						MessageBox.Show(controller, Lang.MenuText[5], Lang.InterfaceText[43], new string[1] { Lang.MenuText[90] });
						CurMenuMode = MenuMode.SIGN_IN_FAILED;
					}
					else if (playerStorage == null || playerStorage.IsDone)
					{
						PrevMenu();
					}
				}
				else
				{
					CurMenuMode = MenuMode.SIGN_IN_FAILED;
				}
			}
		}
		else if (CurMenuMode == MenuMode.CONTROLS)
		{
			numMenuItems = 0;
			if (IsButtonTriggered(Buttons.A))
			{
				alternateGrappleControls = !alternateGrappleControls;
				UpdateAlternateGrappleControls();
				SettingsDirty = true;
			}
			else if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.SETTINGS)
		{
			menuTop = 200;
			menuSpace = 44;
			numMenuItems = 9;
			if (this == MainUI)
			{
				menuString[0] = Lang.MenuText[65];
				MENU_SETTINGS_COORDS[0].X = ViewportHalfWidth;
			}
			else
			{
				menuString[0] = "";
				MENU_SETTINGS_COORDS[0].X = 0;
				if (uiY == 0)
				{
					uiY = 1;
				}
			}
			if (autoSave)
			{
				menuString[1] = Lang.MenuText[67];
			}
			else
			{
				menuString[1] = Lang.MenuText[68];
			}
			if (HasPlayerStorage())
			{
				MENU_SETTINGS_COORDS[1].X = ViewportHalfWidth;
				menuHC[1] = 0;
			}
			else
			{
				MENU_SETTINGS_COORDS[1].X = 0;
				menuHC[1] = 3;
				if (uiY == 1)
				{
					uiY = 2;
				}
			}
			if (ShowItemText)
			{
				menuString[2] = Lang.MenuText[71];
			}
			else
			{
				menuString[2] = Lang.MenuText[72];
			}
			// In-game toggle for the cheat system. Previously the only way to change it was
			// editing Settings.ini, which is not something a player should have to do.
			menuString[3] = Texts.Format("ui.settings.cheats", Texts.Get(Terraria.Cheats.CheatState.Enabled ? "ui.common.on" : "ui.common.off"));
			MENU_SETTINGS_COORDS[3].X = ViewportHalfWidth;
			menuHC[3] = 0;
			menuString[4] = Texts.Get("ui.settings.keys");
			MENU_SETTINGS_COORDS[4].X = ViewportHalfWidth;
			menuHC[4] = 0;
			menuString[5] = Texts.Format("ui.settings.mods", Terraria.Mods.ModLoader.ModCount);
			MENU_SETTINGS_COORDS[5].X = ViewportHalfWidth;
			menuHC[5] = 0;
			menuString[6] = Texts.Format("ui.settings.display", Terraria.DisplaySettings.Width, Terraria.DisplaySettings.Height);
			MENU_SETTINGS_COORDS[6].X = ViewportHalfWidth;
			menuHC[6] = 0;
			menuString[7] = Texts.Format("ui.settings.network", Terraria.Net.NetworkConfig.Port);
			MENU_SETTINGS_COORDS[7].X = ViewportHalfWidth;
			menuHC[7] = 0;
			menuString[8] = Texts.Format("ui.settings.hud", Terraria.HudSettings.ZoomLabel);
			MENU_SETTINGS_COORDS[8].X = ViewportHalfWidth;
			menuHC[8] = 0;
			if (selectedMenu == 8)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.HUD);
			}
			else if (selectedMenu == 7)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.NETWORK);
			}
			else if (selectedMenu == 6)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.DISPLAY);
			}
			else if (selectedMenu == 5)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.MODS);
			}
			else if (selectedMenu == 4)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.KEYBINDS);
			}
			else if (selectedMenu == 3)
			{
				Main.PlaySound(12);
				Terraria.Cheats.CheatState.Enabled = !Terraria.Cheats.CheatState.Enabled;
				Terraria.Cheats.CheatState.PersistEnabled();
				if (!Terraria.Cheats.CheatState.Enabled)
				{
					// Leaving god mode and friends active after switching cheats off would be
					// confusing: make the off switch mean off.
					Terraria.Cheats.CheatState.Reset();
				}
				SettingsDirty = true;
			}
			else if (selectedMenu == 2)
			{
				Main.PlaySound(12);
				ShowItemText = !ShowItemText;
				SettingsDirty = true;
			}
			else if (selectedMenu == 1)
			{
				Main.PlaySound(12);
				autoSave = !autoSave;
				SettingsDirty = true;
			}
			else if (selectedMenu == 0)
			{
				Main.PlaySound(10);
				SetMenu(MenuMode.VOLUME);
			}
			else if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.VOLUME)
		{
			soundUI.Update();
			menuTop = 200;
			if (Main.ScreenHeightPtr == ScreenHeights.FHD)
			{
				menuTop -= 10;
			}
			menuSpace = 60;
			numMenuItems = 1;
			menuString[0] = Lang.MenuText[65];
			noFocus[0] = true;
			if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
		}
		else if (CurMenuMode == MenuMode.WORLD_SIZE)
		{
			menuTop = 190;
			menuSpace = 60;
			menuY[1] = 30;
			menuY[2] = 30;
			menuY[3] = 30;
			menuY[4] = 40;
			menuString[0] = Lang.MenuText[91];
			noFocus[0] = true;
			menuString[1] = Lang.MenuText[92];
			menuString[2] = Lang.MenuText[93];
			menuString[3] = Lang.MenuText[94];
			numMenuItems = 4;
			if (IsBackButtonTriggered())
			{
				PrevMenu();
			}
			else if (selectedMenu > 0)
			{
				Main.PlaySound(10);
				if (selectedMenu == 1)
				{
					Main.MaxTilesX = 4200;
					Main.MaxTilesY = 1200;
				}
				else if (selectedMenu == 2)
				{
					Main.MaxTilesX = 4620;
					Main.MaxTilesY = 1320;
				}
				else
				{
					Main.MaxTilesX = 5040;
					Main.MaxTilesY = 1440;
				}
				WorldGen.setWorldSize();
				ClearInput();
				SetMenu(MenuMode.NAME_WORLD);
			}
		}
		else
		{
			if (CurMenuMode == MenuMode.QUIT)
			{
				Exit();
				return;
			}
			if (CurMenuMode == MenuMode.SIGN_IN_FAILED)
			{
				if (!MessageBox.IsVisible())
				{
					if (this == MainUI)
					{
						SetMenu(MenuMode.WELCOME, rememberPrevious: false, reset: true);
					}
					else
					{
						Exit();
					}
				}
			}
			else if (CurMenuMode == MenuMode.CREDITS)
			{
				numMenuItems = 0;
				Credits.Update();
			}
			else if (CurMenuMode == MenuMode.UPSELL)
			{
				numMenuItems = 0;
				if (IsBackButtonTriggered())
				{
					SetMenu(MenuMode.TITLE, rememberPrevious: false, reset: true);
				}
				else if (IsButtonTriggered(Buttons.A))
				{
					HasQuit = true;
				}
				else if (IsButtonTriggered(Buttons.X) && SignedInGamer.Privileges.AllowPurchaseContent)
				{
					_ = Guide.IsVisible;
				}
			}
		}
		if (Main.HardmodeAlert && CurMenuMode == MenuMode.HARDMODE_UPSELL)
		{
			hardmodeUpsell.Update();
			if (IsBackButtonTriggered())
			{
				ResumeGame();
			}
		}
		if (CurMenuMode != curMenuMode)
		{
			numMenuItems = 0;
			for (int n = 0; n < 14; n++)
			{
				menuItemScale[n] = 0.8f;
			}
		}
		focusMenu = -1;
		selectedMenu = -1;
		for (int num3 = 0; num3 < numMenuItems; num3++)
		{
			// Same arithmetic as the drawing code in DrawMenu, including ScreenMultiplier. The row
			// is drawn at menuTop * mult, so hit-testing at the unscaled menuTop put the clickable
			// area somewhere else entirely as soon as the render scale was not 540p - which is
			// exactly the "the mouse still thinks it is 540p" symptom.
			float num31 = (float)menuTop * Main.ScreenMultiplier + (float)menuSpace * Main.ScreenMultiplier * (float)num3 + (float)menuY[num3];
			float num32 = 50f * menuScale[num3] * Main.ScreenMultiplier;
			if (menuString[num3] != null && (float)MouseY > num31 && (float)MouseY < num31 + num32 && Main.HasFocus && !noFocus[num3] && !blockFocus[num3])
			{
				focusMenu = (sbyte)num3;
				if (oldMenu != focusMenu)
				{
					Main.PlaySound(12);
				}
				if ((CurMenuMode != MenuMode.PAUSE && CurMenuMode != MenuMode.CHEATS) ? IsSelectButtonTriggered() : IsButtonTriggered(Buttons.A))
				{
					selectedMenu = (sbyte)num3;
				}
			}
		}
		oldMenu = focusMenu;
	}

	private void DrawSaveIconMessage()
	{
		int num = 500;
		int y = 304;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num = (int)((float)num * 1.15f);
			y = 371;
			break;
		case ScreenHeights.FHD:
			num = (int)((float)num * 1.5f);
			y = 572;
			break;
		}
		DrawDialog(new Vector2(Main.ResolutionWidth - num >> 1, 220f * Main.ScreenMultiplier), new Color(128, 128, 128, 64), new Color(255, 255, 255, 255), saveIconMessage, Lang.MenuText[3]);
		float num2 = 60f;
		SpriteSheet<_sheetSprites>.Draw(642, Main.ResolutionWidth / 2, y, Color.White, (float)((double)Main.FrameCounter * (-Math.PI / (double)num2)), 1f);
	}

	private void DrawMenu()
	{
		if (CurMenuMode != MenuMode.CONTROLS && CurMenuMode != MenuMode.HARDMODE_UPSELL && CurMenuMode != MenuMode.MAP && CurMenuMode != MenuMode.HOW_TO_PLAY && CurMenuMode != MenuMode.LEADERBOARDS && CurMenuMode != MenuMode.ACHIEVEMENTS && CurMenuMode != MenuMode.WORLD_SELECT && CurMenuMode != MenuMode.CREATE_CHARACTER && CurMenuMode != MenuMode.NAME_CHARACTER && CurMenuMode != MenuMode.CREDITS && CurMenuMode != MenuMode.UPSELL && CurMenuMode != MenuMode.CHEATS)
		{
			DrawLogo();
		}
		if (saveIconMessageTime > 0)
		{
			if (!Guide.IsVisible)
			{
				saveIconMessageTime--;
				if (IsButtonTriggered(Buttons.B))
				{
					saveIconMessageTime = 0;
				}
				DrawSaveIconMessage();
				DrawControls();
			}
			return;
		}
		int num = 500;
		float y = 460f;
		int wrapWidth = 470;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num = (int)((float)num * 1.15f);
			y = 600f;
			wrapWidth = 548;
			break;
		case ScreenHeights.FHD:
			num = (int)((float)num * 1.534f);
			y = 920f;
			wrapWidth = 705;
			break;
		}
		if (CurMenuMode == MenuMode.STATUS_SCREEN || CurMenuMode == MenuMode.NETPLAY)
		{
			int num2 = menuTop + 100;
			float num3 = Progress * numProgressStepsInv + progressTotal;
			Rectangle value = default;
			value.Height = progressBarTexture.Height >> 1;
			value.Width = (int)(float)progressBarTexture.Width;
			value.Y = value.Height;
			Vector2 position = new Vector2
			{
				X = CurrentView.ViewWidth - (int)((float)progressBarTexture.Width * Main.ScreenMultiplier) >> 1,
				Y = (float)num2 * Main.ScreenMultiplier
			};
			Main.SpriteBatch.Draw(progressBarTexture, position, value, Color.White, 0f, default, Main.ScreenMultiplier, SpriteEffects.None, 0f);
			if (num3 > 0f)
			{
				value.Y = 0;
				value.Width = (int)((float)progressBarTexture.Width * num3);
				Main.SpriteBatch.Draw(progressBarTexture, position, value, Color.White, 0f, default, Main.ScreenMultiplier, SpriteEffects.None, 0f);
			}
			CurrentView.SetScreenViewWideCentered();
			tips.Draw();
			CurrentView.SetScreenView();
		}
		else if (CurMenuMode == MenuMode.CONTROLS)
		{
			int num4 = CurrentView.ViewWidth >> 1;
			Vector2 position2 = new Vector2
			{
				X = num4 - (controlsTexture.Width >> 1),
				Y = Main.ResolutionHeight - controlsTexture.Height >> 1
			};
			Main.SpriteBatch.Draw(controlsTexture, position2, Color.White);
			int num5 = (int)MeasureString(SmallFont, Lang.InterfaceText[24]).X + 60;
			num4 = CurrentView.ViewWidth - CurrentView.SafeAreaOffsetRight - num5;
			int num6 = Main.ResolutionHeight - 76 - CurrentView.SafeAreaOffsetBottom;
			CurrentView.Ui.DrawInventoryCursor(num4, num6, 1.0);
			if (alternateGrappleControls)
			{
				SpriteSheet<_sheetSprites>.Draw(202, num4 + 10, num6 + 10, Color.White);
			}
			DrawStringLC(SmallFont, Lang.InterfaceText[24], num4 + 60, num6 + 26, Color.White);
			ControlDesc[] array = Lang.Controls();
			for (int num7 = array.Length - 1; num7 >= 0; num7--)
			{
				int alignment = array[num7].Alignment;
				int num8 = array[num7].X;
				if (CurrentView.ViewWidth > Main.ResolutionWidth)
				{
					num8 += Main.ResolutionWidth / 2;
				}
				int num9 = array[num7].Y;
				string text = array[num7].Label;
				switch (num7)
				{
				case 0:
					if (alternateGrappleControls)
					{
						text = array[9].Label;
					}
					break;
				case 4:
					if (Main.PSMode)
					{
						text = array[5].Label;
						text = ((!alternateGrappleControls) ? (text + array[9].Label) : (text + array[0].Label));
					}
					break;
				case 5:
					if (Main.PSMode)
					{
						text = array[4].Label;
					}
					else
					{
						text = ((!alternateGrappleControls) ? (text + array[9].Label) : (text + array[0].Label));
					}
					break;
				}
				Vector2 vector = MeasureString(BoldSmallFont, text);
				if (alignment < 2)
				{
					num8 -= (int)vector.X >> 1;
					if (alignment == 0)
					{
						num9 -= (int)vector.Y;
					}
				}
				else
				{
					if (alignment == 3)
					{
						num8 -= (int)vector.X;
					}
					num9 -= (int)vector.Y >> 1;
				}
				DrawStringLT(BoldSmallFont, text, num8, num9, Color.White);
			}
		}
		else if (CurMenuMode == MenuMode.MAP)
		{
			if (Netplay.Session != null)
			{
				DrawMiniMap();
			}
		}
		else if (CurMenuMode == MenuMode.HOW_TO_PLAY)
		{
			CurrentView.SetScreenViewWideCentered();
			howtoUI.Draw();
			CurrentView.SetScreenView();
		}
		else if (CurMenuMode == MenuMode.WORLD_SELECT)
		{
			WorldSelect.Draw(CurrentView);
		}
		else if (CurMenuMode == MenuMode.GAME_MODE)
		{
			GameMode.Draw(CurrentView);
		}
		else if (CurMenuMode == MenuMode.CREATE_CHARACTER || CurMenuMode == MenuMode.NAME_CHARACTER)
		{
			// NAME_CHARACTER draws the creator too: the on-screen keyboard asks for the name on
			// top of this screen, and blanking it to the logo behind the keyboard looked like
			// the creator had crashed.
			CurrentView.SetScreenViewWideCentered();
			CreateCharacterGUI.Draw(CurrentView);
			CurrentView.SetScreenView();
			showPlayer = -1;
		}
		else if (CurMenuMode == MenuMode.VOLUME)
		{
			soundUI.Draw(Main.SpriteBatch);
		}
		else if (CurMenuMode == MenuMode.WELCOME)
		{
			string text2 = Lang.MenuText[52];
			if (Main.ScreenHeightPtr == ScreenHeights.FHD)
			{
				text2 = $"Press {'\u0088'}";
			}
			SpriteFont bigFont = BigFont;
			Vector2 origin = bigFont.MeasureString(text2) * 0.5f;
			Vector2 position3 = new Vector2(Main.ResolutionWidth / 2, y);
			float num10 = 0.75f;
			num10 *= 1f + cursorAlpha * 0.1f;
			Color color = new Color(CursorColour.A, CursorColour.A, 100, 255);
			if (Main.ScreenHeightPtr == ScreenHeights.FHD)
			{
				text2 = "Press ";
				Main.SpriteBatch.DrawString(bigFont, text2, position3, color, 0f, origin, num10, SpriteEffects.None, 0f);
				text2 = $"  {'\u0088'}";
				origin.X -= (float)(Main.PSMode ? 339 : 318) * 0.5f;
				color = Color.White;
				Main.SpriteBatch.DrawString(bigFont, text2, position3, color, 0f, origin, num10, SpriteEffects.None, 0f);
			}
			else
			{
				Main.SpriteBatch.DrawString(bigFont, text2, position3, color, 0f, origin, num10, SpriteEffects.None, 0f);
			}
		}
		else if (CurMenuMode == MenuMode.LEADERBOARDS)
		{
			CurrentView.SetScreenViewWideCentered();
			Leaderboards.Draw(CurrentView);
			CurrentView.SetScreenView();
		}
		else if (CurMenuMode == MenuMode.ACHIEVEMENTS)
		{
			CurrentView.SetScreenViewWideCentered();
			Achievements.Draw(CurrentView);
			CurrentView.SetScreenView();
		}
		else if (CurMenuMode == MenuMode.ERROR)
		{
			float y2 = 260f * Main.ScreenMultiplier;
			if (errorCompiledText == null)
			{
				errorCompiledText = new CompiledText(errorDescription, wrapWidth, BoldSmallTextStyle);
			}
			DrawDialog(new Vector2(CurrentView.ViewWidth - num >> 1, y2), new Color(128, 128, 128, 64), new Color(255, 255, 255, 255), errorCompiledText, errorCaption);
		}
		else if (CurMenuMode == MenuMode.CREDITS)
		{
			Credits.Draw();
		}
		else if (CurMenuMode == MenuMode.UPSELL)
		{
			theGame.DrawUpsell();
		}
		else if (CurMenuMode == MenuMode.CHEATS)
		{
			Terraria.Cheats.CheatMenu.Draw();
		}
		else if (CurMenuMode == MenuMode.KEYBINDS)
		{
			Terraria.Input.KeyBindScreen.Draw(this);
		}
		else if (CurMenuMode == MenuMode.MODS)
		{
			Terraria.Mods.ModMenu.Draw();
		}
		else if (CurMenuMode == MenuMode.DISPLAY)
		{
			Terraria.DisplayMenu.Draw();
		}
		else if (CurMenuMode == MenuMode.NETWORK)
		{
			Terraria.NetworkMenu.Draw();
		}
		else if (CurMenuMode == MenuMode.HUD)
		{
			Terraria.HudMenu.Draw();
		}
		else if (CurMenuMode == MenuMode.BACKUPS)
		{
			Terraria.BackupsMenu.Draw();
		}
		if (Main.HardmodeAlert && CurMenuMode == MenuMode.HARDMODE_UPSELL)
		{
			CurrentView.SetScreenViewWideCentered();
			hardmodeUpsell.Draw();
			CurrentView.SetScreenView();
		}
		// One font for measuring, drawing and hit-testing.
		SpriteFont menuFont = BigFont;
		// Invalidate the whole hit-rect table before recording this frame's entries. It used to be
		// invalidated only for indices whose text was null, so a screen that draws its own layout
		// (the game-mode screen, world select, the inventory-style screens) inherited the hit
		// rectangles of the menu it replaced: `ClickOutsideMenuEntries` stayed true, every click
		// outside those stale rectangles was swallowed instead of becoming A, and the screen
		// answered only where the previous menu's text happened to be. That is why the mode
		// checkboxes reacted to their labels but not to their icons.
		ClearMenuHits();
		for (int i = 0; i < numMenuItems; i++)
		{
			if (menuString[i] != null)
			{
				float num11 = menuItemScale[i];
				Color c;
				if (menuHC[i] == 3)
				{
					c = new Color(120, 120, 120);
				}
				else if (menuHC[i] == 2)
				{
					c = new Color(200, 125, 255);
				}
				else if (menuHC[i] == 1)
				{
					c = new Color(125, 125, 255);
				}
				else if (noFocus[i])
				{
					c = new Color(255, 200, 62, 255);
				}
				else if (i != focusMenu)
				{
					c = new Color(240, 240, 240, 240);
				}
				else
				{
					num11 *= 1f + cursorAlpha * 0.1f;
					c = new Color(CursorColour.A, CursorColour.A, 100, 255);
				}
				// One font for measuring, drawing and hit-testing. The cheat list uses the small
				// font because its entries are long sentences rather than two-word menu items.
				Vector2 pivot = MeasureString(menuFont, menuString[i]);
				pivot.X *= 0.5f;
				pivot.Y *= 0.5f;
				num11 *= menuScale[i];
				Vector2 vector = new Vector2(menuLeft, (float)menuTop * Main.ScreenMultiplier + (float)(int)menuSpace * Main.ScreenMultiplier * (float)i + pivot.Y * menuScale[i] + (float)menuY[i]);
				DrawStringScaled(pos: vector, font: menuFont, s: menuString[i], c: c, pivot: pivot, scale: num11);
				// Record the real on-screen bounds so mouse hit-testing matches the text
				// exactly instead of guessing "nearest entry" from a single point.
				Vector2 vector2 = MeasureString(menuFont, menuString[i]) * num11;
				menuHitRects[i] = new Rectangle((int)(vector.X - vector2.X * 0.5f), (int)(vector.Y - vector2.Y * 0.5f), (int)vector2.X, (int)vector2.Y);
				menuHitValid[i] = true;
			}
		}
		menuHitAny = false;
		for (int num13 = 0; num13 < 14; num13++)
		{
			if (menuHitValid[num13])
			{
				menuHitAny = true;
				break;
			}
		}
		for (int j = 0; j < 14; j++)
		{
			if (j == focusMenu)
			{
				if (menuItemScale[j] < 1f)
				{
					menuItemScale[j] += 0.05f;
				}
				if (menuItemScale[j] > 1f)
				{
					menuItemScale[j] = 1f;
				}
			}
			else if ((double)menuItemScale[j] > 0.8)
			{
				menuItemScale[j] -= 0.05f;
			}
		}
		if (showPlayer >= 0)
		{
			Player player = loadPlayer[showPlayer];
			player.velocity.X = 1f;
			player.PlayerFrame();
			float x = 148f * Main.ScreenMultiplier;
			float y3 = 244f * Main.ScreenMultiplier;
			float scale = 4f * Main.ScreenMultiplier;
			DrawPlayer(player, new Vector2(x, y3), scale);
		}
		DrawControls();
	}

	private void ResumeGame()
	{
		Main.PlaySound(10);
		PrevMenu();
		ClearButtonTriggers();
		CurMenuType = MenuType.NONE;
		worldFadeTarget = 1f;
	}

	public void DrawPlayer(Player player, Vector2 position, float scale)
	{
		Main.SpriteBatch.End();
		Vector2 position2 = player.Position;
		player.Position.X = CurrentView.ScreenPosition.X;
		player.Position.Y = CurrentView.ScreenPosition.Y;
		Matrix world = Matrix.CreateScale(scale, scale, 1f);
		world.M41 = position.X;
		world.M42 = position.Y;
		CurrentView.ScreenProjection.World = world;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, CurrentView.ScreenProjection);
		player.Draw(CurrentView, IsMenuScr: true);
		Main.SpriteBatch.End();
		CurrentView.ScreenProjection.World = Matrix.Identity;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, CurrentView.ScreenProjection);
		player.Position = position2;
		player.XYWH.X = (int)position2.X;
		player.XYWH.Y = (int)position2.Y;
	}

	public void DrawPlayerIcon(Player player, Vector2 position, float scale)
	{
		Vector2 position2 = player.Position;
		player.Position.X = CurrentView.ScreenPosition.X;
		player.Position.Y = CurrentView.ScreenPosition.Y;
		Vector2 velocity = player.velocity;
		player.velocity.X = 0f;
		player.velocity.Y = 0f;
		Matrix world = Matrix.CreateScale(scale, scale, 1f);
		world.M41 = position.X;
		world.M42 = position.Y;
		CurrentView.ScreenProjection.World = world;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, CurrentView.ScreenProjection);
		player.Draw(CurrentView, IsMenuScr: true, IsIcon: true);
		Main.SpriteBatch.End();
		CurrentView.ScreenProjection.World = Matrix.Identity;
		player.velocity = velocity;
		player.Position = position2;
		player.XYWH.X = (int)position2.X;
		player.XYWH.Y = (int)position2.Y;
	}

	private bool IsGamerValid()
	{
		if (this == MainUI && SignedInGamer.IsGuest)
		{
			return false;
		}
		if (Main.NetMode > 0 && !CanPlayOnline())
		{
			return false;
		}
		return true;
	}

	private void ShowSignInPortal()
	{
		foreach (SignedInGamer signedInGamer in Gamer.SignedInGamers)
		{
			if (signedInGamer.PlayerIndex == controller)
			{
				SignedInGamer = signedInGamer;
				if (IsGamerValid())
				{
					signedInGamer.Presence.SetPresenceModeStringEXT("Menu");
					InitPlayerStorage();
					return;
				}
				SignedInGamer = null;
			}
		}
		SetMenu(MenuMode.SHOW_SIGN_IN);
	}

	private void ShowAchievements()
	{
		if (!Guide.IsVisible)
		{
			Main.PlaySound(10);
			SetMenu(MenuMode.ACHIEVEMENTS);
			AchievementSystem.LoadAchievements();
			AchievementData.SetAchievements();
			Achievements.InitializeData();
		}
	}

	public void ShowParty()
	{
		if (!CanPlayOnline() || Guide.IsVisible)
		{
			return;
		}
		Main.PlaySound(10);
		bool flag;
		do
		{
			flag = false;
			try
			{
				Guide.ShowPartySessions(controller);
			}
			catch (GuideAlreadyVisibleException)
			{
				Thread.Sleep(32);
				flag = true;
			}
		}
		while (flag);
	}

	private void DrawLogo()
	{
		logoRotation += logoRotationSpeed * 3E-05f;
		if ((double)logoRotation > 0.1)
		{
			logoRotationDirection = -1f;
		}
		else if ((double)logoRotation < -0.1)
		{
			logoRotationDirection = 1f;
		}
		if ((logoRotationSpeed < 20f) & (logoRotationDirection == 1f))
		{
			logoRotationSpeed++;
		}
		else if ((logoRotationSpeed > -20f) & (logoRotationDirection == -1f))
		{
			logoRotationSpeed--;
		}
		logoScale += logoScaleSpeed * 1E-05f;
		if ((double)logoScale > 1.1)
		{
			logoScaleDirection = -1f;
		}
		else if ((double)logoScale < 0.9)
		{
			logoScaleDirection = 1f;
		}
		if ((logoScaleSpeed < 50f) & (logoScaleDirection == 1f))
		{
			logoScaleSpeed++;
		}
		else if ((logoScaleSpeed > -50f) & (logoScaleDirection == -1f))
		{
			logoScaleSpeed--;
		}
		Color color = new Color(LogoA, LogoA, LogoA, LogoA);
		Color color2 = new Color(LogoB, LogoB, LogoB, LogoB);
		float x = ((CurrentView != null) ? (CurrentView.ViewWidth >> 1) : (Main.ResolutionWidth / 2));
		float num = 1f;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num = 1.15f;
			break;
		case ScreenHeights.FHD:
			num = 1.5f;
			break;
		}
		Main.SpriteBatch.Draw(logoTexture, new Vector2(x, 100f * Main.ScreenMultiplier), new Rectangle(0, 0, logoTexture.Width, logoTexture.Height), color, logoRotation, new Vector2(logoTexture.Width >> 1, logoTexture.Height >> 1), logoScale * num, SpriteEffects.None, 0f);
		Main.SpriteBatch.Draw(logo2Texture, new Vector2(x, 100f * Main.ScreenMultiplier), new Rectangle(0, 0, logoTexture.Width, logoTexture.Height), color2, logoRotation, new Vector2(logoTexture.Width >> 1, logoTexture.Height >> 1), logoScale * num, SpriteEffects.None, 0f);
		if (CurrentView.WorldTime.DayTime)
		{
			LogoA += 2;
			if (LogoA > 255)
			{
				LogoA = 255;
			}
			LogoB--;
			if (LogoB < 0)
			{
				LogoB = 0;
			}
		}
		else
		{
			LogoB += 2;
			if (LogoB > 255)
			{
				LogoB = 255;
			}
			LogoA--;
			if (LogoA < 0)
			{
				LogoA = 0;
			}
		}
	}

	private void initCharacterSelectCoordinates()
	{
		// The character list is built in UpdateMenu (it depends on how many rows fit on screen),
		// so there is nothing fixed to initialise here. Kept as a named no-op for the SetMenu case
		// below: the coordinates are rebuilt on the first frame of the screen, before any input.
	}

	private void DrawControls()
	{
		if (CurMenuType == MenuType.NONE && InventoryMode == 0)
		{
			return;
		}
		Main.StrBuilder.Length = 0;
		if (PointerActive)
		{
			// Keyboard and mouse players cannot use "A / B / Y" hints - show their own keys.
			AppendPointerHints();
			if (Main.StrBuilder.Length > 0)
			{
				DrawStringLB(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetBottom);
			}
			return;
		}
		if (CurMenuType == MenuType.NONE)
		{
			return;
		}
		if (saveIconMessageTime > 0)
		{
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CLOSE));
		}
		else
		{
			switch (CurMenuMode)
			{
			case MenuMode.WAITING_SCREEN:
			case MenuMode.NETPLAY:
			case MenuMode.ERROR:
			case MenuMode.LOAD_FAILED_NO_BACKUP:
			case MenuMode.CREDITS:
			case MenuMode.ACHIEVEMENTS:
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
				break;
			case MenuMode.MAP:
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.MOVE_MAP));
				Main.StrBuilder.Append(' ');
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.ZOOM));
				Main.StrBuilder.Append(' ');
				if (mapScreenCursorY >= 2)
				{
					if (CanViewGamerCard() && Netplay.Session != null)
					{
						GamerCollection<NetworkGamer> allGamers = Netplay.Session.AllGamers;
						if (mapScreenCursorY - 2 < allGamers.Count)
						{
							Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SHOW_GAMERCARD));
							Main.StrBuilder.Append(' ');
						}
					}
				}
				else if (mapScreenCursorX == 0)
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.TOGGLE_PVP));
					Main.StrBuilder.Append(' ');
				}
				else
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT_TEAM));
					Main.StrBuilder.Append(' ');
				}
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
				if (Netplay.gamer != null && Main.NetMode > 0)
				{
					if (CanCommunicate())
					{
						Main.StrBuilder.Append(' ');
						Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.INVITE_PLAYER));
					}
					if (SignedInGamer.PartySize > 1)
					{
						Main.StrBuilder.Append(' ');
						Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.INVITE_PARTY));
					}
				}
				break;
			case MenuMode.TITLE:
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT));
				if (playerStorage != null)
				{
					Main.StrBuilder.Append(' ');
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHANGE_STORAGE));
				}
				if (CanPlayOnline())
				{
					Main.StrBuilder.Append(' ');
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SHOW_PARTY));
				}
				break;
			case MenuMode.CREATE_CHARACTER:
				CreateCharacterGUI.ControlDescription(Main.StrBuilder);
				break;
			case MenuMode.VOLUME:
				soundUI.ControlDescription(Main.StrBuilder);
				break;
			case MenuMode.HOW_TO_PLAY:
				howtoUI.ControlDescription(Main.StrBuilder);
				break;
			case MenuMode.LEADERBOARDS:
				Leaderboards.ControlDescription(Main.StrBuilder);
				break;
			case MenuMode.CHARACTER_SELECT:
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT));
				if (Netplay.gamersWhoReceivedInvite.Count < 2 || !Netplay.gamersWhoReceivedInvite.Contains(SignedInGamer))
				{
					Main.StrBuilder.Append(' ');
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
				}
				// uiY is an absolute row index here (the cursor scrolls, the menu entries are the
				// visible slice), so it names the character directly.
				int num = uiY;
				bool flag = num >= 0 && num < charRows.Count;
				if (flag && loadPlayer[charRows[num]].difficulty == 67)
				{
					Main.StrBuilder.Remove(0, 8);
				}
				if (flag)
				{
					Main.StrBuilder.Append(' ');
					Main.StrBuilder.Append(Lang.MenuText[17]);
				}
				if (CharacterRowCount() > charVisible)
				{
					Main.StrBuilder.Append(' ');
					Main.StrBuilder.Append(Texts.Format("ui.page.counter", charScroll + charVisible, CharacterRowCount()));
				}
				break;
			case MenuMode.WORLD_SELECT:
				WorldSelect.ControlDescription(Main.StrBuilder);
				break;
			case MenuMode.UPSELL:
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.EXIT));
				Main.StrBuilder.Append(' ');
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK_TO_GAME));
				if (SignedInGamer.Privileges.AllowPurchaseContent)
				{
					Main.StrBuilder.Append(' ');
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.UNLOCK_FULL_GAME));
				}
				break;
			case MenuMode.CONTROLS:
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.TOGGLE_GRAPPLE_MODE));
				Main.StrBuilder.Append(' ');
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
				break;
			case MenuMode.PAUSE:
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT));
				Main.StrBuilder.Append(' ');
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
				if (Main.NetMode == 1)
				{
					Main.StrBuilder.Append(' ');
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BLACKLIST));
				}
				Main.StrBuilder.Append(' ');
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.ZOOM));
				break;
			default:
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT));
				Main.StrBuilder.Append(' ');
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
				break;
			case MenuMode.WELCOME:
			case MenuMode.STATUS_SCREEN:
				break;
			}
			if (Main.HardmodeAlert && CurMenuMode == MenuMode.HARDMODE_UPSELL)
			{
				Main.StrBuilder.Length = 0;
				hardmodeUpsell.ControlDescription(Main.StrBuilder);
			}
		}
		if (CurMenuMode == MenuMode.WELCOME)
		{
			string text = "Port by DartPower Team";
			Vector2 vector = BoldSmallFont.MeasureString(Main.OGCVersion);
			int num = Main.ResolutionHeight - CurrentView.SafeAreaOffsetBottom;
			float scale = ((NumActiveViews <= 1) ? TextScale : (TextScale * 2f));
			Main.SpriteBatch.DrawString(BoldSmallFont, text, new Vector2(CurrentView.SafeAreaOffsetLeft, num), Color.White, 0f, new Vector2(0f, BoldSmallFont.MeasureString(text).Y), scale, SpriteEffects.None, 0f);
			Main.SpriteBatch.DrawString(BoldSmallFont, Main.OGCVersion, new Vector2((float)(Main.ResolutionWidth - CurrentView.SafeAreaOffsetRight) - vector.X, num), Color.White, 0f, new Vector2(0f, vector.Y), scale, SpriteEffects.None, 0f);
		}
		if (Main.StrBuilder.Length > 0)
		{
			DrawStringLB(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetBottom);
		}
	}

	/// <summary>
	/// Control hints for keyboard and mouse. Mirrors <see cref="DrawControls"/> but names the
	/// actual keys, which is what a desktop player needs to see.
	/// </summary>
	private void AppendPointerHints()
	{
		if (InventoryMode > 0)
		{
			Main.StrBuilder.Append("LMB - move item    Esc / Tab - close inventory");
			return;
		}
		switch (CurMenuMode)
		{
		case MenuMode.MAP:
			Main.StrBuilder.Append("WASD - move    Tab / Esc - close");
			break;
		case MenuMode.CREATE_CHARACTER:
			Main.StrBuilder.Append("LMB - select    Enter - confirm    Esc - back    Q / R - category");
			break;
		case MenuMode.WORLD_SELECT:
		case MenuMode.GAME_MODE:
			Main.StrBuilder.Append("LMB - select    Esc - back");
			break;
		case MenuMode.CHEATS:
			Main.StrBuilder.Append("LMB - toggle    Esc - back");
			break;
		case MenuMode.KEYBINDS:
			Main.StrBuilder.Append("LMB / Space - rebind    R - reset    Esc - back");
			break;
		case MenuMode.MODS:
			Main.StrBuilder.Append("LMB / Space - enable or disable    R - rescan    Esc - back");
			break;
		case MenuMode.PAUSE:
			Main.StrBuilder.Append("LMB - select    Esc - resume");
			break;
		case MenuMode.WELCOME:
			Main.StrBuilder.Append("Enter - start");
			break;
		case MenuMode.WAITING_SCREEN:
		case MenuMode.NETPLAY:
		case MenuMode.ERROR:
		case MenuMode.LOAD_FAILED_NO_BACKUP:
		case MenuMode.CREDITS:
		case MenuMode.ACHIEVEMENTS:
		case MenuMode.LEADERBOARDS:
		case MenuMode.HOW_TO_PLAY:
			Main.StrBuilder.Append("Esc - back");
			break;
		default:
			Main.StrBuilder.Append("LMB - select    Esc - back");
			break;
		}
	}

	private void DrawHud()
	{
		Vector2 pos = default;
		Color c = new Color(128, 128, 128, 128);
		for (int i = 1; i < ActivePlayer.StatLifeMax / 20 + 1; i++)
		{
			int num = CurrentView.ViewWidth - CurrentView.SafeAreaOffsetRight - 460 + 26 * (i - 1) + 160 + 11;
			int num2 = CurrentView.SafeAreaOffsetTop;
			if (i > 10)
			{
				num -= 260;
				num2 += 26;
			}
			SpriteSheet<_sheetSprites>.Draw(436, num, num2, c);
			int id = 435;
			if (ActivePlayer.statLife < i * 20)
			{
				float num3 = (float)(ActivePlayer.statLife - (i - 1) * 20) / 20f;
				if (num3 <= 0f)
				{
					continue;
				}
				if ((double)num3 < 0.25)
				{
					id = 434;
				}
				else
				{
					id = ((!((double)num3 < 0.5)) ? 432 : 433);
				}
			}
			SpriteSheet<_sheetSprites>.Draw(id, num, num2);
		}
		if (ActivePlayer.breath < 200)
		{
			for (int j = 1; j < 11; j++)
			{
				int num4 = CurrentView.ViewWidth - CurrentView.SafeAreaOffsetRight - 460 + 26 * (j - 1) + 160 + 11;
				int num5 = CurrentView.SafeAreaOffsetTop + 52;
				SpriteSheet<_sheetSprites>.Draw(141, num4, num5, c);
				if (ActivePlayer.breath < j * 20)
				{
					float num6 = (float)(ActivePlayer.breath - (j - 1) * 20) / 20f;
					if (num6 > 0f)
					{
						float scaleCenter = num6 * 0.25f + 0.75f;
						int num7 = (int)(30f + 225f * num6);
						if (num7 < 30)
						{
							num7 = 30;
						}
						c.R = (byte)num7;
						c.G = (byte)num7;
						c.B = (byte)num7;
						c.A = (byte)num7;
						SpriteSheet<_sheetSprites>.DrawScaled(140, num4 + 11, num5 + 11, scaleCenter, c);
						c = new Color(128, 128, 128, 128);
					}
				}
				else
				{
					SpriteSheet<_sheetSprites>.Draw(140, num4, num5);
				}
			}
		}
		if (ActivePlayer.statManaMax2 > 0)
		{
			int num8 = ActivePlayer.statManaMax2 / 20;
			int num9 = CurrentView.ViewWidth - CurrentView.SafeAreaOffsetRight - 22;
			pos.X = num9 + 11;
			c.R = byte.MaxValue;
			c.G = byte.MaxValue;
			c.B = byte.MaxValue;
			c.A = 229;
			int num10 = 0;
			do
			{
				bool flag = false;
				float num11 = 1f;
				if (ActivePlayer.statMana >= (num10 + 1) * 20)
				{
					if (ActivePlayer.statMana == (num10 + 1) * 20)
					{
						flag = true;
					}
				}
				else
				{
					float num12 = (float)(ActivePlayer.statMana - num10 * 20) / 20f;
					int num13 = (int)(30f + 225f * num12);
					if (num13 < 30)
					{
						num13 = 30;
					}
					c.R = (byte)num13;
					c.G = (byte)num13;
					c.B = (byte)num13;
					c.A = (byte)((double)num13 * 0.9);
					num11 = num12 * 0.25f + 0.5f;
					if (num11 < 0.5f)
					{
						num11 = 0.5f;
					}
					if (num12 > 0f)
					{
						flag = true;
					}
				}
				if (flag)
				{
					num11 += cursorScale - 1f;
				}
				int num14 = CurrentView.SafeAreaOffsetTop + 23 * num10;
				pos.Y = (float)(num14 + 11) + (6f - 6f * num11);
				SpriteSheet<_sheetSprites>.Draw(1087, num9, num14, new Color(128, 128, 128, 128));
				SpriteSheet<_sheetSprites>.DrawScaled(1086, ref pos, c, num11);
			}
			while (++num10 < num8);
		}
		c = new Color(99, 99, 99, 99);
		for (int k = 0; k < 10; k++)
		{
			int type = ActivePlayer.buff[k].Type;
			if (type <= 0)
			{
				continue;
			}
			int num15 = (int)(32f * Main.ScreenMultiplier) + CurrentView.SafeAreaOffsetLeft + k * (int)(38f * Main.ScreenMultiplier);
			int num16 = (int)(76f * Main.ScreenMultiplier) + CurrentView.SafeAreaOffsetTop;
			int num17 = 141 + type;
			if (type == 40)
			{
				num17 += ActivePlayer.pet;
			}
			if (Main.ScreenHeightPtr == ScreenHeights.BASE)
			{
				SpriteSheet<_sheetSprites>.Draw(num17, num15, num16, c);
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawScaled(num17, num15, num16, Main.ScreenMultiplier, c);
			}
			if (type != 28 && type != 34 && type != 37 && type != 38 && type != 40)
			{
				int num18 = ActivePlayer.buff[k].Time / 60;
				Main.StrBuilder.Length = 0;
				Main.StrBuilder.Append((num18 / 60).ToStringLookup());
				Main.StrBuilder.Append(':');
				num18 %= 60;
				if (num18 < 10)
				{
					Main.StrBuilder.Append('0');
				}
				Main.StrBuilder.Append(num18.ToStringLookup());
				switch (Main.ScreenHeightPtr)
				{
				case ScreenHeights.HD:
					num15 -= 24;
					num16 -= 10;
					break;
				case ScreenHeights.FHD:
					num15 -= 38;
					num16 += 4;
					break;
				}
				DrawStringLT(ItemStackFont, num15, num16 + 32, Color.White);
			}
		}
		if (npcChatText == null)
		{
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			for (int l = 0; l < 3; l++)
			{
				Main.StrBuilder.Length = 0;
				if (ActivePlayer.accCompass && !flag4)
				{
					Main.StrBuilder.Append(Lang.MenuText[95]);
					int num19 = (ActivePlayer.XYWH.X + 10 >> 3) - Main.MaxTilesX;
					num19 >>= 2;
					if (num19 > 0)
					{
						Main.StrBuilder.Append(num19.ToStringLookup());
						Main.StrBuilder.Append(Lang.MenuText[96]);
					}
					else if (num19 < 0)
					{
						num19 = -num19;
						Main.StrBuilder.Append(num19.ToStringLookup());
						Main.StrBuilder.Append(Lang.MenuText[97]);
					}
					else
					{
						Main.StrBuilder.Append(Lang.MenuText[98]);
					}
					flag4 = true;
				}
				else if (ActivePlayer.accDepthMeter && !flag3)
				{
					Main.StrBuilder.Append(Lang.MenuText[85]);
					int num20 = (ActivePlayer.XYWH.Y + 42 >> 3) - Main.WorldSurface * 2;
					num20 >>= 2;
					if (num20 > 0)
					{
						Main.StrBuilder.Append(num20.ToStringLookup());
						Main.StrBuilder.Append(Lang.MenuText[86]);
					}
					else if (num20 < 0)
					{
						num20 = -num20;
						Main.StrBuilder.Append(num20.ToStringLookup());
						Main.StrBuilder.Append(Lang.MenuText[87]);
					}
					else
					{
						Main.StrBuilder.Append(Lang.MenuText[88]);
					}
					flag3 = true;
				}
				else if (ActivePlayer.accWatch > 0 && !flag2)
				{
					string value = " AM";
					double num21 = Main.GameTime.WorldTime;
					if (!Main.GameTime.DayTime)
					{
						num21 += 54000.0;
					}
					num21 = num21 / 86400.0 * 24.0;
					num21 = num21 - 7.5 - 12.0;
					if (num21 < 0.0)
					{
						num21 += 24.0;
					}
					if (num21 >= 12.0)
					{
						value = " PM";
					}
					int num22 = (int)num21;
					int num23 = (int)((num21 - (double)num22) * 60.0);
					if (num22 > 12)
					{
						num22 -= 12;
					}
					if (num22 == 0)
					{
						num22 = 12;
					}
					string text;
					if (ActivePlayer.accWatch == 1)
					{
						text = "00";
					}
					else if (ActivePlayer.accWatch == 2)
					{
						text = ((num23 >= 30) ? "30" : "00");
					}
					else
					{
						text = num23.ToStringLookup();
						if (num23 < 10)
						{
							text = "0" + text;
						}
					}
					Main.StrBuilder.Append(Lang.InterfaceText[34]);
					Main.StrBuilder.Append(num22.ToStringLookup());
					Main.StrBuilder.Append(':');
					Main.StrBuilder.Append(text);
					Main.StrBuilder.Append(value);
					flag2 = true;
				}
				if (Main.StrBuilder.Length > 0)
				{
					int num24 = 22;
					switch (Main.ScreenHeightPtr)
					{
					case ScreenHeights.HD:
						num24 = 25;
						break;
					case ScreenHeights.FHD:
						num24 = 33;
						break;
					}
					DrawStringLT(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, (int)(110f * Main.ScreenMultiplier + (float)(num24 * l) + (float)(int)(48f * Main.ScreenMultiplier)), mouseTextColor);
				}
			}
		}
		int oldSelectedItem = ActivePlayer.oldSelectedItem;
		int num25 = ((oldSelectedItem >= 0) ? oldSelectedItem : ActivePlayer.SelectedItem);
		float num26 = 1.25f;
		float num27 = 1f;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num26 = 1.40625f;
			num27 = 1.15f;
			break;
		case ScreenHeights.FHD:
			num26 = 1.875f;
			num27 = 1.534f;
			break;
		}
		float num28 = ((NumActiveViews > 1) ? num26 : num27);
		int num29 = CurrentView.SafeAreaOffsetLeft;
		for (int m = 0; m < 10; m++)
		{
			float num30 = hotbarScale[m];
			if (m == num25)
			{
				if (num30 < 1f)
				{
					num30 += 0.05f;
					hotbarScale[m] = num30;
				}
			}
			else if (num30 > 0.75f)
			{
				num30 -= 0.05f;
				hotbarScale[m] = num30;
			}
			num30 *= num28;
			int y = (int)((float)CurrentView.SafeAreaOffsetTop + 22f * (1f - num30));
			int num31 = (int)(65f + 180f * num30);
			Color itemColor = new Color(num31, num31, num31, num31);
			if (m == num25)
			{
				c.R = 200;
				c.G = 200;
				c.B = 200;
				c.A = 200;
				SpriteSheet<_sheetSprites>.DrawTL(448, num29, y, c, num30);
			}
			else
			{
				c.R = 100;
				c.G = 100;
				c.B = 100;
				c.A = 100;
				SpriteSheet<_sheetSprites>.DrawTL(451, num29, y, c, num30);
			}
			int num32 = ((m == oldSelectedItem) ? ActivePlayer.SelectedItem : m);
			if (ActivePlayer.Inventory[num32].Type > 0 && ActivePlayer.Inventory[num32].Stack > 0)
			{
				inventoryScale = num30;
				DrawInventoryItem(ref ActivePlayer.Inventory[num32], num29, y, itemColor, StackType.HOTBAR);
			}
			num29 += (int)(52f * num30) + 4;
		}
		if (quickAccessDisplayTime > 0)
		{
			quickAccessDisplayTime--;
			int alpha = ((quickAccessDisplayTime < 64) ? (quickAccessDisplayTime << 2) : 255);
			DrawQuickAccess(ActivePlayer.SelectedItem, CurrentView.SafeAreaOffsetLeft, Main.ResolutionHeight - CurrentView.SafeAreaOffsetBottom - (int)(160f * Main.ScreenMultiplier), alpha, StackType.HOTBAR);
		}
		if (hotbarItemNameTime > 0)
		{
			hotbarItemNameTime--;
			if (ActivePlayer.Inventory[ActivePlayer.SelectedItem].Type != 0)
			{
				string s = ActivePlayer.Inventory[ActivePlayer.SelectedItem].AffixName();
				int num33 = ((hotbarItemNameTime < 64) ? (hotbarItemNameTime << 2) : 255);
				DrawStringCT(SmallFont, s, (int)(216f * num28) + CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetTop + (int)(44f * num28), new Color(num33, num33, num33, num33));
			}
		}
		DrawControlsIngame();
	}

	private void DrawInventoryItem(int itemTexId, int x, int y, Color itemColor)
	{
		int width = SpriteSheet<_sheetSprites>.Source[itemTexId].Width;
		int height = SpriteSheet<_sheetSprites>.Source[itemTexId].Height;
		float num = ((width <= height) ? (41f / (float)height) : (41f / (float)width));
		if (Main.ScreenHeightPtr == ScreenHeights.BASE)
		{
			num *= inventoryScale;
		}
		if (num > 1.25f)
		{
			num = 1.25f;
		}
		if (Main.ScreenHeightPtr != ScreenHeights.BASE)
		{
			num *= inventoryScale;
		}
		Vector2 pos = new Vector2
		{
			X = (float)x + 26f * inventoryScale,
			Y = (float)y + 26f * inventoryScale
		};
		SpriteSheet<_sheetSprites>.DrawScaled(itemTexId, ref pos, itemColor, num);
	}

	/// <summary>
	/// Draws one sprite from a mod's packed sheet, positioned and scaled exactly like
	/// <c>SpriteSheet.DrawScaled</c>: centred on the cell, rotated about the middle.
	///
	/// Kept as its own method rather than a branch inside the sprite sheet because the sheet class
	/// holds one texture for the whole game, and a mod's sprites are in a texture of their own.
	/// </summary>
	private static void DrawModSprite(Microsoft.Xna.Framework.Graphics.Texture2D texture, Rectangle rect,
		ref Vector2 pos, Color color, float scale)
	{
		Main.SpriteBatch.Draw(texture, pos, rect, color, 0f,
			new Vector2(rect.Width >> 1, rect.Height >> 1), scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
	}

	private void DrawInventoryItem(ref Item item, int x, int y, Color itemColor, StackType stackType = StackType.NONE)
	{
		// A mod item is not in the compiled sprite atlas: its picture lives in that mod's packed
		// sheet. Asking the registry first is what keeps the two from disagreeing.
		Microsoft.Xna.Framework.Graphics.Texture2D modTexture;
		Rectangle modRect;
		bool isModItem = Terraria.Mods.ModItems.TryGetSprite(item.Type, out modTexture, out modRect);
		if (!isModItem && Terraria.Mods.ModItems.IsModItem(item.Type))
		{
			// A mod item whose sprite is missing: the vanilla path would index past the end of
			// the compiled atlas with an id like 20043. Drawing nothing is the honest answer -
			// the slot looks empty, and the log already said why.
			return;
		}
		int num = isModItem ? 0 : (item.Type + 452 - 1);
		int width = isModItem ? modRect.Width : SpriteSheet<_sheetSprites>.Source[num].Width;
		int height = isModItem ? modRect.Height : SpriteSheet<_sheetSprites>.Source[num].Height;
		float num2 = ((width <= height) ? (41f / (float)height) : (41f / (float)width));
		if (Main.ScreenHeightPtr == ScreenHeights.BASE)
		{
			num2 *= inventoryScale;
		}
		if (num2 > 1.25f)
		{
			num2 = 1.25f;
		}
		if (Main.ScreenHeightPtr != ScreenHeights.BASE)
		{
			num2 *= inventoryScale;
		}
		Vector2 pos = new Vector2
		{
			X = (float)x + 26f * inventoryScale,
			Y = (float)y + 26f * inventoryScale
		};
		if (isModItem)
		{
			DrawModSprite(modTexture, modRect, ref pos, item.GetAlphaInventory(itemColor), num2);
			if (item.Colour.PackedValue != 0)
			{
				DrawModSprite(modTexture, modRect, ref pos, item.GetColor(itemColor), num2);
			}
		}
		else
		{
			SpriteSheet<_sheetSprites>.DrawScaled(num, ref pos, item.GetAlphaInventory(itemColor), num2);
			if (item.Colour.PackedValue != 0)
			{
				SpriteSheet<_sheetSprites>.DrawScaled(num, ref pos, item.GetColor(itemColor), num2);
			}
		}
		switch (stackType)
		{
		case StackType.INGREDIENT:
			DrawIngredientStack(ref item, x, y, itemColor);
			return;
		case StackType.NONE:
			return;
		}
		DrawInventoryItemStack(ref item, x, y, ref itemColor);
		if (stackType != StackType.HOTBAR)
		{
			return;
		}
		int useAmmo = item.UseAmmo;
		float num3 = inventoryScale;
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			num3 *= 0.5f;
			num3 -= 0.1f;
			y += FONT_STACK_Y_OFFSET;
		}
		if (useAmmo > 0)
		{
			int num4 = 0;
			for (int i = 0; i < ActivePlayer.Inventory.Length - 1; i++)
			{
				if (ActivePlayer.Inventory[i].Ammo == useAmmo)
				{
					num4 += ActivePlayer.Inventory[i].Stack;
				}
			}
			DrawStringScaled(ItemStackFont, num4.ToStringLookup(), new Vector2((float)(x + FONT_STACK_X_OFFSET) + 10f * inventoryScale, (float)y + 26f * inventoryScale), itemColor, default, num3 + 0.1f);
		}
		else if (item.Type == 509)
		{
			int num5 = 0;
			for (int j = 0; j < ActivePlayer.Inventory.Length - 1; j++)
			{
				if (ActivePlayer.Inventory[j].Type == 530)
				{
					num5 += ActivePlayer.Inventory[j].Stack;
				}
			}
			DrawStringScaled(ItemStackFont, num5.ToStringLookup(), new Vector2((float)(x + FONT_STACK_X_OFFSET) + 10f * inventoryScale, (float)y + 26f * inventoryScale), itemColor, default, num3 + 0.1f);
		}
		if (item.IsPotion)
		{
			Color alphaInventory = item.GetAlphaInventory(itemColor);
			float num6 = (float)(int)ActivePlayer.potionDelay / (float)(int)ActivePlayer.potionDelayTime;
			alphaInventory *= num6;
			pos.X = (float)x + 26f * inventoryScale;
			pos.Y = (float)y + 26f * inventoryScale;
			SpriteSheet<_sheetSprites>.DrawScaled(204, ref pos, alphaInventory, inventoryScale);
		}
	}

	private static void DrawInventoryItemStack(ref Item item, int x, int y, ref Color itemColor)
	{
		if (item.Stack <= 1)
		{
			return;
		}
		float num = inventoryScale;
		SpriteFont font = ItemStackFont;
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			num *= 0.5f;
			if (!MainUI.CurrentView.IsFullScreen() && NumActiveViews > 1)
			{
				font = BoldSmallFont;
				num = 1f;
				y -= 15;
			}
			y += FONT_STACK_Y_OFFSET;
		}
		DrawStringScaled(font, item.Stack.ToStringLookup(), new Vector2((float)(x + FONT_STACK_X_OFFSET) + 10f * inventoryScale, (float)y + 26f * inventoryScale), itemColor, default, num + 0.1f);
	}

	private void DrawIngredientStack(ref Item item, int x, int y, Color itemColor)
	{
		int num = Math.Min(item.Stack, ActivePlayer.CountInventory(item.NetID));
		Main.StrBuilder.Length = 0;
		Main.StrBuilder.Append(item.Stack.ToStringLookup());
		if (num < item.Stack)
		{
			itemColor.G >>= 1;
			itemColor.B >>= 1;
		}
		float num2 = inventoryScale;
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			num2 *= 0.5f;
			y += FONT_STACK_Y_OFFSET;
		}
		DrawStringScaled(ItemStackFont, new Vector2((float)(x + FONT_STACK_X_OFFSET) + 10f * inventoryScale, (float)y + 26f * inventoryScale), itemColor, default, num2 - 0.1f);
	}

	public void DrawInterface()
	{
		if (showNPCs)
		{
			CurrentView.DrawNPCHouse();
		}
		Vector2 pos = default;
		if (ActivePlayer.rulerAcc)
		{
			CurrentView.DrawGrid();
		}
		if (signBubble)
		{
			signBubble = false;
			int num = signX - CurrentView.ScreenPosition.X;
			int num2 = signY - CurrentView.ScreenPosition.Y;
			SpriteEffects se = SpriteEffects.None;
			if ((float)signX > ActivePlayer.Position.X + 20f)
			{
				se = SpriteEffects.FlipHorizontally;
				num -= 40;
			}
			else
			{
				num += 8;
			}
			num2 -= 22;
			SpriteSheet<_sheetSprites>.Draw(200, num, num2, mouseTextColor, se);
		}
		Main.SpriteBatch.End();
		for (int num3 = 7; num3 >= 0; num3--)
		{
			if (MyPlayer != num3)
			{
				Player player = Main.PlayerSet[num3];
				if (player.Active != 0 && !player.IsDead && !player.XYWH.Intersects(CurrentView.ViewArea) && (ActivePlayer.team == 0 || player.team == 0 || ActivePlayer.team == player.team))
				{
					Vector2 GameView = default;
					Vector2 PlayerView = default;
					GameView.X = CurrentView.ViewWidth >> 1;
					GameView.Y = Main.ResolutionHeight / 2;
					PlayerView.X = player.XYWH.X + 10 - CurrentView.ScreenPosition.X;
					PlayerView.Y = player.XYWH.Y + 21 - CurrentView.ScreenPosition.Y;
					Vector2 Intersection = default;
					bool flag = false;
					int safeAreaOffsetLeft = CurrentView.SafeAreaOffsetLeft;
					int num4 = CurrentView.ViewWidth - CurrentView.SafeAreaOffsetRight - 40;
					int num5 = CurrentView.SafeAreaOffsetTop + 20 + 40;
					int num6 = Main.ResolutionHeight - CurrentView.SafeAreaOffsetBottom - 40;
					if (PlayerView.X <= (float)safeAreaOffsetLeft)
					{
						flag = Collision.LineIntersection(ref GameView, ref PlayerView, new Vector2(safeAreaOffsetLeft, num5), new Vector2(safeAreaOffsetLeft, num6), ref Intersection);
					}
					else if (PlayerView.X >= (float)num4)
					{
						flag = Collision.LineIntersection(ref GameView, ref PlayerView, new Vector2(num4, num5), new Vector2(num4, num6), ref Intersection);
					}
					if (!flag)
					{
						if (PlayerView.Y <= (float)num5)
						{
							flag = Collision.LineIntersection(ref GameView, ref PlayerView, new Vector2(safeAreaOffsetLeft, num5), new Vector2(num4, num5), ref Intersection);
						}
						else if (PlayerView.Y >= (float)num6)
						{
							flag = Collision.LineIntersection(ref GameView, ref PlayerView, new Vector2(safeAreaOffsetLeft, num6), new Vector2(num4, num6), ref Intersection);
						}
					}
					float num7 = (float)CurrentView.ViewWidth * 1.5f / (PlayerView - GameView).Length();
					if (num7 < 0.5f)
					{
						num7 = 0.5f;
					}
					Matrix matrix = Matrix.CreateTranslation(-10f, -8f, 0f) * Matrix.CreateScale(num7, num7, 1f);
					Vector2 position = player.Position;
					player.Position.X = CurrentView.ScreenPosition.X;
					player.Position.Y = CurrentView.ScreenPosition.Y;
					CurrentView.ScreenProjection.World = matrix * Matrix.CreateTranslation(Intersection.X, Intersection.Y, 0f);
					Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, CurrentView.ScreenProjection);
					player.Draw(CurrentView, IsMenuScr: true, IsIcon: true);
					Main.SpriteBatch.End();
					player.Position = position;
					player.XYWH.X = (int)position.X;
					player.XYWH.Y = (int)position.Y;
				}
			}
		}
		CurrentView.ScreenProjection.World = Matrix.Identity;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, CurrentView.ScreenProjection);
		if (ActivePlayer.IsDead)
		{
			CloseInventory();
			string s = Lang.InterfaceText[38];
			int x = CurrentView.ViewWidth >> 1;
			int y = Main.ResolutionHeight / 2;
			Color deathAlpha = ActivePlayer.GetDeathAlpha(default);
			DrawStringCC(BigFont, s, x, y, deathAlpha);
			return;
		}
		if (InventoryMode != 0)
		{
			DrawInventoryMenu();
			return;
		}
		CurrentView.SetWorldView();
		Rectangle rectangle = new Rectangle(ActivePlayer.XYWH.X + 10 - 80, ActivePlayer.XYWH.Y + 21 - 64, 160, 128);
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active != 0 && MyPlayer != i && !Main.PlayerSet[i].IsDead)
			{
				Rectangle value = new Rectangle(Main.PlayerSet[i].XYWH.X + -6, Main.PlayerSet[i].XYWH.Y + 42 - 48, 32, 48);
				if (rectangle.Intersects(value))
				{
					Main.PlayerSet[i].DrawInfo(CurrentView);
				}
			}
		}
		if (!ActivePlayer.IsDead)
		{
			for (int num8 = 195; num8 >= 0; num8--)
			{
				CurrentView.drawNpcName[num8] = true;
			}
			ActivePlayer.npcChatBubble = -1;
			int num9 = 10496;
			Rectangle value2 = default;
			Point center = rectangle.Center;
			for (int j = 0; j < 196; j++)
			{
				NPC nPC = Main.NPCSet[j];
				if (nPC.Active == 0)
				{
					continue;
				}
				int type = nPC.Type;
				if (type == 85 && Main.NPCSet[j].AI0 == 0f)
				{
					continue;
				}
				if ((type >= 87 && type <= 92) || (type >= 159 && type <= 164))
				{
					value2.X = nPC.XYWH.X + (nPC.Width >> 1) - 32;
					value2.Y = nPC.XYWH.Y + (nPC.Height >> 1) - 32;
					value2.Width = 64;
					value2.Height = 64;
				}
				else
				{
					int width = SpriteSheet<_sheetSprites>.Source[1088 + type].Width;
					value2.X = nPC.XYWH.X + (nPC.Width >> 1) - (width >> 1);
					value2.Y = nPC.XYWH.Y + nPC.Height - nPC.FrameHeight;
					value2.Width = width;
					value2.Height = nPC.FrameHeight;
				}
				rectangle.Intersects(ref value2, out var result);
				if (result)
				{
					if (nPC.CanTalk() && ActivePlayer.CanInteractWithNPC())
					{
						Point value3 = value2.Center;
						int num10 = Math.Abs(center.X - value3.X);
						if (num10 <= 80)
						{
							int num11 = num10 * num10;
							num10 = Math.Abs(center.Y - value3.Y);
							if (num10 <= 64)
							{
								num11 += num10 * num10;
								if (num11 < num9)
								{
									bool result2;
									if (UsingSmartCursor)
									{
										result2 = true;
									}
									else
									{
										value3.X = MouseX + CurrentView.ScreenPosition.X;
										value3.Y = MouseY + CurrentView.ScreenPosition.Y;
										value2.Contains(ref value3, out result2);
									}
									if (result2)
									{
										ActivePlayer.npcChatBubble = (short)j;
									}
								}
							}
						}
					}
					if (nPC.DrawMyName < 32)
					{
						nPC.DrawMyName = 32;
					}
				}
				if (nPC.DrawMyName > 0 && CurrentView.ClipArea.Intersects(value2))
				{
					nPC.DrawMyName--;
					nPC.DrawInfo(CurrentView);
				}
			}
			if (ActivePlayer.npcChatBubble >= 0)
			{
				NPC nPC2 = Main.NPCSet[ActivePlayer.npcChatBubble];
				int num12 = -((nPC2.Width >> 1) + 8);
				SpriteEffects se2 = SpriteEffects.None;
				if (nPC2.SpriteDirection == -1)
				{
					se2 = SpriteEffects.FlipHorizontally;
					num12 = -num12;
				}
				pos.X = nPC2.XYWH.X + (nPC2.Width >> 1) - CurrentView.ScreenPosition.X - 16 - num12;
				pos.Y = nPC2.XYWH.Y - 26 - CurrentView.ScreenPosition.Y;
				SpriteSheet<_sheetSprites>.Draw(201, ref pos, mouseTextColor, se2);
			}
		}
		CurrentView.SetScreenView();
		if (npcChatText != null || ActivePlayer.sign != -1)
		{
			DrawNpcChat();
		}
	}

	public void ClearInput()
	{
		inputTextEnter = false;
		inputTextCanceled = false;
	}

	public UserString GetInputText(UserString oldString, string title = null, string desc = null, bool validate = true)
	{
		if (!inputTextEnter && oldString.IsEditable())
		{
			if (!Guide.IsVisible)
			{
				try
				{
					kbResult = Guide.BeginShowKeyboardInput(controller, title, desc, oldString.IsCensored ? "" : oldString.UserText, null, null);
				}
				catch (GuideAlreadyVisibleException)
				{
					return oldString;
				}
			}
			if (kbResult != null && kbResult.IsCompleted)
			{
				inputTextEnter = true;
				string text = Guide.EndShowKeyboardInput(kbResult);
				kbResult = null;
				inputTextCanceled = text == null;
				if (!inputTextCanceled)
				{
					text = text.Trim();
					char[] array = text.ToCharArray();
					bool flag = false;
					for (int num = array.Length - 1; num >= 0; num--)
					{
						if (array[num] == '¤')
						{
							array[num] = 'Ʃ';
							flag = true;
						}
						else if (!BoldSmallFont.Characters.Contains(array[num]))
						{
							if (array[num] == '€')
							{
								array[num] = 'Ɛ';
							}
							else
							{
								array[num] = '?';
							}
							flag = true;
						}
					}
					if (flag)
					{
						text = new string(array);
					}
					if (validate)
					{
						oldString.SetUserString(text);
					}
					else
					{
						oldString.SetSystemString(text);
					}
				}
			}
		}
		return oldString;
	}

	public void FirstProgressStep(int numSteps, string text = null)
	{
		Progress = 0f;
		progressTotal = 0f;
		numProgressStepsInv = 1f / (float)numSteps;
		if (text != null)
		{
			statusText = text;
		}
	}

	public void NextProgressStep(string text = null)
	{
		Progress = 0f;
		progressTotal += numProgressStepsInv;
		if (text != null)
		{
			statusText = text;
		}
	}

	private static void UpdateCursorColor()
	{
		cursorAlpha = (float)(0.8 + 0.2 * Math.Sin((double)Main.FrameCounter * 0.0625));
		double num = (double)cursorAlpha * 0.3 + 0.7;
		byte r = (byte)((float)(int)mouseColor.R * cursorAlpha);
		byte g = (byte)((float)(int)mouseColor.G * cursorAlpha);
		byte b = (byte)((float)(int)mouseColor.B * cursorAlpha);
		byte alpha = (byte)(255.0 * num);
		CursorColour = new Color(r, g, b, alpha);
		cursorScale = (float)(num + 0.1);
	}

	/// <summary>
	/// Copies the desktop pointer into the game cursor. Does NOT return early: callers must
	/// still run the normal per-context logic, because in gameplay the player derives its aim
	/// (controlDir, tile targets) from MouseX/MouseY further down the chain.
	/// </summary>
	private void ApplyPointerPosition()
	{
		int num = Terraria.Input.KeyboardMouseInput.MouseX;
		int num2 = Terraria.Input.KeyboardMouseInput.MouseY;
		if (num < 0)
		{
			num = 0;
		}
		else if (num > Main.ResolutionWidth - 1)
		{
			num = Main.ResolutionWidth - 1;
		}
		if (num2 < 0)
		{
			num2 = 0;
		}
		else if (num2 > Main.ResolutionHeight - 1)
		{
			num2 = Main.ResolutionHeight - 1;
		}
		MouseX = (short)num;
		MouseY = (short)num2;
		oldMouseX = MouseX;
		uiDelay = 0;
		uiDelayValue = 12;
	}

	/// <summary>Menu only: moves the console selection to the entry nearest the pointer.</summary>
	private void SelectMenuEntryUnderPointer()
	{
		if (menuHitAny)
		{
			// Real hit-testing: the pointer must be on an entry for the selection to move.
			// Leaving the pointer off the list keeps the last selection instead of jumping
			// to whatever happened to be closest.
			int num = MenuEntryUnderPointer;
			if (num >= 0)
			{
				int num10 = Math.Max(1, (int)uiWidth);
				uiX = (short)(num % num10);
				// Menu entries are the *visible* rows; the cursor is an absolute row so the list
				// can scroll. Without the offset the pointer would point at the wrong character
				// once the list is scrolled.
				uiY = (short)(num / num10 + ((CurMenuMode == MenuMode.CHARACTER_SELECT) ? charScroll : 0));
			}
			return;
		}
		if (uiCoords == null || uiWidth <= 0 || uiHeight <= 0)
		{
			return;
		}
		int num2 = MouseX;
		int num3 = MouseY;
		int num4 = Math.Min(uiWidth * uiHeight, uiCoords.Length);
		int num5 = -1;
		long num6 = long.MaxValue;
		for (int i = 0; i < num4; i++)
		{
			Location location = uiCoords[i];
			if (location.X == 0 && location.Y == 0)
			{
				continue;
			}
			long num7 = location.X - num2;
			long num8 = location.Y - num3;
			long num9 = num7 * num7 + num8 * num8;
			if (num9 < num6)
			{
				num6 = num9;
				num5 = i;
			}
		}
		if (num5 >= 0 && num6 < 160 * 160)
		{
			uiX = (short)(num5 % uiWidth);
			uiY = (short)(num5 / uiWidth);
		}
	}

	/// <summary>True while the pointer was the active device on the previous frame.</summary>
	private bool mouseWasActive;

	/// <summary>
	/// PC parity: the slot under the pointer becomes the active slot, instead of having to
	/// walk a console cursor there with the D-Pad. Returns true when the pointer is over
	/// something this method handled.
	///
	/// <paramref name="OverGrid"/> says whether that something was an item slot. A click on a tab
	/// must not also act on the slot that happened to be highlighted, which is exactly what
	/// "clicking the Equipment tab moved an item" was.
	/// </summary>
	/// <summary>
	/// Finds the equipment slot under the pointer and moves the section cursor onto it. The
	/// rectangles were recorded by <see cref="DrawEquip"/>, so this cannot disagree with what is
	/// on screen - the lesson from the inventory grid, where draw and hit-test used two different
	/// formulas.
	/// </summary>
	private bool TryEquipSlotUnderPointer()
	{
		for (int i = 0; i < equipSlotRects.Length; i++)
		{
			if (equipSlotValid[i] && equipSlotRects[i].Contains(MouseX, MouseY))
			{
				inventoryEquipX = equipSlotX[i];
				inventoryEquipY = equipSlotY[i];
				return true;
			}
		}
		return false;
	}

	/// <summary>Same for the housing list.</summary>
	private bool TryHousingSlotUnderPointer()
	{
		for (int i = 0; i < housingSlotRects.Length; i++)
		{
			if (housingSlotValid[i] && housingSlotRects[i].Contains(MouseX, MouseY))
			{
				inventoryHousingX = housingSlotX[i];
				inventoryHousingY = housingSlotY[i];
				return true;
			}
		}
		return false;
	}

	/// <summary>And the chest / bank / safe grid.</summary>
	private bool TryChestSlotUnderPointer()
	{
		for (int i = 0; i < chestSlotRects.Length; i++)
		{
			if (chestSlotValid[i] && chestSlotRects[i].Contains(MouseX, MouseY))
			{
				inventoryChestX = chestSlotX[i];
				inventoryChestY = chestSlotY[i];
				return true;
			}
		}
		return false;
	}

	/// <summary>And the loot all / deposit / quick stack column of the chest screen.</summary>
	private bool TryChestButtonUnderPointer()
	{
		for (int i = 1; i < chestButtonRects.Length; i++)
		{
			if (chestButtonValid[i] && chestButtonRects[i].Contains(MouseX, MouseY))
			{
				// The cursor value -1 is what tells UpdateStorage "this is a button, not a slot",
				// and the row number picks which of the three.
				inventoryChestX = -1;
				inventoryChestY = (sbyte)i;
				return true;
			}
		}
		return false;
	}

	/// <summary>And the shop grid, which uses the same cursor variables as a chest.</summary>
	private bool TryShopSlotUnderPointer()
	{
		for (int i = 0; i < shopSlotRects.Length; i++)
		{
			if (shopSlotValid[i] && shopSlotRects[i].Contains(MouseX, MouseY))
			{
				inventoryChestX = (sbyte)(i % 5);
				inventoryChestY = (sbyte)(i / 5);
				return true;
			}
		}
		return false;
	}

	private bool UpdateInventoryMouseHover(out bool OverGrid)
	{
		OverGrid = false;
		// Tabs first: they sit above the grid and are valid in every section.
		InventorySection inventorySection;
		if (TryInventorySectionUnderPointer(out inventorySection))
		{
			return true;
		}
		// Equipment, housing and chest have their own grids and their own cursor variables, and
		// each records its rectangles in its own draw call. They must NOT be gated on the 10x4
		// items layout below: that grid is not even drawn on those tabs, so gating them on it
		// made the whole section dead to the pointer until the player happened to open the items
		// tab first - which is why a chest opened straight from the world ignored the mouse.
		if (ActiveInvSection == InventorySection.EQUIP && TryEquipSlotUnderPointer())
		{
			OverGrid = true;
			return true;
		}
		if (ActiveInvSection == InventorySection.HOUSING && TryHousingSlotUnderPointer())
		{
			OverGrid = true;
			return true;
		}
		if (ActiveInvSection == InventorySection.CHEST)
		{
			// Chest, bank, safe and shop share this tab; which grid is on screen decides which
			// rectangles were recorded.
			if (npcShop > 0)
			{
				if (TryShopSlotUnderPointer())
				{
					OverGrid = true;
					return true;
				}
			}
			else
			{
				// Loot all / deposit / quick stack are drawn in their own column to the left of
				// the chest grid. They were reachable only with the stick, so the three buttons
				// every chest screen shows were the part of it the mouse could not press.
				if (TryChestButtonUnderPointer())
				{
					OverGrid = true;
					return true;
				}
				if (TryChestSlotUnderPointer())
				{
					OverGrid = true;
					return true;
				}
			}
			return false;
		}
		if (ActiveInvSection != InventorySection.ITEMS)
		{
			return false;
		}
		if (!invMouseLayoutValid || invMouseCell <= 0f)
		{
			return false;
		}
		// The secondary slots first: they sit outside the 10x4 grid, so a check against the grid
		// cannot accidentally claim them, and before the grid so a point that is somehow in both
		// resolves to the more specific one.
		if (invMouseTrashValid && invMouseTrash.Contains(MouseX, MouseY))
		{
			inventoryItemX = 9;
			inventoryItemY = 6;
			OverGrid = true;
			return true;
		}
		if (invMouseCoinValid && invMouseCoin.Contains(MouseX, MouseY))
		{
			int num5 = InventoryGrid.CellIndex(MouseX - invMouseCoin.X, invMouseCell, 4);
			if (num5 >= 0)
			{
				inventoryItemX = (sbyte)(6 + num5);
				inventoryItemY = 5;
				OverGrid = true;
				return true;
			}
		}
		if (invMouseAmmoValid && invMouseAmmo.Contains(MouseX, MouseY))
		{
			int num6 = InventoryGrid.CellIndex(MouseX - invMouseAmmo.X, invMouseCell, 4);
			if (num6 >= 0)
			{
				inventoryItemX = (sbyte)(6 + num6);
				inventoryItemY = 4;
				OverGrid = true;
				return true;
			}
		}
		int column;
		int row;
		if (!InventoryGrid.TryCell(MouseX, MouseY, invMouseGrid.X, invMouseGrid.Y, invMouseCell, 10, 4, out column, out row))
		{
			return false;
		}
		inventoryItemX = (sbyte)column;
		inventoryItemY = (sbyte)row;
		OverGrid = true;
		return true;
	}

	/// <summary>
	/// True when the pointer is over a tab, which is then made the active section. Unavailable
	/// tabs (no chest open, no crafting stations) are ignored rather than selected, so a click
	/// cannot land the player in a section the console build would never have shown.
	///
	/// Hover only highlights; the switch needs a click. It used to switch on hover, and the
	/// caller then still ran the slot action on the same click - so clicking a tab changed the
	/// section and moved an item from whatever slot was highlighted before.
	/// </summary>
	private bool TryInventorySectionUnderPointer(out InventorySection section)
	{
		section = ActiveInvSection;
		for (int i = 0; i < inventorySectionRects.Length; i++)
		{
			if (!inventorySectionRectValid[i] || !inventorySectionRects[i].Contains(MouseX, MouseY))
			{
				continue;
			}
			InventorySection inventorySection = (InventorySection)i;
			if (!IsInventorySectionAvailable(inventorySection))
			{
				// The pointer is on a tab, it just cannot be used: still "handled", so the click
				// does not fall through to the item grid underneath.
				return true;
			}
			section = inventorySection;
			if (ActiveInvSection != inventorySection && Terraria.Input.KeyboardMouseInput.LeftClicked)
			{
				ActiveInvSection = inventorySection;
				Main.PlaySound(12);
				toolTip.Init();
				if (inventorySection == InventorySection.CRAFTING)
				{
					Recipe.FindRecipes(this, craftingCategory, craftingShowCraftable);
				}
			}
			return true;
		}
		return false;
	}

	private void UpdateMouse()
	{
		inventoryMenuDone = false;
		inventoryAction.Reset();
		// The pointer must be tracked on EVERY screen, including ones without menu entries
		// (map, inventory): otherwise MouseX/MouseY stay at their last value and the rendered
		// cursor freezes in place while the real pointer moves. That was the "stuck cursor".
		bool flag2 = Terraria.Input.KeyboardMouseInput.Enabled
			&& Terraria.Input.KeyboardMouseInput.CursorOverride
			&& NumActiveViews <= 1;
		if (!flag2)
		{
			mouseWasActive = false;
		}
		if (flag2)
		{
			// Switching to the mouse means "I aim at what I point at". Smart cursor is the
			// console convenience that picks a target for you and would fight the pointer.
			if (!mouseWasActive)
			{
				UsingSmartCursor = false;
			}
			mouseWasActive = true;
			ApplyPointerPosition();
			if (InventoryMode > 0 && !Terraria.Input.VirtualKeyboard.IsOpen)
			{
				// Order matters: the section/stack bookkeeping runs first, then the hover
				// moves the slot cursor, and only then is the click applied - otherwise a
				// click would act on whatever slot was selected before the pointer moved.
				UpdateInventoryMenuOnce();
				bool overGrid;
				// A click on a slot is this screen's own business. It cannot be routed through
				// `Buttons.A`, because that button is only synthesised when the pointer is NOT
				// handled by the screen - and the inventory is such a screen. That is why every
				// slot action was dead to the mouse while the tabs, which read the pointer
				// directly, still worked.
				pointerActivate = Terraria.Input.KeyboardMouseInput.LeftClicked;
				if (pointerActivate)
				{
					// A click belongs to the screen, even when it lands on empty space. Taking the
					// gate keeps the stick path below from acting on the slot the cursor happened
					// to be sitting on: with no synthetic A involved, "click the panel" used to
					// move an item from wherever the cursor was left.
					inventoryAction.TryBegin();
				}
				if (ActiveInvSection == InventorySection.CRAFTING)
				{
					// The recipe grid has its own hit-test: rows are subcategories and each row
					// scrolls on its own, so it cannot go through the inventory's grid mapping.
					UpdateCraftingPointer();
				}
				else if (UpdateInventoryMouseHover(out overGrid) && overGrid)
				{
					inventoryAction.TryBegin();
					// Each section has its own action handler, and the pointer has to call the
					// one that belongs to the section it is pointing at. Only ITEMS was wired up,
					// so equipment, housing and crafting were unreachable by mouse even with the
					// cursor moved onto the right slot.
					switch (ActiveInvSection)
					{
					case InventorySection.EQUIP:
						UpdateEquip();
						break;
					case InventorySection.HOUSING:
						UpdateHousing();
						break;
					case InventorySection.ITEMS:
						UpdateInventory();
						break;
					case InventorySection.CHEST:
						if (npcShop > 0)
						{
							UpdateShop();
						}
						else
						{
							UpdateStorage();
						}
						break;
					default:
						// Crafting keeps its own input path: the recipe grid is clipped and
						// scrolled, so pointing at it needs its own mapping. Listed in BUGS.md.
						break;
					}
				}
				pointerActivate = false;
			}
		}
		if (uiCoords == null)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 3;
			bool flag = InventoryMode > 0 || CurMenuMode == MenuMode.MAP || CurMenuMode == MenuMode.WORLD_SELECT || CurMenuMode == MenuMode.GAME_MODE;
			if (flag)
			{
				num3 = 1;
			}
			else
			{
				if (CurMenuType == MenuType.NONE)
				{
					if (IsButtonTriggered(Buttons.RightStick))
					{
						UsingSmartCursor = !UsingSmartCursor;
					}
					if (UsingSmartCursor)
					{
						ActivePlayer.UpdateMouseSmart();
					}
					else
					{
						ActivePlayer.UpdateMouse();
					}
					return;
				}
				uiDelay = 0;
				uiDelayValue = 12;
			}
			if (CurMenuMode != MenuMode.MAP)
			{
				if (PadState.ThumbSticks.Right.X < -0.125f)
				{
					num = -num3;
				}
				else if (PadState.ThumbSticks.Right.X > 0.125f)
				{
					num = num3;
				}
				if (PadState.ThumbSticks.Right.Y < -0.125f)
				{
					num2 = num3;
				}
				else if (PadState.ThumbSticks.Right.Y > 0.125f)
				{
					num2 = -num3;
				}
			}
			if (CurMenuType != MenuType.NONE || InventoryMode > 0)
			{
				if (PadState.ThumbSticks.Left.X < -0.125f)
				{
					num = -num3;
				}
				else if (PadState.ThumbSticks.Left.X > 0.125f)
				{
					num = num3;
				}
				if (PadState.ThumbSticks.Left.Y < -0.125f)
				{
					num2 = num3;
				}
				else if (PadState.ThumbSticks.Left.Y > 0.125f)
				{
					num2 = -num3;
				}
				if (InventoryMode == 0 || ActiveInvSection != InventorySection.ITEMS)
				{
					if (PadState.DPad.Left == ButtonState.Pressed)
					{
						num = -num3;
					}
					else if (PadState.DPad.Right == ButtonState.Pressed)
					{
						num = num3;
					}
					if (PadState.DPad.Down == ButtonState.Pressed)
					{
						num2 = num3;
					}
					else if (PadState.DPad.Up == ButtonState.Pressed)
					{
						num2 = -num3;
					}
				}
			}
			if (uiDelay > 0)
			{
				if (num == 0 && num2 == 0)
				{
					uiDelay = 0;
					uiDelayValue = 12;
				}
				else
				{
					uiDelay--;
				}
			}
			if (uiDelay != 0)
			{
				return;
			}
			if (flag)
			{
				if (num != 0 || num2 != 0)
				{
					uiDelay = uiDelayValue;
					uiDelayValue = 6;
				}
				if (CurMenuMode == MenuMode.MAP)
				{
					PositionMapScreenCursor(num, num2);
					return;
				}
				if (CurMenuMode == MenuMode.WORLD_SELECT)
				{
					WorldSelect.UpdateCursor(num, num2);
					return;
				}
				if (CurMenuMode == MenuMode.GAME_MODE)
				{
					GameMode.UpdateCursor(num, num2);
					return;
				}
				UpdateInventoryMenuOnce();
				if (ActiveInvSection == InventorySection.CRAFTING)
				{
					PositionCraftingCursor(num, num2);
				}
				else
				{
					PositionInventoryCursor(num, num2);
				}
			}
			else if (num != 0 || num2 != 0)
			{
				uiDelay = 4;
				MouseX += (short)num;
				if (MouseX < 0)
				{
					MouseX = 0;
				}
				else if (MouseX > Main.ResolutionWidth - 16)
				{
					MouseX = (short)(Main.ResolutionWidth - 16);
				}
				MouseY += (short)num2;
				if (MouseY < 0)
				{
					MouseY = 0;
				}
				else if (MouseY > Main.ResolutionHeight - 16)
				{
					MouseY = (short)(Main.ResolutionHeight - 16);
				}
			}
			return;
		}
		if (flag2)
		{
			// Menu with the pointer in charge: never let the stick path move the cursor,
			// otherwise it snaps onto menu entries and the mouse appears to stick to them.
			SelectMenuEntryUnderPointer();
			return;
		}
		if (uiDelay > 0)
		{
			if (PadState.ThumbSticks.Left.LengthSquared() <= 1f / 64f && PadState.ThumbSticks.Right.LengthSquared() <= 1f / 64f && PadState.DPad.Up == ButtonState.Released && PadState.DPad.Down == ButtonState.Released && PadState.DPad.Left == ButtonState.Released && PadState.DPad.Right == ButtonState.Released)
			{
				uiDelay = 0;
				uiDelayValue = 12;
			}
			else
			{
				uiDelay--;
			}
			return;
		}
		int num4 = (IsLeftButtonDown() ? (-1) : (IsRightButtonDown() ? 1 : 0));
		if (num4 < 0)
		{
			if (--uiX < 0)
			{
				uiX = (short)(uiWidth - 1);
			}
			uiDelay = uiDelayValue;
			uiDelayValue = 6;
		}
		else if (num4 > 0)
		{
			if (++uiX == uiWidth)
			{
				uiX = 0;
			}
			uiDelay = uiDelayValue;
			uiDelayValue = 6;
		}
		num4 = (IsDownButtonDown() ? (-1) : (IsUpButtonDown() ? 1 : 0));
		while (true)
		{
			if (num4 > 0)
			{
				if (--uiY < 0)
				{
					uiY = (short)(uiHeight - 1);
				}
				uiDelay = uiDelayValue;
				uiDelayValue = 6;
			}
			else if (num4 < 0)
			{
				if (++uiY == uiHeight)
				{
					uiY = 0;
				}
				uiDelay = uiDelayValue;
				uiDelayValue = 6;
			}
			MouseX = uiCoords[uiX + uiY * uiWidth].X;
			if (MouseX != 0)
			{
				break;
			}
			if (num4 == 0 && ++uiY >= uiHeight)
			{
				uiY = 0;
			}
		}
		MouseY = uiCoords[uiX + uiY * uiWidth].Y;
		MouseY++;
	}

	public static void UpdateOnce()
	{
		UpdateCursorColor();
		MouseTextBrightness = (byte)(MouseTextBrightness + mouseTextColorChange);
		if (MouseTextBrightness <= 175 || MouseTextBrightness >= 250)
		{
			mouseTextColorChange = (sbyte)(-mouseTextColorChange);
		}
		mouseTextColor.R = MouseTextBrightness;
		mouseTextColor.G = MouseTextBrightness;
		mouseTextColor.B = MouseTextBrightness;
		mouseTextColor.A = MouseTextBrightness;
		invAlpha = (byte)(invAlpha + invDir);
		if (invAlpha > 240)
		{
			invAlpha = 240;
			invDir = (sbyte)(-invDir);
		}
		else if (invAlpha < 180)
		{
			invAlpha = 180;
			invDir = (sbyte)(-invDir);
		}
		PulseScale += essDir;
		if (PulseScale > 1f)
		{
			PulseScale = 1f;
			essDir = 0f - essDir;
		}
		else if (PulseScale < 0.7f)
		{
			PulseScale = 0.7f;
			essDir = 0f - essDir;
		}
		BlueWave += blueDelta;
		if (BlueWave > 1f)
		{
			BlueWave = 1f;
			blueDelta = 0f - blueDelta;
		}
		else if (BlueWave < 0.97f)
		{
			BlueWave = 0.97f;
			blueDelta = 0f - blueDelta;
		}
		if (MessageBox.CurrentMess.AutoUpdate)
		{
			MessageBox.Update();
		}
	}

	public void UpdateGamePad()
	{
		if (Main.HasFocus)
		{
			gpPrevState = PadState;
			GamePadState gamePadState = GamePad.GetState(controller);
			if (controller == PlayerIndex.One)
			{
				// Context flags are derived inside KeyboardMouseInput.Update(), before any UI
				// runs; merging here only needs the device state.
				gamePadState = Terraria.Input.KeyboardMouseInput.Merge(gamePadState);
			}
			PadState = gamePadState;
		}
		else
		{
			gpPrevState = (PadState = (PadState = default));
		}
	}

	public void Update()
	{
		if (MainUI.CurMenuMode == MenuMode.WELCOME)
		{
			if (IsSelectButtonTriggered())
			{
				ClearButtonTriggers();
				OpenMainView();
			}
		}
		else if (CurrentView == null)
		{
			if (Main.IsGameStarted && IsSelectButtonTriggered() && !Main.IsTutorial() && (Netplay.Session == null || Netplay.Session.AllGamers.Count != 8))
			{
				ClearButtonTriggers();
				SetMenu(MenuMode.CHARACTER_SELECT, rememberPrevious: false, reset: true);
				OpenView();
			}
			return;
		}
		CurrentUI = this;
		// The on-screen keyboard owns input while it is open. It is drawn over whatever screen
		// opened it, and the two overlap: a click on a key also landed on the settings row behind
		// it, so typing re-opened the prompt and reset the text. Nothing else may read the mouse
		// or the pad until the keyboard closes.
		if (Terraria.Input.VirtualKeyboard.IsOpen)
		{
			// The pointer is still tracked, or the drawn cursor stays where it was.
			UpdateMouse();
			return;
		}
		// Keyboard users need an explicit way out of the inventory: the console build only
		// exposes "close" through Buttons.B/Y, and neither is reliably reachable from a
		// keyboard once the inventory is open. Escape and Tab (map) close it here.
		if (this == MainUI && InventoryMode > 0 && !MessageBox.IsVisible()
			&& (Terraria.Input.KeyboardMouseInput.KeyPressed(Keys.Escape) || Terraria.Input.KeyboardMouseInput.KeyPressed(Keys.Tab)))
		{
			Main.PlaySound(11);
			CloseInventory();
			return;
		}
		// E / I toggle the inventory, in both directions. The console build opens on Y *release*
		// and closes on B, so on a keyboard the same key could only ever open: closing left the
		// player hunting for Escape. One key, one action.
		if (this == MainUI && !MessageBox.IsVisible() && Terraria.Input.KeyboardMouseInput.ConsumeInventoryToggle())
		{
			if (InventoryMode > 0)
			{
				Main.PlaySound(11);
				CloseInventory();
				return;
			}
			if (CurMenuType == MenuType.NONE && CurrentView != null && !ActivePlayer.ghost)
			{
				Main.PlaySound(10);
				OpenInventory();
				return;
			}
		}
		if (CurMenuType == MenuType.NONE)
		{
			if (this == MainUI && Terraria.Cheats.CheatState.IsAvailable && Terraria.Input.KeyboardMouseInput.KeyPressed(Keys.F1))
			{
				// Keyboard shortcut straight into the single-player cheat menu.
				Main.PlaySound(10);
				CurMenuType = MenuType.PAUSE;
				cheatsFromPlay = true;
				SetMenu(MenuMode.CHEATS);
				ClearButtonTriggers();
				return;
			}
			if (IsButtonUntriggered(Buttons.Start))
			{
				Main.PlaySound(10);
				uiFade = 0f;
				uiFadeTarget = 1f;
				CurMenuType = MenuType.PAUSE;
				SetMenu(MenuMode.PAUSE);
				ClearButtonTriggers();
			}
			else if (IsButtonTriggered(Buttons.Back))
			{
				miniMap.CreateMap(this);
				Main.PlaySound(10);
				if (Main.NetMode == 1)
				{
					NetMessage.CreateMessage0(11);
					NetMessage.SendMessage();
				}
				SetMenu(MenuMode.MAP);
				CurMenuType = MenuType.PAUSE;
				ClearButtonTriggers();
			}
		}
		else
		{
			if (transferredPlayerStorage.Count > 0 && !MessageBox.IsVisible())
			{
				DeleteTransferredPlayerStorage();
			}
			if (saveIconMessageTime <= 0)
			{
				UpdateMenu();
			}
		}
		if (CurMenuType != MenuType.MAIN)
		{
			UpdateIngame();
		}
		if (teamCooldown > 0 && --teamCooldown == 0 && teamSelected != ActivePlayer.team)
		{
			ActivePlayer.team = teamSelected;
			NetMessage.CreateMessage1(45, MyPlayer);
			NetMessage.SendMessage();
		}
		if (pvpCooldown > 0 && --pvpCooldown == 0 && pvpSelected != ActivePlayer.hostile)
		{
			ActivePlayer.hostile = pvpSelected;
			NetMessage.CreateMessage1(30, MyPlayer);
			NetMessage.SendMessage();
		}
		UpdateMouse();
		// Number keys 1..0 pick a hotbar slot directly, as on the PC version.
		if (this == MainUI && !Terraria.Input.KeyboardMouseInput.MenuContext)
		{
			int num = Terraria.Input.KeyboardMouseInput.ConsumeHotbarRequest();
			// Select the slot even when it is empty, exactly like the PC version: the slot
			// highlight has to move, otherwise the keys look like they do nothing.
			if (num >= 0 && num < 10)
			{
				ActivePlayer.SelectedItem = (sbyte)num;
				Main.PlaySound(12);
			}
			// Wheel scrolls the hotbar, the way it does on the PC version. It used to be
			// mapped onto the shoulder buttons, which meant the wheel and "switch inventory
			// section" shared one action.
			//
			// Direction matches the PC build: scrolling down selects the next slot to the right.
			// The PC code does `selectedItem -= scrollDelta / 120`, and a wheel-down gives a
			// negative delta, so the sign is inverted on purpose - this is not a guess, it is that
			// line (Player.cs in refs/T12 and T111M).
			int wheel = Terraria.Input.KeyboardMouseInput.ConsumeWheelSteps();
			if (wheel != 0)
			{
				sbyte selected = ActivePlayer.SelectedItem;
				ActivePlayer.SelectedItem = (sbyte)Terraria.Input.WheelNav.Wrap(selected, -wheel, 10);
				if (ActivePlayer.SelectedItem != selected)
				{
					Main.PlaySound(12);
				}
			}
			switch (Terraria.Input.KeyboardMouseInput.ConsumeQuickAction())
			{
			case Terraria.Input.QuickAction.Heal:
				ActivePlayer.QuickHeal();
				break;
			case Terraria.Input.QuickAction.Mana:
				ActivePlayer.QuickMana();
				break;
			case Terraria.Input.QuickAction.Buff:
				ActivePlayer.QuickBuff();
				break;
			}
		}
		if (SignedInGamer != null && !SignedInGamer.IsGuest && !Main.IsTrial)
		{
			UpdateAchievements();
		}
	}

	private void UpdateIngame()
	{
		if (editSign)
		{
			ActivePlayer.UpdateEditSign();
		}
		if (autoSave && Main.TutorialState == Tutorial.NUM_TUTORIALS && HasPlayerStorage())
		{
			if (!saveTime.IsRunning)
			{
				saveTime.Start();
			}
			else if (saveTime.ElapsedMilliseconds > 600000)
			{
				saveTime.Reset();
				if (Main.NetMode == 1 || MainUI != this)
				{
					WorldGen.savePlayerWhilePlaying();
				}
				else
				{
					WorldGen.saveAllWhilePlaying();
				}
			}
		}
		else if (saveTime.IsRunning)
		{
			saveTime.Stop();
		}
		CurrentView.itemTextLocal.Update();
		CurrentView.dustLocal.UpdateDust();
		CurrentView.spawnSnow();
	}

	private void OpenMainView(SignedInGamer gamer = null)
	{
		if (MainUI != this)
		{
			CurrentView = MainUI.CurrentView;
			CurrentView.Ui = this;
			CurrentView.Player = ActivePlayer;
			MainUI.CurrentView = null;
			MainUI = this;
			Main.MusicVolume = MusicVolume;
			Main.SoundVolume = SoundVolume;
		}
		SignedInGamer = gamer;
		SetMenu(MenuMode.TITLE);
		if (gamer == null)
		{
			SignedInGamer = new SignedInGamer(Main.Gamertag);
			InitPlayerStorage();
		}
		else
		{
			InitPlayerStorage();
		}
	}

	private void OpenView()
	{
		CheckHDTV();
		MessageBox.Update();
		for (int i = 0; i < 4; i++)
		{
			UI uI = Main.UIInstance[i];
			if (uI.CurrentView != null)
			{
				uI.SetUIView(WorldView.getViewType(uI.controller, this), NoAutoFullScreen: true);
			}
		}
		SetUIView(WorldView.getViewType(controller, this), NoAutoFullScreen: true);
		ShowSignInPortal();
	}

	public void ClearButtonTriggers()
	{
		gpPrevState = PadState;
	}

	public bool IsBackButtonTriggered()
	{
		Buttons button = (Main.TouchpadButton ? Buttons.Start : Buttons.Back);
		if (!PadState.IsButtonDown(button) || !gpPrevState.IsButtonUp(button))
		{
			if (PadState.IsButtonDown(Buttons.B))
			{
				return gpPrevState.IsButtonUp(Buttons.B);
			}
			return false;
		}
		return true;
	}

	public bool IsSelectButtonTriggered()
	{
		Buttons button = (Main.TouchpadButton ? Buttons.TouchPadEXT : Buttons.Start);
		if (!PadState.IsButtonDown(button) || !gpPrevState.IsButtonUp(button))
		{
			if (PadState.IsButtonDown(Buttons.A))
			{
				return gpPrevState.IsButtonUp(Buttons.A);
			}
			return false;
		}
		return true;
	}

	public bool IsLeftButtonDown()
	{
		if (!PadState.IsButtonDown(Buttons.DPadLeft) && !(PadState.ThumbSticks.Left.X < -0.125f))
		{
			return PadState.ThumbSticks.Right.X < -0.125f;
		}
		return true;
	}

	public bool IsRightButtonDown()
	{
		if (!PadState.IsButtonDown(Buttons.DPadRight) && !(PadState.ThumbSticks.Left.X > 0.125f))
		{
			return PadState.ThumbSticks.Right.X > 0.125f;
		}
		return true;
	}

	public bool IsUpButtonDown()
	{
		if (!PadState.IsButtonDown(Buttons.DPadUp) && !(PadState.ThumbSticks.Left.Y > 0.125f))
		{
			return PadState.ThumbSticks.Right.Y > 0.125f;
		}
		return true;
	}

	public bool IsDownButtonDown()
	{
		if (!PadState.IsButtonDown(Buttons.DPadDown) && !(PadState.ThumbSticks.Left.Y < -0.125f))
		{
			return PadState.ThumbSticks.Right.Y < -0.125f;
		}
		return true;
	}

	public bool IsAlternateLeftButtonDown()
	{
		return PadState.ThumbSticks.Right.X < -0.125f;
	}

	public bool IsAlternateRightButtonDown()
	{
		return PadState.ThumbSticks.Right.X > 0.125f;
	}

	public bool IsAlternateUpButtonDown()
	{
		return PadState.ThumbSticks.Right.Y > 0.125f;
	}

	public bool IsAlternateDownButtonDown()
	{
		return PadState.ThumbSticks.Right.Y < -0.125f;
	}

	public bool IsLeftButtonTriggered()
	{
		if (!PadState.IsButtonDown(Buttons.DPadLeft) || !gpPrevState.IsButtonUp(Buttons.DPadLeft))
		{
			if (PadState.ThumbSticks.Left.X < -0.125f)
			{
				return gpPrevState.ThumbSticks.Left.X >= -0.125f;
			}
			return false;
		}
		return true;
	}

	public bool IsRightButtonTriggered()
	{
		if (!PadState.IsButtonDown(Buttons.DPadRight) || !gpPrevState.IsButtonUp(Buttons.DPadRight))
		{
			if (PadState.ThumbSticks.Left.X > 0.125f)
			{
				return gpPrevState.ThumbSticks.Left.X <= 0.125f;
			}
			return false;
		}
		return true;
	}

	public bool IsButtonDown(Buttons b)
	{
		b = TouchpadSwap(b);
		return PadState.IsButtonDown(b);
	}

	public bool IsButtonTriggered(Buttons b)
	{
		b = TouchpadSwap(b);
		if (PadState.IsButtonDown(b))
		{
			return gpPrevState.IsButtonUp(b);
		}
		return false;
	}

	public bool IsButtonUntriggered(Buttons b)
	{
		b = TouchpadSwap(b);
		if (PadState.IsButtonUp(b))
		{
			return gpPrevState.IsButtonDown(b);
		}
		return false;
	}

	private Buttons TouchpadSwap(Buttons b)
	{
		if (Main.TouchpadButton)
		{
			switch (b)
			{
			case Buttons.Start:
				b = Buttons.TouchPadEXT;
				break;
			case Buttons.Back:
				b = Buttons.Start;
				break;
			}
		}
		return b;
	}

	/// <summary>
	/// Draws the pointer while a menu is open. The in-game cursor is skipped whenever
	/// <see cref="MenuType"/> is not NONE, and the OS cursor is hidden by FNA, so without
	/// this the menus had no visible pointer at all when driven by the mouse.
	/// Only shown while the mouse is actually in use, so gamepad players never see it.
	/// </summary>
	private void DrawMenuCursor()
	{
		if (!Terraria.Input.KeyboardMouseInput.Enabled || !Terraria.Input.KeyboardMouseInput.CursorOverride)
		{
			return;
		}
		if (cursorTexture == null)
		{
			return;
		}
		// Frame (0,0) is the plain arrow; the second column is the "pressed" variant.
		Rectangle value = new Rectangle(0, 0, 16, 16);
		Vector2 position = new Vector2(MouseX, MouseY);
		float scale = 1f;
		// A single 16x16 frame scaled to 2x at 1080p keeps the pointer readable.
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			scale = 2f;
		}
		else if (Main.ScreenHeightPtr == ScreenHeights.HD)
		{
			scale = 1.25f;
		}
		// The shipped cursor is a 2x2 grid of 16x16 frames; column 1 is the "held" state.
		// Guard on the real texture size so a single-frame replacement still works.
		int pressedFrameX = (cursorTexture.Width >= 32) ? 16 : 0;
		if (Terraria.Input.KeyboardMouseInput.LeftPressed)
		{
			value.X = pressedFrameX;
		}
		Main.SpriteBatch.Draw(cursorTexture, position, value, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}

	private void DrawCursor()
	{
		if (CurMenuType != MenuType.NONE || InventoryMode != 0 || npcChatText != null || ActivePlayer.IsDead)
		{
			return;
		}
		bool flag = ActivePlayer.Inventory[ActivePlayer.SelectedItem].PickPower > 0 || ActivePlayer.Inventory[ActivePlayer.SelectedItem].AxePower > 0 || ActivePlayer.Inventory[ActivePlayer.SelectedItem].HammerPower > 0 || ActivePlayer.Inventory[ActivePlayer.SelectedItem].CreateTile >= 0 || ActivePlayer.Inventory[ActivePlayer.SelectedItem].CreateWall >= 0;
		if (cursorHighlight > 0)
		{
			int num = (16 - (CurrentView.ScreenPosition.X & 0xF)) & 0xF;
			int x = ((MouseX - num) & -16) + num;
			num = (16 - (CurrentView.ScreenPosition.Y & 0xF)) & 0xF;
			int y = ((MouseY - num) & -16) + num;
			Main.DrawRect(new Rectangle(x, y, 16, 16), new Color(cursorHighlight << 1, cursorHighlight << 1, 0, cursorHighlight << 1));
		}
		if (flag && ActivePlayer.velocity.X == 0f && ActivePlayer.velocity.Y == 0f)
		{
			if (cursorHighlight < 64)
			{
				cursorHighlight += 4;
			}
		}
		else if (cursorHighlight > 0)
		{
			cursorHighlight -= 2;
		}
		Rectangle value = new Rectangle
		{
			Y = (int)(Main.FrameCounter & 0x10),
			Width = 16,
			Height = 16
		};
		Vector2 position = default;
		if (!UsingSmartCursor)
		{
			value.X = 16;
			position.X = MouseX - 8;
			position.Y = MouseY - 8;
		}
		else
		{
			if (ActivePlayer.controlDir.LengthSquared() <= 576f)
			{
				return;
			}
			position.X = ActivePlayer.XYWH.X + 10 - CurrentView.ScreenPosition.X + (int)ActivePlayer.controlDir.X - 8;
			position.Y = ActivePlayer.XYWH.Y + 21 - CurrentView.ScreenPosition.Y + (int)ActivePlayer.controlDir.Y - 8;
		}
		Main.SpriteBatch.Draw(cursorTexture, position, value, Color.White);
	}

	public void DrawInventoryCursor(int x, int y, double scale, int alpha = 255)
	{
		// This sprite is the SELECTION marker (the highlighted slot / menu row), not a mouse
		// pointer: it must keep drawing even when the mouse drives the selection, otherwise
		// the inventory loses its highlight entirely.
		//
		// It used to also write MouseX/MouseY, which dragged the real pointer back onto the
		// marker every frame - the "sticky cursor". That write is what must not happen while
		// the mouse is the active device.
		alpha = MouseTextBrightness * alpha >> 8;
		SpriteSheet<_sheetSprites>.DrawTL(448, x, y, new Color(alpha, alpha, alpha, alpha), (float)scale);
		if (!PointerActive)
		{
			MouseX = (short)x;
			MouseY = (short)y;
		}
	}

	public static bool IsStorageEnabledForAnyPlayer()
	{
		if (Main.IsTrial)
		{
			return false;
		}
		for (int i = 0; i < 4; i++)
		{
			if (Main.UIInstance[i].HasPlayerStorage())
			{
				return true;
			}
		}
		return false;
	}

	public void CheckPlayerStorage(string name)
	{
		Main.ShowSaveIcon();
		Main.HideSaveIcon();
	}

	public void DeleteTransferredPlayerStorage()
	{
		for (int num = transferredPlayerStorage.Count - 1; num >= 0; num--)
		{
			playerStorage.Device.DeleteContainer(transferredPlayerStorage[num]);
		}
		transferredPlayerStorage.Clear();
		DeviceSelected(null, null);
	}

	public StorageContainer OpenPlayerStorage(string name)
	{
		IAsyncResult asyncResult = playerStorage.Device.BeginOpenContainer(name, null, null);
		asyncResult.AsyncWaitHandle.WaitOne();
		StorageContainer result = playerStorage.Device.EndOpenContainer(asyncResult);
		asyncResult.AsyncWaitHandle.Close();
		return result;
	}

	private void DeviceDisconnected(object sender, EventArgs args)
	{
		LoadPlayers();
		WorldSelect.LoadWorlds();
		MessageBox.Show(controller, Lang.MenuText[69], Lang.MenuText[70], new string[1] { Lang.MenuText[90] });
		if (CurMenuMode == MenuMode.CONFIRM_DELETE_CHARACTER || CurMenuMode == MenuMode.CONFIRM_DELETE_WORLD)
		{
			PrevMenu();
		}
	}

	private void DeviceSelectorCanceled(object sender, EventArgs args)
	{
		LoadPlayers();
		WorldSelect.LoadWorlds();
		MessageBox.Show(controller, Lang.MenuText[69], Lang.MenuText[66], new string[1] { Lang.MenuText[90] });
	}

	private void DeviceSelected(object sender, EventArgs e)
	{
		try
		{
			CheckPlayerStorage("Settings");
			CheckPlayerStorage("Characters");
			CheckPlayerStorage("Worlds");
		}
		catch (Exception)
		{
			transferredPlayerStorage.Clear();
			return;
		}
		if (transferredPlayerStorage.Count <= 0)
		{
			if (!OpenSettings())
			{
				MessageBox.Show(controller, Lang.MenuText[9], Lang.MenuText[102], new string[1] { Lang.MenuText[90] });
			}
			LoadPlayers();
			if (this == MainUI && !WorldSelect.LoadWorlds())
			{
				MessageBox.Show(controller, Lang.MenuText[9], Lang.MenuText[103], new string[1] { Lang.MenuText[90] });
			}
		}
	}

	public void LoadPlayers()
	{
		numLoadPlayers = 0;
		if (HasPlayerStorage())
		{
			try
			{
				using StorageContainer storageContainer = MainUI.OpenPlayerStorage("Characters");
				// "player*.plr", not "player?.plr": the single-character wildcard capped the list
				// at nine files no matter what the rest of the code allowed.
				string[] fileNames = storageContainer.GetFileNames("player*.plr");
				int num = SaveSlots.SlotCount(fileNames, "player", ".plr");
				EnsurePlayerSlots(num);
				// Forget which slots had a file last time: a deleted character must not keep a row.
				for (int i = 0; i < loadPlayerPath.Length; i++)
				{
					loadPlayerPath[i] = null;
				}
				for (int j = 0; j < fileNames.Length; j++)
				{
					int num2;
					if (SaveSlots.TryParseIndex(fileNames[j], "player", ".plr", out num2))
					{
						EnsurePlayerSlots(num2 + 1);
						loadPlayerPath[num2] = fileNames[j];
					}
				}
				for (int k = 0; k < loadPlayerPath.Length; k++)
				{
					if (loadPlayerPath[k] == null)
					{
						if (loadPlayer[k] == null)
						{
							loadPlayer[k] = new Player();
						}
						continue;
					}
					if (loadPlayer[k] == null)
					{
						loadPlayer[k] = new Player();
					}
					try
					{
						loadPlayer[k].Load(storageContainer, loadPlayerPath[k]);
						if (loadPlayer[k].Name == null)
						{
							// Loaded, but without a name: the file is intact enough to exist and
							// damaged enough to be unusable. Say so once and keep the row, so the
							// player can delete it instead of wondering where it went.
							MessageBox.Show(controller, Lang.MenuText[9], Lang.MenuText[12], new string[1] { Lang.MenuText[90] });
							loadPlayer[k].CharacterName = loadPlayerPath[k];
						}
					}
					catch (Exception ex)
					{
						Program.LogWarn("UI", "could not load " + loadPlayerPath[k] + ": " + ex.Message);
						loadPlayer[k].CharacterName = loadPlayerPath[k];
					}
				}
				numLoadPlayers = CountSavedPlayers();
			}
			catch (IOException)
			{
				ReadError();
				numLoadPlayers = 0;
			}
			catch (Exception)
			{
				numLoadPlayers = 0;
			}
		}
		if (CurMenuMode == MenuMode.CHARACTER_SELECT)
		{
			ResetPlayerMenuSelection();
		}
	}

	/// <summary>Saved characters on disk, which is what the counters and guards actually mean.</summary>
	private int CountSavedPlayers()
	{
		int num = 0;
		for (int i = 0; i < loadPlayerPath.Length; i++)
		{
			if (!string.IsNullOrEmpty(loadPlayerPath[i]))
			{
				num++;
			}
		}
		return num;
	}

	public void SaveSettings()
	{
		if (!HasPlayerStorage())
		{
			return;
		}
		using MemoryStream memoryStream = new MemoryStream(512);
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(5);
		binaryWriter.Write(0u);
		binaryWriter.Write(SoundVolume);
		binaryWriter.Write(MusicVolume);
		binaryWriter.Write(autoSave);
		binaryWriter.Write(ShowItemText);
		binaryWriter.Write(alternateGrappleControls);
		byte[] buffer = Statistics.Serialize();
		binaryWriter.Write(buffer);
		binaryWriter.Write(totalSteps);
		binaryWriter.Write(TotalOrePicked);
		binaryWriter.Write(TotalBarsCrafted);
		binaryWriter.Write(TotalAnvilCrafting);
		binaryWriter.Write(totalWires);
		binaryWriter.Write(totalAirTime);
		binaryWriter.Write(petSpawnMask);
		int num = armorFound.Length + 7 >> 3;
		byte[] array = new byte[num];
		armorFound.CopyTo(array, 0);
		binaryWriter.Write((ushort)num);
		binaryWriter.Write(array, 0, num);
		binaryWriter.Write(IsOnline);
		binaryWriter.Write(IsInviteOnly);
		num = blacklist.Count;
		int num2 = num;
		if (num > 65535)
		{
			num = 65535;
		}
		binaryWriter.Write((ushort)num);
		for (int i = num2 - num; i < num2; i++)
		{
			binaryWriter.Write(blacklist[i]);
		}
		CRC32 cRC = new CRC32();
		cRC.Update(memoryStream.GetBuffer(), 8, (int)memoryStream.Length - 8);
		binaryWriter.Seek(4, SeekOrigin.Begin);
		binaryWriter.Write(cRC.GetValue());
		Main.ShowSaveIcon();
		try
		{
			if (TestStorageSpace("Settings", "config.dat", (int)memoryStream.Length))
			{
				using StorageContainer storageContainer = OpenPlayerStorage("Settings");
				using (Stream stream = storageContainer.OpenFile("config.dat", FileMode.Create))
				{
					stream.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
					stream.Close();
				}
				SettingsDirty = false;
			}
		}
		catch (IOException)
		{
			WriteError();
		}
		catch (Exception)
		{
		}
		binaryWriter.Close();
		Main.HideSaveIcon();
	}

	private bool OpenSettings()
	{
		bool flag = true;
		try
		{
			using (StorageContainer storageContainer = OpenPlayerStorage("Settings"))
			{
				if (storageContainer.FileExists("config.dat"))
				{
					try
					{
						using Stream stream = storageContainer.OpenFile("config.dat", FileMode.Open);
						using MemoryStream memoryStream = new MemoryStream((int)stream.Length);
						memoryStream.SetLength(stream.Length);
						stream.Read(memoryStream.GetBuffer(), 0, (int)stream.Length);
						stream.Close();
						using BinaryReader binaryReader = new BinaryReader(memoryStream);
						int num = binaryReader.ReadInt32();
						if (num > 5)
						{
							throw new InvalidOperationException("Invalid version");
						}
						if (num == 4 || num == 5)
						{
							CRC32 cRC = new CRC32();
							cRC.Update(memoryStream.GetBuffer(), 8, (int)memoryStream.Length - 8);
							if (cRC.GetValue() != binaryReader.ReadUInt32())
							{
								throw new InvalidOperationException("Invalid CRC32");
							}
						}
						else
						{
							binaryReader.ReadUInt32();
						}
						SoundVolume = binaryReader.ReadSingle();
						MusicVolume = binaryReader.ReadSingle();
						autoSave = binaryReader.ReadBoolean();
						ShowItemText = binaryReader.ReadBoolean();
						alternateGrappleControls = binaryReader.ReadBoolean();
						if (num <= 3)
						{
							alternateGrappleControls = false;
						}
						UpdateAlternateGrappleControls();
						if (this == MainUI)
						{
							Main.MusicVolume = MusicVolume;
							Main.SoundVolume = SoundVolume;
						}
						int count = Statistics.CalculateSerialisationSize();
						byte[] statStream = binaryReader.ReadBytes(count);
						Statistics.Deserialize(statStream);
						if (num >= 2)
						{
							if (num >= 3)
							{
								totalSteps = binaryReader.ReadUInt32();
								TotalOrePicked = binaryReader.ReadUInt32();
								TotalBarsCrafted = binaryReader.ReadUInt32();
								TotalAnvilCrafting = binaryReader.ReadUInt32();
								totalWires = binaryReader.ReadUInt32();
								totalAirTime = binaryReader.ReadUInt32();
								petSpawnMask = binaryReader.ReadByte();
								int count2 = binaryReader.ReadUInt16();
								armorFound = new BitArray(binaryReader.ReadBytes(count2));
								if (armorFound.Length < 632)
								{
									armorFound.Length = 632;
								}
							}
							IsOnline = binaryReader.ReadBoolean();
							IsInviteOnly = binaryReader.ReadBoolean();
						}
						if (num >= 6)
						{
							binaryReader.ReadBoolean();
							binaryReader.ReadBoolean();
						}
						blacklist.Clear();
						if (num >= 5)
						{
							int num2 = binaryReader.ReadUInt16();
							blacklist.Capacity = num2 + 4;
							while (num2 > 0)
							{
								blacklist.Add(binaryReader.ReadUInt64());
								num2--;
							}
						}
						binaryReader.Close();
					}
					catch (InvalidOperationException)
					{
						Main.ShowSaveIcon();
						flag = false;
						storageContainer.DeleteFile("config.dat");
						armorFound = new BitArray(632);
						Main.HideSaveIcon();
					}
					catch (Exception)
					{
					}
				}
			}
			SettingsDirty = !flag;
		}
		catch (IOException)
		{
			if (!flag)
			{
				WriteError();
				flag = true;
			}
			else
			{
				ReadError();
			}
		}
		catch (Exception)
		{
		}
		if (Main.NetMode == 1 && Main.IsGameStarted)
		{
			CheckBlacklist();
		}
		return flag;
	}

	public void ErasePlayer(int i)
	{
		if (HasPlayerStorage() && i >= 0 && i < loadPlayerPath.Length && !string.IsNullOrEmpty(loadPlayerPath[i]))
		{
			Main.ShowSaveIcon();
			try
			{
				using StorageContainer storageContainer = OpenPlayerStorage("Characters");
				storageContainer.DeleteFile(loadPlayerPath[i]);
			}
			catch (IOException)
			{
				WriteError();
			}
			catch (Exception)
			{
			}
			Main.HideSaveIcon();
		}
		if (i < 0 || i >= loadPlayerPath.Length)
		{
			return;
		}
		// The slot is emptied, not shifted: slot numbers are file numbers, so moving the last
		// character into the hole would rename it into somebody else's save on the next load.
		loadPlayerPath[i] = null;
		loadPlayer[i] = new Player();
		numLoadPlayers = CountSavedPlayers();
	}

	private string nextLoadPlayer()
	{
		string result = null;
		if (HasPlayerStorage())
		{
			try
			{
				using StorageContainer storageContainer = OpenPlayerStorage("Characters");
				// Ask for a free slot, then make sure the file really is free: the list in memory
				// can be older than the directory (another view saved in between).
				int num = NextFreePlayerSlot();
				do
				{
					result = "player" + num.ToStringLookup() + ".plr";
					if (!storageContainer.FileExists(result))
					{
						return result;
					}
					num++;
				}
				while (num < 4096);
				return null;
			}
			catch (IOException)
			{
				ReadError();
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}
		return result;
	}

	public void SetUIView(WorldView.Type viewType, bool NoAutoFullScreen = false)
	{
		if (CurrentView != null)
		{
			for (int i = 0; i < NumActiveViews; i++)
			{
				if (activeView[i] == CurrentView)
				{
					activeView[i] = activeView[--NumActiveViews];
					activeView[NumActiveViews] = null;
					break;
				}
			}
		}
		if (viewType != WorldView.Type.NONE)
		{
			if (CurrentView == null)
			{
				CurrentView = new WorldView();
				CurrentView.Player = ActivePlayer;
				CurrentView.Ui = this;
			}
			activeView[NumActiveViews++] = CurrentView;
			CurrentUI = this;
			if (NumActiveViews == 2)
			{
				LoadSplitscreenFonts(theGame.Content);
				InvalidateCachedText();
			}
			if (CurrentView.setViewType(viewType) && CurMenuType != MenuType.MAIN)
			{
				worldFade = -0.25f;
			}
		}
		else if (CurrentView != null)
		{
			CurrentView.Dispose();
			CurrentView = null;
			if (NumActiveViews == 1)
			{
				LoadFonts(theGame.Content);
				InvalidateCachedText();
			}
			if (MainUI == this)
			{
				if (NumActiveViews > 0)
				{
					for (int j = 0; j < 4; j++)
					{
						if (Main.UIInstance[j].CurrentView != null)
						{
							MainUI = Main.UIInstance[j];
							WorldSelect.LoadWorlds();
							break;
						}
					}
				}
				else
				{
					MainUI = null;
				}
			}
		}
		else
		{
			NoAutoFullScreen = true;
		}
		if (ActivePlayer != null)
		{
			ActivePlayer.CurrentView = CurrentView;
			if (CurrentView == null)
			{
				ActivePlayer.Active = 0;
			}
		}
		if (NoAutoFullScreen)
		{
			return;
		}
		if (NumActiveViews == 1 && MainUI != null && MainUI.CurrentView != null && !MainUI.CurrentView.IsFullScreen())
		{
			MainUI.CurrentView.setViewType();
			if (MainUI.CurMenuType != MenuType.MAIN)
			{
				MainUI.worldFade = -0.25f;
			}
			return;
		}
		for (int k = 0; k < 4; k++)
		{
			UI uI = Main.UIInstance[k];
			if (uI.CurrentView != null)
			{
				uI.CurrentView.setViewType(WorldView.getViewType(uI.controller));
			}
		}
	}

	public void setPlayer(int id, bool swapIfUsed = true)
	{
		if (id < 0)
		{
			for (int num = 7; num >= 0; num--)
			{
				if (Main.PlayerSet[num].Active == 0 && Main.PlayerSet[num].CurrentView == null)
				{
					id = num;
					break;
				}
			}
			if (id < 0)
			{
				MyPlayer = 8;
				ActivePlayer = null;
				return;
			}
		}
		if (ActivePlayer != null && id != MyPlayer)
		{
			Player player = ActivePlayer.DeepCopy();
			player.WhoAmI = (byte)id;
			ActivePlayer.ui = null;
			ActivePlayer.CurrentView = null;
			if (swapIfUsed)
			{
				for (int i = 0; i < 4; i++)
				{
					UI uI = Main.UIInstance[i];
					if (uI != this && uI.MyPlayer == id)
					{
						uI.setPlayer(MyPlayer, swapIfUsed: false);
						break;
					}
				}
			}
			if (id != MyPlayer)
			{
				Main.PlayerSet[id] = player;
			}
		}
		MyPlayer = (byte)id;
		ActivePlayer = Main.PlayerSet[id];
		ActivePlayer.ui = this;
		ActivePlayer.CurrentView = CurrentView;
		if (SignedInGamer != null)
		{
			ActivePlayer.Name = SignedInGamer.Gamertag;
		}
		teamCooldown = 0;
		teamSelected = ActivePlayer.team;
		pvpCooldown = 0;
		pvpSelected = ActivePlayer.hostile;
		if (CurrentView != null)
		{
			CurrentView.Player = ActivePlayer;
		}
		else
		{
			ActivePlayer.Active = 0;
		}
	}

	public void SetPlayer(Player p)
	{
		if (ActivePlayer != null && p != ActivePlayer)
		{
			ActivePlayer.ui = null;
			ActivePlayer.CurrentView = null;
		}
		ActivePlayer = p;
		teamCooldown = 0;
		pvpCooldown = 0;
		if (CurrentView != null)
		{
			CurrentView.Player = p;
		}
		if (p != null)
		{
			teamSelected = p.team;
			pvpSelected = p.hostile;
			p.ui = this;
			p.CurrentView = CurrentView;
			p.WhoAmI = MyPlayer;
			if (SignedInGamer != null)
			{
				p.Name = SignedInGamer.Gamertag;
			}
			p.Active = 0;
			Main.PlayerSet[MyPlayer] = p;
		}
		else
		{
			MyPlayer = 8;
			SetUIView(WorldView.Type.NONE);
		}
	}

	public void JoinSession(int newPlayerIndex)
	{
		setPlayer(newPlayerIndex);
		if (Main.NetMode == 1)
		{
			NetMessage.CreateMessage0(11);
			NetMessage.SendMessage();
		}
	}

	public void LeaveSession()
	{
		localGamer = null;
	}

	public void InviteAccepted(InviteAcceptedEventArgs e)
	{
		if (e.IsCurrentSession || Netplay.isJoiningRemoteInvite)
		{
			if (Netplay.Session == null)
			{
				if (!Netplay.gamersWhoReceivedInvite.Contains(e.Gamer))
				{
					Netplay.gamersWhoReceivedInvite.Add(e.Gamer);
				}
				if (!Netplay.gamersWaitingToJoinInvite.Contains(e.Gamer))
				{
					Netplay.gamersWaitingToJoinInvite.Add(e.Gamer);
				}
			}
			if (CurrentView == null)
			{
				SetMenu(MenuMode.CHARACTER_SELECT, rememberPrevious: false, reset: true);
				OpenView();
			}
			return;
		}
		Netplay.isJoiningRemoteInvite = true;
		Netplay.gamersWhoReceivedInvite.Add(e.Gamer);
		Netplay.gamersWaitingToJoinInvite.Add(e.Gamer);
		for (int i = 0; i < 4; i++)
		{
			SignedInGamer signedInGamer = Main.UIInstance[i].SignedInGamer;
			if (signedInGamer != null && !Netplay.gamersWaitingToJoinInvite.Contains(signedInGamer))
			{
				Netplay.gamersWaitingToJoinInvite.Add(signedInGamer);
			}
		}
		if (Netplay.Session != null)
		{
			ExitGame();
			return;
		}
		if (MainUI.CurMenuMode == MenuMode.WELCOME)
		{
			OpenMainView(e.Gamer);
			return;
		}
		if (Main.WorldGenThread != null)
		{
			Main.WorldGenThread.Abort();
			Main.WorldGenThread = null;
			WorldGen.Gen = false;
		}
		MainUI.SetMenu(MenuMode.TITLE, rememberPrevious: false, reset: true);
	}

	private static void CancelInvite(SignedInGamer gamer)
	{
		Netplay.gamersWhoReceivedInvite.Remove(gamer);
		if (Netplay.gamersWhoReceivedInvite.Count == 0)
		{
			Netplay.isJoiningRemoteInvite = false;
			Netplay.gamersWaitingToJoinInvite.Clear();
		}
		else
		{
			Netplay.gamersWaitingToJoinInvite.Remove(gamer);
		}
	}

	private unsafe void DrawControlsIngame()
	{
		if (CurMenuType != MenuType.NONE)
		{
			return;
		}
		Main.StrBuilder.Length = 0;
		if (!Main.TutorialMaskY)
		{
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.INVENTORY));
			Main.StrBuilder.Append(' ');
		}
		if (ActivePlayer.grappleItemSlot >= 0)
		{
			Main.StrBuilder.Append(Lang.Controls(alternateGrappleControls ? Lang.CONTROLS.GRAPPLE_ALT : Lang.CONTROLS.GRAPPLE));
			Main.StrBuilder.Append(' ');
		}
		fixed (Item* ptr = &ActivePlayer.Inventory[ActivePlayer.SelectedItem])
		{
			if (!Main.TutorialMaskRT && ptr->Type > 0)
			{
				if (ptr->PickPower > 0)
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.DIG));
				}
				else if (ptr->AxePower > 0)
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHOP));
				}
				else if (ptr->HammerPower > 0)
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.HIT));
				}
				else if (ptr->CreateTile >= 0 || ptr->CreateWall >= 0)
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BUILD));
				}
				else if (ptr->Ammo > 0 || ptr->Damage > 0)
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.ATTACK));
				}
				else
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.USE));
				}
				Main.StrBuilder.Append(' ');
			}
			if (!Main.TutorialMaskB)
			{
				if (ActivePlayer.npcChatBubble >= 0)
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.TALK));
				}
				else if (ActivePlayer.tileInteractX != 0)
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.INTERACT));
				}
			}
		}
		DrawStringLB(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetBottom);
	}

	private void DrawControlsInventory()
	{
		if (CurMenuType != MenuType.NONE)
		{
			return;
		}
		if (PointerActive)
		{
			// Desktop hint set: the gamepad glyphs below are meaningless with a mouse.
			Main.StrBuilder.Length = 0;
			Main.StrBuilder.Append("LMB - pick / place    Esc or Tab - close");
			if (toolTip.Type > 0 && !reforge && !CraftGuide && toolTip.Stack > 1)
			{
				Main.StrBuilder.Append("    RMB - split stack");
			}
			DrawStringLB(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetBottom);
			return;
		}
		Main.StrBuilder.Length = 0;
		Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHANGE_MENU));
		Main.StrBuilder.Append(' ');
		if (toolTip.Type > 0 && !reforge && !CraftGuide)
		{
			if (toolTip.IsEquipable() && (ActiveInvSection != InventorySection.EQUIP || inventoryEquipX == 0))
			{
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.EQUIP));
				Main.StrBuilder.Append(' ');
			}
			if (toolTip.Type >= 599 && toolTip.Type <= 601)
			{
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.OPEN));
				Main.StrBuilder.Append(' ');
			}
			else if (toolTip.Stack > 1 && (mouseItem.Type == 0 || mouseItem.NetID == toolTip.NetID))
			{
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT_ONE));
				Main.StrBuilder.Append(' ');
			}
		}
		if (mouseItem.Type == 0)
		{
			if (toolTip.Type > 0)
			{
				if (reforge)
				{
					if (toolTip.SetPrefix(-3))
					{
						Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.REFORGE));
						Main.StrBuilder.Append(' ');
					}
				}
				else if (CraftGuide)
				{
					if (toolTip.IsMaterial)
					{
						Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SHOW_RECIPES));
						Main.StrBuilder.Append(' ');
					}
				}
				else
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT_ALL));
					Main.StrBuilder.Append(' ');
				}
			}
			else if (ActiveInvSection == InventorySection.CHEST)
			{
				if (inventoryChestX < 0)
				{
					switch (inventoryChestY)
					{
					case 1:
						Main.StrBuilder.Append(Lang.InterfaceText[29]);
						break;
					case 2:
						Main.StrBuilder.Append(Lang.InterfaceText[30]);
						break;
					case 3:
						Main.StrBuilder.Append(Lang.InterfaceText[31]);
						break;
					}
					Main.StrBuilder.Append(' ');
				}
			}
			else if (ActiveInvSection == InventorySection.EQUIP && inventoryEquipY == 0)
			{
				int num = inventoryEquipX + inventoryBuffX;
				num %= 10;
				if (ActivePlayer.buff[num].Time > 0 && !ActivePlayer.buff[num].IsDebuff())
				{
					Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CANCEL_BUFF));
					Main.StrBuilder.Append(' ');
				}
			}
		}
		else
		{
			bool flag = false;
			bool flag2 = true;
			if (ActiveInvSection == InventorySection.EQUIP)
			{
				sbyte b = inventoryEquipY;
				if (1 == 0)
				{
				}
				bool flag3 = b switch
				{
					0 => mouseItem.HeadSlot >= 0, 
					1 => mouseItem.BodySlot >= 0, 
					2 => mouseItem.LegSlot >= 0, 
					_ => mouseItem.IsAccessory, 
				};
				if (1 == 0)
				{
				}
				flag = flag3;
			}
			else if (ActiveInvSection == InventorySection.ITEMS && mouseItem.Type > 0 && mouseItem.Stack > 0)
			{
				switch (inventoryItemY)
				{
				case 4:
					flag2 = mouseItem.CanBePlacedInAmmoSlot();
					break;
				case 5:
					flag2 = mouseItem.CanBePlacedInCoinSlot();
					break;
				}
			}
			if (flag2)
			{
				if (toolTip.Type > 0 && (toolTip.NetID != mouseItem.NetID || toolTip.Stack == toolTip.MaxStack || mouseItem.Stack == mouseItem.MaxStack))
				{
					if ((ActiveInvSection != InventorySection.EQUIP) | flag)
					{
						Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SWAP));
						Main.StrBuilder.Append(' ');
					}
				}
				else if (toolTip.Type == 0 || toolTip.Stack < toolTip.MaxStack)
				{
					if (ActiveInvSection == InventorySection.EQUIP)
					{
						if (flag)
						{
							Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.PLACE_EQUIPMENT));
							Main.StrBuilder.Append(' ');
						}
					}
					else
					{
						Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.PLACE));
						Main.StrBuilder.Append(' ');
					}
				}
			}
		}
		Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CLOSE));
		Main.StrBuilder.Append(' ');
		if (!reforge && !CraftGuide)
		{
			if (mouseItem.Type > 0)
			{
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.DROP));
				Main.StrBuilder.Append(' ');
			}
			else if (ActiveInvSection == InventorySection.ITEMS && toolTip.Type > 0)
			{
				Lang.CONTROLS idx = ((npcShop <= 0) ? Lang.CONTROLS.TRASH : Lang.CONTROLS.SELL);
				Main.StrBuilder.Append(Lang.Controls(idx));
				Main.StrBuilder.Append(' ');
			}
		}
		DrawStringLB(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetBottom);
	}

	private void DrawControlsShop()
	{
		if (CurMenuType != MenuType.NONE)
		{
			return;
		}
		Main.StrBuilder.Length = 0;
		Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHANGE_MENU));
		Main.StrBuilder.Append(' ');
		if (toolTip.Type > 0 && toolTip.Stack > 1 && (mouseItem.Type == 0 || mouseItem.NetID == toolTip.NetID))
		{
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BUY_ONE));
			Main.StrBuilder.Append(' ');
		}
		if (mouseItem.Type == 0)
		{
			if (toolTip.Type > 0)
			{
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BUY_ALL));
				Main.StrBuilder.Append(' ');
			}
		}
		else if (toolTip.Type == 0)
		{
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SELL_ITEM_IN_HAND));
			Main.StrBuilder.Append(' ');
		}
		Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CLOSE));
		Main.StrBuilder.Append(' ');
		if (mouseItem.Type > 0)
		{
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.DROP));
			Main.StrBuilder.Append(' ');
		}
		DrawStringLB(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetBottom);
	}

	private void DrawControlsCrafting()
	{
		if (CurMenuType == MenuType.NONE)
		{
			Main.StrBuilder.Length = 0;
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHANGE_MENU));
			Main.StrBuilder.Append(' ');
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CRAFTING_CATEGORY));
			Main.StrBuilder.Append(' ');
			if (ActivePlayer.CanCraftRecipe(CraftingRecipe))
			{
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CRAFT));
				Main.StrBuilder.Append(' ');
			}
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CLOSE));
			Main.StrBuilder.Append(' ');
			if (mouseItem.Type > 0)
			{
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.DROP));
				Main.StrBuilder.Append(' ');
			}
			else
			{
				Main.StrBuilder.Append(Lang.Controls(craftingShowCraftable ? Lang.CONTROLS.SHOW_ALL : Lang.CONTROLS.SHOW_AVAILABLE));
				Main.StrBuilder.Append(' ');
			}
			Lang.CONTROLS idx = ((craftingSection == CraftingSection.RECIPES) ? Lang.CONTROLS.INGREDIENTS : Lang.CONTROLS.RECIPES);
			Main.StrBuilder.Append(Lang.Controls(idx));
			DrawStringLB(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetBottom);
		}
	}

	private void DrawControlsHousing()
	{
		if (CurMenuType == MenuType.NONE)
		{
			Main.StrBuilder.Length = 0;
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHANGE_MENU));
			Main.StrBuilder.Append(' ');
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHECK_HOUSING));
			Main.StrBuilder.Append(' ');
			Main.StrBuilder.Append(Lang.Controls(showNPCs ? Lang.CONTROLS.HIDE_BANNERS : Lang.CONTROLS.SHOW_BANNERS));
			Main.StrBuilder.Append(' ');
			if (inventoryHousingNpc >= 0)
			{
				Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.ASSIGN_TO_ROOM));
				Main.StrBuilder.Append(' ');
			}
			Main.StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CLOSE));
			Main.StrBuilder.Append(' ');
			DrawStringLB(BoldSmallFont, CurrentView.SafeAreaOffsetLeft, CurrentView.SafeAreaOffsetBottom);
		}
	}

	public int DrawDialog(Vector2 pos, Color backColor, Color textColor, CompiledText ct, string caption = null, bool anchorBottom = false)
	{
		int num = (int)(30f * Main.ScreenMultiplier);
		if (Main.ScreenHeightPtr != ScreenHeights.BASE)
		{
			if (anchorBottom)
			{
				switch (Main.ScreenHeightPtr)
				{
				case ScreenHeights.HD:
					num = (int)((float)num / 1.33f);
					break;
				case ScreenHeights.FHD:
					num = (int)((float)num / 2f);
					break;
				}
			}
			else
			{
				switch (Main.ScreenHeightPtr)
				{
				case ScreenHeights.HD:
					num = (int)((float)num / 1.33f);
					break;
				case ScreenHeights.FHD:
					num = (int)((float)num / 1.25f);
					break;
				}
			}
		}
		if (anchorBottom)
		{
			pos.Y -= ct.Height + num;
			num = 0;
		}
		int num2 = 30;
		int width = 470;
		int num3 = 500;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num3 = (int)((float)num3 * 1.15f);
			width = 548;
			if (CurMenuType != MenuType.NONE)
			{
				num2 = (int)((float)num2 / 1.33f);
			}
			break;
		case ScreenHeights.FHD:
			num3 = (int)((float)num3 * 1.534f);
			width = 705;
			break;
		}
		if (Main.PSMode && Main.ScreenHeightPtr == ScreenHeights.HD)
		{
			num2 -= 7;
		}
		if (Main.ScreenHeightPtr != ScreenHeights.BASE)
		{
			Main.DrawRect(451, pos, new Rectangle(0, 0, num3, ct.Height + num + num2), backColor);
		}
		else
		{
			Main.SpriteBatch.Draw(chatBackTexture, pos, new Rectangle(0, 0, chatBackTexture.Width, ct.Height + num), backColor);
			pos.Y += ct.Height + num;
			Main.SpriteBatch.Draw(chatBackTexture, pos, new Rectangle(0, chatBackTexture.Height - 30, chatBackTexture.Width, num2), backColor);
			pos.Y -= ct.Height + num;
		}
		if (caption != null)
		{
			int num4 = (int)BoldSmallFont.MeasureString(caption).X;
			int num5 = num3 - num4 >> 1;
			int num6 = 0;
			if (Main.ScreenHeightPtr == ScreenHeights.FHD)
			{
				num6 += num2 / 2;
			}
			Main.SpriteBatch.DrawString(BoldSmallFont, caption, new Vector2(pos.X + (float)num5, pos.Y + (float)num6), Color.LightGreen);
			pos.Y += num;
		}
		else
		{
			pos.Y += 10f;
		}
		pos.X += 20f;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			if (caption == null)
			{
				pos.Y += 0.62f;
			}
			if (CurMenuType != MenuType.NONE)
			{
				pos.Y -= 5f;
				pos.X -= 6.25f;
			}
			pos.X -= 1.25f;
			break;
		case ScreenHeights.FHD:
			pos.X += 8f;
			pos.Y += 4f;
			if (caption != null)
			{
				pos.Y += 12f;
			}
			break;
		}
		ct.Draw(Main.SpriteBatch, new Rectangle((int)pos.X, (int)pos.Y, width, Main.ResolutionHeight), textColor, new Color(255, 212, 64, 255));
		return ct.Height;
	}

	private void HelpText()
	{
		bool flag = ActivePlayer.StatLifeMax > 100;
		bool flag2 = ActivePlayer.statManaMax > 0;
		bool flag3 = true;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		bool flag9 = false;
		for (int i = 0; i < 48; i++)
		{
			if (ActivePlayer.Inventory[i].PickPower > 0 && ActivePlayer.Inventory[i].NetID != -13)
			{
				flag3 = false;
			}
			else if (ActivePlayer.Inventory[i].AxePower > 0 && ActivePlayer.Inventory[i].NetID != -16)
			{
				flag3 = false;
			}
			else if (ActivePlayer.Inventory[i].HammerPower > 0)
			{
				flag3 = false;
			}
			switch ((EntityID.ItemID)ActivePlayer.Inventory[i].Type)
			{
			case EntityID.ItemID.IRON_ORE:
			case EntityID.ItemID.COPPER_ORE:
			case EntityID.ItemID.GOLD_ORE:
			case EntityID.ItemID.SILVER_ORE:
				flag4 = true;
				break;
			case EntityID.ItemID.GOLD_BAR:
			case EntityID.ItemID.COPPER_BAR:
			case EntityID.ItemID.SILVER_BAR:
			case EntityID.ItemID.IRON_BAR:
				flag5 = true;
				break;
			case EntityID.ItemID.FALLEN_STAR:
				flag6 = true;
				break;
			case EntityID.ItemID.LENS:
				flag7 = true;
				break;
			case EntityID.ItemID.ROTTEN_CHUNK:
			case EntityID.ItemID.WORM_FOOD:
				flag8 = true;
				break;
			case EntityID.ItemID.GRAPPLING_HOOK:
				flag9 = true;
				break;
			}
		}
		bool flag10 = false;
		bool flag11 = false;
		bool flag12 = false;
		bool flag13 = false;
		bool flag14 = false;
		bool flag15 = false;
		bool flag16 = false;
		bool flag17 = false;
		bool flag18 = false;
		for (int j = 0; j < 196; j++)
		{
			if (Main.NPCSet[j].Active != 0)
			{
				switch ((EntityID.NPCID)Main.NPCSet[j].Type)
				{
				case EntityID.NPCID.MERCHANT:
					flag10 = true;
					break;
				case EntityID.NPCID.NURSE:
					flag11 = true;
					break;
				case EntityID.NPCID.ARMS_DEALER:
					flag13 = true;
					break;
				case EntityID.NPCID.DRYAD:
					flag12 = true;
					break;
				case EntityID.NPCID.GOBLIN_TINKERER:
					flag17 = true;
					break;
				case EntityID.NPCID.CLOTHIER:
					flag18 = true;
					break;
				case EntityID.NPCID.MECHANIC:
					flag15 = true;
					break;
				case EntityID.NPCID.DEMOLITIONIST:
					flag14 = true;
					break;
				case EntityID.NPCID.WIZARD:
					flag16 = true;
					break;
				}
			}
		}
		while (true)
		{
			helpText++;
			if (flag3)
			{
				if (helpText == 1)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 177);
					return;
				}
				if (helpText == 2)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 178);
					return;
				}
				if (helpText == 3)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 179);
					return;
				}
				if (helpText == 4)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 180);
					return;
				}
				if (helpText == 5)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 181);
					return;
				}
				if (helpText == 6)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 182);
					return;
				}
			}
			if (flag3 && !flag4 && !flag5 && helpText == 11)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 183);
				return;
			}
			if ((flag3 & flag4) && !flag5)
			{
				if (helpText == 21)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 184);
					return;
				}
				if (helpText == 22)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 185);
					return;
				}
			}
			if (flag3 & flag5)
			{
				if (helpText == 31)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 186);
					return;
				}
				if (helpText == 32)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 187);
					return;
				}
			}
			if (!flag && helpText == 41)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 188);
				return;
			}
			if (!flag2 && helpText == 42)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 189);
				return;
			}
			if (!flag2 && !flag6 && helpText == 43)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 190);
				return;
			}
			if (!flag10 && !flag11)
			{
				if (helpText == 51)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 191);
					return;
				}
				if (helpText == 52)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 192);
					return;
				}
				if (helpText == 53)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 193);
					return;
				}
				if (helpText == 54)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 194);
					return;
				}
			}
			if (!flag10 && helpText == 61)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 195);
				return;
			}
			if (!flag11 && helpText == 62)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 196);
				return;
			}
			if (!flag13 && helpText == 63)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 197);
				return;
			}
			if (!flag12 && helpText == 64)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 198);
				return;
			}
			if (!flag15 && helpText == 65 && NPC.HasDownedBoss3)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 199);
				return;
			}
			if (!flag18 && helpText == 66 && NPC.HasDownedBoss3)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 200);
				return;
			}
			if (!flag14 && helpText == 67)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 201);
				return;
			}
			if (!flag17 && NPC.HasDownedBoss2 && helpText == 68)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 202);
				return;
			}
			if (!flag16 && Main.InHardMode && helpText == 69)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 203);
				return;
			}
			if (flag7 && helpText == 71)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 204);
				return;
			}
			if (flag8 && helpText == 72)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 205);
				return;
			}
			if ((flag7 | flag8) && helpText == 80)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 206);
				return;
			}
			if (!flag9 && helpText == 201 && !Main.InHardMode && !NPC.HasDownedBoss3 && !NPC.HasDownedBoss2)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 207);
				return;
			}
			if (helpText == 1000 && !NPC.HasDownedBoss1 && !NPC.HasDownedBoss2)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 208);
				return;
			}
			if (helpText == 1001 && !NPC.HasDownedBoss1 && !NPC.HasDownedBoss2)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 209);
				return;
			}
			if (helpText == 1002 && !NPC.HasDownedBoss3)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 210);
				return;
			}
			if (helpText == 1050 && !NPC.HasDownedBoss1)
			{
				if (ActivePlayer.StatLifeMax < 200)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 211);
					return;
				}
				continue;
			}
			if (helpText == 1051 && !NPC.HasDownedBoss1)
			{
				if (ActivePlayer.StatDefense <= 10)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 212);
					return;
				}
				continue;
			}
			if (helpText == 1052 && !NPC.HasDownedBoss1)
			{
				if (ActivePlayer.StatLifeMax >= 200 && ActivePlayer.StatDefense > 10)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 213);
					return;
				}
				continue;
			}
			if (helpText == 1053 && NPC.HasDownedBoss1 && !NPC.HasDownedBoss2)
			{
				if (ActivePlayer.StatLifeMax < 300)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 214);
					return;
				}
				continue;
			}
			if (helpText == 1054 && NPC.HasDownedBoss1 && !NPC.HasDownedBoss2)
			{
				if (ActivePlayer.StatLifeMax >= 300)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 215);
					return;
				}
				continue;
			}
			if (helpText == 1055 && NPC.HasDownedBoss1 && !NPC.HasDownedBoss2)
			{
				if (ActivePlayer.StatLifeMax >= 300)
				{
					npcChatText = Lang.NPCDialog(ActivePlayer, 216);
					return;
				}
				continue;
			}
			if (helpText == 1056 && NPC.HasDownedBoss1 && NPC.HasDownedBoss2 && !NPC.HasDownedBoss3)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 217);
				return;
			}
			if (helpText == 1057 && NPC.HasDownedBoss1 && NPC.HasDownedBoss2 && NPC.HasDownedBoss3 && !Main.InHardMode && ActivePlayer.StatLifeMax < 400)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 218);
				return;
			}
			if (helpText == 1058 && NPC.HasDownedBoss1 && NPC.HasDownedBoss2 && NPC.HasDownedBoss3 && !Main.InHardMode && ActivePlayer.StatLifeMax >= 400)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 219);
				return;
			}
			if (helpText == 1059 && NPC.HasDownedBoss1 && NPC.HasDownedBoss2 && NPC.HasDownedBoss3 && !Main.InHardMode && ActivePlayer.StatLifeMax >= 400)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 220);
				return;
			}
			if (helpText == 1060 && NPC.HasDownedBoss1 && NPC.HasDownedBoss2 && NPC.HasDownedBoss3 && !Main.InHardMode && ActivePlayer.StatLifeMax >= 400)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 221);
				return;
			}
			if (helpText == 1061 && Main.InHardMode)
			{
				npcChatText = Lang.NPCDialog(ActivePlayer, 222);
				return;
			}
			if (helpText == 1062 && Main.InHardMode)
			{
				break;
			}
			if (helpText > 1100)
			{
				helpText = 0;
			}
		}
		npcChatText = Lang.NPCDialog(ActivePlayer, 223);
	}

	public void UpdateNpcChat()
	{
		focusText = null;
		focusText3 = null;
		int num = (MouseTextBrightness * 2 + 255) / 3;
		focusColor = new Color(num, (int)((double)num * 0.9090909361839294), num >> 1, num);
		int num2 = ActivePlayer.StatLifeMax - ActivePlayer.statLife;
		if (ActivePlayer.sign >= 0)
		{
			focusText = Lang.InterfaceText[48];
		}
		else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 20)
		{
			focusText = Lang.InterfaceText[28];
			focusText3 = Lang.InterfaceText[49];
		}
		else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 107)
		{
			focusText = Lang.InterfaceText[28];
			focusText3 = Lang.InterfaceText[19];
		}
		else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 17 || Main.NPCSet[ActivePlayer.TalkNPC].Type == 19 || Main.NPCSet[ActivePlayer.TalkNPC].Type == 38 || Main.NPCSet[ActivePlayer.TalkNPC].Type == 54 || Main.NPCSet[ActivePlayer.TalkNPC].Type == 108 || Main.NPCSet[ActivePlayer.TalkNPC].Type == 124 || Main.NPCSet[ActivePlayer.TalkNPC].Type == 142)
		{
			focusText = Lang.InterfaceText[28];
		}
		else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 37)
		{
			if (!Main.GameTime.DayTime)
			{
				focusText = Lang.InterfaceText[50];
			}
		}
		else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 22)
		{
			focusText = Lang.InterfaceText[51];
			if (!Main.IsTutorial())
			{
				focusText3 = Lang.InterfaceText[25];
			}
		}
		else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 18)
		{
			focusText = Lang.InterfaceText[54];
			for (int i = 0; i < 10; i++)
			{
				if (ActivePlayer.buff[i].IsHealable())
				{
					num2 += 1000;
				}
			}
			if (num2 > 0)
			{
				string text = "";
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = (int)((double)num2 * 0.75);
				if (num7 < 1)
				{
					num7 = 1;
				}
				num2 = num7;
				if (num7 >= 1000000)
				{
					num3 = num7 / 1000000;
					num7 -= num3 * 1000000;
				}
				if (num7 >= 10000)
				{
					num4 = num7 / 10000;
					num7 -= num4 * 10000;
				}
				if (num7 >= 100)
				{
					num5 = num7 / 100;
					num7 -= num5 * 100;
				}
				if (num7 > 0)
				{
					num6 = num7;
				}
				if (num3 > 0)
				{
					text = num3 + Lang.InterfaceText[15];
				}
				if (num4 > 0)
				{
					text = text + num4 + Lang.InterfaceText[16];
				}
				if (num5 > 0)
				{
					text = text + num5 + Lang.InterfaceText[17];
				}
				if (num6 > 0)
				{
					text = text + num6 + Lang.InterfaceText[18];
				}
				float num8 = (float)(int)MouseTextBrightness * (1f / 255f);
				if (num3 > 0)
				{
					focusColor = new Color((byte)(220f * num8), (byte)(220f * num8), (byte)(198f * num8), MouseTextBrightness);
				}
				else if (num4 > 0)
				{
					focusColor = new Color((byte)(224f * num8), (byte)(201f * num8), (byte)(92f * num8), MouseTextBrightness);
				}
				else if (num5 > 0)
				{
					focusColor = new Color((byte)(181f * num8), (byte)(192f * num8), (byte)(193f * num8), MouseTextBrightness);
				}
				else if (num6 > 0)
				{
					focusColor = new Color((byte)(246f * num8), (byte)(138f * num8), (byte)(96f * num8), MouseTextBrightness);
				}
				focusText = focusText + " (" + text + ")";
			}
		}
		ActivePlayer.releaseUseItem = false;
		// The chat options are drawn as three words in a row, and until now only the stick and A
		// could reach them: the pointer was ignored, and a left click fell through to "use item".
		// The rectangles come from the draw pass, so a click lands on the word it is over.
		bool pointerActivate = false;
		if (UI.PointerActive)
		{
			int hovered = NpcChatOptionUnderPointer();
			if (hovered >= 0)
			{
				if (npcChatSelectedItem != hovered)
				{
					npcChatSelectedItem = (sbyte)hovered;
					Main.PlaySound(12);
				}
				if (Terraria.Input.KeyboardMouseInput.LeftClicked)
				{
					pointerActivate = true;
				}
			}
		}
		// Right click closes the dialog, the way Escape does: in this screen there is nothing else
		// for the right button to mean, and reaching for Escape to leave a conversation is worse.
		bool pointerCancel = UI.PointerActive && Terraria.Input.KeyboardMouseInput.RightClicked;
		if (focusText == null && focusText3 == null)
		{
			npcChatSelectedItem = 1;
		}
		else if (IsLeftButtonTriggered())
		{
			Main.PlaySound(12);
			if (--npcChatSelectedItem < 0)
			{
				npcChatSelectedItem = (sbyte)((focusText3 == null) ? 1 : 2);
			}
		}
		else if (IsRightButtonTriggered())
		{
			Main.PlaySound(12);
			npcChatSelectedItem++;
			if (npcChatSelectedItem == 3 || (npcChatSelectedItem == 2 && focusText3 == null))
			{
				npcChatSelectedItem = 0;
			}
		}
		if (IsButtonTriggered(Buttons.B) || pointerCancel)
		{
			ActivePlayer.TalkNPC = -1;
			ActivePlayer.sign = -1;
			editSign = false;
			npcChatText = null;
			Main.PlaySound(11);
			ClearButtonTriggers();
		}
		else
		{
			if (!IsButtonTriggered(Buttons.A) && !pointerActivate)
			{
				return;
			}
			if (npcChatSelectedItem == 0)
			{
				if (ActivePlayer.sign != -1)
				{
					Main.PlaySound(12);
					editSign = true;
					ClearInput();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 17)
				{
					npcChatText = null;
					npcShop = 1;
					Main.Shop[npcShop].SetupShop(npcShop, ActivePlayer);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 19)
				{
					npcChatText = null;
					npcShop = 2;
					Main.Shop[npcShop].SetupShop(npcShop);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 124)
				{
					npcChatText = null;
					npcShop = 8;
					Main.Shop[npcShop].SetupShop(npcShop);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 142)
				{
					npcChatText = null;
					npcShop = 9;
					Main.Shop[npcShop].SetupShop(npcShop);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 37)
				{
					if (Main.NetMode != 1)
					{
						NPC.SpawnSkeletron();
					}
					else
					{
						NetMessage.CreateMessage0(62);
						NetMessage.SendMessage();
					}
					npcChatText = null;
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 20)
				{
					npcChatText = null;
					npcShop = 3;
					Main.Shop[npcShop].SetupShop(npcShop);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 38)
				{
					npcChatText = null;
					npcShop = 4;
					Main.Shop[npcShop].SetupShop(npcShop);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 54)
				{
					npcChatText = null;
					npcShop = 5;
					Main.Shop[npcShop].SetupShop(npcShop, ActivePlayer);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 107)
				{
					npcChatText = null;
					npcShop = 6;
					Main.Shop[npcShop].SetupShop(npcShop);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 108)
				{
					npcChatText = null;
					npcShop = 7;
					Main.Shop[npcShop].SetupShop(npcShop);
					Main.PlaySound(12);
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 22)
				{
					Main.PlaySound(12);
					HelpText();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 18)
				{
					Main.PlaySound(12);
					if (num2 > 0)
					{
						if (ActivePlayer.BuyItem(num2))
						{
							Main.PlaySound(2, -1, -1, 4);
							ActivePlayer.HealEffect(ActivePlayer.StatLifeMax - ActivePlayer.statLife);
							if ((float)ActivePlayer.statLife < (float)ActivePlayer.StatLifeMax * 0.25f)
							{
								npcChatText = Lang.NPCDialog(ActivePlayer, 227);
							}
							else if ((float)ActivePlayer.statLife < (float)ActivePlayer.StatLifeMax * 0.5f)
							{
								npcChatText = Lang.NPCDialog(ActivePlayer, 228);
							}
							else if ((float)ActivePlayer.statLife < (float)ActivePlayer.StatLifeMax * 0.75f)
							{
								npcChatText = Lang.NPCDialog(ActivePlayer, 229);
							}
							else
							{
								npcChatText = Lang.NPCDialog(ActivePlayer, 230);
							}
							ActivePlayer.statLife = ActivePlayer.StatLifeMax;
							for (int j = 0; j < 10; j++)
							{
								if (ActivePlayer.buff[j].IsHealable())
								{
									j = ActivePlayer.DelBuff(j);
								}
							}
						}
						else
						{
							switch (Main.Rand.Next(3))
							{
							case 0:
								npcChatText = Lang.NPCDialog(ActivePlayer, 52);
								break;
							case 1:
								npcChatText = Lang.NPCDialog(ActivePlayer, 53);
								break;
							default:
								npcChatText = Lang.NPCDialog(ActivePlayer, 54);
								break;
							}
						}
					}
					else
					{
						switch (Main.Rand.Next(3))
						{
						case 0:
							npcChatText = Lang.NPCDialog(ActivePlayer, 55);
							break;
						case 1:
							npcChatText = Lang.NPCDialog(ActivePlayer, 56);
							break;
						default:
							npcChatText = Lang.NPCDialog(ActivePlayer, 57);
							break;
						}
					}
				}
			}
			else if (npcChatSelectedItem == 1)
			{
				ActivePlayer.TalkNPC = -1;
				ActivePlayer.sign = -1;
				editSign = false;
				npcChatText = null;
				Main.PlaySound(11);
			}
			else if (ActivePlayer.TalkNPC >= 0)
			{
				if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 20)
				{
					Main.PlaySound(12);
					npcChatText = Lang.DryadEvilGood();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 22)
				{
					npcChatText = null;
					Main.PlaySound(12);
					CraftGuide = true;
					GuideItem.Init();
					OpenInventory();
				}
				else if (Main.NPCSet[ActivePlayer.TalkNPC].Type == 107)
				{
					npcChatText = null;
					Main.PlaySound(12);
					reforge = true;
					OpenInventory();
				}
			}
			ClearButtonTriggers();
		}
	}

	/// <summary>Chat option rectangles, recorded while drawing so a click lands on the word.</summary>
	private readonly Rectangle[] npcChatOptionRects = new Rectangle[3];

	private readonly bool[] npcChatOptionValid = new bool[3];

	/// <summary>
	/// Crafting recipe cells, recorded while drawing.
	///
	/// The recipe grid is not a plain grid: rows are subcategories, each row scrolls horizontally
	/// on its own, and the whole list animates when the cursor moves. So the cell a click lands on
	/// cannot be computed from the pointer - it is recorded where the cell was actually drawn, and
	/// only while no animation is in flight.
	/// </summary>
	private readonly Rectangle[] craftSlotRects = new Rectangle[48];

	private readonly sbyte[] craftSlotRow = new sbyte[48];

	private readonly sbyte[] craftSlotIndex = new sbyte[48];

	private readonly bool[] craftSlotValid = new bool[48];

	/// <summary>How many cells were recorded this draw.</summary>
	private int craftSlotCount;

	/// <summary>Ingredient cells of the selected recipe, recorded while drawing.</summary>
	private readonly Rectangle[] craftIngredientRects = new Rectangle[12];

	private readonly sbyte[] craftIngredientCol = new sbyte[12];

	private readonly sbyte[] craftIngredientRow = new sbyte[12];

	private readonly bool[] craftIngredientValid = new bool[12];

	/// <summary>Which chat option the pointer is over, or -1.</summary>
	private int NpcChatOptionUnderPointer()
	{
		if (!Terraria.Input.KeyboardMouseInput.Enabled || !Terraria.Input.KeyboardMouseInput.CursorOverride)
		{
			return -1;
		}
		// Straight from the input layer: UpdateNpcChat runs from Player.Update, which may be
		// before UI.Update, so the UI's own MouseX can still be one frame behind.
		int num = Terraria.Input.KeyboardMouseInput.MouseX;
		int num2 = Terraria.Input.KeyboardMouseInput.MouseY;
		for (int i = 0; i < npcChatOptionRects.Length; i++)
		{
			if (npcChatOptionValid[i] && npcChatOptionRects[i].Contains(num, num2))
			{
				return i;
			}
		}
		return -1;
	}

	private void DrawNpcChat()
	{
		string text = npcChatText.GetString();
		int num = 500;
		int wrapWidth = 470;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num = (int)((float)num * 1.15f);
			wrapWidth = 548;
			break;
		case ScreenHeights.FHD:
			num = (int)((float)num * 1.534f);
			wrapWidth = 705;
			break;
		}
		if (text != npcCompiledChatText)
		{
			npcCompiledChatText = text;
			npcChatCompiledText = new CompiledText(text, wrapWidth, BoldSmallTextStyle);
		}
		int num2 = (MouseTextBrightness * 2 + 255) / 3;
		int num3 = DrawDialog(textColor: new Color(num2, num2, num2, num2), pos: new Vector2(CurrentView.ViewWidth - num >> 1, 100f * Main.ScreenMultiplier), backColor: new Color(200, 200, 200, 200), ct: npcChatCompiledText);
		num2 = MouseTextBrightness;
		int num4 = 180 + (CurrentView.ViewWidth - 800 >> 1);
		int num5 = 128 + num3;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num4 = 370;
			num5 = 150 + num3;
			break;
		case ScreenHeights.FHD:
			num4 = 602;
			num5 = 215 + num3;
			break;
		}
		Vector2 pivot = default;
		// Reset first: the option count changes with the NPC, and a stale rectangle would let a
		// click hit a word that is no longer on screen.
		for (int i = 0; i < npcChatOptionValid.Length; i++)
		{
			npcChatOptionValid[i] = false;
		}
		// A word is a small target, and the option is meant to be aimed at, not spelled. The
		// gap between two options is 30px (more at higher presets), so a little padding cannot
		// make them overlap; it only stops the pointer from having to land on a letter.
		int padX = (int)(8f * Main.ScreenMultiplier);
		int padY = (int)(6f * Main.ScreenMultiplier);
		if (focusText != null)
		{
			pivot = MeasureString(BoldSmallFont, focusText);
			pivot.X *= 0.5f;
			pivot.Y *= 0.5f;
			DrawStringScaled(BoldSmallFont, focusText, new Vector2((float)num4 + pivot.X, (float)num5 + pivot.Y), focusColor, pivot, (npcChatSelectedItem == 0) ? 1.1f : 0.9f);
			npcChatOptionRects[0] = new Rectangle(num4 - padX, num5 - padY, (int)(pivot.X * 2f) + padX * 2, (int)(pivot.Y * 2f) + padY * 2);
			npcChatOptionValid[0] = true;
		}
		string text2 = Lang.InterfaceText[52];
		Color c = new Color(num2, (int)((double)num2 * 0.9090909361839294), num2 >> 1, num2);
		float num6 = 30f;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num6 *= 1.15f;
			break;
		case ScreenHeights.FHD:
			num6 *= 1.5f;
			break;
		}
		num4 = num4 + (int)(pivot.X * 2f) + (int)num6;
		Vector2 pivot2 = MeasureString(BoldSmallFont, text2);
		pivot2.X *= 0.5f;
		pivot2.Y *= 0.5f;
		DrawStringScaled(BoldSmallFont, text2, new Vector2((float)num4 + pivot2.X, (float)num5 + pivot2.Y), c, pivot2, (npcChatSelectedItem == 1) ? 1.1f : 0.9f);
		npcChatOptionRects[1] = new Rectangle(num4 - padX, num5 - padY, (int)(pivot2.X * 2f) + padX * 2, (int)(pivot2.Y * 2f) + padY * 2);
		npcChatOptionValid[1] = true;
		if (focusText3 != null)
		{
			num4 = num4 + (int)(pivot2.X * 2f) + 30;
			Vector2 pivot3 = MeasureString(BoldSmallFont, focusText3);
			pivot3.X *= 0.5f;
			pivot3.Y *= 0.5f;
			DrawStringScaled(BoldSmallFont, focusText3, new Vector2((float)num4 + pivot3.X, (float)num5 + pivot3.Y), c, pivot3, (npcChatSelectedItem == 2) ? 1.1f : 0.9f);
			npcChatOptionRects[2] = new Rectangle(num4 - padX, num5 - padY, (int)(pivot3.X * 2f) + padX * 2, (int)(pivot3.Y * 2f) + padY * 2);
			npcChatOptionValid[2] = true;
		}
	}

	private void Reforge(int slot, bool isArmor = false)
	{
		if (toolTip.SetPrefix(-3) && ActivePlayer.BuyItem(toolTip.Value))
		{
			int prefixType = toolTip.PrefixType;
			toolTip.NetDefaults(toolTip.NetID);
			do
			{
				toolTip.SetPrefix(-2);
			}
			while (toolTip.PrefixType == prefixType);
			toolTip.Position.X = ActivePlayer.XYWH.X + 10 - (toolTip.Width >> 1);
			toolTip.Position.Y = ActivePlayer.XYWH.Y + 21 - (toolTip.Height >> 1);
			if (isArmor)
			{
				ref Item reference = ref ActivePlayer.armor[slot];
				reference = toolTip;
			}
			else
			{
				ref Item reference2 = ref ActivePlayer.Inventory[slot];
				reference2 = toolTip;
			}
			Main.PlaySound(2, ActivePlayer.XYWH.X, ActivePlayer.XYWH.Y, 37);
		}
	}

	private void CraftingGuide()
	{
		if (toolTip.Type <= 0 || !toolTip.IsMaterial)
		{
			return;
		}
		GuideItem = toolTip;
		ActiveInvSection = InventorySection.CRAFTING;
		craftingCategory = Recipe.Category.MISC;
		for (int i = 0; i < 6; i++)
		{
			NextCraftingCategory();
			if (CurrentRecipeCategory.Count > 0)
			{
				break;
			}
		}
	}

	private bool IsSlotAssignedToQuickAccess(int slot)
	{
		if (quickAccessUp != slot && quickAccessDown != slot && quickAccessLeft != slot)
		{
			return quickAccessRight == slot;
		}
		return true;
	}

	private void UpdateInventory()
	{
		if (inventoryItemX == 9 && inventoryItemY == 6)
		{
			if (!IsButtonTriggered(Buttons.A))
			{
				return;
			}
			if (mouseItem.Type != 0)
			{
				trashItem.Init();
			}
			Item item = mouseItem;
			mouseItem = trashItem;
			trashItem = item;
			mouseItemSrcSection = InventorySection.ITEMS;
			mouseItemSrcX = inventoryItemX;
			mouseItemSrcY = inventoryItemY;
			if (trashItem.Type == 0 || trashItem.Stack < 1)
			{
				trashItem.Init();
			}
			if (mouseItem.NetID == trashItem.NetID && trashItem.Stack != trashItem.MaxStack && mouseItem.Stack != mouseItem.MaxStack)
			{
				if (mouseItem.Stack + trashItem.Stack <= mouseItem.MaxStack)
				{
					trashItem.Stack += mouseItem.Stack;
					mouseItem.Stack = 0;
				}
				else
				{
					short num = (short)(mouseItem.MaxStack - trashItem.Stack);
					trashItem.Stack += num;
					mouseItem.Stack -= num;
				}
			}
			if (mouseItem.Type == 0 || mouseItem.Stack < 1)
			{
				mouseItem.Init();
			}
			if (mouseItem.Type > 0 || trashItem.Type > 0)
			{
				Main.PlaySound(7);
			}
			return;
		}
		bool flag = true;
		int num2;
		switch (inventoryItemY)
		{
		case 4:
			num2 = 44 + inventoryItemX - 6;
			flag = mouseItem.CanBePlacedInAmmoSlot();
			break;
		case 5:
			num2 = 40 + inventoryItemX - 6;
			flag = mouseItem.CanBePlacedInCoinSlot();
			break;
		default:
			num2 = inventoryItemX + inventoryItemY * 10;
			break;
		}
		// Shift-click: move the whole stack to the open container, PC-style. Handled before the
		// normal click so one action does one thing - the click that transfers must not also pick
		// the stack up.
		if (Terraria.Input.KeyboardMouseInput.ShiftDown && mouseItem.Type == 0 && IsButtonTriggered(Buttons.A)
			&& QuickTransferToContainer(num2))
		{
			return;
		}
		if (num2 < 40 && mouseItem.Type == 0)
		{
			if (IsButtonTriggered(Buttons.DPadUp))
			{
				if (quickAccessUp == num2)
				{
					quickAccessUp = -1;
				}
				else
				{
					quickAccessUp = (sbyte)num2;
					if (quickAccessDown == num2)
					{
						quickAccessDown = -1;
					}
					else if (quickAccessLeft == num2)
					{
						quickAccessLeft = -1;
					}
					else if (quickAccessRight == num2)
					{
						quickAccessRight = -1;
					}
				}
				Main.PlaySound(7);
			}
			else if (IsButtonTriggered(Buttons.DPadDown))
			{
				if (quickAccessDown == num2)
				{
					quickAccessDown = -1;
				}
				else
				{
					quickAccessDown = (sbyte)num2;
					if (quickAccessUp == num2)
					{
						quickAccessUp = -1;
					}
					else if (quickAccessLeft == num2)
					{
						quickAccessLeft = -1;
					}
					else if (quickAccessRight == num2)
					{
						quickAccessRight = -1;
					}
				}
				Main.PlaySound(7);
			}
			else if (IsButtonTriggered(Buttons.DPadLeft))
			{
				if (quickAccessLeft == num2)
				{
					quickAccessLeft = -1;
				}
				else
				{
					quickAccessLeft = (sbyte)num2;
					if (quickAccessUp == num2)
					{
						quickAccessUp = -1;
					}
					else if (quickAccessDown == num2)
					{
						quickAccessDown = -1;
					}
					else if (quickAccessRight == num2)
					{
						quickAccessRight = -1;
					}
				}
				Main.PlaySound(7);
			}
			else if (IsButtonTriggered(Buttons.DPadRight))
			{
				if (quickAccessRight == num2)
				{
					quickAccessRight = -1;
				}
				else
				{
					quickAccessRight = (sbyte)num2;
					if (quickAccessUp == num2)
					{
						quickAccessUp = -1;
					}
					else if (quickAccessDown == num2)
					{
						quickAccessDown = -1;
					}
					else if (quickAccessLeft == num2)
					{
						quickAccessLeft = -1;
					}
				}
				Main.PlaySound(7);
			}
		}
		if (ActivatePressed())
		{
			if (reforge)
			{
				Reforge(num2);
			}
			else if (CraftGuide)
			{
				CraftingGuide();
			}
			else
			{
				if (mouseItem.Type != 0 && (!flag || (ActivePlayer.SelectedItem == num2 && ActivePlayer.itemAnimation > 0)))
				{
					return;
				}
				Item item2 = mouseItem;
				mouseItem = ActivePlayer.Inventory[num2];
				ActivePlayer.Inventory[num2] = item2;
				if (ActivePlayer.Inventory[num2].Type == 0 || ActivePlayer.Inventory[num2].Stack < 1)
				{
					ActivePlayer.Inventory[num2].Init();
				}
				bool flag2 = false;
				if (mouseItem.NetID == ActivePlayer.Inventory[num2].NetID && ActivePlayer.Inventory[num2].Stack != ActivePlayer.Inventory[num2].MaxStack && mouseItem.Stack != mouseItem.MaxStack)
				{
					if (mouseItem.Stack + ActivePlayer.Inventory[num2].Stack <= mouseItem.MaxStack)
					{
						ActivePlayer.Inventory[num2].Stack += mouseItem.Stack;
						mouseItem.Init();
					}
					else
					{
						short num3 = (short)(mouseItem.MaxStack - ActivePlayer.Inventory[num2].Stack);
						ActivePlayer.Inventory[num2].Stack += num3;
						mouseItem.Stack -= num3;
						flag2 = true;
					}
				}
				if (mouseItem.Type > 0 && item2.Type > 0 && !flag2 && mouseItemSrcSection == InventorySection.ITEMS && mouseItemSrcX < 10 && mouseItemSrcY < 4)
				{
					int num4 = mouseItemSrcX + mouseItemSrcY * 10;
					if (ActivePlayer.Inventory[num4].Type == 0)
					{
						ref Item reference = ref ActivePlayer.Inventory[num4];
						reference = mouseItem;
						mouseItem.Init();
					}
					if (quickAccessUp == num4)
					{
						quickAccessUp = (sbyte)num2;
					}
					else if (quickAccessDown == num4)
					{
						quickAccessDown = (sbyte)num2;
					}
					else if (quickAccessLeft == num4)
					{
						quickAccessLeft = (sbyte)num2;
					}
					else if (quickAccessRight == num4)
					{
						quickAccessRight = (sbyte)num2;
					}
				}
				mouseItemSrcSection = InventorySection.ITEMS;
				mouseItemSrcX = inventoryItemX;
				mouseItemSrcY = inventoryItemY;
				if (mouseItem.Type > 0 || ActivePlayer.Inventory[num2].Type > 0)
				{
					Main.PlaySound(7);
				}
			}
		}
		else if (PadState.IsButtonDown(Buttons.RightTrigger))
		{
			if (gpPrevState.IsButtonUp(Buttons.RightTrigger))
			{
				if (ActivePlayer.Inventory[num2].Type >= 599 && ActivePlayer.Inventory[num2].Type <= 601)
				{
					Main.PlaySound(7);
					stackSplit = 30;
					int num5 = Main.Rand.Next(14);
					if (num5 == 0 && Main.InHardMode)
					{
						ActivePlayer.Inventory[num2].SetDefaults(602);
						return;
					}
					ActivePlayer.Inventory[num2].SetDefaults((num5 <= 7) ? 586 : 591);
					ActivePlayer.Inventory[num2].Stack = (short)Main.Rand.Next(20, 50);
				}
				else if (ActivePlayer.Inventory[num2].IsEquipable())
				{
					ref Item reference2 = ref ActivePlayer.Inventory[num2];
					reference2 = ActivePlayer.armorSwap(ref ActivePlayer.Inventory[num2]);
				}
			}
			else if (stackSplit <= 1 && ActivePlayer.Inventory[num2].MaxStack > 1 && ActivePlayer.Inventory[num2].Type > 0 && (mouseItem.NetID == ActivePlayer.Inventory[num2].NetID || mouseItem.Type == 0) && (mouseItem.Stack < mouseItem.MaxStack || mouseItem.Type == 0))
			{
				if (mouseItem.Type == 0)
				{
					mouseItem = ActivePlayer.Inventory[num2];
					mouseItem.Stack = 0;
					mouseItemSrcSection = InventorySection.ITEMS;
					mouseItemSrcX = inventoryItemX;
					mouseItemSrcY = inventoryItemY;
				}
				mouseItem.Stack++;
				ActivePlayer.Inventory[num2].Stack--;
				if (ActivePlayer.Inventory[num2].Stack <= 0)
				{
					ActivePlayer.Inventory[num2].Init();
				}
				Main.PlaySound(12);
				if (stackSplit == 0)
				{
					stackSplit = 15;
				}
				else
				{
					stackSplit = stackDelay;
				}
			}
		}
		else
		{
			if (mouseItem.Type != 0 || reforge || !IsButtonTriggered(Buttons.X) || ActivePlayer.Inventory[num2].Type <= 0)
			{
				return;
			}
			if (npcShop > 0 && !ActivePlayer.Inventory[num2].CanBePlacedInCoinSlot())
			{
				if (ActivePlayer.SellItem(ActivePlayer.Inventory[num2].Value, ActivePlayer.Inventory[num2].Stack))
				{
					Main.Shop[npcShop].AddShop(ref ActivePlayer.Inventory[num2]);
					ActivePlayer.Inventory[num2].Init();
					Main.PlaySound(18);
				}
				else if (ActivePlayer.Inventory[num2].Value == 0)
				{
					Main.Shop[npcShop].AddShop(ref ActivePlayer.Inventory[num2]);
					ActivePlayer.Inventory[num2].Init();
					Main.PlaySound(7);
				}
			}
			else
			{
				Main.PlaySound(7);
				trashItem = ActivePlayer.Inventory[num2];
				ActivePlayer.Inventory[num2].Init();
			}
		}
	}

	private void DrawInventory(int itemsSectionX, int itemsSectionY)
	{
		// 10 columns by 4 rows of item slots; the ammo/coin rows and the trash slot sit below.
		invMouseCell = 52.15f * Main.ScreenMultiplier;
		invMouseGrid = new Rectangle(itemsSectionX, itemsSectionY,
			InventoryGrid.CellStart(10, invMouseCell), InventoryGrid.CellStart(4, invMouseCell));
		invMouseLayoutValid = true;
		// The secondary slots are drawn from their own formulas; the rectangles are built from the
		// same numbers here so the pointer can reach them at all. They were previously unreachable
		// by mouse: only the 10x4 grid was hit-tested, so clicking a coin or the trash did nothing.
		invMouseTrash = new Rectangle(itemsSectionX + (int)(469f * Main.ScreenMultiplier),
			itemsSectionY + (int)(312f * Main.ScreenMultiplier), InventoryGrid.CellStart(1, invMouseCell), InventoryGrid.CellStart(1, invMouseCell));
		invMouseTrashValid = true;
		invMouseCoin = new Rectangle(itemsSectionX + InventoryGrid.CellStart(6, invMouseCell),
			itemsSectionY + (int)(260f * Main.ScreenMultiplier), InventoryGrid.CellStart(4, invMouseCell), InventoryGrid.CellStart(1, invMouseCell));
		invMouseCoinValid = true;
		invMouseAmmo = new Rectangle(itemsSectionX + InventoryGrid.CellStart(6, invMouseCell),
			itemsSectionY + (int)(208f * Main.ScreenMultiplier), InventoryGrid.CellStart(4, invMouseCell), InventoryGrid.CellStart(1, invMouseCell));
		invMouseAmmoValid = true;
		Vector2 pos = default;
		Color color = new Color(invAlpha, invAlpha, invAlpha, invAlpha);
		inventoryScale = 149f / 160f;
		inventoryScale *= Main.ScreenMultiplier;
		int num = (int)(469f * Main.ScreenMultiplier);
		DrawStringRC(SmallFont, Lang.InterfaceText[3], itemsSectionX + num, (int)((float)itemsSectionY + 338f * Main.ScreenMultiplier), Color.White);
		int num2 = (int)(469f * Main.ScreenMultiplier + (float)itemsSectionX);
		int num3 = (int)(312f * Main.ScreenMultiplier + (float)itemsSectionY);
		if (inventoryItemX == 9 && inventoryItemY == 6)
		{
			DrawInventoryCursor(num2, num3, inventoryScale);
			toolTip = trashItem;
		}
		else
		{
			SpriteSheet<_sheetSprites>.DrawTL(446, num2, num3, color, inventoryScale);
		}
		if (trashItem.Type == 0 || trashItem.Stack == 0)
		{
			pos.X = (float)num2 + 26f * inventoryScale;
			pos.Y = (float)num3 + 26f * inventoryScale;
			SpriteSheet<_sheetSprites>.DrawScaled(1482, ref pos, new Color(100, 100, 100, 100), inventoryScale);
		}
		else
		{
			DrawInventoryItem(ref trashItem, num2, num3, Color.White, StackType.INVENTORY);
		}
		for (int i = 0; i < 10; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				int x = itemsSectionX + InventoryGrid.CellStart(i, invMouseCell);
				int y = itemsSectionY + InventoryGrid.CellStart(j, invMouseCell);
				int num4 = i + j * 10;
				if (inventoryItemX == i && inventoryItemY == j)
				{
					toolTip = ActivePlayer.Inventory[num4];
					DrawInventoryCursor(x, y, inventoryScale);
				}
				else
				{
					Color c = color;
					int id;
					if (!IsSlotAssignedToQuickAccess(num4))
					{
						id = ((j == 0) ? 447 : 451);
					}
					else
					{
						id = 450;
						c = mouseTextColor;
					}
					SpriteSheet<_sheetSprites>.DrawTL(id, x, y, c, inventoryScale);
				}
				if (ActivePlayer.Inventory[num4].Type > 0 && ActivePlayer.Inventory[num4].Stack > 0)
				{
					StackType stackType;
					Color itemColor;
					if ((CraftGuide && !ActivePlayer.Inventory[num4].IsMaterial) || (reforge && !ActivePlayer.Inventory[num4].SetPrefix(-3)))
					{
						stackType = StackType.NONE;
						itemColor = DISABLED_COLOR;
					}
					else
					{
						stackType = StackType.INVENTORY;
						itemColor = new Color(255, 255, 255, 255);
					}
					DrawInventoryItem(ref ActivePlayer.Inventory[num4], x, y, itemColor, stackType);
				}
			}
		}
		int num5 = (int)(312f * Main.ScreenMultiplier);
		DrawStringRC(SmallFont, Lang.InterfaceText[26], itemsSectionX + num5, (int)((float)itemsSectionY + 286f * Main.ScreenMultiplier), Color.White);
		for (int k = 0; k < 4; k++)
		{
			int x2 = itemsSectionX + InventoryGrid.CellStart(6 + k, invMouseCell);
			int y2 = (int)(260f * Main.ScreenMultiplier + (float)itemsSectionY);
			int num6 = k + 40;
			if (inventoryItemY == 5 && inventoryItemX - 6 == k)
			{
				DrawInventoryCursor(x2, y2, inventoryScale);
				toolTip = ActivePlayer.Inventory[num6];
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawTL(451, x2, y2, color, inventoryScale);
			}
			if (ActivePlayer.Inventory[num6].Type > 0 && ActivePlayer.Inventory[num6].Stack > 0)
			{
				DrawInventoryItem(ref ActivePlayer.Inventory[num6], x2, y2, Color.White, StackType.INVENTORY);
			}
		}
		DrawStringRC(SmallFont, Lang.InterfaceText[27], itemsSectionX + num5, (int)((float)itemsSectionY + 234f * Main.ScreenMultiplier), Color.White);
		for (int l = 0; l < 4; l++)
		{
			int x3 = itemsSectionX + InventoryGrid.CellStart(6 + l, invMouseCell);
			int y3 = (int)(208f * Main.ScreenMultiplier + (float)itemsSectionY);
			int num7 = l + 40 + 4;
			if (ActiveInvSection == InventorySection.ITEMS && inventoryItemY == 4 && inventoryItemX - 6 == l)
			{
				DrawInventoryCursor(x3, y3, inventoryScale);
				toolTip = ActivePlayer.Inventory[num7];
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawTL(451, x3, y3, color, inventoryScale);
			}
			if (ActivePlayer.Inventory[num7].Type > 0 && ActivePlayer.Inventory[num7].Stack > 0)
			{
				StackType stackType2;
				Color itemColor2;
				if ((CraftGuide && !ActivePlayer.Inventory[num7].IsMaterial) || reforge)
				{
					stackType2 = StackType.NONE;
					itemColor2 = DISABLED_COLOR;
				}
				else
				{
					stackType2 = StackType.INVENTORY;
					itemColor2 = new Color(255, 255, 255, 255);
				}
				DrawInventoryItem(ref ActivePlayer.Inventory[num7], x3, y3, itemColor2, stackType2);
			}
		}
		DrawQuickAccess(-1, (int)((float)itemsSectionX + 26f * Main.ScreenMultiplier), (int)((float)itemsSectionY + 234f * Main.ScreenMultiplier), 255, StackType.INVENTORY);
		UpdateToolTipText(null);
		int num8 = (int)(8f * Main.ScreenMultiplier);
		DrawToolTip(itemsSectionX + INVENTORY_W - TOOLTIP_W - num8, itemsSectionY + num8, (int)(344f * Main.ScreenMultiplier));
		DrawControlsInventory();
	}

	public int UpdateQuickAccess()
	{
		int result = -1;
		int num = quickAccessUp;
		if (num >= 0)
		{
			if (ActivePlayer.Inventory[num].Type == 0)
			{
				quickAccessUp = -1;
			}
			else if (PadState.DPad.Up == ButtonState.Pressed && gpPrevState.DPad.Up == ButtonState.Released)
			{
				result = num;
			}
		}
		num = quickAccessDown;
		if (num >= 0)
		{
			if (ActivePlayer.Inventory[num].Type == 0)
			{
				quickAccessDown = -1;
			}
			else if (PadState.DPad.Down == ButtonState.Pressed && gpPrevState.DPad.Down == ButtonState.Released)
			{
				result = num;
			}
		}
		num = quickAccessLeft;
		if (num >= 0)
		{
			if (ActivePlayer.Inventory[num].Type == 0)
			{
				quickAccessLeft = -1;
			}
			else if (PadState.DPad.Left == ButtonState.Pressed && gpPrevState.DPad.Left == ButtonState.Released)
			{
				result = num;
			}
		}
		num = quickAccessRight;
		if (num >= 0)
		{
			if (ActivePlayer.Inventory[num].Type == 0)
			{
				quickAccessRight = -1;
			}
			else if (PadState.DPad.Right == ButtonState.Pressed && gpPrevState.DPad.Right == ButtonState.Released)
			{
				result = num;
			}
		}
		return result;
	}

	private void DrawQuickAccess(int selectedItem, int x, int y, int alpha, StackType stackType)
	{
		Color color = new Color(alpha, alpha, alpha, alpha);
		Color color2 = new Color(0, 0, 0, alpha >> 1);
		alpha = alpha * MouseTextBrightness >> 8;
		Color c = new Color(alpha, alpha, alpha, alpha);
		int num = 56;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num = 75;
			break;
		case ScreenHeights.FHD:
			num = 112;
			break;
		}
		int num2 = num;
		Main.SpriteBatch.Draw(dpadTexture, new Vector2(x + num, y + num2), new Rectangle(0, 0, 112, 112), color, 0f, new Vector2(56f, 56f), Main.ScreenMultiplier, SpriteEffects.None, 0f);
		inventoryScale = Main.ScreenMultiplier;
		int num3 = quickAccessUp;
		int num4 = (int)(32f * Main.ScreenMultiplier);
		int num5 = (int)(30f * Main.ScreenMultiplier);
		int num6 = (int)(70f * Main.ScreenMultiplier);
		int num7 = (int)(4f * Main.ScreenMultiplier);
		if (num3 >= 0)
		{
			if (selectedItem == num3)
			{
				SpriteSheet<_sheetSprites>.DrawTL(448, x + num4 + num7, y - num7, color2, Main.ScreenMultiplier);
				SpriteSheet<_sheetSprites>.DrawTL(448, x + num4, y - num7 * 2, c, Main.ScreenMultiplier);
			}
			if (ActivePlayer.Inventory[num3].Type > 0)
			{
				if (selectedItem != num3)
				{
					DrawInventoryItem(ref ActivePlayer.Inventory[num3], x + num4 + num7, y - num7, color2, stackType);
				}
				DrawInventoryItem(ref ActivePlayer.Inventory[num3], x + num4, y - num7 * 2, color, stackType);
			}
		}
		num3 = quickAccessDown;
		if (num3 >= 0)
		{
			if (selectedItem == num3)
			{
				SpriteSheet<_sheetSprites>.DrawTL(448, x + num4 + num7, y + num6 + num7, color2, Main.ScreenMultiplier);
				SpriteSheet<_sheetSprites>.DrawTL(448, x + num4, y + num6, c, Main.ScreenMultiplier);
			}
			if (ActivePlayer.Inventory[num3].Type > 0)
			{
				if (selectedItem != num3)
				{
					DrawInventoryItem(ref ActivePlayer.Inventory[num3], x + num4 + num7, y + num6 + num7, color2, stackType);
				}
				DrawInventoryItem(ref ActivePlayer.Inventory[num3], x + num4, y + num6, color, stackType);
			}
		}
		num3 = quickAccessLeft;
		if (num3 >= 0)
		{
			if (selectedItem == num3)
			{
				SpriteSheet<_sheetSprites>.DrawTL(448, x - num7, y + num5 + num7, color2, Main.ScreenMultiplier);
				SpriteSheet<_sheetSprites>.DrawTL(448, x - num7 * 2, y + num5, c, Main.ScreenMultiplier);
			}
			if (ActivePlayer.Inventory[num3].Type > 0)
			{
				if (selectedItem != num3)
				{
					DrawInventoryItem(ref ActivePlayer.Inventory[num3], x - num7, y + num5 + num7, color2, stackType);
				}
				DrawInventoryItem(ref ActivePlayer.Inventory[num3], x - num7 * 2, y + num5, color, stackType);
			}
		}
		num3 = quickAccessRight;
		if (num3 < 0)
		{
			return;
		}
		if (selectedItem == num3)
		{
			SpriteSheet<_sheetSprites>.DrawTL(448, x + num6 + num7, y + num5 + num7, color2, Main.ScreenMultiplier);
			SpriteSheet<_sheetSprites>.DrawTL(448, x + num6, y + num5, c, Main.ScreenMultiplier);
		}
		if (ActivePlayer.Inventory[num3].Type > 0)
		{
			if (selectedItem != num3)
			{
				DrawInventoryItem(ref ActivePlayer.Inventory[num3], x + num6 + num7, y + num5 + num7, color2, stackType);
			}
			DrawInventoryItem(ref ActivePlayer.Inventory[num3], x + num6, y + num5, color, stackType);
		}
	}

	/// <summary>
	/// Shift-click from the inventory into the open container (chest, piggy bank, safe). Returns
	/// true when something moved, so the click can be consumed.
	///
	/// The container is anything the inventory screen can show on its chest tab, which is why this
	/// goes through <see cref="ActivePlayer"/>'s fields and not through a tile lookup.
	/// </summary>
	private bool QuickTransferToContainer(int Slot)
	{
		int playerChest = ActivePlayer.PlayerChest;
		if (playerChest == -1 || Slot < 0 || Slot >= ActivePlayer.Inventory.Length)
		{
			return false;
		}
		Chest chest;
		switch (playerChest)
		{
		case -2:
			chest = ActivePlayer.bank;
			break;
		case -3:
			chest = ActivePlayer.safe;
			break;
		default:
			chest = ((playerChest >= 0 && playerChest < Main.ChestSet.Length) ? Main.ChestSet[playerChest] : null);
			break;
		}
		if (chest == null)
		{
			return false;
		}
		int num;
		if (!InventoryTransfer.Move(ActivePlayer.Inventory, Slot, chest.ItemSet, 20, out num))
		{
			return false;
		}
		Main.PlaySound(7);
		if (playerChest >= 0 && num >= 0)
		{
			NetMessage.CreateMessage2(32, playerChest, num);
			NetMessage.SendMessage();
		}
		return true;
	}

	/// <summary>Shift-click from the open container back into the inventory.</summary>
	private bool QuickTransferFromContainer(Chest Chest, int Slot, int PlayerChest)
	{
		if (Chest == null || Slot < 0 || Slot >= 20)
		{
			return false;
		}
		int num;
		if (!InventoryTransfer.Move(Chest.ItemSet, Slot, ActivePlayer.Inventory, ActivePlayer.Inventory.Length, out num))
		{
			return false;
		}
		Main.PlaySound(7);
		if (PlayerChest >= 0)
		{
			// The chest lost items: that is what the server has to hear about. The inventory side
			// syncs on its own, like every other inventory change in this screen.
			NetMessage.CreateMessage2(32, PlayerChest, Slot);
			NetMessage.SendMessage();
		}
		return true;
	}

	private void UpdateStorage()
	{
		if (inventoryChestX < 0)
		{
			// ActivatePressed rather than Buttons.A: the pointer sets the same cursor value the
			// stick does, and one gate for both keeps a click and a button press from drifting
			// apart. The buttons were unreachable by mouse only because nothing recorded where
			// they were drawn - see DrawStorage.
			if (!ActivatePressed())
			{
				return;
			}
			switch (inventoryChestY)
			{
			case 1:
				if (ActivePlayer.PlayerChest >= 0)
				{
					Main.ChestSet[ActivePlayer.PlayerChest].LootAll(ActivePlayer);
				}
				else if (ActivePlayer.PlayerChest == -3)
				{
					ActivePlayer.safe.LootAll(ActivePlayer);
				}
				else
				{
					ActivePlayer.bank.LootAll(ActivePlayer);
				}
				break;
			case 2:
				if (ActivePlayer.PlayerChest >= 0)
				{
					Main.ChestSet[ActivePlayer.PlayerChest].Deposit(ActivePlayer);
				}
				else if (ActivePlayer.PlayerChest == -3)
				{
					ActivePlayer.safe.Deposit(ActivePlayer);
				}
				else
				{
					ActivePlayer.bank.Deposit(ActivePlayer);
				}
				break;
			case 3:
				if (ActivePlayer.PlayerChest >= 0)
				{
					Main.ChestSet[ActivePlayer.PlayerChest].QuickStack(ActivePlayer);
				}
				else if (ActivePlayer.PlayerChest == -3)
				{
					ActivePlayer.safe.QuickStack(ActivePlayer);
				}
				else
				{
					ActivePlayer.bank.QuickStack(ActivePlayer);
				}
				break;
			}
			return;
		}
		int playerChest = ActivePlayer.PlayerChest;
		if (1 == 0)
		{
		}
		Chest chest = playerChest switch
		{
			-2 => ActivePlayer.bank, 
			-3 => ActivePlayer.safe, 
			_ => Main.ChestSet[playerChest], 
		};
		if (1 == 0)
		{
		}
		Chest chest2 = chest;
		int num = inventoryChestX + inventoryChestY * 5;
		// Shift-click on a chest slot: pull the stack into the inventory. Same one-action rule as
		// on the inventory side.
		if (Terraria.Input.KeyboardMouseInput.ShiftDown && mouseItem.Type == 0 && IsButtonTriggered(Buttons.A)
			&& QuickTransferFromContainer(chest2, num, playerChest))
		{
			return;
		}
		if (ActivatePressed())
		{
			if (ActivePlayer.SelectedItem == num && ActivePlayer.itemAnimation > 0)
			{
				return;
			}
			Item item = mouseItem;
			mouseItem = chest2.ItemSet[num];
			mouseItemSrcSection = InventorySection.CHEST;
			mouseItemSrcX = inventoryChestX;
			mouseItemSrcY = inventoryChestY;
			chest2.ItemSet[num] = item;
			if (chest2.ItemSet[num].Type == 0 || chest2.ItemSet[num].Stack < 1)
			{
				chest2.ItemSet[num].Init();
			}
			if (mouseItem.NetID == chest2.ItemSet[num].NetID && chest2.ItemSet[num].Stack != chest2.ItemSet[num].MaxStack && mouseItem.Stack != mouseItem.MaxStack)
			{
				if (mouseItem.Stack + chest2.ItemSet[num].Stack <= mouseItem.MaxStack)
				{
					chest2.ItemSet[num].Stack += mouseItem.Stack;
					mouseItem.Stack = 0;
				}
				else
				{
					short num2 = (short)(mouseItem.MaxStack - chest2.ItemSet[num].Stack);
					chest2.ItemSet[num].Stack += num2;
					mouseItem.Stack -= num2;
				}
			}
			if (mouseItem.Type == 0 || mouseItem.Stack < 1)
			{
				mouseItem.Init();
			}
			if (mouseItem.Type > 0 || chest2.ItemSet[num].Type > 0)
			{
				Main.PlaySound(7);
			}
			if (playerChest >= 0)
			{
				NetMessage.CreateMessage2(32, playerChest, num);
				NetMessage.SendMessage();
			}
		}
		else if (IsButtonTriggered(Buttons.RightTrigger) && chest2.ItemSet[num].IsEquipable())
		{
			ref Item reference = ref chest2.ItemSet[num];
			reference = ActivePlayer.armorSwap(ref chest2.ItemSet[num]);
			if (playerChest >= 0)
			{
				NetMessage.CreateMessage2(32, playerChest, num);
				NetMessage.SendMessage();
			}
		}
		else if (stackSplit <= 1 && PadState.IsButtonDown(Buttons.RightTrigger) && chest2.ItemSet[num].MaxStack > 1 && (mouseItem.NetID == chest2.ItemSet[num].NetID || mouseItem.Type == 0) && (mouseItem.Stack < mouseItem.MaxStack || mouseItem.Type == 0))
		{
			if (mouseItem.Type == 0)
			{
				mouseItem = chest2.ItemSet[num];
				mouseItem.Stack = 0;
				mouseItemSrcSection = InventorySection.CHEST;
				mouseItemSrcX = inventoryChestX;
				mouseItemSrcY = inventoryChestY;
			}
			mouseItem.Stack++;
			chest2.ItemSet[num].Stack--;
			if (chest2.ItemSet[num].Stack <= 0)
			{
				chest2.ItemSet[num].Init();
			}
			Main.PlaySound(12);
			if (stackSplit == 0)
			{
				stackSplit = 15;
			}
			else
			{
				stackSplit = stackDelay;
			}
			if (playerChest >= 0)
			{
				NetMessage.CreateMessage2(32, playerChest, num);
				NetMessage.SendMessage();
			}
		}
	}

	private void DrawStorage(int INVENTORY_X, int INVENTORY_Y)
	{
		int playerChest = ActivePlayer.PlayerChest;
		Chest chest;
		switch (playerChest)
		{
		case -1:
			return;
		case -2:
			chest = ActivePlayer.bank;
			break;
		case -3:
			chest = ActivePlayer.safe;
			break;
		default:
			chest = Main.ChestSet[playerChest];
			if (chest == null)
			{
				return;
			}
			break;
		}
		Color c = new Color(invAlpha, invAlpha, invAlpha, invAlpha);
		inventoryScale = Main.ScreenMultiplier;
		int num = (int)(56f * inventoryScale);
		int x = num * 2 + INVENTORY_X - num;
		int num2 = num + INVENTORY_Y + num;
		// The three buttons are recorded the same way the slots are: from the numbers they are
		// drawn with, so the pointer and the picture cannot disagree.
		for (int i = 0; i < chestButtonValid.Length; i++)
		{
			chestButtonValid[i] = false;
		}
		chestButtonRects[1] = new Rectangle(x, num2, num, num);
		chestButtonValid[1] = true;
		int id = ((inventoryChestX < 0 && inventoryChestY == 1) ? 448 : 451);
		SpriteSheet<_sheetSprites>.DrawTL(id, x, num2, c, inventoryScale);
		id = 1085;
		DrawInventoryItem(id, x, num2, Color.White);
		num2 += num;
		chestButtonRects[2] = new Rectangle(x, num2, num, num);
		chestButtonValid[2] = true;
		id = ((inventoryChestX < 0 && inventoryChestY == 2) ? 448 : 451);
		SpriteSheet<_sheetSprites>.DrawTL(id, x, num2, c, inventoryScale);
		id = 213;
		DrawInventoryItem(id, x, num2, Color.White);
		num2 += num;
		chestButtonRects[3] = new Rectangle(x, num2, num, num);
		chestButtonValid[3] = true;
		id = ((inventoryChestX < 0 && inventoryChestY == 3) ? 448 : 451);
		SpriteSheet<_sheetSprites>.DrawTL(id, x, num2, c, inventoryScale);
		id = 1469;
		DrawInventoryItem(id, x, num2, Color.White);
		if (inventoryChestX < 0)
		{
			toolTip.Init();
		}
		for (int i = 0; i < chestSlotRects.Length; i++)
		{
			chestSlotValid[i] = false;
		}
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				x = num * 2 + INVENTORY_X + i * num;
				num2 = num + INVENTORY_Y + j * num;
				int num3 = i + j * 5;
				chestSlotRects[num3] = new Rectangle(x, num2, num, num);
				chestSlotX[num3] = (sbyte)i;
				chestSlotY[num3] = (sbyte)j;
				chestSlotValid[num3] = true;
				SpriteSheet<_sheetSprites>.DrawTL(444, x, num2, c, inventoryScale);
				if (inventoryChestX == i && inventoryChestY == j)
				{
					DrawInventoryCursor(x, num2, inventoryScale);
					toolTip = chest.ItemSet[num3];
				}
				if (chest.ItemSet[num3].Type > 0 && chest.ItemSet[num3].Stack > 0)
				{
					Color white = Color.White;
					DrawInventoryItem(ref chest.ItemSet[num3], x, num2, white, StackType.INVENTORY);
				}
			}
		}
		UpdateToolTipText(null);
		DrawToolTip(INVENTORY_X + INVENTORY_W - TOOLTIP_W - (int)(8f * Main.ScreenMultiplier), INVENTORY_Y + (int)(8f * Main.ScreenMultiplier), (int)(344f * Main.ScreenMultiplier));
		DrawControlsInventory();
	}

	private void UpdateShop()
	{
		int num = inventoryChestX + inventoryChestY * 5;
		if (ActivatePressed())
		{
			if (mouseItem.Type == 0)
			{
				if ((ActivePlayer.SelectedItem == num && ActivePlayer.itemAnimation > 0) || !ActivePlayer.BuyItem(Main.Shop[npcShop].ItemSet[num].Value))
				{
					return;
				}
				if (Main.Shop[npcShop].ItemSet[num].OnlyBuyOnce)
				{
					int prefixType = Main.Shop[npcShop].ItemSet[num].PrefixType;
					mouseItem.NetDefaults(Main.Shop[npcShop].ItemSet[num].NetID);
					mouseItem.SetPrefix(prefixType);
				}
				else
				{
					mouseItem.NetDefaults(Main.Shop[npcShop].ItemSet[num].NetID);
					mouseItem.SetPrefix(-1);
				}
				mouseItem.Position.X = ActivePlayer.Position.X + 10f - (float)(mouseItem.Width >> 1);
				mouseItem.Position.Y = ActivePlayer.Position.Y + 21f - (float)(mouseItem.Height >> 1);
				if (Main.Shop[npcShop].ItemSet[num].OnlyBuyOnce)
				{
					Main.Shop[npcShop].ItemSet[num].Stack--;
					if (Main.Shop[npcShop].ItemSet[num].Stack <= 0)
					{
						Main.Shop[npcShop].ItemSet[num].Init();
					}
				}
				Main.PlaySound(18);
			}
			else if (Main.Shop[npcShop].ItemSet[num].Type == 0)
			{
				if (ActivePlayer.SellItem(mouseItem.Value, mouseItem.Stack))
				{
					Main.Shop[npcShop].AddShop(ref mouseItem);
					mouseItem.Stack = 0;
					mouseItem.Type = 0;
					Main.PlaySound(18);
				}
				else if (mouseItem.Value == 0)
				{
					Main.Shop[npcShop].AddShop(ref mouseItem);
					mouseItem.Stack = 0;
					mouseItem.Type = 0;
					Main.PlaySound(7);
				}
			}
		}
		else
		{
			if (stackSplit > 1 || !PadState.IsButtonDown(Buttons.RightTrigger) || (mouseItem.NetID != Main.Shop[npcShop].ItemSet[num].NetID && mouseItem.Type != 0) || (mouseItem.Stack >= mouseItem.MaxStack && mouseItem.Type != 0) || !ActivePlayer.BuyItem(Main.Shop[npcShop].ItemSet[num].Value))
			{
				return;
			}
			Main.PlaySound(18);
			if (mouseItem.Type == 0)
			{
				mouseItem.NetDefaults(Main.Shop[npcShop].ItemSet[num].NetID);
				mouseItem.Stack = 0;
			}
			mouseItem.Stack++;
			if (stackSplit == 0)
			{
				stackSplit = 15;
			}
			else
			{
				stackSplit = stackDelay;
			}
			if (Main.Shop[npcShop].ItemSet[num].OnlyBuyOnce)
			{
				Main.Shop[npcShop].ItemSet[num].Stack--;
				if (Main.Shop[npcShop].ItemSet[num].Stack <= 0)
				{
					Main.Shop[npcShop].ItemSet[num].Init();
				}
			}
		}
	}

	private void DrawShop(int INVENTORY_X, int INVENTORY_Y)
	{
		Color c = new Color(invAlpha, invAlpha, invAlpha, invAlpha);
		inventoryScale = Main.ScreenMultiplier;
		for (int i = 0; i < shopSlotRects.Length; i++)
		{
			shopSlotValid[i] = false;
		}
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				int num = (int)(56f * Main.ScreenMultiplier);
				int x = num * 2 + INVENTORY_X + i * num;
				int y = num + INVENTORY_Y + j * num;
				int num2 = i + j * 5;
				shopSlotRects[num2] = new Rectangle(x, y, num, num);
				shopSlotValid[num2] = true;
				SpriteSheet<_sheetSprites>.DrawTL(445, x, y, c, inventoryScale);
				if (ActiveInvSection == InventorySection.CHEST && inventoryChestX == i && inventoryChestY == j)
				{
					DrawInventoryCursor(x, y, inventoryScale);
					toolTip = Main.Shop[npcShop].ItemSet[num2];
					toolTip.CanBuy = true;
				}
				if (Main.Shop[npcShop].ItemSet[num2].Type > 0 && Main.Shop[npcShop].ItemSet[num2].Stack > 0)
				{
					Color white = Color.White;
					DrawInventoryItem(ref Main.Shop[npcShop].ItemSet[num2], x, y, white, StackType.INVENTORY);
				}
			}
		}
		UpdateToolTipText(null);
		DrawToolTip(INVENTORY_X + INVENTORY_W - TOOLTIP_W - (int)(8f * Main.ScreenMultiplier), INVENTORY_Y + (int)(8f * Main.ScreenMultiplier), (int)(344f * Main.ScreenMultiplier));
		DrawControlsShop();
	}

	private void UpdateEquip()
	{
		if (inventoryEquipY == 0)
		{
			if (ActivatePressed())
			{
				int num = inventoryEquipX + inventoryBuffX;
				num %= 10;
				if (ActivePlayer.buff[num].Time > 0 && !ActivePlayer.buff[num].IsDebuff())
				{
					ActivePlayer.DelBuff(num);
					Main.PlaySound(12);
				}
			}
			return;
		}
		int num2;
		if (inventoryEquipY == 4)
		{
			num2 = 3 + inventoryEquipX;
		}
		else
		{
			num2 = ((inventoryEquipX != 0) ? (inventoryEquipY + 7) : (inventoryEquipY - 1));
		}
		if (ActivatePressed())
		{
			if (reforge)
			{
				Reforge(num2, isArmor: true);
			}
			else if (CraftGuide)
			{
				CraftingGuide();
			}
			else if (mouseItem.Type == 0 || (mouseItem.HeadSlot >= 0 && (num2 == 0 || num2 == 8)) || (mouseItem.BodySlot >= 0 && (num2 == 1 || num2 == 9)) || (mouseItem.LegSlot >= 0 && (num2 == 2 || num2 == 10)) || (mouseItem.IsAccessory && num2 > 2 && !AccCheck(ref mouseItem, num2)))
			{
				Item item = mouseItem;
				mouseItem = ActivePlayer.armor[num2];
				ActivePlayer.armor[num2] = item;
				mouseItemSrcSection = InventorySection.EQUIP;
				mouseItemSrcX = inventoryEquipX;
				mouseItemSrcY = inventoryEquipY;
				if (ActivePlayer.armor[num2].Type == 0 || ActivePlayer.armor[num2].Stack < 1)
				{
					ActivePlayer.armor[num2].Init();
				}
				if (mouseItem.Type > 0 || ActivePlayer.armor[num2].Type > 0)
				{
					Main.PlaySound(7);
				}
			}
		}
		else if (IsButtonTriggered(Buttons.RightTrigger) && ActivePlayer.armor[num2].IsEquipable() && !reforge)
		{
			ref Item reference = ref ActivePlayer.armor[num2];
			reference = ActivePlayer.armorSwap(ref ActivePlayer.armor[num2]);
		}
	}

	private void DrawEquip(int INVENTORY_X, int INVENTORY_Y)
	{
		Color c = new Color(invAlpha, invAlpha, invAlpha, invAlpha);
		Color c2 = new Color(invAlpha >> 1, invAlpha, invAlpha >> 1, invAlpha);
		inventoryScale = Main.ScreenMultiplier;
		int num = (int)(56f * Main.ScreenMultiplier);
		int num2 = INVENTORY_X + num * 2;
		int y = INVENTORY_Y;
		Rectangle rect = new Rectangle
		{
			Y = y,
			Width = (int)(16f * Main.ScreenMultiplier),
			Height = num
		};
		if (inventoryBuffX > 0)
		{
			rect.X = num2 - (int)(16f * Main.ScreenMultiplier);
			SpriteSheet<_sheetSprites>.DrawCentered(136, ref rect, SpriteEffects.FlipHorizontally);
		}
		if (inventoryBuffX < 5)
		{
			rect.X = num2 + (int)(280f * Main.ScreenMultiplier);
			SpriteSheet<_sheetSprites>.DrawCentered(136, ref rect);
		}
		string extraInfo = null;
		for (int i = 0; i < equipSlotRects.Length; i++)
		{
			equipSlotValid[i] = false;
		}
		for (int i = 0; i < 5; i++)
		{
			int num3 = ActivePlayer.buff[inventoryBuffX + i].Type;
			equipSlotRects[i] = new Rectangle(num2, y, num, num);
			equipSlotX[i] = (sbyte)i;
			equipSlotY[i] = 0;
			equipSlotValid[i] = true;
			if (inventoryEquipY == 0 && inventoryEquipX == i)
			{
				DrawInventoryCursor(num2, y, inventoryScale);
				toolTip.Init();
				extraInfo = ((!Buff.IsDebuff(num3)) ? "<f c='#C0FFC0'>" : "<f c='#FFC0C0'>");
				if (num3 == 40)
				{
					num3 += ActivePlayer.pet;
				}
				extraInfo += Buff.BuffName[num3];
				extraInfo += "</f>\n";
				extraInfo += Buff.BuffTip[num3];
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawTL(442, num2, y, c2, inventoryScale);
			}
			if (num3 > 0)
			{
				int num4 = 141 + num3;
				if (num3 == 40)
				{
					num4 += ActivePlayer.pet;
				}
				DrawInventoryItem(num4, num2, y, Color.White);
			}
			num2 += num;
		}
		num2 = INVENTORY_X + 2 * num;
		y = INVENTORY_Y + (int)(88f * Main.ScreenMultiplier);
		for (int j = 0; j < 3; j++)
		{
			equipSlotRects[5 + j] = new Rectangle(num2, y, num, num);
			equipSlotX[5 + j] = 0;
			equipSlotY[5 + j] = (sbyte)(j + 1);
			equipSlotValid[5 + j] = true;
			if (inventoryEquipX == 0 && inventoryEquipY == j + 1)
			{
				DrawInventoryCursor(num2, y, inventoryScale);
				toolTip = ActivePlayer.armor[j];
				toolTip.WornArmor = true;
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawTL(442, num2, y, c, inventoryScale);
			}
			if (ActivePlayer.armor[j].Type > 0 && ActivePlayer.armor[j].Stack > 0)
			{
				DrawInventoryItem(itemColor: ((CraftGuide && !ActivePlayer.armor[j].IsMaterial) || (reforge && !ActivePlayer.armor[j].SetPrefix(-3))) ? DISABLED_COLOR : new Color(255, 255, 255, 255), item: ref ActivePlayer.armor[j], x: num2, y: y);
			}
			y += num;
		}
		y += (int)(32f * Main.ScreenMultiplier);
		for (int k = 3; k < 8; k++)
		{
			equipSlotRects[8 + k - 3] = new Rectangle(num2, y, num, num);
			equipSlotX[8 + k - 3] = (sbyte)(k - 3);
			equipSlotY[8 + k - 3] = 4;
			equipSlotValid[8 + k - 3] = true;
			if (inventoryEquipY == 4 && inventoryEquipX == k - 3)
			{
				DrawInventoryCursor(num2, y, inventoryScale);
				toolTip = ActivePlayer.armor[k];
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawTL(442, num2, y, c, inventoryScale);
			}
			if (ActivePlayer.armor[k].Type > 0 && ActivePlayer.armor[k].Stack > 0)
			{
				DrawInventoryItem(itemColor: ((CraftGuide && !ActivePlayer.armor[k].IsMaterial) || (reforge && !ActivePlayer.armor[k].SetPrefix(-3))) ? DISABLED_COLOR : new Color(255, 255, 255, 255), item: ref ActivePlayer.armor[k], x: num2, y: y);
			}
			num2 += num;
		}
		num2 -= num;
		y = INVENTORY_Y + (int)(88f * Main.ScreenMultiplier);
		for (int l = 8; l < 11; l++)
		{
			equipSlotRects[13 + l - 8] = new Rectangle(num2, y, num, num);
			equipSlotX[13 + l - 8] = 4;
			equipSlotY[13 + l - 8] = (sbyte)(l - 7);
			equipSlotValid[13 + l - 8] = true;
			if (inventoryEquipX == 4 && inventoryEquipY == l - 7)
			{
				DrawInventoryCursor(num2, y, inventoryScale);
				toolTip = ActivePlayer.armor[l];
				toolTip.IsSocial = true;
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawTL(442, num2, y, c, inventoryScale);
			}
			if (ActivePlayer.armor[l].Type > 0 && ActivePlayer.armor[l].Stack > 0)
			{
				DrawInventoryItem(itemColor: ((CraftGuide && !ActivePlayer.armor[l].IsMaterial) || (reforge && !ActivePlayer.armor[l].SetPrefix(-3))) ? DISABLED_COLOR : new Color(255, 255, 255, 255), item: ref ActivePlayer.armor[l], x: num2, y: y);
			}
			y += num;
		}
		int num5 = (int)(6f * Main.ScreenMultiplier);
		DrawPlayer(ActivePlayer, new Vector2(INVENTORY_X + num * 4, INVENTORY_Y + (int)(88f * Main.ScreenMultiplier)), 3.75f * Main.ScreenMultiplier);
		DrawStringCT(SmallFont, Lang.InterfaceText[6], INVENTORY_X + 2 * num + (int)(140f * Main.ScreenMultiplier), INVENTORY_Y + num - 2 * num5, Color.White);
		DrawStringCT(SmallFont, Lang.InterfaceText[45], INVENTORY_X + num * 2 + (int)(28f * Main.ScreenMultiplier), y - 2 * num5, Color.White);
		DrawStringCT(SmallFont, Lang.InterfaceText[11], num2 + (int)(28f * Main.ScreenMultiplier), y - num5 * 2, Color.White);
		DrawStringCT(BoldSmallFont, ((int)ActivePlayer.StatDefense).ToStringLookup() + Lang.InterfaceText[10], INVENTORY_X + num * 2 + (int)(140f * Main.ScreenMultiplier), y - num5, new Color(100, 255, 200, 255));
		UpdateToolTipText(extraInfo);
		DrawToolTip(INVENTORY_X + INVENTORY_W - TOOLTIP_W - (int)(8f * Main.ScreenMultiplier), INVENTORY_Y + (int)(8f * Main.ScreenMultiplier), (int)(344f * Main.ScreenMultiplier));
		DrawControlsInventory();
	}

	private void UpdateHousing()
	{
		int x = ActivePlayer.XYWH.X >> 4;
		int y = ActivePlayer.XYWH.Y >> 4;
		if (IsButtonTriggered(Buttons.X))
		{
			Main.PlaySound(12);
			if (WorldGen.MoveNPC(x, y, -1))
			{
				Main.NewText(Lang.InterfaceText[39], 255, 240, 20);
			}
		}
		else if (IsButtonTriggered(Buttons.Y))
		{
			Main.PlaySound(12);
			showNPCs = !showNPCs;
		}
		else if (ActivatePressed() && inventoryHousingNpc >= 0)
		{
			// ActivatePressed, not Buttons.A: the pointer selects the resident (the rectangles come
			// from the draw pass) but a click synthesises no A inside the inventory, so the action
			// was unreachable by mouse.
			Main.PlaySound(12);
			if (WorldGen.MoveNPC(x, y, inventoryHousingNpc))
			{
				WorldGen.moveRoom(x, y, inventoryHousingNpc);
				Main.PlaySound(12);
			}
		}
	}

	private void DrawHousing(int INVENTORY_X, int INVENTORY_Y)
	{
		Color c = new Color(invAlpha, invAlpha, invAlpha, invAlpha);
		inventoryScale = Main.ScreenMultiplier;
		string extraInfo = null;
		int num = INVENTORY_X;
		int num2 = INVENTORY_Y;
		for (int i = 0; i < housingSlotRects.Length; i++)
		{
			housingSlotValid[i] = false;
		}
		for (int i = 0; i < 11; i++)
		{
			if (i == 6)
			{
				num += (int)(240f * Main.ScreenMultiplier);
				num2 = INVENTORY_Y;
			}
			housingSlotRects[i] = new Rectangle(num, num2, (int)(52f * Main.ScreenMultiplier), (int)(56f * Main.ScreenMultiplier));
			housingSlotX[i] = (sbyte)(i / 6);
			housingSlotY[i] = (sbyte)(i % 6);
			housingSlotValid[i] = true;
			string text = null;
			NPC nPC = null;
			int num3 = -1;
			for (int j = 0; j < 196; j++)
			{
				nPC = Main.NPCSet[j];
				if (nPC.Active != 0 && i + 1 == nPC.GetHeadTextureID())
				{
					num3 = j;
					text = ((!nPC.HasName()) ? nPC.DisplayName : nPC.GetName());
					break;
				}
			}
			if (inventoryHousingX == i / 6 && inventoryHousingY == i % 6)
			{
				DrawInventoryCursor(num, num2, inventoryScale);
				inventoryHousingNpc = (short)num3;
				if (text != null)
				{
					extraInfo = text;
					if (i != 10 && Lang.LangOption <= 1)
					{
						extraInfo = extraInfo + " the " + nPC.TypeName;
					}
					extraInfo += "\n";
					extraInfo += Lang.InterfaceText[55 + i];
				}
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawTL(449, num, num2, c, inventoryScale);
			}
			if (num3 >= 0)
			{
				int itemTexId = i + 1256;
				DrawInventoryItem(itemTexId, num, num2, Color.White);
				if (!nPC.IsHomeless)
				{
					SpriteSheet<_sheetSprites>.DrawTL(437, num + (int)(44f * Main.ScreenMultiplier), num2 - (int)(8f * Main.ScreenMultiplier), Color.White, inventoryScale);
				}
				DrawStringLC(BoldSmallFont, text, num + (int)(60f * Main.ScreenMultiplier), num2 + (int)(30f * Main.ScreenMultiplier), Color.White);
			}
			num2 += (int)(60f * Main.ScreenMultiplier);
		}
		UpdateToolTipText(extraInfo);
		DrawToolTip(INVENTORY_X + INVENTORY_W - TOOLTIP_W - (int)(8f * Main.ScreenMultiplier), INVENTORY_Y + (int)(8f * Main.ScreenMultiplier), (int)(344f * Main.ScreenMultiplier));
		DrawControlsHousing();
	}

	private void DrawMouseItem()
	{
		if (mouseItem.Stack <= 0)
		{
			mouseItem.Init();
		}
		else if (mouseItem.Type > 0 && mouseItem.Stack > 0)
		{
			inventoryScale = cursorScale * Main.ScreenMultiplier;
			DrawInventoryItem(ref mouseItem, MouseX + 6, MouseY + 6, new Color(0, 0, 0, 128), StackType.INVENTORY);
			DrawInventoryItem(ref mouseItem, MouseX, MouseY, Color.White, StackType.INVENTORY);
		}
	}

	private void DrawInventoryMenu()
	{
		int num = CurrentView.SafeAreaOffsetLeft;
		int safeAreaOffsetTop = CurrentView.SafeAreaOffsetTop;
		if (CurrentView.ViewWidth > Main.ResolutionWidth)
		{
			num = CurrentView.ViewWidth - INVENTORY_W >> 1;
		}
		bool flag = ActivePlayer.PlayerChest != -1;
		bool flag2 = npcShop > 0;
		bool flag3 = flag | flag2;
		InventorySection activeInvSection = ActiveInvSection;
		if (1 == 0)
		{
		}
		int num2 = activeInvSection switch
		{
			InventorySection.CRAFTING => 441, 
			InventorySection.EQUIP => 442, 
			InventorySection.CHEST => flag2 ? 445 : 444, 
			InventorySection.HOUSING => 449, 
			_ => 451, 
		};
		if (1 == 0)
		{
		}
		int texId = num2;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			INVENTORY_H = USABLE_HEIGHT;
			break;
		case ScreenHeights.FHD:
			INVENTORY_H = USABLE_HEIGHT + 16;
			break;
		}
		Main.DrawRect(texId, new Rectangle(num, safeAreaOffsetTop, INVENTORY_W, INVENTORY_H), 96);
		Color c = default;
		int num3 = num + (INVENTORY_W - (int)((float)((flag3 ? 4 : 3) * 43 + 56) * Main.ScreenMultiplier)) / 2;
		for (int i = 0; i < 5; i++)
		{
			switch ((byte)i)
			{
			case 1:
				texId = 440;
				break;
			case 0:
				texId = 205;
				break;
			case 3:
				texId = 219;
				break;
			case 2:
			{
				if (flag2)
				{
					texId = Chest.GetShopOwnerHeadTextureId(npcShop);
					break;
				}
				if (!flag)
				{
					continue;
				}
				short playerChest = ActivePlayer.PlayerChest;
				if (1 == 0)
				{
				}
				num2 = playerChest switch
				{
					-2 => 538, 
					-3 => 797, 
					_ => 499, 
				};
				if (1 == 0)
				{
				}
				texId = num2;
				break;
			}
			default:
				texId = 437;
				break;
			}
			int activeInvSection2 = (int)ActiveInvSection;
			float num4 = inventoryMenuSectionScale[i];
			if (i == activeInvSection2)
			{
				if (num4 < 1f)
				{
					num4 += 0.05f;
					inventoryMenuSectionScale[i] = num4;
				}
			}
			else if (num4 > 0.75f)
			{
				num4 -= 0.05f;
				inventoryMenuSectionScale[i] = num4;
			}
			int y = (int)((float)safeAreaOffsetTop + 22f * Main.ScreenMultiplier * (1f - num4));
			int num5 = (int)(65f + 180f * num4);
			num4 *= Main.ScreenMultiplier;
			// Remember where the tab was drawn, so a click can select it. The tabs are the most
			// obvious thing to click in the inventory and were the one part that ignored the
			// pointer: opening the inventory and clicking "crafting" did nothing.
			if (i >= 0 && i < inventorySectionRects.Length)
			{
				int num6 = (int)(52f * num4);
				inventorySectionRects[i] = new Rectangle(num3, y, num6, (int)(52f * num4));
				inventorySectionRectValid[i] = true;
			}
			if (i == activeInvSection2)
			{
				c.R = 200;
				c.G = 200;
				c.B = 200;
				c.A = 200;
				SpriteSheet<_sheetSprites>.DrawTL(448, num3, y, c, num4);
			}
			else
			{
				c.R = 100;
				c.G = 100;
				c.B = 100;
				c.A = 100;
				SpriteSheet<_sheetSprites>.DrawTL(451, num3, y, c, num4);
			}
			inventoryScale = num4;
			Color itemColor = ((!IsInventorySectionAvailable((InventorySection)i)) ? DISABLED_COLOR : new Color(num5, num5, num5, num5));
			DrawInventoryItem(texId, num3, y, itemColor);
			num3 += (int)(52f * num4) + (int)(4f * Main.ScreenMultiplier);
		}
		InventorySection activeInvSection3 = ActiveInvSection;
		if (1 == 0)
		{
		}
		string text;
		switch (activeInvSection3)
		{
		case InventorySection.CRAFTING:
			text = Lang.InterfaceText[25];
			break;
		case InventorySection.ITEMS:
			text = Lang.InterfaceText[4];
			break;
		case InventorySection.CHEST:
		{
			string text3;
			if (!flag2)
			{
				Player.ExtraStorage playerChest2 = (Player.ExtraStorage)ActivePlayer.PlayerChest;
				if (1 == 0)
				{
				}
				string text2 = playerChest2 switch
				{
					Player.ExtraStorage.PIGGYBANK => Lang.InterfaceText[32], 
					Player.ExtraStorage.SAFE => Lang.InterfaceText[33], 
					_ => chestText, 
				};
				if (1 == 0)
				{
				}
				text3 = text2;
			}
			else
			{
				text3 = Lang.InterfaceText[28];
			}
			text = text3;
			break;
		}
		case InventorySection.EQUIP:
			text = Lang.InterfaceText[45];
			break;
		default:
			text = Lang.InterfaceText[7];
			break;
		}
		if (1 == 0)
		{
		}
		string text4 = text;
		if (reforge)
		{
			text4 += " (";
			text4 += Lang.InterfaceText[19];
			text4 += ")";
		}
		DrawStringCT(SmallFont, text4, (int)((float)num + 432f * Main.ScreenMultiplier), (int)((float)safeAreaOffsetTop + 44f * Main.ScreenMultiplier), Color.White);
		switch (ActiveInvSection)
		{
		case InventorySection.ITEMS:
			DrawInventory(num, safeAreaOffsetTop + INVENTORY_CLIENT_Y_OFFSET);
			break;
		case InventorySection.CHEST:
			if (flag2)
			{
				DrawShop(num, safeAreaOffsetTop + INVENTORY_CLIENT_Y_OFFSET);
			}
			else
			{
				DrawStorage(num, safeAreaOffsetTop + INVENTORY_CLIENT_Y_OFFSET);
			}
			break;
		case InventorySection.CRAFTING:
			DrawCrafting(num, safeAreaOffsetTop + INVENTORY_CLIENT_Y_OFFSET);
			break;
		case InventorySection.EQUIP:
			DrawEquip(num, safeAreaOffsetTop + INVENTORY_CLIENT_Y_OFFSET);
			break;
		case InventorySection.HOUSING:
			DrawHousing(num, safeAreaOffsetTop + INVENTORY_CLIENT_Y_OFFSET);
			break;
		}
		DrawMouseItem();
	}

	private void DrawCrafting(int CRAFTING_X, int CRAFTING_Y)
	{
		Color color = default;
		Color color2 = new Color(invAlpha, invAlpha, invAlpha, invAlpha);
		string text = null;
		int num = 48;
		int height = 318;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num = 60;
			height = 438;
			break;
		case ScreenHeights.FHD:
			num = 88;
			height = 676;
			break;
		}
		Main.DrawRectOpenAtTop(441, new Rectangle(CRAFTING_X, CRAFTING_Y + num, INVENTORY_W, height), 192);
		int num2 = (int)((float)CRAFTING_X + 3f * Main.ScreenMultiplier);
		for (int i = 0; i < 6; i++)
		{
			Rectangle rect = new Rectangle(num2, (int)((float)CRAFTING_Y + 8f * Main.ScreenMultiplier), (int)(127f * Main.ScreenMultiplier), (int)(32f * Main.ScreenMultiplier));
			// Click target for the category tab, recorded where it is drawn.
			if (i < craftingCategoryRects.Length)
			{
				craftingCategoryRects[i] = rect;
				craftingCategoryRectValid[i] = true;
			}
			if ((int)craftingCategory != i)
			{
				Main.DrawRectStraightBottom(441, rect, 192, 2);
			}
			else
			{
				Main.DrawRectOpenAtBottom(441, rect, 192);
			}
			SpriteSheet<_sheetSprites>.DrawCentered(206 + i, ref rect, Main.ScreenMultiplier);
			num2 += (int)(146f * Main.ScreenMultiplier);
		}
		int num3 = (int)((float)CRAFTING_X + 20f * Main.ScreenMultiplier);
		int num4 = (int)((float)CRAFTING_Y + 60f * Main.ScreenMultiplier);
		// Reset the recorded cells: the list changes with the category and with what is craftable.
		for (int i = 0; i < craftSlotValid.Length; i++)
		{
			craftSlotValid[i] = false;
		}
		craftSlotCount = 0;
		// Reset here rather than where the cells are recorded: with no recipes at all the
		// recording loop never runs, and stale rectangles would keep claiming the panel.
		for (int i = 0; i < craftIngredientValid.Length; i++)
		{
			craftIngredientValid[i] = false;
		}
		int count = CurrentRecipeCategory.Count;
		int num5 = (int)(56f * Main.ScreenMultiplier);
		byte b = 8;
		byte b2 = 5;
		if (count > 0)
		{
			inventoryScale = Main.ScreenMultiplier;
			int num6 = CraftingRecipeY - Math.Sign(craftingRecipeScrollY);
			int num7 = Math.Min(b, CurrentRecipeCategory[CraftingRecipeY].RecipeList.Count) * num5;
			int width = ((craftingRecipeScrollX == 0f) ? ((int)(448f * Main.ScreenMultiplier)) : num7);
			int num8 = Math.Min(b2, count) * num5;
			Rectangle scissorRectangle = default;
			for (int j = 0; j < 2; j++)
			{
				Main.SpriteBatch.End();
				scissorRectangle.X = num3;
				scissorRectangle.Y = num4;
				if (j == 0)
				{
					scissorRectangle.Width = num5;
					scissorRectangle.Height = num8;
				}
				else
				{
					scissorRectangle.Y += num5;
					scissorRectangle.Width = width;
					scissorRectangle.Height = num5;
				}
				if (!CurrentView.IsFullScreen())
				{
					scissorRectangle.X >>= 1;
					scissorRectangle.X += CurrentView.ActiveViewport.X;
					scissorRectangle.Y >>= 1;
					scissorRectangle.Y += CurrentView.ActiveViewport.Y;
					scissorRectangle.Width >>= 1;
					scissorRectangle.Height >>= 1;
				}
				WorldView.GraphicsDevice.ScissorRectangle = scissorRectangle;
				Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, WorldView.ScissorRastTest, CurrentView.ScreenProjection);
				int num9 = num4;
				int num10 = 1;
				if (craftingRecipeScrollY != 0f)
				{
					num10 = 2;
					num9 -= num5;
					num9 -= (int)((1f - Math.Abs(craftingRecipeScrollY)) * (float)num5) * Math.Sign(craftingRecipeScrollY);
					if (j == 1)
					{
						craftingRecipeScrollY *= craftingRecipeScrollMul;
						if (Math.Abs(craftingRecipeScrollY) < 1f / 56f)
						{
							craftingRecipeScrollY = 0f;
						}
					}
				}
				for (int k = -num10; k < b2 + num10; k++)
				{
					int num11 = num6 + k;
					if (num11 >= 0)
					{
						if (num11 >= count)
						{
							int num12 = num9 - num4;
							if (num12 < num8)
							{
								num8 = num12;
							}
							break;
						}
						int num13 = 0;
						int num14 = num3;
						if (craftingRecipeScrollX != 0f && num11 == CraftingRecipeY)
						{
							num13 = 1;
							num14 -= num5;
							if (j == 0)
							{
								craftingRecipeScrollX *= craftingRecipeScrollMul;
								if (Math.Abs(craftingRecipeScrollX) < 1f / 56f)
								{
									craftingRecipeScrollX = 0f;
								}
							}
							num14 -= (int)((1f - Math.Abs(craftingRecipeScrollX)) * (float)num5) * Math.Sign(craftingRecipeScrollX);
						}
						Recipe.SubCategoryList subCategoryList = CurrentRecipeCategory[num11];
						int num15 = ((num11 == CraftingRecipeY) ? (CraftingRecipeWindowX - Math.Sign(craftingRecipeScrollX)) : 0);
						int count2 = subCategoryList.RecipeList.Count;
						for (int l = -num13; l < b + num13; l++)
						{
							int num16 = num15 + l;
							if (num16 < 0)
							{
								num16 += count2;
							}
							if (num13 == 0 && l >= count2)
							{
								break;
							}
							num16 %= subCategoryList.RecipeList.Count;
							int num17 = subCategoryList.RecipeList[num16];
							Recipe recipe = Main.ActiveRecipe[num17];
							bool flag = ActivePlayer.CanCraftRecipe(recipe);
							bool flag2 = num11 == CraftingRecipeY && num16 == CraftingRecipeX;
							// Remember where the cell was drawn. Only on the first pass and only with
							// no animation in flight: a cell sliding into place is not where a click
							// should land, and the pointer is read a frame later.
							if (j == 0 && num13 == 0 && craftingRecipeScrollX == 0f && craftingRecipeScrollY == 0f
								&& craftSlotCount < craftSlotRects.Length)
							{
								craftSlotRects[craftSlotCount] = new Rectangle(num14, num9, num5, num5);
								craftSlotRow[craftSlotCount] = (sbyte)num11;
								craftSlotIndex[craftSlotCount] = (sbyte)num16;
								craftSlotValid[craftSlotCount] = true;
								craftSlotCount++;
							}
							if (flag2)
							{
								CraftingRecipe = recipe;
								DrawInventoryCursor(num14, num9, inventoryScale, (craftingSection == CraftingSection.RECIPES) ? 255 : 96);
								if (j == 0)
								{
									toolTip = recipe.CraftedItem;
									text = "(";
									text += ActivePlayer.CountPossession(toolTip.NetID).ToStringLookup();
									text += Lang.MenuText[1];
									UpdateToolTipText(text);
								}
							}
							else
							{
								int id = ((j == 0) ? 443 : 450);
								color = color2;
								if (!flag)
								{
									color.R >>= 1;
									color.G >>= 1;
									color.B >>= 1;
									color.A >>= 1;
								}
								SpriteSheet<_sheetSprites>.DrawTL(id, num14, num9, color, inventoryScale);
							}
							color = (flag ? new Color(255, 255, 255, 255) : new Color(16, 16, 16, 128));
							DrawInventoryItem(ref recipe.CraftedItem, num14, num9, color, StackType.INVENTORY);
							if (ActivePlayer.recipesNew.Get(num17))
							{
								if (flag2)
								{
									ActivePlayer.recipesNew.Set(num17, value: false);
								}
								else
								{
									color = new Color(cursorAlpha, cursorAlpha, cursorAlpha, cursorAlpha);
									SpriteSheet<_sheetTiles>.Draw(23, num14 + (int)(40f * Main.ScreenMultiplier), num9 + (int)(12f * Main.ScreenMultiplier), color, (float)((double)Main.FrameCounter * (1.0 / (8.0 * Math.PI))), (float)(0.800000011920929 + Math.Sin((double)Main.FrameCounter * (1.0 / (16.0 * Math.PI))) * 0.20000000298023224));
									SpriteSheet<_sheetTiles>.Draw(23, num14 + (int)(28f * Main.ScreenMultiplier), num9 + (int)(28f * Main.ScreenMultiplier), color, (float)((double)Main.FrameCounter * (1.0 / (6.0 * Math.PI))), (float)(0.6000000238418579 + Math.Sin((double)Main.FrameCounter * (1.0 / (24.0 * Math.PI))) * 0.4000000059604645));
									SpriteSheet<_sheetTiles>.Draw(23, num14 + (int)(16f * Main.ScreenMultiplier), num9 + (int)(20f * Main.ScreenMultiplier), color, (float)((double)Main.FrameCounter * (1.0 / (4.0 * Math.PI))), (float)(0.699999988079071 + Math.Sin((double)Main.FrameCounter * (1.0 / (32.0 * Math.PI))) * 0.30000001192092896));
								}
							}
							num14 += num5;
						}
					}
					num9 += num5;
				}
			}
			Main.SpriteBatch.End();
			Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, CurrentView.ScreenProjection);
			if (craftingRecipeScrollX == 0f && craftingRecipeScrollY == 0f)
			{
				Rectangle rect2 = default;
				if (num7 > num5)
				{
					rect2.X = num3 - (int)(16f * Main.ScreenMultiplier);
					rect2.Y = num4 + num5;
					rect2.Width = (int)(16f * Main.ScreenMultiplier);
					rect2.Height = num5;
					SpriteSheet<_sheetSprites>.DrawCentered(136, ref rect2, SpriteEffects.FlipHorizontally);
					rect2.X += (int)(10f * Main.ScreenMultiplier) + num7;
					SpriteSheet<_sheetSprites>.DrawCentered(136, ref rect2);
				}
				if (count > 1)
				{
					if (CraftingRecipeY == 0)
					{
						num4 += num5;
						num8 -= num5;
					}
					rect2.X = num3;
					rect2.Y = num4 - (int)(16f * Main.ScreenMultiplier);
					rect2.Width = num5;
					rect2.Height = (int)(16f * Main.ScreenMultiplier);
					SpriteSheet<_sheetSprites>.DrawCentered(135, ref rect2, SpriteEffects.FlipVertically);
					rect2.Y += num8 + (int)(10f * Main.ScreenMultiplier);
					SpriteSheet<_sheetSprites>.DrawCentered(135, ref rect2);
					if (CraftingRecipeY == 0)
					{
						num4 -= num5;
						num8 += num5;
					}
				}
			}
			int num18 = (int)((float)num3 + 64f * Main.ScreenMultiplier);
			int num19 = (int)((float)num4 + 120f * Main.ScreenMultiplier);
			Main.DrawRect(441, new Rectangle(num18, num19, (int)((float)(CRAFTING_X + INVENTORY_W - TOOLTIP_W) - 32f * Main.ScreenMultiplier - (float)num18), (int)(174f * Main.ScreenMultiplier)), 96);
			SmallFont.MeasureString(Lang.MenuText[0]);
			DrawStringCT(SmallFont, Lang.MenuText[0], (int)((float)num18 + 100f * Main.ScreenMultiplier), (int)((float)num19 + 144f * Main.ScreenMultiplier), Color.White);
			inventoryScale = 0.9f * Main.ScreenMultiplier;
			int num20 = 0;
			for (int m = 0; m < 3; m++)
			{
				int num21 = 0;
				while (num21 < 4)
				{
					int num22 = (int)(50f * Main.ScreenMultiplier);
					int x = num18 + num22 * num21;
					int y = num19 + num22 * m;
					if (num20 < craftIngredientRects.Length)
					{
						craftIngredientRects[num20] = new Rectangle(x, y, num22, num22);
						craftIngredientCol[num20] = (sbyte)num21;
						craftIngredientRow[num20] = (sbyte)m;
						craftIngredientValid[num20] = true;
					}
					if (craftingSection == CraftingSection.INGREDIENTS && num21 == craftingIngredientX && m == craftingIngredientY)
					{
						DrawInventoryCursor(x, y, inventoryScale);
						if (num20 < CraftingRecipe.NumRequiredItems)
						{
							toolTip = CraftingRecipe.RequiredItem[num20];
							text = "(";
							text += ActivePlayer.CountPossession(toolTip.NetID).ToStringLookup();
							text += Lang.MenuText[1];
							UpdateToolTipText(text);
						}
					}
					else
					{
						color = ((num20 < CraftingRecipe.NumRequiredItems) ? color2 : new Color(64, 64, 64, 64));
						SpriteSheet<_sheetSprites>.DrawTL(442, x, y, color, inventoryScale);
					}
					if (num20 < CraftingRecipe.NumRequiredItems)
					{
						DrawInventoryItem(ref CraftingRecipe.RequiredItem[num20], x, y, new Color(255, 255, 255, 255), StackType.INGREDIENT);
					}
					num21++;
					num20++;
				}
			}
			int num23 = -1;
			Main.StrBuilder.Length = 0;
			if (CraftingRecipe.NumRequiredTiles > 0)
			{
				Main.StrBuilder.Append(Main.TileNames[CraftingRecipe.RequiredTile[0]]);
				switch (CraftingRecipe.RequiredTile[0])
				{
				case 13:
					num23 = 1;
					break;
				case 14:
					num23 = 1480;
					Main.StrBuilder.Append(" & ");
					Main.StrBuilder.Append(Main.TileNames[15]);
					break;
				case 16:
					num23 = 486;
					break;
				case 17:
					num23 = 484;
					break;
				case 18:
					if (CraftingRecipe.RequiredTile[1] == 15)
					{
						num23 = 1487;
						Main.StrBuilder.Append(" & ");
						Main.StrBuilder.Append(Main.TileNames[15]);
					}
					else
					{
						num23 = 487;
					}
					break;
				case 26:
					num23 = 212;
					break;
				case 86:
					num23 = 783;
					break;
				case 94:
					num23 = 803;
					break;
				case 96:
					num23 = 796;
					break;
				case 101:
					num23 = 139;
					break;
				case 106:
					num23 = 814;
					break;
				case 114:
					num23 = 849;
					break;
				case 134:
					num23 = 976;
					break;
				}
			}
			else if (CraftingRecipe.NeedsWater)
			{
				num23 = 1484;
				Main.StrBuilder.Append(Lang.InterfaceText[53]);
			}
			if (num23 >= 0)
			{
				Rectangle destRect = new Rectangle
				{
					X = (int)((float)num18 + 280f * Main.ScreenMultiplier),
					Y = (int)((float)num19 + 46f * Main.ScreenMultiplier),
					Width = (int)(68f * Main.ScreenMultiplier),
					Height = (int)(68f * Main.ScreenMultiplier)
				};
				Main.DrawRect(442, destRect, 192);
				int width2 = SpriteSheet<_sheetSprites>.Source[num23].Width;
				int height2 = SpriteSheet<_sheetSprites>.Source[num23].Height;
				float num24 = ((width2 <= height2) ? (64f / (float)height2) : (64f / (float)width2));
				Vector2 pos = new Vector2
				{
					X = destRect.Center.X,
					Y = destRect.Center.Y
				};
				SpriteSheet<_sheetSprites>.DrawScaled(num23, ref pos, Color.White, num24 * Main.ScreenMultiplier);
				DrawStringCT(SmallFont, destRect.Center.X, destRect.Bottom + 2, ActivePlayer.IsNearCraftingStation(CraftingRecipe) ? Color.White : new Color(255, 64, 64, 255));
			}
			if (text != null)
			{
				DrawToolTip((int)((float)(CRAFTING_X + INVENTORY_W - TOOLTIP_W) - 8f * Main.ScreenMultiplier), (int)((float)num4 + 8f * Main.ScreenMultiplier), INVENTORY_CLIENT_H - INVENTORY_CLIENT_Y_OFFSET);
			}
		}
		DrawControlsCrafting();
	}

	private void DrawMiniMap()
	{
		int safeAreaOffsetLeft = CurrentView.SafeAreaOffsetLeft;
		int safeAreaOffsetTop = CurrentView.SafeAreaOffsetTop;
		int width = CurrentView.ViewWidth - CurrentView.SafeAreaOffsetLeft - CurrentView.SafeAreaOffsetRight;
		int height = Main.ResolutionHeight - CurrentView.SafeAreaOffsetTop - CurrentView.SafeAreaOffsetBottom - (int)(36f * Main.ScreenMultiplier);
		Main.DrawRect(451, new Rectangle(safeAreaOffsetLeft, safeAreaOffsetTop, width, height), 128);
		int num = (int)(31f * Main.ScreenMultiplier) + safeAreaOffsetLeft;
		int num2 = (int)(2f * Main.ScreenMultiplier) + safeAreaOffsetTop;
		Color white = Color.White;
		// Click targets, recorded from the same numbers the widgets are drawn with. The pointer
		// has no other way to know where the PvP box and the team dots ended up.
		MapScreenMouse.RecordWidgets(
			new Rectangle(num - (int)(10f * Main.ScreenMultiplier), num2 - (int)(10f * Main.ScreenMultiplier), (int)(52f * Main.ScreenMultiplier), (int)(52f * Main.ScreenMultiplier)),
			TeamRectsForMouse(safeAreaOffsetLeft, safeAreaOffsetTop),
			new Rectangle(safeAreaOffsetLeft - (int)(2f * Main.ScreenMultiplier), (int)(98f * Main.ScreenMultiplier) + safeAreaOffsetTop - (int)(2f * Main.ScreenMultiplier), (int)(44f * Main.ScreenMultiplier), (int)(8f * 44f * Main.ScreenMultiplier)));
		if (mapScreenCursorX == 0 && mapScreenCursorY < 2)
		{
			DrawInventoryCursor(num, num2, Main.ScreenMultiplier);
		}
		else
		{
			SpriteSheet<_sheetSprites>.DrawTL(451, num, num2, white, Main.ScreenMultiplier);
		}
		if (pvpSelected)
		{
			Vector2 pos = new Vector2(num + (int)(9f * Main.ScreenMultiplier), num2 + (int)(11f * Main.ScreenMultiplier));
			Vector2 pivot = default;
			SpriteSheet<_sheetSprites>.Draw(455, ref pos, white, 0f, ref pivot, Main.ScreenMultiplier, SpriteEffects.None);
			pos.X += (int)(2f * Main.ScreenMultiplier);
			SpriteSheet<_sheetSprites>.Draw(455, ref pos, white, 0f, ref pivot, Main.ScreenMultiplier, SpriteEffects.FlipHorizontally);
		}
		else
		{
			SpriteSheet<_sheetSprites>.DrawRotatedTL(455, num - (int)(7f * Main.ScreenMultiplier), num2 + (int)(25f * Main.ScreenMultiplier), white, -0.785f, Main.ScreenMultiplier);
			SpriteSheet<_sheetSprites>.DrawRotatedTL(455, num + (int)(11f * Main.ScreenMultiplier), num2 + (int)(25f * Main.ScreenMultiplier), white, -0.785f, Main.ScreenMultiplier);
		}
		int num3 = num + (int)(123f * Main.ScreenMultiplier);
		for (int i = 0; i < 5; i++)
		{
			white = Main.TeamColors[i];
			if (i == teamSelected)
			{
				white.A = MouseTextBrightness;
			}
			else
			{
				white.R >>= 1;
				white.G >>= 1;
				white.B >>= 1;
				white.A >>= 1;
			}
			int num4 = num3;
			int num5 = num2 + (int)(3f * Main.ScreenMultiplier);
			switch (i)
			{
			case 0:
				num4 -= (int)(32f * Main.ScreenMultiplier);
				num5 += (int)(16f * Main.ScreenMultiplier);
				break;
			case 2:
				num4 += (int)(32f * Main.ScreenMultiplier);
				break;
			case 3:
				num5 += (int)(32f * Main.ScreenMultiplier);
				break;
			default:
				num4 += (int)(32f * Main.ScreenMultiplier);
				num5 += (int)(32f * Main.ScreenMultiplier);
				break;
			case 1:
				break;
			}
			num4 -= 8;
			num5 -= 8;
			if ((i == 0 && mapScreenCursorX == 1) || (i == 1 && mapScreenCursorX == 2 && mapScreenCursorY == 0) || (i == 2 && mapScreenCursorX == 3 && mapScreenCursorY == 0) || (i == 3 && mapScreenCursorX == 2 && mapScreenCursorY == 1) || (i == 4 && mapScreenCursorX == 3 && mapScreenCursorY == 1))
			{
				DrawInventoryCursor(num4, num5, 8f * Main.ScreenMultiplier / 13f);
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawScaledTL(451, num4, num5, Color.White, 8f * Main.ScreenMultiplier / 13f);
			}
			num4 += (int)(8f * Main.ScreenMultiplier);
			num5 += (int)(8f * Main.ScreenMultiplier);
			if (Main.ScreenHeightPtr == ScreenHeights.HD)
			{
				num4++;
				num5++;
			}
			SpriteSheet<_sheetSprites>.DrawTL(1481, num4, num5, white, Main.ScreenMultiplier);
		}
		int num6 = safeAreaOffsetLeft;
		int num7 = (int)(98f * Main.ScreenMultiplier) + safeAreaOffsetTop;
		for (int num8 = 8; num8 > 0; num8--)
		{
			int num9 = num7 + (int)(44f * Main.ScreenMultiplier) * (num8 - 1);
			if (mapScreenCursorY - 1 == num8)
			{
				DrawInventoryCursor(num6 - (int)(2f * Main.ScreenMultiplier), num9 - (int)(2f * Main.ScreenMultiplier), 12f * Main.ScreenMultiplier / 13f);
			}
			else
			{
				SpriteSheet<_sheetSprites>.DrawScaledTL(451, num6 + (int)(2f * Main.ScreenMultiplier), num9 + (int)(2f * Main.ScreenMultiplier), Color.White, 10.5f * Main.ScreenMultiplier / 13f);
			}
		}
		Main.SpriteBatch.End();
		GamerCollection<NetworkGamer> allGamers = Netplay.Session.AllGamers;
		for (int j = 0; j < allGamers.Count; j++)
		{
			if (allGamers[j].Tag is Player player)
			{
				DrawPlayerIcon(player, new Vector2(num6 + (int)(8f * Main.ScreenMultiplier), num7 + (int)(8f * Main.ScreenMultiplier) + j * (int)(44f * Main.ScreenMultiplier)), 1.5f * Main.ScreenMultiplier);
			}
		}
		miniMap.DrawMap(CurrentView);
		DrawStringCC(SmallFont, Lang.MenuText[81], num + (int)(26f * Main.ScreenMultiplier), num2 + (int)(69f * Main.ScreenMultiplier), Color.White);
		DrawStringCC(SmallFont, Lang.MenuText[82], num3 + (int)(16f * Main.ScreenMultiplier), num2 + (int)(69f * Main.ScreenMultiplier), Color.White);
		for (int k = 0; k < allGamers.Count; k++)
		{
			if (allGamers[k].Tag is Player player2)
			{
				int y = num7 + 8 + 88 * k;
				DrawStringLT(BoldSmallFont, player2.Name, num6 + (int)(52f * Main.ScreenMultiplier), y, Main.TeamColors[player2.team]);
			}
		}
	}

	/// <summary>
	/// The five team dots' click rectangles, from the same offsets the drawing loop uses. Written
	/// as a separate function rather than returned from the loop because the pointer is tested
	/// before the frame that records it: an unrecorded frame must not leave stale dots.
	/// </summary>
	private static Rectangle[] TeamRectsForMouse(int Left, int Top)
	{
		float num = Main.ScreenMultiplier;
		int num2 = (int)(31f * num) + Left;
		int num3 = num2 + (int)(123f * num);
		int num4 = (int)(2f * num) + Top;
		Rectangle[] array = new Rectangle[5];
		for (int i = 0; i < 5; i++)
		{
			int num5 = num3;
			int num6 = num4 + (int)(3f * num);
			switch (i)
			{
			case 0:
				num5 -= (int)(32f * num);
				num6 += (int)(16f * num);
				break;
			case 2:
				num5 += (int)(32f * num);
				break;
			case 3:
				num6 += (int)(32f * num);
				break;
			case 4:
				num5 += (int)(32f * num);
				num6 += (int)(32f * num);
				break;
			}
			num5 -= 8;
			num6 -= 8;
			// Sixteen physical pixels at 540p plus a few of slack: the dot is small, and asking
			// for pixel accuracy on it is a worse experience than a slightly generous target.
			array[i] = new Rectangle(num5 - (int)(4f * num), num6 - (int)(4f * num), (int)(24f * num), (int)(24f * num));
		}
		return array;
	}

	private bool AccCheck(ref Item newItem, int slot)
	{
		if (ActivePlayer.armor[slot].NetID == newItem.NetID)
		{
			return false;
		}
		for (int i = 3; i < 11; i++)
		{
			if (newItem.NetID == ActivePlayer.armor[i].NetID)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateToolTipText(string extraInfo)
	{
		Main.StrBuilder.Length = 0;
		string text2;
		if (toolTip.Type > 0)
		{
			switch ((EntityID.RarityID)toolTip.Rarity)
			{
			case EntityID.RarityID.GREY:
				Main.StrBuilder.Append("<f c='#828282'>");
				break;
			case EntityID.RarityID.BLUE:
				Main.StrBuilder.Append("<f c='#9696FF'>");
				break;
			case EntityID.RarityID.GREEN:
				Main.StrBuilder.Append("<f c='#96FF96'>");
				break;
			case EntityID.RarityID.ORANGE:
				Main.StrBuilder.Append("<f c='#FFC896'>");
				break;
			case EntityID.RarityID.LIGHT_RED:
				Main.StrBuilder.Append("<f c='#FF9696'>");
				break;
			case EntityID.RarityID.PINK:
				Main.StrBuilder.Append("<f c='#FF96FF'>");
				break;
			case EntityID.RarityID.LIGHT_PURPLE:
				Main.StrBuilder.Append("<f c='#D2A0FF'>");
				break;
			default:
				Main.StrBuilder.Append("<f c='#FFFFD2'>");
				break;
			}
			Main.StrBuilder.Append(toolTip.AffixName());
			if (toolTip.Stack > 1)
			{
				Main.StrBuilder.Append(toolTip.Stack.ToStackString());
			}
			Main.StrBuilder.Append("</f>\n");
			if (extraInfo != null)
			{
				Main.StrBuilder.Append(extraInfo);
				Main.StrBuilder.Append('\n');
			}
			if (npcShop > 0 || (reforge && toolTip.SetPrefix(-3)))
			{
				if (toolTip.Value > 0)
				{
					int num = toolTip.Value * toolTip.Stack;
					if (reforge)
					{
						Main.StrBuilder.Append(Lang.InterfaceText[46]);
					}
					else if (toolTip.CanBuy)
					{
						Main.StrBuilder.Append(Lang.TipText[50]);
					}
					else
					{
						num /= 5;
						Main.StrBuilder.Append(Lang.TipText[49]);
					}
					int num2 = 0;
					int num3 = 0;
					int num4 = 0;
					int num5 = 0;
					if (num < 1)
					{
						num = 1;
					}
					if (num >= 1000000)
					{
						num2 = num / 1000000;
						num -= num2 * 1000000;
					}
					if (num >= 10000)
					{
						num3 = num / 10000;
						num -= num3 * 10000;
					}
					if (num >= 100)
					{
						num4 = num / 100;
						num -= num4 * 100;
					}
					if (num >= 1)
					{
						num5 = num;
					}
					if (num2 > 0)
					{
						Main.StrBuilder.Append("<f c='#DCDCC6'>");
						Main.StrBuilder.Append(num2.ToStringLookup());
						Main.StrBuilder.Append(Lang.InterfaceText[15]);
						Main.StrBuilder.Append("</f>");
					}
					if (num3 > 0)
					{
						Main.StrBuilder.Append("<f c='#E0C95C'>");
						Main.StrBuilder.Append(num3.ToStringLookup());
						Main.StrBuilder.Append(Lang.InterfaceText[16]);
						Main.StrBuilder.Append("</f>");
					}
					if (num4 > 0)
					{
						Main.StrBuilder.Append("<f c='#B5C0C1'>");
						Main.StrBuilder.Append(num4.ToStringLookup());
						Main.StrBuilder.Append(Lang.InterfaceText[17]);
						Main.StrBuilder.Append("</f>");
					}
					if (num5 > 0)
					{
						Main.StrBuilder.Append("<f c='#F68A60'>");
						Main.StrBuilder.Append(num5.ToStringLookup());
						Main.StrBuilder.Append(Lang.InterfaceText[18]);
						Main.StrBuilder.Append("</f>");
					}
					Main.StrBuilder.Append('\n');
				}
				else
				{
					Main.StrBuilder.Append("<f c='#787878'>");
					Main.StrBuilder.Append(Lang.TipText[51]);
					Main.StrBuilder.Append("</f>\n");
				}
			}
			if (toolTip.IsMaterial)
			{
				Main.StrBuilder.Append("¤ ");
				Main.StrBuilder.Append(Lang.TipText[36]);
				Main.StrBuilder.Append('\n');
			}
			if (toolTip.CreateWall > 0 || toolTip.CreateTile >= 0)
			{
				if (toolTip.Type != 213)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(Lang.TipText[33]);
					Main.StrBuilder.Append('\n');
				}
			}
			else if (toolTip.Ammo > 0)
			{
				Main.StrBuilder.Append("¤ ");
				Main.StrBuilder.Append(Lang.TipText[34]);
				Main.StrBuilder.Append('\n');
			}
			else if (toolTip.IsConsumable)
			{
				Main.StrBuilder.Append("¤ ");
				Main.StrBuilder.Append(Lang.TipText[35]);
				Main.StrBuilder.Append('\n');
			}
			if (toolTip.IsSocial)
			{
				Main.StrBuilder.Append(Lang.TipText[0]);
				Main.StrBuilder.Append('\n');
				Main.StrBuilder.Append(Lang.TipText[1]);
				Main.StrBuilder.Append('\n');
			}
			else
			{
				if (toolTip.Damage > 0)
				{
					int damage = toolTip.Damage;
					int num6 = 0;
					if (toolTip.IsMelee)
					{
						Main.StrBuilder.Append("¤ ");
						Main.StrBuilder.Append(((int)(ActivePlayer.meleeDamage * (float)damage)).ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[2]);
						num6 = ActivePlayer.meleeCrit;
					}
					else if (toolTip.IsRanged)
					{
						Main.StrBuilder.Append("¤ ");
						Main.StrBuilder.Append(((int)(ActivePlayer.rangedDamage * (float)damage)).ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[3]);
						num6 = ActivePlayer.rangedCrit;
					}
					else if (toolTip.IsMagic)
					{
						Main.StrBuilder.Append("¤ ");
						Main.StrBuilder.Append(((int)(ActivePlayer.magicDamage * (float)damage)).ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[4]);
						num6 = ActivePlayer.magicCrit;
					}
					num6 -= ActivePlayer.Inventory[ActivePlayer.SelectedItem].Crit;
					num6 += toolTip.Crit;
					Main.StrBuilder.Append("\n¤ ");
					Main.StrBuilder.Append(num6.ToStringLookup());
					Main.StrBuilder.Append(Lang.TipText[5]);
					Main.StrBuilder.Append('\n');
					if (toolTip.UseStyle > 0)
					{
						int num7 = 13;
						if (toolTip.UseAnimation <= 8)
						{
							num7 = 6;
						}
						else if (toolTip.UseAnimation <= 20)
						{
							num7 = 7;
						}
						else if (toolTip.UseAnimation <= 25)
						{
							num7 = 8;
						}
						else if (toolTip.UseAnimation <= 30)
						{
							num7 = 9;
						}
						else if (toolTip.UseAnimation <= 35)
						{
							num7 = 10;
						}
						else if (toolTip.UseAnimation <= 45)
						{
							num7 = 11;
						}
						else if (toolTip.UseAnimation <= 55)
						{
							num7 = 12;
						}
						Main.StrBuilder.Append("¤ ");
						Main.StrBuilder.Append(Lang.TipText[num7]);
						Main.StrBuilder.Append('\n');
					}
					int num8 = 22;
					double num9 = toolTip.Knockback;
					if (ActivePlayer.kbGlove)
					{
						num9 *= 1.7;
					}
					if (num9 == 0.0)
					{
						num8 = 14;
					}
					else if (num9 <= 1.5)
					{
						num8 = 15;
					}
					else if (num9 <= 3.0)
					{
						num8 = 16;
					}
					else if (num9 <= 4.0)
					{
						num8 = 17;
					}
					else if (num9 <= 6.0)
					{
						num8 = 18;
					}
					else if (num9 <= 7.0)
					{
						num8 = 19;
					}
					else if (num9 <= 9.0)
					{
						num8 = 20;
					}
					else if (num9 <= 11.0)
					{
						num8 = 21;
					}
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(Lang.TipText[num8]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.IsEquipable())
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(Lang.TipText[23]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.IsVanity)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(Lang.TipText[24]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.Defense > 0)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(toolTip.Defense.ToStringLookup());
					Main.StrBuilder.Append(Lang.TipText[25]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.PickPower > 0)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(((int)toolTip.PickPower).ToStringLookup());
					Main.StrBuilder.Append(Lang.TipText[26]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.AxePower > 0)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append((toolTip.AxePower * 5).ToStringLookup());
					Main.StrBuilder.Append(Lang.TipText[27]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.HammerPower > 0)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(((int)toolTip.HammerPower).ToStringLookup());
					Main.StrBuilder.Append(Lang.TipText[28]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.HealLife > 0)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(Lang.TipText[29]);
					Main.StrBuilder.Append(toolTip.HealLife.ToStringLookup());
					Main.StrBuilder.Append(Lang.TipText[30]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.HealMana > 0)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(Lang.TipText[29]);
					Main.StrBuilder.Append(toolTip.HealMana.ToStringLookup());
					Main.StrBuilder.Append(Lang.TipText[31]);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.Mana > 0 && (toolTip.Type != 127 || !ActivePlayer.spaceGun))
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(Lang.TipText[32]);
					Main.StrBuilder.Append(((int)((float)(int)toolTip.Mana * ActivePlayer.manaCost)).ToStringLookup());
					Main.StrBuilder.Append(Lang.TipText[31]);
					Main.StrBuilder.Append('\n');
				}
				string text = Lang.ToolTip(toolTip.NetID);
				if (text != null)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(text);
					Main.StrBuilder.Append('\n');
				}
				text = Lang.ToolTip2(toolTip.NetID);
				if (text != null)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(text);
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.BuffTime > 0)
				{
					Main.StrBuilder.Append("¤ ");
					if (toolTip.BuffTime / 60 >= 60)
					{
						Main.StrBuilder.Append(((int)Math.Round((double)(toolTip.BuffTime / 60) / 60.0)).ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[37]);
					}
					else
					{
						Main.StrBuilder.Append(((int)Math.Round((double)(int)toolTip.BuffTime / 60.0)).ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[38]);
					}
					Main.StrBuilder.Append('\n');
				}
				if (toolTip.PrefixType > 0)
				{
					if (cpItem.NetID != toolTip.NetID)
					{
						cpItem.NetDefaults(toolTip.NetID);
					}
					if (cpItem.Damage != toolTip.Damage)
					{
						int num10 = toolTip.Damage - cpItem.Damage;
						num10 = (int)Math.Round((double)num10 * 100.0 / (double)cpItem.Damage);
						if (num10 > 0)
						{
							Main.StrBuilder.Append("¤ <f c='#78BE78'>+");
						}
						else
						{
							Main.StrBuilder.Append("¤ <f c='#BE7878'>");
						}
						Main.StrBuilder.Append(num10.ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[39]);
						Main.StrBuilder.Append("</f>\n");
					}
					if (cpItem.UseAnimation != toolTip.UseAnimation)
					{
						int num11 = toolTip.UseAnimation - cpItem.UseAnimation;
						num11 = (int)Math.Round((double)num11 * -100.0 / (double)(int)cpItem.UseAnimation);
						if (num11 > 0)
						{
							Main.StrBuilder.Append("¤ <f c='#78BE78'>+");
						}
						else
						{
							Main.StrBuilder.Append("¤ <f c='#BE7878'>");
						}
						Main.StrBuilder.Append(num11.ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[40]);
						Main.StrBuilder.Append("</f>\n");
					}
					if (cpItem.Crit != toolTip.Crit)
					{
						int num12 = toolTip.Crit - cpItem.Crit;
						if (num12 > 0)
						{
							Main.StrBuilder.Append("¤ <f c='#78BE78'>+");
						}
						else
						{
							Main.StrBuilder.Append("¤ <f c='#BE7878'>");
						}
						Main.StrBuilder.Append(num12.ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[41]);
						Main.StrBuilder.Append("</f>\n");
					}
					if (cpItem.Mana != toolTip.Mana)
					{
						int num13 = toolTip.Mana - cpItem.Mana;
						num13 = (int)Math.Round((double)num13 * 100.0 / (double)(int)cpItem.Mana);
						if (num13 > 0)
						{
							Main.StrBuilder.Append("¤ <f c='#78BE78'>+");
						}
						else
						{
							Main.StrBuilder.Append("¤ <f c='#BE7878'>");
						}
						Main.StrBuilder.Append(num13.ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[42]);
						Main.StrBuilder.Append("</f>\n");
					}
					if (cpItem.Scale != toolTip.Scale)
					{
						int num14 = (int)Math.Round((double)(toolTip.Scale - cpItem.Scale) * 100.0 / (double)cpItem.Scale);
						if (num14 > 0)
						{
							Main.StrBuilder.Append("¤ <f c='#78BE78'>+");
						}
						else
						{
							Main.StrBuilder.Append("¤ <f c='#BE7878'>");
						}
						Main.StrBuilder.Append(num14.ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[43]);
						Main.StrBuilder.Append("</f>\n");
					}
					if (cpItem.ShootSpeed != toolTip.ShootSpeed)
					{
						int num15 = (int)Math.Round((double)(toolTip.ShootSpeed - cpItem.ShootSpeed) * 100.0 / (double)cpItem.ShootSpeed);
						if (num15 > 0)
						{
							Main.StrBuilder.Append("¤ <f c='#78BE78'>+");
						}
						else
						{
							Main.StrBuilder.Append("¤ <f c='#BE7878'>");
						}
						Main.StrBuilder.Append(num15.ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[44]);
						Main.StrBuilder.Append("</f>\n");
					}
					if (cpItem.Knockback != toolTip.Knockback)
					{
						int num16 = (int)Math.Round((double)(toolTip.Knockback - cpItem.Knockback) * 100.0 / (double)cpItem.Knockback);
						if (num16 > 0)
						{
							Main.StrBuilder.Append("¤ <f c='#78BE78'>+");
						}
						else
						{
							Main.StrBuilder.Append("¤ <f c='#BE7878'>");
						}
						Main.StrBuilder.Append(num16.ToStringLookup());
						Main.StrBuilder.Append(Lang.TipText[45]);
						Main.StrBuilder.Append("</f>\n");
					}
					switch ((EntityID.PrefixID)toolTip.PrefixType)
					{
					case EntityID.PrefixID.HARD:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+1");
						Main.StrBuilder.Append(Lang.TipText[25]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.GUARDING:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+2");
						Main.StrBuilder.Append(Lang.TipText[25]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.ARMORED:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+3");
						Main.StrBuilder.Append(Lang.TipText[25]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.WARDING:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+4");
						Main.StrBuilder.Append(Lang.TipText[25]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.ARCANE:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+20");
						Main.StrBuilder.Append(Lang.TipText[31]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.PRECISE:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+1");
						Main.StrBuilder.Append(Lang.TipText[5]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.LUCKY:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+2");
						Main.StrBuilder.Append(Lang.TipText[5]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.JAGGED:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+1");
						Main.StrBuilder.Append(Lang.TipText[39]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.SPIKED:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+2");
						Main.StrBuilder.Append(Lang.TipText[39]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.ANGRY:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+3");
						Main.StrBuilder.Append(Lang.TipText[39]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.MENACING:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+4");
						Main.StrBuilder.Append(Lang.TipText[39]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.BRISK:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+1");
						Main.StrBuilder.Append(Lang.TipText[46]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.FLEETING:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+2");
						Main.StrBuilder.Append(Lang.TipText[46]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.HASTY2:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+3");
						Main.StrBuilder.Append(Lang.TipText[46]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.QUICK2:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+4");
						Main.StrBuilder.Append(Lang.TipText[46]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.WILD:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+1");
						Main.StrBuilder.Append(Lang.TipText[47]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.RASH:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+2");
						Main.StrBuilder.Append(Lang.TipText[47]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.INTREPID:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+3");
						Main.StrBuilder.Append(Lang.TipText[47]);
						Main.StrBuilder.Append("</f>\n");
						break;
					case EntityID.PrefixID.VIOLENT:
						Main.StrBuilder.Append("¤ <f c='#78BE78'>+4");
						Main.StrBuilder.Append(Lang.TipText[47]);
						Main.StrBuilder.Append("</f>\n");
						break;
					}
				}
				if (toolTip.WornArmor && ActivePlayer.setBonus != null)
				{
					Main.StrBuilder.Append("¤ ");
					Main.StrBuilder.Append(Lang.TipText[48]);
					Main.StrBuilder.Append(ActivePlayer.setBonus);
					Main.StrBuilder.Append('\n');
				}
			}
			text2 = Main.StrBuilder.ToString();
		}
		else
		{
			text2 = extraInfo;
			if (text2 == null)
			{
				text2 = "";
			}
		}
		if (text2 != toolTipText)
		{
			toolTipText = text2;
			compiledToolTipText = new CompiledText(text2, TOOLTIP_W, BoldSmallTextStyle);
		}
	}

	private void DrawToolTip(int TOOLTIP_X, int TOOLTIP_Y, int TOOLTIP_H)
	{
		Rectangle rectangle = new Rectangle(TOOLTIP_X, TOOLTIP_Y, TOOLTIP_W, TOOLTIP_H);
		Main.DrawRect(451, rectangle, 255);
		compiledToolTipText.Draw(Main.SpriteBatch, rectangle, new Color(255, 255, 255, 255), new Color(255, 212, 64, 255));
	}

	public void OpenInventory()
	{
		InventoryMode = 1;
		oldMouseX = MouseX;
		oldMouseY = MouseY;
		// Rectangles from the previous time this screen was open describe a different layout (the
		// panel moves with the section, and with split-screen the view is half as wide). Forget
		// them rather than letting the first frame's click land on a stale tab.
		for (int i = 0; i < craftingCategoryRectValid.Length; i++)
		{
			craftingCategoryRectValid[i] = false;
		}
		ClearButtonTriggers();
		if (worldFadeTarget > 0.5f)
		{
			worldFadeTarget = 0.5f;
		}
		CraftingRecipeX = 0;
		CraftingRecipeWindowX = 0;
		CraftingRecipeY = 0;
		ActivePlayer.AdjTiles();
		if (npcShop > 0)
		{
			restoreOldInventorySection = true;
			oldInventorySection = ActiveInvSection;
			ActiveInvSection = InventorySection.CHEST;
			inventoryChestX = 0;
			inventoryChestY = 0;
		}
		else if (ActivePlayer.PlayerChest != -1)
		{
			restoreOldInventorySection = true;
			oldInventorySection = ActiveInvSection;
			ActiveInvSection = InventorySection.CHEST;
			inventoryChestX = -1;
			inventoryChestY = 1;
		}
		else if (reforge || CraftGuide)
		{
			restoreOldInventorySection = true;
			oldInventorySection = ActiveInvSection;
			ActiveInvSection = InventorySection.ITEMS;
		}
		else if (ActiveInvSection == InventorySection.CRAFTING)
		{
			Recipe.FindRecipes(this, craftingCategory, craftingShowCraftable);
		}
	}

	public void CloseInventory()
	{
		if (InventoryMode > 0)
		{
			if (restoreOldInventorySection)
			{
				restoreOldInventorySection = false;
				ActiveInvSection = oldInventorySection;
			}
			InventoryMode = 0;
			ActivePlayer.PlayerChest = -1;
			ActivePlayer.TalkNPC = -1;
			npcShop = 0;
			reforge = false;
			CraftGuide = false;
			MouseX = oldMouseX;
			MouseY = oldMouseY;
			toolTip.Init();
			ClearButtonTriggers();
			if (worldFadeTarget == 0.5f)
			{
				worldFadeTarget = 1f;
			}
			hotbarItemNameTime = 210;
		}
	}

	private bool IsInventorySectionAvailable(InventorySection section)
	{
		bool result = ActivePlayer.PlayerChest < -1 || (ActivePlayer.PlayerChest >= 0 && Main.ChestSet[ActivePlayer.PlayerChest] != null) || npcShop > 0;
		switch (section)
		{
		case InventorySection.CHEST:
			return result;
		case InventorySection.HOUSING:
			if (mouseItem.Type == 0 && !reforge)
			{
				return !CraftGuide;
			}
			return false;
		case InventorySection.CRAFTING:
			if (mouseItem.Type == 0 && !reforge)
			{
				if (CraftGuide)
				{
					return GuideItem.Type > 0;
				}
				return true;
			}
			return false;
		case InventorySection.EQUIP:
			if (mouseItem.Type != 0)
			{
				return mouseItem.IsEquipable();
			}
			return true;
		default:
			return true;
		}
	}

	/// <summary>
	/// The screen's own bookkeeping (section tabs, stack-split timers) must run exactly once per
	/// frame. It used to run from the pointer path and again from the stick path when a key was
	/// held, so one press of the shoulder button switched two tabs and the highlight visibly
	/// jumped two entries - the "it flies through the elements" report.
	/// </summary>
	private void UpdateInventoryMenuOnce()
	{
		if (inventoryMenuDone)
		{
			return;
		}
		inventoryMenuDone = true;
		UpdateInventoryMenu();
	}

	/// <summary>Set the first time <see cref="UpdateInventoryMenuOnce"/> runs in a frame.</summary>
	private bool inventoryMenuDone;

	private void UpdateInventoryMenu()
	{
		// Drain the wheel here too: with the inventory open the hotbar must not scroll, but
		// the steps must not pile up for when it closes either.
		int wheelSteps = Terraria.Input.KeyboardMouseInput.ConsumeWheelSteps();
		int num = (int)ActiveInvSection;
		bool flag = true;
		bool flag2 = IsButtonTriggered(Buttons.RightShoulder);
		bool flag3 = IsButtonTriggered(Buttons.LeftShoulder);
		do
		{
			if (flag2)
			{
				if (++num == 5)
				{
					num = 0;
				}
			}
			else if (flag3)
			{
				if (--num < 0)
				{
					num = 4;
				}
			}
			else if (!flag)
			{
				num = 1;
			}
			flag = IsInventorySectionAvailable((InventorySection)num);
		}
		while (!flag);
		ActiveInvSection = (InventorySection)num;
		if (flag3 | flag2)
		{
			toolTip.Init();
			if (ActiveInvSection == InventorySection.CRAFTING)
			{
				Recipe.FindRecipes(this, craftingCategory, craftingShowCraftable);
			}
		}
		// Crafting category tabs with the mouse. Clicking one is a category change only: it must
		// not also craft, so the click is consumed here before the recipe grid sees it.
		int clickedCategory;
		if (ActiveInvSection == InventorySection.CRAFTING && Terraria.Input.KeyboardMouseInput.LeftClicked
			&& !Terraria.Input.VirtualKeyboard.IsOpen && TryCraftingCategoryUnderPointer(out clickedCategory)
			&& clickedCategory != (int)craftingCategory)
		{
			Main.PlaySound(12);
			SelectCraftingCategory(clickedCategory);
			return;
		}
		// Wheel over the crafting grid scrolls the recipe list, as on the PC version. It has
		// to come after the section switch above: the wheel used to be turned into shoulder
		// presses, so it changed section instead of scrolling.
		if (wheelSteps != 0 && ActiveInvSection == InventorySection.CRAFTING && CraftingRecipeY >= 0 && CraftingRecipeY < CurrentRecipeCategory.Count)
		{
			// One cell per frame, no matter how many notches arrived at once. A free-spinning
			// wheel reports several notches in a single frame, and in a row that scrolls
			// sideways the skipped cells are never seen - that was "the scroll is too fast".
			int step = ((wheelSteps > 0) ? 1 : (-1));
			int recipeCount = CurrentRecipeCategory[CraftingRecipeY].RecipeList.Count;
			// The wheel moves the row, not the cursor inside it: with the window as the cursor,
			// scrolling and hovering would fight over the same number.
			int nextRecipe = Terraria.Input.WheelNav.Wrap(CraftingRecipeWindowX, step, recipeCount);
			if (nextRecipe != CraftingRecipeWindowX)
			{
				// No slide animation: the cells are only hit-testable once it has finished, so
				// animating a wheel scroll made the grid ignore clicks for the next 20 frames.
				craftingRecipeScrollX = 0f;
				craftingRecipeScrollY = 0f;
				// The wheel always means "the recipe row", whatever the pointer last selected.
				craftingSection = CraftingSection.RECIPES;
				CraftingRecipeWindowX = (sbyte)nextRecipe;
				CraftingRecipeX = (sbyte)nextRecipe;
			}
		}
		if (PadState.IsButtonUp(Buttons.RightTrigger) && PadState.IsButtonUp(Buttons.A))
		{
			stackSplit = 0;
		}
		else if (stackSplit > 0)
		{
			stackSplit--;
		}
		if (stackSplit == 0)
		{
			stackCounter = 0;
			stackDelay = 7;
		}
		else if (++stackCounter >= 30)
		{
			stackCounter = 0;
			if (--stackDelay < 2)
			{
				stackDelay = 2;
			}
		}
	}

	public void PositionInventoryCursor(int dx, int dy)
	{
		// The pointer already acted on a slot this frame: running the same handler again would
		// undo it, because IsButtonTriggered is not consumed.
		if (inventoryAction.Taken)
		{
			return;
		}
		while (true)
		{
			switch (ActiveInvSection)
			{
			case InventorySection.ITEMS:
				inventoryItemX += (sbyte)dx;
				if (inventoryItemX < 0)
				{
					inventoryItemX += 10;
				}
				else if (inventoryItemX >= 10)
				{
					inventoryItemX -= 10;
				}
				inventoryItemY += (sbyte)dy;
				if (inventoryItemY < 0)
				{
					inventoryItemY += 7;
				}
				else if (inventoryItemY >= 7)
				{
					inventoryItemY -= 7;
				}
				if (inventoryItemY == 4)
				{
					if (reforge)
					{
						dy |= 1;
						break;
					}
					if (inventoryItemX < 6)
					{
						break;
					}
				}
				else if (inventoryItemY == 5)
				{
					if (reforge)
					{
						dy |= 1;
						break;
					}
					if (inventoryItemX < 6)
					{
						break;
					}
				}
				else if (inventoryItemY == 6 && inventoryItemX < 9)
				{
					break;
				}
				UpdateInventory();
				return;
			case InventorySection.CHEST:
				inventoryChestX += (sbyte)dx;
				if (inventoryChestX < -1)
				{
					inventoryChestX += 6;
				}
				else if (inventoryChestX >= 5)
				{
					inventoryChestX -= 6;
				}
				inventoryChestY += (sbyte)dy;
				if (inventoryChestY < 0)
				{
					inventoryChestY += 4;
				}
				else if (inventoryChestY >= 4)
				{
					inventoryChestY -= 4;
				}
				if (inventoryChestX < 0)
				{
					if (npcShop > 0 || mouseItem.Type > 0)
					{
						if (dx == 0)
						{
							inventoryChestX = 0;
						}
						break;
					}
					if (inventoryChestY == 0)
					{
						inventoryChestY = 1;
					}
				}
				if (npcShop > 0)
				{
					UpdateShop();
				}
				else
				{
					UpdateStorage();
				}
				return;
			case InventorySection.EQUIP:
				inventoryEquipY += (sbyte)dy;
				if (inventoryEquipY < 0)
				{
					inventoryEquipY += 5;
				}
				else if (inventoryEquipY >= 5)
				{
					inventoryEquipY -= 5;
				}
				inventoryEquipX += (sbyte)dx;
				if (inventoryEquipX < 0)
				{
					if (inventoryEquipY == 0)
					{
						inventoryEquipX = 0;
						if (--inventoryBuffX < 0)
						{
							inventoryBuffX = 0;
						}
					}
					else
					{
						inventoryEquipX += 5;
					}
				}
				else if (inventoryEquipX >= 5)
				{
					if (inventoryEquipY == 0)
					{
						inventoryEquipX = 4;
						if (++inventoryBuffX > 5)
						{
							inventoryBuffX = 5;
						}
					}
					else
					{
						inventoryEquipX -= 5;
					}
				}
				if (inventoryEquipX < 1 || inventoryEquipX > 3 || inventoryEquipY < 1 || inventoryEquipY > 3)
				{
					UpdateEquip();
					return;
				}
				break;
			case InventorySection.HOUSING:
				if (dx != 0)
				{
					inventoryHousingX ^= 1;
				}
				inventoryHousingY += (sbyte)dy;
				if (inventoryHousingY < 0)
				{
					inventoryHousingY += 6;
				}
				else if (inventoryHousingY >= 6)
				{
					inventoryHousingY -= 6;
				}
				if (inventoryHousingX != 1 || inventoryHousingY != 5)
				{
					UpdateHousing();
					return;
				}
				break;
			}
		}
	}

	private bool UpdateCraftButtonInput(Recipe r)
	{
		if (stackSplit <= 1 && PadState.IsButtonDown(Buttons.A) && ActivePlayer.CanCraftRecipe(r))
		{
			CraftOnce(r);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Performs one craft. Split from the button check because the pointer reaches the same action
	/// and must not have to fake a gamepad button to do it.
	/// </summary>
	private void CraftOnce(Recipe r)
	{
		{
			short type = mouseItem.Type;
			short stack = mouseItem.Stack;
			mouseItem = r.CraftedItem;
			mouseItem.SetPrefix(-1);
			mouseItem.Stack += stack;
			mouseItem.Position.X = ActivePlayer.XYWH.X + 10 - (mouseItem.Width >> 1);
			mouseItem.Position.Y = ActivePlayer.XYWH.Y + 21 - (mouseItem.Height >> 1);
			mouseItemSrcSection = InventorySection.CRAFTING;
			mouseItemSrcX = -1;
			mouseItemSrcY = -1;
			r.Create(this);
			if (mouseItem.Type > 0 || r.CraftedItem.Type > 0)
			{
				Main.PlaySound(7);
			}
			if (type == 0)
			{
				if (stackSplit == 0)
				{
					stackSplit = 15;
				}
				else
				{
					stackSplit = stackDelay;
				}
				ActivePlayer.GetItem(ref mouseItem);
			}
		}
	}

	/// <summary>
	/// Pointer handling for the crafting grid: hover selects the recipe, click crafts it.
	///
	/// The recipe grid was the one inventory section still unreachable by mouse, because its cells
	/// are drawn with two independent scroll animations and the index-to-pixel mapping is not a
	/// division. The cells are recorded where they were drawn instead.
	/// </summary>
	private void UpdateCraftingPointer()
	{
		// A pointer does not need the slide animation: the highlight should be under the cursor
		// on the next frame, not twenty frames later. It also has a hard cost - cells are only
		// recorded while no animation is in flight, so after every wheel notch the grid was
		// unclickable for a third of a second. That was the "not everything presses" report.
		craftingRecipeScrollX = 0f;
		craftingRecipeScrollY = 0f;
		int col;
		int row;
		if (TryCraftingIngredientUnderPointer(out col, out row))
		{
			// Both panels are visible at once, so the pointer decides which one has the cursor;
			// there is no reason to make the player press the "switch section" button as well.
			if (craftingSection != CraftingSection.INGREDIENTS || craftingIngredientX != col || craftingIngredientY != row)
			{
				craftingSection = CraftingSection.INGREDIENTS;
				craftingIngredientX = (sbyte)col;
				craftingIngredientY = (sbyte)row;
			}
			// Clicking an ingredient selects it and shows its count; it does not craft. Crafting
			// by clicking a list of things you already have would be an accident waiting to
			// happen, and the recipe cell is the thing that means "make this".
			return;
		}
		int row2;
		int index;
		if (!TryCraftingRecipeUnderPointer(out row2, out index))
		{
			return;
		}
		craftingSection = CraftingSection.RECIPES;
		if (row2 != CraftingRecipeY)
		{
			// A different row starts from its first recipe: the window is shared, so carrying
			// the previous row's offset over would show a row starting in the middle. The index
			// from the cell rectangles was computed against the old window, so it is not used on
			// this frame - the next one records the row again and the pointer selects properly.
			CraftingRecipeY = (sbyte)row2;
			CraftingRecipeWindowX = 0;
			CraftingRecipeX = 0;
			return;
		}
		if (index != CraftingRecipeX)
		{
			// Only the cursor moves. The window must stay put, otherwise the row slides under
			// a pointer that has not moved and the next frame selects a different recipe - the
			// grid then scrolls by itself for as long as the pointer rests on it.
			CraftingRecipeX = (sbyte)index;
			Main.PlaySound(12);
		}
		if (!pointerActivate)
		{
			return;
		}
		// The recipe is read from the list rather than from CraftingRecipe: that field is only
		// updated while drawing, so on the frame of the click it still holds the previous one.
		if (row2 < 0 || row2 >= CurrentRecipeCategory.Count)
		{
			return;
		}
		System.Collections.Generic.List<short> list = CurrentRecipeCategory[row2].RecipeList;
		if (index < 0 || index >= list.Count)
		{
			return;
		}
		Recipe recipe = Main.ActiveRecipe[list[index]];
		CraftingRecipe = recipe;
		if (ActivePlayer.CanCraftRecipe(recipe))
		{
			CraftOnce(recipe);
		}
	}

	/// <summary>Which ingredient cell the pointer is over, or false.</summary>
	private bool TryCraftingIngredientUnderPointer(out int Col, out int Row)
	{
		Col = 0;
		Row = 0;
		if (!Terraria.Input.KeyboardMouseInput.Enabled || !Terraria.Input.KeyboardMouseInput.CursorOverride)
		{
			return false;
		}
		int num = Terraria.Input.KeyboardMouseInput.MouseX;
		int num2 = Terraria.Input.KeyboardMouseInput.MouseY;
		for (int i = 0; i < craftIngredientValid.Length; i++)
		{
			if (craftIngredientValid[i] && craftIngredientRects[i].Contains(num, num2))
			{
				Col = craftIngredientCol[i];
				Row = craftIngredientRow[i];
				return true;
			}
		}
		return false;
	}

	/// <summary>Which recipe cell the pointer is over, as (row, index within that row).</summary>
	private bool TryCraftingRecipeUnderPointer(out int Row, out int Index)
	{
		Row = -1;
		Index = -1;
		if (!Terraria.Input.KeyboardMouseInput.Enabled || !Terraria.Input.KeyboardMouseInput.CursorOverride)
		{
			return false;
		}
		int num = Terraria.Input.KeyboardMouseInput.MouseX;
		int num2 = Terraria.Input.KeyboardMouseInput.MouseY;
		for (int i = 0; i < craftSlotValid.Length; i++)
		{
			if (craftSlotValid[i] && craftSlotRects[i].Contains(num, num2))
			{
				Row = craftSlotRow[i];
				Index = craftSlotIndex[i];
				return true;
			}
		}
		return false;
	}

	private void PrevCraftingCategory()
	{
		int num = (int)craftingCategory;
		if (--num < 0)
		{
			num = 5;
		}
		SelectCraftingCategory(num);
	}

	/// <summary>
	/// The crafting category tab under the pointer, or false. Rectangles come from the draw call,
	/// so a category cannot be clicked in a place the tab is not drawn.
	/// </summary>
	private bool TryCraftingCategoryUnderPointer(out int Category)
	{
		Category = 0;
		for (int i = 0; i < craftingCategoryRects.Length; i++)
		{
			if (craftingCategoryRectValid[i] && craftingCategoryRects[i].Contains(MouseX, MouseY))
			{
				Category = i;
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Switches the crafting category and resets the recipe cursor. Shared by the shoulder
	/// buttons and the mouse, so a click and a button press cannot leave the panel in different
	/// states (the recipe list is rebuilt from these three fields only).
	/// </summary>
	private void SelectCraftingCategory(int Category)
	{
		if (Category < 0 || Category > 5)
		{
			return;
		}
		Recipe.FindRecipes(this, (Recipe.Category)Category, craftingShowCraftable);
		craftingCategory = (Recipe.Category)Category;
		CraftingRecipeX = 0;
		CraftingRecipeWindowX = 0;
		CraftingRecipeY = 0;
		craftingRecipeScrollX = 0f;
		craftingRecipeScrollY = 0f;
	}

	private void NextCraftingCategory()
	{
		int num = (int)craftingCategory;
		if (++num == 6)
		{
			num = 0;
		}
		SelectCraftingCategory(num);
	}

	public void PositionCraftingCursor(int dx, int dy)
	{
		if (IsButtonUntriggered(Buttons.X) && mouseItem.Type == 0)
		{
			craftingShowCraftable = !craftingShowCraftable;
			CraftingRecipeX = 0;
			CraftingRecipeWindowX = 0;
			craftingRecipeScrollX = 0f;
			craftingRecipeScrollY = 0f;
			Recipe.FindRecipes(this, craftingCategory, craftingShowCraftable);
		}
		else if (IsButtonUntriggered(Buttons.A))
		{
			Recipe.FindRecipes(this, craftingCategory, craftingShowCraftable);
		}
		else
		{
			// The pointer has already acted if it clicked this frame. Without this the synthetic
			// Buttons.A made a click anywhere in the crafting panel craft the selected recipe,
			// and a click on a recipe cell could craft twice.
			if (!inventoryAction.Taken && UpdateCraftButtonInput(CraftingRecipe))
			{
				return;
			}
			if (IsButtonTriggered(Buttons.Y))
			{
				craftingSection ^= CraftingSection.INGREDIENTS;
			}
			else if (IsButtonTriggered(Buttons.LeftTrigger))
			{
				PrevCraftingCategory();
			}
			else if (IsButtonTriggered(Buttons.RightTrigger))
			{
				NextCraftingCategory();
			}
			else if (dx != 0 || dy != 0)
			{
				if (craftingRecipeScrollMul >= 0.325f)
				{
					craftingRecipeScrollMul -= 0.075f;
				}
				if (craftingSection == CraftingSection.RECIPES)
				{
					if (dx != 0)
					{
						if (craftingRecipeScrollX != 0f || craftingRecipeScrollY != 0f || CurrentRecipeCategory.Count == 0)
						{
							return;
						}
						int count = CurrentRecipeCategory[CraftingRecipeY].RecipeList.Count;
						if (count != 1)
						{
							int num = CraftingRecipeWindowX + dx;
							if (num < 0)
							{
								num += count;
							}
							else if (num >= count)
							{
								num -= count;
							}
							// The stick moves the row and keeps the cursor on its first cell, the
							// way the console build did. The pointer is the case that needs them
							// apart (see CraftingRecipeWindowX).
							CraftingRecipeWindowX = (sbyte)num;
							CraftingRecipeX = (sbyte)num;
							craftingRecipeScrollX = dx;
						}
					}
					else if (craftingRecipeScrollX == 0f && craftingRecipeScrollY == 0f)
					{
						CraftingRecipeX = 0;
						CraftingRecipeWindowX = 0;
						int num2 = CraftingRecipeY + dy;
						int count2 = CurrentRecipeCategory.Count;
						if (num2 < 0)
						{
							num2 += count2;
						}
						else if (num2 >= count2)
						{
							num2 -= count2;
						}
						else
						{
							craftingRecipeScrollY = dy;
						}
						CraftingRecipeY = (sbyte)num2;
					}
				}
				else
				{
					int num3 = craftingIngredientX;
					num3 += dx;
					if (num3 < 0)
					{
						num3 += 4;
					}
					else if (num3 >= 4)
					{
						num3 -= 4;
					}
					craftingIngredientX = (sbyte)num3;
					int num4 = craftingIngredientY;
					num4 += dy;
					if (num4 < 0)
					{
						num4 += 3;
					}
					else if (num4 >= 3)
					{
						num4 -= 3;
					}
					craftingIngredientY = (sbyte)num4;
				}
			}
			else
			{
				craftingRecipeScrollMul = 0.8125f;
			}
		}
	}

	public void PositionMapScreenCursor(int dx, int dy)
	{
		if (IsButtonTriggered(Buttons.A))
		{
			Main.PlaySound(10);
			if (mapScreenCursorX == 0)
			{
				if (mapScreenCursorY < 2)
				{
					pvpCooldown = 180;
					pvpSelected = !pvpSelected;
				}
				else if (CanViewGamerCard() && Netplay.Session != null)
				{
					GamerCollection<NetworkGamer> allGamers = Netplay.Session.AllGamers;
					int num = mapScreenCursorY - 2;
					if (num < allGamers.Count)
					{
						NetworkGamer gamer = allGamers[num];
						ShowGamerCard(gamer);
					}
				}
			}
			else
			{
				teamCooldown = 180;
				if (mapScreenCursorY == 0)
				{
					teamSelected = (byte)(mapScreenCursorX - 1);
				}
				else
				{
					teamSelected = (byte)((mapScreenCursorX != 1) ? ((uint)(mapScreenCursorX + 1)) : 0u);
				}
			}
			return;
		}
		if (IsButtonTriggered(Buttons.X) && CanCommunicate())
		{
			if (Main.NetMode <= 0 || Netplay.gamer == null || Guide.IsVisible)
			{
				return;
			}
			Main.PlaySound(10);
			bool flag;
			do
			{
				flag = false;
				try
				{
					Guide.ShowGameInvite(controller, null);
				}
				catch (GuideAlreadyVisibleException)
				{
					Thread.Sleep(32);
					flag = true;
				}
			}
			while (flag);
			return;
		}
		if (IsButtonTriggered(Buttons.Y))
		{
			if (SignedInGamer.PartySize > 1 && Main.NetMode > 0 && Netplay.gamer != null && localGamer != null)
			{
				localGamer.SendPartyInvites();
			}
			return;
		}
		if (dy != 0)
		{
			do
			{
				mapScreenCursorY += dy;
			}
			while (mapScreenCursorX == 0 && mapScreenCursorY == 1);
			dy = ((mapScreenCursorX > 0) ? 2 : 10);
			if (mapScreenCursorY < 0)
			{
				mapScreenCursorY += dy;
			}
			else if (mapScreenCursorY >= dy)
			{
				mapScreenCursorY -= dy;
			}
		}
		if (mapScreenCursorY < 2)
		{
			mapScreenCursorX += dx;
			if (mapScreenCursorX < 0)
			{
				mapScreenCursorX += 4;
			}
			else if (mapScreenCursorX >= 4)
			{
				mapScreenCursorX -= 4;
			}
		}
	}

	public void FoundPotentialArmor(int itemType)
	{
		if (!TriggerCheckEnabled(Trigger.CollectedAllArmor))
		{
			return;
		}
		armorFound.Set(itemType, value: true);
		bool flag = true;
		EntityID.ItemID[] armorIDs = Item.ArmorIDs;
		EntityID.ItemID[] array = armorIDs;
		foreach (EntityID.ItemID index in array)
		{
			if (!armorFound.Get((int)index))
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			SetTriggerState(Trigger.CollectedAllArmor);
		}
	}

	private void UpdateAlternateGrappleControls()
	{
		if (alternateGrappleControls)
		{
			BTN_JUMP2 = Buttons.LeftTrigger;
			BTN_GRAPPLE = Buttons.LeftStick;
		}
		else
		{
			BTN_JUMP2 = Buttons.LeftStick;
			BTN_GRAPPLE = Buttons.LeftTrigger;
		}
	}

	public bool IsJumpButtonDown()
	{
		if (!PadState.IsButtonDown(Buttons.A))
		{
			return PadState.IsButtonDown(BTN_JUMP2);
		}
		return true;
	}

	public bool WasJumpButtonUp()
	{
		if (gpPrevState.IsButtonUp(Buttons.A))
		{
			return gpPrevState.IsButtonUp(BTN_JUMP2);
		}
		return false;
	}

	public void ShowGamerCard(Gamer gamer)
	{
		if (gamer == null || Guide.IsVisible)
		{
			return;
		}
		bool flag;
		do
		{
			flag = false;
			try
			{
				Guide.ShowGamerCard(controller, gamer);
			}
			catch (GuideAlreadyVisibleException)
			{
				Thread.Sleep(32);
				flag = true;
			}
			catch (ObjectDisposedException)
			{
				gamer = Gamer.GetFromGamertag(gamer.Gamertag);
				flag = true;
			}
			catch (Exception)
			{
			}
		}
		while (flag);
	}

	public void SignOut()
	{
		if (SignedInGamer == null)
		{
			return;
		}
		SignedInGamer signedInGamer = SignedInGamer;
		SignedInGamer = null;
		foreach (SignedInGamer signedInGamer3 in Gamer.SignedInGamers)
		{
			if (signedInGamer3.PlayerIndex == controller && signedInGamer3.Gamertag == signedInGamer.Gamertag)
			{
				SignedInGamer = signedInGamer3;
			}
		}
		if (SignedInGamer != null)
		{
			if (Main.NetMode > 0 && !HasOnline())
			{
				Error(Lang.MenuText[5], Lang.InterfaceText[36]);
				MainUI.ExitGame();
			}
			else
			{
				if (!Main.IsGameStarted)
				{
					return;
				}
				wasRemovedFromSessionWithoutOurConsent = true;
				if (this == MainUI)
				{
					for (int i = 0; i < Netplay.Session.LocalGamers.Count; i++)
					{
						SignedInGamer signedInGamer2 = Netplay.Session.LocalGamers[i].SignedInGamer;
						Main.UIInstance[(int)signedInGamer2.PlayerIndex].wasRemovedFromSessionWithoutOurConsent = true;
					}
				}
			}
			return;
		}
		wasRemovedFromSessionWithoutOurConsent = false;
		MessageBox.RemoveMessagesFor(controller);
		CancelInvite(signedInGamer);
		blacklist.Clear();
		if (this == MainUI)
		{
			Netplay.StopFindingSessions();
			if (Main.WorldGenThread != null)
			{
				Main.WorldGenThread.Abort();
				Main.WorldGenThread = null;
				WorldGen.Gen = false;
			}
			if (playerStorage != null)
			{
				theGame.Components.Remove(playerStorage);
				playerStorage.Dispose();
				playerStorage = null;
			}
			if (NumActiveViews == 1 || Main.IsGameStarted)
			{
				if (Main.IsGameStarted)
				{
					ExitGame();
				}
				SetMenu(MenuMode.WELCOME, rememberPrevious: false, reset: true);
				return;
			}
		}
		Exit();
	}

	public bool TestStorageSpace(string container, string destinationPath, int writeSize)
	{
		using (StorageContainer storageContainer = OpenPlayerStorage(container))
		{
			long num = storageContainer.StorageDevice.FreeSpace;
			if (num >= writeSize)
			{
				return true;
			}
			if (storageContainer.FileExists(destinationPath))
			{
				using (Stream stream = storageContainer.OpenFile(destinationPath, FileMode.Open, FileAccess.Read))
				{
					num += stream.Length;
				}
				if (num >= writeSize)
				{
					storageContainer.DeleteFile(destinationPath);
					return true;
				}
			}
		}
		MessageBox.Show(controller, Lang.MenuText[5], Lang.InterfaceText[70], new string[1] { Lang.MenuText[90] });
		return false;
	}

	public void WriteError()
	{
		MessageBox.Show(controller, Lang.MenuText[5], Lang.WorldGenText[54], new string[1] { Lang.MenuText[90] });
	}

	public void ReadError()
	{
		MessageBox.Show(controller, Lang.MenuText[9], Lang.WorldGenText[53], new string[1] { Lang.MenuText[90] });
	}

	/// <summary>
	/// One line describing where the UI was, written into the crash log.
	///
	/// Without it a crash report says "IndexOutOfRangeException in UpdateMenu" and nothing else -
	/// that method handles every screen in the game, so the report cannot be acted on. With it,
	/// the next report names the screen, the section and the highlighted row.
	/// </summary>
	public string DebugState()
	{
		return "menu=" + CurMenuMode + " type=" + CurMenuType + " inventory=" + InventoryMode
			+ " section=" + ActiveInvSection + " rows=" + numMenuItems
			+ " selected=" + selectedMenu + " focus=" + focusMenu + " cursor=" + uiX + "," + uiY
			+ " items=" + numLoadPlayers;
	}

	public void CheckHDTV()
	{
		if (!Main.IsHDTV)
		{
			Main.IsHDTV = true;
			MessageBox.Show(controller, Lang.MenuText[3], Lang.InterfaceText[71], new string[1] { Lang.MenuText[90] });
		}
	}

	public bool CheckBlacklist()
	{
		ulong worldId = Main.GetWorldId();
		for (int num = blacklist.Count - 1; num >= 0; num--)
		{
			if (blacklist[num] == worldId)
			{
				if (CurMenuType != MenuType.MAIN)
				{
					CurMenuType = MenuType.PAUSE;
				}
				string[] array = new string[2];
				if (CurMenuType == MenuType.MAIN)
				{
					array[0] = Lang.MenuText[15];
				}
				else
				{
					array[0] = Lang.MenuText[100];
				}
				array[1] = Lang.InterfaceText[75];
				MessageBox.Show(controller, Lang.MenuText[3], Lang.InterfaceText[74], array, ShouldAutoUpdate: false);
				SetMenu(MenuMode.BLACKLIST_REMOVE);
				return true;
			}
		}
		return false;
	}

	public void CheckUserGeneratedContent()
	{
		if (!IsUserGeneratedContentAllowed())
		{
			CurMenuType = MenuType.PAUSE;
			string[] boxOptions = ((!autoSave && IsStorageEnabledForAnyPlayer()) ? new string[2]
			{
				Lang.InterfaceText[2],
				Lang.InterfaceText[1]
			} : new string[1] { Lang.MenuText[15] });
			MessageBox.Show(controller, Lang.MenuText[3], Lang.InterfaceText[79], boxOptions, ShouldAutoUpdate: false);
			SetMenu(MenuMode.EXIT_UGC_BLOCKED);
		}
	}
}
