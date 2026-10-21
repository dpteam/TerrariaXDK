using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria;

public sealed class MiniMap
{
	private const int ScrollingSpeed = 8;

	private const float ScalingSpeed = 0.05f;

	private const float MinScale = 1f;

	private const float MaxScale = 4f;

	private const int MapOffsetX = 290;

	private const int SCMapOffsetX = 340;

	private const int MapTexSlices = 4;

	private static short Width;

	private static short Height;

	private static short TexWidth;

	private short MapX;

	private short MapY;

	private float MapScale = 1f;

	public volatile bool IsThreadDone;

	private short MapDestH = 486;

	private int Alpha;

	private Texture2D[] MapTexture;

	private static readonly uint[] WallColors = new uint[32]
	{
		0u, 4282532418u, 4283972910u, 4281410885u, 4283051548u, 4282729797u, 4283432960u, 4278190176u, 4278210560u, 4282908751u,
		4286007047u, 4286743170u, 4282325255u, 4282320647u, 4278190176u, 4278211840u, 4283972910u, 4278190147u, 4278204160u, 4281532471u,
		4278190176u, 4279378988u, 4285621091u, 4283318338u, 4282396723u, 4278919998u, 4282145594u, 4282001693u, 4283520101u, 4289331200u,
		4278233600u, 4288256443u
	};

	private static readonly uint[] TileColors = new uint[150]
	{
		4287720015u, 4286611584u, 4280080478u, 4279067940u, 4294786563u, 4284696113u, 4285225294u, 4291188253u, 4290356247u, 4292468703u,
		4278255602u, 4278255602u, 4294901760u, 4278255602u, 4278255602u, 4278255602u, 4278255602u, 4278255602u, 4278255602u, 4285217304u,
		4279067940u, 4292720640u, 4284637095u, 4287465951u, 4287465951u, 4283124354u, 4287627519u, 4291100436u, 4287375142u, 4278255602u,
		4285024564u, 4278190080u, 4286210447u, 4294786563u, 4294786563u, 4294786563u, 4294786563u, 4292845449u, 4287664272u, 4289855743u,
		4289485645u, 4283716760u, 4293890123u, 4281968721u, 4289855743u, 4294956308u, 4293256677u, 4294924544u, 4285361517u, 4281044991u,
		4278255602u, 4294967295u, 4279067940u, 4294957624u, 1090519039u, 4294946398u, 4283912621u, 4282664012u, 4284883490u, 4284236873u,
		4287616797u, 4287616797u, 4287286812u, 4280976122u, 4280976122u, 4280976122u, 4280976122u, 4280976122u, 4280976122u, 4284362807u,
		4284317695u, 4289834627u, 4288057198u, 4279067940u, 4279067940u, 4289855743u, 4291166237u, 4292149264u, 4278255602u, 4278255602u,
		4278232320u, 4293219136u, 4294932480u, 4294932480u, 4294932480u, 4290822336u, 4294902015u, 4278255602u, 4278255602u, 4278255602u,
		4278255602u, 4278255602u, 4278255602u, 4278255602u, 4278255602u, 4278255602u, 4278255602u, 4294902015u, 4294902015u, 4294901968u,
		4294902015u, 4294902015u, 4294902015u, 4294902015u, 4294902015u, 4278255602u, 4278255602u, 4278931599u, 4284197289u, 4283351523u,
		4280194632u, 4286585396u, 4284965498u, 4280194632u, 4286536773u, 4281499553u, 4292199621u, 4290096318u, 4292199621u, 4282335049u,
		4288051837u, 4280645291u, 4287741813u, 4284044115u, 4284236854u, 4286686719u, 4292598747u, 4285051848u, 4287653968u, 4278208889u,
		4289045925u, 4279900698u, 4291363587u, 4287172626u, 4288065159u, 4294799986u, 4291608768u, 4287401100u, 4284703587u, 4288242499u,
		4286084531u, 4289536803u, 4291363587u, 4291363587u, 4291363587u, 4294901760u, 4278255360u, 4293848831u, 4291611886u, 4294901760u
	};

	/// <summary>
	/// Horizontal space the map leaves on the left, before the safe area.
	///
	/// One helper because the map's own pan bounds and its drawing used to disagree: the drawing
	/// scaled these numbers by <c>ScreenMultiplier</c> and the bounds did not, so at 720p and above
	/// the map could be scrolled past its own edge.
	/// </summary>
	private static int MapLeftInset()
	{
		return (int)((UI.NumActiveViews > 1 ? 340f : 290f) * Main.ScreenMultiplier);
	}

	public static void OnStartGame()	{
		Width = (short)(Main.MaxTilesX - 68);
		Height = (short)(Main.MaxTilesY - 68);
		TexWidth = (short)(Width / 4);
	}

