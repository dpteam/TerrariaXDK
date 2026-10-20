using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework.Design;

public class Vector3Converter : MathTypeConverter
{
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is string text)
		{
			string[] array = text.Split(culture.TextInfo.ListSeparator.ToCharArray());
			return new Vector3(float.Parse(array[0], culture), float.Parse(array[1], culture), float.Parse(array[2], culture));
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == typeof(string))
		{
			Vector3 vector = (Vector3)value;
			return string.Join(culture.TextInfo.ListSeparator + " ", vector.X.ToString(culture), vector.Y.ToString(culture), vector.Z.ToString(culture));
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		return new Vector3((float)propertyValues["X"], (float)propertyValues["Y"], (float)propertyValues["Z"]);
	}
}
