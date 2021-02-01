#nullable enable

namespace Windows.UI.Composition
{
	public partial class ShapeVisual : global::Windows.UI.Composition.ContainerVisual
	{
		public ShapeVisual(Compositor compositor)
			: base(compositor)
		{
			Shapes = new CompositionShapeCollection(compositor, this);
		}

		public CompositionViewBox? ViewBox { get; set; }

		public CompositionShapeCollection Shapes { get; }
	}
}
