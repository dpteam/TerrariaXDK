using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using Terraria.Storage;

namespace Terraria;

public static class WorldSelect
{
	/// <summary>
	/// Saved worlds, one entry per slot. Grown from the directory listing, not fixed at sixteen:
	/// the limit used to live in this array, so a full list simply stopped offering new rows.
	/// A null entry is an empty slot, drawn as "Create new world".
	/// </summary>
	private static string[] worldNames = new string[0];

	/// <summary>Rows on screen: every slot up to the last saved world, plus one free row.</summary>
	private static int worldRowCount = 1;

	/// <summary>Rows that fit in the panel at this resolution, recomputed while drawing.</summary>
	private static int worldVisible = 8;

	/// <summary>First visible world row.</summary>
	private static int worldScroll;

	private static byte selectedWorld;

	private static byte selectedSession;

	private static byte sessionListTop;

	/// <summary>
	/// Server cards that fit in the panel, recomputed every draw because it depends on the
	/// resolution. The cursor logic used a hardcoded 16 here, which paged the list by a number
	/// that had nothing to do with the screen.
	/// </summary>
	private static int sessionVisible = 8;

	public static bool isLocalWorld;

	/// <summary>
	/// Why the last world load failed, when the file itself explains it (truncated save,
	/// damaged header, PC-edition file). Null when the reason is unknown.
	/// </summary>
	public static string LastLoadError;

	public static string worldPathName;

	private static int cursorX = 0;

	private static JoinableSession session;

	// Geometry captured while drawing, so mouse hit-testing matches the visible list exactly.
	private static Rectangle MouseWorldArea;

	private static Rectangle MouseSessionArea;

	private static int MouseRowHeight = 1;

	/// <summary>
	/// Row height of the server list, which is twice the world row height: each server is a
	/// card with two lines. Kept apart from <see cref="MouseRowHeight"/> on purpose - the two
	/// lists live in different panels, and one shared number is how a click lands on the wrong
	/// server.
	/// </summary>
	private static int MouseSessionRowHeight = 1;

	private static int MouseWorldStart;

	private static int MouseWorldEnd;

	private static int MouseSessionStart;

	private static int MouseSessionEnd;

	/// <summary>
	/// Address of the row the player had selected when an automatic refresh started. The
	/// finder clears and refills the list, so the index alone cannot be trusted afterwards.
	/// </summary>
	private static string _selectionAddress;

	private static bool _wasSearching;

	/// <summary>
	/// Pointer support for the world list. Hovering a row selects it, clicking a world loads
	/// it, clicking a session row joins it. Returns true when the click was consumed, so the
	/// caller can skip the gamepad branch and avoid acting twice on one click.
	/// </summary>
	/// <summary>
	/// Which list row a click at this Y selects.
	///
	/// Pure and shared by both lists, because the two panels have different row heights (a server
	/// card is two lines, a world is one) and the arithmetic was duplicated at both call sites.
	/// Duplicated integer division is how a click lands on the row above.
	/// </summary>
	internal static int RowAt(int y, int areaTop, int rowStart, int rowHeight)
	{
		if (rowHeight <= 0)
		{
			return rowStart;
		}
		return rowStart + (y - areaTop) / rowHeight;
	}

