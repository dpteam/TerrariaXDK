using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public class Model
{
	private static Matrix[] sharedDrawBoneMatrices;

	public ModelBoneCollection Bones { get; private set; }

	public ModelMeshCollection Meshes { get; private set; }

	public ModelBone Root { get; internal set; }

	public object Tag { get; set; }

	internal Model(GraphicsDevice graphicsDevice, List<ModelBone> bones, List<ModelMesh> meshes)
	{
		Bones = new ModelBoneCollection(bones);
		Meshes = new ModelMeshCollection(meshes);
	}

	public void Draw(Matrix world, Matrix view, Matrix projection)
	{
		int count = Bones.Count;
		if (sharedDrawBoneMatrices == null || sharedDrawBoneMatrices.Length < count)
		{
			sharedDrawBoneMatrices = new Matrix[count];
		}
		CopyAbsoluteBoneTransformsTo(sharedDrawBoneMatrices);
		foreach (ModelMesh mesh in Meshes)
		{
			foreach (Effect effect in mesh.Effects)
			{
				if (!(effect is IEffectMatrices effectMatrices))
				{
					throw new InvalidOperationException();
				}
				effectMatrices.World = sharedDrawBoneMatrices[mesh.ParentBone.Index] * world;
				effectMatrices.View = view;
				effectMatrices.Projection = projection;
			}
			mesh.Draw();
		}
	}

	public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
	{
		if (destinationBoneTransforms == null)
		{
			throw new ArgumentNullException("destinationBoneTransforms");
		}
		if (destinationBoneTransforms.Length < Bones.Count)
		{
			throw new ArgumentOutOfRangeException("destinationBoneTransforms");
		}
		int count = Bones.Count;
		for (int i = 0; i < count; i++)
		{
			ModelBone modelBone = Bones[i];
			if (modelBone.Parent == null)
			{
				destinationBoneTransforms[i] = modelBone.Transform;
				continue;
			}
			int index = modelBone.Parent.Index;
			Matrix matrix = modelBone.Transform;
			Matrix.Multiply(ref matrix, ref destinationBoneTransforms[index], out destinationBoneTransforms[i]);
		}
	}

	public void CopyBoneTransformsFrom(Matrix[] sourceBoneTransforms)
	{
		if (sourceBoneTransforms == null)
		{
			throw new ArgumentNullException("sourceBoneTransforms");
		}
		if (sourceBoneTransforms.Length < Bones.Count)
		{
			throw new ArgumentOutOfRangeException("sourceBoneTransforms");
		}
		for (int i = 0; i < sourceBoneTransforms.Length; i++)
		{
			Bones[i].Transform = sourceBoneTransforms[i];
		}
	}

	public void CopyBoneTransformsTo(Matrix[] destinationBoneTransforms)
	{
		if (destinationBoneTransforms == null)
		{
			throw new ArgumentNullException("destinationBoneTransforms");
		}
		if (destinationBoneTransforms.Length < Bones.Count)
		{
			throw new ArgumentOutOfRangeException("destinationBoneTransforms");
		}
		for (int i = 0; i < destinationBoneTransforms.Length; i++)
		{
			destinationBoneTransforms[i] = Bones[i].Transform;
		}
	}
}
