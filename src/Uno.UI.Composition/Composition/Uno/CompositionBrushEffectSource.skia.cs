#nullable enable

using System.Numerics;
using Windows.Foundation;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

/// <summary>
/// Adapts a <see cref="CompositionBrush"/> to the backend-neutral <see cref="IEffectSource"/> so the drawing SPI
/// (<see cref="IDrawingFactory.CreateEffectFilter"/>) never references a compositor brush type.
/// </summary>
internal sealed class CompositionBrushEffectSource : IEffectSource
{
	private readonly CompositionBrush _brush;

	private CompositionBrushEffectSource(CompositionBrush brush) => _brush = brush;

	/// <summary>Wraps <paramref name="brush"/>, or returns null when it is null.</summary>
	public static IEffectSource? From(CompositionBrush? brush) => brush is null ? null : new CompositionBrushEffectSource(brush);

	public bool IsBackdrop => _brush is CompositionBackdropBrush;

	public Vector2? Size => (_brush as ISizedBrush)?.Size;

	public bool Paint(IDrawingSession session, float opacity, Rect bounds) => _brush.TryPaint(session, opacity, bounds);
}
