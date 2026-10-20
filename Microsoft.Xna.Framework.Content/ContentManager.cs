#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace Microsoft.Xna.Framework.Content;

public class ContentManager : IDisposable
{
	private GraphicsDevice graphicsDevice;

	private Dictionary<string, object> loadedAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

	private List<IDisposable> disposableAssets = new List<IDisposable>();

	/// <summary>
	/// Guards <see cref="loadedAssets"/> and <see cref="disposableAssets"/>. TerrariaXDK decodes
	/// audio on a loader thread while the main thread may still request splash assets, and a
	/// plain Dictionary tolerates no concurrent access at all - corruption there shows up as a
	/// hang or a bogus "asset not found" on an otherwise healthy install.
	/// </summary>
	private readonly object assetLock = new object();

	private bool disposed;

	/// <summary>
	/// Number of assets actually decoded since startup. The loading screen uses it to drive a
	/// progress bar from real work instead of coarse "phase started" markers, which is what
	/// made the bar jump forward in big steps.
	/// </summary>
	internal static int AssetsLoaded;

	private static object ContentManagerLock = new object();

	private static List<WeakReference> ContentManagers = new List<WeakReference>();

	private static readonly byte[] xnbHeader = new byte[4];

	private static List<char> targetPlatformIdentifiers = new List<char>
	{
		'w', 'x', 'm', 'i', 'a', 'd', 'X', 'W', 'n', 'u',
		'p', 'M', 'r', 'P', 'g', 'l'
	};

	public IServiceProvider ServiceProvider { get; private set; }

	public string RootDirectory { get; set; }

	internal string RootDirectoryFullPath
	{
		get
		{
			if (Path.IsPathRooted(RootDirectory))
			{
				return RootDirectory;
			}
			return Path.Combine(TitleLocation.Path, RootDirectory);
		}
	}

	public ContentManager(IServiceProvider serviceProvider)
	{
		if (serviceProvider == null)
		{
			throw new ArgumentNullException("serviceProvider");
		}
		ServiceProvider = serviceProvider;
		RootDirectory = string.Empty;
		AddContentManager(this);
	}

	public ContentManager(IServiceProvider serviceProvider, string rootDirectory)
	{
		if (serviceProvider == null)
		{
			throw new ArgumentNullException("serviceProvider");
		}
		if (rootDirectory == null)
		{
			throw new ArgumentNullException("rootDirectory");
		}
		ServiceProvider = serviceProvider;
		RootDirectory = rootDirectory;
		AddContentManager(this);
	}

