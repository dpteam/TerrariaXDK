namespace Microsoft.Xna.Framework.Graphics;

public class RasterizerState : GraphicsResource
{
	public static readonly RasterizerState CullClockwise = new RasterizerState("RasterizerState.CullClockwise", CullMode.CullClockwiseFace);

	public static readonly RasterizerState CullCounterClockwise = new RasterizerState("RasterizerState.CullCounterClockwise", CullMode.CullCounterClockwiseFace);

	public static readonly RasterizerState CullNone = new RasterizerState("RasterizerState.CullNone", CullMode.None);

	internal FNA3D.FNA3D_RasterizerState state;

	public CullMode CullMode
	{
		get
		{
			return state.cullMode;
		}
		set
		{
			state.cullMode = value;
		}
	}

	public float DepthBias
	{
		get
		{
			return state.depthBias;
		}
		set
		{
			state.depthBias = value;
		}
	}

	public FillMode FillMode
	{
		get
		{
			return state.fillMode;
		}
		set
		{
			state.fillMode = value;
		}
	}

	public bool MultiSampleAntiAlias
	{
		get
		{
			return state.multiSampleAntiAlias == 1;
		}
		set
		{
			state.multiSampleAntiAlias = (byte)(value ? 1u : 0u);
		}
	}

	public bool ScissorTestEnable
	{
		get
		{
			return state.scissorTestEnable == 1;
		}
		set
		{
			state.scissorTestEnable = (byte)(value ? 1u : 0u);
		}
	}

	public float SlopeScaleDepthBias
	{
		get
		{
			return state.slopeScaleDepthBias;
		}
		set
		{
			state.slopeScaleDepthBias = value;
		}
	}

	public RasterizerState()
	{
		CullMode = CullMode.CullCounterClockwiseFace;
		FillMode = FillMode.Solid;
		DepthBias = 0f;
		MultiSampleAntiAlias = true;
		ScissorTestEnable = false;
		SlopeScaleDepthBias = 0f;
	}

	private RasterizerState(string name, CullMode cullMode)
		: this()
	{
		Name = name;
		CullMode = cullMode;
	}
}
