namespace Uno.UI.Helpers;

/// <summary>
/// Extension methods for <see cref="UnoRuntimePlatform"/>.
/// </summary>
public static class UnoRuntimePlatformExtensions
{
	/// <summary>
	/// Gets a value indicating whether the platform is rendered by Uno Platform using Skia.
	/// </summary>
	public static bool IsSkia(this UnoRuntimePlatform platform) =>
		platform is UnoRuntimePlatform.SkiaWin32
		or UnoRuntimePlatform.SkiaX11
		or UnoRuntimePlatform.SkiaMacOS
		or UnoRuntimePlatform.SkiaIslands
		or UnoRuntimePlatform.SkiaWasm
		or UnoRuntimePlatform.SkiaAndroid
		or UnoRuntimePlatform.SkiaIOS
		or UnoRuntimePlatform.SkiaTvOS
		or UnoRuntimePlatform.SkiaFrameBuffer
		or UnoRuntimePlatform.SkiaHeadless;

	/// <summary>
	/// Gets a value indicating whether the platform is native WinUI on Windows App SDK.
	/// </summary>
	public static bool IsWindowsAppSdk(this UnoRuntimePlatform platform) => platform is UnoRuntimePlatform.NativeWinUI;

	/// <summary>
	/// Gets a value indicating whether the platform is a desktop one (Windows, Linux or macOS).
	/// </summary>
	public static bool IsDesktop(this UnoRuntimePlatform platform) =>
		platform is UnoRuntimePlatform.NativeWinUI
		or UnoRuntimePlatform.SkiaWin32
		or UnoRuntimePlatform.SkiaX11
		or UnoRuntimePlatform.SkiaMacOS
		or UnoRuntimePlatform.SkiaIslands
		or UnoRuntimePlatform.SkiaFrameBuffer;

	/// <summary>
	/// Gets a value indicating whether the platform is a mobile one (Android, iOS or tvOS).
	/// </summary>
	public static bool IsMobile(this UnoRuntimePlatform platform) =>
		platform is UnoRuntimePlatform.SkiaAndroid
		or UnoRuntimePlatform.SkiaIOS
		or UnoRuntimePlatform.SkiaTvOS;

	/// <summary>
	/// Gets a value indicating whether the platform is WebAssembly.
	/// </summary>
	public static bool IsWasm(this UnoRuntimePlatform platform) => platform is UnoRuntimePlatform.SkiaWasm;

	/// <summary>
	/// Gets a value indicating whether the platform is Android.
	/// </summary>
	public static bool IsAndroid(this UnoRuntimePlatform platform) => platform is UnoRuntimePlatform.SkiaAndroid;

	/// <summary>
	/// Gets a value indicating whether the platform is iOS.
	/// </summary>
	public static bool IsIOS(this UnoRuntimePlatform platform) => platform is UnoRuntimePlatform.SkiaIOS;

	/// <summary>
	/// Gets a value indicating whether the platform is tvOS.
	/// </summary>
	public static bool IsTvOS(this UnoRuntimePlatform platform) => platform is UnoRuntimePlatform.SkiaTvOS;
}
