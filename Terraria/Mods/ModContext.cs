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

	/// <summary>
	/// Returns a handle for changing an existing item's stats, or null if the name is unknown.
	///
	/// The name is the one the game shows ("Copper Shortsword"), not a type id: ids are a
	/// property of this build's content, names are what a mod author and a save file have in
	/// common. The override applies right after <c>Item.SetDefaults</c>, so it reaches every
	/// copy of the item everywhere, including ones loaded from a save.
	///
	/// Returns null rather than throwing so one typo does not take down the whole mod: the miss
	/// is logged, and the rest of the mod's overrides still apply.
	/// </summary>
	public ItemOverride Item(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		Item item = default(Item);
		// By name, through the real table: SetDefaults(string) only knows the two dozen legacy
		// console items, so "Copper Bar" would silently produce an empty item.
		int type = ItemLookup.Find(name);
		if (type <= 0)
		{
			Log("unknown item '" + name + "'; no override applied");
			return null;
		}
		item.SetDefaults(type);
		if (item.Type <= 0)
		{
			Log("unknown item '" + name + "'; no override applied");
			return null;
		}
		return new ItemOverride(item.Type, name);
	}

	/// <summary>
	/// Declares a new item, with its sprite. Returns null when the declaration cannot work (no
	/// name, no sprite, a duplicate), and logs why.
	///
	/// The sprite is a PNG in the mod's own folder, e.g. <c>"items/copper-bulgeon.png"</c>. All of
	/// a mod's sprites are packed into one texture at load time, so drawing them costs no extra
	/// batch flushes.
	///
	/// Ids are assigned after every mod has loaded, in sorted order, so two players with the same
	/// mods get the same ids regardless of load order - an item id travels on the wire.
	/// </summary>
	public ModItem DefineItem(string name, string spritePath)
	{
		string error;
		ModItem modItem = ModItems.Register(Name, name, spritePath, _directory, out error);
		if (modItem == null)
		{
			Log("cannot define item '" + name + "': " + error);
		}
		return modItem;
	}

	/// <summary>
	/// Declares a crafting recipe. Returns null when the result name is empty, and logs why.
	///
	/// The recipe is resolved after every mod has loaded, because a mod item has no id until then.
	/// Ingredients and stations are added by name:
	/// <code>
	/// ModRecipe recipe = Context.AddRecipe("Copper Bulgeon");
	/// recipe.Amount = 1;
	/// recipe.AddIngredient("Copper Bar", 6);
	/// recipe.AddIngredient("Wood", 1);
	/// recipe.AddStation("Anvil");
	/// </code>
	/// An unknown ingredient or station is reported when the recipe is built, and the recipe is
	/// skipped: offering a recipe that produces nothing is worse than not offering it.
	/// </summary>
	public ModRecipe AddRecipe(string resultItemName)
	{
		string error;
		ModRecipe modRecipe = ModRecipes.Register(Name, resultItemName, out error);
		if (modRecipe == null)
		{
			Log("cannot add a recipe for '" + resultItemName + "': " + error);
		}
		return modRecipe;
	}

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
