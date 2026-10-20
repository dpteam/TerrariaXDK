using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class VertexDeclaration : GraphicsResource
{
	internal VertexElement[] elements;

	internal IntPtr elementsPin;

	private GCHandle handle;

	public int VertexStride { get; private set; }

	public VertexDeclaration(params VertexElement[] elements)
		: this(GetVertexStride(elements), elements)
	{
	}

	public VertexDeclaration(int vertexStride, params VertexElement[] elements)
	{
		if (elements == null || elements.Length == 0)
		{
			throw new ArgumentNullException("elements", "Elements cannot be empty");
		}
		this.elements = (VertexElement[])elements.Clone();
		handle = GCHandle.Alloc(this.elements, GCHandleType.Pinned);
		elementsPin = handle.AddrOfPinnedObject();
		VertexStride = vertexStride;
	}

	~VertexDeclaration()
	{
		handle.Free();
	}

	public VertexElement[] GetVertexElements()
	{
		return (VertexElement[])elements.Clone();
	}

	internal static VertexDeclaration FromType(Type vertexType)
	{
		if (vertexType == null)
		{
			throw new ArgumentNullException("vertexType", "Cannot be null");
		}
		if (!vertexType.IsValueType)
		{
			throw new ArgumentException("vertexType", "Must be value type");
		}
		if (!(Activator.CreateInstance(vertexType) is IVertexType { VertexDeclaration: var vertexDeclaration }) || 1 == 0)
		{
			throw new ArgumentException("vertexData does not inherit IVertexType");
		}
		if (vertexDeclaration == null)
		{
			throw new ArgumentException("vertexType's VertexDeclaration cannot be null");
		}
		return vertexDeclaration;
	}

	private static int GetVertexStride(VertexElement[] elements)
	{
		int num = 0;
		for (int i = 0; i < elements.Length; i++)
		{
			int num2 = elements[i].Offset + GetTypeSize(elements[i].VertexElementFormat);
			if (num < num2)
			{
				num = num2;
			}
		}
		return num;
	}

	private static int GetTypeSize(VertexElementFormat elementFormat)
	{
		if (1 == 0)
		{
		}
		int result = elementFormat switch
		{
			VertexElementFormat.Single => 4, 
			VertexElementFormat.Vector2 => 8, 
			VertexElementFormat.Vector3 => 12, 
			VertexElementFormat.Vector4 => 16, 
			VertexElementFormat.Color => 4, 
			VertexElementFormat.Byte4 => 4, 
			VertexElementFormat.Short2 => 4, 
			VertexElementFormat.Short4 => 8, 
			VertexElementFormat.NormalizedShort2 => 4, 
			VertexElementFormat.NormalizedShort4 => 8, 
			VertexElementFormat.HalfVector2 => 4, 
			VertexElementFormat.HalfVector4 => 8, 
			_ => 0, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
