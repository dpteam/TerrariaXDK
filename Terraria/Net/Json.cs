using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Just enough JSON for the master server, with no external dependency.
///
/// net40 has no System.Text.Json, and pulling System.Web.Extensions in for a handful of
/// objects is not worth it. Parsing is deliberately strict: a malformed document throws
/// <see cref="FormatException"/>, and the caller drops the whole response instead of
/// guessing. Values are <c>Dictionary&lt;string, object&gt;</c>, <c>List&lt;object&gt;</c>,
/// <c>string</c>, <c>double</c>, <c>bool</c> or <c>null</c>.
/// </summary>
internal static class Json
{
	/// <summary>Guards against a hostile master answering with hundreds of megabytes.</summary>
	public const int MaxDocumentSize = 512 * 1024;

	private const int MaxDepth = 16;

	public static object Parse(string text)
	{
		if (text == null)
		{
			throw new FormatException("empty JSON document");
		}
		if (text.Length > MaxDocumentSize)
		{
			throw new FormatException("JSON document is too large (" + text.Length + " chars)");
		}
		int index = 0;
		object result = ParseValue(text, ref index, 0);
		SkipWhitespace(text, ref index);
		if (index != text.Length)
		{
			throw new FormatException("trailing characters at position " + index);
		}
		return result;
	}

	/// <summary>Reads a property as a string, returning null when it is missing or not a scalar.</summary>
	public static string GetString(Dictionary<string, object> obj, string key)
	{
		object value;
		if (obj == null || !obj.TryGetValue(key, out value))
		{
			return null;
		}
		string text = value as string;
		if (text != null)
		{
			return text;
		}
		if (value is double)
		{
			return ((double)value).ToString(CultureInfo.InvariantCulture);
		}
		if (value is bool)
		{
			return ((bool)value) ? "true" : "false";
		}
		return null;
	}

	/// <summary>Reads a property as an integer, returning the fallback when it is missing or odd.</summary>
	public static int GetInt(Dictionary<string, object> obj, string key, int fallback)
	{
		object value;
		if (obj == null || !obj.TryGetValue(key, out value))
		{
			return fallback;
		}
		if (value is double)
		{
			double num = (double)value;
			if (num >= -2147483648.0 && num <= 2147483647.0)
			{
				return (int)num;
			}
		}
		return fallback;
	}

	public static bool GetBool(Dictionary<string, object> obj, string key)
	{
		object value;
		if (obj == null || !obj.TryGetValue(key, out value))
		{
			return false;
		}
		if (value is bool)
		{
			return (bool)value;
		}
		if (value is string)
		{
			return ((string)value).Equals("true", StringComparison.OrdinalIgnoreCase);
		}
		if (value is double)
		{
			return (double)value != 0.0;
		}
		return false;
	}

	public static List<object> GetArray(Dictionary<string, object> obj, string key)
	{
		object value;
		if (obj == null || !obj.TryGetValue(key, out value))
		{
			return null;
		}
		return value as List<object>;
	}

	/// <summary>Escapes a string for use as a JSON value (including the quotes).</summary>
	public static string Quote(string text)
	{
		if (text == null)
		{
			return "null";
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length + 2);
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
			case '\b':
				stringBuilder.Append("\\b");
				break;
			case '\f':
				stringBuilder.Append("\\f");
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
				if (c < ' ' || c == '\u007f')
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

	// --- Parser -------------------------------------------------------------

	private static object ParseValue(string text, ref int index, int depth)
	{
		if (depth > MaxDepth)
		{
			throw new FormatException("JSON nesting is too deep");
		}
		SkipWhitespace(text, ref index);
		if (index >= text.Length)
		{
			throw new FormatException("unexpected end of JSON");
		}
		char c = text[index];
		switch (c)
		{
		case '{':
			return ParseObject(text, ref index, depth);
		case '[':
			return ParseArray(text, ref index, depth);
		case '"':
			return ParseString(text, ref index);
		case 't':
			Expect(text, ref index, "true");
			return true;
		case 'f':
			Expect(text, ref index, "false");
			return false;
		case 'n':
			Expect(text, ref index, "null");
			return null;
		default:
			return ParseNumber(text, ref index);
		}
	}

	private static Dictionary<string, object> ParseObject(string text, ref int index, int depth)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		index++;
		SkipWhitespace(text, ref index);
		if (index < text.Length && text[index] == '}')
		{
			index++;
			return dictionary;
		}
		while (true)
		{
			SkipWhitespace(text, ref index);
			if (index >= text.Length || text[index] != '"')
			{
				throw new FormatException("expected a property name at position " + index);
			}
			string key = ParseString(text, ref index);
			SkipWhitespace(text, ref index);
			if (index >= text.Length || text[index] != ':')
			{
				throw new FormatException("expected ':' at position " + index);
			}
			index++;
			dictionary[key] = ParseValue(text, ref index, depth + 1);
			SkipWhitespace(text, ref index);
			if (index >= text.Length)
			{
				throw new FormatException("unterminated JSON object");
			}
			if (text[index] == ',')
			{
				index++;
				continue;
			}
			if (text[index] == '}')
			{
				index++;
				return dictionary;
			}
			throw new FormatException("expected ',' or '}' at position " + index);
		}
	}

