using System;
using System.IO;

namespace Terraria.World;

/// <summary>
/// What a world file actually is.
/// </summary>
internal enum WorldFileKind
{
	/// <summary>Nothing readable: empty file or not a world at all.</summary>
	Unknown,

	/// <summary>This build's format (versions 46..49): what the game can load.</summary>
	Console,

	/// <summary>
	/// A PC-edition world. Recognised so it can be reported honestly, not loaded: the PC format
	/// is laid out differently and version-gated field by field.
	/// </summary>
	Pc
}

/// <summary>
/// The result of looking at a world file without loading it.
/// </summary>
internal sealed class WorldFileReport
{
	public WorldFileKind Kind = WorldFileKind.Unknown;

	/// <summary>Raw version number from the header. 0 when the header could not be read.</summary>
	public int Version;

	public string Name;

	public int TilesX;

	public int TilesY;

	public int Tiles;

	public int Chests;

	public int Signs;

	public int Npcs;

	/// <summary>Byte offset where reading stopped, for diagnostics.</summary>
	public long StoppedAt;

	/// <summary>Byte offset where the tile section ended, for diagnostics.</summary>
	public long TileEnd;

	public long Length;

	/// <summary>True when the whole structure could be walked to the end.</summary>
	public bool Complete;

	/// <summary>One line a player can act on.</summary>
	public string Summary;

	/// <summary>True when the game can load this file.</summary>
	public bool Loadable => Kind == WorldFileKind.Console && Complete;
}

/// <summary>
/// Reads a world file's structure without touching the world.
///
/// Two jobs, both of which used to be impossible:
/// <list type="bullet">
/// <item><b>Tell the player why a world will not load.</b> The game only knew "loadWorld
/// returned false" and showed a menu with no explanation - a truncated save, a corrupt header
/// and a PC-edition world all looked identical.</item>
/// <item><b>Answer the PC-edition question with evidence.</b> Conversions from the PC format
/// are not implemented, but "your file is a PC world, version N" is a real answer, and it is
/// the difference between a bug report and a support reply.</item>
/// </list>
///
/// The tile loop mirrors <see cref="WorldGen.loadNewWorld"/> field by field: same RLE rule,
/// same "frame-important tiles carry FrameX/FrameY" rule. It reads into no storage, so it needs
/// no world, no graphics device and no UI - which is what makes it testable.
/// </summary>
internal static class WorldFileInspector
{
	/// <summary>First version this build cannot load (console formats stop at 49).</summary>
	public const int FirstUnsupportedVersion = 50;

	/// <summary>1000 chests and 1000 signs are fixed-size tables in the save format.</summary>
	private const int ChestCount = 1000;

	private const int SignCount = 1000;

	/// <summary>Frame-important tiles per <see cref="EntityID.FrameImportantTiles"/>, built once.</summary>
	private static bool[] _frameImportant;

	public static WorldFileReport Inspect(Stream stream)
	{
		WorldFileReport worldFileReport = new WorldFileReport();
		if (stream == null)
		{
			worldFileReport.Summary = "No file.";
			return worldFileReport;
		}
		byte[] array;
		try
		{
			long length = stream.Length;
			worldFileReport.Length = length;
			array = new byte[length];
			stream.Position = 0;
			int num = 0;
			while (num < array.Length)
			{
				int num2 = stream.Read(array, num, array.Length - num);
				if (num2 <= 0)
				{
					break;
				}
				num += num2;
			}
			if (num < array.Length)
			{
				Array.Resize(ref array, num);
				worldFileReport.Length = num;
			}
		}
		catch (Exception ex)
		{
			worldFileReport.Summary = "Could not read the file: " + ex.Message;
			return worldFileReport;
		}
		if (array.Length < 4)
		{
			worldFileReport.Summary = "The file is empty or too short to be a world.";
			return worldFileReport;
		}
		// Checked before the version: a handful of bytes cannot be a world of either edition, and
		// reading a version out of them would misfile garbage as a PC file.
		if (array.Length < 24)
		{
			worldFileReport.Version = BitConverter.ToInt32(array, 0);
			worldFileReport.Summary = "The file is too short to be a world (" + array.Length + " bytes).";
			return worldFileReport;
		}
		worldFileReport.Version = BitConverter.ToInt32(array, 0);
		if (worldFileReport.Version >= FirstUnsupportedVersion)
		{
			// Recognisably a world, but from the PC edition: its layout differs and is gated on
			// dozens of per-version fields. Say so instead of failing anonymously.
			worldFileReport.Kind = WorldFileKind.Pc;
			worldFileReport.Summary = "This is a PC-edition world (version " + worldFileReport.Version
				+ "). This build cannot load PC worlds.";
			return worldFileReport;
		}
		if (worldFileReport.Version < 0)
		{
			worldFileReport.Summary = "The file does not look like a world (version " + worldFileReport.Version + ").";
			return worldFileReport;
		}
		worldFileReport.Kind = WorldFileKind.Console;
		try
		{
			if (worldFileReport.Version <= 46)
			{
				WalkOld(array, worldFileReport);
			}
			else
			{
				Walk(array, worldFileReport);
			}
		}
		catch (EndOfStreamException)
		{
			worldFileReport.Complete = false;
			worldFileReport.Summary = "The save is truncated: it ends at byte " + worldFileReport.StoppedAt
				+ " of " + worldFileReport.Length + ".";
			return worldFileReport;
		}
		catch (Exception ex)
		{
			worldFileReport.Complete = false;
			worldFileReport.Summary = "The save is damaged: " + ex.Message;
			return worldFileReport;
		}
		worldFileReport.Complete = true;
		worldFileReport.Summary = Describe(worldFileReport);
		return worldFileReport;
	}

