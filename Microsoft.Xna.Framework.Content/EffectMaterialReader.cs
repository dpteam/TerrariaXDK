#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class EffectMaterialReader : ContentTypeReader<EffectMaterial>
{
	protected internal override EffectMaterial Read(ContentReader input, EffectMaterial existingInstance)
	{
		Effect cloneSource = input.ReadExternalReference<Effect>();
		EffectMaterial effectMaterial = new EffectMaterial(cloneSource);
		Dictionary<string, object> dictionary = input.ReadObject<Dictionary<string, object>>();
		foreach (KeyValuePair<string, object> item in dictionary)
		{
			EffectParameter effectParameter = effectMaterial.Parameters[item.Key];
			if (effectParameter != null)
			{
				Type type = item.Value.GetType();
				if (typeof(Texture).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Texture)item.Value);
					continue;
				}
				if (typeof(int).IsAssignableFrom(type))
				{
					effectParameter.SetValue((int)item.Value);
					continue;
				}
				if (typeof(int[]).IsAssignableFrom(type))
				{
					effectParameter.SetValue((int[])item.Value);
					continue;
				}
				if (typeof(bool).IsAssignableFrom(type))
				{
					effectParameter.SetValue((bool)item.Value);
					continue;
				}
				if (typeof(float).IsAssignableFrom(type))
				{
					effectParameter.SetValue((float)item.Value);
					continue;
				}
				if (typeof(float[]).IsAssignableFrom(type))
				{
					effectParameter.SetValue((float[])item.Value);
					continue;
				}
				if (typeof(Vector2).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Vector2)item.Value);
					continue;
				}
				if (typeof(Vector2[]).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Vector2[])item.Value);
					continue;
				}
				if (typeof(Vector3).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Vector3)item.Value);
					continue;
				}
				if (typeof(Vector3[]).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Vector3[])item.Value);
					continue;
				}
				if (typeof(Vector4).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Vector4)item.Value);
					continue;
				}
				if (typeof(Vector4[]).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Vector4[])item.Value);
					continue;
				}
				if (typeof(Matrix).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Matrix)item.Value);
					continue;
				}
				if (typeof(Matrix[]).IsAssignableFrom(type))
				{
					effectParameter.SetValue((Matrix[])item.Value);
					continue;
				}
				if (!typeof(Quaternion).IsAssignableFrom(type))
				{
					throw new NotSupportedException("Parameter type is not supported");
				}
				effectParameter.SetValue((Quaternion)item.Value);
			}
			else
			{
				Debug.WriteLine("No parameter " + item.Key);
			}
		}
		return effectMaterial;
	}
}
