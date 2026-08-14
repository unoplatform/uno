#nullable enable

using System.Collections.Generic;
using Windows.UI;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.Effects;

/// <summary>
/// A neutral, closed effect-graph node — the intermediate representation the Uno-internal parser
/// (<see cref="EffectGraphParser"/>) produces from a WinUI <see cref="Windows.Graphics.Effects.IGraphicsEffect"/>
/// graph. The render path evaluates these typed nodes with drawing primitives instead of reflecting over Direct2D,
/// so a backend never interprets an effect. See <c>specs/effects-neutralization/design.md</c>.
/// </summary>
internal abstract record EffectNode;

/// <summary>The live already-composited scene behind the element (the acrylic/backdrop input).</summary>
internal sealed record BackdropInput : EffectNode;

/// <summary>A solid colour source (D2D <c>ColorSourceEffect</c>).</summary>
internal sealed record ColorInput(Color Color) : EffectNode;

/// <summary>
/// A rasterizable brush/image input (image, noise, nested visual…), materialized via
/// <see cref="IEffectSource.Paint"/>. The one leaf that still needs <see cref="IEffectSource"/>.
/// </summary>
internal sealed record BrushInput(IEffectSource Source) : EffectNode;

/// <summary>
/// A per-pixel colour transform over <see cref="Source"/>, as a 5×4 (row-major, Skia-order) colour matrix. A run
/// of these fuses by matrix-multiply into a single colour filter, applied in one draw (no intermediate offscreen).
/// </summary>
internal sealed record ColorMatrixEffectNode(EffectNode Source, float[] Matrix) : EffectNode;

/// <summary>
/// A gaussian blur of <see cref="Source"/>. <see cref="ClampEdge"/> clamps to the source edge
/// (<c>BorderMode.Hard</c> — no bleed past the element) versus fading to transparent.
/// </summary>
internal sealed record BlurEffectNode(EffectNode Source, float Sigma, bool ClampEdge) : EffectNode;

/// <summary>Two inputs combined with a blend <see cref="Mode"/> (D2D <c>BlendEffect</c>).</summary>
internal sealed record BlendEffectNode(EffectNode Background, EffectNode Foreground, BlendMode Mode) : EffectNode;

/// <summary>N inputs combined pairwise with a composite <see cref="Mode"/> (D2D <c>CompositeEffect</c>).</summary>
internal sealed record CompositeEffectNode(IReadOnlyList<EffectNode> Sources, BlendMode Mode) : EffectNode;

/// <summary>
/// An effect the parser doesn't (yet) translate. The evaluator renders <see cref="Source"/> (the first input, if
/// any) unmodified so content still appears, and logs the unsupported effect once.
/// </summary>
internal sealed record UnsupportedEffectNode(string EffectName, EffectNode? Source) : EffectNode;
