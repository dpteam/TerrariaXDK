using System;
using System.Collections.Generic;
using System.IO;
using MonoGame.Utilities;

namespace Microsoft.Xna.Framework.Content;

public sealed class ContentReader : BinaryReader
{
	internal int version;

	internal char platform;

	private ContentManager contentManager;

	private Action<IDisposable> recordDisposableObject;

	private ContentTypeReaderManager typeReaderManager;

	private ContentTypeReader[] typeReaders;

	private string assetName;

	private int sharedResourceCount;

	private object[] sharedResources;

	private List<Action<object>>[] sharedResourceFixups;

	public ContentManager ContentManager => contentManager;

	public string AssetName => assetName;

	internal ContentTypeReader[] TypeReaders => typeReaders;

	internal ContentReader(ContentManager manager, Stream stream, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject)
		: base(stream)
	{
		this.recordDisposableObject = recordDisposableObject;
		contentManager = manager;
		this.assetName = assetName;
		this.version = version;
		this.platform = platform;
	}

	public T ReadExternalReference<T>()
	{
		string text = ReadString();
		if (!string.IsNullOrEmpty(text))
		{
			return contentManager.Load<T>(FileHelpers.ResolveRelativePath(assetName, text));
		}
		return default;
	}

	public Matrix ReadMatrix()
	{
		return new Matrix
		{
			M11 = ReadSingle(),
			M12 = ReadSingle(),
			M13 = ReadSingle(),
			M14 = ReadSingle(),
			M21 = ReadSingle(),
			M22 = ReadSingle(),
			M23 = ReadSingle(),
			M24 = ReadSingle(),
			M31 = ReadSingle(),
			M32 = ReadSingle(),
			M33 = ReadSingle(),
			M34 = ReadSingle(),
			M41 = ReadSingle(),
			M42 = ReadSingle(),
			M43 = ReadSingle(),
			M44 = ReadSingle()
		};
	}

	public T ReadObject<T>()
	{
		return ReadObject(default(T));
	}

	public T ReadObject<T>(ContentTypeReader typeReader)
	{
		T result = (T)typeReader.Read(this, default(T));
		RecordDisposable(result);
		return result;
	}

	public T ReadObject<T>(T existingInstance)
	{
		return InnerReadObject(existingInstance);
	}

	public T ReadObject<T>(ContentTypeReader typeReader, T existingInstance)
	{
		if (!typeReader.TargetType.IsValueType)
		{
			return ReadObject(existingInstance);
		}
		T result = (T)typeReader.Read(this, existingInstance);
		RecordDisposable(result);
		return result;
	}

	public Quaternion ReadQuaternion()
	{
		return new Quaternion
		{
			X = ReadSingle(),
			Y = ReadSingle(),
			Z = ReadSingle(),
			W = ReadSingle()
		};
	}

	public T ReadRawObject<T>()
	{
		return ReadRawObject(default(T));
	}

	public T ReadRawObject<T>(ContentTypeReader typeReader)
	{
		return ReadRawObject<T>(typeReader, default);
	}

	public T ReadRawObject<T>(T existingInstance)
	{
		Type typeFromHandle = typeof(T);
		ContentTypeReader[] array = typeReaders;
		ContentTypeReader[] array2 = array;
		foreach (ContentTypeReader contentTypeReader in array2)
		{
			if (contentTypeReader.TargetType == typeFromHandle)
			{
				return ReadRawObject(contentTypeReader, existingInstance);
			}
		}
		throw new NotSupportedException();
	}

	public T ReadRawObject<T>(ContentTypeReader typeReader, T existingInstance)
	{
		return (T)typeReader.Read(this, existingInstance);
	}

	public void ReadSharedResource<T>(Action<T> fixup)
	{
		int num = Read7BitEncodedInt();
		if (num <= 0)
		{
			return;
		}
		sharedResourceFixups[num - 1].Add((object v) =>
		{
			if (!(v is T))
			{
				throw new ContentLoadException("Error loading shared resource. Expected type " + typeof(T).Name + ", received type " + v.GetType().Name);
			}
			fixup((T)v);
		});
	}

	public Vector2 ReadVector2()
	{
		return new Vector2
		{
			X = ReadSingle(),
			Y = ReadSingle()
		};
	}

	public Vector3 ReadVector3()
	{
		return new Vector3
		{
			X = ReadSingle(),
			Y = ReadSingle(),
			Z = ReadSingle()
		};
	}

	public Vector4 ReadVector4()
	{
		return new Vector4
		{
			X = ReadSingle(),
			Y = ReadSingle(),
			Z = ReadSingle(),
			W = ReadSingle()
		};
	}

	public Color ReadColor()
	{
		return new Color
		{
			R = ReadByte(),
			G = ReadByte(),
			B = ReadByte(),
			A = ReadByte()
		};
	}

	internal object ReadAsset<T>()
	{
		InitializeTypeReaders();
		object result = ReadObject<T>();
		ReadSharedResources();
		return result;
	}

	internal void InitializeTypeReaders()
	{
		typeReaderManager = new ContentTypeReaderManager();
		typeReaders = typeReaderManager.LoadAssetReaders(this);
		sharedResourceCount = Read7BitEncodedInt();
		sharedResources = new object[sharedResourceCount];
		sharedResourceFixups = new List<Action<object>>[sharedResourceCount];
		for (int i = 0; i < sharedResourceCount; i++)
		{
			sharedResourceFixups[i] = new List<Action<object>>();
		}
	}

	internal void ReadSharedResources()
	{
		for (int i = 0; i < sharedResourceCount; i++)
		{
			sharedResources[i] = InnerReadObject<object>(null);
		}
		for (int j = 0; j < sharedResourceCount; j++)
		{
			object obj = sharedResources[j];
			foreach (Action<object> item in sharedResourceFixups[j])
			{
				item(obj);
			}
		}
	}

	internal new int Read7BitEncodedInt()
	{
		return base.Read7BitEncodedInt();
	}

	internal BoundingSphere ReadBoundingSphere()
	{
		Vector3 center = ReadVector3();
		float radius = ReadSingle();
		return new BoundingSphere(center, radius);
	}

	private T InnerReadObject<T>(T existingInstance)
	{
		int num = Read7BitEncodedInt();
		if (num == 0)
		{
			return existingInstance;
		}
		if (num > typeReaders.Length)
		{
			throw new ContentLoadException("Incorrect type reader index found!");
		}
		ContentTypeReader contentTypeReader = typeReaders[num - 1];
		T result = (T)contentTypeReader.Read(this, default(T));
		RecordDisposable(result);
		return result;
	}

	private void RecordDisposable<T>(T result)
	{
		if (result is IDisposable disposable)
		{
			if (recordDisposableObject != null)
			{
				recordDisposableObject(disposable);
			}
			else
			{
				contentManager.RecordDisposable(disposable);
			}
		}
	}
}
