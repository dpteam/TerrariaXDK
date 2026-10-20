using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace Terraria;

public static class MessageBox
{
	public struct Message
	{
		public bool AutoUpdate;

		public sbyte PlayerIdx;

		public string Caption;

		public string Contents;

		public string[] CurOptions;
	}

	public static IAsyncResult MessBoxResult;

	public static Message CurrentMess;

	private static readonly List<Message> Queue;

	public static int? UserChoice;

	static MessageBox()
	{
		MessBoxResult = null;
		Queue = new List<Message>();
		CurrentMess.PlayerIdx = -1;
	}

	public static void Show(PlayerIndex Controller, string BoxCaption, string BoxContents, string[] BoxOptions, bool ShouldAutoUpdate = true)
	{
		lock (Queue)
		{
			Message message = new Message
			{
				AutoUpdate = ShouldAutoUpdate,
				PlayerIdx = (sbyte)Controller,
				Caption = BoxCaption,
				Contents = BoxContents,
				CurOptions = BoxOptions
			};
			if (CurrentMess.PlayerIdx < 0)
			{
				CurrentMess = message;
			}
			else
			{
				if (!(CurrentMess.Contents != BoxContents))
				{
					return;
				}
				for (int num = Queue.Count - 1; num >= 0; num--)
				{
					if (BoxContents == Queue[num].Contents)
					{
						return;
					}
				}
				Queue.Add(message);
			}
		}
	}

	private static void NextMessage()
	{
		if (Queue.Count > 0)
		{
			CurrentMess = Queue[0];
			Queue.RemoveAt(0);
		}
		else
		{
			CurrentMess.AutoUpdate = false;
			CurrentMess.PlayerIdx = -1;
		}
	}

	public static bool IsVisible()
	{
		return CurrentMess.PlayerIdx >= 0;
	}

	public static bool IsAutoUpdate()
	{
		return CurrentMess.AutoUpdate;
	}

	public static bool Update()
	{
		if (CurrentMess.PlayerIdx >= 0)
		{
			lock (Queue)
			{
				if (!Guide.IsVisible)
				{
					try
					{
						MessBoxResult = Guide.BeginShowMessageBox((PlayerIndex)CurrentMess.PlayerIdx, CurrentMess.Caption, CurrentMess.Contents, CurrentMess.CurOptions, 0, MessageBoxIcon.None, null, null);
						if (MessBoxResult != null && MessBoxResult.IsCompleted)
						{
							UserChoice = Guide.EndShowMessageBox(MessBoxResult);
							MessBoxResult = null;
							NextMessage();
							return true;
						}
					}
					catch (GuideAlreadyVisibleException)
					{
					}
				}
				else if (MessBoxResult != null && MessBoxResult.IsCompleted)
				{
					UserChoice = Guide.EndShowMessageBox(MessBoxResult);
					MessBoxResult = null;
					NextMessage();
					return true;
				}
			}
		}
		return false;
	}

	public static int GetResult()
	{
		if (!UserChoice.HasValue)
		{
			return -1;
		}
		return UserChoice.Value;
	}

	public static void RemoveMessagesFor(PlayerIndex Controller)
	{
		while ((PlayerIndex)CurrentMess.PlayerIdx == Controller)
		{
			if (MessBoxResult == null)
			{
				NextMessage();
			}
		}
		for (int num = Queue.Count - 1; num >= 0; num--)
		{
			if ((PlayerIndex)Queue[num].PlayerIdx == Controller)
			{
				Queue.RemoveAt(num);
			}
		}
	}
}
