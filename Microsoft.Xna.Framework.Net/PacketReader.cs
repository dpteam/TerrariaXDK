using System.IO;

namespace Microsoft.Xna.Framework.Net;

public class PacketReader : BinaryReader
{
	public int Length => (int)BaseStream.Length;

	public int Position
	{
		get
		{
			return (int)BaseStream.Position;
		}
		set
		{
			BaseStream.Position = value;
		}
	}

	public PacketReader()
		: base(new MemoryStream())
	{
	}

	public PacketReader(int capacity)
		: base(new MemoryStream(capacity))
	{
	}

	public Color ReadColor()
	{
		float r = ReadSingle();
		float g = ReadSingle();
		float b = ReadSingle();
		float alpha = ReadSingle();
		return new Color(r, g, b, alpha);
	}

	public Matrix ReadMatrix()
	{
		float m = ReadSingle();
		float m2 = ReadSingle();
		float m3 = ReadSingle();
		float m4 = ReadSingle();
		float m5 = ReadSingle();
		float m6 = ReadSingle();
		float m7 = ReadSingle();
		float m8 = ReadSingle();
		float m9 = ReadSingle();
		float m10 = ReadSingle();
		float m11 = ReadSingle();
		float m12 = ReadSingle();
		float m13 = ReadSingle();
		float m14 = ReadSingle();
		float m15 = ReadSingle();
		float m16 = ReadSingle();
		return new Matrix(m, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, m13, m14, m15, m16);
	}

	public Quaternion ReadQuaternion()
	{
		float x = ReadSingle();
		float y = ReadSingle();
		float z = ReadSingle();
		float w = ReadSingle();
		return new Quaternion(x, y, z, w);
	}

	public Vector2 ReadVector2()
	{
		float x = ReadSingle();
		float y = ReadSingle();
		return new Vector2(x, y);
	}

	public Vector3 ReadVector3()
	{
		float x = ReadSingle();
		float y = ReadSingle();
		float z = ReadSingle();
		return new Vector3(x, y, z);
	}

	public Vector4 ReadVector4()
	{
		float x = ReadSingle();
		float y = ReadSingle();
		float z = ReadSingle();
		float w = ReadSingle();
		return new Vector4(x, y, z, w);
	}

	public override float ReadSingle()
	{
		return base.ReadSingle();
	}

	public override double ReadDouble()
	{
		return base.ReadDouble();
	}
}
