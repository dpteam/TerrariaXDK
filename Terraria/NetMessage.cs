using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Net;
using Terraria.Achievements;

namespace Terraria;

public sealed class NetMessage
{
	private static readonly PacketWriter PacketOut = new PacketWriter(65536);

	public static PacketReader PacketIn = new PacketReader(65536);

	private static readonly byte[] PRIORITY = new byte[68]
	{
		1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 3, 1, 1, 3, 1, 1, 1,
		1, 1, 1, 2, 1, 1, 1, 0, 1, 1,
		1, 1, 1, 1, 1, 1, 0, 1, 1, 3,
		1, 2, 2, 1, 1, 1, 1, 1, 1, 1,
		1, 0, 1, 1, 1, 1, 1, 1, 0, 1,
		1, 1, 1, 1, 1, 1, 1, 1
	};

	/// <summary>
	/// World protection gate for edits arriving from a client. Returns true when the edit may
	/// be applied. Refusals are explained to the player at most once every few seconds, and
	/// logged once per player as well, so a griefing client cannot flood either.
	///
	/// An unknown sender (no <see cref="NetClient"/>, which should not happen for a data
	/// packet) is allowed through: breaking normal play to defend against a state we cannot
	/// even name would be the wrong trade, and the transport handshake has already passed.
	/// </summary>
	private static bool AllowRemoteEdit(NetClient sender, int x, int y, out string reason)
	{
		reason = null;
		string gamertag = null;
		try
		{
			if (sender != null && sender.NetGamer != null)
			{
				gamertag = sender.NetGamer.Gamertag;
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(gamertag))
		{
			return true;
		}
		if (Terraria.Net.WorldProtection.CanEdit(isHost: false, gamertag, x, y, out reason))
		{
			return true;
		}
		if (Terraria.Net.WorldProtection.ShouldNotify(gamertag))
		{
			Program.LogInfo("Protection", "refused " + gamertag + " at (" + x + ", " + y + "): " + reason);
			Terraria.Net.TcpPeer host = Terraria.Net.TcpPeer.CurrentHost;
			string sent;
			if (host != null)
			{
				host.SendTextTo(gamertag, reason, out sent);
			}
		}
		return false;
	}

	/// <summary>
	/// Tells a player that a move was refused, at most once every few seconds per player. A client
	/// that keeps sending bad positions must not be able to turn the refusal into a chat flood -
	/// the throttle is the same one world protection uses, so one player has one budget.
	/// </summary>
	private static void RefuseSuspiciousMove(NetClient sender, string reason)
	{
		string text = null;
		try
		{
			if (sender != null && sender.NetGamer != null)
			{
				text = sender.NetGamer.Gamertag;
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (!Terraria.Net.WorldProtection.ShouldNotify(text))
		{
			return;
		}
		Terraria.Net.TcpPeer host = Terraria.Net.TcpPeer.CurrentHost;
		string sent;
		if (host != null)
		{
			host.SendTextTo(text, Texts.Format("chat.anticheat.refused", reason), out sent);
		}
	}

	/// <summary>
	/// Tells a player that an item is not allowed, at most once every few seconds per player so
	/// a client resending its inventory cannot flood the chat or the log.
	/// </summary>
	private static void RefuseBannedItem(NetClient sender, int netId)
	{
		string text = null;
		try
		{
			if (sender != null && sender.NetGamer != null)
			{
				text = sender.NetGamer.Gamertag;
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (!Terraria.Net.WorldProtection.ShouldNotify(text))
		{
			return;
		}
		Program.LogInfo("Items", "refused item " + netId + " from " + text);
		Terraria.Net.TcpPeer host = Terraria.Net.TcpPeer.CurrentHost;
		string sent;
		if (host != null)
		{
			host.SendTextTo(text, Terraria.Net.ItemRestrictions.Notice + " (" + netId + ")", out sent);
		}
	}

	public static void CheckBytesServer()
	{
		LocalNetworkGamer gamer = Netplay.gamer;
		if (gamer == null)
		{
			return;
		}
		lock (PacketIn)
		{
			while (gamer.IsDataAvailable)
			{
				gamer.ReceiveData(PacketIn, out var sender);
				GetData((sender.Tag as Player).client);
			}
		}
	}

	public static void CheckBytesClient()
	{
		NetworkGamer sender;
		for (int num = Netplay.gamersWaitingForPlayerId.Count - 1; num >= 0; num--)
		{
			UI uI = Netplay.gamersWaitingForPlayerId[num];
			if (uI.localGamer.IsDataAvailable)
			{
				lock (PacketIn)
				{
					uI.localGamer.ReceiveData(PacketIn, out sender);
					int num2 = PacketIn.ReadByte();
					num2 = PacketIn.ReadByte();
					uI.JoinSession(num2);
					uI.localGamer.Tag = Main.PlayerSet[num2];
					SendHello(num2);
					Netplay.gamersWaitingForPlayerId.RemoveAt(num);
					Netplay.gamersWaitingToSendSpawn.Add(uI);
					if (Netplay.clientState <= Netplay.ClientState.WAITING_FOR_PLAYER_ID)
					{
						Netplay.clientState = Netplay.ClientState.WAITING_FOR_PLAYER_DATA_REQ;
					}
				}
			}
		}
		if (Netplay.gamersWaitingToSendSpawn.Count > 0 && Netplay.clientState >= Netplay.ClientState.ANNOUNCING_SPAWN_LOCATION)
		{
			UI uI2 = Netplay.gamersWaitingToSendSpawn[0];
			Netplay.gamersWaitingToSendSpawn.RemoveAt(0);
			uI2.ActivePlayer.FindSpawn();
			CreateMessage3(8, uI2.MyPlayer, uI2.ActivePlayer.SpawnX, uI2.ActivePlayer.SpawnY);
			SendMessage();
			if (Netplay.clientState == Netplay.ClientState.ANNOUNCING_SPAWN_LOCATION)
			{
				Netplay.clientState = Netplay.ClientState.WAITING_FOR_TILE_DATA;
			}
		}
		else if (Netplay.gamersWaitingToSpawn.Count > 0 && Netplay.clientState >= Netplay.ClientState.PLAYING)
		{
			Main.JoinGame(Netplay.gamersWaitingToSpawn[0]);
			Netplay.gamersWaitingToSpawn.RemoveAt(0);
		}
		LocalNetworkGamer gamer = Netplay.gamer;
		if (gamer == null)
		{
			return;
		}
		lock (PacketIn)
		{
			while (gamer.IsDataAvailable)
			{
				gamer.ReceiveData(PacketIn, out sender);
				GetData(null);
			}
		}
	}

	private static void WriteCompacted(uint value)
	{
		if (value < 128)
		{
			PacketOut.Write((byte)value);
			return;
		}
		uint num = (value & 0x7F) | 0x80 | (value >> 7 << 8);
		if (value < 16384)
		{
			PacketOut.Write((ushort)num);
			return;
		}
		PacketOut.Write((ushort)((num & 0x7FFF) | 0x8000));
		value >>= 14;
		PacketOut.Write((byte)value);
	}

	private static uint ReadCompacted()
	{
		uint num = PacketIn.ReadByte();
		if (num >= 128)
		{
			num &= 0x7F;
			num |= (uint)(PacketIn.ReadByte() << 7);
			if (num >= 16384)
			{
				num &= 0x3FFF;
				num |= (uint)(PacketIn.ReadByte() << 14);
			}
		}
		return num;
	}

	public static void CreateMessage0(int msgType)
	{
		PacketOut.Write((byte)msgType);
		switch ((NetMessageId)msgType)
		{
		case NetMessageId.CLIENT_WORLD_DATA:
		{
			PacketOut.Write(Main.GameTime.WorldTime);
			int num2 = Main.GameTime.MoonPhase << 2;
			if (Main.GameTime.DayTime)
			{
				num2 |= 1;
			}
			if (Main.GameTime.IsBloodMoon)
			{
				num2 |= 2;
			}
			PacketOut.Write((byte)num2);
			PacketOut.Write((ushort)Main.MaxTilesX);
			PacketOut.Write((ushort)Main.MaxTilesY);
			PacketOut.Write((ushort)Main.SpawnTileX);
			PacketOut.Write((ushort)Main.SpawnTileY);
			PacketOut.Write((ushort)Main.WorldSurface);
			PacketOut.Write((ushort)Main.RockLayer);
			PacketOut.Write(Main.WorldID);
			PacketOut.Write(Main.WorldTimestamp);
			num2 = (WorldGen.HasShadowOrbSmashed ? 1 : 0);
			if (NPC.HasDownedBoss1)
			{
				num2 |= 2;
			}
			if (NPC.HasDownedBoss2)
			{
				num2 |= 4;
			}
			if (NPC.HasDownedBoss3)
			{
				num2 |= 8;
			}
			if (Main.InHardMode)
			{
				num2 |= 0x10;
			}
			if (NPC.HasDownedClown)
			{
				num2 |= 0x20;
			}
			PacketOut.Write((byte)num2);
			PacketOut.Write(Main.WorldName);
			break;
		}
		case NetMessageId.CLIENT_TILE_FRAME_SECTION:
			if (Main.NetMode == 2)
			{
				GamerCollection<NetworkGamer> allGamers = Netplay.Session.AllGamers;
				int num = allGamers.Count;
				PacketOut.Write((byte)num);
				do
				{
					Player player = allGamers[--num].Tag as Player;
					PacketOut.Write(player.WhoAmI);
				}
				while (num > 0);
			}
			break;
		case (NetMessageId)57:
			PacketOut.Write(WorldGen.GoodCoverage);
			PacketOut.Write(WorldGen.EvilCoverage);
			break;
		}
	}

	public static void CreateMessage1(int msgType, int number)
	{
		PacketOut.Write((byte)msgType);
		switch ((NetMessageId)msgType)
		{
		case NetMessageId.NEVERCALLED:
			PacketOut.Write((byte)number);
			break;
		case NetMessageId.CLIENT_SAY_HELLO:
			PacketOut.Write((byte)2);
			PacketOut.Write((byte)number);
			break;
		case NetMessageId.CLIENT_KICK:
			PacketOut.Write((ushort)number);
			break;
		case NetMessageId.CLIENT_PLAYER_INFO:
			PacketOut.Write((byte)number);
			break;
		case NetMessageId.CLIENT_NAME_HEAD:
		{
			Player player2 = Main.PlayerSet[number];
			int num6 = number | (player2.hair << 4) | (player2.difficulty << 11);
			if (player2.male)
			{
				num6 |= 0x400;
			}
			PacketOut.Write((ushort)num6);
			PacketOut.Write(player2.hairColor.R);
			PacketOut.Write(player2.hairColor.G);
			PacketOut.Write(player2.hairColor.B);
			PacketOut.Write(player2.skinColor.R);
			PacketOut.Write(player2.skinColor.G);
			PacketOut.Write(player2.skinColor.B);
			PacketOut.Write(player2.eyeColor.R);
			PacketOut.Write(player2.eyeColor.G);
			PacketOut.Write(player2.eyeColor.B);
			PacketOut.Write(player2.shirtColor.R);
			PacketOut.Write(player2.shirtColor.G);
			PacketOut.Write(player2.shirtColor.B);
			PacketOut.Write(player2.underShirtColor.R);
			PacketOut.Write(player2.underShirtColor.G);
			PacketOut.Write(player2.underShirtColor.B);
			PacketOut.Write(player2.pantsColor.R);
			PacketOut.Write(player2.pantsColor.G);
			PacketOut.Write(player2.pantsColor.B);
			PacketOut.Write(player2.shoeColor.R);
			PacketOut.Write(player2.shoeColor.G);
			PacketOut.Write(player2.shoeColor.B);
			PacketOut.Write(player2.Name);
			break;
		}
		case NetMessageId.STATUS_TEXT_SIZE:
			PacketOut.Write((byte)number);
			break;
		case NetMessageId.CLIENT_PLAYER_SPAWN:
			PacketOut.Write((byte)number);
			PacketOut.Write((short)Main.PlayerSet[number].SpawnX);
			PacketOut.Write((short)Main.PlayerSet[number].SpawnY);
			break;
		case NetMessageId.CLIENT_PLAYER_CONTROLS:
		{
			Player player = Main.PlayerSet[number];
			int num5 = 0;
			if (player.controlUp)
			{
				num5 = 1;
			}
			if (player.IsControlDown)
			{
				num5 |= 2;
			}
			if (player.controlLeft)
			{
				num5 |= 4;
			}
			if (player.controlRight)
			{
				num5 |= 8;
			}
			if (player.controlJump)
			{
				num5 |= 0x10;
			}
			if (player.controlUseItem)
			{
				num5 |= 0x20;
			}
			number |= 0x20;
			if (player.direction == 1)
			{
				number |= 0x40;
			}
			if (num5 != 0)
			{
				PacketOut.Write((byte)(number | 0x80));
				PacketOut.Write((byte)num5);
			}
			else
			{
				PacketOut.Write((byte)number);
			}
			if ((number & 0x20) != 0)
			{
				PacketOut.Write(player.SelectedItem);
			}
			PacketOut.Write(player.Position);
			HalfVector2 halfVector3 = new HalfVector2(player.velocity);
			PacketOut.Write(halfVector3.PackedValue);
			if (Main.NetMode == 2 && ++player.netSkip > 2)
			{
				player.netSkip = 0;
			}
			break;
		}
		case NetMessageId.MSG_PLAYER_LIFE:
		{
			int value = number | ((Main.PlayerSet[number].statLife & 0xFFF) << 4) | (Main.PlayerSet[number].StatLifeMax << 16);
			PacketOut.Write(value);
			break;
		}
		case NetMessageId.MSG_ITEM_OWNER:
		{
			PacketOut.Write((byte)number);
			int num = Main.ItemSet[number].Owner;
			if (num < 8)
			{
				Vector2 velocity = Main.ItemSet[number].Velocity;
				if (velocity.X != 0f || velocity.Y != 0f)
				{
					num |= 0x80;
				}
				PacketOut.Write((byte)num);
				PacketOut.Write(Main.ItemSet[number].Position);
				if ((num & 0x80) != 0)
				{
					HalfVector2 halfVector = new HalfVector2(velocity);
					PacketOut.Write(halfVector.PackedValue);
				}
			}
			else
			{
				PacketOut.Write((byte)num);
			}
			break;
		}
		case NetMessageId.MSG_SYNC_NPC:
		{
			PacketOut.Write((byte)number);
			int num3 = ((Main.NPCSet[number].Active != 0) ? Main.NPCSet[number].Life : 0);
			if (num3 <= 0)
			{
				PacketOut.Write((byte)0);
				Main.NPCSet[number].NetSkip = 0;
				break;
			}
			WriteCompacted((uint)num3);
			PacketOut.Write(Main.NPCSet[number].NetID);
			PacketOut.Write(Main.NPCSet[number].Position);
			HalfVector2 halfVector2 = new HalfVector2(Main.NPCSet[number].Velocity);
			PacketOut.Write(halfVector2.PackedValue);
			PacketOut.Write((sbyte)(Main.NPCSet[number].Target | ((Main.NPCSet[number].Direction & 3) << 4) | (Main.NPCSet[number].DirectionY << 6)));
			int num4 = 0;
			float aI = Main.NPCSet[number].AI0;
			if (aI != 0f)
			{
				num4 = 1;
			}
			float aI2 = Main.NPCSet[number].AI1;
			if (aI2 != 0f)
			{
				num4 |= 2;
			}
			float aI3 = Main.NPCSet[number].AI2;
			if (aI3 != 0f)
			{
				num4 |= 4;
			}
			float aI4 = Main.NPCSet[number].AI3;
			if (aI4 != 0f)
			{
				num4 |= 8;
			}
			PacketOut.Write((byte)num4);
			if ((num4 & 1) != 0)
			{
				PacketOut.Write(aI);
			}
			if ((num4 & 2) != 0)
			{
				PacketOut.Write(aI2);
			}
			if ((num4 & 4) != 0)
			{
				PacketOut.Write(aI3);
			}
			if ((num4 & 8) != 0)
			{
				PacketOut.Write(aI4);
			}
			break;
		}
		case NetMessageId.MSG_PLAYER_VS_PLAYER:
			if (Main.PlayerSet[number].hostile)
			{
				number |= 0x80;
			}
			PacketOut.Write((byte)number);
			break;
		case NetMessageId.MSG_PLAYER_ZONE_INFO:
		{
			PacketOut.Write((byte)number);
			int num2 = 0;
			if (Main.PlayerSet[number].ZoneEvil)
			{
				num2 = 1;
			}
			if (Main.PlayerSet[number].ZoneMeteor)
			{
				num2 |= 2;
			}
			if (Main.PlayerSet[number].ZoneDungeon)
			{
				num2 |= 4;
			}
			if (Main.PlayerSet[number].ZoneJungle)
			{
				num2 |= 8;
			}
			if (Main.PlayerSet[number].zoneHoly)
			{
				num2 |= 0x10;
			}
			PacketOut.Write((byte)num2);
			break;
		}
		case NetMessageId.MSG_PLAYER_NPC_TALK:
			PacketOut.Write((byte)number);
			PacketOut.Write(Main.PlayerSet[number].TalkNPC);
			break;
		case NetMessageId.MSG_PLAYER_GUN_ROTATION:
			PacketOut.Write((byte)number);
			PacketOut.Write(Main.PlayerSet[number].itemRotation);
			PacketOut.Write(Main.PlayerSet[number].itemAnimation);
			break;
		case NetMessageId.MSG_PLAYER_MANA:
			PacketOut.Write((byte)number);
			PacketOut.Write(Main.PlayerSet[number].statMana);
			PacketOut.Write(Main.PlayerSet[number].statManaMax);
			break;
		case NetMessageId.UNK_MSG_45:
			PacketOut.Write((byte)(number | (Main.PlayerSet[number].team << 4)));
			break;
		case NetMessageId.MSG_INITIAL_SPAWN:
			PacketOut.Write((byte)number);
			break;
		case NetMessageId.MSG_PLAYER_BUFFS:
		{
			PacketOut.Write((byte)number);
			for (int j = 0; j < 10; j++)
			{
				PacketOut.Write((byte)Main.PlayerSet[number].buff[j].Type);
			}
			break;
		}
		case (NetMessageId)51:
			PacketOut.Write((byte)number);
			break;
		case (NetMessageId)54:
		{
			PacketOut.Write((byte)number);
			for (int i = 0; i < 5; i++)
			{
				uint type = Main.NPCSet[number].ActiveBuffs[i].Type;
				PacketOut.Write((byte)type);
				if (type != 0)
				{
					WriteCompacted(Main.NPCSet[number].ActiveBuffs[i].Time);
				}
			}
			break;
		}
		case (NetMessageId)56:
			PacketOut.Write((byte)number);
			PacketOut.Write(NPC.TypeNames[number]);
			break;
		case (NetMessageId)58:
			PacketOut.Write((byte)number);
			PacketOut.Write(Main.HarpNote);
			break;
		}
	}

	public unsafe static void CreateMessage2(int msgType, int number, int number2)
	{
		PacketOut.Write((byte)msgType);
		switch ((NetMessageId)msgType)
		{
		case NetMessageId.CLIENT_INVENTORY:
		{
			int num7 = number;
			int num8 = number2;
			PacketOut.Write((byte)num7);
			PacketOut.Write((byte)num8);
			int stack2;
			int netID;
			int prefixType;
			if (num8 < 49)
			{
				stack2 = Main.PlayerSet[num7].Inventory[num8].Stack;
				netID = Main.PlayerSet[num7].Inventory[num8].NetID;
				prefixType = Main.PlayerSet[num7].Inventory[num8].PrefixType;
			}
			else
			{
				num8 -= 49;
				stack2 = Main.PlayerSet[num7].armor[num8].Stack;
				netID = Main.PlayerSet[num7].armor[num8].NetID;
				prefixType = Main.PlayerSet[num7].armor[num8].PrefixType;
			}
			PacketOut.Write((byte)stack2);
			if (stack2 > 0)
			{
				PacketOut.Write((byte)prefixType);
				PacketOut.Write((short)netID);
			}
			break;
		}
		case NetMessageId.CLIENT_TILE_SECTION:
		{
			PacketOut.Write((byte)number);
			PacketOut.Write((byte)number2);
			int num9 = number * 40;
			int num10 = number2 * 30;
			fixed (Tile* tileSet = Main.TileSet)
			{
				Tile* ptr2 = null;
				uint num11 = 0u;
				for (int i = num9; i < num9 + 40; i++)
				{
					Tile* ptr3 = tileSet + (num10 + i * 1440);
					for (int num12 = 29; num12 >= 0; num12--)
					{
						if (ptr2 != null && ptr3->isTheSameAsExcludingVisibility(ref *ptr2))
						{
							num11++;
						}
						else
						{
							if (ptr2 != null)
							{
								WriteCompacted(num11);
							}
							num11 = 0u;
							ptr2 = ptr3;
							int num14;
							int num13 = (num14 = ptr3->IsActive);
							int wallType2 = ptr3->WallType;
							if (wallType2 > 0)
							{
								num14 |= 8;
							}
							int liquid2 = ptr3->Liquid;
							if (liquid2 > 0)
							{
								num14 |= ptr3->Lava;
								num14 = ((liquid2 != 255) ? (num14 | 4) : (num14 | 2));
							}
							num14 |= ptr3->wire;
							PacketOut.Write((byte)num14);
							if (num13 != 0)
							{
								int type2 = ptr3->Type;
								PacketOut.Write((byte)type2);
								if (Main.TileFrameImportant[type2])
								{
									WriteCompacted((uint)ptr3->FrameX);
									WriteCompacted((uint)ptr3->FrameY);
								}
							}
							if (wallType2 > 0)
							{
								PacketOut.Write((byte)wallType2);
							}
							if ((num14 & 4) != 0)
							{
								PacketOut.Write((byte)liquid2);
							}
						}
						ptr3++;
					}
				}
				WriteCompacted(num11);
			}
			break;
		}
		case NetMessageId.CLIENT_PLAYER_ACTIVE:
			PacketOut.Write((byte)(number | (number2 << 7)));
			break;
		case NetMessageId.CLIENT_REQ_RESYNC:
		{
			int num3 = number;
			PacketOut.Write((ushort)num3);
			PacketOut.Write((ushort)number2);
			fixed (Tile* ptr = &Main.TileSet[num3, number2])
			{
				int num5;
				int num4 = (num5 = ptr->IsActive);
				int wallType = ptr->WallType;
				if (wallType > 0)
				{
					num5 |= 4;
				}
				int num6 = ((Main.NetMode == 2) ? ptr->Liquid : 0);
				if (num6 > 0)
				{
					num5 |= 8 | ptr->Lava;
				}
				num5 |= ptr->wire;
				PacketOut.Write((byte)num5);
				if (num4 != 0)
				{
					int type = ptr->Type;
					PacketOut.Write((byte)type);
					if (Main.TileFrameImportant[type])
					{
						WriteCompacted((uint)ptr->FrameX);
						WriteCompacted((uint)ptr->FrameY);
					}
				}
				if (wallType > 0)
				{
					PacketOut.Write((byte)wallType);
				}
				if (num6 > 0)
				{
					PacketOut.Write((byte)num6);
				}
			}
			break;
		}
		case NetMessageId.MSG_SYNC_ITEM:
		{
			int num2 = 0;
			int stack = Main.ItemSet[number2].Stack;
			if (stack > 0 && Main.ItemSet[number2].Active != 0)
			{
				num2 = Main.ItemSet[number2].NetID;
			}
			if (num2 == 0 && number2 >= 200)
			{
				ClearMessage();
				break;
			}
			PacketOut.Write((byte)number2);
			PacketOut.Write((short)((num2 << 5) | number));
			if (num2 != 0)
			{
				PacketOut.Write(Main.ItemSet[number2].PrefixType);
				PacketOut.Write((byte)stack);
				PacketOut.Write(Main.ItemSet[number2].Position);
				HalfVector2 halfVector = new HalfVector2(Main.ItemSet[number2].Velocity);
				PacketOut.Write(halfVector.PackedValue);
			}
			break;
		}
		case NetMessageId.MSG_UNUSED_MELEE_STRIKE:
			PacketOut.Write((ushort)number);
			PacketOut.Write((ushort)number2);
			break;
		case NetMessageId.MSG_KILL_PROJECTILE:
			WriteCompacted((uint)number);
			PacketOut.Write((byte)number2);
			break;
		case NetMessageId.MSG_CHEST_ITEM:
		{
			WriteCompacted((uint)number);
			PacketOut.Write((byte)number2);
			int netID2 = Main.ChestSet[number].ItemSet[number2].NetID;
			PacketOut.Write((short)netID2);
			if (netID2 != 0)
			{
				PacketOut.Write(Main.ChestSet[number].ItemSet[number2].PrefixType);
				PacketOut.Write((byte)Main.ChestSet[number].ItemSet[number2].Stack);
			}
			break;
		}
		case NetMessageId.MSG_PLAYER_CHEST_VAR:
			PacketOut.Write((short)((number2 << 5) | number));
			if (number2 >= 0)
			{
				PacketOut.Write(Main.ChestSet[number2].XPos);
				PacketOut.Write(Main.ChestSet[number2].YPos);
			}
			break;
		case NetMessageId.CLIENT_REQ_KILL_TILE:
			PacketOut.Write((ushort)number);
			PacketOut.Write((ushort)number2);
			break;
		case NetMessageId.MSG_PLAYER_HEAL_EFFECT:
			PacketOut.Write((byte)number);
			WriteCompacted((uint)number2);
			break;
		case NetMessageId.UNK_MSG_43:
			PacketOut.Write((byte)number);
			PacketOut.Write((short)number2);
			break;
		case NetMessageId.UNK_MSG_47:
			PacketOut.Write((byte)number);
			WriteCompacted((uint)number2);
			PacketOut.Write(Main.SignSet[number2].SignX);
			PacketOut.Write(Main.SignSet[number2].SignY);
			Main.SignSet[number2].SignString.Write(PacketOut);
			break;
		case NetMessageId.MSG_LIQUID_UPDATE:
		{
			int num = number;
			int liquid = Main.TileSet[num, number2].Liquid;
			if (liquid > 0)
			{
				number = ((liquid >= 255) ? (number | 0x4000) : (number | 0x8000));
			}
			PacketOut.Write((ushort)number);
			PacketOut.Write((ushort)(number2 | (Main.TileSet[num, number2].Lava << 10)));
			if ((number & 0x8000) != 0)
			{
				PacketOut.Write((byte)liquid);
			}
			break;
		}
		case (NetMessageId)59:
			PacketOut.Write((ushort)number);
			PacketOut.Write((ushort)number2);
			break;
		case (NetMessageId)61:
			PacketOut.Write((byte)number);
			PacketOut.Write((short)number2);
			break;
		case (NetMessageId)64:
			PacketOut.Write((byte)number);
			PacketOut.Write((byte)number2);
			break;
		case (NetMessageId)65:
			PacketOut.Write((byte)number);
			PacketOut.Write((byte)number2);
			break;
		}
	}

	public static void CreateMessage3(int msgType, int number, int number2, int number3)
	{
		PacketOut.Write((byte)msgType);
		switch ((NetMessageId)msgType)
		{
		case NetMessageId.CLIENT_REQ_STARTING_DATA:
			PacketOut.Write((byte)number);
			PacketOut.Write((short)number2);
			PacketOut.Write((short)number3);
			break;
		case NetMessageId.MSG_TOGGLE_DOOR_STATE:
			PacketOut.Write((ushort)number);
			PacketOut.Write((ushort)number2);
			PacketOut.Write((sbyte)number3);
			break;
		case NetMessageId.UNK_MSG_20:
		{
			PacketOut.Write((byte)number);
			PacketOut.Write((ushort)number2);
			PacketOut.Write((ushort)number3);
			for (int i = number2; i < number2 + number; i++)
			{
				for (int j = number3; j < number3 + number; j++)
				{
					int num2;
					int num = (num2 = Main.TileSet[i, j].IsActive);
					int wallType = Main.TileSet[i, j].WallType;
					if (wallType > 0)
					{
						num2 |= 4;
					}
					int num3 = ((Main.NetMode == 2) ? Main.TileSet[i, j].Liquid : 0);
					if (num3 > 0)
					{
						num2 |= 8 | Main.TileSet[i, j].Lava;
					}
					num2 |= Main.TileSet[i, j].wire;
					PacketOut.Write((byte)num2);
					if (num != 0)
					{
						int type = Main.TileSet[i, j].Type;
						PacketOut.Write((byte)type);
						if (Main.TileFrameImportant[type])
						{
							WriteCompacted((uint)Main.TileSet[i, j].FrameX);
							WriteCompacted((uint)Main.TileSet[i, j].FrameY);
						}
					}
					if (wallType > 0)
					{
						PacketOut.Write((byte)wallType);
					}
					if (num3 > 0)
					{
						PacketOut.Write((byte)num3);
					}
				}
			}
			break;
		}
		case NetMessageId.MSG_REQ_CHEST_ITEM:
			PacketOut.Write((byte)number);
			PacketOut.Write((ushort)number2);
			PacketOut.Write((ushort)number3);
			break;
		case NetMessageId.UNK_MSG_46:
			PacketOut.Write((byte)number);
			PacketOut.Write((ushort)number2);
			PacketOut.Write((ushort)number3);
			break;
		case (NetMessageId)52:
			PacketOut.Write((byte)number);
			PacketOut.Write((ushort)number2);
			PacketOut.Write((ushort)number3);
			break;
		case (NetMessageId)53:
			PacketOut.Write((byte)number);
			PacketOut.Write((byte)number2);
			WriteCompacted((uint)number3);
			break;
		case (NetMessageId)55:
			PacketOut.Write((byte)number);
			PacketOut.Write((byte)number2);
			WriteCompacted((uint)number3);
			break;
		}
	}

	public static void CreateMessage4(int msgType, int number, int number2, int number3, int number4)
	{
		PacketOut.Write((byte)msgType);
		if (msgType == 60)
		{
			PacketOut.Write((byte)number);
			PacketOut.Write((short)number2);
			PacketOut.Write((short)number3);
			PacketOut.Write((byte)number4);
		}
	}

	public static void CreateMessage5(int msgType, int number, int number2, int number3, int number4, int number5 = 0)
	{
		PacketOut.Write((byte)msgType);
		switch ((NetMessageId)msgType)
		{
		case NetMessageId.MSG_WORLD_CHANGED:
			PacketOut.Write((byte)number);
			PacketOut.Write((ushort)number2);
			PacketOut.Write((ushort)number3);
			if (number <= 4)
			{
				PacketOut.Write((byte)number4);
				if (number == 1)
				{
					PacketOut.Write((byte)number5);
				}
			}
			break;
		case NetMessageId.UNK_MSG_44:
			PacketOut.Write((byte)number);
			PacketOut.Write((sbyte)number2);
			PacketOut.Write((short)number3);
			PacketOut.Write((byte)number4);
			PacketOut.Write((uint)number5);
			break;
		}
	}

	public static void SendPlayerId(NetworkGamer gamer, int playerId)
	{
		CreateMessage1(0, playerId);
		SendMessage(gamer);
	}

	public static void SendHello(int playerId)
	{
		CreateMessage1(1, playerId);
		SendMessage();
	}

	public static void SendKick(NetClient client, int textId)
	{
		CreateMessage1(2, textId);
		SendMessage(client);
	}

	public static void SendPlayerInfoRequest(NetClient client, int playerId)
	{
		CreateMessage1(3, playerId);
		SendMessage(client);
	}

	public static void SendPlayerHurt(int playerId, int dir, int dmg, bool pvp, bool critical, uint deathText)
	{
		if (pvp)
		{
			playerId |= 0x40;
		}
		if (critical)
		{
			playerId |= 0x80;
		}
		PacketOut.Write((byte)26);
		PacketOut.Write((byte)playerId);
		PacketOut.Write((sbyte)dir);
		PacketOut.Write((short)dmg);
		PacketOut.Write(deathText);
		SendMessage();
	}

	public static void SendProjectile(int number, SendDataOptions sendOptions = SendDataOptions.Reliable)
	{
		PacketOut.Write((byte)27);
		int num = 0;
		float knockBack = Main.ProjectileSet[number].knockBack;
		if (knockBack != 0f)
		{
			num = 1;
		}
		int damage = Main.ProjectileSet[number].damage;
		if (damage != 0)
		{
			num |= 2;
		}
		float ai = Main.ProjectileSet[number].ai0;
		if (ai != 0f)
		{
			num |= 4;
		}
		int ai2 = Main.ProjectileSet[number].ai1;
		if (ai2 != 0)
		{
			num |= 8;
		}
		PacketOut.Write((byte)(Main.ProjectileSet[number].owner | (num << 4)));
		PacketOut.Write(Main.ProjectileSet[number].type);
		WriteCompacted(Main.ProjectileSet[number].identity);
		PacketOut.Write(Main.ProjectileSet[number].position);
		HalfVector2 halfVector = new HalfVector2(Main.ProjectileSet[number].velocity);
		PacketOut.Write(halfVector.PackedValue);
		if ((num & 1) != 0)
		{
			HalfSingle halfSingle = new HalfSingle(knockBack);
			PacketOut.Write(halfSingle.PackedValue);
		}
		if ((num & 2) != 0)
		{
			PacketOut.Write((short)damage);
		}
		if ((num & 4) != 0)
		{
			PacketOut.Write(ai);
		}
		if ((num & 8) != 0)
		{
			PacketOut.Write((short)ai2);
		}
		SendProjectileMessage(ref Main.ProjectileSet[number], sendOptions);
	}

	public static void SendNpcHurt(int npcId, int dmg)
	{
		PacketOut.Write((byte)28);
		PacketOut.Write((byte)npcId);
		PacketOut.Write((short)dmg);
		SendMessage();
	}

	public static void SendNpcHurt(int npcId, int dmg, double kb, int dir, bool critical = false)
	{
		PacketOut.Write((byte)28);
		PacketOut.Write((byte)npcId);
		PacketOut.Write((short)dmg);
		if (dmg >= 0)
		{
			HalfSingle halfSingle = new HalfSingle((float)kb);
			PacketOut.Write(halfSingle.PackedValue);
			dir <<= 1;
			if (critical)
			{
				dir |= 1;
			}
			PacketOut.Write((sbyte)dir);
		}
		SendMessage();
	}

	public static void SendText(int textId, int r, int g, int b, int player)
	{
		if (player < 0 || Main.PlayerSet[player].client == null)
		{
			Main.NewText(Lang.MiscText[textId], r, g, b);
			if (player >= 0 && Main.PlayerSet[player].client == null)
			{
				return;
			}
		}
		PacketOut.Write((byte)18);
		PacketOut.Write((byte)r);
		PacketOut.Write((byte)g);
		PacketOut.Write((byte)b);
		PacketOut.Write((ushort)textId);
		if (player < 0)
		{
			SendMessage();
		}
		else
		{
			SendMessage(Main.PlayerSet[player].client);
		}
	}

	public static void SendText(string prefix, int textId, int r, int g, int b, int player)
	{
		if (player < 0 || Main.PlayerSet[player].client == null)
		{
			Main.NewText(prefix + Lang.MiscText[textId], r, g, b);
			if (player >= 0 && Main.PlayerSet[player].client == null)
			{
				return;
			}
		}
		PacketOut.Write((byte)37);
		PacketOut.Write((byte)r);
		PacketOut.Write((byte)g);
		PacketOut.Write((byte)b);
		PacketOut.Write((ushort)textId);
		PacketOut.Write(prefix);
		if (player < 0)
		{
			SendMessage();
		}
		else
		{
			SendMessage(Main.PlayerSet[player].client);
		}
	}

	public static void SendText(int textId, string postfix, int r, int g, int b, int player)
	{
		if (player < 0 || Main.PlayerSet[player].client == null)
		{
			Main.NewText(Lang.MiscText[textId] + postfix, r, g, b);
			if (player >= 0 && Main.PlayerSet[player].client == null)
			{
				return;
			}
		}
		PacketOut.Write((byte)38);
		PacketOut.Write((byte)r);
		PacketOut.Write((byte)g);
		PacketOut.Write((byte)b);
		PacketOut.Write((ushort)textId);
		PacketOut.Write(postfix);
		if (player < 0)
		{
			SendMessage();
		}
		else
		{
			SendMessage(Main.PlayerSet[player].client);
		}
	}

	public static void SendText(string text, int r, int g, int b, int player)
	{
		if (player < 0 || Main.PlayerSet[player].client == null)
		{
			Main.NewText(text, r, g, b);
			if (player >= 0 && Main.PlayerSet[player].client == null)
			{
				return;
			}
		}
		PacketOut.Write((byte)25);
		PacketOut.Write((byte)r);
		PacketOut.Write((byte)g);
		PacketOut.Write((byte)b);
		PacketOut.Write(text);
		if (player < 0)
		{
			SendMessage();
		}
		else
		{
			SendMessage(Main.PlayerSet[player].client);
		}
	}

	public static void SendDeathText(string name, uint deathText, int r, int g, int b)
	{
		Main.NewText(name + Lang.DeathMsgString(deathText), r, g, b);
		PacketOut.Write((byte)63);
		PacketOut.Write((byte)r);
		PacketOut.Write((byte)g);
		PacketOut.Write((byte)b);
		PacketOut.Write(deathText);
		PacketOut.Write(name);
		SendMessage();
	}

	public static void SendMessage()
	{
		MemoryStream memoryStream = (MemoryStream)PacketOut.BaseStream;
		int num = memoryStream.GetBuffer()[0];
		SendDataOptions options = (SendDataOptions)PRIORITY[num];
		if (Main.NetMode == 1 && Netplay.Session.Host != null)
		{
			try
			{
				Netplay.gamer.SendData(PacketOut, options, Netplay.Session.Host);
			}
			catch
			{
			}
		}
		else
		{
			for (int num2 = Netplay.ClientList.Count - 1; num2 >= 0; num2--)
			{
				NetClient netClient = Netplay.ClientList[num2];
				if (netClient.IsReadyToReceive(memoryStream.GetBuffer()))
				{
					try
					{
						Netplay.gamer.SendData(memoryStream.GetBuffer(), 0, PacketOut.Length, options, netClient.NetGamer);
					}
					catch
					{
					}
				}
			}
		}
		memoryStream.Position = 0L;
		memoryStream.SetLength(0L);
	}

	private static void SendProjectileMessage(ref Projectile projectile, SendDataOptions sendOptions)
	{
		MemoryStream memoryStream = (MemoryStream)PacketOut.BaseStream;
		if (Main.NetMode == 1)
		{
			try
			{
				Netplay.gamer.SendData(memoryStream.GetBuffer(), 0, PacketOut.Length, sendOptions, Netplay.Session.Host);
			}
			catch
			{
			}
		}
		else
		{
			for (int num = Netplay.ClientList.Count - 1; num >= 0; num--)
			{
				NetClient netClient = Netplay.ClientList[num];
				if (netClient.IsReadyToReceiveProjectile(ref projectile))
				{
					try
					{
						Netplay.gamer.SendData(memoryStream.GetBuffer(), 0, PacketOut.Length, sendOptions, netClient.NetGamer);
					}
					catch
					{
					}
				}
			}
		}
		memoryStream.Position = 0L;
		memoryStream.SetLength(0L);
	}

	public static void SendMessage(NetClient client)
	{
		MemoryStream memoryStream = (MemoryStream)PacketOut.BaseStream;
		int num = memoryStream.GetBuffer()[0];
		SendDataOptions sendDataOptions = (SendDataOptions)PRIORITY[num];
		switch (sendDataOptions)
		{
		case SendDataOptions.None:
			sendDataOptions = SendDataOptions.Reliable;
			break;
		case SendDataOptions.InOrder:
			sendDataOptions = SendDataOptions.ReliableInOrder;
			break;
		}
		try
		{
			Netplay.gamer.SendData(PacketOut, sendDataOptions, client.NetGamer);
		}
		catch
		{
			memoryStream.Position = 0L;
			memoryStream.SetLength(0L);
		}
	}

	public static void SendMessageNoClear(NetClient client)
	{
		MemoryStream memoryStream = (MemoryStream)PacketOut.BaseStream;
		int num = memoryStream.GetBuffer()[0];
		SendDataOptions sendDataOptions = (SendDataOptions)PRIORITY[num];
		switch (sendDataOptions)
		{
		case SendDataOptions.None:
			sendDataOptions = SendDataOptions.Reliable;
			break;
		case SendDataOptions.InOrder:
			sendDataOptions = SendDataOptions.ReliableInOrder;
			break;
		}
		try
		{
			Netplay.gamer.SendData(memoryStream.GetBuffer(), 0, PacketOut.Length, sendDataOptions, client.NetGamer);
		}
		catch
		{
		}
	}

	public static void ClearMessage()
	{
		MemoryStream memoryStream = (MemoryStream)PacketOut.BaseStream;
		memoryStream.Position = 0L;
		memoryStream.SetLength(0L);
	}

	public static void SendMessageIgnore(NetClient ignoreClient)
	{
		MemoryStream memoryStream = (MemoryStream)PacketOut.BaseStream;
		int num = memoryStream.GetBuffer()[0];
		SendDataOptions options = (SendDataOptions)PRIORITY[num];
		for (int num2 = Netplay.ClientList.Count - 1; num2 >= 0; num2--)
		{
			NetClient netClient = Netplay.ClientList[num2];
			if (netClient != ignoreClient && netClient.IsReadyToReceive(memoryStream.GetBuffer()))
			{
				try
				{
					Netplay.gamer.SendData(memoryStream.GetBuffer(), 0, PacketOut.Length, options, netClient.NetGamer);
				}
				catch
				{
				}
			}
		}
		memoryStream.Position = 0L;
		memoryStream.SetLength(0L);
	}

	public static void SendMessage(NetworkGamer gamer)
	{
		MemoryStream memoryStream = (MemoryStream)PacketOut.BaseStream;
		int num = memoryStream.GetBuffer()[0];
		SendDataOptions options = (SendDataOptions)PRIORITY[num];
		try
		{
			Netplay.gamer.SendData(PacketOut, options, gamer);
		}
		catch
		{
			memoryStream.Position = 0L;
			memoryStream.SetLength(0L);
		}
	}

	private static void EchoMessage(NetClient sender)
	{
		MemoryStream memoryStream = (MemoryStream)PacketIn.BaseStream;
		int num = memoryStream.GetBuffer()[0];
		SendDataOptions options = (SendDataOptions)PRIORITY[num];
		for (int num2 = Netplay.ClientList.Count - 1; num2 >= 0; num2--)
		{
			NetClient netClient = Netplay.ClientList[num2];
			if (netClient != sender && netClient.IsReadyToReceive(memoryStream.GetBuffer()))
			{
				try
				{
					Netplay.gamer.SendData(memoryStream.GetBuffer(), 0, PacketIn.Length, options, netClient.NetGamer);
				}
				catch
				{
				}
			}
		}
	}

	private static void EchoProjectileMessage(NetClient sender, ref Projectile projectile)
	{
		MemoryStream memoryStream = (MemoryStream)PacketIn.BaseStream;
		int num = memoryStream.GetBuffer()[0];
		SendDataOptions options = (SendDataOptions)PRIORITY[num];
		for (int num2 = Netplay.ClientList.Count - 1; num2 >= 0; num2--)
		{
			NetClient netClient = Netplay.ClientList[num2];
			if (netClient != sender && netClient.IsReadyToReceiveProjectile(ref projectile))
			{
				try
				{
					Netplay.gamer.SendData(memoryStream.GetBuffer(), 0, PacketIn.Length, options, netClient.NetGamer);
				}
				catch
				{
				}
			}
		}
	}

	public unsafe static void GetData(NetClient sender)
	{
		int num = PacketIn.ReadByte();
		if (Main.NetMode == 1)
		{
			UI mainUI = UI.MainUI;
			if (Netplay.clientStatusMax > 0)
			{
				Netplay.clientStatusCount++;
			}
			switch (num)
			{
			case 2:
				Netplay.PlayDisconnect = true;
				mainUI.statusText = Lang.MiscText[PacketIn.ReadUInt16()];
				break;
			case 3:
			{
				if (Netplay.clientState == Netplay.ClientState.WAITING_FOR_PLAYER_DATA_REQ)
				{
					Netplay.clientState = Netplay.ClientState.RECEIVED_PLAYER_DATA_REQ;
				}
				int number = PacketIn.ReadByte();
				CreateMessage1(4, number);
				SendMessage();
				CreateMessage1(16, number);
				SendMessage();
				CreateMessage1(42, number);
				SendMessage();
				CreateMessage1(50, number);
				SendMessage();
				for (int k = 0; k < 49; k++)
				{
					CreateMessage2(5, number, k);
					SendMessage();
				}
				for (int l = 0; l < 11; l++)
				{
					CreateMessage2(5, number, l + 49);
					SendMessage();
				}
				CreateMessage0(6);
				SendMessage();
				break;
			}
			case 7:
			{
				Main.GameTime.WorldTime = PacketIn.ReadSingle();
				int num3 = PacketIn.ReadByte();
				Main.GameTime.DayTime = (num3 & 1) != 0;
				Main.GameTime.IsBloodMoon = (num3 & 2) != 0;
				Main.GameTime.MoonPhase = (byte)(num3 >> 2);
				Main.MaxTilesX = PacketIn.ReadInt16();
				Main.MaxTilesY = PacketIn.ReadInt16();
				Main.SpawnTileX = PacketIn.ReadInt16();
				Main.SpawnTileY = PacketIn.ReadInt16();
				Main.WorldSurface = PacketIn.ReadInt16();
				Main.WorldSurfacePixels = Main.WorldSurface << 4;
				Main.RockLayer = PacketIn.ReadInt16();
				Main.RockLayerPixels = Main.RockLayer << 4;
				num3 = PacketIn.ReadInt32();
				if (num3 != Main.WorldID)
				{
					Main.WorldID = num3;
					Main.CheckWorldId = true;
				}
				num3 = PacketIn.ReadInt32();
				if (num3 != Main.WorldTimestamp)
				{
					Main.WorldTimestamp = num3;
					Main.CheckWorldId = true;
				}
				num3 = PacketIn.ReadByte();
				WorldGen.HasShadowOrbSmashed = (num3 & 1) != 0;
				NPC.HasDownedBoss1 = (num3 & 2) != 0;
				NPC.HasDownedBoss2 = (num3 & 4) != 0;
				NPC.HasDownedBoss3 = (num3 & 8) != 0;
				Main.InHardMode = (num3 & 0x10) != 0;
				NPC.HasDownedClown = (num3 & 0x20) != 0;
				Main.WorldName = PacketIn.ReadString();
				WorldGen.setWorldSize();
				WorldGen.UpdateMagmaLayerPos();
				if (Netplay.clientState <= Netplay.ClientState.WAITING_FOR_WORLD_INFO)
				{
					Netplay.clientState = Netplay.ClientState.ANNOUNCING_SPAWN_LOCATION;
					UI.MainUI.NextProgressStep(Lang.MenuText[74]);
				}
				break;
			}
			case 9:
				Netplay.clientStatusMax += PacketIn.ReadByte();
				break;
			case 10:
			{
				int num19 = PacketIn.ReadByte() * 40;
				int num20 = PacketIn.ReadByte() * 30;
				fixed (Tile* tileSet = Main.TileSet)
				{
					uint num21 = 0u;
					Tile* ptr = null;
					for (int m = num19; m < num19 + 40; m++)
					{
						Tile* ptr2 = tileSet + (num20 + m * 1440);
						for (int num22 = 29; num22 >= 0; num22--)
						{
							if (num21 != 0)
							{
								num21--;
								*ptr2 = *ptr;
							}
							else
							{
								ptr = ptr2;
								int num23 = PacketIn.ReadByte();
								int num24 = num23 & 1;
								if (num24 != 0)
								{
									int type = ptr2->Type;
									int num25 = (ptr2->Type = PacketIn.ReadByte());
									if (Main.TileFrameImportant[num25])
									{
										ptr2->FrameX = (short)ReadCompacted();
										ptr2->FrameY = (short)ReadCompacted();
									}
									else if (num25 != type || ptr2->IsActive == 0)
									{
										ptr2->FrameX = -1;
										ptr2->FrameY = -1;
									}
								}
								ptr2->IsActive = (byte)num24;
								ptr2->WallType = (byte)(((num23 & 8) != 0) ? PacketIn.ReadByte() : 0);
								if ((num23 & 2) != 0)
								{
									ptr2->Liquid = byte.MaxValue;
								}
								else
								{
									ptr2->Liquid = (byte)(((num23 & 4) != 0) ? PacketIn.ReadByte() : 0);
								}
								ptr2->wire = num23 & 0x10;
								ptr2->Lava = (byte)(num23 & 0x20);
								num21 = ReadCompacted();
							}
							ptr2++;
						}
					}
				}
				WorldGen.SectionTileFrame(num19, num20);
				break;
			}
			case 11:
			{
				int num10 = PacketIn.ReadByte();
				GamerCollection<NetworkGamer> allGamers = Netplay.Session.AllGamers;
				if (num10 != allGamers.Count)
				{
					CreateMessage0(11);
					SendMessage();
					break;
				}
				do
				{
					NetworkGamer networkGamer = allGamers[--num10];
					int num11 = PacketIn.ReadByte();
					Player tag = Main.PlayerSet[num11];
					_ = networkGamer.IsLocal;
					networkGamer.Tag = tag;
				}
				while (num10 > 0);
				break;
			}
			case 14:
			{
				int num5 = PacketIn.ReadByte();
				int num6 = num5 & 0x80;
				num5 ^= num6;
				Player player = Main.PlayerSet[num5];
				if (num6 != 0)
				{
					if (player.Active == 0)
					{
						player.Init();
						player.Active = 1;
					}
					Netplay.SetAsRemotePlayerSlot(num5);
				}
				else
				{
					player.Active = 0;
				}
				break;
			}
			case 23:
			{
				int num13 = PacketIn.ReadByte();
				int num14 = (int)ReadCompacted();
				Main.NPCSet[num13].Life = num14;
				if (num14 == 0)
				{
					Main.NPCSet[num13].Active = 0;
					break;
				}
				int num15 = PacketIn.ReadInt16();
				if (Main.NPCSet[num13].Active == 0 || Main.NPCSet[num13].NetID != num15)
				{
					Main.NPCSet[num13].NetDefaults(num15);
				}
				Main.NPCSet[num13].Position = PacketIn.ReadVector2();
				Main.NPCSet[num13].XYWH.X = (int)Main.NPCSet[num13].Position.X;
				Main.NPCSet[num13].XYWH.Y = (int)Main.NPCSet[num13].Position.Y;
				HalfVector2 halfVector = new HalfVector2
				{
					PackedValue = PacketIn.ReadUInt32()
				};
				Main.NPCSet[num13].Velocity = halfVector.ToVector2();
				int num16 = PacketIn.ReadSByte();
				Main.NPCSet[num13].Target = (byte)(num16 & 0xF);
				Main.NPCSet[num13].Direction = (sbyte)(num16 << 26 >> 30);
				Main.NPCSet[num13].DirectionY = (sbyte)(num16 >> 6);
				int num17 = PacketIn.ReadByte();
				Main.NPCSet[num13].AI0 = (((num17 & 1) != 0) ? PacketIn.ReadSingle() : 0f);
				Main.NPCSet[num13].AI1 = (((num17 & 2) != 0) ? PacketIn.ReadSingle() : 0f);
				Main.NPCSet[num13].AI2 = (((num17 & 4) != 0) ? PacketIn.ReadSingle() : 0f);
				Main.NPCSet[num13].AI3 = (((num17 & 8) != 0) ? PacketIn.ReadSingle() : 0f);
				break;
			}
			case 18:
			case 25:
			case 37:
			case 38:
			case 63:
			{
				byte r = PacketIn.ReadByte();
				byte g = PacketIn.ReadByte();
				byte b = PacketIn.ReadByte();
				uint num18 = 0u;
				string text;
				if (num == 18)
				{
					num18 = PacketIn.ReadUInt16();
					text = Lang.MiscText[num18];
				}
				else if (num == 63)
				{
					num18 = PacketIn.ReadUInt32();
					text = PacketIn.ReadString();
					text += Lang.DeathMsgString(num18);
				}
				else
				{
					if (num != 25)
					{
						num18 = PacketIn.ReadUInt16();
					}
					text = PacketIn.ReadString();
					switch (num)
					{
					case 37:
						text += Lang.MiscText[num18];
						break;
					case 38:
						text = Lang.MiscText[num18] + text;
						break;
					}
				}
				Main.NewText(text, r, g, b);
				break;
			}
			case 49:
			{
				int num12 = PacketIn.ReadByte();
				for (int j = 0; j < 4; j++)
				{
					UI uI = Main.UIInstance[j];
					if (uI.localGamer != null && uI.MyPlayer == num12)
					{
						Netplay.gamersWaitingToSpawn.Add(uI);
						break;
					}
				}
				if (Netplay.clientState >= Netplay.ClientState.WAITING_FOR_TILE_DATA)
				{
					Netplay.clientState = Netplay.ClientState.PLAYING;
				}
				break;
			}
			case 54:
			{
				int num8 = PacketIn.ReadByte();
				for (int i = 0; i < 5; i++)
				{
					uint num9 = PacketIn.ReadByte();
					Main.NPCSet[num8].ActiveBuffs[i].Type = (ushort)num9;
					Main.NPCSet[num8].ActiveBuffs[i].Time = (ushort)((num9 != 0) ? ReadCompacted() : 0u);
				}
				break;
			}
			case 56:
			{
				int num7 = PacketIn.ReadByte();
				NPC.TypeNames[num7] = PacketIn.ReadString();
				break;
			}
			case 57:
				WorldGen.GoodCoverage = PacketIn.ReadByte();
				WorldGen.EvilCoverage = PacketIn.ReadByte();
				break;
			case 64:
			{
				int num4 = PacketIn.ReadByte();
				UI ui2 = Main.PlayerSet[num4].ui;
				num4 = PacketIn.ReadByte();
				ui2?.SetTriggerState((Trigger)num4);
				break;
			}
			case 65:
			{
				int num2 = PacketIn.ReadByte();
				UI ui = Main.PlayerSet[num2].ui;
				num2 = PacketIn.ReadByte();
				ui?.Statistics.IncreaseStat((StatisticEntry)num2);
				break;
			}
			}
		}
		else if (Main.NetMode == 2)
		{
			switch (num)
			{
			case 1:
			{
				int num35 = PacketIn.ReadByte();
				if (sender.ServerState == 0)
				{
					if (num35 != 2)
					{
						num35 = PacketIn.ReadByte();
						BootPlayer(num35, 22);
						break;
					}
					sender.ServerState = 1;
				}
				num35 = PacketIn.ReadByte();
				SendPlayerInfoRequest(sender, num35);
				break;
			}
			case 6:
				if (sender.ServerState == 1)
				{
					sender.ServerState = 2;
				}
				CreateMessage0(7);
				SendMessage(sender);
				break;
			case 8:
			{
				int number2 = PacketIn.ReadByte();
				int num28 = PacketIn.ReadInt16();
				int num29 = PacketIn.ReadInt16();
				bool flag2 = num28 >= 0 && num29 >= 0;
				if (flag2)
				{
					if (num28 < 10 || num28 > Main.MaxTilesX - 10)
					{
						flag2 = false;
					}
					else if (num29 < 10 || num29 > Main.MaxTilesY - 10)
					{
						flag2 = false;
					}
				}
				int num30 = 9;
				if (flag2)
				{
					num30 <<= 1;
				}
				if (sender.ServerState == 2)
				{
					sender.ServerState = 3;
				}
				CreateMessage1(9, num30);
				SendMessage(sender);
				int sectionX = Main.SpawnTileX / 40;
				int sectionY = Main.SpawnTileY / 30;
				SendSectionSquare(sender, sectionX, sectionY, 3);
				if (flag2)
				{
					num28 /= 40;
					num29 /= 30;
					SendSectionSquare(sender, num28, num29, 3);
				}
				for (int num31 = 0; num31 < 200; num31++)
				{
					if (Main.ItemSet[num31].Active != 0)
					{
						CreateMessage2(21, UI.MainUI.MyPlayer, num31);
						SendMessage(sender);
						CreateMessage1(22, num31);
						SendMessage(sender);
					}
				}
				for (int num32 = 0; num32 < 196; num32++)
				{
					NPC nPC = Main.NPCSet[num32];
					if (nPC.Active != 0)
					{
						if (nPC.IsTownNPC)
						{
							// Sections are 200 tiles wide (3200 px). The X coordinate must also be
							// divided by the tile width, and the section size for a 3x3 block is the
							// third argument - a literal 640 on both lines was the *pixel* size of a
							// 40-tile block, which is neither.
							int sectionX2 = nPC.XYWH.X / 3200;
							int sectionY2 = nPC.XYWH.Y / 3200;
							SendSectionSquare(sender, sectionX2, sectionY2, 3);
						}
						CreateMessage1(23, num32);
						SendMessage(sender);
					}
				}
				CreateMessage1(56, 17);
				SendMessage(sender);
				CreateMessage1(56, 18);
				SendMessage(sender);
				CreateMessage1(56, 19);
				SendMessage(sender);
				CreateMessage1(56, 20);
				SendMessage(sender);
				CreateMessage1(56, 22);
				SendMessage(sender);
				CreateMessage1(56, 38);
				SendMessage(sender);
				CreateMessage1(56, 54);
				SendMessage(sender);
				CreateMessage1(56, 107);
				SendMessage(sender);
				CreateMessage1(56, 108);
				SendMessage(sender);
				CreateMessage1(56, 124);
				SendMessage(sender);
				CreateMessage0(57);
				SendMessage(sender);
				CreateMessage1(49, number2);
				SendMessage(sender);
				break;
			}
			case 11:
				CreateMessage0(11);
				SendMessage(sender);
				break;
			case 31:
			{
				int num36 = PacketIn.ReadByte();
				Player player2 = Main.PlayerSet[num36];
				ushort x = PacketIn.ReadUInt16();
				int y = PacketIn.ReadUInt16();
				int num37 = Chest.FindChest(x, y);
				if (num37 >= 0 && Chest.UsingChest(num37) == -1)
				{
					for (int num38 = 0; num38 < 20; num38++)
					{
						CreateMessage2(32, num37, num38);
						SendMessage(sender);
					}
					CreateMessage2(33, num36, num37);
					SendMessage(sender);
					player2.PlayerChest = (short)num37;
				}
				break;
			}
			case 34:
			{
				int num33 = PacketIn.ReadUInt16();
				int num34 = PacketIn.ReadUInt16();
				if (Main.NetMode == 2)
				{
					// Breaking a chest is still a world edit.
					string reason3;
					if (!AllowRemoteEdit(sender, num33, num34, out reason3))
					{
						break;
					}
				}
				if (Main.TileSet[num33, num34].Type == 21 && WorldGen.KillTile(num33, num34))
				{
					CreateMessage5(17, 0, num33, num34, 0);
					SendMessage();
				}
				break;
			}
			case 46:
			{
				int number3 = PacketIn.ReadByte();
				ushort x2 = PacketIn.ReadUInt16();
				int y2 = PacketIn.ReadUInt16();
				int num40 = Sign.ReadSign(x2, y2);
				if (num40 >= 0)
				{
					CreateMessage2(47, number3, num40);
					SendMessage(sender);
				}
				break;
			}
			case 53:
			{
				int num39 = PacketIn.ReadByte();
				int buffType = PacketIn.ReadByte();
				int time = (int)ReadCompacted();
				Main.NPCSet[num39].AddBuff(buffType, time, quiet: true);
				CreateMessage1(54, num39);
				SendMessage();
				break;
			}
			case 61:
			{
				int num26 = PacketIn.ReadByte();
				int num27 = PacketIn.ReadInt16();
				if (num27 < 0)
				{
					if (Main.InvasionType == 0)
					{
						Main.InvasionDelay = 0;
						Main.StartInvasion(-num27);
					}
					break;
				}
				bool flag = true;
				for (int n = 0; n < 196; n++)
				{
					if (Main.NPCSet[n].Type == num27 && Main.NPCSet[n].Active != 0)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					NPC.SpawnOnPlayer(Main.PlayerSet[num26], num27);
				}
				break;
			}
			case 62:
				NPC.SpawnSkeletron();
				break;
			case 66:
				sender.RequestedPublicSlot();
				break;
			case 67:
				sender.CanceledPublicSlot();
				break;
			}
		}
		switch (num)
		{
		case 4:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num99 = PacketIn.ReadUInt16();
			Player player13 = Main.PlayerSet[num99 & 7];
			num99 >>= 4;
			player13.hair = (byte)(num99 & 0x3F);
			num99 >>= 6;
			player13.male = (num99 & 1) != 0;
			num99 >>= 1;
			player13.difficulty = (byte)num99;
			player13.hairColor.R = PacketIn.ReadByte();
			player13.hairColor.G = PacketIn.ReadByte();
			player13.hairColor.B = PacketIn.ReadByte();
			player13.skinColor.R = PacketIn.ReadByte();
			player13.skinColor.G = PacketIn.ReadByte();
			player13.skinColor.B = PacketIn.ReadByte();
			player13.eyeColor.R = PacketIn.ReadByte();
			player13.eyeColor.G = PacketIn.ReadByte();
			player13.eyeColor.B = PacketIn.ReadByte();
			player13.shirtColor.R = PacketIn.ReadByte();
			player13.shirtColor.G = PacketIn.ReadByte();
			player13.shirtColor.B = PacketIn.ReadByte();
			player13.underShirtColor.R = PacketIn.ReadByte();
			player13.underShirtColor.G = PacketIn.ReadByte();
			player13.underShirtColor.B = PacketIn.ReadByte();
			player13.pantsColor.R = PacketIn.ReadByte();
			player13.pantsColor.G = PacketIn.ReadByte();
			player13.pantsColor.B = PacketIn.ReadByte();
			player13.shoeColor.R = PacketIn.ReadByte();
			player13.shoeColor.G = PacketIn.ReadByte();
			player13.shoeColor.B = PacketIn.ReadByte();
			player13.oldName = player13.Name;
			player13.Name = PacketIn.ReadString();
			break;
		}
		case 5:
		{
			int num119 = PacketIn.ReadByte();
			Player player18 = Main.PlayerSet[num119];
			int num120 = PacketIn.ReadByte();
			int num121 = PacketIn.ReadByte();
			int prefix3 = 0;
			int type5 = 0;
			if (num121 > 0)
			{
				prefix3 = PacketIn.ReadByte();
				type5 = PacketIn.ReadInt16();
			}
			if (Main.NetMode == 2 && num121 > 0 && Terraria.Net.ItemRestrictions.IsBanned(type5))
			{
				// The host is authoritative for what exists in the world: refuse the slot and say
				// why. Returning here also skips the echo, so the item never reaches other
				// players - relaying it would defeat the ban entirely.
				RefuseBannedItem(sender, type5);
				break;
			}
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			if (num120 < 49)
			{
				if (num121 > 0)
				{
					player18.Inventory[num120].NetDefaults(type5, num121);
					player18.Inventory[num120].SetPrefix(prefix3);
				}
				else
				{
					player18.Inventory[num120].Init();
				}
				break;
			}
			num120 -= 49;
			if (num121 > 0)
			{
				player18.armor[num120].NetDefaults(type5, num121);
				player18.armor[num120].SetPrefix(prefix3);
			}
			else
			{
				player18.armor[num120].Init();
			}
			break;
		}
		case 12:
		{
			int num72 = PacketIn.ReadByte();
			Player player8 = Main.PlayerSet[num72];
			if (Main.NetMode == 1)
			{
				if (player8.ui != null)
				{
					player8.ui.SetPlayer(null);
				}
			}
			else
			{
				EchoMessage(sender);
			}
			player8.SpawnX = PacketIn.ReadInt16();
			player8.SpawnY = PacketIn.ReadInt16();
			player8.Spawn();
			if (Main.NetMode == 2 && sender.ServerState >= 3)
			{
				if (sender.ServerState == 3)
				{
					sender.ServerState = 10;
					greetPlayer(num72);
					syncPlayers();
				}
				else
				{
					SyncPlayer(num72);
				}
			}
			break;
		}
		case 13:
		{
			int num86 = PacketIn.ReadByte();
			Player player10 = Main.PlayerSet[num86 & 0x1F];
			// The move has to be judged before anything is applied: refusing after assignment
			// would leave the host holding a position it just decided not to trust, and the
			// echo below would still broadcast it.
			//
			// Read into locals first, so the validation sees the whole packet and the apply
			// happens only once. In single player and on clients the check does nothing at all
			// (it is gated on hosting), which is why this stays a small change here.
			sbyte b = (sbyte)(((num86 & 0x40) != 0) ? 1 : (-1));
			int num87 = (((num86 & 0x80) != 0) ? PacketIn.ReadByte() : 0);
			sbyte b2 = player10.SelectedItem;
			bool flag13 = false;
			if ((num86 & 0x20) != 0)
			{
				b2 = PacketIn.ReadSByte();
				flag13 = true;
			}
			Vector2 vector = PacketIn.ReadVector2();
			HalfVector2 halfVector = default(HalfVector2);
			halfVector.PackedValue = PacketIn.ReadUInt32();
			if (Main.NetMode == 2 && sender != null && Terraria.Net.AntiCheat.Enabled)
			{
				// A dead player is about to respawn at spawn, and a grappling hook pulls far faster
				// than walking; both are legitimate and both are large jumps.
				string reason;
				if (!Terraria.Net.AntiCheat.Allow(num86 & 0x1F, System.Environment.TickCount, player10.Position, vector,
					player10.grappling[0] >= 0, player10.IsDead, Main.MaxTilesX << 4, Main.MaxTilesY << 4, out reason))
				{
					// Refused: tell the sender (throttled, so a resending client cannot flood the
					// chat) and stop. No assignment, no echo - for everyone else the packet simply
					// does not exist.
					RefuseSuspiciousMove(sender, reason);
					break;
				}
			}
			player10.direction = b;
			player10.controlUp = (num87 & 1) != 0;
			player10.IsControlDown = (num87 & 2) != 0;
			player10.controlLeft = (num87 & 4) != 0;
			player10.controlRight = (num87 & 8) != 0;
			player10.controlJump = (num87 & 0x10) != 0;
			player10.controlUseItem = (num87 & 0x20) != 0;
			if (flag13)
			{
				player10.SelectedItem = b2;
			}
			player10.Position = vector;
			player10.XYWH.X = (int)player10.Position.X;
			player10.XYWH.Y = (int)player10.Position.Y;
			player10.velocity = halfVector.ToVector2();
			player10.fallStart = (short)(player10.XYWH.Y >> 4);
			if (Main.NetMode == 2 && sender.ServerState == 10)
			{
				EchoMessage(sender);
			}
			break;
		}
		case 15:
		{
			int num55 = PacketIn.ReadUInt16();
			int num56 = PacketIn.ReadUInt16();
			int num57 = PacketIn.ReadByte();
			int isActive = Main.TileSet[num55, num56].IsActive;
			Main.TileSet[num55, num56].IsActive = (byte)(num57 & 1);
			Main.TileSet[num55, num56].wire = num57 & 0x10;
			if (Main.TileSet[num55, num56].IsActive != 0)
			{
				int type2 = Main.TileSet[num55, num56].Type;
				int num58 = (Main.TileSet[num55, num56].Type = PacketIn.ReadByte());
				if (Main.TileFrameImportant[num58])
				{
					Main.TileSet[num55, num56].FrameX = (short)ReadCompacted();
					Main.TileSet[num55, num56].FrameY = (short)ReadCompacted();
				}
				else if (isActive == 0 || num58 != type2)
				{
					Main.TileSet[num55, num56].FrameX = -1;
					Main.TileSet[num55, num56].FrameY = -1;
				}
			}
			if ((num57 & 4) != 0)
			{
				Main.TileSet[num55, num56].WallType = PacketIn.ReadByte();
			}
			if (Main.NetMode != 2 && (num57 & 8) != 0)
			{
				Main.TileSet[num55, num56].Lava = (byte)(num57 & 0x20);
				Main.TileSet[num55, num56].Liquid = PacketIn.ReadByte();
			}
			WorldGen.TileFrame(num55, num56);
			WorldGen.WallFrame(num55, num56);
			if (Main.NetMode == 2)
			{
				CreateMessage2(15, num55, num56);
				SendMessageIgnore(sender);
			}
			break;
		}
		case 16:
		{
			int num50 = PacketIn.ReadInt32();
			Player player3 = Main.PlayerSet[num50 & 0xF];
			int num51 = num50 << 16 >> 20;
			player3.statLife = (short)num51;
			if (num51 <= 0)
			{
				player3.IsDead = true;
			}
			player3.StatLifeMax = (short)(num50 >> 16);
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			break;
		}
		case 17:
		{
			int num94 = PacketIn.ReadByte();
			int num95 = PacketIn.ReadUInt16();
			int num96 = PacketIn.ReadUInt16();
			int num97 = ((num94 > 4) ? 1 : PacketIn.ReadByte());
			bool flag5 = num97 == 1;
			int num98 = 0;
			if (Main.NetMode == 2)
			{
				// World protection, before anything is applied: a refused edit must not reach
				// the world, and must not be relayed to the other players either.
				string reason;
				if (!AllowRemoteEdit(sender, num95, num96, out reason))
				{
					break;
				}
				if (!flag5 && (num94 == 0 || num94 == 2 || num94 == 4) && !sender.TileSectionsTaken[num95 / 40, num96 / 30])
				{
					flag5 = true;
				}
				EchoMessage(sender);
			}
			switch (num94)
			{
			case 0:
				WorldGen.KillTile(num95, num96, flag5);
				break;
			case 1:
				num98 = PacketIn.ReadByte();
				WorldGen.PlaceTile(num95, num96, num97, ToMute: false, IsForced: true, -1, num98);
				if (num97 == 53 && Main.NetMode == 2)
				{
					SendTile(num95, num96);
				}
				break;
			case 2:
				WorldGen.KillWall(num95, num96, flag5);
				break;
			case 3:
				WorldGen.PlaceWall(num95, num96, num97);
				break;
			case 4:
				WorldGen.KillTile(num95, num96, flag5, EffectOnly: false, noItem: true);
				break;
			case 5:
				WorldGen.PlaceWire(num95, num96);
				break;
			case 6:
				WorldGen.KillWire(num95, num96);
				break;
			}
			break;
		}
		case 19:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			ushort i4 = PacketIn.ReadUInt16();
			int j4 = PacketIn.ReadUInt16();
			int direction = PacketIn.ReadSByte();
			WorldGen.OpenDoor(i4, j4, direction);
			break;
		}
		case 20:
		{
			int num79 = PacketIn.ReadByte();
			int num80 = PacketIn.ReadUInt16();
			int num81 = PacketIn.ReadUInt16();
			if (Main.NetMode == 2)
			{
				// Bulk tile data is another way to write the world, so it is gated the same way.
				// The box covers the whole section; refused as a unit so a griefing client
				// cannot slip tiles in at the edges.
				string reason2;
				if (!AllowRemoteEdit(sender, num80 + num79 / 2, num81 + num79 / 2, out reason2))
				{
					break;
				}
			}
			for (int num82 = num80; num82 < num80 + num79; num82++)
			{
				for (int num83 = num81; num83 < num81 + num79; num83++)
				{
					int num84 = PacketIn.ReadByte();
					int isActive2 = Main.TileSet[num82, num83].IsActive;
					Main.TileSet[num82, num83].IsActive = (byte)(num84 & 1);
					Main.TileSet[num82, num83].wire = num84 & 0x10;
					if (Main.TileSet[num82, num83].IsActive != 0)
					{
						int type4 = Main.TileSet[num82, num83].Type;
						int num85 = (Main.TileSet[num82, num83].Type = PacketIn.ReadByte());
						if (Main.TileFrameImportant[num85])
						{
							Main.TileSet[num82, num83].FrameX = (short)ReadCompacted();
							Main.TileSet[num82, num83].FrameY = (short)ReadCompacted();
						}
						else if (isActive2 == 0 || num85 != type4)
						{
							Main.TileSet[num82, num83].FrameX = -1;
							Main.TileSet[num82, num83].FrameY = -1;
						}
					}
					if ((num84 & 4) != 0)
					{
						Main.TileSet[num82, num83].WallType = PacketIn.ReadByte();
					}
					if (Main.NetMode != 2 && (num84 & 8) != 0)
					{
						Main.TileSet[num82, num83].Lava = (byte)(num84 & 0x20);
						Main.TileSet[num82, num83].Liquid = PacketIn.ReadByte();
					}
				}
			}
			WorldGen.RangeFrame(num80, num81, num80 + num79, num81 + num79);
			if (Main.NetMode == 2)
			{
				CreateMessage3(20, num79, num80, num81);
				SendMessageIgnore(sender);
			}
			break;
		}
		case 21:
		{
			int num65 = PacketIn.ReadByte();
			int num66 = PacketIn.ReadInt16();
			int num67 = num66 & 0x1F;
			num66 >>= 5;
			int owner = Main.ItemSet[num65].Owner;
			if (Main.NetMode == 1)
			{
				if (num66 == 0)
				{
					Main.ItemSet[num65].Active = 0;
					break;
				}
				Main.ItemSet[num65].NetDefaults(num66);
				Main.ItemSet[num65].SetPrefix(PacketIn.ReadByte());
				Main.ItemSet[num65].Stack = PacketIn.ReadByte();
				Main.ItemSet[num65].Position = PacketIn.ReadVector2();
				HalfVector2 halfVector3 = new HalfVector2
				{
					PackedValue = PacketIn.ReadUInt32()
				};
				Main.ItemSet[num65].Velocity = halfVector3.ToVector2();
				Main.ItemSet[num65].IsWet = Collision.WetCollision(ref Main.ItemSet[num65].Position, Main.ItemSet[num65].Width, Main.ItemSet[num65].Height);
				Main.ItemSet[num65].Owner = (byte)owner;
				break;
			}
			if (num66 == 0)
			{
				Main.ItemSet[num65].Active = 0;
				CreateMessage2(21, num67, num65);
				SendMessage();
				break;
			}
			int prefix = PacketIn.ReadByte();
			int stack = PacketIn.ReadByte();
			float num68 = PacketIn.ReadSingle();
			float num69 = PacketIn.ReadSingle();
			bool flag4 = num65 == 200;
			if (flag4)
			{
				Item item = default;
				item.NetDefaults(num66, stack);
				num65 = (short)Item.NewItem((int)num68, (int)num69, item.Width, item.Height, item.Type, stack, DoNotBroadcast: true);
			}
			else
			{
				Main.ItemSet[num65].Position.X = num68;
				Main.ItemSet[num65].Position.Y = num69;
			}
			Main.ItemSet[num65].NetDefaults(num66, stack);
			Main.ItemSet[num65].SetPrefix(prefix);
			HalfVector2 halfVector4 = new HalfVector2
			{
				PackedValue = PacketIn.ReadUInt32()
			};
			Main.ItemSet[num65].Velocity = halfVector4.ToVector2();
			CreateMessage2(21, num67, num65);
			if (flag4)
			{
				SendMessage();
				Main.ItemSet[num65].OwnIgnore = (byte)num67;
				Main.ItemSet[num65].OwnTime = 100;
			}
			else
			{
				Main.ItemSet[num65].Owner = (byte)owner;
				SendMessageIgnore(Main.PlayerSet[num67].client);
			}
			break;
		}
		case 22:
		{
			int num52 = PacketIn.ReadByte();
			int num53 = PacketIn.ReadByte();
			int num54 = num53 & 0x80;
			num53 ^= num54;
			Main.ItemSet[num52].Owner = (byte)num53;
			if (num53 < 8)
			{
				Main.ItemSet[num52].KeepTime = 15;
				Main.ItemSet[num52].Position = PacketIn.ReadVector2();
				if (num54 != 0)
				{
					HalfVector2 halfVector2 = new HalfVector2
					{
						PackedValue = PacketIn.ReadUInt32()
					};
					Main.ItemSet[num52].Velocity = halfVector2.ToVector2();
				}
				else
				{
					Main.ItemSet[num52].Velocity.X = 0f;
					Main.ItemSet[num52].Velocity.Y = 0f;
				}
			}
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			break;
		}
		case 24:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			ushort i3 = PacketIn.ReadUInt16();
			int j3 = PacketIn.ReadUInt16();
			WorldGen.CloseDoor(i3, j3, forced: true);
			break;
		}
		case 26:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num115 = PacketIn.ReadByte();
			int hitDirection2 = PacketIn.ReadSByte();
			int damage = PacketIn.ReadInt16();
			bool pvp = (num115 & 0x40) != 0;
			bool crit2 = (num115 & 0x80) != 0;
			uint deathText2 = PacketIn.ReadUInt32();
			Main.PlayerSet[num115 & 7].Hurt(damage, hitDirection2, pvp, quiet: true, deathText2, crit2);
			break;
		}
		case 27:
		{
			int num108 = PacketIn.ReadByte();
			int num109 = num108 >> 4;
			num108 &= 0xF;
			int num110 = PacketIn.ReadByte();
			int num111 = (int)ReadCompacted();
			int num112 = 512;
			for (int num113 = 0; num113 < 512; num113++)
			{
				if (Main.ProjectileSet[num113].owner == num108 && Main.ProjectileSet[num113].identity == num111 && Main.ProjectileSet[num113].active != 0)
				{
					num112 = num113;
					break;
				}
			}
			if (num112 == 512)
			{
				for (int num114 = 0; num114 < 512; num114++)
				{
					if (Main.ProjectileSet[num114].active == 0)
					{
						num112 = num114;
						break;
					}
				}
			}
			if (Main.ProjectileSet[num112].active == 0 || Main.ProjectileSet[num112].type != num110)
			{
				Main.ProjectileSet[num112].SetDefaults(num110);
			}
			Main.ProjectileSet[num112].type = (byte)num110;
			Main.ProjectileSet[num112].owner = (byte)num108;
			Main.ProjectileSet[num112].identity = (ushort)num111;
			Main.ProjectileSet[num112].position = PacketIn.ReadVector2();
			Main.ProjectileSet[num112].XYWH.X = (int)Main.ProjectileSet[num112].position.X;
			Main.ProjectileSet[num112].XYWH.Y = (int)Main.ProjectileSet[num112].position.Y;
			HalfVector2 halfVector5 = new HalfVector2
			{
				PackedValue = PacketIn.ReadUInt32()
			};
			Main.ProjectileSet[num112].velocity = halfVector5.ToVector2();
			if ((num109 & 1) != 0)
			{
				HalfSingle halfSingle2 = new HalfSingle
				{
					PackedValue = PacketIn.ReadUInt16()
				};
				Main.ProjectileSet[num112].knockBack = halfSingle2.ToSingle();
			}
			else
			{
				Main.ProjectileSet[num112].knockBack = 0f;
			}
			Main.ProjectileSet[num112].damage = (short)(((num109 & 2) != 0) ? PacketIn.ReadInt16() : 0);
			Main.ProjectileSet[num112].ai0 = (((num109 & 4) != 0) ? PacketIn.ReadSingle() : 0f);
			Main.ProjectileSet[num112].ai1 = (((num109 & 8) != 0) ? PacketIn.ReadInt16() : 0);
			if (Main.NetMode == 2)
			{
				EchoProjectileMessage(sender, ref Main.ProjectileSet[num112]);
			}
			break;
		}
		case 28:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num62 = PacketIn.ReadByte();
			int num63 = PacketIn.ReadInt16();
			if (num63 >= 0)
			{
				HalfSingle halfSingle = new HalfSingle
				{
					PackedValue = PacketIn.ReadUInt16()
				};
				float knockBack = halfSingle.ToSingle();
				int num64 = PacketIn.ReadSByte();
				bool crit = (num64 & 1) != 0;
				num64 >>= 1;
				Main.NPCSet[num62].StrikeNPC(num63, knockBack, num64, crit);
			}
			else
			{
				Main.NPCSet[num62].Life = 0;
				if (Main.NPCSet[num62].Active == 0)
				{
					break;
				}
				Main.NPCSet[num62].HitEffect();
				Main.NPCSet[num62].Active = 0;
			}
			if (Main.NetMode == 2)
			{
				if (Main.NPCSet[num62].Life <= 0)
				{
					CreateMessage1(23, num62);
					SendMessage();
				}
				else
				{
					Main.NPCSet[num62].ShouldNetUpdate = true;
				}
			}
			break;
		}
		case 29:
		{
			int num44 = (int)ReadCompacted();
			int num45 = PacketIn.ReadByte();
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			for (int num46 = 0; num46 < 512; num46++)
			{
				if (Main.ProjectileSet[num46].owner == num45 && Main.ProjectileSet[num46].identity == num44 && Main.ProjectileSet[num46].active != 0)
				{
					Main.ProjectileSet[num46].Kill();
					break;
				}
			}
			break;
		}
		case 30:
		{
			int num104 = PacketIn.ReadByte();
			int num105 = num104 & 0x80;
			num104 ^= num105;
			Player player14 = Main.PlayerSet[num104];
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
				int textId = ((num105 != 0) ? 24 : 25);
				SendText(player14.Name, textId, Main.TeamColors[player14.team].R, Main.TeamColors[player14.team].G, Main.TeamColors[player14.team].B, -1);
			}
			player14.hostile = num105 != 0;
			break;
		}
		case 32:
		{
			int num73 = (int)ReadCompacted();
			int num74 = PacketIn.ReadByte();
			if (Main.ChestSet[num73] == null)
			{
				Main.ChestSet[num73] = new Chest();
			}
			int num75 = PacketIn.ReadInt16();
			if (num75 != 0)
			{
				int prefix2 = PacketIn.ReadByte();
				int stack2 = PacketIn.ReadByte();
				Main.ChestSet[num73].ItemSet[num74].NetDefaults(num75, stack2);
				Main.ChestSet[num73].ItemSet[num74].SetPrefix(prefix2);
			}
			else
			{
				Main.ChestSet[num73].ItemSet[num74].Init();
			}
			break;
		}
		case 33:
		{
			int num106 = PacketIn.ReadInt16();
			Player player15 = Main.PlayerSet[num106 & 0x1F];
			num106 >>= 5;
			if (player15.isLocal())
			{
				int playerChest = player15.PlayerChest;
				player15.PlayerChest = (short)num106;
				if (num106 >= 0)
				{
					player15.chestX = PacketIn.ReadInt16();
					player15.chestY = PacketIn.ReadInt16();
				}
				if (playerChest == -1)
				{
					player15.ui.OpenInventory();
					Main.PlaySound(10);
				}
				else if (num106 == -1)
				{
					if (player15.ui.ActiveInvSection == UI.InventorySection.CHEST)
					{
						player15.ui.CloseInventory();
						Main.PlaySound(11);
					}
				}
				else if (playerChest != num106)
				{
					player15.ui.OpenInventory();
					Main.PlaySound(12);
				}
			}
			else
			{
				player15.PlayerChest = (short)num106;
				if (num106 >= 0)
				{
					PacketIn.Position += 4;
				}
			}
			break;
		}
		case 35:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num93 = PacketIn.ReadByte();
			int healAmount = (int)ReadCompacted();
			Main.PlayerSet[num93].HealEffect(healAmount);
			break;
		}
		case 36:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num88 = PacketIn.ReadByte();
			Player player11 = Main.PlayerSet[num88];
			int num89 = PacketIn.ReadByte();
			player11.ZoneEvil = (num89 & 1) != 0;
			player11.ZoneMeteor = (num89 & 2) != 0;
			player11.ZoneDungeon = (num89 & 4) != 0;
			player11.ZoneJungle = (num89 & 8) != 0;
			player11.zoneHoly = (num89 & 0x10) != 0;
			break;
		}
		case 40:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num78 = PacketIn.ReadByte();
			Main.PlayerSet[num78].TalkNPC = PacketIn.ReadInt16();
			break;
		}
		case 41:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num70 = PacketIn.ReadByte();
			Player player6 = Main.PlayerSet[num70];
			player6.itemRotation = PacketIn.ReadSingle();
			player6.itemAnimation = PacketIn.ReadInt16();
			player6.channel = player6.Inventory[player6.SelectedItem].Channelling;
			break;
		}
		case 42:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num61 = PacketIn.ReadByte();
			Player player5 = Main.PlayerSet[num61];
			player5.statMana = PacketIn.ReadInt16();
			player5.statManaMax = PacketIn.ReadInt16();
			break;
		}
		case 43:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num59 = PacketIn.ReadByte();
			int manaAmount = PacketIn.ReadInt16();
			Main.PlayerSet[num59].ManaEffect(manaAmount);
			break;
		}
		case 44:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num47 = PacketIn.ReadByte();
			int hitDirection = PacketIn.ReadSByte();
			int num48 = PacketIn.ReadInt16();
			int num49 = PacketIn.ReadByte();
			uint deathText = PacketIn.ReadUInt32();
			Main.PlayerSet[num47].KillMe(num48, hitDirection, num49 != 0, deathText);
			break;
		}
		case 45:
		{
			int num116 = PacketIn.ReadByte();
			int num117 = num116 >> 4;
			num116 &= 0xF;
			Player player17 = Main.PlayerSet[num116];
			int team = player17.team;
			player17.team = (byte)num117;
			if (Main.NetMode != 2)
			{
				break;
			}
			EchoMessage(sender);
			int textId2 = 26 + num117;
			for (int num118 = 0; num118 < 8; num118++)
			{
				if (num118 == num116 || (team > 0 && Main.PlayerSet[num118].team == team) || (num117 > 0 && Main.PlayerSet[num118].team == num117))
				{
					SendText(player17.Name, textId2, Main.TeamColors[num117].R, Main.TeamColors[num117].G, Main.TeamColors[num117].B, num118);
				}
			}
			break;
		}
		case 47:
		{
			int num107 = PacketIn.ReadByte();
			Player player16 = Main.PlayerSet[num107];
			num107 = (int)ReadCompacted();
			Main.SignSet[num107].Read(PacketIn);
			if (Main.NetMode == 1 && num107 != player16.sign)
			{
				player16.ui.CloseInventory();
				player16.TalkNPC = -1;
				player16.ui.editSign = false;
				Main.PlaySound(10);
				player16.sign = (short)num107;
				player16.ui.npcChatText = Main.SignSet[num107].SignString;
			}
			break;
		}
		case 48:
		{
			int num100 = PacketIn.ReadUInt16();
			int num101 = num100 & -49153;
			int num102 = PacketIn.ReadUInt16();
			int num103 = num102 & 0x7FFF;
			fixed (Tile* ptr3 = &Main.TileSet[num101, num103])
			{
				if ((num100 & 0x8000) != 0)
				{
					ptr3->Liquid = PacketIn.ReadByte();
				}
				else if ((num100 & 0x4000) != 0)
				{
					ptr3->Liquid = byte.MaxValue;
				}
				else
				{
					ptr3->Liquid = 0;
				}
				ptr3->Lava = (byte)((num102 >> 10) & 0x20);
			}
			if (Main.NetMode == 2)
			{
				WorldGen.SquareTileFrame(num101, num103);
			}
			break;
		}
		case 50:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num90 = PacketIn.ReadByte();
			Player player12 = Main.PlayerSet[num90];
			for (int num91 = 0; num91 < 10; num91++)
			{
				int num92 = PacketIn.ReadByte();
				player12.buff[num91].Type = (ushort)num92;
				player12.buff[num91].Time = (ushort)((num92 > 0) ? 60u : 0u);
			}
			break;
		}
		case 51:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			Player player9 = Main.PlayerSet[PacketIn.ReadByte()];
			Main.PlaySound(2, player9.XYWH.X, player9.XYWH.Y);
			break;
		}
		case 52:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			PacketIn.ReadByte();
			int num76 = PacketIn.ReadUInt16();
			int num77 = PacketIn.ReadUInt16();
			Chest.Unlock(num76, num77);
			if (Main.NetMode == 2)
			{
				SendTileSquare(num76, num77, 2);
			}
			break;
		}
		case 55:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num71 = PacketIn.ReadByte();
			Player player7 = Main.PlayerSet[num71];
			int type3 = PacketIn.ReadByte();
			int time2 = (int)ReadCompacted();
			player7.AddBuff(type3, time2);
			break;
		}
		case 58:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			int num60 = PacketIn.ReadByte();
			Player player4 = Main.PlayerSet[num60];
			Main.HarpNote = PacketIn.ReadSingle();
			Main.PlaySound(2, player4.XYWH.X, player4.XYWH.Y, (player4.Inventory[player4.SelectedItem].Type == 507) ? 35 : 26);
			break;
		}
		case 59:
		{
			if (Main.NetMode == 2)
			{
				EchoMessage(sender);
			}
			ushort i2 = PacketIn.ReadUInt16();
			int j2 = PacketIn.ReadUInt16();
			WorldGen.HitSwitch(i2, j2);
			break;
		}
		case 60:
		{
			int num41 = PacketIn.ReadByte();
			int num42 = PacketIn.ReadInt16();
			int num43 = PacketIn.ReadInt16();
			bool flag3 = PacketIn.ReadBoolean();
			if (Main.NetMode == 1)
			{
				Main.NPCSet[num41].IsHomeless = flag3;
				Main.NPCSet[num41].HomeTileX = (short)num42;
				Main.NPCSet[num41].HomeTileY = (short)num43;
			}
			else if (!flag3)
			{
				WorldGen.kickOut(num41);
			}
			else
			{
				WorldGen.moveRoom(num42, num43, num41);
			}
			break;
		}
		case 6:
		case 7:
		case 8:
		case 9:
		case 10:
		case 11:
		case 14:
		case 18:
		case 23:
		case 25:
		case 31:
		case 34:
		case 37:
		case 38:
		case 39:
		case 46:
		case 49:
		case 53:
		case 54:
		case 56:
		case 57:
			break;
		}
	}

	public static void BootPlayer(int plr, int stringId)
	{
		SendKick(Main.PlayerSet[plr].client, stringId);
		Main.PlayerSet[plr].ToKill = true;
	}

	public static void SendTileSquare(int tileX, int tileY, int size)
	{
		int num = size - 1 >> 1;
		CreateMessage3(20, size, tileX - num, tileY - num);
		SendMessage();
	}

	public static void SendTile(int tileX, int tileY)
	{
		CreateMessage2(15, tileX, tileY);
		SendMessage();
	}

	public static bool SendSection(NetClient client, int sectionX, int sectionY)
	{
		if (sectionX >= 0 && sectionY >= 0 && sectionX < Main.MaxSectionsX && sectionY < Main.MaxSectionsY && !client.TileSectionsTaken[sectionX, sectionY])
		{
			client.TileSectionsTaken[sectionX, sectionY] = true;
			CreateMessage2(10, sectionX, sectionY);
			SendMessage(client);
			return true;
		}
		return false;
	}

	public static void SendSectionSquare(NetClient client, int sectionX, int sectionY, int size)
	{
		int num = size - 1 >> 1;
		for (int i = sectionX - num; i <= sectionX + num; i++)
		{
			for (int j = sectionY - num; j <= sectionY + num; j++)
			{
				SendSection(client, i, j);
			}
		}
	}

	public static void greetPlayer(int plr)
	{
		SendText(31, Main.PlayerSet[plr].Name + "!", 255, 240, 20, plr);
		string text = null;
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active != 0)
			{
				text = ((text != null) ? (text + "\n- " + Main.PlayerSet[i].Name) : ("\n- " + Main.PlayerSet[i].Name));
			}
		}
		SendText(23, text, 255, 240, 20, plr);
	}

	public static void SendWater(int x, int y)
	{
		CreateMessage2(48, x, y);
		if (Main.NetMode == 1)
		{
			SendMessage();
			return;
		}
		for (int num = Netplay.ClientList.Count - 1; num >= 0; num--)
		{
			NetClient netClient = Netplay.ClientList[num];
			if (netClient.ServerState >= 3)
			{
				int num2 = x / 40;
				int num3 = y / 30;
				if (netClient.TileSectionsTaken[num2, num3])
				{
					SendMessageNoClear(netClient);
				}
			}
		}
		ClearMessage();
	}

	public static void SyncPlayer(int plr)
	{
		Player player = Main.PlayerSet[plr];
		NetClient client = player.client;
		CreateMessage2(14, plr, player.Active);
		SendMessageIgnore(client);
		if (player.Active != 0 && (client == null || client.ServerState == 10))
		{
			CreateMessage1(4, plr);
			SendMessageIgnore(client);
			CreateMessage1(13, plr);
			SendMessageIgnore(client);
			CreateMessage1(16, plr);
			SendMessageIgnore(client);
			CreateMessage1(30, plr);
			SendMessageIgnore(client);
			CreateMessage1(42, plr);
			SendMessageIgnore(client);
			CreateMessage1(45, plr);
			SendMessageIgnore(client);
			CreateMessage1(50, plr);
			SendMessageIgnore(client);
			for (int i = 0; i < 49; i++)
			{
				CreateMessage2(5, plr, i);
				SendMessage();
			}
			for (int j = 0; j < 11; j++)
			{
				CreateMessage2(5, plr, j + 49);
				SendMessage();
			}
			if (!Main.PlayerSet[plr].IsAnnounced)
			{
				Main.PlayerSet[plr].IsAnnounced = true;
				SendText(player.Name, 32, 255, 240, 20, -1);
				player.oldName = player.Name;
			}
		}
		else if (player.IsAnnounced)
		{
			player.IsAnnounced = false;
			SendText(player.oldName, 33, 255, 240, 20, -1);
		}
	}

	public static void syncPlayers()
	{
		for (int i = 0; i < 8; i++)
		{
			SyncPlayer(i);
		}
		for (int j = 0; j < 196; j++)
		{
			if (Main.NPCSet[j].Active != 0 && Main.NPCSet[j].IsTownNPC && -1 != Main.NPCSet[j].GetHeadTextureID())
			{
				CreateMessage4(60, j, Main.NPCSet[j].HomeTileX, Main.NPCSet[j].HomeTileY, Main.NPCSet[j].IsHomeless ? 1 : 0);
				SendMessage();
			}
		}
	}
}
