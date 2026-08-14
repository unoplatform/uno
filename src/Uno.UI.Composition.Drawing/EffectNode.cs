#nullable enable

using System.Collections.Generic;
using System.Linq;
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

/// <summary>A brush/image/noise input Uno already rasterized to a backend texture via
/// <see cref="IDrawingFactory.RenderOffscreen"/>. Replaces the old <c>IEffectSource</c> brush leaf; Uno owns
/// and disposes <see cref="Texture"/>.</summary>
public sealed record TextureInput(IImageTexture Texture) : EffectNode
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
