using System;
using System.Reflection;

namespace MonoGame.Utilities;

internal static class AssemblyHelper
{
	public static string GetDefaultWindowTitle()
	{
		string text = string.Empty;
		Assembly entryAssembly = Assembly.GetEntryAssembly();
		if (entryAssembly != null)
		{
			try
			{
				AssemblyTitleAttribute assemblyTitleAttribute = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(entryAssembly, typeof(AssemblyTitleAttribute));
				if (assemblyTitleAttribute != null)
				{
					text = assemblyTitleAttribute.Title;
				}
			}
			catch
			{
			}
			if (string.IsNullOrEmpty(text))
			{
				text = entryAssembly.GetName().Name;
			}
		}
		return text;
	}
}
