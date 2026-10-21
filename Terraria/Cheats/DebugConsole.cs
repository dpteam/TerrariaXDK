#if DEBUG
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Terraria.Cheats;

/// <summary>
/// A developer console window: the live log plus a command line that runs cheat commands.
///
/// Why a window and not just the chat: the chat can only be opened in game, swallows what you
/// type into it as a chat line, and is not there while the game is loading or in a menu - the two
/// moments you most want to see what went wrong. This shows <c>Storage/Logs/terraria-debug.log</c>
/// as it is written and accepts the same <c>!commands</c> the chat does.
///
/// Two rules it follows, both from <c>AGENTS.md</c>:
/// <list type="bullet">
/// <item><b>It never touches the game from the UI thread.</b> A WinForms window runs its own
/// message loop on its own thread; commands are queued and executed by
/// <see cref="Drain"/> on the game thread, and log lines are only appended to a text box.</item>
/// <item><b>It does not exist in Release.</b> Whole file behind <c>#if DEBUG</c>, so the
/// executable contains neither the window, nor the strings, nor the commands.</item>
/// </list>
/// </summary>
internal static class DebugConsole
{
	private static Form _form;

	private static RichTextBox _log;

	private static TextBox _input;

	private static readonly Queue<string> Pending = new Queue<string>();

	private static readonly object Sync = new object();

	/// <summary>Line budget for the window. A long session logs tens of thousands of lines.</summary>
	private const int MaxLines = 4000;

	/// <summary>True once the window has been created. Opening twice focuses the existing one.</summary>
	public static bool IsOpen => _form != null && !_form.IsDisposed && _form.Visible;

	/// <summary>
	/// Creates and shows the window. Safe to call from the game thread: everything that touches
	/// controls marshals itself onto the UI thread.
	/// </summary>
	public static void Open()
	{
		if (_form != null && !_form.IsDisposed)
		{
			_form.Show();
			_form.Activate();
			return;
		}
		try
		{
			_form = new Form();
			_form.Text = "TerrariaXDK - debug console";
			_form.Width = 900;
			_form.Height = 520;
			_form.StartPosition = FormStartPosition.Manual;
			_form.Location = new System.Drawing.Point(40, 40);
			_form.MinimizeBox = true;
			_form.ShowInTaskbar = true;

			_log = new RichTextBox();
			_log.Dock = DockStyle.Fill;
			_log.ReadOnly = true;
			_log.BackColor = System.Drawing.Color.FromArgb(16, 18, 24);
			_log.ForeColor = System.Drawing.Color.FromArgb(210, 220, 235);
			_log.Font = new System.Drawing.Font("Consolas", 9f);
			_log.WordWrap = false;
			_log.HideSelection = false;
			// Scroll to the end only when the caret is already there, so scrolling back to read
			// something does not get yanked to the bottom by the next log line.
			_log.HideSelection = true;

			_input = new TextBox();
			_input.Dock = DockStyle.Bottom;
			_input.Font = new System.Drawing.Font("Consolas", 9f);
			_input.BackColor = System.Drawing.Color.FromArgb(24, 28, 36);
			_input.ForeColor = System.Drawing.Color.FromArgb(230, 235, 245);
			_input.KeyDown += OnInputKeyDown;

			Panel panel = new Panel();
			panel.Dock = DockStyle.Bottom;
			panel.Height = 54;
			Label label = new Label();
			label.Text = "!command  (Enter to run, e.g. !help, !debugkit, !allitems 0, !anticheat)";
			label.Dock = DockStyle.Top;
			label.ForeColor = System.Drawing.Color.FromArgb(150, 160, 180);
			label.Height = 18;
			panel.Controls.Add(_input);
			panel.Controls.Add(label);

			_form.Controls.Add(_log);
			_form.Controls.Add(panel);
			_form.FormClosed += OnFormClosed;

			Program.LogSink = Append;
			_form.Show();
			_form.Activate();
			Append("---- debug console opened (Debug build) ----");
		}
		catch (Exception ex)
		{
			// A missing desktop (headless session, no WinForms) must not take the game down.
			Program.LogWarn("DebugConsole", "could not open the console window: " + ex.Message);
			_form = null;
		}
	}

