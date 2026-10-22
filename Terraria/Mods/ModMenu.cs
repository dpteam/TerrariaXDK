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

		/// <summary>
		/// A mod with no assembly: <c>Storage/Mods/&lt;Folder&gt;/Item/*.ini</c> plus sprites.
		/// Loaded, counted and signed like any other mod - it just cannot add behaviour.
		/// </summary>
		Raw,

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
			bool flag2 = files.Length > 0;
			// A folder with no assembly is still a mod if it holds item definitions: that is the
			// raw form, and listing it as "not a mod" is what made a working pack invisible.
			bool flag3 = !flag2 && RawMod.LooksLikeRawMod(text2);
			if (!flag2 && !flag3)
			{
				continue;
			}
			bool flag = !File.Exists(Path.Combine(text2, "disabled.txt"));
			Entry entry = new Entry
			{
				Folder = Path.GetFileName(text2),
				Enabled = flag,
				Detail = "",
				Kind = (flag2 ? EntryKind.Mod : EntryKind.Raw)
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
			if (entry.Kind == EntryKind.Raw)
			{
				for (int i = 0; i < ModLoader.RawMods.Count; i++)
				{
					RawMod rawMod = ModLoader.RawMods[i];
					if (!rawMod.Name.Equals(entry.Folder, StringComparison.OrdinalIgnoreCase)
						&& !Path.GetFileName(rawMod.Directory).Equals(entry.Folder, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					// The skipped count is the point: a pack with 146 NPC files that loads "fine" and
					// spawns nothing is the report this line prevents.
					int num = rawMod.IgnoredFileCount;
					entry.Detail = (num > 0)
						? Texts.Format("ui.mods.raw.detail.skipped", rawMod.NewItems + rawMod.Overrides, num, rawMod.Version)
						: Texts.Format("ui.mods.raw.detail", rawMod.NewItems + rawMod.Overrides, rawMod.Version);
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

	/// <summary>The conversion in progress, or null.</summary>
	private static TConfigConverter.Job _job;

	/// <summary>
	/// How many files one frame may convert. Small on purpose: a pack of two hundred items takes a
	/// few seconds, and the screen has to keep drawing for the bar to mean anything.
	/// </summary>
	private const int FilesPerFrame = 4;

	/// <summary>Advances the running conversion, and finishes it when the last file is done.</summary>
	private static void UpdateJob()
	{
		_job.Step(FilesPerFrame);
		if (!_job.Done)
		{
			return;
		}
		TConfigConverter.Job job = _job;
		_job = null;
		string error;
		if (!job.Finish(out error))
		{
			_status = Texts.Format("ui.mods.convert.failed", job.ModName, error);
			_statusTime = 600;
			Main.PlaySound(14);
			return;
		}
		Main.PlaySound(12);
		// One line has room for one sentence, so it says the two things that matter: it worked,
		// and whether anything was lost. The detail is in the log and in the generated README.
		if (job.Result.HasProblems)
		{
			_status = Texts.Format(job.Result.IsRaw ? "ui.mods.convert.raw.partial" : "ui.mods.convert.partial",
				job.ModName,
				(job.Result.Errors.Count + job.Result.Warnings.Count).ToString(),
				string.IsNullOrEmpty(job.Result.LogFile) ? job.Result.OutputDirectory : job.Result.LogFile);
		}
		else
		{
			_status = Texts.Format(job.Result.IsRaw ? "ui.mods.convert.raw.done" : "ui.mods.convert.done",
				job.ModName, job.Result.OutputDirectory);
		}
		_statusTime = 900;
		// A raw mod landed in Storage/Mods, so the game has something new to load - the same
		// "restart required" a toggle shows. A project is not loaded until it is built and copied.
		if (job.Result.IsRaw)
		{
			_dirty = true;
		}
		_pending = new PendingDelete();
		_pending.Folder = job.ModName;
		_pending.PackPath = job.PackDirectory;
		_pending.ProjectDirectory = job.Result.OutputDirectory;
		_confirmLayoutValid = false;
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
		// A conversion in progress owns the frame: it advances a few files and comes back next
		// frame, so the screen keeps drawing and the bar keeps moving.
		if (_job != null)
		{
			UpdateJob();
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
		// C writes a raw mod (no compiler needed), P a C# project (for adding behaviour).
		if (KeyboardMouseInput.KeyPressed(Keys.C) || KeyboardMouseInput.KeyPressed(Keys.Enter))
		{
			ConvertSelected(raw: true);
		}
		if (KeyboardMouseInput.KeyPressed(Keys.P))
		{
			ConvertSelected(raw: false);
		}
		if (KeyboardMouseInput.KeyPressed(Keys.R))
		{
			Rebuild();
			_status = Texts.Format("ui.mods.rescanned", ModsRootOverride ?? ModLoader.ModsDirectory);
			_statusTime = 180;
		}
		return false;
	}

	/// <summary>The only action a row has: a mod toggles, a pack converts (raw, the useful default).</summary>
	private static void Activate(int index)
	{
		if (index < 0 || index >= Entries.Count)
		{
			return;
		}
		if (Entries[index].Kind == EntryKind.Pack)
		{
			ConvertSelected(raw: true);
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
		// Gamepad: left/right picks a button, A presses it. The keyboard had Y/N and the pointer
		// had the two rectangles, but a controller had nothing - and this screen is reachable with
		// one, so the question could not be answered at all.
		if (KeyboardMouseInput.KeyPressed(Keys.Left) || PadPressed(Buttons.DPadLeft))
		{
			_confirmChoice = 0;
			Main.PlaySound(12);
		}
		else if (KeyboardMouseInput.KeyPressed(Keys.Right) || PadPressed(Buttons.DPadRight))
		{
			_confirmChoice = 1;
			Main.PlaySound(12);
		}
		if (PadPressed(Buttons.A))
		{
			Finish(_confirmChoice == 0);
			return false;
		}
		if (PadPressed(Buttons.B))
		{
			Finish(false);
			return false;
		}
		if (UI.PointerActive && _confirmLayoutValid)
		{
			int num = KeyboardMouseInput.MouseX;
			int num2 = KeyboardMouseInput.MouseY;
			// Hover moves the highlight, so the pointer and the controller agree about which
			// button is about to be pressed.
			if (_yesRect.Contains(num, num2))
			{
				_confirmChoice = 0;
			}
			else if (_noRect.Contains(num, num2))
			{
				_confirmChoice = 1;
			}
			if (KeyboardMouseInput.LeftClicked)
			{
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
		}
		return false;
	}

	/// <summary>0 = Delete, 1 = Keep. Keep is the default: it is the answer that destroys nothing.</summary>
	private static int _confirmChoice = 1;

	/// <summary>
	/// Edge-detected pad button, through the same UI instance the rest of the game uses.
	///
	/// Not a raw <c>GamePad.GetState</c> comparison: <see cref="UI.IsButtonTriggered"/> already
	/// handles the touchpad swap and the merged keyboard state, and a second edge detector here
	/// would disagree with it.
	/// </summary>
	private static bool PadPressed(Buttons button)
	{
		UI ui = UI.MainUI;
		return ui != null && ui.IsButtonTriggered(button);
	}

	/// <summary>
	/// Converts the selected pack. Raw by default: it needs no compiler, so it is the conversion
	/// that always works. The C# project is the option for someone who wants to add behaviour.
	/// </summary>
	private static void ConvertSelected(bool raw)
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
		string output = raw
			? Path.Combine(ModsRootOverride ?? Terraria.Storage.AppPaths.Mods, entry.Folder)
			: Path.Combine(ModSourcesRootOverride ?? Terraria.Storage.AppPaths.ModSources, entry.Folder);
		// Started here and advanced in Update: the conversion runs a few files per frame so the
		// progress bar can move and the screen stays alive.
		TConfigConverter.Job job = new TConfigConverter.Job();
		job.Raw = raw;
		string error;
		if (!job.Begin(entry.PackPath, output, entry.Folder, out error))
		{
			_status = Texts.Format("ui.mods.convert.failed", entry.Folder, error);
			_statusTime = 600;
			Main.PlaySound(14);
			return;
		}
		_job = job;
		Main.PlaySound(12);
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
		if (entry.Kind == EntryKind.Pack)
		{
			return;
		}
		string text = ModsRootOverride ?? ModLoader.ModsDirectory;
		string path = Path.Combine(text, entry.Folder, "disabled.txt");
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
		if (entry.Kind == EntryKind.Pack)
		{
			return;
		}
		List<string> list = new List<string>();
		for (int i = 0; i < Entries.Count; i++)
		{
			// Raw mods are folders in the same directory and are loaded by the same pass, so the
			// load order applies to them too - two packs that touch the same item need an order
			// just as much as two assemblies do.
			if (Entries[i].Kind != EntryKind.Pack)
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
			if (Entries[j].Kind != EntryKind.Pack && Entries[j].Folder == text)
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

	/// <summary>
	/// Moves the selection by <paramref name="steps"/> rows, for the wheel. Negative is down, the
	/// same sign the stick path uses, so the two cannot disagree about which way is which.
	/// </summary>
	internal static void Scroll(int steps)
	{
		if (steps == 0 || Entries.Count == 0)
		{
			return;
		}
		int num = Math.Max(0, Math.Min(Entries.Count - 1, _selected + steps));
		if (num == _selected)
		{
			return;
		}
		_selected = num;
		ClampScroll();
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
			string text2 = Texts.Get(entry.Kind == EntryKind.Pack ? "ui.mods.kind.tconfig"
				: ((entry.Kind == EntryKind.Raw) ? "ui.mods.kind.raw" : "ui.mods.kind.xdk"));
			Color color = (entry.Kind != EntryKind.Pack) ? (entry.Enabled ? new Color(120, 230, 140) : new Color(160, 160, 160))
				: new Color(230, 200, 130);
			UI.DrawStringLC(UI.SmallFont, text2, num2 + (int)(8f * num), y + (int)(9f * num), new Color(140, 150, 170));
			float num6 = UI.SmallFont.MeasureString(text2).X;
			// A mod shows its on/off state; a pack has no such state, so it shows its name plain.
			string text3 = (entry.Kind != EntryKind.Pack)
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
		if (_job != null)
		{
			DrawProgress(rectangle, num);
		}
	}

	/// <summary>
	/// The conversion progress bar. Drawn over the list, with the pack name, the file being
	/// converted and a filled bar: a conversion of a few hundred items takes seconds, and a screen
	/// that simply freezes for that long looks broken.
	/// </summary>
	private static void DrawProgress(Rectangle rows, float scale)
	{
		int width = (int)(420f * scale);
		int height = (int)(92f * scale);
		Rectangle rectangle = new Rectangle(rows.Center.X - width / 2, rows.Center.Y - height / 2, width, height);
		Main.DrawSolidRect(rectangle, new Color(20, 24, 34, 245));
		Main.DrawRect(451, rectangle, 255);
		UI.DrawStringCC(UI.SmallFont, Texts.Format("ui.mods.convert.running", _job.ModName),
			rectangle.Center.X, rectangle.Y + (int)(14f * scale), Color.White);
		// The bar itself: a track and a fill, so the fraction is visible even at zero.
		int num = (int)(12f * scale);
		Rectangle track = new Rectangle(rectangle.X + num, rectangle.Y + (int)(40f * scale),
			width - num * 2, (int)(12f * scale));
		Main.DrawSolidRect(track, new Color(45, 52, 68));
		int num2 = (int)((float)track.Width * _job.Progress);
		if (num2 > 0)
		{
			Main.DrawSolidRect(new Rectangle(track.X, track.Y, num2, track.Height), new Color(90, 160, 230));
		}
		string text = (_job.CurrentFile.Length == 0)
			? Texts.Get("ui.mods.convert.finishing")
			: Texts.Format("ui.mods.convert.file", _job.CurrentFile, _job.DoneFiles, _job.TotalFiles);
		UI.DrawStringCC(UI.SmallFont, text, rectangle.Center.X, track.Y + track.Height + (int)(10f * scale),
			new Color(150, 160, 180));
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
		// The chosen button is highlighted, so the pointer, the keys and the pad all show which
		// answer Enter or A will give.
		DrawButton(_yesRect, Texts.Get("ui.mods.convert.yes"),
			(_confirmChoice == 0) ? new Color(180, 90, 90) : new Color(120, 70, 70));
		DrawButton(_noRect, Texts.Get("ui.mods.convert.no"),
			(_confirmChoice == 1) ? new Color(70, 100, 150) : new Color(55, 70, 100));
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

	/// <summary>Test hook: which row the cursor is on.</summary>
	internal static int SelectedIndexForTests => _selected;

	/// <summary>Test hook: selects a row as if the player had.</summary>
	internal static void SelectForTests(int index)	{
		if (index < 0 || index >= Entries.Count)
		{
			return;
		}
		_selected = index;
		ClampScroll();
	}

	/// <summary>
	/// Test hook: runs the conversion action on the selected row, to completion.
	/// <paramref name="raw"/> picks the output kind, like the two keys do.
	/// </summary>
	internal static void ConvertSelectedForTests(bool raw = true)
	{
		ConvertSelected(raw);
		// The menu converts a few files per frame; a test has no frames, so it pumps instead.
		// The real code path is unchanged - this only drives it.
		while (_job != null)
		{
			UpdateJob();
		}
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
