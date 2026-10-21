using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Named regions, stored in <c>Storage/Config/regions.txt</c>.
///
/// One region per line, so the file stays readable and hand-editable:
/// <code>
/// # name = x1,y1,x2,y2[,group]
/// house = 100,200,160,240,admin
/// farm  = 400,300,470,340
/// </code>
/// A region without a group is host-only - a region exists to protect something, so the safe
/// default is closed, not open.
///
/// Overlapping regions are rejected rather than merged: two regions covering the same tile
/// would make "may I build here" depend on which one is checked first, and neither the player
/// nor the host would be able to predict the answer.
/// </summary>
internal static class Regions
{
	public const string FileName = "regions.txt";

	private static readonly List<Region> Items = new List<Region>();

	private static readonly List<string> Problems = new List<string>();

	private static readonly object Sync = new object();

	private static bool _loaded;

	public static string FilePath => Path.Combine(Terraria.Storage.AppPaths.Config, FileName);

	/// <summary>Lines the file could not understand.</summary>
	public static IList<string> LoadProblems
	{
		get
		{
			Load();
			lock (Sync)
			{
				return new List<string>(Problems);
			}
		}
	}

	public static int Count
	{
		get
		{
			Load();
			lock (Sync)
			{
				return Items.Count;
			}
		}
	}

	/// <summary>A copy of the regions, sorted by name.</summary>
	public static List<Region> All
	{
		get
		{
			Load();
			lock (Sync)
			{
				List<Region> list = new List<Region>(Items);
				list.Sort(delegate(Region a, Region b)
				{
					return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
				});
				return list;
			}
		}
	}

