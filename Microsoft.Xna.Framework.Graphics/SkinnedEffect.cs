using System;

namespace Microsoft.Xna.Framework.Graphics;

public class SkinnedEffect : Effect, IEffectMatrices, IEffectLights, IEffectFog
{
	public const int MaxBones = 72;

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

	private EffectParameter bonesParam;

	private EffectParameter shaderIndexParam;

	private bool preferPerPixelLighting;

	private bool oneLight;

	private bool fogEnabled;

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

	private int weightsPerVertex = 4;

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

	public int WeightsPerVertex
	{
		get
		{
			return weightsPerVertex;
		}
		set
		{
			if (value != 1 && value != 2 && value != 4)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			weightsPerVertex = value;
			dirtyFlags |= EffectDirtyFlags.ShaderIndex;
		}
	}

	bool IEffectLights.LightingEnabled
	{
		get
		{
			return true;
		}
		set
		{
			if (!value)
			{
				throw new NotSupportedException("SkinnedEffect does not support setting LightingEnabled to false.");
			}
		}
	}

	public void SetBoneTransforms(Matrix[] boneTransforms)
	{
		if (boneTransforms == null || boneTransforms.Length == 0)
		{
			throw new ArgumentNullException("boneTransforms");
		}
		if (boneTransforms.Length > 72)
		{
			throw new ArgumentException();
		}
		bonesParam.SetValue(boneTransforms);
	}

	public Matrix[] GetBoneTransforms(int count)
	{
		if (count <= 0 || count > 72)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		Matrix[] valueMatrixArray = bonesParam.GetValueMatrixArray(count);
		for (int i = 0; i < valueMatrixArray.Length; i++)
		{
			valueMatrixArray[i].M44 = 1f;
		}
		return valueMatrixArray;
	}

	public SkinnedEffect(GraphicsDevice device)
		: base(device, Resources.SkinnedEffect)
	{
		CacheEffectParameters(null);
		DirectionalLight0.Enabled = true;
		SpecularColor = Vector3.One;
		SpecularPower = 16f;
		Matrix[] array = new Matrix[72];
		for (int i = 0; i < 72; i++)
		{
			array[i] = Matrix.Identity;
		}
		SetBoneTransforms(array);
	}

	protected SkinnedEffect(SkinnedEffect cloneSource)
		: base(cloneSource)
	{
		CacheEffectParameters(cloneSource);
		preferPerPixelLighting = cloneSource.preferPerPixelLighting;
		fogEnabled = cloneSource.fogEnabled;
		world = cloneSource.world;
		view = cloneSource.view;
		projection = cloneSource.projection;
		diffuseColor = cloneSource.diffuseColor;
		emissiveColor = cloneSource.emissiveColor;
		ambientLightColor = cloneSource.ambientLightColor;
		alpha = cloneSource.alpha;
		fogStart = cloneSource.fogStart;
		fogEnd = cloneSource.fogEnd;
		weightsPerVertex = cloneSource.weightsPerVertex;
	}

	public override Effect Clone()
	{
		return new SkinnedEffect(this);
	}

	public void EnableDefaultLighting()
	{
		AmbientLightColor = EffectHelpers.EnableDefaultLighting(light0, light1, light2);
	}

	private void CacheEffectParameters(SkinnedEffect cloneSource)
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
		bonesParam = Parameters["Bones"];
		shaderIndexParam = Parameters["ShaderIndex"];
		light0 = new DirectionalLight(Parameters["DirLight0Direction"], Parameters["DirLight0DiffuseColor"], Parameters["DirLight0SpecularColor"], cloneSource?.light0);
		light1 = new DirectionalLight(Parameters["DirLight1Direction"], Parameters["DirLight1DiffuseColor"], Parameters["DirLight1SpecularColor"], cloneSource?.light1);
		light2 = new DirectionalLight(Parameters["DirLight2Direction"], Parameters["DirLight2DiffuseColor"], Parameters["DirLight2SpecularColor"], cloneSource?.light2);
	}

	protected internal override void OnApply()
	{
		dirtyFlags = EffectHelpers.SetWorldViewProjAndFog(dirtyFlags, ref world, ref view, ref projection, ref worldView, fogEnabled, fogStart, fogEnd, worldViewProjParam, fogVectorParam);
		dirtyFlags = EffectHelpers.SetLightingMatrices(dirtyFlags, ref world, ref view, worldParam, worldInverseTransposeParam, eyePositionParam);
		if ((dirtyFlags & EffectDirtyFlags.MaterialColor) != 0)
		{
			EffectHelpers.SetMaterialColor(lightingEnabled: true, alpha, ref diffuseColor, ref emissiveColor, ref ambientLightColor, diffuseColorParam, emissiveColorParam);
			dirtyFlags &= ~EffectDirtyFlags.MaterialColor;
		}
		bool flag = !light1.Enabled && !light2.Enabled;
		if (oneLight != flag)
		{
			oneLight = flag;
			dirtyFlags |= EffectDirtyFlags.ShaderIndex;
		}
		if ((dirtyFlags & EffectDirtyFlags.ShaderIndex) != 0)
		{
			int num = 0;
			if (!fogEnabled)
			{
				num++;
			}
			if (weightsPerVertex == 2)
			{
				num += 2;
			}
			else if (weightsPerVertex == 4)
			{
				num += 4;
			}
			if (preferPerPixelLighting)
			{
				num += 12;
			}
			else if (oneLight)
			{
				num += 6;
			}
			shaderIndexParam.SetValue(num);
			dirtyFlags &= ~EffectDirtyFlags.ShaderIndex;
		}
	}
}
