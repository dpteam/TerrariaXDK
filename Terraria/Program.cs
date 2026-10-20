using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Terraria;

internal static class Program
{
	private static string _logFile = Terraria.Storage.AppPaths.LogFile;

	private static Stopwatch _gameTimer = new Stopwatch();

	private static object _logLock = new object();

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
			}
		}
		InitLogger();
		Log("=== TERRARIA XDK - DEBUG v2 ===");
		Log($"[INIT] DateTime: {DateTime.Now}");
		Log($"[INIT] OS: {Environment.OSVersion}");
		Log($"[INIT] .NET/Mono: {Environment.Version}");
		try
		{
			string text = (Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory);
			Terraria.Storage.AppPaths.EnsureDirectories();
			Log("[PATH] Storage root: " + Terraria.Storage.AppPaths.Root);
			Log("[PATH] BaseDirectory: " + text);
			Log("[PATH] CurrentDirectory: " + Environment.CurrentDirectory);
			CheckCriticalPaths(text);
		}
		catch (Exception arg)
		{
			Log($"[ERROR] Init failed: {arg}");
		}
		AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs resolveArgs) =>
		{
			Log("[ASSEMBLY_RESOLVE] " + resolveArgs.Name);
			return (Assembly)null;
		};
		AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
		{
			Log($"[UNHANDLED_EXCEPTION] {e.ExceptionObject}");
		};
		Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", "opengl");
		Environment.SetEnvironmentVariable("FNA3D_BACKBUFFER_SCALE_NEAREST", "1");
		Environment.SetEnvironmentVariable("FNA3D_GRAPHICS_DEBUG_LEVEL", "2");
		_gameTimer.Start();
		Log("[GAME] === CREATING MAIN INSTANCE ===");
		using Main main = new Main();
		Log($"[GAME] Main instance created in {_gameTimer.ElapsedMilliseconds}ms");
		Log("[GAME] === CALLING RUN (blocking call) ===");
		try
		{
			main.Run();
			Log($"[GAME] ✅ TheGame.Run() COMPLETED normally after {_gameTimer.ElapsedMilliseconds}ms");
		}
		catch (Exception ex)
		{
			Log($"[FATAL_ERROR] TheGame.Run() threw exception: {ex}");
			WriteCrashLog(ex);
		}
		_gameTimer.Stop();
		Log($"=== SESSION ENDED - Total: {_gameTimer.ElapsedMilliseconds}ms ===");
	}

	private static void InitLogger()
	{
		try
		{
			string directoryName = System.IO.Path.GetDirectoryName(_logFile);
			if (!string.IsNullOrEmpty(directoryName))
			{
				System.IO.Directory.CreateDirectory(directoryName);
			}
			if (File.Exists(_logFile))
			{
				File.Delete(_logFile);
			}
			File.WriteAllText(_logFile, string.Empty);
		}
		catch
		{
		}
	}

	public static void Log(string message)
	{
		string text = $"[{_gameTimer.ElapsedMilliseconds:D8}ms] {message}";
		lock (_logLock)
		{
			try
			{
				File.AppendAllText(_logFile, text + Environment.NewLine);
				Console.WriteLine(text);
			}
			catch
			{
				Console.WriteLine(text);
			}
		}
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
			Log(string.Format("[PATH] {0}: {1} ({2} files)", text, flag ? "✅" : "❌", num));
		}
	}

	private static void WriteCrashLog(Exception ex)
	{
		try
		{
			using StreamWriter streamWriter = new StreamWriter(Terraria.Storage.AppPaths.CrashLogFile, append: true);
			streamWriter.WriteLine($"=== CRASH {DateTime.Now} ===");
			streamWriter.WriteLine($"Runtime: {_gameTimer.ElapsedMilliseconds}ms");
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
