#nullable enable

using SkiaSharp;
using System;
using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Windows.UI.Composition;

public partial class CompositionShape
{
	internal virtual void Render(in Visual.PaintingSession session)
	{
		var offset = Offset;
		var transform = this.GetTransform();

		if (offset != Vector2.Zero || transform is not { IsIdentity: true })
		{
			session.Canvas.Save();

			if (offset != Vector2.Zero)
			{
				session.Canvas.Translate(offset.X, offset.Y);
			}

			// Intentionally not applying transform here.
			// Derived classes should be responsible to call GetTransform and use it appropriately.
			// For example, CompositionSpriteShape shouldn't "scale" the stroke thickness.
		}

		Paint(in session);

		if (offset != Vector2.Zero || transform is not { IsIdentity: true })
		{
			session.Canvas.Restore();
		}
	}

	internal virtual void Paint(in Visual.PaintingSession session)
	{
	}
}
