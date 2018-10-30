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
		Net46 = 8,
		WASM = 16,
		All = Android | iOS | MacOS | Net46 | WASM,
		Main = Android | iOS | WASM,
		Xamarin = Android | iOS
	}
}
