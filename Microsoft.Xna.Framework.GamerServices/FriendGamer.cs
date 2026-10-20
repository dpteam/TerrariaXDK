namespace Microsoft.Xna.Framework.GamerServices;

public sealed class FriendGamer : Gamer
{
	public bool FriendRequestReceivedFrom { get; private set; }

	public bool FriendRequestSentTo { get; private set; }

	public bool HasVoice { get; private set; }

	public bool InviteAccepted { get; private set; }

	public bool InviteReceivedFrom { get; private set; }

	public bool InviteRejected { get; private set; }

	public bool InviteSentTo { get; private set; }

	public bool IsAway { get; private set; }

	public bool IsBusy { get; private set; }

	public bool IsJoinable { get; private set; }

	public bool IsOnline { get; private set; }

	public bool IsPlaying { get; private set; }

	public string Presence { get; private set; }

	internal FriendGamer(string gamertag, string displayName, bool online, bool playing, bool away, bool busy, bool requestingFriend, bool friendRequesting)
		: base(gamertag, displayName)
	{
		IsOnline = online;
		IsPlaying = playing;
		IsAway = away;
		IsBusy = busy;
		FriendRequestSentTo = requestingFriend;
		FriendRequestReceivedFrom = friendRequesting;
		IsJoinable = false;
		InviteAccepted = false;
		InviteReceivedFrom = false;
		InviteRejected = false;
		InviteSentTo = false;
		HasVoice = false;
		Presence = string.Empty;
	}
}
