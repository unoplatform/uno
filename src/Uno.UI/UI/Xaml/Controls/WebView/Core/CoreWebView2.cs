using System;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Net.Http;
using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal const string BlankUrl = "about:blank";
	internal static readonly Uri BlankUri = new Uri(BlankUrl);

	private readonly IWebView _owner;
	private readonly INativeWebView _nativeWebView;

	private object _source;

	internal CoreWebView2(IWebView owner)
	{
		_owner = owner;
	}

	public void Navigate(string uri)
	{
		if (!Uri.TryCreate(uri, UriKind.Absolute, out var actualUri))
		{
			throw new InvalidOperationException(); //TODO:MZ: What exception does UWP throw here?
		}

		_source = actualUri;
		ProcessNavigation(actualUri);
	}

	public void NavigateToString(string htmlContent)
	{
		_source = htmlContent;
		ProcessNavigation(htmlContent);
	}

	public void GoBack() => _nativeWebView.GoBack();

	public void GoForward() => _nativeWebView.GoForward();

	public void Stop() => _nativeWebView.Stop();

	public void Reload() => _nativeWebView.Reload();

	internal void OnOwnerApplyTemplate()
	{
		_nativeWebView = GetNativeWebViewFromTemplate();

		//The nativate WebView already navigate to a blank page if no source is set.
		//Avoid a bug where invoke GoBack() on WebView do nothing in Android 4.4
		_owner.UpdateFromInternalSource();
	}

	private bool VerifyWebViewAvailability()
	{
		if (_nativeWebView == null)
		{
			if (_owner.IsLoaded)
			{
				_owner.Log().Warn(
					"This WebView control instance does not have a native WebView child, " +
					"the control template may be missing.");
			}

			return false;
		}

		return true;
	}

	private void UpdateFromInternalSource()
	{
		if (_source is Uri uri)
		{
			ProcessNavigation(uri);
			return;
		}

		if (_source is string html)
		{
			ProcessNavigation(html);
		}

		if (_source is HttpRequestMessage httpRequestMessage)
		{
			ProcessNavigation(message);
		}
	}

	internal void RaiseNavigationStarting()
	{
		NavigationStarting?.Invoke(this, new CoreWebView2NavigationStartingEventArgs(0, null));//TODO:MZ:
	}

	internal void RaiseNewWindowRequested()
	{
		NewWindowRequested?.Invoke(this, new());//TODO:MZ:
	}

	internal void SetHistoryProperties(bool canGoBack, bool canGoForward)
	{
		CanGoBack = canGoBack;
		CanGoForward = canGoForward;
		HistoryChanged?.Invoke(this, null);
	}
}
