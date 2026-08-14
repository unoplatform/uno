#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A neutral, closed effect-graph node — the intermediate representation Uno produces (internally, once) from a
/// WinUI <see cref="Windows.Graphics.Effects.IGraphicsEffect"/> graph. A render backend <b>fuses</b> a tree of these
/// into its own opaque <see cref="IEffectFilter"/> (<see cref="IDrawingFactory.CreateEffectFilter(EffectNode, Windows.Foundation.Rect)"/>);
/// it never reflects over Direct2D. Every non-backdrop brush/image input is pre-rasterized to a
/// <see cref="TextureInput"/> before the tree reaches the backend, so <see cref="BackdropInput"/> is the only
/// deferred (live-scene) leaf. See <c>specs/effects-neutralization/design.md</c>.
/// </summary>
public abstract record EffectNode
{
	/// <summary>The node's inputs, in graph order (empty for a leaf). Enables neutral tree walks.</summary>
	public abstract IReadOnlyList<EffectNode> Children { get; }

	/// <summary>True when this subtree references the live backdrop — drives <c>RequiresRepaintOnEveryFrame</c>.</summary>
	public bool ContainsBackdrop() => this is BackdropInput || Children.Any(c => c.ContainsBackdrop());

	/// <summary>Every <see cref="TextureInput"/> in this subtree (the textures Uno owns and must dispose).</summary>
	public IEnumerable<TextureInput> EnumerateTextures()
	{
		if (this is TextureInput t)
		{
			yield return t;
		}

		foreach (var child in Children)
		{
			foreach (var nested in child.EnumerateTextures())
			{
				yield return nested;
			}
		}
	}
}

/// <summary>The live already-composited scene behind the element (the acrylic/backdrop input). The one deferred leaf:
/// its pixels don't exist until present, so a backend realizes it as the filter's implicit backdrop input.</summary>
public sealed record BackdropInput : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => System.Array.Empty<EffectNode>();
}

/// <summary>A solid colour source (D2D <c>ColorSourceEffect</c>) filling the effect bounds.</summary>
public sealed record ColorInput(Color Color) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => System.Array.Empty<EffectNode>();
}

/// <summary>A sampled source: a brush/image/noise input Uno already rasterized to a backend texture via
/// <see cref="IDrawingFactory.RenderOffscreen"/>, plus how it is sampled outside its own rectangle
/// (<see cref="ExtendX"/>/<see cref="ExtendY"/> — D2D BorderEffect's edge behaviour; <see cref="EdgeExtend.None"/>
/// is a plain finite image). The brush/image leaf of the tree; Uno owns and disposes <see cref="Texture"/>.</summary>
public sealed record TextureInput(IImageTexture Texture, EdgeExtend ExtendX = EdgeExtend.None, EdgeExtend ExtendY = EdgeExtend.None) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => System.Array.Empty<EffectNode>();
}

/// <summary>
/// A per-pixel colour transform over <see cref="Source"/>, as a 4×5 (row-major, Skia-order) colour matrix. Absorbs
/// D2D Opacity and every colour effect a backend realizes as a plain colour matrix (Grayscale, Invert, HueRotation,
/// Saturation, Sepia, Exposure, TemperatureAndTint, ColorMatrix). Consecutive matrices fuse by matrix-multiply.
/// </summary>
public sealed record ColorMatrixEffectNode(EffectNode Source, float[] Matrix) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>
/// A gaussian blur of <see cref="Source"/>. <see cref="ClampEdge"/> clamps to the source edge
/// (D2D <c>BorderMode.Hard</c> — no bleed past the element, what a backdrop/acrylic blur wants) versus fading to
/// transparent (the default).
/// </summary>
public sealed record BlurEffectNode(EffectNode Source, float Sigma, bool ClampEdge) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>Two inputs combined with a blend <see cref="Mode"/> (D2D <c>BlendEffect</c>). The acrylic
/// luminosity/colour non-separable blends are this node.</summary>
public sealed record BlendEffectNode(EffectNode Background, EffectNode Foreground, BlendMode Mode) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Background, Foreground };
}

