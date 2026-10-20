using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

public sealed class ContentTypeReaderManager
{
	private Dictionary<Type, ContentTypeReader> contentReaders;

	private static readonly object locker;

	private static readonly string assemblyName;

	private static readonly Dictionary<Type, ContentTypeReader> contentReadersCache;

	private static readonly string[] nestedMark;

	private static bool falseflag;

	private static Dictionary<string, Func<ContentTypeReader>> typeCreators;

	static ContentTypeReaderManager()
	{
		nestedMark = new string[1] { "[[" };
		falseflag = false;
		typeCreators = new Dictionary<string, Func<ContentTypeReader>>();
		locker = new object();
		contentReadersCache = new Dictionary<Type, ContentTypeReader>(255);
		assemblyName = typeof(ContentTypeReaderManager).Assembly.FullName;
	}

	internal ContentTypeReaderManager()
	{
	}

	public ContentTypeReader GetTypeReader(Type targetType)
	{
		if (contentReaders.TryGetValue(targetType, out var value))
		{
			return value;
		}
		Type type = Type.GetType(PrepareType(targetType.FullName), throwOnError: false);
		if (type != null && contentReaders.TryGetValue(type, out value))
		{
			return value;
		}
		return null;
	}

	internal ContentTypeReader[] LoadAssetReaders(ContentReader reader)
	{
		if (falseflag)
		{
			ByteReader byteReader = new ByteReader();
			SByteReader sByteReader = new SByteReader();
			DateTimeReader dateTimeReader = new DateTimeReader();
			DecimalReader decimalReader = new DecimalReader();
			BoundingSphereReader boundingSphereReader = new BoundingSphereReader();
			BoundingFrustumReader boundingFrustumReader = new BoundingFrustumReader();
			RayReader rayReader = new RayReader();
			ListReader<char> listReader = new ListReader<char>();
			ListReader<Rectangle> listReader2 = new ListReader<Rectangle>();
			ArrayReader<Rectangle> arrayReader = new ArrayReader<Rectangle>();
			ListReader<Vector3> listReader3 = new ListReader<Vector3>();
			ListReader<StringReader> listReader4 = new ListReader<StringReader>();
			ListReader<int> listReader5 = new ListReader<int>();
			SpriteFontReader spriteFontReader = new SpriteFontReader();
			Texture2DReader texture2DReader = new Texture2DReader();
			CharReader charReader = new CharReader();
			RectangleReader rectangleReader = new RectangleReader();
			StringReader stringReader = new StringReader();
			Vector2Reader vector2Reader = new Vector2Reader();
			Vector3Reader vector3Reader = new Vector3Reader();
			Vector4Reader vector4Reader = new Vector4Reader();
			CurveReader curveReader = new CurveReader();
			IndexBufferReader indexBufferReader = new IndexBufferReader();
			BoundingBoxReader boundingBoxReader = new BoundingBoxReader();
			MatrixReader matrixReader = new MatrixReader();
			BasicEffectReader basicEffectReader = new BasicEffectReader();
			VertexBufferReader vertexBufferReader = new VertexBufferReader();
			AlphaTestEffectReader alphaTestEffectReader = new AlphaTestEffectReader();
			EnumReader<SpriteEffects> enumReader = new EnumReader<SpriteEffects>();
			ArrayReader<float> arrayReader2 = new ArrayReader<float>();
			ArrayReader<Vector2> arrayReader3 = new ArrayReader<Vector2>();
			ListReader<Vector2> listReader6 = new ListReader<Vector2>();
			ArrayReader<Matrix> arrayReader4 = new ArrayReader<Matrix>();
			EnumReader<Blend> enumReader2 = new EnumReader<Blend>();
			NullableReader<Rectangle> nullableReader = new NullableReader<Rectangle>();
			EffectMaterialReader effectMaterialReader = new EffectMaterialReader();
			ExternalReferenceReader externalReferenceReader = new ExternalReferenceReader();
			SoundEffectReader soundEffectReader = new SoundEffectReader();
			SongReader songReader = new SongReader();
			ModelReader modelReader = new ModelReader();
			Int32Reader int32Reader = new Int32Reader();
		}
		int num = reader.Read7BitEncodedInt();
		ContentTypeReader[] array = new ContentTypeReader[num];
		BitArray bitArray = new BitArray(num);
		contentReaders = new Dictionary<Type, ContentTypeReader>(num);
		lock (locker)
		{
			for (int i = 0; i < num; i++)
			{
				string text = reader.ReadString();
				if (typeCreators.TryGetValue(text, out var value))
				{
					array[i] = value();
					bitArray[i] = true;
				}
				else
				{
					string type = text;
					type = PrepareType(type);
					Type type2 = Type.GetType(type);
					if (!(type2 != null))
					{
						throw new ContentLoadException("Could not find ContentTypeReader Type. Please ensure the name of the Assembly that contains the Type matches the assembly in the full type name: " + text + " (" + type + ")");
					}
					if (!contentReadersCache.TryGetValue(type2, out var value2))
					{
						try
						{
							value2 = type2.GetDefaultConstructor().Invoke(null) as ContentTypeReader;
						}
						catch (TargetInvocationException innerException)
						{
							throw new InvalidOperationException("Failed to get default constructor for ContentTypeReader. To work around, add a creation function to ContentTypeReaderManager.AddTypeCreator() with the following failed type string: " + text, innerException);
						}
						bitArray[i] = true;
						contentReadersCache.Add(type2, value2);
					}
					array[i] = value2;
				}
				if (array[i].TargetType != null)
				{
					contentReaders.Add(array[i].TargetType, array[i]);
				}
				reader.ReadInt32();
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (bitArray.Get(j))
				{
					array[j].Initialize(this);
				}
			}
		}
		return array;
	}

	internal static void AddTypeCreator(string typeString, Func<ContentTypeReader> createFunction)
	{
		if (!typeCreators.ContainsKey(typeString))
		{
			typeCreators.Add(typeString, createFunction);
		}
	}

	internal static void ClearTypeCreators()
	{
		typeCreators.Clear();
	}

	internal static string PrepareType(string type)
	{
		int num = type.Split(nestedMark, StringSplitOptions.None).Length - 1;
		string text = type;
		for (int i = 0; i < num; i++)
		{
			text = Regex.Replace(text, "\\[(.+?), Version=.+?\\]", "[$1]");
		}
		if (text.Contains("PublicKeyToken"))
		{
			text = Regex.Replace(text, "(.+?), Version=.+?$", "$1");
		}
		text = text.Replace(", Microsoft.Xna.Framework.Graphics", ", " + assemblyName);
		text = text.Replace(", Microsoft.Xna.Framework.Video", ", " + assemblyName);
		text = text.Replace(", Microsoft.Xna.Framework", ", " + assemblyName);
		return text.Replace(", MonoGame.Framework", ", " + assemblyName);
	}
}
