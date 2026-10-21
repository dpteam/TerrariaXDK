using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Mods;

/// <summary>
/// Per-mod service object: file access, texture/sound loading and logging. All content is
/// loaded from the mod's own folder so mods never touch the game's Content directory.
/// </summary>
public sealed class ModContext
{
	private readonly string _directory;

	internal ModContext(string name, string directory)
	{
		Name = name;
		_directory = directory;
	}

	public string Name { get; private set; }

	/// <summary>Absolute path of the mod folder.</summary>
	public string Directory => _directory;

	/// <summary>The player controlled by this machine, or null while in a menu.</summary>
	public Player LocalPlayer => UI.MainUI?.ActivePlayer;

	public bool IsMultiplayer => Main.NetMode != 0;

	public bool IsHost => Main.NetMode == 2;

	/// <summary>Loads a PNG or DDS texture relative to the mod folder.</summary>
	public Texture2D LoadTexture(string relativePath)
	{
		string text = Resolve(relativePath);
		if (text == null || ModLoader.GraphicsDevice == null)
		{
			return null;
		}
		try
		{
			using FileStream stream = File.OpenRead(text);
			if (text.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
			{
				return Texture2D.DDSFromStreamEXT(ModLoader.GraphicsDevice, stream);
			}
			return Texture2D.FromStream(ModLoader.GraphicsDevice, stream);
		}
		catch (Exception ex)
		{
			Log("Failed to load texture '" + relativePath + "': " + ex.Message);
			return null;
		}
	}

	/// <summary>Loads a WAV sound effect relative to the mod folder.</summary>
	public SoundEffect LoadSound(string relativePath)
	{
		string text = Resolve(relativePath);
		if (text == null)
		{
			return null;
		}
		try
		{
			using FileStream stream = File.OpenRead(text);
			return SoundEffect.FromStream(stream);
		}
		catch (Exception ex)
		{
			Log("Failed to load sound '" + relativePath + "': " + ex.Message);
			return null;
		}
	}

	/// <summary>Reads a text file (config, definition list, ...) from the mod folder.</summary>
	public string ReadAllText(string relativePath)
	{
		string text = Resolve(relativePath);
		return (text == null) ? null : File.ReadAllText(text);
	}

	public void Log(string message)
	{
		ModLoader.Log("[" + Name + "] " + message);
	}

	/// <summary>
	/// Sends a payload to the other players. The mod id is carried with it, so two mods can
	/// never collide and nothing has to be registered in advance. Returns false in
	/// singleplayer or when the payload is larger than 64 KiB.
	/// </summary>
	public bool SendPacket(byte[] payload)
	{
		return Terraria.Net.ModNet.Send(Name, payload);
	}

	/// <summary>True when there is someone to send to (host or joined client).</summary>
	public bool CanSendPackets => Terraria.Net.ModNet.IsConnected;

	private string Resolve(string relativePath)
	{
		if (string.IsNullOrEmpty(relativePath))
		{
			return null;
		}
		string text = Path.Combine(_directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
		return File.Exists(text) ? text : null;
	}
}
