using System;
using System.Linq;
using Android.Webkit;

namespace Uno.UI.Xaml.Controls;

internal class NativeWebViewWrapper : INativeWebView
{
	private readonly WebView _webView;

	public NativeWebViewWrapper(WebView webView) : base(ContextHelper.Current)
	{
		_webView = webView;

		// For some reason, the native WebView requires this internal registration
		// to avoid launching an external task, out of context of the current activity.
		//
		// this will still be used to handle extra activity with the native control.

		_webView.SetWebViewClient(new InternalClient(_owner));
		_webView.SetWebChromeClient(new InternalWebChromeClient());
		_webView.Settings.JavaScriptEnabled = true;
		_webView.Settings.DomStorageEnabled = true;
		_webView.Settings.BuiltInZoomControls = true;
		_webView.Settings.DisplayZoomControls = false;
		_webView.Settings.SetSupportZoom(true);
		_webView.Settings.LoadWithOverviewMode = true;
		_webView.Settings.UseWideViewPort = true;

		//Allow ThirdPartyCookies by default only on Android 5.0 and UP
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
		{
			Android.Webkit.CookieManager.Instance.SetAcceptThirdPartyCookies(_webView, true);
		}

		// The native webview control requires to have LayoutParameters to function properly.
		_webView.LayoutParameters = new ViewGroup.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
	}

	public void GoBack() => GoToNearestValidHistoryEntry(direction: -1 /* backward */);

	public void GoForward() => GoToNearestValidHistoryEntry(direction: 1 /* forward */);

	public void Stop() => _webView.StopLoading();

	private void GoToNearestValidHistoryEntry(int direction) =>
		Enumerable
			.Repeat(
				element: direction > 0
					? (Action)_webView.GoForward
					: (Action)_webView.GoBack,
				count: GetStepsToNearestValidHistoryEntry(direction))
			.ForEach(action => action.Invoke());

	private int GetStepsToNearestValidHistoryEntry(int direction)
	{
		var history = _webView.CopyBackForwardList();

		// Iterate through every next/previous (depending on direction) history entry until a valid one is found
		for (int i = history.CurrentIndex + direction; 0 <= i && i < history.Size; i += direction)
			if (GetIsHistoryEntryValid(history.GetItemAtIndex(i).Url))
				// return the absolute number of steps from the current entry to the nearest valid entry
				return Math.Abs(i - history.CurrentIndex);

		return 0; // no valid entry found
	}
}

