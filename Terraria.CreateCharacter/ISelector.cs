using Microsoft.Xna.Framework;

namespace Terraria.CreateCharacter;

public interface ISelector
{
	void Draw(Vector2 Position, float Alpha);

	void Update();

	bool SelectLeft();

	bool SelectRight();

	bool SelectUp();

	bool SelectDown();

	void SetCursor(Vector2i CursorVect);

	/// <summary>
	/// Mouse support. Moves the selection to whatever the cursor points at and returns true
	/// only when the selection actually changed, so callers can react once per change instead
	/// of every frame. Selectors cache their layout during <see cref="Draw"/>, so this must be
	/// called after the first frame has been rendered.
	/// </summary>
	bool HitTest(Vector2 MousePosition);

	void Reset();

	void Show();

	void FlashSelection(int Duration);

	void CancelSelection();
}
