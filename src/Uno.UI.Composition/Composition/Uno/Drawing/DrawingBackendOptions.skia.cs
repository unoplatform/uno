#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Init-time options that select the SkiaSharp-free managed engines over the default Skia ones. A host/builder
/// sets these during initialization (before the first frame); they replace the former <c>UNO_MANAGED_*</c>
/// environment-variable toggles, so backend selection is part of app configuration rather than ambient state.
/// </summary>
public static class DrawingBackendOptions
{
	/// <summary>Resolve and render fonts through the managed font manager (system-font lookup + <c>ManagedFont</c>) instead of Skia.</summary>
	public static bool UseManagedFonts { get; set; }

	/// <summary>Build geometry through the managed path engine (<c>ManagedPathBuilder</c>) instead of <c>SKPath</c>.</summary>
	public static bool UseManagedGeometry { get; set; }

	/// <summary>Decode images through the managed decoder before falling back to the Skia codec.</summary>
	public static bool UseManagedImageDecoder { get; set; }
}
