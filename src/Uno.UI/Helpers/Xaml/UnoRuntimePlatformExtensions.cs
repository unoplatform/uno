namespace Uno.UI.Helpers;

public static class UnoRuntimePlatformExtensions
{
	public static bool IsSkia(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaFrameBuffer
						   || platform == UnoRuntimePlatform.SkiaGtk
						   || platform == UnoRuntimePlatform.SkiaMacOS
						   || platform == UnoRuntimePlatform.SkiaWpfIslands
						   || platform == UnoRuntimePlatform.SkiaX11;

	public static bool IsIOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.iOS;

	public static bool IsMacCatalyst(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.MacCatalyst;

	public static bool IsWebAssembly(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.WebAssembly;

	public static bool IsWindows(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.Windows;

	public static bool IsAndroid(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.Android;
}
