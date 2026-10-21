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
	private const int Rows = 8;

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
			VirtualKeyboard.Open(Texts.Get("ui.net.name.title"), PlayerIdentity.Current, delegate(string text)
			{
				if (!PlayerIdentity.Rename(text))
				{
					SetStatus(Texts.Format("ui.net.name.usage", PlayerIdentity.MaxLength));
					return;
				}
				SetStatus(Texts.Format("ui.net.name", PlayerIdentity.Current));
			});
			break;
		case 1:
			VirtualKeyboard.Open(Texts.Get("ui.net.port.title"), Terraria.Net.NetworkConfig.Port.ToString(), delegate(string text)
			{
				int result;
				if (!int.TryParse(text, out result) || result <= 0 || result > 65535)
				{
					SetStatus(Texts.Get("ui.net.port.invalid"));
					return;
				}
				Terraria.Net.NetworkConfig.Port = result;
				Persist();
				SetStatus(Texts.Format("ui.net.port.set", result));
			});
			break;
		case 2:
			VirtualKeyboard.Open(Texts.Get("ui.net.password.title"), Terraria.Net.NetworkConfig.Password ?? string.Empty, delegate(string text)
			{
				Terraria.Net.NetworkConfig.Password = (text ?? string.Empty).Trim();
				Persist();
				SetStatus(string.IsNullOrEmpty(Terraria.Net.NetworkConfig.Password) ? Texts.Format("ui.net.password", Texts.Get("ui.net.off")) : Texts.Get("ui.net.password.set"));
			});
			break;
		case 3:
			Change(row, 1);
			break;
		case 4:
			VirtualKeyboard.Open(Texts.Get("ui.net.master.title"), Terraria.Net.MasterServerClient.PrimaryEndpoint, delegate(string text)
			{
				if (!Terraria.Net.MasterServerClient.SetEndpoints(text))
				{
					SetStatus(Texts.Format("ui.net.master.invalid", text));
					return;
				}
				Persist();
				SetStatus(Texts.Format("ui.net.master.set", Terraria.Net.MasterServerClient.PrimaryEndpoint));
			});
			break;
		case 5:
			Change(row, 1);
			break;
		case 6:
			Change(row, 1);
			break;
		case 7:
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
			SetStatus(Terraria.Net.MasterServerClient.Publish ? Texts.Get("ui.net.publish.on") : Texts.Get("ui.net.publish.off"));
			break;
		case 6:
		{
			// 0, 8, 16, 24, 32, 48 tiles: "off" plus the sizes people actually use.
			int[] radii = new int[6] { 0, 8, 16, 24, 32, 48 };
			int num2 = 0;
			for (int j = 0; j < radii.Length; j++)
			{
				if (radii[j] == Terraria.Net.WorldProtection.SpawnRadius)
				{
					num2 = j;
					break;
				}
			}
			num2 = (num2 + direction + radii.Length) % radii.Length;
			Terraria.Net.WorldProtection.SpawnRadius = radii[num2];
			SetStatus((radii[num2] > 0) ? Texts.Format("ui.net.spawn", radii[num2]) : Texts.Get("ui.net.spawn.off"));
			Main.PlaySound(12);
			Persist();
			break;
		}
		case 7:
			Terraria.Net.WorldProtection.DisableBuild = !Terraria.Net.WorldProtection.DisableBuild;
			SetStatus(Texts.Get(Terraria.Net.WorldProtection.DisableBuild ? "ui.net.build.disabled" : "ui.net.build.allowed"));
			Main.PlaySound(12);
			Persist();
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
			SetStatus((steps[num] > 0) ? Texts.Format("ui.net.refresh", steps[num]) : Texts.Get("ui.net.refresh.off"));
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
			// Hosting keys go through ServerConfig, which writes to whichever file is actually
			// in charge. Writing straight to Settings.ini while a hosting profile overrides it
			// would look exactly like "my change did not save".
			Terraria.Net.ServerConfig.Persist("Port", Terraria.Net.NetworkConfig.Port.ToString(), "TCP port used when this game hosts a multiplayer world in-process.");
			Terraria.Net.ServerConfig.Persist("Password", Terraria.Net.NetworkConfig.Password ?? string.Empty, "Password clients must send to join this host.\n; Leave empty to accept anyone.");
			Terraria.Net.ServerConfig.Persist("MaxPlayers", Terraria.Net.NetworkConfig.MaxPlayers.ToString(), "How many remote players the host accepts (1-255).");
			Terraria.Net.ServerConfig.Persist("DisableBuild", Terraria.Net.WorldProtection.DisableBuild ? "True" : "False", "While set, only the host can change the world.");
			Terraria.Net.ServerConfig.Persist("ProtectSpawnRadius", Terraria.Net.WorldProtection.SpawnRadius.ToString(), "Tiles around spawn that remote players cannot modify (0 = off).");
			// Client-side keys stay in Settings.ini: they are about this player's game, not the
			// server's policy.
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
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
		UI.DrawStringCC(UI.BigFont, Texts.Get("ui.net.title"), rectangle.Center.X, rectangle.Y - (int)(34f * num), Color.White);
		string password = Terraria.Net.NetworkConfig.Password ?? string.Empty;
		string[] array = new string[Rows]
		{
			Texts.Format("ui.net.name.row", PlayerIdentity.Current),
			Texts.Format("ui.net.port", Terraria.Net.NetworkConfig.Port),
			Texts.Format("ui.net.password", (password.Length == 0) ? Texts.Get("ui.net.off") : new string('*', Math.Min(password.Length, 12))),
			Texts.Format("ui.net.publish", Texts.Get(Terraria.Net.MasterServerClient.Publish ? "ui.common.on" : "ui.common.off")),
			Texts.Format("ui.net.master.set", Terraria.Net.MasterServerClient.PrimaryEndpoint),
			(Terraria.Net.ServerListRefresh.IntervalSeconds > 0) ? Texts.Format("ui.net.refresh", Terraria.Net.ServerListRefresh.IntervalSeconds) : Texts.Get("ui.net.refresh.off"),
			(Terraria.Net.WorldProtection.SpawnRadius > 0) ? Texts.Format("ui.net.spawn", Terraria.Net.WorldProtection.SpawnRadius) : Texts.Get("ui.net.spawn.off"),
			Texts.Format("ui.net.build", Texts.Get(Terraria.Net.WorldProtection.DisableBuild ? "ui.common.on" : "ui.common.off"))
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
		string text = _status ?? Texts.Get("ui.common.change.back");
		UI.DrawStringCC(UI.SmallFont, text, rectangle.Center.X, num2, new Color(150, 160, 180));
		UI.DrawStringCC(UI.SmallFont, Texts.Get("ui.net.hint.scope"), rectangle.Center.X, num2 + (int)(18f * num), new Color(120, 130, 150));
	}
}
