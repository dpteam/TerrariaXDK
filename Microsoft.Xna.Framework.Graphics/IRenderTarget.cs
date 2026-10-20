using System;

namespace Microsoft.Xna.Framework.Graphics;

internal interface IRenderTarget
{
	int Width { get; }

	int Height { get; }

	int LevelCount { get; }

	RenderTargetUsage RenderTargetUsage { get; }

	DepthFormat DepthStencilFormat { get; }

	IntPtr DepthStencilBuffer { get; }

	IntPtr ColorBuffer { get; }

	int MultiSampleCount { get; }
}
