using System;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public class DisplayMode
{
	public float AspectRatio => (float)Width / (float)Height;

	public SurfaceFormat Format { get; private set; }

	public int Height { get; private set; }

	public int Width { get; private set; }

	public Rectangle TitleSafeArea => new Rectangle(0, 0, Width, Height);

	internal DisplayMode(int width, int height, SurfaceFormat format)
	{
		Width = width;
		Height = height;
		Format = format;
	}

	public static bool operator !=(DisplayMode left, DisplayMode right)
	{
		return !(left == right);
	}

	public static bool operator ==(DisplayMode left, DisplayMode right)
	{
		if ((object)left == right)
		{
			return true;
		}
		if ((object)left == null || (object)right == null)
		{
			return false;
		}
		return left.Format == right.Format && left.Height == right.Height && left.Width == right.Width;
	}

	public override bool Equals(object obj)
	{
		return obj as DisplayMode == this;
	}

	public override int GetHashCode()
	{
		return Width.GetHashCode() ^ Height.GetHashCode() ^ Format.GetHashCode();
	}

	public override string ToString()
	{
		return "{{Width:" + Width + " Height:" + Height + " Format:" + Format.ToString() + "}}";
	}
}
