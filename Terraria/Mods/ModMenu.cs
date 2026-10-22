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
/// It also lists the old tConfig packs from <c>Storage/tConfig</c>, tagged <c>[tConfig]</c>.
/// Those are deliberately **not** loaded as mods: a pack is a folder of INI files, it has no
/// name or version of its own, and it takes no part in the multiplayer signature. Showing them
/// is the point - otherwise a player has a folder that quietly changes item numbers on one
/// machine and no way to see it, name it, or share it. Converting one into a real mod is a
/// keypress away.
///
/// Loading itself happens at startup, so a change always reports "restart required" instead
/// of pretending to hot-swap assemblies - unloading an assembly that the game has already
/// bound to is not something .NET supports.
/// </summary>
internal static class ModMenu
{
	private const int RowHeight = 30;

	private const int VisibleRows = 10;

	internal enum EntryKind
	{
		/// <summary>A real mod: <c>Storage/Mods/&lt;Folder&gt;/*.dll</c>.</summary>
		Mod,

		/// <summary>A legacy tConfig pack: <c>Storage/tConfig/&lt;Folder&gt;/Item/*.ini</c>.</summary>
		Pack
	}

	private sealed class Entry
	{
		public string Folder;

		public bool Enabled;

		public string Detail;

		public EntryKind Kind;

		/// <summary>Full path of the pack folder; empty for mods.</summary>
		public string PackPath;
	}

	/// <summary>A conversion that finished and is waiting for "delete the original?".</summary>
	private sealed class PendingDelete
	{
		public string Folder;

		public string PackPath;

		public string ProjectDirectory;
	}

	private static readonly List<Entry> Entries = new List<Entry>();

	private static int _selected;

	private static int _scroll;

	private static string _status;

	private static int _statusTime;

	private static bool _dirty;

	private static PendingDelete _pending;

	/// <summary>
	/// Test hooks: where to scan instead of the real folders.
	///
	/// The menu reads <c>Storage/Mods</c> and <c>Storage/tConfig</c>, and a test that wrote into
	/// those would be writing into the player's own folders. Pointing the scan at a temporary
	/// directory is the difference between a check and an accident.
	/// </summary>
	internal static string ModsRootOverride;

	internal static string PacksRootOverride;

	/// <summary>Test hook: where conversions write instead of Storage/ModSources.</summary>
	internal static string ModSourcesRootOverride;

	/// <summary>Test hook: where deleted packs go instead of Storage/Backups.</summary>
	internal static string BackupsRootOverride;

	/// <summary>Two buttons of the confirmation, rebuilt while drawing so clicks match pixels.</summary>
	private static Rectangle _yesRect;

	private static Rectangle _noRect;

	private static bool _confirmLayoutValid;

	public static void Reset()
	{
		_selected = 0;
		_scroll = 0;
		_status = null;
		_statusTime = 0;
		_dirty = false;
		_pending = null;
		_confirmLayoutValid = false;
		Rebuild();
	}

	/// <summary>Rescans both folders. Cheap: two directory listings.</summary>
	internal static void Rebuild()
	{
		Entries.Clear();
		RebuildMods();
		RebuildPacks();
	}

