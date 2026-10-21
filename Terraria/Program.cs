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
			}
		}
		InitLogger();
		LogSection("STARTUP");
		LogInfo("Init", "TerrariaXDK starting");
		LogInfo("Init", "Date      : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		LogInfo("Init", "OS        : " + Environment.OSVersion);
		LogInfo("Init", "Runtime   : " + Environment.Version);
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
			streamWriter.WriteLine(ex);
			streamWriter.WriteLine();
			StackTrace stackTrace = new StackTrace(ex, fNeedFileInfo: true);
			for (int i = 0; i < stackTrace.FrameCount; i++)
			{
				StackFrame frame = stackTrace.GetFrame(i);
				streamWriter.WriteLine($"  at {frame.GetMethod()} line {frame.GetFileLineNumber()}");
			}
		}
		catch
		{
		}
	}
}
