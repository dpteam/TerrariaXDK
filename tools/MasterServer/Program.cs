using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace MasterServer;

/// <summary>
/// The master server as a console program.
///
/// <code>
/// MasterServer.exe                     listen on 0.0.0.0:17777
/// MasterServer.exe --port 17777        explicit port
/// MasterServer.exe --ttl 120           drop a listing after this long without an announce
/// MasterServer.exe --selftest          run the protocol checks against a temporary instance
/// </code>
///
/// It exists because the game's client (<c>Terraria/Net/MasterServerClient.cs</c>) needs a server
/// to talk to, and nothing in the repository provided one: pointing the game at a hostname only
/// worked if somebody else happened to be running a compatible service. This is that service,
/// small enough to read in one sitting and dependency-free so it starts on any Windows box.
///
/// The console output is the point of it for a developer: every request, every listing change and
/// every expiry shows up with a timestamp, so "is it working?" is answered by looking at it.
/// </summary>
internal static class Program
{
	private static int Main(string[] args)
	{
		int num = 17777;
		int num2 = MasterServerHost.DefaultTtlSeconds;
		int num3 = 17778;
		string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "master-servers.json");
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < args.Length; i++)
		{
			string text = args[i].ToLowerInvariant();
			if (text == "--selftest")
			{
				flag = true;
			}
			else if (text == "--no-relay")
			{
				flag2 = true;
			}
			else if (text == "--relay-port" && i + 1 < args.Length)
			{
				if (!int.TryParse(args[++i], out num3) || num3 < 0 || num3 > 65535)
				{
					Console.Error.WriteLine("--relay-port needs a number 0-65535");
					return 2;
				}
			}
			else if (text == "--port" && i + 1 < args.Length)
			{
				if (!int.TryParse(args[++i], out num) || num < 0 || num > 65535)
				{
					Console.Error.WriteLine("--port needs a number 0-65535");
					return 2;
				}
			}
			else if (text == "--ttl" && i + 1 < args.Length)
			{
				if (!int.TryParse(args[++i], out num2) || num2 < 5 || num2 > 86400)
				{
					Console.Error.WriteLine("--ttl needs seconds, 5-86400");
					return 2;
				}
			}
			else if (text == "--data" && i + 1 < args.Length)
			{
				dataPath = args[++i];
			}
			else if (text == "--help" || text == "-h" || text == "/?")
			{
				Usage();
				return 0;
			}
			else
			{
				Console.Error.WriteLine("unknown argument: " + args[i]);
				Usage();
				return 2;
			}
		}
		if (flag)
		{
			return SelfTest.Run();
		}
		return Run(num, num2, dataPath, flag2 ? (-1) : num3);
	}

	private static int Run(int port, int ttl, string dataPath, int relayPort)
	{
		Title("TerrariaXDK master server");
		MasterServerHost host = new MasterServerHost(port, ttl, dataPath);
		DateTime lastSave = DateTime.MinValue;
		DateTime lastStatsSave = DateTime.MinValue;
		host.Log += delegate(string message)
		{
			Say(message);
		};
		host.Changed += delegate
		{
			// Debounced: a host announcing every 45 seconds must not rewrite the file every time.
			DateTime utcNow = DateTime.UtcNow;
			if ((utcNow - lastSave).TotalSeconds < 5.0)
			{
				return;
			}
			lastSave = utcNow;
			host.Save();
		};
		host.StatsChangedEvent += delegate
		{
			// Same reasoning, on its own file: statistics are written when they change, and a busy
			// server should not rewrite the whole population on every report.
			DateTime utcNow = DateTime.UtcNow;
			if ((utcNow - lastStatsSave).TotalSeconds < 10.0)
			{
				return;
			}
			lastStatsSave = utcNow;
			host.Stats.Save();
		};
		try
		{
			host.Start();
		}
		catch (Exception ex)
		{
			Say("could not listen on port " + port + ": " + ex.Message);
			Say("another program may already be using it, or the port is below 1024 and needs elevation");
			return 1;
		}
		Say("listening on 0.0.0.0:" + host.Port);
		Say("ttl " + host.TtlSeconds + "s, data " + dataPath);
		Say("list   http://localhost:" + host.Port + "/v1/servers");
		Say("status http://localhost:" + host.Port + "/");
		Say("waiting for announces (the game sends one every 45s while hosting)");
		Say("");
		Say("achievements http://localhost:" + host.Port + "/v1/achievements");
		Say("leaderboard  http://localhost:" + host.Port + "/v1/leaderboard?stat=AirTravel");
		Say("");
		RelayHost relay = null;
		if (relayPort >= 0)
		{
			// The relay is optional: a master that only lists worlds is a perfectly good setup,
			// and an operator may not want to pay for other people's game traffic.
			relay = new RelayHost(relayPort);
			relay.Log += delegate(string message)
			{
				Say(message);
			};
			try
			{
				relay.Start();
				Say("relay listening on 0.0.0.0:" + relay.Port + " (max " + RelayHost.MaxRooms
					+ " room(s), " + RelayHost.MaxBytesPerSecondPerRoom / 1024 + " KiB/s each)");
			}
			catch (Exception ex)
			{
				Say("could not start the relay on port " + relayPort + ": " + ex.Message);
				relay = null;
			}
			Say("");
		}
		else
		{
			Say("relay disabled (--no-relay)");
			Say("");
		}
		ManualResetEvent manualResetEvent = new ManualResetEvent(initialState: false);
		Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
		{
			// Stop deliberately instead of letting the runtime kill the process mid-save.
			e.Cancel = true;
			manualResetEvent.Set();
		};
		AppDomain.CurrentDomain.ProcessExit += delegate
		{
			manualResetEvent.Set();
		};
		manualResetEvent.WaitOne();
		Say("");
		Say("shutting down, " + host.EntryCount + " server(s) were listed");
		if (relay != null)
		{
			relay.Stop();
		}
		host.Stop();
		return 0;
	}

	private static void Usage()
	{
		Console.WriteLine("usage: MasterServer [--port N] [--relay-port N | --no-relay] [--ttl SECONDS] [--data FILE] [--selftest]");
		Console.WriteLine();
		Console.WriteLine("  --port N        TCP port to listen on (default 17777). 0 picks a free one.");
		Console.WriteLine("  --relay-port N  TCP port for the game relay (default 17778). 0 picks a free one.");
		Console.WriteLine("  --no-relay      do not run the relay: the master only lists worlds.");
		Console.WriteLine("  --ttl SECONDS   drop a listing after this long without an announce (default "
			+ MasterServerHost.DefaultTtlSeconds + ").");
		Console.WriteLine("  --data FILE     where the list is saved (default master-servers.json next to the exe).");
		Console.WriteLine("  --selftest      check the protocol against a temporary instance and exit.");
	}

	private static void Title(string text)
	{
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine(text);
		Console.WriteLine(new string('=', text.Length));
		Console.ResetColor();
	}

	/// <summary>
	/// One line per event, timestamped. Colours are set per line and reset immediately, so a
	/// redirected console (or a host that does not support colours) still gets clean text.
	/// </summary>
	private static void Say(string message)
	{
		string text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
		ConsoleColor foregroundColor = ConsoleColor.Gray;
		if (message.StartsWith("listed", StringComparison.Ordinal) || message.StartsWith("updated", StringComparison.Ordinal))
		{
			foregroundColor = ConsoleColor.Green;
		}
		else if (message.StartsWith("expired", StringComparison.Ordinal) || message.StartsWith("removed", StringComparison.Ordinal))
		{
			foregroundColor = ConsoleColor.DarkYellow;
		}
		else if (message.Contains("-> 4") || message.Contains("-> 5") || message.StartsWith("error", StringComparison.Ordinal)
			|| message.StartsWith("could not", StringComparison.Ordinal))
		{
			foregroundColor = ConsoleColor.Red;
		}
		else if (message.Length == 0)
		{
			Console.WriteLine();
			return;
		}
		try
		{
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("[" + text + "] ");
			Console.ForegroundColor = foregroundColor;
			Console.WriteLine(message);
			Console.ResetColor();
		}
		catch
		{
			// No console handle (service, redirected stream): still emit the text.
			Console.WriteLine("[" + text + "] " + message);
		}
	}
}
