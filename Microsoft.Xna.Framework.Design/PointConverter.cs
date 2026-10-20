using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework.Design;

public class PointConverter : MathTypeConverter
{
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is string text)
		{
			string[] array = text.Split(culture.TextInfo.ListSeparator.ToCharArray());
			return new Point(int.Parse(array[0], culture), int.Parse(array[1], culture));
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == typeof(string))
		{
			Point point = (Point)value;
			return string.Join(culture.TextInfo.ListSeparator + " ", point.X.ToString(culture), point.Y.ToString(culture));
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		return new Point((int)propertyValues["X"], (int)propertyValues["Y"]);
	}
}
