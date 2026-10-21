using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Cheats;

/// <summary>
/// Single-player cheat menu, modelled after the command console from the old PC builds
/// (refs/T1061C ModClass) but driven through the console-style UI instead of chat commands.
/// The menu reuses the game's existing menu renderer: rows are published through
/// <see cref="UI"/>'s menuString array, so navigation, mouse hover and scaling come for free.
/// </summary>
internal static class CheatMenu
{
	public const int MenuTop = 200;

	public const int MenuSpace = 38;

	/// <summary>The UI menu renderer supports 14 rows; one is reserved for the page switch.</summary>
	public const int ActionsPerPage = 13;

	public const int ActionCount = 18;

	/// <summary>Coordinates used by the UI's virtual cursor while the menu is open.</summary>
	public static readonly Location[] Coords = new Location[ActionsPerPage + 1];

	private static int _page;

	public static int PageCount => (ActionCount + ActionsPerPage - 1) / ActionsPerPage;

	public static int CurrentPage => _page;

	/// <summary>Builds the rows for the current page, including a live state suffix.</summary>
	public static string[] BuildRows()
	{
		int num = _page * ActionsPerPage;
		int num2 = System.Math.Min(num + ActionsPerPage, ActionCount);
		int num3 = num2 - num;
		bool flag = PageCount > 1;
		string[] array = new string[num3 + (flag ? 1 : 0)];
		for (int i = 0; i < num3; i++)
		{
			array[i] = Label(num + i);
		}
		if (flag)
		{
			array[num3] = Texts.Format("ui.cheats.page", _page + 1, PageCount);
		}
		return array;
	}

	private static string Label(int action)
	{
		switch (action)
		{
		case 0:
			return Texts.Format("ui.cheats.god", OnOff(CheatState.GodMode));
		case 1:
			return Texts.Format("ui.cheats.noclip", OnOff(CheatState.NoClip));
		case 2:
			return Texts.Format("ui.cheats.mana", OnOff(CheatState.InfiniteMana));
		case 3:
			return Texts.Format("ui.cheats.buffs", OnOff(CheatState.InfiniteBuffs));
		case 4:
			return Texts.Format("ui.cheats.fall", OnOff(CheatState.NoFallDamage));
		case 5:
			return Texts.Get("ui.cheats.heal");
		case 6:
			return Texts.Get("ui.cheats.caps");
		case 7:
			return Texts.Get("ui.cheats.dawn");
		case 8:
			return Texts.Get("ui.cheats.noon");
		case 9:
			return Texts.Get("ui.cheats.dusk");
		case 10:
			return Texts.Get("ui.cheats.midnight");
		case 11:
			return Texts.Format("ui.cheats.bloodmoon", OnOff(Main.GameTime.IsBloodMoon));
		case 12:
			return Texts.Get("ui.cheats.eye");
		case 13:
			return Texts.Get("ui.cheats.skeletron");
		case 14:
			return Texts.Get("ui.cheats.killall");
		case 15:
			return Texts.Get("ui.cheats.meteor");
		case 16:
			return Texts.Format("ui.cheats.recipes", OnOff(Main.UnlockAllRecipes));
		case 17:
			return Texts.Get("ui.cheats.save");
		default:
			return "?";
		}
	}

	private static string OnOff(bool value)
	{
		return Texts.Get(value ? "ui.common.on.upper" : "ui.common.off.upper");
	}

	/// <summary>Refreshes the cursor coordinates shared with the UI menu navigation.</summary>
	public static void SyncLayout(string[] rows)
	{
		int num = Main.ResolutionWidth >> 1;
		for (int i = 0; i < Coords.Length; i++)
		{
			if (i < rows.Length)
			{
				Coords[i].X = (short)num;
				Coords[i].Y = (short)((float)(MenuTop + MenuSpace * i) * Main.ScreenMultiplier);
			}
			else
			{
				Coords[i].X = 0;
				Coords[i].Y = 0;
			}
		}
	}

