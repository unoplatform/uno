using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Helpers;

public enum UnoRuntimePlatform
{
	Unknown,
	Android,
	iOS,
	MacCatalyst,
	MacOSX,
	WebAssembly,
	Windows,
	SkiaGtk,
	SkiaWpfIslands,
	SkiaX11,
	SkiaFrameBuffer,
	SkiaMacOS
}
