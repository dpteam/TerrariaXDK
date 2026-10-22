using System;
using Microsoft.Xna.Framework;

namespace Terraria;

public struct Item
{
	public const int MaxNumItemTypes = 632;

	public const uint PotionDelay = 3600u;

	public const uint PotionDelayPhilosopher = 2700u;

	public static short[] HeadType = new short[48];

	public static short[] BodyType = new short[29];

	public static short[] LegType = new short[28];

	public short Type;

	public byte Active;

	public bool BeingGrabbed;

	public bool WornArmor;

	public bool Mech;

	public bool IsWet;

	public byte WetCount;

	public bool IsInLava;

	public bool Channelling;

	public bool IsAccessory;

	public bool IsPotion;

	public bool IsConsumable;

	public bool AutoReuse;

	public bool CanUseTurn;

	public bool OnlyBuyOnce;

	public bool NoUseGraphic;

	public bool NoMelee;

	public bool CanBuy;

	public bool IsSocial;

	public bool IsVanity;

	public bool IsMaterial;

	public bool CantTouchLiquid;

	public bool IsMelee;

	public bool IsMagic;

	public bool IsRanged;

	public byte PrefixType;

	public byte NoGrabDelay;

	public byte HoldStyle;

	public byte UseStyle;

	public byte UseAnimation;

	public byte UseTime;

	public byte PickPower;

	public byte AxePower;

	public byte HammerPower;

	public sbyte TileBoost;

	public byte PlaceStyle;

	public byte Alpha;

	public byte Owner;

	public byte OwnIgnore;

	public byte OwnTime;

	public byte KeepTime;

	public byte UseSound;

	public short Stack;

	public short MaxStack;

	public short CreateTile;

	public short CreateWall;

	public short Damage;

	public short HealLife;

	public short HealMana;

	public uint SpawnTime;

	public ushort Width;

	public ushort Height;

	public Vector2 Position;

	public Vector2 Velocity;

	public float Knockback;

	public Color Colour;

	public float Scale;

	public short Defense;

	public short HeadSlot;

	public short BodySlot;

	public short LegSlot;

	public ushort BuffTime;

	public byte BuffType;

	public byte ReuseDelay;

	public short NetID;

	public short Crit;

	public sbyte Rarity;

	/// <summary>
	/// The ammo category this item belongs to, or the category it consumes.
	///
	/// A <c>short</c> rather than a <c>byte</c>, unlike <c>Shoot</c> below, because these two hold
	/// an <b>item id</b> and mod items start at 20000. The console protocol fixes the width of the
	/// fields it carries, and it does not carry these: they are derived from the item's own
	/// definition by <c>SetDefaults</c> on both sides, never saved and never sent. So widening them
	/// costs nothing and removes a real limit - a mod weapon can consume a mod's ammo, instead of
	/// the name being refused because the id would not fit.
	///
	/// <c>Shoot</c> stays a byte: it holds a projectile type, there are 120 of them, and a pack
	/// naming a projectile is refused for a different reason (no projectile table to resolve it).
	/// </summary>
	public short Ammo;

	/// <inheritdoc cref="Ammo"/>
	public short UseAmmo;

	/// <summary>
	/// How brightly this item glows, 0 (not at all) to 1, or 0 for every item in this build.
	///
	/// tConfig had <c>light</c> on items and 13 items in the packs on this machine use it; the
	/// port refused it with "items do not glow here", which was true and not a reason to keep it
	/// that way. The value is read by the held-item and dropped-item light calls, which already
	/// exist for torches - a glowing mod item is the same <c>Lighting.AddLight</c> call with white
	/// instead of a torch colour.
	///
	/// A single float, not an RGB triple: that is what the pack field means, and inventing a colour
	/// from nothing would be a guess. White light at the given brightness is the honest reading.
	/// </summary>
	public float Light;

	public byte Shoot;

	public float ShootSpeed;

	public byte LifeRegen;

	public byte Mana;

	public ushort Release;

	public int Value;

	private static readonly EntityID.PrefixID[] ToolPrefixs = new EntityID.PrefixID[40]
	{
		EntityID.PrefixID.LARGE,
		EntityID.PrefixID.MASSIVE,
		EntityID.PrefixID.DANGEROUS,
		EntityID.PrefixID.SAVAGE,
		EntityID.PrefixID.SHARP,
		EntityID.PrefixID.POINTY,
		EntityID.PrefixID.TINY,
		EntityID.PrefixID.TERRIBLE,
		EntityID.PrefixID.SMALL,
		EntityID.PrefixID.DULL,
		EntityID.PrefixID.UNHAPPY,
		EntityID.PrefixID.BULKY,
		EntityID.PrefixID.SHAMEFUL,
		EntityID.PrefixID.HEAVY,
		EntityID.PrefixID.LIGHT,
		EntityID.PrefixID.KEEN,
		EntityID.PrefixID.SUPERIOR,
		EntityID.PrefixID.FORCEFUL,
		EntityID.PrefixID.HURTFUL,
		EntityID.PrefixID.STRONG,
		EntityID.PrefixID.UNPLEASANT,
		EntityID.PrefixID.BROKEN,
		EntityID.PrefixID.DAMAGED,
		EntityID.PrefixID.WEAK,
		EntityID.PrefixID.SHODDY,
		EntityID.PrefixID.RUTHLESS,
		EntityID.PrefixID.QUICK,
		EntityID.PrefixID.DEADLY2,
		EntityID.PrefixID.AGILE,
		EntityID.PrefixID.NIMBLE,
		EntityID.PrefixID.MURDEROUS,
		EntityID.PrefixID.SLOW,
		EntityID.PrefixID.SLUGGISH,
		EntityID.PrefixID.LAZY,
		EntityID.PrefixID.ANNOYING,
		EntityID.PrefixID.NASTY,
		EntityID.PrefixID.GODLY,
		EntityID.PrefixID.DEMONIC,
		EntityID.PrefixID.ZEALOUS,
		EntityID.PrefixID.LEGENDARY
	};

	private static readonly EntityID.PrefixID[] SpearPrefixs = new EntityID.PrefixID[14]
	{
		EntityID.PrefixID.KEEN,
		EntityID.PrefixID.SUPERIOR,
		EntityID.PrefixID.FORCEFUL,
		EntityID.PrefixID.HURTFUL,
		EntityID.PrefixID.STRONG,
		EntityID.PrefixID.UNPLEASANT,
		EntityID.PrefixID.BROKEN,
		EntityID.PrefixID.DAMAGED,
		EntityID.PrefixID.WEAK,
		EntityID.PrefixID.SHODDY,
		EntityID.PrefixID.RUTHLESS,
		EntityID.PrefixID.GODLY,
		EntityID.PrefixID.DEMONIC,
		EntityID.PrefixID.ZEALOUS
	};

	private static readonly EntityID.PrefixID[] GunPrefixs = new EntityID.PrefixID[36]
	{
		EntityID.PrefixID.SIGHTED,
		EntityID.PrefixID.RAPID,
		EntityID.PrefixID.HASTY,
		EntityID.PrefixID.INTIMIDATING,
		EntityID.PrefixID.DEADLY,
		EntityID.PrefixID.STAUNCH,
		EntityID.PrefixID.AWFUL,
		EntityID.PrefixID.LETHARGIC,
		EntityID.PrefixID.AWKWARD,
		EntityID.PrefixID.POWERFUL,
		EntityID.PrefixID.FRENZYING,
		EntityID.PrefixID.KEEN,
		EntityID.PrefixID.SUPERIOR,
		EntityID.PrefixID.FORCEFUL,
		EntityID.PrefixID.HURTFUL,
		EntityID.PrefixID.STRONG,
		EntityID.PrefixID.UNPLEASANT,
		EntityID.PrefixID.BROKEN,
		EntityID.PrefixID.DAMAGED,
		EntityID.PrefixID.WEAK,
		EntityID.PrefixID.SHODDY,
		EntityID.PrefixID.RUTHLESS,
		EntityID.PrefixID.QUICK,
		EntityID.PrefixID.DEADLY2,
		EntityID.PrefixID.AGILE,
		EntityID.PrefixID.NIMBLE,
		EntityID.PrefixID.MURDEROUS,
		EntityID.PrefixID.SLOW,
		EntityID.PrefixID.SLUGGISH,
		EntityID.PrefixID.LAZY,
		EntityID.PrefixID.ANNOYING,
		EntityID.PrefixID.NASTY,
		EntityID.PrefixID.GODLY,
		EntityID.PrefixID.DEMONIC,
		EntityID.PrefixID.ZEALOUS,
		EntityID.PrefixID.UNREAL
	};

	private static readonly EntityID.PrefixID[] MagicPrefixs = new EntityID.PrefixID[36]
	{
		EntityID.PrefixID.MYSTIC,
		EntityID.PrefixID.ADEPT,
		EntityID.PrefixID.MASTERFUL,
		EntityID.PrefixID.INEPT,
		EntityID.PrefixID.IGNORANT,
		EntityID.PrefixID.DERANGED,
		EntityID.PrefixID.INTENSE,
		EntityID.PrefixID.TABOO,
		EntityID.PrefixID.CELESTIAL,
		EntityID.PrefixID.FURIOUS,
		EntityID.PrefixID.MANIC,
		EntityID.PrefixID.KEEN,
		EntityID.PrefixID.SUPERIOR,
		EntityID.PrefixID.FORCEFUL,
		EntityID.PrefixID.HURTFUL,
		EntityID.PrefixID.STRONG,
		EntityID.PrefixID.UNPLEASANT,
		EntityID.PrefixID.BROKEN,
		EntityID.PrefixID.DAMAGED,
		EntityID.PrefixID.WEAK,
		EntityID.PrefixID.SHODDY,
		EntityID.PrefixID.RUTHLESS,
		EntityID.PrefixID.QUICK,
		EntityID.PrefixID.DEADLY2,
		EntityID.PrefixID.AGILE,
		EntityID.PrefixID.NIMBLE,
		EntityID.PrefixID.MURDEROUS,
		EntityID.PrefixID.SLOW,
		EntityID.PrefixID.SLUGGISH,
		EntityID.PrefixID.LAZY,
		EntityID.PrefixID.ANNOYING,
		EntityID.PrefixID.NASTY,
		EntityID.PrefixID.GODLY,
		EntityID.PrefixID.DEMONIC,
		EntityID.PrefixID.ZEALOUS,
		EntityID.PrefixID.MYTHICAL
	};

	private static readonly EntityID.PrefixID[] BoomerangPrefixs = new EntityID.PrefixID[14]
	{
		EntityID.PrefixID.KEEN,
		EntityID.PrefixID.SUPERIOR,
		EntityID.PrefixID.FORCEFUL,
		EntityID.PrefixID.HURTFUL,
		EntityID.PrefixID.STRONG,
		EntityID.PrefixID.UNPLEASANT,
		EntityID.PrefixID.BROKEN,
		EntityID.PrefixID.DAMAGED,
		EntityID.PrefixID.WEAK,
		EntityID.PrefixID.SHODDY,
		EntityID.PrefixID.RUTHLESS,
		EntityID.PrefixID.GODLY,
		EntityID.PrefixID.DEMONIC,
		EntityID.PrefixID.ZEALOUS
	};

	public static readonly EntityID.ItemID[] ArmorIDs = new EntityID.ItemID[59]
	{
		EntityID.ItemID.MINING_HELMET,
		EntityID.ItemID.MINING_SHIRT,
		EntityID.ItemID.MINING_PANTS,
		EntityID.ItemID.COPPER_HELMET,
		EntityID.ItemID.COPPER_CHAINMAIL,
		EntityID.ItemID.COPPER_GREAVES,
		EntityID.ItemID.IRON_HELMET,
		EntityID.ItemID.IRON_CHAINMAIL,
		EntityID.ItemID.IRON_GREAVES,
		EntityID.ItemID.SILVER_HELMET,
		EntityID.ItemID.SILVER_CHAINMAIL,
		EntityID.ItemID.SILVER_GREAVES,
		EntityID.ItemID.GOLD_HELMET,
		EntityID.ItemID.GOLD_CHAINMAIL,
		EntityID.ItemID.GOLD_GREAVES,
		EntityID.ItemID.METEOR_HELMET,
		EntityID.ItemID.METEOR_SUIT,
		EntityID.ItemID.METEOR_LEGGINGS,
		EntityID.ItemID.SHADOW_HELMET,
		EntityID.ItemID.SHADOW_SCALEMAIL,
		EntityID.ItemID.SHADOW_GREAVES,
		EntityID.ItemID.JUNGLE_HAT,
		EntityID.ItemID.JUNGLE_SHIRT,
		EntityID.ItemID.JUNGLE_PANTS,
		EntityID.ItemID.NECRO_HELMET,
		EntityID.ItemID.NECRO_BREASTPLATE,
		EntityID.ItemID.NECRO_GREAVES,
		EntityID.ItemID.MOLTEN_HELMET,
		EntityID.ItemID.MOLTEN_BREASTPLATE,
		EntityID.ItemID.MOLTEN_GREAVES,
		EntityID.ItemID.COBALT_HAT,
		EntityID.ItemID.COBALT_HELMET,
		EntityID.ItemID.COBALT_MASK,
		EntityID.ItemID.COBALT_BREASTPLATE,
		EntityID.ItemID.COBALT_LEGGINGS,
		EntityID.ItemID.MYTHRIL_HOOD,
		EntityID.ItemID.MYTHRIL_HELMET,
		EntityID.ItemID.MYTHRIL_HAT,
		EntityID.ItemID.MYTHRIL_CHAINMAIL,
		EntityID.ItemID.MYTHRIL_GREAVES,
		EntityID.ItemID.ADAMANTITE_HEADGEAR,
		EntityID.ItemID.ADAMANTITE_MASK,
		EntityID.ItemID.ADAMANTITE_HELMET,
		EntityID.ItemID.ADAMANTITE_BREASTPLATE,
		EntityID.ItemID.ADAMANTITE_LEGGINGS,
		EntityID.ItemID.HALLOWED_HEADGEAR,
		EntityID.ItemID.HALLOWED_MASK,
		EntityID.ItemID.HALLOWED_HELMET,
		EntityID.ItemID.HALLOWED_PLATE_MAIL,
		EntityID.ItemID.HALLOWED_GREAVES,
		EntityID.ItemID.DRAGON_MASK,
		EntityID.ItemID.DRAGON_BREASTPLATE,
		EntityID.ItemID.DRAGON_GREAVES,
		EntityID.ItemID.TITAN_HELMET,
		EntityID.ItemID.TITAN_MAIL,
		EntityID.ItemID.TITAN_LEGGINGS,
		EntityID.ItemID.SPECTRAL_HEADGEAR,
		EntityID.ItemID.SPECTRAL_ARMOR,
		EntityID.ItemID.SPECTRAL_SUBLIGAR
	};

	private static uint LastItemIndex = 0u;

	public void Init()
	{
		Active = 0;
		Owner = 8;
		Type = 0;
		NetID = 0;
		PrefixType = 0;
		Crit = 0;
		WornArmor = false;
		Mech = false;
		ReuseDelay = 0;
		IsMelee = false;
		IsMagic = false;
		IsRanged = false;
		PlaceStyle = 0;
		BuffTime = 0;
		BuffType = 0;
		Light = 0f;
		IsMaterial = false;
		CantTouchLiquid = false;
		IsVanity = false;
		Mana = 0;
		IsWet = false;
		WetCount = 0;
		IsInLava = false;
		Channelling = false;
		OnlyBuyOnce = false;
		IsSocial = false;
		Release = 0;
		NoMelee = false;
		NoUseGraphic = false;
		LifeRegen = 0;
		ShootSpeed = 0f;
		Alpha = 0;
		Ammo = 0;
		UseAmmo = 0;
		AutoReuse = false;
		IsAccessory = false;
		AxePower = 0;
		HealMana = 0;
		BodySlot = -1;
		LegSlot = -1;
		HeadSlot = -1;
		IsPotion = false;
		IsConsumable = false;
		CreateTile = -1;
		CreateWall = -1;
		Damage = 0;
		Defense = 0;
		HammerPower = 0;
		HealLife = 0;
		Knockback = 0f;
		PickPower = 0;
		Rarity = 0;
		Scale = 1f;
		Shoot = 0;
		Stack = 0;
		MaxStack = 0;
		TileBoost = 0;
		HoldStyle = 0;
		UseStyle = 0;
		UseSound = 0;
		UseTime = 100;
		UseAnimation = 100;
		Value = 0;
		CanUseTurn = false;
		CanBuy = false;
		OwnIgnore = 8;
		OwnTime = 0;
		KeepTime = 0;
	}

	public bool IsLocal()
	{
		if (Owner < 8)
		{
			return Main.PlayerSet[Owner].isLocal();
		}
		return false;
	}

	public bool IsEquipable()
	{
		if (!IsAccessory && HeadSlot < 0 && BodySlot < 0)
		{
			return LegSlot >= 0;
		}
		return true;
	}