	/// <summary>Runs the action behind a visible row.</summary>
	public static void Activate(int row)
	{
		string[] array = BuildRows();
		if (row < 0 || row >= array.Length)
		{
			return;
		}
		if (PageCount > 1 && row == array.Length - 1)
		{
			_page = (_page + 1) % PageCount;
			Main.PlaySound(12);
			return;
		}
		if (!CheatState.IsAvailable)
		{
			Main.NewText(Texts.Get("ui.cheats.single.only"), 255, 120, 120);
			return;
		}
		Player activePlayer = UI.MainUI?.ActivePlayer;
		if (activePlayer == null)
		{
			return;
		}
		int num = _page * ActionsPerPage + row;
		switch (num)
		{
		case 0:
			CheatState.GodMode = !CheatState.GodMode;
			break;
		case 1:
			CheatState.NoClip = !CheatState.NoClip;
			break;
		case 2:
			CheatState.InfiniteMana = !CheatState.InfiniteMana;
			break;
		case 3:
			CheatState.InfiniteBuffs = !CheatState.InfiniteBuffs;
			break;
		case 4:
			CheatState.NoFallDamage = !CheatState.NoFallDamage;
			break;
		case 5:
			activePlayer.statLife = activePlayer.StatLifeMax;
			activePlayer.statMana = activePlayer.statManaMax2;
			break;
		case 6:
			activePlayer.StatLifeMax = 500;
			activePlayer.statManaMax = 200;
			activePlayer.statManaMax2 = 200;
			activePlayer.statLife = 500;
			activePlayer.statMana = 200;
			break;
		case 7:
			SetTime(day: true, 0f);
			break;
		case 8:
			SetTime(day: true, 27000f);
			break;
		case 9:
			SetTime(day: true, 45900f);
			break;
		case 10:
			SetTime(day: false, 0f);
			break;
		case 11:
			Main.GameTime.IsBloodMoon = !Main.GameTime.IsBloodMoon;
			break;
		case 12:
			NPC.SpawnOnPlayer(activePlayer, 4);
			break;
		case 13:
			NPC.SpawnOnPlayer(activePlayer, 35);
			break;
		case 14:
			KillAllEnemies();
			break;
		case 15:
			WorldGen.ToSpawnMeteor = true;
			break;
		case 16:
			Main.UnlockAllRecipes = !Main.UnlockAllRecipes;
			break;
		case 17:
			WorldGen.saveAllWhilePlaying();
			break;
		default:
			return;
		}
		Main.PlaySound(12);
		Main.NewText(Texts.Format("ui.cheats.status", Label(num)), 50, 255, 130);
	}

	public static void ResetPage()
	{
		_page = 0;
	}

	private static void SetTime(bool day, float worldTime)
	{
		Main.GameTime.DayTime = day;
		Main.GameTime.WorldTime = worldTime;
	}

	private static void KillAllEnemies()
	{
		int num = 0;
		for (int i = Main.NPCSet.Length - 1; i >= 0; i--)
		{
			NPC nPC = Main.NPCSet[i];
			if (nPC != null && nPC.Active != 0 && !nPC.IsTownNPC && nPC.Life > 0)
			{
				nPC.Life = 0;
				nPC.Active = 0;
				num++;
			}
		}
		Main.NewText(Texts.Format("ui.cheats.removed", num), 255, 120, 120);
	}

	/// <summary>Backdrop drawn before the generic menu rows.</summary>
	public static void DrawBackdrop(SpriteBatch spriteBatch, int viewWidth)
	{
		// Same shape as the other screens added in this port (Display, HUD, Network): a dark panel
		// with a thin accent line and a title, rather than raw text over the frozen world. The
		// cheat list is long, and without a panel it read as part of the scene behind it.
		float num = Main.ScreenMultiplier;
		int num2 = System.Math.Max(1, Coords.Length);
		int num3 = (int)(520f * num);
		int num4 = (viewWidth - num3) / 2;
		int num5 = (int)((float)(MenuTop - 46) * num);
		int num6 = (int)((float)(num2 * MenuSpace + 76) * num);
		Main.DrawSolidRect(new Rectangle(0, 0, viewWidth, Main.ResolutionHeight), new Color(0, 0, 0, 170));
		Main.DrawSolidRect(new Rectangle(num4 - (int)(10f * num), num5, num3 + (int)(20f * num), num6), new Color(14, 18, 26, 240));
		Main.DrawSolidRect(new Rectangle(num4 - (int)(10f * num), num5, num3 + (int)(20f * num), 2), new Color(70, 110, 170));
		UI.DrawStringCC(UI.BigFont, Terraria.Texts.Get("ui.cheats.title"), viewWidth / 2, num5 + (int)(8f * num), Color.White);
		// Say out loud that this screen is single-player only: it is unreachable in multiplayer,
		// and the note stops the "why is the cheat menu missing" question.
		UI.DrawStringCC(UI.SmallFont, Terraria.Texts.Get("ui.cheats.single.only"), viewWidth / 2, num5 + num6 - (int)(24f * num), new Color(150, 160, 180));
	}
}
