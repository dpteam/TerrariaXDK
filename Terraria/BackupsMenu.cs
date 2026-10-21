using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.Input;

namespace Terraria;

/// <summary>
/// Restores a world from the dated backups in <c>Storage/Backups</c>.
///
/// Backups were already written on every save, but the only way to use one was to find the
/// folder and copy the file over the save by hand - so in practice nobody did, which makes
/// the backups worthless exactly when they matter. This screen is the missing half.
///
/// Only reachable from the world list, where no world is loaded, so a restore can never race
/// with the game writing the same file.
/// </summary>
internal static class BackupsMenu
{
	private const int VisibleRows = 10;

	private static readonly List<string> Entries = new List<string>();

	private static string _worldFile;

	private static string _worldName;

	private static int _selected;

	private static int _scroll;

	private static string _status;

	private static int _statusTime;

	private static bool _confirming;

	/// <summary>Number of listed backups. Used by the self-test.</summary>
	public static int Count => Entries.Count;

	public static void Open(string worldFile, string worldName)
	{
		_worldFile = worldFile;
		_worldName = worldName;
		_selected = 0;
		_scroll = 0;
		_status = null;
		_statusTime = 0;
		_confirming = false;
		if (string.IsNullOrEmpty(worldName))
		{
			_status = Texts.Get("ui.backups.no.name");
			_statusTime = 600;
			Entries.Clear();
			return;
		}
		Reload();
		if (Entries.Count == 0)
		{
			// The player asked for something and got nothing; say why instead of showing an
			// empty list that looks broken.
			_status = Texts.Get("ui.backups.empty");
			_statusTime = 600;
		}
	}

	private static void Reload()
	{
		Entries.Clear();
		Entries.AddRange(WorldBackups.ListFor(_worldName));
		if (_selected >= Entries.Count)
		{
			_selected = Math.Max(0, Entries.Count - 1);
		}
		if (_scroll > _selected)
		{
			_scroll = _selected;
		}
	}

