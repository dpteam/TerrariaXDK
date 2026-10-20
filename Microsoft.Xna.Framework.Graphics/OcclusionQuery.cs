using System;

namespace Microsoft.Xna.Framework.Graphics;

public class OcclusionQuery : GraphicsResource
{
	private IntPtr query;

	public bool IsComplete => FNA3D.FNA3D_QueryComplete(GraphicsDevice.GLDevice, query) == 1;

	public int PixelCount => FNA3D.FNA3D_QueryPixelCount(GraphicsDevice.GLDevice, query);

	public OcclusionQuery(GraphicsDevice graphicsDevice)
	{
		GraphicsDevice = graphicsDevice;
		query = FNA3D.FNA3D_CreateQuery(GraphicsDevice.GLDevice);
	}

	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			FNA3D.FNA3D_AddDisposeQuery(GraphicsDevice.GLDevice, query);
		}
		base.Dispose(disposing);
	}

	public void Begin()
	{
		FNA3D.FNA3D_QueryBegin(GraphicsDevice.GLDevice, query);
	}

	public void End()
	{
		FNA3D.FNA3D_QueryEnd(GraphicsDevice.GLDevice, query);
	}
}
