namespace Microsoft.Xna.Framework.Graphics;

public sealed class DirectionalLight
{
	private Vector3 INTERNAL_diffuseColor;

	private Vector3 INTERNAL_direction;

	private Vector3 INTERNAL_specularColor;

	private bool INTERNAL_enabled;

	internal EffectParameter diffuseColorParameter;

	internal EffectParameter directionParameter;

	internal EffectParameter specularColorParameter;

	public Vector3 DiffuseColor
	{
		get
		{
			return INTERNAL_diffuseColor;
		}
		set
		{
			INTERNAL_diffuseColor = value;
			if (Enabled && diffuseColorParameter != null)
			{
				diffuseColorParameter.SetValue(INTERNAL_diffuseColor);
			}
		}
	}

	public Vector3 Direction
	{
		get
		{
			return INTERNAL_direction;
		}
		set
		{
			INTERNAL_direction = value;
			if (directionParameter != null)
			{
				directionParameter.SetValue(INTERNAL_direction);
			}
		}
	}

	public Vector3 SpecularColor
	{
		get
		{
			return INTERNAL_specularColor;
		}
		set
		{
			INTERNAL_specularColor = value;
			if (Enabled && specularColorParameter != null)
			{
				specularColorParameter.SetValue(INTERNAL_specularColor);
			}
		}
	}

	public bool Enabled
	{
		get
		{
			return INTERNAL_enabled;
		}
		set
		{
			if (INTERNAL_enabled == value)
			{
				return;
			}
			INTERNAL_enabled = value;
			if (INTERNAL_enabled)
			{
				if (diffuseColorParameter != null)
				{
					diffuseColorParameter.SetValue(DiffuseColor);
				}
				if (specularColorParameter != null)
				{
					specularColorParameter.SetValue(SpecularColor);
				}
			}
			else
			{
				if (diffuseColorParameter != null)
				{
					diffuseColorParameter.SetValue(Vector3.Zero);
				}
				if (specularColorParameter != null)
				{
					specularColorParameter.SetValue(Vector3.Zero);
				}
			}
		}
	}

	public DirectionalLight(EffectParameter directionParameter, EffectParameter diffuseColorParameter, EffectParameter specularColorParameter, DirectionalLight cloneSource)
	{
		this.diffuseColorParameter = diffuseColorParameter;
		this.directionParameter = directionParameter;
		this.specularColorParameter = specularColorParameter;
		if (cloneSource != null)
		{
			DiffuseColor = cloneSource.DiffuseColor;
			Direction = cloneSource.Direction;
			SpecularColor = cloneSource.SpecularColor;
			Enabled = cloneSource.Enabled;
		}
	}
}
