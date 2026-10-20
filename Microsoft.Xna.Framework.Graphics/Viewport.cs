using System;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public struct Viewport
{
	internal FNA3D.FNA3D_Viewport viewport;

	public int Height
	{
		get
		{
			return viewport.h;
		}
		set
		{
			viewport.h = value;
		}
	}

	public float MaxDepth
	{
		get
		{
			return viewport.maxDepth;
		}
		set
		{
			viewport.maxDepth = value;
		}
	}

	public float MinDepth
	{
		get
		{
			return viewport.minDepth;
		}
		set
		{
			viewport.minDepth = value;
		}
	}

	public int Width
	{
		get
		{
			return viewport.w;
		}
		set
		{
			viewport.w = value;
		}
	}

	public int Y
	{
		get
		{
			return viewport.y;
		}
		set
		{
			viewport.y = value;
		}
	}

	public int X
	{
		get
		{
			return viewport.x;
		}
		set
		{
			viewport.x = value;
		}
	}

	public float AspectRatio
	{
		get
		{
			if (viewport.h != 0 && viewport.w != 0)
			{
				return (float)viewport.w / (float)viewport.h;
			}
			return 0f;
		}
	}

	public Rectangle Bounds
	{
		get
		{
			return new Rectangle(viewport.x, viewport.y, viewport.w, viewport.h);
		}
		set
		{
			viewport.x = value.X;
			viewport.y = value.Y;
			viewport.w = value.Width;
			viewport.h = value.Height;
		}
	}

	public Rectangle TitleSafeArea => Bounds;

	public Viewport(int x, int y, int width, int height)
	{
		viewport.x = x;
		viewport.y = y;
		viewport.w = width;
		viewport.h = height;
		viewport.minDepth = 0f;
		viewport.maxDepth = 1f;
	}

	public Viewport(Rectangle bounds)
	{
		viewport.x = bounds.X;
		viewport.y = bounds.Y;
		viewport.w = bounds.Width;
		viewport.h = bounds.Height;
		viewport.minDepth = 0f;
		viewport.maxDepth = 1f;
	}

	public Vector3 Project(Vector3 source, Matrix projection, Matrix view, Matrix world)
	{
		Matrix matrix = Matrix.Multiply(Matrix.Multiply(world, view), projection);
		Vector3 result = Vector3.Transform(source, matrix);
		float num = source.X * matrix.M14 + source.Y * matrix.M24 + source.Z * matrix.M34 + matrix.M44;
		if (!MathHelper.WithinEpsilon(num, 1f))
		{
			result.X /= num;
			result.Y /= num;
			result.Z /= num;
		}
		result.X = (result.X + 1f) * 0.5f * (float)Width + (float)X;
		result.Y = (0f - result.Y + 1f) * 0.5f * (float)Height + (float)Y;
		result.Z = result.Z * (MaxDepth - MinDepth) + MinDepth;
		return result;
	}

	public Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
	{
		Matrix matrix = Matrix.Invert(Matrix.Multiply(Matrix.Multiply(world, view), projection));
		source.X = (source.X - (float)X) / (float)Width * 2f - 1f;
		source.Y = 0f - ((source.Y - (float)Y) / (float)Height * 2f - 1f);
		source.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);
		Vector3 result = Vector3.Transform(source, matrix);
		float num = source.X * matrix.M14 + source.Y * matrix.M24 + source.Z * matrix.M34 + matrix.M44;
		if (!MathHelper.WithinEpsilon(num, 1f))
		{
			result.X /= num;
			result.Y /= num;
			result.Z /= num;
		}
		return result;
	}

	public override string ToString()
	{
		return "{X:" + viewport.x + " Y:" + viewport.y + " Width:" + viewport.w + " Height:" + viewport.h + " MinDepth:" + viewport.minDepth + " MaxDepth:" + viewport.maxDepth + "}";
	}
}
