#if __IOS__ || UIKIT_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// iOS/macOS-specific implementation for WebResourceResponse.
/// </summary>
internal partial class NativeCoreWebView2WebResourceResponse : INativeWebResourceResponse
{
	private NativeCoreWebView2HttpResponseHeaders? _headers;
	private int _statusCode = 200;
	private string _reasonPhrase = "OK";
	private global::Windows.Storage.Streams.IRandomAccessStream? _content;

	internal NativeCoreWebView2WebResourceResponse() { }

	public global::Windows.Storage.Streams.IRandomAccessStream Content
	{
		get => _content ?? throw new InvalidOperationException("Content has not been set.");
		set => _content = value;
	}

	public INativeHttpResponseHeaders Headers
		=> _headers ??= new NativeCoreWebView2HttpResponseHeaders();

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
