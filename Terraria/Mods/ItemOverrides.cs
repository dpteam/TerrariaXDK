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
	/// Records a refusal, once, and writes it to the game log the first time.
	///
	/// The list alone was not enough: nothing read it, so every refusal - "this field needs mod
	/// tiles", "a mod item cannot be an ammo type", "the value does not fit the field" - was
	/// silent. The player saw a field that "just did not work", and the log they are told to read
	/// had nothing in it.
	///
	/// Logged once per message because <see cref="ApplyField"/> runs on every
	/// <c>Item.SetDefaults</c> for that item: a potion's buff type is applied every time one is
	/// created, and a repeated line would bury everything else.
	/// </summary>
	private static void RecordError(string message)
	{
		if (Reported.Add(message))
		{
			Errors.Add(message);
			ModLoader.Log("[Overrides] " + message);
		}
	}

	private static readonly HashSet<string> Reported = new HashSet<string>(StringComparer.Ordinal);

	/// <summary>
	/// Fields that can be applied to an item. One table for both overrides and new items: a
	/// tConfig pack's <c>width</c> means the same thing whether it is changing a sword or defining
	/// one, and two tables would drift apart.
	///
	/// Kept explicit on purpose: silently ignoring unknown fields is how broken conversions go
	/// unnoticed.
	/// </summary>
	private static readonly string[] SupportedFields = new string[46]
	{
		"damage", "defense", "pick", "axe", "hammer", "useAnimation", "useTime", "useSound", "shoot", "shootSpeed",
		"knockBack", "mana", "healLife", "healMana", "buffType", "buffTime", "rare", "value", "maxStack", "autoReuse",
		"melee", "magic", "ranged", "consumable", "potion", "accessory", "noMelee", "crit",
		"width", "height", "useStyle", "holdStyle", "scale", "isMaterial", "material", "ammo", "useAmmo",
		"useTurn", "toolTip", "toolTip2", "createTileName",
		// tConfig spellings that mean the same as a field above: `stack` is MaxStack, `color` is
		// the sprite tint, `noUseGraphic` is the item that only fires a projectile, and the two
		// ammo fields are written by name rather than by id.
		"stack", "color", "noUseGraphic", "useAmmoName", "ammoName"
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

	/// <summary>
	/// The field names, for the test that walks all of them with hostile values.
	///
	/// Read-only so the test cannot edit the table it is checking.
	/// </summary>
	internal static IList<string> FieldNames
	{
		get
		{
			return Array.AsReadOnly(SupportedFields);
		}
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
		Reported.Clear();
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

	/// <summary>
	/// Range-checks a pack's number against the width of the item field it is going into, and
	/// returns the field's existing value when it does not fit.
	///
	/// Every assignment in <see cref="ApplyField"/> used to be a plain cast - <c>(byte)result</c>,
	/// <c>(short)result</c> - and a cast to a narrower type <b>truncates silently</b>:
	/// <c>(byte)20043</c> is 75, which is a different item. For an ammo or buff type that is not a
	/// visible glitch but a wrong behaviour: the pack "works" and the weapon consumes, or applies,
	/// something else. A value that does not fit is reported and ignored instead.
	///
	/// <paramref name="note"/> is for the fields where the reason deserves a sentence: an item id
	/// cannot live in a byte, so a mod item can never be an ammo type in this build.
	///
	/// <paramref name="min"/> is for the fields whose range starts above zero. <c>maxStack = 0</c>
	/// is the one that matters: it is inside the <c>short</c> range, so a width check alone would
	/// accept it, and an item whose <c>MaxStack</c> is zero is deleted the moment it is picked up.
	/// </summary>
	private static int Fit(int itemType, string field, int value, int current, int max = 255, string note = null, int min = 0)
	{
		if (value < min || value > max)
		{
			RecordError("item " + itemType + ": '" + field + "' is " + value + ", outside " + min + ".." + max
				+ (note ?? "") + " - left as it was");
			return current;
		}
		return value;
	}

	/// <summary>
	/// The float counterpart of <see cref="Fit"/>: refuses NaN, infinity and absurd magnitudes.
	///
	/// A float has no width to overflow, so the failure looks different - and is worse. <c>1e999</c>
	/// parses as infinity and <c>NaN</c> parses as NaN, and both travel into a sprite's scale and
	/// into rectangles. NaN in a rectangle is not "a big number": it is a draw call that either
	/// throws or draws nothing, and it reads as a rendering bug in the game rather than a bad line
	/// in a pack.
	/// </summary>
	private static float FitFloat(int itemType, string field, float value, float current, float max)
	{
		if (float.IsNaN(value) || float.IsInfinity(value) || value < -max || value > max)
		{
			RecordError("item " + itemType + ": '" + field + "' is " + value.ToString(CultureInfo.InvariantCulture)
				+ ", outside -" + max.ToString(CultureInfo.InvariantCulture) + ".."
				+ max.ToString(CultureInfo.InvariantCulture) + " - left as it was");
			return current;
		}
		return value;
	}

	internal static void ApplyField(ref Item item, string field, string value)
	{
		int result;
		float result2;
		bool flag;
		switch (field.ToLowerInvariant())
		{
		case "damage":
			if (int.TryParse(value, out result))
			{
				item.Damage = (short)Fit(item.Type, field, result, item.Damage, 32767);
			}
			break;
		case "defense":
			if (int.TryParse(value, out result))
			{
				item.Defense = (short)Fit(item.Type, field, result, item.Defense, 32767);
			}
			break;
		case "pick":
			if (int.TryParse(value, out result))
			{
				item.PickPower = (byte)Fit(item.Type, field, result, item.PickPower);
			}
			break;
		case "axe":
			if (int.TryParse(value, out result))
			{
				item.AxePower = (byte)Fit(item.Type, field, result, item.AxePower);
			}
			break;
		case "hammer":
			if (int.TryParse(value, out result))
			{
				item.HammerPower = (byte)Fit(item.Type, field, result, item.HammerPower);
			}
			break;
		case "useanimation":
			if (int.TryParse(value, out result))
			{
				item.UseAnimation = (byte)Fit(item.Type, field, result, item.UseAnimation);
			}
			break;
		case "usetime":
			if (int.TryParse(value, out result))
			{
				item.UseTime = (byte)Fit(item.Type, field, result, item.UseTime);
			}
			break;
		case "usesound":
			if (int.TryParse(value, out result))
			{
				item.UseSound = (byte)Fit(item.Type, field, result, item.UseSound);
			}
			break;
		case "shoot":
			if (int.TryParse(value, out result))
			{
				item.Shoot = (byte)Fit(item.Type, field, result, item.Shoot);
			}
			break;
		case "shootspeed":
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
			{
				item.ShootSpeed = FitFloat(item.Type, field, result2, item.ShootSpeed, 1000f);
			}
			break;
		case "knockback":
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
			{
				item.Knockback = FitFloat(item.Type, field, result2, item.Knockback, 1000f);
			}
			break;
		case "mana":
			if (int.TryParse(value, out result))
			{
				item.Mana = (byte)Fit(item.Type, field, result, item.Mana);
			}
			break;
		case "heallife":
			if (int.TryParse(value, out result))
			{
				item.HealLife = (short)Fit(item.Type, field, result, item.HealLife, 32767);
			}
			break;
		case "healmana":
			if (int.TryParse(value, out result))
			{
				item.HealMana = (short)Fit(item.Type, field, result, item.HealMana, 32767);
			}
			break;
		case "bufftype":
			if (int.TryParse(value, out result))
			{
				// Not just the byte range: the equipment screen indexes Buff.BuffName[] and the
				// sprite table by this number, and this build has Buff.MaxNumBuffs of them. A
				// buffType past that crashed the screen instead of showing a buff.
				item.BuffType = (byte)Fit(item.Type, field, result, item.BuffType, Terraria.Buff.MaxNumBuffs - 1,
					" (this build has " + Terraria.Buff.MaxNumBuffs + " buffs)");
			}
			break;
		case "bufftime":
			if (int.TryParse(value, out result))
			{
				item.BuffTime = (ushort)Fit(item.Type, field, result, item.BuffTime, 65535);
			}
			break;
		case "rare":
			if (int.TryParse(value, out result))
			{
				item.Rarity = (sbyte)Fit(item.Type, field, result, item.Rarity, 127);
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
				item.MaxStack = (short)Fit(item.Type, field, result, item.MaxStack, 32767, null, 1);
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
				item.Crit = (short)Fit(item.Type, field, result, item.Crit, 32767);
			}
			break;
		case "consumable":
			// This build has the field, so the value can be applied rather than guessed at.
			if (bool.TryParse(value, out flag))
			{
				item.IsConsumable = flag;
			}
			break;
		case "useturn":
			if (bool.TryParse(value, out flag))
			{
				item.CanUseTurn = flag;
			}
			break;
		case "tooltip":
			// Descriptions are not fields of Item: they are Lang.ToolTip's switch over ids. The
			// text goes into the override table, which Lang asks first.
			Terraria.ItemToolTips.Set(item.Type, value, null);
			break;
		case "tooltip2":
			Terraria.ItemToolTips.Set(item.Type, null, value);
			break;
		case "createtilename":
			// Placing a new tile needs mod tiles, which this build does not have. Reported rather
			// than mapped onto some vanilla tile, which would look like it worked and place the
			// wrong thing.
			RecordError("item " + item.Type + ": 'createTileName' needs mod tiles, which this build does not have");
			break;
		case "light":
			RecordError("item " + item.Type + ": 'light' has no equivalent field in this build");
			break;
		case "width":
			if (int.TryParse(value, out result))
			{
				item.Width = (ushort)Fit(item.Type, field, result, item.Width, 65535);
			}
			break;
		case "height":
			if (int.TryParse(value, out result))
			{
				item.Height = (ushort)Fit(item.Type, field, result, item.Height, 65535);
			}
			break;
		case "usestyle":
			if (int.TryParse(value, out result))
			{
				item.UseStyle = (byte)Fit(item.Type, field, result, item.UseStyle);
			}
			break;
		case "holdstyle":
			if (int.TryParse(value, out result))
			{
				item.HoldStyle = (byte)Fit(item.Type, field, result, item.HoldStyle);
			}
			break;
		case "scale":
			if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
			{
				item.Scale = FitFloat(item.Type, field, result2, item.Scale, 1000f);
			}
			break;
		case "ismaterial":
			if (bool.TryParse(value, out flag))
			{
				item.IsMaterial = flag;
			}
			break;
		case "material":
			// tConfig writes this name as well as "isMaterial"; same meaning.
			if (bool.TryParse(value, out flag))
			{
				item.IsMaterial = flag;
			}
			break;
		case "ammo":
			if (int.TryParse(value, out result))
			{
				item.Ammo = (byte)Fit(item.Type, field, result, item.Ammo, 255, " (an item id lives in one byte here, so a mod item cannot be an ammo type)");
			}
			break;
		case "useammo":
			if (int.TryParse(value, out result))
			{
				item.UseAmmo = (byte)Fit(item.Type, field, result, item.UseAmmo, 255, " (an item id lives in one byte here, so a mod item cannot be an ammo type)");
			}
			break;
		case "stack":
			// tConfig writes `stack` where the item field is `maxStack`. Same meaning, different
			// name, and 119 items in the packs on this machine lost their stack size to it.
			if (int.TryParse(value, out result))
			{
				item.MaxStack = (short)Fit(item.Type, field, result, item.MaxStack, 32767, null, 1);
			}
			break;
		case "color":
			// `color=180,100,45,80` is R,G,B,A - the sprite tint, not a packed integer.
			if (TryParseColour(value, out int num, out int num2, out int num3, out int num4))
			{
				item.Colour = new Microsoft.Xna.Framework.Color(num, num2, num3, num4);
			}
			break;
		case "nousegraphic":
			// The item fires a projectile and draws nothing itself.
			if (bool.TryParse(value, out flag))
			{
				item.NoUseGraphic = flag;
			}
			break;
		case "useammoname":
		case "ammoname":
		{
			// tConfig writes the ammo type by name ("Bolt"), the item field takes an id. Resolved
			// through the real name table, the same way ingredients are - and a miss is logged
			// rather than silently becoming ammo id 0, which would mean "uses nothing".
			int num5 = Terraria.ItemLookup.Find(value);
			if (num5 <= 0)
			{
				RecordError("item " + item.Type + ": '" + field + "' names '" + value
					+ "', which is not an item in this build - left as it was");
				break;
			}
			if (num5 > 255)
			{
				// A mod item id (20000+) cannot be stored in this field: it is one byte, fixed by
				// the console protocol. (byte)20043 is 75 - a different item - so this is refused
				// rather than truncated. Naming the id makes the limit visible instead of looking
				// like the pack's mistake.
				RecordError("item " + item.Type + ": '" + field + "' names '" + value + "' (id " + num5
					+ "), and this build holds an ammo type in one byte, so a mod item cannot be used"
					+ " - left as it was");
				break;
			}
			if (field.Equals("useAmmoName", StringComparison.OrdinalIgnoreCase))
			{
				item.UseAmmo = (byte)num5;
			}
			else
			{
				item.Ammo = (byte)num5;
			}
			break;
		}
		}
	}

	/// <summary>
	/// Reads <c>R,G,B</c> or <c>R,G,B,A</c> with 0..255 components. Alpha defaults to opaque, which
	/// is what a pack that writes three numbers means.
	/// </summary>
	internal static bool TryParseColour(string value, out int R, out int G, out int B, out int A)
	{
		R = 255;
		G = 255;
		B = 255;
		A = 255;
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		string[] array = value.Split(',');
		if (array.Length < 3 || array.Length > 4)
		{
			return false;
		}
		int[] array2 = new int[4] { 255, 255, 255, 255 };
		for (int i = 0; i < array.Length; i++)
		{
			int result;
			if (!int.TryParse(array[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return false;
			}
			if (result < 0 || result > 255)
			{
				return false;
			}
			array2[i] = result;
		}
		R = array2[0];
		G = array2[1];
		B = array2[2];
		A = array2[3];
		return true;
	}
}