	/// <summary>Returns true when the screen should close.</summary>
	public static bool Update()
	{
		if (_statusTime > 0 && --_statusTime == 0)
		{
			_status = null;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Escape))
		{
			if (_confirming)
			{
				// Esc cancels the confirmation first, then leaves. One press must not do both.
				_confirming = false;
				_status = Texts.Get("ui.backups.cancelled");
				_statusTime = 180;
				return false;
			}
			return true;
		}
		if (Entries.Count == 0)
		{
			return false;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Down))
		{
			_selected = Math.Min(Entries.Count - 1, _selected + 1);
			ClampScroll();
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Up))
		{
			_selected = Math.Max(0, _selected - 1);
			ClampScroll();
		}
		if (UI.PointerActive)
		{
			int num = RowUnderPointer();
			if (num >= 0)
			{
				_selected = num;
				if (KeyboardMouseInput.LeftClicked)
				{
					AskToRestore();
				}
			}
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Space) || KeyboardMouseInput.KeyPressed(Keys.Enter))
		{
			AskToRestore();
		}
		if (KeyboardMouseInput.KeyPressed(Keys.R))
		{
			Reload();
			_status = Texts.Format("ui.backups.count", Entries.Count);
			_statusTime = 180;
		}
		return false;
	}

	/// <summary>
	/// Two-step: the first activation asks, the second does it. Overwriting a save is not
	/// something to do on a stray keypress.
	/// </summary>
	private static void AskToRestore()
	{
		if (!_confirming)
		{
			_confirming = true;
			_status = Texts.Format("ui.backups.confirm", _worldName);
			_statusTime = 600;
			Main.PlaySound(12);
			return;
		}
		_confirming = false;
		string error;
		if (!WorldBackups.Restore(Entries[_selected], _worldFile, out error))
		{
			_status = Texts.Format("ui.backups.failed", error);
			_statusTime = 600;
			Main.PlaySound(11);
			return;
		}
		Main.PlaySound(10);
		_status = Texts.Format("ui.backups.restored", WorldBackups.Describe(Entries[_selected]));
		_statusTime = 600;
		// The save file changed underneath the list; reload so the world entry is accurate.
		WorldSelect.LoadWorlds();
	}

	private static void ClampScroll()
	{
		if (_selected < _scroll)
		{
			_scroll = _selected;
		}
		else if (_selected >= _scroll + VisibleRows)
		{
			_scroll = _selected - VisibleRows + 1;
		}
	}

	private static bool TryGetLayout(out Rectangle rows)
	{
		rows = default;
		UI ui = UI.MainUI;
		if (ui == null || ui.CurrentView == null)
		{
			return false;
		}
		int num = (int)(520f * Main.ScreenMultiplier);
		int num2 = (int)(130f * Main.ScreenMultiplier);
		int num3 = (ui.CurrentView.ViewWidth - num) / 2;
		rows = new Rectangle(num3, num2, num, VisibleRows * (int)(40f * Main.ScreenMultiplier));
		return true;
	}

	private static int RowUnderPointer()
	{
		Rectangle rectangle;
		if (!TryGetLayout(out rectangle))
		{
			return -1;
		}
		if (KeyboardMouseInput.MouseX < rectangle.X || KeyboardMouseInput.MouseX > rectangle.Right)
		{
			return -1;
		}
		int num = KeyboardMouseInput.MouseY - rectangle.Y;
		if (num < 0)
		{
			return -1;
		}
		int num2 = num / (int)(40f * Main.ScreenMultiplier);
		if (num2 < 0 || num2 >= Math.Min(VisibleRows, Entries.Count - _scroll))
		{
			return -1;
		}
		return _scroll + num2;
	}

	public static void Draw()
	{
		Rectangle rectangle;
		if (!TryGetLayout(out rectangle))
		{
			return;
		}
		float num = Main.ScreenMultiplier;
		Main.DrawSolidRect(new Rectangle(0, 0, Main.ResolutionWidth, Main.ResolutionHeight), new Color(0, 0, 0, 180));
		Main.DrawSolidRect(new Rectangle(rectangle.X - (int)(10f * num), rectangle.Y - (int)(46f * num), rectangle.Width + (int)(20f * num), rectangle.Height + (int)(86f * num)), new Color(14, 18, 26, 245));
		UI.DrawStringCC(UI.BigFont, Texts.Format("ui.backups.title", _worldName ?? ""), rectangle.Center.X, rectangle.Y - (int)(34f * num), Color.White);
		if (Entries.Count == 0)
		{
			UI.DrawStringCC(UI.SmallFont, _status ?? Texts.Get("ui.backups.none"), rectangle.Center.X, rectangle.Y + (int)(10f * num), new Color(170, 170, 180));
			return;
		}
		int num2 = Math.Min(VisibleRows, Entries.Count - _scroll);
		for (int i = 0; i < num2; i++)
		{
			int index = _scroll + i;
			int y = rectangle.Y + i * (int)(40f * num);
			bool flag = index == _selected;
			if (flag)
			{
				Main.DrawSolidRect(new Rectangle(rectangle.X, y, rectangle.Width, (int)(40f * num) - 2), new Color(40, 70, 120, 220));
			}
			string text = WorldBackups.Describe(Entries[index]);
			Color color = flag ? Color.White : new Color(200, 200, 205);
			UI.DrawStringLC(UI.BoldSmallFont, text, rectangle.X + (int)(10f * num), y + (int)(10f * num), color);
			// Size helps spot a truncated (0-byte or half-written) backup.
			string text2 = DescribeSize(Entries[index]);
			UI.DrawStringRC(UI.SmallFont, text2, rectangle.Right - (int)(10f * num), y + (int)(12f * num), new Color(150, 150, 160));
		}
		int num3 = rectangle.Y + rectangle.Height + (int)(8f * num);
		UI.DrawStringCC(UI.SmallFont, _status ?? Texts.Get("ui.backups.change.back"), rectangle.Center.X, num3, new Color(150, 160, 180));
	}

	private static string DescribeSize(string path)
	{
		try
		{
			long length = new FileInfo(path).Length;
			if (length < 1024)
			{
				return length + " B";
			}
			if (length < 1024 * 1024)
			{
				return (length / 1024) + " KB";
			}
			return (length / (1024 * 1024)) + " MB";
		}
		catch
		{
			return "?";
		}
	}
}
