using System.IO;
using Microsoft.Xna.Framework.Media;
using MonoGame.Utilities;

namespace Microsoft.Xna.Framework.Content;

internal class SongReader : ContentTypeReader<Song>
{
	private static string[] supportedExtensions = new string[2] { ".ogg", ".oga" };

	internal static string Normalize(string fileName)
	{
		return ContentTypeReader.Normalize(fileName, supportedExtensions);
	}

	protected internal override Song Read(ContentReader input, Song existingInstance)
	{
		string text = FileHelpers.ResolveRelativePath(Path.Combine(input.ContentManager.RootDirectoryFullPath, input.AssetName), input.ReadString());
		text = Normalize(text.Substring(0, text.Length - 4));
		if (string.IsNullOrEmpty(text))
		{
			throw new ContentLoadException();
		}
		int durationMS = input.ReadInt32();
		return new Song(text, durationMS);
	}
}
