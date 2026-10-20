using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace Terraria;

public static class WorldSelect
{
	private const int MAX_WORLDS = 16;

	private static bool SecondPage = false;

	private static byte WorldCap = 8;

	private static byte selectedWorld;

	private static byte selectedSession;

	private static byte sessionListTop;

	public static bool isLocalWorld;

	public static string worldPathName;

	private static string[] worldNames = new string[16];

	private static int cursorX = 0;

	private static JoinableSession session;

	public static void Update()
	{
		if (WorldCap != 11 && Main.ScreenHeightPtr == ScreenHeights.HD)
		{
			WorldCap = 11;
		}
		else if (WorldCap != 16 && Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			WorldCap = 16;
		}
		if (Main.ScreenHeightPtr != ScreenHeights.FHD && UI.MainUI.IsButtonTriggered(Buttons.LeftTrigger))
		{
			Main.PlaySound(12);
			SecondPage = !SecondPage;
			if (cursorX == 0)
			{
				if (selectedWorld >= WorldCap)
				{
					selectedWorld -= WorldCap;
				}
				else if (selectedWorld <= WorldCap)
				{
					selectedWorld += WorldCap;
					if (selectedWorld >= 16)
					{
						selectedWorld = 15;
					}
				}
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
			dy += selectedWorld;
			int num = (SecondPage ? (16 - WorldCap) : WorldCap);
			if (dy < (SecondPage ? WorldCap : 0))
			{
				dy += num;
			}
			else if (dy >= (SecondPage ? 16 : WorldCap))
			{
				dy -= num;
			}
			selectedWorld = (byte)dy;
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
		if (dy < sessionListTop)
		{
			sessionListTop = (byte)dy;
		}
		else if (dy > sessionListTop + 15)
		{
			sessionListTop = (byte)(sessionListTop + (dy - (sessionListTop + 15)));
		}
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
		int num3 = (SecondPage ? WorldCap : 0);
		int num4 = (SecondPage ? 16 : WorldCap);
		int num5 = (SecondPage ? (16 - WorldCap) : WorldCap);
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
			string s = ((!Netplay.IsFindingSessions()) ? Lang.MenuText[77] : Lang.MenuText[76]);
			UI.DrawStringCC(UI.SmallFont, s, destRect.Center.X, destRect.Center.Y, UI.mouseTextColor);
		}
		else
		{
			destRect.X += 8;
			destRect.Y += 80;
			destRect.Width = width2;
			destRect.Height = height2;
			int num7 = Math.Min(count, sessionListTop + 16);
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
					destRect.Y += num;
				}
				destRect.Y -= num * Math.Min(count, 16);
				white = Color.White;
				for (int j = sessionListTop; j < num7; j++)
				{
					JoinableSession joinableSession = Netplay.availableSessions[j];
					string s2 = (j + 1).ToStringLookup() + ".";
					UI.DrawStringLC(UI.BoldSmallFont, s2, destRect.X, destRect.Center.Y, white);
					UI.DrawStringLC(UI.BoldSmallFont, joinableSession.HostTag, destRect.X + 32, destRect.Center.Y, white);
					s2 = Lang.MenuText[78] + joinableSession.PlayerCount.ToStringLookup() + "/8";
					UI.DrawStringRC(UI.BoldSmallFont, s2, destRect.Right, destRect.Center.Y, white);
					destRect.Y += num;
				}
			}
		}
		destRect.X = view.SafeAreaOffsetLeft + 8;
		destRect.Y = view.SafeAreaOffsetTop + 80;
		destRect.Width = width2;
		destRect.Height = height2;
		for (int k = num3; k < num4; k++)
		{
			int texId2 = 450;
			int alpha2 = 255;
			if (k == selectedWorld && cursorX == 0)
			{
				alpha2 = UI.MouseTextBrightness;
				texId2 = 448;
			}
			else if (worldNames[k] == null)
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
			string s3 = worldNames[l] ?? Lang.MenuText[79];
			white = ((worldNames[l] == null) ? new Color(200, 200, 220, 255) : new Color(255, 255, 255, 255));
			UI.DrawStringCC(UI.BoldSmallFont, s3, destRect.Center.X, destRect.Center.Y, white);
			destRect.Y += num;
		}
	}

	public static void ControlDescription(StringBuilder strBuilder)
	{
		if (cursorX == 0)
		{
			if (worldNames[selectedWorld] == null)
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
			if (worldNames[selectedWorld] != null)
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
		if (Main.ScreenHeightPtr != ScreenHeights.FHD)
		{
			Lang.CONTROLS cONTROLS = Lang.CONTROLS.NEXT_PAGE;
			if (SecondPage)
			{
				cONTROLS++;
			}
			strBuilder.Append("\u0080" + Lang.Controls(cONTROLS).Substring(1));
		}
	}

	public static string WorldName()
	{
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
			worldNames[selectedWorld] = name;
		}
		Main.WorldName = name;
		WorldGen.CreateNewWorld();
	}

	public static bool LoadWorlds()
	{
		if (!UI.MainUI.HasPlayerStorage())
		{
			worldPathName = null;
			for (int i = 0; i < 16; i++)
			{
				worldNames[i] = null;
			}
			return true;
		}
		bool result = true;
		try
		{
			using StorageContainer storageContainer = UI.MainUI.OpenPlayerStorage("Worlds");
			for (int j = 0; j < 16; j++)
			{
				string file = "world" + j.ToStringLookup() + ".wld";
				if (storageContainer.FileExists(file))
				{
					try
					{
						using Stream input = storageContainer.OpenFile(file, FileMode.Open);
						using BinaryReader binaryReader = new BinaryReader(input);
						if (binaryReader.ReadInt32() > 46)
						{
							binaryReader.ReadUInt32();
						}
						worldNames[j] = binaryReader.ReadString();
						binaryReader.Close();
					}
					catch
					{
						Main.ShowSaveIcon();
						storageContainer.DeleteFile(file);
						Main.HideSaveIcon();
						worldNames[j] = null;
						result = false;
					}
				}
				else
				{
					worldNames[j] = null;
				}
			}
			return result;
		}
		catch (IOException)
		{
			UI.MainUI.ReadError();
			for (int k = 0; k < 16; k++)
			{
				worldNames[k] = null;
			}
			return true;
		}
		catch (Exception)
		{
			return true;
		}
	}

	private static void DeleteWorld()
	{
		if (worldNames[selectedWorld] != null)
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
		worldNames[selectedWorld] = null;
	}

	public static AvailableNetworkSession Session()
	{
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
}
