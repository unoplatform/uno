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
			get => _strokeThickness;
			set => SetProperty(ref _strokeThickness, value);
		}

		public CompositionStrokeCap StrokeStartCap
		{
			get => _strokeStartCap;
			set => SetEnumProperty(ref _strokeStartCap, value);
		}

		public float StrokeMiterLimit
		{
			get => _strokeMiterLimit;
			set => SetProperty(ref _strokeMiterLimit, value);
		}

		public CompositionStrokeLineJoin StrokeLineJoin
		{
			get => _strokeLineJoin;
			set => SetEnumProperty(ref _strokeLineJoin, value);
		}

		public CompositionStrokeCap StrokeEndCap
		{
			get => _strokeEndCap;
			set => SetEnumProperty(ref _strokeEndCap, value);
		}

		public float StrokeDashOffset
		{
			get => _strokeDashOffset;
			set => SetProperty(ref _strokeDashOffset, value);
		}

		public CompositionStrokeCap StrokeDashCap
		{
			get => _strokeDashCap;
			set => SetEnumProperty(ref _strokeDashCap, value);
		}

		public CompositionBrush? StrokeBrush
		{
			get => _strokeBrush;
			set => SetProperty(ref _strokeBrush, value);
		}

		public bool IsStrokeNonScaling
		{
			get => _isStrokeNonScaling;
			set => SetProperty(ref _isStrokeNonScaling, value);
		}

		public CompositionGeometry? Geometry
		{
			get => _geometry;
			set => SetProperty(ref _geometry, value);
		}

		public CompositionBrush? FillBrush
		{
			get => _fillBrush;
			set => SetProperty(ref _fillBrush, value);
		}

		public CompositionStrokeDashArray? StrokeDashArray
		{
			get => _strokeDashArray;
			set => SetProperty(ref _strokeDashArray, value);
		}
	}
}
