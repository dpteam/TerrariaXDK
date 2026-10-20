using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectPass
{
	private Effect parentEffect;

	private IntPtr parentTechnique;

	private uint pass;

	public string Name { get; private set; }

	public EffectAnnotationCollection Annotations { get; private set; }

	internal EffectPass(string name, EffectAnnotationCollection annotations, Effect parent, IntPtr technique, uint passIndex)
	{
		Name = name;
		Annotations = annotations;
		parentEffect = parent;
		parentTechnique = technique;
		pass = passIndex;
	}

	public void Apply()
	{
		if (parentTechnique != parentEffect.CurrentTechnique.TechniquePointer)
		{
			throw new InvalidOperationException("Applied a pass not in the current technique!");
		}
		parentEffect.OnApply();
		parentEffect.INTERNAL_applyEffect(pass);
	}
}
