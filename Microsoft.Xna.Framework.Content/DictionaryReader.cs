using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Content;

internal class DictionaryReader<TKey, TValue> : ContentTypeReader<Dictionary<TKey, TValue>>
{
	private ContentTypeReader keyReader;

	private ContentTypeReader valueReader;

	private Type keyType;

	private Type valueType;

	public override bool CanDeserializeIntoExistingObject => true;

	protected internal override void Initialize(ContentTypeReaderManager manager)
	{
		keyType = typeof(TKey);
		valueType = typeof(TValue);
		keyReader = manager.GetTypeReader(keyType);
		valueReader = manager.GetTypeReader(valueType);
	}

	protected internal override Dictionary<TKey, TValue> Read(ContentReader input, Dictionary<TKey, TValue> existingInstance)
	{
		int num = input.ReadInt32();
		Dictionary<TKey, TValue> dictionary = existingInstance;
		if (dictionary == null)
		{
			dictionary = new Dictionary<TKey, TValue>(num);
		}
		else
		{
			dictionary.Clear();
		}
		for (int i = 0; i < num; i++)
		{
			TKey key;
			if (keyType.IsValueType)
			{
				key = input.ReadObject<TKey>(keyReader);
			}
			else
			{
				int num2 = input.Read7BitEncodedInt();
				key = ((num2 > 0) ? input.ReadObject<TKey>(input.TypeReaders[num2 - 1]) : default(TKey));
			}
			TValue value;
			if (valueType.IsValueType)
			{
				value = input.ReadObject<TValue>(valueReader);
			}
			else
			{
				int num3 = input.Read7BitEncodedInt();
				value = ((num3 > 0) ? input.ReadObject<TValue>(input.TypeReaders[num3 - 1]) : default(TValue));
			}
			dictionary.Add(key, value);
		}
		return dictionary;
	}
}
