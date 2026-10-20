using System;

namespace Microsoft.Xna.Framework.Content;

internal class ArrayReader<T> : ContentTypeReader<T[]>
{
	private ContentTypeReader elementReader;

	protected internal override void Initialize(ContentTypeReaderManager manager)
	{
		Type typeFromHandle = typeof(T);
		elementReader = manager.GetTypeReader(typeFromHandle);
	}

	protected internal override T[] Read(ContentReader input, T[] existingInstance)
	{
		uint num = input.ReadUInt32();
		T[] array = existingInstance;
		if (array == null)
		{
			array = new T[num];
		}
		if (typeof(T).IsValueType)
		{
			for (uint num2 = 0u; num2 < num; num2++)
			{
				array[num2] = input.ReadObject<T>(elementReader);
			}
		}
		else
		{
			for (uint num3 = 0u; num3 < num; num3++)
			{
				int num4 = input.Read7BitEncodedInt();
				if (num4 > 0)
				{
					array[num3] = input.ReadObject<T>(input.TypeReaders[num4 - 1]);
				}
				else
				{
					array[num3] = default;
				}
			}
		}
		return array;
	}
}
