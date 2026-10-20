using System;
using System.IO;
using System.Threading;

namespace Microsoft.Xna.Framework.Graphics;

public abstract class Texture : GraphicsResource
{
	internal IntPtr texture;

	public SurfaceFormat Format { get; protected set; }

	public int LevelCount { get; protected set; }

	public new string Name
	{
		get
		{
			return base.Name;
		}
		set
		{
			if (!(value == base.Name))
			{
				base.Name = value;
				if (value == null)
				{
					value = string.Empty;
				}
				FNA3D.FNA3D_SetTextureName(GraphicsDevice.GLDevice, texture, value);
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			GraphicsDevice.Textures.RemoveDisposedTexture(this);
			GraphicsDevice.VertexTextures.RemoveDisposedTexture(this);
			IntPtr intPtr = Interlocked.Exchange(ref texture, IntPtr.Zero);
			if (intPtr != IntPtr.Zero)
			{
				FNA3D.FNA3D_AddDisposeTexture(GraphicsDevice.GLDevice, intPtr);
			}
		}
		base.Dispose(disposing);
	}

	protected internal override void GraphicsDeviceResetting()
	{
	}

	public static int GetBlockSizeSquaredEXT(SurfaceFormat format)
	{
		switch (format)
		{
		case SurfaceFormat.Dxt1:
		case SurfaceFormat.Dxt3:
		case SurfaceFormat.Dxt5:
		case SurfaceFormat.Dxt5SrgbEXT:
		case SurfaceFormat.Bc7EXT:
		case SurfaceFormat.Bc7SrgbEXT:
			return 16;
		case SurfaceFormat.Color:
		case SurfaceFormat.Bgr565:
		case SurfaceFormat.Bgra5551:
		case SurfaceFormat.Bgra4444:
		case SurfaceFormat.NormalizedByte2:
		case SurfaceFormat.NormalizedByte4:
		case SurfaceFormat.Rgba1010102:
		case SurfaceFormat.Rg32:
		case SurfaceFormat.Rgba64:
		case SurfaceFormat.Alpha8:
		case SurfaceFormat.Single:
		case SurfaceFormat.Vector2:
		case SurfaceFormat.Vector4:
		case SurfaceFormat.HalfSingle:
		case SurfaceFormat.HalfVector2:
		case SurfaceFormat.HalfVector4:
		case SurfaceFormat.HdrBlendable:
		case SurfaceFormat.ColorBgraEXT:
		case SurfaceFormat.ColorSrgbEXT:
		case SurfaceFormat.ByteEXT:
		case SurfaceFormat.UShortEXT:
			return 1;
		default:
			throw new ArgumentException("Should be a value defined in SurfaceFormat", "Format");
		}
	}

	public static int GetFormatSizeEXT(SurfaceFormat format)
	{
		switch (format)
		{
		case SurfaceFormat.Dxt1:
			return 8;
		case SurfaceFormat.Dxt3:
		case SurfaceFormat.Dxt5:
		case SurfaceFormat.Dxt5SrgbEXT:
		case SurfaceFormat.Bc7EXT:
		case SurfaceFormat.Bc7SrgbEXT:
			return 16;
		case SurfaceFormat.Alpha8:
		case SurfaceFormat.ByteEXT:
			return 1;
		case SurfaceFormat.Bgr565:
		case SurfaceFormat.Bgra5551:
		case SurfaceFormat.Bgra4444:
		case SurfaceFormat.NormalizedByte2:
		case SurfaceFormat.HalfSingle:
		case SurfaceFormat.UShortEXT:
			return 2;
		case SurfaceFormat.Color:
		case SurfaceFormat.NormalizedByte4:
		case SurfaceFormat.Rgba1010102:
		case SurfaceFormat.Rg32:
		case SurfaceFormat.Single:
		case SurfaceFormat.HalfVector2:
		case SurfaceFormat.ColorBgraEXT:
		case SurfaceFormat.ColorSrgbEXT:
			return 4;
		case SurfaceFormat.Rgba64:
		case SurfaceFormat.Vector2:
		case SurfaceFormat.HalfVector4:
		case SurfaceFormat.HdrBlendable:
			return 8;
		case SurfaceFormat.Vector4:
			return 16;
		default:
			throw new ArgumentException("Should be a value defined in SurfaceFormat", "Format");
		}
	}

	internal static int GetPixelStoreAlignment(SurfaceFormat format)
	{
		return Math.Min(8, GetFormatSizeEXT(format));
	}

	internal static void ValidateGetDataFormat(SurfaceFormat format, int elementSizeInBytes)
	{
		if (GetFormatSizeEXT(format) % elementSizeInBytes != 0)
		{
			throw new ArgumentException("The type you are using for T in this method is an invalid size for this resource");
		}
	}

	internal static int CalculateMipLevels(int width, int height = 0, int depth = 0)
	{
		int num = 1;
		int num2 = Math.Max(Math.Max(width, height), depth);
		while (num2 > 1)
		{
			num2 /= 2;
			num++;
		}
		return num;
	}

	internal static int CalculateDDSLevelSize(int width, int height, SurfaceFormat format)
	{
		if (format == SurfaceFormat.Color || format == SurfaceFormat.ColorBgraEXT)
		{
			return (width * 32 + 7) / 8 * height;
		}
		switch (format)
		{
		case SurfaceFormat.HalfVector4:
			return (width * 64 + 7) / 8 * height;
		case SurfaceFormat.Vector4:
			return (width * 128 + 7) / 8 * height;
		default:
		{
			int num = 16;
			if (format == SurfaceFormat.Dxt1)
			{
				num = 8;
			}
			width = Math.Max(width, 1);
			height = Math.Max(height, 1);
			return (width + 3) / 4 * ((height + 3) / 4) * num;
		}
		}
	}

	internal static void ParseDDS(BinaryReader reader, out SurfaceFormat format, out int width, out int height, out int levels, out bool isCube)
	{
		if (reader.ReadUInt32() != 542327876)
		{
			throw new NotSupportedException("Not a DDS!");
		}
		uint num = reader.ReadUInt32();
		if (num != 124)
		{
			throw new NotSupportedException("Invalid DDS header!");
		}
		uint num2 = reader.ReadUInt32();
		if ((num2 & 6) != 6)
		{
			throw new NotSupportedException("Invalid DDS flags!");
		}
		if ((num2 & 0x80008) == 524296)
		{
			throw new NotSupportedException("Invalid DDS flags!");
		}
		height = reader.ReadInt32();
		width = reader.ReadInt32();
		reader.ReadUInt32();
		reader.ReadUInt32();
		levels = reader.ReadInt32();
		reader.ReadBytes(44);
		uint num3 = reader.ReadUInt32();
		if (num3 != 32)
		{
			throw new NotSupportedException("Bogus PIXFMTSIZE!");
		}
		uint num4 = reader.ReadUInt32();
		uint num5 = reader.ReadUInt32();
		uint num6 = reader.ReadUInt32();
		uint num7 = reader.ReadUInt32();
		uint num8 = reader.ReadUInt32();
		uint num9 = reader.ReadUInt32();
		uint num10 = reader.ReadUInt32();
		uint num11 = reader.ReadUInt32();
		if ((num11 & 0x1000) == 0)
		{
			throw new NotSupportedException("Not a texture!");
		}
		isCube = false;
		uint num12 = reader.ReadUInt32();
		if (num12 != 0)
		{
			if ((num12 & 0x200) != 512)
			{
				throw new NotSupportedException("Invalid caps2!");
			}
			isCube = true;
		}
		reader.ReadUInt32();
		reader.ReadUInt32();
		reader.ReadUInt32();
		if ((num11 & 0x400000) != 4194304)
		{
			levels = 1;
		}
		if ((num4 & 4) == 4)
		{
			switch (num5)
			{
			case 113u:
				format = SurfaceFormat.HalfVector4;
				break;
			case 116u:
				format = SurfaceFormat.Vector4;
				break;
			case 827611204u:
				format = SurfaceFormat.Dxt1;
				break;
			case 861165636u:
				format = SurfaceFormat.Dxt3;
				break;
			case 894720068u:
				format = SurfaceFormat.Dxt5;
				break;
			case 808540228u:
			{
				switch (reader.ReadUInt32())
				{
				case 2u:
					format = SurfaceFormat.Vector4;
					break;
				case 10u:
					format = SurfaceFormat.HalfVector4;
					break;
				case 71u:
					format = SurfaceFormat.Dxt1;
					break;
				case 74u:
					format = SurfaceFormat.Dxt3;
					break;
				case 77u:
					format = SurfaceFormat.Dxt5;
					break;
				case 98u:
					format = SurfaceFormat.Bc7EXT;
					break;
				case 99u:
					format = SurfaceFormat.Bc7SrgbEXT;
					break;
				default:
					throw new NotSupportedException("Unsupported DDS texture format");
				}
				uint num13 = reader.ReadUInt32();
				uint num14 = num13;
				uint num15 = num14;
				if (num15 <= 1)
				{
					throw new NotSupportedException("Unsupported DDS texture format");
				}
				reader.ReadUInt32();
				uint num16 = reader.ReadUInt32();
				if (num16 > 1)
				{
					throw new NotSupportedException("Unsupported DDS texture format");
				}
				reader.ReadUInt32();
				break;
			}
			default:
				throw new NotSupportedException("Unsupported DDS texture format");
			}
			return;
		}
		if ((num4 & 0x40) == 64)
		{
			if (num6 != 32)
			{
				throw new NotSupportedException("Unsupported DDS texture format: Alpha channel required");
			}
			bool flag = num7 == 16711680 && num8 == 65280 && num9 == 255 && num10 == 4278190080u;
			bool flag2 = num7 == 255 && num8 == 65280 && num9 == 16711680 && num10 == 4278190080u;
			if (flag)
			{
				format = SurfaceFormat.ColorBgraEXT;
				return;
			}
			if (flag2)
			{
				format = SurfaceFormat.Color;
				return;
			}
			throw new NotSupportedException("Unsupported DDS texture format: Only RGBA and BGRA are supported");
		}
		throw new NotSupportedException("Unsupported DDS texture format");
	}
}
