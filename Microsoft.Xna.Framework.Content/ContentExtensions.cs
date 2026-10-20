using System;
using System.Reflection;

namespace Microsoft.Xna.Framework.Content;

internal static class ContentExtensions
{
	public static ConstructorInfo GetDefaultConstructor(this Type type)
	{
		return type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);
	}
}
