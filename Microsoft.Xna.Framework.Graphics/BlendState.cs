namespace Microsoft.Xna.Framework.Graphics;

public class BlendState : GraphicsResource
{
	public static readonly BlendState Additive = new BlendState("BlendState.Additive", Blend.SourceAlpha, Blend.SourceAlpha, Blend.One, Blend.One);

	public static readonly BlendState AlphaBlend = new BlendState("BlendState.AlphaBlend", Blend.One, Blend.One, Blend.InverseSourceAlpha, Blend.InverseSourceAlpha);

	public static readonly BlendState NonPremultiplied = new BlendState("BlendState.NonPremultiplied", Blend.SourceAlpha, Blend.SourceAlpha, Blend.InverseSourceAlpha, Blend.InverseSourceAlpha);

	public static readonly BlendState Opaque = new BlendState("BlendState.Opaque", Blend.One, Blend.One, Blend.Zero, Blend.Zero);

	internal FNA3D.FNA3D_BlendState state;

	public BlendFunction AlphaBlendFunction
	{
		get
		{
			return state.alphaBlendFunction;
		}
		set
		{
			state.alphaBlendFunction = value;
		}
	}

	public Blend AlphaDestinationBlend
	{
		get
		{
			return state.alphaDestinationBlend;
		}
		set
		{
			state.alphaDestinationBlend = value;
		}
	}

	public Blend AlphaSourceBlend
	{
		get
		{
			return state.alphaSourceBlend;
		}
		set
		{
			state.alphaSourceBlend = value;
		}
	}

	public BlendFunction ColorBlendFunction
	{
		get
		{
			return state.colorBlendFunction;
		}
		set
		{
			state.colorBlendFunction = value;
		}
	}

	public Blend ColorDestinationBlend
	{
		get
		{
			return state.colorDestinationBlend;
		}
		set
		{
			state.colorDestinationBlend = value;
		}
	}

	public Blend ColorSourceBlend
	{
		get
		{
			return state.colorSourceBlend;
		}
		set
		{
			state.colorSourceBlend = value;
		}
	}

	public ColorWriteChannels ColorWriteChannels
	{
		get
		{
			return state.colorWriteEnable;
		}
		set
		{
			state.colorWriteEnable = value;
		}
	}

	public ColorWriteChannels ColorWriteChannels1
	{
		get
		{
			return state.colorWriteEnable1;
		}
		set
		{
			state.colorWriteEnable1 = value;
		}
	}

	public ColorWriteChannels ColorWriteChannels2
	{
		get
		{
			return state.colorWriteEnable2;
		}
		set
		{
			state.colorWriteEnable2 = value;
		}
	}

	public ColorWriteChannels ColorWriteChannels3
	{
		get
		{
			return state.colorWriteEnable3;
		}
		set
		{
			state.colorWriteEnable3 = value;
		}
	}

	public Color BlendFactor
	{
		get
		{
			return state.blendFactor;
		}
		set
		{
			state.blendFactor = value;
		}
	}

	public int MultiSampleMask
	{
		get
		{
			return state.multiSampleMask;
		}
		set
		{
			state.multiSampleMask = value;
		}
	}

	public BlendState()
	{
		AlphaBlendFunction = BlendFunction.Add;
		AlphaDestinationBlend = Blend.Zero;
		AlphaSourceBlend = Blend.One;
		ColorBlendFunction = BlendFunction.Add;
		ColorDestinationBlend = Blend.Zero;
		ColorSourceBlend = Blend.One;
		ColorWriteChannels = ColorWriteChannels.All;
		ColorWriteChannels1 = ColorWriteChannels.All;
		ColorWriteChannels2 = ColorWriteChannels.All;
		ColorWriteChannels3 = ColorWriteChannels.All;
		BlendFactor = Color.White;
		MultiSampleMask = -1;
	}

	private BlendState(string name, Blend colorSourceBlend, Blend alphaSourceBlend, Blend colorDestBlend, Blend alphaDestBlend)
		: this()
	{
		Name = name;
		ColorSourceBlend = colorSourceBlend;
		AlphaSourceBlend = alphaSourceBlend;
		ColorDestinationBlend = colorDestBlend;
		AlphaDestinationBlend = alphaDestBlend;
	}
}
