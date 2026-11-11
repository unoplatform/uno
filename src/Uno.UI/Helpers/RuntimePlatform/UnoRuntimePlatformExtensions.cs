namespace Uno.UI.Helpers;

/// <summary>
/// Extension methods for <see cref="UnoRuntimePlatform"/>.
/// </summary>
public static class UnoRuntimePlatformExtensions
{
	/// <summary>
	/// Gets a value indicating whether the platform is a desktop one (Windows, Linux or macOS).
	/// </summary>
	public static bool IsDesktop(this UnoRuntimePlatform platform) =>
		platform is UnoRuntimePlatform.WindowsAppSdk
		or UnoRuntimePlatform.Win32
		or UnoRuntimePlatform.X11
		or UnoRuntimePlatform.MacOS
		or UnoRuntimePlatform.Islands
		or UnoRuntimePlatform.FrameBuffer;

	/// <summary>
	/// Gets a value indicating whether the platform is a mobile one (Android, iOS or tvOS).
	/// </summary>
	public static bool IsMobile(this UnoRuntimePlatform platform) =>
		platform is UnoRuntimePlatform.Android
		or UnoRuntimePlatform.IOS
		or UnoRuntimePlatform.TvOS;
}