	private static bool HandleMouse()
	{
		if (!Terraria.Input.KeyboardMouseInput.Enabled || !Terraria.Input.KeyboardMouseInput.CursorOverride)
		{
			return false;
		}
		int num = Terraria.Input.KeyboardMouseInput.MouseX;
		int num2 = Terraria.Input.KeyboardMouseInput.MouseY;
		bool leftClicked = Terraria.Input.KeyboardMouseInput.LeftClicked;
		if (MouseWorldArea.Width > 0 && MouseWorldArea.Contains(num, num2))
		{
			int num3 = RowAt(num2, MouseWorldArea.Y, MouseWorldStart, MouseRowHeight);
			if (num3 >= MouseWorldStart && num3 < MouseWorldEnd)
			{
				selectedWorld = (byte)num3;
				cursorX = 0;
				if (leftClicked)
				{
					SelectWorld();
					return true;
				}
			}
			// Wheel scrolls the list, like the D-pad does. Without it a mouse user in a list of
			// twenty worlds could only reach the ones on screen.
			int wheelSteps = Terraria.Input.KeyboardMouseInput.ConsumeWheelSteps();
			if (wheelSteps != 0 && worldRowCount > worldVisible)
			{
				selectedWorld = (byte)SaveSlots.Move(selectedWorld, wheelSteps, worldRowCount);
				worldScroll = SaveSlots.ClampScroll(worldScroll, selectedWorld, worldVisible, worldRowCount);
				Main.PlaySound(12);
			}
			return false;
		}
		if (MouseSessionArea.Width > 0 && MouseSessionArea.Contains(num, num2) && Netplay.availableSessions.Count > 0)
		{
			int sessionCount = Netplay.availableSessions.Count;
			int num4 = RowAt(num2, MouseSessionArea.Y, MouseSessionStart, MouseSessionRowHeight);
			if (num4 >= MouseSessionStart && num4 < MouseSessionEnd && num4 < sessionCount)
			{
				selectedSession = (byte)num4;
				cursorX = 1;
				if (leftClicked)
				{
					JoinSession();
					return true;
				}
			}
			// Wheel scrolls the server list too. With two-line cards fewer fit than before, so a
			// long list needs it at least as much as the world list does.
			int wheelSteps2 = Terraria.Input.KeyboardMouseInput.ConsumeWheelSteps();
			if (wheelSteps2 != 0 && sessionCount > sessionVisible)
			{
				selectedSession = (byte)SaveSlots.Move(selectedSession, wheelSteps2, sessionCount);
				sessionListTop = (byte)SaveSlots.ClampScroll(sessionListTop, selectedSession, sessionVisible, sessionCount);
				cursorX = 1;
				Main.PlaySound(12);
			}
			return false;
		}
		return false;
	}

	public static void Update()
	{
		// While the on-screen keyboard is open it owns all input; otherwise typing an address
		// would also re-trigger the shortcut that opened it.
		if (Terraria.Input.VirtualKeyboard.IsOpen)
		{
			return;
		}
		if (HandleMouse())
		{
			return;
		}
		// The list also refreshes itself while it is on screen: a friend who starts hosting
		// while you are watching the screen should show up without pressing anything.
		bool searching = Netplay.IsFindingSessions();
		if (searching)
		{
			_wasSearching = true;
		}
		else if (_wasSearching)
		{
			// The finder just finished: put the cursor back on the row it was on.
			_wasSearching = false;
			RestoreSelection();
		}
		selectedSession = (byte)Terraria.Net.ServerListRefresh.ClampIndex(selectedSession, Netplay.availableSessions.Count);
		if (Terraria.Net.ServerListRefresh.Tick(screenOpen: true, alreadySearching: searching))
		{
			RefreshSessions();
		}
		if (UI.MainUI.IsButtonTriggered(Buttons.LeftTrigger))
		{
			// A whole page at a time, which is what the old two-page list did. Kept as a button
			// now that the list scrolls smoothly: sixteen worlds at 1080p and forty on a small
			// screen should move the same way.
			Main.PlaySound(12);
			if (cursorX == 0)
			{
				selectedWorld = (byte)SaveSlots.Move(selectedWorld, worldVisible, worldRowCount);
			}
		}
		if (UI.MainUI.IsBackButtonTriggered())
		{
			UI.MainUI.PrevMenu();
		}
		else if (UI.MainUI.IsButtonTriggered(Buttons.Y))
		{
			UI.MainUI.ShowParty();
		}
		else if (Terraria.Input.KeyboardMouseInput.KeyPressed(Microsoft.Xna.Framework.Input.Keys.D) || UI.MainUI.IsButtonTriggered(Buttons.RightShoulder))
		{
			// Direct connect: reach servers that LAN discovery cannot see.
			AskForAddress();
		}
		else if (Terraria.Input.KeyboardMouseInput.KeyPressed(Microsoft.Xna.Framework.Input.Keys.F5) || UI.MainUI.IsButtonTriggered(Buttons.LeftShoulder))
		{
			// Refresh the list. The backoff is cleared too: a manual refresh is the user
			// saying "try again now", and waiting out the timer would look broken.
			RefreshSessions();
		}
		else if (Terraria.Input.KeyboardMouseInput.KeyPressed(Microsoft.Xna.Framework.Input.Keys.F))
		{
			ToggleFavorite();
		}
		else if (Terraria.Input.KeyboardMouseInput.KeyPressed(Microsoft.Xna.Framework.Input.Keys.B) || UI.MainUI.IsButtonTriggered(Buttons.RightStick))
		{
			// Backups are only reachable from here: no world is loaded, so a restore cannot
			// race with the game writing the same save file.
			OpenBackups();
		}
		else if (cursorX == 0)
		{
			if (UI.MainUI.IsButtonTriggered(Buttons.A))
			{
				SelectWorld();
			}
			else if (UI.MainUI.IsButtonTriggered(Buttons.X))
			{
				DeleteWorld();
			}
		}
		else if (UI.MainUI.IsButtonTriggered(Buttons.A))
		{
			JoinSession();
		}
		else if (UI.MainUI.IsButtonTriggered(Buttons.X) && UI.MainUI.CanViewGamerCard())
		{
			UI.MainUI.ShowGamerCard(Gamer.GetFromGamertag(Netplay.availableSessions[selectedSession].HostTag));
		}
	}

