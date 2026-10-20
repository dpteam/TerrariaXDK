using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Framework.GamerServices;

public sealed class SignedInGamer : Gamer
{
	private delegate AchievementCollection GetAchievementsDelegate();

	private GamerAction statStoreAction;

	private AchievementCollection gamerAchievements;

	public GameDefaults GameDefaults { get; private set; }

	public bool IsGuest { get; private set; }

	public bool IsSignedInToLive { get; private set; }

	public int PartySize { get; set; }

	public PlayerIndex PlayerIndex { get; private set; }

	public GamerPresence Presence { get; private set; }

	public GamerPrivileges Privileges { get; private set; }

	public static event EventHandler<SignedInEventArgs> SignedIn;

	public static event EventHandler<SignedOutEventArgs> SignedOut;

	public SignedInGamer(string gamertag, bool isSignedInToLive = false, bool isGuest = false, PlayerIndex playerIndex = PlayerIndex.One)
		: base(gamertag, gamertag)
	{
		IsGuest = isGuest;
		IsSignedInToLive = isSignedInToLive;
		PlayerIndex = playerIndex;
		GameDefaults = new GameDefaults();
		Presence = new GamerPresence();
		Privileges = new GamerPrivileges();
		PartySize = 1;
	}

	public bool IsFriend(Gamer gamer)
	{
		return false;
	}

	public bool IsHeadset(Microphone microphone)
	{
		return microphone.IsHeadset;
	}

	public FriendCollection GetFriends()
	{
		return new FriendCollection(new List<FriendGamer>());
	}

	public void AwardAchievement(string achievementKey)
	{
	}

	public IAsyncResult BeginAwardAchievement(string achievementKey, AsyncCallback callback, object state)
	{
		_ = statStoreAction;
		statStoreAction = new GamerAction(state, callback);
		statStoreAction.IsCompleted = true;
		return statStoreAction;
	}

	public void EndAwardAchievement(IAsyncResult result)
	{
		statStoreAction = null;
	}

	public AchievementCollection GetAchievements()
	{
		if (IsSignedInToLive && gamerAchievements == null)
		{
			gamerAchievements = new AchievementCollection();
		}
		return gamerAchievements;
	}

	public IAsyncResult BeginGetAchievements(AsyncCallback callback, object asyncState)
	{
		GetAchievementsDelegate getAchievementsDelegate = GetAchievements;
		return getAchievementsDelegate.BeginInvoke(callback, getAchievementsDelegate);
	}

	public AchievementCollection EndGetAchievements(IAsyncResult result)
	{
		GetAchievementsDelegate getAchievementsDelegate = (GetAchievementsDelegate)result.AsyncState;
		gamerAchievements = getAchievementsDelegate.EndInvoke(result);
		return gamerAchievements;
	}

	internal static void OnSignIn(SignedInGamer gamer)
	{
		if (SignedIn != null)
		{
			SignedIn(null, new SignedInEventArgs(gamer));
		}
	}

	internal static void OnSignOut(SignedInGamer gamer)
	{
		if (SignedOut != null)
		{
			SignedOut(null, new SignedOutEventArgs(gamer));
		}
	}
}
