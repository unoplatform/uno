#nullable enable

namespace Windows.UI.Composition;

public partial class ShapeVisual : ContainerVisual
{
	private CompositionViewBox? _viewBox;
	private CompositionShapeCollection? _shapes;

	public ShapeVisual(Compositor compositor)
		: base(compositor)
	{
	}

	public CompositionViewBox? ViewBox
	{
		get => _viewBox;
		set => SetProperty(ref _viewBox, value);
	}

	// This is lazy as we are using the `ShapeVisual` for UIElement, but lot of them are not creating shapes, reduce memory pressure.
	public CompositionShapeCollection Shapes
	{
		get
		{
			if (_shapes is null)
			{
				_shapes = new CompositionShapeCollection(Compositor, this);

				// Add this as context for the shape collection so we get
				// notified about changes in the shapes object graph.
				OnCompositionPropertyChanged(null, _shapes, nameof(Shapes));
			}

			return _shapes;
		}
	}
}
