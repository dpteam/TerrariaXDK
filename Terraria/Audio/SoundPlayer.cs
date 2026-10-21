using System;
using Microsoft.Xna.Framework;
using static Terraria.Main;

namespace Terraria.Audio;

/// <summary>
/// The sound the game makes, extracted from <see cref="Main"/>.
///
/// Why a separate class: <see cref="Main"/> is ~3700 lines and mixes input, rendering,
/// networking, world generation and audio. The audio part is self-contained - it reads a handful
/// of public statics (the sound pools, the volume, the resolution) and calls into
/// <see cref="SfxInstancePool"/> - so it can move without touching game logic.
///
/// <c>using static Terraria.Main</c> keeps the moved code byte-identical to the original: no
/// renames, no re-derivation of the volume/pan maths, nothing to get subtly wrong in a 240-line
/// switch. <see cref="Main.PlaySound(int, int, int)"/> stays as a one-line wrapper, so the ~300
/// call sites do not change.
/// </summary>
internal static class SoundPlayer
{
	internal static void Play(int SoundType, int X, int Y, int Style = 1)
	{
		if (SoundVolume == 0f)
		{
			return;
		}
		try
		{
			float num = SoundVolume;
			float num2 = 0f;
			bool flag;
			if (UI.NumActiveViews > 1)
			{
				flag = WorldView.AnyViewContains(X, Y);
			}
			else
			{
				Rectangle rectangle = new Rectangle
				{
					X = UI.CurrentUI.CurrentView.ScreenPosition.X - ResolutionWidth,
					Y = UI.CurrentUI.CurrentView.ScreenPosition.Y - ResolutionHeight,
					Width = ResolutionWidth * 3,
					Height = ResolutionHeight * 3
				};
				flag = rectangle.Contains(X, Y);
				if (flag)
				{
					Vector2 vector = new Vector2(UI.CurrentUI.CurrentView.ScreenPosition.X + ResolutionWidth / 2, UI.CurrentUI.CurrentView.ScreenPosition.Y + ResolutionHeight / 2);
					float num3 = Math.Abs((float)X - vector.X);
					float num4 = Math.Abs((float)Y - vector.Y);
					float num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
					num = 1f - num5 / ((float)ResolutionWidth * (2f / 3f));
					if (num > 1f)
					{
						num = 1f;
					}
					num *= SoundVolume;
					if (num <= 0f)
					{
						return;
					}
					num2 = ((float)X - vector.X) / (float)(ResolutionWidth * 2);
					if (num2 < -1f)
					{
						num2 = -1f;
					}
					else if (num2 > 1f)
					{
						num2 = 1f;
					}
				}
			}
			if (!flag)
			{
				return;
			}
			switch (SoundType)
			{
			case 0:
			{
				int num12 = Rand.Next(3);
				SoundDig[num12].Play(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			}
			case 1:
			{
				int num13 = Rand.Next(3);
				SoundPlayerHit[num13].Play(num, num2);
				break;
			}
			case 2:
			{
				if (Style == 1)
				{
					int num9 = Rand.Next(3);
					if (num9 != 0)
					{
						Style = num9 + 17;
					}
				}
				double num10 = num;
				double pitch;
				if (Style == 26 || Style == 35)
				{
					num10 *= 0.75;
					pitch = HarpNote;
				}
				else
				{
					pitch = (float)Rand.Next(-6, 7) * 0.01f;
				}
				SoundItem[Style - 1].Play(num10, num2, pitch);
				break;
			}
			case 3:
				SoundNPCHit[Style - 1].Play(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			case 4:
				if (Style != 10 || !SoundNPCKilled[Style - 1].IsPlaying())
				{
					SoundNPCKilled[Style - 1].Play(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				}
				break;
			case 5:
				SoundPlayerKilled.Play(num, num2);
				break;
			case 6:
				SoundGrass.Play(num, num2, (float)Rand.Next(-30, 31) * 0.01f);
				break;
			case 7:
				SoundGrab.Play(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			case 8:
				SoundDoorOpen.Play(num, num2, (float)Rand.Next(-20, 21) * 0.01f);
				break;
			case 9:
				SoundDoorClosed.Play(num, num2, (float)Rand.Next(-20, 21) * 0.01f);
				break;
			case 13:
				SoundShatter.Play(num, num2);
				break;
			case 14:
			{
				int num11 = Rand.Next(3);
				SoundZombie[num11].Play(num * 0.4f, num2);
				break;
			}
			case 15:
				if (!SoundRoar[Style].IsPlaying())
				{
					SoundRoar[Style].Play(num, num2);
				}
				break;
			case 16:
				SoundDoubleJump.Play(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			case 17:
				SoundRun.Play(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			case 19:
				SoundSplash[Style].Play(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			case 20:
			{
				int num8 = Rand.Next(3);
				SoundFemaleHit[num8].Play(num, num2);
				break;
			}
			case 21:
			{
				int num7 = Rand.Next(3);
				SoundTink[num7].Play(num, num2);
				break;
			}
			case 22:
				SoundUnlock.Play(num, num2);
				break;
			case 26:
			{
				int num6 = Rand.Next(3, 5);
				SoundZombie[num6].Play(num * 0.9f, num2, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			}
			case 27:
				SoundPixie.UpdateOrPlay(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			case 28:
				if (!SoundMech.IsPlaying())
				{
					SoundMech.Play(num, num2, (float)Rand.Next(-10, 11) * 0.01f);
				}
				break;
			case 10:
			case 11:
			case 12:
			case 18:
			case 23:
			case 24:
			case 25:
				break;
			}
		}
		catch
		{
		}
	}

	internal static void Play(int SoundType)
	{
		if (SoundVolume == 0f)
		{
			return;
		}
		try
		{
			float soundVolume = SoundVolume;
			switch (SoundType)
			{
			case 1:
			{
				int num2 = Rand.Next(3);
				SoundPlayerHit[num2].Play(soundVolume);
				break;
			}
			case 7:
				SoundGrab.Play(soundVolume, 0.0, (float)Rand.Next(-10, 11) * 0.01f);
				break;
			case 10:
				SoundMenuOpen.Play(soundVolume);
				break;
			case 11:
				SoundMenuClose.Play(soundVolume);
				break;
			case 12:
				SoundMenuTick.Play(soundVolume);
				break;
			case 18:
				SoundCoins.Play(soundVolume);
				break;
			case 20:
			{
				int num = Rand.Next(3);
				SoundFemaleHit[num].Play(soundVolume);
				break;
			}
			case 23:
				SoundDrown.Play(soundVolume);
				break;
			case 24:
				SoundChat.Play(soundVolume);
				break;
			case 25:
				SoundMaxMana.Play(soundVolume);
				break;
			}
		}
		catch
		{
		}
	}
}
