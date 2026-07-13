#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>Whether a paint fills or strokes.</summary>
internal enum PaintStyle
{
	Fill,
	Stroke,
}

/// <summary>Stroke end-cap style.</summary>
internal enum StrokeCap
{
	Butt,
	Round,
	Square,
}

/// <summary>Stroke line-join style.</summary>
internal enum StrokeJoin
{
	Miter,
	Round,
	Bevel,
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
