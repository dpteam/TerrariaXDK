namespace Microsoft.Xna.Framework.Graphics;

public struct VertexBufferBinding
{
	private VertexBuffer vertexBuffer;

	private int vertexOffset;

	private int instanceFrequency;

	internal static readonly VertexBufferBinding None;

	public int InstanceFrequency => instanceFrequency;

	public VertexBuffer VertexBuffer => vertexBuffer;

	public int VertexOffset => vertexOffset;

	public VertexBufferBinding(VertexBuffer vertexBuffer)
	{
		this.vertexBuffer = vertexBuffer;
		vertexOffset = 0;
		instanceFrequency = 0;
	}

	public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset)
	{
		this.vertexBuffer = vertexBuffer;
		this.vertexOffset = vertexOffset;
		instanceFrequency = 0;
	}

	public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset, int instanceFrequency)
	{
		this.vertexBuffer = vertexBuffer;
		this.vertexOffset = vertexOffset;
		this.instanceFrequency = instanceFrequency;
	}

	public static implicit operator VertexBufferBinding(VertexBuffer buffer)
	{
		return new VertexBufferBinding(buffer);
	}

	static VertexBufferBinding()
	{
		None = new VertexBufferBinding(null);
	}
}