	public bool SetPrefix(int Setting)
	{
		if (Setting == 0 || Type == 0)
		{
			return false;
		}
		// One funnel for every source of a prefix: a save, MSG_SYNC_ITEM, CLIENT_INVENTORY, a chest
		// slot. The value is a byte on the wire and PrefixType is a byte, so 84..255 are
		// representable, and Lang.ItemPrefix indexes an 84-entry name table with this number - so
		// a modified client could crash the host with one packet, and a save from a build with a
		// larger prefix table would do the same on load. Refused here rather than at each call
		// site, because the call sites are exactly the thing that grows.
		if (Setting >= (int)EntityID.PrefixID.NUM_PREFIXES)
		{
			Program.LogWarn("Items", "prefix " + Setting + " does not exist in this build ("
				+ (int)EntityID.PrefixID.NUM_PREFIXES + " prefixes) - ignored");
			return false;
		}
		int num = Setting;
		float num2 = 1f;
		float num3 = 1f;
		float num4 = 1f;
		float num5 = 1f;
		float num6 = 1f;
		float num7 = 1f;
		int num8 = 0;
		bool flag = true;
		while (flag)
		{
			num2 = 1f;
			num3 = 1f;
			num4 = 1f;
			num5 = 1f;
			num6 = 1f;
			num7 = 1f;
			num8 = 0;
			flag = false;
			if (num == -1 && Main.Rand.Next(4) == 0)
			{
				num = 0;
			}
			if (Setting < -1)
			{
				num = -1;
			}
			if (num == -1 || num == -2 || num == -3)
			{
				if (Terraria.Mods.ModItems.IsModItem(Type))
				{
					// A mod item is in none of the vanilla id lists below, so every reforge fell
					// through to `return false`: the Goblin refused to touch any mod weapon, and the
					// inventory greyed it out as unreforgeable. The item already says what it is -
					// the definition sets IsMelee, IsRanged, IsMagic or IsAccessory - so the pool is
					// chosen from that instead of from a list of ids this build ships.
					//
					// An item with none of those flags is not a weapon and not an accessory, so it is
					// still refused: that is the same answer vanilla gives a block of dirt.
					if (IsAccessory)
					{
						num = Main.Rand.Next(62, 81);
					}
					else if (IsMagic)
					{
						num = (int)MagicPrefixs[Main.Rand.Next(MagicPrefixs.Length)];
					}
					else if (IsRanged)
					{
						num = (int)GunPrefixs[Main.Rand.Next(GunPrefixs.Length)];
					}
					else if (IsMelee)
					{
						num = (int)ToolPrefixs[Main.Rand.Next(ToolPrefixs.Length)];
					}
					else
					{
						return false;
					}
				}
				else if (Array.BinarySearch(EntityID.ToolItems, (EntityID.ItemID)Type) >= 0)
				{
					num = (int)ToolPrefixs[Main.Rand.Next(ToolPrefixs.Length)];
				}
				else if (Array.BinarySearch(EntityID.SpearItems, (EntityID.ItemID)Type) >= 0)
				{
					num = (int)SpearPrefixs[Main.Rand.Next(SpearPrefixs.Length)];
				}
				else if (Array.BinarySearch(EntityID.GunItems, (EntityID.ItemID)Type) >= 0)
				{
					num = (int)GunPrefixs[Main.Rand.Next(GunPrefixs.Length)];
				}
				else if (Array.BinarySearch(EntityID.MagicItems, (EntityID.ItemID)Type) >= 0)
				{
					num = (int)MagicPrefixs[Main.Rand.Next(MagicPrefixs.Length)];
				}
				else if (Array.BinarySearch(EntityID.BoomerangItems, (EntityID.ItemID)Type) >= 0)
				{
					num = (int)BoomerangPrefixs[Main.Rand.Next(BoomerangPrefixs.Length)];
				}
				else
				{
					if (!IsAccessory || Array.BinarySearch(EntityID.ExcludedEquips, (EntityID.ItemID)Type) >= 0)
					{
						return false;
					}
					num = Main.Rand.Next(62, 81);
				}
			}
			switch (Setting)
			{
			case -3:
				return true;
			case -1:
				if (Array.BinarySearch(EntityID.BadPrefixes, (EntityID.PrefixID)num) >= 0 && Main.Rand.Next(3) != 0)
				{
					num = 0;
				}
				break;
			}
			switch ((EntityID.PrefixID)(byte)num)
			{
			case EntityID.PrefixID.LARGE:
				num5 = 1.12f;
				break;
			case EntityID.PrefixID.MASSIVE:
				num5 = 1.18f;
				break;
			case EntityID.PrefixID.DANGEROUS:
				num2 = 1.05f;
				num8 = 2;
				num5 = 1.05f;
				break;
			case EntityID.PrefixID.SAVAGE:
				num2 = 1.1f;
				num5 = 1.1f;
				num3 = 1.1f;
				break;
			case EntityID.PrefixID.SHARP:
				num2 = 1.15f;
				break;
			case EntityID.PrefixID.POINTY:
				num2 = 1.1f;
				break;
			case EntityID.PrefixID.TINY:
				num5 = 0.82f;
				break;
			case EntityID.PrefixID.TERRIBLE:
				num3 = 0.85f;
				num2 = 0.85f;
				num5 = 0.87f;
				break;
			case EntityID.PrefixID.SMALL:
				num5 = 0.9f;
				break;
			case EntityID.PrefixID.DULL:
				num2 = 0.85f;
				break;
			case EntityID.PrefixID.UNHAPPY:
				num4 = 1.1f;
				num3 = 0.9f;
				num5 = 0.9f;
				break;
			case EntityID.PrefixID.BULKY:
				num3 = 1.1f;
				num2 = 1.05f;
				num5 = 1.1f;
				num4 = 1.15f;
				break;
			case EntityID.PrefixID.SHAMEFUL:
				num3 = 0.8f;
				num2 = 0.9f;
				num5 = 1.1f;
				break;
			case EntityID.PrefixID.HEAVY:
				num3 = 1.15f;
				num4 = 1.1f;
				break;
			case EntityID.PrefixID.LIGHT:
				num3 = 0.9f;
				num4 = 0.85f;
				break;
			case EntityID.PrefixID.SIGHTED:
				num2 = 1.1f;
				num8 = 3;
				break;
			case EntityID.PrefixID.RAPID:
				num4 = 0.85f;
				num6 = 1.1f;
				break;
			case EntityID.PrefixID.HASTY:
				num4 = 0.9f;
				num6 = 1.15f;
				break;
			case EntityID.PrefixID.INTIMIDATING:
				num3 = 1.15f;
				num6 = 1.05f;
				break;
			case EntityID.PrefixID.DEADLY:
				num3 = 1.05f;
				num6 = 1.05f;
				num2 = 1.1f;
				num4 = 0.95f;
				num8 = 2;
				break;
			case EntityID.PrefixID.STAUNCH:
				num3 = 1.15f;
				num2 = 1.1f;
				break;
			case EntityID.PrefixID.AWFUL:
				num3 = 0.9f;
				num6 = 0.9f;
				num2 = 0.85f;
				break;
			case EntityID.PrefixID.LETHARGIC:
				num4 = 1.15f;
				num6 = 0.9f;
				break;
			case EntityID.PrefixID.AWKWARD:
				num4 = 1.1f;
				num3 = 0.8f;
				break;
			case EntityID.PrefixID.POWERFUL:
				num4 = 1.1f;
				num2 = 1.15f;
				num8 = 1;
				break;
			case EntityID.PrefixID.MYSTIC:
				num7 = 0.85f;
				num2 = 1.1f;
				break;
			case EntityID.PrefixID.ADEPT:
				num7 = 0.85f;
				break;
			case EntityID.PrefixID.MASTERFUL:
				num7 = 0.85f;
				num2 = 1.15f;
				num3 = 1.05f;
				break;
			case EntityID.PrefixID.INEPT:
				num7 = 1.1f;
				break;
			case EntityID.PrefixID.IGNORANT:
				num7 = 1.2f;
				num2 = 0.9f;
				break;
			case EntityID.PrefixID.DERANGED:
				num3 = 0.9f;
				num2 = 0.9f;
				break;
			case EntityID.PrefixID.INTENSE:
				num7 = 1.15f;
				num2 = 1.1f;
				break;
			case EntityID.PrefixID.TABOO:
				num7 = 1.1f;
				num3 = 1.1f;
				num4 = 0.9f;
				break;
			case EntityID.PrefixID.CELESTIAL:
				num7 = 0.9f;
				num3 = 1.1f;
				num4 = 1.1f;
				num2 = 1.1f;
				break;
			case EntityID.PrefixID.FURIOUS:
				num7 = 1.2f;
				num2 = 1.15f;
				num3 = 1.15f;
				break;
			case EntityID.PrefixID.KEEN:
				num8 = 3;
				break;
			case EntityID.PrefixID.SUPERIOR:
				num2 = 1.1f;
				num8 = 3;
				num3 = 1.1f;
				break;
			case EntityID.PrefixID.FORCEFUL:
				num3 = 1.15f;
				break;
			case EntityID.PrefixID.BROKEN:
				num2 = 0.7f;
				num3 = 0.8f;
				break;
			case EntityID.PrefixID.DAMAGED:
				num2 = 0.85f;
				break;
			case EntityID.PrefixID.SHODDY:
				num3 = 0.85f;
				num2 = 0.9f;
				break;
			case EntityID.PrefixID.QUICK:
				num4 = 0.9f;
				break;
			case EntityID.PrefixID.DEADLY2:
				num2 = 1.1f;
				num4 = 0.9f;
				break;
			case EntityID.PrefixID.AGILE:
				num4 = 0.9f;
				num8 = 3;
				break;
			case EntityID.PrefixID.NIMBLE:
				num4 = 0.95f;
				break;
			case EntityID.PrefixID.MURDEROUS:
				num8 = 3;
				num4 = 0.94f;
				num2 = 1.07f;
				break;
			case EntityID.PrefixID.SLOW:
				num4 = 1.15f;
				break;
			case EntityID.PrefixID.SLUGGISH:
				num4 = 1.2f;
				break;
			case EntityID.PrefixID.LAZY:
				num4 = 1.08f;
				break;
			case EntityID.PrefixID.ANNOYING:
				num2 = 0.8f;
				num4 = 1.15f;
				break;
			case EntityID.PrefixID.NASTY:
				num3 = 0.9f;
				num4 = 0.9f;
				num2 = 1.05f;
				num8 = 2;
				break;
			case EntityID.PrefixID.MANIC:
				num7 = 0.9f;
				num2 = 0.9f;
				num4 = 0.9f;
				break;
			case EntityID.PrefixID.HURTFUL:
				num2 = 1.1f;
				break;
			case EntityID.PrefixID.STRONG:
				num3 = 1.15f;
				break;
			case EntityID.PrefixID.UNPLEASANT:
				num3 = 1.15f;
				num2 = 1.05f;
				break;
			case EntityID.PrefixID.WEAK:
				num3 = 0.8f;
				break;
			case EntityID.PrefixID.RUTHLESS:
				num3 = 0.9f;
				num2 = 1.18f;
				break;
			case EntityID.PrefixID.FRENZYING:
				num4 = 0.85f;
				num2 = 0.85f;
				break;
			case EntityID.PrefixID.GODLY:
				num3 = 1.15f;
				num2 = 1.15f;
				num8 = 5;
				break;
			case EntityID.PrefixID.DEMONIC:
				num2 = 1.15f;
				num8 = 5;
				break;
			case EntityID.PrefixID.ZEALOUS:
				num8 = 5;
				break;
			case EntityID.PrefixID.LEGENDARY:
				num3 = 1.15f;
				num2 = 1.15f;
				num8 = 5;
				num4 = 0.9f;
				num5 = 1.1f;
				break;
			case EntityID.PrefixID.UNREAL:
				num3 = 1.15f;
				num2 = 1.15f;
				num8 = 5;
				num4 = 0.9f;
				num6 = 1.1f;
				break;
			case EntityID.PrefixID.MYTHICAL:
				num3 = 1.15f;
				num2 = 1.15f;
				num8 = 5;
				num4 = 0.9f;
				num7 = 0.9f;
				break;
			}
			if (num2 != 1f && Math.Round((float)Damage * num2) == (double)Damage)
			{
				flag = true;
				num = -1;
			}
			if (num4 != 1f && Math.Round((float)(int)UseAnimation * num4) == (double)(int)UseAnimation)
			{
				flag = true;
				num = -1;
			}
			if (num7 != 1f && Math.Round((float)(int)Mana * num7) == (double)(int)Mana)
			{
				flag = true;
				num = -1;
			}
			if (num3 != 1f && Knockback == 0f)
			{
				flag = true;
				num = -1;
			}
			if (Setting == -2 && num == 0)
			{
				num = -1;
				flag = true;
			}
		}
		Damage = (short)Math.Round((float)Damage * num2);
		UseAnimation = (byte)Math.Round((float)(int)UseAnimation * num4);
		UseTime = (byte)Math.Round((float)(int)UseTime * num4);
		ReuseDelay = (byte)Math.Round((float)(int)ReuseDelay * num4);
		Mana = (byte)Math.Round((float)(int)Mana * num7);
		Knockback *= num3;
		Scale *= num5;
		ShootSpeed *= num6;
		Crit += (short)num8;
		float num9 = num2 * (2f - num4) * (2f - num7) * num5 * num3 * num6 * (1f + (float)Crit * 0.02f);
		switch ((EntityID.PrefixID)(byte)num)
		{
		case EntityID.PrefixID.HARD:
		case EntityID.PrefixID.JAGGED:
		case EntityID.PrefixID.BRISK:
		case EntityID.PrefixID.WILD:
			num9 *= 1.05f;
			break;
		case EntityID.PrefixID.GUARDING:
		case EntityID.PrefixID.PRECISE:
		case EntityID.PrefixID.SPIKED:
		case EntityID.PrefixID.FLEETING:
		case EntityID.PrefixID.RASH:
			num9 *= 1.1f;
			break;
		case EntityID.PrefixID.ARMORED:
		case EntityID.PrefixID.ARCANE:
		case EntityID.PrefixID.ANGRY:
		case EntityID.PrefixID.HASTY2:
		case EntityID.PrefixID.INTREPID:
			num9 *= 1.15f;
			break;
		case EntityID.PrefixID.WARDING:
		case EntityID.PrefixID.LUCKY:
		case EntityID.PrefixID.MENACING:
		case EntityID.PrefixID.QUICK2:
		case EntityID.PrefixID.VIOLENT:
			num9 *= 1.2f;
			break;
		}
		PrefixType = (byte)num;
		if (num9 >= 1.2f)
		{
			Rarity += 2;
		}
		else if (num9 >= 1.05f)
		{
			Rarity++;
		}
		else if (num9 <= 0.8f)
		{
			Rarity -= 2;
		}
		else if (num9 <= 0.95f)
		{
			Rarity--;
		}
		if (Rarity < -1)
		{
			Rarity = -1;
		}
		else if (Rarity > 6)
		{
			Rarity = 6;
		}
		num9 *= num9;
		Value = (int)((float)Value * num9);
		return true;
	}

	public string Name()
	{
		return Lang.ItemName(NetID);
	}

	public string AffixName()
	{
		return Lang.ItemAffixName(PrefixType, NetID);
	}

	public void SetDefaults(string ItemName)
	{
		bool flag = false;
		switch (ItemName)
		{
		case "Gold Pickaxe":
			SetDefaults(1);
			Colour = new Color(210, 190, 0, 100);
			UseTime = 17;
			PickPower = 55;
			UseAnimation = 20;
			Scale = 1.05f;
			Damage = 6;
			Value = 10000;
			NetID = -1;
			break;
		case "Gold Broadsword":
			SetDefaults(4);
			Colour = new Color(210, 190, 0, 100);
			UseAnimation = 20;
			Damage = 13;
			Scale = 1.05f;
			Value = 9000;
			NetID = -2;
			break;
		case "Gold Shortsword":
			SetDefaults(6);
			Colour = new Color(210, 190, 0, 100);
			Damage = 11;
			UseAnimation = 11;
			Scale = 0.95f;
			Value = 7000;
			NetID = -3;
			break;
		case "Gold Axe":
			SetDefaults(10);
			Colour = new Color(210, 190, 0, 100);
			UseTime = 18;
			AxePower = 11;
			UseAnimation = 26;
			Scale = 1.15f;
			Damage = 7;
			Value = 8000;
			NetID = -4;
			break;
		case "Gold Hammer":
			SetDefaults(7);
			Colour = new Color(210, 190, 0, 100);
			UseAnimation = 28;
			UseTime = 23;
			Scale = 1.25f;
			Damage = 9;
			HammerPower = 55;
			Value = 8000;
			NetID = -5;
			break;
		case "Gold Bow":
			SetDefaults(99);
			UseAnimation = 26;
			UseTime = 26;
			Colour = new Color(210, 190, 0, 100);
			Damage = 11;
			Value = 7000;
			NetID = -6;
			break;
		case "Silver Pickaxe":
			SetDefaults(1);
			Colour = new Color(180, 180, 180, 100);
			UseTime = 11;
			PickPower = 45;
			UseAnimation = 19;
			Scale = 1.05f;
			Damage = 6;
			Value = 5000;
			NetID = -7;
			break;
		case "Silver Broadsword":
			SetDefaults(4);
			Colour = new Color(180, 180, 180, 100);
			UseAnimation = 21;
			Damage = 11;
			Value = 4500;
			NetID = -8;
			break;
		case "Silver Shortsword":
			SetDefaults(6);
			Colour = new Color(180, 180, 180, 100);
			Damage = 9;
			UseAnimation = 12;
			Scale = 0.95f;
			Value = 3500;
			NetID = -9;
			break;
		case "Silver Axe":
			SetDefaults(10);
			Colour = new Color(180, 180, 180, 100);
			UseTime = 18;
			AxePower = 10;
			UseAnimation = 26;
			Scale = 1.15f;
			Damage = 6;
			Value = 4000;
			NetID = -10;
			break;
		case "Silver Hammer":
			SetDefaults(7);
			Colour = new Color(180, 180, 180, 100);
			UseAnimation = 29;
			UseTime = 19;
			Scale = 1.25f;
			Damage = 9;
			HammerPower = 45;
			Value = 4000;
			NetID = -11;
			break;
		case "Silver Bow":
			SetDefaults(99);
			UseAnimation = 27;
			UseTime = 27;
			Colour = new Color(180, 180, 180, 100);
			Damage = 9;
			Value = 3500;
			NetID = -12;
			break;
		case "Copper Pickaxe":
			SetDefaults(1);
			Colour = new Color(180, 100, 45, 80);
			UseTime = 15;
			PickPower = 35;
			UseAnimation = 23;
			Damage = 4;
			Scale = 0.9f;
			TileBoost = -1;
			Value = 500;
			NetID = -13;
			break;
		case "Copper Broadsword":
			SetDefaults(4);
			Colour = new Color(180, 100, 45, 80);
			UseAnimation = 23;
			Damage = 8;
			Value = 450;
			NetID = -14;
			break;
		case "Copper Shortsword":
			SetDefaults(6);
			Colour = new Color(180, 100, 45, 80);
			Damage = 5;
			UseAnimation = 13;
			Scale = 0.8f;
			Value = 350;
			NetID = -15;
			break;
		case "Copper Axe":
			SetDefaults(10);
			Colour = new Color(180, 100, 45, 80);
			UseTime = 21;
			AxePower = 7;
			UseAnimation = 30;
			Scale = 1f;
			Damage = 3;
			TileBoost = -1;
			Value = 400;
			NetID = -16;
			break;
		case "Copper Hammer":
			SetDefaults(7);
			Colour = new Color(180, 100, 45, 80);
			UseAnimation = 33;
			UseTime = 23;
			Scale = 1.1f;
			Damage = 4;
			HammerPower = 35;
			TileBoost = -1;
			Value = 400;
			NetID = -17;
			break;
		case "Copper Bow":
			SetDefaults(99);
			UseAnimation = 29;
			UseTime = 29;
			Colour = new Color(180, 100, 45, 80);
			Damage = 6;
			Value = 350;
			NetID = -18;
			break;
		case "Blue Phasesaber":
			SetDefaults(198);
			Damage = 41;
			Scale = 1.15f;
			flag = true;
			AutoReuse = true;
			CanUseTurn = true;
			Rarity = 4;
			NetID = -19;
			break;
		case "Red Phasesaber":
			SetDefaults(199);
			Damage = 41;
			Scale = 1.15f;
			flag = true;
			AutoReuse = true;
			CanUseTurn = true;
			Rarity = 4;
			NetID = -20;
			break;
		case "Green Phasesaber":
			SetDefaults(200);
			Damage = 41;
			Scale = 1.15f;
			flag = true;
			AutoReuse = true;
			CanUseTurn = true;
			Rarity = 4;
			NetID = -21;
			break;
		case "Purple Phasesaber":
			SetDefaults(201);
			Damage = 41;
			Scale = 1.15f;
			flag = true;
			AutoReuse = true;
			CanUseTurn = true;
			Rarity = 4;
			NetID = -22;
			break;
		case "White Phasesaber":
			SetDefaults(202);
			Damage = 41;
			Scale = 1.15f;
			flag = true;
			AutoReuse = true;
			CanUseTurn = true;
			Rarity = 4;
			NetID = -23;
			break;
		case "Yellow Phasesaber":
			SetDefaults(203);
			Damage = 41;
			Scale = 1.15f;
			flag = true;
			AutoReuse = true;
			CanUseTurn = true;
			Rarity = 4;
			NetID = -24;
			break;
		}
		if (flag)
		{
			IsMaterial = false;
		}
		else
		{
			CheckMaterial();
		}
		this = Terraria.Mods.ItemOverrides.Apply(this);
	}

	public bool CheckMaterial()
	{
		if (CanBePlacedInCoinSlot())
		{
			IsMaterial = false;
			return false;
		}
		for (int i = 0; i < Recipe.NumRecipes; i++)
		{
			int num = Main.ActiveRecipe[i].NumRequiredItems - 1;
			do
			{
				if (NetID == Main.ActiveRecipe[i].RequiredItem[num].NetID)
				{
					IsMaterial = true;
					return true;
				}
			}
			while (--num >= 0);
		}
		IsMaterial = false;
		return false;
	}

	public void NetDefaults(int Type, int Stack = 1)
	{
		if (Type < 0)
		{
			switch ((EntityID.ItemID)Type)
			{
			case EntityID.ItemID.GOLD_PICKAXE:
				SetDefaults("Gold Pickaxe");
				break;
			case EntityID.ItemID.GOLD_BROADSWORD:
				SetDefaults("Gold Broadsword");
				break;
			case EntityID.ItemID.GOLD_SHORTSWORD:
				SetDefaults("Gold Shortsword");
				break;
			case EntityID.ItemID.GOLD_AXE:
				SetDefaults("Gold Axe");
				break;
			case EntityID.ItemID.GOLD_HAMMER:
				SetDefaults("Gold Hammer");
				break;
			case EntityID.ItemID.GOLD_BOW:
				SetDefaults("Gold Bow");
				break;
			case EntityID.ItemID.SILVER_PICKAXE:
				SetDefaults("Silver Pickaxe");
				break;
			case EntityID.ItemID.SILVER_BROADSWORD:
				SetDefaults("Silver Broadsword");
				break;
			case EntityID.ItemID.SILVER_SHORTSWORD:
				SetDefaults("Silver Shortsword");
				break;
			case EntityID.ItemID.SILVER_AXE:
				SetDefaults("Silver Axe");
				break;
			case EntityID.ItemID.SILVER_HAMMER:
				SetDefaults("Silver Hammer");
				break;
			case EntityID.ItemID.SILVER_BOW:
				SetDefaults("Silver Bow");
				break;
			case EntityID.ItemID.COPPER_PICKAXE:
				SetDefaults("Copper Pickaxe");
				break;
			case EntityID.ItemID.COPPER_BROADSWORD:
				SetDefaults("Copper Broadsword");
				break;
			case EntityID.ItemID.COPPER_SHORTSWORD:
				SetDefaults("Copper Shortsword");
				break;
			case EntityID.ItemID.COPPER_AXE:
				SetDefaults("Copper Axe");
				break;
			case EntityID.ItemID.COPPER_HAMMER:
				SetDefaults("Copper Hammer");
				break;
			case EntityID.ItemID.COPPER_BOW:
				SetDefaults("Copper Bow");
				break;
			case EntityID.ItemID.BLUE_PHASESABER:
				SetDefaults("Blue Phasesaber");
				break;
			case EntityID.ItemID.RED_PHASESABER:
				SetDefaults("Red Phasesaber");
				break;
			case EntityID.ItemID.GREEN_PHASESABER:
				SetDefaults("Green Phasesaber");
				break;
			case EntityID.ItemID.PURPLE_PHASESABER:
				SetDefaults("Purple Phasesaber");
				break;
			case EntityID.ItemID.WHITE_PHASESABER:
				SetDefaults("White Phasesaber");
				break;
			case EntityID.ItemID.YELLOW_PHASESABER:
				SetDefaults("Yellow Phasesaber");
				break;
			}
		}
		else
		{
			SetDefaults(Type, Stack);
		}
	}

