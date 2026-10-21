using System;

namespace Terraria.Textures;

/// <summary>
/// A decoded image: 8-bit RGBA, row-major, top row first.
///
/// One fixed format on purpose. Every source (PNG of any colour type and bit depth, and later
/// the XNB surfaces the tool already decodes) is normalised into this, so the code that uploads
/// to the GPU never needs to know where the pixels came from, and a test can compare pixels
/// without going near a graphics device.
/// </summary>
internal sealed class ImageData
{
	/// <summary>Bytes per pixel in <see cref="Pixels"/>.</summary>
	public const int BytesPerPixel = 4;

	public readonly int Width;

	public readonly int Height;

	/// <summary>RGBA, 4 bytes per pixel, <c>Width * Height * 4</c> long.</summary>
	public readonly byte[] Pixels;

	public ImageData(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentOutOfRangeException("width", "an image must be at least 1x1, got " + width + "x" + height);
		}
		Width = width;
		Height = height;
		Pixels = new byte[checked(width * height * BytesPerPixel)];
	}

	/// <summary>Offset of one pixel in <see cref="Pixels"/>.</summary>
	public int Offset(int x, int y)
	{
		return (y * Width + x) * BytesPerPixel;
	}

	public byte R(int x, int y)
	{
		return Pixels[Offset(x, y)];
	}

	public byte G(int x, int y)
	{
		return Pixels[Offset(x, y) + 1];
	}

	public byte B(int x, int y)
	{
		return Pixels[Offset(x, y) + 2];
	}

	public byte A(int x, int y)
	{
		return Pixels[Offset(x, y) + 3];
	}

	public void Set(int x, int y, byte r, byte g, byte b, byte a)
	{
		int num = Offset(x, y);
		Pixels[num] = r;
		Pixels[num + 1] = g;
		Pixels[num + 2] = b;
		Pixels[num + 3] = a;
	}

	/// <summary>One line for a log: "Images/Tiles_0.png 64x64 RGBA".</summary>
	public string Describe()
	{
		return Width + "x" + Height + " RGBA";
	}
}
