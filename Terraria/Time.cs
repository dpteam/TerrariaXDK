using System;
using Microsoft.Xna.Framework;

namespace Terraria;

public struct Time
{
	private const int sunWidth = 64;

	private const int moonWidth = 50;

	public const int moonHeight = 50;

	public const float dayLength = 54000f;

	public const float NightLength = 32400f;

	public float DayRate;

	public float WorldTime;

	public bool DayTime;

	public bool IsBloodMoon;

	public byte MoonPhase;

	public Color intermediateCelestialColor;

	public Color intermediateBgColor;

	public short celestialX;

	public short celestialY;

	public float celestialRotation;

	public float celestialScale;

	public Color celestialColor;

	public Color bgColor;

	public Color tileColor;

	public Vector3 TileColorFore;

	public static bool xMas;

	static Time()
	{
	}

	public void reset(float speed)
	{
		DayRate = speed;
		WorldTime = 13500f;
		DayTime = true;
		IsBloodMoon = false;
		MoonPhase = 0;
		intermediateCelestialColor.A = byte.MaxValue;
		intermediateBgColor.A = byte.MaxValue;
		tileColor.A = byte.MaxValue;
		updateDay();
	}

	private void updateNight()
	{
		celestialX = (short)((int)(WorldTime / 32400f * (float)(Main.ResolutionWidth + 100)) - 50);
		celestialRotation = WorldTime / 16200f - 7.3f;
		float num = ((!(WorldTime < 16200f)) ? ((WorldTime / 32400f - 0.5f) * 2f) : (1f - WorldTime / 16200f));
		num *= num;
		celestialY = (short)((int)(num * 250f) + 180);
		celestialScale = 1.2f - num * 0.4f;
		if (IsBloodMoon)
		{
			if (WorldTime < 16200f)
			{
				float num2 = 1f - WorldTime / 16200f;
				intermediateCelestialColor.R = (byte)(num2 * 10f + 205f);
				intermediateCelestialColor.G = (byte)(num2 * 170f + 55f);
				intermediateCelestialColor.B = (byte)(num2 * 200f + 55f);
				intermediateBgColor.R = (byte)(40f - num2 * 40f + 35f);
				intermediateBgColor.G = (byte)(num2 * 20f + 15f);
				intermediateBgColor.B = (byte)(num2 * 20f + 15f);
			}
			else if (WorldTime >= 16200f)
			{
				float num3 = (WorldTime / 32400f - 0.5f) * 2f;
				intermediateCelestialColor.R = (byte)(num3 * 10f + 205f);
				intermediateCelestialColor.G = (byte)(num3 * 170f + 55f);
				intermediateCelestialColor.B = (byte)(num3 * 200f + 55f);
				intermediateBgColor.R = (byte)(40f - num3 * 40f + 35f);
				intermediateBgColor.G = (byte)(num3 * 20f + 15f);
				intermediateBgColor.B = (byte)(num3 * 20f + 15f);
			}
		}
		else if (WorldTime < 16200f)
		{
			float num4 = 1f - WorldTime / 16200f;
			intermediateCelestialColor.R = (byte)(num4 * 10f + 205f);
			intermediateCelestialColor.G = (byte)(num4 * 70f + 155f);
			intermediateCelestialColor.B = (byte)(num4 * 100f + 155f);
			intermediateBgColor.R = (byte)(num4 * 20f + 15f);
			intermediateBgColor.G = (byte)(num4 * 20f + 15f);
			intermediateBgColor.B = (byte)(num4 * 20f + 15f);
		}
		else if (WorldTime >= 16200f)
		{
			float num5 = (WorldTime / 32400f - 0.5f) * 2f;
			intermediateCelestialColor.R = (byte)(num5 * 50f + 205f);
			intermediateCelestialColor.G = (byte)(num5 * 100f + 155f);
			intermediateCelestialColor.B = (byte)(num5 * 100f + 155f);
			intermediateBgColor.R = (byte)(num5 * 10f + 15f);
			intermediateBgColor.G = (byte)(num5 * 20f + 15f);
			intermediateBgColor.B = (byte)(num5 * 20f + 15f);
		}
		else
		{
			intermediateCelestialColor = Color.White;
			intermediateBgColor = Color.White;
		}
	}

