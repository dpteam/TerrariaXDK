using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Mods;

/// <summary>
/// What "the files of a mod" means: which ones exist, how big the set is, and what its content
/// hash is.
///
/// Three things read this and they must agree, or a download either fails its own check or
/// copies files nobody asked for:
/// <list type="bullet">
/// <item>the handshake, which tells the host's clients what a mod is made of;</item>
/// <item>the host, which serves those files;</item>
/// <item>the client, which checks what arrived against the hash before it goes anywhere near
/// <c>Storage/Mods</c>.</item>
/// </list>
///
/// What is deliberately <b>not</b> part of a mod: <c>disabled.txt</c> (a switch, not content -
/// the same rule the signature follows) and the build output (<c>obj/</c>, <c>bin/</c>), because
/// two people compiling the same source produce different timestamps and the hashes would
/// disagree for no reason a player could act on.
/// </summary>
internal static class ModFiles
{
	/// <summary>Upper bound for one mod, as it travels. Matches the per-file download limit.</summary>
	internal const int MaxBytes = 8 * 1024 * 1024;

	/// <summary>Upper bound on file count, so a hostile folder cannot make the host enumerate forever.</summary>
	internal const int MaxFileCount = 512;

	/// <summary>Bytes per transfer frame. Small enough to keep one frame from starving the game.</summary>
	internal const int ChunkSize = 32 * 1024;

	/// <summary>Folders that are build output rather than content.</summary>
	private static readonly string[] SkippedFolders = new string[2] { "obj", "bin" };

	/// <summary>
	/// Where a download is assembled and installed. Defaults to <c>Storage/Mods</c>.
	///
	/// A test hook for the same reason <c>ModMenu</c> has them: a check that downloaded into the
	/// player's own Mods folder would be installing mods they never asked for.
	/// </summary>
	internal static string ModsRootOverride;

	/// <summary>The folder downloads are assembled in, before they are allowed to be mods.</summary>
	internal static string ModsRoot => ModsRootOverride ?? Terraria.Storage.AppPaths.Mods;

	/// <summary>Files that are switches rather than content.</summary>
	private static readonly string[] SkippedFiles = new string[1] { "disabled.txt" };

	/// <summary>
	/// The files of a mod, relative to its folder, sorted so the order is the same on both
	/// machines. Empty with a reason when the folder is unusable or too large to send.
	/// </summary>
	internal static string[] List(string modDirectory, out string error)
	{
		error = null;
		if (string.IsNullOrEmpty(modDirectory) || !Directory.Exists(modDirectory))
		{
			error = "no such folder: " + modDirectory;
			return new string[0];
		}
		string[] files;
		try
		{
			files = Directory.GetFiles(modDirectory, "*", SearchOption.AllDirectories);
		}
		catch (Exception ex)
		{
			error = "cannot list " + modDirectory + ": " + ex.Message;
			return new string[0];
		}
		string root = Path.GetFullPath(modDirectory);
		List<string> list = new List<string>(files.Length);
		for (int i = 0; i < files.Length; i++)
		{
			string text = Relative(root, files[i]);
			if (text == null || IsSkipped(text))
			{
				continue;
			}
			list.Add(text);
		}
		list.Sort(StringComparer.OrdinalIgnoreCase);
		if (list.Count > MaxFileCount)
		{
			error = "the mod has " + list.Count + " files, more than the " + MaxFileCount + " allowed";
			return new string[0];
		}
		return list.ToArray();
	}

