using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.WindowSizeChangedEventArgs;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
#endif

namespace Microsoft.UI.Xaml
{
	public delegate void WindowSizeChangedEventHandler(object sender, WindowSizeChangedEventArgs e);
}
