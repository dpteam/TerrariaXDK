using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelMesh
{
	private GraphicsDevice graphicsDevice;

	public BoundingSphere BoundingSphere { get; internal set; }

	public ModelEffectCollection Effects { get; private set; }

	public ModelMeshPartCollection MeshParts { get; private set; }

	public string Name { get; internal set; }

	public ModelBone ParentBone { get; internal set; }

	public object Tag { get; set; }

	internal ModelMesh(GraphicsDevice graphicsDevice, List<ModelMeshPart> parts)
	{
		this.graphicsDevice = graphicsDevice;
		MeshParts = new ModelMeshPartCollection(parts);
		foreach (ModelMeshPart part in parts)
		{
			part.parent = this;
		}
		Effects = new ModelEffectCollection();
	}

	public void Draw()
	{
		foreach (ModelMeshPart meshPart in MeshParts)
		{
			Effect effect = meshPart.Effect;
			if (meshPart.PrimitiveCount <= 0)
			{
				continue;
			}
			graphicsDevice.SetVertexBuffer(meshPart.VertexBuffer);
			graphicsDevice.Indices = meshPart.IndexBuffer;
			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, meshPart.VertexOffset, 0, meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);
			}
		}
	}
}