	public static void UpdateCursor(int dx, int dy)
	{
		if (dx != 0 || dy != 0)
		{
			Main.PlaySound(12);
		}
		if (dx > 0)
		{
			cursorX = 1;
		}
		else if (dx < 0)
		{
			cursorX = 0;
		}
		int count = Netplay.availableSessions.Count;
		if (count == 0)
		{
			selectedSession = 0;
			sessionListTop = 0;
			cursorX = 0;
		}
		if (dy == 0)
		{
			return;
		}
		if (cursorX == 0)
		{
			// The world list scrolls instead of paging: the cursor can reach every slot, and the
			// window follows it. It used to be two pages whose size depended on the resolution.
			selectedWorld = (byte)SaveSlots.Move(selectedWorld, dy, worldRowCount);
			worldScroll = SaveSlots.ClampScroll(worldScroll, selectedWorld, worldVisible, worldRowCount);
			return;
		}
		dy += selectedSession;
		if (dy < 0)
		{
			dy += count;
		}
		else if (dy >= count)
		{
			dy -= count;
		}
		selectedSession = (byte)dy;
		sessionListTop = (byte)SaveSlots.ClampScroll(sessionListTop, dy, sessionVisible, count);
	}

	/// <summary>Name of a world row, or null for an empty slot or a row that does not exist yet.</summary>
	private static string WorldAt(int Row)
	{
		if (Row < 0 || Row >= worldNames.Length)
		{
			return null;
		}
		return worldNames[Row];
	}

