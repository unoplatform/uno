#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

internal interface INativeWebResourceResponse
{
	IRandomAccessStream Content { get; set; }
	INativeHttpResponseHeaders Headers { get; }
	int StatusCode { get; set; }
	string ReasonPhrase { get; set; }
}

internal interface INativeHttpResponseHeaders : IEnumerable<KeyValuePair<string, string>>
{
	void AppendHeader(string name, string value);
	bool Contains(string name);
	string GetHeader(string name);
	object GetHeaders(string name); // Returns the native iterator object
}

internal interface INativeHttpHeadersCollectionIterator
{
	object Current { get; }
	bool HasCurrent { get; }
	bool MoveNext();
	uint GetMany(object items);
}

internal interface INativeWebResourceRequest
{
	string Uri { get; set; }
	string Method { get; set; }
	IRandomAccessStream Content { get; set; }
	INativeHttpRequestHeaders Headers { get; }
}

internal interface INativeHttpRequestHeaders : IEnumerable<KeyValuePair<string, string>>
{
	string GetHeader(string name);
	object GetHeaders(string name);
	bool Contains(string name);
	void SetHeader(string name, string value);
	void RemoveHeader(string name);
}

internal interface INativeWebResourceRequestedEventArgs
{
	object Request { get; }
	INativeWebResourceResponse? Response { get; set; }
	CoreWebView2WebResourceContext ResourceContext { get; }
	CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind { get; }
	Windows.Foundation.Deferral GetDeferral();
}
