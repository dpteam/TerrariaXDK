namespace Microsoft.Xna.Framework.Graphics;

internal class SpriteEffect : Effect
{
	private EffectParameter matrixParam;

	public SpriteEffect(GraphicsDevice device)
		: base(device, Resources.SpriteEffect)
	{
		CacheEffectParameters();
	}

	protected SpriteEffect(SpriteEffect cloneSource)
		: base(cloneSource)
	{
		CacheEffectParameters();
	}

	public override Effect Clone()
	{
		return new SpriteEffect(this);
	}

	private void CacheEffectParameters()
	{
		matrixParam = Parameters["MatrixTransform"];
	}

	protected internal override void OnApply()
	{
		Viewport viewport = GraphicsDevice.Viewport;
		Matrix matrix = Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, 0f, 1f);
		Matrix matrix2 = Matrix.CreateTranslation(-0.5f, -0.5f, 0f);
		matrixParam.SetValue(matrix2 * matrix);
	}
}
