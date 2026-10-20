using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class TextureCube : Texture
{
	public int Size { get; private set; }

	public TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice");
		}
		GraphicsDevice = graphicsDevice;
		Size = size;
		LevelCount = ((!mipMap) ? 1 : Texture.CalculateMipLevels(size));
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
			else if (format != SurfaceFormat.Color && format != SurfaceFormat.Rgba1010102 && format != SurfaceFormat.Rg32 && format != SurfaceFormat.Rgba64 && format != SurfaceFormat.Single && format != SurfaceFormat.Vector2 && format != SurfaceFormat.Vector4 && format != SurfaceFormat.HalfSingle && format != SurfaceFormat.HalfVector2 && format != SurfaceFormat.HalfVector4 && format != SurfaceFormat.HdrBlendable)
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
		texture = FNA3D.FNA3D_CreateTextureCube(GraphicsDevice.GLDevice, Format, Size, LevelCount, (this is IRenderTarget) ? ((byte)1) : ((byte)0));
	}

	public void SetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
	{
		SetData(cubeMapFace, 0, null, data, 0, data.Length);
	}

	public void SetData<T>(CubeMapFace cubeMapFace, T[] data, int startIndex, int elementCount) where T : struct
	{
		SetData(cubeMapFace, 0, null, data, startIndex, elementCount);
	}

	public void SetData<T>(CubeMapFace cubeMapFace, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		if (data == null)
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
			w = Math.Max(1, Size >> level);
			h = Math.Max(1, Size >> level);
		}
		int num = Marshal.SizeOf(typeof(T));
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_SetTextureDataCube(GraphicsDevice.GLDevice, texture, x, y, w, h, cubeMapFace, level, gCHandle.AddrOfPinnedObject() + startIndex * num, elementCount * num);
		gCHandle.Free();
	}

	public void SetDataPointerEXT(CubeMapFace cubeMapFace, int level, Rectangle? rect, IntPtr data, int dataLength)
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
			w = Math.Max(1, Size >> level);
			h = Math.Max(1, Size >> level);
		}
		FNA3D.FNA3D_SetTextureDataCube(GraphicsDevice.GLDevice, texture, x, y, w, h, cubeMapFace, level, data, dataLength);
	}

	public void GetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
	{
		GetData(cubeMapFace, 0, null, data, 0, data.Length);
	}

	public void GetData<T>(CubeMapFace cubeMapFace, T[] data, int startIndex, int elementCount) where T : struct
	{
		GetData(cubeMapFace, 0, null, data, startIndex, elementCount);
	}

	public void GetData<T>(CubeMapFace cubeMapFace, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		if (data == null || data.Length == 0)
		{
			throw new ArgumentException("data cannot be null");
		}
		if (data.Length < startIndex + elementCount)
		{
			throw new ArgumentException("The data passed has a length of " + data.Length + " but " + elementCount + " pixels have been requested.");
		}
		int x;
		int y;
		int w;
		int h;
		if (!rect.HasValue)
		{
			x = 0;
			y = 0;
			w = Size >> level;
			h = Size >> level;
		}
		else
		{
			x = rect.Value.X;
			y = rect.Value.Y;
			w = rect.Value.Width;
			h = rect.Value.Height;
		}
		int num = Marshal.SizeOf(typeof(T));
		Texture.ValidateGetDataFormat(Format, num);
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_GetTextureDataCube(GraphicsDevice.GLDevice, texture, x, y, w, h, cubeMapFace, level, gCHandle.AddrOfPinnedObject() + startIndex * num, elementCount * num);
		gCHandle.Free();
	}

	public static TextureCube DDSFromStreamEXT(GraphicsDevice graphicsDevice, Stream stream)
	{
		TextureCube textureCube;
		using (BinaryReader binaryReader = new BinaryReader(stream))
		{
			Texture.ParseDDS(binaryReader, out var format, out var width, out var _, out var levels, out var _);
			textureCube = new TextureCube(graphicsDevice, width, levels > 1, format);
			byte[] buffer = null;
			if (stream is MemoryStream && ((MemoryStream)stream).TryGetBuffer(out buffer))
			{
				for (int i = 0; i < 6; i++)
				{
					for (int j = 0; j < levels; j++)
					{
						int num = Texture.CalculateDDSLevelSize(width >> j, width >> j, format);
						textureCube.SetData((CubeMapFace)i, j, null, buffer, (int)stream.Seek(0L, SeekOrigin.Current), num);
						stream.Seek(num, SeekOrigin.Current);
					}
				}
			}
			else
			{
				for (int k = 0; k < 6; k++)
				{
					for (int l = 0; l < levels; l++)
					{
						buffer = binaryReader.ReadBytes(Texture.CalculateDDSLevelSize(width >> l, width >> l, format));
						textureCube.SetData((CubeMapFace)k, l, null, buffer, 0, buffer.Length);
					}
				}
			}
		}
		return textureCube;
	}
}
