#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		internal CompositionSpriteShape(CompositionGeometry geometry = null)
		{
			Geometry = geometry;
		}

		public float StrokeThickness { get; set; }

		public CompositionStrokeCap StrokeStartCap { get; set; }

		public float StrokeMiterLimit { get; set; }

		public CompositionStrokeLineJoin StrokeLineJoin { get; set; }

		public CompositionStrokeCap StrokeEndCap { get; set; }

		public float StrokeDashOffset { get; set; }

		public CompositionStrokeCap StrokeDashCap { get; set; }

		public CompositionBrush StrokeBrush { get; set; }

		public bool IsStrokeNonScaling { get; set; }

		public CompositionGeometry Geometry { get; set; }

		public CompositionBrush FillBrush { get; set; }

		public CompositionStrokeDashArray StrokeDashArray { get; set; }
	}
}
