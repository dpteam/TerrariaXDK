using System;
using System.Collections.Generic;
using System.Globalization;

namespace Terraria.Mods;

/// <summary>
/// Field-level overrides for existing items, applied right after <c>Item.SetDefaults</c>.
///
/// This is the runtime half of tConfig compatibility: an old tConfig mod describes an item
/// as a set of `field = value` lines in an INI file, and those same fields exist on
/// <see cref="Item"/>. Instead of inventing new item ids (which breaks saves and the network
/// protocol) an imported mod adjusts the item it names.
/// </summary>
internal static class ItemOverrides
{
	private sealed class Override
	{
		public int ItemType;

		public Dictionary<string, string> Fields;
	}

	/// <summary>Overrides keyed by item type id.</summary>
	private static readonly Dictionary<int, Override> ByType = new Dictionary<int, Override>();

	private static readonly List<string> Errors = new List<string>();

	public static int Count => ByType.Count;

	public static IList<string> LoadErrors => Errors;

	/// <summary>
	/// True when the named field can be applied. Kept explicit on purpose: silently ignoring
	/// unknown fields is how broken conversions go unnoticed.
	/// </summary>
	private static readonly string[] SupportedFields = new string[28]
	{
		"damage", "defense", "pick", "axe", "hammer", "useAnimation", "useTime", "useSound", "shoot", "shootSpeed",
		"knockBack", "mana", "healLife", "healMana", "buffType", "buffTime", "rare", "value", "maxStack", "autoReuse",
		"melee", "magic", "ranged", "consumable", "potion", "accessory", "noMelee", "crit"
	};

	public static bool IsSupportedField(string field)
	{
		for (int i = 0; i < SupportedFields.Length; i++)
		{
			if (string.Equals(SupportedFields[i], field, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Registers an override for an existing item type.</summary>
	public static void Set(int itemType, string field, string value)
	{
		Override result;
		if (!ByType.TryGetValue(itemType, out result))
		{
			result = new Override
			{
				ItemType = itemType,
				Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			};
			ByType[itemType] = result;
		}
		result.Fields[field] = value;
	}

	public static void Clear()
	{
		ByType.Clear();
		Errors.Clear();
	}

	/// <summary>Applies registered overrides. Called from <c>Item.SetDefaults</c>.</summary>
	public static Item Apply(Item item)
	{
		if (ByType.Count == 0)
		{
			return item;
		}
		Override result;
		if (!ByType.TryGetValue(item.Type, out result))
		{
			return item;
		}
		foreach (KeyValuePair<string, string> field in result.Fields)
		{
			ApplyField(ref item, field.Key, field.Value);
		}
		return item;
	}

	private static void ApplyField(ref Item item, string field, string value)
	{
		int result;
		float result2;
		bool flag;
		switch (field.ToLowerInvariant())
		{
		case "damage":
			if (int.TryParse(value, out result))
			{
				item.Damage = (short)result;
			}
			break;
		case "defense":
			if (int.TryParse(value, out result))
			{
				item.Defense = (short)result;
			}
			break;
		case "pick":
			if (int.TryParse(value, out result))
			{
				item.PickPower = (byte)result;
			}
			break;
		case "axe":
			if (int.TryParse(value, out result))
			{
				item.AxePower = (byte)result;
			}
			break;
		case "hammer":
			if (int.TryParse(value, out result))
			{
				item.HammerPower = (byte)result;
			}
			break;
		case "useanimation":
			if (int.TryParse(value, out result))
			{
				item.UseAnimation = (byte)result;
			}
			break;
		case "usetime":
			if (int.TryParse(value, out result))
			{
				item.UseTime = (byte)result;
			}
			break;
		case "usesound":
			if (int.TryParse(value, out result))
			{
				item.UseSound = (byte)result;
			}
			break;
		case "shoot":
			if (int.TryParse(value, out result))
			{
				item.Shoot = (byte)result;
			}
			break;
		case "shootspeed":
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
			{
				item.ShootSpeed = result2;
			}
			break;
		case "knockback":
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
			{
				item.Knockback = result2;
			}
			break;
		case "mana":
			if (int.TryParse(value, out result))
			{
				item.Mana = (byte)result;
			}
			break;
		case "heallife":
			if (int.TryParse(value, out result))
			{
				item.HealLife = (short)result;
			}
			break;
		case "healmana":
			if (int.TryParse(value, out result))
			{
				item.HealMana = (short)result;
			}
			break;
		case "bufftype":
			if (int.TryParse(value, out result))
			{
				item.BuffType = (byte)result;
			}
			break;
		case "bufftime":
			if (int.TryParse(value, out result))
			{
				item.BuffTime = (ushort)result;
			}
			break;
		case "rare":
			if (int.TryParse(value, out result))
			{
				item.Rarity = (sbyte)result;
			}
			break;
		case "value":
			if (int.TryParse(value, out result))
			{
				item.Value = result;
			}
			break;
		case "maxstack":
			if (int.TryParse(value, out result))
			{
				item.MaxStack = (short)result;
			}
			break;
		case "autoreuse":
			if (bool.TryParse(value, out flag))
			{
				item.AutoReuse = flag;
			}
			break;
		case "melee":
			if (bool.TryParse(value, out flag))
			{
				item.IsMelee = flag;
			}
			break;
		case "magic":
			if (bool.TryParse(value, out flag))
			{
				item.IsMagic = flag;
			}
			break;
		case "ranged":
			if (bool.TryParse(value, out flag))
			{
				item.IsRanged = flag;
			}
			break;
		case "potion":
			if (bool.TryParse(value, out flag))
			{
				item.IsPotion = flag;
			}
			break;
		case "accessory":
			if (bool.TryParse(value, out flag))
			{
				item.IsAccessory = flag;
			}
			break;
		case "nomelee":
			if (bool.TryParse(value, out flag))
			{
				item.NoMelee = flag;
			}
			break;
		case "crit":
			if (int.TryParse(value, out result))
			{
				item.Crit = (short)result;
			}
			break;
		case "consumable":
			// "consumable" has no single backing field in this build; it is implied by the
			// stack/heal values. Recorded as unsupported rather than guessed at.
			Errors.Add("item " + item.Type + ": 'consumable' is not directly supported");
			break;
		}
	}
}
