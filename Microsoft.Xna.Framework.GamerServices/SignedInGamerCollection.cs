using System.Collections.Generic;

namespace Microsoft.Xna.Framework.GamerServices;

public sealed class SignedInGamerCollection : GamerCollection<SignedInGamer>
{
	public SignedInGamer this[PlayerIndex index]
	{
		get
		{
			if ((int)index >= collection.Count)
			{
				return null;
			}
			return collection[(int)index];
		}
	}

	internal SignedInGamerCollection(List<SignedInGamer> collection)
		: base(collection)
	{
	}
}
