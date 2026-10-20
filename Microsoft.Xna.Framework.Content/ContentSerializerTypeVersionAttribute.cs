using System;

namespace Microsoft.Xna.Framework.Content;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ContentSerializerTypeVersionAttribute : Attribute
{
	public int TypeVersion { get; private set; }

	public ContentSerializerTypeVersionAttribute(int typeVersion)
	{
		TypeVersion = typeVersion;
	}
}
