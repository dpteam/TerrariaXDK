using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Content;

internal class ListReader<T> : ContentTypeReader<List<T>>
{
	private ContentTypeReader elementReader;

	public override bool CanDeserializeIntoExistingObject => true;

	protected internal override void Initialize(ContentTypeReaderManager manager)
	{
		Type typeFromHandle = typeof(T);
		elementReader = manager.GetTypeReader(typeFromHandle);
	}

	protected internal override List<T> Read(ContentReader input, List<T> existingInstance)
	{
		int num = input.ReadInt32();
		List<T> list = existingInstance;
		if (list == null)
		{
			list = new List<T>(num);
		}
		for (int i = 0; i < num; i++)
		{
			Type typeFromHandle = typeof(T);
			if (typeFromHandle.IsValueType)
			{
				list.Add(input.ReadObject<T>(elementReader));
				continue;
			}
			int num2 = input.Read7BitEncodedInt();
			list.Add((num2 > 0) ? input.ReadObject<T>(input.TypeReaders[num2 - 1]) : default(T));
		}
		return list;
	}
}
