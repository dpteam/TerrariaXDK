using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria;

public class WorldView : IDisposable
{
	public enum Type
	{
		FULLSCREEN,
		TOP_LEFT,
		TOP_RIGHT,
		BOTTOM_LEFT,
		BOTTOM_RIGHT,
		TOP,
		BOTTOM,
		NONE
	}

	private struct Spec
	{
		public short X;

		public short Y;

		public Color tileColor;
	}

	public const bool DRAW_TO_SCREEN = false;

	public const int OFFSCREEN_RANGE_X = 32;

	public const int OFFSCREEN_RANGE_TOP = 32;

	public const int OFFSCREEN_RANGE_BOTTOM = 64;

	public const int OFFSCREEN_RANGE_VERTICAL = 96;

	public const float CAVE_PARALLAX = 0.9f;

	public const float gfxQuality = 0.25f;

	private const double VIEWPORT_ANIM_STEPS = 30.0;

	private const double VIEWPORT_ANIM_THETA_DELTA = Math.PI / 60.0;

	private const double ZOOM_ANIM_STEPS = 90.0;

	private const double ZOOM_ANIM_THETA_DELTA = Math.PI / 180.0;

	private const int CLIP_AREA_EXTRA_WIDTH = 32;

	private const int CLIP_AREA_EXTRA_HEIGHT = 64;

	public const int MAX_BACKGROUNDS = 32;

	private bool SMOOTH_LIGHT = true;

	private bool isDisposed;

	public short ViewWidth;

	private Type viewType = Type.NONE;

	public int SafeAreaOffsetLeft = Main.ResolutionWidth / 20;

	public int SafeAreaOffsetTop = Main.ResolutionHeight / 20;

	public int SafeAreaOffsetRight = Main.ResolutionWidth / 20;

	public int SafeAreaOffsetBottom = Main.ResolutionHeight / 20;

	private int currentSAFE_AREA_OFFSET_L = Main.ResolutionWidth / 20;

	private int currentSAFE_AREA_OFFSET_T = Main.ResolutionHeight / 20;

	private int currentSAFE_AREA_OFFSET_R = Main.ResolutionWidth / 20;

	private int currentSAFE_AREA_OFFSET_B = Main.ResolutionHeight / 20;

	private int targetSAFE_AREA_OFFSET_L = Main.ResolutionWidth / 20;

	private int targetSAFE_AREA_OFFSET_T = Main.ResolutionHeight / 20;

	private int targetSAFE_AREA_OFFSET_R = Main.ResolutionWidth / 20;

	private int targetSAFE_AREA_OFFSET_B = Main.ResolutionHeight / 20;

	private Viewport currentViewport;

	private Viewport targetViewport;

	public Viewport ActiveViewport;

	private double viewportAnimTheta;

	public Player Player;

	public UI Ui;

	public Time WorldTime;

	public float AtmoOpacity;

	public short FirstTileX;

	public short LastTileX;

	public short FirstTileY;

	public short LastTileY;

	public Rectangle ClipArea = new Rectangle(0, 0, 0, Main.ResolutionHeight + 64);

	public Rectangle ViewArea = new Rectangle(0, 0, 0, Main.ResolutionHeight);

	public Vector2i ScreenPosition;

	public Vector2i ScreenLastPosition;

	public int quickBG = 2;

	public int bgDelay;

	public int bgStyle;

	public float[] bgAlpha = new float[8];

	public float[] bgAlpha2 = new float[8];

	public DustPool dustLocal;

	public ItemTextPool itemTextLocal;

	public Lighting Lighting = new Lighting();

	public int InactiveTiles;

	public int SandTiles;

	public int EvilTiles;

	public int SnowTiles;

	public int HolyTiles;

	public int MeteorTiles;

	public int JungleTiles;

	public int DungeonTiles;

	public int SkyTiles;

	public int MusicBox = -1;

	public bool[] drawNpcName = new bool[196];

	public Vector2i sceneWaterPos;

	public Vector2i sceneTilePos;

	public Vector2i sceneTile2Pos;

	public Vector2i sceneWallPos;

	public Vector2i sceneBackgroundPos;

	public Vector2i sceneBlackPos;

	private RenderTarget2D backWaterTarget;

	private RenderTarget2D waterTarget;

	private RenderTarget2D tileSolidTarget;

	private RenderTarget2D blackTarget;

	private RenderTarget2D tileNonSolidTarget;

	private RenderTarget2D wallTarget;

	private RenderTarget2D backgroundTarget;

	private static readonly short ViewportWidth = (short)Main.ResolutionWidth;

	private static readonly short ViewportHeight = (short)Main.ResolutionHeight;

	public static readonly short[] VIEW_WIDTH = new short[7]
	{
		ViewportWidth,
		ViewportWidth,
		ViewportWidth,
		ViewportWidth,
		ViewportWidth,
		(short)(ViewportWidth * 2),
		(short)(ViewportWidth * 2)
	};

