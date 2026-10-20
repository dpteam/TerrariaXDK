using System;
using System.ComponentModel;

namespace Microsoft.Xna.Framework.Design;

public class MathTypeConverter : ExpandableObjectConverter
{
	protected PropertyDescriptorCollection propertyDescriptions;

	protected bool supportStringConvert;

	public MathTypeConverter()
	{
		supportStringConvert = true;
	}

	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		if (supportStringConvert && sourceType == typeof(string))
		{
			return true;
		}
		return base.CanConvertFrom(context, sourceType);
	}

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		if (supportStringConvert && destinationType == typeof(string))
		{
			return true;
		}
		return base.CanConvertTo(context, destinationType);
	}

	public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	{
		return propertyDescriptions;
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}