	private void updateDay()
	{
		celestialX = (short)((int)(WorldTime / 54000f * (float)(Main.ResolutionWidth + 128)) - 64);
		celestialRotation = WorldTime / 27000f - 7.3f;
		float num = ((!(WorldTime < 27000f)) ? ((WorldTime / 54000f - 0.5f) * 2f) : (1f - WorldTime / 54000f * 2f));
		num *= num;
		celestialY = (short)((int)(num * 250f) + 180);
		celestialScale = (1.2f - num * 0.4f) * 1.1f;
		if (WorldTime < 13500f)
		{
			float num2 = WorldTime / 13500f;
			intermediateCelestialColor.R = (byte)(num2 * 200f + 55f);
			intermediateCelestialColor.G = (byte)(num2 * 180f + 75f);
			intermediateCelestialColor.B = (byte)(num2 * 250f + 5f);
			intermediateBgColor.R = (byte)(num2 * 230f + 25f);
			intermediateBgColor.G = (byte)(num2 * 220f + 35f);
			intermediateBgColor.B = (byte)(num2 * 220f + 35f);
		}
		else if (WorldTime > 45900f)
		{
			float num3 = 1f - (WorldTime / 54000f - 0.85f) * 6.6666665f;
			intermediateCelestialColor.R = (byte)(num3 * 120f + 55f);
			intermediateCelestialColor.G = (byte)(num3 * 100f + 25f);
			intermediateCelestialColor.B = (byte)(num3 * 120f + 55f);
			intermediateBgColor.R = (byte)(num3 * 200f + 35f);
			intermediateBgColor.G = (byte)(num3 * 85f + 35f);
			intermediateBgColor.B = (byte)(num3 * 135f + 35f);
		}
		else if (WorldTime > 37800f)
		{
			float num4 = 1f - (WorldTime / 54000f - 0.7f) * 6.6666665f;
			intermediateCelestialColor.R = (byte)(num4 * 80f + 175f);
			intermediateCelestialColor.G = (byte)(num4 * 130f + 125f);
			intermediateCelestialColor.B = (byte)(num4 * 100f + 155f);
			intermediateBgColor.R = (byte)(num4 * 20f + 235f);
			intermediateBgColor.G = (byte)(num4 * 135f + 120f);
			intermediateBgColor.B = (byte)(num4 * 85f + 170f);
		}
		else
		{
			intermediateCelestialColor = Color.White;
			intermediateBgColor = Color.White;
		}
	}

	public void applyJungle(float light)
	{
		if (light > 1f)
		{
			light = 1f;
		}
		int r = intermediateBgColor.R;
		int g = intermediateBgColor.G;
		int b = intermediateBgColor.B;
		r -= (int)(30f * light * ((float)r * (1f / 255f)));
		b -= (int)(90f * light * ((float)b * (1f / 255f)));
		if (r < 15)
		{
			r = 15;
		}
		if (b < 15)
		{
			b = 15;
		}
		bgColor.R = (byte)r;
		bgColor.G = (byte)g;
		bgColor.B = (byte)b;
		bgColor.A = byte.MaxValue;
		r = intermediateCelestialColor.R;
		g = intermediateCelestialColor.G;
		b = intermediateCelestialColor.B;
		if (DayTime)
		{
			r -= (int)(30f * light * ((float)r * (1f / 255f)));
			b -= (int)(10f * light * ((float)g * (1f / 255f)));
		}
		else
		{
			r -= (int)(140f * light * ((float)r * (1f / 255f)));
			g -= (int)(190f * light * ((float)g * (1f / 255f)));
			b -= (int)(170f * light * ((float)b * (1f / 255f)));
		}
		if (r < 15)
		{
			r = 15;
		}
		if (g < 15)
		{
			g = 15;
		}
		if (b < 15)
		{
			b = 15;
		}
		celestialColor.R = (byte)r;
		celestialColor.G = (byte)g;
		celestialColor.B = (byte)b;
		celestialColor.A = byte.MaxValue;
	}

