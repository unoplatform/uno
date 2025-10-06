namespace Uno.UI.Helpers;

internal static class UnoRuntimePlatformExtensions
{
	internal static bool IsSkia(this UnoRuntimePlatform platform) =>
#if __SKIA__
		true;
#else
	false;
#endif

	internal static bool IsIOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeIOS || platform == UnoRuntimePlatform.SkiaIOS;

	internal static bool IsWindowsAppSdk(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeWinUI;

	internal static bool IsAndroid(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeAndroid || platform == UnoRuntimePlatform.SkiaAndroid;

	internal static bool IsMacCatalyst(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeMacCatalyst || platform == UnoRuntimePlatform.SkiaMacCatalyst;

	internal static bool IsWasm(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeWasm || platform == UnoRuntimePlatform.SkiaWasm;

	internal static bool IsTvOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeTvOS || platform == UnoRuntimePlatform.SkiaTvOS;
}
