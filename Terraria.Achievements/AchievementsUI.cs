using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Achievements;

internal class AchievementsUI
{
	private const int RowSpacing = 9;

	private static readonly int RowHeight = (int)(86f * Main.ScreenMultiplier);

	private static readonly int RowWidth = (int)(900f * Main.ScreenMultiplier);

	private static readonly int BackgroundX = (int)(20f * Main.ScreenMultiplier);

	private static readonly int ImageX = (int)(84f * Main.ScreenMultiplier);

	private static readonly int NameLabelX = (int)(126f * Main.ScreenMultiplier);

	private static readonly int DescLabelX = (int)(446f * Main.ScreenMultiplier);

	private static readonly int MaxRowsPerScreen = 4;

	private readonly AchievementData Data;

	private readonly UI ParentUI;

	private int UIDelay;

	private readonly Texture2D[] AchIcons;

	public AchievementsUI(UI ParentUI)
	{
		Data = new AchievementData(MaxRowsPerScreen);
		UIDelay = 0;
		this.ParentUI = ParentUI;
		AchIcons = Assets.Advancements;
	}

	public void InitializeData()
	{
		Data.LoadLeaderboard();
		// How many players earned each achievement lives on the master server (this port has no
		// Xbox Live). Kicked off here so the first screen already has something to show.
		Terraria.Net.StatSync.RefreshCounts();
	}

	public void Update()
	{
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
			// Cheap when the cached counts are fresh, and a background fetch otherwise.
			Terraria.Net.StatSync.RefreshCounts();
			bool flag = false;
			if (ParentUI.IsDownButtonDown())
			{
				flag = Data.MoveDown();
			}
			else if (ParentUI.IsUpButtonDown())
			{
				flag = Data.MoveUp();
			}
			if (flag)
			{
				UIDelay = 8;
				Main.PlaySound(12);
			}
		}
	}

	public void Draw(WorldView View)
	{
		UI.DrawStringCT(UI.BigFont, "Achievements", Main.ResolutionWidth / 2, View.SafeAreaOffsetTop - 16, Color.White);
		Vector2 vector = new Vector2
		{
			Y = (float)View.SafeAreaOffsetTop + 95f * Main.ScreenMultiplier
		};
		AchievementData.Row[] rows = Data.GetRows();
		for (int i = 0; i < rows.Length; i++)
		{
			int texId = ((i + Data.BatchStart == Data.Selected) ? 448 : 451);
			short num = (short)(300f * Main.ScreenMultiplier);
			short num2 = (short)(475f * Main.ScreenMultiplier);
			Main.DrawRect(texId, new Rectangle(BackgroundX + 22, (int)(vector.Y - 25f * Main.ScreenMultiplier) + 8, RowWidth - 16, RowHeight - 16), 192);
			AchievementData.Row row = rows[i];
			int num3 = row.Rank - 1;
			Texture2D texture2D = AchIcons[num3];
			if (num3 == 0)
			{
				num3 = 39;
			}
			AchievementSystem.TerrariaAchievement achievement = Data.GetAchievement((Achievement)(num3 - 1));
			bool status = achievement.GetStatus();
			if (row != null && row.Available)
			{
				Vector2 position = new Vector2(ImageX >> 1, vector.Y - 13f * Main.ScreenMultiplier);
				Main.SpriteBatch.Draw(texture2D, position, null, Color.White, 0f, default, Main.ScreenMultiplier, SpriteEffects.None, 0f);
				if (!status)
				{
					Main.SpriteBatch.Draw(texture2D, new Vector2(position.X + 2f * Main.ScreenMultiplier, position.Y + 2f * Main.ScreenMultiplier), new Rectangle(2, 2, texture2D.Width - 4, texture2D.Height - 4), new Color(32, 32, 32, 255), 0f, default, Main.ScreenMultiplier, SpriteEffects.None, 0f);
				}
				CompiledText compiledText = new CompiledText("<i>Name:</i> " + achievement.Name, num, UI.BoldSmallTextStyle);
				CompiledText compiledText2 = new CompiledText("<i>Description:</i> " + achievement.Description, num2, UI.BoldSmallTextStyle);
				string text = (status ? achievement.EarnedDate.ToLongDateString() : "Not yet earned");
				Color accentColour = (status ? new Color(64, 255, 128, 255) : new Color(255, 128, 128, 255));
				CompiledText compiledText3 = new CompiledText("<i>Earned:</i> " + text, num, UI.BoldSmallTextStyle);
				Vector2i vector2i = new Vector2i(NameLabelX, (int)vector.Y - 16);
				Vector2i vector2i2 = new Vector2i(DescLabelX - 16, (int)vector.Y - 16);
				Vector2i vector2i3 = new Vector2i(NameLabelX, (int)(vector.Y + 16f * Main.ScreenMultiplier));
				compiledText.Draw(Main.SpriteBatch, new Rectangle(vector2i.X, vector2i.Y, num, Main.ResolutionHeight), Color.White, new Color(255, 212, 64, 255));
				compiledText2.Draw(Main.SpriteBatch, new Rectangle(vector2i2.X, vector2i2.Y, num2, Main.ResolutionHeight), Color.White, new Color(255, 212, 64, 255));
				compiledText3.Draw(Main.SpriteBatch, new Rectangle(vector2i3.X, vector2i3.Y, num, Main.ResolutionHeight), Color.White, accentColour);
				// "Reported by 12 of 40 players", under the description. Absent when the master
				// server has no answer - a row that says "0 of 0" is worse than a row that stays
				// quiet, because it looks like a fact.
				string text2 = Terraria.Net.StatSync.AchievementLabel(num3 - 1);
				if (text2 != null)
				{
					CompiledText compiledText4 = new CompiledText(text2, num2, UI.BoldSmallTextStyle);
					compiledText4.Draw(Main.SpriteBatch, new Rectangle(vector2i2.X, vector2i3.Y, num2, Main.ResolutionHeight), new Color(180, 180, 180, 255), Color.White);
				}
			}
			vector.Y += 99f * Main.ScreenMultiplier;
		}
	}
}
