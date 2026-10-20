using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Storage;

namespace Terraria.Achievements;

public class AchievementSystem
{
	public struct TerrariaAchievement(string name, string description)
	{
		public string Name = name;

		public string Description = description;

		public bool IsEarned = false;

		public bool EarnedOnline = false;

		public DateTime EarnedDate = DateTime.MinValue;

		public byte GamerScore = 0;

		public void Award(bool EarnStatus, DateTime When)
		{
			IsEarned = EarnStatus;
			EarnedDate = When;
		}

		public bool GetStatus()
		{
			return IsEarned;
		}
	}

	private static class AchievementSerializer
	{
		public static void Serialize(Stream stream, Dictionary<Achievement, TerrariaAchievement> achievements)
		{
			BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8);
			binaryWriter.Write(achievements.Count);
			foreach (KeyValuePair<Achievement, TerrariaAchievement> achievement in achievements)
			{
				binaryWriter.Write((int)achievement.Key);
				binaryWriter.Write(achievement.Value.Name ?? string.Empty);
				binaryWriter.Write(achievement.Value.Description ?? string.Empty);
				binaryWriter.Write(achievement.Value.IsEarned);
				binaryWriter.Write(achievement.Value.EarnedOnline);
				binaryWriter.Write(achievement.Value.EarnedDate.ToBinary());
				binaryWriter.Write(achievement.Value.GamerScore);
			}
			binaryWriter.Flush();
		}

		public static Dictionary<Achievement, TerrariaAchievement> Deserialize(Stream stream)
		{
			Dictionary<Achievement, TerrariaAchievement> dictionary = new Dictionary<Achievement, TerrariaAchievement>();
			BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8);
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				Achievement key = (Achievement)binaryReader.ReadInt32();
				string name = binaryReader.ReadString();
				string description = binaryReader.ReadString();
				TerrariaAchievement terrariaAchievement = new TerrariaAchievement(name, description);
				terrariaAchievement.IsEarned = binaryReader.ReadBoolean();
				terrariaAchievement.EarnedOnline = binaryReader.ReadBoolean();
				terrariaAchievement.EarnedDate = DateTime.FromBinary(binaryReader.ReadInt64());
				terrariaAchievement.GamerScore = binaryReader.ReadByte();
				TerrariaAchievement value = terrariaAchievement;
				dictionary[key] = value;
			}
			return dictionary;
		}
	}

	private const int MaxConcurrentSubmissions = 10;

	private readonly IAsyncResult[] Submissions;

	private readonly int FirstFree;

	public const int MaxAchievementCount = 39;

	private static string SavePath;

	private static readonly byte[] EncryptionKey = Encoding.ASCII.GetBytes("ENGINE505RELOGIC");

	private static Dictionary<Achievement, TerrariaAchievement> AchievementsList = new Dictionary<Achievement, TerrariaAchievement>();

	public AchievementSystem()
	{
		Submissions = new IAsyncResult[10];
		FirstFree = 0;
		AchievementDetails achievementDetails = new AchievementDetails();
		for (int i = 0; i < 39; i++)
		{
			AchievementsList.Add((Achievement)i, achievementDetails.Details[i]);
		}
	}

	public void SaveAchievements()
	{
		SavePath = "Achievements-" + UI.MainUI.SignedInGamer.Gamertag + ".dat";
		if (!UI.MainUI.HasPlayerStorage())
		{
			return;
		}
		using MemoryStream memoryStream = new MemoryStream();
		using CryptoStream cryptoStream = new CryptoStream(memoryStream, new RijndaelManaged().CreateEncryptor(EncryptionKey, EncryptionKey), CryptoStreamMode.Write);
		try
		{
			AchievementSerializer.Serialize(cryptoStream, AchievementsList);
			cryptoStream.FlushFinalBlock();
			Main.ShowSaveIcon();
			if (!UI.MainUI.TestStorageSpace("Achievements", SavePath, (int)memoryStream.Length))
			{
				return;
			}
			using StorageContainer storageContainer = UI.MainUI.OpenPlayerStorage("Achievements");
			using Stream stream = storageContainer.OpenFile(SavePath, FileMode.Create);
			stream.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
		}
		catch (IOException)
		{
			UI.MainUI.WriteError();
		}
		catch (Exception)
		{
		}
		finally
		{
			Main.HideSaveIcon();
		}
	}

	public static void LoadAchievements()
	{
		SavePath = "Achievements-" + Main.Gamertag + ".dat";
		try
		{
			using StorageContainer storageContainer = UI.MainUI.OpenPlayerStorage("Achievements");
			if (!storageContainer.FileExists(SavePath))
			{
				return;
			}
			try
			{
				using Stream stream = storageContainer.OpenFile(SavePath, FileMode.Open);
				using CryptoStream stream2 = new CryptoStream(stream, new RijndaelManaged().CreateDecryptor(EncryptionKey, EncryptionKey), CryptoStreamMode.Read);
				AchievementsList = AchievementSerializer.Deserialize(stream2);
			}
			catch (InvalidOperationException)
			{
				Main.ShowSaveIcon();
				storageContainer.DeleteFile(SavePath);
				Main.HideSaveIcon();
			}
			catch (Exception)
			{
			}
		}
		catch (IOException)
		{
			UI.MainUI.ReadError();
		}
		catch (Exception)
		{
		}
	}

	private static BitArray ProcessEarnedAchievements()
	{
		LoadAchievements();
		BitArray bitArray = new BitArray(48);
		try
		{
			foreach (KeyValuePair<Achievement, TerrariaAchievement> achievements in AchievementsList)
			{
				int num = -1;
				try
				{
					num = (int)achievements.Key;
				}
				catch (ArgumentException)
				{
					continue;
				}
				bitArray.Set(num, achievements.Value.GetStatus());
			}
		}
		catch
		{
		}
		return bitArray;
	}

	public BitArray GetEarnedAchievements()
	{
		return ProcessEarnedAchievements();
	}

	public Dictionary<Achievement, TerrariaAchievement> GetCurrentAchievements()
	{
		return AchievementsList;
	}

	public void Award(SignedInGamer Gamer, Achievement AwardedAchievement)
	{
		if (AwardedAchievement >= Achievement.AchievementCount)
		{
			return;
		}
		TerrariaAchievement value = AchievementsList[AwardedAchievement];
		value.Award(EarnStatus: true, DateTime.Today);
		AchievementsList[AwardedAchievement] = value;
		Main.PlaySound(24);
		Main.NewText("Achievement Unlocked: " + value.Name, 255, 200, 62);
		bool flag = true;
		for (int i = 0; i < 38; i++)
		{
			if (!AchievementsList[(Achievement)i].IsEarned)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			value = AchievementsList[Achievement.Completionist];
			value.Award(EarnStatus: true, DateTime.Today);
			AchievementsList[Achievement.Completionist] = value;
			Main.NewText("Congratulations, you have unlocked all the achievements!", 255, 200, 62);
		}
		SaveAchievements();
	}

	public void Update()
	{
		for (int i = 0; i < FirstFree; i++)
		{
			_ = Submissions[i].IsCompleted;
		}
	}
}
