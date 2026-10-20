using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework;

public class LaunchParameters : Dictionary<string, string>
{
	private static readonly char[] flags = new char[2] { '/', '-' };

	public LaunchParameters()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string[] array = commandLineArgs;
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = text.TrimStart(flags);
			if (text2.Length < 3)
			{
				continue;
			}
			int num = text2.IndexOf(":", 1, text2.Length - 2);
			if (num >= 0)
			{
				string key = text2.Substring(0, num);
				if (!ContainsKey(key))
				{
					Add(key, text2.Substring(num + 1));
				}
			}
		}
	}
}
