using System;

namespace Terraria.Net;

/// <summary>
/// What a player is allowed to do on a server.
///
/// The first version of the host commands had exactly one rule: "the host can do anything,
/// nobody else can do anything". That works for playing with friends but not for a server
/// someone else tends, and it is also the wrong shape for everything else on the roadmap
/// (regions, build protection, anti-cheat exemptions) - each of those needs "may this player
/// do X", not "is this player the host".
///
/// Flags rather than an ordered rank: a moderator may kick and mute but must not hand out
/// powers, and expressing that as a rank always ends up with a special case.
/// </summary>
[Flags]
internal enum Permission
{
	None = 0,

	/// <summary>See the roster and the ban list (<c>!who</c>, <c>!bans</c>, <c>!groups</c>).</summary>
	Inspect = 1,

	/// <summary>Disconnect a player (<c>!kick</c>).</summary>
	Kick = 2,

	/// <summary>Ban and unban (<c>!ban</c>, <c>!unban</c>).</summary>
	Ban = 4,

	/// <summary>Silence and unsilence (<c>!mute</c>, <c>!unmute</c>).</summary>
	Mute = 8,

	/// <summary>Send a private message (<c>!whisper</c>).</summary>
	Whisper = 16,

	/// <summary>Change what everyone sees: MOTD, rules, server messages.</summary>
	ServerText = 32,

	/// <summary>Grant and revoke powers (<c>!op</c>, <c>!deop</c>). Deliberately separate.</summary>
	Manage = 64,

	/// <summary>Reserved for world protection: place and break blocks.</summary>
	Build = 128,

	/// <summary>Everything a normal player may do; kept explicit so a group can be "just play".</summary>
	Play = 256,

	/// <summary>Every permission. <c>*</c> in the config file.</summary>
	All = Inspect | Kick | Ban | Mute | Whisper | ServerText | Manage | Build | Play
}
