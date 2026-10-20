using System;

namespace Microsoft.Xna.Framework;

public interface IDrawable
{
	int DrawOrder { get; }

	bool Visible { get; }

	event EventHandler<EventArgs> DrawOrderChanged;

	event EventHandler<EventArgs> VisibleChanged;

	void Draw(GameTime gameTime);
}
