namespace Microsoft.Xna.Framework.Graphics;

public interface IEffectFog
{
	Vector3 FogColor { get; set; }

	bool FogEnabled { get; set; }

	float FogEnd { get; set; }

	float FogStart { get; set; }
}
