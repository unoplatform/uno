#nullable enable

namespace Windows.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private float _strokeThickness;
		private CompositionStrokeCap _strokeStartCap;
		private float _strokeMiterLimit;
		private CompositionStrokeLineJoin _strokeLineJoin;
		private CompositionStrokeCap _strokeEndCap;
		private float _strokeDashOffset;
		private CompositionStrokeCap _strokeDashCap;
		private CompositionBrush? _strokeBrush;
		private bool _isStrokeNonScaling;
		private CompositionGeometry? _geometry;
		private CompositionBrush? _fillBrush;
		private CompositionStrokeDashArray? _strokeDashArray;

		internal CompositionSpriteShape(Compositor compositor, CompositionGeometry? geometry = null) : base(compositor)
		{
			Geometry = geometry;
		}

		public float StrokeThickness
		{
			get => _strokeThickness; set
			{
				_strokeThickness = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionStrokeCap StrokeStartCap
		{
			get => _strokeStartCap; set
			{
				_strokeStartCap = value;
				Compositor.InvalidateRender();
			}
		}

		public float StrokeMiterLimit
		{
			get => _strokeMiterLimit; set
			{
				_strokeMiterLimit = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionStrokeLineJoin StrokeLineJoin
		{
			get => _strokeLineJoin; set
			{
				_strokeLineJoin = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionStrokeCap StrokeEndCap
		{
			get => _strokeEndCap; set
			{
				_strokeEndCap = value;
				Compositor.InvalidateRender();
			}
		}

		public float StrokeDashOffset
		{
			get => _strokeDashOffset; set
			{
				_strokeDashOffset = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionStrokeCap StrokeDashCap
		{
			get => _strokeDashCap; set
			{
				_strokeDashCap = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionBrush? StrokeBrush
		{
			get => _strokeBrush; set
			{
				_strokeBrush = value;
				Compositor.InvalidateRender();
			}
		}

		public bool IsStrokeNonScaling
		{
			get => _isStrokeNonScaling; set
			{
				_isStrokeNonScaling = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionGeometry? Geometry
		{
			get => _geometry; set
			{
				_geometry = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionBrush? FillBrush
		{
			get => _fillBrush; set
			{
				_fillBrush = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionStrokeDashArray? StrokeDashArray
		{
			get => _strokeDashArray; set
			{
				_strokeDashArray = value;
				Compositor.InvalidateRender();
			}
		}
	}
}
