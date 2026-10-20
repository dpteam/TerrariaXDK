namespace Microsoft.Xna.Framework.Graphics;

internal static class VertexDeclarationCache<T> where T : struct, IVertexType
{
	private static VertexDeclaration cached;

	public static VertexDeclaration VertexDeclaration
	{
		get
		{
			if (cached == null)
			{
				cached = VertexDeclaration.FromType(typeof(T));
			}
			return cached;
		}
	}
}
