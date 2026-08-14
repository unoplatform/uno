#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>Stroke end-cap style.</summary>
public enum StrokeCap
{
	Butt,
	Round,
	Square,
	Triangle,
}

/// <summary>Stroke line-join style.</summary>
public enum StrokeJoin
{
	Miter,
	Round,
	Bevel,
	MiterOrBevel,
}

/// <summary>Porter-Duff / separable blend modes used by the drawing pipeline. <see cref="SrcOver"/> is the default.</summary>
public enum BlendMode
{
	SrcOver,
	Src,
	Plus,
	Modulate,
	Multiply,
	DstIn,
	DstOut,
	SrcIn,
	// Porter-Duff modes used by CompositeEffect.
	DstOver,
	SrcOut,
	SrcATop,
	DstATop,
	Xor,
	// Separable/non-separable blend modes used by effect graphs (BlendEffect / acrylic luminosity+colour).
	Screen,
	Darken,
	Lighten,
	ColorBurn,
	ColorDodge,
	Overlay,
	SoftLight,
	HardLight,
	Difference,
	Exclusion,
	Hue,
	Saturation,
	Color,
	Luminosity,
}

/// <summary>Image sampling quality for <see cref="IDrawingSession.DrawImage"/>.</summary>
public enum ImageSampling
{
	NearestNeighbor,
	Linear,
}

/// <summary>How a clip combines with the current clip region.</summary>
public enum ClipOperation
{
	Intersect,
	Difference,
}

/// <summary>How a gradient extends past its defined stops.</summary>
public enum GradientTileMode
{
	Clamp,
	Repeat,
	Mirror,
}

/// <summary>How a <see cref="EffectNode"/> texture input is sampled outside its own rectangle (D2D BorderEffect's
/// edge behaviour). <see cref="None"/> — the default — is a plain finite image (transparent outside).</summary>
public enum EdgeExtend
{
	None,
	Clamp,
	Wrap,
	Mirror,
}

/// <summary>Winding rule used to fill a geometry.</summary>
public enum GeometryFillRule
{
	NonZero,
	EvenOdd,
}
