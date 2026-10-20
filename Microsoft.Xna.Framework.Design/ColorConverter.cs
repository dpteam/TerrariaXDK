using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework.Design;

public class ColorConverter : MathTypeConverter
{
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is string text)
		{
			string[] array = text.Split(culture.TextInfo.ListSeparator.ToCharArray());
			return new Color(int.Parse(array[0], culture), int.Parse(array[1], culture), int.Parse(array[2], culture), int.Parse(array[3], culture));
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == typeof(string))
		{
			Color color = (Color)value;
			return string.Join(culture.TextInfo.ListSeparator + " ", color.R.ToString(culture), color.G.ToString(culture), color.B.ToString(culture), color.A.ToString(culture));
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		return new Color((int)propertyValues["R"], (int)propertyValues["G"], (int)propertyValues["B"], (int)propertyValues["A"]);
	}
}
