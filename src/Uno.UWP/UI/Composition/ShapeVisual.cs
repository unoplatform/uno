namespace Windows.UI.Composition
{
	public partial class ShapeVisual : global::Windows.UI.Composition.ContainerVisual
	{
		public ShapeVisual(Compositor compositor)
			: base(compositor)
		{
			Shapes = new CompositionShapeCollection(this);
		}

		public CompositionViewBox ViewBox { get; set; }

		public CompositionShapeCollection Shapes { get; }
	}
}