	/// <summary>Total size of the files <see cref="List"/> returns, or -1 when unreadable.</summary>
	internal static long TotalSize(string modDirectory)
	{
		string error;
		string[] array = List(modDirectory, out error);
		if (array.Length == 0 && error != null)
		{
			return -1L;
		}
		long num = 0L;
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				num += new FileInfo(Path.Combine(modDirectory, array[i].Replace('/', Path.DirectorySeparatorChar))).Length;
			}
			catch
			{
				return -1L;
			}
		}
		return num;
	}

	/// <summary>
	/// FNV-1a over every file's relative path and bytes, in the order <see cref="List"/> gives.
	///
	/// Content, not the folder name and not a timestamp: two players whose mods differ in one
	/// byte must be told they differ, and two whose files are identical must be told they match.
	/// </summary>
	/// <summary>
	/// The hash of one file, with the same FNV-1a and the same failure vocabulary as
	/// <see cref="ContentHash"/>.
	///
	/// Used for a compiled mod's assembly: what such a mod *does* is its code, so its code is what
	/// the signature covers. A folder hash would also count the author's <c>.csproj</c> and any
	/// stray <c>.dll.bak</c>, and refusing a join over one of those is a refusal the player can
	/// neither explain nor fix.
	/// </summary>
	internal static string FileHash(string path)
	{
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return "missing";
		}
		ulong num = 14695981039346656037uL;
		byte[] array = new byte[8192];
		try
		{
			using FileStream stream = File.OpenRead(path);
			int num2;
			while ((num2 = stream.Read(array, 0, array.Length)) > 0)
			{
				for (int i = 0; i < num2; i++)
				{
					num ^= array[i];
					num *= 1099511628211;
				}
			}
		}
		catch
		{
			return "unreadable";
		}
		return num.ToString("x16");
	}

	internal static string ContentHash(string modDirectory)
	{		string error;
		string[] array = List(modDirectory, out error);
		if (array.Length == 0)
		{
			return (error == null) ? "empty" : "unreadable";
		}
		ulong num = 14695981039346656037uL;
		byte[] array2 = new byte[8192];
		for (int i = 0; i < array.Length; i++)
		{
			num = Hash(num, Encoding.UTF8.GetBytes(array[i]));
			string text = Path.Combine(modDirectory, array[i].Replace('/', Path.DirectorySeparatorChar));
			try
			{
				using FileStream stream = File.OpenRead(text);
				int num2;
				while ((num2 = stream.Read(array2, 0, array2.Length)) > 0)
				{
					for (int j = 0; j < num2; j++)
					{
						num ^= array2[j];
						num *= 1099511628211;
					}
				}
			}
			catch
			{
				// "Could not read" and "empty" are different contents, so the hash must differ.
				num = Hash(num, Encoding.UTF8.GetBytes("<unreadable>"));
			}
			// A separator between files, so moving a byte from the end of one into the start of
			// the next is not invisible.
			num = Hash(num, Encoding.UTF8.GetBytes("\n"));
		}
		return num.ToString("x16");
	}

	/// <summary>
	/// True when a relative path is safe to write under a mod folder: no absolute paths, no
	/// <c>..</c>, no drive letters, forward slashes only.
	///
	/// The client applies this to every path the host sends. A host is another machine, and
	/// "write to the path in this packet" is how a download becomes an arbitrary file write.
	/// </summary>
	internal static bool IsSafeRelativePath(string path)
	{
		if (string.IsNullOrEmpty(path) || path.Length > 200)
		{
			return false;
		}
		if (path[0] == '/' || path[0] == '\\')
		{
			return false;
		}
		if (path.IndexOf(':') >= 0 || path.IndexOf('\\') >= 0)
		{
			return false;
		}
		string[] array = path.Split('/');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			if (text.Length == 0 || text == "." || text == "..")
			{
				return false;
			}
			// Windows refuses these in a name, and a trailing dot/space is silently stripped,
			// which would make two different paths land in the same file.
			if (text.TrimEnd(' ', '.').Length != text.Length)
			{
				return false;
			}
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			for (int j = 0; j < text.Length; j++)
			{
				if (Array.IndexOf(invalidFileNameChars, text[j]) >= 0)
				{
					return false;
				}
			}
		}
		return true;
	}

	/// <summary>
	/// A mod name usable as a folder under <c>Storage/Mods</c>. Anything that is not a plain
	/// name becomes empty, and the caller refuses the download rather than guessing.
	/// </summary>
	internal static string SafeFolderName(string name)
	{
		if (string.IsNullOrEmpty(name) || name.Length > 64)
		{
			return "";
		}
		if (name == "." || name == "..")
		{
			return "";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if (c == '/' || c == '\\' || c == ':' || Array.IndexOf(invalidFileNameChars, c) >= 0)
			{
				return "";
			}
		}
		string text = name.Trim().TrimEnd('.');
		return (text.Length == name.Length) ? text : "";
	}

	/// <summary>Where an incoming download is assembled before it is allowed to be a mod.</summary>
	internal static string IncomingDirectory(string modName)
	{
		return Path.Combine(ModsRoot, ".incoming", modName);
	}

	/// <summary>
	/// Removes a half-finished download. Called before starting a new one and after a failed
	/// hash check: a partial folder must never look like an installed mod.
	/// </summary>
	internal static void DiscardIncoming(string modName)
	{
		string text = IncomingDirectory(modName);
		try
		{
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
		}
		catch (Exception ex)
		{
			ModLoader.Log("[Download] could not remove " + text + ": " + ex.Message);
		}
	}

	/// <summary>
	/// Moves a verified download into place, replacing a folder of the same name.
	///
	/// Replace, not merge: a leftover file from an older version would sit in the new folder and
	/// change the hash of what is on disk, so the next join would refuse the client it just
	/// served. The old folder is moved aside first, so a failure leaves the player with either
	/// the old mod or the new one - never neither.
	/// </summary>
	internal static bool Install(string modName, out string error)
	{
		error = null;
		string text = IncomingDirectory(modName);
		string text2 = Path.Combine(ModsRoot, modName);
		string text3 = Path.Combine(ModsRoot, ".incoming", modName + ".old");
		try
		{
			Directory.CreateDirectory(ModsRoot);
			if (Directory.Exists(text3))
			{
				Directory.Delete(text3, recursive: true);
			}
			if (Directory.Exists(text2))
			{
				Directory.Move(text2, text3);
			}
			try
			{
				Directory.Move(text, text2);
			}
			catch
			{
				// Put the old one back: failing to install must not cost the player the mod they
				// already had.
				if (Directory.Exists(text3) && !Directory.Exists(text2))
				{
					Directory.Move(text3, text2);
				}
				throw;
			}
			if (Directory.Exists(text3))
			{
				Directory.Delete(text3, recursive: true);
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	/// <summary>
	/// Creates one received file, empty. Used for the manifest: a zero-byte file in a mod never
	/// produces a chunk, so without this it would be missing and the hash would not match.
	/// </summary>
	internal static bool TouchReceived(string modName, string relativePath, out string error)
	{
		error = null;
		if (!IsSafeRelativePath(relativePath))
		{
			error = "unsafe path '" + relativePath + "'";
			return false;
		}
		try
		{
			string text = Path.Combine(IncomingDirectory(modName), relativePath.Replace('/', Path.DirectorySeparatorChar));
			string directoryName = Path.GetDirectoryName(text);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			using (new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.None))
			{
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	/// <summary>Writes one received file, refusing a path that escapes the folder.</summary>
	internal static bool WriteReceived(string modName, string relativePath, byte[] data, int offset, int count, out string error)
	{
		error = null;
		if (!IsSafeRelativePath(relativePath))
		{
			error = "unsafe path '" + relativePath + "'";
			return false;
		}
		try
		{
			string text = Path.Combine(IncomingDirectory(modName), relativePath.Replace('/', Path.DirectorySeparatorChar));
			string directoryName = Path.GetDirectoryName(text);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			using FileStream stream = new FileStream(text, FileMode.Append, FileAccess.Write, FileShare.None);
			stream.Write(data, offset, count);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static string Relative(string root, string path)
	{
		try
		{
			string fullPath = Path.GetFullPath(path);
			if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			return fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				.Replace(Path.DirectorySeparatorChar, '/');
		}
		catch
		{
			return null;
		}
	}

	private static bool IsSkipped(string relativePath)
	{
		for (int i = 0; i < SkippedFiles.Length; i++)
		{
			if (relativePath.Equals(SkippedFiles[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		string[] array = relativePath.Split('/');
		if (array.Length < 2)
		{
			return false;
		}
		for (int j = 0; j < SkippedFolders.Length; j++)
		{
			if (array[0].Equals(SkippedFolders[j], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static ulong Hash(ulong hash, byte[] bytes)
	{
		for (int i = 0; i < bytes.Length; i++)
		{
			hash ^= bytes[i];
			hash *= 1099511628211;
		}
		return hash;
	}
}