	/// <summary>Convenience overload for a path; a missing file is reported, not thrown.</summary>
	public static WorldFileReport Inspect(string path)
	{
		try
		{
			using FileStream stream = File.OpenRead(path);
			return Inspect(stream);
		}
		catch (Exception ex)
		{
			WorldFileReport worldFileReport = new WorldFileReport();
			worldFileReport.Summary = "Could not open the file: " + ex.Message;
			return worldFileReport;
		}
	}

	/// <summary>
	/// The legacy layout (versions up to 46), which is what the tutorial world and any save from
	/// an older build uses. It is a genuinely different format - int32 header fields, presence
	/// booleans around walls and liquids, an int16 repeat count instead of the packed byte, NPC
	/// types as strings, and a footer naming the world - so it gets its own walk. Field order
	/// mirrors <see cref="WorldGen.loadOldWorld"/> exactly.
	/// </summary>
	private static void WalkOld(byte[] data, WorldFileReport report)
	{
		BinaryReader binaryReader = new BinaryReader(new MemoryStream(data, writable: false));
		int release = report.Version;
		report.StoppedAt = 0;
		// The version was already read from the buffer directly; skip it here so the reader is
		// aligned with the first real field.
		binaryReader.ReadInt32();
		report.Name = binaryReader.ReadString();
		int worldId = binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		report.TilesY = binaryReader.ReadInt32();
		report.TilesX = binaryReader.ReadInt32();
		if (report.TilesX <= 0 || report.TilesY <= 0)
		{
			throw new InvalidDataException("the header claims a world of " + report.TilesX + "x" + report.TilesY + " tiles");
		}
		binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		binaryReader.ReadDouble();
		binaryReader.ReadDouble();
		binaryReader.ReadDouble();
		binaryReader.ReadBoolean();
		binaryReader.ReadInt32();
		binaryReader.ReadBoolean();
		binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		for (int i = 0; i < 11; i++)
		{
			binaryReader.ReadBoolean();
		}
		binaryReader.ReadByte();
		binaryReader.ReadInt32();
		binaryReader.ReadBoolean();
		binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		binaryReader.ReadDouble();
		report.StoppedAt = binaryReader.BaseStream.Position;

		bool[] frameImportant = FrameImportant();
		long num = 0L;
		for (int j = 0; j < report.TilesX; j++)
		{
			int num2 = 0;
			while (num2 < report.TilesY)
			{
				if (binaryReader.ReadByte() != 0)
				{
					byte b = binaryReader.ReadByte();
					if (b < frameImportant.Length && frameImportant[b])
					{
						binaryReader.ReadInt16();
						binaryReader.ReadInt16();
					}
				}
				if (binaryReader.ReadBoolean())
				{
					binaryReader.ReadByte();
				}
				if (binaryReader.ReadBoolean())
				{
					binaryReader.ReadByte();
					binaryReader.ReadBoolean();
				}
				if (release < 46)
				{
					binaryReader.ReadBoolean();
				}
				else
				{
					binaryReader.ReadByte();
				}
				int num3 = binaryReader.ReadInt16();
				if (num3 < 0)
				{
					throw new InvalidDataException("a negative repeat count at byte " + binaryReader.BaseStream.Position);
				}
				// One entry covers the base tile plus its repeat count: the loader does
				// num += repeat, copies `repeat` tiles, then num++ for the base tile.
				num2 += num3 + 1;
				num += num3 + 1;
				report.StoppedAt = binaryReader.BaseStream.Position;
			}
		}
		report.Tiles = (int)Math.Min(int.MaxValue, num);
		report.TileEnd = binaryReader.BaseStream.Position;

		for (int k = 0; k < ChestCount; k++)
		{
			if (!binaryReader.ReadBoolean())
			{
				continue;
			}
			report.Chests++;
			binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			for (int l = 0; l < 20; l++)
			{
				if (binaryReader.ReadByte() > 0)
				{
					binaryReader.ReadInt32();
					binaryReader.ReadByte();
				}
			}
			report.StoppedAt = binaryReader.BaseStream.Position;
		}
		for (int m = 0; m < SignCount; m++)
		{
			if (binaryReader.ReadBoolean())
			{
				report.Signs++;
				binaryReader.ReadString();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
			}
			report.StoppedAt = binaryReader.BaseStream.Position;
		}
		bool flag = binaryReader.ReadBoolean();
		int num4 = 0;
		while (flag)
		{
			num4++;
			binaryReader.ReadString();
			binaryReader.ReadSingle();
			binaryReader.ReadSingle();
			binaryReader.ReadBoolean();
			binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			flag = binaryReader.ReadBoolean();
			report.StoppedAt = binaryReader.BaseStream.Position;
		}
		report.Npcs = num4;
		// Ten NPC type names, then the footer that names the world and repeats its id.
		for (int n = 0; n < 10; n++)
		{
			binaryReader.ReadString();
		}
		binaryReader.ReadBoolean();
		string text = binaryReader.ReadString();
		int num5 = binaryReader.ReadInt32();
		report.StoppedAt = binaryReader.BaseStream.Position;
		if (!string.Equals(text, report.Name, StringComparison.Ordinal))
		{
			throw new InvalidDataException("the footer names '" + text + "' but the header names '" + report.Name + "'");
		}
		if (num5 != worldId)
		{
			throw new InvalidDataException("the footer world id " + num5 + " does not match the header " + worldId);
		}
	}