	public void applyEvil(float light)
	{
		if (light > 1f)
		{
			light = 1f;
		}
		int r = intermediateBgColor.R;
		int g = intermediateBgColor.G;
		int b = intermediateBgColor.B;
		r -= (int)(100f * light * ((float)r * (1f / 255f)));
		g -= (int)(140f * light * ((float)g * (1f / 255f)));
		b -= (int)(80f * light * ((float)b * (1f / 255f)));
		if (r < 15)
		{
			r = 15;
		}
		if (g < 15)
		{
			g = 15;
		}
		if (b < 15)
		{
			b = 15;
		}
		bgColor.R = (byte)r;
		bgColor.G = (byte)g;
		bgColor.B = (byte)b;
		bgColor.A = byte.MaxValue;
		r = intermediateCelestialColor.R;
		g = intermediateCelestialColor.G;
		b = intermediateCelestialColor.B;
		if (DayTime)
		{
			r -= (int)(100f * light * ((float)r * (1f / 255f)));
			g -= (int)(100f * light * ((float)g * (1f / 255f)));
		}
		else
		{
			r -= (int)(140f * light * ((float)r * (1f / 255f)));
			g -= (int)(190f * light * ((float)g * (1f / 255f)));
			b -= (int)(170f * light * ((float)b * (1f / 255f)));
		}
		if (r < 15)
		{
			r = 15;
		}
		if (g < 15)
		{
			g = 15;
		}
		if (b < 15)
		{
			b = 15;
		}
		celestialColor.R = (byte)r;
		celestialColor.G = (byte)g;
		celestialColor.B = (byte)b;
		celestialColor.A = byte.MaxValue;
	}

	public void applyNothing()
	{
		celestialColor = intermediateCelestialColor;
		bgColor = intermediateBgColor;
	}

	public void finalizeColors()
	{
		if (IsBloodMoon)
		{
			if (bgColor.R < 35)
			{
				bgColor.R = 35;
			}
			if (bgColor.G < 35)
			{
				bgColor.G = 35;
			}
			if (bgColor.B < 35)
			{
				bgColor.B = 35;
			}
		}
		else
		{
			if (bgColor.R < 25)
			{
				bgColor.R = 25;
			}
			if (bgColor.G < 25)
			{
				bgColor.G = 25;
			}
			if (bgColor.B < 25)
			{
				bgColor.B = 25;
			}
		}
		tileColor.R = (byte)((bgColor.G + bgColor.B + bgColor.R * 8) / 10);
		tileColor.G = (byte)((bgColor.R + bgColor.B + bgColor.G * 8) / 10);
		tileColor.B = (byte)((bgColor.R + bgColor.G + bgColor.B * 8) / 10);
		TileColorFore.X = (float)(int)tileColor.R * (1f / 255f);
		TileColorFore.Y = (float)(int)tileColor.G * (1f / 255f);
		TileColorFore.Z = (float)(int)tileColor.B * (1f / 255f);
	}

	public bool Update()
	{
		WorldTime += DayRate;
		if (!DayTime)
		{
			if (WorldTime > 32400f)
			{
				WorldTime = 0f;
				DayTime = true;
				IsBloodMoon = false;
				MoonPhase = (byte)((MoonPhase + 1) & 7);
				updateDay();
				return true;
			}
			updateNight();
		}
		else
		{
			if (WorldTime > 54000f)
			{
				WorldTime = 0f;
				DayTime = false;
				updateNight();
				return true;
			}
			updateDay();
		}
		return false;
	}

	public static void CheckXMas()
	{
		DateTime now = DateTime.Now;
		xMas = now.Month == 12 && now.Day >= 15;
	}
}
