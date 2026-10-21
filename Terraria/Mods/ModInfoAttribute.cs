using System;

namespace Terraria.Mods;

/// <summary>
/// Declares the identity of a mod assembly. Applied at assembly level:
/// <c>[assembly: ModInfo("ExampleMod", "1.0.0", "Terraria Agent")]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ModInfoAttribute : Attribute
{
	public string Name { get; private set; }

	public string Version { get; private set; }

	public string Author { get; private set; }

	public string Description { get; private set; }

	public ModInfoAttribute(string name, string version, string author = "", string description = "")
	{
		Name = name;
		Version = version;
		Author = author;
		Description = description;
	}
}
