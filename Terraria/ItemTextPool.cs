using Microsoft.Xna.Framework;

namespace Terraria;

public sealed class ItemTextPool
{
	public const int MaxNumItemTexts = 4;

	public int ActiveTexts;

	public WorldView View;

	public ItemText[] ItemText;

	public ItemTextPool(WorldView ActiveView)
	{
		ActiveTexts = 0;
		View = ActiveView;
		ItemText = new ItemText[4];
	}

	public void Clear()
	{
		for (int num = 3; num >= 0; num--)
		{
			ItemText[num].Init();
		}
	}

	public void Update()
	{
		int num = 0;
		for (int num2 = 3; num2 >= 0; num2--)
		{
			if (ItemText[num2].Active != 0)
			{
				num++;
				ItemText[num2].Update(num2, this);
			}
		}
		ActiveTexts = num;
	}

	public void NewText(ref Item NewItem, int ItemStack)
	{
		if (View.Ui.InventoryMode > 0 || !View.Ui.ShowItemText || NewItem.Active == 0)
		{
			return;
		}
		int num = -1;
		for (int num2 = 3; num2 >= 0; num2--)
		{
			if (ItemText[num2].Active != 0)
			{
				if (ItemText[num2].NetID == NewItem.NetID && NewItem.PrefixType == 0)
				{
					ItemText[num2].Stack += ItemStack;
					Main.StrBuilder.Length = 0;
					Main.StrBuilder.Append(Lang.ItemName(NewItem.NetID));
					Main.StrBuilder.Append(ItemText[num2].Stack.ToStackString());
					Vector2 textSize = UI.BoldSmallFont.MeasureString(Main.StrBuilder);
					ItemText[num2].Text = Main.StrBuilder.ToString();
					ItemText[num2].TextSize = textSize;
					ItemText[num2].LifeTime = 56;
					ItemText[num2].Scale = 0f;
					ItemText[num2].Position.X = NewItem.Position.X + ((float)(int)NewItem.Width - textSize.X) * 0.5f;
					ItemText[num2].Position.Y = NewItem.Position.Y + (float)(NewItem.Height >> 2) - textSize.Y * 0.5f;
					ItemText[num2].VelocityY = -7f;
					return;
				}
			}
			else
			{
				num = num2;
			}
		}
		if (num < 0)
		{
			float num3 = Main.BottomWorld;
			for (int i = 0; i < 4; i++)
			{
				if (num3 > ItemText[i].Position.Y)
				{
					num = i;
					num3 = ItemText[i].Position.Y;
				}
			}
		}
		if (num >= 0)
		{
			string text = NewItem.AffixName();
			ItemText[num].Active = 1;
			ItemText[num].LifeTime = 56;
			ItemText[num].NetID = NewItem.NetID;
			ItemText[num].Stack = ItemStack;
			if (ItemStack > 1)
			{
				text += ItemStack.ToStackString();
			}
			ItemText[num].Text = text;
			Vector2 textSize2 = UI.MeasureString(UI.BoldSmallFont, text);
			ItemText[num].TextSize = textSize2;
			ItemText[num].Alpha = 1f;
			ItemText[num].AlphaDir = -0.01f;
			ItemText[num].Scale = 0f;
			ItemText[num].VelocityY = -7f;
			ItemText[num].Position.X = NewItem.Position.X + (float)(int)NewItem.Width * 0.5f - textSize2.X * 0.5f;
			ItemText[num].Position.Y = NewItem.Position.Y + (float)(int)NewItem.Height * 0.25f - textSize2.Y * 0.5f;
			switch ((EntityID.RarityID)NewItem.Rarity)
			{
			case EntityID.RarityID.BLUE:
				ItemText[num].Color = new Color(150, 150, 255);
				break;
			case EntityID.RarityID.GREEN:
				ItemText[num].Color = new Color(150, 255, 150);
				break;
			case EntityID.RarityID.ORANGE:
				ItemText[num].Color = new Color(255, 200, 150);
				break;
			case EntityID.RarityID.LIGHT_RED:
				ItemText[num].Color = new Color(255, 150, 150);
				break;
			case EntityID.RarityID.PINK:
				ItemText[num].Color = new Color(255, 150, 255);
				break;
			case EntityID.RarityID.GREY:
				ItemText[num].Color = new Color(130, 130, 130);
				break;
			case EntityID.RarityID.LIGHT_PURPLE:
				ItemText[num].Color = new Color(210, 160, 255);
				break;
			default:
				ItemText[num].Color = Color.White;
				break;
			}
		}
	}
}
