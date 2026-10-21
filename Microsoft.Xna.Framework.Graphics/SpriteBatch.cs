using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics;

public class SpriteBatch : GraphicsResource
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct VertexPositionColorTexture4 : IVertexType
	{
		public const int RealStride = 96;

		public Vector3 Position0;

		public Color Color0;

		public Vector2 TextureCoordinate0;

		public Vector3 Position1;

		public Color Color1;

		public Vector2 TextureCoordinate1;

		public Vector3 Position2;

		public Color Color2;

		public Vector2 TextureCoordinate2;

		public Vector3 Position3;

		public Color Color3;

		public Vector2 TextureCoordinate3;

		VertexDeclaration IVertexType.VertexDeclaration
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct SpriteInfo
	{
		public int textureHash;

		public float sourceX;

		public float sourceY;

		public float sourceW;

		public float sourceH;

		public float destinationX;

		public float destinationY;

		public float destinationW;

		public float destinationH;

		public Color color;

		public float originX;

		public float originY;

		public float rotationSin;

		public float rotationCos;

		public float depth;

		public byte effects;
	}

	private class TextureComparer : IComparer<IntPtr>
	{
		public unsafe int Compare(IntPtr i1, IntPtr i2)
		{
			SpriteInfo* ptr = (SpriteInfo*)(void*)i1;
			SpriteInfo* ptr2 = (SpriteInfo*)(void*)i2;
			return ptr->textureHash.CompareTo(ptr2->textureHash);
		}
	}

	private class BackToFrontComparer : IComparer<IntPtr>
	{
		public unsafe int Compare(IntPtr i1, IntPtr i2)
		{
			SpriteInfo* ptr = (SpriteInfo*)(void*)i1;
			SpriteInfo* ptr2 = (SpriteInfo*)(void*)i2;
			return ptr2->depth.CompareTo(ptr->depth);
		}
	}

	private class FrontToBackComparer : IComparer<IntPtr>
	{
		public unsafe int Compare(IntPtr i1, IntPtr i2)
		{
			SpriteInfo* ptr = (SpriteInfo*)(void*)i1;
			SpriteInfo* ptr2 = (SpriteInfo*)(void*)i2;
			return ptr->depth.CompareTo(ptr2->depth);
		}
	}

	private const int MAX_SPRITES = 2048;

	private const int MAX_VERTICES = 8192;

	private const int MAX_INDICES = 12288;

	private static readonly float[] axisDirectionX = new float[4] { -1f, 1f, -1f, 1f };

	private static readonly float[] axisDirectionY = new float[4] { -1f, -1f, 1f, 1f };

	private static readonly float[] axisIsMirroredX = new float[4] { 0f, 1f, 0f, 1f };

	private static readonly float[] axisIsMirroredY = new float[4] { 0f, 0f, 1f, 1f };

	private static readonly float[] CornerOffsetX = new float[4] { 0f, 1f, 0f, 1f };

	private static readonly float[] CornerOffsetY = new float[4] { 0f, 0f, 1f, 1f };

	private DynamicVertexBuffer vertexBuffer;

	private IndexBuffer indexBuffer;

	private SpriteInfo[] spriteInfos;

	private IntPtr[] sortedSpriteInfos;

	private VertexPositionColorTexture4[] vertexInfo;

	private Texture2D[] textureInfo;

	private Effect spriteEffect;

	private IntPtr spriteMatrixTransform;

	private EffectPass spriteEffectPass;

	private bool beginCalled;

	/// <summary>
	/// How many times a Begin/End pair did not balance this session. Non-zero means some screen
	/// forgot to close its batch; the log names the count, and this makes it visible to the
	/// self-test instead of hiding in a crash log.
	/// </summary>
	public static int BeginEndMismatches { get; private set; }

	private static int _lastMismatchTick;

	/// <summary>True while a batch is open. Used by tests and by the frame guard.</summary>
	public bool IsBegun => beginCalled;

	private SpriteSortMode sortMode;

	private BlendState blendState;

	private SamplerState samplerState;

	private DepthStencilState depthStencilState;

	private RasterizerState rasterizerState;

	private int numSprites;

	private int bufferOffset;

	private bool supportsNoOverwrite;

	private Matrix transformMatrix;

	private Effect customEffect;

	private static readonly byte[] spriteEffectCode = Resources.SpriteEffect;

	private static readonly short[] indexData = GenerateIndexArray();

	private static readonly TextureComparer TextureCompare = new TextureComparer();

	private static readonly BackToFrontComparer BackToFrontCompare = new BackToFrontComparer();

	private static readonly FrontToBackComparer FrontToBackCompare = new FrontToBackComparer();

	public SpriteBatch(GraphicsDevice graphicsDevice)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice");
		}
		GraphicsDevice = graphicsDevice;
		vertexInfo = new VertexPositionColorTexture4[2048];
		textureInfo = new Texture2D[2048];
		spriteInfos = new SpriteInfo[2048];
		sortedSpriteInfos = new IntPtr[2048];
		vertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexPositionColorTexture), 8192, BufferUsage.WriteOnly);
		indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 12288, BufferUsage.WriteOnly);
		indexBuffer.SetData(indexData);
		spriteEffect = new Effect(graphicsDevice, spriteEffectCode);
		spriteMatrixTransform = spriteEffect.Parameters["MatrixTransform"].values;
		spriteEffectPass = spriteEffect.CurrentTechnique.Passes[0];
		beginCalled = false;
		numSprites = 0;
		supportsNoOverwrite = FNA3D.FNA3D_SupportsNoOverwrite(GraphicsDevice.GLDevice) == 1;
	}

	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			spriteEffect.Dispose();
			indexBuffer.Dispose();
			vertexBuffer.Dispose();
		}
		base.Dispose(disposing);
	}

	public void Begin()
	{
		Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
	}

	public void Begin(SpriteSortMode sortMode, BlendState blendState)
	{
		Begin(sortMode, blendState, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
	}

	public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState)
	{
		Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, null, Matrix.Identity);
	}

	public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect)
	{
		Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, Matrix.Identity);
	}

	public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformationMatrix)
	{
		if (beginCalled)
		{
			// XNA throws here, and the original code relied on every Begin/End pair balancing.
			// This port has many pairs spread over screens that can change mid-frame, and one
			// missing End then killed the session with a message that says nothing about which
			// screen forgot it. Flushing the previous batch keeps the frame alive and the warning
			// names the problem; a dropped sprite is recoverable, an exception in Draw is not.
			BeginEndMismatches++;
			if (BeginEndMismatches == 1 || Environment.TickCount - _lastMismatchTick > 5000)
			{
				_lastMismatchTick = Environment.TickCount;
				Terraria.Program.LogWarn("SpriteBatch", "Begin() while a batch was still open - closing the previous one ("
					+ BeginEndMismatches + " time(s) this session)");
			}
			try
			{
				End();
			}
			catch
			{
			}
		}
		beginCalled = true;
		this.sortMode = sortMode;
		this.blendState = blendState ?? BlendState.AlphaBlend;
		this.samplerState = samplerState ?? SamplerState.LinearClamp;
		this.depthStencilState = depthStencilState ?? DepthStencilState.None;
		this.rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
		customEffect = effect;
		transformMatrix = transformationMatrix;
		if (sortMode == SpriteSortMode.Immediate)
		{
			PrepRenderState();
		}
	}

	/// <summary>
	/// Closes a batch whatever state it is in, and reports whether it had to. Called at the top of
	/// a frame: if some screen forgot its End, the next frame would otherwise die on Begin.
	/// </summary>
	public void ForceEnd()
	{
		if (beginCalled)
		{
			try
			{
				End();
			}
			catch
			{
			}
		}
	}

	public void End()
	{
		if (!beginCalled)
		{
			// The mirror of the case above: the caller ended a batch that was not open. Same
			// reasoning - a stray End must not be fatal. Counted separately so the log shows
			// which way the mismatch goes.
			BeginEndMismatches++;
			if (BeginEndMismatches == 1 || Environment.TickCount - _lastMismatchTick > 5000)
			{
				_lastMismatchTick = Environment.TickCount;
				Terraria.Program.LogWarn("SpriteBatch", "End() without an open batch - ignored ("
					+ BeginEndMismatches + " time(s) this session)");
			}
			return;
		}
		beginCalled = false;
		if (sortMode != SpriteSortMode.Immediate)
		{
			FlushBatch();
		}
		customEffect = null;
	}

	public void Draw(Texture2D texture, Vector2 position, Color color)
	{
		CheckBegin("Draw");
		PushSprite(texture, 0f, 0f, 1f, 1f, position.X, position.Y, texture.Width, texture.Height, color, 0f, 0f, 0f, 1f, 0f, 0);
	}

	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
	{
		float sourceX;
		float sourceY;
		float sourceW;
		float sourceH;
		float destinationW;
		float destinationH;
		if (sourceRectangle.HasValue)
		{
			sourceX = (float)sourceRectangle.Value.X / (float)texture.Width;
			sourceY = (float)sourceRectangle.Value.Y / (float)texture.Height;
			sourceW = (float)sourceRectangle.Value.Width / (float)texture.Width;
			sourceH = (float)sourceRectangle.Value.Height / (float)texture.Height;
			destinationW = sourceRectangle.Value.Width;
			destinationH = sourceRectangle.Value.Height;
		}
		else
		{
			sourceX = 0f;
			sourceY = 0f;
			sourceW = 1f;
			sourceH = 1f;
			destinationW = texture.Width;
			destinationH = texture.Height;
		}
		CheckBegin("Draw");
		PushSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, destinationW, destinationH, color, 0f, 0f, 0f, 1f, 0f, 0);
	}

	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		CheckBegin("Draw");
		float num = scale;
		float num2 = scale;
		float sourceX;
		float sourceY;
		float num3;
		float num4;
		if (sourceRectangle.HasValue)
		{
			sourceX = (float)sourceRectangle.Value.X / (float)texture.Width;
			sourceY = (float)sourceRectangle.Value.Y / (float)texture.Height;
			num3 = (float)Math.Sign(sourceRectangle.Value.Width) * Math.Max(Math.Abs(sourceRectangle.Value.Width), MathHelper.MachineEpsilonFloat) / (float)texture.Width;
			num4 = (float)Math.Sign(sourceRectangle.Value.Height) * Math.Max(Math.Abs(sourceRectangle.Value.Height), MathHelper.MachineEpsilonFloat) / (float)texture.Height;
			num *= (float)sourceRectangle.Value.Width;
			num2 *= (float)sourceRectangle.Value.Height;
		}
		else
		{
			sourceX = 0f;
			sourceY = 0f;
			num3 = 1f;
			num4 = 1f;
			num *= (float)texture.Width;
			num2 *= (float)texture.Height;
		}
		PushSprite(texture, sourceX, sourceY, num3, num4, position.X, position.Y, num, num2, color, origin.X / num3 / (float)texture.Width, origin.Y / num4 / (float)texture.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), layerDepth, (byte)(effects & (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically)));
	}

	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		CheckBegin("Draw");
		float sourceX;
		float sourceY;
		float num;
		float num2;
		if (sourceRectangle.HasValue)
		{
			sourceX = (float)sourceRectangle.Value.X / (float)texture.Width;
			sourceY = (float)sourceRectangle.Value.Y / (float)texture.Height;
			num = (float)Math.Sign(sourceRectangle.Value.Width) * Math.Max(Math.Abs(sourceRectangle.Value.Width), MathHelper.MachineEpsilonFloat) / (float)texture.Width;
			num2 = (float)Math.Sign(sourceRectangle.Value.Height) * Math.Max(Math.Abs(sourceRectangle.Value.Height), MathHelper.MachineEpsilonFloat) / (float)texture.Height;
			scale.X *= sourceRectangle.Value.Width;
			scale.Y *= sourceRectangle.Value.Height;
		}
		else
		{
			sourceX = 0f;
			sourceY = 0f;
			num = 1f;
			num2 = 1f;
			scale.X *= texture.Width;
			scale.Y *= texture.Height;
		}
		PushSprite(texture, sourceX, sourceY, num, num2, position.X, position.Y, scale.X, scale.Y, color, origin.X / num / (float)texture.Width, origin.Y / num2 / (float)texture.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), layerDepth, (byte)(effects & (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically)));
	}

	public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color)
	{
		CheckBegin("Draw");
		PushSprite(texture, 0f, 0f, 1f, 1f, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, color, 0f, 0f, 0f, 1f, 0f, 0);
	}

	public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
	{
		CheckBegin("Draw");
		float sourceX;
		float sourceY;
		float sourceW;
		float sourceH;
		if (sourceRectangle.HasValue)
		{
			sourceX = (float)sourceRectangle.Value.X / (float)texture.Width;
			sourceY = (float)sourceRectangle.Value.Y / (float)texture.Height;
			sourceW = (float)sourceRectangle.Value.Width / (float)texture.Width;
			sourceH = (float)sourceRectangle.Value.Height / (float)texture.Height;
		}
		else
		{
			sourceX = 0f;
			sourceY = 0f;
			sourceW = 1f;
			sourceH = 1f;
		}
		PushSprite(texture, sourceX, sourceY, sourceW, sourceH, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, color, 0f, 0f, 0f, 1f, 0f, 0);
	}

	public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
	{
		CheckBegin("Draw");
		float sourceX;
		float sourceY;
		float num;
		float num2;
		if (sourceRectangle.HasValue)
		{
			sourceX = (float)sourceRectangle.Value.X / (float)texture.Width;
			sourceY = (float)sourceRectangle.Value.Y / (float)texture.Height;
			num = (float)Math.Sign(sourceRectangle.Value.Width) * Math.Max(Math.Abs(sourceRectangle.Value.Width), MathHelper.MachineEpsilonFloat) / (float)texture.Width;
			num2 = (float)Math.Sign(sourceRectangle.Value.Height) * Math.Max(Math.Abs(sourceRectangle.Value.Height), MathHelper.MachineEpsilonFloat) / (float)texture.Height;
		}
		else
		{
			sourceX = 0f;
			sourceY = 0f;
			num = 1f;
			num2 = 1f;
		}
		PushSprite(texture, sourceX, sourceY, num, num2, destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, color, origin.X / num / (float)texture.Width, origin.Y / num2 / (float)texture.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), layerDepth, (byte)(effects & (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically)));
	}

	public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		DrawString(spriteFont, text, position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
	}

	public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		DrawString(spriteFont, text, position, color, rotation, origin, new Vector2(scale), effects, layerDepth);
	}

	public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		CheckBegin("DrawString");
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		if (text.Length == 0)
		{
			return;
		}
		effects &= SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;
		Texture2D textureValue = spriteFont.textureValue;
		List<Rectangle> glyphData = spriteFont.glyphData;
		List<Rectangle> croppingData = spriteFont.croppingData;
		List<Vector3> kerning = spriteFont.kerning;
		Dictionary<char, int> characterIndexMap = spriteFont.characterIndexMap;
		Vector2 vector = origin;
		float num = axisDirectionX[(int)effects];
		float num2 = axisDirectionY[(int)effects];
		float num3 = 0f;
		float num4 = 0f;
		if (effects != SpriteEffects.None)
		{
			Vector2 vector2 = spriteFont.MeasureString(text);
			vector.X -= vector2.X * axisIsMirroredX[(int)effects];
			vector.Y -= vector2.Y * axisIsMirroredY[(int)effects];
			num3 = axisIsMirroredX[(int)effects];
			num4 = axisIsMirroredY[(int)effects];
		}
		Vector2 zero = Vector2.Zero;
		bool flag = true;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '\n':
				zero.X = 0f;
				zero.Y += spriteFont.LineSpacing;
				flag = true;
				continue;
			case '\r':
				continue;
			}
			if (!characterIndexMap.TryGetValue(c, out var value))
			{
				if (!spriteFont.DefaultCharacter.HasValue)
				{
					throw new ArgumentException("Text contains characters that cannot be resolved by this SpriteFont.", "text");
				}
				value = characterIndexMap[spriteFont.DefaultCharacter.Value];
			}
			Vector3 vector3 = kerning[value];
			if (flag)
			{
				zero.X += Math.Abs(vector3.X);
				flag = false;
			}
			else
			{
				zero.X += spriteFont.Spacing + vector3.X;
			}
			Rectangle rectangle = croppingData[value];
			Rectangle rectangle2 = glyphData[value];
			float num5 = vector.X + (zero.X + (float)rectangle.X) * num;
			float num6 = vector.Y + (zero.Y + (float)rectangle.Y) * num2;
			if (effects != SpriteEffects.None)
			{
				num5 += (float)rectangle2.Width * num3;
				num6 += (float)rectangle2.Height * num4;
			}
			float num7 = (float)Math.Sign(rectangle2.Width) * Math.Max(Math.Abs(rectangle2.Width), MathHelper.MachineEpsilonFloat) / (float)textureValue.Width;
			float num8 = (float)Math.Sign(rectangle2.Height) * Math.Max(Math.Abs(rectangle2.Height), MathHelper.MachineEpsilonFloat) / (float)textureValue.Height;
			PushSprite(textureValue, (float)rectangle2.X / (float)textureValue.Width, (float)rectangle2.Y / (float)textureValue.Height, num7, num8, position.X, position.Y, (float)rectangle2.Width * scale.X, (float)rectangle2.Height * scale.Y, color, num5 / num7 / (float)textureValue.Width, num6 / num8 / (float)textureValue.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), layerDepth, (byte)effects);
			zero.X += vector3.Y + vector3.Z;
		}
	}

	public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color)
	{
		DrawString(spriteFont, text, position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
	}

	public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		DrawString(spriteFont, text, position, color, rotation, origin, new Vector2(scale), effects, layerDepth);
	}

	public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		CheckBegin("DrawString");
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		if (text.Length == 0)
		{
			return;
		}
		effects &= SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;
		Texture2D textureValue = spriteFont.textureValue;
		List<Rectangle> glyphData = spriteFont.glyphData;
		List<Rectangle> croppingData = spriteFont.croppingData;
		List<Vector3> kerning = spriteFont.kerning;
		Dictionary<char, int> characterIndexMap = spriteFont.characterIndexMap;
		Vector2 vector = origin;
		float num = axisDirectionX[(int)effects];
		float num2 = axisDirectionY[(int)effects];
		float num3 = 0f;
		float num4 = 0f;
		if (effects != SpriteEffects.None)
		{
			Vector2 vector2 = spriteFont.MeasureString(text);
			vector.X -= vector2.X * axisIsMirroredX[(int)effects];
			vector.Y -= vector2.Y * axisIsMirroredY[(int)effects];
			num3 = axisIsMirroredX[(int)effects];
			num4 = axisIsMirroredY[(int)effects];
		}
		Vector2 zero = Vector2.Zero;
		bool flag = true;
		foreach (char c in text)
		{
			switch (c)
			{
			case '\n':
				zero.X = 0f;
				zero.Y += spriteFont.LineSpacing;
				flag = true;
				continue;
			case '\r':
				continue;
			}
			if (!characterIndexMap.TryGetValue(c, out var value))
			{
				if (!spriteFont.DefaultCharacter.HasValue)
				{
					throw new ArgumentException("Text contains characters that cannot be resolved by this SpriteFont.", "text");
				}
				value = characterIndexMap[spriteFont.DefaultCharacter.Value];
			}
			Vector3 vector3 = kerning[value];
			if (flag)
			{
				zero.X += Math.Abs(vector3.X);
				flag = false;
			}
			else
			{
				zero.X += spriteFont.Spacing + vector3.X;
			}
			Rectangle rectangle = croppingData[value];
			Rectangle rectangle2 = glyphData[value];
			float num5 = vector.X + (zero.X + (float)rectangle.X) * num;
			float num6 = vector.Y + (zero.Y + (float)rectangle.Y) * num2;
			if (effects != SpriteEffects.None)
			{
				num5 += (float)rectangle2.Width * num3;
				num6 += (float)rectangle2.Height * num4;
			}
			float num7 = (float)Math.Sign(rectangle2.Width) * Math.Max(Math.Abs(rectangle2.Width), MathHelper.MachineEpsilonFloat) / (float)textureValue.Width;
			float num8 = (float)Math.Sign(rectangle2.Height) * Math.Max(Math.Abs(rectangle2.Height), MathHelper.MachineEpsilonFloat) / (float)textureValue.Height;
			PushSprite(textureValue, (float)rectangle2.X / (float)textureValue.Width, (float)rectangle2.Y / (float)textureValue.Height, num7, num8, position.X, position.Y, (float)rectangle2.Width * scale.X, (float)rectangle2.Height * scale.Y, color, num5 / num7 / (float)textureValue.Width, num6 / num8 / (float)textureValue.Height, (float)Math.Sin(rotation), (float)Math.Cos(rotation), layerDepth, (byte)effects);
			zero.X += vector3.Y + vector3.Z;
		}
	}

	private unsafe void PushSprite(Texture2D texture, float sourceX, float sourceY, float sourceW, float sourceH, float destinationX, float destinationY, float destinationW, float destinationH, Color color, float originX, float originY, float rotationSin, float rotationCos, float depth, byte effects)
	{
		if (numSprites >= vertexInfo.Length)
		{
			int newSize = vertexInfo.Length + 2048;
			Array.Resize(ref vertexInfo, newSize);
			Array.Resize(ref textureInfo, newSize);
			Array.Resize(ref spriteInfos, newSize);
			Array.Resize(ref sortedSpriteInfos, newSize);
		}
		if (sortMode == SpriteSortMode.Immediate)
		{
			int baseSprite;
			fixed (VertexPositionColorTexture4* ptr = &vertexInfo[0])
			{
				GenerateVertexInfo(ptr, sourceX, sourceY, sourceW, sourceH, destinationX, destinationY, destinationW, destinationH, color, originX, originY, rotationSin, rotationCos, depth, effects);
				if (supportsNoOverwrite)
				{
					baseSprite = UpdateVertexBuffer(0, 1);
				}
				else
				{
					baseSprite = 0;
					vertexBuffer.SetDataPointerEXT(0, (IntPtr)ptr, 96, SetDataOptions.None);
				}
			}
			DrawPrimitives(texture, baseSprite, 1);
			return;
		}
		if (sortMode == SpriteSortMode.Deferred)
		{
			fixed (VertexPositionColorTexture4* sprite = &vertexInfo[numSprites])
			{
				GenerateVertexInfo(sprite, sourceX, sourceY, sourceW, sourceH, destinationX, destinationY, destinationW, destinationH, color, originX, originY, rotationSin, rotationCos, depth, effects);
			}
			textureInfo[numSprites] = texture;
			numSprites++;
			return;
		}
		fixed (SpriteInfo* ptr2 = &spriteInfos[numSprites])
		{
			ptr2->textureHash = texture.GetHashCode();
			ptr2->sourceX = sourceX;
			ptr2->sourceY = sourceY;
			ptr2->sourceW = sourceW;
			ptr2->sourceH = sourceH;
			ptr2->destinationX = destinationX;
			ptr2->destinationY = destinationY;
			ptr2->destinationW = destinationW;
			ptr2->destinationH = destinationH;
			ptr2->color = color;
			ptr2->originX = originX;
			ptr2->originY = originY;
			ptr2->rotationSin = rotationSin;
			ptr2->rotationCos = rotationCos;
			ptr2->depth = depth;
			ptr2->effects = effects;
		}
		textureInfo[numSprites] = texture;
		numSprites++;
	}

	private unsafe void FlushBatch()
	{
		PrepRenderState();
		if (numSprites == 0)
		{
			return;
		}
		if (sortMode != SpriteSortMode.Deferred)
		{
			IComparer<IntPtr> comparer;
			if (sortMode != SpriteSortMode.Texture)
			{
				if (sortMode == SpriteSortMode.BackToFront)
				{
					IComparer<IntPtr> backToFrontCompare = BackToFrontCompare;
					comparer = backToFrontCompare;
				}
				else
				{
					IComparer<IntPtr> backToFrontCompare = FrontToBackCompare;
					comparer = backToFrontCompare;
				}
			}
			else
			{
				IComparer<IntPtr> backToFrontCompare = TextureCompare;
				comparer = backToFrontCompare;
			}
			IComparer<IntPtr> comparer2 = comparer;
			fixed (SpriteInfo* ptr = &spriteInfos[0])
			{
				fixed (IntPtr* ptr2 = &sortedSpriteInfos[0])
				{
					fixed (VertexPositionColorTexture4* ptr3 = &vertexInfo[0])
					{
						for (int i = 0; i < numSprites; i++)
						{
							ptr2[i] = (IntPtr)(ptr + i);
						}
						Array.Sort(sortedSpriteInfos, textureInfo, 0, numSprites, comparer2);
						for (int j = 0; j < numSprites; j++)
						{
							SpriteInfo* ptr4 = (SpriteInfo*)(void*)ptr2[j];
							GenerateVertexInfo(ptr3 + j, ptr4->sourceX, ptr4->sourceY, ptr4->sourceW, ptr4->sourceH, ptr4->destinationX, ptr4->destinationY, ptr4->destinationW, ptr4->destinationH, ptr4->color, ptr4->originX, ptr4->originY, ptr4->rotationSin, ptr4->rotationCos, ptr4->depth, ptr4->effects);
						}
					}
				}
			}
		}
		int num = 0;
		while (true)
		{
			int num2 = Math.Min(numSprites, 2048);
			int num3 = UpdateVertexBuffer(num, num2);
			int num4 = 0;
			Texture2D texture2D = textureInfo[num];
			for (int k = 1; k < num2; k++)
			{
				Texture2D texture2D2 = textureInfo[num + k];
				if (texture2D2 != texture2D)
				{
					DrawPrimitives(texture2D, num3 + num4, k - num4);
					texture2D = texture2D2;
					num4 = k;
				}
			}
			DrawPrimitives(texture2D, num3 + num4, num2 - num4);
			if (numSprites <= 2048)
			{
				break;
			}
			numSprites -= 2048;
			num += 2048;
		}
		numSprites = 0;
	}

	private unsafe int UpdateVertexBuffer(int start, int count)
	{
		int num;
		SetDataOptions options;
		if (bufferOffset + count > 2048 || !supportsNoOverwrite)
		{
			num = 0;
			options = SetDataOptions.Discard;
		}
		else
		{
			num = bufferOffset;
			options = SetDataOptions.NoOverwrite;
		}
		fixed (VertexPositionColorTexture4* ptr = &vertexInfo[start])
		{
			vertexBuffer.SetDataPointerEXT(num * 96, (IntPtr)ptr, count * 96, options);
		}
		bufferOffset = num + count;
		return num;
	}

	private unsafe static void GenerateVertexInfo(VertexPositionColorTexture4* sprite, float sourceX, float sourceY, float sourceW, float sourceH, float destinationX, float destinationY, float destinationW, float destinationH, Color color, float originX, float originY, float rotationSin, float rotationCos, float depth, byte effects)
	{
		float num = (0f - originX) * destinationW;
		float num2 = (0f - originY) * destinationH;
		sprite->Position0.X = (0f - rotationSin) * num2 + rotationCos * num + destinationX;
		sprite->Position0.Y = rotationCos * num2 + rotationSin * num + destinationY;
		num = (1f - originX) * destinationW;
		num2 = (0f - originY) * destinationH;
		sprite->Position1.X = (0f - rotationSin) * num2 + rotationCos * num + destinationX;
		sprite->Position1.Y = rotationCos * num2 + rotationSin * num + destinationY;
		num = (0f - originX) * destinationW;
		num2 = (1f - originY) * destinationH;
		sprite->Position2.X = (0f - rotationSin) * num2 + rotationCos * num + destinationX;
		sprite->Position2.Y = rotationCos * num2 + rotationSin * num + destinationY;
		num = (1f - originX) * destinationW;
		num2 = (1f - originY) * destinationH;
		sprite->Position3.X = (0f - rotationSin) * num2 + rotationCos * num + destinationX;
		sprite->Position3.Y = rotationCos * num2 + rotationSin * num + destinationY;
		fixed (float* ptr = &CornerOffsetX[0])
		{
			fixed (float* ptr2 = &CornerOffsetY[0])
			{
				sprite->TextureCoordinate0.X = ptr[0 ^ effects] * sourceW + sourceX;
				sprite->TextureCoordinate0.Y = ptr2[0 ^ effects] * sourceH + sourceY;
				sprite->TextureCoordinate1.X = ptr[1 ^ effects] * sourceW + sourceX;
				sprite->TextureCoordinate1.Y = ptr2[1 ^ effects] * sourceH + sourceY;
				sprite->TextureCoordinate2.X = ptr[2 ^ effects] * sourceW + sourceX;
				sprite->TextureCoordinate2.Y = ptr2[2 ^ effects] * sourceH + sourceY;
				sprite->TextureCoordinate3.X = ptr[3 ^ effects] * sourceW + sourceX;
				sprite->TextureCoordinate3.Y = ptr2[3 ^ effects] * sourceH + sourceY;
			}
		}
		sprite->Position0.Z = depth;
		sprite->Position1.Z = depth;
		sprite->Position2.Z = depth;
		sprite->Position3.Z = depth;
		sprite->Color0 = color;
		sprite->Color1 = color;
		sprite->Color2 = color;
		sprite->Color3 = color;
	}

	private unsafe void PrepRenderState()
	{
		GraphicsDevice.BlendState = blendState;
		GraphicsDevice.SamplerStates[0] = samplerState;
		GraphicsDevice.DepthStencilState = depthStencilState;
		GraphicsDevice.RasterizerState = rasterizerState;
		GraphicsDevice.SetVertexBuffer(vertexBuffer);
		GraphicsDevice.Indices = indexBuffer;
		Viewport viewport = GraphicsDevice.Viewport;
		float num = (float)(2.0 / (double)viewport.Width);
		float num2 = (float)(-2.0 / (double)viewport.Height);
		float* ptr = (float*)(void*)spriteMatrixTransform;
		*ptr = num * transformMatrix.M11 - transformMatrix.M14;
		ptr[1] = num * transformMatrix.M21 - transformMatrix.M24;
		ptr[2] = num * transformMatrix.M31 - transformMatrix.M34;
		ptr[3] = num * transformMatrix.M41 - transformMatrix.M44;
		ptr[4] = num2 * transformMatrix.M12 + transformMatrix.M14;
		ptr[5] = num2 * transformMatrix.M22 + transformMatrix.M24;
		ptr[6] = num2 * transformMatrix.M32 + transformMatrix.M34;
		ptr[7] = num2 * transformMatrix.M42 + transformMatrix.M44;
		ptr[8] = transformMatrix.M13;
		ptr[9] = transformMatrix.M23;
		ptr[10] = transformMatrix.M33;
		ptr[11] = transformMatrix.M43;
		ptr[12] = transformMatrix.M14;
		ptr[13] = transformMatrix.M24;
		ptr[14] = transformMatrix.M34;
		ptr[15] = transformMatrix.M44;
		spriteEffectPass.Apply();
	}

	private void DrawPrimitives(Texture texture, int baseSprite, int batchSize)
	{
		GraphicsDevice.Textures[0] = texture;
		if (customEffect != null)
		{
			foreach (EffectPass pass in customEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite * 4, 0, batchSize * 4, 0, batchSize * 2);
			}
			return;
		}
		GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite * 4, 0, batchSize * 4, 0, batchSize * 2);
	}

	private void CheckBegin(string method)
	{
		if (!beginCalled)
		{
			throw new InvalidOperationException(method + " was called, but Begin has not yet been called. Begin must be called successfully before you can call " + method + ".");
		}
	}

	private static short[] GenerateIndexArray()
	{
		short[] array = new short[12288];
		int num = 0;
		int num2 = 0;
		while (num < 12288)
		{
			array[num] = (short)num2;
			array[num + 1] = (short)(num2 + 1);
			array[num + 2] = (short)(num2 + 2);
			array[num + 3] = (short)(num2 + 3);
			array[num + 4] = (short)(num2 + 2);
			array[num + 5] = (short)(num2 + 1);
			num += 6;
			num2 += 4;
		}
		return array;
	}
}
