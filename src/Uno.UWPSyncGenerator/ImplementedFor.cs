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
		WASM = 16,
		UAP = 32,
		Uno = Android | iOS | MacOS | Net461 | WASM,
		Main = Android | iOS | WASM | MacOS,
		Mobile = Android | iOS,
		Xamarin = Android | iOS | MacOS
	}
}