	public void SetDefaults(int ItemType, int ItemStack = 1, bool NoMaterialCheck = false)
	{
		Active = 1;
		Owner = 8;
		Type = (short)ItemType;
		NetID = (short)ItemType;
		PrefixType = 0;
		Crit = 0;
		WornArmor = false;
		Mech = false;
		ReuseDelay = 0;
		IsMelee = false;
		IsMagic = false;
		IsRanged = false;
		PlaceStyle = 0;
		BuffTime = 0;
		BuffType = 0;
		Light = 0f;
		IsMaterial = false;
		CantTouchLiquid = false;
		IsVanity = false;
		Mana = 0;
		IsWet = false;
		WetCount = 0;
		IsInLava = false;
		Channelling = false;
		OnlyBuyOnce = false;
		IsSocial = false;
		Release = 0;
		NoMelee = false;
		NoUseGraphic = false;
		LifeRegen = 0;
		ShootSpeed = 0f;
		Alpha = 0;
		Ammo = 0;
		UseAmmo = 0;
		AutoReuse = false;
		IsAccessory = false;
		AxePower = 0;
		HealMana = 0;
		BodySlot = -1;
		LegSlot = -1;
		HeadSlot = -1;
		IsPotion = false;
		Colour = default;
		IsConsumable = false;
		CreateTile = -1;
		CreateWall = -1;
		Damage = 0;
		Defense = 0;
		HammerPower = 0;
		HealLife = 0;
		Knockback = 0f;
		PickPower = 0;
		Rarity = 0;
		Scale = 1f;
		Shoot = 0;
		Stack = (short)ItemStack;
		MaxStack = (short)ItemStack;
		TileBoost = 0;
		HoldStyle = 0;
		UseStyle = 0;
		UseSound = 0;
		UseTime = 100;
		UseAnimation = 100;
		Value = 0;
		CanUseTurn = false;
		CanBuy = false;
		OwnIgnore = 8;
		OwnTime = 0;
		KeepTime = 0;
		// A new item from a mod: the compiled switch below only knows vanilla ids, so the registry
		// answers first. Placed after the reset block so a definition starts from the same clean
		// state every vanilla item does.
		if (Terraria.Mods.ModItems.IsModItem(ItemType))
		{
			Terraria.Mods.ModItems.Apply(ref this, ItemType);
			// Overrides apply here too, after the definition. The `return` that used to be here
			// skipped them, so `Context.Item("Some Mod's Sword")` - which resolves through the
			// same name table and returns a real handle - stored an override that was never read.
			// A mod that adjusted another mod's item got a silent no-op, which is the worst kind
			// of "it works": the handle was not null and nothing reported a problem.
			this = Terraria.Mods.ItemOverrides.Apply(this);
			return;
		}
		switch ((EntityID.ItemID)ItemType)
		{
		case EntityID.ItemID.IRON_PICKAXE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 20;
			UseTime = 13;
			AutoReuse = true;
			Width = 24;
			Height = 28;
			Damage = 5;
			PickPower = 40;
			UseSound = 1;
			Knockback = 2f;
			Value = 2000;
			IsMelee = true;
			break;
		case EntityID.ItemID.DIRT_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 0;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.STONE_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 1;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.IRON_BROADSWORD:
			UseStyle = 1;
			CanUseTurn = false;
			UseAnimation = 21;
			UseTime = 21;
			Width = 24;
			Height = 28;
			Damage = 10;
			Knockback = 5f;
			UseSound = 1;
			Scale = 1f;
			Value = 1800;
			IsMelee = true;
			break;
		case EntityID.ItemID.MUSHROOM:
			UseStyle = 2;
			UseSound = 2;
			CanUseTurn = false;
			UseAnimation = 17;
			UseTime = 17;
			Width = 16;
			Height = 18;
			HealLife = 15;
			MaxStack = 99;
			IsConsumable = true;
			IsPotion = true;
			Value = 25;
			break;
		case EntityID.ItemID.IRON_SHORTSWORD:
			UseStyle = 3;
			CanUseTurn = false;
			UseAnimation = 12;
			UseTime = 12;
			Width = 24;
			Height = 28;
			Damage = 8;
			Knockback = 4f;
			Scale = 0.9f;
			UseSound = 1;
			CanUseTurn = true;
			Value = 1400;
			IsMelee = true;
			break;
		case EntityID.ItemID.IRON_HAMMER:
			AutoReuse = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 30;
			UseTime = 20;
			HammerPower = 45;
			Width = 24;
			Height = 28;
			Damage = 7;
			Knockback = 5.5f;
			Scale = 1.2f;
			UseSound = 1;
			Value = 1600;
			IsMelee = true;
			break;
		case EntityID.ItemID.TORCH:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			Width = 10;
			Height = 12;
			Value = 50;
			break;
		case EntityID.ItemID.WOOD:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 30;
			Width = 8;
			Height = 10;
			break;
		case EntityID.ItemID.IRON_AXE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 27;
			Knockback = 4.5f;
			UseTime = 19;
			AutoReuse = true;
			Width = 24;
			Height = 28;
			Damage = 5;
			AxePower = 9;
			Scale = 1.1f;
			UseSound = 1;
			Value = 1600;
			IsMelee = true;
			break;
		case EntityID.ItemID.IRON_ORE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 6;
			Width = 12;
			Height = 12;
			Value = 500;
			break;
		case EntityID.ItemID.COPPER_ORE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 7;
			Width = 12;
			Height = 12;
			Value = 250;
			break;
		case EntityID.ItemID.GOLD_ORE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 8;
			Width = 12;
			Height = 12;
			Value = 2000;
			break;
		case EntityID.ItemID.SILVER_ORE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 9;
			Width = 12;
			Height = 12;
			Value = 1000;
			break;
		case EntityID.ItemID.COPPER_WATCH:
			Width = 24;
			Height = 28;
			IsAccessory = true;
			Value = 1000;
			break;
		case EntityID.ItemID.SILVER_WATCH:
			Width = 24;
			Height = 28;
			IsAccessory = true;
			Value = 5000;
			break;
		case EntityID.ItemID.GOLD_WATCH:
			Width = 24;
			Height = 28;
			IsAccessory = true;
			Rarity = 1;
			Value = 10000;
			break;
		case EntityID.ItemID.DEPTH_METER:
			Width = 24;
			Height = 18;
			IsAccessory = true;
			Rarity = 1;
			Value = 10000;
			break;
		case EntityID.ItemID.GOLD_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Value = 6000;
			break;
		case EntityID.ItemID.COPPER_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Value = 750;
			break;
		case EntityID.ItemID.SILVER_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Value = 3000;
			break;
		case EntityID.ItemID.IRON_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Value = 1500;
			break;
		case EntityID.ItemID.GEL:
			Width = 10;
			Height = 12;
			MaxStack = 250;
			Alpha = 175;
			Ammo = 23;
			Colour = new Color(0, 80, 255, 100);
			Value = 5;
			break;
		case EntityID.ItemID.WOODEN_SWORD:
			UseStyle = 1;
			CanUseTurn = false;
			UseAnimation = 25;
			Width = 24;
			Height = 28;
			Damage = 7;
			Knockback = 4f;
			Scale = 0.95f;
			UseSound = 1;
			Value = 100;
			IsMelee = true;
			break;
		case EntityID.ItemID.WOODEN_DOOR:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 10;
			Width = 14;
			Height = 28;
			Value = 200;
			break;
		case EntityID.ItemID.STONE_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 1;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.ACORN:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 20;
			Width = 18;
			Height = 18;
			Value = 10;
			break;
		case EntityID.ItemID.LESSER_HEALING_POTION:
			UseSound = 3;
			HealLife = 50;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			IsPotion = true;
			Value = 300;
			break;
		case EntityID.ItemID.LIFE_CRYSTAL:
			MaxStack = 99;
			IsConsumable = true;
			Width = 18;
			Height = 18;
			UseStyle = 4;
			UseTime = 30;
			UseSound = 4;
			UseAnimation = 30;
			Rarity = 2;
			Value = 75000;
			break;
		case EntityID.ItemID.DIRT_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 16;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.BOTTLE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 13;
			Width = 16;
			Height = 24;
			Value = 20;
			break;
		case EntityID.ItemID.WOODEN_TABLE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 14;
			Width = 26;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.FURNACE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 17;
			Width = 26;
			Height = 24;
			Value = 300;
			break;
		case EntityID.ItemID.WOODEN_CHAIR:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 15;
			Width = 12;
			Height = 30;
			Value = 150;
			break;
		case EntityID.ItemID.IRON_ANVIL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 16;
			Width = 28;
			Height = 14;
			Value = 5000;
			break;
		case EntityID.ItemID.WORK_BENCH:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 18;
			Width = 28;
			Height = 14;
			Value = 150;
			break;
		case EntityID.ItemID.GOGGLES:
			Width = 28;
			Height = 12;
			Defense = 1;
			HeadSlot = 10;
			Rarity = 1;
			Value = 1000;
			break;
		case EntityID.ItemID.LENS:
			Width = 12;
			Height = 20;
			MaxStack = 99;
			Value = 500;
			break;
		case EntityID.ItemID.WOODEN_BOW:
			UseStyle = 5;
			UseAnimation = 30;
			UseTime = 30;
			Width = 12;
			Height = 28;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 4;
			ShootSpeed = 6.1f;
			NoMelee = true;
			Value = 100;
			IsRanged = true;
			break;
		case EntityID.ItemID.WOODEN_ARROW:
			ShootSpeed = 3f;
			Shoot = 1;
			Damage = 4;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 2f;
			Value = 10;
			IsRanged = true;
			break;
		case EntityID.ItemID.FLAMING_ARROW:
			ShootSpeed = 3.5f;
			Shoot = 2;
			Damage = 6;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 2f;
			Value = 15;
			IsRanged = true;
			break;
		case EntityID.ItemID.SHURIKEN:
			UseStyle = 1;
			ShootSpeed = 9f;
			Shoot = 3;
			Damage = 10;
			Width = 18;
			Height = 20;
			MaxStack = 250;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 20;
			IsRanged = true;
			break;
		case EntityID.ItemID.SUSPICIOUS_LOOKING_EYE:
			UseStyle = 4;
			Width = 22;
			Height = 14;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			MaxStack = 20;
			break;
		case EntityID.ItemID.SUSPICIOUS_LOOKING_SKULL:
			UseStyle = 4;
			Width = 26;
			Height = 26;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			MaxStack = 20;
			break;
		case EntityID.ItemID.DEMON_BOW:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 25;
			Width = 12;
			Height = 28;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 14;
			ShootSpeed = 6.7f;
			Knockback = 1f;
			Alpha = 30;
			Rarity = 1;
			NoMelee = true;
			Value = 18000;
			IsRanged = true;
			break;
		case EntityID.ItemID.WAR_AXE_OF_THE_NIGHT:
			AutoReuse = true;
			UseStyle = 1;
			UseAnimation = 30;
			Knockback = 6f;
			UseTime = 15;
			Width = 24;
			Height = 28;
			Damage = 20;
			AxePower = 15;
			Scale = 1.2f;
			UseSound = 1;
			Rarity = 1;
			Value = 13500;
			IsMelee = true;
			break;
		case EntityID.ItemID.LIGHTS_BANE:
			UseStyle = 1;
			UseAnimation = 20;
			Knockback = 5f;
			Width = 24;
			Height = 28;
			Damage = 17;
			Scale = 1.1f;
			UseSound = 1;
			Rarity = 1;
			Value = 13500;
			IsMelee = true;
			break;
		case EntityID.ItemID.UNHOLY_ARROW:
			ShootSpeed = 3.4f;
			Shoot = 4;
			Damage = 8;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 3f;
			Alpha = 30;
			Rarity = 1;
			Value = 40;
			IsRanged = true;
			break;
		case EntityID.ItemID.CHEST:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 21;
			Width = 26;
			Height = 22;
			Value = 500;
			break;
		case EntityID.ItemID.BAND_OF_REGENERATION:
			Width = 22;
			Height = 22;
			IsAccessory = true;
			LifeRegen = 1;
			Rarity = 1;
			Value = 50000;
			break;
		case EntityID.ItemID.MAGIC_MIRROR:
			Mana = 20;
			CanUseTurn = true;
			Width = 20;
			Height = 20;
			UseStyle = 4;
			UseTime = 90;
			UseSound = 6;
			UseAnimation = 90;
			Rarity = 1;
			Value = 50000;
			break;
		case EntityID.ItemID.JESTERS_ARROW:
			ShootSpeed = 0.5f;
			Shoot = 5;
			Damage = 9;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 4f;
			Rarity = 1;
			Value = 100;
			IsRanged = true;
			break;
		case EntityID.ItemID.ANGEL_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 1;
			break;
		case EntityID.ItemID.CLOUD_IN_A_BOTTLE:
			Width = 16;
			Height = 24;
			IsAccessory = true;
			Rarity = 1;
			Value = 50000;
			break;
		case EntityID.ItemID.HERMES_BOOTS:
			Width = 28;
			Height = 24;
			IsAccessory = true;
			Rarity = 1;
			Value = 50000;
			break;
		case EntityID.ItemID.ENCHANTED_BOOMERANG:
			NoMelee = true;
			UseStyle = 1;
			ShootSpeed = 10f;
			Shoot = 6;
			Damage = 13;
			Knockback = 8f;
			Width = 14;
			Height = 28;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			Rarity = 1;
			Value = 50000;
			IsMelee = true;
			break;
		case EntityID.ItemID.DEMONITE_ORE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 22;
			Width = 12;
			Height = 12;
			Rarity = 1;
			Value = 4000;
			break;
		case EntityID.ItemID.DEMONITE_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Rarity = 1;
			Value = 16000;
			break;
		case EntityID.ItemID.HEART:
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.CORRUPT_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 23;
			Width = 14;
			Height = 14;
			Value = 500;
			break;
		case EntityID.ItemID.VILE_MUSHROOM:
			Width = 16;
			Height = 18;
			MaxStack = 99;
			Value = 50;
			break;
		case EntityID.ItemID.EBONSTONE_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 25;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.GRASS_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 2;
			Width = 14;
			Height = 14;
			Value = 20;
			break;
		case EntityID.ItemID.SUNFLOWER:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 27;
			Width = 26;
			Height = 26;
			Value = 200;
			break;
		case EntityID.ItemID.VILETHORN:
			Mana = 12;
			Damage = 8;
			UseStyle = 1;
			ShootSpeed = 32f;
			Shoot = 7;
			Width = 26;
			Height = 28;
			UseSound = 8;
			UseAnimation = 30;
			UseTime = 30;
			Rarity = 1;
			NoMelee = true;
			Knockback = 1f;
			Value = 10000;
			IsMagic = true;
			break;
		case EntityID.ItemID.STARFURY:
			AutoReuse = true;
			Mana = 16;
			Knockback = 5f;
			Alpha = 100;
			Colour = new Color(150, 150, 150, 0);
			Damage = 16;
			UseStyle = 1;
			Scale = 1.15f;
			ShootSpeed = 12f;
			Shoot = 9;
			Width = 14;
			Height = 28;
			UseSound = 9;
			UseAnimation = 25;
			UseTime = 10;
			Rarity = 1;
			Value = 50000;
			IsMagic = true;
			break;
		case EntityID.ItemID.PURIFICATION_POWDER:
			UseStyle = 1;
			ShootSpeed = 4f;
			Shoot = 10;
			Width = 16;
			Height = 24;
			MaxStack = 99;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoMelee = true;
			Value = 75;
			break;
		case EntityID.ItemID.VILE_POWDER:
			Damage = 0;
			UseStyle = 1;
			ShootSpeed = 4f;
			Shoot = 11;
			Width = 16;
			Height = 24;
			MaxStack = 99;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoMelee = true;
			Value = 100;
			break;
		case EntityID.ItemID.ROTTEN_CHUNK:
			Width = 18;
			Height = 20;
			MaxStack = 99;
			Value = 10;
			break;
		case EntityID.ItemID.WORM_TOOTH:
			Width = 8;
			Height = 20;
			MaxStack = 99;
			Value = 100;
			break;
		case EntityID.ItemID.WORM_FOOD:
			UseStyle = 4;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			Width = 28;
			Height = 28;
			MaxStack = 20;
			break;
		case EntityID.ItemID.COPPER_COIN:
			Width = 10;
			Height = 12;
			MaxStack = 100;
			Value = 5;
			break;
		case EntityID.ItemID.SILVER_COIN:
			Width = 10;
			Height = 12;
			MaxStack = 100;
			Value = 500;
			break;
		case EntityID.ItemID.GOLD_COIN:
			Width = 10;
			Height = 12;
			MaxStack = 100;
			Value = 50000;
			break;
		case EntityID.ItemID.PLATINUM_COIN:
			Width = 10;
			Height = 12;
			MaxStack = 100;
			Value = 5000000;
			break;
		case EntityID.ItemID.FALLEN_STAR:
			Width = 18;
			Height = 20;
			MaxStack = 100;
			Alpha = 75;
			Ammo = 15;
			Value = 500;
			UseStyle = 4;
			UseSound = 4;
			CanUseTurn = false;
			UseAnimation = 17;
			UseTime = 17;
			IsConsumable = true;
			Rarity = 1;
			break;
		case EntityID.ItemID.COPPER_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 1;
			LegSlot = 1;
			Value = 750;
			break;
		case EntityID.ItemID.IRON_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 2;
			LegSlot = 2;
			Value = 3000;
			break;
		case EntityID.ItemID.SILVER_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 3;
			LegSlot = 3;
			Value = 7500;
			break;
		case EntityID.ItemID.GOLD_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 4;
			LegSlot = 4;
			Value = 15000;
			break;
		case EntityID.ItemID.COPPER_CHAINMAIL:
			Width = 18;
			Height = 18;
			Defense = 2;
			BodySlot = 1;
			Value = 1000;
			break;
		case EntityID.ItemID.IRON_CHAINMAIL:
			Width = 18;
			Height = 18;
			Defense = 3;
			BodySlot = 2;
			Value = 4000;
			break;
		case EntityID.ItemID.SILVER_CHAINMAIL:
			Width = 18;
			Height = 18;
			Defense = 4;
			BodySlot = 3;
			Value = 10000;
			break;
		case EntityID.ItemID.GOLD_CHAINMAIL:
			Width = 18;
			Height = 18;
			Defense = 5;
			BodySlot = 4;
			Value = 20000;
			break;
		case EntityID.ItemID.GRAPPLING_HOOK:
			NoUseGraphic = true;
			Damage = 0;
			Knockback = 7f;
			UseStyle = 5;
			ShootSpeed = 11f;
			Shoot = 13;
			Width = 18;
			Height = 28;
			UseSound = 1;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 1;
			NoMelee = true;
			Value = 20000;
			break;
		case EntityID.ItemID.CHAIN:
			Width = 14;
			Height = 20;
			MaxStack = 99;
			Value = 1000;
			break;
		case EntityID.ItemID.SHADOW_SCALE:
			Width = 14;
			Height = 18;
			MaxStack = 99;
			Rarity = 1;
			Value = 500;
			break;
		case EntityID.ItemID.PIGGY_BANK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 29;
			Width = 20;
			Height = 12;
			Value = 10000;
			break;
		case EntityID.ItemID.MINING_HELMET:
			Width = 22;
			Height = 16;
			Defense = 1;
			HeadSlot = 11;
			Rarity = 1;
			Value = 80000;
			break;
		case EntityID.ItemID.COPPER_HELMET:
			Width = 18;
			Height = 18;
			Defense = 1;
			HeadSlot = 1;
			Value = 1250;
			break;
		case EntityID.ItemID.IRON_HELMET:
			Width = 18;
			Height = 18;
			Defense = 2;
			HeadSlot = 2;
			Value = 5000;
			break;
		case EntityID.ItemID.SILVER_HELMET:
			Width = 18;
			Height = 18;
			Defense = 3;
			HeadSlot = 3;
			Value = 12500;
			break;
		case EntityID.ItemID.GOLD_HELMET:
			Width = 18;
			Height = 18;
			Defense = 4;
			HeadSlot = 4;
			Value = 25000;
			break;
		case EntityID.ItemID.WOOD_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 4;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.WOOD_PLATFORM:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 19;
			Width = 8;
			Height = 10;
			break;
		case EntityID.ItemID.FLINTLOCK_PISTOL:
			UseStyle = 5;
			UseAnimation = 16;
			UseTime = 16;
			Width = 24;
			Height = 28;
			Shoot = 14;
			UseAmmo = 14;
			UseSound = 11;
			Damage = 10;
			ShootSpeed = 5f;
			NoMelee = true;
			Value = 50000;
			Scale = 0.9f;
			Rarity = 1;
			IsRanged = true;
			break;
		case EntityID.ItemID.MUSKET:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 43;
			UseTime = 43;
			Width = 44;
			Height = 14;
			Shoot = 10;
			UseAmmo = 14;
			UseSound = 11;
			Damage = 23;
			ShootSpeed = 8f;
			NoMelee = true;
			Value = 100000;
			Knockback = 4f;
			Rarity = 1;
			IsRanged = true;
			break;
		case EntityID.ItemID.MUSKET_BALL:
			ShootSpeed = 4f;
			Shoot = 14;
			Damage = 7;
			Width = 8;
			Height = 8;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 14;
			Knockback = 2f;
			Value = 7;
			IsRanged = true;
			break;
		case EntityID.ItemID.MINISHARK:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 8;
			UseTime = 8;
			Width = 50;
			Height = 18;
			Shoot = 10;
			UseAmmo = 14;
			UseSound = 11;
			Damage = 6;
			ShootSpeed = 7f;
			NoMelee = true;
			Value = 350000;
			Rarity = 2;
			IsRanged = true;
			break;
		case EntityID.ItemID.IRON_BOW:
			UseStyle = 5;
			UseAnimation = 28;
			UseTime = 28;
			Width = 12;
			Height = 28;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 8;
			ShootSpeed = 6.6f;
			NoMelee = true;
			Value = 1400;
			IsRanged = true;
			break;
		case EntityID.ItemID.SHADOW_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 6;
			LegSlot = 5;
			Rarity = 1;
			Value = 22500;
			break;
		case EntityID.ItemID.SHADOW_SCALEMAIL:
			Width = 18;
			Height = 18;
			Defense = 7;
			BodySlot = 5;
			Rarity = 1;
			Value = 30000;
			break;
		case EntityID.ItemID.SHADOW_HELMET:
			Width = 18;
			Height = 18;
			Defense = 6;
			HeadSlot = 5;
			Rarity = 1;
			Value = 37500;
			break;
		case EntityID.ItemID.NIGHTMARE_PICKAXE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 20;
			UseTime = 15;
			AutoReuse = true;
			Width = 24;
			Height = 28;
			Damage = 9;
			PickPower = 65;
			UseSound = 1;
			Knockback = 3f;
			Rarity = 1;
			Value = 18000;
			Scale = 1.15f;
			IsMelee = true;
			break;
		case EntityID.ItemID.THE_BREAKER:
			AutoReuse = true;
			UseStyle = 1;
			UseAnimation = 45;
			UseTime = 19;
			HammerPower = 55;
			Width = 24;
			Height = 28;
			Damage = 24;
			Knockback = 6f;
			Scale = 1.3f;
			UseSound = 1;
			Rarity = 1;
			Value = 15000;
			IsMelee = true;
			break;
		case EntityID.ItemID.CANDLE:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 33;
			Width = 8;
			Height = 18;
			HoldStyle = 1;
			break;
		case EntityID.ItemID.COPPER_CHANDELIER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 34;
			Width = 26;
			Height = 26;
			break;
		case EntityID.ItemID.SILVER_CHANDELIER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 35;
			Width = 26;
			Height = 26;
			break;
		case EntityID.ItemID.GOLD_CHANDELIER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 36;
			Width = 26;
			Height = 26;
			break;
		case EntityID.ItemID.MANA_CRYSTAL:
			MaxStack = 99;
			IsConsumable = true;
			Width = 18;
			Height = 18;
			UseStyle = 4;
			UseTime = 30;
			UseSound = 29;
			UseAnimation = 30;
			Rarity = 2;
			break;
		case EntityID.ItemID.LESSER_MANA_POTION:
			UseSound = 3;
			HealMana = 50;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 20;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			Value = 100;
			break;
		case EntityID.ItemID.BAND_OF_STARPOWER:
			Width = 22;
			Height = 22;
			IsAccessory = true;
			Rarity = 1;
			Value = 50000;
			break;
		case EntityID.ItemID.FLOWER_OF_FIRE:
			Mana = 17;
			Damage = 44;
			UseStyle = 1;
			ShootSpeed = 6f;
			Shoot = 15;
			Width = 26;
			Height = 28;
			UseSound = 20;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Knockback = 5.5f;
			Value = 10000;
			IsMagic = true;
			break;
		case EntityID.ItemID.MAGIC_MISSILE:
			Mana = 10;
			Channelling = true;
			Damage = 22;
			UseStyle = 1;
			ShootSpeed = 6f;
			Shoot = 16;
			Width = 26;
			Height = 28;
			UseSound = 9;
			UseAnimation = 17;
			UseTime = 17;
			Rarity = 2;
			NoMelee = true;
			Knockback = 5f;
			TileBoost = 64;
			Value = 10000;
			IsMagic = true;
			break;
		case EntityID.ItemID.DIRT_ROD:
			Mana = 5;
			Channelling = true;
			Damage = 0;
			UseStyle = 1;
			Shoot = 17;
			Width = 26;
			Height = 28;
			UseSound = 8;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 1;
			NoMelee = true;
			Knockback = 5f;
			Value = 200000;
			break;
		case EntityID.ItemID.SHADOW_ORB:
			Mana = 40;
			Channelling = true;
			Damage = 0;
			UseStyle = 4;
			Shoot = 18;
			Width = 24;
			Height = 24;
			UseSound = 8;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 1;
			NoMelee = true;
			Value = 10000;
			BuffType = 19;
			BuffTime = 18000;
			break;
		case EntityID.ItemID.METEORITE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 37;
			Width = 12;
			Height = 12;
			Value = 1000;
			break;
		case EntityID.ItemID.METEORITE_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Rarity = 1;
			Value = 7000;
			break;
		case EntityID.ItemID.HOOK:
			MaxStack = 99;
			Width = 18;
			Height = 18;
			Value = 1000;
			break;
		case EntityID.ItemID.FLAMARANG:
			NoMelee = true;
			UseStyle = 1;
			ShootSpeed = 11f;
			Shoot = 19;
			Damage = 32;
			Knockback = 8f;
			Width = 14;
			Height = 28;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			Rarity = 3;
			Value = 100000;
			IsMelee = true;
			break;
		case EntityID.ItemID.MOLTEN_FURY:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 25;
			Width = 14;
			Height = 32;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 29;
			ShootSpeed = 8f;
			Knockback = 2f;
			Alpha = 30;
			Rarity = 3;
			NoMelee = true;
			Scale = 1.1f;
			Value = 27000;
			IsRanged = true;
			break;
		case EntityID.ItemID.SHARANGA:
			UseStyle = 5;
			UseAnimation = 20;
			UseTime = 20;
			Width = 14;
			Height = 32;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 35;
			ShootSpeed = 10f;
			Knockback = 2.3f;
			Alpha = 30;
			Rarity = 5;
			NoMelee = true;
			Scale = 1.1f;
			Value = 60000;
			IsRanged = true;
			break;
		case EntityID.ItemID.FIERY_GREATSWORD:
			UseStyle = 1;
			UseAnimation = 34;
			Knockback = 6.5f;
			Width = 24;
			Height = 28;
			Damage = 36;
			Scale = 1.3f;
			UseSound = 1;
			Rarity = 3;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.MOLTEN_PICKAXE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 25;
			UseTime = 25;
			AutoReuse = true;
			Width = 24;
			Height = 28;
			Damage = 12;
			PickPower = 100;
			Scale = 1.15f;
			UseSound = 1;
			Knockback = 2f;
			Rarity = 3;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.METEOR_HELMET:
			Width = 18;
			Height = 18;
			Defense = 3;
			HeadSlot = 6;
			Rarity = 1;
			Value = 45000;
			break;
		case EntityID.ItemID.METEOR_SUIT:
			Width = 18;
			Height = 18;
			Defense = 4;
			BodySlot = 6;
			Rarity = 1;
			Value = 30000;
			break;
		case EntityID.ItemID.METEOR_LEGGINGS:
			Width = 18;
			Height = 18;
			Defense = 3;
			LegSlot = 6;
			Rarity = 1;
			Value = 30000;
			break;
		case EntityID.ItemID.BOTTLED_WATER:
			UseSound = 3;
			HealLife = 20;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			IsPotion = true;
			Value = 20;
			break;
		case EntityID.ItemID.SPACE_GUN:
			AutoReuse = true;
			UseStyle = 5;
			UseAnimation = 19;
			UseTime = 19;
			Width = 24;
			Height = 28;
			Shoot = 20;
			Mana = 8;
			UseSound = 12;
			Knockback = 0.5f;
			Damage = 17;
			ShootSpeed = 10f;
			NoMelee = true;
			Scale = 0.8f;
			Rarity = 1;
			IsMagic = true;
			Value = 20000;
			break;
		case EntityID.ItemID.ROCKET_BOOTS:
			Width = 28;
			Height = 24;
			IsAccessory = true;
			Rarity = 3;
			Value = 50000;
			break;
		case EntityID.ItemID.GRAY_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 38;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.GRAY_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 5;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.RED_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 39;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.RED_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 6;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.CLAY_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 40;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.BLUE_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 41;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.BLUE_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 17;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.CHAIN_LANTERN:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 42;
			Width = 12;
			Height = 28;
			break;
		case EntityID.ItemID.GREEN_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 43;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.GREEN_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 18;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.PINK_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 44;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.PINK_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 19;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.GOLD_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 45;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.GOLD_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 10;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.SILVER_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 46;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.SILVER_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 11;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.COPPER_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 47;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.COPPER_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 12;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.SPIKE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 48;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.WATER_CANDLE:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 49;
			Width = 8;
			Height = 18;
			HoldStyle = 1;
			break;
		case EntityID.ItemID.BOOK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 50;
			Width = 24;
			Height = 28;
			break;
		case EntityID.ItemID.COBWEB:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 51;
			Width = 20;
			Height = 24;
			Alpha = 100;
			break;
		case EntityID.ItemID.NECRO_HELMET:
			Width = 18;
			Height = 18;
			Defense = 5;
			HeadSlot = 7;
			Rarity = 2;
			Value = 45000;
			break;
		case EntityID.ItemID.NECRO_BREASTPLATE:
			Width = 18;
			Height = 18;
			Defense = 6;
			BodySlot = 7;
			Rarity = 2;
			Value = 30000;
			break;
		case EntityID.ItemID.NECRO_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 5;
			LegSlot = 7;
			Rarity = 2;
			Value = 30000;
			break;
		case EntityID.ItemID.BONE:
			MaxStack = 99;
			IsConsumable = true;
			Width = 12;
			Height = 14;
			Value = 50;
			UseAnimation = 12;
			UseTime = 12;
			UseStyle = 1;
			UseSound = 1;
			ShootSpeed = 8f;
			NoUseGraphic = true;
			Damage = 22;
			Knockback = 4f;
			Shoot = 21;
			IsRanged = true;
			break;
		case EntityID.ItemID.MURAMASA:
			AutoReuse = true;
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 20;
			Width = 40;
			Height = 40;
			Damage = 18;
			Scale = 1.1f;
			UseSound = 1;
			Rarity = 2;
			Value = 27000;
			Knockback = 1f;
			IsMelee = true;
			break;
		case EntityID.ItemID.COBALT_SHIELD:
			Width = 24;
			Height = 28;
			Rarity = 2;
			Value = 27000;
			IsAccessory = true;
			Defense = 1;
			break;
		case EntityID.ItemID.AQUA_SCEPTER:
			Mana = 7;
			AutoReuse = true;
			UseStyle = 5;
			UseAnimation = 16;
			UseTime = 8;
			Knockback = 5f;
			Width = 38;
			Height = 10;
			Damage = 14;
			Scale = 1f;
			Shoot = 22;
			ShootSpeed = 11f;
			UseSound = 13;
			Rarity = 2;
			Value = 27000;
			IsMagic = true;
			break;
		case EntityID.ItemID.LUCKY_HORSESHOE:
			Width = 20;
			Height = 22;
			Rarity = 1;
			Value = 27000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.SHINY_RED_BALLOON:
			Width = 14;
			Height = 28;
			Rarity = 1;
			Value = 27000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.HARPOON:
			AutoReuse = true;
			UseStyle = 5;
			UseAnimation = 30;
			UseTime = 30;
			Knockback = 6f;
			Width = 30;
			Height = 10;
			Damage = 25;
			Scale = 1.1f;
			Shoot = 23;
			ShootSpeed = 11f;
			UseSound = 10;
			Rarity = 2;
			Value = 27000;
			IsRanged = true;
			break;
		case EntityID.ItemID.SPIKY_BALL:
			UseStyle = 1;
			ShootSpeed = 5f;
			Shoot = 24;
			Knockback = 1f;
			Damage = 15;
			Width = 10;
			Height = 10;
			MaxStack = 250;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 80;
			IsRanged = true;
			break;
		case EntityID.ItemID.BALL_O_HURT:
			UseStyle = 5;
			UseAnimation = 45;
			UseTime = 45;
			Knockback = 6.5f;
			Width = 30;
			Height = 10;
			Damage = 15;
			Scale = 1.1f;
			NoUseGraphic = true;
			Shoot = 25;
			ShootSpeed = 12f;
			UseSound = 1;
			Rarity = 1;
			Value = 27000;
			IsMelee = true;
			Channelling = true;
			NoMelee = true;
			break;
		case EntityID.ItemID.BLUE_MOON:
			UseStyle = 5;
			UseAnimation = 45;
			UseTime = 45;
			Knockback = 7f;
			Width = 30;
			Height = 10;
			Damage = 23;
			Scale = 1.1f;
			NoUseGraphic = true;
			Shoot = 26;
			ShootSpeed = 12f;
			UseSound = 1;
			Rarity = 2;
			Value = 27000;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.HANDGUN:
			AutoReuse = false;
			UseStyle = 5;
			UseAnimation = 12;
			UseTime = 12;
			Width = 24;
			Height = 24;
			Shoot = 14;
			Knockback = 3f;
			UseAmmo = 14;
			UseSound = 11;
			Damage = 14;
			ShootSpeed = 10f;
			NoMelee = true;
			Value = 50000;
			Scale = 0.75f;
			Rarity = 2;
			IsRanged = true;
			break;
		case EntityID.ItemID.WATER_BOLT:
			AutoReuse = true;
			Rarity = 2;
			Mana = 14;
			UseSound = 21;
			UseStyle = 5;
			Damage = 17;
			UseAnimation = 17;
			UseTime = 17;
			Width = 24;
			Height = 28;
			Shoot = 27;
			Scale = 0.9f;
			ShootSpeed = 4.5f;
			Knockback = 5f;
			IsMagic = true;
			Value = 50000;
			break;
		case EntityID.ItemID.BOMB:
			UseStyle = 1;
			ShootSpeed = 5f;
			Shoot = 28;
			Width = 20;
			Height = 20;
			MaxStack = 50;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 25;
			UseTime = 25;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 500;
			Damage = 0;
			break;
		case EntityID.ItemID.DYNAMITE:
			UseStyle = 1;
			ShootSpeed = 4f;
			Shoot = 29;
			Width = 8;
			Height = 28;
			MaxStack = 5;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 40;
			UseTime = 40;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 5000;
			Rarity = 1;
			break;
		case EntityID.ItemID.GRENADE:
			UseStyle = 5;
			ShootSpeed = 5.5f;
			Shoot = 30;
			Width = 20;
			Height = 20;
			MaxStack = 99;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 45;
			UseTime = 45;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 400;
			Damage = 60;
			Knockback = 8f;
			IsRanged = true;
			break;
		case EntityID.ItemID.SAND_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 53;
			Width = 12;
			Height = 12;
			Ammo = 42;
			break;
		case EntityID.ItemID.GLASS:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 54;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.SIGN:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 55;
			Width = 28;
			Height = 28;
			break;
		case EntityID.ItemID.ASH_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 57;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.OBSIDIAN:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 56;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.HELLSTONE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 58;
			Width = 12;
			Height = 12;
			Rarity = 2;
			break;
		case EntityID.ItemID.HELLSTONE_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Rarity = 2;
			Value = 20000;
			break;
		case EntityID.ItemID.MUD_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 59;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.AMETHYST:
			MaxStack = 99;
			Alpha = 50;
			Width = 10;
			Height = 14;
			Value = 1875;
			break;
		case EntityID.ItemID.TOPAZ:
			MaxStack = 99;
			Alpha = 50;
			Width = 10;
			Height = 14;
			Value = 3750;
			break;
		case EntityID.ItemID.SAPPHIRE:
			MaxStack = 99;
			Alpha = 50;
			Width = 10;
			Height = 14;
			Value = 5625;
			break;
		case EntityID.ItemID.EMERALD:
			MaxStack = 99;
			Alpha = 50;
			Width = 10;
			Height = 14;
			Value = 7500;
			break;
		case EntityID.ItemID.RUBY:
			MaxStack = 99;
			Alpha = 50;
			Width = 10;
			Height = 14;
			Value = 11250;
			break;
		case EntityID.ItemID.DIAMOND:
			MaxStack = 99;
			Alpha = 50;
			Width = 10;
			Height = 14;
			Value = 15000;
			break;
		case EntityID.ItemID.GLOWING_MUSHROOM:
			UseStyle = 2;
			UseSound = 2;
			CanUseTurn = false;
			UseAnimation = 17;
			UseTime = 17;
			Width = 16;
			Height = 18;
			HealLife = 25;
			MaxStack = 99;
			IsConsumable = true;
			IsPotion = true;
			Value = 50;
			break;
		case EntityID.ItemID.STAR:
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.IVY_WHIP:
			NoUseGraphic = true;
			Damage = 0;
			Knockback = 7f;
			UseStyle = 5;
			ShootSpeed = 13f;
			Shoot = 32;
			Width = 18;
			Height = 28;
			UseSound = 1;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Value = 20000;
			break;
		case EntityID.ItemID.BREATHING_REED:
			Width = 44;
			Height = 44;
			Rarity = 1;
			Value = 10000;
			HoldStyle = 2;
			break;
		case EntityID.ItemID.FLIPPER:
			Width = 28;
			Height = 28;
			Rarity = 1;
			Value = 10000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.HEALING_POTION:
			UseSound = 3;
			HealLife = 100;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			Rarity = 1;
			IsPotion = true;
			Value = 1000;
			break;
		case EntityID.ItemID.MANA_POTION:
			UseSound = 3;
			HealMana = 100;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 50;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			Rarity = 1;
			Value = 250;
			break;
		case EntityID.ItemID.BLADE_OF_GRASS:
			UseStyle = 1;
			UseAnimation = 30;
			Knockback = 3f;
			Width = 40;
			Height = 40;
			Damage = 28;
			Scale = 1.4f;
			UseSound = 1;
			Rarity = 3;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.THORN_CHAKRAM:
			NoMelee = true;
			UseStyle = 1;
			ShootSpeed = 11f;
			Shoot = 33;
			Damage = 25;
			Knockback = 8f;
			Width = 14;
			Height = 28;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			Rarity = 3;
			Value = 50000;
			IsMelee = true;
			break;
		case EntityID.ItemID.OBSIDIAN_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 75;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.OBSIDIAN_SKULL:
			Width = 20;
			Height = 22;
			Rarity = 2;
			Value = 27000;
			IsAccessory = true;
			Defense = 1;
			break;
		case EntityID.ItemID.MUSHROOM_GRASS_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 70;
			Width = 14;
			Height = 14;
			Value = 150;
			break;
		case EntityID.ItemID.JUNGLE_GRASS_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 60;
			Width = 14;
			Height = 14;
			Value = 150;
			break;
		case EntityID.ItemID.WOODEN_HAMMER:
			AutoReuse = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 37;
			UseTime = 25;
			HammerPower = 25;
			Width = 24;
			Height = 28;
			Damage = 2;
			Knockback = 5.5f;
			Scale = 1.2f;
			UseSound = 1;
			TileBoost = -1;
			Value = 50;
			IsMelee = true;
			break;
		case EntityID.ItemID.STAR_CANNON:
			AutoReuse = true;
			UseStyle = 5;
			UseAnimation = 12;
			UseTime = 12;
			Width = 50;
			Height = 18;
			Shoot = 12;
			UseAmmo = 15;
			UseSound = 9;
			Damage = 55;
			ShootSpeed = 14f;
			NoMelee = true;
			Value = 500000;
			Rarity = 2;
			IsRanged = true;
			break;
		case EntityID.ItemID.BLUE_PHASEBLADE:
			UseStyle = 1;
			UseAnimation = 25;
			Knockback = 3f;
			Width = 40;
			Height = 40;
			Damage = 21;
			Scale = 1f;
			UseSound = 15;
			Rarity = 1;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.RED_PHASEBLADE:
			UseStyle = 1;
			UseAnimation = 25;
			Knockback = 3f;
			Width = 40;
			Height = 40;
			Damage = 21;
			Scale = 1f;
			UseSound = 15;
			Rarity = 1;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.GREEN_PHASEBLADE:
			UseStyle = 1;
			UseAnimation = 25;
			Knockback = 3f;
			Width = 40;
			Height = 40;
			Damage = 21;
			Scale = 1f;
			UseSound = 15;
			Rarity = 1;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.PURPLE_PHASEBLADE:
			UseStyle = 1;
			UseAnimation = 25;
			Knockback = 3f;
			Width = 40;
			Height = 40;
			Damage = 21;
			Scale = 1f;
			UseSound = 15;
			Rarity = 1;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.WHITE_PHASEBLADE:
			UseStyle = 1;
			UseAnimation = 25;
			Knockback = 3f;
			Width = 40;
			Height = 40;
			Damage = 21;
			Scale = 1f;
			UseSound = 15;
			Rarity = 1;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.YELLOW_PHASEBLADE:
			UseStyle = 1;
			UseAnimation = 25;
			Knockback = 3f;
			Width = 40;
			Height = 40;
			Damage = 21;
			Scale = 1f;
			UseSound = 15;
			Rarity = 1;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.METEOR_HAMAXE:
			CanUseTurn = true;
			AutoReuse = true;
			UseStyle = 1;
			UseAnimation = 30;
			UseTime = 16;
			HammerPower = 60;
			AxePower = 20;
			Width = 24;
			Height = 28;
			Damage = 20;
			Knockback = 7f;
			Scale = 1.2f;
			UseSound = 1;
			Rarity = 1;
			Value = 15000;
			IsMelee = true;
			break;
		case EntityID.ItemID.EMPTY_BUCKET:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			Width = 20;
			Height = 20;
			HeadSlot = 13;
			Defense = 1;
			break;
		case EntityID.ItemID.WATER_BUCKET:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			Width = 20;
			Height = 20;
			break;
		case EntityID.ItemID.LAVA_BUCKET:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			Width = 20;
			Height = 20;
			break;
		case EntityID.ItemID.JUNGLE_ROSE:
			Width = 20;
			Height = 20;
			Value = 100;
			HeadSlot = 23;
			IsVanity = true;
			break;
		case EntityID.ItemID.STINGER:
			Width = 16;
			Height = 18;
			MaxStack = 99;
			Value = 200;
			break;
		case EntityID.ItemID.VINE:
			Width = 14;
			Height = 20;
			MaxStack = 99;
			Value = 1000;
			break;
		case EntityID.ItemID.FERAL_CLAWS:
			Width = 20;
			Height = 20;
			IsAccessory = true;
			Rarity = 3;
			Value = 50000;
			break;
		case EntityID.ItemID.ANKLET_OF_THE_WIND:
			Width = 20;
			Height = 20;
			IsAccessory = true;
			Rarity = 3;
			Value = 50000;
			break;
		case EntityID.ItemID.STAFF_OF_REGROWTH:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 25;
			UseTime = 13;
			AutoReuse = true;
			Width = 24;
			Height = 28;
			Damage = 7;
			CreateTile = 2;
			Scale = 1.2f;
			UseSound = 1;
			Knockback = 3f;
			Rarity = 3;
			Value = 2000;
			IsMelee = true;
			break;
		case EntityID.ItemID.HELLSTONE_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 76;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.WHOOPIE_CUSHION:
			Width = 18;
			Height = 18;
			CanUseTurn = true;
			UseTime = 30;
			UseAnimation = 30;
			NoUseGraphic = true;
			NoMelee = true;
			UseStyle = 10;
			UseSound = 16;
			Rarity = 2;
			Value = 100;
			break;
		case EntityID.ItemID.SHACKLE:
			Width = 20;
			Height = 20;
			Rarity = 1;
			Value = 1500;
			IsAccessory = true;
			Defense = 1;
			break;
		case EntityID.ItemID.MOLTEN_HAMAXE:
			CanUseTurn = true;
			AutoReuse = true;
			UseStyle = 1;
			UseAnimation = 27;
			UseTime = 14;
			HammerPower = 70;
			AxePower = 30;
			Width = 24;
			Height = 28;
			Damage = 20;
			Knockback = 7f;
			Scale = 1.4f;
			UseSound = 1;
			Rarity = 3;
			Value = 15000;
			IsMelee = true;
			break;
		case EntityID.ItemID.FLAMELASH:
			Mana = 16;
			Channelling = true;
			Damage = 34;
			UseStyle = 1;
			ShootSpeed = 6f;
			Shoot = 34;
			Width = 26;
			Height = 28;
			UseSound = 20;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Knockback = 6.5f;
			TileBoost = 64;
			Value = 10000;
			IsMagic = true;
			break;
		case EntityID.ItemID.PHOENIX_BLASTER:
			AutoReuse = false;
			UseStyle = 5;
			UseAnimation = 11;
			UseTime = 11;
			Width = 24;
			Height = 22;
			Shoot = 14;
			Knockback = 2f;
			UseAmmo = 14;
			UseSound = 11;
			Damage = 23;
			ShootSpeed = 13f;
			NoMelee = true;
			Value = 50000;
			Scale = 0.75f;
			Rarity = 3;
			IsRanged = true;
			break;
		case EntityID.ItemID.SUNFURY:
			NoMelee = true;
			UseStyle = 5;
			UseAnimation = 45;
			UseTime = 45;
			Knockback = 7f;
			Width = 30;
			Height = 10;
			Damage = 33;
			Scale = 1.1f;
			NoUseGraphic = true;
			Shoot = 35;
			ShootSpeed = 12f;
			UseSound = 1;
			Rarity = 3;
			Value = 27000;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.HELLFORGE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 77;
			Width = 26;
			Height = 24;
			Value = 3000;
			break;
		case EntityID.ItemID.CLAY_POT:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 78;
			Width = 14;
			Height = 14;
			Value = 100;
			break;
		case EntityID.ItemID.NATURES_GIFT:
			Width = 20;
			Height = 22;
			Rarity = 3;
			Value = 27000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.BED:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 79;
			Width = 28;
			Height = 20;
			Value = 2000;
			break;
		case EntityID.ItemID.SILK:
			MaxStack = 99;
			Width = 22;
			Height = 22;
			Value = 1000;
			break;
		case EntityID.ItemID.LESSER_RESTORATION_POTION:
			UseSound = 3;
			HealMana = 50;
			HealLife = 50;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 20;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			IsPotion = true;
			Value = 2000;
			break;
		case EntityID.ItemID.RESTORATION_POTION:
			UseSound = 3;
			HealMana = 100;
			HealLife = 100;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 20;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			IsPotion = true;
			Value = 4000;
			break;
		case EntityID.ItemID.JUNGLE_HAT:
			Width = 18;
			Height = 18;
			Defense = 4;
			HeadSlot = 8;
			Rarity = 3;
			Value = 45000;
			break;
		case EntityID.ItemID.JUNGLE_SHIRT:
			Width = 18;
			Height = 18;
			Defense = 5;
			BodySlot = 8;
			Rarity = 3;
			Value = 30000;
			break;
		case EntityID.ItemID.JUNGLE_PANTS:
			Width = 18;
			Height = 18;
			Defense = 4;
			LegSlot = 8;
			Rarity = 3;
			Value = 30000;
			break;
		case EntityID.ItemID.MOLTEN_HELMET:
			Width = 18;
			Height = 18;
			Defense = 8;
			HeadSlot = 9;
			Rarity = 3;
			Value = 45000;
			break;
		case EntityID.ItemID.MOLTEN_BREASTPLATE:
			Width = 18;
			Height = 18;
			Defense = 9;
			BodySlot = 9;
			Rarity = 3;
			Value = 30000;
			break;
		case EntityID.ItemID.MOLTEN_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 8;
			LegSlot = 9;
			Rarity = 3;
			Value = 30000;
			break;
		case EntityID.ItemID.METEOR_SHOT:
			ShootSpeed = 3f;
			Shoot = 36;
			Damage = 9;
			Width = 8;
			Height = 8;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 14;
			Knockback = 1f;
			Value = 8;
			Rarity = 1;
			IsRanged = true;
			break;
		case EntityID.ItemID.STICKY_BOMB:
			UseStyle = 1;
			ShootSpeed = 5f;
			Shoot = 37;
			Width = 20;
			Height = 20;
			MaxStack = 50;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 25;
			UseTime = 25;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 500;
			Damage = 0;
			break;
		case EntityID.ItemID.BLACK_LENS:
			Width = 12;
			Height = 20;
			MaxStack = 99;
			Value = 5000;
			break;
		case EntityID.ItemID.SUNGLASSES:
			Width = 28;
			Height = 12;
			HeadSlot = 12;
			Rarity = 2;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.WIZARD_HAT:
			Width = 28;
			Height = 20;
			HeadSlot = 14;
			Rarity = 2;
			Value = 10000;
			Defense = 2;
			break;
		case EntityID.ItemID.TOP_HAT:
			Width = 18;
			Height = 18;
			HeadSlot = 15;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.TUXEDO_SHIRT:
			Width = 18;
			Height = 18;
			BodySlot = 10;
			Value = 5000;
			IsVanity = true;
			break;
		case EntityID.ItemID.TUXEDO_PANTS:
			Width = 18;
			Height = 18;
			LegSlot = 10;
			Value = 5000;
			IsVanity = true;
			break;
		case EntityID.ItemID.SUMMER_HAT:
			Width = 18;
			Height = 18;
			HeadSlot = 16;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.BUNNY_HOOD:
			Width = 18;
			Height = 18;
			HeadSlot = 17;
			Value = 20000;
			IsVanity = true;
			break;
		case EntityID.ItemID.PLUMBERS_HAT:
			Width = 18;
			Height = 12;
			HeadSlot = 18;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.PLUMBERS_SHIRT:
			Width = 18;
			Height = 18;
			BodySlot = 11;
			Value = 250000;
			IsVanity = true;
			break;
		case EntityID.ItemID.PLUMBERS_PANTS:
			Width = 18;
			Height = 18;
			LegSlot = 11;
			Value = 250000;
			IsVanity = true;
			break;
		case EntityID.ItemID.HEROS_HAT:
			Width = 18;
			Height = 12;
			HeadSlot = 19;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.HEROS_SHIRT:
			Width = 18;
			Height = 18;
			BodySlot = 12;
			Value = 5000;
			IsVanity = true;
			break;
		case EntityID.ItemID.HEROS_PANTS:
			Width = 18;
			Height = 18;
			LegSlot = 12;
			Value = 5000;
			IsVanity = true;
			break;
		case EntityID.ItemID.FISH_BOWL:
			Width = 18;
			Height = 18;
			HeadSlot = 20;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.ARCHAEOLOGISTS_HAT:
			Width = 18;
			Height = 12;
			HeadSlot = 21;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.ARCHAEOLOGISTS_JACKET:
			Width = 18;
			Height = 18;
			BodySlot = 13;
			Value = 5000;
			IsVanity = true;
			break;
		case EntityID.ItemID.ARCHAEOLOGISTS_PANTS:
			Width = 18;
			Height = 18;
			LegSlot = 13;
			Value = 5000;
			IsVanity = true;
			break;
		case EntityID.ItemID.BLACK_THREAD:
			MaxStack = 99;
			Width = 12;
			Height = 20;
			Value = 10000;
			break;
		case EntityID.ItemID.PURPLE_THREAD:
			MaxStack = 99;
			Width = 12;
			Height = 20;
			Value = 2000;
			break;
		case EntityID.ItemID.NINJA_HOOD:
			Width = 18;
			Height = 12;
			HeadSlot = 22;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.NINJA_SHIRT:
			Width = 18;
			Height = 18;
			BodySlot = 14;
			Value = 5000;
			IsVanity = true;
			break;
		case EntityID.ItemID.NINJA_PANTS:
			Width = 18;
			Height = 18;
			LegSlot = 14;
			Value = 5000;
			IsVanity = true;
			break;
		case EntityID.ItemID.LEATHER:
			Width = 18;
			Height = 20;
			MaxStack = 99;
			Value = 50;
			break;
		case EntityID.ItemID.RED_HAT:
			Width = 18;
			Height = 14;
			HeadSlot = 24;
			Value = 1000;
			IsVanity = true;
			break;
		case EntityID.ItemID.GOLDFISH:
			UseStyle = 2;
			UseSound = 2;
			CanUseTurn = false;
			UseAnimation = 17;
			UseTime = 17;
			Width = 20;
			Height = 10;
			MaxStack = 99;
			HealLife = 20;
			IsConsumable = true;
			Value = 1000;
			IsPotion = true;
			break;
		case EntityID.ItemID.ROBE:
			Width = 18;
			Height = 14;
			BodySlot = 15;
			Value = 2000;
			IsVanity = true;
			break;
		case EntityID.ItemID.ROBOT_HAT:
			Width = 18;
			Height = 18;
			HeadSlot = 25;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.GOLD_CROWN:
			Width = 18;
			Height = 18;
			HeadSlot = 26;
			Value = 10000;
			IsVanity = true;
			break;
		case EntityID.ItemID.HELLFIRE_ARROW:
			ShootSpeed = 6.5f;
			Shoot = 41;
			Damage = 10;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 8f;
			Value = 100;
			Rarity = 2;
			IsRanged = true;
			break;
		case EntityID.ItemID.VULCAN_BOLT:
			ShootSpeed = 6.6f;
			Shoot = 114;
			Damage = 12;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 8.2f;
			Value = 150;
			Rarity = 3;
			IsRanged = true;
			break;
		case EntityID.ItemID.SANDGUN:
			UseStyle = 5;
			UseAnimation = 16;
			UseTime = 16;
			AutoReuse = true;
			Width = 40;
			Height = 20;
			Shoot = 42;
			UseAmmo = 42;
			UseSound = 11;
			Damage = 30;
			ShootSpeed = 12f;
			NoMelee = true;
			Knockback = 5f;
			Value = 10000;
			Rarity = 2;
			IsRanged = true;
			break;
		case EntityID.ItemID.GUIDE_VOODOO_DOLL:
			IsAccessory = true;
			Width = 14;
			Height = 26;
			Value = 1000;
			break;
		case EntityID.ItemID.DIVING_HELMET:
			HeadSlot = 27;
			Defense = 2;
			Width = 20;
			Height = 20;
			Value = 1000;
			Rarity = 2;
			break;
		case EntityID.ItemID.FAMILIAR_SHIRT:
			BodySlot = 0;
			Width = 20;
			Height = 20;
			Value = 10000;
			if (UI.CurrentUI != null && UI.CurrentUI.ActivePlayer != null)
			{
				Colour = UI.CurrentUI.ActivePlayer.shirtColor;
			}
			break;
		case EntityID.ItemID.FAMILIAR_PANTS:
			LegSlot = 0;
			Width = 20;
			Height = 20;
			Value = 10000;
			if (UI.CurrentUI != null && UI.CurrentUI.ActivePlayer != null)
			{
				Colour = UI.CurrentUI.ActivePlayer.pantsColor;
			}
			break;
		case EntityID.ItemID.FAMILIAR_WIG:
			HeadSlot = 0;
			Width = 20;
			Height = 20;
			Value = 10000;
			if (UI.CurrentUI != null && UI.CurrentUI.ActivePlayer != null)
			{
				Colour = UI.CurrentUI.ActivePlayer.hairColor;
			}
			break;
		case EntityID.ItemID.DEMON_SCYTHE:
			Mana = 14;
			Damage = 35;
			UseStyle = 5;
			ShootSpeed = 0.2f;
			Shoot = 45;
			Width = 26;
			Height = 28;
			UseSound = 8;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Knockback = 5f;
			Scale = 0.9f;
			Value = 10000;
			IsMagic = true;
			break;
		case EntityID.ItemID.NIGHTS_EDGE:
			UseStyle = 1;
			UseAnimation = 27;
			UseTime = 27;
			Knockback = 4.5f;
			Width = 40;
			Height = 40;
			Damage = 42;
			Scale = 1.15f;
			UseSound = 1;
			Rarity = 3;
			Value = 27000;
			IsMelee = true;
			break;
		case EntityID.ItemID.DARK_LANCE:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 25;
			ShootSpeed = 5f;
			Knockback = 4f;
			Width = 40;
			Height = 40;
			Damage = 27;
			Scale = 1.1f;
			UseSound = 1;
			Shoot = 46;
			Rarity = 3;
			Value = 27000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			break;
		case EntityID.ItemID.CORAL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 81;
			Width = 20;
			Height = 22;
			Value = 400;
			break;
		case EntityID.ItemID.CACTUS:
			MaxStack = 250;
			Width = 12;
			Height = 12;
			Value = 10;
			break;
		case EntityID.ItemID.TRIDENT:
			UseStyle = 5;
			UseAnimation = 31;
			UseTime = 31;
			ShootSpeed = 4f;
			Knockback = 5f;
			Width = 40;
			Height = 40;
			Damage = 10;
			Scale = 1.1f;
			UseSound = 1;
			Shoot = 47;
			Rarity = 1;
			Value = 10000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			break;
		case EntityID.ItemID.SILVER_BULLET:
			ShootSpeed = 4.5f;
			Shoot = 14;
			Damage = 9;
			Width = 8;
			Height = 8;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 14;
			Knockback = 3f;
			Value = 15;
			IsRanged = true;
			break;
		case EntityID.ItemID.THROWING_KNIFE:
			UseStyle = 1;
			ShootSpeed = 10f;
			Shoot = 48;
			Damage = 12;
			Width = 18;
			Height = 20;
			MaxStack = 250;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 50;
			Knockback = 2f;
			IsRanged = true;
			break;
		case EntityID.ItemID.SPEAR:
			UseStyle = 5;
			UseAnimation = 31;
			UseTime = 31;
			ShootSpeed = 3.7f;
			Knockback = 6.5f;
			Width = 32;
			Height = 32;
			Damage = 8;
			Scale = 1f;
			UseSound = 1;
			Shoot = 49;
			Value = 1000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			break;
		case EntityID.ItemID.BLOWPIPE:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 45;
			UseTime = 45;
			Width = 38;
			Height = 6;
			Shoot = 10;
			UseAmmo = 51;
			UseSound = 5;
			Damage = 9;
			ShootSpeed = 11f;
			NoMelee = true;
			Value = 10000;
			Knockback = 4f;
			IsRanged = true;
			break;
		case EntityID.ItemID.GLOWSTICK:
			UseStyle = 1;
			ShootSpeed = 6f;
			Shoot = 50;
			Width = 12;
			Height = 12;
			MaxStack = 99;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoMelee = true;
			Value = 10;
			HoldStyle = 1;
			break;
		case EntityID.ItemID.SEED:
			Shoot = 51;
			Width = 8;
			Height = 8;
			MaxStack = 250;
			Ammo = 51;
			break;
		case EntityID.ItemID.WOODEN_BOOMERANG:
			NoMelee = true;
			UseStyle = 1;
			ShootSpeed = 6.5f;
			Shoot = 52;
			Damage = 7;
			Knockback = 5f;
			Width = 14;
			Height = 28;
			UseSound = 1;
			UseAnimation = 16;
			UseTime = 16;
			NoUseGraphic = true;
			Value = 5000;
			IsMelee = true;
			break;
		case EntityID.ItemID.AGLET:
			Width = 24;
			Height = 8;
			IsAccessory = true;
			Value = 5000;
			break;
		case EntityID.ItemID.STICKY_GLOWSTICK:
			UseStyle = 1;
			ShootSpeed = 6f;
			Shoot = 53;
			Width = 12;
			Height = 12;
			MaxStack = 99;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoMelee = true;
			Value = 20;
			HoldStyle = 1;
			break;
		case EntityID.ItemID.POISONED_KNIFE:
			UseStyle = 1;
			ShootSpeed = 11f;
			Shoot = 54;
			Damage = 13;
			Width = 18;
			Height = 20;
			MaxStack = 250;
			IsConsumable = true;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 60;
			Knockback = 2f;
			IsRanged = true;
			break;
		case EntityID.ItemID.OBSIDIAN_SKIN_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 1;
			BuffTime = 14400;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.REGENERATION_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 2;
			BuffTime = 18000;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.SWIFTNESS_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 3;
			BuffTime = 14400;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.GILLS_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 4;
			BuffTime = 7200;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.IRONSKIN_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 5;
			BuffTime = 18000;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.MANA_REGENERATION_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 6;
			BuffTime = 7200;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.MAGIC_POWER_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 7;
			BuffTime = 7200;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.FEATHERFALL_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 8;
			BuffTime = 18000;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.SPELUNKER_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 9;
			BuffTime = 18000;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.INVISIBILITY_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 10;
			BuffTime = 7200;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.SHINE_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 11;
			BuffTime = 18000;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.NIGHT_OWL_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 12;
			BuffTime = 14400;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.BATTLE_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 13;
			BuffTime = 25200;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.THORNS_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 14;
			BuffTime = 7200;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.WATER_WALKING_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 15;
			BuffTime = 18000;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.ARCHERY_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 16;
			BuffTime = 14400;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.HUNTER_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 17;
			BuffTime = 18000;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.GRAVITATION_POTION:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			BuffType = 18;
			BuffTime = 10800;
			Value = 1000;
			Rarity = 1;
			break;
		case EntityID.ItemID.GOLD_CHEST:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 21;
			PlaceStyle = 1;
			Width = 26;
			Height = 22;
			Value = 5000;
			break;
		case EntityID.ItemID.DAYBLOOM_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 82;
			PlaceStyle = 0;
			Width = 12;
			Height = 14;
			Value = 80;
			break;
		case EntityID.ItemID.MOONGLOW_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 82;
			PlaceStyle = 1;
			Width = 12;
			Height = 14;
			Value = 80;
			break;
		case EntityID.ItemID.BLINKROOT_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 82;
			PlaceStyle = 2;
			Width = 12;
			Height = 14;
			Value = 80;
			break;
		case EntityID.ItemID.DEATHWEED_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 82;
			PlaceStyle = 3;
			Width = 12;
			Height = 14;
			Value = 80;
			break;
		case EntityID.ItemID.WATERLEAF_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 82;
			PlaceStyle = 4;
			Width = 12;
			Height = 14;
			Value = 80;
			break;
		case EntityID.ItemID.FIREBLOSSOM_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 82;
			PlaceStyle = 5;
			Width = 12;
			Height = 14;
			Value = 80;
			break;
		case EntityID.ItemID.DAYBLOOM:
			MaxStack = 99;
			Width = 12;
			Height = 14;
			Value = 100;
			break;
		case EntityID.ItemID.MOONGLOW:
			MaxStack = 99;
			Width = 12;
			Height = 14;
			Value = 100;
			break;
		case EntityID.ItemID.BLINKROOT:
			MaxStack = 99;
			Width = 12;
			Height = 14;
			Value = 100;
			break;
		case EntityID.ItemID.DEATHWEED:
			MaxStack = 99;
			Width = 12;
			Height = 14;
			Value = 100;
			break;
		case EntityID.ItemID.WATERLEAF:
			MaxStack = 99;
			Width = 12;
			Height = 14;
			Value = 100;
			break;
		case EntityID.ItemID.FIREBLOSSOM:
			MaxStack = 99;
			Width = 12;
			Height = 14;
			Value = 100;
			break;
		case EntityID.ItemID.SHARK_FIN:
			MaxStack = 99;
			Width = 16;
			Height = 14;
			Value = 200;
			break;
		case EntityID.ItemID.FEATHER:
			MaxStack = 99;
			Width = 16;
			Height = 14;
			Value = 50;
			break;
		case EntityID.ItemID.TOMBSTONE:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 85;
			Width = 20;
			Height = 20;
			break;
		case EntityID.ItemID.MIME_MASK:
			HeadSlot = 28;
			Width = 20;
			Height = 20;
			Value = 20000;
			break;
		case EntityID.ItemID.ANTLION_MANDIBLE:
			Width = 10;
			Height = 20;
			MaxStack = 99;
			Value = 50;
			break;
		case EntityID.ItemID.ILLEGAL_GUN_PARTS:
			Width = 10;
			Height = 20;
			MaxStack = 99;
			Value = 750000;
			break;
		case EntityID.ItemID.THE_DOCTORS_SHIRT:
			Width = 18;
			Height = 18;
			BodySlot = 16;
			Value = 200000;
			IsVanity = true;
			break;
		case EntityID.ItemID.THE_DOCTORS_PANTS:
			Width = 18;
			Height = 18;
			LegSlot = 15;
			Value = 200000;
			IsVanity = true;
			break;
		case EntityID.ItemID.GOLDEN_KEY:
			Width = 14;
			Height = 20;
			MaxStack = 99;
			break;
		case EntityID.ItemID.SHADOW_CHEST:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 21;
			PlaceStyle = 3;
			Width = 26;
			Height = 22;
			Value = 5000;
			break;
		case EntityID.ItemID.SHADOW_KEY:
			Width = 14;
			Height = 20;
			MaxStack = 1;
			Value = 75000;
			break;
		case EntityID.ItemID.OBSIDIAN_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 20;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.JUNGLE_SPORES:
			Width = 18;
			Height = 16;
			MaxStack = 99;
			Value = 100;
			break;
		case EntityID.ItemID.LOOM:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 86;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.PIANO:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 87;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.DRESSER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 88;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.BENCH:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 89;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.BATHTUB:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 90;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.RED_BANNER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 91;
			PlaceStyle = 0;
			Width = 10;
			Height = 24;
			Value = 500;
			break;
		case EntityID.ItemID.GREEN_BANNER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 91;
			PlaceStyle = 1;
			Width = 10;
			Height = 24;
			Value = 500;
			break;
		case EntityID.ItemID.BLUE_BANNER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 91;
			PlaceStyle = 2;
			Width = 10;
			Height = 24;
			Value = 500;
			break;
		case EntityID.ItemID.YELLOW_BANNER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 91;
			PlaceStyle = 3;
			Width = 10;
			Height = 24;
			Value = 500;
			break;
		case EntityID.ItemID.LAMP_POST:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 92;
			Width = 10;
			Height = 24;
			Value = 500;
			break;
		case EntityID.ItemID.TIKI_TORCH:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 93;
			Width = 10;
			Height = 24;
			Value = 500;
			break;
		case EntityID.ItemID.BARREL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 21;
			PlaceStyle = 5;
			Width = 20;
			Height = 20;
			Value = 500;
			break;
		case EntityID.ItemID.CHINESE_LANTERN:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 95;
			Width = 20;
			Height = 20;
			Value = 500;
			break;
		case EntityID.ItemID.COOKING_POT:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 96;
			Width = 20;
			Height = 20;
			Value = 500;
			break;
		case EntityID.ItemID.SAFE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 97;
			Width = 20;
			Height = 20;
			Value = 500000;
			break;
		case EntityID.ItemID.SKULL_LANTERN:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 98;
			Width = 20;
			Height = 20;
			Value = 500;
			break;
		case EntityID.ItemID.TRASH_CAN:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 21;
			PlaceStyle = 6;
			Width = 20;
			Height = 20;
			Value = 1000;
			break;
		case EntityID.ItemID.CANDELABRA:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 100;
			Width = 20;
			Height = 20;
			Value = 1500;
			break;
		case EntityID.ItemID.PINK_VASE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 13;
			PlaceStyle = 3;
			Width = 16;
			Height = 24;
			Value = 70;
			break;
		case EntityID.ItemID.MUG:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 13;
			PlaceStyle = 4;
			Width = 16;
			Height = 24;
			Value = 20;
			break;
		case EntityID.ItemID.KEG:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 94;
			Width = 24;
			Height = 24;
			Value = 600;
			break;
		case EntityID.ItemID.ALE:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 10;
			Height = 10;
			BuffType = 25;
			BuffTime = 7200;
			Value = 100;
			break;
		case EntityID.ItemID.BOOKCASE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 101;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.THRONE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 102;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.BOWL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 103;
			Width = 16;
			Height = 24;
			Value = 20;
			break;
		case EntityID.ItemID.BOWL_OF_SOUP:
			UseSound = 3;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 10;
			Height = 10;
			BuffType = 26;
			BuffTime = 36000;
			Rarity = 1;
			Value = 1000;
			break;
		case EntityID.ItemID.TOILET:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 15;
			PlaceStyle = 1;
			Width = 12;
			Height = 30;
			Value = 150;
			break;
		case EntityID.ItemID.GRANDFATHER_CLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 104;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.GOBLIN_BATTLE_STANDARD:
			UseStyle = 4;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			Width = 28;
			Height = 28;
			break;
		case EntityID.ItemID.TATTERED_CLOTH:
			MaxStack = 99;
			Width = 24;
			Height = 24;
			Value = 30;
			break;
		case EntityID.ItemID.SAWMILL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 106;
			Width = 20;
			Height = 20;
			Value = 300;
			break;
		case EntityID.ItemID.COBALT_ORE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 107;
			Width = 12;
			Height = 12;
			Value = 3500;
			Rarity = 3;
			break;
		case EntityID.ItemID.MYTHRIL_ORE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 108;
			Width = 12;
			Height = 12;
			Value = 5500;
			Rarity = 3;
			break;
		case EntityID.ItemID.ADAMANTITE_ORE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 111;
			Width = 12;
			Height = 12;
			Value = 7500;
			Rarity = 3;
			break;
		case EntityID.ItemID.PWNHAMMER:
			CanUseTurn = true;
			AutoReuse = true;
			UseStyle = 1;
			UseAnimation = 27;
			UseTime = 14;
			HammerPower = 80;
			Width = 24;
			Height = 28;
			Damage = 26;
			Knockback = 7.5f;
			Scale = 1.2f;
			UseSound = 1;
			Rarity = 4;
			Value = 39000;
			IsMelee = true;
			break;
		case EntityID.ItemID.EXCALIBUR:
			AutoReuse = true;
			UseStyle = 1;
			UseAnimation = 25;
			UseTime = 25;
			Knockback = 4.5f;
			Width = 40;
			Height = 40;
			Damage = 47;
			Scale = 1.15f;
			UseSound = 1;
			Rarity = 5;
			Value = 230000;
			IsMelee = true;
			break;
		case EntityID.ItemID.TIZONA:
			AutoReuse = true;
			UseStyle = 1;
			UseAnimation = 25;
			UseTime = 25;
			Knockback = 4.6f;
			Width = 40;
			Height = 40;
			Damage = 55;
			Scale = 1.15f;
			UseSound = 1;
			Rarity = 5;
			Value = 300000;
			IsMelee = true;
			break;
		case EntityID.ItemID.HALLOWED_SEEDS:
			CanUseTurn = true;
			UseStyle = 1;
			UseAnimation = 15;
			UseTime = 10;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 109;
			Width = 14;
			Height = 14;
			Value = 2000;
			Rarity = 3;
			break;
		case EntityID.ItemID.EBONSAND_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 112;
			Width = 12;
			Height = 12;
			Ammo = 42;
			break;
		case EntityID.ItemID.COBALT_HAT:
			Width = 18;
			Height = 18;
			Defense = 2;
			HeadSlot = 29;
			Rarity = 4;
			Value = 75000;
			break;
		case EntityID.ItemID.COBALT_HELMET:
			Width = 18;
			Height = 18;
			Defense = 11;
			HeadSlot = 30;
			Rarity = 4;
			Value = 75000;
			break;
		case EntityID.ItemID.COBALT_MASK:
			Width = 18;
			Height = 18;
			Defense = 4;
			HeadSlot = 31;
			Rarity = 4;
			Value = 75000;
			break;
		case EntityID.ItemID.COBALT_BREASTPLATE:
			Width = 18;
			Height = 18;
			Defense = 8;
			BodySlot = 17;
			Rarity = 4;
			Value = 60000;
			break;
		case EntityID.ItemID.COBALT_LEGGINGS:
			Width = 18;
			Height = 18;
			Defense = 7;
			LegSlot = 16;
			Rarity = 4;
			Value = 45000;
			break;
		case EntityID.ItemID.MYTHRIL_HOOD:
			Width = 18;
			Height = 18;
			Defense = 3;
			HeadSlot = 32;
			Rarity = 4;
			Value = 112500;
			break;
		case EntityID.ItemID.MYTHRIL_HELMET:
			Width = 18;
			Height = 18;
			Defense = 16;
			HeadSlot = 33;
			Rarity = 4;
			Value = 112500;
			break;
		case EntityID.ItemID.MYTHRIL_HAT:
			Width = 18;
			Height = 18;
			Defense = 6;
			HeadSlot = 34;
			Rarity = 4;
			Value = 112500;
			break;
		case EntityID.ItemID.MYTHRIL_CHAINMAIL:
			Width = 18;
			Height = 18;
			Defense = 12;
			BodySlot = 18;
			Rarity = 4;
			Value = 90000;
			break;
		case EntityID.ItemID.MYTHRIL_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 9;
			LegSlot = 17;
			Rarity = 4;
			Value = 67500;
			break;
		case EntityID.ItemID.COBALT_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Value = 10500;
			Rarity = 3;
			break;
		case EntityID.ItemID.MYTHRIL_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Value = 22000;
			Rarity = 3;
			break;
		case EntityID.ItemID.COBALT_CHAINSAW:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 8;
			ShootSpeed = 40f;
			Knockback = 2.75f;
			Width = 20;
			Height = 12;
			Damage = 23;
			AxePower = 14;
			UseSound = 23;
			Shoot = 57;
			Rarity = 4;
			Value = 54000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.MYTHRIL_CHAINSAW:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 8;
			ShootSpeed = 40f;
			Knockback = 3f;
			Width = 20;
			Height = 12;
			Damage = 29;
			AxePower = 17;
			UseSound = 23;
			Shoot = 58;
			Rarity = 4;
			Value = 81000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.COBALT_DRILL:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 13;
			ShootSpeed = 32f;
			Knockback = 0f;
			Width = 20;
			Height = 12;
			Damage = 10;
			PickPower = 110;
			UseSound = 23;
			Shoot = 59;
			Rarity = 4;
			Value = 54000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.MYTHRIL_DRILL:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 10;
			ShootSpeed = 32f;
			Knockback = 0f;
			Width = 20;
			Height = 12;
			Damage = 15;
			PickPower = 150;
			UseSound = 23;
			Shoot = 60;
			Rarity = 4;
			Value = 81000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.ADAMANTITE_CHAINSAW:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 6;
			ShootSpeed = 40f;
			Knockback = 4.5f;
			Width = 20;
			Height = 12;
			Damage = 33;
			AxePower = 20;
			UseSound = 23;
			Shoot = 61;
			Rarity = 4;
			Value = 108000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.ADAMANTITE_DRILL:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 7;
			ShootSpeed = 32f;
			Knockback = 0f;
			Width = 20;
			Height = 12;
			Damage = 20;
			PickPower = 180;
			UseSound = 23;
			Shoot = 62;
			Rarity = 4;
			Value = 108000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.DAO_OF_POW:
			NoMelee = true;
			UseStyle = 5;
			UseAnimation = 45;
			UseTime = 45;
			Knockback = 7f;
			Width = 30;
			Height = 10;
			Damage = 49;
			Scale = 1.1f;
			NoUseGraphic = true;
			Shoot = 63;
			ShootSpeed = 15f;
			UseSound = 1;
			Rarity = 5;
			Value = 144000;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.MYTHRIL_HALBERD:
			UseStyle = 5;
			UseAnimation = 26;
			UseTime = 26;
			ShootSpeed = 4.5f;
			Knockback = 5f;
			Width = 40;
			Height = 40;
			Damage = 35;
			Scale = 1.1f;
			UseSound = 1;
			Shoot = 64;
			Rarity = 4;
			Value = 67500;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			break;
		case EntityID.ItemID.ADAMANTITE_BAR:
			Width = 20;
			Height = 20;
			MaxStack = 99;
			Value = 37500;
			Rarity = 3;
			break;
		case EntityID.ItemID.GLASS_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 21;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.COMPASS:
			Width = 24;
			Height = 28;
			Rarity = 3;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.DIVING_GEAR:
			Width = 24;
			Height = 28;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.GPS:
			Width = 24;
			Height = 28;
			Rarity = 4;
			Value = 150000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.OBSIDIAN_HORSESHOE:
			Width = 24;
			Height = 28;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.OBSIDIAN_SHIELD:
			Width = 24;
			Height = 28;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			Defense = 2;
			break;
		case EntityID.ItemID.TINKERERS_WORKSHOP:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 114;
			Width = 26;
			Height = 20;
			Value = 100000;
			break;
		case EntityID.ItemID.CLOUD_IN_A_BALLOON:
			Width = 14;
			Height = 28;
			Rarity = 4;
			Value = 150000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.ADAMANTITE_HEADGEAR:
			Width = 18;
			Height = 18;
			Defense = 4;
			HeadSlot = 35;
			Rarity = 4;
			Value = 150000;
			break;
		case EntityID.ItemID.ADAMANTITE_HELMET:
			Width = 18;
			Height = 18;
			Defense = 22;
			HeadSlot = 36;
			Rarity = 4;
			Value = 150000;
			break;
		case EntityID.ItemID.ADAMANTITE_MASK:
			Width = 18;
			Height = 18;
			Defense = 8;
			HeadSlot = 37;
			Rarity = 4;
			Value = 150000;
			break;
		case EntityID.ItemID.ADAMANTITE_BREASTPLATE:
			Width = 18;
			Height = 18;
			Defense = 14;
			BodySlot = 19;
			Rarity = 4;
			Value = 120000;
			break;
		case EntityID.ItemID.ADAMANTITE_LEGGINGS:
			Width = 18;
			Height = 18;
			Defense = 10;
			LegSlot = 18;
			Rarity = 4;
			Value = 90000;
			break;
		case EntityID.ItemID.SPECTRE_BOOTS:
			Width = 28;
			Height = 24;
			IsAccessory = true;
			Rarity = 4;
			Value = 100000;
			break;
		case EntityID.ItemID.ADAMANTITE_GLAIVE:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 25;
			ShootSpeed = 5f;
			Knockback = 6f;
			Width = 40;
			Height = 40;
			Damage = 38;
			Scale = 1.1f;
			UseSound = 1;
			Shoot = 66;
			Rarity = 4;
			Value = 90000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			break;
		case EntityID.ItemID.TOOLBELT:
			Width = 28;
			Height = 24;
			IsAccessory = true;
			Rarity = 3;
			Value = 100000;
			break;
		case EntityID.ItemID.PEARLSAND_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 116;
			Width = 12;
			Height = 12;
			Ammo = 42;
			break;
		case EntityID.ItemID.PEARLSTONE_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 117;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.MINING_SHIRT:
			Width = 18;
			Height = 18;
			Defense = 1;
			BodySlot = 20;
			Value = 5000;
			Rarity = 1;
			break;
		case EntityID.ItemID.MINING_PANTS:
			Width = 18;
			Height = 18;
			Defense = 1;
			LegSlot = 19;
			Value = 5000;
			Rarity = 1;
			break;
		case EntityID.ItemID.PEARLSTONE_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 118;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.IRIDESCENT_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 119;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.MUDSTONE_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 120;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.COBALT_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 121;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.MYTHRIL_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 122;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.PEARLSTONE_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 22;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.IRIDESCENT_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 23;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.MUDSTONE_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 24;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.COBALT_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 25;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.MYTHRIL_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 26;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.HOLY_WATER:
			UseStyle = 1;
			ShootSpeed = 9f;
			Rarity = 3;
			Damage = 20;
			Shoot = 69;
			Width = 18;
			Height = 20;
			MaxStack = 250;
			IsConsumable = true;
			Knockback = 3f;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 200;
			break;
		case EntityID.ItemID.UNHOLY_WATER:
			UseStyle = 1;
			ShootSpeed = 9f;
			Rarity = 3;
			Damage = 20;
			Shoot = 70;
			Width = 18;
			Height = 20;
			MaxStack = 250;
			IsConsumable = true;
			Knockback = 3f;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 200;
			break;
		case EntityID.ItemID.SILT_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 123;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.FAIRY_BELL:
			Mana = 40;
			Channelling = true;
			Damage = 0;
			UseStyle = 1;
			Shoot = 72;
			Width = 24;
			Height = 24;
			UseSound = 25;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 5;
			NoMelee = true;
			Value = (Value = 250000);
			BuffType = 27;
			BuffTime = 18000;
			break;
		case EntityID.ItemID.BREAKER_BLADE:
			UseStyle = 1;
			UseAnimation = 30;
			Knockback = 8f;
			Width = 60;
			Height = 70;
			Damage = 39;
			Scale = 1.05f;
			UseSound = 1;
			Rarity = 4;
			Value = 150000;
			IsMelee = true;
			break;
		case EntityID.ItemID.BLUE_TORCH:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			PlaceStyle = 1;
			Width = 10;
			Height = 12;
			Value = 200;
			break;
		case EntityID.ItemID.RED_TORCH:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			PlaceStyle = 2;
			Width = 10;
			Height = 12;
			Value = 200;
			break;
		case EntityID.ItemID.GREEN_TORCH:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			PlaceStyle = 3;
			Width = 10;
			Height = 12;
			Value = 200;
			break;
		case EntityID.ItemID.PURPLE_TORCH:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			PlaceStyle = 4;
			Width = 10;
			Height = 12;
			Value = 200;
			break;
		case EntityID.ItemID.WHITE_TORCH:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			PlaceStyle = 5;
			Width = 10;
			Height = 12;
			Value = 500;
			break;
		case EntityID.ItemID.YELLOW_TORCH:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			PlaceStyle = 6;
			Width = 10;
			Height = 12;
			Value = 200;
			break;
		case EntityID.ItemID.DEMON_TORCH:
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			PlaceStyle = 7;
			Width = 10;
			Height = 12;
			Value = 300;
			break;
		case EntityID.ItemID.CLOCKWORK_ASSAULT_RIFLE:
			AutoReuse = true;
			UseStyle = 5;
			UseAnimation = 12;
			UseTime = 4;
			ReuseDelay = 14;
			Width = 50;
			Height = 18;
			Shoot = 10;
			UseAmmo = 14;
			UseSound = 31;
			Damage = 19;
			ShootSpeed = 7.75f;
			NoMelee = true;
			Value = 150000;
			Rarity = 4;
			IsRanged = true;
			break;
		case EntityID.ItemID.COBALT_REPEATER:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 25;
			UseTime = 25;
			Width = 50;
			Height = 18;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 30;
			ShootSpeed = 9f;
			NoMelee = true;
			Value = 60000;
			IsRanged = true;
			Rarity = 4;
			Knockback = 1.5f;
			break;
		case EntityID.ItemID.MYTHRIL_REPEATER:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 23;
			UseTime = 23;
			Width = 50;
			Height = 18;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 34;
			ShootSpeed = 9.5f;
			NoMelee = true;
			Value = 90000;
			IsRanged = true;
			Rarity = 4;
			Knockback = 2f;
			break;
		case EntityID.ItemID.DUAL_HOOK:
			NoUseGraphic = true;
			Damage = 0;
			Knockback = 7f;
			UseStyle = 5;
			ShootSpeed = 14f;
			Shoot = 73;
			Width = 18;
			Height = 28;
			UseSound = 1;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 4;
			NoMelee = true;
			Value = 200000;
			break;
		case EntityID.ItemID.STAR_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 2;
			break;
		case EntityID.ItemID.SWORD_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 3;
			break;
		case EntityID.ItemID.SLIME_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 4;
			break;
		case EntityID.ItemID.GOBLIN_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 5;
			break;
		case EntityID.ItemID.SHIELD_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 6;
			break;
		case EntityID.ItemID.BAT_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 7;
			break;
		case EntityID.ItemID.FISH_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 8;
			break;
		case EntityID.ItemID.BUNNY_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 9;
			break;
		case EntityID.ItemID.SKELETON_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 10;
			break;
		case EntityID.ItemID.REAPER_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 11;
			break;
		case EntityID.ItemID.WOMAN_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 12;
			break;
		case EntityID.ItemID.IMP_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 13;
			break;
		case EntityID.ItemID.GARGOYLE_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 14;
			break;
		case EntityID.ItemID.GLOOM_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 15;
			break;
		case EntityID.ItemID.HORNET_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 16;
			break;
		case EntityID.ItemID.BOMB_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 17;
			break;
		case EntityID.ItemID.CRAB_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 18;
			break;
		case EntityID.ItemID.HAMMER_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 19;
			break;
		case EntityID.ItemID.POTION_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 20;
			break;
		case EntityID.ItemID.SPEAR_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 21;
			break;
		case EntityID.ItemID.CROSS_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 22;
			break;
		case EntityID.ItemID.JELLYFISH_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 23;
			break;
		case EntityID.ItemID.BOW_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 24;
			break;
		case EntityID.ItemID.BOOMERANG_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 25;
			break;
		case EntityID.ItemID.BOOT_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 26;
			break;
		case EntityID.ItemID.CHEST_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 27;
			break;
		case EntityID.ItemID.BIRD_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 28;
			break;
		case EntityID.ItemID.AXE_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 29;
			break;
		case EntityID.ItemID.CORRUPT_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 30;
			break;
		case EntityID.ItemID.TREE_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 31;
			break;
		case EntityID.ItemID.ANVIL_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 32;
			break;
		case EntityID.ItemID.PICKAXE_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 33;
			break;
		case EntityID.ItemID.MUSHROOM_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 34;
			break;
		case EntityID.ItemID.EYEBALL_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 35;
			break;
		case EntityID.ItemID.PILLAR_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 36;
			break;
		case EntityID.ItemID.HEART_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 37;
			break;
		case EntityID.ItemID.POT_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 38;
			break;
		case EntityID.ItemID.SUNFLOWER_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 39;
			break;
		case EntityID.ItemID.KING_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 40;
			break;
		case EntityID.ItemID.QUEEN_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 41;
			break;
		case EntityID.ItemID.PIRANHA_STATUE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 105;
			Width = 20;
			Height = 20;
			Value = 300;
			PlaceStyle = 42;
			break;
		case EntityID.ItemID.PLANKED_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 7;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 27;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.WOODEN_BEAM:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 124;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.ADAMANTITE_REPEATER:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 20;
			UseTime = 20;
			Width = 50;
			Height = 18;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 37;
			ShootSpeed = 10f;
			NoMelee = true;
			Value = 120000;
			IsRanged = true;
			Rarity = 4;
			Knockback = 2.5f;
			break;
		case EntityID.ItemID.ADAMANTITE_SWORD:
			UseStyle = 1;
			UseAnimation = 27;
			UseTime = 27;
			Knockback = 6f;
			Width = 40;
			Height = 40;
			Damage = 44;
			Scale = 1.2f;
			UseSound = 1;
			Rarity = 4;
			Value = 138000;
			IsMelee = true;
			break;
		case EntityID.ItemID.COBALT_SWORD:
			CanUseTurn = true;
			AutoReuse = true;
			UseStyle = 1;
			UseAnimation = 23;
			UseTime = 23;
			Knockback = 3.85f;
			Width = 40;
			Height = 40;
			Damage = 34;
			Scale = 1.1f;
			UseSound = 1;
			Rarity = 4;
			Value = 69000;
			IsMelee = true;
			break;
		case EntityID.ItemID.MYTHRIL_SWORD:
			UseStyle = 1;
			UseAnimation = 26;
			UseTime = 26;
			Knockback = 6f;
			Width = 40;
			Height = 40;
			Damage = 39;
			Scale = 1.15f;
			UseSound = 1;
			Rarity = 4;
			Value = 103500;
			IsMelee = true;
			break;
		case EntityID.ItemID.MOON_CHARM:
			Rarity = 4;
			Width = 24;
			Height = 28;
			IsAccessory = true;
			Value = 150000;
			break;
		case EntityID.ItemID.RULER:
			Width = 10;
			Height = 26;
			IsAccessory = true;
			Value = 10000;
			Rarity = 1;
			break;
		case EntityID.ItemID.CRYSTAL_BALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 125;
			Width = 22;
			Height = 22;
			Value = 100000;
			Rarity = 3;
			break;
		case EntityID.ItemID.DISCO_BALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 126;
			Width = 22;
			Height = 26;
			Value = 10000;
			break;
		case EntityID.ItemID.SORCERER_EMBLEM:
			Width = 24;
			Height = 24;
			IsAccessory = true;
			Value = 100000;
			Rarity = 4;
			break;
		case EntityID.ItemID.RANGER_EMBLEM:
			Width = 24;
			Height = 24;
			IsAccessory = true;
			Value = 100000;
			break;
		case EntityID.ItemID.WARRIOR_EMBLEM:
			Width = 24;
			Height = 24;
			IsAccessory = true;
			Value = 100000;
			Rarity = 4;
			break;
		case EntityID.ItemID.DEMON_WINGS:
		case EntityID.ItemID.ANGEL_WINGS:
			Width = 24;
			Height = 8;
			IsAccessory = true;
			Value = 400000;
			Rarity = 5;
			break;
		case EntityID.ItemID.MAGICAL_HARP:
			Rarity = 5;
			UseStyle = 5;
			UseAnimation = 12;
			UseTime = 12;
			Width = 12;
			Height = 28;
			Shoot = 76;
			HoldStyle = 3;
			AutoReuse = true;
			Damage = 30;
			ShootSpeed = 4.5f;
			NoMelee = true;
			Value = 200000;
			Mana = 4;
			IsMagic = true;
			break;
		case EntityID.ItemID.RAINBOW_ROD:
			Rarity = 5;
			Mana = 10;
			Channelling = true;
			Damage = 53;
			UseStyle = 1;
			ShootSpeed = 6f;
			Shoot = 79;
			Width = 26;
			Height = 28;
			UseSound = 28;
			UseAnimation = 15;
			UseTime = 15;
			NoMelee = true;
			Knockback = 5f;
			TileBoost = 64;
			Value = 200000;
			IsMagic = true;
			break;
		case EntityID.ItemID.ICE_ROD:
			Rarity = 4;
			Mana = 7;
			Damage = 26;
			UseStyle = 1;
			ShootSpeed = 12f;
			Shoot = 80;
			Width = 26;
			Height = 28;
			UseSound = 28;
			UseAnimation = 17;
			UseTime = 17;
			Rarity = 4;
			AutoReuse = true;
			NoMelee = true;
			Knockback = 0f;
			Value = 1000000;
			IsMagic = true;
			Knockback = 2f;
			break;
		case EntityID.ItemID.NEPTUNES_SHELL:
			Width = 24;
			Height = 28;
			IsAccessory = true;
			Value = 150000;
			Rarity = 5;
			break;
		case EntityID.ItemID.MANNEQUIN:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 128;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.GREATER_HEALING_POTION:
			UseSound = 3;
			HealLife = 150;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 30;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			Rarity = 3;
			IsPotion = true;
			Value = 5000;
			break;
		case EntityID.ItemID.GREATER_MANA_POTION:
			UseSound = 3;
			HealMana = 200;
			UseStyle = 2;
			CanUseTurn = true;
			UseAnimation = 17;
			UseTime = 17;
			MaxStack = 99;
			IsConsumable = true;
			Width = 14;
			Height = 24;
			Rarity = 3;
			Value = 500;
			break;
		case EntityID.ItemID.PIXIE_DUST:
			Width = 16;
			Height = 14;
			MaxStack = 99;
			Value = 500;
			Rarity = 1;
			break;
		case EntityID.ItemID.CRYSTAL_SHARD:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 129;
			Width = 24;
			Height = 24;
			Value = 8000;
			Rarity = 1;
			break;
		case EntityID.ItemID.CLOWN_HAT:
			Width = 18;
			Height = 18;
			HeadSlot = 40;
			Value = 20000;
			IsVanity = true;
			Rarity = 2;
			break;
		case EntityID.ItemID.CLOWN_SHIRT:
			Width = 18;
			Height = 18;
			BodySlot = 23;
			Value = 10000;
			IsVanity = true;
			Rarity = 2;
			break;
		case EntityID.ItemID.CLOWN_PANTS:
			Width = 18;
			Height = 18;
			LegSlot = 22;
			Value = 10000;
			IsVanity = true;
			Rarity = 2;
			break;
		case EntityID.ItemID.FLAMETHROWER:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 30;
			UseTime = 6;
			Width = 50;
			Height = 18;
			Shoot = 85;
			UseAmmo = 23;
			UseSound = 34;
			Damage = 27;
			Knockback = 0.3f;
			ShootSpeed = 7f;
			NoMelee = true;
			Value = 500000;
			Rarity = 5;
			IsRanged = true;
			break;
		case EntityID.ItemID.BELL:
			Rarity = 3;
			UseStyle = 1;
			UseAnimation = 12;
			UseTime = 12;
			Width = 12;
			Height = 28;
			AutoReuse = true;
			NoMelee = true;
			Value = 10000;
			break;
		case EntityID.ItemID.HARP:
			Rarity = 3;
			UseStyle = 5;
			UseAnimation = 12;
			UseTime = 12;
			Width = 12;
			Height = 28;
			AutoReuse = true;
			NoMelee = true;
			Value = 10000;
			break;
		case EntityID.ItemID.WRENCH:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			Width = 24;
			Height = 28;
			Rarity = 1;
			Value = 20000;
			Mech = true;
			break;
		case EntityID.ItemID.WIRE_CUTTER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			Width = 24;
			Height = 28;
			Rarity = 1;
			Value = 20000;
			Mech = true;
			break;
		case EntityID.ItemID.ACTIVE_STONE_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 130;
			Width = 12;
			Height = 12;
			Value = 1000;
			Mech = true;
			break;
		case EntityID.ItemID.INACTIVE_STONE_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 131;
			Width = 12;
			Height = 12;
			Value = 1000;
			Mech = true;
			break;
		case EntityID.ItemID.LEVER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 132;
			Width = 24;
			Height = 24;
			Value = 3000;
			Mech = true;
			break;
		case EntityID.ItemID.LASER_RIFLE:
			AutoReuse = true;
			UseStyle = 5;
			UseAnimation = 12;
			UseTime = 12;
			Width = 36;
			Height = 22;
			Shoot = 88;
			Mana = 8;
			UseSound = 12;
			Knockback = 2.5f;
			Damage = 29;
			ShootSpeed = 17f;
			NoMelee = true;
			Rarity = 4;
			IsMagic = true;
			Value = 500000;
			break;
		case EntityID.ItemID.CRYSTAL_BULLET:
			ShootSpeed = 5f;
			Shoot = 89;
			Damage = 9;
			Width = 8;
			Height = 8;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 14;
			Knockback = 1f;
			Value = 30;
			IsRanged = true;
			Rarity = 3;
			break;
		case EntityID.ItemID.HOLY_ARROW:
			ShootSpeed = 3.5f;
			Shoot = 91;
			Damage = 6;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 2f;
			Value = 80;
			IsRanged = true;
			Rarity = 3;
			break;
		case EntityID.ItemID.MAGIC_DAGGER:
			UseStyle = 1;
			ShootSpeed = 10f;
			Shoot = 93;
			Damage = 28;
			Width = 18;
			Height = 20;
			Mana = 7;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			NoMelee = true;
			Value = 1000000;
			Knockback = 2f;
			IsMagic = true;
			Rarity = 4;
			break;
		case EntityID.ItemID.CRYSTAL_STORM:
			AutoReuse = true;
			Rarity = 4;
			Mana = 5;
			UseSound = 9;
			UseStyle = 5;
			Damage = 26;
			UseAnimation = 7;
			UseTime = 7;
			Width = 24;
			Height = 28;
			Shoot = 94;
			Scale = 0.9f;
			ShootSpeed = 16f;
			Knockback = 5f;
			IsMagic = true;
			Value = 500000;
			break;
		case EntityID.ItemID.CURSED_FLAMES:
			AutoReuse = true;
			Rarity = 4;
			Mana = 14;
			UseSound = 20;
			UseStyle = 5;
			Damage = 35;
			UseAnimation = 20;
			UseTime = 20;
			Width = 24;
			Height = 28;
			Shoot = 95;
			Scale = 0.9f;
			ShootSpeed = 10f;
			Knockback = 6.5f;
			IsMagic = true;
			Value = 500000;
			break;
		case EntityID.ItemID.SOUL_OF_LIGHT:
			Width = 18;
			Height = 18;
			MaxStack = 250;
			Value = 1000;
			Rarity = 3;
			break;
		case EntityID.ItemID.SOUL_OF_NIGHT:
			Width = 18;
			Height = 18;
			MaxStack = 250;
			Value = 1000;
			Rarity = 3;
			break;
		case EntityID.ItemID.SOUL_OF_BLIGHT:
			Width = 18;
			Height = 18;
			MaxStack = 250;
			Value = 100000;
			Rarity = 5;
			break;
		case EntityID.ItemID.CURSED_FLAME:
			Width = 12;
			Height = 14;
			MaxStack = 99;
			Value = 4000;
			Rarity = 3;
			break;
		case EntityID.ItemID.CURSED_TORCH:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			HoldStyle = 1;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 4;
			PlaceStyle = 8;
			Width = 10;
			Height = 12;
			Value = 300;
			Rarity = 1;
			break;
		case EntityID.ItemID.ADAMANTITE_FORGE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 133;
			Width = 44;
			Height = 30;
			Value = 50000;
			Rarity = 3;
			break;
		case EntityID.ItemID.MYTHRIL_ANVIL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 134;
			Width = 28;
			Height = 14;
			Value = 25000;
			Rarity = 3;
			break;
		case EntityID.ItemID.UNICORN_HORN:
			Width = 14;
			Height = 14;
			MaxStack = 99;
			Value = 15000;
			Rarity = 1;
			break;
		case EntityID.ItemID.DARK_SHARD:
			Width = 14;
			Height = 14;
			MaxStack = 99;
			Value = 4500;
			Rarity = 2;
			break;
		case EntityID.ItemID.LIGHT_SHARD:
			Width = 14;
			Height = 14;
			MaxStack = 99;
			Value = 4500;
			Rarity = 2;
			break;
		case EntityID.ItemID.RED_PRESSURE_PLATE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 135;
			Width = 12;
			Height = 12;
			PlaceStyle = 0;
			Mech = true;
			Value = 5000;
			Mech = true;
			break;
		case EntityID.ItemID.WIRE:
			Width = 12;
			Height = 18;
			MaxStack = 250;
			Value = 500;
			Mech = true;
			break;
		case EntityID.ItemID.SPELL_TOME:
			Width = 12;
			Height = 18;
			MaxStack = 99;
			Value = 50000;
			Rarity = 1;
			break;
		case EntityID.ItemID.STAR_CLOAK:
			Width = 20;
			Height = 24;
			Value = 100000;
			IsAccessory = true;
			Rarity = 4;
			break;
		case EntityID.ItemID.MEGASHARK:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 7;
			UseTime = 7;
			Width = 50;
			Height = 18;
			Shoot = 10;
			UseAmmo = 14;
			UseSound = 11;
			Damage = 23;
			ShootSpeed = 10f;
			NoMelee = true;
			Value = 300000;
			Rarity = 5;
			Knockback = 1f;
			IsRanged = true;
			break;
		case EntityID.ItemID.SHOTGUN:
			Knockback = 6.5f;
			UseStyle = 5;
			UseAnimation = 45;
			UseTime = 45;
			Width = 50;
			Height = 14;
			Shoot = 10;
			UseAmmo = 14;
			UseSound = 36;
			Damage = 18;
			ShootSpeed = 6f;
			NoMelee = true;
			Value = 700000;
			Rarity = 4;
			IsRanged = true;
			break;
		case EntityID.ItemID.PHILOSOPHERS_STONE:
			Width = 12;
			Height = 18;
			Value = 100000;
			IsAccessory = true;
			Rarity = 4;
			break;
		case EntityID.ItemID.TITAN_GLOVE:
			Width = 12;
			Height = 18;
			Value = 100000;
			Rarity = 4;
			IsAccessory = true;
			break;
		case EntityID.ItemID.COBALT_NAGINATA:
			UseStyle = 5;
			UseAnimation = 28;
			UseTime = 28;
			ShootSpeed = 4.3f;
			Knockback = 4f;
			Width = 40;
			Height = 40;
			Damage = 29;
			Scale = 1.1f;
			UseSound = 1;
			Shoot = 97;
			Rarity = 4;
			Value = 45000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			break;
		case EntityID.ItemID.SWITCH:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 136;
			Width = 12;
			Height = 12;
			Value = 2000;
			Mech = true;
			break;
		case EntityID.ItemID.DART_TRAP:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 137;
			Width = 12;
			Height = 12;
			Value = 10000;
			Mech = true;
			break;
		case EntityID.ItemID.BOULDER:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 138;
			Width = 12;
			Height = 12;
			Mech = true;
			break;
		case EntityID.ItemID.GREEN_PRESSURE_PLATE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 135;
			Width = 12;
			Height = 12;
			PlaceStyle = 1;
			Mech = true;
			Value = 5000;
			break;
		case EntityID.ItemID.GRAY_PRESSURE_PLATE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 135;
			Width = 12;
			Height = 12;
			PlaceStyle = 2;
			Mech = true;
			Value = 5000;
			break;
		case EntityID.ItemID.BROWN_PRESSURE_PLATE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 135;
			Width = 12;
			Height = 12;
			PlaceStyle = 3;
			Mech = true;
			Value = 5000;
			break;
		case EntityID.ItemID.MECHANICAL_EYE:
			UseStyle = 4;
			Width = 22;
			Height = 14;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			MaxStack = 20;
			Rarity = 3;
			break;
		case EntityID.ItemID.CURSED_ARROW:
			ShootSpeed = 4f;
			Shoot = 103;
			Damage = 14;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 3f;
			Value = 80;
			IsRanged = true;
			Rarity = 3;
			break;
		case EntityID.ItemID.SPECTRAL_ARROW:
			ShootSpeed = 4.2f;
			Shoot = 113;
			Damage = 16;
			Width = 10;
			Height = 28;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 1;
			Knockback = 3.1f;
			Value = 90;
			IsRanged = true;
			Rarity = 4;
			break;
		case EntityID.ItemID.CURSED_BULLET:
			ShootSpeed = 5f;
			Shoot = 104;
			Damage = 12;
			Width = 8;
			Height = 8;
			MaxStack = 250;
			IsConsumable = true;
			Ammo = 14;
			Knockback = 4f;
			Value = 30;
			Rarity = 1;
			IsRanged = true;
			Rarity = 3;
			break;
		case EntityID.ItemID.SOUL_OF_FRIGHT:
			Width = 18;
			Height = 18;
			MaxStack = 250;
			Value = 100000;
			Rarity = 5;
			break;
		case EntityID.ItemID.SOUL_OF_MIGHT:
			Width = 18;
			Height = 18;
			MaxStack = 250;
			Value = 100000;
			Rarity = 5;
			break;
		case EntityID.ItemID.SOUL_OF_SIGHT:
			Width = 18;
			Height = 18;
			MaxStack = 250;
			Value = 100000;
			Rarity = 5;
			break;
		case EntityID.ItemID.GUNGNIR:
			UseStyle = 5;
			UseAnimation = 22;
			UseTime = 22;
			ShootSpeed = 5.6f;
			Knockback = 6.4f;
			Width = 40;
			Height = 40;
			Damage = 42;
			Scale = 1.1f;
			UseSound = 1;
			Shoot = 105;
			Rarity = 5;
			Value = 1500000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			break;
		case EntityID.ItemID.TONBOGIRI:
			UseStyle = 5;
			UseAnimation = 20;
			UseTime = 20;
			ShootSpeed = 5.75f;
			Knockback = 6.7f;
			Width = 40;
			Height = 40;
			Damage = 50;
			Scale = 1.1f;
			UseSound = 1;
			Shoot = 112;
			Rarity = 5;
			Value = 2000000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			break;
		case EntityID.ItemID.HALLOWED_PLATE_MAIL:
			Width = 18;
			Height = 18;
			Defense = 15;
			BodySlot = 24;
			Rarity = 5;
			Value = 200000;
			break;
		case EntityID.ItemID.HALLOWED_GREAVES:
			Width = 18;
			Height = 18;
			Defense = 11;
			LegSlot = 23;
			Rarity = 5;
			Value = 150000;
			break;
		case EntityID.ItemID.HALLOWED_HELMET:
			Width = 18;
			Height = 18;
			Defense = 9;
			HeadSlot = 41;
			Rarity = 5;
			Value = 250000;
			break;
		case EntityID.ItemID.HALLOWED_HEADGEAR:
			Width = 18;
			Height = 18;
			Defense = 5;
			HeadSlot = 42;
			Rarity = 5;
			Value = 250000;
			break;
		case EntityID.ItemID.HALLOWED_MASK:
			Width = 18;
			Height = 18;
			Defense = 24;
			HeadSlot = 43;
			Rarity = 5;
			Value = 250000;
			break;
		case EntityID.ItemID.CROSS_NECKLACE:
			Width = 20;
			Height = 24;
			Value = 1500;
			IsAccessory = true;
			Rarity = 4;
			break;
		case EntityID.ItemID.MANA_FLOWER:
			Width = 20;
			Height = 24;
			Value = 50000;
			IsAccessory = true;
			Rarity = 4;
			break;
		case EntityID.ItemID.MECHANICAL_WORM:
			UseStyle = 4;
			Width = 22;
			Height = 14;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			MaxStack = 20;
			Rarity = 3;
			break;
		case EntityID.ItemID.MECHANICAL_SKULL:
			UseStyle = 4;
			Width = 22;
			Height = 14;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			MaxStack = 20;
			Rarity = 3;
			break;
		case EntityID.ItemID.SLIME_CROWN:
			UseStyle = 4;
			Width = 22;
			Height = 14;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			MaxStack = 20;
			Rarity = 1;
			break;
		case EntityID.ItemID.LIGHT_DISC:
			IsMelee = true;
			AutoReuse = true;
			NoMelee = true;
			UseStyle = 1;
			ShootSpeed = 13f;
			Shoot = 106;
			Damage = 35;
			Knockback = 8f;
			Width = 24;
			Height = 24;
			UseSound = 1;
			UseAnimation = 15;
			UseTime = 15;
			NoUseGraphic = true;
			Rarity = 5;
			MaxStack = 5;
			Value = 500000;
			break;
		case EntityID.ItemID.MUSIC_BOX_OVERWORLD_DAY:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 0;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_EERIE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 1;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_NIGHT:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 2;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_TITLE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 3;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_UNDERGROUND:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 4;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_BOSS1:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 5;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_JUNGLE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 6;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_CORRUPTION:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 7;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_UNDERGROUND_CORRUPTION:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 8;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_THE_HALLOW:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 9;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_BOSS2:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 10;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_UNDERGROUND_HALLOW:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 11;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_BOSS3:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 12;
			Width = 24;
			Height = 24;
			Rarity = 3;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_DESERT:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 13;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_SPACE:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 14;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_TUTORIAL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 15;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_BOSS4:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 16;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_OCEAN:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 17;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.MUSIC_BOX_SNOW:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			IsConsumable = true;
			CreateTile = 139;
			PlaceStyle = 18;
			Width = 24;
			Height = 24;
			Rarity = 4;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.SOUL_OF_FLIGHT:
			Width = 18;
			Height = 18;
			MaxStack = 250;
			Value = 1000;
			Rarity = 3;
			break;
		case EntityID.ItemID.MUSIC_BOX:
			Width = 24;
			Height = 24;
			Rarity = 3;
			Value = 100000;
			IsAccessory = true;
			break;
		case EntityID.ItemID.DEMONITE_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 140;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.HALLOWED_REPEATER:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 19;
			UseTime = 19;
			Width = 50;
			Height = 18;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 39;
			ShootSpeed = 11f;
			NoMelee = true;
			Value = 200000;
			IsRanged = true;
			Rarity = 4;
			Knockback = 2.5f;
			break;
		case EntityID.ItemID.VULCAN_REPEATER:
			UseStyle = 5;
			AutoReuse = true;
			UseAnimation = 18;
			UseTime = 18;
			Width = 50;
			Height = 18;
			Shoot = 1;
			UseAmmo = 1;
			UseSound = 5;
			Damage = 42;
			ShootSpeed = 12f;
			NoMelee = true;
			Value = 250000;
			IsRanged = true;
			Rarity = 5;
			Knockback = 2.65f;
			break;
		case EntityID.ItemID.HAMDRAX:
			UseStyle = 5;
			UseAnimation = 25;
			UseTime = 7;
			ShootSpeed = 36f;
			Knockback = 4.75f;
			Width = 20;
			Height = 12;
			Damage = 35;
			PickPower = 200;
			AxePower = 22;
			HammerPower = 85;
			UseSound = 23;
			Shoot = 107;
			Rarity = 4;
			Value = 220000;
			NoMelee = true;
			NoUseGraphic = true;
			IsMelee = true;
			Channelling = true;
			break;
		case EntityID.ItemID.EXPLOSIVES:
			Mech = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 141;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.INLET_PUMP:
			Mech = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 142;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.OUTLET_PUMP:
			Mech = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 143;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.ONE_SECOND_TIMER:
			Mech = true;
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 144;
			PlaceStyle = 0;
			Width = 10;
			Height = 12;
			Value = 50;
			break;
		case EntityID.ItemID.THREE_SECOND_TIMER:
			Mech = true;
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 144;
			PlaceStyle = 1;
			Width = 10;
			Height = 12;
			Value = 50;
			break;
		case EntityID.ItemID.FIVE_SECOND_TIMER:
			Mech = true;
			CantTouchLiquid = true;
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 99;
			IsConsumable = true;
			CreateTile = 144;
			PlaceStyle = 2;
			Width = 10;
			Height = 12;
			Value = 50;
			break;
		case EntityID.ItemID.CANDY_CANE_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 145;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.CANDY_CANE_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 29;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.SANTA_HAT:
			Width = 18;
			Height = 12;
			HeadSlot = 44;
			Value = 150000;
			IsVanity = true;
			break;
		case EntityID.ItemID.SANTA_SHIRT:
			Width = 18;
			Height = 18;
			BodySlot = 25;
			Value = 150000;
			IsVanity = true;
			break;
		case EntityID.ItemID.SANTA_PANTS:
			Width = 18;
			Height = 18;
			LegSlot = 24;
			Value = 150000;
			IsVanity = true;
			break;
		case EntityID.ItemID.GREEN_CANDY_CANE_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 146;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.GREEN_CANDY_CANE_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 30;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.SNOW_BLOCK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 147;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.SNOW_BRICK:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 148;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.SNOW_BRICK_WALL:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateWall = 31;
			Width = 12;
			Height = 12;
			break;
		case EntityID.ItemID.BLUE_LIGHT:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 149;
			PlaceStyle = 0;
			Width = 12;
			Height = 12;
			Value = 500;
			break;
		case EntityID.ItemID.RED_LIGHT:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 149;
			PlaceStyle = 1;
			Width = 12;
			Height = 12;
			Value = 500;
			break;
		case EntityID.ItemID.GREEN_LIGHT:
			UseStyle = 1;
			CanUseTurn = true;
			UseAnimation = 15;
			UseTime = 10;
			AutoReuse = true;
			MaxStack = 250;
			IsConsumable = true;
			CreateTile = 149;
			PlaceStyle = 2;
			Width = 12;
			Height = 12;
			Value = 500;
			break;
		case EntityID.ItemID.BLUE_PRESENT:
			Width = 12;
			Height = 12;
			Rarity = 1;
			break;
		case EntityID.ItemID.GREEN_PRESENT:
			Width = 12;
			Height = 12;
			Rarity = 1;
			break;
		case EntityID.ItemID.YELLOW_PRESENT:
			Width = 12;
			Height = 12;
			Rarity = 1;
			break;
		case EntityID.ItemID.SNOW_GLOBE:
			UseStyle = 4;
			IsConsumable = true;
			UseAnimation = 45;
			UseTime = 45;
			Width = 28;
			Height = 28;
			Rarity = 2;
			break;
		case EntityID.ItemID.PET_SPAWN_1:
			Damage = 0;
			UseStyle = 1;
			Shoot = 111;
			Width = 16;
			Height = 30;
			UseSound = 2;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Value = 0;
			BuffType = 40;
			break;
		case EntityID.ItemID.PET_SPAWN_2:
			Damage = 0;
			UseStyle = 1;
			Shoot = 115;
			Width = 16;
			Height = 30;
			UseSound = 2;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Value = 0;
			BuffType = 40;
			break;
		case EntityID.ItemID.PET_SPAWN_3:
			Damage = 0;
			UseStyle = 1;
			Shoot = 116;
			Width = 16;
			Height = 30;
			UseSound = 2;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Value = 0;
			BuffType = 40;
			break;
		case EntityID.ItemID.PET_SPAWN_4:
			Damage = 0;
			UseStyle = 1;
			Shoot = 117;
			Width = 16;
			Height = 30;
			UseSound = 2;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Value = 0;
			BuffType = 40;
			break;
		case EntityID.ItemID.PET_SPAWN_5:
			Damage = 0;
			UseStyle = 1;
			Shoot = 118;
			Width = 16;
			Height = 30;
			UseSound = 2;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Value = 0;
			BuffType = 40;
			break;
		case EntityID.ItemID.PET_SPAWN_6:
			Damage = 0;
			UseStyle = 1;
			Shoot = 119;
			Width = 16;
			Height = 30;
			UseSound = 2;
			UseAnimation = 20;
			UseTime = 20;
			Rarity = 3;
			NoMelee = true;
			Value = 0;
			BuffType = 40;
			break;
		case EntityID.ItemID.DRAGON_MASK:
			Width = 26;
			Height = 18;
			Defense = 26;
			HeadSlot = 45;
			Rarity = 5;
			Value = 500000;
			break;
		case EntityID.ItemID.TITAN_HELMET:
			Width = 26;
			Height = 22;
			Defense = 14;
			HeadSlot = 46;
			Rarity = 5;
			Value = 500000;
			break;
		case EntityID.ItemID.SPECTRAL_HEADGEAR:
			Width = 22;
			Height = 20;
			Defense = 10;
			HeadSlot = 47;
			Rarity = 5;
			Value = 500000;
			break;
		case EntityID.ItemID.DRAGON_BREASTPLATE:
			Width = 26;
			Height = 18;
			Defense = 20;
			BodySlot = 26;
			Rarity = 5;
			Value = 1000000;
			break;
		case EntityID.ItemID.TITAN_MAIL:
			Width = 30;
			Height = 18;
			Defense = 18;
			BodySlot = 27;
			Rarity = 5;
			Value = 1000000;
			break;
		case EntityID.ItemID.SPECTRAL_ARMOR:
			Width = 30;
			Height = 28;
			Defense = 15;
			BodySlot = 28;
			Rarity = 5;
			Value = 1000000;
			break;
		case EntityID.ItemID.DRAGON_GREAVES:
			Width = 22;
			Height = 18;
			Defense = 14;
			LegSlot = 25;
			Rarity = 5;
			Value = 750000;
			break;
		case EntityID.ItemID.TITAN_LEGGINGS:
			Width = 22;
			Height = 18;
			Defense = 13;
			LegSlot = 26;
			Rarity = 5;
			Value = 750000;
			break;
		case EntityID.ItemID.SPECTRAL_SUBLIGAR:
			Width = 22;
			Height = 18;
			Defense = 15;
			LegSlot = 27;
			Rarity = 5;
			Value = 750000;
			break;
		default:
			Active = 0;
			Stack = 0;
			break;
		}
		if (!NoMaterialCheck)
		{
			CheckMaterial();
		}
		this = Terraria.Mods.ItemOverrides.Apply(this);
	}

