using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Terraria;

internal static class Program
{
	private enum LogLevel
	{
		Debug = 0,
		Info = 1,
		Warn = 2,
		Error = 3
	}

	private static string _logFile = Terraria.Storage.AppPaths.LogFile;

	private static Stopwatch _gameTimer = new Stopwatch();

	private static readonly object _logLock = new object();

	private static StreamWriter _logWriter;

	private static bool _loggerReady;

	private static LogLevel _minLevel = LogLevel.Info;

	/// <summary>Width of the category column; longer names are truncated.</summary>
	private const int CategoryWidth = 16;

	public static void Main(string[] args)
	{
		if (args != null && args.Length != 0)
		{
			for (int i = 0; i < args.Length; i++)
			{
				string text = args[i].ToLowerInvariant();
				if (text == "--selftest" || text == "-selftest")
				{
					Environment.ExitCode = Terraria.Headless.SelfTest.Run(args);
					return;
				}
				if (text == "--lang-export")
				{
					// Writes a starting file for translators and exits. Headless on purpose: it
					// only needs the string tables, and requiring a graphics device to export
					// text would put the tool out of reach of the machines people translate on.
					string text3 = (i + 1 < args.Length) ? args[i + 1] : null;
					Environment.ExitCode = ExportLanguage(text3);
					return;
				}
				if (text == "--tconfig-convert")
				{
					// Turns an old tConfig pack into a mod project. Headless for the same reason:
					// it reads INI files and item names, and needs no window to do it.
					Environment.ExitCode = ConvertTConfig(args, i + 1, raw: false);
					return;
				}
				if (text == "--tconfig-raw")
				{
					// The same pack, written as a raw mod: a folder the game loads without any
					// compiler. This is the path for machines that have no .NET SDK.
					Environment.ExitCode = ConvertTConfig(args, i + 1, raw: true);
					return;
				}
				if (text == "--font-check")
				{
					// Answers "will the game accept this atlas" for anyone who generated one.
					// Read-only, and it runs without a graphics device: the metrics file is the
					// part that goes wrong, and the texture is checked for existence and size.
					string fontFile = (i + 1 < args.Length) ? args[i + 1] : null;
					Environment.ExitCode = CheckFontAtlasFile(fontFile);
					return;
				}
				if (text == "--upnp")
				{
					// Answers "will my router open the port by itself", which is the question
					// behind "why can nobody join me". Read-only: it reports, it changes nothing.
					int upnpPort = 7777;
					if (i + 1 < args.Length)
					{
						int.TryParse(args[i + 1], out upnpPort);
					}
					Environment.ExitCode = ProbeUPnP(upnpPort);
					return;
				}
			}
		}
		InitLogger();
		LogSection("STARTUP");
		LogInfo("Init", "TerrariaXDK starting");
		LogInfo("Init", "Date      : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		LogInfo("Init", "OS        : " + Environment.OSVersion);
		LogInfo("Init", "Runtime   : " + Environment.Version);
		LogInfo("Init", "Platform  : " + Terraria.Portability.Describe());
		try
		{
			string text2 = (Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory);
			Terraria.Storage.AppPaths.EnsureDirectories();
			LogInfo("Path", "Storage   : " + Terraria.Storage.AppPaths.Root);
			LogInfo("Path", "Base      : " + text2);
			CheckCriticalPaths(text2);
		}
		catch (Exception arg)
		{
			LogError("Init", "Initialization failed: " + arg);
		}
		AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs resolveArgs) =>
		{
			LogWarn("Assembly", "Could not resolve: " + resolveArgs.Name);
			return (Assembly)null;
		};
		AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
		{
			LogError("Crash", "Unhandled exception: " + e.ExceptionObject);
		};
		Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", "opengl");
		Environment.SetEnvironmentVariable("FNA3D_BACKBUFFER_SCALE_NEAREST", "1");
		Environment.SetEnvironmentVariable("FNA3D_GRAPHICS_DEBUG_LEVEL", "2");
		_gameTimer.Start();
		LogSection("GAME LOOP");
		using Main main = new Main();
		LogInfo("Game", $"Main created in {_gameTimer.ElapsedMilliseconds} ms");
		try
		{
			main.Run();
			LogInfo("Game", $"Run() returned after {_gameTimer.ElapsedMilliseconds} ms");
		}
		catch (Exception ex)
		{
			LogError("Game", "Run() threw: " + ex.Message);
			WriteCrashLog(ex);
		}
		_gameTimer.Stop();
		LogSection($"SESSION ENDED ({_gameTimer.ElapsedMilliseconds} ms)");
		CloseLogger();
	}

