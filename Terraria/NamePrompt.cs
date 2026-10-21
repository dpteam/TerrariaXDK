using System;

namespace Terraria;

/// <summary>
/// Asks the player for a name, once, from inside a menu that is otherwise stateless.
///
/// The character and world creation menus run their logic every frame, so opening the
/// keyboard there directly would reopen it on every frame after a cancel. This keeps a
/// one-shot latch plus the state needed to re-ask after a rejected name.
///
/// The keyboard closes before its accept callback runs, so a rejected name is handled by
/// opening the prompt again with the text the player typed still in the buffer - losing a
/// typed name to a validation error would be worse than the error itself.
/// </summary>
internal static class NamePrompt
{
	private static MenuMode _activeMode = MenuMode.NONE;

	private static bool _open;

	private static string _title;

	private static string _hint;

	private static int _maxLength;

	private static Action<string> _onAccept;

	private static Action _onCancel;

	/// <summary>
	/// True while a prompt is showing; the menu must not run its own input then.
	///
	/// Deliberately also requires the keyboard to be open: if anything closes it behind our
	/// back, the latch reports "not asking" and the next frame asks again, instead of leaving
	/// the player on a menu that silently ignores every input.
	/// </summary>
	public static bool IsOpen => _open && Input.VirtualKeyboard.IsOpen;

	/// <summary>True when the given menu has already asked and is waiting for an answer.</summary>
	public static bool IsOpenFor(MenuMode mode)
	{
		return _open && _activeMode == mode;
	}

	/// <summary>Last validation message, for tests and diagnostics.</summary>
	public static string Hint => _hint;

	/// <summary>
	/// Opens the keyboard unless one is already showing. <paramref name="onAccept"/> only
	/// ever receives a cleaned, length-checked name.
	/// </summary>
	public static void Ask(MenuMode mode, string title, string initial, int maxLength, Action<string> onAccept, Action onCancel)
	{
		if (_open)
		{
			// Already asking: the first frame did the work.
			return;
		}
		_open = true;
		_activeMode = mode;
		_title = title;
		_hint = null;
		_maxLength = maxLength;
		_onAccept = onAccept;
		_onCancel = onCancel;
		Show(initial);
	}

	private static void Show(string initial)
	{
		string title = _title;
		if (!string.IsNullOrEmpty(_hint))
		{
			title = title + "  (" + _hint + ")";
		}
		Input.VirtualKeyboard.Open(title, initial, OnAccepted, OnCancelled);
	}

	private static void OnAccepted(string text)
	{
		string name;
		string error;
		if (!NameRules.TryNormalize(text, _maxLength, out name, out error))
		{
			// Still open, with the typed text intact and the reason in the title.
			_hint = error;
			Show(text);
			return;
		}
		Action<string> onAccept = _onAccept;
		Finish();
		if (onAccept != null)
		{
			onAccept(name);
		}
	}

	private static void OnCancelled()
	{
		Action onCancel = _onCancel;
		Finish();
		if (onCancel != null)
		{
			onCancel();
		}
	}

	private static void Finish()
	{
		_open = false;
		_activeMode = MenuMode.NONE;
		_title = null;
		_hint = null;
		_maxLength = 0;
		_onAccept = null;
		_onCancel = null;
	}

	/// <summary>
	/// Drops the latch and closes the keyboard if it belongs to a prompt. Called when a menu
	/// is left by any route, so a keyboard can never outlive the question it was asking.
	/// </summary>
	internal static void Reset()
	{
		if (_open && Input.VirtualKeyboard.IsOpen)
		{
			// Close (not cancel): the caller already decided to move on, and running the
			// cancel callback here would jump to a menu the player never chose.
			Input.VirtualKeyboard.Close();
		}
		Finish();
	}
}
