using System;

namespace Microsoft.Xna.Framework.Content;

internal class EnumReader<T> : ContentTypeReader<T>
{
	private ContentTypeReader elementReader;

	protected internal override void Initialize(ContentTypeReaderManager manager)
	{
		Type underlyingType = Enum.GetUnderlyingType(typeof(T));
		elementReader = manager.GetTypeReader(underlyingType);
	}

	protected internal override T Read(ContentReader input, T existingInstance)
	{
		return input.ReadRawObject<T>(elementReader);
	}
}
