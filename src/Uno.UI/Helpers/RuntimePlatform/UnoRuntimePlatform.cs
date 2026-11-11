namespace Uno.UI.Helpers;

/// <summary>
/// The platform the app is currently running on.
/// </summary>
public enum UnoRuntimePlatform
{
	/// <summary>
	/// The platform is not known yet (no Uno Platform host has started) or is not recognized.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Native WinUI on Windows App SDK.
	/// </summary>
	NativeWinUI = 1,

	/// <summary>
	/// Skia rendering on Windows (Win32).
	/// </summary>
	SkiaWin32 = 2,

	/// <summary>
	/// Skia rendering on Linux (X11).
	/// </summary>
	SkiaX11 = 3,

	/// <summary>
	/// Skia rendering on macOS.
	/// </summary>
	SkiaMacOS = 4,

	/// <summary>
	/// Skia rendering hosted as an island inside a non-Uno Platform application.
	/// </summary>
	SkiaIslands = 5,

	/// <summary>
	/// Skia rendering in the browser (WebAssembly).
	/// </summary>
	SkiaWasm = 6,

	/// <summary>
	/// Skia rendering on Android.
	/// </summary>
	SkiaAndroid = 7,

	/// <summary>
	/// Skia rendering on iOS.
	/// </summary>
	SkiaIOS = 8,

	/// <summary>
	/// Skia rendering on tvOS.
	/// </summary>
	SkiaTvOS = 9,

	/// <summary>
	/// Skia rendering on the Linux framebuffer.
	/// </summary>
	SkiaFrameBuffer = 10,

	/// <summary>
	/// Skia rendering without a display (headless host).
	/// </summary>
	SkiaHeadless = 11,
}
