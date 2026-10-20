using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework.Design;

public class RayConverter : MathTypeConverter
{
	public RayConverter()
	{
		supportStringConvert = false;
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		return base.ConvertFrom(context, culture, value);
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		return new Ray((Vector3)propertyValues["Position"], (Vector3)propertyValues["Direction"]);
	}
}
