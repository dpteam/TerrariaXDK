using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelBone
{
	private List<ModelBone> children = new List<ModelBone>();

	private List<ModelMesh> meshes = new List<ModelMesh>();

	public ModelBoneCollection Children { get; private set; }

	public int Index { get; internal set; }

	public string Name { get; internal set; }

	public ModelBone Parent { get; internal set; }

	public Matrix Transform { get; set; }

	internal ModelBone()
	{
		Children = new ModelBoneCollection(new List<ModelBone>());
		meshes = new List<ModelMesh>();
	}

	internal void AddMesh(ModelMesh mesh)
	{
		meshes.Add(mesh);
	}

	internal void AddChild(ModelBone modelBone)
	{
		children.Add(modelBone);
		Children = new ModelBoneCollection(children);
	}
}
