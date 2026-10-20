using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.CreateCharacter;

internal class Assets
{
	public const int HairTypes = 36;

	public static Texture2D CategoryContainer;

	public static Texture2D SelectedCategoryContainer;

	public static Texture2D HairContainer;

	public static Texture2D SelectedHairContainer;

	public static Texture2D Palette;

	public static Texture2D SelectedColorContainer;

	public static Texture2D MaleIcon;

	public static Texture2D FemaleIcon;

	public static Texture2D SoftcoreIcon;

	public static Texture2D MediumcoreIcon;

	public static Texture2D HardcoreIcon;

	public static Texture2D SelectedHorizontalContainer;

	public static Texture2D HorizontalContainer;

	public static Texture2D Frame;

	public static Texture2D[] CategoryIcons;

	public static Rectangle[] Sources;

	public static void LoadContent(ContentManager Content)
	{
		CategoryContainer = Content.Load<Texture2D>("UI/CharacterCreation/Tile01");
		SelectedCategoryContainer = Content.Load<Texture2D>("UI/CharacterCreation/Tile01_Bigger");
		CategoryIcons = new Texture2D[10]
		{
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Gender"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Hair"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_HairColor"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Eyes"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Skin"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Shirt"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Undershirt"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Pants"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Shoes"),
			Content.Load<Texture2D>("UI/CharacterCreation/Category_Difficulty")
		};
		Sources = new Rectangle[36];
		for (int i = 0; i < 36; i++)
		{
			ref Rectangle reference = ref Sources[i];
			reference = new Rectangle(0, 4, 40, 30);
		}
		ref Rectangle reference2 = ref Sources[2];
		reference2 = new Rectangle(0, 6, 40, 40);
		ref Rectangle reference3 = ref Sources[8];
		reference3 = new Rectangle(4, 4, 40, 30);
		ref Rectangle reference4 = ref Sources[9];
		reference4 = new Rectangle(0, 4, 35, 30);
		ref Rectangle reference5 = ref Sources[10];
		reference5 = new Rectangle(2, 4, 40, 28);
		ref Rectangle reference6 = ref Sources[16];
		reference6 = new Rectangle(2, 0, 30, 28);
		ref Rectangle reference7 = ref Sources[17];
		reference7 = new Rectangle(0, 6, 40, 32);
		ref Rectangle reference8 = ref Sources[23];
		reference8 = new Rectangle(0, 0, 40, 28);
		ref Rectangle reference9 = ref Sources[26];
		reference9 = new Rectangle(0, 4, 35, 30);
		SelectedHairContainer = Content.Load<Texture2D>("UI/CharacterCreation/Hair_Selector");
		HairContainer = Content.Load<Texture2D>("UI/CharacterCreation/Hair_Background");
		Palette = Content.Load<Texture2D>("UI/CharacterCreation/Palette");
		SelectedColorContainer = Content.Load<Texture2D>("UI/CharacterCreation/Color_Selector");
		MaleIcon = Content.Load<Texture2D>("UI/CharacterCreation/Gender_Male");
		FemaleIcon = Content.Load<Texture2D>("UI/CharacterCreation/Gender_Female");
		SoftcoreIcon = Content.Load<Texture2D>("UI/CharacterCreation/Button_Difficulty_Soft");
		MediumcoreIcon = Content.Load<Texture2D>("UI/CharacterCreation/Button_Difficulty_Med");
		HardcoreIcon = Content.Load<Texture2D>("UI/CharacterCreation/Button_Difficulty_Hard");
		SelectedHorizontalContainer = Content.Load<Texture2D>("UI/CharacterCreation/Hair_Selector");
		HorizontalContainer = Content.Load<Texture2D>("UI/CharacterCreation/Hair_Background");
		Frame = Content.Load<Texture2D>("UI/CharacterCreation/Frame01");
	}
}
