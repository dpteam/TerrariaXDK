using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Terraria.Cheats;

/// <summary>
/// Console-style commands typed into the chat, modelled on the <c>ModClass</c> command
/// console from the old PC builds (refs/T1061C). Commands are available only where cheats
/// are allowed (<see cref="CheatState.IsAvailable"/>), so a client can never use them to
/// cheat on someone else's server.
/// </summary>
internal static class CheatConsole
{
	private sealed class Command
	{
		public readonly string Name;
		public readonly string Args;
		public readonly string Help;
		public readonly Action<string[]> Run;

		public Command(string name, string args, string help, Action<string[]> run)
		{
			Name = name;
			Args = args;
			Help = help;
			Run = run;
		}
	}

	private static readonly List<Command> Commands = Build();

	private static List<Command> Build()
	{
		return new List<Command>
		{
			new Command("god", "[on|off]", "toggle god mode", delegate(string[] a) { CheatState.GodMode = Toggle(a, CheatState.GodMode); Echo("god mode", CheatState.GodMode); }),
			new Command("noclip", "[on|off]", "toggle no-clip", delegate(string[] a) { CheatState.NoClip = Toggle(a, CheatState.NoClip); Echo("no-clip", CheatState.NoClip); }),
			new Command("infmana", "[on|off]", "infinite mana", delegate(string[] a) { CheatState.InfiniteMana = Toggle(a, CheatState.InfiniteMana); Echo("infinite mana", CheatState.InfiniteMana); }),
			new Command("infbuff", "[on|off]", "infinite buffs", delegate(string[] a) { CheatState.InfiniteBuffs = Toggle(a, CheatState.InfiniteBuffs); Echo("infinite buffs", CheatState.InfiniteBuffs); }),
			new Command("heal", "", "restore health and mana", delegate(string[] a)
			{
				Player player = UI.MainUI.ActivePlayer;
				player.statLife = player.StatLifeMax;
				player.statMana = player.statManaMax2;
				Echo("healed", value: true);
			}),
			new Command("time", "<dawn|noon|dusk|midnight>", "set the time of day", SetTime),
			new Command("bloodmoon", "[on|off]", "toggle the blood moon", delegate(string[] a)
			{
				Main.GameTime.IsBloodMoon = Toggle(a, Main.GameTime.IsBloodMoon);
				Echo("blood moon", Main.GameTime.IsBloodMoon);
			}),
			new Command("give", "<item> [count]", "add an item to the inventory", Give),
			new Command("tp", "<x> <y>", "teleport to tile coordinates", Teleport),
			new Command("spawn", "<npcId>", "spawn an NPC at your position", Spawn),
			new Command("meteor", "", "drop a meteor", delegate(string[] a) { WorldGen.ToSpawnMeteor = true; Echo("meteor incoming", value: true); }),
			new Command("killall", "", "remove all enemies", delegate(string[] a)
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
				Echo("removed " + num + " enemies", value: true);
			}),
			new Command("help", "", "list commands", delegate(string[] a)
			{
				Echo("cheat commands (!cmd):", value: true);
				for (int i = 0; i < Commands.Count; i++)
				{
					Command command = Commands[i];
					Echo("  !" + command.Name + " " + command.Args + " - " + command.Help, value: true);
				}
			})
		};
	}

	/// <summary>True when the text should be treated as a cheat command.</summary>
	public static bool IsCommand(string text)
	{
		return !string.IsNullOrEmpty(text) && (text[0] == '!' || text[0] == '/');
	}

	/// <summary>
	/// Runs a command. Returns true when it was recognized, so the caller knows whether the
	/// text should still be broadcast as normal chat.
	/// </summary>
	public static bool TryExecute(string text, out string error)
	{
		error = null;
		if (!CheatState.IsAvailable)
		{
			error = "Cheats are disabled outside single player.";
			return false;
		}
		string[] array = SplitArgs(text.Substring(1));
		if (array.Length == 0)
		{
			return false;
		}
		string text2 = array[0].ToLowerInvariant();
		string[] array2 = new string[array.Length - 1];
		Array.Copy(array, 1, array2, 0, array2.Length);
		for (int i = 0; i < Commands.Count; i++)
		{
			if (Commands[i].Name == text2)
			{
				try
				{
					Commands[i].Run(array2);
				}
				catch (Exception ex)
				{
					error = "Command failed: " + ex.Message;
					Main.NewText(error, 255, 120, 120);
				}
				return true;
			}
		}
		error = "Unknown command '!" + text2 + "'. Type !help.";
		Main.NewText(error, 255, 120, 120);
		return false;
	}

	private static string[] SplitArgs(string text)
	{
		return text.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
	}

	private static bool Toggle(string[] args, bool current)
	{
		if (args.Length == 0)
		{
			return !current;
		}
		string text = args[0].ToLowerInvariant();
		if (text == "on" || text == "1" || text == "true")
		{
			return true;
		}
		if (text == "off" || text == "0" || text == "false")
		{
			return false;
		}
		return !current;
	}

	private static void Echo(string what, bool value)
	{
		Main.NewText("Cheats: " + what + " " + (value ? "on" : "off"), 50, 255, 130);
	}

	private static void SetTime(string[] args)
	{
		if (args.Length == 0)
		{
			Main.NewText("Usage: !time <dawn|noon|dusk|midnight>", 255, 200, 120);
			return;
		}
		switch (args[0].ToLowerInvariant())
		{
		case "dawn":
			Main.GameTime.DayTime = true;
			Main.GameTime.WorldTime = 0f;
			break;
		case "noon":
			Main.GameTime.DayTime = true;
			Main.GameTime.WorldTime = 27000f;
			break;
		case "dusk":
			Main.GameTime.DayTime = true;
			Main.GameTime.WorldTime = 45900f;
			break;
		case "midnight":
			Main.GameTime.DayTime = false;
			Main.GameTime.WorldTime = 0f;
			break;
		default:
			Main.NewText("Usage: !time <dawn|noon|dusk|midnight>", 255, 200, 120);
			return;
		}
		Echo("time set to " + args[0].ToLowerInvariant(), value: true);
	}

	private static void Give(string[] args)
	{
		if (args.Length == 0)
		{
			Main.NewText("Usage: !give <item> [count]", 255, 200, 120);
			return;
		}
		int num = 1;
		int num2 = args.Length;
		int result;
		if (args.Length > 1 && int.TryParse(args[args.Length - 1], out result))
		{
			num = result;
			num2 = args.Length - 1;
		}
		string text = string.Join(" ", args, 0, num2);
		Player activePlayer = UI.MainUI.ActivePlayer;
		for (int i = 0; i < activePlayer.Inventory.Length; i++)
		{
			if (activePlayer.Inventory[i].Type == 0)
			{
				Item item = default;
				item.SetDefaults(text);
				if (item.Type <= 0)
				{
					Main.NewText("Unknown item: " + text, 255, 120, 120);
					return;
				}
				item.Stack = (short)Math.Min(num, (int)item.MaxStack);
				activePlayer.Inventory[i] = item;
				Echo("gave " + text + " x" + item.Stack, value: true);
				return;
			}
		}
		Main.NewText("Inventory is full", 255, 120, 120);
	}

	private static void Teleport(string[] args)
	{
		int result;
		int result2;
		if (args.Length < 2 || !int.TryParse(args[0], out result) || !int.TryParse(args[1], out result2))
		{
			Main.NewText("Usage: !tp <x> <y>  (tile coordinates)", 255, 200, 120);
			return;
		}
		Player activePlayer = UI.MainUI.ActivePlayer;
		activePlayer.Position.X = result * 16;
		activePlayer.Position.Y = result2 * 16;
		activePlayer.velocity = default;
		Echo("teleported to " + result + "," + result2, value: true);
	}

	private static void Spawn(string[] args)
	{
		int result;
		if (args.Length < 1 || !int.TryParse(args[0], out result))
		{
			Main.NewText("Usage: !spawn <npcId>", 255, 200, 120);
			return;
		}
		NPC.SpawnOnPlayer(UI.MainUI.ActivePlayer, result);
		Echo("spawned npc " + result, value: true);
	}
}