	public static void Close()
	{
		Form form = _form;
		_form = null;
		Program.LogSink = null;
		try
		{
			form?.Close();
		}
		catch
		{
		}
	}

	private static void OnFormClosed(object sender, FormClosedEventArgs e)
	{
		// Stop appending to disposed controls the moment the window goes away.
		Program.LogSink = null;
		_form = null;
		_log = null;
		_input = null;
	}

	/// <summary>
	/// Appends one log line. Called from whichever thread logged it, so it marshals itself.
	/// </summary>
	private static void Append(string line)
	{
		RichTextBox box = _log;
		if (box == null || box.IsDisposed || !box.IsHandleCreated)
		{
			return;
		}
		if (box.InvokeRequired)
		{
			try
			{
				box.BeginInvoke((Action<string>)Append, new object[1] { line });
			}
			catch
			{
				// The window is closing.
			}
			return;
		}
		try
		{
			if (box.Lines.Length > MaxLines)
			{
				// Drop the oldest half in one go: trimming per line is what makes a log window
				// stutter once it is full.
				string[] array = box.Lines;
				string[] array2 = new string[MaxLines / 2];
				Array.Copy(array, array.Length - array2.Length, array2, 0, array2.Length);
				box.Lines = array2;			}
			// Respect the scroll position: only follow the tail when the caret is at the end.
			bool flag = box.SelectionStart >= box.TextLength - 2;
			box.AppendText(line + Environment.NewLine);
			if (flag)
			{
				box.SelectionStart = box.TextLength;
				box.ScrollToCaret();
			}
		}
		catch
		{
		}
	}

	private static void OnInputKeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode != Keys.Enter)
		{
			return;
		}
		e.SuppressKeyPress = true;
		string text = (_input.Text ?? string.Empty).Trim();
		_input.Clear();
		if (text.Length == 0)
		{
			return;
		}
		Append("> " + text);
		// Queued, not executed: the game state belongs to the game thread.
		lock (Sync)
		{
			Pending.Enqueue(text);
		}
	}

	/// <summary>
	/// Runs any queued commands. Called once per frame from the game thread, so everything it
	/// touches (player, world, chat) is the same state the game sees.
	/// </summary>
	internal static void Drain()
	{
		string text;
		while (true)
		{
			lock (Sync)
			{
				if (Pending.Count == 0)
				{
					return;
				}
				text = Pending.Dequeue();
			}
			RunCommand(text);
		}
	}

	/// <summary>
	/// Runs one command line. Split from <see cref="Drain"/> so the parsing is testable without a
	/// window: this is the part that can be wrong.
	/// </summary>
	internal static void RunCommand(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string text2 = text.StartsWith("!", StringComparison.Ordinal) ? text : ("!" + text);
		string error;
		bool flag;
		try
		{
			flag = Terraria.Cheats.CheatConsole.TryExecute(text2, out error);
		}
		catch (Exception ex)
		{
			error = "command threw: " + ex.Message;
			flag = true;
		}
		if (!string.IsNullOrEmpty(error))
		{
			Append("! " + error);
		}
		if (flag)
		{
			return;
		}
		// Not a cheat command: try a host command, which is what a hosting developer wants.
		if (Terraria.Net.HostCommands.IsCommand(text2))
		{
			try
			{
				Terraria.Net.HostCommands.TryExecute(text2, null, out error);
			}
			catch (Exception ex2)
			{
				error = "host command threw: " + ex2.Message;
			}
			if (!string.IsNullOrEmpty(error))
			{
				Append("! " + error);
			}
			return;
		}
		Append("! unknown command: " + text);
	}

	/// <summary>
	/// Lines queued but not yet run. For the self-test: it asserts that a command typed into the
	/// window does not touch the game until the game thread drains it.
	/// </summary>
	internal static int PendingCount
	{
		get
		{
			lock (Sync)
			{
				return Pending.Count;
			}
		}
	}

	/// <summary>Queues a line as if it had been typed. Test hook.</summary>
	internal static void EnqueueForTests(string text)
	{
		lock (Sync)
		{
			Pending.Enqueue(text);
		}
	}
}
#endif
