using System;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class Texture3DReader : ContentTypeReader<Texture3D>
{
	protected internal override Texture3D Read(ContentReader reader, Texture3D existingInstance)
	{
		SurfaceFormat format = (SurfaceFormat)reader.ReadInt32();
		int num = reader.ReadInt32();
		int num2 = reader.ReadInt32();
		int num3 = reader.ReadInt32();
		int num4 = reader.ReadInt32();
		Texture3D texture3D = ((existingInstance != null) ? existingInstance : new Texture3D(reader.ContentManager.GetGraphicsDevice(), num, num2, num3, num4 > 1, format));
		for (int i = 0; i < num4; i++)
		{
			int num5 = reader.ReadInt32();
			byte[] data = reader.ReadBytes(num5);
			texture3D.SetData(i, 0, 0, num, num2, 0, num3, data, 0, num5);
			num = Math.Max(num >> 1, 1);
			num2 = Math.Max(num2 >> 1, 1);
			num3 = Math.Max(num3 >> 1, 1);
		}
		return texture3D;
	}
}
