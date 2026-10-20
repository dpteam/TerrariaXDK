using Microsoft.Xna.Framework;

namespace Terraria;

public struct DustPool(WorldView CurrentView, int MaxSize)
{
	public WorldView View = CurrentView;

	public short SnowDust = 0;

	public short LavaBubbles = 0;

	public short NextDust = 0;

	public short PoolSize = (short)MaxSize;

	public Dust[] Dusts = new Dust[MaxSize];

	public void Init()
	{
		for (int num = PoolSize - 1; num >= 0; num--)
		{
			Dusts[num].Init();
		}
	}

	private bool ClipDust(int DustX, int DustY)
	{
		if (View == null)
		{
			return !WorldView.AnyViewContains(DustX, DustY);
		}
		return !View.ClipArea.Contains(DustX, DustY);
	}

	public unsafe Dust* NewDust(int Type, ref Rectangle Dimensions, double SpeedX = 0.0, double SpeedY = 0.0, int Alpha = 0, Color DustColor = default(Color), double Scale = 1.0)
	{
		return NewDust(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height, Type, SpeedX, SpeedY, Alpha, DustColor, Scale);
	}

	public unsafe Dust* NewDust(int X, int Y, int Width, int Height, int Type, double SpeedX = 0.0, double SpeedY = 0.0, int Alpha = 0, Color newColor = default(Color), double Scale = 1.0)
	{
		if (ClipDust(X, Y))
		{
			return null;
		}
		int num = NextDust;
		int num2 = PoolSize - 1;
		for (int i = 0; i <= num2; i++)
		{
			int num3 = num & num2;
			fixed (Dust* ptr = &Dusts[num3])
			{
				if (ptr->Active == 0)
				{
					ptr->Active = 1;
					ptr->NoGravity = false;
					ptr->NoLight = false;
					ptr->Type = (ushort)Type;
					ptr->Color = newColor;
					ptr->Alpha = (short)Alpha;
					ptr->FadeIn = 0f;
					ptr->Rotation = 0f;
					ptr->Scale = (float)(Scale + (double)Main.Rand.Next(-20, 21) * 0.01 * Scale);
					ptr->Frame.X = 10 * Type;
					ptr->Frame.Y = 10 * Main.Rand.Next(3);
					if (Type == -1)
					{
						ptr->Frame.X -= 1000;
						ptr->Frame.Y += 30;
					}
					ptr->Frame.Width = 8;
					ptr->Frame.Height = 8;
					int num4 = Width;
					int num5 = Height;
					if (num4 < 5)
					{
						num4 = 5;
					}
					if (num5 < 5)
					{
						num5 = 5;
					}
					ptr->Position.X = X + Main.Rand.Next(num4 - 4) + 4;
					ptr->Position.Y = Y + Main.Rand.Next(num5 - 4) + 4;
					switch (Type)
					{
					case 6:
					case 29:
					case 59:
					case 60:
					case 61:
					case 62:
					case 63:
					case 64:
					case 65:
					case 75:
					case 135:
					case 158:
					case 169:
						ptr->Velocity.X = (float)(((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedX) * 0.3);
						ptr->Velocity.Y = (float)Main.Rand.Next(-10, 6) * 0.1f;
						ptr->Scale *= 0.7f;
						break;
					case 127:
					case 187:
						ptr->Velocity.X = (float)(((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedX) * 0.3);
						ptr->Velocity.Y = (float)(((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedY) * 0.3);
						ptr->Scale *= 0.7f;
						break;
					case 33:
					case 52:
					case 98:
					case 99:
					case 100:
					case 101:
					case 102:
					case 103:
					case 104:
					case 105:
						ptr->Alpha = 170;
						ptr->Velocity.X = (float)(((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedX) * 0.5);
						ptr->Velocity.Y = (float)(((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedY) * 0.5 + 1.0);
						break;
					case 41:
						ptr->Velocity.X = 0f;
						ptr->Velocity.Y = 0f;
						break;
					case 80:
						ptr->Alpha = 50;
						break;
					case 34:
						ptr->Position.Y -= 8f;
						if (!Collision.WetCollision(ref ptr->Position, 4, 4))
						{
							ptr->Active = 0;
							NextDust = (short)num3;
							return ptr;
						}
						ptr->Position.Y += 8f;
						ptr->Velocity.X = (float)(((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedX) * 0.1);
						ptr->Velocity.Y = -0.5f;
						break;
					case 35:
					case 152:
						ptr->Velocity.X = (float)(((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedX) * 0.1);
						ptr->Velocity.Y = -0.5f;
						break;
					default:
						ptr->Velocity.X = (float)((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedX);
						ptr->Velocity.Y = (float)((double)Main.Rand.Next(-20, 21) * 0.1 + SpeedY);
						break;
					}
					NextDust = (short)(num3 + 1);
					return ptr;
				}
			}
			num++;
		}
		return null;
	}

	public unsafe void UpdateDust()
	{
		LavaBubbles = 0;
		SnowDust = 0;
		Vector3 rGB = default;
		fixed (Dust* dusts = Dusts)
		{
			Dust* ptr = dusts;
			for (int num = PoolSize - 1; num >= 0; ptr++, num--)
			{
				if (ptr->Active == 0)
				{
					continue;
				}
				int type = ptr->Type;
				float num2 = ptr->Scale;
				ptr->Position.X += ptr->Velocity.X;
				ptr->Position.Y += ptr->Velocity.Y;
				int num3 = (int)ptr->Position.X;
				int num4 = (int)ptr->Position.Y;
				if (ptr->Type == 154)
				{
					ptr->Rotation += ptr->Velocity.X * 0.3f;
					ptr->Scale -= 0.03f;
				}
				if (ptr->Type == 172)
				{
					float num5 = ptr->Scale * 0.5f;
					if (num5 > 1f)
					{
						num5 = 1f;
					}
					rGB.X = num5 * 0f * num5;
					rGB.Y = num5 * 0.25f * num5;
					rGB.Z = num5 * 1f * num5;
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), rGB);
				}
				if (ptr->Type == 182)
				{
					ptr->Rotation++;
					float num6 = ptr->Scale * 0.25f;
					if (num6 > 1f)
					{
						num6 = 1f;
					}
					rGB.X = num6 * 1f * num6;
					rGB.Y = num6 * 0.2f * num6;
					rGB.Z = num6 * 0.1f * num6;
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), rGB);
				}
				if (ptr->Type == 157)
				{
					float num7 = ptr->Scale * 0.2f;
					rGB.X = num7 * 0.25f * num7;
					rGB.Y = num7 * 1f * num7;
					rGB.Z = num7 * 0.5f * num7;
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), rGB);
				}
				if (ptr->Type == 163)
				{
					float num8 = ptr->Scale * 0.25f;
					rGB.X = num8 * 0.25f * num8;
					rGB.Y = num8 * 1f * num8;
					rGB.Z = num8 * 0.05f * num8;
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), rGB);
				}
				if (ptr->Type == 170)
				{
					float num9 = ptr->Scale * 0.5f;
					rGB.X = num9 * 1f * num9;
					rGB.Y = num9 * 1f * num9;
					rGB.Z = num9 * 0.05f * num9;
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), rGB);
				}
				if (ptr->Type == 156)
				{
					float num10 = ptr->Scale * 0.6f;
					rGB.X = num10 * 0.5f * num10;
					rGB.Y = num10 * 0.9f * num10;
					rGB.Z = num10 * 1f * num10;
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), rGB);
				}
				if (ptr->Type == 174)
				{
					float num11 = ptr->Scale * 1f;
					if (num11 > 0.6f)
					{
						num11 = 0.6f;
					}
					rGB.X = num11;
					rGB.Y = num11 * 0.4f;
					rGB.Z = 0f;
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), rGB);
				}
				switch (type)
				{
				case 6:
				case 29:
				case 59:
				case 60:
				case 61:
				case 62:
				case 63:
				case 64:
				case 65:
				case 75:
				case 127:
				case 135:
				case 158:
				case 167:
					if (!ptr->NoLight)
					{
						if (ClipDust(num3, num4))
						{
							ptr->Active = 0;
							continue;
						}
						num2 *= 1.4f;
						switch (type)
						{
						case 6:
							if (num2 > 0.6f)
							{
								num2 = 0.6f;
							}
							rGB.X = num2;
							rGB.Y = num2 * 0.65f;
							rGB.Z = num2 * 0.4f;
							break;
						case 29:
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB.X = num2 * 0.1f;
							rGB.Y = num2 * 0.4f;
							rGB.Z = num2;
							break;
						case 59:
							if (num2 > 0.8f)
							{
								num2 = 0.8f;
							}
							rGB.X = 0f;
							rGB.Y = num2 * 0.1f;
							rGB.Z = num2 * 1.3f;
							break;
						case 60:
							if (num2 > 0.8f)
							{
								num2 = 0.8f;
							}
							rGB.X = num2;
							rGB.Y = (rGB.Z = num2 * 0.1f);
							break;
						case 61:
							if (num2 > 0.8f)
							{
								num2 = 0.8f;
							}
							rGB.X = 0f;
							rGB.Y = num2;
							rGB.Z = num2 * 0.1f;
							break;
						case 62:
							if (num2 > 0.8f)
							{
								num2 = 0.8f;
							}
							rGB.X = (rGB.Z = num2 * 0.9f);
							rGB.Y = 0f;
							break;
						default:
							if (num2 > 0.8f)
							{
								num2 = 0.8f;
							}
							rGB.X = (rGB.Y = (rGB.Z = num2 * 1.3f));
							break;
						case 64:
							if (num2 > 0.8f)
							{
								num2 = 0.8f;
							}
							rGB.X = (rGB.Y = num2 * 0.9f);
							rGB.Z = 0f;
							break;
						case 65:
							if (num2 > 0.8f)
							{
								num2 = 0.8f;
							}
							rGB.X = 0.5f * Main.DemonTorch + 1f * (1f - Main.DemonTorch);
							rGB.Y = 0.3f;
							rGB.Z = Main.DemonTorch + 0.5f * (1f - Main.DemonTorch);
							break;
						case 75:
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB.X = num2 * 0.7f;
							rGB.Y = num2;
							rGB.Z = num2 * 0.2f;
							break;
						case 127:
							num2 *= 1.3f;
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB.X = num2;
							rGB.Y = num2 * 0.45f;
							rGB.Z = num2 * 0.2f;
							break;
						case 135:
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB.X = num2 * 0.2f;
							rGB.Y = num2 * 0.7f;
							rGB.Z = num2;
							break;
						case 158:
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB.X = num2 * 1f;
							rGB.Y = num2 * 0.5f;
							rGB.Z = 0f;
							break;
						case 169:
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB.X = num2 * 1.1f;
							rGB.Y = num2 * 1.1f;
							rGB.Z = num2 * 0.2f;
							break;
						}
						Lighting.AddLight(num3 >> 4, num4 >> 4, rGB);
					}
					if (!ptr->NoGravity)
					{
						ptr->Velocity.Y += 0.05f;
					}
					break;
				default:
					switch (type)
					{
					case 14:
					case 16:
					case 46:
					case 124:
					case 186:
						ptr->Velocity.X *= 0.98f;
						ptr->Velocity.Y *= 0.98f;
						break;
					case 31:
						ptr->Velocity.X *= 0.98f;
						ptr->Velocity.Y *= 0.98f;
						if (ptr->NoGravity)
						{
							ptr->Alpha += 4;
							if (ptr->Alpha > 255)
							{
								ptr->Active = 0;
								continue;
							}
							ptr->Velocity.X *= 1.02f;
							ptr->Velocity.Y *= 1.02f;
							ptr->Scale += 0.02f;
						}
						break;
					case 32:
						ptr->Scale -= 0.01f;
						ptr->Velocity.X *= 0.96f;
						ptr->Velocity.Y += 0.1f;
						break;
					case 43:
						ptr->Rotation += 0.1f * ptr->Scale;
						if (num2 > 0.048f)
						{
							rGB.X *= num2 * 272.85f / 270f;
							rGB.Y *= num2 * 272.85f / 270f;
							rGB.Z *= num2 * 272.85f / 270f;
							Lighting.AddLight(num3 >> 4, num4 >> 4, rGB);
							if (ptr->Alpha < 255)
							{
								ptr->Scale += 0.09f;
								if (ptr->Scale >= 1f)
								{
									ptr->Scale = 1f;
									ptr->Alpha = 255;
								}
							}
							else if (ptr->Scale < 0.5f)
							{
								ptr->Scale -= 0.02f;
							}
							else if (ptr->Scale < 0.8f)
							{
								ptr->Scale -= 0.01f;
							}
							break;
						}
						ptr->Active = 0;
						continue;
					case 15:
					case 57:
					case 58:
						ptr->Velocity.X *= 0.98f;
						ptr->Velocity.Y *= 0.98f;
						if (type != 15)
						{
							num2 *= 0.8f;
						}
						if (ptr->NoLight)
						{
							ptr->Velocity.X *= 0.95f;
							ptr->Velocity.Y *= 0.95f;
						}
						if (num2 > 1f)
						{
							num2 = 1f;
						}
						switch (type)
						{
						case 15:
							Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(num2 * 0.45f, num2 * 0.55f, num2));
							break;
						case 57:
							Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(num2 * 0.95f, num2 * 0.95f, num2 * 0.45f));
							break;
						case 58:
							Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(num2, num2 * 0.55f, num2 * 0.75f));
							break;
						}
						break;
					case 110:
					case 111:
					case 112:
					case 113:
					case 114:
					{
						float num12 = ptr->Scale * 0.1f;
						if (type == 111)
						{
							num12 = ptr->Scale * 0.125f;
						}
						if (num12 > 1f)
						{
							num12 = 1f;
						}
						switch (type)
						{
						case 110:
							rGB.X = num12 * 0.2f;
							rGB.Y = num12;
							rGB.Z = num12 * 0.5f;
							break;
						case 111:
							rGB.X = num12 * 0.2f;
							rGB.Y = num12 * 0.7f;
							rGB.Z = num12;
							break;
						case 112:
							rGB.X = num12 * 0.8f;
							rGB.Y = num12 * 0.2f;
							rGB.Z = num12 * 0.8f;
							break;
						case 113:
							rGB.X = num12 * 0.2f;
							rGB.Y = num12 * 0.3f;
							rGB.Z = num12 * 1.3f;
							break;
						case 114:
							rGB.X = num12 * 1.2f;
							rGB.Y = num12 * 0.5f;
							rGB.Z = num12 * 0.4f;
							break;
						}
						Lighting.AddLight(num3 >> 4, num4 >> 4, rGB);
						break;
					}
					case 66:
						if (ptr->Velocity.X < 0f)
						{
							ptr->Rotation--;
						}
						else
						{
							ptr->Rotation++;
						}
						ptr->Velocity.X *= 0.98f;
						ptr->Velocity.Y *= 0.98f;
						ptr->Scale += 0.02f;
						num2 *= 0.0031372549f;
						if (num2 > 1f / 255f)
						{
							num2 = 1f / 255f;
						}
						rGB.X = num2 * (float)(int)ptr->Color.R;
						rGB.Y = num2 * (float)(int)ptr->Color.G;
						rGB.Z = num2 * (float)(int)ptr->Color.B;
						Lighting.AddLight(num3 >> 4, num4 >> 4, rGB);
						break;
					case 20:
					case 21:
						ptr->Scale += 0.005f;
						ptr->Velocity.X *= 0.94f;
						ptr->Velocity.Y *= 0.94f;
						if (type == 21)
						{
							num2 *= 0.4f;
							rGB.X = num2 * 0.8f;
							rGB.Y = num2 * 0.3f;
						}
						else
						{
							num2 *= 0.8f;
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB.X = num2 * 0.3f;
							rGB.Y = num2 * 0.6f;
						}
						rGB.Z = num2;
						Lighting.AddLight(num3 >> 4, num4 >> 4, rGB);
						break;
					case 27:
					case 45:
						ptr->Velocity.X *= 0.94f;
						ptr->Velocity.Y *= 0.94f;
						ptr->Scale += 0.002f;
						if (ptr->NoLight)
						{
							num2 *= 0.1f;
							ptr->Scale -= 0.06f;
							if (ptr->Scale < 1f)
							{
								ptr->Scale -= 0.06f;
							}
							if (View != null)
							{
								if (View.Player.IsWet)
								{
									ptr->Position.X += View.Player.velocity.X * 0.5f;
									ptr->Position.Y += View.Player.velocity.Y * 0.5f;
								}
								else
								{
									ptr->Position.X += View.Player.velocity.X;
									ptr->Position.Y += View.Player.velocity.Y;
								}
							}
						}
						if (num2 > 1f)
						{
							num2 = 1f;
						}
						Lighting.AddLight((int)ptr->Position.X >> 4, (int)ptr->Position.Y >> 4, new Vector3(num2 * 0.6f, num2 * 0.2f, num2));
						break;
					case 55:
					case 56:
					case 73:
					case 74:
						ptr->Velocity.X *= 0.98f;
						ptr->Velocity.Y *= 0.98f;
						switch (type)
						{
						case 55:
							num2 *= 0.8f;
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB = new Vector3(num2, num2, num2 * 0.6f);
							break;
						case 73:
							num2 *= 0.8f;
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB = new Vector3(num2, num2 * 0.35f, num2 * 0.5f);
							break;
						case 74:
							num2 *= 0.8f;
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB = new Vector3(num2 * 0.35f, num2, num2 * 0.5f);
							break;
						default:
							num2 *= 1.2f;
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB = new Vector3(num2 * 0.35f, num2 * 0.5f, num2);
							break;
						}
						Lighting.AddLight(num3 >> 4, num4 >> 4, rGB);
						break;
					case 71:
					case 72:
						ptr->Velocity.X *= 0.98f;
						ptr->Velocity.Y *= 0.98f;
						if (num2 > 1f)
						{
							num2 = 1f;
						}
						Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(num2 * 0.2f, 0f, num2 * 0.1f));
						break;
					case 76:
						SnowDust++;
						ptr->Scale += 0.009f;
						if (View != null)
						{
							if (Collision.SolidCollision(ref ptr->Position, 1, 1))
							{
								ptr->Active = 0;
								continue;
							}
							ptr->Position.X += View.Player.velocity.X * 0.2f;
							ptr->Position.Y += View.Player.velocity.Y * 0.2f;
						}
						break;
					default:
						if (!ptr->NoGravity)
						{
							switch (type)
							{
							case 107:
								ptr->Velocity.Y *= 0.9f;
								break;
							default:
								ptr->Velocity.Y += 0.1f;
								break;
							case 41:
							case 44:
								break;
							}
						}
						else if (type == 5)
						{
							ptr->Scale -= 0.04f;
						}
						break;
					}
					break;
				}
				if (type == 33 || type == 52 || (type >= 98 && type <= 105) || type == 123)
				{
					if (ptr->Velocity.X == 0f)
					{
						if (Collision.SolidCollision(ref ptr->Position, 2, 2))
						{
							ptr->Active = 0;
							continue;
						}
						ptr->Rotation += 0.5f;
						ptr->Scale -= 0.01f;
					}
					if (Collision.WetCollision(ref ptr->Position, 4, 4))
					{
						ptr->Scale -= 0.105f;
						ptr->Alpha += 22;
					}
					else
					{
						ptr->Scale -= 0.005f;
						ptr->Alpha += 2;
					}
					if (ptr->Alpha > 255)
					{
						ptr->Active = 0;
						continue;
					}
					ptr->Velocity.X *= 0.93f;
					if (ptr->Velocity.Y > 4f)
					{
						ptr->Velocity.Y = 4f;
					}
					if (ptr->NoGravity)
					{
						if (ptr->Velocity.X < 0f)
						{
							ptr->Rotation -= 0.2f;
						}
						else
						{
							ptr->Rotation += 0.2f;
						}
						ptr->Scale += 0.03f;
						ptr->Velocity.X *= 1.05f;
						ptr->Velocity.Y += 0.15f;
					}
				}
				else if (type == 152 && ptr->NoGravity)
				{
					ptr->Scale += 0.03f;
					if (ptr->Scale < 1f)
					{
						ptr->Velocity.Y += 0.075f;
					}
					ptr->Velocity.X *= 1.08f;
					if (ptr->Velocity.X > 0f)
					{
						ptr->Rotation += 0.01f;
					}
					else
					{
						ptr->Rotation -= 0.01f;
					}
				}
				else if (type == 67 || type == 92)
				{
					if (num2 > 1f)
					{
						num2 = 1f;
					}
					Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(0f, num2 * 0.8f, num2));
				}
				else if (ptr->Type == 185)
				{
					float num13 = ptr->Scale;
					if (num13 > 1f)
					{
						num13 = 1f;
					}
					if (ptr->NoLight)
					{
						num13 *= 0.1f;
					}
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), new Vector3(num13 * 0.1f, num13 * 0.7f, num13));
				}
				else if (ptr->Type == 107)
				{
					float num14 = ptr->Scale * 0.5f;
					if (num14 > 1f)
					{
						num14 = 1f;
					}
					Lighting.AddLight((int)(ptr->Position.X / 16f), (int)(ptr->Position.Y / 16f), new Vector3(num14 * 0.1f, num14, num14 * 0.4f));
				}
				else if (type == 34 || type == 35 || type == 152)
				{
					if (type == 35)
					{
						LavaBubbles++;
						if (ptr->NoGravity)
						{
							ptr->Scale += 0.03f;
							if (ptr->Scale < 1f)
							{
								ptr->Velocity.Y += 0.075f;
							}
							ptr->Velocity.X *= 1.08f;
							if (ptr->Velocity.X > 0f)
							{
								ptr->Rotation += 0.01f;
							}
							else
							{
								ptr->Rotation -= 0.01f;
							}
							ptr->Velocity.X *= 0.99f;
							num2 = num2 * 0.6f + 0.018f;
							if (num2 > 1f)
							{
								num2 = 1f;
							}
							rGB.X = num2;
							rGB.Y = num2 * 0.3f;
							rGB.Z = num2 * 0.1f;
							Lighting.AddLight(num3 >> 4, (num4 >> 4) + 1, rGB);
							goto IL_20fe;
						}
						num2 = num2 * 0.3f + 0.4f;
						if (num2 > 1f)
						{
							num2 = 1f;
						}
						rGB.X = num2;
						rGB.Y = num2 * 0.5f;
						rGB.Z = num2 * 0.3f;
						Lighting.AddLight(num3 >> 4, num4 >> 4, rGB);
						ptr->Scale -= 0.01f;
						ptr->Velocity.Y = -0.2f;
						ptr->Alpha += (short)Main.Rand.Next(2);
					}
					else if (type == 34)
					{
						ptr->Scale += 0.005f;
						ptr->Velocity.Y = -0.5f;
					}
					else
					{
						ptr->Scale -= 0.01f;
						ptr->Alpha++;
						ptr->Velocity.Y = -0.7f;
					}
					if (++ptr->Alpha > 255)
					{
						ptr->Active = 0;
						continue;
					}
					ptr->Position.Y -= 8f;
					if (!Collision.WetCollision(ref ptr->Position, 4, 4))
					{
						ptr->Active = 0;
						continue;
					}
					ptr->Position.Y += 8f;
					ptr->Velocity.X += (float)Main.Rand.Next(-10, 10) * 0.002f;
					if (ptr->Velocity.X < -0.25f)
					{
						ptr->Velocity.X = -0.25f;
					}
					else if (ptr->Velocity.X > 0.25f)
					{
						ptr->Velocity.X = 0.25f;
					}
				}
				else
				{
					switch (type)
					{
					case 68:
						num2 *= 0.3f;
						if (num2 > 1f)
						{
							num2 = 1f;
						}
						Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(num2 * 0.1f, num2 * 0.2f, num2));
						break;
					case 70:
						num2 *= 0.3f;
						if (num2 > 1f)
						{
							num2 = 1f;
						}
						Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(num2 * 0.5f, 0f, num2));
						break;
					}
				}
				switch (type)
				{
				case 41:
					ptr->Velocity.X += (float)Main.Rand.Next(-10, 11) * 0.01f;
					ptr->Velocity.Y += (float)Main.Rand.Next(-10, 11) * 0.01f;
					if (ptr->Velocity.X > 0.75f)
					{
						ptr->Velocity.X = 0.75f;
					}
					else if (ptr->Velocity.X < -0.75f)
					{
						ptr->Velocity.X = -0.75f;
					}
					if (ptr->Velocity.Y > 0.75f)
					{
						ptr->Velocity.Y = 0.75f;
					}
					else if (ptr->Velocity.Y < -0.75f)
					{
						ptr->Velocity.Y = -0.75f;
					}
					ptr->Scale += 0.007f;
					num2 = num2 * 0.7f + 0.0049f;
					if (num2 > 1f)
					{
						num2 = 1f;
					}
					Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(num2 * 0.4f, num2 * 0.9f, num2));
					break;
				case 44:
					ptr->Velocity.X += (float)Main.Rand.Next(-10, 11) * 0.003f;
					ptr->Velocity.Y += (float)Main.Rand.Next(-10, 11) * 0.003f;
					if (ptr->Velocity.X > 0.35f)
					{
						ptr->Velocity.X = 0.35f;
					}
					else if (ptr->Velocity.X < -0.35f)
					{
						ptr->Velocity.X = -0.35f;
					}
					if (ptr->Velocity.Y > 0.35f)
					{
						ptr->Velocity.Y = 0.35f;
					}
					else if (ptr->Velocity.Y < -0.35f)
					{
						ptr->Velocity.Y = -0.35f;
					}
					ptr->Scale += 0.0085f;
					num2 = num2 * 0.7f + 0.00595f;
					if (num2 > 1f)
					{
						num2 = 1f;
					}
					Lighting.AddLight(num3 >> 4, num4 >> 4, new Vector3(num2 * 0.7f, num2, num2 * 0.8f));
					break;
				default:
					ptr->Velocity.X *= 0.99f;
					break;
				}
				goto IL_20fe;
				IL_20fe:
				if (ptr->FadeIn > 0f)
				{
					if (type == 46)
					{
						ptr->Scale += 0.1f;
					}
					else
					{
						ptr->Scale += 0.03f;
					}
					if (ptr->Scale > ptr->FadeIn)
					{
						ptr->FadeIn = 0f;
					}
				}
				else
				{
					ptr->Scale -= 0.01f;
				}
				if (ptr->NoGravity)
				{
					ptr->Velocity.X *= 0.92f;
					ptr->Velocity.Y *= 0.92f;
					if (ptr->FadeIn == 0f)
					{
						ptr->Scale -= 0.04f;
					}
				}
				if (ptr->Scale < 0.1f)
				{
					ptr->Active = 0;
				}
				else if (type != 79)
				{
					ptr->Rotation += ptr->Velocity.X * 0.5f;
				}
			}
		}
	}

	public unsafe void DrawDust(WorldView DrawView)
	{
		fixed (Dust* dusts = Dusts)
		{
			Dust* ptr = dusts;
			Vector2 pivot = new Vector2(4f, 4f);
			Vector2 pos = default;
			new Rectangle(DrawView.ScreenPosition.X - 500, DrawView.ScreenPosition.Y - 50, Main.ResolutionWidth + 1000, Main.ResolutionHeight + 100);
			for (int num = PoolSize - 1; num >= 0; num--)
			{
				if (ptr->Active != 0)
				{
					int num2 = (int)ptr->Position.X;
					int num3 = (int)ptr->Position.Y;
					if (View != null || DrawView.ClipArea.Contains(num2, num3))
					{
						Color DustColor = ((!ptr->NoLight && ptr->Type != 6 && ptr->Type != 15 && (ptr->Type < 59 || ptr->Type > 64)) ? DrawView.Lighting.GetColor(num2 + 4 >> 4, num3 + 4 >> 4) : Color.White);
						ptr->GetAlpha(ref DustColor);
						if (DustColor.PackedValue == 0)
						{
							ptr->Active = 0;
						}
						else
						{
							pos.X = num2 - DrawView.ScreenPosition.X;
							pos.Y = num3 - DrawView.ScreenPosition.Y;
							SpriteSheet<_sheetSprites>.Draw(218, ref pos, ref ptr->Frame, DustColor, ptr->Rotation, ref pivot, ptr->Scale);
							if (ptr->Color.PackedValue != 0)
							{
								ptr->GetColor(ref DustColor);
								SpriteSheet<_sheetSprites>.Draw(218, ref pos, ref ptr->Frame, DustColor, ptr->Rotation, ref pivot, ptr->Scale);
							}
						}
					}
				}
				ptr++;
			}
		}
	}
}