	~ContentManager()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
		RemoveContentManager(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				Unload();
			}
			disposed = true;
		}
	}

	public virtual T Load<T>(string assetName)
	{
		if (string.IsNullOrEmpty(assetName))
		{
			throw new ArgumentNullException("assetName");
		}
		if (disposed)
		{
			throw new ObjectDisposedException("ContentManager");
		}
		T val = default;
		string key = assetName.Replace('\\', '/');
		lock (assetLock)
		{
			object value = null;
			if (loadedAssets.TryGetValue(key, out value) && value is T result)
			{
				return result;
			}
			// Load outside the lock is unsafe: two threads would decode the same asset and
			// the second write would leak the first. Serialising is fine - loads are rare.
			val = ReadAsset<T>(assetName, null);
			loadedAssets[key] = val;
			System.Threading.Interlocked.Increment(ref AssetsLoaded);
			return val;
		}
	}

	public virtual void Unload()
	{
		lock (assetLock)
		{
			foreach (IDisposable disposableAsset in disposableAssets)
			{
				disposableAsset?.Dispose();
			}
			disposableAssets.Clear();
			loadedAssets.Clear();
		}
	}

	protected virtual Stream OpenStream(string assetName)
	{
		try
		{
			return TitleContainer.OpenStream(Path.Combine(RootDirectory, assetName) + ".xnb");
		}
		catch (FileNotFoundException innerException)
		{
			throw new ContentLoadException("The content file was not found.", innerException);
		}
		catch (DirectoryNotFoundException innerException2)
		{
			throw new ContentLoadException("The directory was not found.", innerException2);
		}
		catch (Exception innerException3)
		{
			throw new ContentLoadException("Opening stream error.", innerException3);
		}
	}

	protected T ReadAsset<T>(string assetName, Action<IDisposable> recordDisposableObject)
	{
		if (string.IsNullOrEmpty(assetName))
		{
			throw new ArgumentNullException("assetName");
		}
		if (disposed)
		{
			throw new ObjectDisposedException("ContentManager");
		}
		object obj = null;
		Stream stream = null;
		try
		{
			stream = OpenStream(assetName);
		}
		catch (Exception ex)
		{
			stream = OpenStreamRaw<T>(assetName);
			if (stream == null)
			{
				throw new ContentLoadException("Could not load asset " + assetName + "! Error: " + ex.Message, ex);
			}
		}
		stream.Read(xnbHeader, 0, xnbHeader.Length);
		if (xnbHeader[0] == 88 && xnbHeader[1] == 78 && xnbHeader[2] == 66 && targetPlatformIdentifiers.Contains((char)xnbHeader[3]))
		{
			using BinaryReader xnbReader = new BinaryReader(stream);
			using ContentReader contentReader = GetContentReaderFromXnb(assetName, ref stream, xnbReader, (char)xnbHeader[3], recordDisposableObject);
			obj = contentReader.ReadAsset<T>();
			if (obj is GraphicsResource graphicsResource)
			{
				graphicsResource.Name = assetName;
			}
		}
		else
		{
			stream.Seek(0L, SeekOrigin.Begin);
			if (typeof(T) == typeof(Texture2D) || typeof(T) == typeof(Texture))
			{
				Texture2D texture2D = ((xnbHeader[0] != 68 || xnbHeader[1] != 68 || xnbHeader[2] != 83 || xnbHeader[3] != 32) ? Texture2D.FromStream(GetGraphicsDevice(), stream) : Texture2D.DDSFromStreamEXT(GetGraphicsDevice(), stream));
				texture2D.Name = assetName;
				obj = texture2D;
			}
			else if (typeof(T) == typeof(TextureCube))
			{
				TextureCube textureCube = TextureCube.DDSFromStreamEXT(GetGraphicsDevice(), stream);
				textureCube.Name = assetName;
				obj = textureCube;
			}
			else if (typeof(T) == typeof(SoundEffect))
			{
				SoundEffect soundEffect = SoundEffect.FromStream(stream);
				soundEffect.Name = assetName;
				obj = soundEffect;
			}
			else if (typeof(T) == typeof(Effect))
			{
				byte[] array = new byte[stream.Length];
				stream.Read(array, 0, (int)stream.Length);
				Effect effect = new Effect(GetGraphicsDevice(), array);
				effect.Name = assetName;
				obj = effect;
			}
			else if (typeof(T) == typeof(Song))
			{
				string name = (stream as FileStream).Name;
				stream.Close();
				obj = new Song(name);
			}
			else
			{
				if (!(typeof(T) == typeof(Video)))
				{
					stream.Close();
					throw new ContentLoadException("Could not load " + assetName + " asset!");
				}
				string name2 = (stream as FileStream).Name;
				stream.Close();
				obj = new Video(name2, GetGraphicsDevice());
				FNALoggerEXT.LogWarn("Video " + name2 + " does not have an XNB file! Hacking Duration property!");
			}
			if (obj is IDisposable disposable)
			{
				if (recordDisposableObject != null)
				{
					recordDisposableObject(disposable);
				}
				else
				{
					lock (assetLock)
					{
						disposableAssets.Add(disposable);
					}
				}
			}
			stream.Close();
		}
		return (T)obj;
	}

	private Stream OpenStreamRaw<T>(string assetName)
	{
		return null;
	}

	internal void RecordDisposable(IDisposable disposable)
	{
		Debug.Assert(disposable != null, "The disposable is null!");
		if (!disposableAssets.Contains(disposable))
		{
			disposableAssets.Add(disposable);
		}
	}

	internal GraphicsDevice GetGraphicsDevice()
	{
		if (graphicsDevice == null)
		{
			if (!(ServiceProvider.GetService(typeof(IGraphicsDeviceService)) is IGraphicsDeviceService graphicsDeviceService))
			{
				throw new ContentLoadException("No Graphics Device Service");
			}
			graphicsDevice = graphicsDeviceService.GraphicsDevice;
		}
		return graphicsDevice;
	}

	private ContentReader GetContentReaderFromXnb(string originalAssetName, ref Stream stream, BinaryReader xnbReader, char platform, Action<IDisposable> recordDisposableObject)
	{
		byte b = xnbReader.ReadByte();
		byte b2 = xnbReader.ReadByte();
		bool flag = (b2 & 0x80) != 0;
		if (b != 5 && b != 4)
		{
			throw new ContentLoadException("Invalid XNB version");
		}
		int num = xnbReader.ReadInt32();
		if (flag)
		{
			int num2 = num - 14;
			int num3 = xnbReader.ReadInt32();
			MemoryStream memoryStream = new MemoryStream(new byte[num3], 0, num3, writable: true, publiclyVisible: true);
			MemoryStream memoryStream2 = new MemoryStream(new byte[num2], 0, num2, writable: true, publiclyVisible: true);
			stream.Read(memoryStream2.GetBuffer(), 0, num2);
			LzxDecoder lzxDecoder = new LzxDecoder(16);
			int num4 = 0;
			long num5 = 0L;
			while (num5 < num2)
			{
				int num6 = memoryStream2.ReadByte();
				int num7 = memoryStream2.ReadByte();
				int num8 = (num6 << 8) | num7;
				int num9 = 32768;
				if (num6 == 255)
				{
					num6 = num7;
					num7 = (byte)memoryStream2.ReadByte();
					num9 = (num6 << 8) | num7;
					num6 = (byte)memoryStream2.ReadByte();
					num7 = (byte)memoryStream2.ReadByte();
					num8 = (num6 << 8) | num7;
					num5 += 5;
				}
				else
				{
					num5 += 2;
				}
				if (num8 == 0 || num9 == 0)
				{
					break;
				}
				lzxDecoder.Decompress(memoryStream2, num8, memoryStream, num9);
				num5 += num8;
				num4 += num9;
				memoryStream2.Seek(num5, SeekOrigin.Begin);
			}
			if (memoryStream.Position != num3)
			{
				throw new ContentLoadException("Decompression of " + originalAssetName + " failed. ");
			}
			memoryStream.Seek(0L, SeekOrigin.Begin);
			return new ContentReader(this, memoryStream, originalAssetName, b, platform, recordDisposableObject);
		}
		return new ContentReader(this, stream, originalAssetName, b, platform, recordDisposableObject);
	}

	private static void AddContentManager(ContentManager contentManager)
	{
		lock (ContentManagerLock)
		{
			bool flag = false;
			for (int num = ContentManagers.Count - 1; num >= 0; num--)
			{
				WeakReference weakReference = ContentManagers[num];
				if (weakReference.Target == contentManager)
				{
					flag = true;
				}
				if (!weakReference.IsAlive)
				{
					ContentManagers.RemoveAt(num);
				}
			}
			if (!flag)
			{
				ContentManagers.Add(new WeakReference(contentManager));
			}
		}
	}

	private static void RemoveContentManager(ContentManager contentManager)
	{
		lock (ContentManagerLock)
		{
			for (int num = ContentManagers.Count - 1; num >= 0; num--)
			{
				WeakReference weakReference = ContentManagers[num];
				if (!weakReference.IsAlive || weakReference.Target == contentManager)
				{
					ContentManagers.RemoveAt(num);
				}
			}
		}
	}
}
