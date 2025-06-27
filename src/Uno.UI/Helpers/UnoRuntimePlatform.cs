using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Helpers;

public enum UnoRuntimePlatform
{
	Unknown = 0,
	Android = 1,
	iOS = 2,
	MacCatalyst = 3,
	Windows = 4,
	WebAssembly = 5,
	SkiaWebAssembly = 6,
	SkiaGtk = 7,
	SkiaWpfIslands = 8,
	SkiaX11 = 9,
	SkiaFrameBuffer = 10,
	SkiaWin32 = 11,
	SkiaMacOS = 12,
	SkiaAndroid = 13,
	SkiaAppleUIKit = 14
}
