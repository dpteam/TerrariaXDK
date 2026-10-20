using System;
using System.IO;

namespace Microsoft.Xna.Framework.Storage;

public class StorageContainer : IDisposable
{
	private readonly string storagePath;

	public string DisplayName { get; private set; }

	public bool IsDisposed { get; private set; }

	public StorageDevice StorageDevice { get; private set; }

	public event EventHandler<EventArgs> Disposing;

	internal StorageContainer(StorageDevice device, string name, string rootPath, PlayerIndex? playerIndex)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("A title name has to be provided in parameter name.");
		}
		StorageDevice = device;
		DisplayName = name;
		storagePath = Path.Combine(rootPath, name, playerIndex.HasValue ? ("Player" + (int)(playerIndex.Value + 1)) : "AllPlayers");
		if (!Directory.Exists(storagePath))
		{
			Directory.CreateDirectory(storagePath);
		}
	}

	public void Dispose()
	{
		if (Disposing != null)
		{
			Disposing(this, null);
		}
		IsDisposed = true;
	}

	public void CreateDirectory(string directory)
	{
		if (string.IsNullOrEmpty(directory))
		{
			throw new ArgumentNullException("Parameter directory must contain a value.");
		}
		string path = Path.Combine(storagePath, directory);
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}

	public Stream CreateFile(string file)
	{
		if (string.IsNullOrEmpty(file))
		{
			throw new ArgumentNullException("Parameter file must contain a value.");
		}
		string path = Path.Combine(storagePath, file);
		return File.Create(path);
	}

	public void DeleteDirectory(string directory)
	{
		if (string.IsNullOrEmpty(directory))
		{
			throw new ArgumentNullException("Parameter directory must contain a value.");
		}
		string path = Path.Combine(storagePath, directory);
		Directory.Delete(path);
	}

	public void DeleteFile(string file)
	{
		if (string.IsNullOrEmpty(file))
		{
			throw new ArgumentNullException("Parameter file must contain a value.");
		}
		string path = Path.Combine(storagePath, file);
		File.Delete(path);
	}

	public bool DirectoryExists(string directory)
	{
		if (string.IsNullOrEmpty(directory))
		{
			throw new ArgumentNullException("Parameter directory must contain a value.");
		}
		string path = Path.Combine(storagePath, directory);
		return Directory.Exists(path);
	}

	public bool FileExists(string file)
	{
		if (string.IsNullOrEmpty(file))
		{
			throw new ArgumentNullException("Parameter file must contain a value.");
		}
		string path = Path.Combine(storagePath, file);
		return File.Exists(path);
	}

	public string[] GetDirectoryNames()
	{
		string[] directories = Directory.GetDirectories(storagePath);
		for (int i = 0; i < directories.Length; i++)
		{
			directories[i] = directories[i].Substring(storagePath.Length + 1);
		}
		return directories;
	}

	public string[] GetDirectoryNames(string searchPattern)
	{
		if (string.IsNullOrEmpty(searchPattern))
		{
			throw new ArgumentNullException("Parameter searchPattern must contain a value.");
		}
		string[] directories = Directory.GetDirectories(storagePath, searchPattern);
		for (int i = 0; i < directories.Length; i++)
		{
			directories[i] = directories[i].Substring(storagePath.Length + 1);
		}
		return directories;
	}

	public string[] GetFileNames()
	{
		string[] files = Directory.GetFiles(storagePath);
		for (int i = 0; i < files.Length; i++)
		{
			files[i] = files[i].Substring(storagePath.Length + 1);
		}
		return files;
	}

	public string[] GetFileNames(string searchPattern)
	{
		if (string.IsNullOrEmpty(searchPattern))
		{
			throw new ArgumentNullException("Parameter searchPattern must contain a value.");
		}
		string[] files = Directory.GetFiles(storagePath, searchPattern);
		for (int i = 0; i < files.Length; i++)
		{
			files[i] = files[i].Substring(storagePath.Length + 1);
		}
		return files;
	}

	public Stream OpenFile(string file, FileMode fileMode)
	{
		return OpenFile(file, fileMode, FileAccess.ReadWrite, FileShare.ReadWrite);
	}

	public Stream OpenFile(string file, FileMode fileMode, FileAccess fileAccess)
	{
		return OpenFile(file, fileMode, fileAccess, FileShare.ReadWrite);
	}

	public Stream OpenFile(string file, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
	{
		if (string.IsNullOrEmpty(file))
		{
			throw new ArgumentNullException("Parameter file must contain a value.");
		}
		string path = Path.Combine(storagePath, file);
		return File.Open(path, fileMode, fileAccess, fileShare);
	}
}
