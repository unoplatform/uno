#nullable enable

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

		public ShapeVisual CreateShapeVisual()
			=> new ShapeVisual(this);

		public CompositionSpriteShape CreateSpriteShape()
			=> new CompositionSpriteShape(this);

		public CompositionSpriteShape CreateSpriteShape(CompositionGeometry geometry)
			=> new CompositionSpriteShape(this, geometry);

		public CompositionPathGeometry CreatePathGeometry()
			=> new CompositionPathGeometry(this);

		public CompositionPathGeometry CreatePathGeometry(CompositionPath path)
			=> new CompositionPathGeometry(this, path);

		public CompositionSurfaceBrush CreateSurfaceBrush()
			=> new CompositionSurfaceBrush(this);

		public CompositionSurfaceBrush CreateSurfaceBrush(ICompositionSurface surface)
			=> new CompositionSurfaceBrush(this, surface);

		public CompositionGeometricClip CreateGeometricClip()
			=> new CompositionGeometricClip(this);

		public CompositionGeometricClip CreateGeometricClip(CompositionGeometry geometry)
			=> new CompositionGeometricClip(this) { Geometry = geometry };

		public CompositionPropertySet CreatePropertySet()
			=> new CompositionPropertySet(this);

		public InsetClip CreateInsetClip()
			=> new InsetClip(this);

		public InsetClip CreateInsetClip(float leftInset, float topInset, float rightInset, float bottomInset)
			=> new InsetClip(this) {
				LeftInset = leftInset,
				TopInset = topInset,
				RightInset = rightInset,
				BottomInset = bottomInset
			};

		internal void InvalidateRender() => InvalidateRenderPartial();

		partial void InvalidateRenderPartial();
	}
}
