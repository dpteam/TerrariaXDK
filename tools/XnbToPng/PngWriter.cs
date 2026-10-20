using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace XnbToPng;

/// <summary>
/// Writes a decoded RGBA image as PNG.
///
/// System.Drawing is used instead of a hand-rolled PNG encoder: the tool targets net40 on
/// Windows anyway, and the point is the decoding, not the file format.
/// </summary>
internal static class PngWriter
{
	/// <summary>Writes RGBA bytes (top-down, 4 bytes per pixel) to <paramref name="path"/>.</summary>
	public static void Write(string path, byte[] rgba, int width, int height)
	{
		string directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
		using Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		try
		{
			// GDI+ stores BGRA in memory, while the decoders produce RGBA.
			byte[] array = new byte[rgba.Length];
			for (int i = 0; i < rgba.Length; i += 4)
			{
				array[i] = rgba[i + 2];
				array[i + 1] = rgba[i + 1];
				array[i + 2] = rgba[i];
				array[i + 3] = rgba[i + 3];
			}
			if (bitmapData.Stride == width * 4)
			{
				System.Runtime.InteropServices.Marshal.Copy(array, 0, bitmapData.Scan0, array.Length);
			}
			else
			{
				// Padding between rows: copy line by line.
				for (int j = 0; j < height; j++)
				{
					IntPtr destination = new IntPtr(bitmapData.Scan0.ToInt64() + j * bitmapData.Stride);
					System.Runtime.InteropServices.Marshal.Copy(array, j * width * 4, destination, width * 4);
				}
			}
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
		bitmap.Save(path, ImageFormat.Png);
	}
}