	/// <summary>
	/// <c>--lang-export [code]</c>: writes <c>Storage/Lang/&lt;code&gt;.txt</c> for a translator to
	/// edit and returns an exit code. Runs without a window or a graphics device, because the
	/// machines people translate on should not need one.
	/// </summary>
	private static int ExportLanguage(string requested)
	{
		Terraria.Storage.AppPaths.Headless = true;
		try
		{
			Terraria.Storage.AppPaths.EnsureDirectories();
		}
		catch
		{
		}
		int num;
		if (!Terraria.LangOverrides.TryParse(requested, out num))
		{
			Console.Error.WriteLine("Unknown language '" + requested + "'. Use auto, en, de, it, fr or es.");
			return 2;
		}
		string error;
		string text = Terraria.LangOverrides.WriteTemplate(num, out error);
		if (text == null)
		{
			Console.Error.WriteLine("Could not write the template: " + error);
			return 1;
		}
		Console.WriteLine("Wrote " + text);
		Console.WriteLine("Edit it and start the game: the file is read on top of the built-in text.");
		return 0;
	}

	/// <summary>
	/// <c>--tconfig-convert &lt;pack&gt; [--out &lt;dir&gt;] [--name &lt;Mod&gt;]</c>: writes a mod
	/// project that does what the pack did, and returns an exit code.
	/// <c>--tconfig-raw</c> takes the same arguments and writes a raw mod (INI + PNG) instead -
	/// the same pack, usable without a compiler.
	///
	/// The pack is read the same way the runtime importer reads it, so the conversion cannot
	/// disagree with what the game would have done with the folder.
	/// </summary>
	private static int ConvertTConfig(string[] args, int start, bool raw)
	{
		Terraria.Storage.AppPaths.Headless = true;
		try
		{
			Terraria.Storage.AppPaths.EnsureDirectories();
		}
		catch
		{
		}
		// The conversion resolves item names, and the table it needs is built by calling
		// Item.SetDefaults for every id in the build - about 140 ms, measured. Left to first use it
		// lands in the middle of the conversion, which in the menu is a visible hitch; the game
		// warms it on the loading screen, and here there is no screen, so it is warmed up front.
		Terraria.ItemLookup.Warm();
		string pack = null;
		string output = null;
		string name = null;
		// Pack names contain spaces - every tConfig pack from the PC scene does ("Omnirs Weapons",
		// "The Arsenal"). So everything that is not a flag belongs to the pack path, joined back
		// together. Taking only the first word silently converted "Omnirs" and dropped "Weapons".
		System.Collections.Generic.List<string> packParts = new System.Collections.Generic.List<string>();
		for (int i = start; i < args.Length; i++)
		{
			string text = args[i].ToLowerInvariant();
			if (text == "--out")
			{
				output = (i + 1 < args.Length) ? args[++i] : null;
			}
			else if (text == "--name")
			{
				name = (i + 1 < args.Length) ? args[++i] : null;
			}
			else
			{
				packParts.Add(args[i]);
			}
		}
		if (packParts.Count > 0)
		{
			pack = string.Join(" ", packParts.ToArray());
		}
		if (pack == null)
		{
			Console.Error.WriteLine("Usage: TerrariaXDK.exe " + (raw ? "--tconfig-raw" : "--tconfig-convert")
				+ " <pack> [--out <dir>] [--name <Mod>]");
			Console.Error.WriteLine("  <pack>  folder with Item/*.ini, e.g. Storage/tConfig/MyPack");
			Console.Error.WriteLine(raw
				? "  --out   where to write the mod folder (default: Storage/Mods/<Mod>)"
				: "  --out   where to write the project (default: Storage/ModSources/<Mod>)");
			return 2;
		}
		if (output == null)
		{
			string text2 = (name != null) ? name : new DirectoryInfo(Path.GetFullPath(pack)).Name;
			output = Path.Combine(raw ? Terraria.Storage.AppPaths.Mods : Terraria.Storage.AppPaths.ModSources, text2);
		}
		string error;
		Terraria.Mods.TConfigConverter.Result result = Terraria.Mods.TConfigConverter.Convert(pack, output, name, raw, out error);
		if (result == null)
		{
			Console.Error.WriteLine("Cannot convert: " + error);
			return 1;
		}
		Console.WriteLine("Wrote " + (result.IsRaw ? result.ManifestFile : result.ProjectFile));
		Console.WriteLine("  " + result.Items + " item(s), " + result.Fields + " field(s)");
		if (result.IsRaw)
		{
			Console.WriteLine("  " + result.NewItems + " new item(s), " + result.Recipes + " recipe(s)");
			Console.WriteLine("  restart the game to load it from Storage/Mods/" + Path.GetFileName(result.OutputDirectory) + "/");
		}
		if (result.UnknownItems > 0)
		{
			Console.WriteLine("  " + result.UnknownItems + " item(s) skipped: not in this build");
		}
		if (result.UnsupportedFields > 0)
		{
			Console.WriteLine("  " + result.UnsupportedFields + " field(s) dropped: no equivalent here");
		}
		// Failures go to stderr, not stdout: a script that pipes the output should be able to tell
		// "converted with some losses" from "converted cleanly" without parsing prose.
		if (result.Errors.Count > 0)
		{
			Console.Error.WriteLine("  " + result.Errors.Count + " file(s) or item(s) failed and are NOT in the mod:");
			for (int j = 0; j < result.Errors.Count; j++)
			{
				Console.Error.WriteLine("  ! " + result.Errors[j]);
			}
		}
		for (int k = 0; k < result.Warnings.Count && k < 20; k++)
		{
			Console.WriteLine("  - " + result.Warnings[k]);
		}
		if (result.Warnings.Count > 20)
		{
			Console.WriteLine("  ... and " + (result.Warnings.Count - 20) + " more in the log");
		}
		if (result.LogFile.Length != 0)
		{
			Console.WriteLine("Log: " + result.LogFile);
		}
		if (!result.IsRaw)
		{
			Console.WriteLine("Build it with: dotnet build \"" + result.ProjectFile + "\"");
			Console.WriteLine("Then copy the DLL into Storage/Mods/" + Path.GetFileNameWithoutExtension(result.ProjectFile) + "/");
		}
		// A conversion with failures still succeeded, but not cleanly - say so in the exit code so
		// a build script notices instead of shipping a mod that is missing half its items.
		return (result.Errors.Count > 0) ? 3 : 0;
	}

