using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UWPSyncGenerator
{
	[Flags]
	public enum ImplementedFor
	{
		None = 0,
		Android = 1,
		iOS = 2,
		MacOS = 4,
		Net461 = 8,
		NetStdReference = 16,
		WASM = 32,
		Skia = 64,
		UAP = 128,
		Uno = Android | iOS | MacOS | Net461 | NetStdReference | WASM | Skia,
		Main = Android | iOS | WASM | Skia | MacOS,
		Mobile = Android | iOS,
		Xamarin = Android | iOS | MacOS
	}
}
