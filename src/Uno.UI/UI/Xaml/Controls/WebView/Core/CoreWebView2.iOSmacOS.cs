#nullable enable

using System;
using System.Linq;
using Uno.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Controls;

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#else
using AppKit;
using _View = AppKit.NSView;
#endif

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal INativeWebView? GetNativeWebViewFromTemplate()
	{
		var nativeWebView = ((_View)_owner)
			.FindSubviewsOfType<INativeWebView>()
			.FirstOrDefault();

		if (nativeWebView is null)
		{
			_owner.Log().Error(
				$"No view of type {nameof(NativeWebView)} found in children, " +
				$"are you missing one of these types in a template? ");
		}
		else
		{
			nativeWebView.SetOwner(this);
		}

		return nativeWebView;
	}
}

