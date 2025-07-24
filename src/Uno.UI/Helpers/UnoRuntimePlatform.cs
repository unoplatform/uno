using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Helpers;

public enum UnoRuntimePlatform
{
	Unknown = 0,

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
}
