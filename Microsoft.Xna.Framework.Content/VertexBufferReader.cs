using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class VertexBufferReader : ContentTypeReader<VertexBuffer>
{
	protected internal override VertexBuffer Read(ContentReader input, VertexBuffer existingInstance)
	{
		VertexDeclaration vertexDeclaration = input.ReadRawObject<VertexDeclaration>();
		int num = (int)input.ReadUInt32();
		byte[] data = input.ReadBytes(num * vertexDeclaration.VertexStride);
		VertexBuffer vertexBuffer = new VertexBuffer(input.ContentManager.GetGraphicsDevice(), vertexDeclaration, num, BufferUsage.None);
		vertexBuffer.SetData(data);
		return vertexBuffer;
	}
}