	private static void RebuildMods()
	{
		string text = ModsRootOverride ?? ModLoader.ModsDirectory;
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
				Detail = "",
				Kind = EntryKind.Mod
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

	private static void RebuildPacks()
	{
		string importFolder = PacksRootOverride ?? TConfigImporter.ImportFolder;
		if (!Directory.Exists(importFolder))
		{
			return;
		}
		string[] directories = Directory.GetDirectories(importFolder);
		Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
		string[] array = directories;
		foreach (string text in array)
		{
			// A pack is a folder with Item/*.ini in it; anything else in that folder is not a
			// pack, and listing it as one would only invite a conversion that cannot work.
			string text2 = Path.Combine(text, "Item");
			if (!Directory.Exists(text2) || Directory.GetFiles(text2, "*.ini", SearchOption.TopDirectoryOnly).Length == 0)
			{
				continue;
			}
			Entries.Add(new Entry
			{
				Folder = Path.GetFileName(text),
				// A pack is never loaded, so "enabled" is not a meaningful state. False here means
				// "cannot be toggled", which is exactly what the row shows.
				Enabled = false,
				Detail = Texts.Get("ui.mods.pack.note"),
				Kind = EntryKind.Pack,
				PackPath = text
			});
		}
	}

	/// <summary>Returns true when the screen should close.</summary>
	public static bool Update()
	{
		if (_statusTime > 0 && --_statusTime == 0)
		{
			_status = null;
		}
		// The confirmation owns the input while it is up: a click on "Delete" must not also
		// toggle the row underneath it.
		if (_pending != null)
		{
			return UpdateConfirm();
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
					Activate(num);
				}
			}
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Space))
		{
			Activate(_selected);
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
		// Convert. On a mod this does nothing: there is nothing to convert, and quietly
		// re-converting a mod folder would be a surprise.
		if (KeyboardMouseInput.KeyPressed(Keys.C) || KeyboardMouseInput.KeyPressed(Keys.Enter))
		{
			ConvertSelected();
		}
		if (KeyboardMouseInput.KeyPressed(Keys.R))
		{
			Rebuild();
			_status = Texts.Format("ui.mods.rescanned", ModsRootOverride ?? ModLoader.ModsDirectory);
			_statusTime = 180;
		}
		return false;
	}

	/// <summary>The only action a row has: a mod toggles, a pack converts.</summary>
	private static void Activate(int index)
	{
		if (index < 0 || index >= Entries.Count)
		{
			return;
		}
		if (Entries[index].Kind == EntryKind.Pack)
		{
			ConvertSelected();
			return;
		}
		Toggle(index);
	}

	private static bool UpdateConfirm()
	{
		// Escape and N both mean "keep". Escape is the safe answer, and a confirmation whose
		// cancel key is not the obvious one is a trap.
		if (KeyboardMouseInput.KeyPressed(Keys.Escape) || KeyboardMouseInput.KeyPressed(Keys.N))
		{
			Finish(false);
			return false;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Y) || KeyboardMouseInput.KeyPressed(Keys.Enter))
		{
			Finish(true);
			return false;
		}
		if (UI.PointerActive && _confirmLayoutValid && KeyboardMouseInput.LeftClicked)
		{
			int num = KeyboardMouseInput.MouseX;
			int num2 = KeyboardMouseInput.MouseY;
			if (_yesRect.Contains(num, num2))
			{
				Finish(true);
				return false;
			}
			if (_noRect.Contains(num, num2))
			{
				Finish(false);
				return false;
			}
		}
		return false;
	}

	private static void ConvertSelected()
	{
		if (_selected < 0 || _selected >= Entries.Count)
		{
			return;
		}
		Entry entry = Entries[_selected];
		if (entry.Kind != EntryKind.Pack)
		{
			return;
		}
		string output = Path.Combine(ModSourcesRootOverride ?? Terraria.Storage.AppPaths.ModSources, entry.Folder);
		string error;
		TConfigConverter.Result result = TConfigConverter.Convert(entry.PackPath, output, entry.Folder, out error);
		if (result == null)
		{
			_status = Texts.Format("ui.mods.convert.failed", entry.Folder, error);
			_statusTime = 600;
			Main.PlaySound(14);
			return;
		}
		Main.PlaySound(12);
		_status = Texts.Format("ui.mods.convert.done", entry.Folder, result.OutputDirectory);
		_statusTime = 600;
		_pending = new PendingDelete
		{
			Folder = entry.Folder,
			PackPath = entry.PackPath,
			ProjectDirectory = result.OutputDirectory
		};
		_confirmLayoutValid = false;
	}

	/// <summary>
	/// Answers the confirmation. Deleting means moving the pack into <c>Storage/Backups</c>,
	/// not removing it: the only copy of somebody's work should not disappear because a menu
	/// button was pressed, and the backup folder already exists for exactly this kind of thing.
	/// </summary>
	private static void Finish(bool delete)
	{
		PendingDelete pending = _pending;
		_pending = null;
		_confirmLayoutValid = false;
		if (pending == null)
		{
			return;
		}
		if (!delete)
		{
			_status = Texts.Format("ui.mods.convert.kept", pending.Folder);
			_statusTime = 420;
			return;
		}
		try
		{
			string backups = BackupsRootOverride ?? Terraria.Storage.AppPaths.Backups;
			string text = Path.Combine(backups,
				"tConfig_" + pending.Folder + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
			Directory.CreateDirectory(backups);
			Directory.Move(pending.PackPath, text);
			_status = Texts.Format("ui.mods.convert.deleted", pending.Folder);
			_statusTime = 420;
			Main.PlaySound(12);
			Rebuild();
			ClampSelection();
		}
		catch (Exception ex)
		{
			_status = Texts.Format("ui.mods.convert.delete.failed", pending.Folder, ex.Message);
			_statusTime = 600;
		}
	}

	private static void Toggle(int index)
	{
		if (index < 0 || index >= Entries.Count)
		{
			return;
		}
		Entry entry = Entries[index];
		if (entry.Kind != EntryKind.Mod)
		{
			return;
		}
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
	/// Moves the selected mod up or down the load order and saves it. Packs are not part of the
	/// order - they are not loaded - so they are skipped rather than being written into the file
	/// where the next startup would find a folder that is not a mod.
	/// </summary>
	private static void Move(int direction)
	{
		if (_selected < 0 || _selected >= Entries.Count)
		{
			return;
		}
		Entry entry = Entries[_selected];
		if (entry.Kind != EntryKind.Mod)
		{
			return;
		}
		List<string> list = new List<string>();
		for (int i = 0; i < Entries.Count; i++)
		{
			if (Entries[i].Kind == EntryKind.Mod)
			{
				list.Add(Entries[i].Folder);
			}
		}
		int num = list.IndexOf(entry.Folder);
		if (num < 0)
		{
			return;
		}
		int num2 = num + direction;
		if (num2 < 0 || num2 >= list.Count)
		{
			return;
		}
		string text = list[num];
		list[num] = list[num2];
		list[num2] = text;
		if (!ModLoader.WriteLoadOrder(ModLoader.ModsDirectory, list))
		{
			_status = Texts.Get("ui.mods.order.failed");
			_statusTime = 300;
			return;
		}
		Rebuild();
		// Rebuild sorts by the file we just wrote, so keep the cursor on the moved mod.
		for (int j = 0; j < Entries.Count; j++)
		{
			if (Entries[j].Kind == EntryKind.Mod && Entries[j].Folder == text)
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

	private static void ClampSelection()
	{
		if (_selected >= Entries.Count)
		{
			_selected = Math.Max(0, Entries.Count - 1);
		}
		ClampScroll();
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
		if (_scroll > Math.Max(0, Entries.Count - VisibleRows))
		{
			_scroll = Math.Max(0, Entries.Count - VisibleRows);
		}
		if (_scroll < 0)
		{
			_scroll = 0;
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
			UI.DrawStringCC(UI.SmallFont, Texts.Format("ui.mods.none", ModsRootOverride ?? ModLoader.ModsDirectory), rectangle.Center.X, num3 + (int)(10f * num), new Color(200, 200, 200));
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
			// The tag comes first so the kind lines up in one column across every row: a list
			// where the tag moves with the name length is a list you have to read twice.
			string text2 = Texts.Get(entry.Kind == EntryKind.Pack ? "ui.mods.kind.tconfig" : "ui.mods.kind.xdk");
			Color color = (entry.Kind == EntryKind.Mod) ? (entry.Enabled ? new Color(120, 230, 140) : new Color(160, 160, 160))
				: new Color(230, 200, 130);
			UI.DrawStringLC(UI.SmallFont, text2, num2 + (int)(8f * num), y + (int)(9f * num), new Color(140, 150, 170));
			float num6 = UI.SmallFont.MeasureString(text2).X;
			// A mod shows its on/off state; a pack has no such state, so it shows its name plain.
			string text3 = (entry.Kind == EntryKind.Mod)
				? (Texts.Get(entry.Enabled ? "ui.mods.entry.on" : "ui.mods.entry.off") + entry.Folder)
				: entry.Folder;
			UI.DrawStringLC(UI.BoldSmallFont, text3, num2 + (int)(8f * num) + (int)num6 + (int)(4f * num), y + (int)(8f * num), color);
			if (entry.Detail.Length > 0)
			{
				Vector2 vector = UI.SmallFont.MeasureString(entry.Detail);
				UI.DrawStringLC(UI.SmallFont, entry.Detail, rectangle.Right - (int)vector.X - (int)(8f * num), y + (int)(10f * num), new Color(150, 160, 180));
			}
		}
		int num7 = num3 + VisibleRows * (int)(RowHeight * num) + (int)(6f * num);
		string text4 = _status ?? Texts.Get("ui.mods.change.back");
		UI.DrawStringCC(UI.SmallFont, text4, rectangle.Center.X, num7, new Color(150, 160, 180));
		// The convert hint only appears when it can actually be used - a permanent line about a
		// key that does nothing on the selected row is noise.
		int num8 = 0;
		if (_selected >= 0 && _selected < Entries.Count && Entries[_selected].Kind == EntryKind.Pack)
		{
			UI.DrawStringCC(UI.SmallFont, Texts.Get("ui.mods.pack.hint"), rectangle.Center.X, num7 + (int)(18f * num), new Color(230, 200, 130));
			num8 = 18;
		}
		if (_dirty)
		{
			UI.DrawStringCC(UI.SmallFont, Texts.Get("ui.mods.hint.restart"), rectangle.Center.X, num7 + (int)((18 + num8) * num), new Color(255, 210, 120));
			num8 += 18;
		}
		if (ModLoader.LoadErrors.Count > 0)
		{
			UI.DrawStringCC(UI.SmallFont, Texts.Format("ui.mods.errors", ModLoader.LoadErrors.Count), rectangle.Center.X, num7 + (int)((18 + num8) * num), new Color(255, 120, 120));
		}
		if (_pending != null)
		{
			DrawConfirm(rectangle, num);
		}
	}

	/// <summary>
	/// The "delete the original?" question. Drawn over the list, with two real buttons rather
	/// than a key hint only: the question is about deleting somebody's files, and that deserves
	/// something you can click.
	/// </summary>
	private static void DrawConfirm(Rectangle rows, float scale)
	{
		int width = (int)(380f * scale);
		int height = (int)(110f * scale);
		Rectangle rectangle = new Rectangle(rows.Center.X - width / 2, rows.Center.Y - height / 2, width, height);
		Main.DrawSolidRect(rectangle, new Color(20, 24, 34, 245));
		Main.DrawRect(451, rectangle, 255);
		UI.DrawStringCC(UI.SmallFont, Texts.Format("ui.mods.convert.confirm", _pending.Folder),
			rectangle.Center.X, rectangle.Y + (int)(18f * scale), Color.White);
		UI.DrawStringCC(UI.SmallFont, Texts.Get("ui.mods.convert.confirm.note"),
			rectangle.Center.X, rectangle.Y + (int)(42f * scale), new Color(150, 160, 180));
		int num = (int)(120f * scale);
		int num2 = (int)(34f * scale);
		int num3 = rectangle.Y + height - num2 - (int)(14f * scale);
		_yesRect = new Rectangle(rectangle.Center.X - num - (int)(8f * scale), num3, num, num2);
		_noRect = new Rectangle(rectangle.Center.X + (int)(8f * scale), num3, num, num2);
		_confirmLayoutValid = true;
		DrawButton(_yesRect, Texts.Get("ui.mods.convert.yes"), new Color(180, 90, 90));
		DrawButton(_noRect, Texts.Get("ui.mods.convert.no"), new Color(70, 100, 150));
	}

	private static void DrawButton(Rectangle rect, string label, Color color)
	{
		Main.DrawSolidRect(rect, color);
		UI.DrawStringCC(UI.SmallFont, label, rect.Center.X, rect.Center.Y - (int)(7f * Main.ScreenMultiplier), Color.White);
	}

	/// <summary>Test hook: what the list holds, without a screen.</summary>
	internal static int EntryCount => Entries.Count;

	/// <summary>Test hook: the kind of one row.</summary>
	internal static bool IsPackAt(int index)
	{
		return index >= 0 && index < Entries.Count && Entries[index].Kind == EntryKind.Pack;
	}

	/// <summary>Test hook: the folder name of one row.</summary>
	internal static string FolderAt(int index)
	{
		return (index < 0 || index >= Entries.Count) ? null : Entries[index].Folder;
	}

	/// <summary>Test hook: selects a row as if the player had.</summary>
	internal static void SelectForTests(int index)
	{
		if (index < 0 || index >= Entries.Count)
		{
			return;
		}
		_selected = index;
		ClampScroll();
	}

	/// <summary>Test hook: runs the conversion action on the selected row.</summary>
	internal static void ConvertSelectedForTests()
	{
		ConvertSelected();
	}

	/// <summary>Test hook: true while the delete confirmation is up.</summary>
	internal static bool IsConfirmingForTests => _pending != null;

	/// <summary>Test hook: answers the confirmation.</summary>
	internal static void AnswerConfirmForTests(bool delete)
	{
		Finish(delete);
	}

	/// <summary>Test hook: the status line, for asserting on the outcome.</summary>
	internal static string StatusForTests => _status;
}
