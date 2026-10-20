using System;

namespace Microsoft.Xna.Framework.Graphics;

public class RenderTargetCube : TextureCube, IRenderTarget
{
	private IntPtr glDepthStencilBuffer;

	private IntPtr glColorBuffer;

	public DepthFormat DepthStencilFormat { get; private set; }

	public int MultiSampleCount { get; private set; }

	public RenderTargetUsage RenderTargetUsage { get; private set; }

	public bool IsContentLost => false;

	int IRenderTarget.Width => Size;

	int IRenderTarget.Height => Size;

	IntPtr IRenderTarget.DepthStencilBuffer => glDepthStencilBuffer;

	IntPtr IRenderTarget.ColorBuffer => glColorBuffer;

	public event EventHandler<EventArgs> ContentLost;

	public RenderTargetCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
		: this(graphicsDevice, size, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.DiscardContents)
	{
	}

	public RenderTargetCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
		: base(graphicsDevice, size, mipMap, preferredFormat)
	{
		DepthStencilFormat = preferredDepthFormat;
		MultiSampleCount = FNA3D.FNA3D_GetMaxMultiSampleCount(graphicsDevice.GLDevice, Format, MathHelper.ClosestMSAAPower(preferredMultiSampleCount));
		RenderTargetUsage = usage;
		if (MultiSampleCount > 0)
		{
			glColorBuffer = FNA3D.FNA3D_GenColorRenderbuffer(graphicsDevice.GLDevice, Size, Size, Format, MultiSampleCount, texture);
		}
		if (DepthStencilFormat != DepthFormat.None)
		{
			glDepthStencilBuffer = FNA3D.FNA3D_GenDepthStencilRenderbuffer(graphicsDevice.GLDevice, Size, Size, DepthStencilFormat, MultiSampleCount);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (glColorBuffer != IntPtr.Zero)
			{
				FNA3D.FNA3D_AddDisposeRenderbuffer(GraphicsDevice.GLDevice, glColorBuffer);
			}
			if (glDepthStencilBuffer != IntPtr.Zero)
			{
				FNA3D.FNA3D_AddDisposeRenderbuffer(GraphicsDevice.GLDevice, glDepthStencilBuffer);
			}
		}
		base.Dispose(disposing);
	}
}
