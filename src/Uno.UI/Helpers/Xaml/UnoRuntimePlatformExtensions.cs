namespace Uno.UI.Helpers;

public static class UnoRuntimePlatformExtensions
{
	public static bool IsSkia(this UnoRuntimePlatform platform) =>
							  platform == UnoRuntimePlatform.SkiaFrameBuffer
						   || platform == UnoRuntimePlatform.SkiaGtk
						   || platform == UnoRuntimePlatform.SkiaMacOS
						   || platform == UnoRuntimePlatform.SkiaWpfIslands
						   || platform == UnoRuntimePlatform.SkiaX11
						   || platform == UnoRuntimePlatform.SkiaWin32
						   || platform == UnoRuntimePlatform.SkiaAndroid
						   || platform == UnoRuntimePlatform.SkiaWebAssembly
						   || platform == UnoRuntimePlatform.SkiaAppleUIKit;

	public static bool IsSkiaFrameBuffer(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaFrameBuffer;

	public static bool IsSkiaGtk(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaGtk;

	public static bool IsSkiaMacOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaMacOS;

	public static bool IsSkiaWpfIslands(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaWpfIslands;

	public static bool IsSkiaX11(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaX11;

	public static bool IsSkiaWin32(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaWin32;

	public static bool IsSkiaAndroid(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaAndroid;

	public static bool IsSkiaWebAssembly(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaWebAssembly;

	public static bool IsSkiaAppleUIKit(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.SkiaAppleUIKit;

	public static bool IsIOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.iOS;

	public static bool IsWindows(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.Windows;

	public static bool IsAndroid(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.Android;

	public static bool IsMacCatalyst(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.MacCatalyst;
}
