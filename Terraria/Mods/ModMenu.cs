using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.Input;

namespace Terraria.Mods;

/// <summary>
/// In-game mod manager. Lists every folder under <c>Storage/Mods</c>, shows whether it is
/// loaded, and lets the player enable or disable it by toggling <c>disabled.txt</c>.
///
/// Loading itself happens at startup, so a change always reports "restart required" instead
/// of pretending to hot-swap assemblies - unloading an assembly that the game has already
/// bound to is not something .NET supports.
/// </summary>
internal static class ModMenu
{
	private const int RowHeight = 30;

	private const int VisibleRows = 10;

	private sealed class Entry
	{
		public string Folder;

		public bool Enabled;

		public string Detail;
	}

	private static readonly List<Entry> Entries = new List<Entry>();

	private static int _selected;

	private static int _scroll;

	private static string _status;

	private static int _statusTime;

	private static bool _dirty;

	public static void Reset()
	{
		_selected = 0;
		_scroll = 0;
		_status = null;
		_statusTime = 0;
		_dirty = false;
		Rebuild();
	}

	/// <summary>Rescans the mods folder. Cheap: one directory listing.</summary>
	internal static void Rebuild()
	{
		Entries.Clear();
		string text = ModLoader.ModsDirectory;
		if (!Directory.Exists(text))
		{
			return;
		}
		string[] directories = Directory.GetDirectories(text);
		Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
		string[] array = directories;
		foreach (string text2 in array)
		{
			string[] files = Directory.GetFiles(text2, "*.dll", SearchOption.TopDirectoryOnly);
			if (files.Length == 0)
			{
				continue;
			}
			bool flag = !File.Exists(Path.Combine(text2, "disabled.txt"));
			Entry entry = new Entry
			{
				Folder = Path.GetFileName(text2),
				Enabled = flag,
				Detail = ""
			};
			// Match the loaded instance (if any) to show its version.
			for (int i = 0; i < ModLoader.Mods.Count; i++)
			{
				Mod mod = ModLoader.Mods[i];
				if (mod.Name != null && mod.Name.Equals(entry.Folder, StringComparison.OrdinalIgnoreCase))
				{
					entry.Detail = "v" + mod.Version;
					break;
				}
			}
			if (flag && entry.Detail.Length == 0 && !ModLoader.Enabled)
			{
				entry.Detail = Texts.Get("ui.mods.disabled.settings");
			}
			Entries.Add(entry);
		}
	}

