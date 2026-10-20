using System.IO;
using Microsoft.Xna.Framework.Net;

namespace Terraria;

public struct Sign
{
	public const int MaxNumSigns = 1000;

	public short SignX;

	public short SignY;

	public UserString SignString;

	public void Init()
	{
		SignX = -1;
		SignY = -1;
		SignString = null;
	}

	public static void KillSign(int X, int Y)
	{
		for (int i = 0; i < 1000; i++)
		{
			if (Main.SignSet[i].SignX == X && Main.SignSet[i].SignY == Y)
			{
				Main.SignSet[i].Init();
				break;
			}
		}
	}

	public static int ReadSign(int X, int Y)
	{
		int num = (Main.TileSet[X, Y].FrameX / 18) & 1;
		int num2 = Main.TileSet[X, Y].FrameY / 18;
		int num3 = X - num;
		int num4 = Y - num2;
		if (Main.TileSet[num3, num4].Type != 55 && Main.TileSet[num3, num4].Type != 85)
		{
			KillSign(num3, num4);
			return -1;
		}
		int num5 = 0;
		for (int i = 0; i < 1000; i++)
		{
			if (Main.SignSet[i].SignX < 0)
			{
				num5 = i;
			}
			else if (Main.SignSet[i].SignX == num3 && Main.SignSet[i].SignY == num4)
			{
				return i;
			}
		}
		for (int j = num5; j < 1000; j++)
		{
			if (Main.SignSet[j].SignX < 0)
			{
				Main.SignSet[j].SignX = (short)num3;
				Main.SignSet[j].SignY = (short)num4;
				Main.SignSet[j].SignString = new UserString("", NowVerified: true);
				return j;
			}
		}
		return -1;
	}

	private unsafe bool Validate()
	{
		fixed (Tile* ptr = &Main.TileSet[SignX, SignY])
		{
			if (ptr->IsActive == 0 || (ptr->Type != 55 && ptr->Type != 85))
			{
				Init();
				return false;
			}
		}
		return true;
	}

	public void SetText(UserString Text)
	{
		if (Validate())
		{
			SignString = Text;
		}
	}

	public void Read(PacketReader Input)
	{
		SignX = Input.ReadInt16();
		SignY = Input.ReadInt16();
		SignString = new UserString(Input);
	}

	public void Read(BinaryReader FileIO, int Release)
	{
		if (FileIO.ReadBoolean())
		{
			if (Release >= 49)
			{
				SignX = FileIO.ReadInt16();
				SignY = FileIO.ReadInt16();
				SignString = new UserString(FileIO);
			}
			else
			{
				SignString = FileIO.ReadString();
				SignX = FileIO.ReadInt16();
				SignY = FileIO.ReadInt16();
			}
			Validate();
		}
	}

	public void Write(BinaryWriter FileIO)
	{
		if (SignX < 0 || SignString == null)
		{
			FileIO.Write(value: false);
			return;
		}
		FileIO.Write(value: true);
		FileIO.Write(SignX);
		FileIO.Write(SignY);
		SignString.Write(FileIO);
	}
}
