namespace Microsoft.Xna.Framework.Content;

internal class CurveReader : ContentTypeReader<Curve>
{
	protected internal override Curve Read(ContentReader input, Curve existingInstance)
	{
		Curve curve = existingInstance;
		if (curve == null)
		{
			curve = new Curve();
		}
		curve.PreLoop = (CurveLoopType)input.ReadInt32();
		curve.PostLoop = (CurveLoopType)input.ReadInt32();
		int num = input.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			float position = input.ReadSingle();
			float value = input.ReadSingle();
			float tangentIn = input.ReadSingle();
			float tangentOut = input.ReadSingle();
			CurveContinuity continuity = (CurveContinuity)input.ReadInt32();
			curve.Keys.Add(new CurveKey(position, value, tangentIn, tangentOut, continuity));
		}
		return curve;
	}
}
