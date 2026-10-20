using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class IndexBufferReader : ContentTypeReader<IndexBuffer>
{
	protected internal override IndexBuffer Read(ContentReader input, IndexBuffer existingInstance)
	{
		IndexBuffer indexBuffer = existingInstance;
		bool flag = input.ReadBoolean();
		int num = input.ReadInt32();
		byte[] data = input.ReadBytes(num);
		if (indexBuffer == null)
		{
			indexBuffer = ((!flag) ? new IndexBuffer(input.ContentManager.GetGraphicsDevice(), IndexElementSize.ThirtyTwoBits, num / 4, BufferUsage.None) : new IndexBuffer(input.ContentManager.GetGraphicsDevice(), IndexElementSize.SixteenBits, num / 2, BufferUsage.None));
		}
		indexBuffer.SetData(data);
		return indexBuffer;
	}
}
