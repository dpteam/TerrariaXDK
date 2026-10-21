using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria.HowToPlay;

namespace Terraria.Leaderboards;

internal class LeaderboardsUI
{
	private enum LeaderboardMode
	{
		Overall,
		MyScore,
		Friends
	}

	public const uint MaxSupportedNum = 99999999u;

	public string MaxSupportedNumStr = 99999999u.ToString();

	private const int RowSpacing = 5;

	private static readonly int RowHeight = (int)(50f * Main.ScreenMultiplier);

	private static readonly int RowWidth = (int)(900f * Main.ScreenMultiplier);

	private static readonly int BackgroundX = (int)(20f * Main.ScreenMultiplier);

	private static readonly int RankX = (int)(80f * Main.ScreenMultiplier);

	private static readonly int GamertagX = (int)(250f * Main.ScreenMultiplier);

	private static readonly int StatsX = (int)(375f * Main.ScreenMultiplier);

	private static readonly int StatsWidth = (int)(550f * Main.ScreenMultiplier);

	private static readonly int MaxRowsPerScreen = 7;

	private const Leaderboard DefLeaderboard = Leaderboard.DISTANCE;

	private static Color RowColour = UI.DefaultDialogColor;

	private static Color SelectedRowColour = Color.White;

	private readonly BoxGraphic Box;

	private readonly LeaderboardData Data;

	private readonly UI ParentUI;

	private int UIDelay;

	private LeaderboardMode Mode;

	private Leaderboard SelectedLeaderboard;

	private int[] StatsPositions;

	private int[] IconPositions;

	private Texture2D[] IconIndices;

	public LeaderboardsUI(UI ParentUI)
	{
		Data = new LeaderboardData(MaxRowsPerScreen);
		Box = BoxGraphic.Create(RowWidth, RowHeight, Terraria.HowToPlay.Assets.TextBG, 8, RowColour);
		UIDelay = 0;
		this.ParentUI = ParentUI;
		SelectedLeaderboard = Leaderboard.DISTANCE;
	}

	public void InitializeData()
	{
		Mode = LeaderboardMode.Overall;
		SelectedLeaderboard = Leaderboard.DISTANCE;
		LoadLeaderboard();
	}

	private void CalculateStatisticPositions()
	{
		float x = UI.MeasureString(UI.BoldSmallFont, MaxSupportedNumStr).X;
		Column[] columns = Data.Columns;
		int num = columns.Length;
		float num2 = ((float)(StatsWidth / num) - x) * 0.5f;
		StatsPositions = new int[num];
		IconIndices = new Texture2D[num];
		IconPositions = new int[num];
		float num3 = StatsX;
		for (int i = 0; i < num; i++)
		{
			StatsPositions[i] = (int)(num3 + num2 + x * 0.5f);
			num3 += x + num2 * 2f;
			Column column = columns[i];
			IconIndices[i] = Assets.ColumnIcons[(int)column];
			IconPositions[i] = StatsPositions[i] - ((int)((float)IconIndices[i].Width * Main.ScreenMultiplier) >> 1);
		}
	}

	public void Update()
	{
		// Xbox Live is gone; LeaderboardData falls back to the master server's table, and only
		// fails (returns false) when neither service has an answer.
		if (!Data.Update())
		{
			MessageBox.Show(ParentUI.controller, Lang.MenuText[5], Lang.InterfaceText[36], new string[1] { Lang.MenuText[90] });
			ParentUI.PrevMenu();
		}
		else if (UIDelay > 0)
		{
			UIDelay--;
		}
		else if (Data.Ready)
		{
			bool flag = false;
			if (ParentUI.IsDownButtonDown())
			{
				flag = Data.MoveDown();
			}
			else if (ParentUI.IsUpButtonDown())
			{
				flag = Data.MoveUp();
			}
			else if (ParentUI.IsButtonTriggered(Buttons.X) && !ParentUI.SignedInGamer.IsGuest)
			{
				SwitchLeaderboardFilter();
			}
			else if (ParentUI.IsButtonTriggered(Buttons.LeftShoulder))
			{
				PreviousLeaderboard();
			}
			else if (ParentUI.IsButtonTriggered(Buttons.RightShoulder))
			{
				NextLeaderboard();
			}
			else if (ParentUI.IsButtonTriggered(Buttons.A) && ParentUI.CanViewGamerCard() && Data.NumEntries > 0)
			{
				// A row read from the master server has a name but no Gamer to open a card for.
				Gamer selectedGamer = Data.GetSelectedGamer();
				if (selectedGamer != null)
				{
					ParentUI.ShowGamerCard(selectedGamer);
				}
			}
			if (flag)
			{
				UIDelay = 12;
				Main.PlaySound(12);
			}
		}
	}

	private void NextLeaderboard()
	{
		int selectedLeaderboard = (int)SelectedLeaderboard;
		selectedLeaderboard++;
		if (selectedLeaderboard == 5)
		{
			selectedLeaderboard = 0;
		}
		SelectedLeaderboard = (Leaderboard)selectedLeaderboard;
		LoadLeaderboard();
	}

	private void PreviousLeaderboard()
	{
		int selectedLeaderboard = (int)SelectedLeaderboard;
		selectedLeaderboard--;
		if (selectedLeaderboard < 0)
		{
			selectedLeaderboard = 4;
		}
		SelectedLeaderboard = (Leaderboard)selectedLeaderboard;
		LoadLeaderboard();
	}

