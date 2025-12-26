#nullable enable

#if ANDROID_SKIA
using System;
using Android.Runtime;
using Android.Webkit;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// WebViewClient that intercepts web resource requests for Skia Android.
/// This is a Skia Android-specific version that works with the adapter pattern.
/// </summary>
internal class WebResourceInterceptingClientSkia : WebViewClient
{
	private readonly CoreWebView2 _coreWebView;
	private readonly NativeWebViewWrapper _wrapper;

	public WebResourceInterceptingClientSkia(CoreWebView2 coreWebView, NativeWebViewWrapper wrapper)
	{
		_coreWebView = coreWebView ?? throw new ArgumentNullException(nameof(coreWebView));
		_wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
	}

	public override WebResourceResponse? ShouldInterceptRequest(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
	{
		try
		{
			var response = _wrapper.OnWebResourceRequested(request);
			if (response != null)
			{
				return response;
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Error in ShouldInterceptRequest", ex);
			}
		}

		return base.ShouldInterceptRequest(view, request);
	}

	public override void OnPageFinished(global::Android.Webkit.WebView? view, string? url)
	{
		base.OnPageFinished(view, url);

		if (view == null)
		{
			return;
		}

		try
		{
			var uri = string.IsNullOrWhiteSpace(url) ? null : new Uri(url);
			_coreWebView.RaiseNavigationCompleted(uri, isSuccess: true, httpStatusCode: 200, CoreWebView2WebErrorStatus.Unknown);
			_coreWebView.SetHistoryProperties(view.CanGoBack(), view.CanGoForward());
			_coreWebView.RaiseHistoryChanged();
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Error in OnPageFinished", ex);
			}
		}
	}

	public override void OnReceivedError(global::Android.Webkit.WebView? view, IWebResourceRequest? request, WebResourceError? error)
	{
		base.OnReceivedError(view, request, error);

		if (view == null || request == null)
		{
			return;
		}

		// Only handle main frame errors
		if (!request.IsForMainFrame)
		{
			return;
		}

		try
		{
			var url = request.Url?.ToString();
			var uri = string.IsNullOrWhiteSpace(url) ? null : new Uri(url);
			var errorStatus = MapErrorCode(error?.ErrorCode ?? ClientError.Unknown);
			_coreWebView.RaiseNavigationCompleted(uri, isSuccess: false, httpStatusCode: 0, errorStatus);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Error in OnReceivedError", ex);
			}
		}
	}

	public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
	{
		if (request == null)
		{
			return false;
		}

		var url = request.Url?.ToString();
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}

		try
		{
			var uri = new Uri(url);

			// Check for unsupported schemes
			if (!IsHttpOrHttps(uri))
			{
				_coreWebView.RaiseUnsupportedUriSchemeIdentified(uri, out var handled);
				return handled;
			}

			// Check navigation starting
			_coreWebView.RaiseNavigationStarting(uri, out var cancel);
			return cancel;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Error in ShouldOverrideUrlLoading", ex);
			}
			return false;
		}
	}

	public override void OnPageStarted(global::Android.Webkit.WebView? view, string? url, global::Android.Graphics.Bitmap? favicon)
	{
		base.OnPageStarted(view, url, favicon);
	}

	private static bool IsHttpOrHttps(Uri uri)
		=> string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);

	private static CoreWebView2WebErrorStatus MapErrorCode(ClientError errorCode)
	{
		return errorCode switch
		{
			ClientError.HostLookup => CoreWebView2WebErrorStatus.HostNameNotResolved,
			ClientError.Connect => CoreWebView2WebErrorStatus.CannotConnect,
			ClientError.Timeout => CoreWebView2WebErrorStatus.Timeout,
			ClientError.FailedSslHandshake => CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect,
			ClientError.Authentication => CoreWebView2WebErrorStatus.ServerUnreachable,
			ClientError.FileNotFound => CoreWebView2WebErrorStatus.Unknown,
			ClientError.UnsupportedScheme => CoreWebView2WebErrorStatus.Unknown,
			_ => CoreWebView2WebErrorStatus.Unknown
		};
	}
}
#endif