	public static void Draw(WorldView view)
	{
		Rectangle destRect = default;
		Color white = Color.White;
		int num = 48;
		destRect.X = view.SafeAreaOffsetLeft;
		destRect.Y = view.SafeAreaOffsetTop;
		int width = 416;
		int height = 446;
		int num2 = 448;
		int width2 = 400;
		int height2 = 24;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			width = 560;
			height = 608;
			num2 = 592;
			width2 = 544;
			height2 = 28;
			num++;
			break;
		case ScreenHeights.FHD:
			width = 848;
			height = 932;
			num2 = 880;
			width2 = 832;
			height2 = 35;
			num = 53;
			break;
		}
		// How many world rows fit under the header, and which one is at the top. The list used to
		// be paged at 8/11/16 rows depending on the resolution; now it is "however many fit".
		worldVisible = SaveSlots.VisibleRows(height - 80, num);
		worldScroll = SaveSlots.ClampScroll(worldScroll, selectedWorld, worldVisible, worldRowCount);
		int num3 = worldScroll;
		int num4 = Math.Min(worldScroll + worldVisible, worldRowCount);
		int num5 = num4 - num3;
		int num6 = num * num5;
		destRect.Width = width;
		destRect.Height = height;
		Main.DrawRect(451, destRect, 64);
		destRect.X += num2;
		Main.DrawRect(451, destRect, 64);
		UI.DrawStringCC(UI.BigFont, Lang.MenuText[83], view.SafeAreaOffsetLeft + (destRect.Width >> 1), destRect.Y + 32, white);
		UI.DrawStringCC(UI.BigFont, Lang.MenuText[84], destRect.Center.X, destRect.Y + 32, white);
		int count = Netplay.availableSessions.Count;
		if (count == 0)
		{
			MouseSessionArea = default;
			MouseSessionRowHeight = num;
			string s = ((!Netplay.IsFindingSessions()) ? Lang.MenuText[77] : Lang.MenuText[76]);
			UI.DrawStringCC(UI.SmallFont, s, destRect.Center.X, destRect.Center.Y, UI.mouseTextColor);
		}
		else
		{
			destRect.X += 8;
			destRect.Y += 80;
			destRect.Width = width2;
			destRect.Height = height2;
			// Each server is a card: two lines, so half as many fit as worlds do. The height is
			// its own number rather than `num`, because the two lists sit in different panels and
			// sharing one row height is how a hit-test ends up pointing at the wrong server.
			int cardHeight = num * 2;
			MouseSessionRowHeight = cardHeight;
			sessionVisible = Math.Max(1, SaveSlots.VisibleRows(height - 80, cardHeight));
			sessionListTop = (byte)SaveSlots.ClampScroll(sessionListTop, selectedSession, sessionVisible, count);
			int num7 = Math.Min(count, sessionListTop + sessionVisible);
			MouseSessionStart = sessionListTop;
			MouseSessionEnd = num7;
			MouseSessionArea = new Rectangle(destRect.X, destRect.Y, width2, cardHeight * Math.Max(0, num7 - sessionListTop));
			int cardTop = destRect.Y;
			lock (Netplay.availableSessions)
			{
				for (int i = sessionListTop; i < num7; i++)
				{
					int texId = 450;
					int alpha = 255;
					if (i == selectedSession && cursorX == 1)
					{
						alpha = UI.MouseTextBrightness;
						texId = 448;
					}
					Main.DrawRect(texId, destRect, alpha);
					destRect.Y += cardHeight;
				}
				destRect.Y = cardTop;
				white = Color.White;
				for (int j = sessionListTop; j < num7; j++)
				{
					JoinableSession joinableSession = Netplay.availableSessions[j];
					string address = Terraria.Net.ServerFavorites.AddressOf(joinableSession.AvailableSession.EndPoint);
					bool favorite = address != null && Terraria.Net.ServerFavorites.Contains(address);
					bool timedOut;
					string pingLabel = Terraria.Net.ServerPing.Label(joinableSession.AvailableSession.EndPoint, out timedOut);
					ServerCard.Card card = ServerCard.Build(j, joinableSession, address, favorite, pingLabel);
					Color color = card.Incompatible ? new Color(235, 130, 120) : white;
					int maxPlayers2 = joinableSession.AvailableSession.OpenPublicGamerSlots + joinableSession.PlayerCount;
					string players = Lang.MenuText[78] + joinableSession.PlayerCount.ToStringLookup() + "/"
						+ Math.Max(1, maxPlayers2).ToStringLookup();
					// Line one: the name on the left, ping and fill on the right. Two lines means
					// the right side has room for both without crowding the name.
					int titleY = destRect.Y + (num >> 1);
					UI.DrawStringLC(UI.BoldSmallFont, card.Title, destRect.X, titleY, color);
					string right = (pingLabel.Length != 0) ? (pingLabel + "   " + players) : players;
					Color pingColor = timedOut ? new Color(235, 130, 120) : color;
					UI.DrawStringRC(UI.BoldSmallFont, right, destRect.Right, titleY, pingColor);
					// Line two: address and caveats, dimmer than the name so the eye lands on who
					// it is first and reads the caveats second.
					if (card.Detail.Length != 0)
					{
						UI.DrawStringLC(UI.SmallFont, card.Detail, destRect.X, destRect.Y + num + (num >> 1),
							card.Incompatible ? color : new Color(200, 200, 220));
					}
					destRect.Y += cardHeight;
				}
			}
		}
		destRect.X = view.SafeAreaOffsetLeft + 8;
		destRect.Y = view.SafeAreaOffsetTop + 80;
		destRect.Width = width2;
		destRect.Height = height2;
		MouseWorldStart = num3;
		MouseWorldEnd = num4;
		MouseRowHeight = num;
		MouseWorldArea = new Rectangle(destRect.X, destRect.Y, width2, num * Math.Max(0, num4 - num3));
		for (int k = num3; k < num4; k++)
		{
			int texId2 = 450;
			int alpha2 = 255;
			if (k == selectedWorld && cursorX == 0)
			{
				alpha2 = UI.MouseTextBrightness;
				texId2 = 448;
			}
			else if (WorldAt(k) == null)
			{
				alpha2 = 212;
				texId2 = 451;
			}
			Main.DrawRect(texId2, destRect, alpha2);
			destRect.Y += num;
		}
		destRect.Y -= num6;
		for (int l = num3; l < num4; l++)
		{
			string s3 = WorldAt(l) ?? Lang.MenuText[79];
			white = ((WorldAt(l) == null) ? new Color(200, 200, 220, 255) : new Color(255, 255, 255, 255));
			UI.DrawStringCC(UI.BoldSmallFont, s3, destRect.Center.X, destRect.Center.Y, white);
			destRect.Y += num;
		}
	}

	public static void ControlDescription(StringBuilder strBuilder)
	{
		string text = WorldName();
		if (cursorX == 0)
		{
			if (string.IsNullOrEmpty(text))
			{
				strBuilder.Append(Lang.Controls(Lang.CONTROLS.CREATE_WORLD));
			}
			else
			{
				strBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT));
			}
		}
		else
		{
			strBuilder.Append(Lang.Controls(Lang.CONTROLS.JOIN));
		}
		strBuilder.Append(' ');
		strBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
		strBuilder.Append(' ');
		if (cursorX == 0)
		{
			if (!string.IsNullOrEmpty(text))
			{
				strBuilder.Append(Lang.MenuText[17]);
				strBuilder.Append(' ');
			}
		}
		else if (UI.MainUI.CanViewGamerCard())
		{
			strBuilder.Append(Lang.Controls(Lang.CONTROLS.X_SHOW_GAMERCARD));
			strBuilder.Append(' ');
		}
		if (UI.MainUI.CanPlayOnline())
		{
			strBuilder.Append(Lang.Controls(Lang.CONTROLS.SHOW_PARTY));
			strBuilder.Append(' ');
		}
		// The list scrolls now, so a counter is the only way to tell "this is all of them" from
		// "there is more below". The old screen flipped pages, and the hint said so.
		if (cursorX == 0 && SaveSlots.NeedsScroll(worldRowCount, worldVisible))
		{
			strBuilder.Append(Texts.Format("ui.page.counter", worldScroll + worldVisible, worldRowCount));
		}
	}

	/// <summary>Name of the highlighted world, or null when the row is an empty slot.</summary>
	public static string WorldName()
	{
		if (selectedWorld < 0 || selectedWorld >= worldNames.Length)
		{
			return null;
		}
		return worldNames[selectedWorld];
	}

	private static void SelectWorld()
	{
		isLocalWorld = true;
		Main.PlaySound(10);
		worldPathName = "world" + selectedWorld + ".wld";
		UI.MainUI.SetMenu(MenuMode.GAME_MODE);
	}

	public static void CreateWorld(string name)
	{
		isLocalWorld = true;
		if (UI.MainUI.HasPlayerStorage())
		{
			if (selectedWorld >= worldNames.Length)
			{
				Array.Resize(ref worldNames, selectedWorld + 1);
			}
			worldNames[selectedWorld] = name;
			EnsureWorldRows();
		}
		Main.WorldName = name;
		WorldGen.CreateNewWorld();
	}

	public static bool LoadWorlds()
	{
		if (!UI.MainUI.HasPlayerStorage())
		{
			worldPathName = null;
			worldNames = new string[0];
			EnsureWorldRows();
			return true;
		}
		bool result = true;
		try
		{
			using StorageContainer storageContainer = UI.MainUI.OpenPlayerStorage("Worlds");
			// "world*.wld", not a loop over 0..15: the loop was the limit, and a file with a
			// two-digit number was invisible to it.
			string[] fileNames = storageContainer.GetFileNames("world*.wld");
			int num = SaveSlots.SlotCount(fileNames, "world", ".wld");
			worldNames = new string[num];
			for (int i = 0; i < fileNames.Length; i++)
			{
				int num2;
				if (!SaveSlots.TryParseIndex(fileNames[i], "world", ".wld", out num2))
				{
					continue;
				}
				if (num2 >= worldNames.Length)
				{
					Array.Resize(ref worldNames, num2 + 1);
				}
				try
				{
					using Stream input = storageContainer.OpenFile(fileNames[i], FileMode.Open);
					using BinaryReader binaryReader = new BinaryReader(input);
					if (binaryReader.ReadInt32() > 46)
					{
						binaryReader.ReadUInt32();
					}
					worldNames[num2] = binaryReader.ReadString();
					binaryReader.Close();
				}
				catch
				{
					Main.ShowSaveIcon();
					storageContainer.DeleteFile(fileNames[i]);
					Main.HideSaveIcon();
					worldNames[num2] = null;
					result = false;
				}
			}
			EnsureWorldRows();
			return result;
		}
		catch (IOException)
		{
			UI.MainUI.ReadError();
			worldNames = new string[0];
			EnsureWorldRows();
			return true;
		}
		catch (Exception)
		{
			return true;
		}
	}

	/// <summary>
	/// Grows the row list so there is always one empty row to create into, and keeps the cursor
	/// inside it. One free row, not fifteen: the console build showed sixteen rows because it had
	/// sixteen slots, and an empty slot is a row that says "Create new world".
	/// </summary>
	private static void EnsureWorldRows()
	{
		int num = worldNames.Length;
		bool flag = false;
		for (int i = 0; i < worldNames.Length; i++)
		{
			if (string.IsNullOrEmpty(worldNames[i]))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			// Every slot is taken: one more row, so creating a world is still possible.
			num = worldNames.Length + 1;
			Array.Resize(ref worldNames, num);
		}
		worldRowCount = Math.Max(1, num);
		if (selectedWorld >= worldRowCount)
		{
			selectedWorld = (byte)(worldRowCount - 1);
		}
		worldScroll = SaveSlots.ClampScroll(worldScroll, selectedWorld, worldVisible, worldRowCount);
	}

	private static void DeleteWorld()
	{
		if (WorldAt(selectedWorld) != null)
		{
			Main.PlaySound(10);
			UI.MainUI.SetMenu(MenuMode.CONFIRM_DELETE_WORLD);
		}
	}

	public static void EraseWorld()
	{
		if (!UI.MainUI.HasPlayerStorage())
		{
			return;
		}
		Main.ShowSaveIcon();
		try
		{
			using StorageContainer storageContainer = UI.MainUI.OpenPlayerStorage("Worlds");
			worldPathName = "world" + selectedWorld + ".wld";
			storageContainer.DeleteFile(worldPathName);
		}
		catch (IOException)
		{
			UI.MainUI.WriteError();
		}
		catch (Exception)
		{
		}
		Main.HideSaveIcon();
		if (selectedWorld < worldNames.Length)
		{
			worldNames[selectedWorld] = null;
		}
		EnsureWorldRows();
	}

	/// <summary>
	/// Starts a fresh search and remembers which row the player was on, so the list does not
	/// jump back to the top when a refresh replaces the sessions.
	/// </summary>
	private static void RefreshSessions()
	{
		_selectionAddress = SelectedAddress();
		Terraria.Net.ServerListRefresh.Reset();
		// A manual refresh must not wait out the master server's backoff.
		Terraria.Net.MasterServerClient.ResetBackoff();
		Netplay.FindSessions();
	}

	/// <summary>Address of the highlighted server, or null when nothing is selected.</summary>
	private static string SelectedAddress()
	{
		if (cursorX != 1 || selectedSession >= Netplay.availableSessions.Count)
		{
			return null;
		}
		return Terraria.Net.ServerFavorites.AddressOf(Netplay.availableSessions[selectedSession].AvailableSession.EndPoint);
	}

	/// <summary>Puts the cursor back on the remembered address, if it came back in the list.</summary>
	private static void RestoreSelection()
	{
		if (string.IsNullOrEmpty(_selectionAddress))
		{
			return;
		}
		lock (Netplay.availableSessions)
		{
			for (int i = 0; i < Netplay.availableSessions.Count; i++)
			{
				if (Terraria.Net.ServerFavorites.AddressOf(Netplay.availableSessions[i].AvailableSession.EndPoint) == _selectionAddress)
				{
					selectedSession = (byte)i;
					cursorX = 1;
					return;
				}
			}
		}
		_selectionAddress = null;
	}

	/// <summary>
	/// Opens the backup list for the highlighted world. The world file name is
	/// <c>world&lt;n&gt;.wld</c> inside the player's storage container, which is also where a
	/// restore has to write.
	/// </summary>
	private static void OpenBackups()
	{
		if (cursorX != 0)
		{
			// Only a local world can be restored; server rows have nothing to back up.
			UI.MainUI.statusText = Texts.Get("ui.world.backups.pick");
			Main.PlaySound(11);
			return;
		}
		string text = WorldAt(selectedWorld);
		if (string.IsNullOrEmpty(text))
		{
			UI.MainUI.statusText = Texts.Get("ui.world.slot.empty");
			Main.PlaySound(11);
			return;
		}
		try
		{
			using StorageContainer storageContainer = UI.MainUI.OpenPlayerStorage("Worlds");
			string text2 = "world" + selectedWorld + ".wld";
			Main.PlaySound(10);
			Terraria.BackupsMenu.Open(storageContainer.FilePath(text2), text);
			UI.MainUI.SetMenu(MenuMode.BACKUPS);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Backups", "could not open the backup list: " + ex.Message);
			UI.MainUI.statusText = Texts.Get("ui.world.folder.failed");
		}
	}

	/// <summary>
	/// Saves or forgets the server currently under the cursor. Favourites survive both a
	/// failed LAN scan and an unreachable master server.
	/// </summary>
	private static void ToggleFavorite()
	{
		if (cursorX != 1 || selectedSession >= Netplay.availableSessions.Count)
		{
			return;
		}
		JoinableSession joinableSession = Netplay.availableSessions[selectedSession];
		string text = Terraria.Net.ServerFavorites.AddressOf(joinableSession.AvailableSession.EndPoint);
		if (text == null)
		{
			return;
		}
		if (Terraria.Net.ServerFavorites.Contains(text))
		{
			Terraria.Net.ServerFavorites.Remove(text);
			UI.MainUI.statusText = Texts.Format("ui.server.removed", text);
			Main.PlaySound(11);
			return;
		}
		if (Terraria.Net.ServerFavorites.Add(text))
		{
			UI.MainUI.statusText = Texts.Format("ui.server.saved", text);
			Main.PlaySound(12);
		}
	}

	public static AvailableNetworkSession Session()
	{
		if (session == null)
		{
			return null;
		}
		// Built-in transport: we already know the endpoint, no Xbox Live round trip needed.
		if (Terraria.Net.NetworkConfig.UseTcpTransport)
		{
			AvailableNetworkSession availableSession = session.AvailableSession;
			session = null;
			return availableSession;
		}
		List<SignedInGamer> list = new List<SignedInGamer>(1);
		list.Add(UI.MainUI.SignedInGamer);
		AvailableNetworkSessionCollection availableNetworkSessionCollection = NetworkSession.Find(NetworkSessionType.PlayerMatch, list, session.AvailableSession.SessionProperties);
		session = null;
		if (availableNetworkSessionCollection.Count <= 0)
		{
			return null;
		}
		return availableNetworkSessionCollection[0];
	}

	private static void JoinSession()
	{
		session = Netplay.availableSessions[selectedSession];
		Netplay.StopFindingSessions();
		UI.MainUI.SetMenu(MenuMode.NETPLAY);
		UI.MainUI.statusText = Lang.MenuText[80];
		Netplay.StartClient();
	}

	/// <summary>
	/// Manual connect: asks for "host" or "host:port" and joins it directly. LAN discovery
	/// only finds worlds on the same subnet, so this is the way to reach a friend over the
	/// internet. Typing works with a keyboard or the on-screen keyboard for gamepads.
	/// </summary>
	private static void AskForAddress()
	{
		Terraria.Input.VirtualKeyboard.Open(Texts.Get("ui.server.address"), Terraria.Net.NetworkConfig.JoinAddress, delegate(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string error;
			if (!Netplay.JoinDirect(text.Trim(), out error))
			{
				UI.Error(Texts.Get("ui.connect.title"), error);
				return;
			}
			session = Netplay.availableSessions[Netplay.availableSessions.Count - 1];
			selectedSession = (byte)(Netplay.availableSessions.Count - 1);
			cursorX = 1;
			JoinSession();
		});
	}
}
