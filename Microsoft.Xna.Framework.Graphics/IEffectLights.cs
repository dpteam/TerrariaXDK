namespace Microsoft.Xna.Framework.Graphics;

public interface IEffectLights
{
	Vector3 AmbientLightColor { get; set; }

	DirectionalLight DirectionalLight0 { get; }

	DirectionalLight DirectionalLight1 { get; }

	DirectionalLight DirectionalLight2 { get; }

	bool LightingEnabled { get; set; }

	void EnableDefaultLighting();
}
