namespace Microsoft.Xna.Framework.Net;

internal enum NetworkMessageType : byte
{
	Data,
	GamerJoined,
	GamerLeft,
	Introduction,
	GamerProfile,
	RequestGamerProfile,
	GamerStateChange,
	SessionStateChange
}
