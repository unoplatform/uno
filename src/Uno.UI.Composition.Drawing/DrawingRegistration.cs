#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Neutral hand-off point for the drawing backend's default renderer, so the backend-agnostic framework (Uno.UI)
/// need not reference a concrete backend type. A backend sets this during its <c>Register()</c> so
/// <c>CompositionTarget.Renderer</c> can fall back to it for heads that don't install their own renderer explicitly.
/// </summary>
internal static class DrawingRegistration
{
	private static IDrawingFactory? _defaultRenderer;

	/// <summary>The backend-provided default backend, or <c>null</c> if no backend registered one. The host builder
	/// resolves the default (Skia) renderer at Build() time; a declared backend (e.g. WebGPU) owns this seam.</summary>
	public static IDrawingFactory? DefaultRenderer
	{
		get => _defaultRenderer;
		internal set => _defaultRenderer = value;
	}

	/// <summary>Registers <paramref name="renderer"/> only if none is set — so the Skia fallback never clobbers a
	/// renderer a head installed explicitly. Framework-internal; does not trigger the getter's implicit fallback.</summary>
	internal static void RegisterDefaultRenderer(IDrawingFactory renderer)
		=> _defaultRenderer ??= renderer;
}