/// <summary>Per-channel multiply of <see cref="Source"/> by <see cref="Color"/> (D2D <c>TintEffect</c>, realized as a
/// Modulate blend rather than a colour matrix so it runs identically on CPU and GPU).</summary>
public sealed record ModulateEffectNode(EffectNode Source, Color Color) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>Replaces each pixel's alpha with its luminance (D2D <c>LuminanceToAlphaEffect</c>).</summary>
public sealed record LuminanceToAlphaEffectNode(EffectNode Source) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>D2D <c>ContrastEffect</c> — a non-matrix per-pixel curve; the backend realizes it (e.g. via a shader).</summary>
public sealed record ContrastEffectNode(EffectNode Source, float Contrast, bool Clamp) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>D2D <c>LinearTransferEffect</c> — per-channel <c>offset + value·slope</c>. Arrays are R,G,B,A order.</summary>
public sealed record LinearTransferEffectNode(EffectNode Source, float[] Offsets, float[] Slopes, bool[] Disable, bool Clamp) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>D2D <c>GammaTransferEffect</c> — per-channel <c>amplitude·value^exponent + offset</c>. Arrays are R,G,B,A order.</summary>
public sealed record GammaTransferEffectNode(EffectNode Source, float[] Amplitudes, float[] Exponents, float[] Offsets, bool[] Disable, bool Clamp) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>D2D <c>Transform2DEffect</c> — a 2D affine transform of the source.</summary>
public sealed record Transform2DEffectNode(EffectNode Source, Matrix3x2 Matrix) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>D2D <c>CrossFadeEffect</c> — linear blend of two sources by <see cref="Weight"/> (0 = A, 1 = B).</summary>
public sealed record CrossFadeEffectNode(EffectNode SourceA, EffectNode SourceB, float Weight) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { SourceA, SourceB };
}

/// <summary>D2D <c>AlphaMaskEffect</c> — <see cref="Source"/> shown through <see cref="Mask"/>'s alpha.</summary>
public sealed record AlphaMaskEffectNode(EffectNode Source, EffectNode Mask) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source, Mask };
}

/// <summary>D2D <c>ArithmeticCompositeEffect</c> — <c>M·bg·fg + S1·bg + S2·fg + Offset</c>.</summary>
public sealed record ArithmeticCompositeEffectNode(EffectNode Background, EffectNode Foreground, float Multiply, float Source1, float Source2, float Offset) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Background, Foreground };
}

/// <summary>D2D <c>TurbulenceEffect</c>/white-noise — a procedural noise source (no inputs).</summary>
public sealed record WhiteNoiseEffectNode(Vector2 Frequency, Vector2 Offset) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => System.Array.Empty<EffectNode>();
}

/// <summary>Which D2D lighting effect a <see cref="LightingEffectNode"/> realizes.</summary>
public enum LightingKind
{
	DistantDiffuse,
	DistantSpecular,
	SpotDiffuse,
	SpotSpecular,
	PointDiffuse,
	PointSpecular,
}

/// <summary>
/// D2D distant/spot/point diffuse/specular lighting over <see cref="Source"/>'s alpha as a height field. The parser
/// pre-computes the light geometry (via the neutral <c>EffectHelpers</c>); each <see cref="LightingKind"/> reads only
/// the fields it needs — <see cref="Light"/> (distant: light vector; point/spot: position), <see cref="Target"/>
/// (spot only), <see cref="Amount"/> (diffuse Kd / specular Ks), <see cref="SpecularExponent"/> (specular),
/// <see cref="Focus"/> and <see cref="ConeAngle"/> (spot).
/// </summary>
public sealed record LightingEffectNode(
	EffectNode Source,
	LightingKind Kind,
	Vector3 Light,
	Vector3 Target,
	Color LightColor,
	float Amount,
	float SpecularExponent,
	float Focus,
	float ConeAngle) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => new[] { Source };
}

/// <summary>N inputs combined pairwise with a composite <see cref="Mode"/> (D2D <c>CompositeEffect</c>).</summary>
public sealed record CompositeEffectNode(IReadOnlyList<EffectNode> Sources, BlendMode Mode) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => Sources;
}

/// <summary>
/// An effect Uno's parser doesn't (yet) translate to a neutral node. A backend renders <see cref="Source"/> (the
/// first input, if any) unmodified so content still appears. Carries <see cref="EffectName"/> for diagnostics.
/// </summary>
public sealed record UnsupportedEffectNode(string EffectName, EffectNode? Source) : EffectNode
{
	public override IReadOnlyList<EffectNode> Children => Source is null ? System.Array.Empty<EffectNode>() : new[] { Source };
}
