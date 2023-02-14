#nullable enable

namespace Microsoft.UI.Composition
{
	public partial class ShapeVisual : ContainerVisual
	{
		private CompositionViewBox? _viewBox;

		public ShapeVisual(Compositor compositor)
			: base(compositor)
		{
			Shapes = new CompositionShapeCollection(compositor, this);

			// Add this as context for the shape collection so we get
			// notified about changes in the shapes object graph.
			OnCompositionPropertyChanged(null, Shapes, nameof(Shapes));
		}

		public CompositionViewBox? ViewBox
		{
			get => _viewBox;
			set => SetProperty(ref _viewBox, value);
		}

		public CompositionShapeCollection Shapes { get; }
	}
}