	public void UpdateMap(UI ActiveUI)
	{
		int num = ActiveUI.CurrentView.SafeAreaOffsetLeft + MapLeftInset();
		int num2 = ActiveUI.CurrentView.ViewWidth - ActiveUI.CurrentView.SafeAreaOffsetRight - num;
		if (ActiveUI.IsAlternateLeftButtonDown())
		{
			MapX -= 8;
		}
		if (ActiveUI.IsAlternateRightButtonDown())
		{
			MapX += 8;
		}
		if (ActiveUI.IsAlternateUpButtonDown())
		{
			MapY -= 8;
		}
		if (ActiveUI.IsAlternateDownButtonDown())
		{
			MapY += 8;
		}
		float left = ActiveUI.PadState.Triggers.Left;
		if (left > 0.125f)
		{
			MapScale -= 0.05f * left;
			if (MapScale < 1f)
			{
				MapScale = 1f;
			}
		}
		left = ActiveUI.PadState.Triggers.Right;
		if (left > 0.125f)
		{
			MapScale += 0.05f * left;
			if (MapScale > 4f)
			{
				MapScale = 4f;
			}
		}
		int num3 = (int)((double)((float)num2 * (1f / MapScale - 1f)) * 0.5);
		int num4 = Width - num2 - num3;
		if (MapX < num3)
		{
			MapX = (short)num3;
		}
		else if (MapX > num4)
		{
			MapX = (short)num4;
		}
		num3 = (int)((double)((float)MapDestH * (1f / MapScale - 1f)) * 0.5);
		num4 = Height - MapDestH - num3;
		if (MapY < num3)
		{
			MapY = (short)num3;
		}
		else if (MapY > num4)
		{
			MapY = (short)num4;
		}
	}

	public void CreateMap(UI CurrentUI)
	{
		if (IsThreadDone)
		{
			DestroyMap();
		}
		Alpha = 0;
		MapTexture = new Texture2D[4];
		for (int num = 3; num >= 0; num--)
		{
			MapTexture[num] = new Texture2D(WorldView.GraphicsDevice, TexWidth, Height, mipMap: false, SurfaceFormat.Bgr565);
		}
		Thread thread = new Thread(CreateMapThread);
		thread.IsBackground = true;
		thread.Start(CurrentUI);
	}

	public unsafe void CreateMapThread(object UIArg)
	{
		UI uI = (UI)UIArg;
		int num = TexWidth * Height;
		ushort[] array = new ushort[num];
		sbyte[] array2 = new sbyte[Height];
		sbyte[] array3 = new sbyte[Width];
		Player activePlayer = uI.ActivePlayer;
		for (int num2 = 3; num2 >= 0; num2--)
		{
			fixed (ushort* ptr = &array[num - 1])
			{
				ushort* ptr2 = ptr;
				for (int num3 = Height - 1; num3 >= 0; num3--)
				{
					int num4 = TexWidth - 1;
					int num5 = num4 + num2 * TexWidth;
					while (num4 >= 0)
					{
						fixed (Tile* ptr3 = &Main.TileSet[num5 + 34, num3 + 34])
						{
							if ((ptr3->CurrentFlags & Tile.Flags.VISITED) == 0)
							{
								if (array2[num3] > 0)
								{
									array2[num3]--;
								}
								if (array3[num5] > 0)
								{
									array3[num5]--;
								}
							}
							else
							{
								if (array2[num3] < 4)
								{
									array2[num3]++;
								}
								if (array3[num5] < 4)
								{
									array3[num5]++;
								}
							}
							if (array2[num3] <= 0 && array3[num5] <= 0)
							{
								*ptr2 = 0;
							}
							else
							{
								int num6;
								if (array2[num3] < array3[num5])
								{
									num6 = (array2[num3] + 1) * 4 + array3[num5] * array3[num5];
								}
								else
								{
									num6 = ((array2[num3] <= array3[num5]) ? (array2[num3] * array2[num3] + array3[num5] * array3[num5]) : (array2[num3] * array2[num3] + (array3[num5] + 1) * 4));
								}
								uint num8;
								uint num9;
								uint num10;
								if (ptr3->IsActive != 0)
								{
									uint num7 = TileColors[ptr3->Type];
									num8 = num7 & 0xFF;
									num9 = (num7 >> 8) & 0xFF;
									num10 = (num7 >> 16) & 0xFF;
								}
								else if (ptr3->WallType == 0)
								{
									if (num3 < Main.WorldSurface)
									{
										num10 = (uint)((num3 >> 1) * uI.CurrentView.WorldTime.bgColor.R / Main.WorldSurface);
										num9 = (uint)(num3 * uI.CurrentView.WorldTime.bgColor.G / (int)((float)Main.WorldSurface * 1.2f));
										num8 = (uint)((num3 << 1) * uI.CurrentView.WorldTime.bgColor.B / Main.WorldSurface);
										if (num8 > 255)
										{
											num8 = 255u;
										}
									}
									else if (num3 < Main.RockLayer)
									{
										num10 = 84u;
										num9 = 57u;
										num8 = 42u;
									}
									else if (num3 >= Main.MaxTilesY - 200)
									{
										num10 = 51u;
										num9 = 0u;
										num8 = 0u;
									}
									else
									{
										num10 = 72u;
										num9 = 64u;
										num8 = 57u;
									}
								}
								else
								{
									uint num11 = WallColors[ptr3->WallType];
									num8 = num11 & 0xFF;
									num9 = (num11 >> 8) & 0xFF;
									num10 = (num11 >> 16) & 0xFF;
								}
								uint liquid = ptr3->Liquid;
								if (liquid != 0)
								{
									if (ptr3->Lava == 32)
									{
										num10 = (num10 * (255 - liquid) >> 8) + liquid;
										num8 >>= 1;
									}
									else
									{
										num10 >>= 1;
										num8 = (num8 * (255 - liquid) >> 8) + liquid;
									}
									num9 >>= 1;
								}
								if (num6 < 32)
								{
									num10 = (uint)(num10 * num6) >> 5;
									num9 = (uint)(num9 * num6) >> 5;
									num8 = (uint)(num8 * num6) >> 5;
								}
								*ptr2 = (ushort)((num10 >> 3 << 11) | (num9 >> 2 << 5) | (num8 >> 3));
							}
							ptr2--;
						}
						num4--;
						num5--;
					}
				}
			}
			MapTexture[num2].SetData(array);
		}
		int num12 = uI.CurrentView.SafeAreaOffsetLeft + MapLeftInset();
		int num13 = uI.CurrentView.ViewWidth - uI.CurrentView.SafeAreaOffsetRight - num12;
		int num14 = Width - num13;
		int num15 = Height - MapDestH;
		MapX = (short)((activePlayer.XYWH.X >> 4) - 34 - (num13 >> 1));
		if (MapX < 0)
		{
			MapX = 0;
		}
		else if (MapX > num14)
		{
			MapX = (short)num14;
		}
		MapY = (short)((activePlayer.XYWH.Y >> 4) - 34 - (MapDestH >> 1));
		if (MapY < 0)
		{
			MapY = 0;
		}
		else if (MapY > num15)
		{
			MapY = (short)num15;
		}
		IsThreadDone = true;
	}

