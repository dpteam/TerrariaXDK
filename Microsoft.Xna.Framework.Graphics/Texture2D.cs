using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class Texture2D : Texture
{
	public int Width { get; private set; }

	public int Height { get; private set; }

	public Rectangle Bounds => new Rectangle(0, 0, Width, Height);

	public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
		: this(graphicsDevice, width, height, mipMap: false, SurfaceFormat.Color)
	{
	}

	public Texture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat format)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice");
		}
		GraphicsDevice = graphicsDevice;
		Width = width;
		Height = height;
		LevelCount = ((!mipMap) ? 1 : Texture.CalculateMipLevels(width, height));
		if (this is IRenderTarget)
		{
			if (format == SurfaceFormat.ColorSrgbEXT)
			{
				if (FNA3D.FNA3D_SupportsSRGBRenderTargets(GraphicsDevice.GLDevice) == 0)
				{
					Format = SurfaceFormat.Color;
				}
				else
				{
					Format = format;
				}
			}
			else if (format != SurfaceFormat.Color && format != SurfaceFormat.Rgba1010102 && format != SurfaceFormat.Rg32 && format != SurfaceFormat.Rgba64 && format != SurfaceFormat.Single && format != SurfaceFormat.Vector2 && format != SurfaceFormat.Vector4 && format != SurfaceFormat.HalfSingle && format != SurfaceFormat.HalfVector2 && format != SurfaceFormat.HalfVector4 && format != SurfaceFormat.HdrBlendable && format != SurfaceFormat.ByteEXT && format != SurfaceFormat.UShortEXT)
			{
				Format = SurfaceFormat.Color;
			}
			else
			{
				Format = format;
			}
		}
		else
		{
			Format = format;
		}
		texture = FNA3D.FNA3D_CreateTexture2D(GraphicsDevice.GLDevice, Format, Width, Height, LevelCount, (this is IRenderTarget) ? ((byte)1) : ((byte)0));
	}

	public void SetData<T>(T[] data) where T : struct
	{
		SetData(0, null, data, 0, data.Length);
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		SetData(0, null, data, startIndex, elementCount);
	}

	public void SetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex");
		}
		if (data.Length < elementCount + startIndex)
		{
			throw new ArgumentOutOfRangeException("elementCount");
		}
		int x;
		int y;
		int num;
		int num2;
		if (rect.HasValue)
		{
			x = rect.Value.X;
			y = rect.Value.Y;
			num = rect.Value.Width;
			num2 = rect.Value.Height;
		}
		else
		{
			x = 0;
			y = 0;
			num = Math.Max(Width >> level, 1);
			num2 = Math.Max(Height >> level, 1);
		}
		int num3 = MarshalHelper.SizeOf<T>();
		int num4 = num * num2 * Texture.GetFormatSizeEXT(Format) / Texture.GetBlockSizeSquaredEXT(Format);
		int num5 = elementCount * num3;
		if (num4 > num5)
		{
			throw new ArgumentOutOfRangeException("rect", "The region you are trying to upload is larger than the amount of data you provided.");
		}
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_SetTextureData2D(GraphicsDevice.GLDevice, texture, x, y, num, num2, level, gCHandle.AddrOfPinnedObject() + startIndex * num3, elementCount * num3);
		gCHandle.Free();
	}

	public void SetDataPointerEXT(int level, Rectangle? rect, IntPtr data, int dataLength)
	{
		if (data == IntPtr.Zero)
		{
			throw new ArgumentNullException("data");
		}
		int x;
		int y;
		int w;
		int h;
		if (rect.HasValue)
		{
			x = rect.Value.X;
			y = rect.Value.Y;
			w = rect.Value.Width;
			h = rect.Value.Height;
		}
		else
		{
			x = 0;
			y = 0;
			w = Math.Max(Width >> level, 1);
			h = Math.Max(Height >> level, 1);
		}
		FNA3D.FNA3D_SetTextureData2D(GraphicsDevice.GLDevice, texture, x, y, w, h, level, data, dataLength);
	}

	public void GetData<T>(T[] data) where T : struct
	{
		GetData(0, null, data, 0, data.Length);
	}

	public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		GetData(0, null, data, startIndex, elementCount);
	}

	public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		if (data == null || data.Length == 0)
		{
			throw new ArgumentException("data cannot be null");
		}
		if (data.Length < startIndex + elementCount)
		{
			throw new ArgumentException("The data passed has a length of " + data.Length + " but " + elementCount + " pixels have been requested.");
		}
		int num = MarshalHelper.SizeOf<T>();
		Texture.ValidateGetDataFormat(Format, num);
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		GetDataPointerEXT(level, rect, gCHandle.AddrOfPinnedObject() + startIndex * num, elementCount * num);
		gCHandle.Free();
	}

	public void GetDataPointerEXT(int level, Rectangle? rect, IntPtr data, int dataLengthBytes)
	{
		int x;
		int y;
		int w;
		int h;
		if (!rect.HasValue)
		{
			x = 0;
			y = 0;
			w = Width >> level;
			h = Height >> level;
		}
		else
		{
			x = rect.Value.X;
			y = rect.Value.Y;
			w = rect.Value.Width;
			h = rect.Value.Height;
		}
		FNA3D.FNA3D_GetTextureData2D(GraphicsDevice.GLDevice, texture, x, y, w, h, level, data, dataLengthBytes);
	}

	public void SaveAsJpeg(Stream stream, int width, int height)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("FNA_GRAPHICS_JPEG_SAVE_QUALITY");
		if (string.IsNullOrEmpty(environmentVariable) || !int.TryParse(environmentVariable, out var result))
		{
			result = 100;
		}
		int num = Width * Height * Texture.GetFormatSizeEXT(Format);
		IntPtr intPtr = FNAPlatform.Malloc(num);
		FNA3D.FNA3D_GetTextureData2D(GraphicsDevice.GLDevice, texture, 0, 0, Width, Height, 0, intPtr, num);
		FNA3D.WriteJPGStream(stream, Width, Height, width, height, intPtr, result);
		FNAPlatform.Free(intPtr);
	}

	public void SaveAsPng(Stream stream, int width, int height)
	{
		int num = Width * Height * Texture.GetFormatSizeEXT(Format);
		IntPtr intPtr = FNAPlatform.Malloc(num);
		FNA3D.FNA3D_GetTextureData2D(GraphicsDevice.GLDevice, texture, 0, 0, Width, Height, 0, intPtr, num);
		FNA3D.WritePNGStream(stream, Width, Height, width, height, intPtr);
		FNAPlatform.Free(intPtr);
	}

	public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
	{
		if (stream.CanSeek && stream.Position == stream.Length)
		{
			stream.Seek(0L, SeekOrigin.Begin);
		}
		IntPtr intPtr = FNA3D.ReadImageStream(stream, out var width, out var height, out var len);
		if (intPtr == IntPtr.Zero || width <= 0 || height <= 0)
		{
			throw new Exception("Decoding image failed!");
		}
		Texture2D texture2D = new Texture2D(graphicsDevice, width, height);
		texture2D.SetDataPointerEXT(0, null, intPtr, len);
		FNA3D.FNA3D_Image_Free(intPtr);
		return texture2D;
	}

	public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream, int width, int height, bool zoom)
	{
		if (stream.CanSeek && stream.Position == stream.Length)
		{
			stream.Seek(0L, SeekOrigin.Begin);
		}
		IntPtr intPtr = FNA3D.ReadImageStream(stream, out var width2, out var height2, out var len, width, height, zoom);
		if (intPtr == IntPtr.Zero || width2 <= 0 || height2 <= 0)
		{
			throw new Exception("Decoding image failed!");
		}
		Texture2D texture2D = new Texture2D(graphicsDevice, width2, height2);
		texture2D.SetDataPointerEXT(0, null, intPtr, len);
		FNA3D.FNA3D_Image_Free(intPtr);
		return texture2D;
	}

	public static void TextureDataFromStreamEXT(Stream stream, out int width, out int height, out byte[] pixels, int requestedWidth = -1, int requestedHeight = -1, bool zoom = false)
	{
		if (stream.CanSeek && stream.Position == stream.Length)
		{
			stream.Seek(0L, SeekOrigin.Begin);
		}
		IntPtr intPtr = FNA3D.ReadImageStream(stream, out width, out height, out var len, requestedWidth, requestedHeight, zoom);
		pixels = new byte[len];
		Marshal.Copy(intPtr, pixels, 0, len);
		FNA3D.FNA3D_Image_Free(intPtr);
	}

	public static Texture2D DDSFromStreamEXT(GraphicsDevice graphicsDevice, Stream stream)
	{
		Texture2D texture2D;
		using (BinaryReader binaryReader = new BinaryReader(stream))
		{
			Texture.ParseDDS(binaryReader, out var format, out var width, out var height, out var levels, out var isCube);
			if (isCube)
			{
				throw new FormatException("This file contains cube map data!");
			}
			texture2D = new Texture2D(graphicsDevice, width, height, levels > 1, format);
			byte[] buffer = null;
			if (stream is MemoryStream && ((MemoryStream)stream).TryGetBuffer(out buffer))
			{
				for (int i = 0; i < levels; i++)
				{
					int num = Texture.CalculateDDSLevelSize(width >> i, height >> i, format);
					texture2D.SetData(i, null, buffer, (int)stream.Seek(0L, SeekOrigin.Current), num);
					stream.Seek(num, SeekOrigin.Current);
				}
			}
			else
			{
				for (int j = 0; j < levels; j++)
				{
					buffer = binaryReader.ReadBytes(Texture.CalculateDDSLevelSize(width >> j, height >> j, format));
					texture2D.SetData(j, null, buffer, 0, buffer.Length);
				}
			}
		}
		return texture2D;
	}
}
