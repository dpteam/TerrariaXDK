using System.Collections.Generic;

namespace Terraria.Achievements;

public class AchievementDetails
{
	public List<AchievementSystem.TerrariaAchievement> Details = new List<AchievementSystem.TerrariaAchievement>();

	public AchievementDetails()
	{
		Details.Add(new AchievementSystem.TerrariaAchievement("Terraria Expert", "You have completed the tutorial!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Terraria Student", "You have begun the tutorial!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Home Sweet Home", "The Guide has moved in!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("All in the Family", "Every NPC has moved in!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Rock Bottom", "You have reached the bottom of the World!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Exterminator", "You have defeated every boss!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Slimer", "You have killed every type of slime!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Challenge Accepted", "You have unlocked Hard Mode!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Maxed Out", "You have the maximum health and mana!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Corruptible", "Your world is corrupt!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Hallowed Be Thy Name", "Your world is hallowed!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Ophthalmologist", "You have defeated The Twins!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Bona Fide", "You have defeated Skeletron Prime!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Ride The Worm", "You have defeated The Destroyer!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Marathon Runner", "You have traveled over 42km on the ground!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Landscaper", "You have removed more than 10,000 blocks!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Crowd Control", "You have defeated the Goblin Army!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Survivor", "You survived the first night!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Icarus", "You have reached the top of the world!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Vanity of Vanities", "Looking good!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Pet Hoarder", "You seem to like the pets."));
		Details.Add(new AchievementSystem.TerrariaAchievement("Be Prepared", "You are ready for battle!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Airtime", "Enjoy the view."));
		Details.Add(new AchievementSystem.TerrariaAchievement("Blacksmith", "You are a master smith!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("I'm Smelting!", "You have smelted 10,000 bars of metal!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("A Knight in Shining Armors", "Obtain every type of armor available."));
		Details.Add(new AchievementSystem.TerrariaAchievement("Engineer", "You have placed 100 wires!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Red Moon Rises", "You have survived the Blood Moon!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Crafty", "You have used every crafting station!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("To Hell and Back", "You have gone to The Underworld and back without dying!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Back for Seconds", "You have defeated all the prime bosses twice!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Is This Heaven?", "You have found a floating island!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Superman Jump", "Jump really, really high."));
		Details.Add(new AchievementSystem.TerrariaAchievement("Safe Fall", "You have landed safely."));
		Details.Add(new AchievementSystem.TerrariaAchievement("Hellevator", "Go from the surface to The Underworld in under a minute."));
		Details.Add(new AchievementSystem.TerrariaAchievement("Gone in 60 seconds", "You ran continuously for 60 seconds!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Appease the Volcano Gods", "You sacrificed The Guide in boiling hot magma!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Homicidal", "You killed The Guide, you maniac!"));
		Details.Add(new AchievementSystem.TerrariaAchievement("Completionist", "All accomplishments have been unlocked!"));
	}
}
