using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class VertexDeclarationReader : ContentTypeReader<VertexDeclaration>
{
	protected internal override VertexDeclaration Read(ContentReader reader, VertexDeclaration existingInstance)
	{
		int vertexStride = reader.ReadInt32();
		int num = reader.ReadInt32();
		VertexElement[] array = new VertexElement[num];
		for (int i = 0; i < num; i++)
		{
			int offset = reader.ReadInt32();
			VertexElementFormat elementFormat = (VertexElementFormat)reader.ReadInt32();
			VertexElementUsage elementUsage = (VertexElementUsage)reader.ReadInt32();
			int usageIndex = reader.ReadInt32();
			array[i] = new VertexElement(offset, elementFormat, elementUsage, usageIndex);
		}
		return new VertexDeclaration(vertexStride, array);
	}
}
