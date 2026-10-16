#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opt-in capabilities of the active drawing backend, for callers that build geometry BEFORE a drawing session
/// exists (a shape records into a command list, whose retained session cannot know the eventual target).
/// </summary>
public static class DrawingCapabilities
{
	/// <summary>
	/// The backend strokes a path better than the caller can by pre-converting it to a fill geometry, so callers
	/// should hand it the path + thickness + join via <see cref="IDrawingSession.StrokePath"/>. Setting this is a
	/// promise to honour every <see cref="StrokeJoin"/>: widening the path instead costs the caller a stroke-to-fill
	/// per frame and hands the backend a self-overlapping outline, which is the expensive shape to rasterise.
	/// </summary>
	public static bool NativeStroking { get; set; }
}
