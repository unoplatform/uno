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
	private static IRenderer? _defaultRenderer;

	/// <summary>The backend-provided default <see cref="IRenderer"/>, or <c>null</c> if no backend registered one.</summary>
	public static IRenderer? DefaultRenderer
	{
		get
		{
			if (_defaultRenderer is null)
			{
				// Nothing registered explicitly: light up ONLY the Skia graphics backend (renderer + its matched
				// factory) by reflection if present — but not when a backend was declared (a WebGPU head owns this
				// seam via GraphicsRegistry). Font/image content seams fall back independently, elsewhere.
				DrawingBackendFallback.EnsureGraphicsBackend();
			}

			return _defaultRenderer;
		}
		set => _defaultRenderer = value;
	}

	/// <summary>Registers <paramref name="renderer"/> only if none is set — so the Skia fallback never clobbers a
	/// renderer a head installed explicitly. Does not trigger the getter's implicit fallback.</summary>
	public static void RegisterDefaultRenderer(IRenderer renderer)
		=> _defaultRenderer ??= renderer;
}
