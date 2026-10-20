using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SDL2;

namespace Microsoft.Xna.Framework.GamerServices;

public static class Guide
{
	internal class GuideAction : IAsyncResult
	{
		public readonly AsyncCallback Callback;

		public object AsyncState { get; private set; }

		public bool CompletedSynchronously => false;

		public bool IsCompleted { get; internal set; }

		public WaitHandle AsyncWaitHandle { get; private set; }

		public GuideAction(object state, AsyncCallback callback)
		{
			AsyncState = state;
			Callback = callback;
			IsCompleted = false;
			AsyncWaitHandle = new ManualResetEvent(initialState: true);
		}
	}

	private static NotificationPosition position;

	private static Form InputDialog;

	private static TaskCompletionSource<string> InputCompletion;

	private static Form MessageDialog;

	private static TaskCompletionSource<int?> MessageCompletion;

	public static bool IsScreenSaverEnabled
	{
		get
		{
			return SDL.SDL_IsScreenSaverEnabled() == SDL.SDL_bool.SDL_TRUE;
		}
		set
		{
			if (value)
			{
				SDL.SDL_EnableScreenSaver();
			}
			else
			{
				SDL.SDL_DisableScreenSaver();
			}
		}
	}

	public static bool IsTrialMode { get; set; }

	public static bool IsVisible
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public static NotificationPosition NotificationPosition
	{
		get
		{
			return position;
		}
		set
		{
			if (value != position)
			{
				position = value;
			}
		}
	}

	public static bool SimulateTrialMode { get; set; }

	static Guide()
	{
		position = NotificationPosition.BottomRight;
		IsTrialMode = false;
		SimulateTrialMode = false;
	}

	public static IAsyncResult BeginShowKeyboardInput(PlayerIndex player, string title, string description, string defaultText, AsyncCallback callback, object state)
	{
		return BeginShowKeyboardInput(player, title, description, defaultText, callback, state, usePasswordMode: false);
	}

	private static Task<string> ShowInputBox(string title, string description, string defaultText = "", bool usePasswordMode = false)
	{
		if (IsVisible)
		{
			throw new Exception("Guide is already visible; cannot execute further");
		}
		IsVisible = true;
		Task<string> result = InputShow(title, description, defaultText, usePasswordMode);
		IsVisible = false;
		return result;
	}

	public static IAsyncResult BeginShowKeyboardInput(PlayerIndex player, string title, string description, string defaultText, AsyncCallback callback, object state, bool usePasswordMode)
	{
		return ShowInputBox(title, description, defaultText, usePasswordMode);
	}

	public static string EndShowKeyboardInput(IAsyncResult result)
	{
		return ((Task<string>)result).Result;
	}

	private static Task<int?> ShowMessageBox(string title, string text, IEnumerable<string> buttons, int focusButton, MessageBoxIcon icon)
	{
		if (IsVisible)
		{
			throw new Exception("Guide is already visible; cannot execute further");
		}
		IsVisible = true;
		List<string> list = buttons.ToList();
		if (list.Count > 3 || list.Count == 0)
		{
			throw new ArgumentException("Invalid number of buttons: one to three required", "buttons");
		}
		Task<int?> result = MessageShow(title, text, list);
		IsVisible = false;
		return result;
	}

	public static IAsyncResult BeginShowMessageBox(PlayerIndex player, string title, string text, IEnumerable<string> buttons, int focusButton, MessageBoxIcon icon, AsyncCallback callback, object state)
	{
		return ShowMessageBox(title, text, buttons, focusButton, icon);
	}

	public static IAsyncResult BeginShowMessageBox(string title, string text, IEnumerable<string> buttons, int focusButton, MessageBoxIcon icon, AsyncCallback callback, object state)
	{
		return BeginShowMessageBox(PlayerIndex.One, title, text, buttons, focusButton, icon, callback, state);
	}

	public static int? EndShowMessageBox(IAsyncResult result)
	{
		return ((Task<int?>)result).Result;
	}

	public static void DelayNotifications(TimeSpan delay)
	{
	}

	public static void ShowComposeMessage(PlayerIndex player, string text, IEnumerable<Gamer> recipients)
	{
	}

	public static void ShowFriendRequest(PlayerIndex player, Gamer gamer)
	{
	}

	public static void ShowFriends(PlayerIndex player)
	{
	}

	public static void ShowGameInvite(PlayerIndex player, IEnumerable<Gamer> recipients)
	{
	}

	public static void ShowGameInvite(string sessionId)
	{
	}

	public static void ShowGamerCard(PlayerIndex player, Gamer gamer)
	{
	}

	public static void ShowMarketplace(PlayerIndex player)
	{
	}

	public static void ShowMessages(PlayerIndex player)
	{
	}

	public static void ShowParty(PlayerIndex player)
	{
	}

	public static void ShowPartySessions(PlayerIndex player)
	{
	}

	public static void ShowPlayerReview(PlayerIndex player, Gamer gamer)
	{
	}

	public static void ShowPlayers(PlayerIndex player)
	{
	}

	public static void ShowSignIn(int paneCount, bool onlineOnly)
	{
	}

	public static void ShowAchievementsEXT(AchievementCollection oi)
	{
	}

