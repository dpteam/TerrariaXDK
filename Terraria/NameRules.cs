using System;
using System.Security.Cryptography;
using System.Text;

namespace Terraria;

/// <summary>
/// Rules for character and world names.
///
/// Names used to be generated and committed without asking: pressing Start in character
/// creation produced something like "A3F19C07251015" and that was your character's name
/// forever. The port has an on-screen keyboard now, so the player gets to type one - these
/// rules are what the prompt enforces, and they are pure functions so the self-test can
/// cover every rejection without a keyboard.
/// </summary>
internal static class NameRules
{
	/// <summary>Longest character name. The console lists show it in a fixed column.</summary>
	public const int MaxCharacterLength = 16;

	/// <summary>Longest world name.</summary>
	public const int MaxWorldLength = 20;

	/// <summary>
	/// Trims, turns control characters into spaces and collapses runs of whitespace. A name
	/// with a newline in it used to corrupt the save file's string field and the server's
	/// roster line, so control characters never reach either.
	/// </summary>
	public static string Sanitize(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		bool flag = false;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (char.IsControl(c) || c == '\t' || c == '\r' || c == '\n')
			{
				c = ' ';
			}
			if (c == ' ')
			{
				// One space between words: "a    b" becomes "a b".
				if (stringBuilder.Length == 0 || flag)
				{
					continue;
				}
				flag = true;
				stringBuilder.Append(' ');
				continue;
			}
			flag = false;
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString().TrimEnd();
	}

	/// <summary>
	/// Validates and normalises a name. Returns false with a message the prompt can show
	/// instead of silently accepting something that will look broken in a list.
	/// </summary>
	public static bool TryNormalize(string text, int maxLength, out string name, out string error)
	{
		name = Sanitize(text);
		error = null;
		if (name.Length == 0)
		{
			error = "The name cannot be empty.";
			return false;
		}
		if (name.Length > maxLength)
		{
			error = "Use at most " + maxLength + " characters.";
			return false;
		}
		return true;
	}

	/// <summary>
	/// Fallback name, used when the player confirms an empty field: the old generated
	/// pattern, so a slot never ends up unnamed.
	/// </summary>
	public static string DefaultCharacterName()
	{
		byte[] array = new byte[4];
		using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(array);
		}
		string text = CRC32Helper.CalculateCRC32(array).ToString("X8");
		string text2 = text.ToUpper() + DateTime.Now.ToString("yyMMdd");
		return (text2.Length > MaxCharacterLength) ? text2.Substring(0, MaxCharacterLength) : text2;
	}

	/// <summary>Fallback world name, same shape as the one the game used to generate.</summary>
	public static string DefaultWorldName()
	{
		byte[] data = new byte[8];
		using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(data);
		}
		string text = "WORLD_" + CRC32Helper.CalculateCRC32(data).ToString("X8") + "_" + DateTime.Now.ToString("HHmmss");
		return (text.Length > MaxWorldLength) ? text.Substring(0, MaxWorldLength) : text;
	}
}
