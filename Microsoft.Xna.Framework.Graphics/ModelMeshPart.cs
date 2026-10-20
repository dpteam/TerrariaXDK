namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelMeshPart
{
	internal ModelMesh parent;

	private Effect INTERNAL_effect;

	public Effect Effect
	{
		get
		{
			return INTERNAL_effect;
		}
		set
		{
			if (value == INTERNAL_effect)
			{
				return;
			}
			if (INTERNAL_effect != null)
			{
				bool flag = true;
				foreach (ModelMeshPart meshPart in parent.MeshParts)
				{
					if (meshPart != this && meshPart.INTERNAL_effect == INTERNAL_effect)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					parent.Effects.Remove(INTERNAL_effect);
				}
			}
			INTERNAL_effect = value;
			if (INTERNAL_effect != null && !parent.Effects.Contains(INTERNAL_effect))
			{
				parent.Effects.Add(value);
			}
		}
	}

	public IndexBuffer IndexBuffer { get; internal set; }

	public int NumVertices { get; internal set; }

	public int PrimitiveCount { get; internal set; }

	public int StartIndex { get; internal set; }

	public object Tag { get; set; }

	public VertexBuffer VertexBuffer { get; internal set; }

	public int VertexOffset { get; internal set; }

	internal ModelMeshPart()
	{
	}
}
