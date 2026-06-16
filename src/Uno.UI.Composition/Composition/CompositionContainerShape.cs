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
				// CompositionShapeCollection accepts a null ShapeVisual owner; ContainerShape
				// rendering is implemented in this class and doesn't use the parent visual reference
				// for invalidation.
				_shapes = new CompositionShapeCollection(Compositor, null);
				OnCompositionPropertyChanged(null, _shapes, nameof(Shapes));
			}

			return _shapes;
		}
	}
}
