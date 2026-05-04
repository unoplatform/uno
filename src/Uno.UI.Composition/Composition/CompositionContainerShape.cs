#nullable enable

using System;

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
				// CompositionShapeCollection requires a ShapeVisual owner today; pass null safely
				// since ContainerShape rendering is implemented inside this class and does not
				// rely on the parent visual reference for invalidation.
				_shapes = new CompositionShapeCollection(Compositor, null!);
				OnCompositionPropertyChanged(null, _shapes, nameof(Shapes));
			}

			return _shapes;
		}
	}
}
