#define DEBUG
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class DynamicIndexBuffer : IndexBuffer
{
	public bool IsContentLost => false;

	// XNA API compatibility: FNA raises this for D3D buffer loss, which GL never reports.
#pragma warning disable 0067
	public event EventHandler<EventArgs> ContentLost;
#pragma warning restore 0067

	public DynamicIndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage usage)
		: base(graphicsDevice, indexElementSize, indexCount, usage, dynamic: true)
	{
	}

	public DynamicIndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage usage)
		: base(graphicsDevice, indexType, indexCount, usage, dynamic: true)
	{
	}

	public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, SetDataOptions options) where T : struct
	{
		ErrorCheck(data, startIndex, elementCount);
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_SetIndexBufferData(GraphicsDevice.GLDevice, buffer, offsetInBytes, gCHandle.AddrOfPinnedObject() + startIndex * Marshal.SizeOf(typeof(T)), elementCount * Marshal.SizeOf(typeof(T)), options);
		gCHandle.Free();
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount, SetDataOptions options) where T : struct
	{
		ErrorCheck(data, startIndex, elementCount);
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		FNA3D.FNA3D_SetIndexBufferData(GraphicsDevice.GLDevice, buffer, 0, gCHandle.AddrOfPinnedObject() + startIndex * Marshal.SizeOf(typeof(T)), elementCount * Marshal.SizeOf(typeof(T)), options);
		gCHandle.Free();
	}
}
