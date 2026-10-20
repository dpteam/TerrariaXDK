using System;

namespace Microsoft.Xna.Framework.Graphics;

public class RenderTarget2D : Texture2D, IRenderTarget
{
	private IntPtr glDepthStencilBuffer;

	private IntPtr glColorBuffer;

	public DepthFormat DepthStencilFormat { get; private set; }

	public int MultiSampleCount { get; private set; }

	public RenderTargetUsage RenderTargetUsage { get; private set; }

	public bool IsContentLost => false;

	IntPtr IRenderTarget.DepthStencilBuffer => glDepthStencilBuffer;

	IntPtr IRenderTarget.ColorBuffer => glColorBuffer;

	public event EventHandler<EventArgs> ContentLost;

	public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
		: this(graphicsDevice, width, height, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents)
	{
	}

	public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
		: this(graphicsDevice, width, height, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.DiscardContents)
	{
	}

	public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
		: base(graphicsDevice, width, height, mipMap, preferredFormat)
	{
		DepthStencilFormat = preferredDepthFormat;
		MultiSampleCount = FNA3D.FNA3D_GetMaxMultiSampleCount(graphicsDevice.GLDevice, Format, MathHelper.ClosestMSAAPower(preferredMultiSampleCount));
		RenderTargetUsage = usage;
		if (MultiSampleCount > 0)
		{
			glColorBuffer = FNA3D.FNA3D_GenColorRenderbuffer(graphicsDevice.GLDevice, Width, Height, Format, MultiSampleCount, texture);
		}
		if (DepthStencilFormat != DepthFormat.None)
		{
			glDepthStencilBuffer = FNA3D.FNA3D_GenDepthStencilRenderbuffer(graphicsDevice.GLDevice, Width, Height, DepthStencilFormat, MultiSampleCount);
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

	protected internal override void GraphicsDeviceResetting()
	{
		base.GraphicsDeviceResetting();
	}
}
