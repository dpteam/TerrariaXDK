using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework.Design;

public class PlaneConverter : MathTypeConverter
{
	public PlaneConverter()
	{
		supportStringConvert = false;
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
	{
		return new Plane((Vector3)propertyValues["Normal"], (float)propertyValues["D"]);
	}
}
