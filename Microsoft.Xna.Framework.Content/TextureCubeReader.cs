using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class TextureCubeReader : ContentTypeReader<TextureCube>
{
	private static string[] supportedExtensions = new string[1] { ".dds" };

	internal static string Normalize(string fileName)
	{
		return ContentTypeReader.Normalize(fileName, supportedExtensions);
	}

	protected internal override TextureCube Read(ContentReader reader, TextureCube existingInstance)
	{
		SurfaceFormat format = (SurfaceFormat)reader.ReadInt32();
		int size = reader.ReadInt32();
		int num = reader.ReadInt32();
		TextureCube textureCube = ((existingInstance != null) ? existingInstance : new TextureCube(reader.ContentManager.GetGraphicsDevice(), size, num > 1, format));
		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int num2 = reader.ReadInt32();
				byte[] data = reader.ReadBytes(num2);
				textureCube.SetData((CubeMapFace)i, j, null, data, 0, num2);
			}
		}
		return textureCube;
	}
}
