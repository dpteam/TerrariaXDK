using System;

namespace Microsoft.Xna.Framework.Input;

public static class TextInputEXT
{
	public static event Action<char> TextInput;

	public static event Action<string, int, int> TextEditing;

	public static void StartTextInput()
	{
		FNAPlatform.StartTextInput();
	}

	public static void StopTextInput()
	{
		FNAPlatform.StopTextInput();
	}

	public static void SetInputRectangle(Rectangle rectangle)
	{
		FNAPlatform.SetTextInputRectangle(rectangle);
	}

	internal static void OnTextInput(char c)
	{
		if (TextInput != null)
		{
			TextInput(c);
		}
	}

	internal static void OnTextEditing(string text, int start, int length)
	{
		if (TextEditing != null)
		{
			TextEditing(text, start, length);
		}
	}
}
