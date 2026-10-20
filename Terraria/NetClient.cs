using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace Terraria;

public sealed class NetClient
{
	public NetworkMachine Machine;

	public NetworkGamer NetGamer;

	public short ServerState;

	private bool IsPublicSlotRequest;

	public bool[] PlayerSlotsTaken;

	public bool[,] TileSectionsTaken;

	public NetClient(NetworkGamer Gamer)
	{
		Machine = Gamer.Machine;
		NetGamer = Gamer;
		ServerState = 0;
		IsPublicSlotRequest = false;
		PlayerSlotsTaken = new bool[16];
		TileSectionsTaken = new bool[127, 49];
	}

	public void RequestedPublicSlot()
	{
		Netplay.Session.PrivateGamerSlots--;
		IsPublicSlotRequest = true;
	}

	public void CanceledPublicSlot()
	{
		Netplay.Session.PrivateGamerSlots++;
		IsPublicSlotRequest = false;
	}

	public void GamerJoined(Player JoinedPlayer)
	{
		JoinedPlayer.client = this;
		int whoAmI = JoinedPlayer.WhoAmI;
		PlayerSlotsTaken[whoAmI] = true;
		PlayerSlotsTaken[whoAmI + 8] = IsPublicSlotRequest;
		IsPublicSlotRequest = false;
	}

	public bool GamerLeft(Player LeftPlayer)
	{
		int whoAmI = LeftPlayer.WhoAmI;
		PlayerSlotsTaken[whoAmI] = false;
		if (PlayerSlotsTaken[whoAmI + 8])
		{
			PlayerSlotsTaken[whoAmI + 8] = false;
			Netplay.Session.PrivateGamerSlots++;
		}
		LeftPlayer.client = null;
		return Machine.Gamers.Count == 0;
	}

	public void ResetSections()
	{
		for (int i = 0; i < Main.MaxSectionsX; i++)
		{
			for (int j = 0; j < Main.MaxSectionsY; j++)
			{
				TileSectionsTaken[i, j] = false;
			}
		}
	}

	public void ResetSections(ref Vector2i Min, ref Vector2i Max)
	{
		int num = Min.X / 40;
		int num2 = Min.Y / 30;
		int num3 = Max.X / 40;
		int num4 = Max.Y / 30;
		for (int i = num; i <= num3; i++)
		{
			for (int j = num2; j <= num4; j++)
			{
				TileSectionsTaken[i, j] = false;
			}
		}
	}

	public bool SectionRange(int Size, int FirstX, int FirstY)
	{
		int num = FirstX / 40;
		int num2 = FirstY / 30;
		if (TileSectionsTaken[num, num2])
		{
			return true;
		}
		int num3 = (FirstY + Size) / 30;
		if (TileSectionsTaken[num, num3])
		{
			return true;
		}
		num = (FirstX + Size) / 40;
		if (TileSectionsTaken[num, num2])
		{
			return true;
		}
		return TileSectionsTaken[num, num3];
	}

	public bool IsReadyToReceive(byte[] Packets)
	{
		if (ServerState < 10)
		{
			return false;
		}
		switch (Packets[0])
		{
		case 13:
		{
			Player player2 = Main.PlayerSet[Packets[1] & 7];
			if (player2.netSkip == 0)
			{
				return true;
			}
			Rectangle xYWH2 = player2.XYWH;
			xYWH2.X -= 2500;
			xYWH2.Y -= 2500;
			xYWH2.Width += 5000;
			xYWH2.Height += 5000;
			for (int num2 = Machine.Gamers.Count - 1; num2 >= 0; num2--)
			{
				Player player3 = Machine.Gamers[num2].Tag as Player;
				if (xYWH2.Intersects(player3.XYWH))
				{
					return true;
				}
			}
			return false;
		}
		case 20:
			return SectionRange(Packets[1], Packets[2] | (Packets[3] << 8), Packets[4] | (Packets[5] << 8));
		case 23:
		case 28:
		{
			NPC nPC = Main.NPCSet[Packets[1]];
			if (nPC.Life <= 0)
			{
				return true;
			}
			if (nPC.IsTownNPC)
			{
				return true;
			}
			Rectangle xYWH = nPC.XYWH;
			xYWH.X -= 2400;
			xYWH.Y -= 2400;
			xYWH.Width += 4800;
			xYWH.Height += 4800;
			for (int num = Machine.Gamers.Count - 1; num >= 0; num--)
			{
				Player player = Machine.Gamers[num].Tag as Player;
				if (xYWH.Intersects(player.XYWH))
				{
					return true;
				}
			}
			return false;
		}
		default:
			return true;
		}
	}

	public bool IsReadyToReceiveProjectile(ref Projectile NetProj)
	{
		if (ServerState == 10)
		{
			if (NetProj.type == 12)
			{
				return true;
			}
			Rectangle xYWH = NetProj.XYWH;
			xYWH.X -= 4000;
			xYWH.Y -= 4000;
			xYWH.Width += 8000;
			xYWH.Height += 8000;
			for (int num = Machine.Gamers.Count - 1; num >= 0; num--)
			{
				Player player = Machine.Gamers[num].Tag as Player;
				if (xYWH.Intersects(player.XYWH))
				{
					return true;
				}
			}
		}
		return false;
	}
}
