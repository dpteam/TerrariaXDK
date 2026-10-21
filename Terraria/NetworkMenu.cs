using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.Input;

namespace Terraria;

/// <summary>
/// Network settings screen: everything about hosting and the server list that used to be
/// editable only by hand-editing <c>Settings.ini</c>.
///
/// Values apply on the next host/list update - the port and password are read when a world
/// starts hosting, so the screen says so instead of pretending otherwise.
/// </summary>
internal static class NetworkMenu
{
	private const int Rows = 6;

	private static int _selected;

	private static string _status;

	private static int _statusTime;

	public static void Reset()
	{
		_selected = 0;
		_status = null;
		_statusTime = 0;
	}

	/// <summary>Returns true when the screen should close.</summary>
	public static bool Update()
	{
		if (_statusTime > 0 && --_statusTime == 0)
		{
			_status = null;
		}
		// While the on-screen keyboard is open it owns the input.
		if (VirtualKeyboard.IsOpen)
		{
			return false;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Down))
		{
			_selected = (_selected + 1) % Rows;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Up))
		{
			_selected = (_selected + Rows - 1) % Rows;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Escape))
		{
			return true;
		}
		int num = RowUnderPointer();
		if (num >= 0)
		{
			_selected = num;
		}
		bool flag = KeyboardMouseInput.KeyPressed(Keys.Space) || KeyboardMouseInput.KeyPressed(Keys.Enter) || (num >= 0 && KeyboardMouseInput.LeftClicked);
		if (KeyboardMouseInput.KeyPressed(Keys.Right))
		{
			Change(_selected, 1);
			return false;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Left))
		{
			Change(_selected, -1);
			return false;
		}
		if (flag)
		{
			Activate(_selected);
		}
		return false;
	}

	/// <summary>Enter/Space: toggles a value, or opens the keyboard for a text field.</summary>
	private static void Activate(int row)
	{
		switch (row)
		{
		case 0:
			VirtualKeyboard.Open("Player name (what others see)", PlayerIdentity.Current, delegate(string text)
			{
				if (!PlayerIdentity.Rename(text))
				{
					SetStatus("Use 1-" + PlayerIdentity.MaxLength + " characters");
					return;
				}
				SetStatus("Name: " + PlayerIdentity.Current);
			});
			break;
		case 1:
			VirtualKeyboard.Open("Port (1-65535)", Terraria.Net.NetworkConfig.Port.ToString(), delegate(string text)
			{
				int result;
				if (!int.TryParse(text, out result) || result <= 0 || result > 65535)
				{
					SetStatus("Port must be 1-65535");
					return;
				}
				Terraria.Net.NetworkConfig.Port = result;
				Persist();
				SetStatus("Port: " + result + " - applies when the next world is hosted");
			});
			break;
		case 2:
			VirtualKeyboard.Open("Password (empty = anyone)", Terraria.Net.NetworkConfig.Password ?? string.Empty, delegate(string text)
			{
				Terraria.Net.NetworkConfig.Password = (text ?? string.Empty).Trim();
				Persist();
				SetStatus(string.IsNullOrEmpty(Terraria.Net.NetworkConfig.Password) ? "Password: off" : "Password: set - applies when the next world is hosted");
			});
			break;
		case 3:
			Change(row, 1);
			break;
		case 4:
			VirtualKeyboard.Open("Master server (host:port)", Terraria.Net.MasterServerClient.PrimaryEndpoint, delegate(string text)
			{
				if (!Terraria.Net.MasterServerClient.SetEndpoints(text))
				{
					SetStatus("Could not parse '" + text + "' - use host:port");
					return;
				}
				Persist();
				SetStatus("Master server: " + Terraria.Net.MasterServerClient.PrimaryEndpoint);
			});
			break;
		case 5:
			Change(row, 1);
			break;
		}
	}

	private static void Change(int row, int direction)
	{
		switch (row)
		{
		case 3:
			Terraria.Net.MasterServerClient.Publish = !Terraria.Net.MasterServerClient.Publish;
			SetStatus(Terraria.Net.MasterServerClient.Publish ? "Publishing this world while hosting" : "Not publishing");
			break;
		case 5:
			// 0, 10 .. 120 seconds.
			int[] steps = new int[13]
			{
				0, 5, 10, 15, 20, 30, 40, 50, 60, 75, 90, 105, 120
			};
			int num = 0;
			for (int i = 0; i < steps.Length; i++)
			{
				if (steps[i] == Terraria.Net.ServerListRefresh.IntervalSeconds)
				{
					num = i;
					break;
				}
			}
			num = (num + direction + steps.Length) % steps.Length;
			Terraria.Net.ServerListRefresh.IntervalSeconds = steps[num];
			Terraria.Net.ServerListRefresh.Enabled = steps[num] > 0;
			SetStatus(steps[num] > 0 ? ("Auto refresh: " + steps[num] + " s") : "Auto refresh: off");
			break;
		default:
			return;
		}
		Main.PlaySound(12);
		Persist();
	}

	private static void SetStatus(string text)
	{
		_status = text;
		_statusTime = 300;
	}

	private static void Persist()
	{
		try
		{
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
			settings.Update("Network", "Port", Terraria.Net.NetworkConfig.Port.ToString(), "TCP port used when this game hosts a multiplayer world in-process.");
			settings.Update("Network", "Password", Terraria.Net.NetworkConfig.Password ?? string.Empty, "Password clients must send to join this host.\n; Leave empty to accept anyone.");
			Terraria.Net.MasterServerClient.Save(settings);
			Terraria.Net.ServerListRefresh.Save(settings);
			settings.Close();
		}
		catch (Exception ex)
		{
			Program.LogWarn("NetworkMenu", "could not save the network settings: " + ex.Message);
		}
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
		int num2 = num / (int)(44f * Main.ScreenMultiplier);
		if (num2 < 0 || num2 >= Rows)
		{
			return -1;
		}
		return num2;
	}

	private static bool TryGetLayout(out Rectangle rows)
	{
		rows = default;
		UI ui = UI.MainUI;
		if (ui == null || ui.CurrentView == null)
		{
			return false;
		}
		int num = (int)(520f * Main.ScreenMultiplier);
		int num2 = (int)(130f * Main.ScreenMultiplier);
		int num3 = (ui.CurrentView.ViewWidth - num) / 2;
		rows = new Rectangle(num3, num2, num, Rows * (int)(44f * Main.ScreenMultiplier));
		return true;
	}

	public static void Draw()
	{
		Rectangle rectangle;
		if (!TryGetLayout(out rectangle))
		{
			return;
		}
		float num = Main.ScreenMultiplier;
		Main.DrawSolidRect(new Rectangle(0, 0, Main.ResolutionWidth, Main.ResolutionHeight), new Color(0, 0, 0, 170));
		Main.DrawSolidRect(new Rectangle(rectangle.X - (int)(10f * num), rectangle.Y - (int)(46f * num), rectangle.Width + (int)(20f * num), rectangle.Height + (int)(96f * num)), new Color(14, 18, 26, 240));
		UI.DrawStringCC(UI.BigFont, "Network", rectangle.Center.X, rectangle.Y - (int)(34f * num), Color.White);
		string password = Terraria.Net.NetworkConfig.Password ?? string.Empty;
		string[] array = new string[Rows]
		{
			"Player name: " + PlayerIdentity.Current,
			"Port: " + Terraria.Net.NetworkConfig.Port,
			"Password: " + (password.Length == 0 ? "off" : new string('*', Math.Min(password.Length, 12))),
			"Publish to master: " + (Terraria.Net.MasterServerClient.Publish ? "On" : "Off"),
			"Master server: " + Terraria.Net.MasterServerClient.PrimaryEndpoint,
			"Auto refresh: " + ((Terraria.Net.ServerListRefresh.IntervalSeconds > 0) ? (Terraria.Net.ServerListRefresh.IntervalSeconds + " s") : "off")
		};
		for (int i = 0; i < Rows; i++)
		{
			int y = rectangle.Y + i * (int)(44f * num);
			if (i == _selected)
			{
				Main.DrawSolidRect(new Rectangle(rectangle.X, y, rectangle.Width, (int)(44f * num) - 2), new Color(40, 70, 120, 220));
			}
			UI.DrawStringLC(UI.BoldSmallFont, array[i], rectangle.X + (int)(10f * num), y + (int)(12f * num), Color.White);
		}
		int num2 = rectangle.Y + rectangle.Height + (int)(8f * num);
		string text = _status ?? "LMB / Space - change    Esc - back";
		UI.DrawStringCC(UI.SmallFont, text, rectangle.Center.X, num2, new Color(150, 160, 180));
		UI.DrawStringCC(UI.SmallFont, "Port and password apply when the next world is hosted", rectangle.Center.X, num2 + (int)(18f * num), new Color(120, 130, 150));
	}
}
