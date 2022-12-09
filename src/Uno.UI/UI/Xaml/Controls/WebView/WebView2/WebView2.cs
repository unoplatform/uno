#if __WASM__ || __MACOS__
#pragma warning disable CS0067, CS0414
#endif

#if XAMARIN || __WASM__ || __SKIA__
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an object that enables the hosting of web content.
/// </summary>
#if __WASM__ || __SKIA__
[NotImplemented]
#endif
public partial class WebView2 : Control
{
	private const string BlankUrl = "about:blank";
	private static readonly Uri BlankUri = new Uri(BlankUrl);

	private object _internalSource;
	private string _invokeScriptResponse = string.Empty;

#pragma warning disable CS0414 // not used in skia
	private bool _isLoaded;
#pragma warning restore CS0414

	public WebView()
	{
		DefaultStyleKey = typeof(WebView);
	}

	public void GoBack()
	{
		GoBackPartial();
	}

	public void GoForward()
	{
		GoForwardPartial();
	}

	public void Navigate(Uri uri)
	{
		this.SetInternalSource(uri ?? BlankUri);
	}

	//
	// Summary:
	//     Loads the specified HTML content as a new document.
	//
	// Parameters:
	//   text:
	//     The HTML content to display in the WebView control.
	public void NavigateToString(string text)
	{
		this.SetInternalSource(text ?? "");
	}

	public void NavigateWithHttpRequestMessage(HttpRequestMessage requestMessage)
	{
		if (requestMessage?.RequestUri == null)
		{
			throw new ArgumentException("Invalid request message. It does not have a RequestUri.");
		}

		SetInternalSource(requestMessage);
	}

	public void Stop()
	{
		StopPartial();
	}

	partial void GoBackPartial();
	partial void GoForwardPartial();
	partial void NavigatePartial(Uri uri);
	partial void NavigateToStringPartial(string text);
	partial void NavigateWithHttpRequestMessagePartial(HttpRequestMessage requestMessage);
	partial void StopPartial();

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		_isLoaded = true;
	}

	private void SetInternalSource(object source)
	{
		_internalSource = source;

		this.UpdateFromInternalSource();
	}

	private void UpdateFromInternalSource()
	{
		var uri = _internalSource as Uri;
		if (uri != null)
		{
			NavigatePartial(uri);
			return;
		}

		var html = _internalSource as string;
		if (html != null)
		{
			NavigateToStringPartial(html);
		}

		var message = _internalSource as HttpRequestMessage;
		if (message != null)
		{
			NavigateWithHttpRequestMessagePartial(message);
		}
	}

	private static string ConcatenateJavascriptArguments(string[] arguments)
	{
		var argument = string.Empty;
		if (arguments != null && arguments.Any())
		{
			argument = string.Join(",", arguments);
		}

		return argument;
	}

	internal void OnUnsupportedUriSchemeIdentified(WebViewUnsupportedUriSchemeIdentifiedEventArgs args)
	{
		UnsupportedUriSchemeIdentified?.Invoke(this, args);
	}

	internal bool GetIsHistoryEntryValid(string url) => !url.IsNullOrWhiteSpace() && !url.Equals(BlankUrl, StringComparison.OrdinalIgnoreCase);
}
#endif
