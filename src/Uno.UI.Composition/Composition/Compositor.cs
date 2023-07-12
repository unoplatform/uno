#nullable enable

using System.Numerics;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class Compositor : global::System.IDisposable
	{
		private static Compositor? _sharedCompositor;

		public Compositor()
		{
			Current = this;
		}

		internal static Compositor GetSharedCompositor() => _sharedCompositor ??= new Compositor();
		internal static Compositor? Current;

		public ContainerVisual CreateContainerVisual()
			=> new ContainerVisual(this);

		public SpriteVisual CreateSpriteVisual()
			=> new SpriteVisual(this);

		public CompositionColorBrush CreateColorBrush()
			=> new CompositionColorBrush(this);

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

		public CompositionEllipseGeometry CreateEllipseGeometry()
			=> new CompositionEllipseGeometry(this);

		public CompositionLineGeometry CreateLineGeometry()
			=> new CompositionLineGeometry(this);

		public CompositionRectangleGeometry CreateRectangleGeometry()
			=> new CompositionRectangleGeometry(this);

		public CompositionRoundedRectangleGeometry CreateRoundedRectangleGeometry()
			=> new CompositionRoundedRectangleGeometry(this);

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
			=> new InsetClip(this)
			{
				LeftInset = leftInset,
				TopInset = topInset,
				RightInset = rightInset,
				BottomInset = bottomInset
			};

		public RectangleClip CreateRectangleClip()
			=> new RectangleClip(this);

		public RectangleClip CreateRectangleClip(float left, float top, float right, float bottom)
			=> new RectangleClip(this)
			{
				Left = left,
				Top = top,
				Right = right,
				Bottom = bottom
			};

		public RectangleClip CreateRectangleClip(
			float left,
			float top,
			float right,
			float bottom,
			Vector2 topLeftRadius,
			Vector2 topRightRadius,
			Vector2 bottomRightRadius,
			Vector2 bottomLeftRadius)
			=> new RectangleClip(this)
			{
				Left = left,
				Top = top,
				Right = right,
				Bottom = bottom,
				TopLeftRadius = topLeftRadius,
				TopRightRadius = topRightRadius,
				BottomRightRadius = bottomRightRadius,
				BottomLeftRadius = bottomLeftRadius
			};

		public CompositionLinearGradientBrush CreateLinearGradientBrush()
			=> new CompositionLinearGradientBrush(this);

		public CompositionRadialGradientBrush CreateRadialGradientBrush()
			=> new CompositionRadialGradientBrush(this);

		public CompositionColorGradientStop CreateColorGradientStop()
			=> new CompositionColorGradientStop(this);

		public CompositionColorGradientStop CreateColorGradientStop(float offset, Color color)
			=> new CompositionColorGradientStop(this)
			{
				Offset = offset,
				Color = color
			};

		public CompositionViewBox CreateViewBox()
			=> new CompositionViewBox(this);

		public RedirectVisual CreateRedirectVisual()
			=> new RedirectVisual(this);

		public RedirectVisual CreateRedirectVisual(Visual source)
			=> new RedirectVisual(this) { Source = source };

		public CompositionVisualSurface CreateVisualSurface()
			=> new CompositionVisualSurface(this);

		public CompositionMaskBrush CreateMaskBrush()
			=> new CompositionMaskBrush(this);

		public CompositionNineGridBrush CreateNineGridBrush()
			=> new CompositionNineGridBrush(this);

		internal void InvalidateRender() => InvalidateRenderPartial();

		partial void InvalidateRenderPartial();
	}
}