	public Color GetAlpha(Color ItemColour)
	{
		if (Type == 75)
		{
			return new Color(255, 255, 255, ItemColour.A - Alpha);
		}
		if ((Type >= 119 && Type <= 122) || (Type >= 198 && Type <= 203) || Type == 217 || Type == 218 || Type == 219 || Type == 220)
		{
			return new Color(255, 255, 255, 255);
		}
		if (Type == 520 || Type == 521 || Type == 522 || Type == 547 || Type == 548 || Type == 549 || Type == 575)
		{
			return new Color(255, 255, 255, 50);
		}
		if (Type == 58 || Type == 184 || Type == 501)
		{
			return new Color(200, 200, 200, 200);
		}
		int num = 256 - Alpha;
		int r = ItemColour.R * num >> 8;
		int g = ItemColour.G * num >> 8;
		int b = ItemColour.B * num >> 8;
		int alpha = ItemColour.A - Alpha;
		return new Color(r, g, b, alpha);
	}

	public Color GetAlphaInventory(Color ItemColour)
	{
		int num = 256 - Alpha;
		int r = ItemColour.R * num >> 8;
		int g = ItemColour.G * num >> 8;
		int b = ItemColour.B * num >> 8;
		int alpha = ItemColour.A - Alpha;
		return new Color(r, g, b, alpha);
	}

