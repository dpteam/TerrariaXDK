using Microsoft.Xna.Framework;

namespace Terraria.CreateCharacter;

public interface IAttributeWidget
{
	string ControlDescription { get; }

	string WidgetDescription { get; }

	void Draw(Vector2 Position, float Alpha);

	void Update();

	bool SelectLeft();

	bool SelectRight();

	bool SelectUp();

	bool SelectDown();

	void Back();

	void SetCursor(Vector2i CursorVect);

	/// <summary>Mouse support: selects the option under the cursor. See <see cref="ISelector.HitTest"/>.</summary>
	bool HitTest(Vector2 MousePosition);

	void Apply(Player Player);

	void Reset();

	void Show();

	void FlashSelection(int Duration);
}
