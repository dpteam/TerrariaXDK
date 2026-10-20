using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public class DisplayModeCollection : IEnumerable<DisplayMode>, IEnumerable
{
	private readonly List<DisplayMode> modes;

	public IEnumerable<DisplayMode> this[SurfaceFormat format]
	{
		get
		{
			List<DisplayMode> list = new List<DisplayMode>();
			foreach (DisplayMode mode in modes)
			{
				if (mode.Format == format)
				{
					list.Add(mode);
				}
			}
			return list;
		}
	}

	internal DisplayModeCollection(List<DisplayMode> setmodes)
	{
		modes = setmodes;
	}

	public IEnumerator<DisplayMode> GetEnumerator()
	{
		return modes.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return modes.GetEnumerator();
	}
}
