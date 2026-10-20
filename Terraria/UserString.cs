using System;
using System.IO;

namespace Terraria;

public sealed class UserString
{
	public string UserText;

	public bool IsVerified;

	public bool IsCensored;

	private IAsyncResult AsyncResult;

	public static implicit operator UserString(string String)
	{
		return new UserString(String, NowVerified: true);
	}

	public UserString(string String, bool NowVerified)
	{
		UserText = String;
		IsVerified = NowVerified;
		IsCensored = false;
		AsyncResult = null;
	}

	public UserString(BinaryReader Input)
	{
		int num = Input.ReadByte();
		IsVerified = (num & 1) != 0;
		IsCensored = (num & 2) != 0;
		if (IsCensored)
		{
			UserText = null;
		}
		else
		{
			UserText = Input.ReadString();
		}
		AsyncResult = null;
	}

	public void Write(BinaryWriter Output)
	{
		int num = (IsVerified ? 1 : 0);
		if (IsCensored)
		{
			num |= 2;
		}
		Output.Write((byte)num);
		if (!IsCensored)
		{
			Output.Write(UserText);
		}
	}

	public void SetUserString(string String)
	{
		UserText = String;
		IsVerified = String.Length == 0;
		IsCensored = false;
	}

	public void SetSystemString(string String)
	{
		UserText = String;
		IsVerified = true;
		IsCensored = false;
	}

	public string GetString()
	{
		if (AsyncResult != null)
		{
			return Lang.InterfaceText[76];
		}
		if (!IsCensored)
		{
			return UserText;
		}
		return Lang.InterfaceText[77];
	}

	public bool IsEditable()
	{
		if (!IsVerified)
		{
			return Main.NetMode == 0;
		}
		return true;
	}

	private void OnCheckStringDone(object String)
	{
		try
		{
			IsCensored = false;
			IsVerified = true;
		}
		catch
		{
		}
		AsyncResult = null;
	}
}
