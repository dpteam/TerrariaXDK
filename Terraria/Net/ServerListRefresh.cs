using System;

namespace Terraria.Net;

/// <summary>
/// Decides when the server list should quietly refresh itself while it is on screen.
///
/// The list only refreshed when it was (re)opened, so a friend who started hosting while
/// you were staring at the screen never appeared. This is a plain frame counter rather than
/// a timer: the game already has a frame loop, and a counter keeps the decision testable
/// without a clock.
/// </summary>
internal static class ServerListRefresh
{
	/// <summary>False when the player turned it off in the settings screen.</summary>
	public static bool Enabled = true;

	/// <summary>Seconds between automatic refreshes.</summary>
	public static int IntervalSeconds = 20;

	/// <summary>Assumed frame rate for the counter. Only affects how exact the interval is.</summary>
	private const int FramesPerSecond = 60;

	private static int _frames;

	/// <summary>Restarts the countdown; call when the screen opens or the player refreshes.</summary>
	public static void Reset()
	{
		_frames = 0;
	}

	/// <summary>
	/// Advances one frame and reports whether a refresh should start. Never fires while a
	/// search is already running, and never in singleplayer-only contexts where the screen
	/// is closed - otherwise it would restart the finder under the player's feet.
	/// </summary>
	public static bool Tick(bool screenOpen, bool alreadySearching)
	{
		if (!Enabled || !screenOpen || alreadySearching)
		{
			_frames = 0;
			return false;
		}
		if (IntervalSeconds <= 0)
		{
			return false;
		}
		_frames++;
		if (_frames < IntervalSeconds * FramesPerSecond)
		{
			return false;
		}
		_frames = 0;
		return true;
	}

	/// <summary>Keeps a remembered index inside a list that just changed size. The finder clears and
	/// refills the list, so the old selection can point past the end - which used to throw
	/// on the next join attempt.</summary>
	public static int ClampIndex(int selected, int count)
	{
		if (count <= 0)
		{
			return 0;
		}
		if (selected < 0)
		{
			return 0;
		}
		return (selected >= count) ? (count - 1) : selected;
	}

	public static void Load(Settings settings)
	{
		int result;
		if (int.TryParse(settings.Get("Network", "AutoRefreshSeconds"), out result) && result >= 0 && result <= 600)
		{
			IntervalSeconds = result;
		}
		Enabled = IntervalSeconds > 0;
	}

	public static void Save(Settings settings)
	{
		settings.Update("Network", "AutoRefreshSeconds", IntervalSeconds.ToString(), "Seconds between automatic server list refreshes while the list is open.\n; 0 disables it; manual refresh is F5 / LB.");
	}
}
