using System.Globalization;

namespace Terraria.Mods;

/// <summary>
/// A write-only handle to one existing item, for changing its stats.
///
/// <code>
/// Context.Item("Copper Shortsword").Damage = 12;
/// </code>
///
/// Why write-only: the override is not applied to a live object. It is stored and applied
/// right after <c>Item.SetDefaults</c>, so every copy of the item - in a chest, in a shop, in a
/// player's inventory after a reload - sees the same numbers. Reading back through this handle
/// would only show the override, not the item, and that is a different question with a
/// different answer per instance.
///
/// Only fields that exist on this build are exposed. A field with no property here is either
/// applied through <c>Set(field, value)</c> (colours, the two ammo names) or refused with a reason
/// (<c>createTileName</c>, which needs mod tiles) - and <c>--selftest</c> enforces that every
/// supported field is one of those three, because "supported by the converter, missing from the
/// project" is a loss nobody sees until the item behaves differently.
/// </summary>
public sealed class ItemOverride
{
	private readonly int _type;

	internal ItemOverride(int type, string name)
	{
		_type = type;
		Name = name;
	}

	/// <summary>The name this override was requested by, as written in the mod.</summary>
	public string Name { get; private set; }

	/// <summary>The item type id it resolved to.</summary>
	public int Type => _type;

	/// <summary>Melee damage.</summary>
	public int Damage
	{
		set { Set("damage", value); }
	}

	/// <summary>Defence, for armour and accessories.</summary>
	public int Defense
	{
		set { Set("defense", value); }
	}

	/// <summary>Pickaxe power (percent).</summary>
	public int Pick
	{
		set { Set("pick", value); }
	}

	/// <summary>Axe power (percent).</summary>
	public int Axe
	{
		set { Set("axe", value); }
	}

	/// <summary>Hammer power (percent).</summary>
	public int Hammer
	{
		set { Set("hammer", value); }
	}

	/// <summary>Frames the swing animation lasts.</summary>
	public int UseAnimation
	{
		set { Set("useAnimation", value); }
	}

	/// <summary>Frames between uses.</summary>
	public int UseTime
	{
		set { Set("useTime", value); }
	}

	/// <summary>Sound id played on use.</summary>
	public int UseSound
	{
		set { Set("useSound", value); }
	}

	/// <summary>Projectile id fired by the item.</summary>
	public int Shoot
	{
		set { Set("shoot", value); }
	}

	/// <summary>Speed of the fired projectile.</summary>
	public float ShootSpeed
	{
		set { Set("shootSpeed", value); }
	}

	/// <summary>Knockback.</summary>
	public float Knockback
	{
		set { Set("knockBack", value); }
	}

	/// <summary>Mana per use.</summary>
	public int Mana
	{
		set { Set("mana", value); }
	}

	/// <summary>Health restored on use.</summary>
	public int HealLife
	{
		set { Set("healLife", value); }
	}

	/// <summary>Mana restored on use.</summary>
	public int HealMana
	{
		set { Set("healMana", value); }
	}

	/// <summary>Buff id applied on use.</summary>
	public int BuffType
	{
		set { Set("buffType", value); }
	}

	/// <summary>Buff duration in frames.</summary>
	public int BuffTime
	{
		set { Set("buffTime", value); }
	}

	/// <summary>Rarity, which drives the name colour.</summary>
	public int Rare
	{
		set { Set("rare", value); }
	}

	/// <summary>Shop value in copper.</summary>
	public int Value
	{
		set { Set("value", value); }
	}

	/// <summary>Stack size.</summary>
	public int MaxStack
	{
		set { Set("maxStack", value); }
	}

	/// <summary>Whether holding the button keeps using the item.</summary>
	public bool AutoReuse
	{
		set { Set("autoReuse", value); }
	}

	/// <summary>Counts as melee for damage bonuses.</summary>
	public bool Melee
	{
		set { Set("melee", value); }
	}

	/// <summary>Counts as magic for damage bonuses.</summary>
	public bool Magic
	{
		set { Set("magic", value); }
	}

	/// <summary>Counts as ranged for damage bonuses.</summary>
	public bool Ranged
	{
		set { Set("ranged", value); }
	}

