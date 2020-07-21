using Windows.UI;

namespace Windows.UI.Composition
{
	public partial class Compositor : global::System.IDisposable
	{
		private static object _gate = new object();

		public ContainerVisual CreateContainerVisual()
			=> new ContainerVisual(this)
			{
			};

		public SpriteVisual CreateSpriteVisual()
			=> new SpriteVisual(this)
			{
			};

		public CompositionColorBrush CreateColorBrush()
			=> new CompositionColorBrush(this)
			{
			};

		public CompositionColorBrush CreateColorBrush(Color color)
			=> new CompositionColorBrush(this)
			{
				Color = color
			};

		public ScalarKeyFrameAnimation CreateScalarKeyFrameAnimation()
			=> new ScalarKeyFrameAnimation(this);

		public CompositionScopedBatch CreateScopedBatch(CompositionBatchTypes batchType)
			=> new CompositionScopedBatch(this, batchType);

		public global::Windows.UI.Composition.ShapeVisual CreateShapeVisual()
			=> new ShapeVisual(this);

		public global::Windows.UI.Composition.CompositionSpriteShape CreateSpriteShape()
			=> new CompositionSpriteShape();

		public CompositionSpriteShape CreateSpriteShape(CompositionGeometry geometry)
			=> new CompositionSpriteShape(geometry);

		public CompositionPathGeometry CreatePathGeometry()
			=> new CompositionPathGeometry(this);

		public global::Windows.UI.Composition.CompositionPathGeometry CreatePathGeometry(global::Windows.UI.Composition.CompositionPath path)
			=> new CompositionPathGeometry(this, path);
	}
}
