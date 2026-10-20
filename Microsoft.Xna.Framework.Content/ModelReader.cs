using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class ModelReader : ContentTypeReader<Model>
{
	private static int ReadBoneReference(ContentReader reader, uint boneCount)
	{
		uint num = ((boneCount >= 255) ? reader.ReadUInt32() : reader.ReadByte());
		if (num != 0)
		{
			return (int)(num - 1);
		}
		return -1;
	}

	protected internal override Model Read(ContentReader reader, Model existingInstance)
	{
		uint num = reader.ReadUInt32();
		List<ModelBone> list = new List<ModelBone>((int)num);
		for (uint num2 = 0u; num2 < num; num2++)
		{
			string name = reader.ReadObject<string>();
			Matrix transform = reader.ReadMatrix();
			ModelBone item = new ModelBone
			{
				Transform = transform,
				Index = (int)num2,
				Name = name
			};
			list.Add(item);
		}
		for (int i = 0; i < num; i++)
		{
			ModelBone modelBone = list[i];
			int num3 = ReadBoneReference(reader, num);
			if (num3 != -1)
			{
				modelBone.Parent = list[num3];
			}
			uint num4 = reader.ReadUInt32();
			if (num4 == 0)
			{
				continue;
			}
			for (uint num5 = 0u; num5 < num4; num5++)
			{
				int num6 = ReadBoneReference(reader, num);
				if (num6 != -1)
				{
					modelBone.AddChild(list[num6]);
				}
			}
		}
		List<ModelMesh> list2 = new List<ModelMesh>();
		int num7 = reader.ReadInt32();
		GraphicsDevice graphicsDevice = reader.ContentManager.GetGraphicsDevice();
		for (int j = 0; j < num7; j++)
		{
			string name2 = reader.ReadObject<string>();
			int index = ReadBoneReference(reader, num);
			BoundingSphere boundingSphere = reader.ReadBoundingSphere();
			object tag = reader.ReadObject<object>();
			int num8 = reader.ReadInt32();
			List<ModelMeshPart> parts = new List<ModelMeshPart>(num8);
			for (uint num9 = 0u; num9 < num8; num9++)
			{
				ModelMeshPart modelMeshPart = ((existingInstance == null) ? new ModelMeshPart() : existingInstance.Meshes[j].MeshParts[(int)num9]);
				modelMeshPart.VertexOffset = reader.ReadInt32();
				modelMeshPart.NumVertices = reader.ReadInt32();
				modelMeshPart.StartIndex = reader.ReadInt32();
				modelMeshPart.PrimitiveCount = reader.ReadInt32();
				modelMeshPart.Tag = reader.ReadObject<object>();
				parts.Add(modelMeshPart);
				int jj = (int)num9;
				reader.ReadSharedResource((VertexBuffer v) =>
				{
					parts[jj].VertexBuffer = v;
				});
				reader.ReadSharedResource((IndexBuffer v) =>
				{
					parts[jj].IndexBuffer = v;
				});
				reader.ReadSharedResource((Effect v) =>
				{
					parts[jj].Effect = v;
				});
			}
			if (existingInstance == null)
			{
				ModelMesh modelMesh = new ModelMesh(graphicsDevice, parts);
				modelMesh.Tag = tag;
				modelMesh.Name = name2;
				modelMesh.ParentBone = list[index];
				modelMesh.ParentBone.AddMesh(modelMesh);
				modelMesh.BoundingSphere = boundingSphere;
				list2.Add(modelMesh);
			}
		}
		if (existingInstance != null)
		{
			ReadBoneReference(reader, num);
			reader.ReadObject<object>();
			return existingInstance;
		}
		int index2 = ReadBoneReference(reader, num);
		Model model = new Model(graphicsDevice, list, list2);
		model.Root = list[index2];
		model.Tag = reader.ReadObject<object>();
		return model;
	}
}
