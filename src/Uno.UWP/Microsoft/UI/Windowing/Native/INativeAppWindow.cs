using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;

namespace Microsoft.UI.Windowing.Native;

internal interface INativeAppWindow
{
	string Title { get; set; }

	void SetPresenter(AppWindowPresenter presenter);

#if __IOS__
	UIKit.UIScreen Screen { get; }
#elif __MACOS__
	AppKit.NSScreen Screen { get; }
#endif
}
