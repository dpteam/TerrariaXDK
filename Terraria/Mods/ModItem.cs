using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Mods;

/// <summary>
/// A new item declared by a mod: stats plus a sprite.
///
/// Write-only, for the same reason as <see cref="ItemOverride"/>: the definition is not a live
/// object. It is stored and applied every time <c>Item.SetDefaults</c> runs for this id, so every
/// copy of the item - in a chest, on the ground, in a save file - gets the same numbers.
///
/// Ids are assigned after all mods have loaded, in sorted order, so two players with the same mods
/// get the same ids no matter what order the mods were loaded in. That matters more than it looks:
/// an item id travels on the wire, so a different id on each side is a desync that looks like
/// "the item turned into something else".
/// </summary>
public sealed class ModItem
{
	internal readonly Dictionary<string, string> Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>The mod that declared it.</summary>
	public string ModName { get; internal set; }

	/// <summary>Display name, and the key the definition is stored under.</summary>
	public string Name { get; internal set; }

	/// <summary>Item type id, or 0 until ids are assigned.</summary>
	public int Type { get; internal set; }

	/// <summary>Sprite file, relative to the mod folder. Empty when the sprite was missing.</summary>
	public string SpritePath { get; internal set; }

	/// <summary>True when the sprite was loaded and the item can be drawn.</summary>
	public bool HasSprite { get; internal set; }

	public int Damage
	{
		set { Set("damage", value); }
	}

	public int Defense
	{
		set { Set("defense", value); }
	}

	public int Pick
	{
		set { Set("pick", value); }
	}

	public int Axe
	{
		set { Set("axe", value); }
	}

	public int Hammer
	{
		set { Set("hammer", value); }
	}

	public int UseAnimation
	{
		set { Set("useAnimation", value); }
	}

	public int UseTime
	{
		set { Set("useTime", value); }
	}

	public int UseSound
	{
		set { Set("useSound", value); }
	}

	/// <summary>Use style: 1 swing, 2 eat/drink, 3 stab, 4 hold up, 5 shoot.</summary>
	public int UseStyle
	{
		set { Set("useStyle", value); }
	}

	public int HoldStyle
	{
		set { Set("holdStyle", value); }
	}

	public int Shoot
	{
		set { Set("shoot", value); }
	}

	public float ShootSpeed
	{
		set { Set("shootSpeed", value); }
	}

	public float Knockback
	{
		set { Set("knockBack", value); }
	}

	public int Mana
	{
		set { Set("mana", value); }
	}

	public int HealLife
	{
		set { Set("healLife", value); }
	}

	public int HealMana
	{
		set { Set("healMana", value); }
	}

	public int BuffType
	{
		set { Set("buffType", value); }
	}

	public int BuffTime
	{
		set { Set("buffTime", value); }
	}

	public int Rare
	{
		set { Set("rare", value); }
	}

	public int Value
	{
		set { Set("value", value); }
	}

	public int MaxStack
	{
		set { Set("maxStack", value); }
	}

	/// <summary>Sprite width in pixels. Defaults to the sprite's own width when not set.</summary>
	public int Width
	{
		set { Set("width", value); }
	}

	/// <summary>Sprite height in pixels. Defaults to the sprite's own height when not set.</summary>
	public int Height
	{
		set { Set("height", value); }
	}

	public float Scale
	{
		set { Set("scale", value); }
	}

	public bool AutoReuse
	{
		set { Set("autoReuse", value ? "true" : "false"); }
	}

	public bool Melee
	{
		set { Set("melee", value ? "true" : "false"); }
	}

	public bool Magic
	{
		set { Set("magic", value ? "true" : "false"); }
	}

	public bool Ranged
	{
		set { Set("ranged", value ? "true" : "false"); }
	}

	public bool Potion
	{
		set { Set("potion", value ? "true" : "false"); }
	}

	public bool Accessory
	{
		set { Set("accessory", value ? "true" : "false"); }
	}

	public bool NoMelee
	{
		set { Set("noMelee", value ? "true" : "false"); }
	}

	/// <summary>The item fires a projectile and draws nothing itself.</summary>
	public bool NoUseGraphic
	{
		set { Set("noUseGraphic", value ? "true" : "false"); }
	}

	/// <summary>Counts as a crafting material.</summary>
	public bool Material
	{
		set { Set("isMaterial", value ? "true" : "false"); }
	}

	/// <summary>
	/// Whether the sprite turns to follow the swing. tConfig writes this as <c>useTurn</c>; the
	/// item field is <c>CanUseTurn</c>, so the property is named after the field.
	/// </summary>
	public bool CanUseTurn
	{
		set { Set("useTurn", value ? "true" : "false"); }
	}

	/// <summary>Ammo type this item consumes, 0 for none.</summary>
	public int UseAmmo
	{
		set { Set("useAmmo", value); }
	}

	/// <summary>Ammo type this item is, 0 for not ammo.</summary>
	public int Ammo
	{
		set { Set("ammo", value); }
	}

	public int Crit
	{
		set { Set("crit", value); }
	}

	/// <summary>
	/// The line shown when the item is hovered in the inventory.
	///
	/// Stored as text rather than as a field of the item: descriptions are not part of
	/// <see cref="Item"/>, they are <see cref="Lang.ToolTip"/>'s switch over ids. The registry
	/// copies them into <see cref="ItemToolTips"/> once ids are assigned.
	/// </summary>
	public string ToolTip
	{
		set { ToolTipText = value ?? string.Empty; }
	}

	/// <summary>The second tooltip line: what the item does, as opposed to what it is.</summary>
	public string ToolTip2
	{
		set { ToolTip2Text = value ?? string.Empty; }
	}

	internal string ToolTipText = "";

	internal string ToolTip2Text = "";

	/// <summary>Escape hatch for a field added later, so a mod written today keeps working.</summary>
	public void Set(string field, string value)
	{
		if (string.IsNullOrEmpty(field))
		{
			return;
		}
		Fields[field] = value ?? string.Empty;
	}

	private void Set(string field, int value)
	{
		Fields[field] = value.ToString(CultureInfo.InvariantCulture);
	}

	private void Set(string field, float value)
	{
		Fields[field] = value.ToString("0.###", CultureInfo.InvariantCulture);
	}
}
