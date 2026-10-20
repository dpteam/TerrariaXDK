namespace Terraria;

public struct Buff
{
	public const int MaxNumBuffs = 41;

	public const int MaxNumBuffStrings = 46;

	public static string[] BuffName = new string[46];

	public static string[] BuffTip = new string[46];

	public ushort Type;

	public ushort Time;

	public bool IsPvpBuff()
	{
		switch (Type)
		{
		case 20:
		case 24:
		case 30:
		case 31:
		case 39:
			return true;
		default:
			return false;
		}
	}

	public static bool IsDebuff(int type)
	{
		switch (type)
		{
		case 20:
		case 21:
		case 22:
		case 23:
		case 24:
		case 25:
		case 28:
		case 30:
		case 31:
		case 32:
		case 33:
		case 34:
		case 35:
		case 36:
		case 37:
		case 38:
		case 39:
			return true;
		default:
			return false;
		}
	}

	public bool IsDebuff()
	{
		return IsDebuff(Type);
	}

	public bool IsHealable()
	{
		switch (Type)
		{
		case 20:
		case 21:
		case 22:
		case 23:
		case 24:
		case 25:
		case 30:
		case 31:
		case 32:
		case 33:
		case 35:
		case 36:
		case 37:
		case 38:
		case 39:
			return Time > 0;
		default:
			return false;
		}
	}

	public void Init()
	{
		Type = 0;
		Time = 0;
	}
}
