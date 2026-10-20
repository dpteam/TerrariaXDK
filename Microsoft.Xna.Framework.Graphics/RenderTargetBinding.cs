using System;

namespace Microsoft.Xna.Framework.Graphics;

public struct RenderTargetBinding
{
	private readonly Texture renderTarget;

	private readonly CubeMapFace cubeMapFace;

	public Texture RenderTarget => renderTarget;

	public CubeMapFace CubeMapFace => cubeMapFace;

	public RenderTargetBinding(RenderTarget2D renderTarget)
	{
		if (renderTarget == null)
		{
			throw new ArgumentNullException("renderTarget");
		}
		this.renderTarget = renderTarget;
		cubeMapFace = CubeMapFace.PositiveX;
	}

	public RenderTargetBinding(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
	{
		if (renderTarget == null)
		{
			throw new ArgumentNullException("renderTarget");
		}
		if (cubeMapFace < CubeMapFace.PositiveX || cubeMapFace > CubeMapFace.NegativeZ)
		{
			throw new ArgumentOutOfRangeException("cubeMapFace");
		}
		this.renderTarget = renderTarget;
		this.cubeMapFace = cubeMapFace;
	}

	public static implicit operator RenderTargetBinding(RenderTarget2D renderTarget)
	{
		return new RenderTargetBinding(renderTarget);
	}
}