	/// <summary>The region containing this tile, or null.</summary>
	public static Region FindAt(int x, int y)
	{
		Load();
		lock (Sync)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].Contains(x, y))
				{
					return Items[i];
				}
			}
		}
		return null;
	}

	public static Region FindByName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		Load();
		lock (Sync)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (string.Equals(Items[i].Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					return Items[i];
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Adds a region. Returns false with a reason when the name is taken, the rectangle is
	/// empty, or it overlaps an existing region.
	/// </summary>
	public static bool Add(Region region, out string error)
	{
		error = null;
		if (region == null)
		{
			error = "No region given.";
			return false;
		}
		region.Name = (region.Name ?? string.Empty).Trim();
		if (region.Name.Length == 0)
		{
			error = "A region needs a name.";
			return false;
		}
		if (region.Name.IndexOf('=') >= 0 || region.Name.IndexOf('#') >= 0)
		{
			error = "A region name cannot contain '=' or '#'.";
			return false;
		}
		region.Normalize();
		if (region.Width <= 0 || region.Height <= 0)
		{
			error = "The region is empty.";
			return false;
		}
		Load();
		lock (Sync)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (string.Equals(Items[i].Name, region.Name, StringComparison.OrdinalIgnoreCase))
				{
					error = "There is already a region called '" + region.Name + "'.";
					return false;
				}
				if (Items[i].Overlaps(region))
				{
					error = "It overlaps '" + Items[i].Name + "'.";
					return false;
				}
			}
			Items.Add(region);
		}
		Save();
		return true;
	}

	public static bool Remove(string name)
	{
		Region region = FindByName(name);
		if (region == null)
		{
			return false;
		}
		lock (Sync)
		{
			Items.Remove(region);
		}
		Save();
		return true;
	}

	/// <summary>Changes which group may build in a region. Null or empty means host-only.</summary>
	public static bool SetGroup(string name, string group, out string error)
	{
		error = null;
		Region region = FindByName(name);
		if (region == null)
		{
			error = "No region called '" + name + "'.";
			return false;
		}
		if (!string.IsNullOrEmpty(group) && Permissions.GroupNames().IndexOf(group) < 0)
		{
			error = "No group called '" + group + "'. Known: " + string.Join(", ", Permissions.GroupNames().ToArray());
			return false;
		}
		lock (Sync)
		{
			region.Group = string.IsNullOrEmpty(group) ? null : group;
		}
		Save();
		return true;
	}

	/// <summary>
	/// May this player edit inside this region? The host always may (someone has to be able to
	/// repair), and the region's group may. Everyone else is refused.
	/// </summary>
	internal static bool AllowsIn(Region region, bool isHost, string gamertag)
	{
		if (isHost)
		{
			return true;
		}
		if (region == null || string.IsNullOrEmpty(region.Group))
		{
			return false;
		}
		string text = Permissions.GroupOf(gamertag);
		return text != null && string.Equals(text, region.Group, StringComparison.OrdinalIgnoreCase);
	}

	public static void Reload()
	{
		lock (Sync)
		{
			_loaded = false;
			Items.Clear();
			Problems.Clear();
		}
	}

	/// <summary>Parses <c>x1,y1,x2,y2[,group]</c>. Returns false for anything malformed.</summary>
	internal static bool TryParseBody(string body, out Region region, out string error)
	{
		region = null;
		error = null;
		if (string.IsNullOrWhiteSpace(body))
		{
			error = "expected x1,y1,x2,y2[,group]";
			return false;
		}
		string[] array = body.Split(',');
		if (array.Length < 4 || array.Length > 5)
		{
			error = "expected x1,y1,x2,y2[,group], got " + array.Length + " field(s)";
			return false;
		}
		int[] array2 = new int[4];
		for (int i = 0; i < 4; i++)
		{
			if (!int.TryParse(array[i].Trim(), out array2[i]))
			{
				error = "'" + array[i].Trim() + "' is not a number";
				return false;
			}
		}
		Region region2 = new Region();
		region2.X1 = array2[0];
		region2.Y1 = array2[1];
		region2.X2 = array2[2];
		region2.Y2 = array2[3];
		region2.Group = (array.Length == 5 && array[4].Trim().Length != 0) ? array[4].Trim() : null;
		region2.Normalize();
		// The world is finite; a region bigger than it is a typo, not an intention.
		if (region2.X1 < 0 || region2.Y1 < 0 || region2.X2 >= Main.MaxTilesX || region2.Y2 >= Main.MaxTilesY)
		{
			error = "coordinates must be inside the world (0-" + (Main.MaxTilesX - 1) + ", 0-" + (Main.MaxTilesY - 1) + ")";
			return false;
		}
		region = region2;
		return true;
	}

	private static void Load()
	{
		lock (Sync)
		{
			if (_loaded)
			{
				return;
			}
			_loaded = true;
			Items.Clear();
			Problems.Clear();
			try
			{
				if (!File.Exists(FilePath))
				{
					return;
				}
				string[] array = File.ReadAllLines(FilePath);
				for (int i = 0; i < array.Length; i++)
				{
					string text = array[i].Trim();
					if (text.Length == 0 || text.StartsWith("#") || text.StartsWith(";"))
					{
						continue;
					}
					int num = text.IndexOf('=');
					if (num <= 0)
					{
						Problems.Add("line " + (i + 1) + ": expected 'name = x1,y1,x2,y2[,group]'");
						continue;
					}
					string text2 = text.Substring(0, num).Trim();
					Region region;
					string error;
					if (!TryParseBody(text.Substring(num + 1), out region, out error))
					{
						Problems.Add("region '" + text2 + "': " + error);
						continue;
					}
					region.Name = text2;
					bool flag = false;
					for (int j = 0; j < Items.Count; j++)
					{
						if (string.Equals(Items[j].Name, region.Name, StringComparison.OrdinalIgnoreCase))
						{
							Problems.Add("region '" + text2 + "': duplicate name");
							flag = true;
							break;
						}
						if (Items[j].Overlaps(region))
						{
							Problems.Add("region '" + text2 + "': overlaps '" + Items[j].Name + "'");
							flag = true;
						}
					}
					if (!flag)
					{
						Items.Add(region);
					}
				}
			}
			catch (Exception ex)
			{
				Problems.Add("could not read the file: " + ex.Message);
			}
		}
	}

	private static void Save()
	{
		try
		{
			string text = Path.GetDirectoryName(FilePath);
			if (!string.IsNullOrEmpty(text) && !Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# Named regions. name = x1,y1,x2,y2[,group]");
			stringBuilder.AppendLine("# Without a group, only the host may build there. Overlapping regions are refused.");
			lock (Sync)
			{
				List<Region> list = new List<Region>(Items);
				list.Sort(delegate(Region a, Region b)
				{
					return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
				});
				for (int i = 0; i < list.Count; i++)
				{
					Region region = list[i];
					stringBuilder.Append(region.Name).Append(" = ").Append(region.X1).Append(',').Append(region.Y1)
						.Append(',').Append(region.X2).Append(',').Append(region.Y2);
					if (!string.IsNullOrEmpty(region.Group))
					{
						stringBuilder.Append(',').Append(region.Group);
					}
					stringBuilder.AppendLine();
				}
			}
			File.WriteAllText(FilePath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Regions", "could not save the regions: " + ex.Message);
		}
	}
}
