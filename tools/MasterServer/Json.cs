using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MasterServer;

/// <summary>
/// Just enough JSON for the master protocol: flat objects, which is all the announce body and
/// the server list are.
///
/// Hand-rolled for the same reason the game has its own parser (see <c>Terraria/Net/Json.cs</c>):
/// the tool targets net40, where <c>System.Text.Json</c> does not exist, and pulling a NuGet
/// dependency into a console utility that has to run on any machine is not worth it.
/// </summary>
internal static class Json
{
	/// <summary>Reads a JSON object. Nested values become <c>Dictionary</c> or <c>List</c>.</summary>
	public static Dictionary<string, object> ParseObject(string text, out string error)
	{
		error = null;
		if (text == null)
		{
			error = "empty body";
			return null;
		}
		int i = SkipWhitespace(text, 0);
		if (i >= text.Length || text[i] != '{')
		{
			error = "not a JSON object";
			return null;
		}
		return ReadObject(text, ref i, out error);
	}

	private static object ReadValue(string text, ref int i, out string error)
	{
		error = null;
		if (i >= text.Length)
		{
			error = "unexpected end of value";
			return null;
		}
		char c = text[i];
		if (c == '"')
		{
			return ReadString(text, ref i, out error);
		}
		if (c == '{')
		{
			// Nested objects are parsed properly: the statistics report is an object of counters,
			// and skipping it would silently drop every number.
			return ReadObject(text, ref i, out error);
		}
		if (c == '[')
		{
			return ReadArray(text, ref i, out error);
		}
		int num = i;
		while (i < text.Length && text[i] != ',' && text[i] != '}' && text[i] != ']')
		{
			i++;
		}
		string text2 = text.Substring(num, i - num).Trim();
		if (text2.Length == 0)
		{
			error = "empty value";
			return null;
		}
		if (text2.Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text2.Equals("false", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (text2.Equals("null", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		double num2;
		if (double.TryParse(text2, NumberStyles.Float, CultureInfo.InvariantCulture, out num2))
		{
			return num2;
		}
		// Unquoted text is not valid JSON, but refusing the whole announce over it would lose a
		// host for a typo in one optional field.
		return text2;
	}

	private static Dictionary<string, object> ReadObject(string text, ref int i, out string error)
	{
		error = null;
		Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		i++;
		while (true)
		{
			i = SkipWhitespace(text, i);
			if (i >= text.Length)
			{
				error = "unexpected end of object";
				return null;
			}
			if (text[i] == '}')
			{
				i++;
				return dictionary;
			}
			if (text[i] == ',')
			{
				i++;
				continue;
			}
			if (text[i] != '"')
			{
				error = "expected a key at " + i;
				return null;
			}
			string key = ReadString(text, ref i, out error);
			if (key == null)
			{
				return null;
			}
			i = SkipWhitespace(text, i);
			if (i >= text.Length || text[i] != ':')
			{
				error = "expected ':' after '" + key + "'";
				return null;
			}
			i = SkipWhitespace(text, i + 1);
			object value = ReadValue(text, ref i, out error);
			if (error != null)
			{
				return null;
			}
			dictionary[key] = value;
		}
	}

	private static List<object> ReadArray(string text, ref int i, out string error)
	{
		error = null;
		List<object> list = new List<object>();
		i++;
		while (true)
		{
			i = SkipWhitespace(text, i);
			if (i >= text.Length)
			{
				error = "unexpected end of array";
				return null;
			}
			if (text[i] == ']')
			{
				i++;
				return list;
			}
			if (text[i] == ',')
			{
				i++;
				continue;
			}
			object item = ReadValue(text, ref i, out error);
			if (error != null)
			{
				return null;
			}
			list.Add(item);
		}
	}

	private static string ReadString(string text, ref int i, out string error)
	{
		error = null;
		i++;
		StringBuilder stringBuilder = new StringBuilder(32);
		while (i < text.Length)
		{
			char c = text[i++];
			if (c == '"')
			{
				return stringBuilder.ToString();
			}
			if (c != '\\')
			{
				stringBuilder.Append(c);
				continue;
			}
			if (i >= text.Length)
			{
				break;
			}
			char c2 = text[i++];
			switch (c2)
			{
			case 'n':
				stringBuilder.Append('\n');
				break;
			case 'r':
				stringBuilder.Append('\r');
				break;
			case 't':
				stringBuilder.Append('\t');
				break;
			case 'b':
				stringBuilder.Append('\b');
				break;
			case 'f':
				stringBuilder.Append('\f');
				break;
			case 'u':
				if (i + 4 <= text.Length)
				{
					int result;
					if (int.TryParse(text.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
					{
						stringBuilder.Append((char)result);
					}
					i += 4;
				}
				break;
			default:
				stringBuilder.Append(c2);
				break;
			}
		}
		error = "unterminated string";
		return null;
	}

	private static int SkipWhitespace(string text, int i)
	{
		while (i < text.Length && (text[i] == ' ' || text[i] == '\t' || text[i] == '\r' || text[i] == '\n'))
		{
			i++;
		}
		return i;
	}

	/// <summary>Quotes and escapes a string for output.</summary>
	public static string Quote(string text)
	{
		if (text == null)
		{
			return "\"\"";
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length + 8);
		stringBuilder.Append('"');
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '"':
				stringBuilder.Append("\\\"");
				break;
			case '\\':
				stringBuilder.Append("\\\\");
				break;
			case '\n':
				stringBuilder.Append("\\n");
				break;
			case '\r':
				stringBuilder.Append("\\r");
				break;
			case '\t':
				stringBuilder.Append("\\t");
				break;
			default:
				if (c < ' ')
				{
					stringBuilder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
				}
				else
				{
					stringBuilder.Append(c);
				}
				break;
			}
		}
		stringBuilder.Append('"');
		return stringBuilder.ToString();
	}

	public static string GetString(Dictionary<string, object> source, string key)
	{
		object value;
		return (source != null && source.TryGetValue(key, out value) && value != null) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
	}

	public static int GetInt(Dictionary<string, object> source, string key, int fallback)
	{
		string text = GetString(source, key);
		int result;
		return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : fallback;
	}

	public static bool GetBool(Dictionary<string, object> source, string key)
	{
		string text = GetString(source, key);
		if (text == null)
		{
			return false;
		}
		text = text.Trim();
		return text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1";
	}

	/// <summary>A nested object, or null when the key is missing or is not an object.</summary>
	public static Dictionary<string, object> GetObject(Dictionary<string, object> source, string key)
	{
		object value;
		if (source == null || !source.TryGetValue(key, out value))
		{
			return null;
		}
		return value as Dictionary<string, object>;
	}

	/// <summary>A nested array, or null when the key is missing or is not an array.</summary>
	public static List<object> GetArray(Dictionary<string, object> source, string key)
	{
		object value;
		if (source == null || !source.TryGetValue(key, out value))
		{
			return null;
		}
		return value as List<object>;
	}
}
