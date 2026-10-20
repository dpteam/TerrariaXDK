using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

internal class PipelineCache
{
	private GraphicsDevice device;

	public BlendFunction AlphaBlendFunction;

	public Blend AlphaDestinationBlend;

	public Blend AlphaSourceBlend;

	public BlendFunction ColorBlendFunction;

	public Blend ColorDestinationBlend;

	public Blend ColorSourceBlend;

	public ColorWriteChannels ColorWriteChannels;

	public ColorWriteChannels ColorWriteChannels1;

	public ColorWriteChannels ColorWriteChannels2;

	public ColorWriteChannels ColorWriteChannels3;

	public Color BlendFactor;

	public int MultiSampleMask;

	public bool SeparateAlphaBlend;

	private Dictionary<StateHash, BlendState> blendCache = new Dictionary<StateHash, BlendState>();

	public bool DepthBufferEnable;

	public bool DepthBufferWriteEnable;

	public CompareFunction DepthBufferFunction;

	public bool StencilEnable;

	public CompareFunction StencilFunction;

	public StencilOperation StencilPass;

	public StencilOperation StencilFail;

	public StencilOperation StencilDepthBufferFail;

	public bool TwoSidedStencilMode;

	public CompareFunction CCWStencilFunction;

	public StencilOperation CCWStencilFail;

	public StencilOperation CCWStencilPass;

	public StencilOperation CCWStencilDepthBufferFail;

	public int StencilMask;

	public int StencilWriteMask;

	public int ReferenceStencil;

	private Dictionary<StateHash, DepthStencilState> depthStencilCache = new Dictionary<StateHash, DepthStencilState>();

	public CullMode CullMode;

	public FillMode FillMode;

	public float DepthBias;

	public bool MultiSampleAntiAlias;

	public bool ScissorTestEnable;

	public float SlopeScaleDepthBias;

	private Dictionary<StateHash, RasterizerState> rasterizerCache = new Dictionary<StateHash, RasterizerState>();

	public TextureAddressMode AddressU;

	public TextureAddressMode AddressV;

	public TextureAddressMode AddressW;

	public int MaxAnisotropy;

	public int MaxMipLevel;

	public float MipMapLODBias;

	public TextureFilter Filter;

	private Dictionary<StateHash, SamplerState> samplerCache = new Dictionary<StateHash, SamplerState>();

	private const ulong HASH_FACTOR = 39uL;

	public PipelineCache(GraphicsDevice graphicsDevice)
	{
		device = graphicsDevice;
	}

	private static StateHash GetBlendHash(BlendFunction alphaBlendFunc, Blend alphaDestBlend, Blend alphaSrcBlend, BlendFunction colorBlendFunc, Blend colorDestBlend, Blend colorSrcBlend, ColorWriteChannels channels, ColorWriteChannels channels1, ColorWriteChannels channels2, ColorWriteChannels channels3, Color blendFactor, int multisampleMask)
	{
		int num = ((int)alphaBlendFunc << 4) | (int)colorBlendFunc;
		int num2 = ((int)alphaDestBlend << 28) | ((int)alphaSrcBlend << 24) | ((int)colorDestBlend << 20) | ((int)colorSrcBlend << 16) | ((int)channels << 12) | ((int)channels1 << 8) | ((int)channels2 << 4) | (int)channels3;
		return new StateHash((ulong)(((long)num << 32) | num2), (ulong)(((long)multisampleMask << 32) | blendFactor.PackedValue));
	}

	public static StateHash GetBlendHash(BlendState state)
	{
		return GetBlendHash(state.AlphaBlendFunction, state.AlphaDestinationBlend, state.AlphaSourceBlend, state.ColorBlendFunction, state.ColorDestinationBlend, state.ColorSourceBlend, state.ColorWriteChannels, state.ColorWriteChannels1, state.ColorWriteChannels2, state.ColorWriteChannels3, state.BlendFactor, state.MultiSampleMask);
	}