	/// <summary>Returns true when the screen should close.</summary>
	public static bool Update()
	{
		if (_statusTime > 0 && --_statusTime == 0)
		{
			_status = null;
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
		if (KeyboardMouseInput.KeyPressed(Keys.Escape))
		{
			return true;
		}
		if (UI.PointerActive)
		{
			int num = RowUnderPointer();
			if (num >= 0)
			{
				_selected = num;
				if (KeyboardMouseInput.LeftClicked)
				{
					Toggle(num);
				}
			}
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Space))
		{
			Toggle(_selected);
		}
		// Left/Right changes the load order: last loaded wins item overrides, so this is
		// how a player resolves a conflict between two mods.
		if (KeyboardMouseInput.KeyPressed(Keys.Left))
		{
			Move(-1);
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Right))
		{
			Move(1);
		}
		if (KeyboardMouseInput.KeyPressed(Keys.R))
		{
			Rebuild();
			_status = Texts.Format("ui.mods.rescanned", ModLoader.ModsDirectory);
			_statusTime = 180;
		}
		return false;
	}

	private static void Toggle(int index)
	{
		if (index < 0 || index >= Entries.Count)
		{
			return;
		}
		Entry entry = Entries[index];
		string path = Path.Combine(ModLoader.ModsDirectory, entry.Folder, "disabled.txt");
		try
		{
			if (entry.Enabled)
			{
				File.WriteAllText(path, "Disable this mod by keeping this file.\r\nRemove it to enable the mod again.\r\n");
			}
			else
			{
				File.Delete(path);
			}
			entry.Enabled = !entry.Enabled;
			_dirty = true;
			_status = Texts.Format(entry.Enabled ? "ui.mods.enabled" : "ui.mods.disabled", entry.Folder);
			_statusTime = 240;
			Main.PlaySound(12);
		}
		catch (Exception ex)
		{
			_status = Texts.Format("ui.mods.failed", entry.Folder, ex.Message);
			_statusTime = 300;
		}
	}

	/// <summary>
	/// Moves the selected mod up or down the load order and saves it. The list on screen is
	/// rebuilt from the file so what is shown is exactly what will load next time.
	/// </summary>
	private static void Move(int direction)
	{
		if (_selected < 0 || _selected >= Entries.Count)
		{
			return;
		}
		int num = _selected + direction;
		if (num < 0 || num >= Entries.Count)
		{
			return;
		}
		List<string> list = new List<string>(Entries.Count);
		for (int i = 0; i < Entries.Count; i++)
		{
			list.Add(Entries[i].Folder);
		}
		string text = list[_selected];
		list[_selected] = list[num];
		list[num] = text;
		if (!ModLoader.WriteLoadOrder(ModLoader.ModsDirectory, list))
		{
			_status = Texts.Get("ui.mods.order.failed");
			_statusTime = 300;
			return;
		}
		_selected = num;
		Rebuild();
		// Rebuild sorts by the file we just wrote, so keep the cursor on the moved mod.
		for (int j = 0; j < Entries.Count; j++)
		{
			if (Entries[j].Folder == text)
			{
				_selected = j;
				break;
			}
		}
		ClampScroll();
		_status = Texts.Format("ui.mods.order.moved", text, Texts.Get((direction < 0) ? "ui.mods.moved.up" : "ui.mods.moved.down"));
		_statusTime = 240;
		Main.PlaySound(12);
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
		int num = (int)(440f * Main.ScreenMultiplier);
		int num2 = (int)(120f * Main.ScreenMultiplier);
		int num3 = (ui.CurrentView.ViewWidth - num) / 2;
		rows = new Rectangle(num3, num2, num, VisibleRows * (int)(RowHeight * Main.ScreenMultiplier));
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
		int num2 = num / (int)(RowHeight * Main.ScreenMultiplier);
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
		int num2 = rectangle.X;
		int num3 = rectangle.Y;
		int width = rectangle.Width;
		Main.DrawSolidRect(new Rectangle(num2 - (int)(8f * num), num3 - (int)(52f * num), width + (int)(16f * num), rectangle.Height + (int)(110f * num)), new Color(0, 0, 0, 190));
		UI.DrawStringCC(UI.BigFont, Texts.Get("ui.mods.title"), rectangle.Center.X, num3 - (int)(40f * num), Color.White);
		string text = Texts.Format("ui.mods.signature", ModLoader.Signature, ModLoader.Mods.Count);
		UI.DrawStringCC(UI.SmallFont, text, rectangle.Center.X, num3 - (int)(16f * num), new Color(150, 160, 180));
		if (Entries.Count == 0)
		{
			UI.DrawStringCC(UI.SmallFont, Texts.Format("ui.mods.none", ModLoader.ModsDirectory), rectangle.Center.X, num3 + (int)(10f * num), new Color(200, 200, 200));
		}
		int num4 = Math.Min(VisibleRows, Entries.Count - _scroll);
		for (int i = 0; i < num4; i++)
		{
			int num5 = _scroll + i;
			Entry entry = Entries[num5];
			int y = num3 + i * (int)(RowHeight * num);
			if (num5 == _selected)
			{
				Main.DrawSolidRect(new Rectangle(num2, y, width, (int)(RowHeight * num) - 2), new Color(40, 70, 120, 220));
			}
			Color color = entry.Enabled ? new Color(120, 230, 140) : new Color(160, 160, 160);
			UI.DrawStringLC(UI.BoldSmallFont, Texts.Get(entry.Enabled ? "ui.mods.entry.on" : "ui.mods.entry.off") + entry.Folder, num2 + (int)(8f * num), y + (int)(8f * num), color);
			if (entry.Detail.Length > 0)
			{
				Vector2 vector = UI.SmallFont.MeasureString(entry.Detail);
				UI.DrawStringLC(UI.SmallFont, entry.Detail, rectangle.Right - (int)vector.X - (int)(8f * num), y + (int)(10f * num), new Color(150, 160, 180));
			}
		}
		int num6 = num3 + VisibleRows * (int)(RowHeight * num) + (int)(6f * num);
		string text2 = _status ?? Texts.Get("ui.mods.change.back");
		UI.DrawStringCC(UI.SmallFont, text2, rectangle.Center.X, num6, new Color(150, 160, 180));
		if (_dirty)
		{
			UI.DrawStringCC(UI.SmallFont, Texts.Get("ui.mods.hint.restart"), rectangle.Center.X, num6 + (int)(18f * num), new Color(255, 210, 120));
		}
		if (ModLoader.LoadErrors.Count > 0)
		{
			UI.DrawStringCC(UI.SmallFont, Texts.Format("ui.mods.errors", ModLoader.LoadErrors.Count), rectangle.Center.X, num6 + (int)(36f * num), new Color(255, 120, 120));
		}
	}
}
