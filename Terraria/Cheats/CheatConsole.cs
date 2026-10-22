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
		/// <summary>Key into <see cref="Texts"/>, not the text: the help list is translated too.</summary>
		public readonly string Help;
		public readonly Action<string[]> Run;

		/// <summary>
		/// True when the command only means anything in single player.
		///
		/// A debug build may run the console while hosting (see <see cref="DebugHostConsole"/>), and
		/// the world commands - spawn, time, invasions - do something there. The player-flag ones do
		/// not: <c>CheatState.GodModeActive</c> and friends are gated on single player, so setting
		/// the flag online would answer "on" and change nothing. Saying so is better than a switch
		/// that silently does nothing.
		/// </summary>
		public readonly bool SinglePlayerOnly;

		public Command(string name, string args, string help, Action<string[]> run, bool singlePlayerOnly = false)
		{
			Name = name;
			Args = args;
			Help = help;
			Run = run;
			SinglePlayerOnly = singlePlayerOnly;
		}
	}

	private static readonly List<Command> Commands = Build();

	private static List<Command> Build()
	{
		return new List<Command>
		{
			new Command("god", "[on|off]", "chat.cheats.help.god", delegate(string[] a) { CheatState.GodMode = Toggle(a, CheatState.GodMode); Echo(Texts.Get("chat.cheats.god"), CheatState.GodMode); }, singlePlayerOnly: true),
			new Command("noclip", "[on|off]", "chat.cheats.help.noclip", delegate(string[] a) { CheatState.NoClip = Toggle(a, CheatState.NoClip); Echo(Texts.Get("chat.cheats.noclip"), CheatState.NoClip); }, singlePlayerOnly: true),
			new Command("infmana", "[on|off]", "chat.cheats.help.mana", delegate(string[] a) { CheatState.InfiniteMana = Toggle(a, CheatState.InfiniteMana); Echo(Texts.Get("chat.cheats.mana"), CheatState.InfiniteMana); }, singlePlayerOnly: true),
			new Command("infbuff", "[on|off]", "chat.cheats.help.buffs", delegate(string[] a) { CheatState.InfiniteBuffs = Toggle(a, CheatState.InfiniteBuffs); Echo(Texts.Get("chat.cheats.buffs"), CheatState.InfiniteBuffs); }, singlePlayerOnly: true),
			new Command("heal", "", "chat.cheats.help.heal", delegate(string[] a)
			{
				Player player = UI.MainUI.ActivePlayer;
				player.statLife = player.StatLifeMax;
				player.statMana = player.statManaMax2;
				Echo(Texts.Get("chat.cheats.healed"), value: true);
			}),
			new Command("time", "<dawn|noon|dusk|midnight>", "chat.cheats.help.time", SetTime),
			new Command("bloodmoon", "[on|off]", "chat.cheats.help.bloodmoon", delegate(string[] a)
			{
				Main.GameTime.IsBloodMoon = Toggle(a, Main.GameTime.IsBloodMoon);
				Echo(Texts.Get("chat.cheats.bloodmoon"), Main.GameTime.IsBloodMoon);
			}),
			new Command("give", "<item> [count]", "chat.cheats.help.give", Give),
			new Command("tp", "<x> <y>", "chat.cheats.help.tp", Teleport),
			new Command("spawn", "<npcId>", "chat.cheats.help.spawn", Spawn),
			new Command("meteor", "", "chat.cheats.help.meteor", delegate(string[] a) { WorldGen.ToSpawnMeteor = true; Echo(Texts.Get("chat.cheats.meteor"), value: true); }),
			new Command("killall", "", "chat.cheats.help.killall", delegate(string[] a)
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
				Echo(Texts.Format("chat.cheats.killall", num), value: true);
			}),
			new Command("help", "", "chat.cheats.help.help", delegate(string[] a)
			{
				Echo(Texts.Get("chat.cheats.list"), value: true);
				for (int i = 0; i < Commands.Count; i++)
				{
					Command command = Commands[i];
					Echo("  !" + command.Name + " " + command.Args + " - " + Texts.Get(command.Help), value: true);
				}
			})
#if DEBUG
			// Developer commands. Compiled only into Debug builds: a Release binary must not
			// contain a way to hand yourself every item, and the preprocessor guarantees that
			// rather than a setting someone can flip.
			,
			new Command("debugkit", "[default|pvp|pve|full]", "chat.debugkit.help", delegate(string[] a)
			{
				DebugKit.Kit kit = DebugKit.Kit.Default;
				if (a.Length != 0 && !DebugKit.Parse(a[0], out kit))
				{
					Main.NewText(Texts.Format("chat.debugkit.kits", DebugKit.List), 255, 200, 120);
					return;
				}
				DebugKit.ApplyEssentials(UI.MainUI.ActivePlayer);
				int num = DebugKit.Give(UI.MainUI.ActivePlayer, kit);
				Echo(Texts.Format("chat.debugkit.given", kit.ToString().ToLowerInvariant(), num), value: true);
			}),
			new Command("debugsave", "", "chat.debugkit.save", delegate(string[] a)
			{
				// Uses the same save the game performs on quit, so a debug character survives a
				// restart and can be loaded from the normal character list.
				WorldGen.savePlayerWhilePlaying();
				Echo(Texts.Get("chat.debugkit.saved"), value: true);
			}),
			new Command("allitems", "[page]", "chat.debugkit.allitems", delegate(string[] a)
			{
				int result = 0;
				if (a.Length != 0 && !int.TryParse(a[0], out result))
				{
					Main.NewText(Texts.Format("chat.debugkit.usage", DebugKit.PageCount - 1), 255, 200, 120);
					return;
				}
				int num = DebugKit.GivePage(UI.MainUI.ActivePlayer, result);
				if (num == 0)
				{
					Main.NewText(Texts.Format("chat.debugkit.usage", DebugKit.PageCount - 1), 255, 200, 120);
					return;
				}
				Echo(Texts.Format("chat.debugkit.items", num, result, DebugKit.PageCount - 1), value: true);
			}),
			new Command("debugworld", "", "chat.debugworld.help", delegate(string[] a)
			{
				string error;
				if (!DebugWorld.Build(UI.MainUI.ActivePlayer, out error))
				{
					Main.NewText(Texts.Format("chat.debugworld.failed", error), 255, 130, 120);
					return;
				}
				// The count is reported because the room is sized from the item table: with mods
				// it is 70 chests, and a partial room (an obstructed spot, a hole in the floor) has
				// to be visible rather than something the player discovers by searching.
				if (DebugWorld.LastPlacedItems < DebugWorld.LastWantedItems)
				{
					Main.NewText(Texts.Format("chat.debugworld.partial",
						DebugWorld.LastPlacedItems, DebugWorld.LastWantedItems), 255, 200, 120);
					return;
				}
				Echo(Texts.Format("chat.debugworld.done", DebugWorld.LastPlacedItems), value: true);
			}),
			// Spawning by name: the ids are what the wiki and the debug world use, and nobody
			// remembers that the Eye of Cthulhu is 4. Accepts either, so an existing habit and a
			// readable name both work.
			new Command("spawnnpc", "<id|name> [count]", "chat.debugnpc.help", delegate(string[] a)
			{
				if (a.Length == 0)
				{
					Main.NewText(Texts.Get("chat.debugnpc.usage"), 255, 200, 120);
					return;
				}
				// A name with spaces is the normal case ("Green Slime"), so everything up to the
				// last argument is the name and the last one may be a count.
				int count = 1;
				int nameEnd = a.Length;
				if (a.Length > 1 && int.TryParse(a[a.Length - 1], out count))
				{
					nameEnd = a.Length - 1;
				}
				if (count < 1)
				{
					count = 1;
				}
				if (count > 20)
				{
					count = 20;
				}
				string text = string.Join(" ", a, 0, nameEnd);
				int type = NpcLookup.Find(text);
				if (type <= 0)
				{
					Main.NewText(Texts.Format("chat.debugnpc.unknown", text), 255, 120, 120);
					return;
				}
				Player player = UI.MainUI.ActivePlayer;
				for (int i = 0; i < count; i++)
				{
					NPC.SpawnOnPlayer(player, type);
				}
				Echo(Texts.Format("chat.debugnpc.spawned", count, Lang.NpcName(type)), value: true);
			}),
			// Invasions by name. This build has two (the pirates arrived in 1.2, which is not this
			// game), so the list is short and says so instead of pretending.
			new Command("invasion", "<goblins|snow>", "chat.debuginvasion.help", delegate(string[] a)
			{
				string text = (a.Length == 0) ? "" : a[0].ToLowerInvariant();
				int num;
				if (text == "goblins" || text == "goblin" || text == "1")
				{
					num = 1;
				}
				else if (text == "snow" || text == "frost" || text == "2")
				{
					num = 2;
				}
				else
				{
					Main.NewText(Texts.Get("chat.debuginvasion.usage"), 255, 200, 120);
					return;
				}
				Main.StartInvasion(num);
				Echo(Texts.Format("chat.debuginvasion.started", num), value: true);
			})
#endif
		};
	}

	/// <summary>
	/// Debug builds only: the console also runs while this machine is hosting.
	///
	/// The reason it exists at all is the bug report "reproduce it on the server": cheats are
	/// forbidden online on purpose, but *debugging* a world someone else is standing in is exactly
	/// when you need to spawn an NPC or start an invasion. A release build has neither the commands
	/// nor this flag - the preprocessor removes both, so "debug mode, switched off" can never be
	/// turned back on by editing a file.
	/// </summary>
	private static bool DebugHostConsole
	{
		get
		{
#if DEBUG
			return Main.NetMode == 2;
#else
			return false;
#endif
		}
	}

	/// <summary>
	/// Whether the console may run on this machine at all.
	///
	/// The single gate for both the single-player cheats and the Debug-build host console, so a
	/// new caller cannot half-open one of them.
	/// </summary>
	internal static bool CanRunHere => CheatState.IsAvailable || DebugHostConsole;

	/// <summary>
	/// True when a command is one whose effect is gated on single player. Exposed so the self-test
	/// can check the classification without running the command (running it would touch the UI).
	/// </summary>
	internal static bool IsSinglePlayerOnly(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		string text = name.TrimStart('!', '/').ToLowerInvariant();
		for (int i = 0; i < Commands.Count; i++)
		{
			if (Commands[i].Name == text)
			{
				return Commands[i].SinglePlayerOnly;
			}
		}
		return false;
	}

	/// <summary>True when the text should be treated as a cheat command.</summary>
	public static bool IsCommand(string text)
	{
		return !string.IsNullOrEmpty(text) && (text[0] == '!' || text[0] == '/');
	}

	/// <summary>Command names, for tab completion in the chat prompt.</summary>
	public static string[] CommandNames
	{
		get
		{
			string[] array = new string[Commands.Count];
			for (int i = 0; i < Commands.Count; i++)
			{
				array[i] = Commands[i].Name;
			}
			return array;
		}
	}

	/// <summary>
	/// Runs a command. Returns true when it was recognized, so the caller knows whether the
	/// text should still be broadcast as normal chat.
	/// </summary>
	public static bool TryExecute(string text, out string error)
	{
		error = null;
		bool flag = DebugHostConsole;
		if (!CanRunHere)
		{
			// Reported as "handled": this is a local command, so it must never be broadcast
			// as chat, and the player has to see why nothing happened.
			error = CheatState.Enabled
				? Texts.Format("chat.cheats.unavailable.connected", Main.NetMode == 2 ? "host" : "client")
				: Texts.Get("chat.cheats.unavailable.disabled");
			Main.NewText(error, 255, 200, 120);
			return true;
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
			if (Commands[i].Name != text2)
			{
				continue;
			}
			if (flag && Commands[i].SinglePlayerOnly)
			{
				// The flag would be set and nothing would happen, because the effects are gated on
				// single player. Saying that is the difference between "broken" and "by design".
				error = Texts.Format("chat.cheats.host.local.only", text2);
				Main.NewText(error, 255, 200, 120);
				return true;
			}
			try
			{
				Commands[i].Run(array2);
			}
			catch (Exception ex)
			{
				error = Texts.Format("chat.cheats.failed", ex.Message);
				Main.NewText(error, 255, 120, 120);
			}
			return true;
		}
		error = Texts.Format("chat.cheats.unknown", text2);
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
		Main.NewText(Texts.Format("chat.cheats.state", what, Texts.Get(value ? "chat.cheats.on" : "chat.cheats.off")), 50, 255, 130);
	}

	private static void SetTime(string[] args)
	{
		if (args.Length == 0)
		{
			Main.NewText(Texts.Get("chat.cheats.usage.time"), 255, 200, 120);
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
			Main.NewText(Texts.Get("chat.cheats.usage.time"), 255, 200, 120);
			return;
		}
		Echo(Texts.Format("chat.cheats.time", args[0].ToLowerInvariant()), value: true);
	}

	private static void Give(string[] args)
	{
		if (args.Length == 0)
		{
			Main.NewText(Texts.Get("chat.cheats.usage.give"), 255, 200, 120);
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
					Main.NewText(Texts.Format("chat.cheats.no.item", text), 255, 120, 120);
					return;
				}
				item.Stack = (short)Math.Min(num, (int)item.MaxStack);
				activePlayer.Inventory[i] = item;
				Echo(Texts.Format("chat.cheats.gave", text, item.Stack), value: true);
				return;
			}
		}
		Main.NewText(Texts.Get("chat.cheats.full"), 255, 120, 120);
	}

	private static void Teleport(string[] args)
	{
		int result;
		int result2;
		if (args.Length < 2 || !int.TryParse(args[0], out result) || !int.TryParse(args[1], out result2))
		{
			Main.NewText(Texts.Get("chat.cheats.usage.tp"), 255, 200, 120);
			return;
		}
		Player activePlayer = UI.MainUI.ActivePlayer;
		activePlayer.Position.X = result * 16;
		activePlayer.Position.Y = result2 * 16;
		activePlayer.velocity = default;
		Echo(Texts.Format("chat.cheats.teleported", result, result2), value: true);
	}

	private static void Spawn(string[] args)
	{
		int result;
		if (args.Length < 1 || !int.TryParse(args[0], out result))
		{
			Main.NewText(Texts.Get("chat.cheats.usage.spawn"), 255, 200, 120);
			return;
		}
		NPC.SpawnOnPlayer(UI.MainUI.ActivePlayer, result);
		Echo(Texts.Format("chat.cheats.spawned", result), value: true);
	}
}