	public void DestroyMap()
	{
		IsThreadDone = false;
		if (MapTexture != null)
		{
			for (int i = 0; i < 4; i++)
			{
				MapTexture[i].Dispose();
				MapTexture[i] = null;
			}
			MapTexture = null;
		}
	}

	public void DrawMap(WorldView ActiveView)
	{
		int num = ActiveView.SafeAreaOffsetLeft + MapLeftInset();
		int safeAreaOffsetTop = ActiveView.SafeAreaOffsetTop;
		MapDestH = (short)(Main.ResolutionHeight - ActiveView.SafeAreaOffsetTop - ActiveView.SafeAreaOffsetBottom - (int)(36f * Main.ScreenMultiplier));
		int num2 = ActiveView.ViewWidth - ActiveView.SafeAreaOffsetRight - num;
		if (!IsThreadDone)
		{
			// The batch is left open on purpose: this method always returns with a batch open and
			// the caller (UI's map screen) closes it. Ending it here made the caller's own End
			// the mismatch, so the spinner path must match the normal path, not "look balanced".
			Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, ActiveView.ScreenProjection);
			SpriteSheet<_sheetSprites>.Draw(1012, num + (num2 >> 1), safeAreaOffsetTop + (MapDestH >> 1), Color.White, (float)((double)Main.FrameCounter * (Math.PI / 30.0)), 1f);
			return;
		}
		Alpha += 16;
		if (Alpha > 255)
		{
			Alpha = 255;
		}
		int num3 = num + (num2 >> 1);
		int num4 = safeAreaOffsetTop + (MapDestH >> 1);
		Matrix view = Matrix.CreateTranslation(-num3, -num4, 0f) * Matrix.CreateScale(MapScale, MapScale, 1f) * Matrix.CreateTranslation(num3, num4, 0f);
		ActiveView.ScreenProjection.View = view;
		Rectangle scissorRectangle = default;
		if (ActiveView.IsFullScreen())
		{
			scissorRectangle.X = num;
			scissorRectangle.Y = safeAreaOffsetTop;
			scissorRectangle.Width = num2;
			scissorRectangle.Height = MapDestH;
		}
		else
		{
			scissorRectangle.X = (num >> 1) + ActiveView.ActiveViewport.X;
			scissorRectangle.Y = (safeAreaOffsetTop >> 1) + ActiveView.ActiveViewport.Y;
			scissorRectangle.Width = num2 >> 1;
			scissorRectangle.Height = MapDestH >> 1;
		}
		WorldView.GraphicsDevice.ScissorRectangle = scissorRectangle;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, WorldView.ScissorRastTest, ActiveView.ScreenProjection);
		int num5 = MapX / TexWidth;
		int num6 = (MapX + num2 - 1) / TexWidth;
		if (num6 >= MapTexture.Length)
		{
			num6 = MapTexture.Length - 1;
		}
		Vector2 position = new Vector2(num, safeAreaOffsetTop);
		Rectangle rectangle = default;
		rectangle.X = MapX - num5 * TexWidth;
		rectangle.Y = MapY;
		rectangle.Width = Math.Min(num2, TexWidth - rectangle.X);
		rectangle.Height = MapDestH;
		Color color = new Color(Alpha, Alpha, Alpha, Alpha);
		for (int i = num5; i <= num6; i++)
		{
			Main.SpriteBatch.Draw(MapTexture[i], position, rectangle, color);
			position.X += rectangle.Width;
			rectangle.X = 0;
			rectangle.Width = TexWidth;
		}
		switch (Main.MagmaBGFrame)
		{
		case 0:
			rectangle.X = 659;
			rectangle.Y = 10;
			break;
		case 1:
			rectangle.X = 659;
			rectangle.Y = 0;
			break;
		default:
			rectangle.X = 759;
			rectangle.Y = 10;
			break;
		}
		rectangle.Width = 10;
		rectangle.Height = 10;
		for (int num7 = 195; num7 >= 0; num7--)
		{
			NPC nPC = Main.NPCSet[num7];
			if (nPC.Active != 0)
			{
				int num8 = nPC.XYWH.X >> 4;
				int num9 = nPC.XYWH.Y >> 4;
				if (num8 >= 0 && num9 >= 0 && num8 < Main.MaxTilesX && num9 < Main.MaxTilesY && (Main.TileSet[num8, num9].CurrentFlags & Tile.Flags.VISITED) == Tile.Flags.VISITED)
				{
					num8 -= 34 + MapX;
					if (num8 >= 0 && num8 + 4 < num2)
					{
						num9 -= 34 + MapY;
						if (num9 >= 0 && num9 + 4 < MapDestH)
						{
							int headTextureID = nPC.GetHeadTextureID();
							if (headTextureID < 0)
							{
								color = new Color(106, 0, 66, 127);
								color.R = (byte)(color.R * UI.CursorColour.A >> 8);
								color.G = (byte)(color.G * UI.CursorColour.A >> 8);
								color.B = (byte)(color.B * UI.CursorColour.A >> 8);
								color.A = (byte)(color.A * UI.CursorColour.A >> 8);
								SpriteSheet<_sheetSprites>.DrawCentered(218, num + num8, safeAreaOffsetTop + num9, rectangle, color);
							}
							else
							{
								SpriteSheet<_sheetSprites>.DrawScaled(1255 + headTextureID, num + num8, safeAreaOffsetTop + num9, 0.5f, new Color(248, 248, 248, 248), (nPC.SpriteDirection >= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
							}
						}
					}
				}
			}
		}
		Main.SpriteBatch.End();
		Matrix world = ActiveView.ScreenProjection.World;
		float num10 = (float)(0.6875 + 0.0625 * Math.Sin((float)Main.FrameCounter * (1f / 12f)));
		view = Matrix.CreateTranslation(-10f, -8f, 0f) * Matrix.CreateScale(num10, num10, 1f);
		for (int num11 = 7; num11 >= 0; num11--)
		{
			Player player = Main.PlayerSet[num11];
			if (player.Active != 0 && !player.IsDead)
			{
				int num12 = player.XYWH.X >> 4;
				int num13 = player.XYWH.Y >> 4;
				if ((Main.TileSet[num12, num13].CurrentFlags & Tile.Flags.VISITED) != 0)
				{
					num12 -= 34 + MapX;
					num13 -= 34 + MapY;
					Vector2 position2 = player.Position;
					player.Position.X = ActiveView.ScreenPosition.X;
					player.Position.Y = ActiveView.ScreenPosition.Y;
					ActiveView.ScreenProjection.World = view * Matrix.CreateTranslation(num + num12, safeAreaOffsetTop + num13, 0f);
					Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, WorldView.ScissorRastTest, ActiveView.ScreenProjection);
					player.Draw(ActiveView, IsMenuScr: true, IsIcon: true);
					Main.SpriteBatch.End();
					player.Position = position2;
					player.XYWH.X = (int)position2.X;
					player.XYWH.Y = (int)position2.Y;
				}
			}
		}
		ActiveView.ScreenProjection.World = world;
		ActiveView.ScreenProjection.View = Matrix.Identity;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, ActiveView.ScreenProjection);
	}
}