	/// <summary>
	/// <c>--font-check &lt;metrics&gt;</c>: reports whether the game would accept a generated font
	/// atlas, and returns an exit code.
	///
	/// The texture is checked by reading its PNG header rather than by decoding it: decoding needs
	/// a graphics device, and the question here is "are the glyph rectangles inside the image",
	/// which the header answers. A file that is not a PNG at all is caught by the signature.
	/// </summary>
	private static int CheckFontAtlasFile(string path)
	{
		Terraria.Storage.AppPaths.Headless = true;
		if (string.IsNullOrEmpty(path))
		{
			Console.Error.WriteLine("Usage: TerrariaXDK.exe --font-check <file.metrics>");
			return 2;
		}
		string text;
		try
		{
			text = File.ReadAllText(path);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("Cannot read " + path + ": " + ex.Message);
			return 1;
		}
		string error;
		Terraria.Fonts.FontAtlas fontAtlas = Terraria.Fonts.FontAtlas.Parse(text, out error);
		if (fontAtlas == null)
		{
			Console.Error.WriteLine("Rejected: " + error);
			return 1;
		}
		Console.WriteLine("Metrics  : ok, " + fontAtlas.Count + " glyph(s), line height " + fontAtlas.LineHeight);
		if (fontAtlas.DefaultCharacter.HasValue)
		{
			Console.WriteLine("Fallback : '" + fontAtlas.DefaultCharacter.Value + "'");
		}
		else
		{
			Console.WriteLine("Fallback : none - a character outside the atlas will throw");
		}
		string directoryName = Path.GetDirectoryName(Path.GetFullPath(path));
		string text2 = Path.Combine(directoryName ?? "", fontAtlas.TexturePath);
		if (!File.Exists(text2))
		{
			Console.Error.WriteLine("Rejected: the texture " + text2 + " does not exist");
			return 1;
		}
		int num;
		int num2;
		if (!ReadPngSize(text2, out num, out num2))
		{
			Console.Error.WriteLine("Rejected: " + text2 + " is not a PNG");
			return 1;
		}
		Console.WriteLine("Texture  : " + Path.GetFileName(text2) + ", " + num + "x" + num2);
		for (int i = 0; i < fontAtlas.Count; i++)
		{
			Microsoft.Xna.Framework.Rectangle rectangle = fontAtlas.GlyphBounds[i];
			if (rectangle.X + rectangle.Width > num || rectangle.Y + rectangle.Height > num2)
			{
				Console.Error.WriteLine("Rejected: glyph " + ((int)fontAtlas.Characters[i]) + " at ("
					+ rectangle.X + ", " + rectangle.Y + ") " + rectangle.Width + "x" + rectangle.Height
					+ " lies outside the texture");
				return 1;
			}
		}
		// What the game will actually be able to show: the point of the tool is a language the
		// baked atlases cannot draw, so the interesting line is whether those letters are there.
		int num3 = 0;
		for (int j = 0; j < fontAtlas.Count; j++)
		{
			if (fontAtlas.Characters[j] >= '\u0400' && fontAtlas.Characters[j] <= '\u04ff')
			{
				num3++;
			}
		}
		Console.WriteLine("Cyrillic : " + num3 + " glyph(s)");
		Console.WriteLine("This atlas is usable. Rename it to <language code>.<font>.metrics and put it in Storage/Fonts/.");
		return 0;
	}

