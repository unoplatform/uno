#if __IOS__ || __MACOS__ || UIKIT_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// iOS/macOS-specific implementation for WebResourceResponse.
/// </summary>
public partial class CoreWebView2WebResourceResponse
{
	private CoreWebView2HttpResponseHeaders? _headers;
	private int _statusCode = 200;
	private string _reasonPhrase = "OK";
	private global::Windows.Storage.Streams.IRandomAccessStream? _content;

	internal CoreWebView2WebResourceResponse() { }

	public global::Windows.Storage.Streams.IRandomAccessStream Content
	{
		get => _content ?? throw new InvalidOperationException("Content has not been set.");
		set => _content = value;
	}

	public CoreWebView2HttpResponseHeaders Headers
		=> _headers ??= new CoreWebView2HttpResponseHeaders();

	public int StatusCode
	{
		get => _statusCode;
		set => _statusCode = value;
	}

	public string ReasonPhrase
	{
		get => _reasonPhrase;
		set => _reasonPhrase = value ?? "OK";
	}
}
#endif
