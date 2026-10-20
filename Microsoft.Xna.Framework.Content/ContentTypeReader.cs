using System;
using System.IO;

namespace Microsoft.Xna.Framework.Content;

public abstract class ContentTypeReader
{
	private Type targetType;

	public virtual bool CanDeserializeIntoExistingObject => false;

	public Type TargetType => targetType;

	public virtual int TypeVersion => 0;

	protected ContentTypeReader(Type targetType)
	{
		this.targetType = targetType;
	}

	protected internal static string Normalize(string fileName, string[] extensions)
	{
		if (File.Exists(fileName))
		{
			return fileName;
		}
		foreach (string text in extensions)
		{
			string text2 = fileName + text;
			if (File.Exists(text2))
			{
				return text2;
			}
		}
		return null;
	}

	protected internal virtual void Initialize(ContentTypeReaderManager manager)
	{
	}

	protected internal abstract object Read(ContentReader input, object existingInstance);
}
public abstract class ContentTypeReader<T> : ContentTypeReader
{
	protected ContentTypeReader()
		: base(typeof(T))
	{
	}

	protected internal override object Read(ContentReader input, object existingInstance)
	{
		if (existingInstance == null)
		{
			return Read(input, default);
		}
		return Read(input, (T)existingInstance);
	}

	protected internal abstract T Read(ContentReader input, T existingInstance);
}