	private static List<object> ParseArray(string text, ref int index, int depth)
	{
		List<object> list = new List<object>();
		index++;
		SkipWhitespace(text, ref index);
		if (index < text.Length && text[index] == ']')
		{
			index++;
			return list;
		}
		while (true)
		{
			list.Add(ParseValue(text, ref index, depth + 1));
			SkipWhitespace(text, ref index);
			if (index >= text.Length)
			{
				throw new FormatException("unterminated JSON array");
			}
			if (text[index] == ',')
			{
				index++;
				continue;
			}
			if (text[index] == ']')
			{
				index++;
				return list;
			}
			throw new FormatException("expected ',' or ']' at position " + index);
		}
	}

	private static string ParseString(string text, ref int index)
	{
		index++;
		StringBuilder stringBuilder = new StringBuilder();
		while (index < text.Length)
		{
			char c = text[index++];
			if (c == '"')
			{
				return stringBuilder.ToString();
			}
			if (c != '\\')
			{
				stringBuilder.Append(c);
				continue;
			}
			if (index >= text.Length)
			{
				break;
			}
			char c2 = text[index++];
			switch (c2)
			{
			case '"':
				stringBuilder.Append('"');
				break;
			case '\\':
				stringBuilder.Append('\\');
				break;
			case '/':
				stringBuilder.Append('/');
				break;
			case 'b':
				stringBuilder.Append('\b');
				break;
			case 'f':
				stringBuilder.Append('\f');
				break;
			case 'n':
				stringBuilder.Append('\n');
				break;
			case 'r':
				stringBuilder.Append('\r');
				break;
			case 't':
				stringBuilder.Append('\t');
				break;
			case 'u':
				if (index + 4 > text.Length)
				{
					throw new FormatException("truncated \\u escape at position " + index);
				}
				int num;
				if (!int.TryParse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out num))
				{
					throw new FormatException("bad \\u escape at position " + index);
				}
				stringBuilder.Append((char)num);
				index += 4;
				break;
			default:
				throw new FormatException("bad escape '\\" + c2 + "' at position " + (index - 1));
			}
		}
		throw new FormatException("unterminated JSON string");
	}

	private static double ParseNumber(string text, ref int index)
	{
		int i = index;
		if (i < text.Length && (text[i] == '-' || text[i] == '+'))
		{
			i++;
		}
		while (i < text.Length)
		{
			char c = text[i];
			if ((c < '0' || c > '9') && c != '.' && c != 'e' && c != 'E' && c != '+' && c != '-')
			{
				break;
			}
			i++;
		}
		if (i == index)
		{
			throw new FormatException("unexpected character '" + text[index] + "' at position " + index);
		}
		double result;
		if (!double.TryParse(text.Substring(index, i - index), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
		{
			throw new FormatException("bad number at position " + index);
		}
		index = i;
		return result;
	}

	private static void Expect(string text, ref int index, string literal)
	{
		if (index + literal.Length > text.Length || string.CompareOrdinal(text, index, literal, 0, literal.Length) != 0)
		{
			throw new FormatException("expected '" + literal + "' at position " + index);
		}
		index += literal.Length;
	}

	private static void SkipWhitespace(string text, ref int index)
	{
		while (index < text.Length)
		{
			char c = text[index];
			if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
			{
				break;
			}
			index++;
		}
	}
}
