#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-free CPU-side drawing registration: installs the managed drawing factory, image decoder and font
/// provider so an app can run with no reference to the Skia backend assembly (and thus no native libSkiaSharp).
/// The app entry calls this instead of <c>Uno.UI.Composition.Skia.SkiaBackend.Register()</c> when built without the
/// Skia backend. The GPU render backend (e.g. WebGPU) is installed by the platform head, which sets
/// <c>CompositionTarget.Renderer</c>; there is no managed CPU renderer, so no <see cref="DrawingRegistration.DefaultRenderer"/>
/// is set here.
/// </summary>
public static class ManagedBackend
{
	public static void Register()
	{
		// Force the managed (SkiaSharp-free) drawing backend, overriding any Skia default a module initializer may
		// have installed when the Skia assembly is present in the closure. Geometry lives on the backend.
		DrawingFactory.Register(new ManagedDrawingFactory());

		// Install the managed content seams as register-if-absent DEFAULTS so an app that registered its own
		// implementor (any IImageDecoder / IFontProvider) before this call wins.
		ImageDecoder.RegisterDefault(new ManagedImageDecoderBackend());
		FontProvider.RegisterDefault(new ManagedFontProvider());
	}
}
