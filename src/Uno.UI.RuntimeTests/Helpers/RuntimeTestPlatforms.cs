namespace Microsoft.VisualStudio.TestTools.UnitTesting;

public enum RuntimeTestPlatforms
{
	None = 0,

	// Native platforms
	NativeWinUI = 1 << 0,
	NativeWasm = 1 << 1,
	NativeAndroid = 1 << 2,
	NativeIOS = 1 << 3,
	NativeMacCatalyst = 1 << 4,
	NativeTvOS = 1 << 5,

	// Skia platforms
	SkiaGtk = 1 << 6,
	SkiaWpf = 1 << 7,
	SkiaWin32 = 1 << 8,
	SkiaX11 = 1 << 9,
	SkiaMacOS = 1 << 10,
	SkiaIslands = 1 << 11,
	SkiaWasm = 1 << 12,
	SkiaAndroid = 1 << 13,
	SkiaIOS = 1 << 14,
	SkiaMacCatalyst = 1 << 15,
	SkiaTvOS = 1 << 16,

	// Combined platforms
	NativeUIKit = NativeIOS | NativeTvOS | NativeMacCatalyst,
	SkiaUIKit = SkiaIOS | SkiaTvOS | SkiaMacCatalyst,
	SkiaMobile = SkiaAndroid | SkiaUIKit,
	SkiaDesktop = SkiaGtk | SkiaWpf | SkiaX11 | SkiaMacOS | SkiaIslands,
	Skia = SkiaDesktop | SkiaWasm | SkiaMobile,
	Native = NativeWasm | NativeAndroid | NativeIOS | NativeMacCatalyst | NativeTvOS | NativeWinUI,
}
