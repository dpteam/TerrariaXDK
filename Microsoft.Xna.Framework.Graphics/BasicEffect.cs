namespace Microsoft.Xna.Framework.Graphics;

public class BasicEffect : Effect, IEffectMatrices, IEffectLights, IEffectFog
{
	private EffectParameter textureParam;

	private EffectParameter diffuseColorParam;

	private EffectParameter emissiveColorParam;

	private EffectParameter specularColorParam;

	private EffectParameter specularPowerParam;

	private EffectParameter eyePositionParam;

	private EffectParameter fogColorParam;

	private EffectParameter fogVectorParam;

	private EffectParameter worldParam;

	private EffectParameter worldInverseTransposeParam;

	private EffectParameter worldViewProjParam;

	private EffectParameter shaderIndexParam;

	private bool lightingEnabled;

	private bool preferPerPixelLighting;

	private bool oneLight;

	private bool fogEnabled;

	private bool textureEnabled;

	private bool vertexColorEnabled;

	private Matrix world = Matrix.Identity;

	private Matrix view = Matrix.Identity;

	private Matrix projection = Matrix.Identity;

	private Matrix worldView;

	private Vector3 diffuseColor = Vector3.One;

	private Vector3 emissiveColor = Vector3.Zero;

	private Vector3 ambientLightColor = Vector3.Zero;

	private float alpha = 1f;

	private DirectionalLight light0;

	private DirectionalLight light1;

	private DirectionalLight light2;

	private float fogStart = 0f;

	private float fogEnd = 1f;

	private EffectDirtyFlags dirtyFlags = EffectDirtyFlags.All;

	public Matrix World
	{
		get
		{
			return world;
		}
		set
		{
			world = value;
			dirtyFlags |= EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.World | EffectDirtyFlags.Fog;
		}
	}

	public Matrix View
	{
		get
		{
			return view;
		}
		set
		{
			view = value;
			dirtyFlags |= EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.EyePosition | EffectDirtyFlags.Fog;
		}
	}

	public Matrix Projection
	{
		get
		{
			return projection;
		}
		set
		{
			projection = value;
			dirtyFlags |= EffectDirtyFlags.WorldViewProj;
		}
	}

	public Vector3 DiffuseColor
	{
		get
		{
			return diffuseColor;
		}
		set
		{
			diffuseColor = value;
			dirtyFlags |= EffectDirtyFlags.MaterialColor;
		}
	}

	public Vector3 EmissiveColor
	{
		get
		{
			return emissiveColor;
		}
		set
		{
			emissiveColor = value;
			dirtyFlags |= EffectDirtyFlags.MaterialColor;
		}
	}

	public Vector3 SpecularColor
	{
		get
		{
			return specularColorParam.GetValueVector3();
		}
		set
		{
			specularColorParam.SetValue(value);
		}
	}

	public float SpecularPower
	{
		get
		{
			return specularPowerParam.GetValueSingle();
		}
		set
		{
			specularPowerParam.SetValue(value);
		}
	}

	public float Alpha
	{
		get
		{
			return alpha;
		}
		set
		{
			alpha = value;
			dirtyFlags |= EffectDirtyFlags.MaterialColor;
		}
	}

