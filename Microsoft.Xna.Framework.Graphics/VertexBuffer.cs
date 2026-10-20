#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class VertexBuffer : GraphicsResource
{
	internal IntPtr buffer;

	public BufferUsage BufferUsage { get; private set; }

	public int VertexCount { get; private set; }

	public VertexDeclaration VertexDeclaration { get; private set; }

	public VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
		: this(graphicsDevice, vertexDeclaration, vertexCount, bufferUsage, dynamic: false)
	{
	}

	public VertexBuffer(GraphicsDevice graphicsDevice, Type type, int vertexCount, BufferUsage bufferUsage)
		: this(graphicsDevice, VertexDeclaration.FromType(type), vertexCount, bufferUsage, dynamic: false)
	{
	}

	protected VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage, bool dynamic)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice");
		}
		GraphicsDevice = graphicsDevice;
		VertexDeclaration = vertexDeclaration;
		VertexCount = vertexCount;
		BufferUsage = bufferUsage;
		if (vertexDeclaration.GraphicsDevice != graphicsDevice)
		{
			vertexDeclaration.GraphicsDevice = graphicsDevice;
		}
		buffer = FNA3D.FNA3D_GenVertexBuffer(GraphicsDevice.GLDevice, (byte)(dynamic ? 1u : 0u), bufferUsage, VertexCount * VertexDeclaration.VertexStride);
	}

	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			FNA3D.FNA3D_AddDisposeVertexBuffer(GraphicsDevice.GLDevice, buffer);
		}
		base.Dispose(disposing);
	}

	public void GetData<T>(T[] data) where T : struct
	{
		GetData(0, data, 0, data.Length, Marshal.SizeOf(typeof(T)));
	}

	public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		GetData(0, data, startIndex, elementCount, Marshal.SizeOf(typeof(T)));
	}

	public void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (data.Length < startIndex + elementCount)
		{
			throw new ArgumentOutOfRangeException("elementCount", "This parameter must be a valid index within the array.");
		}
		if (BufferUsage == BufferUsage.WriteOnly)
		{
			throw new NotSupportedException("Calling GetData on a resource that was created with BufferUsage.WriteOnly is not supported.");
		}
		int num = Marshal.SizeOf(typeof(T));
		if (vertexStride == 0)
		{
			vertexStride = num;
		}
		else if (vertexStride < num)
		{
			throw new ArgumentOutOfRangeException("vertexStride", "The vertex stride is too small for the type of data requested. This is not allowed.");
		}
		if (elementCount > 1 && elementCount * vertexStride > VertexCount * VertexDeclaration.VertexStride)
		{
			throw new InvalidOperationException("The array is not the correct size for the amount of data requested.");
		}
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_GetVertexBufferData(GraphicsDevice.GLDevice, buffer, offsetInBytes, gCHandle.AddrOfPinnedObject() + startIndex * num, elementCount, num, vertexStride);
		gCHandle.Free();
	}

	public void SetData<T>(T[] data) where T : struct
	{
		SetData(0, data, 0, data.Length, Marshal.SizeOf(typeof(T)));
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		SetData(0, data, startIndex, elementCount, Marshal.SizeOf(typeof(T)));
	}

	public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
	{
		ErrorCheck(data, startIndex, elementCount, vertexStride);
		int num = Marshal.SizeOf(typeof(T));
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_SetVertexBufferData(GraphicsDevice.GLDevice, buffer, offsetInBytes, gCHandle.AddrOfPinnedObject() + startIndex * num, elementCount, num, vertexStride, SetDataOptions.None);
		gCHandle.Free();
	}

	public void SetDataPointerEXT(int offsetInBytes, IntPtr data, int dataLength, SetDataOptions options)
	{
		FNA3D.FNA3D_SetVertexBufferData(GraphicsDevice.GLDevice, buffer, offsetInBytes, data, dataLength, 1, 1, options);
	}

	[Conditional("DEBUG")]
	internal void ErrorCheck<T>(T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (startIndex + elementCount > data.Length || elementCount <= 0)
		{
			throw new InvalidOperationException("The array specified in the data parameter is not the correct size for the amount of data requested.");
		}
		if (elementCount > 1 && elementCount * vertexStride > VertexCount * VertexDeclaration.VertexStride)
		{
			throw new InvalidOperationException("The vertex stride is larger than the vertex buffer.");
		}
		int num = Marshal.SizeOf(typeof(T));
		if (vertexStride == 0)
		{
			vertexStride = num;
		}
		if (vertexStride < num)
		{
			throw new ArgumentOutOfRangeException("The vertex stride must be greater than or equal to the size of the specified data (" + num + ").");
		}
	}

	protected internal override void GraphicsDeviceResetting()
	{
	}
}
