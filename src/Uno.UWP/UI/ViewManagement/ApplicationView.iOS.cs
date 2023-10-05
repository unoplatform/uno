using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Core;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

namespace Windows.UI.ViewManagement;

partial class ApplicationView
{
	public bool TryEnterFullScreenMode()
	{
		CoreDispatcher.CheckThreadAccess();
		UIApplication.SharedApplication.StatusBarHidden = true;
		return UIApplication.SharedApplication.StatusBarHidden;
	}

	public void ExitFullScreenMode()
	{
		CoreDispatcher.CheckThreadAccess();
		UIApplication.SharedApplication.StatusBarHidden = false;
	}
}
