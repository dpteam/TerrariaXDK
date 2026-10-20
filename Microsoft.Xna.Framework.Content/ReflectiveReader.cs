using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Xna.Framework.Content;

internal class ReflectiveReader<T> : ContentTypeReader
{
	private delegate void ReadElement(ContentReader input, object parent);

	private List<ReadElement> readers;

	private ConstructorInfo constructor;

	private ContentTypeReader baseTypeReader;

	public override bool CanDeserializeIntoExistingObject => TargetType.IsClass;

	internal ReflectiveReader()
		: base(typeof(T))
	{
	}

	protected internal override void Initialize(ContentTypeReaderManager manager)
	{
		base.Initialize(manager);
		Type baseType = TargetType.BaseType;
		if (baseType != null && baseType != typeof(object))
		{
			baseTypeReader = manager.GetTypeReader(baseType);
		}
		constructor = TargetType.GetDefaultConstructor();
		PropertyInfo[] properties = TargetType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		FieldInfo[] fields = TargetType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		readers = new List<ReadElement>(fields.Length + properties.Length);
		PropertyInfo[] array = properties;
		PropertyInfo[] array2 = array;
		foreach (PropertyInfo propertyInfo in array2)
		{
			MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic: true);
			if (!(getMethod == null) && !(getMethod != getMethod.GetBaseDefinition()))
			{
				ReadElement elementReader = GetElementReader(manager, propertyInfo);
				if (elementReader != null)
				{
					readers.Add(elementReader);
				}
			}
		}
		FieldInfo[] array3 = fields;
		FieldInfo[] array4 = array3;
		foreach (FieldInfo member in array4)
		{
			ReadElement elementReader2 = GetElementReader(manager, member);
			if (elementReader2 != null)
			{
				readers.Add(elementReader2);
			}
		}
	}

	protected internal override object Read(ContentReader input, object existingInstance)
	{
		T val;
		if (existingInstance != null)
		{
			val = (T)existingInstance;
		}
		else
		{
			val = ((!(constructor == null)) ? ((T)constructor.Invoke(null)) : ((T)Activator.CreateInstance(typeof(T))));
		}
		if (baseTypeReader != null)
		{
			baseTypeReader.Read(input, val);
		}
		object obj = val;
		foreach (ReadElement reader in readers)
		{
			reader(input, obj);
		}
		val = (T)obj;
		return val;
	}

	private static ReadElement GetElementReader(ContentTypeReaderManager manager, MemberInfo member)
	{
		PropertyInfo property = member as PropertyInfo;
		FieldInfo fieldInfo = member as FieldInfo;
		if (property != null)
		{
			if (!property.CanRead)
			{
				return null;
			}
			if (property.GetIndexParameters().Length != 0)
			{
				return null;
			}
		}
		Attribute customAttribute = Attribute.GetCustomAttribute(member, typeof(ContentSerializerIgnoreAttribute));
		if (customAttribute != null)
		{
			return null;
		}
		ContentSerializerAttribute contentSerializerAttribute = Attribute.GetCustomAttribute(member, typeof(ContentSerializerAttribute)) as ContentSerializerAttribute;
		if (contentSerializerAttribute == null)
		{
			if (property != null)
			{
				MethodInfo getMethod = property.GetGetMethod(nonPublic: true);
				if (getMethod != null && !getMethod.IsPublic)
				{
					return null;
				}
				MethodInfo setMethod = property.GetSetMethod(nonPublic: true);
				if (setMethod != null && !setMethod.IsPublic)
				{
					return null;
				}
				if (!property.CanWrite)
				{
					ContentTypeReader typeReader = manager.GetTypeReader(property.PropertyType);
					if (typeReader == null || !typeReader.CanDeserializeIntoExistingObject)
					{
						return null;
					}
				}
			}
			else
			{
				if (!fieldInfo.IsPublic)
				{
					return null;
				}
				if (fieldInfo.IsInitOnly)
				{
					return null;
				}
			}
		}
		Type type;
		Action<object, object> setter;
		if (property != null)
		{
			type = property.PropertyType;
			if (property.CanWrite)
			{
				setter = (object o, object v) =>
				{
					property.SetValue(o, v, null);
				};
			}
			else
			{
				setter = delegate
				{
				};
			}
		}
		else
		{
			type = fieldInfo.FieldType;
			setter = fieldInfo.SetValue;
		}
		if (contentSerializerAttribute != null && contentSerializerAttribute.SharedResource)
		{
			return (ContentReader input, object parent) =>
			{
				Action<object> fixup = (object value) =>
				{
					setter(parent, value);
				};
				input.ReadSharedResource(fixup);
			};
		}
		ContentTypeReader reader = manager.GetTypeReader(type);
		if (reader == null)
		{
			throw new ContentLoadException("Content reader could not be found for " + type.FullName + " type.");
		}
		Func<object, object> construct = (object parent) => (object)null;
		if (property != null && !property.CanWrite)
		{
			construct = (object parent) => property.GetValue(parent, null);
		}
		return (ContentReader input, object parent) =>
		{
			object existingInstance = construct(parent);
			object arg = input.ReadObject(reader, existingInstance);
			setter(parent, arg);
		};
	}
}