	public void BeginApplyBlend()
	{
		BlendState blendState = device.BlendState;
		AlphaBlendFunction = blendState.AlphaBlendFunction;
		AlphaDestinationBlend = blendState.AlphaDestinationBlend;
		AlphaSourceBlend = blendState.AlphaSourceBlend;
		ColorBlendFunction = blendState.ColorBlendFunction;
		ColorDestinationBlend = blendState.ColorDestinationBlend;
		ColorSourceBlend = blendState.ColorSourceBlend;
		ColorWriteChannels = blendState.ColorWriteChannels;
		ColorWriteChannels1 = blendState.ColorWriteChannels1;
		ColorWriteChannels2 = blendState.ColorWriteChannels2;
		ColorWriteChannels3 = blendState.ColorWriteChannels3;
		BlendFactor = blendState.BlendFactor;
		MultiSampleMask = blendState.MultiSampleMask;
		SeparateAlphaBlend = ColorBlendFunction != AlphaBlendFunction || ColorDestinationBlend != AlphaDestinationBlend;
	}

	public void EndApplyBlend()
	{
		StateHash blendHash = GetBlendHash(AlphaBlendFunction, AlphaDestinationBlend, AlphaSourceBlend, ColorBlendFunction, ColorDestinationBlend, ColorSourceBlend, ColorWriteChannels, ColorWriteChannels1, ColorWriteChannels2, ColorWriteChannels3, BlendFactor, MultiSampleMask);
		if (!blendCache.TryGetValue(blendHash, out var value))
		{
			value = new BlendState();
			value.AlphaBlendFunction = AlphaBlendFunction;
			value.AlphaDestinationBlend = AlphaDestinationBlend;
			value.AlphaSourceBlend = AlphaSourceBlend;
			value.ColorBlendFunction = ColorBlendFunction;
			value.ColorDestinationBlend = ColorDestinationBlend;
			value.ColorSourceBlend = ColorSourceBlend;
			value.ColorWriteChannels = ColorWriteChannels;
			value.ColorWriteChannels1 = ColorWriteChannels1;
			value.ColorWriteChannels2 = ColorWriteChannels2;
			value.ColorWriteChannels3 = ColorWriteChannels3;
			value.BlendFactor = BlendFactor;
			value.MultiSampleMask = MultiSampleMask;
			blendCache.Add(blendHash, value);
		}
		device.BlendState = value;
	}

	private static StateHash GetDepthStencilHash(bool depthBufferEnable, bool depthWriteEnable, CompareFunction depthFunc, bool stencilEnable, CompareFunction stencilFunc, StencilOperation stencilPass, StencilOperation stencilFail, StencilOperation stencilDepthFail, bool twoSidedStencil, CompareFunction ccwStencilFunc, StencilOperation ccwStencilPass, StencilOperation ccwStencilFail, StencilOperation ccwStencilDepthFail, int stencilMask, int stencilWriteMask, int referenceStencil)
	{
		int num = (depthBufferEnable ? 1 : 0);
		int num2 = (depthWriteEnable ? 1 : 0);
		int num3 = (stencilEnable ? 1 : 0);
		int num4 = (twoSidedStencil ? 1 : 0);
		int num5 = (num << 30) | (num2 << 29) | (num3 << 28) | (num4 << 27) | ((int)depthFunc << 24) | ((int)stencilFunc << 21) | ((int)ccwStencilFunc << 18) | ((int)stencilPass << 15) | ((int)stencilFail << 12) | ((int)stencilDepthFail << 9) | ((int)ccwStencilFail << 6) | ((int)ccwStencilPass << 3) | (int)ccwStencilDepthFail;
		return new StateHash((ulong)(((long)stencilMask << 32) | num5), (ulong)(((long)referenceStencil << 32) | stencilWriteMask));
	}

	public static StateHash GetDepthStencilHash(DepthStencilState state)
	{
		return GetDepthStencilHash(state.DepthBufferEnable, state.DepthBufferWriteEnable, state.DepthBufferFunction, state.StencilEnable, state.StencilFunction, state.StencilPass, state.StencilFail, state.StencilDepthBufferFail, state.TwoSidedStencilMode, state.CounterClockwiseStencilFunction, state.CounterClockwiseStencilPass, state.CounterClockwiseStencilFail, state.CounterClockwiseStencilDepthBufferFail, state.StencilMask, state.StencilWriteMask, state.ReferenceStencil);
	}