	/// <summary>Reads width and height from a PNG's IHDR. False for anything that is not a PNG.</summary>
	private static bool ReadPngSize(string path, out int width, out int height)
	{
		width = 0;
		height = 0;
		try
		{
			byte[] array = new byte[24];
			using (FileStream stream = File.OpenRead(path))
			{
				if (stream.Read(array, 0, array.Length) != array.Length)
				{
					return false;
				}
			}
			byte[] array2 = new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };
			for (int i = 0; i < array2.Length; i++)
			{
				if (array[i] != array2[i])
				{
					return false;
				}
			}
			// IHDR is the first chunk: length(4) type(4) width(4) height(4), big endian.
			if (array[12] != 'I' || array[13] != 'H' || array[14] != 'D' || array[15] != 'R')
			{
				return false;
			}
			width = (array[16] << 24) | (array[17] << 16) | (array[18] << 8) | array[19];
			height = (array[20] << 24) | (array[21] << 16) | (array[22] << 8) | array[23];
			return width > 0 && height > 0;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// <c>--upnp [port]</c>: reports what the router says about the game port and returns an exit
	/// code. Read-only on purpose - it never creates or removes a mapping, so it is safe to run
	/// on a machine whose port was forwarded by hand.
	/// </summary>
	private static int ProbeUPnP(int port)
	{
		Terraria.Storage.AppPaths.Headless = true;
		string error;
		Console.WriteLine("Looking for a UPnP gateway (2 s)...");
		Terraria.Net.UPnP.Gateway gateway = Terraria.Net.UPnP.Discover(2000, out error);
		if (gateway == null)
		{
			Console.WriteLine("No gateway: " + error);
			Console.WriteLine("The router either has UPnP switched off or does not have it. Without it,");
			Console.WriteLine("a world can still be hosted, but players outside your network need either a");
			Console.WriteLine("manual port forward or the relay ([Network] AllowRelay=True).");
			return 1;
		}
		Console.WriteLine("Gateway  : " + gateway.ControlUrl);
		Console.WriteLine("Service  : " + gateway.ServiceType);
		string text = Terraria.Net.UPnP.LocalAddressFor(gateway.Host);
		Console.WriteLine("This host: " + (text ?? "(unknown)"));
		string text2 = Terraria.Net.UPnP.ExternalAddress(gateway, out error);
		Console.WriteLine("External : " + (text2 ?? "(the router did not say: " + error + ")"));
		string text3 = Terraria.Net.UPnP.DescribeMapping(gateway, port, out error);
		Console.WriteLine("Port " + port + " : " + (text3 ?? "not forwarded (" + error + ")"));
		if (text3 != null && text != null && text3 == text)
		{
			Console.WriteLine("This machine can be reached directly on port " + port + ".");
			return 0;
		}
		if (text3 != null)
		{
			Console.WriteLine("Port " + port + " goes to " + text3 + ", not to this machine.");
			return 1;
		}
		Console.WriteLine("Hosting will ask the router to open it, and fall back to the relay if refused.");
		return 0;
	}

	private static void InitLogger()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(_logFile);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			// Keep the previous session's log. The file used to be truncated on start, so
			// relaunching to investigate a crash destroyed exactly the log you needed.
			RotatePreviousLog(_logFile);
			_logWriter = new StreamWriter(_logFile, append: false, Encoding.UTF8);
			_logWriter.AutoFlush = true;
			_loggerReady = true;
		}
		catch
		{
			_logWriter = null;
		}
		string environmentVariable = Environment.GetEnvironmentVariable("TXDK_LOG_LEVEL");
		if (!string.IsNullOrEmpty(environmentVariable))
		{
			switch (environmentVariable.Trim().ToUpperInvariant())
			{
			case "DEBUG":
				_minLevel = LogLevel.Debug;
				break;
			case "WARN":
				_minLevel = LogLevel.Warn;
				break;
			case "ERROR":
				_minLevel = LogLevel.Error;
				break;
			}
		}
	}

	/// <summary>
	/// Renames a log that has content to <c>&lt;name&gt;.prev&lt;ext&gt;</c>, replacing any older
	/// copy. An empty or missing file is left alone so restarting does not shuffle empty files.
	/// </summary>
	internal static bool RotatePreviousLog(string path)
	{
		try
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path) || new FileInfo(path).Length == 0)
			{
				return false;
			}
			string text = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, Path.GetFileNameWithoutExtension(path) + ".prev" + Path.GetExtension(path));
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Move(path, text);
			return true;
		}
		catch
		{
			// A read-only or locked log must not stop the game from starting; the new session
			// simply appends over the old file.
			return false;
		}
	}

	private static void CloseLogger()
	{
		lock (_logLock)
		{
			try
			{
				_logWriter?.Flush();
				_logWriter?.Dispose();
				_logWriter = null;
			}
			catch
			{
			}
		}
	}

	// --- Public logging API -------------------------------------------------

	/// <summary>
	/// Legacy entry point. Category is taken from a leading "[Name]" and severity from the
	/// message wording, so the hundreds of existing call sites keep working unchanged.
	/// </summary>
	public static void Log(string message)
	{
		string category;
		string message2 = SplitCategory(message, out category);
		Write(InferLevel(message2), category, message2);
	}

	public static void LogInfo(string category, string message)
	{
		Write(LogLevel.Info, category, message);
	}

	public static void LogWarn(string category, string message)
	{
		Write(LogLevel.Warn, category, message);
	}

	public static void LogError(string category, string message)
	{
		Write(LogLevel.Error, category, message);
	}

	public static void LogDebug(string category, string message)
	{
		Write(LogLevel.Debug, category, message);
	}

	/// <summary>
	/// Receives every log line that reaches the destination. Used by the debug console (Debug
	/// builds only): it lives in its own file behind <c>#if DEBUG</c>, so in a Release build
	/// nothing ever subscribes and this stays null.
	/// </summary>
	internal static Action<string> LogSink
	{
		get
		{
			return _logSink;
		}
		set
		{
			_logSink = value;
		}
	}

	private static Action<string> _logSink;

	/// <summary>Major phase marker: a titled separator that makes the log skimmable.</summary>
	public static void LogSection(string title)
	{
		Write(LogLevel.Info, "", "---- " + title + " " + new string('-', Math.Max(0, 60 - title.Length)));
	}

	// --- Internals ----------------------------------------------------------

	private static void Write(LogLevel level, string category, string message)
	{
		// Before InitLogger there is no destination; the self-test relies on a clean stdout,
		// so component chatter during its run must not leak into it.
		if (!_loggerReady || level < _minLevel)
		{
			return;
		}
		message = Sanitize(message);
		string text = BuildLine(level, category, message);
		// The debug console subscribes here. Null in Release, and nulling it is what keeps a
		// detached window from holding a reference to a disposed form.
		Action<string> sink = _logSink;
		if (sink != null)
		{
			try
			{
				sink(text);
			}
			catch
			{
				// A broken listener must never break logging.
			}
		}
		lock (_logLock)
		{
			try
			{
				_logWriter?.WriteLine(text);
			}
			catch (ObjectDisposedException)
			{
			}
			// Console colouring is best-effort; some hosts (no console, redirected output)
			// throw on ForegroundColor, so every access is guarded.
			bool useColor = true;
			ConsoleColor foregroundColor = ConsoleColor.Gray;
			try
			{
				foregroundColor = Console.ForegroundColor;
			}
			catch
			{
				useColor = false;
			}
			if (useColor)
			{
				try
				{
					if (level == LogLevel.Error)
					{
						Console.ForegroundColor = ConsoleColor.Red;
					}
					else if (level == LogLevel.Warn)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
					}
					else if (level == LogLevel.Debug)
					{
						Console.ForegroundColor = ConsoleColor.DarkGray;
					}
				}
				catch
				{
				}
			}
			Console.WriteLine(text);
			if (useColor)
			{
				try
				{
					Console.ForegroundColor = foregroundColor;
				}
				catch
				{
				}
			}
		}
	}

	private static string BuildLine(LogLevel level, string category, string message)
	{
		string text = DateTime.Now.ToString("HH:mm:ss.fff");
		string text2 = $"+{_gameTimer.ElapsedMilliseconds:D7}ms";
		string text3 = (level switch
		{
			LogLevel.Debug => "DEBUG",
			LogLevel.Warn => "WARN",
			LogLevel.Error => "ERROR",
			_ => "INFO",
		});
		string text4 = string.IsNullOrEmpty(category) ? "" : category;
		if (text4.Length > CategoryWidth)
		{
			text4 = text4.Substring(0, CategoryWidth - 1) + "~";
		}
		string text5 = text + " | " + text2 + " | " + text3.PadRight(5) + " | " + text4.PadRight(CategoryWidth) + " | ";
		// Multi-line messages (stack traces) stay inside the columns instead of breaking them.
		return text5 + message.Replace("\n", "\n" + new string(' ', text5.Length));
	}

	/// <summary>
	/// Removes decorative characters that the decompiled sources sprinkle through messages
	/// (check marks, warning signs, arrows, emoji). They render as '?' in most log viewers and
	/// shift the columns, and severity is now carried by the level field instead.
	/// </summary>
	private static string Sanitize(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(message.Length);
		for (int i = 0; i < message.Length; i++)
		{
			char c = message[i];
			if (char.IsSurrogate(c) || c == '\uFE0F' || c == '\uFE0E' || c == '\u200B')
			{
				continue;
			}
			// Arrows, miscellaneous symbols, dingbats and geometric shapes used as icons.
			if ((c >= '\u2190' && c <= '\u27BF') || (c >= '\u2B00' && c <= '\u2BFF'))
			{
				continue;
			}
			stringBuilder.Append(c);
		}
		string text = stringBuilder.ToString().Replace("\r\n", "\n").Replace('\r', '\n');
		// Trim per line, but keep interior spacing: call sites use it to align values.
		string[] array = text.Split('\n');
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = array[j].Trim();
		}
		return string.Join("\n", array);
	}

	/// <summary>Pulls a leading "[Name]" off the message; returns the remainder.</summary>
	private static string SplitCategory(string message, out string category)
	{
		category = "General";
		if (string.IsNullOrEmpty(message))
		{
			return "";
		}
		int num = message.IndexOf(']');
		if (message[0] == '[' && num > 1 && num <= 32)
		{
			string text = message.Substring(1, num - 1);
			// "MAIN.LoadContent()" is noisy in a column - keep the leading identifier.
			int num2 = text.IndexOf('(');
			if (num2 > 0)
			{
				text = text.Substring(0, num2);
			}
			int num3 = text.IndexOf('.');
			if (num3 > 0)
			{
				text = text.Substring(0, num3);
			}
			if (text.Length > 0)
			{
				category = text;
			}
			return message.Substring(num + 1).Trim();
		}
		return message;
	}

	/// <summary>
	/// Best-effort severity for legacy messages that carry no explicit level.
	/// Deliberately conservative: only clear failure wording escalates the level.
	/// </summary>
	private static LogLevel InferLevel(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return LogLevel.Info;
		}
		string text = message.ToUpperInvariant();
		if (text.Contains("EXCEPTION") || text.Contains("FATAL") || text.Contains("FAILED") || text.Contains("ERROR") || text.Contains("TIMED OUT") || text.Contains("HUNG"))
		{
			return LogLevel.Error;
		}
		if (text.Contains("WARNING") || text.Contains(" WARN") || text.Contains("MISSING") || text.Contains("DISABLED") || text.Contains("SKIPPING"))
		{
			return LogLevel.Warn;
		}
		return LogLevel.Info;
	}

	private static void CheckCriticalPaths(string baseDir)
	{
		string[] array = new string[13]
		{
			"Content", "Content/Achievements", "Content/ExtMusic", "Content/Fonts", "Content/Fonts/HD", "Content/Images", "Content/Sounds", "Content/UI", "Content/UI/CharacterCreation",
			"Content/UI/HowToPlay", "Content/UI/Leaderboards", "Content/UI/Upsell", "Content/Worlds"
		};
		string[] array2 = array;
		foreach (string text in array2)
		{
			string path = Path.Combine(baseDir, text);
			bool flag = Directory.Exists(path);
			int num = (flag ? Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length : 0);
			if (flag)
			{
				LogDebug("Content", $"{text} ({num} files)");
			}
			else
			{
				LogWarn("Content", $"{text} is missing");
			}
		}
	}

	private static void WriteCrashLog(Exception ex)
	{
		try
		{
			using StreamWriter streamWriter = new StreamWriter(Terraria.Storage.AppPaths.CrashLogFile, append: true);
			streamWriter.WriteLine($"=== CRASH {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
			streamWriter.WriteLine($"Runtime: {_gameTimer.ElapsedMilliseconds} ms");
			// Which screen was open. A stack trace that names UpdateMenu is not actionable on its
			// own: that one method drives every menu in the game.
			try
			{
				Terraria.UI ui = Terraria.UI.MainUI;
				streamWriter.WriteLine("UI: " + ((ui != null) ? ui.DebugState() : "(no UI)"));
			}
			catch (Exception)
			{
				streamWriter.WriteLine("UI: (state unavailable)");
			}
			streamWriter.WriteLine(ex);
			streamWriter.WriteLine();
			StackTrace stackTrace = new StackTrace(ex, fNeedFileInfo: true);
			for (int i = 0; i < stackTrace.FrameCount; i++)
			{
				StackFrame frame = stackTrace.GetFrame(i);
				// The IL offset is the fallback that still identifies the instruction when the PDB is
				// missing (GetFileLineNumber is 0 in that case, which is what most reports had).
				streamWriter.WriteLine($"  at {frame.GetMethod()} il={frame.GetILOffset()} line {frame.GetFileLineNumber()}");
			}
		}
		catch
		{
		}
	}
}