	/// <summary>Counts as a potion.</summary>
	public bool Potion
	{
		set { Set("potion", value); }
	}

	/// <summary>Equippable in an accessory slot.</summary>
	public bool Accessory
	{
		set { Set("accessory", value); }
	}

	/// <summary>The item does no melee damage of its own (it only fires a projectile).</summary>
	public bool NoMelee
	{
		set { Set("noMelee", value); }
	}

	/// <summary>
	/// The item fires a projectile and draws nothing itself. tConfig writes this as
	/// <c>noUseGraphic</c>; the item field is <c>NoUseGraphic</c>.
	/// </summary>
	public bool NoUseGraphic
	{
		set { Set("noUseGraphic", value ? "true" : "false"); }
	}

	/// <summary>Critical strike chance in percent.</summary>
	public int Crit
	{
		set { Set("crit", value); }
	}

	// The remaining fields exist on both APIs. They are settable on an existing item as well as on
	// a new one: a pack that changes a sword's swing style is the same operation as a pack that
	// defines one, and two different field tables would drift apart.

	/// <summary>Sprite width in pixels.</summary>
	public int Width
	{
		set { Set("width", value); }
	}

	/// <summary>Sprite height in pixels.</summary>
	public int Height
	{
		set { Set("height", value); }
	}

	/// <summary>Use style: 1 swing, 2 eat/drink, 3 stab, 4 hold up, 5 shoot.</summary>
	public int UseStyle
	{
		set { Set("useStyle", value); }
	}

	/// <summary>How the item is held when idle.</summary>
	public int HoldStyle
	{
		set { Set("holdStyle", value); }
	}

	/// <summary>Sprite scale.</summary>
	public float Scale
	{
		set { Set("scale", value); }
	}

	/// <summary>Counts as a crafting material.</summary>
	public bool Material
	{
		set { Set("material", value ? "true" : "false"); }
	}

	/// <summary>
	/// Consumed on use: a potion, a thrown weapon, a placeable that disappears from the stack.
	///
	/// It is here because the game has the field (<c>Item.IsConsumable</c>) and the converter
	/// applies it. It used to be missing, with a comment claiming this build derived it from stack
	/// and heal values - which was not true, and made the C# project output drop the field on 107
	/// items across the packs while the raw-mod output kept it.
	/// </summary>
	public bool Consumable
	{
		set { Set("consumable", value ? "true" : "false"); }
	}

	/// <summary>How brightly the item glows, 0 (not at all) to 1.</summary>
	public float Light
	{
		set { Set("light", value); }
	}

	/// <summary>Whether the sprite turns to follow the swing.</summary>
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

	/// <summary>
	/// The ammo this item consumes, by the name the game shows ("Bolt"). tConfig writes the name
	/// rather than the id, and the registry resolves it through the real item table.
	/// </summary>
	public string UseAmmoName
	{
		set { Set("useAmmoName", value); }
	}

	/// <summary>The ammo type this item is, by name.</summary>
	public string AmmoName
	{
		set { Set("ammoName", value); }
	}

	/// <summary>The line shown when the item is hovered.</summary>
	public string ToolTip
	{
		set { Set("toolTip", value); }
	}

	/// <summary>The second hovered line.</summary>
	public string ToolTip2
	{
		set { Set("toolTip2", value); }
	}

	/// <summary>
	/// Escape hatch for a field added to the supported list later, so a mod written today keeps
	/// working. Unknown names are logged by <see cref="ItemOverrides"/> instead of vanishing.
	/// </summary>
	public void Set(string field, string value)
	{
		ItemOverrides.Set(_type, field, value);
	}

	private void Set(string field, int value)
	{
		ItemOverrides.Set(_type, field, value.ToString(CultureInfo.InvariantCulture));
	}

	private void Set(string field, float value)
	{
		ItemOverrides.Set(_type, field, value.ToString(CultureInfo.InvariantCulture));
	}

	private void Set(string field, bool value)
	{
		// bool.TryParse wants "True"/"False", which is what ToString gives.
		ItemOverrides.Set(_type, field, value ? "True" : "False");
	}
}
