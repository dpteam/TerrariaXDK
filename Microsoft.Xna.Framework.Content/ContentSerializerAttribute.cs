using System;

namespace Microsoft.Xna.Framework.Content;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ContentSerializerAttribute : Attribute
{
	private string collectionItemName;

	public bool AllowNull { get; set; }

	public string CollectionItemName
	{
		get
		{
			if (string.IsNullOrEmpty(collectionItemName))
			{
				return "Item";
			}
			return collectionItemName;
		}
		set
		{
			collectionItemName = value;
		}
	}

	public string ElementName { get; set; }

	public bool FlattenContent { get; set; }

	public bool HasCollectionItemName => !string.IsNullOrEmpty(collectionItemName);

	public bool Optional { get; set; }

	public bool SharedResource { get; set; }

	public ContentSerializerAttribute()
	{
		AllowNull = true;
	}

	public ContentSerializerAttribute Clone()
	{
		ContentSerializerAttribute contentSerializerAttribute = new ContentSerializerAttribute();
		contentSerializerAttribute.AllowNull = AllowNull;
		contentSerializerAttribute.collectionItemName = collectionItemName;
		contentSerializerAttribute.ElementName = ElementName;
		contentSerializerAttribute.FlattenContent = FlattenContent;
		contentSerializerAttribute.Optional = Optional;
		contentSerializerAttribute.SharedResource = SharedResource;
		return contentSerializerAttribute;
	}
}
