using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[Flags]
public enum RuntimeTestPlatforms
{
	None = 0,

	// Native WinUI (WinAppSDK), i.e. rendered by WinUI rather than by Uno
	NativeWinUI = 1 << 0,

	// Skia platforms
	SkiaWin32 = 1 << 7,
	SkiaX11 = 1 << 8,
	SkiaMacOS = 1 << 9,
	SkiaIslands = 1 << 10,
	SkiaWasm = 1 << 11,
	SkiaAndroid = 1 << 12,
	SkiaIOS = 1 << 13,
	SkiaTvOS = 1 << 15,
	SkiaFrameBuffer = 1 << 16,

	// Combined platforms
	SkiaUIKit = SkiaIOS | SkiaTvOS,
	SkiaMobile = SkiaAndroid | SkiaUIKit,
	SkiaDesktop = SkiaWin32 | SkiaX11 | SkiaMacOS | SkiaIslands | SkiaFrameBuffer,
	Skia = SkiaDesktop | SkiaWasm | SkiaMobile,
}
