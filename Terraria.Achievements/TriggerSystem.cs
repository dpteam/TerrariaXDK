using System.Collections;
using Microsoft.Xna.Framework.GamerServices;

namespace Terraria.Achievements;

public class TriggerSystem
{
	private struct TriggerLink
	{
		public Trigger Trigger;

		public Achievement Achievement;
	}

	private readonly TriggerLink[] Links = new TriggerLink[39]
	{
		Link(Trigger.HighestPosition, Achievement.BewareIcharus),
		Link(Trigger.HouseGuide, Achievement.HomeSweetHome),
		Link(Trigger.HousedAllNPCs, Achievement.FamilyNight),
		Link(Trigger.LowestPosition, Achievement.GoingDown),
		Link(Trigger.AllSlimesKilled, Achievement.KingOfSlimes),
		Link(Trigger.AllBossesKilled, Achievement.Exterminator),
		Link(Trigger.UnlockedHardMode, Achievement.LookingForChallenge),
		Link(Trigger.MaxHealthAndMana, Achievement.AllPumpedUp),
		Link(Trigger.CorruptedWorld, Achievement.CorruptedSoul),
		Link(Trigger.HallowedWorld, Achievement.HallowedBeThyName),
		Link(Trigger.FirstTutorialTaskCompleted, Achievement.TerrariaStudent),
		Link(Trigger.AllTutorialTasksCompleted, Achievement.TerrariaExpert),
		Link(Trigger.KilledTheTwins, Achievement.Ophthalmologist),
		Link(Trigger.KilledSkeletronPrime, Achievement.Anthropologist),
		Link(Trigger.KilledDestroyer, Achievement.Biologist),
		Link(Trigger.Walked42KM, Achievement.MarathonRunner),
		Link(Trigger.RemovedLotsOfTiles, Achievement.LandscapeArchitect),
		Link(Trigger.KilledGoblinArmy, Achievement.DefeatedTheMob),
		Link(Trigger.Sunrise, Achievement.Survivor),
		Link(Trigger.SunriseAfterBloodMoon, Achievement.WhenTheMoonTurnsRed),
		Link(Trigger.AllVanitySlotsEquipped, Achievement.FashionModel),
		Link(Trigger.CreatedLotsOfBars, Achievement.Smelter),
		Link(Trigger.Has5Buffs, Achievement.PreparationIsEverything),
		Link(Trigger.SpawnedAllPets, Achievement.AnimalShelter),
		Link(Trigger.CollectedAllArmor, Achievement.Collector),
		Link(Trigger.UsedLotsOfAnvils, Achievement.Blacksmith),
		Link(Trigger.UsedAllCraftingStations, Achievement.ExpertCrafter),
		Link(Trigger.PlacedLotsOfWires, Achievement.Engineer),
		Link(Trigger.WentDownAndUpWithoutDyingOrWarping, Achievement.HellAndBack),
		Link(Trigger.InTheSky, Achievement.Airtime),
		Link(Trigger.BackForSeconds, Achievement.BackForSeconds),
		Link(Trigger.CouldThisBeHeaven, Achievement.CouldThisBeHeaven),
		Link(Trigger.LeapTallBuildingInSingleBound, Achievement.LeapTallBuildingInSingleBound),
		Link(Trigger.SafeFall, Achievement.SafeFall),
		Link(Trigger.Hellevator, Achievement.Hellevator),
		Link(Trigger.GoneIn60Seconds, Achievement.GoneIn60Seconds),
		Link(Trigger.OldFashioned, Achievement.OldFashioned),
		Link(Trigger.Homicidal, Achievement.Homicidal),
		Link(Trigger.MadLad, Achievement.Completionist)
	};

	private readonly bool[] Triggers = new bool[39];

	private readonly bool[] CheckTriggers = new bool[39];

	private static TriggerLink Link(Trigger Trigger, Achievement Achievement)
	{
		return new TriggerLink
		{
			Trigger = Trigger,
			Achievement = Achievement
		};
	}

	public bool CheckEnabled(Trigger Trigger)
	{
		return CheckTriggers[(int)Trigger];
	}

	public void SetState(Trigger Trigger, bool State)
	{
		Triggers[(int)Trigger] = State;
	}

	public void ReadProfile(SignedInGamer CurrentUser)
	{
		if (!CurrentUser.IsGuest)
		{
			BitArray earnedAchievements = Main.AchievementSystem.GetEarnedAchievements();
			UpdateTriggerChecks(earnedAchievements);
		}
	}

	private void UpdateTriggerChecks(BitArray EarnCheck)
	{
		for (int num = 38; num >= 0; num--)
		{
			int trigger = (int)Links[num].Trigger;
			int achievement = (int)Links[num].Achievement;
			CheckTriggers[trigger] = !EarnCheck.Get(achievement);
			Triggers[trigger] = false;
		}
	}

	public void UpdateAchievements(SignedInGamer CurrentUser)
	{
		for (int num = 38; num >= 0; num--)
		{
			int trigger = (int)Links[num].Trigger;
			Achievement achievement = Links[num].Achievement;
			if (!Main.IsTrial && CheckTriggers[trigger] && Triggers[trigger])
			{
				CheckTriggers[trigger] = false;
				Main.AchievementSystem.Award(CurrentUser, achievement);
			}
		}
	}
}
