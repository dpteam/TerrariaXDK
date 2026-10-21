using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Terraria.Input;

/// <summary>
/// Rebindable keyboard actions. Bindings live in Settings.ini under [Keys] so a player can
/// change them without editing code, and the in-game screen edits the same values.
/// </summary>
internal static class KeyBindings
{
	/// <summary>Every rebindable action, in the order shown on the rebind screen.</summary>
	internal enum Action
	{
		MoveLeft,
		MoveRight,
		MoveUp,
		MoveDown,
		Jump,
		Inventory,
		Throw,
		SmartCursor,
		Pause,
		Map,
		Heal,
		Mana,
		Buff,
		Grapple,
		Chat
	}

	private static readonly Dictionary<Action, Keys> Bound = new Dictionary<Action, Keys>();

	/// <summary>Human-readable label per action, used by the rebind screen.</summary>
	internal static string Label(Action action)
	{
		switch (action)
		{
		case Action.MoveLeft:
			return "Move left";
		case Action.MoveRight:
			return "Move right";
		case Action.MoveUp:
			return "Move up";
		case Action.MoveDown:
			return "Move down";
		case Action.Jump:
			return "Jump";
		case Action.Inventory:
			return "Inventory";
		case Action.Throw:
			return "Throw item";
		case Action.SmartCursor:
			return "Smart cursor";
		case Action.Pause:
			return "Pause / back";
		case Action.Map:
			return "Map";
		case Action.Heal:
			return "Quick heal";
		case Action.Mana:
			return "Quick mana";
		case Action.Buff:
			return "Quick buff";
		case Action.Grapple:
			return "Grapple";
		case Action.Chat:
			return "Chat";
		default:
			return action.ToString();
		}
	}

	/// <summary>Settings.ini key name, kept stable so old configs keep working.</summary>
	internal static string ConfigKey(Action action)
	{
		return action.ToString();
	}

	private static Keys Default(Action action)
	{
		switch (action)
		{
		case Action.MoveLeft:
			return Keys.A;
		case Action.MoveRight:
			return Keys.D;
		case Action.MoveUp:
			return Keys.W;
		case Action.MoveDown:
			return Keys.S;
		case Action.Jump:
			return Keys.Space;
		case Action.Inventory:
			return Keys.E;
		case Action.Throw:
			return Keys.Q;
		case Action.SmartCursor:
			return Keys.C;
		case Action.Pause:
			return Keys.Escape;
		case Action.Map:
			return Keys.Tab;
		case Action.Heal:
			return Keys.H;
		case Action.Mana:
			return Keys.J;
		case Action.Buff:
			return Keys.B;
		case Action.Grapple:
			return Keys.V;
		case Action.Chat:
			return Keys.Enter;
		default:
			return Keys.None;
		}
	}

	/// <summary>All actions in display order.</summary>
	internal static Action[] All
	{
		get
		{
			Array values = Enum.GetValues(typeof(Action));
			Action[] array = new Action[values.Length];
			values.CopyTo(array, 0);
			return array;
		}
	}

	/// <summary>Current key for an action (never <see cref="Keys.None"/>).</summary>
	internal static Keys Get(Action action)
	{
		Keys result;
		if (Bound.TryGetValue(action, out result) && result != Keys.None)
		{
			return result;
		}
		return Default(action);
	}

	/// <summary>Rebinds an action. Refuses duplicates so two actions never share a key.</summary>
	internal static bool TrySet(Action action, Keys key)
	{
		if (key == Keys.None)
		{
			return false;
		}
		Action[] all = All;
		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] != action && Get(all[i]) == key)
			{
				return false;
			}
		}
		Bound[action] = key;
		return true;
	}

	/// <summary>Restores every action to its built-in default.</summary>
	internal static void ResetToDefaults()
	{
		Bound.Clear();
	}

	internal static bool IsDefault(Action action)
	{
		return Get(action) == Default(action);
	}

	/// <summary>Reads overrides from the config. Missing or invalid entries keep defaults.</summary>
	internal static void Load(Settings settings)
	{
		Bound.Clear();
		Action[] all = All;
		for (int i = 0; i < all.Length; i++)
		{
			string text = settings.Get("Keys", ConfigKey(all[i]));
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			Keys result;
			if (Enum.TryParse<Keys>(text, ignoreCase: true, out result) && result != Keys.None)
			{
				Bound[all[i]] = result;
			}
		}
	}

	/// <summary>
	/// Writes current bindings back, including defaults - that way the file documents every
	/// rebindable action instead of only the changed ones.
	/// </summary>
	internal static void Save(Settings settings)
	{
		Action[] all = All;
		for (int i = 0; i < all.Length; i++)
		{
			settings.Update("Keys", ConfigKey(all[i]), Get(all[i]).ToString(), Label(all[i]));
		}
	}

	internal static bool IsBound(Keys key)
	{
		Action[] all = All;
		for (int i = 0; i < all.Length; i++)
		{
			if (Get(all[i]) == key)
			{
				return true;
			}
		}
		return false;
	}
}
