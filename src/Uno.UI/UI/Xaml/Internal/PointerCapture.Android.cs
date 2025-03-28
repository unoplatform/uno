using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Core;

partial class PointerCapture
{
	partial void AddOptions(UIElement target, PointerCaptureOptions options)
	{
		if (options.HasFlag(PointerCaptureOptions.PreventOSSteal))
		{
			target.RequestDisallowInterceptTouchEvent(true);
		}
	}

	// Note: We don't have to implement RemoveOptions because the Android will clear the RequestDisallowInterceptTouchEvent by it's own on pointer release
	//		 and forcefully doing it in RemoveOptions could conflict with handling in the GestureRecognizer.
}
