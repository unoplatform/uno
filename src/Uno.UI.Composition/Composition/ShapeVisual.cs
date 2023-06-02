#nullable enable

namespace Windows.UI.Composition
{
	public partial class ShapeVisual : ContainerVisual
	{
		private CompositionViewBox? _viewBox;
		private CompositionShapeCollection? _shapes;

		public ShapeVisual(Compositor compositor)
			: base(compositor)
		{
			// Add this as context for the shape collection so we get
			// notified about changes in the shapes object graph.
			OnCompositionPropertyChanged(null, Shapes, nameof(Shapes));
		}

		public CompositionViewBox? ViewBox
		{
			get => _viewBox;
			set => SetProperty(ref _viewBox, value);
		}

		// This ia lazy as we are using the `ShapeVisual` for UIElement, but lot of them are not creating shapes, reduce memory pressure.
		public CompositionShapeCollection Shapes => _shapes ??= new CompositionShapeCollection(Compositor, this);
	}
}
