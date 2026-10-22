using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Terraria.Mods;

/// <summary>
/// Turns a tConfig pack into a mod project.
///
/// A tConfig pack is a folder of INI files read at startup by <see cref="TConfigImporter"/>.
/// That is enough to change numbers, but it is not a mod: it has no name in the mod list, it
/// takes no part in the multiplayer signature, and a host cannot require it. Converting the pack
/// into a project gives exactly those three things, and gives the author a place to add code.
///
/// The output is a normal project, not a one-way export: build it, edit it, keep editing it.
/// </summary>
internal static class TConfigConverter
{
	internal sealed class Result
	{
		public string OutputDirectory;

		public string ProjectFile;

		public string SourceFile;

		public int Items;

		public int Fields;

		public int UnknownItems;

		public int UnsupportedFields;

		/// <summary>Files that could not be read or produced no usable output. Not fatal.</summary>
		public int FailedFiles;

		/// <summary>
		/// Items the pack defines from scratch (<c>type=-1</c>). These are converted through
		/// <c>Context.DefineItem</c>, not <c>Context.Item</c>: there is no existing item to tweak.
		/// </summary>
		public int NewItems;

		/// <summary>
		/// New items that could not be converted, almost always because the pack has no sprite for
		/// them. Reported separately from <see cref="NewItems"/> so "converted" means converted.
		/// </summary>
		public int NewItemsSkipped;

		/// <summary>Sprite files copied into the project, one per new item.</summary>
		public int SpritesCopied;

		/// <summary>Recipes emitted into the project.</summary>
		public int Recipes;

		/// <summary>Recipes the pack described but that could not be built (unknown station, no ingredients).</summary>
		public int RecipesSkipped;

		/// <summary>
		/// Content folders the pack has that this build cannot load at all: NPC, Tiles, Walls, Buffs,
		/// Projectiles. Reported because a pack with 146 NPCs that converts to "111 items, done" reads
		/// as if the NPCs came along - they did not, and they never could.
		/// </summary>
		public readonly List<Terraria.Mods.RawMod.IgnoredPart> Ignored = new List<Terraria.Mods.RawMod.IgnoredPart>();

		/// <summary>Total time spent converting files, in milliseconds. For the log and the report.</summary>
		public long ConvertMs;

		/// <summary>
		/// The single slowest file, in milliseconds, and its name. This is the number that answers
		/// "why did it hiccup": a budgeted loop can only be as smooth as its worst file.
		/// </summary>
		public long SlowestFileMs;

		public string SlowestFile = "";

		/// <summary>
		/// Gen-2 collections that happened while the slowest file was converting.
		///
		/// Zero means the pause really was that file. Anything else means the garbage collector
		/// stopped the world and the file was a bystander - and the two call for opposite fixes:
		/// "make the parser cheaper" versus "allocate less". A 39 ms file among 0.5 ms ones is
		/// almost always the second, and without this number the log points at the wrong file.
		/// </summary>
		public int SlowestFileGen2;

		/// <summary>
		/// Things the player should look at: dropped fields, unknown names, and files that threw.
		/// Kept apart from <see cref="Errors"/> by severity, not by source.
		/// </summary>
		public readonly List<string> Warnings = new List<string>();

		/// <summary>
		/// Failures that were survived. A pack with one broken file still converts: refusing the
		/// whole pack because of one typo is worse than converting the rest and saying what was
		/// skipped - the player would have no way to find the typo from "it did not work".
		/// </summary>
		public readonly List<string> Errors = new List<string>();

		/// <summary>Path of the log written for this run, or empty when logging failed too.</summary>
		public string LogFile = string.Empty;

		/// <summary>
		/// True when the output is a raw mod (INI + PNG, no assembly) rather than a C# project.
		/// Both are "the pack, converted" - the difference is whether the result needs a compiler.
		/// </summary>
		public bool IsRaw;

		/// <summary>The written <c>mod.ini</c> of a raw mod; empty for a project.</summary>
		public string ManifestFile = string.Empty;

		public bool HasProblems => Errors.Count > 0 || Warnings.Count > 0;
	}

	/// <summary>
	/// Where a conversion writes its log. One file per run is not enough to debug an old pack, so
	/// the file is appended to and rotated at a size limit - the same rule the game log follows.
	/// </summary>
	internal static string LogPath => LogPathOverride ?? Path.Combine(Terraria.Storage.AppPaths.Logs, "tconfig-convert.log");

	/// <summary>Test hook: keeps a self-test from writing into the player's own log.</summary>
	internal static string LogPathOverride;

	private const long MaxLogBytes = 1024 * 1024;

	/// <summary>
	/// Reads <paramref name="packDirectory"/> and writes a project into
	/// <paramref name="outputDirectory"/>. Returns null when the pack has nothing to convert;
	/// the reason is in <paramref name="error"/>.
	///
	/// Never throws. A pack is somebody else's data: a truncated INI, a name that is not an item
	/// in this build, a file that is locked by an editor - each of those is a thing to report,
	/// not a reason to bring the menu down. Everything that goes wrong lands in
	/// <see cref="Result.Errors"/>, in the generated README, and in a log file.
	/// </summary>
	internal static Result Convert(string packDirectory, string outputDirectory, string modName, out string error)
	{
		return Convert(packDirectory, outputDirectory, modName, raw: false, out error);
	}

	/// <summary>
	/// The same conversion, with the output chosen: <paramref name="raw"/> writes a mod folder the
	/// game can load as it is, otherwise a C# project.
	/// </summary>
	internal static Result Convert(string packDirectory, string outputDirectory, string modName, bool raw, out string error)
	{
		error = null;
		Job job = new Job();
		job.Raw = raw;
		try
		{
			if (!job.Begin(packDirectory, outputDirectory, modName, out error))
			{
				WriteLog(packDirectory, job.Result, error);
				return null;
			}
			// All at once: the caller is a command line or a test, and nobody is watching a bar.
			while (!job.Done)
			{
				job.Step(int.MaxValue);
			}
			if (!job.Finish(out error))
			{
				WriteLog(packDirectory, job.Result, error);
				return null;
			}
			return job.Result;
		}
		catch (Exception ex)
		{
			// The last line of defence. Everything below is wrapped per file, so reaching here
			// means something unexpected - and it is still better to say so than to crash.
			error = "unexpected failure: " + ex.GetType().Name + ": " + ex.Message;
			job.Result.Errors.Add(error);
			WriteLog(packDirectory, job.Result, error);
			return null;
		}
	}

	/// <summary>
	/// A conversion in progress: one pack, one file at a time.
	///
	/// The split exists so the menu can show a progress bar without a background thread. Running
	/// the converter on another thread would be simpler, but it calls <c>Item.SetDefaults</c> and
	/// reads the shared language tables - and "simple until it races with the game loop" is not
	/// simple. A file takes well under a frame, so a few per frame keeps the screen alive.
	/// </summary>
	internal sealed class Job
	{
		internal readonly Result Result = new Result();

		internal string PackDirectory;

		internal string OutputDirectory;

		internal string ModName;

		/// <summary>
		/// Write a raw mod (INI + PNG) instead of a C# project. Set before <see cref="Begin"/>.
		///
		/// Same reader, same report, same log: the only difference is the output. A raw mod is
		/// what a player can actually use without a .NET SDK on the machine, so this is the
		/// default in the menu and the project is the option.
		/// </summary>
		internal bool Raw;

		private readonly List<string> _files = new List<string>();

		private readonly StringBuilder _body = new StringBuilder();

		private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);

		private int _index;

		/// <summary>Files converted so far.</summary>
		internal int DoneFiles => _index;

		/// <summary>Total files in the pack.</summary>
		internal int TotalFiles => _files.Count;

		/// <summary>Name of the file being converted, for the progress line.</summary>
		internal string CurrentFile { get; private set; } = "";

		/// <summary>True when every file has been processed.</summary>
		internal bool Done => _index >= _files.Count;

		/// <summary>0..1, for a progress bar.</summary>
		internal float Progress => (_files.Count == 0) ? 0f : ((float)_index / (float)_files.Count);

		/// <summary>Validates the pack and lists its files. False with a reason when unusable.</summary>
		internal bool Begin(string packDirectory, string outputDirectory, string modName, out string error)
		{
			error = null;
			PackDirectory = packDirectory;
			OutputDirectory = outputDirectory;
			if (string.IsNullOrEmpty(packDirectory) || !Directory.Exists(packDirectory))
			{
				error = "no such pack folder: " + packDirectory;
				return false;
			}
			if (string.IsNullOrEmpty(modName))
			{
				// The folder name is the pack's name everywhere else, so it is the honest default.
				modName = new DirectoryInfo(Path.GetFullPath(packDirectory)).Name;
			}
			if (Raw)
			{
				// A raw mod's name becomes a folder under Storage/Mods and the name the menu
				// shows, so it stays readable ("Omnirs Weapons"). A C# project needs an
				// identifier instead, which is a different requirement.
				modName = FolderName(modName);
			}
			else
			{
				modName = Identifier(modName, upperFirst: true);
			}
			if (modName.Length == 0)
			{
				error = "the pack folder name has no usable characters for a mod name";
				return false;
			}
			ModName = modName;
			string text = Path.Combine(packDirectory, "Item");
			if (Directory.Exists(text))
			{
				try
				{
					string[] files = Directory.GetFiles(text, "*.ini", SearchOption.TopDirectoryOnly);
					Array.Sort(files, StringComparer.OrdinalIgnoreCase);
					_files.AddRange(files);
				}
				catch (Exception ex)
				{
					error = "cannot list " + text + ": " + ex.Message;
					Result.Errors.Add(error);
					return false;
				}
			}
			if (_files.Count == 0)
			{
				error = "no Item/*.ini files in " + packDirectory;
				return false;
			}
			// What the pack has and this build cannot take. Counted once, here, so the report and
			// the README describe the same pack the loader would see.
			Result.Ignored.AddRange(Terraria.Mods.RawMod.ScanIgnored(packDirectory));
			if (Raw)
			{
				// Clear the previous conversion before this one writes: a pack that dropped an item
				// must not keep the old file, or the mod would still define it. Only inside a folder
				// this tool generated - see CleanStaleItems.
				try
				{
					Directory.CreateDirectory(outputDirectory);
					// Create Item/ here rather than on the first sprite copy: the first write into a
					// new folder is the single slowest thing in a whole conversion (20-37 ms on this
					// machine, measured, with no GC involved - it is the filesystem and the virus
					// scanner meeting a new directory). Doing it once up front costs the same and
					// keeps the budgeted loop honest, instead of charging it to one item's frame.
					Directory.CreateDirectory(Path.Combine(outputDirectory, "Item"));
				}
				catch (Exception ex2)
				{
					error = "cannot create " + outputDirectory + ": " + ex2.Message;
					Result.Errors.Add(error);
					return false;
				}
				CleanStaleItems(outputDirectory);
			}
			return true;
		}

