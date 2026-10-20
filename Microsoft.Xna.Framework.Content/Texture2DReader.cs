using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class Texture2DReader : ContentTypeReader<Texture2D>
{
	private static string[] supportedExtensions = new string[9] { ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tga", ".tif", ".tiff", ".dds" };

	internal Texture2DReader()
	{
	}

	internal static string Normalize(string fileName)
	{
		return ContentTypeReader.Normalize(fileName, supportedExtensions);
	}

	protected internal override Texture2D Read(ContentReader reader, Texture2D existingInstance)
	{
		Texture2D texture2D = null;
		SurfaceFormat surfaceFormat;
		if (reader.version >= 5)
		{
			surfaceFormat = (SurfaceFormat)reader.ReadInt32();
		}
		else
		{
			int num = reader.ReadInt32();
			if (1 == 0)
			{
			}
			SurfaceFormat surfaceFormat2 = num switch
			{
				1 => SurfaceFormat.ColorBgraEXT, 
				28 => SurfaceFormat.Dxt1, 
				30 => SurfaceFormat.Dxt3, 
				32 => SurfaceFormat.Dxt5, 
				_ => throw new NotSupportedException("Unsupported legacy surface format."), 
			};
			if (1 == 0)
			{
			}
			surfaceFormat = surfaceFormat2;
		}
		SurfaceFormat surfaceFormat3 = surfaceFormat;
		int num2 = reader.ReadInt32();
		int num3 = reader.ReadInt32();
		int num4 = reader.ReadInt32();
		int num5 = num4;
		GraphicsDevice graphicsDevice = reader.ContentManager.GetGraphicsDevice();
		SurfaceFormat surfaceFormat4 = surfaceFormat3;
		if (surfaceFormat3 == SurfaceFormat.Dxt1 && FNA3D.FNA3D_SupportsDXT1(graphicsDevice.GLDevice) == 0)
		{
			surfaceFormat4 = SurfaceFormat.Color;
		}
		else if ((surfaceFormat3 == SurfaceFormat.Dxt3 || surfaceFormat3 == SurfaceFormat.Dxt5) && FNA3D.FNA3D_SupportsS3TC(graphicsDevice.GLDevice) == 0)
		{
			surfaceFormat4 = SurfaceFormat.Color;
		}
		texture2D = ((existingInstance != null) ? existingInstance : new Texture2D(graphicsDevice, num2, num3, num5 > 1, surfaceFormat4));
		for (int i = 0; i < num4; i++)
		{
			int num6 = reader.ReadInt32();
			byte[] buffer = null;
			int width = num2 >> i;
			int height = num3 >> i;
			if (i >= num5)
			{
				continue;
			}
			if (reader.platform == 'x')
			{
				if (surfaceFormat3 == SurfaceFormat.Color || surfaceFormat3 == SurfaceFormat.ColorBgraEXT)
				{
					buffer = X360TexUtil.SwapColor(reader.ReadBytes(num6));
					num6 = buffer.Length;
				}
				else
				{
					switch (surfaceFormat3)
					{
					case SurfaceFormat.Dxt1:
						buffer = X360TexUtil.SwapDxt1(reader.ReadBytes(num6), width, height);
						num6 = buffer.Length;
						break;
					case SurfaceFormat.Dxt3:
						buffer = X360TexUtil.SwapDxt3(reader.ReadBytes(num6), width, height);
						num6 = buffer.Length;
						break;
					case SurfaceFormat.Dxt5:
						buffer = X360TexUtil.SwapDxt5(reader.ReadBytes(num6), width, height);
						num6 = buffer.Length;
						break;
					}
				}
			}
			if (surfaceFormat4 != surfaceFormat3)
			{
				if (buffer == null)
				{
					buffer = reader.ReadBytes(num6);
				}
				switch (surfaceFormat3)
				{
				case SurfaceFormat.Dxt1:
					buffer = DxtUtil.DecompressDxt1(buffer, width, height);
					break;
				case SurfaceFormat.Dxt3:
					buffer = DxtUtil.DecompressDxt3(buffer, width, height);
					break;
				case SurfaceFormat.Dxt5:
					buffer = DxtUtil.DecompressDxt5(buffer, width, height);
					break;
				}
				num6 = buffer.Length;
			}
			int startIndex = 0;
			if (buffer == null)
			{
				if (reader.BaseStream is MemoryStream && ((MemoryStream)reader.BaseStream).TryGetBuffer(out buffer))
				{
					startIndex = (int)reader.BaseStream.Seek(0L, SeekOrigin.Current);
					reader.BaseStream.Seek(num6, SeekOrigin.Current);
				}
				else
				{
					buffer = reader.ReadBytes(num6);
				}
			}
			texture2D.SetData(i, null, buffer, startIndex, num6);
		}
		return texture2D;
	}
}