	public void BeginApplyDepthStencil()
	{
		DepthStencilState depthStencilState = device.DepthStencilState;
		DepthBufferEnable = depthStencilState.DepthBufferEnable;
		DepthBufferWriteEnable = depthStencilState.DepthBufferWriteEnable;
		DepthBufferFunction = depthStencilState.DepthBufferFunction;
		StencilEnable = depthStencilState.StencilEnable;
		StencilFunction = depthStencilState.StencilFunction;
		StencilPass = depthStencilState.StencilPass;
		StencilFail = depthStencilState.StencilFail;
		StencilDepthBufferFail = depthStencilState.StencilDepthBufferFail;
		TwoSidedStencilMode = depthStencilState.TwoSidedStencilMode;
		CCWStencilFunction = depthStencilState.CounterClockwiseStencilFunction;
		CCWStencilFail = depthStencilState.CounterClockwiseStencilFail;
		CCWStencilPass = depthStencilState.CounterClockwiseStencilPass;
		CCWStencilDepthBufferFail = depthStencilState.CounterClockwiseStencilDepthBufferFail;
		StencilMask = depthStencilState.StencilMask;
		StencilWriteMask = depthStencilState.StencilWriteMask;
		ReferenceStencil = depthStencilState.ReferenceStencil;
	}

	public void EndApplyDepthStencil()
	{
		StateHash depthStencilHash = GetDepthStencilHash(DepthBufferEnable, DepthBufferWriteEnable, DepthBufferFunction, StencilEnable, StencilFunction, StencilPass, StencilFail, StencilDepthBufferFail, TwoSidedStencilMode, CCWStencilFunction, CCWStencilPass, CCWStencilFail, CCWStencilDepthBufferFail, StencilMask, StencilWriteMask, ReferenceStencil);
		if (!depthStencilCache.TryGetValue(depthStencilHash, out var value))
		{
			value = new DepthStencilState();
			value.DepthBufferEnable = DepthBufferEnable;
			value.DepthBufferWriteEnable = DepthBufferWriteEnable;
			value.DepthBufferFunction = DepthBufferFunction;
			value.StencilEnable = StencilEnable;
			value.StencilFunction = StencilFunction;
			value.StencilPass = StencilPass;
			value.StencilFail = StencilFail;
			value.StencilDepthBufferFail = StencilDepthBufferFail;
			value.TwoSidedStencilMode = TwoSidedStencilMode;
			value.CounterClockwiseStencilFunction = CCWStencilFunction;
			value.CounterClockwiseStencilFail = CCWStencilFail;
			value.CounterClockwiseStencilPass = CCWStencilPass;
			value.CounterClockwiseStencilDepthBufferFail = CCWStencilDepthBufferFail;
			value.StencilMask = StencilMask;
			value.StencilWriteMask = StencilWriteMask;
			value.ReferenceStencil = ReferenceStencil;
			depthStencilCache.Add(depthStencilHash, value);
		}
		device.DepthStencilState = value;
	}

	private static StateHash GetRasterizerHash(CullMode cullMode, FillMode fillMode, float depthBias, bool msaa, bool scissor, float slopeScaleDepthBias)
	{
		int num = (msaa ? 1 : 0);
		int num2 = (scissor ? 1 : 0);
		int num3 = (num << 4) | (num2 << 3) | ((int)cullMode << 1) | (int)fillMode;
		return new StateHash((ulong)num3, (FloatToULong(slopeScaleDepthBias) << 32) | FloatToULong(depthBias));
	}

	public static StateHash GetRasterizerHash(RasterizerState state)
	{
		return GetRasterizerHash(state.CullMode, state.FillMode, state.DepthBias, state.MultiSampleAntiAlias, state.ScissorTestEnable, state.SlopeScaleDepthBias);
	}

	public void BeginApplyRasterizer()
	{
		RasterizerState rasterizerState = device.RasterizerState;
		CullMode = rasterizerState.CullMode;
		FillMode = rasterizerState.FillMode;
		DepthBias = rasterizerState.DepthBias;
		MultiSampleAntiAlias = rasterizerState.MultiSampleAntiAlias;
		ScissorTestEnable = rasterizerState.ScissorTestEnable;
		SlopeScaleDepthBias = rasterizerState.SlopeScaleDepthBias;
	}

