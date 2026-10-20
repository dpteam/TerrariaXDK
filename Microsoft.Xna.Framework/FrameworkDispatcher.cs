using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;

namespace Microsoft.Xna.Framework;

public static class FrameworkDispatcher
{
	internal static bool ActiveSongChanged = false;

	internal static bool MediaStateChanged = false;

	internal static List<DynamicSoundEffectInstance> Streams = new List<DynamicSoundEffectInstance>();

	public static void Update()
	{
		lock (Streams)
		{
			for (int i = 0; i < Streams.Count; i++)
			{
				DynamicSoundEffectInstance dynamicSoundEffectInstance = Streams[i];
				dynamicSoundEffectInstance.Update();
				if (dynamicSoundEffectInstance.IsDisposed)
				{
					i--;
				}
			}
		}
		if (Microphone.micList != null)
		{
			for (int j = 0; j < Microphone.micList.Count; j++)
			{
				Microphone.micList[j].CheckBuffer();
			}
		}
		MediaPlayer.Update();
		if (ActiveSongChanged)
		{
			MediaPlayer.OnActiveSongChanged();
			ActiveSongChanged = false;
		}
		if (MediaStateChanged)
		{
			MediaPlayer.OnMediaStateChanged();
			MediaStateChanged = false;
		}
		if (TouchPanel.TouchDeviceExists)
		{
			TouchPanel.Update();
		}
	}
}