		/// <summary>
		/// Converts files until either <paramref name="maxFiles"/> is reached or the frame's time
		/// budget is spent. Returns how many it did.
		///
		/// Time, not just a count: a pack is a pile of other people's files, and one of them can be
		/// four hundred lines while the next is eight. A fixed "four files per frame" therefore
		/// stalls on the big ones and wastes the frame on the small ones - which is what "it
		/// hiccups" is. The count stays as a hard ceiling so a pathological run of tiny files
		/// cannot spin the whole frame away either.
		/// </summary>
		internal int Step(int maxFiles)
		{
			int num = 0;
			System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
			while (_index < _files.Count && num < maxFiles)
			{
				string text = _files[_index];
				CurrentFile = Path.GetFileNameWithoutExtension(text);
				long num2 = stopwatch.ElapsedMilliseconds;
				int num4 = GC.CollectionCount(2);
				try
				{
					if (Raw)
					{
						WriteItemRaw(text, PackDirectory, OutputDirectory, Result);
					}
					else
					{
						AppendItem(_body, text, PackDirectory, OutputDirectory, _used, Result);
					}
				}
				catch (Exception ex)
				{
					// One file, one failure, and the rest of the pack still converts.
					Result.FailedFiles++;
					Result.Errors.Add(Path.GetFileName(text) + ": " + ex.GetType().Name + ": " + ex.Message);
				}
				_index++;
				num++;
				long num3 = stopwatch.ElapsedMilliseconds - num2;
				if (num3 > Result.SlowestFileMs)
				{
					Result.SlowestFileMs = num3;
					Result.SlowestFile = Path.GetFileName(text);
					Result.SlowestFileGen2 = GC.CollectionCount(2) - num4;
				}
				// The budget is checked after at least one file, so progress always happens: a file
				// that alone exceeds the budget still moves the bar instead of freezing it.
				if (stopwatch.ElapsedMilliseconds >= FrameBudgetMs)
				{
					break;
				}
			}
			Result.ConvertMs += stopwatch.ElapsedMilliseconds;
			if (Done)
			{
				CurrentFile = "";
			}
			return num;
		}

		/// <summary>
		/// How long one frame may spend converting. 4 ms is a quarter of a 60 Hz frame, which leaves
		/// the rest of the update and the draw room to run - the screen keeps moving at full rate.
		/// </summary>
		internal const int FrameBudgetMs = 4;

