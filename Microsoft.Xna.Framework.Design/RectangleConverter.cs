using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework.Design;

public class RectangleConverter : MathTypeConverter
{
	public RectangleConverter()
	{
		supportStringConvert = false;
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		return new Rectangle((int)propertyValues["X"], (int)propertyValues["Y"], (int)propertyValues["Width"], (int)propertyValues["Height"]);
	}
}
