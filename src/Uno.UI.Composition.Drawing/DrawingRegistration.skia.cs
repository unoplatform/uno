#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Neutral hand-off point for the drawing backend's default renderer. The framework (Uno.UI) is backend-agnostic
/// and packaged once: it must not reference a concrete backend type (e.g. <c>SkiaRenderer</c>). A backend sets this
/// during its <c>Register()</c> so <c>CompositionTarget.Renderer</c> can fall back to it for heads that don't install
/// their own renderer explicitly (WebGPU heads set <c>CompositionTarget.Renderer</c> directly and win over this).
/// </summary>
public static class DrawingRegistration
{
	/// <summary>The backend-provided default <see cref="IRenderer"/>, or <c>null</c> if no backend registered one.</summary>
	public static IRenderer? DefaultRenderer { get; set; }
}
