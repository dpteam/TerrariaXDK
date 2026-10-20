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
			array[num3] = "< page " + (_page + 1) + "/" + PageCount + " >";
		}
		return array;
	}

	private static string Label(int action)
	{
		switch (action)
		{
		case 0:
			return "God mode: " + OnOff(CheatState.GodMode);
		case 1:
			return "No clip: " + OnOff(CheatState.NoClip);
		case 2:
			return "Infinite mana: " + OnOff(CheatState.InfiniteMana);
		case 3:
			return "Infinite buffs: " + OnOff(CheatState.InfiniteBuffs);
		case 4:
			return "No fall damage: " + OnOff(CheatState.NoFallDamage);
		case 5:
			return "Heal and refill mana";
		case 6:
			return "Max life and mana caps";
		case 7:
			return "Time: dawn";
		case 8:
			return "Time: noon";
		case 9:
			return "Time: dusk";
		case 10:
			return "Time: midnight";
		case 11:
			return "Blood moon: " + OnOff(Main.GameTime.IsBloodMoon);
		case 12:
			return "Spawn Eye of Cthulhu";
		case 13:
			return "Spawn Skeletron";
		case 14:
			return "Kill all enemies";
		case 15:
			return "Drop a meteor";
		case 16:
			return "Unlock all recipes: " + OnOff(Main.UnlockAllRecipes);
		case 17:
			return "Save world and player";
		default:
			return "?";
		}
	}

	private static string OnOff(bool value)
	{
		return value ? "ON" : "OFF";
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
			Main.NewText("Cheats are only available in single player", 255, 120, 120);
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
		Main.NewText("Cheats: " + Label(num), 50, 255, 130);
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
		Main.NewText("Removed " + num + " enemies", 255, 120, 120);
	}

	/// <summary>Backdrop drawn before the generic menu rows.</summary>
	public static void DrawBackdrop(SpriteBatch spriteBatch, int viewWidth)
	{
		Main.DrawRect(new Rectangle(0, 0, viewWidth, Main.ResolutionHeight), new Color(0, 0, 0, 170), ToCenter: false);
	}
}
