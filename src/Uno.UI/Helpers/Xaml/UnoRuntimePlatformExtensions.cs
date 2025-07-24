namespace Uno.UI.Helpers;

public static class UnoRuntimePlatformExtensions
{
	public static bool IsSkia(this UnoRuntimePlatform platform) =>
#if __SKIA__
		true;
#else
	false;
#endif


	public static bool IsIOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeIOS || platform == UnoRuntimePlatform.SkiaIOS;

	public static bool IsWindows(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeWinUI;

	public static bool IsAndroid(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeAndroid || platform == UnoRuntimePlatform.SkiaAndroid;

	public static bool IsMacCatalyst(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeMacCatalyst || platform == UnoRuntimePlatform.SkiaMacCatalyst;

	public static bool IsWasm(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeWasm || platform == UnoRuntimePlatform.SkiaWasm;

	public static bool IsTvOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeTvOS || platform == UnoRuntimePlatform.SkiaTvOS;

}
