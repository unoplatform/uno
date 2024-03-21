using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Helpers;

internal static partial class AccessibilityAnnouncer
{
	internal static object WindowWrapper { get; set; }

	public static void Announce(string text)
	{
#if __WASM__
		if (WindowWrapper is not null)
		{
			AnnounceA11y(WindowWrapper, text);
		}
#endif
	}

#if __WASM__
	[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.announceA11y")]
	private static partial void AnnounceA11y([JSMarshalAs<JSType.Any>] object owner, string text);
#endif
}