	public void EndApplyRasterizer()
	{
		StateHash rasterizerHash = GetRasterizerHash(CullMode, FillMode, DepthBias, MultiSampleAntiAlias, ScissorTestEnable, SlopeScaleDepthBias);
		if (!rasterizerCache.TryGetValue(rasterizerHash, out var value))
		{
			value = new RasterizerState();
			value.CullMode = CullMode;
			value.FillMode = FillMode;
			value.DepthBias = DepthBias;
			value.MultiSampleAntiAlias = MultiSampleAntiAlias;
			value.ScissorTestEnable = ScissorTestEnable;
			value.SlopeScaleDepthBias = SlopeScaleDepthBias;
			rasterizerCache.Add(rasterizerHash, value);
		}
		device.RasterizerState = value;
	}

	private static StateHash GetSamplerHash(TextureAddressMode addressU, TextureAddressMode addressV, TextureAddressMode addressW, int maxAnisotropy, int maxMipLevel, float mipLODBias, TextureFilter filter)
	{
		int num = ((int)filter << 6) | ((int)addressU << 4) | ((int)addressV << 2) | (int)addressW;
		return new StateHash((ulong)(((long)maxAnisotropy << 32) | num), (FloatToULong(mipLODBias) << 32) | (ulong)maxMipLevel);
	}

	public static StateHash GetSamplerHash(SamplerState state)
	{
		return GetSamplerHash(state.AddressU, state.AddressV, state.AddressW, state.MaxAnisotropy, state.MaxMipLevel, state.MipMapLevelOfDetailBias, state.Filter);
	}

	public void BeginApplySampler(SamplerStateCollection samplers, int register)
	{
		SamplerState samplerState = samplers[register];
		AddressU = samplerState.AddressU;
		AddressV = samplerState.AddressV;
		AddressW = samplerState.AddressW;
		MaxAnisotropy = samplerState.MaxAnisotropy;
		MaxMipLevel = samplerState.MaxMipLevel;
		MipMapLODBias = samplerState.MipMapLevelOfDetailBias;
		Filter = samplerState.Filter;
	}

	public void EndApplySampler(SamplerStateCollection samplers, int register)
	{
		StateHash samplerHash = GetSamplerHash(AddressU, AddressV, AddressW, MaxAnisotropy, MaxMipLevel, MipMapLODBias, Filter);
		if (!samplerCache.TryGetValue(samplerHash, out var value))
		{
			value = new SamplerState();
			value.Filter = Filter;
			value.AddressU = AddressU;
			value.AddressV = AddressV;
			value.AddressW = AddressW;
			value.MaxAnisotropy = MaxAnisotropy;
			value.MaxMipLevel = MaxMipLevel;
			value.MipMapLevelOfDetailBias = MipMapLODBias;
			samplerCache.Add(samplerHash, value);
		}
		samplers[register] = value;
	}

	public static ulong GetVertexDeclarationHash(VertexDeclaration declaration, ulong vertexShader)
	{
		ulong num = vertexShader;
		for (int i = 0; i < declaration.elements.Length; i++)
		{
			num = num * 39 + (ulong)declaration.elements[i].GetHashCode();
		}
		return num * 39 + (ulong)declaration.VertexStride;
	}

	public static ulong GetVertexBindingHash(VertexBufferBinding[] bindings, int numBindings, ulong vertexShader)
	{
		ulong num = vertexShader;
		for (int i = 0; i < numBindings; i++)
		{
			VertexBufferBinding vertexBufferBinding = bindings[i];
			num = num * 39 + (ulong)vertexBufferBinding.InstanceFrequency;
			num = num * 39 + GetVertexDeclarationHash(vertexBufferBinding.VertexBuffer.VertexDeclaration, vertexShader);
		}
		return num;
	}

	private unsafe static ulong FloatToULong(float f)
	{
		uint num = *(uint*)(&f);
		return num;
	}
}
