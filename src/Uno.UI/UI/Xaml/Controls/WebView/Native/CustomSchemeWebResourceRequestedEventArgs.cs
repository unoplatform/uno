#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// A request a native view received through its <see cref="WebViewVirtualHostMap.NativeScheme"/> handler.
/// </summary>
/// <param name="Uri">The native URL (<c>uno-app://...</c>).</param>
/// <param name="MainDocumentUri">The top-level document the request belongs to, when the engine reports it.</param>
internal sealed record CustomSchemeRequest(
	string Uri,
	string Method,
	IReadOnlyList<KeyValuePair<string, string>> Headers,
	byte[]? Body,
	string? MainDocumentUri);

/// <summary>
/// <c>WebResourceRequested</c> arguments for a request a native view serves itself, with a real deferral: the
/// response is read once the handler returned and every deferral it took is completed.
/// </summary>
internal sealed class CustomSchemeWebResourceRequestedEventArgs : INativeWebResourceRequestedEventArgs
{
	private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

	// One token for the synchronous handler call, plus one per deferral.
	private int _pending = 1;

	public CustomSchemeWebResourceRequestedEventArgs(CustomSchemeRequest request, CoreWebView2WebResourceContext resourceContext)
	{
		ArgumentNullException.ThrowIfNull(request);

		Request = new CustomSchemeWebResourceRequest(request);
		ResourceContext = resourceContext;
	}

	public INativeWebResourceRequest Request { get; }

	public INativeWebResourceResponse? Response { get; set; }

	public CoreWebView2WebResourceContext ResourceContext { get; }

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind => CoreWebView2WebResourceRequestSourceKinds.Document;

	/// <summary>
	/// Completes once the handler returned (<see cref="OnHandlerReturned"/>) and every deferral completed.
	/// </summary>
	public Task Completion => _completion.Task;

	public Deferral GetDeferral()
	{
		Interlocked.Increment(ref _pending);

		var completed = 0;
		return new Deferral(() =>
		{
			// Deferral.Complete may be called more than once; only the first call counts.
			if (Interlocked.Exchange(ref completed, 1) == 0)
			{
				Release();
			}
		});
	}

	/// <summary>
	/// Called once after the event was raised.
	/// </summary>
	public void OnHandlerReturned() => Release();

	private void Release()
	{
		if (Interlocked.Decrement(ref _pending) == 0)
		{
			_completion.TrySetResult();
		}
	}
}

internal sealed class CustomSchemeWebResourceRequest : INativeWebResourceRequest
{
	private IRandomAccessStream? _content;

	public CustomSchemeWebResourceRequest(CustomSchemeRequest request)
	{
		Uri = request.Uri;
		Method = request.Method;
		Headers = new ManagedHttpRequestHeaders(request.Headers);
		_content = request.Body is { Length: > 0 } body ? new MemoryStream(body, writable: false).AsRandomAccessStream() : null;
	}

	public string Uri { get; set; }

	public string Method { get; set; }

	// WebView2 reports a null Content for requests without a body.
	public IRandomAccessStream Content
	{
		get => _content!;
		set => _content = value;
	}

	public INativeHttpRequestHeaders Headers { get; }
}

/// <summary>
/// An ordered, case-insensitive request header list.
/// </summary>
internal sealed class ManagedHttpRequestHeaders : INativeHttpRequestHeaders
{
	private readonly List<KeyValuePair<string, string>> _headers;

	public ManagedHttpRequestHeaders(IEnumerable<KeyValuePair<string, string>>? headers = null)
		=> _headers = headers is null ? new() : new(headers);

	/// <exception cref="ArgumentException">No header is named <paramref name="name"/>, as WebView2 reports.</exception>
	public string GetHeader(string name)
	{
		var index = IndexOf(name);

		return index >= 0
			? _headers[index].Value
			: throw new ArgumentException($"The request has no '{name}' header.", nameof(name));
	}

	public INativeHttpHeadersCollectionIterator GetHeaders(string name)
		=> new ManagedHttpHeadersCollectionIterator(_headers.FindAll(header => IsMatch(header, name)));

	public bool Contains(string name) => IndexOf(name) >= 0;

	public void SetHeader(string name, string value)
	{
		ArgumentNullException.ThrowIfNull(name);

		RemoveHeader(name);
		_headers.Add(new(name, value ?? string.Empty));
	}

	public void RemoveHeader(string name) => _headers.RemoveAll(header => IsMatch(header, name));

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private int IndexOf(string name) => _headers.FindIndex(header => IsMatch(header, name));

	private static bool IsMatch(KeyValuePair<string, string> header, string name)
		=> string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase);
}
