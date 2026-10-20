using System;

namespace Microsoft.Xna.Framework.Content;

internal class NullableReader<T> : ContentTypeReader<T?> where T : struct
{
	private ContentTypeReader elementReader;

	internal NullableReader()
	{
	}

	protected internal override void Initialize(ContentTypeReaderManager manager)
	{
		Type typeFromHandle = typeof(T);
		elementReader = manager.GetTypeReader(typeFromHandle);
	}

	protected internal override T? Read(ContentReader input, T? existingInstance)
	{
		if (input.ReadBoolean())
		{
			return input.ReadObject<T>(elementReader);
		}
		return null;
	}
}