	public bool LightingEnabled
	{
		get
		{
			return lightingEnabled;
		}
		set
		{
			if (lightingEnabled != value)
			{
				lightingEnabled = value;
				dirtyFlags |= EffectDirtyFlags.MaterialColor | EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	public bool PreferPerPixelLighting
	{
		get
		{
			return preferPerPixelLighting;
		}
		set
		{
			if (preferPerPixelLighting != value)
			{
				preferPerPixelLighting = value;
				dirtyFlags |= EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	public Vector3 AmbientLightColor
	{
		get
		{
			return ambientLightColor;
		}
		set
		{
			ambientLightColor = value;
			dirtyFlags |= EffectDirtyFlags.MaterialColor;
		}
	}

	public DirectionalLight DirectionalLight0 => light0;

	public DirectionalLight DirectionalLight1 => light1;

	public DirectionalLight DirectionalLight2 => light2;

	public bool FogEnabled
	{
		get
		{
			return fogEnabled;
		}
		set
		{
			if (fogEnabled != value)
			{
				fogEnabled = value;
				dirtyFlags |= EffectDirtyFlags.FogEnable | EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	public float FogStart
	{
		get
		{
			return fogStart;
		}
		set
		{
			fogStart = value;
			dirtyFlags |= EffectDirtyFlags.Fog;
		}
	}

	public float FogEnd
	{
		get
		{
			return fogEnd;
		}
		set
		{
			fogEnd = value;
			dirtyFlags |= EffectDirtyFlags.Fog;
		}
	}

	public Vector3 FogColor
	{
		get
		{
			return fogColorParam.GetValueVector3();
		}
		set
		{
			fogColorParam.SetValue(value);
		}
	}

	public bool TextureEnabled
	{
		get
		{
			return textureEnabled;
		}
		set
		{
			if (textureEnabled != value)
			{
				textureEnabled = value;
				dirtyFlags |= EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	public Texture2D Texture
	{
		get
		{
			return textureParam.GetValueTexture2D();
		}
		set
		{
			textureParam.SetValue(value);
		}
	}

	public bool VertexColorEnabled
	{
		get
		{
			return vertexColorEnabled;
		}
		set
		{
			if (vertexColorEnabled != value)
			{
				vertexColorEnabled = value;
				dirtyFlags |= EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	public BasicEffect(GraphicsDevice device)
		: base(device, Resources.BasicEffect)
	{
		CacheEffectParameters(null);
		DirectionalLight0.Enabled = true;
		SpecularColor = Vector3.One;
		SpecularPower = 16f;
	}

	protected BasicEffect(BasicEffect cloneSource)
		: base(cloneSource)
	{
		CacheEffectParameters(cloneSource);
		lightingEnabled = cloneSource.lightingEnabled;
		preferPerPixelLighting = cloneSource.preferPerPixelLighting;
		fogEnabled = cloneSource.fogEnabled;
		textureEnabled = cloneSource.textureEnabled;
		vertexColorEnabled = cloneSource.vertexColorEnabled;
		world = cloneSource.world;
		view = cloneSource.view;
		projection = cloneSource.projection;
		diffuseColor = cloneSource.diffuseColor;
		emissiveColor = cloneSource.emissiveColor;
		ambientLightColor = cloneSource.ambientLightColor;
		alpha = cloneSource.alpha;
		fogStart = cloneSource.fogStart;
		fogEnd = cloneSource.fogEnd;
	}

	public override Effect Clone()
	{
		return new BasicEffect(this);
	}

	public void EnableDefaultLighting()
	{
		LightingEnabled = true;
		AmbientLightColor = EffectHelpers.EnableDefaultLighting(light0, light1, light2);
	}

	private void CacheEffectParameters(BasicEffect cloneSource)
	{
		textureParam = Parameters["Texture"];
		diffuseColorParam = Parameters["DiffuseColor"];
		emissiveColorParam = Parameters["EmissiveColor"];
		specularColorParam = Parameters["SpecularColor"];
		specularPowerParam = Parameters["SpecularPower"];
		eyePositionParam = Parameters["EyePosition"];
		fogColorParam = Parameters["FogColor"];
		fogVectorParam = Parameters["FogVector"];
		worldParam = Parameters["World"];
		worldInverseTransposeParam = Parameters["WorldInverseTranspose"];
		worldViewProjParam = Parameters["WorldViewProj"];
		shaderIndexParam = Parameters["ShaderIndex"];
		light0 = new DirectionalLight(Parameters["DirLight0Direction"], Parameters["DirLight0DiffuseColor"], Parameters["DirLight0SpecularColor"], cloneSource?.light0);
		light1 = new DirectionalLight(Parameters["DirLight1Direction"], Parameters["DirLight1DiffuseColor"], Parameters["DirLight1SpecularColor"], cloneSource?.light1);
		light2 = new DirectionalLight(Parameters["DirLight2Direction"], Parameters["DirLight2DiffuseColor"], Parameters["DirLight2SpecularColor"], cloneSource?.light2);
	}

	protected internal override void OnApply()
	{
		dirtyFlags = EffectHelpers.SetWorldViewProjAndFog(dirtyFlags, ref world, ref view, ref projection, ref worldView, fogEnabled, fogStart, fogEnd, worldViewProjParam, fogVectorParam);
		if ((dirtyFlags & EffectDirtyFlags.MaterialColor) != 0)
		{
			EffectHelpers.SetMaterialColor(lightingEnabled, alpha, ref diffuseColor, ref emissiveColor, ref ambientLightColor, diffuseColorParam, emissiveColorParam);
			dirtyFlags &= ~EffectDirtyFlags.MaterialColor;
		}
		if (lightingEnabled)
		{
			dirtyFlags = EffectHelpers.SetLightingMatrices(dirtyFlags, ref world, ref view, worldParam, worldInverseTransposeParam, eyePositionParam);
			bool flag = !light1.Enabled && !light2.Enabled;
			if (oneLight != flag)
			{
				oneLight = flag;
				dirtyFlags |= EffectDirtyFlags.ShaderIndex;
			}
		}
		if ((dirtyFlags & EffectDirtyFlags.ShaderIndex) != 0)
		{
			int num = 0;
			if (!fogEnabled)
			{
				num++;
			}
			if (vertexColorEnabled)
			{
				num += 2;
			}
			if (textureEnabled)
			{
				num += 4;
			}
			if (lightingEnabled)
			{
				if (preferPerPixelLighting)
				{
					num += 24;
				}
				else
				{
					num = ((!oneLight) ? (num + 8) : (num + 16));
				}
			}
			shaderIndexParam.SetValue(num);
			dirtyFlags &= ~EffectDirtyFlags.ShaderIndex;
		}
	}
}
