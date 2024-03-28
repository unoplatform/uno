#nullable enable

using SkiaSharp;
using System;
using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class CompositionShape
{
	internal virtual void Render(in DrawingSession session)
	{
		using var localSession = BeginDrawing(in session);

		Draw(in session); // We use the session on purpose here!
	}

	internal virtual void Draw(in DrawingSession session)
	{
	}

	private DrawingSession? BeginDrawing(in DrawingSession session)
	{
		var offset = Offset;
		var transform = this.GetTransform();

		if (offset == Vector2.Zero && transform is { IsIdentity: true })
		{
			return default; // Use the session without saving it, nothing to dispose
		}

		session.Canvas.Save();

		if (offset != Vector2.Zero)
		{
			session.Canvas.Translate(offset.X, offset.Y);
		}

		// Intentionally not applying transform here.
		// Derived classes should be responsible to call GetTransform and use it appropriately.
		// For example, CompositionSpriteShape shouldn't "scale" the stroke thickness.

		return session;
	}
}
