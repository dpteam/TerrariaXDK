using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Terraria;

internal class Settings
{
	private List<string> Subsections = new List<string>();

	private List<string> Keys = new List<string>();

	private List<string> Values = new List<string>();

	private List<string> Comments = new List<string>();

	private readonly FileInfo Path;

	private readonly string Executable = Assembly.GetExecutingAssembly().GetName().Name;

	internal Settings(string File)
	{
		using (new StreamWriter(File, append: true))
		{
			Path = new FileInfo(File ?? Executable);
		}
		Load();
	}

	internal void Set(string Subsection, string Key, string Value, string Comment = "")
	{
		if (AlreadyExists(Subsection, Key))
		{
			return;
		}
		for (int i = 0; i < Keys.Count; i++)
		{
			if (Keys[i].Equals(Key))
			{
				Subsections[i] = Subsection;
				Values[i] = Value;
				Comments[i] = Comment;
				return;
			}
		}
		Subsections.Add(Subsection);
		Keys.Add(Key);
		Values.Add(Value);
		Comments.Add(Comment);
	}

	internal string Get(string Subsection, string Key)
	{
		for (int i = 0; i < Keys.Count; i++)
		{
			if (Subsections[i].Equals(Subsection) && Keys[i].Equals(Key))
			{
				return Values[i];
			}
		}
		return "";
	}

	private bool AlreadyExists(string Subsection, string Key)
	{
		return Get(Subsection, Key).Length > 0;
	}

	private void Load()
	{
		Subsections = new List<string>();
		Keys = new List<string>();
		Values = new List<string>();
		Comments = new List<string>();
		string text = "";
		string subsection = "";
		using StreamReader streamReader = new StreamReader(Path.FullName);
		string text2 = "";
		string text3 = "";
		string text4 = "";
		while ((text = streamReader.ReadLine()) != null)
		{
			int num = text.IndexOf("=");
			int num2 = text.IndexOf(";");
			if (text.IndexOf("[") == 0)
			{
				subsection = text.Substring(1, text.Length - 2);
			}
			if (num2 != -1 && num == -1)
			{
				text2 = text3;
				text3 = text.Substring(num2 + 1).TrimStart(' ');
				if (text2 != "")
				{
					text2 += "\n; ";
				}
				text4 = text2 + text3;
			}
			if (num > 0)
			{
				if (text4 != "")
				{
					Set(subsection, text.Substring(0, num), text.Substring(num + 1), text4);
				}
				else
				{
					Set(subsection, text.Substring(0, num), text.Substring(num + 1));
				}
				text2 = "";
				text3 = "";
				text4 = "";
			}
		}
	}

	internal void Close()
	{
		using StreamWriter streamWriter = new StreamWriter(Path.FullName);
		List<string> list = new List<string>();
		for (int i = 0; i < Subsections.Count; i++)
		{
			if (!list.Contains(Subsections[i]))
			{
				list.Add(Subsections[i]);
			}
		}
		List<string> subsections = Subsections;
		List<string> keys = Keys;
		List<string> values = Values;
		List<string> comments = Comments;
		for (int j = 0; j < list.Count; j++)
		{
			bool flag = true;
			while (subsections.Contains(list[j]))
			{
				int index = subsections.IndexOf(list[j]);
				if (flag && !list[j].Equals(""))
				{
					if (j > 0)
					{
						streamWriter.WriteLine();
					}
					string value = "[" + list[j] + "]";
					streamWriter.WriteLine(value);
					flag = false;
				}
				if (!comments[index].Equals(""))
				{
					string value2 = "; " + comments[index] + "\n" + keys[index] + "=" + values[index] + "\n";
					streamWriter.WriteLine(value2);
				}
				else
				{
					string value3 = keys[index] + "=" + values[index] + "\n";
					streamWriter.WriteLine(value3);
				}
				subsections.RemoveAt(index);
				keys.RemoveAt(index);
				values.RemoveAt(index);
				comments.RemoveAt(index);
			}
		}
	}
}
