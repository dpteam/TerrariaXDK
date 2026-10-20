#define DEBUG
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class DynamicVertexBuffer : VertexBuffer
{
	public bool IsContentLost => false;

	public event EventHandler<EventArgs> ContentLost;

	public DynamicVertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
		: base(graphicsDevice, vertexDeclaration, vertexCount, bufferUsage, dynamic: true)
	{
	}

	public DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type type, int vertexCount, BufferUsage bufferUsage)
		: base(graphicsDevice, VertexDeclaration.FromType(type), vertexCount, bufferUsage, dynamic: true)
	{
	}

	public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride, SetDataOptions options) where T : struct
	{
		ErrorCheck(data, startIndex, elementCount, vertexStride);
		int num = Marshal.SizeOf(typeof(T));
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_SetVertexBufferData(GraphicsDevice.GLDevice, buffer, offsetInBytes, gCHandle.AddrOfPinnedObject() + startIndex * num, elementCount, num, vertexStride, options);
		gCHandle.Free();
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount, SetDataOptions options) where T : struct
	{
		int num = Marshal.SizeOf(typeof(T));
		ErrorCheck(data, startIndex, elementCount, num);
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_SetVertexBufferData(GraphicsDevice.GLDevice, buffer, 0, gCHandle.AddrOfPinnedObject() + startIndex * num, elementCount, num, num, options);
		gCHandle.Free();
	}
}
