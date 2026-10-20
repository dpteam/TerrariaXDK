using System;

namespace Microsoft.Xna.Framework.Net;

[Flags]
public enum GamerStates
{
	Local = 1,
	Host = 0x10,
	HasVoice = 0x100,
	Guest = 0x1000,
	MutedByLocalUser = 0x10000,
	PrivateSlot = 0x100000,
	Ready = 0x1000000
}
