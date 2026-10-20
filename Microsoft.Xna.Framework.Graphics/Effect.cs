using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class Effect : GraphicsResource
{
	private class IntPtrBoxlessComparer : IEqualityComparer<IntPtr>
	{
		public bool Equals(IntPtr x, IntPtr y)
		{
			return x == y;
		}

		public int GetHashCode(IntPtr obj)
		{
			return obj.GetHashCode();
		}
	}

	private enum MOJOSHADER_symbolClass
	{
		MOJOSHADER_SYMCLASS_SCALAR,
		MOJOSHADER_SYMCLASS_VECTOR,
		MOJOSHADER_SYMCLASS_MATRIX_ROWS,
		MOJOSHADER_SYMCLASS_MATRIX_COLUMNS,
		MOJOSHADER_SYMCLASS_OBJECT,
		MOJOSHADER_SYMCLASS_STRUCT,
		MOJOSHADER_SYMCLASS_TOTAL
	}

	private enum MOJOSHADER_symbolType
	{
		MOJOSHADER_SYMTYPE_VOID,
		MOJOSHADER_SYMTYPE_BOOL,
		MOJOSHADER_SYMTYPE_INT,
		MOJOSHADER_SYMTYPE_FLOAT,
		MOJOSHADER_SYMTYPE_STRING,
		MOJOSHADER_SYMTYPE_TEXTURE,
		MOJOSHADER_SYMTYPE_TEXTURE1D,
		MOJOSHADER_SYMTYPE_TEXTURE2D,
		MOJOSHADER_SYMTYPE_TEXTURE3D,
		MOJOSHADER_SYMTYPE_TEXTURECUBE,
		MOJOSHADER_SYMTYPE_SAMPLER,
		MOJOSHADER_SYMTYPE_SAMPLER1D,
		MOJOSHADER_SYMTYPE_SAMPLER2D,
		MOJOSHADER_SYMTYPE_SAMPLER3D,
		MOJOSHADER_SYMTYPE_SAMPLERCUBE,
		MOJOSHADER_SYMTYPE_PIXELSHADER,
		MOJOSHADER_SYMTYPE_VERTEXSHADER,
		MOJOSHADER_SYMTYPE_PIXELFRAGMENT,
		MOJOSHADER_SYMTYPE_VERTEXFRAGMENT,
		MOJOSHADER_SYMTYPE_UNSUPPORTED,
		MOJOSHADER_SYMTYPE_TOTAL
	}

	private struct MOJOSHADER_symbolTypeInfo
	{
		public MOJOSHADER_symbolClass parameter_class;

		public MOJOSHADER_symbolType parameter_type;

		public uint rows;

		public uint columns;

		public uint elements;

		public uint member_count;

		public IntPtr members;
	}

	private struct MOJOSHADER_symbolStructMember
	{
		public IntPtr name;

		public MOJOSHADER_symbolTypeInfo info;
	}

	private enum MOJOSHADER_renderStateType
	{
		MOJOSHADER_RS_ZENABLE = 0,
		MOJOSHADER_RS_FILLMODE = 1,
		MOJOSHADER_RS_SHADEMODE = 2,
		MOJOSHADER_RS_ZWRITEENABLE = 3,
		MOJOSHADER_RS_ALPHATESTENABLE = 4,
		MOJOSHADER_RS_LASTPIXEL = 5,
		MOJOSHADER_RS_SRCBLEND = 6,
		MOJOSHADER_RS_DESTBLEND = 7,
		MOJOSHADER_RS_CULLMODE = 8,
		MOJOSHADER_RS_ZFUNC = 9,
		MOJOSHADER_RS_ALPHAREF = 10,
		MOJOSHADER_RS_ALPHAFUNC = 11,
		MOJOSHADER_RS_DITHERENABLE = 12,
		MOJOSHADER_RS_ALPHABLENDENABLE = 13,
		MOJOSHADER_RS_FOGENABLE = 14,
		MOJOSHADER_RS_SPECULARENABLE = 15,
		MOJOSHADER_RS_FOGCOLOR = 16,
		MOJOSHADER_RS_FOGTABLEMODE = 17,
		MOJOSHADER_RS_FOGSTART = 18,
		MOJOSHADER_RS_FOGEND = 19,
		MOJOSHADER_RS_FOGDENSITY = 20,
		MOJOSHADER_RS_RANGEFOGENABLE = 21,
		MOJOSHADER_RS_STENCILENABLE = 22,
		MOJOSHADER_RS_STENCILFAIL = 23,
		MOJOSHADER_RS_STENCILZFAIL = 24,
		MOJOSHADER_RS_STENCILPASS = 25,
		MOJOSHADER_RS_STENCILFUNC = 26,
		MOJOSHADER_RS_STENCILREF = 27,
		MOJOSHADER_RS_STENCILMASK = 28,
		MOJOSHADER_RS_STENCILWRITEMASK = 29,
		MOJOSHADER_RS_TEXTUREFACTOR = 30,
		MOJOSHADER_RS_WRAP0 = 31,
		MOJOSHADER_RS_WRAP1 = 32,
		MOJOSHADER_RS_WRAP2 = 33,
		MOJOSHADER_RS_WRAP3 = 34,
		MOJOSHADER_RS_WRAP4 = 35,
		MOJOSHADER_RS_WRAP5 = 36,
		MOJOSHADER_RS_WRAP6 = 37,
		MOJOSHADER_RS_WRAP7 = 38,
		MOJOSHADER_RS_WRAP8 = 39,
		MOJOSHADER_RS_WRAP9 = 40,
		MOJOSHADER_RS_WRAP10 = 41,
		MOJOSHADER_RS_WRAP11 = 42,
		MOJOSHADER_RS_WRAP12 = 43,
		MOJOSHADER_RS_WRAP13 = 44,
		MOJOSHADER_RS_WRAP14 = 45,
		MOJOSHADER_RS_WRAP15 = 46,
		MOJOSHADER_RS_CLIPPING = 47,
		MOJOSHADER_RS_LIGHTING = 48,
		MOJOSHADER_RS_AMBIENT = 49,
		MOJOSHADER_RS_FOGVERTEXMODE = 50,
		MOJOSHADER_RS_COLORVERTEX = 51,
		MOJOSHADER_RS_LOCALVIEWER = 52,
		MOJOSHADER_RS_NORMALIZENORMALS = 53,
		MOJOSHADER_RS_DIFFUSEMATERIALSOURCE = 54,
		MOJOSHADER_RS_SPECULARMATERIALSOURCE = 55,
		MOJOSHADER_RS_AMBIENTMATERIALSOURCE = 56,
		MOJOSHADER_RS_EMISSIVEMATERIALSOURCE = 57,
		MOJOSHADER_RS_VERTEXBLEND = 58,
		MOJOSHADER_RS_CLIPPLANEENABLE = 59,
		MOJOSHADER_RS_POINTSIZE = 60,
		MOJOSHADER_RS_POINTSIZE_MIN = 61,
		MOJOSHADER_RS_POINTSPRITEENABLE = 62,
		MOJOSHADER_RS_POINTSCALEENABLE = 63,
		MOJOSHADER_RS_POINTSCALE_A = 64,
		MOJOSHADER_RS_POINTSCALE_B = 65,
		MOJOSHADER_RS_POINTSCALE_C = 66,
		MOJOSHADER_RS_MULTISAMPLEANTIALIAS = 67,
		MOJOSHADER_RS_MULTISAMPLEMASK = 68,
		MOJOSHADER_RS_PATCHEDGESTYLE = 69,
		MOJOSHADER_RS_DEBUGMONITORTOKEN = 70,
		MOJOSHADER_RS_POINTSIZE_MAX = 71,
		MOJOSHADER_RS_INDEXEDVERTEXBLENDENABLE = 72,
		MOJOSHADER_RS_COLORWRITEENABLE = 73,
		MOJOSHADER_RS_TWEENFACTOR = 74,
		MOJOSHADER_RS_BLENDOP = 75,
		MOJOSHADER_RS_POSITIONDEGREE = 76,
		MOJOSHADER_RS_NORMALDEGREE = 77,
		MOJOSHADER_RS_SCISSORTESTENABLE = 78,
		MOJOSHADER_RS_SLOPESCALEDEPTHBIAS = 79,
		MOJOSHADER_RS_ANTIALIASEDLINEENABLE = 80,
		MOJOSHADER_RS_MINTESSELLATIONLEVEL = 81,
		MOJOSHADER_RS_MAXTESSELLATIONLEVEL = 82,
		MOJOSHADER_RS_ADAPTIVETESS_X = 83,
		MOJOSHADER_RS_ADAPTIVETESS_Y = 84,
		MOJOSHADER_RS_ADAPTIVETESS_Z = 85,
		MOJOSHADER_RS_ADAPTIVETESS_W = 86,
		MOJOSHADER_RS_ENABLEADAPTIVETESSELLATION = 87,
		MOJOSHADER_RS_TWOSIDEDSTENCILMODE = 88,
		MOJOSHADER_RS_CCW_STENCILFAIL = 89,
		MOJOSHADER_RS_CCW_STENCILZFAIL = 90,
		MOJOSHADER_RS_CCW_STENCILPASS = 91,
		MOJOSHADER_RS_CCW_STENCILFUNC = 92,
		MOJOSHADER_RS_COLORWRITEENABLE1 = 93,
		MOJOSHADER_RS_COLORWRITEENABLE2 = 94,
		MOJOSHADER_RS_COLORWRITEENABLE3 = 95,
		MOJOSHADER_RS_BLENDFACTOR = 96,
		MOJOSHADER_RS_SRGBWRITEENABLE = 97,
		MOJOSHADER_RS_DEPTHBIAS = 98,
		MOJOSHADER_RS_SEPARATEALPHABLENDENABLE = 99,
		MOJOSHADER_RS_SRCBLENDALPHA = 100,
		MOJOSHADER_RS_DESTBLENDALPHA = 101,
		MOJOSHADER_RS_BLENDOPALPHA = 102,
		MOJOSHADER_RS_VERTEXSHADER = 146,
		MOJOSHADER_RS_PIXELSHADER = 147
	}

	private enum MOJOSHADER_zBufferType
	{
		MOJOSHADER_ZB_FALSE,
		MOJOSHADER_ZB_TRUE,
		MOJOSHADER_ZB_USEW
	}

	private enum MOJOSHADER_fillMode
	{
		MOJOSHADER_FILL_POINT = 1,
		MOJOSHADER_FILL_WIREFRAME,
		MOJOSHADER_FILL_SOLID
	}

	private enum MOJOSHADER_blendMode
	{
		MOJOSHADER_BLEND_ZERO = 1,
		MOJOSHADER_BLEND_ONE,
		MOJOSHADER_BLEND_SRCCOLOR,
		MOJOSHADER_BLEND_INVSRCCOLOR,
		MOJOSHADER_BLEND_SRCALPHA,
		MOJOSHADER_BLEND_INVSRCALPHA,
		MOJOSHADER_BLEND_DESTALPHA,
		MOJOSHADER_BLEND_INVDESTALPHA,
		MOJOSHADER_BLEND_DESTCOLOR,
		MOJOSHADER_BLEND_INVDESTCOLOR,
		MOJOSHADER_BLEND_SRCALPHASAT,
		MOJOSHADER_BLEND_BOTHSRCALPHA,
		MOJOSHADER_BLEND_BOTHINVSRCALPHA,
		MOJOSHADER_BLEND_BLENDFACTOR,
		MOJOSHADER_BLEND_INVBLENDFACTOR,
		MOJOSHADER_BLEND_SRCCOLOR2,
		MOJOSHADER_BLEND_INVSRCCOLOR2
	}

	private enum MOJOSHADER_cullMode
	{
		MOJOSHADER_CULL_NONE = 1,
		MOJOSHADER_CULL_CW,
		MOJOSHADER_CULL_CCW
	}

	private enum MOJOSHADER_compareFunc
	{
		MOJOSHADER_CMP_NEVER = 1,
		MOJOSHADER_CMP_LESS,
		MOJOSHADER_CMP_EQUAL,
		MOJOSHADER_CMP_LESSEQUAL,
		MOJOSHADER_CMP_GREATER,
		MOJOSHADER_CMP_NOTEQUAL,
		MOJOSHADER_CMP_GREATEREQUAL,
		MOJOSHADER_CMP_ALWAYS
	}

	private enum MOJOSHADER_stencilOp
	{
		MOJOSHADER_STENCILOP_KEEP = 1,
		MOJOSHADER_STENCILOP_ZERO,
		MOJOSHADER_STENCILOP_REPLACE,
		MOJOSHADER_STENCILOP_INCRSAT,
		MOJOSHADER_STENCILOP_DECRSAT,
		MOJOSHADER_STENCILOP_INVERT,
		MOJOSHADER_STENCILOP_INCR,
		MOJOSHADER_STENCILOP_DECR
	}

	private enum MOJOSHADER_blendOp
	{
		MOJOSHADER_BLENDOP_ADD = 1,
		MOJOSHADER_BLENDOP_SUBTRACT,
		MOJOSHADER_BLENDOP_REVSUBTRACT,
		MOJOSHADER_BLENDOP_MIN,
		MOJOSHADER_BLENDOP_MAX
	}

	private enum MOJOSHADER_samplerStateType
	{
		MOJOSHADER_SAMP_UNKNOWN0,
		MOJOSHADER_SAMP_UNKNOWN1,
		MOJOSHADER_SAMP_UNKNOWN2,
		MOJOSHADER_SAMP_UNKNOWN3,
		MOJOSHADER_SAMP_TEXTURE,
		MOJOSHADER_SAMP_ADDRESSU,
		MOJOSHADER_SAMP_ADDRESSV,
		MOJOSHADER_SAMP_ADDRESSW,
		MOJOSHADER_SAMP_BORDERCOLOR,
		MOJOSHADER_SAMP_MAGFILTER,
		MOJOSHADER_SAMP_MINFILTER,
		MOJOSHADER_SAMP_MIPFILTER,
		MOJOSHADER_SAMP_MIPMAPLODBIAS,
		MOJOSHADER_SAMP_MAXMIPLEVEL,
		MOJOSHADER_SAMP_MAXANISOTROPY,
		MOJOSHADER_SAMP_SRGBTEXTURE,
		MOJOSHADER_SAMP_ELEMENTINDEX,
		MOJOSHADER_SAMP_DMAPOFFSET
	}

	private enum MOJOSHADER_textureAddress
	{
		MOJOSHADER_TADDRESS_WRAP = 1,
		MOJOSHADER_TADDRESS_MIRROR,
		MOJOSHADER_TADDRESS_CLAMP,
		MOJOSHADER_TADDRESS_BORDER,
		MOJOSHADER_TADDRESS_MIRRORONCE
	}

	private enum MOJOSHADER_textureFilterType
	{
		MOJOSHADER_TEXTUREFILTER_NONE,
		MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_TEXTUREFILTER_ANISOTROPIC,
		MOJOSHADER_TEXTUREFILTER_PYRAMIDALQUAD,
		MOJOSHADER_TEXTUREFILTER_GAUSSIANQUAD,
		MOJOSHADER_TEXTUREFILTER_CONVOLUTIONMONO
	}

	private struct MOJOSHADER_effectValue
	{
		public IntPtr name;

		public IntPtr semantic;

		public MOJOSHADER_symbolTypeInfo type;

		public uint value_count;

		public IntPtr values;
	}

	private struct MOJOSHADER_effectState
	{
		public MOJOSHADER_renderStateType type;

		public MOJOSHADER_effectValue value;
	}

	private struct MOJOSHADER_effectSamplerState
	{
		public MOJOSHADER_samplerStateType type;

		public MOJOSHADER_effectValue value;
	}

	private struct MOJOSHADER_effectAnnotation
	{
		public IntPtr name;

		public IntPtr semantic;

		public MOJOSHADER_symbolTypeInfo type;

		public uint value_count;

		public IntPtr values;
	}

	private struct MOJOSHADER_effectParam
	{
		public MOJOSHADER_effectValue value;

		public uint annotation_count;

		public IntPtr annotations;
	}

	private struct MOJOSHADER_effectPass
	{
		public IntPtr name;

		public uint state_count;

		public IntPtr states;

		public uint annotation_count;

		public IntPtr annotations;
	}

	private struct MOJOSHADER_effectTechnique
	{
		public IntPtr name;

		public uint pass_count;

		public IntPtr passes;

		public uint annotation_count;

		public IntPtr annotations;
	}

	private struct MOJOSHADER_effectShader
	{
		public MOJOSHADER_symbolType type;

		public uint technique;

		public uint pass;

		public uint is_preshader;

		public uint preshader_param_count;

		public IntPtr preshader_params;

		public uint param_count;

		public IntPtr parameters;

		public uint sampler_count;

		public IntPtr samplers;

		public IntPtr shader;
	}

	private struct MOJOSHADER_effectSamplerMap
	{
		public MOJOSHADER_symbolType type;

		public IntPtr name;
	}

	private struct MOJOSHADER_effectString
	{
		public MOJOSHADER_symbolType type;

		public IntPtr stringvalue;
	}

	private struct MOJOSHADER_effectTexture
	{
		public MOJOSHADER_symbolType type;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct MOJOSHADER_effectObject
	{
		[FieldOffset(0)]
		public MOJOSHADER_symbolType type;

		[FieldOffset(0)]
		public MOJOSHADER_effectShader shader;

		[FieldOffset(0)]
		public MOJOSHADER_effectSamplerMap mapping;

		[FieldOffset(0)]
		public MOJOSHADER_effectString stringvalue;

		[FieldOffset(0)]
		public MOJOSHADER_effectTexture texture;
	}

	private struct MOJOSHADER_samplerStateRegister
	{
		public IntPtr sampler_name;

		public uint sampler_register;

		public uint sampler_state_count;

		public IntPtr sampler_states;
	}

	internal struct MOJOSHADER_effectStateChanges
	{
		public uint render_state_change_count;

		public IntPtr render_state_changes;

		public uint sampler_state_change_count;

		public IntPtr sampler_state_changes;

		public uint vertex_sampler_state_change_count;

		public IntPtr vertex_sampler_state_changes;
	}

	private struct MOJOSHADER_effect
	{
		public int error_count;

		public IntPtr errors;

		public int param_count;

		public IntPtr parameters;

		public int technique_count;

		public IntPtr techniques;

		public int object_count;

		public IntPtr objects;
	}

	private EffectTechnique INTERNAL_currentTechnique;

	internal IntPtr glEffect;

	private Dictionary<IntPtr, EffectParameter> samplerMap = new Dictionary<IntPtr, EffectParameter>(new IntPtrBoxlessComparer());

	private IntPtr stateChangesPtr;

	private static readonly EffectParameterType[] XNAType = new EffectParameterType[10]
	{
		EffectParameterType.Void,
		EffectParameterType.Bool,
		EffectParameterType.Int32,
		EffectParameterType.Single,
		EffectParameterType.String,
		EffectParameterType.Texture,
		EffectParameterType.Texture1D,
		EffectParameterType.Texture2D,
		EffectParameterType.Texture3D,
		EffectParameterType.TextureCube
	};

	private static readonly EffectParameterClass[] XNAClass = new EffectParameterClass[6]
	{
		EffectParameterClass.Scalar,
		EffectParameterClass.Vector,
		EffectParameterClass.Matrix,
		EffectParameterClass.Matrix,
		EffectParameterClass.Object,
		EffectParameterClass.Struct
	};

	private static readonly Blend[] XNABlend = new Blend[16]
	{
		(Blend)(-1),
		Blend.Zero,
		Blend.One,
		Blend.SourceColor,
		Blend.InverseSourceColor,
		Blend.SourceAlpha,
		Blend.InverseSourceAlpha,
		Blend.DestinationAlpha,
		Blend.InverseDestinationAlpha,
		Blend.DestinationColor,
		Blend.InverseDestinationColor,
		Blend.SourceAlphaSaturation,
		(Blend)(-1),
		(Blend)(-1),
		Blend.BlendFactor,
		Blend.InverseBlendFactor
	};

	private static readonly BlendFunction[] XNABlendOp = new BlendFunction[6]
	{
		(BlendFunction)(-1),
		BlendFunction.Add,
		BlendFunction.Subtract,
		BlendFunction.ReverseSubtract,
		BlendFunction.Min,
		BlendFunction.Max
	};

	private static readonly CompareFunction[] XNACompare = new CompareFunction[9]
	{
		(CompareFunction)(-1),
		CompareFunction.Never,
		CompareFunction.Less,
		CompareFunction.Equal,
		CompareFunction.LessEqual,
		CompareFunction.Greater,
		CompareFunction.NotEqual,
		CompareFunction.GreaterEqual,
		CompareFunction.Always
	};

	private static readonly StencilOperation[] XNAStencilOp = new StencilOperation[9]
	{
		(StencilOperation)(-1),
		StencilOperation.Keep,
		StencilOperation.Zero,
		StencilOperation.Replace,
		StencilOperation.IncrementSaturation,
		StencilOperation.DecrementSaturation,
		StencilOperation.Invert,
		StencilOperation.Increment,
		StencilOperation.Decrement
	};

	private static readonly TextureAddressMode[] XNAAddress = new TextureAddressMode[4]
	{
		(TextureAddressMode)(-1),
		TextureAddressMode.Wrap,
		TextureAddressMode.Mirror,
		TextureAddressMode.Clamp
	};

	private static readonly MOJOSHADER_textureFilterType[] XNAMag = new MOJOSHADER_textureFilterType[9]
	{
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR
	};

	private static readonly MOJOSHADER_textureFilterType[] XNAMin = new MOJOSHADER_textureFilterType[9]
	{
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT
	};

	private static readonly MOJOSHADER_textureFilterType[] XNAMip = new MOJOSHADER_textureFilterType[9]
	{
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR,
		MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT
	};

	public EffectTechnique CurrentTechnique
	{
		get
		{
			return INTERNAL_currentTechnique;
		}
		set
		{
			FNA3D.FNA3D_SetEffectTechnique(GraphicsDevice.GLDevice, glEffect, value.TechniquePointer);
			INTERNAL_currentTechnique = value;
		}
	}

	public EffectParameterCollection Parameters { get; private set; }

	public EffectTechniqueCollection Techniques { get; private set; }

	public unsafe Effect(GraphicsDevice graphicsDevice, byte[] effectCode)
	{
		GraphicsDevice = graphicsDevice;
		FNA3D.FNA3D_CreateEffect(graphicsDevice.GLDevice, effectCode, effectCode.Length, out glEffect, out var effectData);
		INTERNAL_parseEffectStruct(effectData);
		CurrentTechnique = Techniques[0];
		stateChangesPtr = Marshal.AllocHGlobal(sizeof(MOJOSHADER_effectStateChanges));
		MOJOSHADER_effectStateChanges* ptr = (MOJOSHADER_effectStateChanges*)(void*)stateChangesPtr;
		ptr->render_state_change_count = 0u;
		ptr->sampler_state_change_count = 0u;
		ptr->vertex_sampler_state_change_count = 0u;
	}

	protected unsafe Effect(Effect cloneSource)
	{
		GraphicsDevice = cloneSource.GraphicsDevice;
		FNA3D.FNA3D_CloneEffect(GraphicsDevice.GLDevice, cloneSource.glEffect, out glEffect, out var effectData);
		INTERNAL_parseEffectStruct(effectData);
		for (int i = 0; i < cloneSource.Parameters.Count; i++)
		{
			Parameters[i].texture = cloneSource.Parameters[i].texture;
		}
		for (int j = 0; j < cloneSource.Techniques.Count; j++)
		{
			if (cloneSource.Techniques[j] == cloneSource.CurrentTechnique)
			{
				CurrentTechnique = Techniques[j];
			}
		}
		stateChangesPtr = Marshal.AllocHGlobal(sizeof(MOJOSHADER_effectStateChanges));
		MOJOSHADER_effectStateChanges* ptr = (MOJOSHADER_effectStateChanges*)(void*)stateChangesPtr;
		ptr->render_state_change_count = 0u;
		ptr->sampler_state_change_count = 0u;
		ptr->vertex_sampler_state_change_count = 0u;
	}

	public virtual Effect Clone()
	{
		return new Effect(this);
	}

	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (glEffect != IntPtr.Zero)
			{
				FNA3D.FNA3D_AddDisposeEffect(GraphicsDevice.GLDevice, glEffect);
			}
			if (stateChangesPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(stateChangesPtr);
				stateChangesPtr = IntPtr.Zero;
			}
		}
		base.Dispose(disposing);
	}

	protected internal virtual void OnApply()
	{
	}

	internal unsafe void INTERNAL_applyEffect(uint pass)
	{
		FNA3D.FNA3D_ApplyEffect(GraphicsDevice.GLDevice, glEffect, pass, stateChangesPtr);
		MOJOSHADER_effectStateChanges* ptr = (MOJOSHADER_effectStateChanges*)(void*)stateChangesPtr;
		if (ptr->render_state_change_count != 0)
		{
			PipelineCache pipelineCache = GraphicsDevice.PipelineCache;
			pipelineCache.BeginApplyBlend();
			pipelineCache.BeginApplyDepthStencil();
			pipelineCache.BeginApplyRasterizer();
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			MOJOSHADER_effectState* ptr2 = (MOJOSHADER_effectState*)(void*)ptr->render_state_changes;
			for (int i = 0; i < ptr->render_state_change_count; i++)
			{
				MOJOSHADER_renderStateType type = ptr2[i].type;
				if (type == MOJOSHADER_renderStateType.MOJOSHADER_RS_VERTEXSHADER || type == MOJOSHADER_renderStateType.MOJOSHADER_RS_PIXELSHADER)
				{
					continue;
				}
				switch (type)
				{
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_ZENABLE:
				{
					MOJOSHADER_zBufferType* ptr32 = (MOJOSHADER_zBufferType*)(void*)ptr2[i].value.values;
					pipelineCache.DepthBufferEnable = *ptr32 == MOJOSHADER_zBufferType.MOJOSHADER_ZB_TRUE;
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_FILLMODE:
				{
					MOJOSHADER_fillMode* ptr37 = (MOJOSHADER_fillMode*)(void*)ptr2[i].value.values;
					if (*ptr37 == MOJOSHADER_fillMode.MOJOSHADER_FILL_SOLID)
					{
						pipelineCache.FillMode = FillMode.Solid;
					}
					else if (*ptr37 == MOJOSHADER_fillMode.MOJOSHADER_FILL_WIREFRAME)
					{
						pipelineCache.FillMode = FillMode.WireFrame;
					}
					flag3 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_ZWRITEENABLE:
				{
					int* ptr36 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.DepthBufferWriteEnable = *ptr36 == 1;
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_SRCBLEND:
				{
					MOJOSHADER_blendMode* ptr38 = (MOJOSHADER_blendMode*)(void*)ptr2[i].value.values;
					pipelineCache.ColorSourceBlend = XNABlend[(int)(*ptr38)];
					if (!pipelineCache.SeparateAlphaBlend)
					{
						pipelineCache.AlphaSourceBlend = XNABlend[(int)(*ptr38)];
					}
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_DESTBLEND:
				{
					MOJOSHADER_blendMode* ptr35 = (MOJOSHADER_blendMode*)(void*)ptr2[i].value.values;
					pipelineCache.ColorDestinationBlend = XNABlend[(int)(*ptr35)];
					if (!pipelineCache.SeparateAlphaBlend)
					{
						pipelineCache.AlphaDestinationBlend = XNABlend[(int)(*ptr35)];
					}
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_CULLMODE:
				{
					MOJOSHADER_cullMode* ptr34 = (MOJOSHADER_cullMode*)(void*)ptr2[i].value.values;
					if (*ptr34 == MOJOSHADER_cullMode.MOJOSHADER_CULL_NONE)
					{
						pipelineCache.CullMode = CullMode.None;
					}
					else if (*ptr34 == MOJOSHADER_cullMode.MOJOSHADER_CULL_CW)
					{
						pipelineCache.CullMode = CullMode.CullClockwiseFace;
					}
					else if (*ptr34 == MOJOSHADER_cullMode.MOJOSHADER_CULL_CCW)
					{
						pipelineCache.CullMode = CullMode.CullCounterClockwiseFace;
					}
					flag3 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_ZFUNC:
				{
					MOJOSHADER_compareFunc* ptr33 = (MOJOSHADER_compareFunc*)(void*)ptr2[i].value.values;
					pipelineCache.DepthBufferFunction = XNACompare[(int)(*ptr33)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_ALPHABLENDENABLE:
				{
					int* ptr31 = (int*)(void*)ptr2[i].value.values;
					if (*ptr31 == 0)
					{
						pipelineCache.ColorSourceBlend = Blend.One;
						pipelineCache.ColorDestinationBlend = Blend.Zero;
						pipelineCache.AlphaSourceBlend = Blend.One;
						pipelineCache.AlphaDestinationBlend = Blend.Zero;
						flag = true;
					}
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILENABLE:
				{
					int* ptr30 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.StencilEnable = *ptr30 == 1;
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILFAIL:
				{
					MOJOSHADER_stencilOp* ptr29 = (MOJOSHADER_stencilOp*)(void*)ptr2[i].value.values;
					pipelineCache.StencilFail = XNAStencilOp[(int)(*ptr29)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILZFAIL:
				{
					MOJOSHADER_stencilOp* ptr28 = (MOJOSHADER_stencilOp*)(void*)ptr2[i].value.values;
					pipelineCache.StencilDepthBufferFail = XNAStencilOp[(int)(*ptr28)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILPASS:
				{
					MOJOSHADER_stencilOp* ptr27 = (MOJOSHADER_stencilOp*)(void*)ptr2[i].value.values;
					pipelineCache.StencilPass = XNAStencilOp[(int)(*ptr27)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILFUNC:
				{
					MOJOSHADER_compareFunc* ptr26 = (MOJOSHADER_compareFunc*)(void*)ptr2[i].value.values;
					pipelineCache.StencilFunction = XNACompare[(int)(*ptr26)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILREF:
				{
					int* ptr25 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.ReferenceStencil = *ptr25;
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILMASK:
				{
					int* ptr24 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.StencilMask = *ptr24;
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_STENCILWRITEMASK:
				{
					int* ptr23 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.StencilWriteMask = *ptr23;
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_MULTISAMPLEANTIALIAS:
				{
					int* ptr22 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.MultiSampleAntiAlias = *ptr22 == 1;
					flag3 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_MULTISAMPLEMASK:
				{
					int* ptr21 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.MultiSampleMask = *ptr21;
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_COLORWRITEENABLE:
				{
					int* ptr20 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.ColorWriteChannels = (ColorWriteChannels)(*ptr20);
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_BLENDOP:
				{
					MOJOSHADER_blendOp* ptr19 = (MOJOSHADER_blendOp*)(void*)ptr2[i].value.values;
					pipelineCache.ColorBlendFunction = XNABlendOp[(int)(*ptr19)];
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_SCISSORTESTENABLE:
				{
					int* ptr18 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.ScissorTestEnable = *ptr18 == 1;
					flag3 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_SLOPESCALEDEPTHBIAS:
				{
					float* ptr17 = (float*)(void*)ptr2[i].value.values;
					pipelineCache.SlopeScaleDepthBias = *ptr17;
					flag3 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_TWOSIDEDSTENCILMODE:
				{
					int* ptr16 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.TwoSidedStencilMode = *ptr16 == 1;
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_CCW_STENCILFAIL:
				{
					MOJOSHADER_stencilOp* ptr15 = (MOJOSHADER_stencilOp*)(void*)ptr2[i].value.values;
					pipelineCache.CCWStencilFail = XNAStencilOp[(int)(*ptr15)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_CCW_STENCILZFAIL:
				{
					MOJOSHADER_stencilOp* ptr14 = (MOJOSHADER_stencilOp*)(void*)ptr2[i].value.values;
					pipelineCache.CCWStencilDepthBufferFail = XNAStencilOp[(int)(*ptr14)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_CCW_STENCILPASS:
				{
					MOJOSHADER_stencilOp* ptr13 = (MOJOSHADER_stencilOp*)(void*)ptr2[i].value.values;
					pipelineCache.CCWStencilPass = XNAStencilOp[(int)(*ptr13)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_CCW_STENCILFUNC:
				{
					MOJOSHADER_compareFunc* ptr12 = (MOJOSHADER_compareFunc*)(void*)ptr2[i].value.values;
					pipelineCache.CCWStencilFunction = XNACompare[(int)(*ptr12)];
					flag2 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_COLORWRITEENABLE1:
				{
					int* ptr11 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.ColorWriteChannels1 = (ColorWriteChannels)(*ptr11);
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_COLORWRITEENABLE2:
				{
					int* ptr10 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.ColorWriteChannels2 = (ColorWriteChannels)(*ptr10);
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_COLORWRITEENABLE3:
				{
					int* ptr9 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.ColorWriteChannels3 = (ColorWriteChannels)(*ptr9);
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_BLENDFACTOR:
				{
					int* ptr8 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.BlendFactor = new Color((*ptr8 >> 24) & 0xFF, (*ptr8 >> 16) & 0xFF, (*ptr8 >> 8) & 0xFF, *ptr8 & 0xFF);
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_DEPTHBIAS:
				{
					float* ptr7 = (float*)(void*)ptr2[i].value.values;
					pipelineCache.DepthBias = *ptr7;
					flag3 = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_SEPARATEALPHABLENDENABLE:
				{
					int* ptr6 = (int*)(void*)ptr2[i].value.values;
					pipelineCache.SeparateAlphaBlend = *ptr6 == 1;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_SRCBLENDALPHA:
				{
					MOJOSHADER_blendMode* ptr5 = (MOJOSHADER_blendMode*)(void*)ptr2[i].value.values;
					pipelineCache.AlphaSourceBlend = XNABlend[(int)(*ptr5)];
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_DESTBLENDALPHA:
				{
					MOJOSHADER_blendMode* ptr4 = (MOJOSHADER_blendMode*)(void*)ptr2[i].value.values;
					pipelineCache.AlphaDestinationBlend = XNABlend[(int)(*ptr4)];
					flag = true;
					break;
				}
				case MOJOSHADER_renderStateType.MOJOSHADER_RS_BLENDOPALPHA:
				{
					MOJOSHADER_blendOp* ptr3 = (MOJOSHADER_blendOp*)(void*)ptr2[i].value.values;
					pipelineCache.AlphaBlendFunction = XNABlendOp[(int)(*ptr3)];
					flag = true;
					break;
				}
				default:
					throw new NotImplementedException("Unhandled render state! " + type);
				case (MOJOSHADER_renderStateType)178:
					break;
				}
			}
			if (flag)
			{
				pipelineCache.EndApplyBlend();
			}
			if (flag2)
			{
				pipelineCache.EndApplyDepthStencil();
			}
			if (flag3)
			{
				pipelineCache.EndApplyRasterizer();
			}
		}
		if (ptr->sampler_state_change_count != 0)
		{
			INTERNAL_updateSamplers(ptr->sampler_state_change_count, (MOJOSHADER_samplerStateRegister*)(void*)ptr->sampler_state_changes, GraphicsDevice.Textures, GraphicsDevice.SamplerStates);
		}
		if (ptr->vertex_sampler_state_change_count != 0)
		{
			INTERNAL_updateSamplers(ptr->vertex_sampler_state_change_count, (MOJOSHADER_samplerStateRegister*)(void*)ptr->vertex_sampler_state_changes, GraphicsDevice.VertexTextures, GraphicsDevice.VertexSamplerStates);
		}
	}

	private unsafe void INTERNAL_updateSamplers(uint changeCount, MOJOSHADER_samplerStateRegister* registers, TextureCollection textures, SamplerStateCollection samplers)
	{
		for (int i = 0; i < changeCount; i++)
		{
			if (registers[i].sampler_state_count == 0)
			{
				continue;
			}
			int sampler_register = (int)registers[i].sampler_register;
			PipelineCache pipelineCache = GraphicsDevice.PipelineCache;
			pipelineCache.BeginApplySampler(samplers, sampler_register);
			bool flag = false;
			bool flag2 = false;
			TextureFilter filter = pipelineCache.Filter;
			MOJOSHADER_textureFilterType mOJOSHADER_textureFilterType = XNAMag[(int)filter];
			MOJOSHADER_textureFilterType mOJOSHADER_textureFilterType2 = XNAMin[(int)filter];
			MOJOSHADER_textureFilterType mOJOSHADER_textureFilterType3 = XNAMip[(int)filter];
			MOJOSHADER_effectSamplerState* ptr = (MOJOSHADER_effectSamplerState*)(void*)registers[i].sampler_states;
			for (int j = 0; j < registers[i].sampler_state_count; j++)
			{
				MOJOSHADER_samplerStateType type = ptr[j].type;
				switch (type)
				{
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_TEXTURE:
				{
					if (samplerMap.TryGetValue(registers[i].sampler_name, out var value))
					{
						Texture texture = value.texture;
						if (texture != null)
						{
							textures[sampler_register] = texture;
						}
					}
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_ADDRESSU:
				{
					MOJOSHADER_textureAddress* ptr10 = (MOJOSHADER_textureAddress*)(void*)ptr[j].value.values;
					pipelineCache.AddressU = XNAAddress[(int)(*ptr10)];
					flag = true;
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_ADDRESSV:
				{
					MOJOSHADER_textureAddress* ptr9 = (MOJOSHADER_textureAddress*)(void*)ptr[j].value.values;
					pipelineCache.AddressV = XNAAddress[(int)(*ptr9)];
					flag = true;
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_ADDRESSW:
				{
					MOJOSHADER_textureAddress* ptr8 = (MOJOSHADER_textureAddress*)(void*)ptr[j].value.values;
					pipelineCache.AddressW = XNAAddress[(int)(*ptr8)];
					flag = true;
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MAGFILTER:
				{
					MOJOSHADER_textureFilterType* ptr7 = (MOJOSHADER_textureFilterType*)(void*)ptr[j].value.values;
					mOJOSHADER_textureFilterType = *ptr7;
					flag2 = true;
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MINFILTER:
				{
					MOJOSHADER_textureFilterType* ptr6 = (MOJOSHADER_textureFilterType*)(void*)ptr[j].value.values;
					mOJOSHADER_textureFilterType2 = *ptr6;
					flag2 = true;
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MIPFILTER:
				{
					MOJOSHADER_textureFilterType* ptr5 = (MOJOSHADER_textureFilterType*)(void*)ptr[j].value.values;
					mOJOSHADER_textureFilterType3 = *ptr5;
					flag2 = true;
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MIPMAPLODBIAS:
				{
					float* ptr4 = (float*)(void*)ptr[j].value.values;
					pipelineCache.MipMapLODBias = *ptr4;
					flag = true;
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MAXMIPLEVEL:
				{
					int* ptr3 = (int*)(void*)ptr[j].value.values;
					pipelineCache.MaxMipLevel = *ptr3;
					flag = true;
					break;
				}
				case MOJOSHADER_samplerStateType.MOJOSHADER_SAMP_MAXANISOTROPY:
				{
					int* ptr2 = (int*)(void*)ptr[j].value.values;
					pipelineCache.MaxAnisotropy = *ptr2;
					flag = true;
					break;
				}
				default:
					throw new NotImplementedException("Unhandled sampler state! " + type);
				}
			}
			if (flag2)
			{
				if (mOJOSHADER_textureFilterType == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
				{
					if (mOJOSHADER_textureFilterType2 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
					{
						if (mOJOSHADER_textureFilterType3 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_NONE || mOJOSHADER_textureFilterType3 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
						{
							pipelineCache.Filter = TextureFilter.Point;
						}
						else
						{
							if (mOJOSHADER_textureFilterType3 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR && mOJOSHADER_textureFilterType3 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC)
							{
								throw new NotImplementedException("Unhandled mipfilter type! " + mOJOSHADER_textureFilterType3);
							}
							pipelineCache.Filter = TextureFilter.PointMipLinear;
						}
					}
					else
					{
						if (mOJOSHADER_textureFilterType2 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR && mOJOSHADER_textureFilterType2 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC)
						{
							throw new NotImplementedException("Unhandled minfilter type! " + mOJOSHADER_textureFilterType2);
						}
						if (mOJOSHADER_textureFilterType3 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_NONE || mOJOSHADER_textureFilterType3 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
						{
							pipelineCache.Filter = TextureFilter.MinLinearMagPointMipPoint;
						}
						else
						{
							if (mOJOSHADER_textureFilterType3 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR && mOJOSHADER_textureFilterType3 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC)
							{
								throw new NotImplementedException("Unhandled mipfilter type! " + mOJOSHADER_textureFilterType3);
							}
							pipelineCache.Filter = TextureFilter.MinLinearMagPointMipLinear;
						}
					}
				}
				else
				{
					if (mOJOSHADER_textureFilterType != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR && mOJOSHADER_textureFilterType != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC)
					{
						throw new NotImplementedException("Unhandled magfilter type! " + mOJOSHADER_textureFilterType);
					}
					if (mOJOSHADER_textureFilterType2 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
					{
						if (mOJOSHADER_textureFilterType3 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_NONE || mOJOSHADER_textureFilterType3 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
						{
							pipelineCache.Filter = TextureFilter.MinPointMagLinearMipPoint;
						}
						else
						{
							if (mOJOSHADER_textureFilterType3 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR && mOJOSHADER_textureFilterType3 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC)
							{
								throw new NotImplementedException("Unhandled mipfilter type! " + mOJOSHADER_textureFilterType3);
							}
							pipelineCache.Filter = TextureFilter.MinPointMagLinearMipLinear;
						}
					}
					else
					{
						if (mOJOSHADER_textureFilterType2 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR && mOJOSHADER_textureFilterType2 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC)
						{
							throw new NotImplementedException("Unhandled minfilter type! " + mOJOSHADER_textureFilterType2);
						}
						if (mOJOSHADER_textureFilterType3 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_NONE || mOJOSHADER_textureFilterType3 == MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_POINT)
						{
							pipelineCache.Filter = TextureFilter.LinearMipPoint;
						}
						else
						{
							if (mOJOSHADER_textureFilterType3 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_LINEAR && mOJOSHADER_textureFilterType3 != MOJOSHADER_textureFilterType.MOJOSHADER_TEXTUREFILTER_ANISOTROPIC)
							{
								throw new NotImplementedException("Unhandled mipfilter type! " + mOJOSHADER_textureFilterType3);
							}
							pipelineCache.Filter = TextureFilter.Linear;
						}
					}
				}
				flag = true;
			}
			if (flag)
			{
				pipelineCache.EndApplySampler(samplers, sampler_register);
			}
		}
	}

	private unsafe void INTERNAL_parseEffectStruct(IntPtr effectData)
	{
		MOJOSHADER_effect* ptr = (MOJOSHADER_effect*)(void*)effectData;
		MOJOSHADER_effectParam* ptr2 = (MOJOSHADER_effectParam*)(void*)ptr->parameters;
		List<EffectParameter> list = new List<EffectParameter>();
		for (int i = 0; i < ptr->param_count; i++)
		{
			MOJOSHADER_effectParam mOJOSHADER_effectParam = ptr2[i];
			if (mOJOSHADER_effectParam.value.type.parameter_type == MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_VERTEXSHADER || mOJOSHADER_effectParam.value.type.parameter_type == MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_PIXELSHADER)
			{
				continue;
			}
			if (mOJOSHADER_effectParam.value.type.parameter_type >= MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_SAMPLER && mOJOSHADER_effectParam.value.type.parameter_type <= MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_SAMPLERCUBE)
			{
				string text = string.Empty;
				MOJOSHADER_effectSamplerState* ptr3 = (MOJOSHADER_effectSamplerState*)(void*)mOJOSHADER_effectParam.value.values;
				for (int j = 0; j < mOJOSHADER_effectParam.value.value_count; j++)
				{
					if (ptr3[j].value.type.parameter_type >= MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURE && ptr3[j].value.type.parameter_type <= MOJOSHADER_symbolType.MOJOSHADER_SYMTYPE_TEXTURECUBE)
					{
						MOJOSHADER_effectObject* ptr4 = (MOJOSHADER_effectObject*)(void*)ptr->objects;
						int* ptr5 = (int*)(void*)ptr3[j].value.values;
						text = Marshal.PtrToStringAnsi(ptr4[*ptr5].mapping.name);
						break;
					}
				}
				for (int k = 0; k < list.Count; k++)
				{
					if (text.Equals(list[k].Name))
					{
						samplerMap[mOJOSHADER_effectParam.value.name] = list[k];
						break;
					}
				}
				continue;
			}
			EffectParameterCollection structureMembers = null;
			if (mOJOSHADER_effectParam.value.type.member_count != 0)
			{
				List<EffectParameter> list2 = new List<EffectParameter>();
				MOJOSHADER_symbolStructMember* ptr6 = (MOJOSHADER_symbolStructMember*)(void*)mOJOSHADER_effectParam.value.type.members;
				IntPtr zero = IntPtr.Zero;
				for (int l = 0; l < mOJOSHADER_effectParam.value.type.member_count; l++)
				{
					uint num = ptr6[l].info.rows * ptr6[l].info.columns;
					if (ptr6[l].info.elements != 0)
					{
						num *= ptr6[l].info.elements;
					}
					list2.Add(new EffectParameter(Marshal.PtrToStringAnsi(ptr6[l].name), null, (int)ptr6[l].info.rows, (int)ptr6[l].info.columns, (int)ptr6[l].info.elements, XNAClass[(int)ptr6[l].info.parameter_class], XNAType[(int)ptr6[l].info.parameter_type], null, null, mOJOSHADER_effectParam.value.values + zero.ToInt32(), num * 4));
					zero += (int)(num * 4);
				}
				structureMembers = new EffectParameterCollection(list2);
			}
			list.Add(new EffectParameter(Marshal.PtrToStringAnsi(mOJOSHADER_effectParam.value.name), Marshal.PtrToStringAnsi(mOJOSHADER_effectParam.value.semantic), (int)mOJOSHADER_effectParam.value.type.rows, (int)mOJOSHADER_effectParam.value.type.columns, (int)mOJOSHADER_effectParam.value.type.elements, XNAClass[(int)mOJOSHADER_effectParam.value.type.parameter_class], XNAType[(int)mOJOSHADER_effectParam.value.type.parameter_type], structureMembers, INTERNAL_readAnnotations(mOJOSHADER_effectParam.annotations, mOJOSHADER_effectParam.annotation_count), mOJOSHADER_effectParam.value.values, mOJOSHADER_effectParam.value.value_count * 4));
		}
		Parameters = new EffectParameterCollection(list);
		MOJOSHADER_effectTechnique* ptr7 = (MOJOSHADER_effectTechnique*)(void*)ptr->techniques;
		List<EffectTechnique> list3 = new List<EffectTechnique>(ptr->technique_count);
		int num2 = 0;
		while (num2 < list3.Capacity)
		{
			MOJOSHADER_effectPass* ptr8 = (MOJOSHADER_effectPass*)(void*)ptr7->passes;
			List<EffectPass> list4 = new List<EffectPass>((int)ptr7->pass_count);
			for (int m = 0; m < list4.Capacity; m++)
			{
				MOJOSHADER_effectPass mOJOSHADER_effectPass = ptr8[m];
				list4.Add(new EffectPass(Marshal.PtrToStringAnsi(mOJOSHADER_effectPass.name), INTERNAL_readAnnotations(mOJOSHADER_effectPass.annotations, mOJOSHADER_effectPass.annotation_count), this, (IntPtr)ptr7, (uint)m));
			}
			list3.Add(new EffectTechnique(Marshal.PtrToStringAnsi(ptr7->name), (IntPtr)ptr7, new EffectPassCollection(list4), INTERNAL_readAnnotations(ptr7->annotations, ptr7->annotation_count)));
			num2++;
			ptr7++;
		}
		Techniques = new EffectTechniqueCollection(list3);
	}

	private unsafe EffectAnnotationCollection INTERNAL_readAnnotations(IntPtr rawAnnotations, uint numAnnotations)
	{
		MOJOSHADER_effectAnnotation* ptr = (MOJOSHADER_effectAnnotation*)(void*)rawAnnotations;
		List<EffectAnnotation> list = new List<EffectAnnotation>((int)numAnnotations);
		for (int i = 0; i < numAnnotations; i++)
		{
			MOJOSHADER_effectAnnotation mOJOSHADER_effectAnnotation = ptr[i];
			list.Add(new EffectAnnotation(Marshal.PtrToStringAnsi(mOJOSHADER_effectAnnotation.name), Marshal.PtrToStringAnsi(mOJOSHADER_effectAnnotation.semantic), (int)mOJOSHADER_effectAnnotation.type.rows, (int)mOJOSHADER_effectAnnotation.type.columns, XNAClass[(int)mOJOSHADER_effectAnnotation.type.parameter_class], XNAType[(int)mOJOSHADER_effectAnnotation.type.parameter_type], mOJOSHADER_effectAnnotation.values));
		}
		return new EffectAnnotationCollection(list);
	}
}
