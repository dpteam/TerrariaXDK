namespace Terraria;

public struct ControlDesc(int LabelAlignment, int LabelX, int LabelY, string Text)
{
	public int Alignment = LabelAlignment;

	public ushort X = (ushort)LabelX;

	public ushort Y = (ushort)LabelY;

	public string Label = Text;
}
