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
	/// should hand it the path + thickness via <see cref="IDrawingSession.StrokePath"/>.
	/// </summary>
	public static bool NativeStroking { get; set; }
}
