using Uno.Helpers;

namespace Uno.UI.Helpers;

internal static class UnoRuntimePlatformExtensions
{
	internal static bool IsSkia(this UnoRuntimePlatform platform) =>
		platform == UnoRuntimePlatform.SkiaWpf ||
		platform == UnoRuntimePlatform.SkiaWin32 ||
		platform == UnoRuntimePlatform.SkiaX11 ||
		platform == UnoRuntimePlatform.SkiaMacOS ||
		platform == UnoRuntimePlatform.SkiaIslands ||
		platform == UnoRuntimePlatform.SkiaWasm ||
		platform == UnoRuntimePlatform.SkiaAndroid ||
		platform == UnoRuntimePlatform.SkiaIOS ||
		platform == UnoRuntimePlatform.SkiaMacCatalyst ||
		platform == UnoRuntimePlatform.SkiaTvOS ||
		platform == UnoRuntimePlatform.SkiaFrameBuffer ||
		platform == UnoRuntimePlatform.SkiaAppleUIKit;

	internal static bool IsIOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeIOS || platform == UnoRuntimePlatform.SkiaIOS;

	internal static bool IsWindowsAppSdk(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeWinUI;

	internal static bool IsAndroid(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeAndroid || platform == UnoRuntimePlatform.SkiaAndroid;

	internal static bool IsMacCatalyst(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeMacCatalyst || platform == UnoRuntimePlatform.SkiaMacCatalyst;

	internal static bool IsWasm(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeWasm || platform == UnoRuntimePlatform.SkiaWasm;

	internal static bool IsTvOS(this UnoRuntimePlatform platform) => platform == UnoRuntimePlatform.NativeTvOS || platform == UnoRuntimePlatform.SkiaTvOS;
}
