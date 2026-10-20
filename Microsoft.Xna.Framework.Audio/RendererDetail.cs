using System;

namespace Microsoft.Xna.Framework.Audio;

[Serializable]
public struct RendererDetail
{
	public string FriendlyName { get; private set; }

	public string RendererId { get; private set; }

	internal RendererDetail(string name, string id)
	{
		this = default;
		FriendlyName = name;
		RendererId = id;
	}

	public override bool Equals(object obj)
	{
		return obj is RendererDetail && RendererId.Equals(((RendererDetail)obj).RendererId);
	}

	public override int GetHashCode()
	{
		return RendererId.GetHashCode();
	}

	public override string ToString()
	{
		return FriendlyName;
	}

	public static bool operator ==(RendererDetail left, RendererDetail right)
	{
		return left.RendererId.Equals(right.RendererId);
	}

	public static bool operator !=(RendererDetail left, RendererDetail right)
	{
		return !left.RendererId.Equals(right.RendererId);
	}
}