	private static Task<string> InputShow(string title, string description, string defaultText, bool usePasswordMode)
	{
		InputCompletion = new TaskCompletionSource<string>();
		Form form = (InputDialog = new Form());
		form.Text = title;
		form.FormBorderStyle = FormBorderStyle.FixedDialog;
		form.MinimizeBox = false;
		form.MaximizeBox = false;
		form.ControlBox = false;
		form.ForeColor = System.Drawing.Color.White;
		form.BackColor = System.Drawing.Color.DarkGray;
		form.StartPosition = FormStartPosition.CenterParent;
		form.AutoSizeMode = AutoSizeMode.GrowOnly;
		form.AutoSize = true;
		Label label = new Label
		{
			Text = description,
			Parent = form,
			Top = 25,
			TextAlign = ContentAlignment.MiddleCenter,
			AutoSize = true,
			BackColor = System.Drawing.Color.DarkGray,
			Font = new Font(Control.DefaultFont, FontStyle.Bold)
		};
		TextBox textBox = new TextBox
		{
			Text = defaultText,
			Parent = form,
			Top = label.Bottom + 15,
			UseSystemPasswordChar = usePasswordMode,
			AutoSize = true,
			TabIndex = 0
		};
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			FlowDirection = FlowDirection.LeftToRight,
			Parent = form,
			Top = textBox.Bottom + 15,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			BackColor = System.Drawing.Color.DarkGray
		};
		Button acceptButton = new Button
		{
			Text = "&Done",
			DialogResult = DialogResult.OK,
			Parent = flowLayoutPanel,
			TabIndex = 1,
			ForeColor = System.Drawing.Color.White,
			BackColor = System.Drawing.Color.OliveDrab,
			Font = new Font(Control.DefaultFont, FontStyle.Bold)
		};
		form.AcceptButton = acceptButton;
		acceptButton = new Button
		{
			Text = "&Cancel",
			DialogResult = DialogResult.Cancel,
			Parent = flowLayoutPanel,
			TabIndex = 2,
			ForeColor = System.Drawing.Color.White,
			BackColor = System.Drawing.Color.OliveDrab,
			Font = new Font(Control.DefaultFont, FontStyle.Bold)
		};
		form.CancelButton = acceptButton;
		form.Width += description.Length * 2;
		form.Height = label.Top + textBox.Top + flowLayoutPanel.Top;
		label.Left = (label.Parent.ClientSize.Width - label.Width) / 2;
		textBox.Width = (int)((float)textBox.Parent.ClientSize.Width * 0.75f);
		textBox.Left = (textBox.Parent.ClientSize.Width - textBox.Width) / 2;
		flowLayoutPanel.Left = (flowLayoutPanel.Parent.ClientSize.Width - flowLayoutPanel.Width) / 2;
		DialogResult dialogResult = form.ShowDialog();
		InputDialog = null;
		if (dialogResult == DialogResult.OK)
		{
			InputCompletion.SetResult(textBox.Text);
		}
		else
		{
			InputCompletion.SetResult(null);
		}
		return InputCompletion.Task;
	}

	private static Task<int?> MessageShow(string title, string description, List<string> buttons)
	{
		MessageCompletion = new TaskCompletionSource<int?>();
		Form form = (MessageDialog = new Form());
		form.Text = title;
		form.FormBorderStyle = FormBorderStyle.FixedDialog;
		form.MinimizeBox = false;
		form.MaximizeBox = false;
		form.ControlBox = false;
		form.ForeColor = System.Drawing.Color.White;
		form.BackColor = System.Drawing.Color.DarkGray;
		form.StartPosition = FormStartPosition.CenterParent;
		form.AutoSizeMode = AutoSizeMode.GrowOnly;
		form.AutoSize = true;
		Label label = new Label
		{
			Text = description,
			Parent = form,
			Top = 25,
			TextAlign = ContentAlignment.MiddleCenter,
			AutoSize = true,
			BackColor = System.Drawing.Color.DarkGray,
			Font = new Font(Control.DefaultFont, FontStyle.Bold)
		};
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			FlowDirection = FlowDirection.LeftToRight,
			Parent = form,
			Top = label.Bottom + 25,
			AutoSize = true,
			Margin = new Padding(15),
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			BackColor = System.Drawing.Color.DarkGray
		};
		for (int i = 0; i < buttons.Count; i++)
		{
			string text = buttons[i];
			Button acceptButton = new Button
			{
				Text = text,
				DialogResult = (DialogResult)(i + 1),
				Parent = flowLayoutPanel,
				TextAlign = ContentAlignment.MiddleCenter,
				AutoSize = true,
				ForeColor = System.Drawing.Color.White,
				BackColor = System.Drawing.Color.OliveDrab,
				Font = new Font(Control.DefaultFont, FontStyle.Bold)
			};
			if (i == 0)
			{
				form.AcceptButton = acceptButton;
			}
		}
		form.Width += description.Length;
		form.Height = label.Top + flowLayoutPanel.Top;
		label.Left = (label.Parent.ClientSize.Width - label.Width) / 2;
		flowLayoutPanel.Left = (flowLayoutPanel.Parent.ClientSize.Width - flowLayoutPanel.Width) / 2;
		int num = (int)form.ShowDialog();
		MessageDialog = null;
		MessageCompletion.SetResult(num - 1);
		return MessageCompletion.Task;
	}
}
