using System;
using Microsoft.Xna.Framework;

namespace Terraria;

public sealed class Collision
{
	public static bool UpCol;

	public static bool DownCol;

	public static bool CanHit(ref Rectangle HitboxA, ref Rectangle HitboxB)
	{
		int num = HitboxA.X + (HitboxA.Width >> 1) >> 4;
		int num2 = HitboxA.Y + (HitboxA.Height >> 1) >> 4;
		int num3 = HitboxB.X + (HitboxB.Width >> 1) >> 4;
		int num4 = HitboxB.Y + (HitboxB.Height >> 1) >> 4;
		try
		{
			do
			{
				int num5 = Math.Abs(num - num3);
				int num6 = Math.Abs(num2 - num4);
				if (num == num3 && num2 == num4)
				{
					return true;
				}
				if (num5 > num6)
				{
					num = ((num >= num3) ? (num - 1) : (num + 1));
					if (Main.TileSet[num, num2 - 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num, num2 - 1].Type] && Main.TileSet[num, num2 + 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num, num2 + 1].Type])
					{
						return false;
					}
				}
				else
				{
					num2 = ((num2 >= num4) ? (num2 - 1) : (num2 + 1));
					if (Main.TileSet[num - 1, num2].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num - 1, num2].Type] && Main.TileSet[num + 1, num2].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num + 1, num2].Type])
					{
						return false;
					}
				}
			}
			while (Main.TileSet[num, num2].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[num, num2].Type]);
			return false;
		}
		catch
		{
			return false;
		}
	}

	public static bool AnyPlayerOrNPC(int X, int Y, int Height)
	{
		Rectangle rectangle = new Rectangle
		{
			X = X << 4,
			Y = Y << 4,
			Width = 16,
			Height = Height << 4
		};
		for (int num = 7; num >= 0; num--)
		{
			if (Main.PlayerSet[num].Active != 0 && rectangle.Intersects(Main.PlayerSet[num].XYWH))
			{
				return true;
			}
		}
		for (int num2 = 195; num2 >= 0; num2--)
		{
			if (Main.NPCSet[num2].Active != 0 && rectangle.Intersects(Main.NPCSet[num2].XYWH))
			{
				return true;
			}
		}
		return false;
	}

	public unsafe static bool DrownCollision(ref Vector2 Position, int Width, int Height, int GravDir)
	{
		Vector2 vector = Position;
		int num = 10;
		int num2 = 12;
		if (num > Width)
		{
			num = Width;
		}
		if (num2 > Height)
		{
			num2 = Height;
		}
		vector.X += Width >> 1;
		vector.X -= num >> 1;
		vector.Y -= 2f;
		if (GravDir == -1)
		{
			vector.Y += (Height >> 1) - 6;
		}
		int num3 = ((int)Position.X >> 4) - 1;
		int num4 = ((int)Position.X + Width >> 4) + 2;
		int num5 = ((int)Position.Y >> 4) - 1;
		int num6 = ((int)Position.Y + Height >> 4) + 2;
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesX)
		{
			num4 = Main.MaxTilesX;
		}
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesY)
		{
			num6 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num3; i < num4; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + num5);
				for (int j = num5; j < num6; j++)
				{
					int liquid = ptr->Liquid;
					if (liquid > 0)
					{
						int num7 = i << 4;
						float num8 = j << 4;
						float num9 = 256 - liquid;
						num9 *= 0.0625f;
						num8 += num9;
						int num10 = 16 - (int)num9;
						if (vector.X + (float)num > (float)num7 && vector.X < (float)(num7 + 16) && vector.Y + (float)num2 > num8 && vector.Y < num8 + (float)num10)
						{
							return true;
						}
					}
					ptr++;
				}
			}
		}
		return false;
	}

	public unsafe static bool WetCollision(ref Vector2 Position, int Width, int Height)
	{
		Vector2 vector = new Vector2(Position.X + (float)(Width >> 1), Position.Y + (float)(Height >> 1));
		int num = 10;
		int num2 = Height >> 1;
		if (num > Width)
		{
			num = Width;
		}
		if (num2 > Height)
		{
			num2 = Height;
		}
		vector.X -= num >> 1;
		vector.Y -= num2 >> 1;
		int num3 = ((int)Position.X >> 4) - 1;
		int num4 = ((int)Position.X + Width >> 4) + 2;
		int num5 = ((int)Position.Y >> 4) - 1;
		int num6 = ((int)Position.Y + Height >> 4) + 2;
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesX)
		{
			num4 = Main.MaxTilesX;
		}
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesY)
		{
			num6 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num3; i < num4; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + num5);
				for (int j = num5; j < num6; j++)
				{
					int liquid = ptr->Liquid;
					if (liquid > 0)
					{
						int num7 = i << 4;
						float num8 = j << 4;
						int num9 = 16;
						float num10 = 256 - liquid;
						num10 *= 0.0625f;
						num8 += num10;
						num9 -= (int)num10;
						if (vector.X + (float)num > (float)num7 && vector.X < (float)(num7 + 16) && vector.Y + (float)num2 > num8 && vector.Y < num8 + (float)num9)
						{
							return true;
						}
					}
					ptr++;
				}
			}
		}
		return false;
	}

	public unsafe static bool LavaCollision(ref Vector2 Position, int Width, int Height)
	{
		int num = ((int)Position.X >> 4) - 1;
		int num2 = ((int)Position.X + Width >> 4) + 2;
		int num3 = ((int)Position.Y >> 4) - 1;
		int num4 = ((int)Position.Y + Height >> 4) + 2;
		if (num < 0)
		{
			num = 0;
		}
		if (num2 > Main.MaxTilesX)
		{
			num2 = Main.MaxTilesX;
		}
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesY)
		{
			num4 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			Vector2 vector = default;
			for (int i = num; i < num2; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + num3);
				for (int j = num3; j < num4; j++)
				{
					int liquid = ptr->Liquid;
					if (liquid > 0 && ptr->Lava != 0)
					{
						vector.X = i << 4;
						vector.Y = j << 4;
						int num5 = 16;
						float num6 = 256 - liquid;
						num6 *= 0.0625f;
						vector.Y += num6;
						num5 -= (int)num6;
						if (Position.X + (float)Width > vector.X && Position.X < vector.X + 16f && Position.Y + (float)Height > vector.Y && Position.Y < vector.Y + (float)num5)
						{
							return true;
						}
					}
					ptr++;
				}
			}
		}
		return false;
	}

	public unsafe static void TileCollision(ref Vector2 Position, ref Vector2 Velocity, int Width, int Height, bool CanFallThrough = false, bool CanAllowFall = false)
	{
		UpCol = false;
		DownCol = false;
		Vector2 vector = Velocity;
		float num = Position.X + (float)Width;
		float num2 = Position.Y + (float)Height;
		float num3 = Position.X + Velocity.X;
		float num4 = Position.Y + Velocity.Y;
		float num5 = num3 + (float)Width;
		float num6 = num4 + (float)Height;
		int num7 = ((int)Position.X >> 4) - 1;
		int num8 = ((int)num >> 4) + 2;
		int num9 = ((int)Position.Y >> 4) - 1;
		int num10 = ((int)num2 >> 4) + 2;
		int num11 = -1;
		int num12 = -1;
		int num13 = -1;
		int num14 = -1;
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesX)
		{
			num8 = Main.MaxTilesX;
		}
		if (num9 < 0)
		{
			num9 = 0;
		}
		if (num10 > Main.MaxTilesY)
		{
			num10 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num7; i < num8; i++)
			{
				int num15 = i << 4;
				if (!(num5 > (float)num15) || !(num3 < (float)(num15 + 16)))
				{
					continue;
				}
				Tile* ptr = tileSet + (i * 1440 + num9);
				for (int j = num9; j < num10; j++)
				{
					if (ptr->IsActive != 0)
					{
						int type = ptr->Type;
						bool flag = Main.TileSolidTop[type];
						if ((flag && ptr->FrameY == 0) || Main.TileSolid[type])
						{
							int num16 = j << 4;
							if (num6 > (float)num16 && num4 < (float)(num16 + 16))
							{
								if (num2 <= (float)num16)
								{
									DownCol = true;
									if (!flag || !CanFallThrough || (!(vector.Y <= 1f) && !CanAllowFall))
									{
										num13 = i;
										num14 = j;
										if (num13 != num11)
										{
											Velocity.Y = (float)num16 - num2;
										}
									}
								}
								else if (!flag)
								{
									if (num <= (float)num15)
									{
										num11 = i;
										num12 = j;
										if (num12 != num14)
										{
											Velocity.X = (float)num15 - num;
										}
										if (num13 == num11)
										{
											Velocity.Y = vector.Y;
										}
									}
									else if (Position.X >= (float)(num15 + 16))
									{
										num11 = i;
										num12 = j;
										if (num12 != num14)
										{
											Velocity.X = (float)(num15 + 16) - Position.X;
										}
										if (num13 == num11)
										{
											Velocity.Y = vector.Y;
										}
									}
									else if (Position.Y >= (float)(num16 + 16))
									{
										UpCol = true;
										num13 = i;
										num14 = j;
										Velocity.Y = (float)(num16 + 16) - Position.Y;
										if (num14 == num12)
										{
											Velocity.X = vector.X;
										}
									}
								}
							}
						}
					}
					ptr++;
				}
			}
		}
	}

	public unsafe static bool SolidCollision(ref Vector2 Position, int Width, int Height)
	{
		float num = Position.X + (float)Width;
		float num2 = Position.Y + (float)Height;
		int num3 = ((int)Position.X >> 4) - 1;
		int num4 = ((int)num >> 4) + 2;
		int num5 = ((int)Position.Y >> 4) - 1;
		int num6 = ((int)num2 >> 4) + 2;
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesX)
		{
			num4 = Main.MaxTilesX;
		}
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesY)
		{
			num6 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num3; i < num4; i++)
			{
				if (!(num > (float)(i << 4)) || !(Position.X < (float)(i + 1 << 4)))
				{
					continue;
				}
				Tile* ptr = tileSet + (i * 1440 + num5);
				for (int j = num5; j < num6; j++)
				{
					if (ptr->IsActive != 0 && num2 > (float)(j << 4) && Position.Y < (float)(j + 1 << 4) && Main.TileSolidNotSolidTop[ptr->Type])
					{
						return true;
					}
					ptr++;
				}
			}
		}
		return false;
	}

	public unsafe static Vector2 WaterCollision(Vector2 Position, Vector2 Velocity, int Width, int Height, bool CanFallThrough = false)
	{
		Vector2 result = Velocity;
		Vector2 vector = Position + Velocity;
		Vector2 vector2 = Position;
		int num = ((int)Position.X >> 4) - 1;
		int num2 = ((int)Position.X + Width >> 4) + 2;
		int num3 = ((int)Position.Y >> 4) - 1;
		int num4 = ((int)Position.Y + Height >> 4) + 2;
		if (num < 0)
		{
			num = 0;
		}
		if (num2 > Main.MaxTilesX)
		{
			num2 = Main.MaxTilesX;
		}
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesY)
		{
			num4 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			Vector2 vector3 = default;
			for (int i = num; i < num2; i++)
			{
				vector3.X = i << 4;
				Tile* ptr = tileSet + (i * 1440 + num3);
				for (int j = num3; j < num4; j++)
				{
					int liquid = ptr->Liquid;
					if (liquid > 0)
					{
						int num5 = liquid + 16 >> 5 << 1;
						vector3.Y = (j << 4) + 16 - num5;
						if (vector.X + (float)Width > vector3.X && vector.X < vector3.X + 16f && vector.Y + (float)Height > vector3.Y && vector.Y < vector3.Y + (float)num5 && vector2.Y + (float)Height <= vector3.Y && !CanFallThrough)
						{
							result.Y = vector3.Y - (vector2.Y + (float)Height);
						}
					}
					ptr++;
				}
			}
		}
		return result;
	}

	public unsafe static void AnyCollision(ref Vector2 Position, ref Vector2 Velocity, int Width, int Height)
	{
		Vector2 vector = Position + Velocity;
		Vector2 vector2 = Position;
		int num = ((int)Position.X >> 4) - 1;
		int num2 = ((int)Position.X + Width >> 4) + 2;
		int num3 = ((int)Position.Y >> 4) - 1;
		int num4 = ((int)Position.Y + Height >> 4) + 2;
		int num5 = -1;
		int num6 = -1;
		int num7 = -1;
		if (num < 0)
		{
			num = 0;
		}
		if (num2 > Main.MaxTilesX)
		{
			num2 = Main.MaxTilesX;
		}
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesY)
		{
			num4 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num; i < num2; i++)
			{
				float num8 = i << 4;
				if (!(vector.X + (float)Width > num8) || !(vector.X < num8 + 16f))
				{
					continue;
				}
				Tile* ptr = tileSet + (i * 1440 + num3);
				for (int j = num3; j < num4; j++)
				{
					if (ptr->IsActive != 0)
					{
						float num9 = j << 4;
						if (vector.Y + (float)Height > num9 && vector.Y < num9 + 16f)
						{
							if (vector2.Y + (float)Height <= num9)
							{
								int num10 = i;
								num7 = j;
								if (num10 != num5)
								{
									Velocity.Y = num9 - (vector2.Y + (float)Height);
								}
							}
							else if (!Main.TileSolidTop[ptr->Type])
							{
								if (vector2.X + (float)Width <= num8)
								{
									num5 = i;
									num6 = j;
									if (num6 != num7)
									{
										Velocity.X = num8 - (vector2.X + (float)Width);
									}
								}
								else if (vector2.X >= num8 + 16f)
								{
									num5 = i;
									num6 = j;
									if (num6 != num7)
									{
										Velocity.X = num8 + 16f - vector2.X;
									}
								}
								else if (vector2.Y >= num9 + 16f)
								{
									num7 = j;
									Velocity.Y = num9 + 16f - vector2.Y + 0.01f;
									if (num7 == num6)
									{
										Velocity.X += 0.01f;
									}
								}
							}
						}
					}
					ptr++;
				}
			}
		}
	}

	public unsafe static void HitTiles(Vector2 Position, Vector2 Velocity, int Width, int Height)
	{
		Vector2 vector = Position + Velocity;
		int num = ((int)Position.X >> 4) - 1;
		int num2 = ((int)Position.X + Width >> 4) + 1;
		int num3 = ((int)Position.Y >> 4) - 1;
		int num4 = ((int)Position.Y + Height >> 4) + 1;
		if (num < 0)
		{
			num = 0;
		}
		if (num2 > Main.MaxTilesX)
		{
			num2 = Main.MaxTilesX;
		}
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesY)
		{
			num4 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num; i < num2; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + num3);
				for (int j = num3; j < num4; j++)
				{
					if (ptr->CanStandOnTop())
					{
						float num5 = i << 4;
						float num6 = j << 4;
						if (vector.X + (float)Width >= num5 && vector.X <= num5 + 16f && vector.Y + (float)Height >= num6 && vector.Y <= num6 + 16f)
						{
							WorldGen.KillTile(i, j, KillToFail: true, EffectOnly: true);
						}
					}
					ptr++;
				}
			}
		}
	}

	public unsafe static int HurtTiles(ref Vector2 Position, ref Vector2 Velocity, int Width, int Height, bool DontBurnPlayer = false)
	{
		int num = ((int)Position.X >> 4) - 1;
		int num2 = ((int)Position.X + Width >> 4) + 1;
		int num3 = ((int)Position.Y >> 4) - 1;
		int num4 = ((int)Position.Y + Height >> 4) + 1;
		if (num < 0)
		{
			num = 0;
		}
		if (num2 > Main.MaxTilesX)
		{
			num2 = Main.MaxTilesX;
		}
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesY)
		{
			num4 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num; i < num2; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + num3);
				for (int j = num3; j < num4; j++)
				{
					if (ptr->IsActive != 0)
					{
						EntityID.TileID type = (EntityID.TileID)ptr->Type;
						switch (type)
						{
						case EntityID.TileID.CORRUPTION_THORN:
						case EntityID.TileID.METEORITE:
						case EntityID.TileID.SPIKE:
						case EntityID.TileID.SAND:
						case EntityID.TileID.ASH:
						case EntityID.TileID.HELLSTONE:
						case EntityID.TileID.JUNGLE_THORN:
						case EntityID.TileID.HELLSTONE_BRICK:
						case EntityID.TileID.EBONSAND:
						case EntityID.TileID.PEARLSAND:
						case EntityID.TileID.SILT:
						{
							float num5 = i << 4;
							float num6 = j << 4;
							int result = 0;
							switch (type)
							{
							case EntityID.TileID.CORRUPTION_THORN:
							case EntityID.TileID.JUNGLE_THORN:
							case EntityID.TileID.CACTUS:
								if (Position.X + (float)Width > num5 && Position.X < num5 + 16f && Position.Y + (float)Height > num6 && Position.Y < num6 + 16.01f)
								{
									result = 10;
									switch (type)
									{
									case EntityID.TileID.JUNGLE_THORN:
										result = 17;
										break;
									case EntityID.TileID.CACTUS:
										result = 6;
										break;
									}
									if ((type == EntityID.TileID.CORRUPTION_THORN || type == EntityID.TileID.JUNGLE_THORN) && WorldGen.KillTile(i, j))
									{
										NetMessage.CreateMessage5(17, 4, i, j, 0);
										NetMessage.SendMessage();
									}
									return result;
								}
								break;
							case EntityID.TileID.SAND:
							case EntityID.TileID.EBONSAND:
							case EntityID.TileID.PEARLSAND:
							case EntityID.TileID.SILT:
								if (Position.X + (float)Width - 2f >= num5 && Position.X + 2f <= num5 + 16f && Position.Y + (float)Height - 2f >= num6 && Position.Y + 2f <= num6 + 16f)
								{
									return 20;
								}
								break;
							default:
								if (Position.X + (float)Width >= num5 && Position.X <= num5 + 16f && Position.Y + (float)Height >= num6 && Position.Y <= num6 + 16.01f)
								{
									if (type == EntityID.TileID.SPIKE)
									{
										result = 40;
									}
									else if (!DontBurnPlayer && (type == EntityID.TileID.METEORITE || type == EntityID.TileID.HELLSTONE || type == EntityID.TileID.HELLSTONE_BRICK))
									{
										result = 20;
									}
									return result;
								}
								break;
							}
							break;
						}
						}
					}
					ptr++;
				}
			}
		}
		return 0;
	}

	public unsafe static bool SwitchTiles(Vector2 Position, int Width, int Height, Vector2 OldPosition)
	{
		int num = ((int)Position.X >> 4) - 1;
		int num2 = ((int)Position.X + Width >> 4) + 1;
		int num3 = ((int)Position.Y >> 4) - 1;
		int num4 = ((int)Position.Y + Height >> 4) + 1;
		if (num < 0)
		{
			num = 0;
		}
		if (num2 > Main.MaxTilesX)
		{
			num2 = Main.MaxTilesX;
		}
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesY)
		{
			num4 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num; i < num2; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + num3);
				for (int j = num3; j < num4; j++)
				{
					if (ptr->Type == 135 && ptr->IsActive != 0)
					{
						float num5 = i << 4;
						float num6 = (j << 4) + 12;
						if (Position.X + (float)Width > num5 && Position.X < num5 + 16f && Position.Y + (float)Height > num6 && Position.Y < num6 + 4.01f && (!(OldPosition.X + (float)Width > num5) || !(OldPosition.X < num5 + 16f) || !(OldPosition.Y + (float)Height > num6) || !(OldPosition.Y < num6 + 16.01f)))
						{
							WorldGen.HitSwitch(i, j);
							NetMessage.CreateMessage2(59, i, j);
							NetMessage.SendMessage();
							return true;
						}
					}
					ptr++;
				}
			}
		}
		return false;
	}

	public unsafe static Vector2i StickyTiles(Vector2 Position, Vector2 Velocity, int Width, int Height)
	{
		Vector2 vector = Position;
		int num = ((int)Position.X >> 4) - 1;
		int num2 = ((int)Position.X + Width >> 4) + 2;
		int num3 = ((int)Position.Y >> 4) - 1;
		int num4 = ((int)Position.Y + Height >> 4) + 2;
		if (num < 0)
		{
			num = 0;
		}
		if (num2 > Main.MaxTilesX)
		{
			num2 = Main.MaxTilesX;
		}
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num4 > Main.MaxTilesY)
		{
			num4 = Main.MaxTilesY;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = num; i < num2; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + num3);
				for (int j = num3; j < num4; j++)
				{
					if (ptr->Type == 51 && ptr->IsActive != 0)
					{
						float num5 = i << 4;
						float num6 = j << 4;
						if (vector.X + (float)Width > num5 && vector.X < num5 + 16f && vector.Y + (float)Height > num6 && vector.Y < num6 + 16.01f)
						{
							if (Math.Abs(Velocity.X) + Math.Abs(Velocity.Y) > 0.7f && Main.Rand.Next(30) == 0)
							{
								Main.DustSet.NewDust((int)num5, (int)num6, 16, 16, 30);
							}
							return new Vector2i(i, j);
						}
					}
					ptr++;
				}
			}
		}
		return new Vector2i(-1, -1);
	}

	public unsafe static bool SolidTiles(int MinX, int MaxX, int MinY, int MaxY)
	{
		if (MinX < 0)
		{
			return true;
		}
		if (MaxX >= Main.MaxTilesX)
		{
			return true;
		}
		if (MinY < 0)
		{
			return true;
		}
		if (MaxY >= Main.MaxTilesY)
		{
			return true;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = MinX; i < MaxX + 1; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + MinY);
				for (int j = MinY; j < MaxY + 1; j++)
				{
					if (ptr->IsActive != 0 && Main.TileSolidNotSolidTop[ptr->Type])
					{
						return true;
					}
					ptr++;
				}
			}
		}
		return false;
	}

	public static bool LineIntersection(ref Vector2 GameView, ref Vector2 PlayerView, Vector2 HorizToVert, Vector2 MirrorHorizToVert, ref Vector2 Intersection)
	{
		Vector2 vector = PlayerView - GameView;
		Vector2 vector2 = MirrorHorizToVert - HorizToVert;
		Vector2 vector3 = HorizToVert - GameView;
		float num = vector.X * vector2.Y - vector.Y * vector2.X;
		if (num == 0f)
		{
			return false;
		}
		float num2 = (vector3.X * vector2.Y - vector3.Y * vector2.X) / num;
		if (num2 < 0f || num2 > 1f)
		{
			return false;
		}
		float num3 = (vector3.X * vector.Y - vector3.Y * vector.X) / num;
		if (num3 < 0f || num3 > 1f)
		{
			return false;
		}
		Intersection = GameView + num2 * vector;
		return true;
	}
}