	public static readonly byte[] SAFE_AREA_OFFSETS = new byte[28]
	{
		(byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X,
		(byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_Y,
		(byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X,
		(byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_Y,
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X * 2),
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_Y * 2),
		0,
		0,
		0,
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_Y * 2),
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X * 2),
		0,
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X * 2),
		0,
		0,
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_Y * 2),
		0,
		0,
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X * 2),
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_Y * 2),
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X * 2),
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_Y * 2),
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X * 2),
		0,
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X * 2),
		0,
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_X * 2),
		(byte)((byte)UI.FULLSCREEN_SAFE_AREA_OFFSET_Y * 2)
	};

	public static readonly Viewport[] VIEWPORT = new Viewport[7]
	{
		new Viewport(0, 0, ViewportWidth, ViewportHeight),
		new Viewport(0, 0, ViewportWidth / 2, ViewportHeight / 2),
		new Viewport(ViewportWidth / 2, 0, ViewportWidth / 2, ViewportHeight / 2),
		new Viewport(0, ViewportHeight / 2, ViewportWidth / 2, ViewportHeight / 2),
		new Viewport(ViewportWidth / 2, ViewportHeight / 2, ViewportWidth / 2, ViewportHeight / 2),
		new Viewport(0, 0, ViewportWidth, ViewportHeight / 2),
		new Viewport(0, ViewportHeight / 2, ViewportWidth, ViewportHeight / 2)
	};

	public static GraphicsDevice GraphicsDevice;

	private static Matrix halfpixelOffset;

	private static Matrix centerWideSplitscreen;

	private BasicEffect renderTargetProjection;

	public BasicEffect ScreenProjection;

	public static RasterizerState ScissorRastTest;

	private Matrix viewMtx = Matrix.Identity;

	private float worldScale = 1f;

	private float worldScaleTarget = 1f;

	private float worldScalePrevious = 1f;

	private double worldScaleAnimTheta;

	public static Texture2D[] backgroundTexture = new Texture2D[32];

	private Spec[] spec = new Spec[512];

	public static Type getViewType(PlayerIndex controller, UI newUI = null)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < 4; i++)
		{
			UI uI = Main.UIInstance[i];
			if (uI == newUI || uI.CurrentView != null)
			{
				num2++;
				if (uI.controller < controller)
				{
					num++;
				}
			}
		}
		if (1 == 0)
		{
		}
		Type result;
		switch (num2)
		{
		case 2:
			result = (Type)(5 + num);
			break;
		case 3:
		{
			if (1 == 0)
			{
			}
			Type type = num switch
			{
				0 => Type.TOP, 
				1 => Type.BOTTOM_LEFT, 
				_ => Type.BOTTOM_RIGHT, 
			};
			if (1 == 0)
			{
			}
			result = type;
			break;
		}
		case 4:
			result = (Type)(1 + num);
			break;
		default:
			result = Type.FULLSCREEN;
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	public static bool AnyViewContains(int X, int Y)
	{
		int num = UI.NumActiveViews;
		do
		{
			if (UI.activeView[--num].ClipArea.Contains(X, Y))
			{
				return true;
			}
		}
		while (num > 0);
		return false;
	}

	public static bool AnyViewIntersects(ref Rectangle rect)
	{
		int num = UI.NumActiveViews;
		bool result;
		do
		{
			UI.activeView[--num].ClipArea.Intersects(ref rect, out result);
		}
		while (!result && num > 0);
		return result;
	}

	public static void LoadContent(ContentManager Content)
	{
		for (int i = 3; i < 32; i++)
		{
			backgroundTexture[i] = Content.Load<Texture2D>("Images/Background_" + i);
		}
	}

	public static void Initialize(GraphicsDevice device)
	{
		GraphicsDevice = device;
		Matrix.CreateTranslation(0f, 0f, 0f, out halfpixelOffset);
		Matrix.CreateTranslation(Main.ResolutionWidth / 2, 0f, 0f, out centerWideSplitscreen);
		ScissorRastTest = new RasterizerState();
		ScissorRastTest.ScissorTestEnable = true;
	}

	public WorldView()
	{
		dustLocal = new DustPool(this, 128);
		itemTextLocal = new ItemTextPool(this);
		bgAlpha[0] = 1f;
		bgAlpha2[0] = 1f;
		renderTargetProjection = new BasicEffect(GraphicsDevice)
		{
			World = Matrix.Identity,
			View = Matrix.Identity,
			TextureEnabled = true,
			VertexColorEnabled = true
		};
		ScreenProjection = new BasicEffect(GraphicsDevice)
		{
			World = Matrix.Identity,
			View = Matrix.Identity,
			TextureEnabled = true,
			VertexColorEnabled = true
		};
	}

	public void OnStartGame()
	{
		Lighting.StartWorkerThread();
	}

	public void onStopGame()
	{
		Lighting.StopWorkerThread();
		itemTextLocal.Clear();
	}

	public bool setViewType(Type type = Type.FULLSCREEN)
	{
		if (type == viewType)
		{
			return false;
		}
		if (type != Type.FULLSCREEN)
		{
			SMOOTH_LIGHT = false;
		}
		else
		{
			SMOOTH_LIGHT = true;
			Zoom(1f);
		}
		Type type2 = viewType;
		bool flag = IsFullScreen();
		int viewWidth = ViewWidth;
		viewType = type;
		if (type != Type.NONE)
		{
			int num = VIEW_WIDTH[(int)type];
			if (viewWidth != num)
			{
				ViewWidth = (short)num;
				ClipArea.Width = num + 32;
				ViewArea.Width = num;
				Lighting.SetWidth(num);
			}
			currentViewport = ActiveViewport;
			targetViewport = VIEWPORT[(int)type];
			currentSAFE_AREA_OFFSET_L = SafeAreaOffsetLeft;
			currentSAFE_AREA_OFFSET_T = SafeAreaOffsetTop;
			currentSAFE_AREA_OFFSET_R = SafeAreaOffsetRight;
			currentSAFE_AREA_OFFSET_B = SafeAreaOffsetBottom;
			targetSAFE_AREA_OFFSET_L = SAFE_AREA_OFFSETS[(int)type << 2];
			targetSAFE_AREA_OFFSET_T = SAFE_AREA_OFFSETS[((int)type << 2) + 1];
			targetSAFE_AREA_OFFSET_R = SAFE_AREA_OFFSETS[((int)type << 2) + 2];
			targetSAFE_AREA_OFFSET_B = SAFE_AREA_OFFSETS[((int)type << 2) + 3];
			if (type2 != Type.NONE && flag == IsFullScreen() && viewWidth == ViewWidth)
			{
				viewportAnimTheta = Math.PI / 2.0;
				return false;
			}
			SafeAreaOffsetLeft = targetSAFE_AREA_OFFSET_L;
			SafeAreaOffsetTop = targetSAFE_AREA_OFFSET_T;
			SafeAreaOffsetRight = targetSAFE_AREA_OFFSET_R;
			SafeAreaOffsetBottom = targetSAFE_AREA_OFFSET_B;
			ActiveViewport = targetViewport;
			viewportAnimTheta = 0.0;
			UpdateProjection();
			int num2 = ViewWidth + 64;
			int num3 = ViewportHeight + 96;
			if (backWaterTarget == null || backWaterTarget.Width != num2 || backWaterTarget.Height != num3)
			{
				DisposeRendertargets();
				backWaterTarget = new RenderTarget2D(GraphicsDevice, num2, num3, mipMap: false, SurfaceFormat.Color, DepthFormat.None);
				waterTarget = new RenderTarget2D(GraphicsDevice, num2, num3, mipMap: false, SurfaceFormat.Color, DepthFormat.None);
				blackTarget = new RenderTarget2D(GraphicsDevice, num2, num3, mipMap: false, SurfaceFormat.Color, DepthFormat.None);
				tileSolidTarget = new RenderTarget2D(GraphicsDevice, num2, num3, mipMap: false, SurfaceFormat.Color, DepthFormat.None);
				tileNonSolidTarget = new RenderTarget2D(GraphicsDevice, num2, num3, mipMap: false, SurfaceFormat.Color, DepthFormat.None);
				wallTarget = new RenderTarget2D(GraphicsDevice, num2, num3, mipMap: false, SurfaceFormat.Color, DepthFormat.None);
				backgroundTarget = new RenderTarget2D(GraphicsDevice, num2, num3, mipMap: false, SurfaceFormat.Color, DepthFormat.None);
			}
			return true;
		}
		DisposeRendertargets();
		return true;
	}

	public void Dispose()
	{
		Lighting.StopWorkerThread();
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void DisposeRendertargets()
	{
		if (backWaterTarget != null)
		{
			backWaterTarget.Dispose();
			backWaterTarget = null;
			waterTarget.Dispose();
			waterTarget = null;
			tileSolidTarget.Dispose();
			tileSolidTarget = null;
			blackTarget.Dispose();
			blackTarget = null;
			tileNonSolidTarget.Dispose();
			tileNonSolidTarget = null;
			wallTarget.Dispose();
			wallTarget = null;
			backgroundTarget.Dispose();
			backgroundTarget = null;
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				DisposeRendertargets();
			}
			isDisposed = true;
		}
	}

	public bool IsFullScreen()
	{
		return viewType == Type.FULLSCREEN;
	}

	public static void RestoreViewport()
	{
		GraphicsDevice.Viewport = VIEWPORT[0];
	}

	public void PrepareDraw(int pass)
	{
		ScreenLastPosition = ScreenPosition;
		Player.updateScreenPosition();
		if (ScreenPosition.X < 560)
		{
			ScreenPosition.X = 560;
		}
		else if (ScreenPosition.X + ViewWidth > Main.RightWorld - 544 - 32)
		{
			ScreenPosition.X = Main.RightWorld - ViewWidth - 544 - 32;
		}
		if (ScreenPosition.Y < 560)
		{
			ScreenPosition.Y = 560;
		}
		else if (ScreenPosition.Y + Main.ResolutionHeight > Main.BottomWorld - 544 - 32)
		{
			ScreenPosition.Y = Main.BottomWorld - Main.ResolutionHeight - 544 - 32;
		}
		ViewArea.X = ScreenPosition.X;
		ViewArea.Y = ScreenPosition.Y;
		ClipArea.X = ScreenPosition.X - 16;
		ClipArea.Y = ScreenPosition.Y - 32;
		FirstTileX = (short)(ScreenPosition.X >> 4);
		FirstTileY = (short)(ScreenPosition.Y >> 4);
		LastTileX = (short)(FirstTileX + (ViewWidth >> 4));
		if (LastTileX > Main.MaxTilesX)
		{
			LastTileX = Main.MaxTilesX;
		}
		LastTileY = (short)(FirstTileY + (ViewportHeight >> 4));
		if (LastTileY > Main.MaxTilesY)
		{
			LastTileY = Main.MaxTilesY;
		}
		Lighting.LightTiles(this);
		FirstTileX -= (short)(2f * Main.ScreenMultiplier);
		if (FirstTileX < 0)
		{
			FirstTileX = 0;
		}
		FirstTileY -= (short)(2f * Main.ScreenMultiplier);
		if (FirstTileY < 0)
		{
			FirstTileY = 0;
		}
		LastTileX += (short)(2f * Main.ScreenMultiplier);
		if (LastTileX > Main.MaxTilesX)
		{
			LastTileX = Main.MaxTilesX;
		}
		LastTileY += (short)(4f * Main.ScreenMultiplier);
		if (LastTileY > Main.MaxTilesY)
		{
			LastTileY = Main.MaxTilesY;
		}
		if (pass > 0)
		{
			Vector2i screenPosition = ScreenPosition;
			ScreenPosition.X &= -2;
			ScreenPosition.Y &= -2;
			switch (pass)
			{
			case 1:
				RenderBackground();
				break;
			case 2:
				if (FirstTileY <= Main.WorldSurface)
				{
					RenderBlack();
				}
				RenderSolidTiles();
				break;
			case 3:
				RenderWalls();
				break;
			case 4:
				RenderNonSolidTiles();
				RenderBackWater();
				RenderWater();
				break;
			}
			ScreenPosition = screenPosition;
		}
		if (worldScaleAnimTheta > 0.0)
		{
			worldScaleAnimTheta -= Math.PI / 180.0;
			if (worldScaleAnimTheta <= 0.0)
			{
				worldScale = worldScaleTarget;
			}
			else
			{
				double num = 1.0 - Math.Sin(worldScaleAnimTheta);
				worldScale = (float)((double)worldScalePrevious + num * (double)(worldScaleTarget - worldScalePrevious));
			}
			UpdateView();
		}
		ScreenProjection.View = viewMtx;
	}

	public void Zoom(float z)
	{
		if (z != worldScaleTarget)
		{
			worldScaleTarget = z;
			worldScalePrevious = worldScale;
			worldScaleAnimTheta = Math.PI / 2.0;
		}
	}

	public void DirectZoom(float z)
	{
		if (z != worldScaleTarget)
		{
			worldScaleTarget = z;
			worldScalePrevious = worldScale;
			worldScale = worldScaleTarget;
			UpdateView();
		}
	}

	private void UpdateProjection()
	{
		if (IsFullScreen())
		{
			ScreenProjection.Projection = halfpixelOffset * Matrix.CreateOrthographicOffCenter(0f, ActiveViewport.Width, ActiveViewport.Height, 0f, 0f, 1f);
		}
		else
		{
			ScreenProjection.Projection = halfpixelOffset * Matrix.CreateOrthographicOffCenter(0f, ActiveViewport.Width << 1, ActiveViewport.Height << 1, 0f, 0f, 1f);
		}
		renderTargetProjection.Projection = halfpixelOffset * Matrix.CreateOrthographicOffCenter(0f, ViewWidth + 64, ViewportHeight + 96, 0f, 0f, 1f);
	}

	private void UpdateView()
	{
		int viewWidth = ViewWidth;
		int resolutionHeight = Main.ResolutionHeight;
		viewWidth >>= 1;
		resolutionHeight >>= 1;
		viewMtx = Matrix.CreateTranslation(-viewWidth, -resolutionHeight, 0f) * Matrix.CreateScale(worldScale, worldScale, 1f) * Matrix.CreateTranslation(viewWidth, resolutionHeight, 0f);
	}

	public void SetWorldView()
	{
		Main.SpriteBatch.End();
		ScreenProjection.View = viewMtx;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, ScreenProjection);
	}

	public void SetScreenView()
	{
		Main.SpriteBatch.End();
		ScreenProjection.View = Matrix.Identity;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, ScreenProjection);
	}

	public void SetScreenViewWideCentered()
	{
		Main.SpriteBatch.End();
		ScreenProjection.View = ((ViewWidth == ViewportWidth) ? Matrix.Identity : centerWideSplitscreen);
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, ScreenProjection);
	}

	private void DrawCombatText()
	{
		for (int i = 0; i < 32; i++)
		{
			if (Main.CombatTextSet[i].Active != 0)
			{
				Vector2 textSize = Main.CombatTextSet[i].TextSize;
				textSize.X *= 0.5f;
				textSize.Y *= 0.5f;
				int num = (Main.CombatTextSet[i].Crit ? 1 : 0);
				float scale = Main.CombatTextSet[i].Scale;
				float num2 = Main.CombatTextSet[i].Alpha * scale;
				Vector2 position = Main.CombatTextSet[i].Position;
				position.X += textSize.X;
				position.X -= ScreenPosition.X;
				position.Y += textSize.Y;
				position.Y -= ScreenPosition.Y;
				byte type = Main.CombatTextSet[i].Type;
				if (1 == 0)
				{
				}
				Color color = type switch
				{
					1 => new Color(255, 80, 90, 255), 
					2 => new Color(100, 255, 100, 255), 
					3 => new Color(100, 100, 255, 255), 
					_ => new Color(num2, num2, num2, num2), 
				};
				if (1 == 0)
				{
				}
				Color c = color;
				UI.DrawString(UI.CombatTextFont[num], Main.CombatTextSet[i].Text, position, c, Main.CombatTextSet[i].Rotation, textSize, scale);
			}
		}
	}

	private void DrawItemText()
	{
		for (int i = 0; i < 4; i++)
		{
			if (itemTextLocal.ItemText[i].Active != 0)
			{
				Vector2 textSize = itemTextLocal.ItemText[i].TextSize;
				textSize.X *= 0.5f;
				textSize.Y *= 0.5f;
				float num = (int)itemTextLocal.ItemText[i].Color.R;
				float num2 = (int)itemTextLocal.ItemText[i].Color.G;
				float num3 = (int)itemTextLocal.ItemText[i].Color.B;
				float num4 = (int)itemTextLocal.ItemText[i].Color.A;
				float scale = itemTextLocal.ItemText[i].Scale;
				_ = ref itemTextLocal.ItemText[i];
				num *= scale;
				num3 *= scale;
				num2 *= scale;
				num4 *= scale;
				Vector2 position = itemTextLocal.ItemText[i].Position;
				position.X += textSize.X;
				position.X -= ScreenPosition.X;
				position.Y += textSize.Y;
				position.Y -= ScreenPosition.Y;
				UI.DrawStringScaled(UI.BoldSmallFont, itemTextLocal.ItemText[i].Text, position, new Color((int)num, (int)num2, (int)num3, (int)num4), textSize, scale);
			}
		}
	}

	private void DrawProjectiles()
	{
		for (int num = 511; num >= 0; num--)
		{
			if (Main.ProjectileSet[num].active != 0 && Main.ProjectileSet[num].type > 0 && !Main.ProjectileSet[num].hide)
			{
				Main.ProjectileSet[num].Draw(this);
			}
		}
	}

	private void DrawPlayers()
	{
		for (int i = 0; i < 8; i++)
		{
			Player player = Main.PlayerSet[i];
			if (player.Active == 0)
			{
				continue;
			}
			if (player.ghost)
			{
				Vector2 position = player.Position;
				player.Position = player.shadowPos[0];
				player.shadow = 0.5f;
				player.DrawGhost(this);
				player.Position = player.shadowPos[1];
				player.shadow = 0.7f;
				player.DrawGhost(this);
				player.Position = player.shadowPos[2];
				player.shadow = 0.9f;
				player.DrawGhost(this);
				player.Position = position;
				player.shadow = 0f;
				player.DrawGhost(this);
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			if (player.legs == 25)
			{
				flag = true;
			}
			if (player.legs == 26)
			{
				flag2 = true;
			}
			else if (player.head == 5 && player.body == 5 && player.legs == 5)
			{
				flag = true;
			}
			else if (player.head == 7 && player.body == 7 && player.legs == 7)
			{
				flag = true;
			}
			else if (player.head == 22 && player.body == 14 && player.legs == 14)
			{
				flag = true;
			}
			else if (player.body == 17 && player.legs == 16)
			{
				if (player.head == 29 || player.head == 30 || player.head == 31)
				{
					flag = true;
				}
			}
			else if (player.body == 19 && player.legs == 18)
			{
				if (player.head == 35 || player.head == 36 || player.head == 37)
				{
					flag2 = true;
				}
			}
			else if (player.body == 24 && player.legs == 23 && (player.head == 42 || player.head == 41 || player.head == 43))
			{
				flag = true;
				flag2 = true;
			}
			if (flag2)
			{
				Vector2 position2 = player.Position;
				player.ghostFade += player.ghostDir * 0.075f;
				if (player.ghostFade < 0.1f)
				{
					player.ghostDir = 1f;
					player.ghostFade = 0.1f;
				}
				if (player.ghostFade > 0.9f)
				{
					player.ghostDir = -1f;
					player.ghostFade = 0.9f;
				}
				player.Position.X = position2.X - player.ghostFade * 5f;
				player.shadow = player.ghostFade;
				player.Draw(this);
				player.Position.X = position2.X + player.ghostFade * 5f;
				player.shadow = player.ghostFade;
				player.Draw(this);
				player.Position = position2;
				player.Position.Y = position2.Y - player.ghostFade * 5f;
				player.shadow = player.ghostFade;
				player.Draw(this);
				player.Position.Y = position2.Y + player.ghostFade * 5f;
				player.shadow = player.ghostFade;
				player.Draw(this);
				player.Position = position2;
				player.shadow = 0f;
			}
			if (flag)
			{
				Vector2 position3 = player.Position;
				player.Position = player.shadowPos[0];
				player.shadow = 0.5f;
				player.Draw(this);
				player.Position = player.shadowPos[1];
				player.shadow = 0.7f;
				player.Draw(this);
				player.Position = player.shadowPos[2];
				player.shadow = 0.9f;
				player.Draw(this);
				player.Position = position3;
				player.shadow = 0f;
			}
			player.Draw(this);
		}
	}

	private unsafe void DrawItems()
	{
		Rectangle rectangle = default;
		Vector2 pos = default;
		fixed (Item* itemSet = Main.ItemSet)
		{
			Item* ptr = itemSet;
			for (int num = 199; num >= 0; num--)
			{
				if (ptr->Active != 0)
				{
					int num2 = (int)ptr->Position.X;
					int num3 = (int)ptr->Position.Y;
					int num4 = 451 + ptr->Type;
					rectangle.Width = SpriteSheet<_sheetSprites>.Source[num4].Width;
					rectangle.Height = SpriteSheet<_sheetSprites>.Source[num4].Height;
					rectangle.X = num2 + (ptr->Width >> 1);
					rectangle.Y = num3 + (rectangle.Height >> 1) + ptr->Height - rectangle.Height + 2;
					if (rectangle.Intersects(ViewArea))
					{
						int x = num2 + (ptr->Width >> 1) >> 4;
						int y = num3 + (ptr->Height >> 1) >> 4;
						Color colorUnsafe = Lighting.GetColorUnsafe(x, y);
						if ((ptr->CanBePlacedInCoinSlot() || ptr->Type == 58 || ptr->Type == 109) && Main.HasFocus && colorUnsafe.R > 60 && (float)Main.Rand.Next(500) - (Math.Abs(ptr->Velocity.X) + Math.Abs(ptr->Velocity.Y)) * 10f < (float)(int)colorUnsafe.R * 0.02f)
						{
							Dust* ptr2 = dustLocal.NewDust(num2, num3, ptr->Width, ptr->Height, 43, 0.0, 0.0, 254, default, 0.5);
							if (ptr2 != null)
							{
								ptr2->Velocity.X = 0f;
								ptr2->Velocity.Y = 0f;
							}
						}
						float rot = ptr->Velocity.X * 0.2f;
						float num5 = 1f;
						Color alpha = ptr->GetAlpha(colorUnsafe);
						if (ptr->Type == 58 || ptr->Type == 184)
						{
							num5 = UI.PulseScale * 0.25f + 0.75f;
							alpha.R = (byte)((float)(int)alpha.R * num5);
							alpha.G = (byte)((float)(int)alpha.G * num5);
							alpha.B = (byte)((float)(int)alpha.B * num5);
							alpha.A = (byte)((float)(int)alpha.A * num5);
						}
						else if (ptr->Type == 520 || ptr->Type == 521 || ptr->Type == 547 || ptr->Type == 548 || ptr->Type == 549 || ptr->Type == 575 || ptr->Type == 620)
						{
							num5 = UI.PulseScale;
							alpha.R = (byte)((float)(int)alpha.R * num5);
							alpha.G = (byte)((float)(int)alpha.G * num5);
							alpha.B = (byte)((float)(int)alpha.B * num5);
							alpha.A = (byte)((float)(int)alpha.A * num5);
						}
						pos.X = rectangle.X - ScreenPosition.X;
						pos.Y = rectangle.Y - ScreenPosition.Y;
						SpriteSheet<_sheetSprites>.Draw(num4, ref pos, alpha, rot, num5);
						if (ptr->Colour.PackedValue != 0)
						{
							SpriteSheet<_sheetSprites>.Draw(num4, ref pos, ptr->GetColor(colorUnsafe), rot, num5);
						}
					}
				}
				ptr++;
			}
		}
	}

	private unsafe void DrawBlack()
	{
		float num = (WorldTime.TileColorFore.X + WorldTime.TileColorFore.Y + WorldTime.TileColorFore.Z) * (2f / 15f);
		Rectangle dest = new Rectangle
		{
			X = 32 + (FirstTileX << 4) - ScreenPosition.X,
			Width = 16 * (16 / SpriteSheet<_sheetTiles>.Source[7].Width)
		};
		Color black = Color.Black;
		int num2 = LastTileY - 1;
		if (num2 > Main.WorldSurface)
		{
			num2 = Main.WorldSurface;
		}
		for (int i = FirstTileX; i < LastTileX; i++)
		{
			int num3 = FirstTileY;
			fixed (Tile* ptr = &Main.TileSet[i, num3])
			{
				Tile* ptr2 = ptr;
				while (true)
				{
					float num4 = Lighting.BrightnessUnsafe(i, num3);
					if (num4 < num && (num4 == 0f || ptr2->Liquid == 0 || (ptr2->IsActive != 0 && Main.TileSolidNotSolidTop[ptr2->Type])))
					{
						if (dest.Height == 0)
						{
							dest.Y = 32 + (num3 << 4) - ScreenPosition.Y;
						}
						dest.Height += 16;
					}
					else if (dest.Height > 0)
					{
						SpriteSheet<_sheetTiles>.DrawStretchedY(7, ref dest, black);
						dest.Height = 0;
					}
					if (++num3 > num2)
					{
						break;
					}
					ptr2++;
				}
				if (dest.Height > 0)
				{
					SpriteSheet<_sheetTiles>.DrawStretchedY(7, ref dest, black);
					dest.Height = 0;
				}
			}
			dest.X += 16;
		}
	}

	private unsafe void DrawWalls()
	{
		int num = 216;
		int num2 = 100;
		Vector2 pos = default;
		Color color = default;
		Rectangle s = default;
		int num3 = FirstTileX;
		fixed (Tile* tileSet = Main.TileSet)
		{
			do
			{
				int num4 = FirstTileY;
				Tile* ptr = tileSet + (num3 * 1440 + num4);
				do
				{
					int wallType = ptr->WallType;
					ptr->CurrentFlags |= Tile.Flags.VISITED;
					if (wallType > 0 && !ptr->isFullTile())
					{
						color = Lighting.GetColorUnsafe(num3, num4);
						int id = 186 + wallType;
						s.X = ptr->wallFrameX << 1;
						s.Y = ptr->wallFrameY << 1;
						s.Width = 32;
						s.Height = 32;
						pos.X = num3 * 16 - ScreenPosition.X - 8 + 32;
						pos.Y = num4 * 16 - ScreenPosition.Y - 8 + 32;
						if (SMOOTH_LIGHT && wallType != 21 && !WorldGen.SolidTile(num3, num4))
						{
							if (color.R > num || (double)(int)color.G > (double)num * 1.1 || (double)(int)color.B > (double)num * 1.2)
							{
								s.Width = 12;
								s.Height = 12;
								Color colorUnsafe = Lighting.GetColorUnsafe(num3 - 1, num4 - 1);
								colorUnsafe.R = (byte)(color.R + colorUnsafe.R >> 1);
								colorUnsafe.G = (byte)(color.G + colorUnsafe.G >> 1);
								colorUnsafe.B = (byte)(color.B + colorUnsafe.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
								pos.X += 12f;
								s.X += 12;
								s.Width = 8;
								colorUnsafe = Lighting.GetColorUnsafe(num3, num4 - 1);
								colorUnsafe.R = (byte)(color.R + colorUnsafe.R >> 1);
								colorUnsafe.G = (byte)(color.G + colorUnsafe.G >> 1);
								colorUnsafe.B = (byte)(color.B + colorUnsafe.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
								pos.X += 8f;
								s.X += 8;
								s.Width = 12;
								colorUnsafe = Lighting.GetColorUnsafe(num3 + 1, num4 - 1);
								colorUnsafe.R = (byte)(color.R + colorUnsafe.R >> 1);
								colorUnsafe.G = (byte)(color.G + colorUnsafe.G >> 1);
								colorUnsafe.B = (byte)(color.B + colorUnsafe.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
								pos.Y += 12f;
								s.Y += 12;
								s.Height = 8;
								colorUnsafe = Lighting.GetColorUnsafe(num3 + 1, num4);
								colorUnsafe.R = (byte)(color.R + colorUnsafe.R >> 1);
								colorUnsafe.G = (byte)(color.G + colorUnsafe.G >> 1);
								colorUnsafe.B = (byte)(color.B + colorUnsafe.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
								pos.X -= 8f;
								s.X -= 8;
								s.Width = 8;
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
								pos.X -= 12f;
								s.X -= 12;
								s.Width = 12;
								colorUnsafe = Lighting.GetColorUnsafe(num3 - 1, num4);
								colorUnsafe.R = (byte)(color.R + colorUnsafe.R >> 1);
								colorUnsafe.G = (byte)(color.G + colorUnsafe.G >> 1);
								colorUnsafe.B = (byte)(color.B + colorUnsafe.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
								pos.Y += 8f;
								s.Y += 8;
								s.Height = 12;
								colorUnsafe = Lighting.GetColorUnsafe(num3 - 1, num4 + 1);
								colorUnsafe.R = (byte)(color.R + colorUnsafe.R >> 1);
								colorUnsafe.G = (byte)(color.G + colorUnsafe.G >> 1);
								colorUnsafe.B = (byte)(color.B + colorUnsafe.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
								pos.X += 12f;
								s.X += 12;
								s.Width = 8;
								colorUnsafe = Lighting.GetColorUnsafe(num3, num4 + 1);
								colorUnsafe.R = (byte)(color.R + colorUnsafe.R >> 1);
								colorUnsafe.G = (byte)(color.G + colorUnsafe.G >> 1);
								colorUnsafe.B = (byte)(color.B + colorUnsafe.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
								pos.X += 8f;
								s.X += 8;
								s.Width = 12;
								colorUnsafe = Lighting.GetColorUnsafe(num3 + 1, num4 + 1);
								colorUnsafe.R = (byte)(color.R + colorUnsafe.R >> 1);
								colorUnsafe.G = (byte)(color.G + colorUnsafe.G >> 1);
								colorUnsafe.B = (byte)(color.B + colorUnsafe.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
							}
							else if (color.R > num2 || (double)(int)color.G > (double)num2 * 1.1 || (double)(int)color.B > (double)num2 * 1.2)
							{
								s.Width = 16;
								s.Height = 16;
								Color c = (Lighting.Brighter(num3, num4 - 1, num3 - 1, num4) ? Lighting.GetColorUnsafe(num3 - 1, num4) : Lighting.GetColorUnsafe(num3, num4 - 1));
								c.R = (byte)(color.R + c.R >> 1);
								c.G = (byte)(color.G + c.G >> 1);
								c.B = (byte)(color.B + c.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, c);
								s.X += 16;
								pos.X += 16f;
								c = (Lighting.Brighter(num3, num4 - 1, num3 + 1, num4) ? Lighting.GetColorUnsafe(num3 + 1, num4) : Lighting.GetColorUnsafe(num3, num4 - 1));
								c.R = (byte)(color.R + c.R >> 1);
								c.G = (byte)(color.G + c.G >> 1);
								c.B = (byte)(color.B + c.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, c);
								s.Y += 16;
								pos.Y += 16f;
								c = (Lighting.Brighter(num3, num4 + 1, num3 + 1, num4) ? Lighting.GetColorUnsafe(num3 + 1, num4) : Lighting.GetColorUnsafe(num3, num4 + 1));
								c.R = (byte)(color.R + c.R >> 1);
								c.G = (byte)(color.G + c.G >> 1);
								c.B = (byte)(color.B + c.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, c);
								s.X -= 16;
								pos.X -= 16f;
								c = (Lighting.Brighter(num3, num4 + 1, num3 - 1, num4) ? Lighting.GetColorUnsafe(num3 - 1, num4) : Lighting.GetColorUnsafe(num3, num4 + 1));
								c.R = (byte)(color.R + c.R >> 1);
								c.G = (byte)(color.G + c.G >> 1);
								c.B = (byte)(color.B + c.B >> 1);
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, c);
							}
							else
							{
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, color);
							}
						}
						else
						{
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, color);
						}
					}
					ptr++;
				}
				while (++num4 < LastTileY);
			}
			while (++num3 < LastTileX);
		}
	}

	private unsafe void DrawWires()
	{
		int num = 38;
		Rectangle s = new Rectangle
		{
			Width = 16,
			Height = 16
		};
		Vector2 pos = default;
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = FirstTileX; i < LastTileX; i++)
			{
				pos.X = 32 + i * 16 - ScreenPosition.X;
				Tile* ptr = tileSet + (i * 1440 + FirstTileY);
				int num2 = FirstTileY;
				while (num2 < LastTileY)
				{
					if (ptr->wire != 0)
					{
						pos.Y = 32 + num2 * 16 - ScreenPosition.Y;
						if (Lighting.IsNotBlackUnsafe(i, num2))
						{
							ptr--;
							int wire = ptr->wire;
							ptr += 2;
							int wire2 = ptr->wire;
							ptr -= 1441;
							int wire3 = ptr->wire;
							ptr += 2880;
							int wire4 = ptr->wire;
							ptr -= 1440;
							if (wire != 0)
							{
								if (wire2 != 0)
								{
									if (wire3 != 0)
									{
										if (wire4 != 0)
										{
											s.X = 18;
											s.Y = 18;
										}
										else
										{
											s.X = 54;
											s.Y = 0;
										}
									}
									else if (wire4 != 0)
									{
										s.X = 36;
										s.Y = 0;
									}
									else
									{
										s.X = 0;
										s.Y = 0;
									}
								}
								else if (wire3 != 0)
								{
									if (wire4 != 0)
									{
										s.X = 0;
										s.Y = 18;
									}
									else
									{
										s.X = 54;
										s.Y = 18;
									}
								}
								else if (wire4 != 0)
								{
									s.X = 36;
									s.Y = 18;
								}
								else
								{
									s.X = 36;
									s.Y = 36;
								}
							}
							else if (wire2 != 0)
							{
								if (wire3 != 0)
								{
									if (wire4 != 0)
									{
										s.X = 72;
										s.Y = 0;
									}
									else
									{
										s.X = 72;
										s.Y = 18;
									}
								}
								else if (wire4 != 0)
								{
									s.X = 0;
									s.Y = 36;
								}
								else
								{
									s.X = 18;
									s.Y = 36;
								}
							}
							else if (wire3 != 0)
							{
								if (wire4 != 0)
								{
									s.X = 18;
									s.Y = 0;
								}
								else
								{
									s.X = 54;
									s.Y = 36;
								}
							}
							else if (wire4 != 0)
							{
								s.X = 72;
								s.Y = 36;
							}
							else
							{
								s.X = 0;
								s.Y = 54;
							}
							Color colorUnsafe = Lighting.GetColorUnsafe(i, num2);
							int id = 218;
							if (SMOOTH_LIGHT && (colorUnsafe.R > num || (double)(int)colorUnsafe.G > (double)num * 1.1 || (double)(int)colorUnsafe.B > (double)num * 1.2))
							{
								for (int j = 0; j < 4; j++)
								{
									int num3 = 0;
									int num4 = 0;
									Color c = colorUnsafe;
									Color color = colorUnsafe;
									switch (j)
									{
									case 0:
										color = ((!Lighting.Brighter(i, num2 - 1, i - 1, num2)) ? Lighting.GetColorUnsafe(i, num2 - 1) : Lighting.GetColorUnsafe(i - 1, num2));
										break;
									case 1:
										color = ((!Lighting.Brighter(i, num2 - 1, i + 1, num2)) ? Lighting.GetColorUnsafe(i, num2 - 1) : Lighting.GetColorUnsafe(i + 1, num2));
										num3 = 8;
										break;
									case 2:
										color = ((!Lighting.Brighter(i, num2 + 1, i - 1, num2)) ? Lighting.GetColorUnsafe(i, num2 + 1) : Lighting.GetColorUnsafe(i - 1, num2));
										num4 = 8;
										break;
									default:
										color = ((!Lighting.Brighter(i, num2 + 1, i + 1, num2)) ? Lighting.GetColorUnsafe(i, num2 + 1) : Lighting.GetColorUnsafe(i + 1, num2));
										num3 = 8;
										num4 = 8;
										break;
									}
									c.R = (byte)(colorUnsafe.R + color.R >> 1);
									c.G = (byte)(colorUnsafe.G + color.G >> 1);
									c.B = (byte)(colorUnsafe.B + color.B >> 1);
									Rectangle s2 = s;
									s2.X += num3;
									s2.Y += num4;
									s2.Width = 8;
									s2.Height = 8;
									pos.X += num3;
									pos.Y += num4;
									SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s2, c);
									pos.X -= num3;
									pos.Y -= num4;
								}
							}
							else
							{
								SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, colorUnsafe);
							}
						}
					}
					num2++;
					ptr++;
				}
			}
		}
	}

	public unsafe void DrawBg(UI ui)
	{
		if (viewportAnimTheta > 0.0 && !Guide.IsVisible)
		{
			viewportAnimTheta -= Math.PI / 60.0;
			if (viewportAnimTheta <= 0.0)
			{
				SafeAreaOffsetLeft = targetSAFE_AREA_OFFSET_L;
				SafeAreaOffsetTop = targetSAFE_AREA_OFFSET_T;
				SafeAreaOffsetRight = targetSAFE_AREA_OFFSET_R;
				SafeAreaOffsetBottom = targetSAFE_AREA_OFFSET_B;
				ActiveViewport = targetViewport;
			}
			else
			{
				double num = 1.0 - Math.Sin(viewportAnimTheta);
				SafeAreaOffsetLeft = currentSAFE_AREA_OFFSET_L + (int)(num * (double)(targetSAFE_AREA_OFFSET_L - currentSAFE_AREA_OFFSET_L));
				SafeAreaOffsetTop = currentSAFE_AREA_OFFSET_T + (int)(num * (double)(targetSAFE_AREA_OFFSET_T - currentSAFE_AREA_OFFSET_T));
				SafeAreaOffsetRight = currentSAFE_AREA_OFFSET_R + (int)(num * (double)(targetSAFE_AREA_OFFSET_R - currentSAFE_AREA_OFFSET_R));
				SafeAreaOffsetBottom = currentSAFE_AREA_OFFSET_B + (int)(num * (double)(targetSAFE_AREA_OFFSET_B - currentSAFE_AREA_OFFSET_B));
				ActiveViewport.X = currentViewport.X + (int)(num * (double)(targetViewport.X - currentViewport.X));
				ActiveViewport.Y = currentViewport.Y + (int)(num * (double)(targetViewport.Y - currentViewport.Y));
			}
		}
		GraphicsDevice.Viewport = ActiveViewport;
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, ScreenProjection);
		int num2;
		if (ui.CurMenuType == MenuType.MAIN)
		{
			num2 = -200;
			AtmoOpacity = 1f;
			WorldTime = Main.MenuTime;
			if (!WorldTime.DayTime)
			{
				WorldTime.intermediateBgColor.R = 35;
				WorldTime.intermediateBgColor.G = 35;
				WorldTime.intermediateBgColor.B = 35;
			}
			bgDelay = 1000;
			if (bgAlpha[1] > 0f)
			{
				WorldTime.applyEvil(bgAlpha[1]);
			}
			else
			{
				WorldTime.applyNothing();
			}
			WorldTime.finalizeColors();
			ScreenLastPosition = ScreenPosition;
			ScreenPosition.X += 2;
			ScreenPosition.Y = Main.WorldSurfacePixels - Main.ResolutionHeight - 1;
		}
		else
		{
			num2 = (int)((float)ScreenPosition.Y / (float)(Main.WorldSurfacePixels - 540) * -260f);
			WorldTime = Main.GameTime;
			if (JungleTiles > 0)
			{
				WorldTime.applyJungle((float)JungleTiles * 0.005f);
			}
			else if (EvilTiles > 0)
			{
				WorldTime.applyEvil((float)EvilTiles * 0.002f);
			}
			else
			{
				WorldTime.applyNothing();
			}
			WorldTime.finalizeColors();
			float num3 = (float)Main.MaxTilesY / 1200f;
			num3 *= num3;
			AtmoOpacity = ((float)(ScreenPosition.Y + Main.ResolutionHeight / 2 >> 4) - (65f + 10f * num3)) / ((float)Main.WorldSurface * 0.2f);
			if (AtmoOpacity >= 1f)
			{
				AtmoOpacity = 1f;
			}
			else
			{
				if (AtmoOpacity < 0f)
				{
					AtmoOpacity = 0f;
				}
				WorldTime.bgColor.R = (byte)((float)(int)WorldTime.bgColor.R * AtmoOpacity);
				WorldTime.bgColor.G = (byte)((float)(int)WorldTime.bgColor.G * AtmoOpacity);
				WorldTime.bgColor.B = (byte)((float)(int)WorldTime.bgColor.B * AtmoOpacity);
			}
		}
		Vector2 pos = default;
		if (ScreenPosition.Y >= Main.WorldSurfacePixels)
		{
			return;
		}
		Rectangle dest = new Rectangle
		{
			X = 0,
			Y = num2,
			Width = ViewWidth,
			Height = 1300 - num2
		};
		SpriteSheet<_sheetTiles>.DrawStretchedX(0, ref dest, WorldTime.bgColor);
		if (255 - WorldTime.bgColor.R - 100 > 0)
		{
			float num4 = (float)EvilTiles * 0.002f;
			if (num4 > 1f)
			{
				num4 = 1f;
			}
			num4 = 1f - num4 * 0.5f;
			if (EvilTiles <= 0)
			{
				num4 = 1f;
			}
			Color c = default;
			Vector2 pos2 = default;
			for (int i = 0; i < 96; i++)
			{
				fixed (Star* ptr = &Star.star[i])
				{
					float num5 = ptr->twinkle * num4;
					int num6 = (int)((float)(255 - WorldTime.bgColor.R - 100) * num5);
					int num7 = (int)((float)(255 - WorldTime.bgColor.G - 100) * num5);
					int num8 = (int)((float)(255 - WorldTime.bgColor.B - 100) * num5);
					if (num6 < 0)
					{
						num6 = 0;
					}
					if (num7 < 0)
					{
						num7 = 0;
					}
					if (num8 < 0)
					{
						num8 = 0;
					}
					c.R = (byte)num6;
					c.G = (byte)((float)num7 * num4);
					c.B = (byte)((float)num8 * num4);
					pos2.X = ptr->position.X;
					if (ViewWidth > ViewportWidth)
					{
						pos2.X = (pos2.X - (float)ViewportWidth) * 2f + (float)(ViewportWidth * 2);
					}
					pos2.Y = ptr->position.Y + (float)num2;
					SpriteSheet<_sheetTiles>.Draw(19 + ptr->type, ref pos2, c, ptr->rotation, ptr->scale * ptr->twinkle);
				}
			}
		}
		int num9 = WorldTime.celestialX;
		if (ViewWidth > ViewportWidth)
		{
			num9 = (num9 - ViewportWidth << 1) + ViewportWidth * 2;
		}
		if (WorldTime.DayTime)
		{
			SpriteSheet<_sheetTiles>.Draw((ui.CurMenuType == MenuType.MAIN || ui.ActivePlayer.head != 12) ? 25 : 24, num9, WorldTime.celestialY + num2, WorldTime.celestialColor, WorldTime.celestialRotation, WorldTime.celestialScale);
		}
		else
		{
			SpriteSheet<_sheetTiles>.Draw(16, num9, WorldTime.celestialY + num2, 50 * WorldTime.MoonPhase, 50, WorldTime.celestialColor, WorldTime.celestialRotation, WorldTime.celestialScale);
		}
		num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1200f) + 1190;
		float num10 = num2 - 50;
		for (int j = 0; j < 20; j++)
		{
			if (!Cloud.ActiveCloud[j].IsActive || !(Cloud.ActiveCloud[j].CloudScale < 1f))
			{
				continue;
			}
			pos.Y = num10 + Cloud.ActiveCloud[j].CloudPos.Y;
			if (pos.Y < (float)ViewportHeight && pos.Y > (float)(-Cloud.ActiveCloud[j].CloudHeight))
			{
				pos.X = Cloud.ActiveCloud[j].CloudPos.X;
				if (ViewWidth > ViewportWidth)
				{
					pos.X = (pos.X - (float)ViewportWidth) * 2f + (float)(ViewportWidth * 2);
				}
				Color c2 = Cloud.ActiveCloud[j].CloudColor(WorldTime.bgColor);
				if (AtmoOpacity < 1f)
				{
					c2.R = (byte)((float)(int)c2.R * AtmoOpacity);
					c2.G = (byte)((float)(int)c2.G * AtmoOpacity);
					c2.B = (byte)((float)(int)c2.B * AtmoOpacity);
					c2.A = (byte)((float)(int)c2.A * AtmoOpacity);
				}
				SpriteSheet<_sheetTiles>.DrawScaledTL(8 + Cloud.ActiveCloud[j].CloudType, ref pos, c2, Cloud.ActiveCloud[j].CloudScale);
			}
		}
		int num11 = (int)((float)backgroundTexture[7].Width * 2f);
		num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1300f) + 1090;
		int num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.15f, num11) - (double)(num11 >> 1));
		int num13 = ViewWidth / num11 + 2;
		if (ui.CurMenuType == MenuType.MAIN)
		{
			num2 = 100;
		}
		Color bgColor = WorldTime.bgColor;
		bgColor.R = (byte)((float)(int)bgColor.R * bgAlpha2[0]);
		bgColor.G = (byte)((float)(int)bgColor.G * bgAlpha2[0]);
		bgColor.B = (byte)((float)(int)bgColor.B * bgAlpha2[0]);
		bgColor.A = (byte)((float)(int)bgColor.A * bgAlpha2[0]);
		if (bgAlpha2[0] > 0f)
		{
			for (int k = 0; k < num13; k++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[7], new Vector2(num12 + num11 * k, num2), new Rectangle(0, 0, backgroundTexture[7].Width, backgroundTexture[7].Height), bgColor, 0f, default, 2f, SpriteEffects.None, 0f);
			}
		}
		bgColor = WorldTime.bgColor;
		bgColor.R = (byte)((float)(int)bgColor.R * bgAlpha2[1]);
		bgColor.G = (byte)((float)(int)bgColor.G * bgAlpha2[1]);
		bgColor.B = (byte)((float)(int)bgColor.B * bgAlpha2[1]);
		bgColor.A = (byte)((float)(int)bgColor.A * bgAlpha2[1]);
		if (bgAlpha2[1] > 0f)
		{
			for (int l = 0; l < num13; l++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[23], new Vector2(num12 + num11 * l, num2), new Rectangle(0, 0, backgroundTexture[7].Width, backgroundTexture[7].Height), bgColor, 0f, default, 2f, SpriteEffects.None, 0f);
			}
		}
		bgColor = WorldTime.bgColor;
		bgColor.R = (byte)((float)(int)bgColor.R * bgAlpha2[2]);
		bgColor.G = (byte)((float)(int)bgColor.G * bgAlpha2[2]);
		bgColor.B = (byte)((float)(int)bgColor.B * bgAlpha2[2]);
		bgColor.A = (byte)((float)(int)bgColor.A * bgAlpha2[2]);
		if (bgAlpha2[2] > 0f)
		{
			for (int m = 0; m < num13; m++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[24], new Vector2(num12 + num11 * m, num2), new Rectangle(0, 0, backgroundTexture[7].Width, backgroundTexture[7].Height), bgColor, 0f, default, 2f, SpriteEffects.None, 0f);
			}
		}
		num10 = num2 - 50;
		for (int n = 0; n < 20; n++)
		{
			if (!Cloud.ActiveCloud[n].IsActive || !((double)Cloud.ActiveCloud[n].CloudScale < 1.15) || !(Cloud.ActiveCloud[n].CloudScale >= 1f))
			{
				continue;
			}
			pos.Y = num10 + Cloud.ActiveCloud[n].CloudPos.Y;
			if (pos.Y < (float)ViewportHeight && pos.Y > (float)(-Cloud.ActiveCloud[n].CloudHeight))
			{
				pos.X = Cloud.ActiveCloud[n].CloudPos.X;
				if (ViewWidth > ViewportWidth)
				{
					pos.X = (pos.X - (float)ViewportWidth) * 2f + (float)(ViewportWidth * 2);
				}
				Color c3 = Cloud.ActiveCloud[n].CloudColor(WorldTime.bgColor);
				if (AtmoOpacity < 1f)
				{
					c3.R = (byte)((float)(int)c3.R * AtmoOpacity);
					c3.G = (byte)((float)(int)c3.G * AtmoOpacity);
					c3.B = (byte)((float)(int)c3.B * AtmoOpacity);
					c3.A = (byte)((float)(int)c3.A * AtmoOpacity);
				}
				SpriteSheet<_sheetTiles>.DrawScaledTL(8 + Cloud.ActiveCloud[n].CloudType, ref pos, c3, Cloud.ActiveCloud[n].CloudScale);
			}
		}
		if (HolyTiles > 0)
		{
			num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.17f, 8085.0) - 4042.0);
			num13 = ViewWidth / 8085 + 2;
			num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1400f) + 900;
			if (ui.CurMenuType == MenuType.MAIN)
			{
				num2 = 230;
				num12 -= 500;
			}
			Color bgColor2 = WorldTime.bgColor;
			float num14 = (float)HolyTiles * 0.0025f;
			if (num14 > 0.5f)
			{
				num14 = 0.5f;
			}
			bgColor2.R = (byte)((float)(int)bgColor2.R * num14);
			bgColor2.G = (byte)((float)(int)bgColor2.G * num14);
			bgColor2.B = (byte)((float)(int)bgColor2.B * num14);
			bgColor2.A = (byte)((float)(int)bgColor2.A * num14 * 0.8f);
			for (int num15 = 0; num15 < num13; num15++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[18], new Vector2(num12 + 8085 * num15, num2), new Rectangle(0, 0, backgroundTexture[18].Width, backgroundTexture[18].Height), bgColor2, 0f, default, 2.2f, SpriteEffects.None, 0f);
			}
			for (int num16 = 0; num16 < num13; num16++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[19], new Vector2(num12 + 8085 * num16 + 1700, num2 + 100), new Rectangle(0, 0, backgroundTexture[19].Width, backgroundTexture[19].Height), bgColor2, 0f, default, 1.98f, SpriteEffects.None, 0f);
			}
		}
		int num17 = (int)((float)backgroundTexture[7].Width * 2.3f);
		num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.2f, num17) - (double)(num17 >> 1));
		num13 = ViewWidth / num17 + 2;
		num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1400f) + 1260;
		if (ui.CurMenuType == MenuType.MAIN)
		{
			num2 = 230;
			num12 -= 500;
		}
		bgColor = WorldTime.bgColor;
		bgColor.R = (byte)((float)(int)bgColor.R * bgAlpha2[0]);
		bgColor.G = (byte)((float)(int)bgColor.G * bgAlpha2[0]);
		bgColor.B = (byte)((float)(int)bgColor.B * bgAlpha2[0]);
		bgColor.A = (byte)((float)(int)bgColor.A * bgAlpha2[0]);
		if (bgAlpha2[0] > 0f)
		{
			for (int num18 = 0; num18 < num13; num18++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[8], new Vector2(num12 + num17 * num18, num2), new Rectangle(0, 0, backgroundTexture[7].Width, backgroundTexture[7].Height), bgColor, 0f, default, 2.3f, SpriteEffects.None, 0f);
			}
		}
		bgColor = WorldTime.bgColor;
		bgColor.R = (byte)((float)(int)bgColor.R * bgAlpha2[1]);
		bgColor.G = (byte)((float)(int)bgColor.G * bgAlpha2[1]);
		bgColor.B = (byte)((float)(int)bgColor.B * bgAlpha2[1]);
		bgColor.A = (byte)((float)(int)bgColor.A * bgAlpha2[1]);
		if (bgAlpha2[1] > 0f)
		{
			for (int num19 = 0; num19 < num13; num19++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[22], new Vector2(num12 + num17 * num19, num2), new Rectangle(0, 0, backgroundTexture[7].Width, backgroundTexture[7].Height), bgColor, 0f, default, 2.3f, SpriteEffects.None, 0f);
			}
		}
		bgColor = WorldTime.bgColor;
		bgColor.R = (byte)((float)(int)bgColor.R * bgAlpha2[2]);
		bgColor.G = (byte)((float)(int)bgColor.G * bgAlpha2[2]);
		bgColor.B = (byte)((float)(int)bgColor.B * bgAlpha2[2]);
		bgColor.A = (byte)((float)(int)bgColor.A * bgAlpha2[2]);
		if (bgAlpha2[2] > 0f)
		{
			for (int num20 = 0; num20 < num13; num20++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[25], new Vector2(num12 + num17 * num20, num2), new Rectangle(0, 0, backgroundTexture[7].Width, backgroundTexture[7].Height), bgColor, 0f, default, 2.3f, SpriteEffects.None, 0f);
			}
		}
		bgColor = WorldTime.bgColor;
		bgColor.R = (byte)((float)(int)bgColor.R * bgAlpha2[3]);
		bgColor.G = (byte)((float)(int)bgColor.G * bgAlpha2[3]);
		bgColor.B = (byte)((float)(int)bgColor.B * bgAlpha2[3]);
		bgColor.A = (byte)((float)(int)bgColor.A * bgAlpha2[3]);
		if (bgAlpha2[3] > 0f)
		{
			for (int num21 = 0; num21 < num13; num21++)
			{
				Main.SpriteBatch.Draw(backgroundTexture[28], new Vector2(num12 + num17 * num21, num2), new Rectangle(0, 0, backgroundTexture[7].Width, backgroundTexture[7].Height), bgColor, 0f, default, 2.3f, SpriteEffects.None, 0f);
			}
		}
		num10 = (float)num2 * 1.01f - 150f;
		for (int num22 = 0; num22 < 20; num22++)
		{
			if (!Cloud.ActiveCloud[num22].IsActive || !(Cloud.ActiveCloud[num22].CloudScale > 2.3f))
			{
				continue;
			}
			pos.Y = num10 + Cloud.ActiveCloud[num22].CloudPos.Y;
			if (pos.Y < (float)ViewportHeight && pos.Y > (float)(-Cloud.ActiveCloud[num22].CloudHeight))
			{
				pos.X = Cloud.ActiveCloud[num22].CloudPos.X;
				if (ViewWidth > ViewportWidth)
				{
					pos.X = (pos.X - (float)ViewportWidth) * 2f + (float)(ViewportWidth * 2);
				}
				Color c4 = Cloud.ActiveCloud[num22].CloudColor(WorldTime.bgColor);
				if (AtmoOpacity < 1f)
				{
					c4.R = (byte)((float)(int)c4.R * AtmoOpacity);
					c4.G = (byte)((float)(int)c4.G * AtmoOpacity);
					c4.B = (byte)((float)(int)c4.B * AtmoOpacity);
					c4.A = (byte)((float)(int)c4.A * AtmoOpacity);
				}
				SpriteSheet<_sheetTiles>.DrawScaledTL(8 + Cloud.ActiveCloud[num22].CloudType, ref pos, c4, Cloud.ActiveCloud[num22].CloudScale);
			}
		}
		int num23 = bgStyle;
		if (ui.CurMenuType != MenuType.MAIN)
		{
			int num24 = ScreenPosition.X + (ViewWidth >> 1) >> 4;
			if (num24 < 380 || num24 > Main.MaxTilesX - 380)
			{
				num23 = 4;
			}
			else if (InactiveTiles < ((ViewWidth == Main.ResolutionWidth) ? ((int)(8000f * Main.ScreenMultiplier)) : ((int)(16000f * Main.ScreenMultiplier))))
			{
				if (SandTiles > 1000)
				{
					num23 = ((!Player.ZoneEvil && !Player.zoneHoly) ? 2 : 5);
				}
				else
				{
					num23 = (Player.zoneHoly ? 6 : (Player.ZoneEvil ? 1 : (Player.ZoneJungle ? 3 : 0)));
				}
			}
		}
		float num25 = 0.05f;
		int num26 = 30;
		if (num23 == 0)
		{
			num26 = 120;
		}
		if (bgDelay < 0)
		{
			bgDelay++;
		}
		else
		{
			if (ui.CurMenuType == MenuType.MAIN)
			{
				num25 = 0.02f;
				if (!WorldTime.DayTime)
				{
					if (ui.CurMenuMode == MenuMode.CREDITS)
					{
						HolyTiles = 200;
						bgStyle = 6;
					}
					else
					{
						HolyTiles = 0;
						bgStyle = 1;
					}
				}
				else if (ui.CurMenuMode == MenuMode.CREDITS)
				{
					HolyTiles = 200;
					bgStyle = 3;
				}
				else
				{
					HolyTiles = 0;
					bgStyle = 0;
				}
				num23 = bgStyle;
			}
			if (num23 != bgStyle)
			{
				bgDelay++;
				if (bgDelay > num26)
				{
					bgDelay = -60;
					bgStyle = num23;
					if (num23 == 0)
					{
						bgDelay = 0;
					}
				}
			}
			else if (bgDelay > 0)
			{
				bgDelay--;
			}
		}
		if (quickBG > 0)
		{
			quickBG--;
			bgStyle = num23;
			num25 = 1f;
		}
		if (bgStyle == 2)
		{
			bgAlpha2[0] -= num25;
			if (bgAlpha2[0] < 0f)
			{
				bgAlpha2[0] = 0f;
			}
			bgAlpha2[1] += num25;
			if (bgAlpha2[1] > 1f)
			{
				bgAlpha2[1] = 1f;
			}
			bgAlpha2[2] -= num25;
			if (bgAlpha2[2] < 0f)
			{
				bgAlpha2[2] = 0f;
			}
			bgAlpha2[3] -= num25;
			if (bgAlpha2[3] < 0f)
			{
				bgAlpha2[3] = 0f;
			}
		}
		else if (bgStyle == 5 || bgStyle == 1 || bgStyle == 6)
		{
			bgAlpha2[0] -= num25;
			if (bgAlpha2[0] < 0f)
			{
				bgAlpha2[0] = 0f;
			}
			bgAlpha2[1] -= num25;
			if (bgAlpha2[1] < 0f)
			{
				bgAlpha2[1] = 0f;
			}
			bgAlpha2[2] += num25;
			if (bgAlpha2[2] > 1f)
			{
				bgAlpha2[2] = 1f;
			}
			bgAlpha2[3] -= num25;
			if (bgAlpha2[3] < 0f)
			{
				bgAlpha2[3] = 0f;
			}
		}
		else if (bgStyle == 4)
		{
			bgAlpha2[0] -= num25;
			if (bgAlpha2[0] < 0f)
			{
				bgAlpha2[0] = 0f;
			}
			bgAlpha2[1] -= num25;
			if (bgAlpha2[1] < 0f)
			{
				bgAlpha2[1] = 0f;
			}
			bgAlpha2[2] -= num25;
			if (bgAlpha2[2] < 0f)
			{
				bgAlpha2[2] = 0f;
			}
			bgAlpha2[3] += num25;
			if (bgAlpha2[3] > 1f)
			{
				bgAlpha2[3] = 1f;
			}
		}
		else
		{
			bgAlpha2[0] += num25;
			if (bgAlpha2[0] > 1f)
			{
				bgAlpha2[0] = 1f;
			}
			bgAlpha2[1] -= num25;
			if (bgAlpha2[1] < 0f)
			{
				bgAlpha2[1] = 0f;
			}
			bgAlpha2[2] -= num25;
			if (bgAlpha2[2] < 0f)
			{
				bgAlpha2[2] = 0f;
			}
			bgAlpha2[3] -= num25;
			if (bgAlpha2[3] < 0f)
			{
				bgAlpha2[3] = 0f;
			}
		}
		for (int num27 = 0; num27 < 7; num27++)
		{
			if (bgStyle == num27)
			{
				bgAlpha[num27] += num25;
				if (bgAlpha[num27] > 1f)
				{
					bgAlpha[num27] = 1f;
				}
			}
			else
			{
				bgAlpha[num27] -= num25;
				if (bgAlpha[num27] < 0f)
				{
					bgAlpha[num27] = 0f;
				}
			}
			bgColor = WorldTime.bgColor;
			bgColor.R = (byte)((float)(int)bgColor.R * bgAlpha[num27]);
			bgColor.G = (byte)((float)(int)bgColor.G * bgAlpha[num27]);
			bgColor.B = (byte)((float)(int)bgColor.B * bgAlpha[num27]);
			bgColor.A = (byte)((float)(int)bgColor.A * bgAlpha[num27]);
			if (num27 == 3 && bgAlpha[num27] > 0f)
			{
				int num28 = (int)((float)backgroundTexture[8].Width * 2.5f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.4f, num28) - (double)(num28 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1800f) + 1660;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 320;
				}
				num13 = ViewWidth / num28 + 2;
				for (int num29 = 0; num29 < num13; num29++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[15], new Vector2(num12 + num28 * num29, num2), new Rectangle(0, 0, backgroundTexture[8].Width, backgroundTexture[8].Height), bgColor, 0f, default, 2.5f, SpriteEffects.None, 0f);
				}
				int num30 = (int)((float)backgroundTexture[8].Width * 2.62f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.43f, num30) - (double)(num30 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1950f) + 1840;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 400;
					num12 -= 80;
				}
				num13 = ViewWidth / num30 + 2;
				for (int num31 = 0; num31 < num13; num31++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[16], new Vector2(num12 + num30 * num31, num2), new Rectangle(0, 0, backgroundTexture[8].Width, backgroundTexture[8].Height), bgColor, 0f, default, 2.62f, SpriteEffects.None, 0f);
				}
				int num32 = (int)((float)backgroundTexture[8].Width * 2.68f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.49f, num32) - (double)(num32 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 2100f) + 2060;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 480;
					num12 -= 120;
				}
				num13 = ViewWidth / num32 + 2;
				for (int num33 = 0; num33 < num13; num33++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[17], new Vector2(num12 + num32 * num33, num2), new Rectangle(0, 0, backgroundTexture[8].Width, backgroundTexture[8].Height), bgColor, 0f, default, 2.68f, SpriteEffects.None, 0f);
				}
			}
			else if (num27 == 2 && bgAlpha[num27] > 0f)
			{
				int num34 = (int)((float)backgroundTexture[21].Width * 2.5f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.37f, num34) - (double)(num34 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1800f) + 1750;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 320;
				}
				num13 = ViewWidth / num34 + 2;
				for (int num35 = 0; num35 < num13; num35++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[21], new Vector2(num12 + num34 * num35, num2), new Rectangle(0, 0, backgroundTexture[21].Width, backgroundTexture[21].Height), bgColor, 0f, default, 2.5f, SpriteEffects.None, 0f);
				}
				int num36 = (int)((float)backgroundTexture[20].Width * 2.68f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.49f, num36) - (double)(num36 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 2100f) + 2150;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 480;
					num12 -= 120;
				}
				num13 = ViewWidth / num36 + 2;
				for (int num37 = 0; num37 < num13; num37++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[20], new Vector2(num12 + num36 * num37, num2), new Rectangle(0, 0, backgroundTexture[20].Width, backgroundTexture[20].Height), bgColor, 0f, default, 2.68f, SpriteEffects.None, 0f);
				}
			}
			else if (num27 == 5 && bgAlpha[num27] > 0f)
			{
				int num38 = (int)((float)backgroundTexture[8].Width * 2.5f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.37f, num38) - (double)(num38 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1800f) + 1750;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 320;
				}
				num13 = ViewWidth / num38 + 2;
				for (int num39 = 0; num39 < num13; num39++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[26], new Vector2(num12 + num38 * num39, num2), new Rectangle(0, 0, backgroundTexture[26].Width, backgroundTexture[26].Height), bgColor, 0f, default, 2.5f, SpriteEffects.None, 0f);
				}
				int num40 = (int)((float)backgroundTexture[8].Width * 2.68f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.49f, num40) - (double)(num40 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 2100f) + 2150;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 480;
					num12 -= 120;
				}
				num13 = ViewWidth / num40 + 2;
				for (int num41 = 0; num41 < num13; num41++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[27], new Vector2(num12 + num40 * num41, num2), new Rectangle(0, 0, backgroundTexture[27].Width, backgroundTexture[27].Height), bgColor, 0f, default, 2.68f, SpriteEffects.None, 0f);
				}
			}
			else if (num27 == 1 && bgAlpha[num27] > 0f)
			{
				int num42 = (int)((float)backgroundTexture[8].Width * 2.5f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.4f, num42) - (double)(num42 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1800f) + 1500;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 320;
				}
				num13 = ViewWidth / num42 + 2;
				for (int num43 = 0; num43 < num13; num43++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[12], new Vector2(num12 + num42 * num43, num2), new Rectangle(0, 0, backgroundTexture[12].Width, backgroundTexture[12].Height), bgColor, 0f, default, 2.5f, SpriteEffects.None, 0f);
				}
				int num44 = (int)((float)backgroundTexture[8].Width * 2.62f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.43f, num44) - (double)(num44 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1950f) + 1750;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 400;
					num12 -= 80;
				}
				num13 = ViewWidth / num44 + 2;
				for (int num45 = 0; num45 < num13; num45++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[13], new Vector2(num12 + num44 * num45, num2), new Rectangle(0, 0, backgroundTexture[13].Width, backgroundTexture[13].Height), bgColor, 0f, default, 2.62f, SpriteEffects.None, 0f);
				}
				int num46 = (int)((float)backgroundTexture[8].Width * 2.68f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.49f, num46) - (double)(num46 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 2100f) + 2000;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 480;
					num12 -= 120;
				}
				num13 = ViewWidth / num46 + 2;
				for (int num47 = 0; num47 < num13; num47++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[14], new Vector2(num12 + num46 * num47, num2), new Rectangle(0, 0, backgroundTexture[14].Width, backgroundTexture[14].Height), bgColor, 0f, default, 2.68f, SpriteEffects.None, 0f);
				}
			}
			else if (num27 == 6 && bgAlpha[num27] > 0f)
			{
				int num48 = (int)((float)backgroundTexture[8].Width * 2.5f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.4f, num48) - (double)(num48 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1800f) + 1500;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 320;
				}
				num13 = ViewWidth / num48 + 2;
				for (int num49 = 0; num49 < num13; num49++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[29], new Vector2(num12 + num48 * num49, num2), new Rectangle(0, 0, backgroundTexture[29].Width, backgroundTexture[29].Height), bgColor, 0f, default, 2.5f, SpriteEffects.None, 0f);
				}
				int num50 = (int)((float)backgroundTexture[8].Width * 2.62f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.43f, num50) - (double)(num50 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1950f) + 1750;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 400;
					num12 -= 80;
				}
				num13 = ViewWidth / num50 + 2;
				for (int num51 = 0; num51 < num13; num51++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[30], new Vector2(num12 + num50 * num51, num2), new Rectangle(0, 0, backgroundTexture[30].Width, backgroundTexture[30].Height), bgColor, 0f, default, 2.62f, SpriteEffects.None, 0f);
				}
				int num52 = (int)((float)backgroundTexture[8].Width * 2.68f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.49f, num52) - (double)(num52 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 2100f) + 2000;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 480;
					num12 -= 120;
				}
				num13 = ViewWidth / num52 + 2;
				for (int num53 = 0; num53 < num13; num53++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[31], new Vector2(num12 + num52 * num53, num2), new Rectangle(0, 0, backgroundTexture[31].Width, backgroundTexture[31].Height), bgColor, 0f, default, 2.68f, SpriteEffects.None, 0f);
				}
			}
			else if (num27 == 0 && bgAlpha[num27] > 0f)
			{
				int num54 = (int)((float)backgroundTexture[8].Width * 2.5f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.4f, num54) - (double)(num54 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1800f) + 1500;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 320;
				}
				num13 = ViewWidth / num54 + 2;
				for (int num55 = 0; num55 < num13; num55++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[9], new Vector2(num12 + num54 * num55, num2), new Rectangle(0, 0, backgroundTexture[9].Width, backgroundTexture[9].Height), bgColor, 0f, default, 2.5f, SpriteEffects.None, 0f);
				}
				int num56 = (int)((float)backgroundTexture[8].Width * 2.62f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.43f, num56) - (double)(num56 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 1950f) + 1750;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 400;
					num12 -= 80;
				}
				num13 = ViewWidth / num56 + 2;
				for (int num57 = 0; num57 < num13; num57++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[10], new Vector2(num12 + num56 * num57, num2), new Rectangle(0, 0, backgroundTexture[8].Width, backgroundTexture[8].Height), bgColor, 0f, default, 2.62f, SpriteEffects.None, 0f);
				}
				int num58 = (int)((float)backgroundTexture[8].Width * 2.68f);
				num12 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.49f, num58) - (double)(num58 >> 1));
				num2 = (int)((float)(-ScreenPosition.Y) / (float)Main.WorldSurfacePixels * 2100f) + 2000;
				if (ui.CurMenuType == MenuType.MAIN)
				{
					num2 = 480;
					num12 -= 120;
				}
				num13 = ViewWidth / num58 + 2;
				for (int num59 = 0; num59 < num13; num59++)
				{
					Main.SpriteBatch.Draw(backgroundTexture[11], new Vector2(num12 + num58 * num59, num2), new Rectangle(0, 0, backgroundTexture[8].Width, backgroundTexture[8].Height), bgColor, 0f, default, 2.68f, SpriteEffects.None, 0f);
				}
			}
		}
	}

	public void DrawWorld()
	{
		Color white = Color.White;
		Rectangle destinationRectangle = new Rectangle
		{
			Width = ViewWidth + 64,
			Height = ViewportHeight + 96,
			X = sceneWaterPos.X - ScreenPosition.X,
			Y = sceneWaterPos.Y - ScreenPosition.Y
		};
		Main.SpriteBatch.Draw(backWaterTarget, destinationRectangle, white);
		destinationRectangle.X = (int)((float)(sceneBackgroundPos.X - ScreenPosition.X + 32) * 0.9f) - 32;
		destinationRectangle.Y = sceneBackgroundPos.Y - ScreenPosition.Y;
		Main.SpriteBatch.Draw(backgroundTarget, destinationRectangle, white);
		if (FirstTileY <= Main.WorldSurface)
		{
			destinationRectangle.X = sceneBlackPos.X - ScreenPosition.X;
			destinationRectangle.Y = sceneBlackPos.Y - ScreenPosition.Y;
			Main.SpriteBatch.Draw(blackTarget, destinationRectangle, white);
		}
		destinationRectangle.X = sceneWallPos.X - ScreenPosition.X;
		destinationRectangle.Y = sceneWallPos.Y - ScreenPosition.Y;
		Main.SpriteBatch.Draw(wallTarget, destinationRectangle, white);
		DrawWoF();
		destinationRectangle.X = sceneTile2Pos.X - ScreenPosition.X;
		destinationRectangle.Y = sceneTile2Pos.Y - ScreenPosition.Y;
		Main.SpriteBatch.Draw(tileNonSolidTarget, destinationRectangle, white);
		destinationRectangle.X = sceneTilePos.X - ScreenPosition.X;
		destinationRectangle.Y = sceneTilePos.Y - ScreenPosition.Y;
		if (Player.detectCreature)
		{
			Main.SpriteBatch.Draw(tileSolidTarget, destinationRectangle, white);
			DrawGore();
			DrawNPCs(behindTiles: true);
			DrawNPCs();
		}
		else
		{
			DrawNPCs(behindTiles: true);
			Main.SpriteBatch.Draw(tileSolidTarget, destinationRectangle, white);
			DrawGore();
			DrawNPCs();
		}
		DrawProjectiles();
		DrawPlayers();
		DrawItems();
		dustLocal.DrawDust(this);
		Main.DustSet.DrawDust(this);
		destinationRectangle.X = sceneWaterPos.X - ScreenPosition.X;
		destinationRectangle.Y = sceneWaterPos.Y - ScreenPosition.Y;
		Main.SpriteBatch.Draw(waterTarget, destinationRectangle, white);
		DrawCombatText();
		DrawItemText();
	}

	private unsafe void DrawBackground()
	{
		float num = 0.9f;
		float num2 = num;
		float num3 = num;
		float num4 = 0f;
		if (HolyTiles > EvilTiles)
		{
			num4 = (float)HolyTiles / 800f;
		}
		else if (EvilTiles > HolyTiles)
		{
			num4 = (float)EvilTiles / 800f;
		}
		if (num4 > 1f)
		{
			num4 = 1f;
		}
		float num5 = (float)(ScreenPosition.Y - (Main.WorldSurface << 4)) / 300f;
		if (num5 < 0f)
		{
			num5 = 0f;
		}
		else if (num5 > 1f)
		{
			num5 = 1f;
		}
		float num6 = 1f - num5 + num * num5;
		Lighting.BrightnessLevel = Lighting.DefBrightness * (1f - num5) + 1f * num5;
		float num7 = (float)(ScreenPosition.Y - Main.ResolutionHeight / 2 + 200 - (Main.RockLayer << 4)) / 300f;
		if (num7 < 0f)
		{
			num7 = 0f;
		}
		else if (num7 > 1f)
		{
			num7 = 1f;
		}
		if (EvilTiles > 0)
		{
			num = 0.8f * num4 + num * (1f - num4);
			num2 = 0.75f * num4 + num2 * (1f - num4);
			num3 = 1.1f * num4 + num3 * (1f - num4);
		}
		else if (HolyTiles > 0)
		{
			num = 1f * num4 + num * (1f - num4);
			num2 = 0.7f * num4 + num2 * (1f - num4);
			num3 = 0.9f * num4 + num3 * (1f - num4);
		}
		num = num6 - num7 + num * num7;
		num2 = num6 - num7 + num2 * num7;
		num3 = num6 - num7 + num3 * num7;
		Lighting.DefBrightness = 1.2f * (1f - num7) + num7;
		int num8 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.9f, 96.0) - 48.0);
		int num9 = ViewWidth / 96 + 2;
		int num10 = Main.WorldSurfacePixels - ScreenPosition.Y;
		int num11 = num8;
		int num12 = -((num8 + ScreenPosition.X + 8) & 0xF);
		if (num12 == -8)
		{
			num12 = 8;
		}
		Vector2 pos = new Vector2(num11 + num12 + 32, num10 + 32);
		Rectangle s = new Rectangle
		{
			X = num12,
			Width = 16,
			Height = 16
		};
		for (int i = 0; i < num9; i++)
		{
			int num13 = 15;
			while (num13 >= 0)
			{
				Color color = Lighting.GetColor(num11 + 8 + ScreenPosition.X >> 4, ScreenPosition.Y + num10 >> 4);
				color.R = (byte)((float)(int)color.R * num);
				color.G = (byte)((float)(int)color.G * num2);
				color.B = (byte)((float)(int)color.B * num3);
				s.X += 16;
				SpriteSheet<_sheetTiles>.Draw(1, ref pos, ref s, color);
				num13--;
				num11 += 16;
			}
		}
		bool flag = false;
		if (Main.WorldSurfacePixels <= ScreenPosition.Y + Main.ResolutionHeight + 32)
		{
			num10 = Main.WorldSurfacePixels - ScreenPosition.Y + 16;
			num8 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.9f, 96.0) - 48.0) - 32;
			num9 = (ViewWidth + 64) / 96 + 2;
			int num14;
			int num15;
			if (Main.WorldSurfacePixels < ScreenPosition.Y - 16)
			{
				num14 = num10 % 96 - 96;
				num15 = (Main.ResolutionHeight - num14 + 96) / 96 + 1;
			}
			else
			{
				num14 = num10;
				num15 = (Main.ResolutionHeight - num10 + 96) / 96 + 1;
			}
			int num16 = 540;
			if (Main.ScreenHeightPtr == ScreenHeights.FHD)
			{
				num16 = 600;
			}
			if (Main.RockLayerPixels < ScreenPosition.Y + num16)
			{
				num15 = (Main.RockLayerPixels - ScreenPosition.Y + num16 - num14) / 96;
				flag = true;
			}
			int num17 = num8 + ScreenPosition.X;
			num17 = -(num17 & 0xF);
			if (num17 == -8)
			{
				num17 = 8;
			}
			Vector2 pos2 = default;
			Rectangle s2 = new Rectangle
			{
				Width = 16,
				Height = 16
			};
			int j = 0;
			int num18 = num8 + 8 + ScreenPosition.X;
			for (; j < num9; j++)
			{
				int num19 = num14 + 8 + ScreenPosition.Y >> 4;
				int num20 = 32 + num14;
				for (int k = 0; k < num15; k++)
				{
					for (int l = 0; l < 96; l += 16)
					{
						int num21 = 32 + num8 + 96 * j + l + num17;
						s2.X = l + num17 + 16;
						int num22 = num18 + l >> 4;
						fixed (Tile* ptr = &Main.TileSet[num22, num19])
						{
							Tile* ptr2 = ptr;
							for (int m = 0; m < 96; m += 16)
							{
								Color colorUnsafe = Lighting.GetColorUnsafe(num22, num19);
								pos2.X = num21;
								pos2.Y = num20 + m;
								s2.Y = m;
								if (colorUnsafe.R > 0 || colorUnsafe.G > 0 || colorUnsafe.B > 0)
								{
									if (SMOOTH_LIGHT && (colorUnsafe.R > 226 || (float)(int)colorUnsafe.G > 248.6f || (float)(int)colorUnsafe.B > 271.2f) && ptr2->IsActive == 0 && (ptr2->WallType == 0 || ptr2->WallType == 21))
									{
										s2.Width = 4;
										s2.Height = 4;
										Color c;
										if (ptr2[-1441].IsActive == 0)
										{
											c = Lighting.GetColorUnsafe(num22 - 1, num19 - 1);
											c.R = (byte)((float)(colorUnsafe.R + c.R >> 1) * num);
											c.G = (byte)((float)(colorUnsafe.G + c.G >> 1) * num2);
											c.B = (byte)((float)(colorUnsafe.B + c.B >> 1) * num3);
										}
										else
										{
											c = colorUnsafe;
										}
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Height = 8;
										s2.Y += 4;
										pos2.Y += 4f;
										if (ptr2[-1440].IsActive == 0)
										{
											c = Lighting.GetColorUnsafe(num22 - 1, num19);
											c.R = (byte)((float)(colorUnsafe.R + c.R >> 1) * num);
											c.G = (byte)((float)(colorUnsafe.G + c.G >> 1) * num2);
											c.B = (byte)((float)(colorUnsafe.B + c.B >> 1) * num3);
										}
										else
										{
											c = colorUnsafe;
										}
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Height = 4;
										s2.Y += 8;
										pos2.Y += 8f;
										if (ptr2[-1439].IsActive == 0)
										{
											c = Lighting.GetColorUnsafe(num22 - 1, num19 + 1);
											c.R = (byte)((float)(colorUnsafe.R + c.R >> 1) * num);
											c.G = (byte)((float)(colorUnsafe.G + c.G >> 1) * num2);
											c.B = (byte)((float)(colorUnsafe.B + c.B >> 1) * num3);
										}
										else
										{
											c = colorUnsafe;
										}
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Width = 8;
										s2.X += 4;
										pos2.X += 4f;
										if (ptr2[1].IsActive == 0)
										{
											c = Lighting.GetColorUnsafe(num22, num19 + 1);
											c.R = (byte)((float)(colorUnsafe.R + c.R >> 1) * num);
											c.G = (byte)((float)(colorUnsafe.G + c.G >> 1) * num2);
											c.B = (byte)((float)(colorUnsafe.B + c.B >> 1) * num3);
										}
										else
										{
											c = colorUnsafe;
										}
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Height = 8;
										s2.Y -= 8;
										pos2.Y -= 8f;
										c.R = (byte)((float)(int)colorUnsafe.R * num);
										c.G = (byte)((float)(int)colorUnsafe.G * num2);
										c.B = (byte)((float)(int)colorUnsafe.B * num3);
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Height = 4;
										s2.Y -= 4;
										pos2.Y -= 4f;
										if (ptr2[-1].IsActive == 0)
										{
											c = Lighting.GetColorUnsafe(num22, num19 - 1);
											c.R = (byte)((float)(colorUnsafe.R + c.R >> 1) * num);
											c.G = (byte)((float)(colorUnsafe.G + c.G >> 1) * num2);
											c.B = (byte)((float)(colorUnsafe.B + c.B >> 1) * num3);
										}
										else
										{
											c = colorUnsafe;
										}
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Width = 4;
										s2.X += 8;
										pos2.X += 8f;
										if (ptr2[1439].IsActive == 0)
										{
											c = Lighting.GetColorUnsafe(num22 + 1, num19 - 1);
											c.R = (byte)((float)(colorUnsafe.R + c.R >> 1) * num);
											c.G = (byte)((float)(colorUnsafe.G + c.G >> 1) * num2);
											c.B = (byte)((float)(colorUnsafe.B + c.B >> 1) * num3);
										}
										else
										{
											c = colorUnsafe;
										}
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Height = 8;
										s2.Y += 4;
										pos2.Y += 4f;
										if (ptr2[1440].IsActive == 0)
										{
											c = Lighting.GetColorUnsafe(num22 + 1, num19);
											c.R = (byte)((float)(colorUnsafe.R + c.R >> 1) * num);
											c.G = (byte)((float)(colorUnsafe.G + c.G >> 1) * num2);
											c.B = (byte)((float)(colorUnsafe.B + c.B >> 1) * num3);
										}
										else
										{
											c = colorUnsafe;
										}
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Height = 4;
										s2.Y += 8;
										pos2.Y += 8f;
										if (ptr2[1441].IsActive == 0)
										{
											c = Lighting.GetColorUnsafe(num22 + 1, num19 + 1);
											c.R = (byte)((float)(colorUnsafe.R + c.R >> 1) * num);
											c.G = (byte)((float)(colorUnsafe.G + c.G >> 1) * num2);
											c.B = (byte)((float)(colorUnsafe.B + c.B >> 1) * num3);
										}
										else
										{
											c = colorUnsafe;
										}
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c);
										s2.Width = (s2.Height = 16);
										s2.X -= 12;
									}
									else if (SMOOTH_LIGHT && (colorUnsafe.R > 160 || (float)(int)colorUnsafe.G > 176f || (float)(int)colorUnsafe.B > 192f))
									{
										s2.Width = 8;
										s2.Height = 8;
										Color c2 = ((!Lighting.Brighter(num22, num19 - 1, num22 - 1, num19)) ? Lighting.GetColorUnsafe(num22, num19 - 1) : Lighting.GetColorUnsafe(num22 - 1, num19));
										c2.R = (byte)((float)(colorUnsafe.R + c2.R >> 1) * num);
										c2.G = (byte)((float)(colorUnsafe.G + c2.G >> 1) * num2);
										c2.B = (byte)((float)(colorUnsafe.B + c2.B >> 1) * num3);
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c2);
										s2.Y += 8;
										pos2.Y += 8f;
										c2 = ((!Lighting.Brighter(num22, num19 + 1, num22 - 1, num19)) ? Lighting.GetColorUnsafe(num22, num19 + 1) : Lighting.GetColorUnsafe(num22 - 1, num19));
										c2.R = (byte)((float)(colorUnsafe.R + c2.R >> 1) * num);
										c2.G = (byte)((float)(colorUnsafe.G + c2.G >> 1) * num2);
										c2.B = (byte)((float)(colorUnsafe.B + c2.B >> 1) * num3);
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c2);
										s2.X += 8;
										pos2.X += 8f;
										c2 = ((!Lighting.Brighter(num22, num19 + 1, num22 + 1, num19)) ? Lighting.GetColorUnsafe(num22, num19 + 1) : Lighting.GetColorUnsafe(num22 + 1, num19));
										c2.R = (byte)((float)(colorUnsafe.R + c2.R >> 1) * num);
										c2.G = (byte)((float)(colorUnsafe.G + c2.G >> 1) * num2);
										c2.B = (byte)((float)(colorUnsafe.B + c2.B >> 1) * num3);
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c2);
										s2.Y -= 8;
										pos2.Y -= 8f;
										c2 = ((!Lighting.Brighter(num22, num19 - 1, num22 + 1, num19)) ? Lighting.GetColorUnsafe(num22, num19 - 1) : Lighting.GetColorUnsafe(num22 + 1, num19));
										c2.R = (byte)((float)(colorUnsafe.R + c2.R >> 1) * num);
										c2.G = (byte)((float)(colorUnsafe.G + c2.G >> 1) * num2);
										c2.B = (byte)((float)(colorUnsafe.B + c2.B >> 1) * num3);
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, c2);
										s2.Width = (s2.Height = 16);
										s2.X -= 8;
									}
									else
									{
										colorUnsafe.R = (byte)((float)(int)colorUnsafe.R * num);
										colorUnsafe.G = (byte)((float)(int)colorUnsafe.G * num2);
										colorUnsafe.B = (byte)((float)(int)colorUnsafe.B * num3);
										SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, colorUnsafe);
									}
								}
								else
								{
									SpriteSheet<_sheetTiles>.Draw(2, ref pos2, ref s2, colorUnsafe);
								}
								num19++;
								ptr2++;
							}
							num19 -= 6;
						}
					}
					num20 += 96;
					num19 += 6;
				}
				num18 += 96;
			}
			if (flag)
			{
				num8 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.9f, 96.0) - 48.0);
				num9 = (ViewWidth + 64) / 96 + 2;
				num10 = num14 + num15 * 96;
				if (num10 > -32)
				{
					Vector2 position = new Vector2(32 + num8 + num17, 32 + num10);
					int num23 = num8 + 8;
					for (int n = 0; n < num9; n++)
					{
						for (int num24 = 0; num24 < 96; num24 += 16)
						{
							Color color2 = Lighting.GetColor(num23 + ScreenPosition.X >> 4, ScreenPosition.Y + num10 >> 4);
							num23 += 16;
							color2.R = (byte)((float)(int)color2.R * num);
							color2.G = (byte)((float)(int)color2.G * num2);
							color2.B = (byte)((float)(int)color2.B * num3);
							Main.SpriteBatch.Draw(backgroundTexture[4], position, new Rectangle(num24 + num17 + 16, 0, 16, 16), color2);
							position.X += 16f;
						}
					}
				}
			}
		}
		bool flag2 = false;
		int magmaLayerPixels = Main.MagmaLayerPixels;
		int num25 = 540;
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			num25 = 600;
		}
		if (Main.RockLayerPixels <= ScreenPosition.Y + num25)
		{
			num10 = Main.RockLayerPixels - ScreenPosition.Y + num25 - 28;
			num8 = (int)(0.0 - Math.IEEERemainder(96f + (float)ScreenPosition.X * 0.9f, 96.0) - 48.0) - 32;
			num9 = (ViewWidth + 64) / 96 + 2;
			int num26;
			int num27;
			if (Main.RockLayerPixels + Main.ResolutionHeight < ScreenPosition.Y - 16)
			{
				num26 = (int)(Math.IEEERemainder(num10, 96.0) - 96.0);
				num27 = (Main.ResolutionHeight - num26 + 96) / 96 + 1;
			}
			else
			{
				num26 = num10;
				num27 = (Main.ResolutionHeight - num10 + 96) / 96 + 1;
			}
			if (magmaLayerPixels < ScreenPosition.Y + num25)
			{
				num27 = (magmaLayerPixels - ScreenPosition.Y + num25 - num26) / 96;
				flag2 = true;
			}
			int num28 = num8 + ScreenPosition.X;
			num28 = -(num28 & 0xF);
			if (num28 == -8)
			{
				num28 = 8;
			}
			for (int num29 = 0; num29 < num9; num29++)
			{
				for (int num30 = 0; num30 < num27; num30++)
				{
					for (int num31 = 0; num31 < 96; num31 += 16)
					{
						int num32 = num8 + 96 * num29 + num31 + 8 + ScreenPosition.X >> 4;
						for (int num33 = 0; num33 < 96; num33 += 16)
						{
							int num34 = num26 + num30 * 96 + num33 + 8 + ScreenPosition.Y >> 4;
							Color colorUnsafe2 = Lighting.GetColorUnsafe(num32, num34);
							bool flag3 = false;
							int wallType = Main.TileSet[num32, num34].WallType;
							if (wallType == 0 || wallType == 21 || Main.TileSet[num32 - 1, num34].WallType == 0 || Main.TileSet[num32 - 1, num34].WallType == 21 || Main.TileSet[num32 + 1, num34].WallType == 0 || Main.TileSet[num32 + 1, num34].WallType == 21)
							{
								flag3 = true;
							}
							if ((!flag3 && colorUnsafe2.R != 0 && colorUnsafe2.G != 0 && colorUnsafe2.B != 0) || (colorUnsafe2.R <= 0 && colorUnsafe2.G <= 0 && colorUnsafe2.B <= 0))
							{
								continue;
							}
							if (SMOOTH_LIGHT && colorUnsafe2.R < 230 && colorUnsafe2.G < 230 && colorUnsafe2.B < 230)
							{
								if ((colorUnsafe2.R > 226 || (double)(int)colorUnsafe2.G > 248.6 || (double)(int)colorUnsafe2.B > 271.2) && Main.TileSet[num32, num34].IsActive == 0)
								{
									for (int num35 = 0; num35 < 9; num35++)
									{
										int num36 = 0;
										int num37 = 0;
										int width = 4;
										int height = 4;
										Color color3 = colorUnsafe2;
										Color color4 = colorUnsafe2;
										switch (num35)
										{
										case 0:
											if (Main.TileSet[num32 - 1, num34 - 1].IsActive == 0)
											{
												color4 = Lighting.GetColorUnsafe(num32 - 1, num34 - 1);
											}
											break;
										case 1:
											width = 8;
											num36 = 4;
											if (Main.TileSet[num32, num34 - 1].IsActive == 0)
											{
												color4 = Lighting.GetColorUnsafe(num32, num34 - 1);
											}
											break;
										case 2:
											if (Main.TileSet[num32 + 1, num34 - 1].IsActive == 0)
											{
												color4 = Lighting.GetColorUnsafe(num32 + 1, num34 - 1);
											}
											num36 = 12;
											break;
										case 3:
											if (Main.TileSet[num32 - 1, num34].IsActive == 0)
											{
												color4 = Lighting.GetColorUnsafe(num32 - 1, num34);
											}
											height = 8;
											num37 = 4;
											break;
										case 4:
											width = 8;
											height = 8;
											num36 = 4;
											num37 = 4;
											break;
										case 5:
											num36 = 12;
											num37 = 4;
											height = 8;
											if (Main.TileSet[num32 + 1, num34].IsActive == 0)
											{
												color4 = Lighting.GetColorUnsafe(num32 + 1, num34);
											}
											break;
										case 6:
											if (Main.TileSet[num32 - 1, num34 + 1].IsActive == 0)
											{
												color4 = Lighting.GetColorUnsafe(num32 - 1, num34 + 1);
											}
											num37 = 12;
											break;
										case 7:
											width = 8;
											height = 4;
											num36 = 4;
											num37 = 12;
											if (Main.TileSet[num32, num34 + 1].IsActive == 0)
											{
												color4 = Lighting.GetColorUnsafe(num32, num34 + 1);
											}
											break;
										default:
											if (Main.TileSet[num32 + 1, num34 + 1].IsActive == 0)
											{
												color4 = Lighting.GetColorUnsafe(num32 + 1, num34 + 1);
											}
											num36 = 12;
											num37 = 12;
											break;
										}
										color3.R = (byte)((float)(colorUnsafe2.R + color4.R >> 1) * num);
										color3.G = (byte)((float)(colorUnsafe2.G + color4.G >> 1) * num2);
										color3.B = (byte)((float)(colorUnsafe2.B + color4.B >> 1) * num3);
										Main.SpriteBatch.Draw(backgroundTexture[3], new Vector2(32 + num8 + 96 * num29 + num31 + num36 + num28, 32 + num26 + 96 * num30 + num33 + num37), new Rectangle(num31 + num36 + num28 + 16, num33 + num37, width, height), color3);
									}
								}
								else if (colorUnsafe2.R > 160 || (double)(int)colorUnsafe2.G > 176.0 || (double)(int)colorUnsafe2.B > 192.0)
								{
									for (int num38 = 0; num38 < 4; num38++)
									{
										int num39 = 0;
										int num40 = 0;
										Color color5 = colorUnsafe2;
										Color color6 = colorUnsafe2;
										switch (num38)
										{
										case 0:
											color6 = ((!Lighting.Brighter(num32, num34 - 1, num32 - 1, num34)) ? Lighting.GetColorUnsafe(num32, num34 - 1) : Lighting.GetColorUnsafe(num32 - 1, num34));
											break;
										case 1:
											color6 = ((!Lighting.Brighter(num32, num34 - 1, num32 + 1, num34)) ? Lighting.GetColorUnsafe(num32, num34 - 1) : Lighting.GetColorUnsafe(num32 + 1, num34));
											num39 = 8;
											break;
										case 2:
											color6 = ((!Lighting.Brighter(num32, num34 + 1, num32 - 1, num34)) ? Lighting.GetColorUnsafe(num32, num34 + 1) : Lighting.GetColorUnsafe(num32 - 1, num34));
											num40 = 8;
											break;
										default:
											color6 = ((!Lighting.Brighter(num32, num34 + 1, num32 + 1, num34)) ? Lighting.GetColorUnsafe(num32, num34 + 1) : Lighting.GetColorUnsafe(num32 + 1, num34));
											num39 = 8;
											num40 = 8;
											break;
										}
										color5.R = (byte)((float)(colorUnsafe2.R + color6.R >> 1) * num);
										color5.G = (byte)((float)(colorUnsafe2.G + color6.G >> 1) * num2);
										color5.B = (byte)((float)(colorUnsafe2.B + color6.B >> 1) * num3);
										Main.SpriteBatch.Draw(backgroundTexture[3], new Vector2(32 + num8 + 96 * num29 + num31 + num39 + num28, 32 + num26 + 96 * num30 + num33 + num40), new Rectangle(num31 + num39 + num28 + 16, num33 + num40, 8, 8), color5);
									}
								}
								else
								{
									colorUnsafe2.R = (byte)((float)(int)colorUnsafe2.R * num);
									colorUnsafe2.G = (byte)((float)(int)colorUnsafe2.G * num2);
									colorUnsafe2.B = (byte)((float)(int)colorUnsafe2.B * num3);
									Main.SpriteBatch.Draw(backgroundTexture[3], new Vector2(32 + num8 + 96 * num29 + num31 + num28, 32 + num26 + 96 * num30 + num33), new Rectangle(num31 + num28 + 16, num33, 16, 16), colorUnsafe2);
								}
							}
							else
							{
								colorUnsafe2.R = (byte)((float)(int)colorUnsafe2.R * num);
								colorUnsafe2.G = (byte)((float)(int)colorUnsafe2.G * num2);
								colorUnsafe2.B = (byte)((float)(int)colorUnsafe2.B * num3);
								Main.SpriteBatch.Draw(backgroundTexture[3], new Vector2(32 + num8 + 96 * num29 + num31 + num28, 32 + num26 + 96 * num30 + num33), new Rectangle(num31 + num28 + 16, num33, 16, 16), colorUnsafe2);
							}
						}
					}
				}
			}
			if (flag2)
			{
				num8 = (int)(0.0 - Math.IEEERemainder((float)ScreenPosition.X * 0.9f, 96.0) - 48.0);
				num9 = ViewWidth / 96 + 2;
				num10 = num26 + num27 * 96;
				Rectangle value = new Rectangle(0, Main.MagmaBGFrame << 4, 16, 16);
				int num41 = num8 + 8;
				for (int num42 = 0; num42 < num9; num42++)
				{
					for (int num43 = 0; num43 < 96; num43 += 16)
					{
						value.X = num43 + num28 + 16;
						Color color7 = Lighting.GetColor(num41 + ScreenPosition.X >> 4, ScreenPosition.Y + num10 >> 4);
						color7.R = (byte)((float)(int)color7.R * num);
						color7.G = (byte)((float)(int)color7.G * num2);
						color7.B = (byte)((float)(int)color7.B * num3);
						Main.SpriteBatch.Draw(backgroundTexture[6], new Vector2(32 + num8 + 96 * num42 + num43 + num28, 32 + num10), value, color7);
						num41 += 16;
					}
				}
			}
		}
		if (magmaLayerPixels <= ScreenPosition.Y + num25)
		{
			num10 = magmaLayerPixels - ScreenPosition.Y + num25 - 28;
			num8 = (int)(0.0 - Math.IEEERemainder(96f + (float)ScreenPosition.X * 0.9f, 96.0) - 48.0) - 32;
			num9 = (ViewWidth + 64) / 96 + 2;
			int num44;
			int num45;
			if (magmaLayerPixels + Main.ResolutionHeight < ScreenPosition.Y - 16)
			{
				num44 = (int)(Math.IEEERemainder(num10, 96.0) - 96.0);
				num45 = (Main.ResolutionHeight - num44 + 96) / 96 + 1;
			}
			else
			{
				num44 = num10;
				num45 = (Main.ResolutionHeight - num10 + 96) / 96 + 1;
			}
			float num46 = num8 + ScreenPosition.X;
			num46 = 0f - (float)Math.IEEERemainder(num46, 16.0);
			num46 = (float)Math.Round(num46);
			int num47 = (int)num46;
			if (num47 == -8)
			{
				num47 = 8;
			}
			for (int num48 = 0; num48 < num9; num48++)
			{
				for (int num49 = 0; num49 < num45; num49++)
				{
					for (int num50 = 0; num50 < 96; num50 += 16)
					{
						int num51 = num8 + 96 * num48 + num50 + 8 + ScreenPosition.X >> 4;
						int num52 = num44 + num49 * 96 + 8 + ScreenPosition.Y >> 4;
						for (int num53 = 0; num53 < 96; num53 += 16)
						{
							Color colorUnsafe3 = Lighting.GetColorUnsafe(num51, num52);
							bool flag4 = false;
							int wallType2 = Main.TileSet[num51, num52].WallType;
							if (wallType2 == 0 || wallType2 == 21 || Main.TileSet[num51 - 1, num52].WallType == 0 || Main.TileSet[num51 - 1, num52].WallType == 21 || Main.TileSet[num51 + 1, num52].WallType == 0 || Main.TileSet[num51 + 1, num52].WallType == 21)
							{
								flag4 = true;
							}
							if ((flag4 || colorUnsafe3.R == 0 || colorUnsafe3.G == 0 || colorUnsafe3.B == 0) && (colorUnsafe3.R > 0 || colorUnsafe3.G > 0 || colorUnsafe3.B > 0))
							{
								if (SMOOTH_LIGHT && colorUnsafe3.R < 230 && colorUnsafe3.G < 230 && colorUnsafe3.B < 230)
								{
									if (((float)(int)colorUnsafe3.R > 339f || (float)(int)colorUnsafe3.G > 372.9f || (float)(int)colorUnsafe3.B > 1278f / (float)Math.PI) && Main.TileSet[num51, num52].IsActive == 0)
									{
										for (int num54 = 0; num54 < 9; num54++)
										{
											int num55 = 0;
											int num56 = 0;
											int width2 = 4;
											int height2 = 4;
											Color color8 = colorUnsafe3;
											Color color9 = colorUnsafe3;
											switch (num54)
											{
											case 0:
												if (Main.TileSet[num51 - 1, num52 - 1].IsActive == 0)
												{
													color9 = Lighting.GetColorUnsafe(num51 - 1, num52 - 1);
												}
												break;
											case 1:
												width2 = 8;
												num55 = 4;
												if (Main.TileSet[num51, num52 - 1].IsActive == 0)
												{
													color9 = Lighting.GetColorUnsafe(num51, num52 - 1);
												}
												break;
											case 2:
												if (Main.TileSet[num51 + 1, num52 - 1].IsActive == 0)
												{
													color9 = Lighting.GetColorUnsafe(num51 + 1, num52 - 1);
												}
												num55 = 12;
												break;
											case 3:
												if (Main.TileSet[num51 - 1, num52].IsActive == 0)
												{
													color9 = Lighting.GetColorUnsafe(num51 - 1, num52);
												}
												height2 = 8;
												num56 = 4;
												break;
											case 4:
												width2 = 8;
												height2 = 8;
												num55 = 4;
												num56 = 4;
												break;
											case 5:
												num55 = 12;
												num56 = 4;
												height2 = 8;
												if (Main.TileSet[num51 + 1, num52].IsActive == 0)
												{
													color9 = Lighting.GetColorUnsafe(num51 + 1, num52);
												}
												break;
											case 6:
												if (Main.TileSet[num51 - 1, num52 + 1].IsActive == 0)
												{
													color9 = Lighting.GetColorUnsafe(num51 - 1, num52 + 1);
												}
												num56 = 12;
												break;
											case 7:
												width2 = 8;
												height2 = 4;
												num55 = 4;
												num56 = 12;
												if (Main.TileSet[num51, num52 + 1].IsActive == 0)
												{
													color9 = Lighting.GetColorUnsafe(num51, num52 + 1);
												}
												break;
											default:
												if (Main.TileSet[num51 + 1, num52 + 1].IsActive == 0)
												{
													color9 = Lighting.GetColorUnsafe(num51 + 1, num52 + 1);
												}
												num55 = 12;
												num56 = 12;
												break;
											}
											color8.R = (byte)((float)(colorUnsafe3.R + color9.R >> 1) * num);
											color8.G = (byte)((float)(colorUnsafe3.G + color9.G >> 1) * num2);
											color8.B = (byte)((float)(colorUnsafe3.B + color9.B >> 1) * num3);
											Main.SpriteBatch.Draw(backgroundTexture[5], new Vector2(32 + num8 + 96 * num48 + num50 + num55 + num47, 32 + num44 + 96 * num49 + num53 + num56), new Rectangle(num50 + num55 + num47 + 16, num53 + 96 * Main.MagmaBGFrame + num56, width2, height2), color8);
										}
									}
									else if (colorUnsafe3.R > 240 || (double)(int)colorUnsafe3.G > 264.0 || (double)(int)colorUnsafe3.B > 288.0)
									{
										for (int num57 = 0; num57 < 4; num57++)
										{
											int num58 = 0;
											int num59 = 0;
											Color color10 = colorUnsafe3;
											Color color11 = colorUnsafe3;
											switch (num57)
											{
											case 0:
												color11 = ((!Lighting.Brighter(num51, num52 - 1, num51 - 1, num52)) ? Lighting.GetColorUnsafe(num51, num52 - 1) : Lighting.GetColorUnsafe(num51 - 1, num52));
												break;
											case 1:
												color11 = ((!Lighting.Brighter(num51, num52 - 1, num51 + 1, num52)) ? Lighting.GetColorUnsafe(num51, num52 - 1) : Lighting.GetColorUnsafe(num51 + 1, num52));
												num58 = 8;
												break;
											case 2:
												color11 = ((!Lighting.Brighter(num51, num52 + 1, num51 - 1, num52)) ? Lighting.GetColorUnsafe(num51, num52 + 1) : Lighting.GetColorUnsafe(num51 - 1, num52));
												num59 = 8;
												break;
											default:
												color11 = ((!Lighting.Brighter(num51, num52 + 1, num51 + 1, num52)) ? Lighting.GetColorUnsafe(num51, num52 + 1) : Lighting.GetColorUnsafe(num51 + 1, num52));
												num58 = 8;
												num59 = 8;
												break;
											}
											color10.R = (byte)((float)(colorUnsafe3.R + color11.R >> 1) * num);
											color10.G = (byte)((float)(colorUnsafe3.G + color11.G >> 1) * num2);
											color10.B = (byte)((float)(colorUnsafe3.B + color11.B >> 1) * num3);
											Main.SpriteBatch.Draw(backgroundTexture[5], new Vector2(32 + num8 + 96 * num48 + num50 + num58 + num47, 32 + num44 + 96 * num49 + num53 + num59), new Rectangle(num50 + num58 + num47 + 16, num53 + 96 * Main.MagmaBGFrame + num59, 8, 8), color10);
										}
									}
									else
									{
										colorUnsafe3.R = (byte)((float)(int)colorUnsafe3.R * num);
										colorUnsafe3.G = (byte)((float)(int)colorUnsafe3.G * num2);
										colorUnsafe3.B = (byte)((float)(int)colorUnsafe3.B * num3);
										Main.SpriteBatch.Draw(backgroundTexture[5], new Vector2(32 + num8 + 96 * num48 + num50 + num47, 32 + num44 + 96 * num49 + num53), new Rectangle(num50 + num47 + 16, num53 + 96 * Main.MagmaBGFrame, 16, 16), colorUnsafe3);
									}
								}
								else
								{
									colorUnsafe3.R = (byte)((float)(int)colorUnsafe3.R * num);
									colorUnsafe3.G = (byte)((float)(int)colorUnsafe3.G * num2);
									colorUnsafe3.B = (byte)((float)(int)colorUnsafe3.B * num3);
									Main.SpriteBatch.Draw(backgroundTexture[5], new Vector2(32 + num8 + 96 * num48 + num50 + num47, 32 + num44 + 96 * num49 + num53), new Rectangle(num50 + num47 + 16, num53 + 96 * Main.MagmaBGFrame, 16, 16), colorUnsafe3);
								}
							}
							num52++;
						}
					}
				}
			}
		}
		Lighting.BrightnessLevel = (Player.blind ? 1f : Lighting.DefBrightness);
	}

	private void RenderBlack()
	{
		GraphicsDevice.SetRenderTarget(blackTarget);
		GraphicsDevice.Clear(default);
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, renderTargetProjection);
		DrawBlack();
		Main.SpriteBatch.End();
		sceneBlackPos.X = ScreenPosition.X - 32;
		sceneBlackPos.Y = ScreenPosition.Y - 32;
	}

	private void RenderWalls()
	{
		GraphicsDevice.SetRenderTarget(wallTarget);
		GraphicsDevice.Clear(default);
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, renderTargetProjection);
		DrawWalls();
		Main.SpriteBatch.End();
		sceneWallPos.X = ScreenPosition.X - 32;
		sceneWallPos.Y = ScreenPosition.Y - 32;
	}

	private void RenderBackWater()
	{
		GraphicsDevice.SetRenderTarget(backWaterTarget);
		GraphicsDevice.Clear(default);
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, renderTargetProjection);
		DrawWater(bg: true);
		Main.SpriteBatch.End();
	}

	private void RenderBackground()
	{
		GraphicsDevice.SetRenderTarget(backgroundTarget);
		GraphicsDevice.Clear(default);
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, renderTargetProjection);
		DrawBackground();
		Main.SpriteBatch.End();
		sceneBackgroundPos.X = ScreenPosition.X - 32;
		sceneBackgroundPos.Y = ScreenPosition.Y - 32;
	}

	private void RenderSolidTiles()
	{
		GraphicsDevice.SetRenderTarget(tileSolidTarget);
		GraphicsDevice.Clear(default);
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, renderTargetProjection);
		DrawSolidTiles();
		Main.SpriteBatch.End();
		sceneTilePos.X = ScreenPosition.X - 32;
		sceneTilePos.Y = ScreenPosition.Y - 32;
	}

	private void RenderNonSolidTiles()
	{
		GraphicsDevice.SetRenderTarget(tileNonSolidTarget);
		GraphicsDevice.Clear(default);
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, renderTargetProjection);
		DrawNonSolidTiles();
		Main.SpriteBatch.End();
		sceneTile2Pos.X = ScreenPosition.X - 32;
		sceneTile2Pos.Y = ScreenPosition.Y - 32;
	}

	private void RenderWater()
	{
		GraphicsDevice.SetRenderTarget(waterTarget);
		GraphicsDevice.Clear(default);
		Main.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, renderTargetProjection);
		DrawWater();
		if (Player.Inventory[Player.SelectedItem].Mech)
		{
			DrawWires();
		}
		Main.SpriteBatch.End();
		sceneWaterPos.X = ScreenPosition.X - 32;
		sceneWaterPos.Y = ScreenPosition.Y - 32;
	}

	public static void shine(ref Color newColor, int type)
	{
		int num;
		int num2;
		int num3;
		switch ((EntityID.TileID)type)
		{
		case EntityID.TileID.EBONSTONE:
			num = newColor.R * 243 >> 8;
			num2 = newColor.G * 217 >> 8;
			num3 = newColor.B * 281 >> 8;
			break;
		case EntityID.TileID.PEARLSTONE:
			num = newColor.R * 281 >> 8;
			num2 = newColor.G;
			num3 = newColor.B * 307 >> 8;
			if (num > 255)
			{
				num = 255;
			}
			break;
		default:
			num = newColor.R * 409 >> 8;
			num2 = newColor.G * 409 >> 8;
			num3 = newColor.B * 409 >> 8;
			if (num > 255)
			{
				num = 255;
			}
			if (num2 > 255)
			{
				num2 = 255;
			}
			break;
		}
		if (num3 > 255)
		{
			num3 = 255;
		}
		newColor.R = (byte)num;
		newColor.G = (byte)num2;
		newColor.B = (byte)num3;
	}

	private unsafe void Highlight2x1(Tile* pTile, Tile.Flags mask)
	{
		Tile.Flags* ptr = &pTile->CurrentFlags;
		*ptr |= mask;
		pTile = ((pTile->FrameX != 0) ? (pTile - 1440) : (pTile + 1440));
		Tile.Flags* ptr2 = &pTile->CurrentFlags;
		*ptr2 |= mask;
	}

	private unsafe void Highlight2x2(Tile* pTile, Tile.Flags mask)
	{
		int num = ((pTile->FrameY == 0) ? 1 : (-1));
		Tile.Flags* ptr = &pTile->CurrentFlags;
		*ptr |= mask;
		pTile += num;
		Tile.Flags* ptr2 = &pTile->CurrentFlags;
		*ptr2 |= mask;
		pTile = ((((pTile->FrameX / 18) & 1) != 0) ? (pTile - 1440) : (pTile + 1440));
		Tile.Flags* ptr3 = &pTile->CurrentFlags;
		*ptr3 |= mask;
		pTile -= num;
		Tile.Flags* ptr4 = &pTile->CurrentFlags;
		*ptr4 |= mask;
	}

	private unsafe void Highlight1x3(Tile* pTile, Tile.Flags mask)
	{
		Tile.Flags* ptr = &pTile->CurrentFlags;
		*ptr |= mask;
		if (pTile->FrameY == 0)
		{
			pTile++;
			Tile.Flags* ptr2 = &pTile->CurrentFlags;
			*ptr2 |= mask;
			pTile++;
		}
		else if (pTile->FrameY == 18)
		{
			pTile++;
			Tile.Flags* ptr3 = &pTile->CurrentFlags;
			*ptr3 |= mask;
			pTile -= 2;
		}
		else
		{
			pTile--;
			Tile.Flags* ptr4 = &pTile->CurrentFlags;
			*ptr4 |= mask;
			pTile--;
		}
		Tile.Flags* ptr5 = &pTile->CurrentFlags;
		*ptr5 |= mask;
	}

	private unsafe void Highlight2x3(Tile* pTile, Tile.Flags mask)
	{
		Tile.Flags* ptr = &pTile->CurrentFlags;
		*ptr |= mask;
		if (pTile->FrameY == 0)
		{
			pTile++;
			Tile.Flags* ptr2 = &pTile->CurrentFlags;
			*ptr2 |= mask;
			pTile++;
			Tile.Flags* ptr3 = &pTile->CurrentFlags;
			*ptr3 |= mask;
			if (((pTile->FrameX / 18) & 1) == 0)
			{
				pTile += 1438;
				Tile.Flags* ptr4 = &pTile->CurrentFlags;
				*ptr4 |= mask;
				pTile++;
				Tile.Flags* ptr5 = &pTile->CurrentFlags;
				*ptr5 |= mask;
				pTile++;
			}
			else
			{
				pTile -= 1440;
				Tile.Flags* ptr6 = &pTile->CurrentFlags;
				*ptr6 |= mask;
				pTile--;
				Tile.Flags* ptr7 = &pTile->CurrentFlags;
				*ptr7 |= mask;
				pTile--;
			}
		}
		else if (pTile->FrameY == 18)
		{
			pTile++;
			Tile.Flags* ptr8 = &pTile->CurrentFlags;
			*ptr8 |= mask;
			pTile -= 2;
			Tile.Flags* ptr9 = &pTile->CurrentFlags;
			*ptr9 |= mask;
			if (((pTile->FrameX / 18) & 1) == 0)
			{
				pTile += 1440;
				Tile.Flags* ptr10 = &pTile->CurrentFlags;
				*ptr10 |= mask;
				pTile++;
				Tile.Flags* ptr11 = &pTile->CurrentFlags;
				*ptr11 |= mask;
				pTile++;
			}
			else
			{
				pTile -= 1440;
				Tile.Flags* ptr12 = &pTile->CurrentFlags;
				*ptr12 |= mask;
				pTile++;
				Tile.Flags* ptr13 = &pTile->CurrentFlags;
				*ptr13 |= mask;
				pTile++;
			}
		}
		else
		{
			pTile--;
			Tile.Flags* ptr14 = &pTile->CurrentFlags;
			*ptr14 |= mask;
			pTile--;
			Tile.Flags* ptr15 = &pTile->CurrentFlags;
			*ptr15 |= mask;
			if (((pTile->FrameX / 18) & 1) == 0)
			{
				pTile += 1440;
				Tile.Flags* ptr16 = &pTile->CurrentFlags;
				*ptr16 |= mask;
				pTile++;
				Tile.Flags* ptr17 = &pTile->CurrentFlags;
				*ptr17 |= mask;
				pTile++;
			}
			else
			{
				pTile -= 1440;
				Tile.Flags* ptr18 = &pTile->CurrentFlags;
				*ptr18 |= mask;
				pTile++;
				Tile.Flags* ptr19 = &pTile->CurrentFlags;
				*ptr19 |= mask;
				pTile++;
			}
		}
		Tile.Flags* ptr20 = &pTile->CurrentFlags;
		*ptr20 |= mask;
	}

	private unsafe void Highlight4x2(Tile* pTile, Tile.Flags mask)
	{
		int num = ((pTile->FrameY == 0) ? 1 : (-1));
		Tile.Flags* ptr = &pTile->CurrentFlags;
		*ptr |= mask;
		pTile += num;
		Tile.Flags* ptr2 = &pTile->CurrentFlags;
		*ptr2 |= mask;
		switch ((pTile->FrameX / 18) & 3)
		{
		case 0:
		{
			pTile += 1440;
			Tile.Flags* ptr15 = &pTile->CurrentFlags;
			*ptr15 |= mask;
			pTile -= num;
			Tile.Flags* ptr16 = &pTile->CurrentFlags;
			*ptr16 |= mask;
			pTile += 1440;
			Tile.Flags* ptr17 = &pTile->CurrentFlags;
			*ptr17 |= mask;
			pTile += num;
			Tile.Flags* ptr18 = &pTile->CurrentFlags;
			*ptr18 |= mask;
			pTile += 1440;
			break;
		}
		case 1:
		{
			pTile -= 1440;
			Tile.Flags* ptr11 = &pTile->CurrentFlags;
			*ptr11 |= mask;
			pTile -= num;
			Tile.Flags* ptr12 = &pTile->CurrentFlags;
			*ptr12 |= mask;
			pTile += 2880;
			Tile.Flags* ptr13 = &pTile->CurrentFlags;
			*ptr13 |= mask;
			pTile += num;
			Tile.Flags* ptr14 = &pTile->CurrentFlags;
			*ptr14 |= mask;
			pTile += 1440;
			break;
		}
		case 2:
		{
			pTile += 1440;
			Tile.Flags* ptr7 = &pTile->CurrentFlags;
			*ptr7 |= mask;
			pTile -= num;
			Tile.Flags* ptr8 = &pTile->CurrentFlags;
			*ptr8 |= mask;
			pTile -= 2880;
			Tile.Flags* ptr9 = &pTile->CurrentFlags;
			*ptr9 |= mask;
			pTile += num;
			Tile.Flags* ptr10 = &pTile->CurrentFlags;
			*ptr10 |= mask;
			pTile -= 1440;
			break;
		}
		default:
		{
			pTile -= 1440;
			Tile.Flags* ptr3 = &pTile->CurrentFlags;
			*ptr3 |= mask;
			pTile -= num;
			Tile.Flags* ptr4 = &pTile->CurrentFlags;
			*ptr4 |= mask;
			pTile -= 1440;
			Tile.Flags* ptr5 = &pTile->CurrentFlags;
			*ptr5 |= mask;
			pTile += num;
			Tile.Flags* ptr6 = &pTile->CurrentFlags;
			*ptr6 |= mask;
			pTile -= 1440;
			break;
		}
		}
		Tile.Flags* ptr19 = &pTile->CurrentFlags;
		*ptr19 |= mask;
		pTile -= num;
		Tile.Flags* ptr20 = &pTile->CurrentFlags;
		*ptr20 |= mask;
	}

	private unsafe void Highlight2x5(Tile* pTile, Tile.Flags mask)
	{
		int num = ((pTile->FrameX == 0) ? 1440 : (-1440));
		Tile.Flags* ptr = &pTile->CurrentFlags;
		*ptr |= mask;
		pTile += num;
		Tile.Flags* ptr2 = &pTile->CurrentFlags;
		*ptr2 |= mask;
		switch (pTile->FrameY / 18)
		{
		case 0:
		{
			pTile++;
			Tile.Flags* ptr27 = &pTile->CurrentFlags;
			*ptr27 |= mask;
			pTile -= num;
			Tile.Flags* ptr28 = &pTile->CurrentFlags;
			*ptr28 |= mask;
			pTile++;
			Tile.Flags* ptr29 = &pTile->CurrentFlags;
			*ptr29 |= mask;
			pTile += num;
			Tile.Flags* ptr30 = &pTile->CurrentFlags;
			*ptr30 |= mask;
			pTile++;
			Tile.Flags* ptr31 = &pTile->CurrentFlags;
			*ptr31 |= mask;
			pTile -= num;
			Tile.Flags* ptr32 = &pTile->CurrentFlags;
			*ptr32 |= mask;
			pTile++;
			break;
		}
		case 1:
		{
			pTile--;
			Tile.Flags* ptr21 = &pTile->CurrentFlags;
			*ptr21 |= mask;
			pTile -= num;
			Tile.Flags* ptr22 = &pTile->CurrentFlags;
			*ptr22 |= mask;
			pTile += 2;
			Tile.Flags* ptr23 = &pTile->CurrentFlags;
			*ptr23 |= mask;
			pTile += num;
			Tile.Flags* ptr24 = &pTile->CurrentFlags;
			*ptr24 |= mask;
			pTile++;
			Tile.Flags* ptr25 = &pTile->CurrentFlags;
			*ptr25 |= mask;
			pTile -= num;
			Tile.Flags* ptr26 = &pTile->CurrentFlags;
			*ptr26 |= mask;
			pTile++;
			break;
		}
		case 2:
		{
			pTile--;
			Tile.Flags* ptr15 = &pTile->CurrentFlags;
			*ptr15 |= mask;
			pTile -= num;
			Tile.Flags* ptr16 = &pTile->CurrentFlags;
			*ptr16 |= mask;
			pTile--;
			Tile.Flags* ptr17 = &pTile->CurrentFlags;
			*ptr17 |= mask;
			pTile += num;
			Tile.Flags* ptr18 = &pTile->CurrentFlags;
			*ptr18 |= mask;
			pTile += 3;
			Tile.Flags* ptr19 = &pTile->CurrentFlags;
			*ptr19 |= mask;
			pTile -= num;
			Tile.Flags* ptr20 = &pTile->CurrentFlags;
			*ptr20 |= mask;
			pTile++;
			break;
		}
		case 3:
		{
			pTile++;
			Tile.Flags* ptr9 = &pTile->CurrentFlags;
			*ptr9 |= mask;
			pTile -= num;
			Tile.Flags* ptr10 = &pTile->CurrentFlags;
			*ptr10 |= mask;
			pTile -= 2;
			Tile.Flags* ptr11 = &pTile->CurrentFlags;
			*ptr11 |= mask;
			pTile += num;
			Tile.Flags* ptr12 = &pTile->CurrentFlags;
			*ptr12 |= mask;
			pTile--;
			Tile.Flags* ptr13 = &pTile->CurrentFlags;
			*ptr13 |= mask;
			pTile -= num;
			Tile.Flags* ptr14 = &pTile->CurrentFlags;
			*ptr14 |= mask;
			pTile--;
			break;
		}
		default:
		{
			pTile--;
			Tile.Flags* ptr3 = &pTile->CurrentFlags;
			*ptr3 |= mask;
			pTile -= num;
			Tile.Flags* ptr4 = &pTile->CurrentFlags;
			*ptr4 |= mask;
			pTile--;
			Tile.Flags* ptr5 = &pTile->CurrentFlags;
			*ptr5 |= mask;
			pTile += num;
			Tile.Flags* ptr6 = &pTile->CurrentFlags;
			*ptr6 |= mask;
			pTile--;
			Tile.Flags* ptr7 = &pTile->CurrentFlags;
			*ptr7 |= mask;
			pTile -= num;
			Tile.Flags* ptr8 = &pTile->CurrentFlags;
			*ptr8 |= mask;
			pTile--;
			break;
		}
		}
		Tile.Flags* ptr33 = &pTile->CurrentFlags;
		*ptr33 |= mask;
		pTile += num;
		Tile.Flags* ptr34 = &pTile->CurrentFlags;
		*ptr34 |= mask;
	}

	private unsafe void DrawNonSolidTiles()
	{
		int num = 198;
		int num2 = 38;
		int num3 = 0;
		Rectangle s = default;
		Vector2 pos = default;
		Main.TileSolid[10] = false;
		fixed (Tile* tileSet = Main.TileSet)
		{
			int i;
			if (!Player.IsDead)
			{
				Tile* ptr = (Tile*)(void*)(((Main.FrameCounter & 0x20) == 0) ? ((IntPtr)(void*)null) : ((IntPtr)(tileSet + (Player.tileInteractX * 1440 + Player.tileInteractY))));
				int num4 = (Player.XYWH.X + 10 >> 4) - 10;
				int num5 = (Player.XYWH.Y + 21 >> 4) - 8;
				for (i = 0; i < 20; i++)
				{
					Tile* ptr2 = tileSet + ((num4 + i) * 1440 + num5);
					for (int j = 0; j < 16; j++)
					{
						if (ptr2->IsActive != 0)
						{
							Tile.Flags flags = ((ptr2 == ptr) ? Tile.Flags.SELECTED : Tile.Flags.NEARBY);
							if ((ptr2->CurrentFlags & flags) != flags)
							{
								switch ((EntityID.TileID)ptr2->Type)
								{
								case EntityID.TileID.TORCH:
								case EntityID.TileID.BOTTLE:
								case EntityID.TileID.CANDLE:
								case EntityID.TileID.WATER_CANDLE:
								case EntityID.TileID.BOOK:
								case EntityID.TileID.SWITCH:
								case EntityID.TileID.TIMER:
								{
									Tile.Flags* ptr3 = &ptr2->CurrentFlags;
									*ptr3 |= flags;
									break;
								}
								case EntityID.TileID.DOOR_OPEN:
								case EntityID.TileID.MANNEQUIN:
									Highlight2x3(ptr2, flags);
									break;
								case EntityID.TileID.DOOR_CLOSED:
									Highlight1x3(ptr2, flags);
									break;
								case EntityID.TileID.CHEST:
								case EntityID.TileID.SIGN:
								case EntityID.TileID.TOMBSTONE:
								case EntityID.TileID.SAFE:
								case EntityID.TileID.CRYSTAL_BALL:
								case EntityID.TileID.LEVER:
								case EntityID.TileID.MUSIC_BOX:
									Highlight2x2(ptr2, flags);
									break;
								case EntityID.TileID.PIGGYBANK:
									Highlight2x1(ptr2, flags);
									break;
								case EntityID.TileID.BED:
									Highlight4x2(ptr2, flags);
									break;
								case EntityID.TileID.GRANDFATHERS_CLOCK:
									Highlight2x5(ptr2, flags);
									break;
								}
							}
						}
						ptr2++;
					}
				}
			}
			i = FirstTileX - 1;
			int num6 = LastTileX + 2;
			int num7 = LastTileY + 2;
			do
			{
				int num8 = FirstTileY;
				Tile* ptr4 = tileSet + (i * 1440 + num8 - 1);
				do
				{
					ptr4++;
					if (ptr4->IsActive == 0)
					{
						continue;
					}
					int num9 = ptr4->Type;
					if (Main.TileSolid[num9])
					{
						continue;
					}
					Color newColor = Lighting.GetColorUnsafe(i, num8);
					int num10 = 0;
					int height = 16;
					int num11 = 16;
					s.X = ptr4->FrameX;
					s.Y = ptr4->FrameY;
					switch ((EntityID.TileID)num9)
					{
					case EntityID.TileID.SHORT_GRASS_PLANTS:
					case EntityID.TileID.SHORT_CORRUPT_PLANTS:
					case EntityID.TileID.SHORT_JUNGLE_PLANTS:
					case EntityID.TileID.GLOWING_MUSHROOM:
					case EntityID.TileID.SHORT_HALLOWED_PLANTS:
						height = 20;
						break;
					case EntityID.TileID.TORCH:
						num11 = 20;
						height = 20;
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 396;
						}
						break;
					case EntityID.TileID.TREE:
						num11 = 20;
						height = 20;
						break;
					case EntityID.TileID.DOOR_CLOSED:
					case EntityID.TileID.DOOR_OPEN:
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 108;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 54;
						}
						break;
					case EntityID.TileID.BOTTLE:
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 36;
						}
						break;
					case EntityID.TileID.PIGGYBANK:
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 36;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 18;
						}
						break;
					case EntityID.TileID.CHEST:
						height = 18;
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 76;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 38;
						}
						break;
					case EntityID.TileID.CANDLE:
					case EntityID.TileID.WATER_CANDLE:
						num10 = -4;
						height = 20;
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 44;
						}
						break;
					case EntityID.TileID.BOOK:
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 32;
						}
						break;
					case EntityID.TileID.TALL_GRASS_PLANTS:
					case EntityID.TileID.TALL_JUNGLE_PLANTS:
					case EntityID.TileID.TALL_HALLOWED_PLANTS:
						num10 = -12;
						height = 32;
						break;
					case EntityID.TileID.CLAY_POT:
					case EntityID.TileID.STATUE:
					case EntityID.TileID.PUMP_IN:
					case EntityID.TileID.PUMP_OUT:
						num10 = 2;
						break;
					case EntityID.TileID.CORAL:
						num10 = -8;
						num11 = 24;
						height = 26;
						break;
					case EntityID.TileID.TOMBSTONE:
						num10 = 2;
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 72;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 36;
						}
						break;
					case EntityID.TileID.GRANDFATHERS_CLOCK:
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 176;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 88;
						}
						break;
					case EntityID.TileID.SAFE:
					case EntityID.TileID.CRYSTAL_BALL:
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 68;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 34;
						}
						break;
					case EntityID.TileID.MANNEQUIN:
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 104;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 52;
						}
						break;
					case EntityID.TileID.LEVER:
						num10 = 2;
						height = 18;
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 72;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 36;
						}
						break;
					case EntityID.TileID.PRESSURE_PLATE:
						num10 = 2;
						height = 18;
						break;
					case EntityID.TileID.SIGN:
					case EntityID.TileID.BED:
					case EntityID.TileID.SWITCH:
					case EntityID.TileID.TIMER:
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 72;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 36;
						}
						break;
					case EntityID.TileID.TABLE:
					case EntityID.TileID.CHAIR:
					case EntityID.TileID.ANVIL:
					case EntityID.TileID.FURNACE:
					case EntityID.TileID.WORK_BENCH:
					case EntityID.TileID.SAPLING:
					case EntityID.TileID.DEMON_ALTAR:
					case EntityID.TileID.SUNFLOWER:
					case EntityID.TileID.CORRUPTION_THORN:
					case EntityID.TileID.JUNGLE_THORN:
					case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
					case EntityID.TileID.HELLFORGE:
					case EntityID.TileID.CACTUS:
					case EntityID.TileID.WOODEN_BEAM:
						height = 18;
						break;
					case EntityID.TileID.MUSIC_BOX:
						num10 = 2;
						if ((ptr4->CurrentFlags & Tile.Flags.SELECTED) != 0)
						{
							s.Y += 1512;
						}
						else if ((ptr4->CurrentFlags & Tile.Flags.NEARBY) != 0)
						{
							s.Y += 756;
						}
						break;
					}
					s.Width = num11;
					s.Height = height;
					pos.X = (i << 4) - ScreenPosition.X - (num11 - 16 >> 1) + 32;
					pos.Y = (num8 << 4) - ScreenPosition.Y + num10 + 32;
					if (Player.findTreasure)
					{
						switch ((EntityID.TileID)num9)
						{
						case EntityID.TileID.LIFE_CRYSTAL:
						case EntityID.TileID.CHEST:
						case EntityID.TileID.POT:
						case EntityID.TileID.DAYBLOOM_GROWING:
						case EntityID.TileID.DAYBLOOM_MATURE:
						case EntityID.TileID.DAYBLOOM_BLOOMING:
							if (newColor.R < UI.MouseTextBrightness >> 1)
							{
								newColor.R = (byte)(UI.MouseTextBrightness >> 1);
							}
							if (newColor.G < 70)
							{
								newColor.G = 70;
							}
							if (newColor.B < 210)
							{
								newColor.B = 210;
							}
							newColor.A = UI.MouseTextBrightness;
							if (Main.Rand.Next(150) == 0)
							{
								Dust* ptr5 = dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 15, 0.0, 0.0, 150, default, 0.800000011920929);
								if (ptr5 != null)
								{
									ptr5->Velocity.X *= 0.1f;
									ptr5->Velocity.Y *= 0.1f;
									ptr5->NoLight = true;
								}
							}
							break;
						}
					}
					switch ((EntityID.TileID)num9)
					{
					case EntityID.TileID.TORCH:
						if (ptr4->FrameX < 66 && Main.Rand.Next(40) == 0)
						{
							int num13 = ptr4->FrameY / 22;
							if (1 == 0)
							{
							}
							int num14 = num13 switch
							{
								0 => 6, 
								8 => 75, 
								_ => 58 + num13, 
							};
							if (1 == 0)
							{
							}
							num13 = num14;
							if (ptr4->FrameX == 22)
							{
								dustLocal.NewDust(i * 16 + 6, num8 * 16, 4, 4, num13, 0.0, 0.0, 100);
							}
							else if (ptr4->FrameX == 44)
							{
								dustLocal.NewDust(i * 16 + 2, num8 * 16, 4, 4, num13, 0.0, 0.0, 100);
							}
							else
							{
								dustLocal.NewDust(i * 16 + 4, num8 * 16, 4, 4, num13, 0.0, 0.0, 100);
							}
						}
						break;
					case EntityID.TileID.SHORT_CORRUPT_PLANTS:
					case EntityID.TileID.CORRUPTION_THORN:
						if (Main.Rand.Next(500) == 0)
						{
							dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 14);
						}
						break;
					case EntityID.TileID.DEMON_ALTAR:
					case EntityID.TileID.SHADOW_ORB:
						if (Main.Rand.Next(20) == 0)
						{
							dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 14, 0.0, 0.0, 100);
						}
						break;
					case EntityID.TileID.CANDLE:
						if (ptr4->FrameX == 0 && Main.Rand.Next(40) == 0)
						{
							dustLocal.NewDust(i * 16 + 4, num8 * 16 - 4, 4, 4, 6, 0.0, 0.0, 100);
						}
						break;
					case EntityID.TileID.CHANDELIER:
					case EntityID.TileID.JACK_O_LANTERN:
					case EntityID.TileID.PRESENT:
						if ((ptr4->FrameX == 0 || ptr4->FrameX == 36) && ptr4->FrameY == 18 && Main.Rand.Next(40) == 0)
						{
							dustLocal.NewDust(i * 16, num8 * 16 + 2, 14, 6, 6, 0.0, 0.0, 100);
						}
						break;
					case EntityID.TileID.WATER_CANDLE:
						if (Main.Rand.Next(20) == 0)
						{
							dustLocal.NewDust(i * 16 + 4, num8 * 16 - 4, 4, 4, 29, 0.0, 0.0, 100);
						}
						break;
					case EntityID.TileID.TIKI_TORCH:
						if (ptr4->FrameX == 0 && ptr4->FrameY == 0 && Main.Rand.Next(40) == 0)
						{
							dustLocal.NewDust(i * 16 + 4, num8 * 16 + 2, 4, 4, 6, 0.0, 0.0, 100);
						}
						break;
					case EntityID.TileID.SKULL_LANTERN:
						if (ptr4->FrameX == 0 && ptr4->FrameY == 0 && Main.Rand.Next(40) == 0)
						{
							dustLocal.NewDust(i * 16 + 12, num8 * 16 + 2, 4, 4, 6, 0.0, 0.0, 100);
						}
						break;
					case EntityID.TileID.CANDELABRA:
						if (ptr4->FrameY != 0 || ptr4->FrameX >= 36 || Main.Rand.Next(40) != 0)
						{
							break;
						}
						if (ptr4->FrameX == 0)
						{
							if (Main.Rand.Next(3) == 0)
							{
								dustLocal.NewDust(i * 16 + 4, num8 * 16 + 2, 4, 4, 6, 0.0, 0.0, 100);
							}
							else
							{
								dustLocal.NewDust(i * 16 + 14, num8 * 16 + 2, 4, 4, 6, 0.0, 0.0, 100);
							}
						}
						else if (Main.Rand.Next(3) == 0)
						{
							dustLocal.NewDust(i * 16 + 6, num8 * 16 + 2, 4, 4, 6, 0.0, 0.0, 100);
						}
						else
						{
							dustLocal.NewDust(i * 16, num8 * 16 + 2, 4, 4, 6, 0.0, 0.0, 100);
						}
						break;
					case EntityID.TileID.SHORT_JUNGLE_PLANTS:
					{
						if (ptr4->FrameX != 144)
						{
							break;
						}
						if (Main.Rand.Next(60) == 0)
						{
							Dust* ptr7 = dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 44, 0.0, 0.0, 250, default, 0.4000000059604645);
							if (ptr7 != null)
							{
								ptr7->FadeIn = 0.7f;
							}
						}
						byte b = (newColor.G = (byte)(245 - UI.MouseTextBrightness + (UI.MouseTextBrightness >> 1)));
						byte b3 = b;
						byte b4 = b3;
						b = (newColor.B = b4);
						b3 = b;
						b4 = b3;
						b = (newColor.R = b4);
						b3 = b;
						b4 = b3;
						newColor.A = b4;
						break;
					}
					case EntityID.TileID.GLOWING_MUSHROOM:
					case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
						if (Main.Rand.Next(500) == 0)
						{
							dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 41, 0.0, 0.0, 250, default, 0.800000011920929);
						}
						break;
					case EntityID.TileID.FURNACE:
					case EntityID.TileID.HELLFORGE:
					case EntityID.TileID.ADAMANTITE_FORGE:
						if (Main.Rand.Next(40) == 0 && ptr4->FrameX == 18 && ptr4->FrameY == 18)
						{
							dustLocal.NewDust(i * 16 + 2, num8 * 16, 8, 6, 6, 0.0, 0.0, 100);
						}
						break;
					default:
					{
						if (Main.TileShine[num9] <= 0 || (newColor.R <= 20 && newColor.B <= 20 && newColor.G <= 20))
						{
							break;
						}
						int num12 = newColor.R;
						if (newColor.G > num12)
						{
							num12 = newColor.G;
						}
						if (newColor.B > num12)
						{
							num12 = newColor.B;
						}
						num12 /= 30;
						if (Main.Rand.Next(Main.TileShine[num9]) < num12 && (num9 != 21 || (ptr4->FrameX >= 36 && ptr4->FrameX < 180)))
						{
							Dust* ptr6 = dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 43, 0.0, 0.0, 254, default, 0.5);
							if (ptr6 != null)
							{
								ptr6->Velocity.X = 0f;
								ptr6->Velocity.Y = 0f;
							}
						}
						break;
					}
					}
					if ((num9 == 5 && ptr4->FrameY >= 198 && ptr4->FrameX >= 22) || (num9 == 128 && ptr4->FrameX >= 100))
					{
						spec[num3].X = (short)i;
						spec[num3].Y = (short)num8;
						spec[num3++].tileColor = newColor;
						if (num9 == 128)
						{
							s.X %= 100;
							SpriteSheet<_sheetTiles>.Draw(154, ref pos, ref s, newColor);
						}
					}
					else if (num9 == 129)
					{
						newColor.R = 200;
						newColor.G = 200;
						newColor.B = 200;
						newColor.A = 0;
						SpriteSheet<_sheetTiles>.Draw(26 + num9, ref pos, ref s, newColor);
					}
					else
					{
						if (newColor.R <= 1 && newColor.G <= 1 && newColor.B <= 1)
						{
							continue;
						}
						if (num9 == 72 && ptr4->FrameX >= 36)
						{
							int num15 = ptr4->FrameY / 18;
							pos.X = i * 16 - ScreenPosition.X - 22 + 32;
							pos.Y = num8 * 16 - ScreenPosition.Y - 26 + 32;
							s.X = num15 * 62;
							s.Y = 0;
							s.Width = 60;
							s.Height = 42;
							SpriteSheet<_sheetTiles>.Draw(18, ref pos, ref s, newColor);
							continue;
						}
						if (num9 == 51)
						{
							newColor.R >>= 1;
							newColor.G >>= 1;
							newColor.B >>= 1;
							newColor.A >>= 1;
							SpriteSheet<_sheetTiles>.Draw(26 + num9, ref pos, ref s, newColor);
							continue;
						}
						if (num9 >= 82 && num9 <= 84)
						{
							if (num9 > 82)
							{
								int num16 = ptr4->FrameX / 18;
								if (num16 == 0 && WorldTime.DayTime)
								{
									num9 = 84;
								}
								else if (num16 == 1 && !WorldTime.DayTime)
								{
									num9 = 84;
								}
								else if (num16 == 3 && WorldTime.IsBloodMoon)
								{
									num9 = 84;
								}
								if (num9 == 84)
								{
									switch (num16)
									{
									case 0:
										if (Main.Rand.Next(100) == 0)
										{
											Dust* ptr9 = dustLocal.NewDust(i * 16, num8 * 16 - 4, 16, 16, 19, 0.0, 0.0, 160, default, 0.10000000149011612);
											if (ptr9 != null)
											{
												ptr9->Velocity.X *= 0.5f;
												ptr9->Velocity.Y *= 0.5f;
												ptr9->NoGravity = true;
												ptr9->FadeIn = 1f;
											}
										}
										break;
									case 1:
										if (Main.Rand.Next(100) == 0)
										{
											dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 41, 0.0, 0.0, 250, default, 0.800000011920929);
										}
										break;
									case 3:
										if (Main.Rand.Next(200) == 0)
										{
											Dust* ptr10 = dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 14, 0.0, 0.0, 100, default, 0.20000000298023224);
											if (ptr10 != null)
											{
												ptr10->FadeIn = 1.2f;
											}
										}
										if (Main.Rand.Next(75) == 0)
										{
											Dust* ptr11 = dustLocal.NewDust(i * 16, num8 * 16, 16, 16, 27, 0.0, 0.0, 100);
											if (ptr11 != null)
											{
												ptr11->Velocity.X *= 0.5f;
												ptr11->Velocity.Y *= 0.5f;
											}
										}
										break;
									case 4:
										if (Main.Rand.Next(150) == 0)
										{
											Dust* ptr12 = dustLocal.NewDust(i * 16, num8 * 16, 16, 8, 16);
											if (ptr12 != null)
											{
												ptr12->Velocity.X /= 3f;
												ptr12->Velocity.Y /= 3f;
												ptr12->Velocity.Y -= 0.7f;
												ptr12->Alpha = 50;
												ptr12->Scale *= 0.1f;
												ptr12->FadeIn = 0.9f;
												ptr12->NoGravity = true;
											}
										}
										break;
									case 5:
										if (Main.Rand.Next(40) == 0)
										{
											Dust* ptr8 = dustLocal.NewDust(i * 16, num8 * 16 - 6, 16, 16, 6, 0.0, 0.0, 0, default, 1.5);
											if (ptr8 != null)
											{
												ptr8->Velocity.Y -= 2f;
												ptr8->NoGravity = true;
											}
										}
										newColor.A = (byte)(UI.MouseTextBrightness >> 1);
										newColor.G = UI.MouseTextBrightness;
										newColor.B = UI.MouseTextBrightness;
										break;
									}
								}
							}
							pos.Y = num8 * 16 - ScreenPosition.Y - 1 + 32;
							s.Height = 20;
							SpriteSheet<_sheetTiles>.Draw(26 + num9, ref pos, ref s, newColor);
							continue;
						}
						if (num9 == 80)
						{
							bool flag = false;
							bool flag2 = false;
							int num17 = i;
							switch (ptr4->FrameX)
							{
							case 36:
								num17--;
								break;
							case 54:
								num17++;
								break;
							case 108:
								num17 = ((ptr4->FrameY != 16) ? (num17 + 1) : (num17 - 1));
								break;
							}
							int num18 = num8;
							bool flag3 = false;
							if (Main.TileSet[num17, num18].Type == 80 && Main.TileSet[num17, num18].IsActive != 0)
							{
								flag3 = true;
							}
							while (Main.TileSet[num17, num18].IsActive == 0 || !Main.TileSolid[Main.TileSet[num17, num18].Type] || !flag3)
							{
								if (Main.TileSet[num17, num18].Type == 80 && Main.TileSet[num17, num18].IsActive != 0)
								{
									flag3 = true;
								}
								num18++;
								if (num18 > num8 + 20)
								{
									break;
								}
							}
							if (Main.TileSet[num17, num18].Type == 112)
							{
								flag = true;
							}
							else if (Main.TileSet[num17, num18].Type == 116)
							{
								flag2 = true;
							}
							int id;
							if (flag)
							{
								id = 12;
							}
							else
							{
								id = ((!flag2) ? (26 + num9) : 13);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor);
							continue;
						}
						bool flag4 = Main.TileShine2[num9];
						int id2 = 26 + num9;
						if (SMOOTH_LIGHT && Main.TileSolid[num9] && num9 != 137)
						{
							if (newColor.R > num || (double)(int)newColor.G > (double)num * 1.1 || (double)(int)newColor.B > (double)num * 1.2)
							{
								s.Width = 4;
								s.Height = 4;
								Color newColor2 = Lighting.GetColorUnsafe(i - 1, num8 - 1);
								newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
								newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
								newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
								if (flag4)
								{
									shine(ref newColor2, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor2);
								pos.X += 4f;
								s.X += 4;
								s.Width = 8;
								newColor2 = Lighting.GetColorUnsafe(i, num8 - 1);
								newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
								newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
								newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
								if (flag4)
								{
									shine(ref newColor2, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor2);
								pos.X += 8f;
								s.X += 8;
								s.Width = 4;
								newColor2 = Lighting.GetColorUnsafe(i + 1, num8 - 1);
								newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
								newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
								newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
								if (flag4)
								{
									shine(ref newColor2, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor2);
								pos.Y += 4f;
								s.Y += 4;
								s.Height = 8;
								newColor2 = Lighting.GetColorUnsafe(i + 1, num8);
								newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
								newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
								newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
								if (flag4)
								{
									shine(ref newColor2, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor2);
								pos.X -= 12f;
								s.X -= 12;
								newColor2 = Lighting.GetColorUnsafe(i - 1, num8);
								newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
								newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
								newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
								if (flag4)
								{
									shine(ref newColor2, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor2);
								pos.Y += 8f;
								s.Y += 8;
								s.Height = 4;
								newColor2 = Lighting.GetColorUnsafe(i - 1, num8 + 1);
								newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
								newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
								newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
								if (flag4)
								{
									shine(ref newColor2, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor2);
								pos.X += 4f;
								s.X += 4;
								s.Width = 8;
								newColor2 = Lighting.GetColorUnsafe(i, num8 + 1);
								newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
								newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
								newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
								if (flag4)
								{
									shine(ref newColor2, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor2);
								pos.X += 8f;
								s.X += 8;
								s.Width = 4;
								newColor2 = Lighting.GetColorUnsafe(i + 1, num8 + 1);
								newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
								newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
								newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
								if (flag4)
								{
									shine(ref newColor2, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor2);
								pos.X -= 8f;
								pos.Y -= 8f;
								s.X -= 8;
								s.Y -= 8;
								s.Width = 8;
								s.Height = 8;
								if (flag4)
								{
									shine(ref newColor, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor);
							}
							else if (newColor.R > num2 || (double)(int)newColor.G > (double)num2 * 1.1 || (double)(int)newColor.B > (double)num2 * 1.2)
							{
								s.Width = 8;
								s.Height = 8;
								Color newColor3 = Lighting.GetColorUnsafe(i - 1, num8 - 1);
								newColor3.R = (byte)(newColor.R + newColor3.R >> 1);
								newColor3.G = (byte)(newColor.G + newColor3.G >> 1);
								newColor3.B = (byte)(newColor.B + newColor3.B >> 1);
								if (flag4)
								{
									shine(ref newColor3, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor3);
								pos.X += 8f;
								s.X += 8;
								newColor3 = Lighting.GetColorUnsafe(i + 1, num8 - 1);
								newColor3.R = (byte)(newColor.R + newColor3.R >> 1);
								newColor3.G = (byte)(newColor.G + newColor3.G >> 1);
								newColor3.B = (byte)(newColor.B + newColor3.B >> 1);
								if (flag4)
								{
									shine(ref newColor3, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor3);
								pos.Y += 8f;
								s.Y += 8;
								newColor3 = Lighting.GetColorUnsafe(i + 1, num8 + 1);
								newColor3.R = (byte)(newColor.R + newColor3.R >> 1);
								newColor3.G = (byte)(newColor.G + newColor3.G >> 1);
								newColor3.B = (byte)(newColor.B + newColor3.B >> 1);
								if (flag4)
								{
									shine(ref newColor3, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor3);
								pos.X -= 8f;
								s.X -= 8;
								newColor3 = Lighting.GetColorUnsafe(i - 1, num8 + 1);
								newColor3.R = (byte)(newColor.R + newColor3.R >> 1);
								newColor3.G = (byte)(newColor.G + newColor3.G >> 1);
								newColor3.B = (byte)(newColor.B + newColor3.B >> 1);
								if (flag4)
								{
									shine(ref newColor3, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor3);
							}
							else
							{
								if (flag4)
								{
									shine(ref newColor, num9);
								}
								SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor);
							}
							continue;
						}
						if (SMOOTH_LIGHT & flag4)
						{
							if (num9 == 21)
							{
								if (ptr4->FrameX >= 36 && ptr4->FrameX < 178)
								{
									shine(ref newColor, num9);
								}
							}
							else
							{
								shine(ref newColor, num9);
							}
						}
						SpriteSheet<_sheetTiles>.Draw(id2, ref pos, ref s, newColor);
						switch ((EntityID.TileID)num9)
						{
						case EntityID.TileID.MUSIC_BOX:
							newColor.R = 200;
							newColor.G = 200;
							newColor.B = 200;
							newColor.A = 0;
							SpriteSheet<_sheetTiles>.Draw(17, ref pos, ref s, newColor);
							break;
						case EntityID.TileID.TIMER:
							newColor.R = 200;
							newColor.G = 200;
							newColor.B = 200;
							newColor.A = 0;
							SpriteSheet<_sheetTiles>.Draw(176, ref pos, ref s, newColor);
							break;
						}
					}
				}
				while (++num8 < num7);
			}
			while (++i < num6);
			if (!Player.IsDead)
			{
				int num19 = (Player.XYWH.X + 10 >> 4) - 10;
				int num20 = (Player.XYWH.Y + 21 >> 4) - 8;
				for (i = -3; i < 23; i++)
				{
					Tile* ptr13 = tileSet + ((num19 + i) * 1440 + num20 - 4);
					for (int k = -4; k < 20; k++)
					{
						Tile.Flags* ptr14 = &ptr13->CurrentFlags;
						*ptr14 &= ~Tile.Flags.HIGHLIGHT_MASK;
						ptr13++;
					}
				}
			}
		}
		Vector2 pos2 = default;
		for (int l = 0; l < num3; l++)
		{
			int x = spec[l].X;
			int y = spec[l].Y;
			Color tileColor = spec[l].tileColor;
			pos2.X = 32 + x * 16 - ScreenPosition.X;
			pos2.Y = 32 + y * 16 - ScreenPosition.Y;
			fixed (Tile* ptr15 = &Main.TileSet[x, y])
			{
				if (ptr15->Type == 128)
				{
					int num21 = ptr15->FrameY / 18;
					short frameX = ptr15->FrameX;
					int num22 = frameX / 100;
					int num23 = frameX % 100;
					SpriteEffects se = SpriteEffects.FlipHorizontally;
					if (num23 >= 36)
					{
						se = SpriteEffects.None;
					}
					pos2.X += -4f;
					pos2.Y += -12 - (num21 << 4);
					int sh = 54;
					switch (num21)
					{
					case 0:
						num22 += 60;
						sh = 36;
						break;
					case 1:
						num22 += 32;
						break;
					default:
						num22 += 107;
						break;
					}
					SpriteSheet<_sheetSprites>.Draw(num22, ref pos2, sh, tileColor, se);
					continue;
				}
				Rectangle s2 = default;
				if (ptr15->FrameX == 22)
				{
					int num24 = 0;
					if (ptr15->FrameY == 220)
					{
						num24 = 1;
					}
					else if (ptr15->FrameY == 242)
					{
						num24 = 2;
					}
					int num25 = 0;
					s2.Width = 80;
					s2.Height = 80;
					int num26 = 32;
					int num27 = ((y + 100 >= Main.MaxTilesY) ? (Main.MaxTilesY - y) : 100);
					for (int m = 0; m < num27; m++)
					{
						switch ((EntityID.TileID)ptr15[m].Type)
						{
						case EntityID.TileID.GRASS:
							num25 = 0;
							break;
						case EntityID.TileID.CORRUPT_GRASS:
							num25 = 1;
							break;
						case EntityID.TileID.JUNGLE_GRASS:
							num25 = 2;
							s2.Width = 114;
							s2.Height = 96;
							num26 = 48;
							break;
						case EntityID.TileID.SNOW:
							num25 = 4;
							break;
						case EntityID.TileID.HALLOWED_GRASS:
							num25 = 3;
							num24 += x % 3 * 3;
							s2.Height = 140;
							break;
						default:
							continue;
						}
						break;
					}
					s2.X = num24 * (s2.Width + 2);
					pos2.X -= num26;
					pos2.Y -= s2.Height - 16;
					SpriteSheet<_sheetTiles>.Draw(182 + num25, ref pos2, ref s2, tileColor);
				}
				else if (ptr15->FrameX == 44)
				{
					s2.Width = 40;
					s2.Height = 40;
					if (ptr15->FrameY == 220)
					{
						s2.Y = 42;
					}
					else if (ptr15->FrameY == 242)
					{
						s2.Y = 84;
					}
					int num28 = 0;
					int num29 = ((y + 100 >= Main.MaxTilesY) ? (Main.MaxTilesY - y) : 100);
					for (int n = 0; n < num29; n++)
					{
						switch ((EntityID.TileID)ptr15[n + 1440].Type)
						{
						case EntityID.TileID.GRASS:
							num28 = 0;
							break;
						case EntityID.TileID.CORRUPT_GRASS:
							num28 = 1;
							break;
						case EntityID.TileID.JUNGLE_GRASS:
							num28 = 2;
							break;
						case EntityID.TileID.SNOW:
							num28 = 4;
							break;
						case EntityID.TileID.HALLOWED_GRASS:
							num28 = 3;
							s2.Y += x % 3 * 126;
							break;
						default:
							continue;
						}
						break;
					}
					pos2.X -= 24f;
					pos2.Y -= 12f;
					SpriteSheet<_sheetTiles>.Draw(177 + num28, ref pos2, ref s2, tileColor);
				}
				else
				{
					if (ptr15->FrameX != 66)
					{
						continue;
					}
					s2.X = 42;
					s2.Width = 40;
					s2.Height = 40;
					if (ptr15->FrameY == 220)
					{
						s2.Y = 42;
					}
					else if (ptr15->FrameY == 242)
					{
						s2.Y = 84;
					}
					int num30 = 0;
					int num31 = ((y + 100 >= Main.MaxTilesY) ? (Main.MaxTilesY - y) : 100);
					for (int num32 = 0; num32 < num31; num32++)
					{
						switch ((EntityID.TileID)ptr15[num32 - 1440].Type)
						{
						case EntityID.TileID.GRASS:
							num30 = 0;
							break;
						case EntityID.TileID.CORRUPT_GRASS:
							num30 = 1;
							break;
						case EntityID.TileID.JUNGLE_GRASS:
							num30 = 2;
							break;
						case EntityID.TileID.SNOW:
							num30 = 4;
							break;
						case EntityID.TileID.HALLOWED_GRASS:
							num30 = 3;
							s2.Y += x % 3 * 126;
							break;
						default:
							continue;
						}
						break;
					}
					pos2.Y -= 12f;
					SpriteSheet<_sheetTiles>.Draw(177 + num30, ref pos2, ref s2, tileColor);
					continue;
				}
			}
		}
		Main.TileSolid[10] = true;
	}

	private unsafe void DrawSolidTiles()
	{
		int num = 198;
		int num2 = 38;
		Main.TileSolid[10] = false;
		Rectangle s = default;
		Vector2 pos = default;
		int lastTileX = LastTileX;
		int lastTileY = LastTileY;
		int num3 = FirstTileX;
		fixed (Tile* tileSet = Main.TileSet)
		{
			do
			{
				int num4 = FirstTileY;
				Tile* ptr = tileSet + (num3 * 1440 + num4 - 1);
				do
				{
					ptr++;
					if (ptr->IsActive == 0)
					{
						continue;
					}
					int type = ptr->Type;
					if (!Main.TileSolid[type])
					{
						continue;
					}
					s.X = ptr->FrameX;
					s.Y = ptr->FrameY;
					s.Width = 16;
					s.Height = ((type == 137 || type == 138) ? 18 : 16);
					pos.X = num3 * 16 - ScreenPosition.X + 32;
					pos.Y = num4 * 16 - ScreenPosition.Y + 32;
					Color newColor = Lighting.GetColorUnsafe(num3, num4);
					if (Player.findTreasure)
					{
						switch ((EntityID.TileID)type)
						{
						case EntityID.TileID.IRON_ORE:
						case EntityID.TileID.COPPER_ORE:
						case EntityID.TileID.GOLD_ORE:
						case EntityID.TileID.SILVER_ORE:
						case EntityID.TileID.DEMONITE_ORE:
						case EntityID.TileID.SAPPHIRE:
						case EntityID.TileID.RUBY:
						case EntityID.TileID.EMERALD:
						case EntityID.TileID.TOPAZ:
						case EntityID.TileID.AMETHYST:
						case EntityID.TileID.DIAMOND:
						case EntityID.TileID.COBALT_ORE:
						case EntityID.TileID.MYTHRIL_ORE:
						case EntityID.TileID.ADAMANTITE_ORE:
							if (newColor.R < UI.MouseTextBrightness >> 1)
							{
								newColor.R = (byte)(UI.MouseTextBrightness >> 1);
							}
							if (newColor.G < 70)
							{
								newColor.G = 70;
							}
							if (newColor.B < 210)
							{
								newColor.B = 210;
							}
							newColor.A = UI.MouseTextBrightness;
							if (Main.Rand.Next(150) == 0)
							{
								Dust* ptr2 = dustLocal.NewDust(num3 * 16, num4 * 16, 16, 16, 15, 0.0, 0.0, 150, default, 0.800000011920929);
								if (ptr2 != null)
								{
									ptr2->Velocity.X *= 0.1f;
									ptr2->Velocity.Y *= 0.1f;
									ptr2->NoLight = true;
								}
							}
							break;
						}
					}
					switch ((EntityID.TileID)type)
					{
					case EntityID.TileID.DEMONITE_ORE:
						if (Main.Rand.Next(400) == 0)
						{
							dustLocal.NewDust(num3 * 16, num4 * 16, 16, 16, 14);
						}
						goto default;
					case EntityID.TileID.EBONSTONE:
						if (Main.Rand.Next(700) == 0)
						{
							dustLocal.NewDust(num3 * 16, num4 * 16, 16, 16, 14);
						}
						break;
					case EntityID.TileID.CORRUPT_GRASS:
						if (Main.Rand.Next(500) == 0)
						{
							dustLocal.NewDust(num3 * 16, num4 * 16, 16, 16, 14);
						}
						break;
					case EntityID.TileID.METEORITE:
						if (Main.Rand.Next(250) == 0)
						{
							Dust* ptr5 = dustLocal.NewDust(num3 * 16, num4 * 16, 16, 16, 6, 0.0, 0.0, 0, default, Main.Rand.Next(3));
							if (ptr5 != null && ptr5->Scale > 1f)
							{
								ptr5->NoGravity = true;
							}
						}
						break;
					case EntityID.TileID.HELLSTONE:
					case EntityID.TileID.HELLSTONE_BRICK:
						if (Main.Rand.Next(250) == 0)
						{
							Dust* ptr3 = dustLocal.NewDust(num3 * 16, num4 * 16, 16, 16, 6, 0.0, 0.0, 0, default, Main.Rand.Next(3));
							if (ptr3 != null && ptr3->Scale > 1f)
							{
								ptr3->NoGravity = true;
								ptr3->NoLight = true;
							}
						}
						break;
					case EntityID.TileID.EBONSAND:
						if (Main.Rand.Next(700) == 0)
						{
							dustLocal.NewDust(num3 * 16, num4 * 16, 16, 16, 14);
						}
						break;
					default:
					{
						if (Main.TileShine[type] <= 0 || (newColor.R <= 20 && newColor.B <= 20 && newColor.G <= 20))
						{
							break;
						}
						int num5 = newColor.R;
						if (newColor.G > num5)
						{
							num5 = newColor.G;
						}
						if (newColor.B > num5)
						{
							num5 = newColor.B;
						}
						num5 /= 30;
						if (Main.Rand.Next(Main.TileShine[type]) < num5)
						{
							Dust* ptr4 = dustLocal.NewDust(num3 * 16, num4 * 16, 16, 16, 43, 0.0, 0.0, 254, default, 0.5);
							if (ptr4 != null)
							{
								ptr4->Velocity.X = 0f;
								ptr4->Velocity.Y = 0f;
							}
						}
						break;
					}
					}
					if (newColor.R <= 1 && newColor.G <= 1 && newColor.B <= 1)
					{
						continue;
					}
					if (!Main.TileSolidTop[type] && (ptr[-1].Liquid > 0 || ptr[1].Liquid > 0 || ptr[-1440].Liquid > 0 || ptr[1440].Liquid > 0))
					{
						int num6 = 0;
						bool flag = false;
						bool flag2 = false;
						bool flag3 = false;
						bool flag4 = false;
						int num7 = 0;
						bool flag5 = false;
						if (ptr[-1440].Liquid > num6)
						{
							num6 = ptr[-1440].Liquid;
							flag = true;
						}
						else if (ptr[-1440].Liquid > 0)
						{
							flag = true;
						}
						if (ptr[1440].Liquid > num6)
						{
							num6 = ptr[1440].Liquid;
							flag2 = true;
						}
						else if (ptr[1440].Liquid > 0)
						{
							num6 = ptr[1440].Liquid;
							flag2 = true;
						}
						if (ptr[-1].Liquid > 0)
						{
							flag3 = true;
						}
						if (ptr[1].Liquid > 240)
						{
							flag4 = true;
						}
						if (ptr[-1440].Liquid > 0)
						{
							if (ptr[-1440].Lava != 0)
							{
								num7 = 1;
							}
							else
							{
								flag5 = true;
							}
						}
						if (ptr[1440].Liquid > 0)
						{
							if (ptr[1440].Lava != 0)
							{
								num7 = 1;
							}
							else
							{
								flag5 = true;
							}
						}
						if (ptr[-1].Liquid > 0)
						{
							if (ptr[-1].Lava != 0)
							{
								num7 = 1;
							}
							else
							{
								flag5 = true;
							}
						}
						if (ptr[1].Liquid > 0)
						{
							if (ptr[1].Lava != 0)
							{
								num7 = 1;
							}
							else
							{
								flag5 = true;
							}
						}
						if (!flag5 || num7 != 1)
						{
							Vector2 pos2 = new Vector2(num3 * 16, num4 * 16);
							Rectangle s2 = new Rectangle(0, 4, 16, 16);
							if (flag4 && (flag | flag2))
							{
								flag = true;
								flag2 = true;
							}
							if ((!flag3 || (!flag && !flag2)) && (!flag4 || !flag3))
							{
								if (flag3)
								{
									s2.Height = 4;
								}
								else if (flag4 && !flag && !flag2)
								{
									pos2.Y += 12f;
									s2.Height = 4;
								}
								else
								{
									int num8 = 256 - num6 >> 4;
									pos2.Y += num8;
									s2.Height -= num8;
									if (!flag || !flag2)
									{
										s2.Width = 4;
										if (!flag)
										{
											pos2.X += 12f;
										}
									}
								}
							}
							Color c = newColor;
							if (num4 >= Main.WorldSurface)
							{
								c.R >>= 1;
								c.G >>= 1;
								c.B >>= 1;
								c.A >>= 1;
								if (num7 == 1)
								{
									c.R += (byte)(c.R >> 1);
									c.G += (byte)(c.G >> 1);
									c.B += (byte)(c.B >> 1);
									c.A += (byte)(c.A >> 1);
								}
							}
							pos2.X -= ScreenPosition.X;
							pos2.X += 32f;
							pos2.Y -= ScreenPosition.Y;
							pos2.Y += 32f;
							SpriteSheet<_sheetTiles>.Draw(14 + num7, ref pos2, ref s2, c);
						}
					}
					bool flag6 = Main.TileShine2[type];
					int id = 26 + type;
					if (SMOOTH_LIGHT && type != 11 && type != 137)
					{
						if (newColor.R > num || (double)(int)newColor.G > (double)num * 1.1 || (double)(int)newColor.B > (double)num * 1.2)
						{
							s.Width = 4;
							s.Height = 4;
							Color newColor2 = Lighting.GetColorUnsafe(num3 - 1, num4 - 1);
							newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
							newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
							newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
							if (flag6)
							{
								shine(ref newColor2, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor2);
							pos.X += 4f;
							s.X += 4;
							s.Width = 8;
							newColor2 = Lighting.GetColorUnsafe(num3, num4 - 1);
							newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
							newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
							newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
							if (flag6)
							{
								shine(ref newColor2, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor2);
							pos.X += 8f;
							s.X += 8;
							s.Width = 4;
							newColor2 = Lighting.GetColorUnsafe(num3 + 1, num4 - 1);
							newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
							newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
							newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
							if (flag6)
							{
								shine(ref newColor2, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor2);
							pos.Y += 4f;
							s.Y += 4;
							s.Height = 8;
							newColor2 = Lighting.GetColorUnsafe(num3 + 1, num4);
							newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
							newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
							newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
							if (flag6)
							{
								shine(ref newColor2, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor2);
							pos.X -= 12f;
							s.X -= 12;
							newColor2 = Lighting.GetColorUnsafe(num3 - 1, num4);
							newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
							newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
							newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
							if (flag6)
							{
								shine(ref newColor2, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor2);
							pos.Y += 8f;
							s.Y += 8;
							s.Height = 4;
							newColor2 = Lighting.GetColorUnsafe(num3 - 1, num4 + 1);
							newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
							newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
							newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
							if (flag6)
							{
								shine(ref newColor2, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor2);
							pos.X += 4f;
							s.X += 4;
							s.Width = 8;
							newColor2 = Lighting.GetColorUnsafe(num3, num4 + 1);
							newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
							newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
							newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
							if (flag6)
							{
								shine(ref newColor2, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor2);
							pos.X += 8f;
							s.X += 8;
							s.Width = 4;
							newColor2 = Lighting.GetColorUnsafe(num3 + 1, num4 + 1);
							newColor2.R = (byte)(newColor.R + newColor2.R >> 1);
							newColor2.G = (byte)(newColor.G + newColor2.G >> 1);
							newColor2.B = (byte)(newColor.B + newColor2.B >> 1);
							if (flag6)
							{
								shine(ref newColor2, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor2);
							pos.X -= 8f;
							pos.Y -= 8f;
							s.X -= 8;
							s.Y -= 8;
							s.Width = 8;
							s.Height = 8;
							if (flag6)
							{
								shine(ref newColor, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor);
						}
						else if (newColor.R > num2 || (double)(int)newColor.G > (double)num2 * 1.1 || (double)(int)newColor.B > (double)num2 * 1.2)
						{
							s.Width = 8;
							s.Height = 8;
							Color newColor3 = Lighting.GetColorUnsafe(num3 - 1, num4 - 1);
							newColor3.R = (byte)(newColor.R + newColor3.R >> 1);
							newColor3.G = (byte)(newColor.G + newColor3.G >> 1);
							newColor3.B = (byte)(newColor.B + newColor3.B >> 1);
							if (flag6)
							{
								shine(ref newColor3, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor3);
							pos.X += 8f;
							s.X += 8;
							newColor3 = Lighting.GetColorUnsafe(num3 + 1, num4 - 1);
							newColor3.R = (byte)(newColor.R + newColor3.R >> 1);
							newColor3.G = (byte)(newColor.G + newColor3.G >> 1);
							newColor3.B = (byte)(newColor.B + newColor3.B >> 1);
							if (flag6)
							{
								shine(ref newColor3, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor3);
							pos.Y += 8f;
							s.Y += 8;
							newColor3 = Lighting.GetColorUnsafe(num3 + 1, num4 + 1);
							newColor3.R = (byte)(newColor.R + newColor3.R >> 1);
							newColor3.G = (byte)(newColor.G + newColor3.G >> 1);
							newColor3.B = (byte)(newColor.B + newColor3.B >> 1);
							if (flag6)
							{
								shine(ref newColor3, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor3);
							pos.X -= 8f;
							s.X -= 8;
							newColor3 = Lighting.GetColorUnsafe(num3 - 1, num4 + 1);
							newColor3.R = (byte)(newColor.R + newColor3.R >> 1);
							newColor3.G = (byte)(newColor.G + newColor3.G >> 1);
							newColor3.B = (byte)(newColor.B + newColor3.B >> 1);
							if (flag6)
							{
								shine(ref newColor3, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor3);
						}
						else
						{
							if (flag6)
							{
								shine(ref newColor, type);
							}
							SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor);
						}
					}
					else
					{
						if (SMOOTH_LIGHT & flag6)
						{
							shine(ref newColor, type);
						}
						SpriteSheet<_sheetTiles>.Draw(id, ref pos, ref s, newColor);
					}
				}
				while (++num4 < lastTileY);
			}
			while (++num3 < lastTileX);
		}
		Main.TileSolid[10] = true;
	}

	private unsafe void DrawWater(bool bg = false)
	{
		int num = 201;
		int num2 = 226;
		int num3 = EvilTiles / 7;
		if (num3 > 50)
		{
			num3 = 50;
		}
		int num4 = 256 - num3;
		int num5 = 256 - (num3 << 1);
		int num6 = FirstTileX;
		int num7 = LastTileX;
		int num8 = FirstTileY;
		int num9 = LastTileY;
		if (num6 < 5)
		{
			num6 = 5;
		}
		if (num8 < 5)
		{
			num8 = 5;
		}
		if (num7 > Main.MaxTilesX - 5)
		{
			num7 = Main.MaxTilesX - 5;
		}
		if (num9 > Main.MaxTilesY - 5)
		{
			num9 = Main.MaxTilesY - 5;
		}
		Vector2 vector = default;
		Vector2 vector2 = default;
		Rectangle s = default;
		fixed (Tile* tileSet = Main.TileSet)
		{
			do
			{
				int num10 = num8;
				Tile* ptr = tileSet + (num6 * 1440 + num10);
				vector2.X = num6 << 4;
				do
				{
					int liquid = ptr->Liquid;
					if (liquid > 0 && (bg || ptr->IsActive == 0 || !Main.TileSolidNotSolidTop[ptr->Type]) && (bg || Lighting.IsNotBlackUnsafe(num6, num10)))
					{
						Color colorUnsafe = Lighting.GetColorUnsafe(num6, num10);
						int num11 = ((ptr->Lava == 0) ? 14 : 15);
						int num12;
						if (bg)
						{
							num12 = 255;
						}
						else
						{
							num12 = ((num11 == 15) ? 230 : 128);
						}
						vector2.Y = num10 << 4;
						s.Width = 16;
						ptr--;
						if (ptr->Liquid == 0)
						{
							s.Y = 0;
							s.Height = Math.Max(1, liquid >> 4);
							vector2.Y += 16 - s.Height;
						}
						else
						{
							s.Y = 4;
							s.Height = 16;
						}
						ptr++;
						if (num11 == 15 && dustLocal.LavaBubbles < 128 && Main.HasFocus)
						{
							if (liquid > 200 && Main.Rand.Next(700) == 0)
							{
								dustLocal.NewDust(num6 << 4, num10 << 4, 16, 16, 35);
							}
							else if (s.Y == 0 && Main.Rand.Next(350) == 0)
							{
								Dust* ptr2 = dustLocal.NewDust(num6 << 4, (num10 << 4) + (liquid >> 4) - 8, 16, 8, 35, 0.0, 0.0, 50, default, 1.5);
								if (ptr2 != null)
								{
									ptr2->Velocity.X *= 1.6f;
									ptr2->Velocity.Y *= 0.8f;
									ptr2->Velocity.Y -= (float)Main.Rand.Next(1, 7) * 0.1f;
									if (Main.Rand.Next(10) == 0)
									{
										ptr2->Velocity.Y *= Main.Rand.Next(2, 5);
									}
									ptr2->NoGravity = true;
								}
							}
						}
						colorUnsafe.R = (byte)(colorUnsafe.R * num12 >> 8);
						colorUnsafe.G = (byte)(colorUnsafe.G * num12 >> 8);
						colorUnsafe.B = (byte)(colorUnsafe.B * num12 >> 8);
						colorUnsafe.A = (byte)num12;
						if (num11 == 14)
						{
							colorUnsafe.B = (byte)(colorUnsafe.B * num5 >> 8);
						}
						else
						{
							colorUnsafe.R = (byte)(colorUnsafe.R * num4 >> 8);
						}
						if (SMOOTH_LIGHT && !bg)
						{
							Color color = colorUnsafe;
							if ((num11 == 14 && (color.R > num || (double)(int)color.G > (double)num * 1.1 || (double)(int)color.B > (double)num * 1.2)) || color.R > num2 || (double)(int)color.G > (double)num2 * 1.1 || (double)(int)color.B > (double)num2 * 1.2)
							{
								for (int i = 0; i < 4; i++)
								{
									int num13 = 0;
									int num14 = 0;
									int width = 8;
									int height = 8;
									Color colorUnsafe2 = Lighting.GetColorUnsafe(num6, num10);
									switch (i)
									{
									case 0:
										if (Lighting.Brighter(num6, num10 - 1, num6 - 1, num10))
										{
											ptr -= 1440;
											if (ptr->IsActive == 0)
											{
												colorUnsafe2 = Lighting.GetColorUnsafe(num6 - 1, num10);
											}
											ptr += 1440;
										}
										else
										{
											ptr--;
											if (ptr->IsActive == 0)
											{
												colorUnsafe2 = Lighting.GetColorUnsafe(num6, num10 - 1);
											}
											ptr++;
										}
										if (s.Height < 8)
										{
											height = s.Height;
										}
										break;
									case 1:
										if (Lighting.Brighter(num6, num10 - 1, num6 + 1, num10))
										{
											if (ptr[1440].IsActive == 0)
											{
												colorUnsafe2 = Lighting.GetColorUnsafe(num6 + 1, num10);
											}
										}
										else
										{
											ptr--;
											if (ptr->IsActive == 0)
											{
												colorUnsafe2 = Lighting.GetColorUnsafe(num6, num10 - 1);
											}
											ptr++;
										}
										num13 = 8;
										if (s.Height < 8)
										{
											height = s.Height;
										}
										break;
									case 2:
										if (Lighting.Brighter(num6, num10 + 1, num6 - 1, num10))
										{
											ptr -= 1440;
											if (ptr->IsActive == 0)
											{
												colorUnsafe2 = Lighting.GetColorUnsafe(num6 - 1, num10);
											}
											ptr += 1440;
										}
										else
										{
											ptr++;
											if (ptr->IsActive == 0)
											{
												colorUnsafe2 = Lighting.GetColorUnsafe(num6, num10 + 1);
											}
											ptr--;
										}
										num14 = 8;
										height = 8 - (16 - s.Height);
										break;
									default:
										if (Lighting.Brighter(num6, num10 + 1, num6 + 1, num10))
										{
											if (ptr[1440].IsActive == 0)
											{
												colorUnsafe2 = Lighting.GetColorUnsafe(num6 + 1, num10);
											}
										}
										else
										{
											ptr++;
											if (ptr->IsActive == 0)
											{
												colorUnsafe2 = Lighting.GetColorUnsafe(num6, num10 + 1);
											}
											ptr--;
										}
										num13 = 8;
										num14 = 8;
										height = 8 - (16 - s.Height);
										break;
									}
									colorUnsafe2.R = (byte)(colorUnsafe2.R * num12 + color.R >> 9);
									colorUnsafe2.G = (byte)(colorUnsafe2.G * num12 + color.G >> 9);
									colorUnsafe2.B = (byte)(colorUnsafe2.B * num12 + color.B >> 9);
									colorUnsafe2.A = (byte)num12;
									vector = vector2;
									vector.X -= ScreenPosition.X - num13 - 32;
									vector.Y -= ScreenPosition.Y - num14 - 32;
									Rectangle s2 = s;
									s2.X += num13;
									s2.Y += num14;
									s2.Width = width;
									s2.Height = height;
									SpriteSheet<_sheetTiles>.Draw(num11, ref vector, ref s2, colorUnsafe2);
								}
							}
							else
							{
								vector = vector2;
								vector.X -= ScreenPosition.X - 32;
								vector.Y -= ScreenPosition.Y - 32;
								SpriteSheet<_sheetTiles>.Draw(num11, ref vector, ref s, colorUnsafe);
							}
						}
						else
						{
							vector = vector2;
							vector.X -= ScreenPosition.X - 32;
							vector.Y -= ScreenPosition.Y - 32;
							SpriteSheet<_sheetTiles>.Draw(num11, ref vector, ref s, colorUnsafe);
						}
					}
					ptr++;
				}
				while (++num10 < num9);
			}
			while (++num6 < num7);
		}
	}

	private void DrawGore()
	{
		Vector2 pivot = default;
		Vector2 pos = default;
		for (int i = 0; i < 128; i++)
		{
			if (Main.GoreSet[i].Active != 0)
			{
				int num = 256 + Main.GoreSet[i].Type;
				pivot.X = SpriteSheet<_sheetSprites>.Source[num].Width >> 1;
				pivot.Y = SpriteSheet<_sheetSprites>.Source[num].Height >> 1;
				pos.X = Main.GoreSet[i].Position.X + pivot.X;
				pos.Y = Main.GoreSet[i].Position.Y + pivot.Y;
				Color alpha = Main.GoreSet[i].GetAlpha(Lighting.GetColor((int)pos.X >> 4, (int)pos.Y >> 4));
				pos.X -= ScreenPosition.X;
				pos.Y -= ScreenPosition.Y;
				SpriteSheet<_sheetSprites>.Draw(num, ref pos, alpha, Main.GoreSet[i].Rotation, ref pivot, Main.GoreSet[i].Scale);
			}
		}
	}

	private unsafe void DrawNPCs(bool behindTiles = false)
	{
		bool flag = false;
		Rectangle rectangle = new Rectangle
		{
			X = ScreenPosition.X - 300,
			Y = ScreenPosition.Y - 300,
			Width = ViewWidth + 600,
			Height = Main.ResolutionHeight + 600
		};
		Vector2 pos = default;
		Color c = default;
		for (int num = 195; num >= 0; num--)
		{
			int type = Main.NPCSet[num].Type;
			if (type <= 0 || Main.NPCSet[num].Active == 0 || Main.NPCSet[num].IsBehindTiles != behindTiles)
			{
				continue;
			}
			if ((type == 125 || type == 126) && !flag)
			{
				flag = true;
				for (int i = 0; i < 196; i++)
				{
					if (Main.NPCSet[i].Active == 0 || num == i || (Main.NPCSet[i].Type != 125 && Main.NPCSet[i].Type != 126))
					{
						continue;
					}
					float num2 = Main.NPCSet[i].Position.X + (float)(Main.NPCSet[i].Width >> 1);
					float num3 = Main.NPCSet[i].Position.Y + (float)(Main.NPCSet[i].Height >> 1);
					Vector2 vector = new Vector2(Main.NPCSet[num].Position.X + (float)(Main.NPCSet[num].Width >> 1), Main.NPCSet[num].Position.Y + (float)(Main.NPCSet[num].Height >> 1));
					float num4 = num2 - vector.X;
					float num5 = num3 - vector.Y;
					float rotCenter = (float)Math.Atan2(num5, num4) - 1.57f;
					bool flag2 = true;
					float num6 = num4 * num4 + num5 * num5;
					if (num6 > 4000000f)
					{
						flag2 = false;
					}
					while (flag2)
					{
						num6 = num4 * num4 + num5 * num5;
						if (num6 < 1600f)
						{
							flag2 = false;
							continue;
						}
						float num7 = (float)SpriteSheet<_sheetSprites>.Source[197].Height / (float)Math.Sqrt(num6);
						num4 *= num7;
						num5 *= num7;
						vector.X += num4;
						vector.Y += num5;
						num4 = num2 - vector.X;
						num5 = num3 - vector.Y;
						pos = vector;
						pos.X -= ScreenPosition.X;
						pos.Y -= ScreenPosition.Y;
						SpriteSheet<_sheetSprites>.DrawRotated(197, ref pos, Lighting.GetColor((int)vector.X >> 4, (int)vector.Y >> 4), rotCenter);
					}
				}
			}
			if (!rectangle.Intersects(Main.NPCSet[num].XYWH))
			{
				continue;
			}
			if (type == 101)
			{
				bool flag3 = true;
				Vector2 vector2 = new Vector2(Main.NPCSet[num].Position.X + (float)(Main.NPCSet[num].Width >> 1), Main.NPCSet[num].Position.Y + (float)(Main.NPCSet[num].Height >> 1));
				float num8 = Main.NPCSet[num].AI0 * 16f + 8f - vector2.X;
				float num9 = Main.NPCSet[num].AI1 * 16f + 8f - vector2.Y;
				float rot = (float)Math.Atan2(num9, num8) - 1.57f;
				bool flag4 = true;
				do
				{
					float num10 = 0.75f;
					int sh = 28;
					float num11 = (float)Math.Sqrt(num8 * num8 + num9 * num9);
					if (num11 < 28f * num10)
					{
						sh = (int)num11 - 40 + 28;
						flag4 = false;
					}
					num11 = 20f * num10 / num11;
					num8 *= num11;
					num9 *= num11;
					vector2.X += num8;
					vector2.Y += num9;
					num8 = Main.NPCSet[num].AI0 * 16f + 8f - vector2.X;
					num9 = Main.NPCSet[num].AI1 * 16f + 8f - vector2.Y;
					pos = vector2;
					pos.X -= ScreenPosition.X;
					pos.Y -= ScreenPosition.Y;
					SpriteSheet<_sheetSprites>.Draw(flag3 ? 196 : 195, ref pos, sh, Lighting.GetColor((int)vector2.X >> 4, (int)vector2.Y >> 4), rot, num10);
					flag3 = !flag3;
				}
				while (flag4);
			}
			else if (Main.NPCSet[num].AIStyle == 13)
			{
				Vector2 vector3 = new Vector2(Main.NPCSet[num].Position.X + (float)(Main.NPCSet[num].Width >> 1), Main.NPCSet[num].Position.Y + (float)(Main.NPCSet[num].Height >> 1));
				float num12 = Main.NPCSet[num].AI0 * 16f + 8f - vector3.X;
				float num13 = Main.NPCSet[num].AI1 * 16f + 8f - vector3.Y;
				float rotCenter2 = (float)Math.Atan2(num13, num12) - 1.57f;
				bool flag5 = true;
				do
				{
					float num14 = (float)Math.Sqrt(num12 * num12 + num13 * num13);
					if (num14 < 40f)
					{
						flag5 = false;
					}
					num14 = 28f / num14;
					num12 *= num14;
					num13 *= num14;
					vector3.X += num12;
					vector3.Y += num13;
					num12 = Main.NPCSet[num].AI0 * 16f + 8f - vector3.X;
					num13 = Main.NPCSet[num].AI1 * 16f + 8f - vector3.Y;
					pos = vector3;
					pos.X -= ScreenPosition.X;
					pos.Y -= ScreenPosition.Y;
					SpriteSheet<_sheetSprites>.DrawRotated((type == 56) ? 190 : 189, ref pos, Lighting.GetColor((int)vector3.X >> 4, (int)vector3.Y >> 4), rotCenter2);
				}
				while (flag5);
			}
			if (type == 36)
			{
				Vector2 vector4 = new Vector2(Main.NPCSet[num].Position.X + (float)(Main.NPCSet[num].Width >> 1) - 5f * Main.NPCSet[num].AI0, Main.NPCSet[num].Position.Y + 20f);
				for (int j = 0; j < 2; j++)
				{
					float num15 = Main.NPCSet[(int)Main.NPCSet[num].AI1].Position.X + (float)(Main.NPCSet[(int)Main.NPCSet[num].AI1].Width >> 1) - vector4.X;
					float num16 = Main.NPCSet[(int)Main.NPCSet[num].AI1].Position.Y + (float)(Main.NPCSet[(int)Main.NPCSet[num].AI1].Height >> 1) - vector4.Y;
					float num17;
					if (j == 0)
					{
						num15 -= 200f * Main.NPCSet[num].AI0;
						num16 += 130f;
						num17 = (float)Math.Sqrt(num15 * num15 + num16 * num16);
						num17 = 92f / num17;
					}
					else
					{
						num15 -= 50f * Main.NPCSet[num].AI0;
						num16 += 80f;
						num17 = (float)Math.Sqrt(num15 * num15 + num16 * num16);
						num17 = 60f / num17;
					}
					vector4.X += num15 * num17;
					vector4.Y += num16 * num17;
					pos.X = vector4.X - (float)ScreenPosition.X;
					pos.Y = vector4.Y - (float)ScreenPosition.Y;
					SpriteSheet<_sheetSprites>.DrawRotated(3, ref pos, Lighting.GetColor((int)vector4.X >> 4, (int)vector4.Y >> 4), (float)Math.Atan2(num16, num15) - 1.57f);
					if (j == 0)
					{
						vector4.X += num15 * num17 * 0.5f;
						vector4.Y += num16 * num17 * 0.5f;
					}
					else if (Main.HasFocus)
					{
						vector4.X += num15 * num17 - 16f;
						vector4.Y += num16 * num17 - 6f;
						Dust* ptr = dustLocal.NewDust((int)vector4.X, (int)vector4.Y, 30, 10, 5, num15 * 0.02f, num16 * 0.02f, 0, default, 2.0);
						if (ptr != null)
						{
							ptr->NoGravity = true;
						}
					}
				}
			}
			if (Main.NPCSet[num].AIStyle >= 33 && Main.NPCSet[num].AIStyle <= 36)
			{
				Vector2 vector5 = new Vector2(Main.NPCSet[num].Position.X + (float)(Main.NPCSet[num].Width >> 1) - 5f * Main.NPCSet[num].AI0, Main.NPCSet[num].Position.Y + 20f);
				for (int k = 0; k < 2; k++)
				{
					float num18 = Main.NPCSet[(int)Main.NPCSet[num].AI1].Position.X + (float)(Main.NPCSet[(int)Main.NPCSet[num].AI1].Width >> 1) - vector5.X;
					float num19 = Main.NPCSet[(int)Main.NPCSet[num].AI1].Position.Y + (float)(Main.NPCSet[(int)Main.NPCSet[num].AI1].Height >> 1) - vector5.Y;
					float num20;
					if (k == 0)
					{
						num18 -= 200f * Main.NPCSet[num].AI0;
						num19 += 130f;
						num20 = (float)Math.Sqrt(num18 * num18 + num19 * num19);
						num20 = 92f / num20;
					}
					else
					{
						num18 -= 50f * Main.NPCSet[num].AI0;
						num19 += 80f;
						num20 = (float)Math.Sqrt(num18 * num18 + num19 * num19);
						num20 = 60f / num20;
					}
					vector5.X += num18 * num20;
					vector5.Y += num19 * num20;
					pos.X = vector5.X - (float)ScreenPosition.X;
					pos.Y = vector5.Y - (float)ScreenPosition.Y;
					SpriteSheet<_sheetSprites>.DrawRotated(4, ref pos, Lighting.GetColor((int)vector5.X >> 4, (int)vector5.Y >> 4), (float)Math.Atan2(num19, num18) - 1.57f);
					if (k == 0)
					{
						vector5.X += num18 * num20 * 0.5f;
						vector5.Y += num19 * num20 * 0.5f;
					}
					else if (Main.HasFocus)
					{
						vector5.X += num18 * num20 - 16f;
						vector5.Y += num19 * num20 - 6f;
						Dust* ptr2 = dustLocal.NewDust((int)vector5.X, (int)vector5.Y, 30, 10, 6, num18 * 0.02f, num19 * 0.02f, 0, default, 2.5);
						if (ptr2 != null)
						{
							ptr2->NoGravity = true;
						}
					}
				}
			}
			else if (Main.NPCSet[num].AIStyle == 20)
			{
				Vector2 vector6 = new Vector2(Main.NPCSet[num].Position.X + (float)(Main.NPCSet[num].Width >> 1), Main.NPCSet[num].Position.Y + (float)(Main.NPCSet[num].Height >> 1));
				float num21 = Main.NPCSet[num].AI1 - vector6.X;
				float num22 = Main.NPCSet[num].AI2 - vector6.Y;
				float num23 = (float)Math.Atan2(num22, num21) - 1.57f;
				Main.NPCSet[num].Rotation = num23;
				bool flag6 = true;
				while (flag6)
				{
					int sh2 = 12;
					float num24 = (float)Math.Sqrt(num21 * num21 + num22 * num22);
					if (num24 < 20f)
					{
						sh2 = (int)num24 - 20 + 12;
						flag6 = false;
					}
					num24 = 12f / num24;
					num21 *= num24;
					num22 *= num24;
					vector6.X += num21;
					vector6.Y += num22;
					num21 = Main.NPCSet[num].AI1 - vector6.X;
					num22 = Main.NPCSet[num].AI2 - vector6.Y;
					pos = vector6;
					pos.X -= ScreenPosition.X;
					pos.Y -= ScreenPosition.Y;
					SpriteSheet<_sheetSprites>.Draw(198, ref pos, sh2, Lighting.GetColor((int)vector6.X >> 4, (int)vector6.Y >> 4), num23);
				}
				pos.X = Main.NPCSet[num].AI1 - (float)ScreenPosition.X;
				pos.Y = Main.NPCSet[num].AI2 - (float)ScreenPosition.Y;
				SpriteSheet<_sheetSprites>.DrawRotated(1474, ref pos, Lighting.GetColor((int)Main.NPCSet[num].AI1 >> 4, (int)Main.NPCSet[num].AI2 >> 4), num23 - 0.75f);
			}
			Color newColor = Lighting.GetColor(Main.NPCSet[num].XYWH.X + (Main.NPCSet[num].Width >> 1) >> 4, Main.NPCSet[num].XYWH.Y + (Main.NPCSet[num].Height >> 1) >> 4);
			if (behindTiles && type != 113 && type != 114)
			{
				int num25 = Main.NPCSet[num].XYWH.X - 8 >> 4;
				int num26 = Main.NPCSet[num].XYWH.X + Main.NPCSet[num].Width + 8 >> 4;
				int num27 = Main.NPCSet[num].XYWH.Y - 8 >> 4;
				int num28 = Main.NPCSet[num].XYWH.Y + Main.NPCSet[num].Height + 8 >> 4;
				for (int l = num25; l <= num26; l++)
				{
					bool flag7 = false;
					for (int m = num27; m <= num28; m++)
					{
						if (Lighting.Brightness(l, m) == 0f)
						{
							newColor.PackedValue = 4278190080u;
							flag7 = true;
							break;
						}
					}
					if (flag7)
					{
						break;
					}
				}
			}
			if (Main.NPCSet[num].IsPoisoned)
			{
				Player.buffColor(ref newColor, 0.65, 1.0, 0.75);
			}
			if (Player.detectCreature && Main.NPCSet[num].LifeMax > 1)
			{
				if (newColor.R < 150)
				{
					newColor.A = UI.MouseTextBrightness;
					if (newColor.R < 50)
					{
						newColor.R = 50;
					}
				}
				if (newColor.G < 200)
				{
					newColor.G = 200;
				}
				if (newColor.B < 100)
				{
					newColor.B = 100;
				}
				if (Main.HasFocus && Main.Rand.Next(52) == 0)
				{
					Dust* ptr3 = dustLocal.NewDust(Main.NPCSet[num].XYWH.X, Main.NPCSet[num].XYWH.Y, Main.NPCSet[num].Width, Main.NPCSet[num].Height, 15, 0.0, 0.0, 150, default, 0.800000011920929);
					if (ptr3 != null)
					{
						ptr3->Velocity.X *= 0.1f;
						ptr3->Velocity.Y *= 0.1f;
						ptr3->NoLight = true;
					}
				}
			}
			switch (type)
			{
			case 50:
			{
				Vector2 vector8 = new Vector2
				{
					Y = 0f - Main.NPCSet[num].Velocity.Y,
					X = Main.NPCSet[num].Velocity.X * -2f
				};
				float rotCenter4 = Main.NPCSet[num].Velocity.X * 0.05f;
				if (Main.NPCSet[num].FrameY == 120)
				{
					vector8.Y += 2f;
				}
				else if (Main.NPCSet[num].FrameY == 360)
				{
					vector8.Y -= 2f;
				}
				else if (Main.NPCSet[num].FrameY == 480)
				{
					vector8.Y -= 6f;
				}
				pos.X = Main.NPCSet[num].Position.X - (float)ScreenPosition.X + (float)(Main.NPCSet[num].Width >> 1) + vector8.X;
				pos.Y = Main.NPCSet[num].Position.Y - (float)ScreenPosition.Y + (float)(Main.NPCSet[num].Height >> 1) + vector8.Y;
				SpriteSheet<_sheetSprites>.DrawRotated(1088, ref pos, newColor, rotCenter4);
				break;
			}
			case 71:
			{
				Vector2 vector7 = new Vector2
				{
					Y = Main.NPCSet[num].Velocity.Y * -0.3f,
					X = Main.NPCSet[num].Velocity.X * -0.6f
				};
				float rotCenter3 = Main.NPCSet[num].Velocity.X * 0.09f;
				if (Main.NPCSet[num].FrameY == 120)
				{
					vector7.Y += 2f;
				}
				else if (Main.NPCSet[num].FrameY == 360)
				{
					vector7.Y -= 2f;
				}
				else if (Main.NPCSet[num].FrameY == 480)
				{
					vector7.Y -= 6f;
				}
				pos.X = Main.NPCSet[num].Position.X - (float)ScreenPosition.X + (float)(Main.NPCSet[num].Width >> 1) + vector7.X;
				pos.Y = Main.NPCSet[num].Position.Y - (float)ScreenPosition.Y + (float)(Main.NPCSet[num].Height >> 1) + vector7.Y;
				SpriteSheet<_sheetSprites>.DrawRotated(778, ref pos, newColor, rotCenter3);
				break;
			}
			case 69:
			case 147:
				pos.X = Main.NPCSet[num].Position.X - (float)ScreenPosition.X + (float)(Main.NPCSet[num].Width >> 1);
				pos.Y = Main.NPCSet[num].Position.Y - (float)ScreenPosition.Y + (float)(int)Main.NPCSet[num].Height + 14f;
				SpriteSheet<_sheetSprites>.DrawRotated((type == 69) ? 2 : 0, ref pos, newColor, (0f - Main.NPCSet[num].Rotation) * 0.3f);
				break;
			}
			float num29 = 0f;
			float num30 = 0f;
			int width = SpriteSheet<_sheetSprites>.Source[1088 + type].Width;
			Vector2 pivot = new Vector2
			{
				X = width >> 1,
				Y = Main.NPCSet[num].FrameHeight >> 1
			};
			switch ((EntityID.NPCID)type)
			{
			case EntityID.NPCID.WIZARD:
			case EntityID.NPCID.MECHANIC:
				num29 = 2f;
				break;
			case EntityID.NPCID.EYE_OF_CTHULHU:
				pivot.Y = 107f;
				break;
			case EntityID.NPCID.OCRAM:
				pivot.Y *= 0.5f;
				break;
			case EntityID.NPCID.EATER_OF_SOULS:
			case EntityID.NPCID.EATER_OF_WORLDS_HEAD:
			case EntityID.NPCID.EATER_OF_WORLDS_BODY:
			case EntityID.NPCID.EATER_OF_WORLDS_TAIL:
			case EntityID.NPCID.BONE_SERPENT_HEAD:
			case EntityID.NPCID.BONE_SERPENT_BODY:
			case EntityID.NPCID.BONE_SERPENT_TAIL:
				num30 = 26f;
				break;
			case EntityID.NPCID.GIANT_WORM_HEAD:
			case EntityID.NPCID.GIANT_WORM_BODY:
			case EntityID.NPCID.GIANT_WORM_TAIL:
				num30 = 8f;
				break;
			case EntityID.NPCID.HARPY:
				num30 = 32f;
				break;
			case EntityID.NPCID.CAVE_BAT:
			case EntityID.NPCID.JUNGLE_BAT:
				num30 = 4f;
				break;
			case EntityID.NPCID.HELLBAT:
				num30 = 10f;
				break;
			case EntityID.NPCID.DEMON:
			case EntityID.NPCID.SHARK:
			case EntityID.NPCID.VOODOO_DEMON:
				num30 = 14f;
				break;
			case EntityID.NPCID.BLUE_JELLYFISH:
			case EntityID.NPCID.PINK_JELLYFISH:
			case EntityID.NPCID.GREEN_JELLYFISH:
				num30 = 4f;
				pivot.Y += 4f;
				break;
			case EntityID.NPCID.ANTLION:
				num30 = 4f;
				pivot.Y += 8f;
				break;
			case EntityID.NPCID.SPIKE_BALL:
				num30 = -4f;
				break;
			case EntityID.NPCID.BLAZING_WHEEL:
				num30 = -2f;
				break;
			case EntityID.NPCID.CURSED_HAMMER:
			case EntityID.NPCID.ENCHANTED_SWORD:
				num30 = 20f;
				break;
			case EntityID.NPCID.DEVOURER_HEAD:
			case EntityID.NPCID.DEVOURER_BODY:
			case EntityID.NPCID.DEVOURER_TAIL:
			case EntityID.NPCID.DIGGER_HEAD:
			case EntityID.NPCID.DIGGER_BODY:
			case EntityID.NPCID.DIGGER_TAIL:
			case EntityID.NPCID.SEEKER_HEAD:
			case EntityID.NPCID.SEEKER_BODY:
			case EntityID.NPCID.SEEKER_TAIL:
				num30 = 13f;
				break;
			case EntityID.NPCID.WYVERN_HEAD:
			case EntityID.NPCID.WYVERN_LEGS:
			case EntityID.NPCID.WYVERN_BODY1:
			case EntityID.NPCID.WYVERN_BODY2:
			case EntityID.NPCID.WYVERN_BODY3:
			case EntityID.NPCID.WYVERN_TAIL:
			case EntityID.NPCID.ARCH_WYVERN_HEAD:
			case EntityID.NPCID.ARCH_WYVERN_LEGS:
			case EntityID.NPCID.ARCH_WYVERN_BODY1:
			case EntityID.NPCID.ARCH_WYVERN_BODY2:
			case EntityID.NPCID.ARCH_WYVERN_BODY3:
			case EntityID.NPCID.ARCH_WYVERN_TAIL:
				num30 = 56f;
				break;
			case EntityID.NPCID.CORRUPTOR:
				num30 = 14f;
				break;
			case EntityID.NPCID.RETINAZER:
			case EntityID.NPCID.SPAZMATISM:
				pivot = new Vector2(55f, 107f);
				num30 = 30f;
				break;
			case EntityID.NPCID.THE_DESTROYER_HEAD:
			case EntityID.NPCID.THE_DESTROYER_BODY:
			case EntityID.NPCID.THE_DESTROYER_TAIL:
				num30 = 30f;
				break;
			}
			num30 *= Main.NPCSet[num].Scale;
			pos = new Vector2(Main.NPCSet[num].Position.X - (float)ScreenPosition.X + (float)(Main.NPCSet[num].Width >> 1) - (float)width * Main.NPCSet[num].Scale * 0.5f + pivot.X * Main.NPCSet[num].Scale, Main.NPCSet[num].Position.Y - (float)ScreenPosition.Y + (float)(int)Main.NPCSet[num].Height - (float)Main.NPCSet[num].FrameHeight * Main.NPCSet[num].Scale + 4f + pivot.Y * Main.NPCSet[num].Scale + num30 + num29);
			if (Main.NPCSet[num].AIStyle == 10 || type == 72)
			{
				newColor = Color.White;
			}
			SpriteEffects e = ((Main.NPCSet[num].SpriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
			switch ((EntityID.NPCID)type)
			{
			case EntityID.NPCID.CURSED_HAMMER:
			case EntityID.NPCID.ENCHANTED_SWORD:
			case EntityID.NPCID.SHADOW_HAMMER:
				SpriteSheet<_sheetSprites>.Draw(1088 + type, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, Color.White, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				continue;
			default:
				if (type < 159 || type > 164)
				{
					break;
				}
				goto case EntityID.NPCID.WYVERN_HEAD;
			case EntityID.NPCID.WYVERN_HEAD:
			case EntityID.NPCID.WYVERN_LEGS:
			case EntityID.NPCID.WYVERN_BODY1:
			case EntityID.NPCID.WYVERN_BODY2:
			case EntityID.NPCID.WYVERN_BODY3:
			case EntityID.NPCID.WYVERN_TAIL:
			{
				c = Main.NPCSet[num].GetAlpha(newColor);
				byte b = (byte)((WorldTime.tileColor.R + WorldTime.tileColor.G + WorldTime.tileColor.B) / 3);
				if (c.R < b)
				{
					c.R = b;
				}
				if (c.G < b)
				{
					c.G = b;
				}
				if (c.B < b)
				{
					c.B = b;
				}
				SpriteSheet<_sheetSprites>.Draw(1088 + type, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				continue;
			}
			}
			switch ((EntityID.NPCID)type)
			{
			case EntityID.NPCID.CORRUPTOR:
			{
				for (int n = 1; n < 6; n += 2)
				{
					c = Main.NPCSet[num].GetAlpha(newColor);
					c.R = (byte)(c.R * (10 - n) / 15);
					c.G = (byte)(c.G * (10 - n) / 15);
					c.B = (byte)(c.B * (10 - n) / 15);
					c.A = (byte)(c.A * (10 - n) / 15);
					pos = new Vector2(Main.NPCSet[num].OldPos[n].X - (float)ScreenPosition.X + (float)(Main.NPCSet[num].Width >> 1) - (float)width * Main.NPCSet[num].Scale * 0.5f + pivot.X * Main.NPCSet[num].Scale, Main.NPCSet[num].OldPos[n].Y - (float)ScreenPosition.Y + (float)(int)Main.NPCSet[num].Height - (float)Main.NPCSet[num].FrameHeight * Main.NPCSet[num].Scale + 4f + pivot.Y * Main.NPCSet[num].Scale + num30);
					SpriteSheet<_sheetSprites>.Draw(1088 + type, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				}
				break;
			}
			case EntityID.NPCID.RETINAZER:
			case EntityID.NPCID.SPAZMATISM:
			case EntityID.NPCID.SKELETRON_PRIME:
			case EntityID.NPCID.PRIME_CANNON:
			case EntityID.NPCID.PRIME_SAW:
			case EntityID.NPCID.PRIME_VICE:
			case EntityID.NPCID.PRIME_LASER:
			case EntityID.NPCID.PROBE:
			case EntityID.NPCID.POSSESSED_ARMOR:
			{
				for (int num31 = 9; num31 >= 0; num31 -= 2)
				{
					c = Main.NPCSet[num].GetAlpha(newColor);
					c.R = (byte)(c.R * (10 - num31) / 20);
					c.G = (byte)(c.G * (10 - num31) / 20);
					c.B = (byte)(c.B * (10 - num31) / 20);
					c.A = (byte)(c.A * (10 - num31) / 20);
					pos = new Vector2(Main.NPCSet[num].OldPos[num31].X - (float)ScreenPosition.X + (float)(Main.NPCSet[num].Width >> 1) - (float)width * Main.NPCSet[num].Scale * 0.5f + pivot.X * Main.NPCSet[num].Scale, Main.NPCSet[num].OldPos[num31].Y - (float)ScreenPosition.Y + (float)(int)Main.NPCSet[num].Height - (float)Main.NPCSet[num].FrameHeight * Main.NPCSet[num].Scale + 4f + pivot.Y * Main.NPCSet[num].Scale + num30);
					SpriteSheet<_sheetSprites>.Draw(1088 + type, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				}
				break;
			}
			}
			SpriteSheet<_sheetSprites>.Draw(1088 + type, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, Main.NPCSet[num].GetAlpha(newColor), Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
			if (Main.NPCSet[num].Colour.PackedValue != 0)
			{
				SpriteSheet<_sheetSprites>.Draw(1088 + type, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, Main.NPCSet[num].GetColor(newColor), Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
			}
			if (Main.NPCSet[num].IsConfused)
			{
				Vector2 pos2 = pos;
				pos2.Y -= SpriteSheet<_sheetSprites>.Source[203].Height + 20;
				c.PackedValue = 1190853370u;
				SpriteSheet<_sheetSprites>.Draw(203, ref pos2, c, Main.NPCSet[num].Velocity.X * -0.05f, UI.PulseScale + 0.2f);
			}
			switch ((EntityID.NPCID)type)
			{
			case EntityID.NPCID.RETINAZER:
				c.PackedValue = 16777215u;
				SpriteSheet<_sheetSprites>.Draw(220, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				continue;
			case EntityID.NPCID.PROBE:
				c.PackedValue = 16777215u;
				SpriteSheet<_sheetSprites>.Draw(1349, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				continue;
			case EntityID.NPCID.SKELETRON_PRIME:
				c.PackedValue = 13158600u;
				SpriteSheet<_sheetSprites>.Draw(137, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				continue;
			case EntityID.NPCID.PRIME_LASER:
				c.PackedValue = 13158600u;
				SpriteSheet<_sheetSprites>.Draw(138, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				continue;
			case EntityID.NPCID.CHAOS_ELEMENTAL:
			case EntityID.NPCID.SPECTRAL_ELEMENTAL:
			{
				for (int num32 = 1; num32 < Main.NPCSet[num].OldPos.Length; num32++)
				{
					c.R = (byte)(150 * (10 - num32) / 15);
					c.G = (byte)(100 * (10 - num32) / 15);
					c.B = (byte)(150 * (10 - num32) / 15);
					c.A = (byte)(50 * (10 - num32) / 15);
					pos = new Vector2(Main.NPCSet[num].OldPos[num32].X - (float)ScreenPosition.X + (float)(Main.NPCSet[num].Width >> 1) - (float)width * Main.NPCSet[num].Scale * 0.5f + pivot.X * Main.NPCSet[num].Scale, Main.NPCSet[num].OldPos[num32].Y - (float)ScreenPosition.Y + (float)(int)Main.NPCSet[num].Height - (float)Main.NPCSet[num].FrameHeight * Main.NPCSet[num].Scale + 4f + pivot.Y * Main.NPCSet[num].Scale + num30);
					SpriteSheet<_sheetSprites>.Draw(199, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				}
				continue;
			}
			case EntityID.NPCID.THE_DESTROYER_HEAD:
			case EntityID.NPCID.THE_DESTROYER_BODY:
			case EntityID.NPCID.THE_DESTROYER_TAIL:
				if (newColor.PackedValue != 4278190080u)
				{
					c.PackedValue = 16777215u;
					SpriteSheet<_sheetSprites>.Draw(214 + type - 134, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				}
				continue;
			}
			switch ((EntityID.NPCID)type)
			{
			case EntityID.NPCID.ILLUMINANT_BAT:
			case EntityID.NPCID.ILLUMINANT_SLIME:
			{
				for (int num34 = 1; num34 < Main.NPCSet[num].OldPos.Length; num34++)
				{
					c.R = (byte)(150 * (10 - num34) / 15);
					c.G = (byte)(100 * (10 - num34) / 15);
					c.B = (byte)(150 * (10 - num34) / 15);
					c.A = (byte)(50 * (10 - num34) / 15);
					pos = new Vector2(Main.NPCSet[num].OldPos[num34].X - (float)ScreenPosition.X + (float)(Main.NPCSet[num].Width >> 1) - (float)width * Main.NPCSet[num].Scale * 0.5f + pivot.X * Main.NPCSet[num].Scale, Main.NPCSet[num].OldPos[num34].Y - (float)ScreenPosition.Y + (float)(int)Main.NPCSet[num].Height - (float)Main.NPCSet[num].FrameHeight * Main.NPCSet[num].Scale + 4f + pivot.Y * Main.NPCSet[num].Scale + num30);
					SpriteSheet<_sheetSprites>.Draw(1088 + type, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, c, Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				}
				break;
			}
			case EntityID.NPCID.WRAITH:
			{
				SpriteSheet<_sheetSprites>.Draw(1488, ref pos, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, new Color(255, 255, 255, 255), Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				for (int num33 = 1; num33 < 10; num33++)
				{
					Vector2 pos3 = pos - Main.NPCSet[num].Velocity * ((float)num33 * 0.5f);
					SpriteSheet<_sheetSprites>.Draw(1488, ref pos3, Main.NPCSet[num].FrameY, Main.NPCSet[num].FrameHeight, new Color(110 - num33 * 10, 110 - num33 * 10, 110 - num33 * 10, 110 - num33 * 10), Main.NPCSet[num].Rotation, ref pivot, Main.NPCSet[num].Scale, e);
				}
				break;
			}
			}
		}
	}

	private void DrawWoF()
	{
		Vector2 pos = default;
		if (NPC.WoF < 0 || !Player.IsHorrified)
		{
			return;
		}
		float num = Main.NPCSet[NPC.WoF].Position.X + (float)(Main.NPCSet[NPC.WoF].Width >> 1);
		float num2 = Main.NPCSet[NPC.WoF].Position.Y + (float)(Main.NPCSet[NPC.WoF].Height >> 1);
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active == 0 || !Main.PlayerSet[i].tongued || Main.PlayerSet[i].IsDead)
			{
				continue;
			}
			Vector2 vector = new Vector2(Main.PlayerSet[i].Position.X + 10f, Main.PlayerSet[i].Position.Y + 21f);
			float num3 = num - vector.X;
			float num4 = num2 - vector.Y;
			float rotCenter = (float)Math.Atan2(num4, num3) - 1.57f;
			bool flag = true;
			do
			{
				float num5 = num3 * num3 + num4 * num4;
				if (num5 < 1600f)
				{
					flag = false;
					continue;
				}
				num5 = (float)SpriteSheet<_sheetSprites>.Source[197].Height / (float)Math.Sqrt(num5);
				num3 *= num5;
				num4 *= num5;
				vector.X += num3;
				vector.Y += num4;
				num3 = num - vector.X;
				num4 = num2 - vector.Y;
				pos = vector;
				pos.X -= ScreenPosition.X;
				pos.Y -= ScreenPosition.Y;
				SpriteSheet<_sheetSprites>.DrawRotated(197, ref pos, Lighting.GetColor((int)vector.X >> 4, (int)vector.Y >> 4), rotCenter);
			}
			while (flag);
		}
		float num6 = NPC.WoFBottom - NPC.WoFTop;
		for (int j = 0; j < 196; j++)
		{
			if (Main.NPCSet[j].Active == 0 || Main.NPCSet[j].AIStyle != 29)
			{
				continue;
			}
			bool flag2 = Main.NPCSet[j].FrameCounter > 7f;
			num2 = (float)NPC.WoFTop + num6 * Main.NPCSet[j].AI0;
			Vector2 vector2 = new Vector2(Main.NPCSet[j].Position.X + (float)(Main.NPCSet[j].Width >> 1), Main.NPCSet[j].Position.Y + (float)(Main.NPCSet[j].Height >> 1));
			float num7 = num - vector2.X;
			float num8 = num2 - vector2.Y;
			float rotCenter2 = (float)Math.Atan2(num8, num7) - 1.57f;
			bool flag3 = true;
			while (flag3)
			{
				SpriteEffects se = SpriteEffects.None;
				if (flag2)
				{
					se = SpriteEffects.FlipHorizontally;
					flag2 = false;
				}
				else
				{
					flag2 = true;
				}
				float num9 = (float)Math.Sqrt(num7 * num7 + num8 * num8);
				if (num9 < 40f)
				{
					flag3 = false;
				}
				num9 = 28f / num9;
				num7 *= num9;
				num8 *= num9;
				vector2.X += num7;
				vector2.Y += num8;
				num7 = num - vector2.X;
				num8 = num2 - vector2.Y;
				pos = vector2;
				pos.X -= ScreenPosition.X;
				pos.Y -= ScreenPosition.Y;
				SpriteSheet<_sheetSprites>.Draw(197, ref pos, Lighting.GetColor((int)vector2.X >> 4, (int)vector2.Y >> 4), rotCenter2, se);
			}
		}
		int num10 = 140;
		float num11 = NPC.WoFTop;
		float num12 = NPC.WoFBottom;
		num12 = ScreenPosition.Y + Main.ResolutionHeight;
		float num13 = (num11 - (float)ScreenPosition.Y) / (float)num10 + 1f;
		num13 *= (float)num10;
		if (num13 > 0f)
		{
			num11 -= num13;
		}
		float num14 = num11;
		float num15 = Main.NPCSet[NPC.WoF].Position.X;
		float num16 = num12 - num11;
		SpriteEffects e = SpriteEffects.None;
		if (Main.NPCSet[NPC.WoF].SpriteDirection == 1)
		{
			e = SpriteEffects.FlipHorizontally;
		}
		if (Main.NPCSet[NPC.WoF].Direction > 0)
		{
			num15 -= 80f;
		}
		int num17 = 0;
		if (++NPC.WoFFront > 12)
		{
			num17 = 280;
			if (NPC.WoFFront > 17)
			{
				NPC.WoFFront = 0;
			}
		}
		else if (NPC.WoFFront > 6)
		{
			num17 = 140;
		}
		do
		{
			num16 = num12 - num14;
			if (num16 > (float)num10)
			{
				num16 = num10;
			}
			int num18 = 0;
			int width = SpriteSheet<_sheetSprites>.Source[1483].Width;
			do
			{
				int x = (int)num15 + (width >> 1) >> 4;
				int y = (int)num14 + num18 >> 4;
				pos.X = num15 - (float)ScreenPosition.X;
				pos.Y = num14 + (float)num18 - (float)ScreenPosition.Y;
				SpriteSheet<_sheetSprites>.Draw(1483, ref pos, num17 + num18, 16, Lighting.GetColor(x, y), e);
				num18 += 16;
			}
			while ((float)num18 < num16);
			num14 += (float)num10;
		}
		while (num14 < num12);
	}

	public void DrawNPCHouse()
	{
		for (int i = 0; i < 196; i++)
		{
			if (Main.NPCSet[i].Active == 0 || !Main.NPCSet[i].IsTownNPC || Main.NPCSet[i].IsHomeless || Main.NPCSet[i].HomeTileX <= 0 || Main.NPCSet[i].HomeTileY <= 0 || Main.NPCSet[i].Type == 37)
			{
				continue;
			}
			int homeTileX = Main.NPCSet[i].HomeTileX;
			int num = Main.NPCSet[i].HomeTileY - 1;
			while (Main.TileSet[homeTileX, num].IsActive == 0 || !Main.TileSolid[Main.TileSet[homeTileX, num].Type])
			{
				num--;
				if (num < 10)
				{
					break;
				}
			}
			int num2 = 18;
			if (Main.TileSet[homeTileX, num].Type == 19)
			{
				num2 -= 8;
			}
			num++;
			Color color = Lighting.GetColor(homeTileX, num);
			SpriteSheet<_sheetSprites>.Draw(439, (homeTileX << 4) - ScreenPosition.X + 8 - 16, (num << 4) - ScreenPosition.Y + num2 - 20, color);
			int num3 = Main.NPCSet[i].GetHeadTextureID() + 1256 - 1;
			float scaleCenter = 1f;
			float num4 = SpriteSheet<_sheetSprites>.Source[num3].Height;
			if ((float)SpriteSheet<_sheetSprites>.Source[num3].Width > num4)
			{
				num4 = SpriteSheet<_sheetSprites>.Source[num3].Width;
			}
			if (num4 > 24f)
			{
				scaleCenter = 24f / num4;
			}
			SpriteSheet<_sheetSprites>.DrawScaled(num3, (homeTileX << 4) - ScreenPosition.X + 8, (num << 4) - ScreenPosition.Y + num2 + 2, scaleCenter, color);
		}
	}

	public void DrawGrid()
	{
		int num = (ScreenPosition.X & -16) - ScreenPosition.X;
		int num2 = (ScreenPosition.Y & -16) - ScreenPosition.Y;
		int num3 = ViewWidth >> 5;
		int num4 = Main.ResolutionHeight >> 5;
		Color c = new Color(100, 100, 100, 15);
		for (int i = 0; i <= num3; i++)
		{
			for (int j = 0; j <= num4; j++)
			{
				SpriteSheet<_sheetSprites>.Draw(431, (i << 6) + num, (j << 6) + num2, c);
			}
		}
	}

	public unsafe void spawnSnow()
	{
		if (SnowTiles <= 1024 || Player.XYWH.Y >= Main.WorldSurfacePixels || dustLocal.SnowDust >= 32)
		{
			return;
		}
		int upperBound = 4096 / SnowTiles;
		if (Main.Rand.Next(upperBound) == 0)
		{
			int num = Main.Rand.Next(ViewWidth + ViewportWidth / 30) - ViewportWidth / 60;
			int num2 = ScreenPosition.Y;
			if (num < 0 || num > ViewportWidth)
			{
				num2 += Main.Rand.Next(ViewportHeight / 2) + ViewportHeight / 10;
			}
			num += ScreenPosition.X;
			Dust* ptr = dustLocal.NewDust(num, num2, 10, 10, 76);
			if (ptr != null)
			{
				ptr->Velocity.X = Cloud.WindSpeed + (float)Main.Rand.Next(-10, 10) * 0.1f;
				ptr->Velocity.Y = 3f + (float)Main.Rand.Next(30) * 0.1f * ptr->Scale;
			}
		}
	}
}
