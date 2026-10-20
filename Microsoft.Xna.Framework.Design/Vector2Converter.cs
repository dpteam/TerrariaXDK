using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework.Design;

public class Vector2Converter : MathTypeConverter
{
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is string text)
		{
			string[] array = text.Split(culture.TextInfo.ListSeparator.ToCharArray());
			return new Vector2(float.Parse(array[0], culture), float.Parse(array[1], culture));
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == typeof(string))
		{
			Vector2 vector = (Vector2)value;
			return string.Join(culture.TextInfo.ListSeparator + " ", vector.X.ToString(culture), vector.Y.ToString(culture));
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		return new Vector2((float)propertyValues["X"], (float)propertyValues["Y"]);
	}
}