	private static string Describe(WorldFileReport report)
	{
		string text = (string.IsNullOrEmpty(report.Name) ? "Unnamed world" : report.Name)
			+ ", " + report.TilesX + "x" + report.TilesY + " tiles";
		if (report.Chests > 0)
		{
			text += ", " + report.Chests + " chest(s)";
		}
		if (report.Npcs > 0)
		{
			text += ", " + report.Npcs + " NPC(s)";
		}
		return text;
	}

	/// <summary>
	/// Walks the structure, counting what it finds. Field order is deliberately identical to
	/// <see cref="WorldGen.loadNewWorld"/> - if these ever disagree, this inspector is lying about
	/// what the loader will accept, which is worse than not having it.
	/// </summary>
	private static void Walk(byte[] data, WorldFileReport report)
	{
		BinaryReader binaryReader = new BinaryReader(new MemoryStream(data, writable: false));
		int release = report.Version;
		report.StoppedAt = 0;
		binaryReader.ReadInt32();
		binaryReader.ReadUInt32();
		report.Name = binaryReader.ReadString();
		binaryReader.ReadInt32();
		if (release >= 48)
		{
			binaryReader.ReadInt32();
		}
		binaryReader.ReadInt32();
		binaryReader.ReadInt16();
		report.TilesY = binaryReader.ReadInt16();
		report.TilesX = binaryReader.ReadInt16();
		if (report.TilesX <= 0 || report.TilesY <= 0)
		{
			throw new InvalidDataException("the header claims a world of " + report.TilesX + "x" + report.TilesY + " tiles");
		}
		binaryReader.ReadInt16();
		binaryReader.ReadInt16();
		binaryReader.ReadInt16();
		binaryReader.ReadInt16();
		binaryReader.ReadSingle();
		binaryReader.ReadBoolean();
		binaryReader.ReadByte();
		binaryReader.ReadBoolean();
		binaryReader.ReadInt16();
		binaryReader.ReadInt16();
		// Bosses and flags: nine booleans plus the orb, altar and hardmode block.
		for (int i = 0; i < 11; i++)
		{
			binaryReader.ReadBoolean();
		}
		binaryReader.ReadByte();
		binaryReader.ReadInt32();
		binaryReader.ReadBoolean();
		binaryReader.ReadByte();
		binaryReader.ReadInt16();
		binaryReader.ReadByte();
		binaryReader.ReadSingle();

		bool[] frameImportant = FrameImportant();
		long num = 0L;
		for (int j = 0; j < report.TilesX; j++)
		{
			int num2 = 0;
			while (num2 < report.TilesY)
			{
				byte b = binaryReader.ReadByte();
				if (b != 0)
				{
					byte b2 = binaryReader.ReadByte();
					if (b2 == 127)
					{
						b = 0;
					}
					if (b2 < frameImportant.Length && frameImportant[b2])
					{
						binaryReader.ReadInt16();
						binaryReader.ReadInt16();
					}
				}
				binaryReader.ReadByte();
				byte b3 = binaryReader.ReadByte();
				if (b3 > 0)
				{
					binaryReader.ReadBoolean();
				}
				binaryReader.ReadByte();
				int num3 = binaryReader.ReadByte();
				if ((num3 & 0x80) != 0)
				{
					num3 &= 0x7F;
					num3 |= binaryReader.ReadByte() << 7;
				}
				// One entry covers the base tile plus its repeat count: the loader does
				// num += repeat, copies `repeat` tiles, then num++ for the base tile.
				num2 += num3 + 1;
				num += num3 + 1;
				report.StoppedAt = binaryReader.BaseStream.Position;
			}
		}
		report.Tiles = (int)Math.Min(int.MaxValue, num);
			report.TileEnd = binaryReader.BaseStream.Position;
		for (int k = 0; k < ChestCount; k++)
		{
			if (!binaryReader.ReadBoolean())
			{
				continue;
			}
			report.Chests++;
			binaryReader.ReadInt16();
			binaryReader.ReadInt16();
			for (int l = 0; l < 20; l++)
			{
				int num4 = ((release <= 49) ? binaryReader.ReadByte() : binaryReader.ReadInt16());
				if (num4 > 0)
				{
					binaryReader.ReadInt16();
					binaryReader.ReadByte();
				}
			}
			report.StoppedAt = binaryReader.BaseStream.Position;
		}
		for (int m = 0; m < SignCount; m++)
		{
			// Sign.Read: only a set sign carries data, and the field order differs before v49.
			if (!binaryReader.ReadBoolean())
			{
				continue;
			}
			report.Signs++;
			if (release >= 49)
			{
				binaryReader.ReadInt16();
				binaryReader.ReadInt16();
				ReadUserString(binaryReader);
			}
			else
			{
				binaryReader.ReadString();
				binaryReader.ReadInt16();
				binaryReader.ReadInt16();
			}
			report.StoppedAt = binaryReader.BaseStream.Position;
		}
		report.StoppedAt = binaryReader.BaseStream.Position;
		bool flag = binaryReader.ReadBoolean();
		int num5 = 0;
		while (flag)
		{
			num5++;
			binaryReader.ReadByte();
			binaryReader.ReadSingle();
			binaryReader.ReadSingle();
			binaryReader.ReadBoolean();
			binaryReader.ReadInt16();
			binaryReader.ReadInt16();
			flag = binaryReader.ReadBoolean();
			report.StoppedAt = binaryReader.BaseStream.Position;
		}
		report.Npcs = num5;
	}

	/// <summary>
	/// Reads one <see cref="UserString"/>: a flags byte, then the text unless it was censored.
	/// Signs use this rather than a plain string, so the length prefix is not where a naive
	/// reader expects it.
	/// </summary>
	private static void ReadUserString(BinaryReader reader)
	{
		int num = reader.ReadByte();
		if ((num & 2) == 0)
		{
			reader.ReadString();
		}
	}

	/// <summary>
	/// Which tile ids carry frame data. Built from <see cref="EntityID.FrameImportantTiles"/>
	/// rather than <c>Main.TileFrameImportant</c>, because the latter is filled by the same
	/// initialisation the inspector must not depend on (it needs to run headless).
	/// </summary>
	private static bool[] FrameImportant()
	{
		if (_frameImportant != null)
		{
			return _frameImportant;
		}
		bool[] array = new bool[256];
		EntityID.TileID[] frameImportantTiles = EntityID.FrameImportantTiles;
		for (int i = 0; i < frameImportantTiles.Length; i++)
		{
			int num = (int)frameImportantTiles[i];
			if (num >= 0 && num < array.Length)
			{
				array[num] = true;
			}
		}
		_frameImportant = array;
		return array;
	}
}
