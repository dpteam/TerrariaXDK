using System;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class GraphicsAdapter
{
	private static ReadOnlyCollection<GraphicsAdapter> adapters;

	public DisplayMode CurrentDisplayMode => FNAPlatform.GetCurrentDisplayMode(Adapters.IndexOf(this));

	public DisplayModeCollection SupportedDisplayModes { get; private set; }

	public string Description { get; private set; }

	public int DeviceId
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public string DeviceName { get; private set; }

	public bool IsDefaultAdapter => this == DefaultAdapter;

	public bool IsWideScreen
	{
		get
		{
			float aspectRatio = CurrentDisplayMode.AspectRatio;
			return aspectRatio > 1.3333334f;
		}
	}

	public IntPtr MonitorHandle
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public int Revision
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public int SubSystemId
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool UseNullDevice { get; set; }

	public bool UseReferenceDevice { get; set; }

	public int VendorId
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public static GraphicsAdapter DefaultAdapter => Adapters[0];

	public static ReadOnlyCollection<GraphicsAdapter> Adapters
	{
		get
		{
			if (adapters == null)
			{
				adapters = new ReadOnlyCollection<GraphicsAdapter>(FNAPlatform.GetGraphicsAdapters());
			}
			return adapters;
		}
	}

	internal GraphicsAdapter(DisplayModeCollection modes, string name, string description)
	{
		SupportedDisplayModes = modes;
		DeviceName = name;
		Description = description;
		UseNullDevice = false;
		UseReferenceDevice = false;
	}

	public bool IsProfileSupported(GraphicsProfile graphicsProfile)
	{
		return true;
	}

	public bool QueryRenderTargetFormat(GraphicsProfile graphicsProfile, SurfaceFormat format, DepthFormat depthFormat, int multiSampleCount, out SurfaceFormat selectedFormat, out DepthFormat selectedDepthFormat, out int selectedMultiSampleCount)
	{
		if (format != SurfaceFormat.Color && format != SurfaceFormat.Rgba1010102 && format != SurfaceFormat.Rg32 && format != SurfaceFormat.Rgba64 && format != SurfaceFormat.Single && format != SurfaceFormat.Vector2 && format != SurfaceFormat.Vector4 && format != SurfaceFormat.HalfSingle && format != SurfaceFormat.HalfVector2 && format != SurfaceFormat.HalfVector4 && format != SurfaceFormat.HdrBlendable)
		{
			selectedFormat = SurfaceFormat.Color;
		}
		else
		{
			selectedFormat = format;
		}
		selectedDepthFormat = depthFormat;
		selectedMultiSampleCount = 0;
		return format == selectedFormat && depthFormat == selectedDepthFormat && multiSampleCount == selectedMultiSampleCount;
	}

	public bool QueryBackBufferFormat(GraphicsProfile graphicsProfile, SurfaceFormat format, DepthFormat depthFormat, int multiSampleCount, out SurfaceFormat selectedFormat, out DepthFormat selectedDepthFormat, out int selectedMultiSampleCount)
	{
		selectedFormat = SurfaceFormat.Color;
		selectedDepthFormat = depthFormat;
		selectedMultiSampleCount = 0;
		return format == selectedFormat && depthFormat == selectedDepthFormat && multiSampleCount == selectedMultiSampleCount;
	}

	internal static void AdaptersChanged()
	{
		adapters = null;
	}
}
