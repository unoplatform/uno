// This enum is available in three forms, selected via compilation symbols:
// - internal "RuntimePlatform" (Uno.Helpers) in the Uno.WinRT projects, for internal platform detection
// - public "RuntimeTestPlatforms" (Microsoft.VisualStudio.TestTools.UnitTesting) in Uno.UI.RuntimeTests, for test conditioning
// - public "UnoRuntimePlatform" (Uno.UI.Toolkit) in other projects, as the public API
// The runtime tests check for multiple platforms at once, so in that context the enum is marked [Flags]
// and combined platform values are defined. In the other contexts only individual platform values exist.

#if IS_UNO_WINRT_PROJECT
namespace Uno.Helpers;
#elif IS_RUNTIME_UI_TESTS
namespace Microsoft.VisualStudio.TestTools.UnitTesting;
#else
namespace Uno.UI.Toolkit;
#endif

#if IS_UNO_WINRT_PROJECT
internal enum RuntimePlatform
#elif IS_RUNTIME_UI_TESTS
[global::System.Flags]
public enum RuntimeTestPlatforms
#else
public enum UnoRuntimePlatform
#endif
{
#if !IS_RUNTIME_UI_TESTS
	Unknown = 0,
#else
	None = 0,
#endif

	// Native platforms
	NativeWinUI = 1 << 0,
	NativeWasm = 1 << 1,
	NativeAndroid = 1 << 2,
	NativeIOS = 1 << 3,
	NativeMacCatalyst = 1 << 4,
	NativeTvOS = 1 << 5,

	// Skia platforms
	SkiaWin32 = 1 << 7,
	SkiaX11 = 1 << 8,
	SkiaMacOS = 1 << 9,
	SkiaIslands = 1 << 10,
	SkiaWasm = 1 << 11,
	SkiaAndroid = 1 << 12,
	SkiaIOS = 1 << 13,
	SkiaMacCatalyst = 1 << 14,
	SkiaTvOS = 1 << 15,
	SkiaFrameBuffer = 1 << 16,

#if IS_RUNTIME_UI_TESTS
	// Combined platforms
	NativeUIKit = NativeIOS | NativeTvOS | NativeMacCatalyst,
	SkiaUIKit = SkiaIOS | SkiaTvOS | SkiaMacCatalyst,
	NativeMobile = NativeAndroid | NativeUIKit,
	SkiaMobile = SkiaAndroid | SkiaUIKit,
	SkiaDesktop = SkiaWin32 | SkiaX11 | SkiaMacOS | SkiaIslands | SkiaFrameBuffer,
	Skia = SkiaDesktop | SkiaWasm | SkiaMobile,
	Native = NativeWasm | NativeAndroid | NativeIOS | NativeMacCatalyst | NativeTvOS | NativeWinUI,

	Wasm = NativeWasm | SkiaWasm,
	Android = NativeAndroid | SkiaAndroid,
	IOS = NativeIOS | SkiaIOS,
#endif
}
