#nullable disable

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Android.Webkit;
using Android.Views;
using Android.Content;
using Uno.Extensions;
using Windows.Foundation;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeWebViewWrapper : INativeWebView
{
	private readonly Uri AndroidAssetBaseUri = new Uri("file:///android_asset/");

	private readonly WebView _webView;
	private readonly CoreWebView2 _coreWebView;

	private string _documentTitle;
	internal bool _wasLoadedFromString;

	public NativeWebViewWrapper(WebView webView, CoreWebView2 coreWebView)
	{
		_webView = webView;
		_coreWebView = coreWebView;

		// For some reason, the native WebView requires this internal registration
		// to avoid launching an external task, out of context of the current activity.
		//
		// this will still be used to handle extra activity with the native control.

		_webView.Settings.JavaScriptEnabled = true;
		_webView.Settings.DomStorageEnabled = true;
		_webView.Settings.BuiltInZoomControls = true;
		_webView.Settings.DisplayZoomControls = false;
		_webView.Settings.SetSupportZoom(true);
		_webView.Settings.LoadWithOverviewMode = true;
		_webView.Settings.UseWideViewPort = true;
		_webView.Settings.SetSupportMultipleWindows(true);
		_webView.SetWebViewClient(new InternalClient(_coreWebView, this));
		_webView.SetWebChromeClient(new InternalWebChromeClient(_coreWebView));

		_webView.AddJavascriptInterface(new UnoWebViewHandler(this), "unoWebView");

		//Allow ThirdPartyCookies by default only on Android 5.0 and UP
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
		{
			Android.Webkit.CookieManager.Instance.SetAcceptThirdPartyCookies(_webView, true);
		}

#if !ANDROID_SKIA
		// The native webview control requires to have LayoutParameters to function properly.
		_webView.LayoutParameters = new ViewGroup.LayoutParams(
			ViewGroup.LayoutParams.MatchParent,
			ViewGroup.LayoutParams.MatchParent);
#endif

#if !ANDROID_SKIA // We only have the flag for Android native. We can add it to Skia if needed.
		if (FeatureConfiguration.WebView.ForceSoftwareRendering)
		{
			//SetLayerType disables hardware acceleration for a single view.
			//_owner is required to remove glitching issues particularly when having a keyboard pop-up with a webview present.
			//http://developer.android.com/guide/topics/graphics/hardware-accel.html
			//http://stackoverflow.com/questions/27172217/android-systemui-glitches-in-lollipop
			_webView.SetLayerType(LayerType.Software, null);
		}
#endif
	}

	public string DocumentTitle
	{
		get => _documentTitle;
		internal set
		{
			if (_documentTitle != value)
			{
				_documentTitle = value;
				_coreWebView?.OnDocumentTitleChanged();
			}
		}
	}

	internal WebView WebView => _webView;

	public void GoBack() => GoToNearestValidHistoryEntry(direction: -1 /* backward */);

	public void GoForward() => GoToNearestValidHistoryEntry(direction: 1 /* forward */);

	public void Stop() => _webView.StopLoading();

	public void Reload() => _webView.Reload();

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
		_webView.HorizontalScrollBarEnabled = isScrollingEnabled;
		_webView.VerticalScrollBarEnabled = isScrollingEnabled;
	}

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
			if (CoreWebView2.GetIsHistoryEntryValid(history.GetItemAtIndex(i).Url))
				// return the absolute number of steps from the current entry to the nearest valid entry
				return Math.Abs(i - history.CurrentIndex);

		return 0; // no valid entry found
	}

	internal void CreateAndLaunchMailtoIntent(Android.Content.Context context, string url)
	{
		var mailto = Android.Net.MailTo.Parse(url);

		var email = new global::Android.Content.Intent(global::Android.Content.Intent.ActionSendto);

		//Set the data with the mailto: uri to ensure only mail apps will show up as options for the user
		email.SetData(global::Android.Net.Uri.Parse("mailto:"));
		email.PutExtra(global::Android.Content.Intent.ExtraEmail, mailto.To);
		email.PutExtra(global::Android.Content.Intent.ExtraCc, mailto.Cc);
		email.PutExtra(global::Android.Content.Intent.ExtraSubject, mailto.Subject);
		email.PutExtra(global::Android.Content.Intent.ExtraText, mailto.Body);

		context.StartActivity(email);
	}

	public void ProcessNavigation(Uri uri)
	{
		_wasLoadedFromString = false;
		if (uri.Scheme.Equals("local", StringComparison.OrdinalIgnoreCase))
		{
			var path = $"file:///android_asset/{uri.PathAndQuery}";
			ScheduleNavigationStarting(path, () => _webView.LoadUrl(path));
			return;
		}

		if (uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
		{
			ScheduleNavigationStarting(uri.AbsoluteUri, () => CreateAndLaunchMailtoIntent(_webView.Context, uri.AbsoluteUri));
			return;
		}

		if (_coreWebView.HostToFolderMap.TryGetValue(uri.Host.ToLowerInvariant(), out var folderName))
		{
			// Load Url with folder
			var folderUri = new Uri(AndroidAssetBaseUri, folderName + '/');

			var relativePath = uri.PathAndQuery;

			if (relativePath.StartsWith('/'))
			{
				relativePath = relativePath.Substring(1);
			}

			var assetUri = new Uri(folderUri, relativePath);

			uri = assetUri;
		}

		// The replace is present because the URI cuts off any slashes that are more than two when it creates the URI.
		// Therefore we add the final forward slash manually in Android because the file:/// requires 3 slashes.
		// However, we want to be smart and only replace if we find file:// not followed by a third slash at the start
		var actualUri = MalformedFileUri().Replace(uri.AbsoluteUri, "file:///");
		ScheduleNavigationStarting(actualUri, () => _webView.LoadUrl(actualUri));
	}

	public void ProcessNavigation(HttpRequestMessage requestMessage)
	{
		var uri = requestMessage.RequestUri;
		var headers = requestMessage.Headers
			.Safe()
			.ToDictionary(
				header => header.Key,
				element => element.Value.JoinBy(", ")
			);

		_wasLoadedFromString = false;
		ScheduleNavigationStarting(uri.AbsoluteUri, () => _webView.LoadUrl(uri.AbsoluteUri, headers));
	}

	public void ProcessNavigation(string html)
	{
		_wasLoadedFromString = true;
		//Note : _webView.LoadData does not work properly on Android 10 even when we encode to base64.
		ScheduleNavigationStarting(null, () => _webView.LoadDataWithBaseURL(null, html, "text/html; charset=utf-8", "utf-8", null));
	}

	private void ScheduleNavigationStarting(string url, Action loadAction)
	{
		// For programmatically-triggered navigations the ShouldOverrideUrlLoading method is not called,
		// to workaround this, we raise the NavigationStarting event here, asynchronously to be in line with
		// the WinUI behavior.
		_ = _coreWebView.Owner.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
		{
			// Ensure we pass the correct navigation data - use Uri for file URLs, string for data URLs
			object navigationData = url;
			if (!string.IsNullOrEmpty(url) && !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			{
				navigationData = new Uri(url);
			}

			_coreWebView.RaiseNavigationStarting(navigationData, out var cancel);

			if (!cancel)
			{
				loadAction.Invoke();
			}
		});
	}

	async Task<string> INativeWebView.ExecuteScriptAsync(string script, CancellationToken token)
	{
		TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

		using var _ = token.Register(() => tcs.TrySetCanceled());

		_webView.EvaluateJavascript(
			string.Format(CultureInfo.InvariantCulture, "javascript:{0}", script),
			new ScriptResponse(value => tcs.SetResult(value)));

		return await tcs.Task;
	}

	async Task<string> INativeWebView.InvokeScriptAsync(string script, string[] arguments, CancellationToken ct)
	{
		var argumentString = Microsoft.UI.Xaml.Controls.WebView.ConcatenateJavascriptArguments(arguments);

		TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
		ct.Register(() => tcs.TrySetCanceled());

		_webView.EvaluateJavascript(
			string.Format(CultureInfo.InvariantCulture, "javascript:{0}(\"{1}\");", script, argumentString),
			new ScriptResponse(value => tcs.SetResult(value)));

		return await tcs.Task;
	}

	// On Windows, the WebView ignores "about:blank" entries from its navigation history.
	// Because Android doesn't let you modify the navigation history, 
	// we need CanGoBack, CanGoForward, GoBack and GoForward to take the above condition into consideration.

	internal void RefreshHistoryProperties()
	{
		// A non-zero number of steps to the nearest valid history entry means that navigation in the given direction is allowed
		var canGoBack = GetStepsToNearestValidHistoryEntry(direction: -1 /* backward */) != 0;
		var canGoForward = GetStepsToNearestValidHistoryEntry(direction: 1 /* forward */) != 0;
		_coreWebView.SetHistoryProperties(canGoBack, canGoForward);
	}

	private class ScriptResponse : Java.Lang.Object, IValueCallback
	{
		private Action<string> _setCallBackValue;

		internal ScriptResponse(Action<string> setCallBackValue)
		{
			_setCallBackValue = setCallBackValue;
		}

		public void OnReceiveValue(Java.Lang.Object value)
		{
			_setCallBackValue(value?.ToString() ?? string.Empty);
		}
	}

	internal void OnScrollEnabledChangedPartial(bool scrollingEnabled)
	{
		_webView.HorizontalScrollBarEnabled = scrollingEnabled;
		_webView.VerticalScrollBarEnabled = scrollingEnabled;
	}

	internal void OnWebMessageReceived(string message)
	{
		if (_coreWebView.Settings.IsWebMessageEnabled)
		{
			_coreWebView.RaiseWebMessageReceived(message);
		}
	}

	[System.Text.RegularExpressions.GeneratedRegex(@"^file://(?!/)")]
	private static partial System.Text.RegularExpressions.Regex MalformedFileUri();
}

