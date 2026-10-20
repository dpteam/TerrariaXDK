using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Hardmode;

internal class UI
{
	private const int ScrollSpeed = 4;

	public static string[] HardmodeText = new string[5] { "<i>WELCOME TO HARD MODE:</i>\nYou have successfully defeated the Wall of Flesh, and the world has permanently transformed as a result. You can now use the <i>Pwnhammer</i> to destroy demon altars found across the world.\n\nDestroying demon altars will bless the world with more and better ores, which you can use to craft new items and fight new bosses.\n\nNew environments will also appear, along with new enemies to face, but be careful, enemies have gotten a lot stronger.", "<i>WILLKOMMEN IM SCHWERMODUS:</i>\nDu hast erfolgreich die Fleischwand besiegt, und die Welt hat sich dauerhaft verändert. Du kannst nun den <i>Pwnhammer</i> verwenden, um Dämonenaltäre zu zerstören, die überall in der Welt zu finden sind.\n\nDas Zerstören von Dämonenaltären wird die Welt mit mehr und besseren Erzen segnen, die du verwenden kannst, um neue Gegenstände herzustellen und gegen neue Bosse zu kämpfen.\n\nEs werden auch neue Umgebungen erscheinen, zusammen mit neuen Feinden, denen du gegenüberstehen musst, aber sei vorsichtig, die Feinde sind jetzt viel mächtiger geworden.", "<i>BENVENUTO NELLA MODALITÀ DIFFICILE:</i>\nHai sconfitto con successo il Muro di Carne, e il mondo si è trasformato permanentemente a causa di ciò. Ora puoi utilizzare il <i>Pwnhammer</i> per distruggere gli altari demoniaci che trovi in giro per il mondo.\n\nDistruggere gli altari demoniaci benedirà il mondo con minerali più numerosi e migliori, che potrai utilizzare per creare nuovi oggetti e affrontare nuovi boss.\n\nNuovi ambienti compariranno anche, insieme a nuovi nemici da affrontare, ma attenzione, gli nemici sono diventati molto più potenti.", "<i>BIENVENUE EN MODE DIFFICILE:</i>\nVous avez réussi à vaincre le Mur de la Chair, et le monde s'est transformé de manière permanente par conséquent. Vous pouvez maintenant utiliser le <i>Pwnhammer</i> pour détruire les autels démoniaques trouvés à travers le monde.\n\nLa destruction des autels démoniaques bénira le monde avec plus de minerais et de meilleurs minerais, que vous pourrez utiliser pour fabriquer de nouveaux objets et affronter de nouveaux boss.\n\nDe nouveaux environnements apparaîtront également, ainsi que de nouveaux ennemis à affronter, mais soyez prudent, les ennemis sont devenus beaucoup plus puissants.", "<i>BIENVENIDO AL MODO DIFÍCIL:</i>\nHas derrotado con éxito a la Muralla de la Carne, y el mundo se ha transformado permanentemente como resultado. Ahora puedes usar el <i>Pwnhammer</i> para destruir altares de demonios que se encuentran en todo el mundo.\n\nDestruir altares de demonios bendecirá el mundo con minerales más valiosos y mejores, que podrás utilizar para fabricar nuevos objetos y enfrentarte a nuevos jefes.\n\nTambién aparecerán nuevos entornos, junto con nuevos enemigos a los que enfrentarte, pero ten cuidado, los enemigos se han vuelto mucho más poderosos." };

	private readonly Terraria.UI ParentUI;

	private short OffsetY;

	private static HardmodeLayout Popup;

	public int Offset
	{
		get
		{
			return OffsetY;
		}
		set
		{
			OffsetY = (short)MathHelper.Clamp(value, Popup.Block.MinOffsetY, 0f);
		}
	}

	public static UI Create(Terraria.UI parentUI)
	{
		return new UI(parentUI);
	}

	public UI(Terraria.UI parentUI)
	{
		ParentUI = parentUI;
		OffsetY = 0;
	}

	public void Update()
	{
		if (Popup.Block.IsScrollable)
		{
			if (ParentUI.IsDownButtonDown())
			{
				Offset -= 4;
			}
			else if (ParentUI.IsUpButtonDown())
			{
				Offset += 4;
			}
		}
	}

	public void Draw()
	{
		int safeAreaOffsetLeft = ParentUI.CurrentView.SafeAreaOffsetLeft;
		int safeAreaOffsetTop = ParentUI.CurrentView.SafeAreaOffsetTop;
		Popup.Draw(safeAreaOffsetLeft, safeAreaOffsetTop, OffsetY);
	}

	public void ControlDescription(StringBuilder StringBuilder)
	{
		if (Popup.Block.IsScrollable)
		{
			StringBuilder.Append(Lang.Controls(Lang.CONTROLS.SCROLL_TEXT));
			StringBuilder.Append(' ');
		}
		StringBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK_TO_GAME));
	}

	public static void GenerateCache(GraphicsDevice GraphicsDevice)
	{
		int langOption = Lang.LangOption;
		if (1 == 0)
		{
		}
		int num = langOption switch
		{
			2 => 2, 
			4 => 4, 
			3 => 3, 
			5 => 5, 
			_ => 1, 
		};
		if (1 == 0)
		{
		}
		int num2 = num;
		Popup = HardmodeLayout.SideBySideLayout(HardmodeText[--num2], (int)(500f * Main.ScreenMultiplier), Assets.HardmodeUpsell);
		Popup.GenerateCache(GraphicsDevice);
	}
}
