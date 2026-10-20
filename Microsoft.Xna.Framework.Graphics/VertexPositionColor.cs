using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionColor : IVertexType
{
	public Vector3 Position;

	public Color Color;

	public static readonly VertexDeclaration VertexDeclaration;

	VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

	static VertexPositionColor()
	{
		VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0));
	}

	public VertexPositionColor(Vector3 position, Color color)
	{
		Position = position;
		Color = color;
	}

	public override int GetHashCode()
	{
		return 0;
	}

	public override string ToString()
	{
		return "{{Position:" + Position.ToString() + " Color:" + Color.ToString() + "}}";
	}

	public static bool operator ==(VertexPositionColor left, VertexPositionColor right)
	{
		return left.Color == right.Color && left.Position == right.Position;
	}

	public static bool operator !=(VertexPositionColor left, VertexPositionColor right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return this == (VertexPositionColor)obj;
	}
}