		/// <summary>Writes the project. False with a reason when there is nothing to write.</summary>
		internal bool Finish(out string error)
		{
			error = null;
			if (Result.Items == 0)
			{
				if (Result.NewItemsSkipped > 0)
				{
					error = "this pack defines " + Result.NewItemsSkipped.ToString(CultureInfo.InvariantCulture)
						+ " new item(s) but ships no sprite for them, and an item that cannot be drawn is not converted";
				}
				else
				{
					error = "the pack names no item this build knows ("
						+ Result.UnknownItems.ToString(CultureInfo.InvariantCulture) + " unknown"
						+ (Result.FailedFiles == 0 ? "" : ", " + Result.FailedFiles.ToString(CultureInfo.InvariantCulture) + " failed")
						+ ")";
				}
				return false;
			}
			string text = Path.Combine(OutputDirectory, ModName + ".cs");
			string text2 = Path.Combine(OutputDirectory, ModName + ".csproj");
			string text3 = Path.Combine(OutputDirectory, "README.md");
			if (Raw)
			{
				// A raw mod: the folder the game loads, with the manifest and a report. No
				// project, no compiler - that is the whole point of this mode.
				try
				{
					Directory.CreateDirectory(OutputDirectory);
					File.WriteAllText(Path.Combine(OutputDirectory, "mod.ini"),
						Manifest(ModName, PackDirectory), new UTF8Encoding(false));
					File.WriteAllText(text3, RawReadme(ModName, PackDirectory, Result), new UTF8Encoding(false));
				}
				catch (Exception ex)
				{
					error = "cannot write to " + OutputDirectory + ": " + ex.Message;
					Result.Errors.Add(error);
					return false;
				}
				Result.OutputDirectory = OutputDirectory;
				Result.ManifestFile = Path.Combine(OutputDirectory, "mod.ini");
				Result.IsRaw = true;
				Result.LogFile = WriteLog(PackDirectory, Result, null);
				return true;
			}
			try
			{
				Directory.CreateDirectory(OutputDirectory);
				File.WriteAllText(text, Source(ModName, PackDirectory, _body.ToString()), new UTF8Encoding(false));
				File.WriteAllText(text2, Project(ModName, GameAssemblyPath()), new UTF8Encoding(false));
				File.WriteAllText(text3, Readme(ModName, PackDirectory, Result), new UTF8Encoding(false));
			}
			catch (Exception ex)
			{
				error = "cannot write to " + OutputDirectory + ": " + ex.Message;
				Result.Errors.Add(error);
				return false;
			}
			Result.OutputDirectory = OutputDirectory;
			Result.ProjectFile = text2;
			Result.SourceFile = text;
			Result.LogFile = WriteLog(PackDirectory, Result, null);
			return true;
		}
	}



	/// <summary>
	/// Appends one run to the conversion log and returns its path (empty when even that failed).
	///
	/// The log is the answer to "the converter said something was wrong, what exactly": the status
	/// line in the menu has room for one sentence, and a pack with thirty dropped fields needs
	/// thirty lines. Failing to write the log must not fail the conversion - the project is
	/// already written and more useful than a log.
	/// </summary>
	private static string WriteLog(string packDirectory, Result result, string fatal)
	{
		try
		{
			string text = LogPath;
			string directoryName = Path.GetDirectoryName(text);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			// Rotate rather than grow without bound: a player converting repeatedly would
			// otherwise accumulate a file nobody reads.
			try
			{
				FileInfo fileInfo = new FileInfo(text);
				if (fileInfo.Exists && fileInfo.Length > MaxLogBytes)
				{
					string text2 = text + ".1";
					if (File.Exists(text2))
					{
						File.Delete(text2);
					}
					File.Move(text, text2);
				}
			}
			catch
			{
				// Rotation is housekeeping; if it fails, keep appending.
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("=== ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
				.Append(" ===\n");
			stringBuilder.Append("pack: ").Append(packDirectory ?? "(none)").Append('\n');
			if (fatal != null)
			{
				stringBuilder.Append("failed: ").Append(fatal).Append('\n');
			}
			else
			{
				stringBuilder.Append("result: ").Append(result.Items.ToString(CultureInfo.InvariantCulture)).Append(" item(s), ")
					.Append(result.Fields.ToString(CultureInfo.InvariantCulture)).Append(" field(s), ")
					.Append(result.UnknownItems.ToString(CultureInfo.InvariantCulture)).Append(" unknown, ")
					.Append(result.UnsupportedFields.ToString(CultureInfo.InvariantCulture)).Append(" dropped, ")
					.Append(result.FailedFiles.ToString(CultureInfo.InvariantCulture)).Append(" failed file(s)\n");
				stringBuilder.Append("output: ").Append(result.OutputDirectory ?? "(not written)").Append('\n');
				// The timing answers "why did it stutter": the loop is budgeted per frame, so the
				// only thing that can still stall one is a single slow file.
				stringBuilder.Append("time: ").Append(result.ConvertMs.ToString(CultureInfo.InvariantCulture))
					.Append(" ms total, slowest file ").Append(result.SlowestFileMs.ToString(CultureInfo.InvariantCulture))
					.Append(" ms");
				if (result.SlowestFile.Length > 0)
				{
					stringBuilder.Append(" ('").Append(result.SlowestFile).Append("')");
				}
				stringBuilder.Append(", gen2 collections during it: ")
					.Append(result.SlowestFileGen2.ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append('\n');
			}
			if (result.Errors.Count > 0)
			{
				stringBuilder.Append("errors:\n");
				for (int i = 0; i < result.Errors.Count; i++)
				{
					stringBuilder.Append("  ! ").Append(result.Errors[i]).Append('\n');
				}
			}
			if (result.Warnings.Count > 0)
			{
				stringBuilder.Append("warnings:\n");
				for (int j = 0; j < result.Warnings.Count; j++)
				{
					stringBuilder.Append("  - ").Append(result.Warnings[j]).Append('\n');
				}
			}
			stringBuilder.Append('\n');
			File.AppendAllText(text, stringBuilder.ToString(), new UTF8Encoding(false));
			return text;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static void AppendItem(StringBuilder body, string iniPath, string packDirectory, string outputDirectory, HashSet<string> used, Result result)
	{
		string text = Path.GetFileNameWithoutExtension(iniPath);
		Item item = default(Item);
		// A pack-wide table in Item/: say so, instead of "no such item in this build".
		string serviceReason = Terraria.Mods.RawMod.ServiceFileReason(text);
		if (serviceReason != null)
		{
			result.UnknownItems++;
			result.Warnings.Add("'" + text + "': " + serviceReason + " - not converted");
			return;
		}
		// SetDefaults is not a lookup table: for a few items it reads the active player (the
		// familiar clothes take their colour from it), and it can throw when there is no UI yet.
		// That is a bug in those items, but it must not take a whole pack down with it.
		try
		{
			item.SetDefaults(Terraria.ItemLookup.Find(text));
		}
		catch (Exception ex)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': cannot create the item (" + ex.GetType().Name + ": " + ex.Message + ")");
			return;
		}
		if (item.Type <= 0)
		{
			// "Unknown item" and "this pack adds a new item" are different facts, and now they
			// lead to different code: one is a mistake in the pack, the other is a feature.
			if (DefinesNewItem(iniPath))
			{
				AppendNewItem(body, iniPath, packDirectory, outputDirectory, used, result);
				return;
			}
			result.UnknownItems++;
			result.Warnings.Add("'" + text + "': no such item in this build, skipped");
			return;
		}
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		string error;
		List<TConfigImporter.SectionLine> lines = TConfigImporter.ReadIniSections(iniPath, out error);
		if (lines == null)
		{
			// A file the player asked to convert that could not be read is a failure to report,
			// not a file with nothing in it.
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': " + Path.GetFileName(iniPath) + ": " + error);
			return;
		}
		try
		{
			for (int i = 0; i < lines.Count; i++)
			{
				TConfigImporter.SectionLine sectionLine = lines[i];
				// Recipe sections are not item fields: they describe how the item is made. Without
			// this check every ingredient was reported as "no equivalent field", which is what
			// produced 450 warnings on one pack and hid the real ones.
				if (IsRecipeSection(sectionLine.Section))
				{
					continue;
				}
				KeyValuePair<string, string> line = new KeyValuePair<string, string>(sectionLine.Key, sectionLine.Value);
				// PropertyName is the second half of the check: ItemOverrides can accept a field
				// that the mod API has no property for (consumable is recorded as an error there),
				// and emitting a property that does not exist would produce a project that cannot
				// build.
				if (!ItemOverrides.IsSupportedField(line.Key)
					|| (PropertyName(line.Key) == null && !IsEscapedField(line.Key)))
				{
					result.UnsupportedFields++;
					result.Warnings.Add("'" + text + "': field '" + line.Key + "' has no equivalent, dropped");
					continue;
				}
				// A value that cannot become a C# literal is quoted by Literal() and would make the
				// generated project fail to compile at that line. Catching it here is better: the
				// project builds, and the player is told which field was nonsense.
				string valueProblem = FieldProblem(line.Key, line.Value);
				if (valueProblem != null)
				{
					result.UnsupportedFields++;
					result.Warnings.Add("'" + text + "': field '" + line.Key + "' has the value '"
						+ line.Value + "', which " + valueProblem + " - dropped");
					continue;
				}
				list.Add(line);
			}
		}
		catch (Exception ex2)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': cannot read " + Path.GetFileName(iniPath)
				+ " (" + ex2.GetType().Name + ": " + ex2.Message + ")");
			return;
		}
		if (list.Count > 0)
		{
			string text2 = UniqueIdentifier(Identifier(text, upperFirst: false), used);
			body.Append("\t\t// ").Append(text).Append('\n');
			body.Append("\t\tItemOverride ").Append(text2).Append(" = Context.Item(").Append(Quote(text)).Append(");\n");
			body.Append("\t\tif (").Append(text2).Append(" != null)\n");
			body.Append("\t\t{\n");
			foreach (KeyValuePair<string, string> item2 in list)
			{
				if (IsEscapedField(item2.Key))
				{
					// No property exists for this one; the escape hatch takes the raw value and the
					// field table interprets it, so both output kinds keep the field.
					body.Append("\t\t\t").Append(text2).Append(".Set(").Append(Quote(item2.Key.ToLowerInvariant()))
						.Append(", ").Append(Quote(item2.Value)).Append(");\n");
					result.Fields++;
					continue;
				}
				body.Append("\t\t\t").Append(text2).Append('.').Append(PropertyName(item2.Key))
					.Append(" = ").Append(Literal(item2.Key, item2.Value)).Append(";\n");
				result.Fields++;
			}
			body.Append("\t\t}\n\n");
			result.Items++;
		}
		else
		{
			result.Warnings.Add("'" + text + "': nothing to convert");
		}
		AppendRecipe(body, iniPath, text, item, null, used, result);
	}

	/// <summary>
	/// True for the sections a tConfig item file uses to describe its recipe.
	///
	/// A pack writes the recipe in the item's own file: <c>[Items]</c> for ingredients,
	/// <c>[Tiles]</c> for the station, <c>[Amount]</c> for the output count and
	/// <c>[needWater]</c> for the water requirement. They are not fields of the item and must not
	/// be treated as such.
	/// </summary>
	internal static bool IsRecipeSection(string section)
	{
		if (string.IsNullOrEmpty(section))
		{
			return false;
		}
		return section.Equals("Items", StringComparison.OrdinalIgnoreCase)
			|| section.Equals("Tiles", StringComparison.OrdinalIgnoreCase)
			|| section.Equals("Amount", StringComparison.OrdinalIgnoreCase)
			|| section.Equals("needWater", StringComparison.OrdinalIgnoreCase)
			|| section.Equals("Recipe", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Emits a <c>Context.AddRecipe</c> block when the pack describes one.
	///
	/// Every ingredient and station is checked here rather than at load time: a project that
	/// compiles and then silently skips its recipe is the failure mode this avoids. An unknown
	/// ingredient is reported by name, which is the only way the author can fix it.
	/// </summary>
	private static void AppendRecipe(StringBuilder body, string iniPath, string itemName, Item item,
		Dictionary<string, string> fields, HashSet<string> used, Result result)
	{
		string error;
		List<TConfigImporter.SectionLine> lines = TConfigImporter.ReadIniSections(iniPath, out error);
		if (lines == null)
		{
			return;
		}
		List<KeyValuePair<string, int>> ingredients;
		List<string> stations;
		int amount;
		bool needsWater;
		if (!ReadRecipe(lines, itemName, result, out ingredients, out stations, out amount, out needsWater))
		{
			return;
		}
		string text = UniqueIdentifier(Identifier("recipe " + itemName, upperFirst: false), used);
		body.Append("\t\t// ").Append(itemName).Append(": recipe\n");
		body.Append("\t\tModRecipe ").Append(text).Append(" = Context.AddRecipe(").Append(Quote(itemName)).Append(");\n");
		body.Append("\t\tif (").Append(text).Append(" != null)\n");
		body.Append("\t\t{\n");
		body.Append("\t\t\t").Append(text).Append(".Amount = ").Append(amount.ToString(CultureInfo.InvariantCulture)).Append(";\n");
		string text2 = CategoryFor(item, fields);
		if (text2 != null)
		{
			body.Append("\t\t\t").Append(text).Append(".Category = Terraria.Recipe.Category.").Append(text2).Append(";\n");
		}
		if (needsWater)
		{
			body.Append("\t\t\t").Append(text).Append(".NeedsWater = true;\n");
		}
		for (int j = 0; j < ingredients.Count; j++)
		{
			body.Append("\t\t\t").Append(text).Append(".AddIngredient(").Append(Quote(ingredients[j].Key))
				.Append(", ").Append(ingredients[j].Value.ToString(CultureInfo.InvariantCulture)).Append(");\n");
		}
		for (int k = 0; k < stations.Count; k++)
		{
			body.Append("\t\t\t").Append(text).Append(".AddStation(").Append(Quote(stations[k])).Append(");\n");
		}
		body.Append("\t\t}\n\n");
		result.Recipes++;
	}

	/// <summary>
	/// Reads the recipe a pack file describes, in either spelling it may use.
	///
	/// One reader for both outputs: the project generator and the raw-mod writer must agree
	/// about what a pack says, or "convert" and "convert to raw" would produce different mods
	/// from the same folder.
	///
	/// Returns false when there is nothing usable - no ingredients, or more than a recipe holds.
	/// Stations this build has no tile for are dropped with a warning rather than failing: an
	/// item that exists and cannot be made is worse than one made without a bench.
	/// </summary>
	internal static bool ReadRecipe(List<TConfigImporter.SectionLine> lines, string itemName, Result result,
		out List<KeyValuePair<string, int>> Ingredients, out List<string> Stations, out int Amount, out bool NeedsWater)
	{
		Ingredients = new List<KeyValuePair<string, int>>();
		Stations = new List<string>();
		Amount = 1;
		NeedsWater = false;
		// A count that does not fit an item's Stack makes the whole recipe a lie: dropping just the
		// ingredient would make it cheaper than the pack says, and keeping it would make it free.
		bool badCount = false;
		for (int i = 0; i < lines.Count; i++)
		{
			TConfigImporter.SectionLine sectionLine = lines[i];
			if (sectionLine.Section.Equals("Recipe", StringComparison.OrdinalIgnoreCase))
			{
				// The newer tConfig spelling: the whole recipe on three lines, with comma-separated
				// lists. Without this branch those items converted into a mod that could never
				// craft them - the file was read, the sections were skipped as "not item fields",
				// and nothing said the recipe had been dropped.
				if (sectionLine.Key.Equals("Items", StringComparison.OrdinalIgnoreCase))
				{
					List<KeyValuePair<string, int>> parsed = ParseCountedList(sectionLine.Value, itemName, result);
					if (parsed == null)
					{
						badCount = true;
						continue;
					}
					for (int j = 0; j < parsed.Count; j++)
					{
						Ingredients.Add(parsed[j]);
					}
				}
				else if (sectionLine.Key.Equals("Tiles", StringComparison.OrdinalIgnoreCase))
				{
					AddStations(Stations, sectionLine.Value, itemName, result);
				}
				else if (sectionLine.Key.Equals("Amount", StringComparison.OrdinalIgnoreCase))
				{
					int parsedAmount;
					if (int.TryParse(sectionLine.Value, out parsedAmount) && parsedAmount > 0)
					{
						if (!CountFits(itemName, "amount", parsedAmount, result))
						{
							badCount = true;
							continue;
						}
						Amount = parsedAmount;
					}
				}
				else if (sectionLine.Key.Equals("needWater", StringComparison.OrdinalIgnoreCase))
				{
					NeedsWater = IsTrue(sectionLine.Value);
				}
				continue;
			}
			if (sectionLine.Section.Equals("Items", StringComparison.OrdinalIgnoreCase))
			{
				int num;
				if (!int.TryParse(sectionLine.Value, out num) || num <= 0)
				{
					result.RecipesSkipped++;
					result.Warnings.Add("'" + itemName + "': ingredient '" + sectionLine.Key
						+ "' has the count '" + sectionLine.Value + "', which is not a number");
					continue;
				}
				Item ingredient = default(Item);
				if (!CountFits(itemName, "ingredient '" + sectionLine.Key + "' count", num, result))
				{
					badCount = true;
					continue;
				}
				// By name through the real table; SetDefaults(string) knows only the legacy items.
				int ingredientType = Terraria.ItemLookup.Find(sectionLine.Key);
				if (ingredientType > 0)
				{
					ingredient.SetDefaults(ingredientType);
				}
				if (ingredient.Type <= 0)
				{
					// The ingredient may be another item this same pack defines; those exist in
					// the generated mod but not in this build, so it cannot be verified here. The
					// generated code checks it at load time and logs.
					result.Warnings.Add("'" + itemName + "': ingredient '" + sectionLine.Key
						+ "' is not an item in this build - kept in the recipe, verified when the mod loads");
				}
				Ingredients.Add(new KeyValuePair<string, int>(sectionLine.Key, num));
			}
			else if (sectionLine.Section.Equals("Tiles", StringComparison.OrdinalIgnoreCase))
			{
				if (!IsTrue(sectionLine.Value))
				{
					continue;
				}
				if (Terraria.Mods.ModRecipes.StationId(sectionLine.Key) < 0)
				{
					// The station is usually a tile the pack adds itself, and this build has no mod
					// tiles. Dropping the whole recipe would leave the item unobtainable, which is
					// worse than dropping the station - so the station goes and the caveat stays.
					result.Warnings.Add("'" + itemName + "': station '" + sectionLine.Key
						+ "' is a mod tile this build does not have - the recipe is converted without it");
					continue;
				}
				Stations.Add(sectionLine.Key);
			}
			else if (sectionLine.Section.Equals("Amount", StringComparison.OrdinalIgnoreCase))
			{
				int num2;
				if (int.TryParse(sectionLine.Value, out num2) && num2 > 0)
				{
					if (!CountFits(itemName, "amount", num2, result))
					{
						badCount = true;
						continue;
					}
					Amount = num2;
				}
			}
			else if (sectionLine.Section.Equals("needWater", StringComparison.OrdinalIgnoreCase))
			{
				NeedsWater = IsTrue(sectionLine.Value);
			}
		}
		if (badCount)
		{
			return false;
		}
		if (Ingredients.Count == 0)
		{
			return false;
		}
		if (Ingredients.Count > 12)
		{
			result.RecipesSkipped++;
			result.Warnings.Add("'" + itemName + "': " + Ingredients.Count
				+ " ingredients, but a recipe holds at most 12 - recipe not converted");
			return false;
		}
		return true;
	}

	/// <summary>
	/// A recipe count has to fit an item's <c>Stack</c>, which is a <c>short</c>.
	///
	/// Above that it wraps negative, and a negative count is not "impossible to craft": it is the
	/// opposite. <c>CanCraftRecipe</c> subtracts what the player has and finds the remainder
	/// already satisfied, and <c>Recipe.Create</c> computes <c>Stack &gt; count</c> on every slot
	/// and then *adds* the difference. So one typo in one line of a pack becomes free crafting and
	/// item duplication. Checked here as well as in <c>ModRecipes.Apply</c> because the two answer
	/// different people: this one tells the pack author at conversion time, that one protects a
	/// hand-written or compiled mod at load time.
	/// </summary>
	private static bool CountFits(string itemName, string what, int count, Result result)
	{
		if (count > 0 && count <= short.MaxValue)
		{
			return true;
		}
		result.RecipesSkipped++;
		result.Warnings.Add("'" + itemName + "': " + what + " is " + count
			+ ", which does not fit an item stack (1.." + short.MaxValue + ") - recipe dropped");
		return false;
	}

	/// <summary>
	/// Parses tConfig's newer ingredient list: <c>6 Copper Bar, 2 Wood</c>. The count comes first
	/// and the name is everything after the first space, so an item whose name contains a space
	/// still works.
	///
	/// Returns <c>null</c> when a count cannot be stored in an item stack: the caller then drops the
	/// whole recipe, because a list with one ingredient quietly missing is a recipe that lies about
	/// its cost.
	/// </summary>
	private static List<KeyValuePair<string, int>> ParseCountedList(string value, string itemName, Result result)
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
		if (string.IsNullOrEmpty(value))
		{
			return list;
		}
		string[] array = value.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0)
			{
				continue;
			}
			int num = text.IndexOf(' ');
			int count;
			if (num <= 0 || !int.TryParse(text.Substring(0, num).Trim(), out count) || count <= 0)
			{
				result.RecipesSkipped++;
				result.Warnings.Add("'" + itemName + "': ingredient '" + text + "' has no count, dropped");
				continue;
			}
			if (!CountFits(itemName, "ingredient '" + text.Substring(num + 1).Trim() + "' count", count, result))
			{
				// One bad count makes the whole list wrong, so the caller drops the recipe. A
				// partial list would quietly make the item cheaper than the pack says.
				return null;
			}
			list.Add(new KeyValuePair<string, int>(text.Substring(num + 1).Trim(), count));
		}
		return list;
	}

	/// <summary>
	/// Adds station names from a comma-separated list, keeping only the ones this build has a
	/// tile for. A station it does not have is reported and dropped, not a reason to lose the
	/// recipe: an item that exists and cannot be made is worse than one made without a bench.
	/// </summary>
	private static void AddStations(List<string> stations, string value, string itemName, Result result)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		string[] array = value.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0 || text.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| text.Equals("false", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (Terraria.Mods.ModRecipes.StationId(text) < 0)
			{
				result.Warnings.Add("'" + itemName + "': station '" + text
					+ "' is a mod tile this build does not have - the recipe is converted without it");
				continue;
			}
			stations.Add(text);
		}
	}

	/// <summary>
	/// Which crafting tab the recipe belongs in.
	///
	/// A guess, and an honest one: tConfig does not say, and the categories are the game's own
	/// UI grouping rather than anything mechanical. Getting it wrong puts the recipe in the wrong
	/// tab, which is a nuisance, not a bug - so the cheap inference from the item's own flags is
	/// good enough, and the author can edit one line.
	/// </summary>
	private static string CategoryFor(Item item, Dictionary<string, string> fields)
	{
		// The parsed fields win over the live item: for a new item the live object is a blank
		// template (its stats exist only in the file), and a blank item's slots read as 0, which
		// put every new sword in the ARMOR tab.
		int num;
		if (ReadInt(fields, "damage", item.Damage, out num) && num > 0)
		{
			return "WEAPONS";
		}
		if (ReadBool(fields, "melee", item.IsMelee) || ReadBool(fields, "magic", item.IsMagic)
			|| ReadBool(fields, "ranged", item.IsRanged))
		{
			return "WEAPONS";
		}
		if (ReadInt(fields, "defense", item.Defense, out num) && num > 0)
		{
			return "ARMOR";
		}
		if (ReadBool(fields, "potion", item.IsPotion))
		{
			return "POTIONS";
		}
		if (ReadInt(fields, "pick", item.PickPower, out num) && num > 0)
		{
			return "TOOLS";
		}
		if (ReadInt(fields, "axe", item.AxePower, out num) && num > 0)
		{
			return "TOOLS";
		}
		if (ReadInt(fields, "hammer", item.HammerPower, out num) && num > 0)
		{
			return "TOOLS";
		}
		// Only reachable for an existing item with no fields of its own: fall back to the live
		// object's slots.
		if (fields == null || fields.Count == 0)
		{
			if (item.HeadSlot >= 0 || item.BodySlot >= 0 || item.LegSlot >= 0)
			{
				return "ARMOR";
			}
		}
		return "MISC";
	}

	private static bool ReadInt(Dictionary<string, string> fields, string name, int fallback, out int value)
	{
		value = fallback;
		if (fields == null)
		{
			return false;
		}
		string text;
		if (!fields.TryGetValue(name, out text))
		{
			return false;
		}
		int num;
		if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
		{
			return false;
		}
		value = num;
		return true;
	}

	private static bool ReadBool(Dictionary<string, string> fields, string name, bool fallback)
	{
		if (fields == null)
		{
			return fallback;
		}
		string text;
		if (!fields.TryGetValue(name, out text))
		{
			return fallback;
		}
		bool result;
		return bool.TryParse(text, out result) ? result : fallback;
	}

	/// <summary>tConfig writes booleans as True/False; a bare flag means true.</summary>
	private static bool IsTrue(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		string text = value.Trim();
		if (text.Length == 0)
		{
			return true;
		}
		bool result;
		if (bool.TryParse(text, out result))
		{
			return result;
		}
		return text == "1";
	}

	/// <summary>
	/// Emits a <c>Context.DefineItem</c> block for a new item and copies its sprite into the
	/// project.
	///
	/// The sprite is required: the item cannot be drawn without one, and an invisible item in the
	/// inventory is indistinguishable from a bug. A pack that defines an item but ships no PNG is
	/// reported rather than converted into something broken.
	/// </summary>
	private static void AppendNewItem(StringBuilder body, string iniPath, string packDirectory,
		string outputDirectory, HashSet<string> used, Result result)
	{
		string text = Path.GetFileNameWithoutExtension(iniPath);
		string spriteSource = Path.ChangeExtension(iniPath, ".png");
		if (!File.Exists(spriteSource))
		{
			result.NewItemsSkipped++;
			result.Warnings.Add("'" + text + "': new item with no sprite (" + Path.GetFileName(spriteSource)
				+ ") - an item that cannot be drawn would look like a bug, so it is not converted");
			return;
		}
		string error;
		List<TConfigImporter.SectionLine> lines = TConfigImporter.ReadIniSections(iniPath, out error);
		if (lines == null)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': " + Path.GetFileName(iniPath) + ": " + error);
			return;
		}
		// Copy the sprite next to the generated source, so the project is self-contained and does
		// not depend on the tConfig folder staying where it is.
		string text2 = Path.Combine("items", text + ".png");
		string text3 = Path.Combine(outputDirectory, text2);
		try
		{
			string directoryName = Path.GetDirectoryName(text3);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.Copy(spriteSource, text3, overwrite: true);
			result.SpritesCopied++;
		}
		catch (Exception ex)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': cannot copy the sprite (" + ex.GetType().Name + ": " + ex.Message + ")");
			return;
		}
		string text4 = UniqueIdentifier(Identifier(text, upperFirst: false), used);
		body.Append("\t\t// ").Append(text).Append(" (new item)\n");
		body.Append("\t\tModItem ").Append(text4).Append(" = Context.DefineItem(")
			.Append(Quote(text)).Append(", ").Append(Quote(text2.Replace('\\', '/'))).Append(");\n");
		body.Append("\t\tif (").Append(text4).Append(" != null)\n");
		body.Append("\t\t{\n");
		int num = 0;
		// The converted fields, so the recipe's category is inferred from what the pack said
		// rather than from a blank item template.
		Dictionary<string, string> fieldMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < lines.Count; i++)
		{
			KeyValuePair<string, string> keyValuePair = new KeyValuePair<string, string>(lines[i].Key, lines[i].Value);
			// "type" is the tConfig marker for "this is a new item"; it is not a field of the item.
			if (string.Equals(keyValuePair.Key, "type", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			// The recipe lives in the same file, in its own sections.
			if (IsRecipeSection(lines[i].Section))
			{
				continue;
			}
			string text5 = PropertyName(keyValuePair.Key);
			string valueProblem2 = FieldProblem(keyValuePair.Key, keyValuePair.Value);
			if ((text5 == null && !IsEscapedField(keyValuePair.Key)) || valueProblem2 != null)
			{
				result.UnsupportedFields++;
				result.Warnings.Add("'" + text + "': field '" + keyValuePair.Key + "' with value '"
					+ keyValuePair.Value + "' " + (valueProblem2 ?? "has no equivalent") + " - dropped");
				continue;
			}
			if (IsEscapedField(keyValuePair.Key))
			{
				body.Append("\t\t\t").Append(text4).Append(".Set(")
					.Append(Quote(keyValuePair.Key.ToLowerInvariant())).Append(", ")
					.Append(Quote(keyValuePair.Value)).Append(");\n");
				fieldMap[keyValuePair.Key] = keyValuePair.Value;
				num++;
				result.Fields++;
				continue;
			}
			body.Append("\t\t\t").Append(text4).Append('.').Append(text5)
				.Append(" = ").Append(Literal(keyValuePair.Key, keyValuePair.Value)).Append(";\n");
			fieldMap[keyValuePair.Key] = keyValuePair.Value;
			num++;
			result.Fields++;
		}
		body.Append("\t\t}\n\n");
		result.NewItems++;
		result.Items++;
		if (num == 0)
		{
			result.Warnings.Add("'" + text + "': no fields converted, the item will exist with default stats");
		}
		// The item's own recipe, from the same file. A new item with no recipe is nearly
		// unobtainable - it exists, but there is no way to make it - so this is the part that
		// makes the conversion useful rather than merely correct.
		Item definition = default(Item);
		AppendRecipe(body, iniPath, text, definition, fieldMap, used, result);
	}

	/// <summary>
	/// True when the INI defines a new item rather than tweaking one: tConfig writes
	/// <c>type=-1</c> (or any negative id) for that. Reading the file is the only way to know,
	/// because the item's name does not exist in this build either way.
	/// </summary>
	private static bool DefinesNewItem(string iniPath)
	{
		string error;
		List<KeyValuePair<string, string>> list = TConfigImporter.ReadIni(iniPath, out error);
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (!string.Equals(list[i].Key, "type", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int result;
			if (int.TryParse(list[i].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return result < 0;
			}
		}
		return false;
	}

	/// <summary>
	/// Marker line in a generated <c>mod.ini</c>. It is what makes it safe to clean the folder
	/// before writing: a folder this tool did not generate must never have files removed.
	/// </summary>
	private const string GeneratedMarker = "; Generated by TerrariaXDK from a tConfig pack";

	/// <summary>
	/// Writes one raw mod item file: the supported fields, then the recipe in the section-per-thing
	/// spelling, plus the sprite for a new item.
	///
	/// The rewrite is deliberate. Copying the pack verbatim would be shorter, but then every
	/// unsupported field would be reported again at load time, and the conversion report would
	/// describe a file rather than the mod: what the player gets is exactly what the report says
	/// it gets.
	/// </summary>
	private static void WriteItemRaw(string iniPath, string packDirectory, string outputDirectory, Result result)
	{
		string text = Path.GetFileNameWithoutExtension(iniPath);
		// The same check as the project path: a pack-wide table is not an item, and "no such item
		// in this build" hid that.
		string text4 = Terraria.Mods.RawMod.ServiceFileReason(text);
		if (text4 != null)
		{
			result.UnknownItems++;
			result.Warnings.Add("'" + text + "': " + text4 + " - not converted");
			return;
		}
		string error;
		List<TConfigImporter.SectionLine> lines = TConfigImporter.ReadIniSections(iniPath, out error);
		if (lines == null)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': " + Path.GetFileName(iniPath) + ": " + error);
			return;
		}
		bool flag = DefinesNewItem(iniPath);
		if (!flag && Terraria.ItemLookup.Find(text) <= 0)
		{
			// Unknown name without type=-1: a hand-written pack may simply have forgotten the
			// marker. A sprite is the evidence that it means to define something.
			if (!File.Exists(Path.ChangeExtension(iniPath, ".png")))
			{
				result.UnknownItems++;
				result.Warnings.Add("'" + text + "': no such item in this build, skipped");
				return;
			}
			flag = true;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("; Converted from '").Append(Path.GetFileName(packDirectory)).Append("' by TerrariaXDK.\n");
		stringBuilder.Append("; Only the fields this build understands are kept; the rest are listed in README.md.\n");
		if (flag)
		{
			string text2 = Path.ChangeExtension(iniPath, ".png");
			if (!File.Exists(text2))
			{
				result.NewItemsSkipped++;
				result.Warnings.Add("'" + text + "': new item with no sprite (" + Path.GetFileName(text2)
					+ ") - an item that cannot be drawn would look like a bug, so it is not converted");
				return;
			}
			try
			{
				string text3 = Path.Combine(outputDirectory, "Item");
				Directory.CreateDirectory(text3);
				File.Copy(text2, Path.Combine(text3, text + ".png"), overwrite: true);
				result.SpritesCopied++;
			}
			catch (Exception ex)
			{
				result.FailedFiles++;
				result.Errors.Add("'" + text + "': cannot copy the sprite (" + ex.GetType().Name + ": " + ex.Message + ")");
				return;
			}
			stringBuilder.Append("type=-1\n");
		}
		int num = 0;
		for (int i = 0; i < lines.Count; i++)
		{
			TConfigImporter.SectionLine sectionLine = lines[i];
			if (IsRecipeSection(sectionLine.Section))
			{
				continue;
			}
			if (string.Equals(sectionLine.Key, "type", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (!Terraria.Mods.ItemOverrides.IsSupportedField(sectionLine.Key))
			{
				result.UnsupportedFields++;
				result.Warnings.Add("'" + text + "': field '" + sectionLine.Key + "' has no equivalent, dropped");
				continue;
			}
			string valueProblem3 = FieldProblem(sectionLine.Key, sectionLine.Value);
			if (valueProblem3 != null)
			{
				result.UnsupportedFields++;
				result.Warnings.Add("'" + text + "': field '" + sectionLine.Key + "' has the value '"
					+ sectionLine.Value + "', which " + valueProblem3 + " - dropped");
				continue;
			}
			stringBuilder.Append(sectionLine.Key).Append('=').Append(sectionLine.Value).Append('\n');
			num++;
		}
		List<KeyValuePair<string, int>> list;
		List<string> list2;
		int num2;
		bool flag2;
		bool flag3 = ReadRecipe(lines, text, result, out list, out list2, out num2, out flag2);
		if (flag3)
		{
			stringBuilder.Append("\n[Items]\n");
			for (int j = 0; j < list.Count; j++)
			{
				stringBuilder.Append(list[j].Key).Append('=').Append(list[j].Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
			}
			if (list2.Count > 0)
			{
				stringBuilder.Append("[Tiles]\n");
				for (int k = 0; k < list2.Count; k++)
				{
					stringBuilder.Append(list2[k]).Append("=true\n");
				}
			}
			stringBuilder.Append("[Amount]\n").Append(num2.ToString(CultureInfo.InvariantCulture)).Append('\n');
			if (flag2)
			{
				stringBuilder.Append("[needWater]\ntrue\n");
			}
			result.Recipes++;
		}
		if (num == 0 && !flag && !flag3)
		{
			result.Warnings.Add("'" + text + "': nothing to convert");
			return;
		}
		try
		{
			string text5 = Path.Combine(outputDirectory, "Item");
			Directory.CreateDirectory(text5);
			File.WriteAllText(Path.Combine(text5, text + ".ini"), stringBuilder.ToString(), new UTF8Encoding(false));
		}
		catch (Exception ex2)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': cannot write the item file (" + ex2.GetType().Name + ": " + ex2.Message + ")");
			return;
		}
		result.Items++;
		result.Fields += num;
		if (flag)
		{
			result.NewItems++;
		}
	}

	/// <summary>
	/// Removes item files from a previous conversion that this pack no longer describes.
	///
	/// Only inside a folder this tool generated: re-converting a pack must not leave the items it
	/// dropped, but it must never touch a folder the player made by hand.
	/// </summary>
	private static void CleanStaleItems(string outputDirectory)
	{
		string path = Path.Combine(outputDirectory, "mod.ini");
		if (!File.Exists(path))
		{
			return;
		}
		string text;
		try
		{
			text = File.ReadAllText(path);
		}
		catch
		{
			return;
		}
		if (text.IndexOf(GeneratedMarker, StringComparison.Ordinal) < 0)
		{
			return;
		}
		string text2 = Path.Combine(outputDirectory, "Item");
		if (!Directory.Exists(text2))
		{
			return;
		}
		string[] files = Directory.GetFiles(text2, "*.ini", SearchOption.TopDirectoryOnly);
		for (int i = 0; i < files.Length; i++)
		{
			// The current run has not written anything yet, so "stale" cannot be told from "fresh"
			// here; the caller deletes before writing for exactly that reason.
			try
			{
				File.Delete(files[i]);
				string path2 = Path.ChangeExtension(files[i], ".png");
				if (File.Exists(path2))
				{
					File.Delete(path2);
				}
			}
			catch
			{
			}
		}
	}

	/// <summary>The <c>mod.ini</c> of a converted raw mod.</summary>
	private static string Manifest(string modName, string packDirectory)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(GeneratedMarker).Append('\n');
		stringBuilder.Append("; A tConfig pack turned into data. Nothing here needs a compiler.\n\n");
		stringBuilder.Append("[Mod]\n");
		stringBuilder.Append("Name=").Append(modName).Append('\n');
		// The pack has no version of its own, and inventing "1.0.0" would claim an authoring
		// history that does not exist. What matters for the signature is the content hash.
		stringBuilder.Append("Version=1.0.0\n");
		stringBuilder.Append("Author=converted from ").Append(Path.GetFileName(packDirectory)).Append('\n');
		return stringBuilder.ToString();
	}

	/// <summary>The README inside a converted raw mod: what it is, and what was lost.</summary>
	private static string RawReadme(string modName, string packDirectory, Result result)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("# ").Append(modName).Append("\n\n");
		stringBuilder.Append("A raw mod, converted from `").Append(packDirectory).Append("` by TerrariaXDK.\n\n");
		stringBuilder.Append("It is data, not code: no compiler was needed to make it and none is needed to use it.\n");
		stringBuilder.Append("What it cannot do is run code - a pack that used tConfig's `OnUse` or a custom AI\n");
		stringBuilder.Append("became a set of stats, sprites and recipes.\n\n");
		stringBuilder.Append("| | |\n|---|---|\n");
		stringBuilder.Append("| items | ").Append(result.Items).Append(" |\n");
		stringBuilder.Append("| new items | ").Append(result.NewItems).Append(" |\n");
		stringBuilder.Append("| fields | ").Append(result.Fields).Append(" |\n");
		stringBuilder.Append("| recipes | ").Append(result.Recipes).Append(" |\n");
		stringBuilder.Append("| sprites | ").Append(result.SpritesCopied).Append(" |\n\n");
		if (result.UnknownItems > 0)
		{
			stringBuilder.Append("- ").Append(result.UnknownItems).Append(" file(s) named an item this build does not have\n");
		}
		if (result.Ignored.Count > 0)
		{
			stringBuilder.Append("\n## What the pack has and this build cannot load\n\n");
			stringBuilder.Append("These are not mistakes in the pack: this build has no mod NPCs, tiles, walls,\n");
			stringBuilder.Append("buffs or projectiles, so no conversion can produce them. A pack that was made of\n");
			stringBuilder.Append("NPCs will become a mod of items only.\n\n");
			for (int k = 0; k < result.Ignored.Count; k++)
			{
				stringBuilder.Append("- ").Append(result.Ignored[k].Files).Append(" file(s) in `")
					.Append(result.Ignored[k].Kind).Append("`\n");
			}
		}
		if (result.NewItemsSkipped > 0)
		{
			stringBuilder.Append("- ").Append(result.NewItemsSkipped).Append(" new item(s) had no sprite and were skipped\n");
		}
		if (result.UnsupportedFields > 0)
		{
			stringBuilder.Append("- ").Append(result.UnsupportedFields).Append(" field(s) had no equivalent here and were dropped\n");
		}
		if (result.RecipesSkipped > 0)
		{
			stringBuilder.Append("- ").Append(result.RecipesSkipped).Append(" recipe(s) could not be converted\n");
		}
		if (result.Errors.Count > 0)
		{
			stringBuilder.Append("\n## What failed\n\n");
			for (int i = 0; i < result.Errors.Count; i++)
			{
				stringBuilder.Append("- ").Append(result.Errors[i]).Append('\n');
			}
		}
		if (result.Warnings.Count > 0)
		{
			stringBuilder.Append("\n## What was dropped\n\n");
			for (int j = 0; j < result.Warnings.Count; j++)
			{
				stringBuilder.Append("- ").Append(result.Warnings[j]).Append('\n');
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// A name usable as a folder under <c>Storage/Mods</c>: readable, but with the characters the
	/// filesystem refuses replaced. Unlike <see cref="Identifier"/> it keeps spaces and apostrophes,
	/// because this is a display name the player sees in the mod list.
	/// </summary>
	internal static string FolderName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		StringBuilder stringBuilder = new StringBuilder(name.Length);
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			stringBuilder.Append((Array.IndexOf(invalidFileNameChars, c) >= 0) ? '_' : c);
		}
		string text = stringBuilder.ToString().Trim().Trim('.');
		if (text.Length > 64)
		{
			text = text.Substring(0, 64).Trim();
		}
		return text;
	}

	/// <summary>
	/// True when a field's value can become a C# literal of the right type. Boolean fields take
	/// true/false; everything else has to parse as a number in the invariant culture.
	///
	/// This exists because <see cref="Literal"/> deliberately quotes a value it cannot read, so
	/// that a bad value fails to compile instead of silently becoming zero. Failing to compile is
	/// the right answer for a generator; for a converter run by a player it is not, because the
	/// project they asked for is then unusable and the reason is buried in a compiler message.
	/// So the bad field is dropped here and named in the report instead.
	/// </summary>
	/// <summary>
	/// Why a field's value cannot be used, or null when it can.
	///
	/// One function for both outputs, so the raw mod and the project cannot disagree about which
	/// fields survive, and so the report names the real problem. "has the value 'True', which is
	/// not a number" was the message for a *boolean* field, and "createTileName ... is not a
	/// number" for a tile name - both true statements that pointed the player at the wrong thing.
	/// </summary>
	internal static string ValueProblem(string field, string value)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			return "is empty";
		}
		string text2 = (field ?? string.Empty).Trim().ToLowerInvariant();
		if (text2 == "createtilename")
		{
			return "needs mod tiles, which this build does not have";
		}
		if (text2 == "createwallname")
		{
			return "needs mod walls, which this build does not have";
		}
		if (text2 == "wetlight")
		{
			// `light` is supported now: it is a brightness, and the item light calls use it.
			// `wetLight` is the *in-water* colour, which is a different thing (a tinted glow that
			// only applies while submerged) and needs the liquid check this build does not do.
			return "is the underwater glow, which this build does not model (only `light` is used)";
		}
		if (text2 == "setname")
		{
			// Verified against tConfig's own source (refs/T111M Config.cs): setName is the *armour
			// set* name, used only to group the three pieces so that the mod's SetBonus method can
			// run. It is not a display name, and without mod code there is no bonus to run - so
			// "renames the item" would have been the wrong reason, and a rename the wrong fix.
			return "names an armour set, and set bonuses are code, which this build cannot load from a pack";
		}
		if (text2 == "projectile")
		{
			return "names a projectile, and this build has no projectile table to resolve it";
		}
		if (text2 == "useammoname" || text2 == "ammoname")
		{
			// Resolved by name at load time; see ItemOverrides. The check only needs to know the
			// value is text rather than a number.
			return null;
		}
		if (text2 == "pretendtype")
		{
			return "makes the item behave as another type, which this build cannot do";
		}
		if (text2 == "headslot" || text2 == "bodyslot" || text2 == "legslot" || text2 == "hasarms")
		{
			// tConfig uses a negative slot number to mean "custom sprite"; this build would have to
			// draw mod armour on the player, and it cannot.
			return "needs a drawn armour sprite, which this build cannot load from a pack";
		}
		if (IsText(field))
		{
			// Any non-empty text is a valid tooltip.
			return null;
		}
		if (IsItemName(field))
		{
			// An item name is text; whether it resolves is decided at load time, and a miss is
			// reported there with the name in it - which is more useful than a conversion error.
			return null;
		}
		if (IsBoolean(field))
		{
			bool result;
			return bool.TryParse(text, out result) ? null : "is not true or false";
		}
		if (IsColour(field))
		{
			int r;
			int g;
			int b;
			int a;
			return Terraria.Mods.ItemOverrides.TryParseColour(text, out r, out g, out b, out a)
				? null : "is not R,G,B or R,G,B,A with 0..255 values";
		}
		double result2;
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
		{
			return null;
		}
		// A comma decimal separator: 49 values in the packs on this machine use it. Accepted here
		// too, or the field would be dropped at conversion and never reach the game at all.
		string swapped = Terraria.Mods.ItemOverrides.SwapDecimalComma(text);
		if (swapped != null
			&& double.TryParse(swapped, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
		{
			return null;
		}
		return "is not a number";
	}

	/// <summary>
	/// Why a field cannot be used at all, or null when it can.
	///
	/// <see cref="ValueProblem"/> answers "is this the right kind of value"; this adds "and does it
	/// fit the field". The ranges are not copied here: the probe runs the same
	/// <c>ItemOverrides.ApplyField</c> the game runs, so the report and the game cannot disagree
	/// about what fits - which is the whole point, because the player only ever sees one of them.
	///
	/// Text, item names, colours and booleans are skipped. An item name is resolved at load time,
	/// when mods are present; judging it here would call every mod item "not an item in this
	/// build", and the load-time message already names it.
	/// </summary>
	internal static string FieldProblem(string field, string value)
	{
		string text = ValueProblem(field, value);
		if (text != null)
		{
			return text;
		}
		if (IsText(field) || IsItemName(field) || IsColour(field) || IsBoolean(field))
		{
			return null;
		}
		return Terraria.Mods.ItemOverrides.FieldRefusal(field, value);
	}

	/// <summary>
	/// True for a field whose value is a colour rather than a number: <c>color=180,100,45,80</c>.
	/// </summary>
	private static bool IsColour(string field)
	{
		return string.Equals((field ?? string.Empty).Trim(), "color", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// True for a field whose value is an item name rather than a number: the two ammo fields are
	/// written by name in a pack and resolved through the item table at load time.
	/// </summary>
	internal static bool IsItemName(string field)
	{
		string text = (field ?? string.Empty).Trim();
		return text.Equals("useAmmoName", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("ammoName", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// True for a field the C# API has no property for, and which is applied through
	/// <c>ItemOverride.Set(field, value)</c> instead. <c>color</c> is the only one: a tint is a
	/// <c>Color</c>, and the generated project would have to build one - the escape hatch exists
	/// for exactly this, and it keeps the two output kinds from drifting apart.
	/// </summary>
	internal static bool IsEscapedField(string field)
	{
		return IsColour(field);
	}

	/// <summary>
	/// True when the field's value can be used. Kept for the call sites that only need the answer;
	/// <see cref="ValueProblem"/> is what the report uses.
	/// </summary>
	private static bool IsUsableValue(string field, string value)
	{
		return ValueProblem(field, value) == null;
	}

	/// <summary>
	/// INI field name to <see cref="ItemOverride"/> property. The two lists have to agree, and a
	/// mismatch here would produce a project that does not compile - which is at least loud, but
	/// the self-test checks the mapping so it never ships that way.
	/// </summary>
	internal static string PropertyName(string field)
	{
		switch (field.ToLowerInvariant())
		{
		case "damage":
			return "Damage";
		case "defense":
			return "Defense";
		case "pick":
			return "Pick";
		case "axe":
			return "Axe";
		case "hammer":
			return "Hammer";
		case "useanimation":
			return "UseAnimation";
		case "usetime":
			return "UseTime";
		case "usesound":
			return "UseSound";
		case "shoot":
			return "Shoot";
		case "shootspeed":
			return "ShootSpeed";
		case "knockback":
			return "Knockback";
		case "mana":
			return "Mana";
		case "heallife":
			return "HealLife";
		case "healmana":
			return "HealMana";
		case "bufftype":
			return "BuffType";
		case "bufftime":
			return "BuffTime";
		case "rare":
			return "Rare";
		case "value":
			return "Value";
		case "maxstack":
			return "MaxStack";
		case "autoreuse":
			return "AutoReuse";
		case "melee":
			return "Melee";
		case "magic":
			return "Magic";
		case "ranged":
			return "Ranged";
		case "potion":
			return "Potion";
		case "accessory":
			return "Accessory";
		case "nomelee":
			return "NoMelee";
		case "crit":
			return "Crit";
		case "material":
			return "Material";
		case "useturn":
			return "CanUseTurn";
		case "tooltip":
			return "ToolTip";
		case "tooltip2":
			return "ToolTip2";
		case "width":
			return "Width";
		case "height":
			return "Height";
		case "usestyle":
			return "UseStyle";
		case "holdstyle":
			return "HoldStyle";
		case "scale":
			return "Scale";
		case "ismaterial":
			return "Material";
		case "ammo":
			return "Ammo";
		case "useammo":
			return "UseAmmo";
		case "useammoname":
			// The ammo type by name. Applied through ItemOverride.Set, which resolves it - there is
			// no property for "an ammo item's name".
			return "UseAmmoName";
		case "ammoname":
			return "AmmoName";
		case "stack":
			// tConfig's name for MaxStack. 119 items in the packs on this machine use it.
			return "MaxStack";
		case "nousegraphic":
			return "NoUseGraphic";
		case "consumable":
			// The game has Item.IsConsumable and ApplyField sets it, so the project output must
			// carry it too. It was missing here, and 107 items in the packs lost the flag when
			// converted to a project (the raw-mod output kept it).
			return "Consumable";
		case "light":
			return "Light";
		default:
			return null;
		}
	}

	/// <summary>
	/// The C# literal for an INI value. Numeric fields keep their text as written (a float with
	/// a comma would not compile), and booleans become C# <c>true</c>/<c>false</c>. A value that
	/// is not the right shape is quoted, so the generated project fails to compile at that line
	/// instead of silently assigning zero.
	/// </summary>
	private static string Literal(string field, string value)
	{
		string text = value.Trim();
		// A comma decimal separator would emit `1,1f`, which does not compile. Normalised here
		// rather than at the call site so both outputs agree on the number.
		if (IsFloat(field))
		{
			string swapped = Terraria.Mods.ItemOverrides.SwapDecimalComma(text);
			if (swapped != null)
			{
				text = swapped;
			}
		}
		if (IsBoolean(field))
		{
			bool result;
			if (bool.TryParse(text, out result))
			{
				return result ? "true" : "false";
			}
			return Quote(text);
		}
		double result2;
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
		{
			// A float field needs the suffix: `4.5` on its own is a double literal and does not
			// convert implicitly. The first version of this emitted the raw text, and every pack
			// with a fractional knockback or scale produced a project that would not compile -
			// 114 items, all of them, which is how it was found.
			return IsFloat(field) ? (text + "f") : text;
		}
		return Quote(text);
	}

	/// <summary>Fields whose property is a <c>float</c> rather than an <c>int</c>.</summary>
	private static bool IsFloat(string field)
	{
		string text = PropertyName(field);
		return text == "ShootSpeed" || text == "Knockback" || text == "Scale" || text == "Light";
	}

	/// <summary>
	/// True when the field takes true/false rather than a number.
	///
	/// The list has to match <c>ItemOverrides.ApplyField</c>: every case there that calls
	/// <c>bool.TryParse</c> belongs here. <c>consumable</c> was missing, so the raw converter
	/// dropped it with "is not a number" - a field this build does support, lost for a wrong
	/// reason, on 90+ items in one pack.
	/// </summary>
	/// <summary>
	/// True when the field takes true/false rather than a number.
	///
	/// The list is by <b>field name</b>, not by property name, and that is the point: it used to
	/// derive from <see cref="PropertyName"/> and match a whitelist of property names, which meant
	/// a field whose property was renamed - or newly added, like <c>consumable</c> - silently fell
	/// out of the boolean set and was dropped as "is not a number". The vocabulary here is the
	/// pack's, the same one <c>ApplyField</c> switches on, so the two cannot disagree about a name.
	///
	/// It must match every <c>bool.TryParse</c> case in <c>ItemOverrides.ApplyField</c>; the
	/// <c>converter field coverage</c> check enforces that by probing each field with "True".
	/// </summary>
	private static bool IsBoolean(string field)
	{
		string text = (field ?? string.Empty).Trim().ToLowerInvariant();
		return text == "autoreuse" || text == "melee" || text == "magic" || text == "ranged"
			|| text == "potion" || text == "accessory" || text == "nomelee"
			|| text == "consumable" || text == "useturn" || text == "ismaterial"
			|| text == "material" || text == "nousegraphic";
	}

	/// <summary>
	/// Fields whose value is text, not a number. A tooltip is a sentence; treating it as a number
	/// would drop every description in the pack.
	/// </summary>
	private static bool IsText(string field)
	{
		string text = PropertyName(field);
		return text == "ToolTip" || text == "ToolTip2";
	}
	internal static string Identifier(string name, bool upperFirst)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if (char.IsLetterOrDigit(c))
			{
				if (stringBuilder.Length == 0 && char.IsDigit(c))
				{
					// An identifier cannot start with a digit; the item name is still in the comment.
					stringBuilder.Append('n');
				}
				stringBuilder.Append(flag ? char.ToUpperInvariant(c) : c);
				flag = false;
			}
			else
			{
				flag = true;
			}
		}
		if (stringBuilder.Length == 0)
		{
			return "";
		}
		if (upperFirst)
		{
			stringBuilder[0] = char.ToUpperInvariant(stringBuilder[0]);
		}
		else
		{
			stringBuilder[0] = char.ToLowerInvariant(stringBuilder[0]);
		}
		string text = stringBuilder.ToString();
		return IsKeyword(text) ? text + "Item" : text;
	}

	private static bool IsKeyword(string text)
	{
		string[] array = new string[16]
		{
			"abstract", "base", "class", "event", "fixed", "internal", "lock", "namespace",
			"object", "operator", "params", "string", "struct", "this", "value", "void"
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == text)
			{
				return true;
			}
		}
		return false;
	}

	private static string UniqueIdentifier(string name, HashSet<string> used)
	{
		string text = name;
		int num = 2;
		while (!used.Add(text))
		{
			text = name + num.ToString(CultureInfo.InvariantCulture);
			num++;
		}
		return text;
	}

	private static string Quote(string text)
	{
		StringBuilder stringBuilder = new StringBuilder("\"");
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '"' || c == '\\')
			{
				stringBuilder.Append('\\');
			}
			stringBuilder.Append(c);
		}
		stringBuilder.Append('"');
		return stringBuilder.ToString();
	}

	private static string Source(string modName, string packDirectory, string body)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("using Terraria.Mods;\n\n");
		stringBuilder.Append("// Generated by TerrariaXDK from the tConfig pack at\n");
		stringBuilder.Append("//   ").Append(packDirectory.Replace('\\', '/')).Append("\n");
		stringBuilder.Append("//\n");
		stringBuilder.Append("// This is a normal mod, not a one-way export: edit it, extend it, keep it under version\n");
		stringBuilder.Append("// control. The numbers below were the pack's Item/*.ini files; anything the pack said that\n");
		stringBuilder.Append("// this build has no field for was dropped and is listed in README.md.\n");
		stringBuilder.Append("[assembly: ModInfo(").Append(Quote(modName)).Append(", \"1.0.0\", \"\", ")
			.Append(Quote("Converted from the tConfig pack '" + modName + "'")).Append(")]\n\n");
		stringBuilder.Append("namespace ").Append(modName).Append(";\n\n");
		stringBuilder.Append("/// <summary>Item stat changes from a tConfig pack.</summary>\n");
		stringBuilder.Append("public class ").Append(modName).Append("Mod : Mod\n");
		stringBuilder.Append("{\n");
		stringBuilder.Append("\tpublic override void Load()\n");
		stringBuilder.Append("\t{\n");
		stringBuilder.Append(body.TrimEnd('\n')).Append('\n');
		stringBuilder.Append("\t}\n");
		stringBuilder.Append("}\n");
		return stringBuilder.ToString();
	}

	private static string Project(string modName, string gameAssembly)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<Project Sdk=\"Microsoft.NET.Sdk\">\n");
		stringBuilder.Append("  <!--\n");
		stringBuilder.Append("    Generated by TerrariaXDK. Build it, then copy the DLL:\n");
		stringBuilder.Append("\n");
		stringBuilder.Append("        dotnet build ").Append(modName).Append(".csproj\n");
		stringBuilder.Append("        copy bin/Debug/net40/").Append(modName).Append(".dll ../../Mods/").Append(modName).Append("/\n");
		stringBuilder.Append("\n");
		stringBuilder.Append("    The reference below points at the executable that generated this project. If you move\n");
		stringBuilder.Append("    the project to another machine, fix the HintPath.\n");
		stringBuilder.Append("  -->\n");
		stringBuilder.Append("  <PropertyGroup>\n");
		stringBuilder.Append("    <AssemblyName>").Append(modName).Append("</AssemblyName>\n");
		stringBuilder.Append("    <TargetFramework>net40</TargetFramework>\n");
		stringBuilder.Append("    <PlatformTarget>x64</PlatformTarget>\n");
		stringBuilder.Append("    <LangVersion>latest</LangVersion>\n");
		stringBuilder.Append("    <GenerateAssemblyInfo>False</GenerateAssemblyInfo>\n");
		stringBuilder.Append("    <RootNamespace>").Append(modName).Append("</RootNamespace>\n");
		stringBuilder.Append("    <NoWarn>MSB3277</NoWarn>\n");
		stringBuilder.Append("  </PropertyGroup>\n");
		stringBuilder.Append("  <ItemGroup>\n");
		stringBuilder.Append("    <Reference Include=\"TerrariaXDK\">\n");
		stringBuilder.Append("      <HintPath>").Append(gameAssembly).Append("</HintPath>\n");
		stringBuilder.Append("      <Private>false</Private>\n");
		stringBuilder.Append("    </Reference>\n");
		stringBuilder.Append("    <Reference Include=\"System\" />\n");
		stringBuilder.Append("    <Reference Include=\"System.Core\" />\n");
		stringBuilder.Append("  </ItemGroup>\n");
		stringBuilder.Append("</Project>\n");
		return stringBuilder.ToString();
	}

	private static string Readme(string modName, string packDirectory, Result result)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("# ").Append(modName).Append("\n\n");
		stringBuilder.Append("Generated from the tConfig pack `")
			.Append(packDirectory.Replace('\\', '/')).Append("`.\n\n");
		stringBuilder.Append("| | |\n|---|---|\n");
		stringBuilder.Append("| Items | ").Append(result.Items.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| Fields | ").Append(result.Fields.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| Unknown items (skipped) | ").Append(result.UnknownItems.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| Fields with no equivalent (dropped) | ").Append(result.UnsupportedFields.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| Files that failed (skipped) | ").Append(result.FailedFiles.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| New items converted | ").Append(result.NewItems.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| New items skipped (no sprite) | ").Append(result.NewItemsSkipped.ToString(CultureInfo.InvariantCulture)).Append(" |\n\n");
		stringBuilder.Append("## Build and install\n\n");
		stringBuilder.Append("```\ndotnet build ").Append(modName).Append(".csproj\n");
		stringBuilder.Append("```\n\n");
		stringBuilder.Append("Copy `bin/Debug/net40/").Append(modName)
			.Append(".dll` into `Storage/Mods/").Append(modName).Append("/`");
		if (result.SpritesCopied > 0)
		{
			// The sprites are separate files, not embedded: a mod that is only a DLL would look
			// for its pictures and find nothing, and its items would be invisible.
			stringBuilder.Append(", **and the `items` folder next to it** - the sprites are files, not embedded");
		}
		stringBuilder.Append(". The mod list in the game\n");
		stringBuilder.Append("then shows it by name, and multiplayer uses it in the mod signature - which is the\n");
		stringBuilder.Append("reason to convert a pack at all: a loose `Storage/tConfig/` folder changes the numbers\n");
		stringBuilder.Append("on one machine and takes no part in the signature, so a host cannot require it.\n\n");
		if (result.Errors.Count > 0)
		{
			stringBuilder.Append("## What failed\n\n");
			stringBuilder.Append("These files or items could not be converted and are **not** in the mod. The\n");
			stringBuilder.Append("rest of the pack was converted anyway, so the project still builds.\n\n");
			for (int i = 0; i < result.Errors.Count; i++)
			{
				stringBuilder.Append("- ").Append(result.Errors[i]).Append('\n');
			}
			stringBuilder.Append('\n');
		}
		if (result.Ignored.Count > 0)
		{
			// The reason this section exists: a pack of 146 NPCs converts to "111 items, done" and
			// reads as if the NPCs came along. They did not, and no conversion can bring them.
			stringBuilder.Append("## What the pack has and this build cannot load\n\n");
			stringBuilder.Append("Not a mistake in the pack: this build has no mod NPCs, tiles, walls, buffs or\n");
			stringBuilder.Append("projectiles. Those folders are listed here so the numbers above are not read as\n");
			stringBuilder.Append("\"everything came across\".\n\n");
			for (int j = 0; j < result.Ignored.Count; j++)
			{
				stringBuilder.Append("- ").Append(result.Ignored[j].Files.ToString(CultureInfo.InvariantCulture))
					.Append(" file(s) in `").Append(result.Ignored[j].Kind).Append("`\n");
			}
			stringBuilder.Append('\n');
		}
		if (result.NewItemsSkipped > 0)
		{
			stringBuilder.Append("## New items that were skipped\n\n");
			stringBuilder.Append("A new item needs a sprite: an item that cannot be drawn is indistinguishable\n");
			stringBuilder.Append("from a bug in the inventory, so it is not converted. The items below had no\n");
			stringBuilder.Append("PNG next to their `.ini` in the pack.\n\n");
			for (int num = 0; num < result.Warnings.Count; num++)
			{
				if (result.Warnings[num].IndexOf("new item with no sprite", StringComparison.Ordinal) >= 0)
				{
					stringBuilder.Append("- ").Append(result.Warnings[num]).Append('\n');
				}
			}
			stringBuilder.Append('\n');
		}
		stringBuilder.Append("## Why a field was dropped\n\n");
		stringBuilder.Append("A tConfig pack can name fields this build does not have, or give a value that is not\n");
		stringBuilder.Append("a number. They are listed here rather than guessed at, because a silently ignored\n");
		stringBuilder.Append("field looks like a conversion that worked.\n\n");
		if (result.Warnings.Count == 0)
		{
			stringBuilder.Append("Nothing was dropped.\n");
		}
		else
		{
			for (int j = 0; j < result.Warnings.Count; j++)
			{
				stringBuilder.Append("- ").Append(result.Warnings[j]).Append('\n');
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>Path of the running executable, used as the project's assembly reference.</summary>
	private static string GameAssemblyPath()
	{
		string text = Path.Combine(Terraria.Storage.AppPaths.BaseDirectory, "TerrariaXDK.exe");
		if (File.Exists(text))
		{
			return text;
		}
		// A Debug build has the same name; if neither exists the project still opens and the
		// author can point the reference somewhere real.
		return text;
	}
}