	private void LoadLeaderboard()
	{
		switch (Mode)
		{
		case LeaderboardMode.MyScore:
			Data.LoadLeaderboard(SelectedLeaderboard, ParentUI.SignedInGamer);
			break;
		case LeaderboardMode.Friends:
		{
			SignedInGamer signedInGamer = ParentUI.SignedInGamer;
			FriendCollection friends = signedInGamer.GetFriends();
			Data.LoadLeaderboard(SelectedLeaderboard, friends, signedInGamer);
			break;
		}
		default:
			Data.LoadLeaderboard(SelectedLeaderboard);
			break;
		}
		CalculateStatisticPositions();
	}

	private void SwitchLeaderboardFilter()
	{
		if (Mode == LeaderboardMode.Overall)
		{
			Mode = LeaderboardMode.MyScore;
		}
		else if (Mode == LeaderboardMode.MyScore)
		{
			Mode = LeaderboardMode.Friends;
		}
		else if (Mode == LeaderboardMode.Friends)
		{
			Mode = LeaderboardMode.Overall;
		}
		LoadLeaderboard();
	}

	public void Draw(WorldView View)
	{
		SpriteFont boldSmallFont = UI.BoldSmallFont;
		int safeAreaOffsetLeft = View.SafeAreaOffsetLeft;
		int safeAreaOffsetTop = View.SafeAreaOffsetTop;
		Main.StrBuilder.Length = 0;
		Main.StrBuilder.Append(Lang.InterfaceText[37]);
		Main.StrBuilder.Append(Data.NumEntries);
		Main.StrBuilder.Append('\n');
		Main.StrBuilder.Append(Data.LeaderboardName);
		UI.DrawStringLT(boldSmallFont, safeAreaOffsetLeft, safeAreaOffsetTop, Color.White);
		Vector2 vector = new Vector2(safeAreaOffsetLeft, (float)safeAreaOffsetTop + 10f * Main.ScreenMultiplier);
		for (int i = 0; i < IconIndices.Length; i++)
		{
			vector.X = IconPositions[i];
			Main.SpriteBatch.Draw(IconIndices[i], vector, null, Color.White, 0f, default, Main.ScreenMultiplier, SpriteEffects.None, 0f);
		}
		vector.Y = (float)View.SafeAreaOffsetTop + 95f * Main.ScreenMultiplier;
		LeaderboardData.Row[] rows = Data.GetRows();
		for (int j = 0; j < rows.Length; j++)
		{
			Color color = ((j + Data.BatchStart == Data.Selected) ? SelectedRowColour : RowColour);
			Box.Color = color;
			Box.Draw(new Vector2i(BackgroundX, (int)(vector.Y - 25f * Main.ScreenMultiplier)), 1f);
			LeaderboardData.Row row = rows[j];
			if (row != null && row.Available)
			{
				vector.X = RankX;
				string text = row.Rank.ToStringLookup();
				Vector2 pivot = UI.MeasureString(boldSmallFont, text);
				pivot.X = (int)pivot.X >> 1;
				pivot.Y = (int)pivot.Y >> 1;
				UI.DrawStringScaled(boldSmallFont, text, vector, Color.White, pivot, 1f);
				vector.X = GamertagX;
				string gamertag = row.Gamertag;
				pivot = UI.MeasureString(boldSmallFont, gamertag);
				pivot.X = (int)pivot.X >> 1;
				pivot.Y = (int)pivot.Y >> 1;
				UI.DrawStringScaled(boldSmallFont, gamertag, vector, Color.White, pivot, 1f);
				for (int num = Math.Min(StatsPositions.Length, row.Statistics.Length) - 1; num >= 0; num--)
				{
					vector.X = StatsPositions[num];
					uint num2 = row.Statistics[num];
					string text2 = ((num2 < 99999999) ? ((int)num2).ToStringLookup() : MaxSupportedNumStr);
					pivot = UI.MeasureString(boldSmallFont, text2);
					pivot.X = (int)pivot.X >> 1;
					pivot.Y = (int)pivot.Y >> 1;
					UI.DrawStringScaled(boldSmallFont, text2, vector, Color.White, pivot, 1f);
				}
			}
			vector.Y += 55f * Main.ScreenMultiplier;
		}
	}

	public void ControlDescription(StringBuilder StrBuilder)
	{
		StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SWITCH_LEADERBOARD));
		StrBuilder.Append(' ');
		if (ParentUI.CanViewGamerCard() && Data.NumEntries > 0)
		{
			StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SHOW_GAMERCARD));
			StrBuilder.Append(' ');
		}
		StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
		if (!ParentUI.SignedInGamer.IsGuest)
		{
			StrBuilder.Append(' ');
			if (Mode == LeaderboardMode.Friends)
			{
				StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SHOW_TOP));
			}
			else if (Mode == LeaderboardMode.Overall)
			{
				StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SHOW_MYSELF));
			}
			else if (Mode == LeaderboardMode.MyScore)
			{
				StrBuilder.Append(Lang.Controls(Lang.CONTROLS.SHOW_FRIENDS));
			}
		}
	}
}
