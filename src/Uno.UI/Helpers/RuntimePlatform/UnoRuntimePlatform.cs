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
	/// WinUI on Windows App SDK, rendered by WinUI rather than by Uno Platform.
	/// </summary>
	WindowsAppSdk = 1,

	/// <summary>
	/// Windows (Win32).
	/// </summary>
	Win32 = 2,

	/// <summary>
	/// Linux (X11).
	/// </summary>
	X11 = 3,

	/// <summary>
	/// macOS.
	/// </summary>
	MacOS = 4,

	/// <summary>
	/// An island hosted inside a non-Uno Platform application.
	/// </summary>
	Islands = 5,

	/// <summary>
	/// The browser (WebAssembly).
	/// </summary>
	Wasm = 6,

	/// <summary>
	/// Android.
	/// </summary>
	Android = 7,

	/// <summary>
	/// iOS.
	/// </summary>
	IOS = 8,

	/// <summary>
	/// tvOS.
	/// </summary>
	TvOS = 9,

	/// <summary>
	/// The Linux framebuffer.
	/// </summary>
	FrameBuffer = 10,

	/// <summary>
	/// Headless, without a display.
	/// </summary>
	Headless = 11,
}
