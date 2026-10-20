using System.IO;
using Microsoft.Xna.Framework.Media;
using MonoGame.Utilities;

namespace Microsoft.Xna.Framework.Content;

internal class VideoReader : ContentTypeReader<Video>
{
	private static string[] supportedExtensions = new string[2] { ".ogv", ".ogg" };

	internal static string Normalize(string fileName)
	{
		return ContentTypeReader.Normalize(fileName, supportedExtensions);
	}

	protected internal override Video Read(ContentReader input, Video existingInstance)
	{
		string text = FileHelpers.ResolveRelativePath(Path.Combine(input.ContentManager.RootDirectoryFullPath, input.AssetName), input.ReadObject<string>());
		text = Normalize(text.Substring(0, text.Length - 4));
		if (string.IsNullOrEmpty(text))
		{
			throw new ContentLoadException();
		}
		int durationMS = input.ReadObject<int>();
		int width = input.ReadObject<int>();
		int height = input.ReadObject<int>();
		float framesPerSecond = input.ReadObject<float>();
		VideoSoundtrackType soundtrackType = (VideoSoundtrackType)input.ReadObject<int>();
		return new Video(text, input.ContentManager.GetGraphicsDevice(), durationMS, width, height, framesPerSecond, soundtrackType);
	}
}
