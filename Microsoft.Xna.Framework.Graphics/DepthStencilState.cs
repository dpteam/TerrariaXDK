namespace Microsoft.Xna.Framework.Graphics;

public class DepthStencilState : GraphicsResource
{
	public static readonly DepthStencilState Default = new DepthStencilState("DepthStencilState.Default", depthBufferEnable: true, depthBufferWriteEnable: true);

	public static readonly DepthStencilState DepthRead = new DepthStencilState("DepthStencilState.DepthRead", depthBufferEnable: true, depthBufferWriteEnable: false);

	public static readonly DepthStencilState None = new DepthStencilState("DepthStencilState.None", depthBufferEnable: false, depthBufferWriteEnable: false);

	internal FNA3D.FNA3D_DepthStencilState state;

	public bool DepthBufferEnable
	{
		get
		{
			return state.depthBufferEnable == 1;
		}
		set
		{
			state.depthBufferEnable = (byte)(value ? 1u : 0u);
		}
	}

	public bool DepthBufferWriteEnable
	{
		get
		{
			return state.depthBufferWriteEnable == 1;
		}
		set
		{
			state.depthBufferWriteEnable = (byte)(value ? 1u : 0u);
		}
	}

	public StencilOperation CounterClockwiseStencilDepthBufferFail
	{
		get
		{
			return state.ccwStencilDepthBufferFail;
		}
		set
		{
			state.ccwStencilDepthBufferFail = value;
		}
	}

	public StencilOperation CounterClockwiseStencilFail
	{
		get
		{
			return state.ccwStencilFail;
		}
		set
		{
			state.ccwStencilFail = value;
		}
	}

	public CompareFunction CounterClockwiseStencilFunction
	{
		get
		{
			return state.ccwStencilFunction;
		}
		set
		{
			state.ccwStencilFunction = value;
		}
	}

	public StencilOperation CounterClockwiseStencilPass
	{
		get
		{
			return state.ccwStencilPass;
		}
		set
		{
			state.ccwStencilPass = value;
		}
	}

	public CompareFunction DepthBufferFunction
	{
		get
		{
			return state.depthBufferFunction;
		}
		set
		{
			state.depthBufferFunction = value;
		}
	}

	public int ReferenceStencil
	{
		get
		{
			return state.referenceStencil;
		}
		set
		{
			state.referenceStencil = value;
		}
	}

	public StencilOperation StencilDepthBufferFail
	{
		get
		{
			return state.stencilDepthBufferFail;
		}
		set
		{
			state.stencilDepthBufferFail = value;
		}
	}

	public bool StencilEnable
	{
		get
		{
			return state.stencilEnable == 1;
		}
		set
		{
			state.stencilEnable = (byte)(value ? 1u : 0u);
		}
	}

	public StencilOperation StencilFail
	{
		get
		{
			return state.stencilFail;
		}
		set
		{
			state.stencilFail = value;
		}
	}

	public CompareFunction StencilFunction
	{
		get
		{
			return state.stencilFunction;
		}
		set
		{
			state.stencilFunction = value;
		}
	}

	public int StencilMask
	{
		get
		{
			return state.stencilMask;
		}
		set
		{
			state.stencilMask = value;
		}
	}

	public StencilOperation StencilPass
	{
		get
		{
			return state.stencilPass;
		}
		set
		{
			state.stencilPass = value;
		}
	}

	public int StencilWriteMask
	{
		get
		{
			return state.stencilWriteMask;
		}
		set
		{
			state.stencilWriteMask = value;
		}
	}

	public bool TwoSidedStencilMode
	{
		get
		{
			return state.twoSidedStencilMode == 1;
		}
		set
		{
			state.twoSidedStencilMode = (byte)(value ? 1u : 0u);
		}
	}

	public DepthStencilState()
	{
		DepthBufferEnable = true;
		DepthBufferWriteEnable = true;
		DepthBufferFunction = CompareFunction.LessEqual;
		StencilEnable = false;
		StencilFunction = CompareFunction.Always;
		StencilPass = StencilOperation.Keep;
		StencilFail = StencilOperation.Keep;
		StencilDepthBufferFail = StencilOperation.Keep;
		TwoSidedStencilMode = false;
		CounterClockwiseStencilFunction = CompareFunction.Always;
		CounterClockwiseStencilFail = StencilOperation.Keep;
		CounterClockwiseStencilPass = StencilOperation.Keep;
		CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
		StencilMask = int.MaxValue;
		StencilWriteMask = int.MaxValue;
		ReferenceStencil = 0;
	}

	private DepthStencilState(string name, bool depthBufferEnable, bool depthBufferWriteEnable)
		: this()
	{
		Name = name;
		DepthBufferEnable = depthBufferEnable;
		DepthBufferWriteEnable = depthBufferWriteEnable;
	}
}
