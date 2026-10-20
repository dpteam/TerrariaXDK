using System;

namespace Microsoft.Xna.Framework.Graphics;

internal struct StateHash(ulong a, ulong b) : IEquatable<StateHash>
{
	private readonly ulong a = a;

	private readonly ulong b = b;

	public override string ToString()
	{
		return Convert.ToString((long)a, 2).PadLeft(64, '0') + "|" + Convert.ToString((long)b, 2).PadLeft(64, '0');
	}

	bool IEquatable<StateHash>.Equals(StateHash hash)
	{
		return a == hash.a && b == hash.b;
	}

	public override bool Equals(object obj)
	{
		if (obj == null || obj.GetType() != GetType())
		{
			return false;
		}
		StateHash stateHash = (StateHash)obj;
		return a == stateHash.a && b == stateHash.b;
	}

	public override int GetHashCode()
	{
		int num = (int)(a ^ (a >> 32));
		int num2 = (int)(b ^ (b >> 32));
		return num + num2;
	}
}
