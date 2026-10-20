using System;

namespace Lidgren.Network;

public struct NetReason
{
	public static readonly NetReason Empty;

	public NetOutgoingMessage Message { get; }

	public string Text { get; }

	public int Length => Message?.LengthBytes ?? (4 + Text.Length + ((Text.Length <= 126) ? 1 : 2));

	public NetReason(string text)
	{
		Message = null;
		Text = text ?? throw new ArgumentNullException("text");
	}

	public NetReason(NetOutgoingMessage message)
	{
		Message = message ?? throw new ArgumentNullException("message");
		Text = null;
	}

	public static implicit operator NetReason(string text)
	{
		return new NetReason(text);
	}

	public static implicit operator NetReason(NetOutgoingMessage message)
	{
		return new NetReason(message);
	}

	static NetReason()
	{
	}
}
