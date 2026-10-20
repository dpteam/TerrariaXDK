using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework.Design;

public class QuaternionConverter : MathTypeConverter
{
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is string text)
		{
			string[] array = text.Split(culture.TextInfo.ListSeparator.ToCharArray());
			return new Quaternion(float.Parse(array[0], culture), float.Parse(array[1], culture), float.Parse(array[2], culture), float.Parse(array[3], culture));
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == typeof(string))
		{
			Quaternion quaternion = (Quaternion)value;
			return string.Join(culture.TextInfo.ListSeparator + " ", quaternion.X.ToString(culture), quaternion.Y.ToString(culture), quaternion.Z.ToString(culture), quaternion.W.ToString(culture));
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		return new Quaternion((float)propertyValues["X"], (float)propertyValues["Y"], (float)propertyValues["Z"], (float)propertyValues["W"]);
	}
}
