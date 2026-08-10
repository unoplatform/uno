#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class CompositionContainerShape : CompositionShape
{
	private CompositionShapeCollection? _shapes;

	internal CompositionContainerShape(Compositor compositor) : base(compositor)
	{
	}

	public CompositionShapeCollection Shapes
	{
		get
		{
			if (_shapes is null)
			{
				// CompositionShapeCollection accepts a null ShapeVisual owner; ContainerShape
				// rendering is implemented in this class and doesn't use the parent visual reference
				// for invalidation.
				_shapes = new CompositionShapeCollection(Compositor, null);
				OnCompositionPropertyChanged(null, _shapes, nameof(Shapes));
			}

			return _shapes;
		}
	}

	internal override void Paint(in Visual.PaintingSession session)
	{
		if (_shapes is { Count: > 0 } shapes)
		{
			// CompositionShape.Render() already applied this container's Offset and
			// CombinedTransformMatrix to the canvas before delegating to Paint, so we just
			// iterate children — they'll apply their own Offset/transform on top.
			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}
		}
	}

	internal override bool CanPaint()
	{
		if (_shapes is { Count: > 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				if (shapes[i].CanPaint())
				{
					return true;
				}
			}
		}

		return false;
	}

	internal override bool HitTest(Point point)
	{
		if (_shapes is { Count: > 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				if (shapes[i].HitTest(point))
				{
					return true;
				}
			}
		}

		return false;
	}
}
