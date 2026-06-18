using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.WinAppSDKSyncGenerator
{
	[Flags]
	public enum ImplementedFor
	{
		None = 0,
		Android = 1,
		iOS = 2,
		MacOS = 4,
		NetStdReference = 16,
		WASM = 32,
		Skia = 64,
		UAP = 128,
		tvOS = 256,
		Uno = Android | iOS | MacOS | NetStdReference | WASM | Skia | tvOS,
		Main = Android | iOS | WASM | Skia | MacOS | tvOS,
		Mobile = Android | iOS | tvOS,
		Xamarin = Android | iOS | MacOS | tvOS
	}
}
