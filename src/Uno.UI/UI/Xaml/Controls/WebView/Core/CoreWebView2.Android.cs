//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;
//using Android.Graphics;
//using Android.Runtime;
//using Android.Views;
//using Android.Webkit;
//using Uno.Disposables;
//using Uno.Extensions;
//using Uno.Foundation.Logging;
//using Uno.UI;
//using Uno.UI.Extensions;
//using Windows.Foundation;
//using Windows.Web;

using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	private NativeWebViewWrapper _nativeWebViewWrapper;
	private bool _wasLoadedFromString;

	internal INativeWebView GetNativeWebViewFromTemplate()
	{
		var webView = _owner
			.GetChildren(v => v is Android.Webkit.WebView)
			.FirstOrDefault() as Android.Webkit.WebView;

		// TODO:MZ: This uses "Android.Webkit.WebView" directly instead of
		// NativeWebView, so it is then wrapped in NativeWebViewWrapper.
		// Should we allow "custom" webview here, or just scratch this
		// and use our own NativeWebView instead?

		if (webView is null)
		{
			return;
		}

		_nativeWebViewWrapper = new NativeWebViewWrapper(webView);

		return _nativeWebViewWrapper;
	}

//	partial void NavigatePartial(Uri uri)
//	{
//		if (!_owner.VerifyWebViewAvailability())
//		{
//			return;
//		}

//		_wasLoadedFromString = false;
//		if (uri.Scheme.Equals("local", StringComparison.OrdinalIgnoreCase))
//		{
//			var path = $"file:///android_asset/{uri.PathAndQuery}";
//			_webView.LoadUrl(path);
//			return;
//		}

//		if (uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
//		{
//			CreateAndLaunchMailtoIntent(_webView.Context, uri.AbsoluteUri);
//			return;
//		}

//		//The replace is present because the uri cuts off any slashes that are more than two when it creates the uri.
//		//Therefore we add the final forward slash manually in Android because the file:/// requires 3 slashles.
//		_webView.LoadUrl(uri.AbsoluteUri.Replace("file://", "file:///"));
//	}

//	private void CreateAndLaunchMailtoIntent(Android.Content.Context context, string url)
//	{
//		var mailto = Android.Net.MailTo.Parse(url);

//		var email = new global::Android.Content.Intent(global::Android.Content.Intent.ActionSendto);

//		//Set the data with the mailto: uri to ensure only mail apps will show up as options for the user
//		email.SetData(global::Android.Net.Uri.Parse("mailto:"));
//		email.PutExtra(global::Android.Content.Intent.ExtraEmail, mailto.To);
//		email.PutExtra(global::Android.Content.Intent.ExtraCc, mailto.Cc);
//		email.PutExtra(global::Android.Content.Intent.ExtraSubject, mailto.Subject);
//		email.PutExtra(global::Android.Content.Intent.ExtraText, mailto.Body);

//		context.StartActivity(email);
//	}

//	partial void NavigateWithHttpRequestMessagePartial(HttpRequestMessage requestMessage)
//	{
//		if (!_owner.VerifyWebViewAvailability())
//		{
//			return;
//		}

//		var uri = requestMessage.RequestUri;
//		var headers = requestMessage.Headers
//			.Safe()
//			.ToDictionary(
//				header => header.Key,
//				element => element.Value.JoinBy(", ")
//			);

//		_wasLoadedFromString = false;
//		_webView.LoadUrl(uri.AbsoluteUri, headers);
//	}

//	partial void NavigateToStringPartial(string text)
//	{
//		if (!_owner.VerifyWebViewAvailability())
//		{
//			return;
//		}

//		_wasLoadedFromString = true;
//		//Note : _webView.LoadData does not work properly on Android 10 even when we encode to base64.
//		_webView.LoadDataWithBaseURL(null, text, "text/html; charset=utf-8", "utf-8", null);
//	}

//	//_owner should be IAsyncOperation<string> instead of Task<string> but we use an extension method to enable the same signature in Win.
//	//IAsyncOperation is not available in Xamarin.
//	internal async Task<string> InvokeScriptAsync(CancellationToken ct, string script, string[] arguments)
//	{
//		var argumentString = ConcatenateJavascriptArguments(arguments);

//		TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
//		ct.Register(() => tcs.TrySetCanceled());

//		_webView.EvaluateJavascript(
//			string.Format(CultureInfo.InvariantCulture, "javascript:{0}(\"{1}\");", script, argumentString),
//			new ScriptResponse(value => tcs.SetResult(value)));

//		return await tcs.Task;
//	}

//	internal IAsyncOperation<string> InvokeScriptAsync(string scriptName, IEnumerable<string> arguments) =>
//		AsyncOperation.FromTask(ct => InvokeScriptAsync(ct, scriptName, arguments?.ToArray()));


//	#region Navigation History

//	// On Windows, the WebView ignores "about:blank" entries from its navigation history.
//	// Because Android doesn't let you modify the navigation history, 
//	// we need CanGoBack, CanGoForward, GoBack and GoForward to take the above condition into consideration.

//	private void OnNavigationHistoryChanged()
//	{
//		// A non-zero number of steps to the nearest valid history entry means that navigation in the given direction is allowed
//		CanGoBack = GetStepsToNearestValidHistoryEntry(direction: -1 /* backward */) != 0;
//		CanGoForward = GetStepsToNearestValidHistoryEntry(direction: 1 /* forward */) != 0;
//	}





//	#endregion


//	private class ScriptResponse : Java.Lang.Object, IValueCallback
//	{
//		private Action<string> _setCallBackValue;
//		internal ScriptResponse(Action<string> setCallBackValue)
//		{
//			_setCallBackValue = setCallBackValue;
//		}

//		internal void OnReceiveValue(Java.Lang.Object value)
//		{
//			_setCallBackValue(value?.ToString() ?? string.Empty);
//		}
//	}

//	partial void OnScrollEnabledChangedPartial(bool scrollingEnabled)
//	{
//		_webView.HorizontalScrollBarEnabled = scrollingEnabled;
//		_webView.VerticalScrollBarEnabled = scrollingEnabled;
//	}

//	internal void OnNewWindowRequested(WebViewNewWindowRequestedEventArgs args)
//	{
//		NewWindowRequested?.Invoke(_owner, args);
//	}

//	private bool VerifyWebViewAvailability()
//	{
//		if (_webView == null)
//		{
//			if (_isLoaded)
//			{
//				_owner.Log().Warn("_owner WebView control instance does not have a UIViewView child, a Control template may be missing.");
//			}

//			return false;
//		}

//		return true;
//	}


//}
