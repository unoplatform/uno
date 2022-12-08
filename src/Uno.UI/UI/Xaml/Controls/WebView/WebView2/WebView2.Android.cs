using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class WebView2 : Control, ICustomClippingElement
{
	private Android.Webkit.WebView _webView;
	private bool _wasLoadedFromString;

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_webView = this
			.GetChildren(v => v is Android.Webkit.WebView)
			.FirstOrDefault() as Android.Webkit.WebView;

		// For some reason, the native WebView requires this internal registration
		// to avoid launching an external task, out of context of the current activity.
		//
		// This will still be used to handle extra activity with the native control.
		_webView.SetWebViewClient(new InternalClient(this));
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

		//The nativate WebView already navigate to a blank page if no source is set.
		//Avoid a bug where invoke GoBack() on WebView do nothing in Android 4.4
		this.UpdateFromInternalSource();
	}

	partial void NavigatePartial(Uri uri)
	{
		if (!this.VerifyWebViewAvailability())
		{
			return;
		}

		_wasLoadedFromString = false;
		if (uri.Scheme.Equals("local", StringComparison.OrdinalIgnoreCase))
		{
			var path = $"file:///android_asset/{uri.PathAndQuery}";
			_webView.LoadUrl(path);
			return;
		}

		if (uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
		{
			CreateAndLaunchMailtoIntent(_webView.Context, uri.AbsoluteUri);
			return;
		}

		//The replace is present because the uri cuts off any slashes that are more than two when it creates the uri.
		//Therefore we add the final forward slash manually in Android because the file:/// requires 3 slashles.
		_webView.LoadUrl(uri.AbsoluteUri.Replace("file://", "file:///"));
	}

	private void CreateAndLaunchMailtoIntent(Android.Content.Context context, string url)
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

	partial void NavigateWithHttpRequestMessagePartial(HttpRequestMessage requestMessage)
	{
		if (!this.VerifyWebViewAvailability())
		{
			return;
		}

		var uri = requestMessage.RequestUri;
		var headers = requestMessage.Headers
			.Safe()
			.ToDictionary(
				header => header.Key,
				element => element.Value.JoinBy(", ")
			);

		_wasLoadedFromString = false;
		_webView.LoadUrl(uri.AbsoluteUri, headers);
	}

	partial void NavigateToStringPartial(string text)
	{
		if (!this.VerifyWebViewAvailability())
		{
			return;
		}

		_wasLoadedFromString = true;
		//Note : _webView.LoadData does not work properly on Android 10 even when we encode to base64.
		_webView.LoadDataWithBaseURL(null, text, "text/html; charset=utf-8", "utf-8", null);
	}

	//This should be IAsyncOperation<string> instead of Task<string> but we use an extension method to enable the same signature in Win.
	//IAsyncOperation is not available in Xamarin.
	public async Task<string> InvokeScriptAsync(CancellationToken ct, string script, string[] arguments)
	{
		var argumentString = ConcatenateJavascriptArguments(arguments);

		TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
		ct.Register(() => tcs.TrySetCanceled());

		_webView.EvaluateJavascript(
			string.Format(CultureInfo.InvariantCulture, "javascript:{0}(\"{1}\");", script, argumentString),
			new ScriptResponse(value => tcs.SetResult(value)));

		return await tcs.Task;
	}

	public IAsyncOperation<string> InvokeScriptAsync(string scriptName, IEnumerable<string> arguments) =>
		AsyncOperation.FromTask(ct => InvokeScriptAsync(ct, scriptName, arguments?.ToArray()));


	#region Navigation History

	// On Windows, the WebView ignores "about:blank" entries from its navigation history.
	// Because Android doesn't let you modify the navigation history, 
	// we need CanGoBack, CanGoForward, GoBack and GoForward to take the above condition into consideration.

	private void OnNavigationHistoryChanged()
	{
		// A non-zero number of steps to the nearest valid history entry means that navigation in the given direction is allowed
		CanGoBack = GetStepsToNearestValidHistoryEntry(direction: -1 /* backward */) != 0;
		CanGoForward = GetStepsToNearestValidHistoryEntry(direction: 1 /* forward */) != 0;
	}

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

	private void GoToNearestValidHistoryEntry(int direction) =>
		Enumerable
			.Repeat(
				element: direction > 0
					? (Action)_webView.GoForward
					: (Action)_webView.GoBack,
				count: GetStepsToNearestValidHistoryEntry(direction))
			.ForEach(action => action.Invoke());

	partial void GoBackPartial() => GoToNearestValidHistoryEntry(direction: -1 /* backward */);
	partial void GoForwardPartial() => GoToNearestValidHistoryEntry(direction: 1 /* forward */);

	#endregion

	partial void StopPartial()
	{
		_webView.StopLoading();
	}

	private class ScriptResponse : Java.Lang.Object, IValueCallback
	{
		private Action<string> _setCallBackValue;
		public ScriptResponse(Action<string> setCallBackValue)
		{
			_setCallBackValue = setCallBackValue;
		}

		public void OnReceiveValue(Java.Lang.Object value)
		{
			_setCallBackValue(value?.ToString() ?? string.Empty);
		}
	}

	partial void OnScrollEnabledChangedPartial(bool scrollingEnabled)
	{
		_webView.HorizontalScrollBarEnabled = scrollingEnabled;
		_webView.VerticalScrollBarEnabled = scrollingEnabled;
	}

	internal void OnNewWindowRequested(WebViewNewWindowRequestedEventArgs args)
	{
		NewWindowRequested?.Invoke(this, args);
	}

	public void Refresh()
	{
		_webView.Reload();
	}

	private bool VerifyWebViewAvailability()
	{
		if (_webView == null)
		{
			if (_isLoaded)
			{
				this.Log().Warn("This WebView control instance does not have a UIViewView child, a Control template may be missing.");
			}

			return false;
		}

		return true;
	}

	private class InternalClient : Android.Webkit.WebViewClient
	{
		private readonly WebView _webView;
		//This is because we go through onReceivedError() and OnPageFinished() when the call fail.
		private bool _webViewSuccess = true;
		//This is to not have duplicate event call
		private WebErrorStatus _webErrorStatus = WebErrorStatus.Unknown;
		public InternalClient(WebView webView)
		{
			_webView = webView;

			if (FeatureConfiguration.WebView.ForceSoftwareRendering)
			{
				//SetLayerType disables hardware acceleration for a single view.
				//This is required to remove glitching issues particularly when having a keyboard pop-up with a webview present.
				//http://developer.android.com/guide/topics/graphics/hardware-accel.html
				//http://stackoverflow.com/questions/27172217/android-systemui-glitches-in-lollipop
				_webView.SetLayerType(LayerType.Software, null);
			}
		}

#pragma warning disable CS0672 // Member overrides obsolete member
		public override bool ShouldOverrideUrlLoading(Android.Webkit.WebView view, string url)
#pragma warning restore CS0672 // Member overrides obsolete member
		{
			if (url.StartsWith(Uri.UriSchemeMailto, true, CultureInfo.InvariantCulture))
			{
				_webView.CreateAndLaunchMailtoIntent(view.Context, url);
				return true;
			}

			var args = new WebViewNavigationStartingEventArgs()
			{
				Uri = new Uri(url)
			};

			_webView.NavigationStarting?.Invoke(_webView, args);

			return args.Cancel;
		}

		public override void OnPageStarted(Android.Webkit.WebView view, string url, Bitmap favicon)
		{
			base.OnPageStarted(view, url, favicon);
			//Reset Webview Success on page started so that if we have successful navigation we don't send an webView error if a previous error happened.
			_webViewSuccess = true;
		}

#pragma warning disable 0672, 618
		public override void OnReceivedError(Android.Webkit.WebView view, [GeneratedEnum] ClientError errorCode, string description, string failingUrl)
		{
			_webViewSuccess = false;
			_webErrorStatus = ConvertClientError(errorCode);

			base.OnReceivedError(view, errorCode, description, failingUrl);
		}
#pragma warning restore 0672, 618

		public override void OnPageFinished(Android.Webkit.WebView view, string url)
		{
			_webView.DocumentTitle = view.Title;

			_webView.OnNavigationHistoryChanged();

			var args = new WebViewNavigationCompletedEventArgs()
			{
				IsSuccess = _webViewSuccess,
				WebErrorStatus = _webErrorStatus
			};
			if (!_webView._wasLoadedFromString && !string.IsNullOrEmpty(url))
			{
				args.Uri = new Uri(url);
			}

			_webView.NavigationCompleted?.Invoke(_webView, args);
			base.OnPageFinished(view, url);
		}

		//Matched using these two sources
		//http://developer.xamarin.com/api/type/Android.Webkit.ClientError/
		//https://msdn.microsoft.com/en-ca/library/windows/apps/windows.web.weberrorstatus
		private WebErrorStatus ConvertClientError(ClientError clientError)
		{
			switch (clientError)
			{
				case ClientError.Authentication:
					return WebErrorStatus.Unauthorized;
				case ClientError.BadUrl:
					return WebErrorStatus.BadRequest;
				case ClientError.Connect:
					return WebErrorStatus.CannotConnect;
				case ClientError.FailedSslHandshake:
					return WebErrorStatus.UnexpectedClientError;
				case ClientError.File:
					return WebErrorStatus.UnexpectedClientError;
				case ClientError.FileNotFound:
					return WebErrorStatus.NotFound;
				case ClientError.HostLookup:
					return WebErrorStatus.HostNameNotResolved;
				case ClientError.Io:
					return WebErrorStatus.InternalServerError;
				case ClientError.ProxyAuthentication:
					return WebErrorStatus.ProxyAuthenticationRequired;
				case ClientError.RedirectLoop:
					return WebErrorStatus.RedirectFailed;
				case ClientError.Timeout:
					return WebErrorStatus.Timeout;
				case ClientError.TooManyRequests:
					return WebErrorStatus.UnexpectedClientError;
				case ClientError.Unknown:
					return WebErrorStatus.Unknown;
				case ClientError.UnsupportedAuthScheme:
					return WebErrorStatus.Unauthorized;
				case ClientError.UnsupportedScheme:
					return WebErrorStatus.UnexpectedClientError;
				default:
					return WebErrorStatus.UnexpectedClientError;
			}
		}
	}

	private class InternalWebChromeClient : WebChromeClient
	{
		private IValueCallback _filePathCallback;

		readonly SerialDisposable _fileChooserTaskDisposable = new SerialDisposable();

		public override bool OnShowFileChooser(Android.Webkit.WebView webView, IValueCallback filePathCallback, FileChooserParams fileChooserParams)
		{
			_filePathCallback = filePathCallback;

			var cancellationDisposable = new CancellationDisposable();
			_fileChooserTaskDisposable.Disposable = cancellationDisposable;

			Task.Run(async () =>
			{
				try
				{
					await StartFileChooser(cancellationDisposable.Token, fileChooserParams);
				}
				catch (Exception e)
				{
					this.Log().Error(e.Message, e);
				}
			});

			return true;
		}

		private async Task StartFileChooser(CancellationToken ct, FileChooserParams fileChooserParams)
		{
			var intent = fileChooserParams.CreateIntent();
			//Get an invisible (Transparent) Activity to handle the Intent
			var delegateActivity = await StartActivity<DelegateActivity>(ct);

			var result = await delegateActivity.GetActivityResult(ct, intent);

			_filePathCallback.OnReceiveValue(FileChooserParams.ParseResult((int)result.ResultCode, result.Intent));
		}

		public override void OnPermissionRequest(PermissionRequest request)
		{
			request.Grant(request.GetResources());
		}

		/// <summary>
		/// Uses the Activity Tracker to start, then return an Activity
		/// </summary>
		/// <typeparam name="T">A BaseActivity to start</typeparam>
		/// <param name="ct">CancellationToken</param>
		/// <returns>The BaseActivity that just started (OnResume called)</returns>
		private async Task<T> StartActivity<T>(CancellationToken ct) where T : BaseActivity
		{
			//Get topmost Activity
			var currentActivity = BaseActivity.Current;

			if (currentActivity != null)
			{
				//Set up event handler for when activity changes
				var finished = new TaskCompletionSource<BaseActivity>();

				EventHandler<CurrentActivityChangedEventArgs> handler = null;
				handler = new EventHandler<CurrentActivityChangedEventArgs>((snd, args) =>
				{
					if (args?.Current != null)
					{
						finished.TrySetResult(args.Current);
						BaseActivity.CurrentChanged -= handler;
					}
				});

				BaseActivity.CurrentChanged += handler;

				//Start a new DelegateActivity
				currentActivity.StartActivity(typeof(T));

				//Wait for it to be the current....
				var newCurrent = await finished.Task;

				//return the activity.
				return newCurrent as T;
			}

			return null;
		}
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

	// Force clipping, otherwise native WebView may exceed its bounds in some circumstances (eg when Xaml WebView is animated)
	bool ICustomClippingElement.ForceClippingToLayoutSlot => true;
}
}