	public Color GetColor(Color ItemColour)
	{
		int r = Colour.R - (255 - ItemColour.R);
		int g = Colour.G - (255 - ItemColour.G);
		int b = Colour.B - (255 - ItemColour.B);
		int alpha = Colour.A - (255 - ItemColour.A);
		return new Color(r, g, b, alpha);
	}

	public static bool MechSpawn(int ItemX, int ItemY, int ItemType)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < 196; i++)
		{
			if (Main.ItemSet[i].Active == 0 || Main.ItemSet[i].Type != ItemType)
			{
				continue;
			}
			num++;
			Vector2 vector = new Vector2(ItemX, ItemY);
			float num4 = Main.ItemSet[i].Position.X - vector.X;
			float num5 = Main.ItemSet[i].Position.Y - vector.Y;
			float num6 = num4 * num4 + num5 * num5;
			if (num6 < 640000f)
			{
				num3++;
				if (num6 < 90000f)
				{
					num2++;
				}
			}
		}
		if (num2 >= 3 || num3 >= 6 || num >= 10)
		{
			return false;
		}
		return true;
	}

	public unsafe void UpdateItem(int ActiveItemIdx)
	{
		float num = 0.1f;
		float num2 = 7f;
		if (IsWet)
		{
			num2 = 5f;
			num = 0.08f;
		}
		Vector2 velocity = Velocity;
		velocity.X *= 0.5f;
		velocity.Y *= 0.5f;
		if (OwnTime > 0 && --OwnTime == 0)
		{
			OwnIgnore = 8;
		}
		if (KeepTime > 0)
		{
			KeepTime--;
		}
		else if (IsLocal() || (Main.NetMode != 1 && (Owner == 8 || Main.PlayerSet[Owner].Active == 0)))
		{
			FindOwner(ActiveItemIdx);
		}
		if (!BeingGrabbed)
		{
			Velocity.X *= 0.95f;
			if (Velocity.X < 0.1f && Velocity.X > -0.1f)
			{
				Velocity.X = 0f;
			}
			if (Type == 520 || Type == 521 || Type == 547 || Type == 548 || Type == 549 || Type == 575)
			{
				Velocity.Y *= 0.95f;
				if (Velocity.Y < 0.1f && Velocity.Y > -0.1f)
				{
					Velocity.Y = 0f;
				}
			}
			else
			{
				Velocity.Y += num;
				if (Velocity.Y > num2)
				{
					Velocity.Y = num2;
				}
			}
			bool flag = Collision.LavaCollision(ref Position, Width, Height);
			IsInLava |= flag;
			if (Collision.WetCollision(ref Position, Width, Height))
			{
				if (!IsWet)
				{
					if (WetCount == 0)
					{
						WetCount = 20;
						if (!flag)
						{
							for (int i = 0; i < 8; i++)
							{
								Dust* ptr = Main.DustSet.NewDust((int)Position.X - 6, (int)Position.Y + (Height >> 1) - 8, Width + 12, 24, 33);
								if (ptr == null)
								{
									break;
								}
								ptr->Velocity.Y -= 4f;
								ptr->Velocity.X *= 2.5f;
								ptr->Scale = 1.3f;
								ptr->Alpha = 100;
								ptr->NoGravity = true;
							}
							Main.PlaySound(19, (int)Position.X, (int)Position.Y);
						}
						else
						{
							for (int j = 0; j < 4; j++)
							{
								Dust* ptr2 = Main.DustSet.NewDust((int)Position.X - 6, (int)Position.Y + (Height >> 1) - 8, Width + 12, 24, 35);
								if (ptr2 == null)
								{
									break;
								}
								ptr2->Velocity.Y -= 1.5f;
								ptr2->Velocity.X *= 2.5f;
								ptr2->Scale = 1.3f;
								ptr2->Alpha = 100;
								ptr2->NoGravity = true;
							}
							Main.PlaySound(19, (int)Position.X, (int)Position.Y);
						}
					}
					IsWet = true;
				}
			}
			else if (IsWet)
			{
				IsWet = false;
			}
			if (WetCount > 0)
			{
				WetCount--;
			}
			if (IsWet)
			{
				Vector2 velocity2 = Velocity;
				Collision.TileCollision(ref Position, ref Velocity, Width, Height);
				if (Velocity.X != velocity2.X)
				{
					velocity.X = Velocity.X;
				}
				if (Velocity.Y != velocity2.Y)
				{
					velocity.Y = Velocity.Y;
				}
			}
			else
			{
				IsInLava = false;
				Collision.TileCollision(ref Position, ref Velocity, Width, Height);
			}
			if (IsInLava)
			{
				if (Type == 267)
				{
					if (Main.NetMode != 1)
					{
						Active = 0;
						Type = 0;
						Stack = 0;
						for (int k = 0; k < 196; k++)
						{
							if (Main.NPCSet[k].Type == 22 && Main.NPCSet[k].Active != 0)
							{
								NetMessage.SendNpcHurt(k, 8192, 10.0, -Main.NPCSet[k].Direction);
								Main.NPCSet[k].StrikeNPC(8192, 10f, -Main.NPCSet[k].Direction);
								NPC.SpawnWOF(ref Position);
								break;
							}
						}
						NetMessage.CreateMessage2(21, UI.MainUI.MyPlayer, ActiveItemIdx);
						NetMessage.SendMessage();
					}
				}
				else if (IsLocal() && Type != 312 && Type != 318 && Type != 173 && Type != 174 && Type != 175 && Rarity == 0)
				{
					Active = 0;
					Type = 0;
					Stack = 0;
					NetMessage.CreateMessage2(21, UI.MainUI.MyPlayer, ActiveItemIdx);
					NetMessage.SendMessage();
				}
			}
			// A mod item that declares `light` glows on the ground, the same call the vanilla items
			// below make. Placed before the chain so it cannot change which vanilla branch runs.
			if (Light > 0f)
			{
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4,
					new Vector3(Light, Light, Light));
			}
			if (Type == 520)
			{
				float num3 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num3 *= UI.PulseScale;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.5f * num3, 0.1f * num3, 0.25f * num3));
			}
			else if (Type == 521)
			{
				float num4 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num4 *= UI.PulseScale;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.25f * num4, 0.1f * num4, 0.5f * num4));
			}
			else if (Type == 547)
			{
				float num5 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num5 *= UI.PulseScale;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.5f * num5, 0.3f * num5, 0.05f * num5));
			}
			else if (Type == 548)
			{
				float num6 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num6 *= UI.PulseScale;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.1f * num6, 0.1f * num6, 0.6f * num6));
			}
			else if (Type == 575)
			{
				float num7 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num7 *= UI.PulseScale;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.1f * num7, 0.3f * num7, 0.5f * num7));
			}
			else if (Type == 549)
			{
				float num8 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num8 *= UI.PulseScale;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.1f * num8, 0.5f * num8, 0.2f * num8));
			}
			else if (Type == 58)
			{
				float num9 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num9 *= UI.PulseScale * 0.5f;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.5f * num9, 0.1f * num9, 0.1f * num9));
			}
			else if (Type == 184)
			{
				float num10 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num10 *= UI.PulseScale * 0.5f;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.1f * num10, 0.1f * num10, 0.5f * num10));
			}
			else if (Type == 522)
			{
				float num11 = (float)Main.Rand.Next(90, 111) * 0.01f;
				num11 *= UI.PulseScale * 0.2f;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.5f * num11, 1f * num11, 0.1f * num11));
			}
			else if (Type == 75 && Main.GameTime.DayTime)
			{
				for (int l = 0; l < 8; l++)
				{
					if (null == Main.DustSet.NewDust((int)Position.X, (int)Position.Y, Width, Height, 15, Velocity.X, Velocity.Y, 150, default, 1.2))
					{
						break;
					}
				}
				for (int m = 0; m < 3; m++)
				{
					Gore.NewGore(Position, Velocity, Main.Rand.Next(16, 18));
				}
				Active = 0;
				Type = 0;
				Stack = 0;
				if (Main.NetMode == 2)
				{
					NetMessage.CreateMessage2(21, UI.MainUI.MyPlayer, ActiveItemIdx);
					NetMessage.SendMessage();
				}
			}
		}
		else
		{
			BeingGrabbed = false;
		}
		if (Type == 501)
		{
			if (Main.Rand.Next(6) == 0)
			{
				Dust* ptr3 = Main.DustSet.NewDust((int)Position.X, (int)Position.Y, Width, Height, 55, 0.0, 0.0, 200, Colour);
				if (ptr3 != null)
				{
					ptr3->Velocity.X *= 0.3f;
					ptr3->Velocity.Y *= 0.3f;
					ptr3->Scale *= 0.5f;
				}
			}
		}
		else if (Type == 8 || Type == 105)
		{
			if (!IsWet)
			{
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(1f, 0.95f, 0.8f));
			}
		}
		else if (Type == 523)
		{
			Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.85f, 1f, 0.7f));
		}
		else if (Type >= 427 && Type <= 432)
		{
			if (!IsWet)
			{
				short type = Type;
				if (1 == 0)
				{
				}
				Vector3 vector = type switch
				{
					427 => new Vector3(0.1f, 0.2f, 1.1f), 
					428 => new Vector3(1f, 0.1f, 0.1f), 
					429 => new Vector3(0f, 1f, 0.1f), 
					430 => new Vector3(0.9f, 0f, 0.9f), 
					431 => new Vector3(1.3f, 1.3f, 1.3f), 
					_ => new Vector3(0.9f, 0.9f, 0f), 
				};
				if (1 == 0)
				{
				}
				Vector3 rGB = vector;
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, rGB);
			}
		}
		else if (Type == 41)
		{
			if (!IsWet)
			{
				Lighting.AddLight((int)Position.X + Width >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(1f, 0.75f, 0.55f));
			}
		}
		else if (Type == 616)
		{
			Lighting.AddLight((int)Position.X + Width >> 4, (int)Position.Y + (Height >> 1) >> 4, IsWet ? new Vector3(0.25f, 0.5f, 0.5f) : new Vector3(0.5f, 1f, 1f));
		}
		else if (Type == 282)
		{
			Lighting.AddLight((int)Position.X + Width >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.7f, 1f, 0.8f));
		}
		else if (Type == 286)
		{
			Lighting.AddLight((int)Position.X + Width >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.7f, 0.8f, 1f));
		}
		else if (Type == 331)
		{
			Lighting.AddLight((int)Position.X + Width >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.55f, 0.75f, 0.6f));
		}
		else if (Type == 183)
		{
			Lighting.AddLight((int)Position.X + Width >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.15f, 0.45f, 0.9f));
		}
		else if (Type == 75)
		{
			Lighting.AddLight((int)Position.X + Width >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.8f, 0.7f, 0.1f));
			if (Main.Rand.Next(32) == 0)
			{
				Main.DustSet.NewDust((int)Position.X, (int)Position.Y, Width, Height, 58, Velocity.X * 0.5f, Velocity.Y * 0.5f, 150, default, 1.2);
			}
			else if (Main.Rand.Next(64) == 0)
			{
				Gore.NewGore(Position, new Vector2(Velocity.X * 0.2f, Velocity.Y * 0.2f), Main.Rand.Next(16, 18));
			}
		}
		SpawnTime++;
		if (Main.NetMode == 2 && !IsLocal() && ++Release >= 300)
		{
			Release = 0;
			FindOwner(ActiveItemIdx);
		}
		if (IsWet)
		{
			Position.X += velocity.X;
			Position.Y += velocity.Y;
		}
		else
		{
			Position.X += Velocity.X;
			Position.Y += Velocity.Y;
		}
		if (NoGrabDelay > 0)
		{
			NoGrabDelay--;
		}
	}

	public unsafe static int NewItem(int X, int Y, int Width, int Height, int Type, int Stack = 1, bool DoNotBroadcast = false, int Prefix = 0)
	{
		int num = 200;
		if (Main.NetMode != 1)
		{
			uint num2 = LastItemIndex;
			uint num3 = Main.ItemSet[num2].SpawnTime;
			uint num4 = num2;
			for (int num5 = 199; num5 >= 0; num5--)
			{
				if (Main.ItemSet[num2].Active == 0)
				{
					num = (int)num2;
					break;
				}
				if (++num2 == 200)
				{
					num2 = 0u;
				}
				uint spawnTime = Main.ItemSet[num2].SpawnTime;
				if (spawnTime > num3)
				{
					num3 = spawnTime;
					num4 = num2;
				}
			}
			if (num == 200)
			{
				num = (int)num4;
			}
			if (++num2 == 200)
			{
				num2 = 0u;
			}
			LastItemIndex = num2;
		}
		fixed (Item* ptr = &Main.ItemSet[num])
		{
			ptr->SetDefaults(Type, Stack);
			ptr->SetPrefix(Prefix);
			ptr->Position.X = X + (Width - ptr->Width >> 1);
			ptr->Position.Y = Y + (Height - ptr->Height >> 1);
			ptr->IsWet = Collision.WetCollision(ref ptr->Position, ptr->Width, ptr->Height);
			ptr->Velocity.X = (float)Main.Rand.Next(-30, 31) * 0.1f;
			if (Type == 520 || Type == 521)
			{
				ptr->Velocity.Y = (float)Main.Rand.Next(-30, 31) * 0.1f;
			}
			else
			{
				ptr->Velocity.Y = (float)Main.Rand.Next(-40, -15) * 0.1f;
			}
			ptr->SpawnTime = 0u;
			if (!DoNotBroadcast && Main.NetMode != 1)
			{
				NetMessage.CreateMessage2(21, UI.MainUI.MyPlayer, num);
				NetMessage.SendMessage();
				ptr->FindOwner(num);
			}
		}
		return num;
	}

	public unsafe void FindOwner(int WhoAmI)
	{
		if (KeepTime > 0)
		{
			return;
		}
		int num = 8;
		int num2 = 1920;
		int num3 = (int)Position.X - (Width >> 1);
		int num4 = (int)Position.Y - Height;
		fixed (Item* pNewItem = &this)
		{
			for (int i = 0; i < 8; i++)
			{
				if (OwnIgnore != i && Main.PlayerSet[i].Active != 0 && Main.PlayerSet[i].ItemSpace(pNewItem))
				{
					int num5 = Math.Abs(Main.PlayerSet[i].XYWH.X + 10 - num3) + Math.Abs(Main.PlayerSet[i].XYWH.Y + 21 - num4);
					if (num5 < num2)
					{
						num2 = num5;
						num = i;
					}
				}
			}
		}
		int owner = Owner;
		if (num != owner)
		{
			bool flag = IsLocal();
			Owner = (byte)num;
			if (((flag && Main.NetMode >= 1) || (owner == 8 && Main.NetMode == 2) || Main.PlayerSet[owner].Active == 0) && Active != 0)
			{
				NetMessage.CreateMessage1(22, WhoAmI);
				NetMessage.SendMessage();
			}
		}
	}

	public bool IsNotTheSameAs(ref Item ComparedItem)
	{
		if (NetID == ComparedItem.NetID && Stack == ComparedItem.Stack)
		{
			return PrefixType != ComparedItem.PrefixType;
		}
		return true;
	}

	public bool CanBePlacedInAmmoSlot()
	{
		if (Ammo <= 0)
		{
			return Type == 530;
		}
		return true;
	}

	public bool CanBeAutoPlacedInEmptyAmmoSlot()
	{
		if (Type != 169 && Type != 75 && Type != 23 && Type != 370)
		{
			return Type != 408;
		}
		return false;
	}

	public bool CanBePlacedInCoinSlot()
	{
		if (Type >= 71)
		{
			return Type <= 74;
		}
		return false;
	}
}
