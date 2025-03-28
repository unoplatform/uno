// TODO:MZ!!!


using System;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace Uno.UI.Helpers.WinUI;

internal static class CppWinRTHelpers
{
	public static IDisposable RegisterXamlRootChanged(XamlRoot xamlRoot, TypedEventHandler<XamlRoot, XamlRootChangedEventArgs> handler)
	{
		xamlRoot.Changed += handler;
		return Disposable.Create(() => xamlRoot.Changed -= handler);
	}

	public static IDisposable RegisterPropertyChanged(DependencyObject dependencyObject, DependencyProperty dependencyProperty, DependencyPropertyChangedCallback callback)
	{
		var token = dependencyObject.RegisterPropertyChangedCallback(dependencyProperty, callback);
		return Disposable.Create(() => dependencyObject.UnregisterPropertyChangedCallback(dependencyProperty, token));
	}

	public static bool SetFocus(DependencyObject obj, FocusState focusState)
	{
		if (obj != null)
		{
			// Use TryFocusAsync if it's available.
			if (false) //TODO Uno specific: WinUI checks for TryFocusAsync method presence, which is not implemented yet in Uno (issue #4256)
			{
				//var result = FocusManager.TryFocusAsync(obj, focusState);
				//if (result.Status == AsyncStatus.Completed)
				//{
				//	return result.GetResults().Succeeded;
				//}
				//// Operation was async, let's assume it worked.
				//return true;
			}

			if (obj is Control control)
			{
				return control.Focus(focusState);
			}
			else if (obj is Hyperlink hyperlink)
			{
				return hyperlink.Focus(focusState);
			}
#if !HAS_UNO_WINUI // ContentLink is no longer available in WinUI
			else if (obj is ContentLink contentlink)
			{
				return contentlink.Focus(focusState);
			}
#endif
			else if (obj is WebView webview)
			{
				return webview.Focus(focusState);
			}
		}

		return false;
	}
}
