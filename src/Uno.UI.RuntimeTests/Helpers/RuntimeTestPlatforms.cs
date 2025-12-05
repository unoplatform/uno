using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[Flags]
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
	SkiaWpf = 1 << 6,
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

	// Combined platforms
	NativeUIKit = NativeIOS | NativeTvOS | NativeMacCatalyst,
	SkiaUIKit = SkiaIOS | SkiaTvOS | SkiaMacCatalyst,
	SkiaMobile = SkiaAndroid | SkiaUIKit,
	SkiaDesktop = SkiaWpf | SkiaWin32 | SkiaX11 | SkiaMacOS | SkiaIslands | SkiaFrameBuffer,
	Skia = SkiaDesktop | SkiaWasm | SkiaMobile,
	Native = NativeWasm | NativeAndroid | NativeIOS | NativeMacCatalyst | NativeTvOS | NativeWinUI,

	Wasm = NativeWasm | SkiaWasm,
	Android = NativeAndroid | SkiaAndroid,
	IOS = NativeIOS | SkiaIOS,
}
