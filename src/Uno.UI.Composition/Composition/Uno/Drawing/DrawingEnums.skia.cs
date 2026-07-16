#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>Whether a paint fills or strokes.</summary>
internal enum PaintStyle
{
	Fill,
	Stroke,
}

/// <summary>Stroke end-cap style. <see cref="Triangle"/> has no direct backend primitive and is simulated with custom cap geometry.</summary>
internal enum StrokeCap
{
	Butt,
	Round,
	Square,
	Triangle,
}

/// <summary>Stroke line-join style.</summary>
internal enum StrokeJoin
{
	Miter,
	Round,
	Bevel,
	MiterOrBevel,
}

/// <summary>Porter-Duff / separable blend modes used by the drawing pipeline. <see cref="SrcOver"/> is the default.</summary>
internal enum BlendMode
{
	SrcOver,
	Src,
	Plus,
	Modulate,
	Multiply,
	DstIn,
	DstOut,
	SrcIn,
}

/// <summary>Image sampling quality for <see cref="IDrawingSession.DrawImage"/>.</summary>
internal enum ImageSampling
{
	NearestNeighbor,
	Linear,
}

/// <summary>How a clip combines with the current clip region.</summary>
internal enum ClipOperation
{
	Intersect,
	Difference,
}

/// <summary>How a gradient extends past its defined stops.</summary>
internal enum GradientTileMode
{
	Clamp,
	Repeat,
	Mirror,
}

/// <summary>Winding rule used to fill a geometry.</summary>
internal enum GeometryFillRule
{
	NonZero,
	EvenOdd,
}
