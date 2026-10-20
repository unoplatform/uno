#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// A plain copy of a web resource response, for native views that hand custom responses to their engine as status,
/// headers and bytes.
/// </summary>
/// <remarks>
/// Works with any <see cref="INativeWebResourceResponse"/>: the <see cref="ManagedWebResourceResponse"/> that
/// <c>CoreWebView2Environment.CreateWebResourceResponse</c> creates as well as platform ones.
/// </remarks>
internal sealed record WebResourceResponseSnapshot(
	int StatusCode,
	string ReasonPhrase,
	IReadOnlyList<KeyValuePair<string, string>> Headers,
	byte[] Body)
{
	public static WebResourceResponseSnapshot Create(INativeWebResourceResponse response)
	{
		ArgumentNullException.ThrowIfNull(response);

		var body = Array.Empty<byte>();
		if (response.Content is { } content)
		{
			body = ReadBody(content, static (stream, destination) =>
			{
				stream.CopyTo(destination);
				return Task.CompletedTask;
			}, CancellationToken.None).GetAwaiter().GetResult();
		}

		return new(response.StatusCode, response.ReasonPhrase ?? string.Empty, CopyHeaders(response), body);
	}

	public static async Task<WebResourceResponseSnapshot> CreateAsync(INativeWebResourceResponse response, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(response);

		var body = Array.Empty<byte>();
		if (response.Content is { } content)
		{
			body = await ReadBody(content, (stream, destination) => stream.CopyToAsync(destination, ct), ct);
		}

		return new(response.StatusCode, response.ReasonPhrase ?? string.Empty, CopyHeaders(response), body);
	}

	private static List<KeyValuePair<string, string>> CopyHeaders(INativeWebResourceResponse response)
	{
		var headers = new List<KeyValuePair<string, string>>();
		if (response.Headers is { } source)
		{
			foreach (var header in source)
			{
				headers.Add(new(header.Key, header.Value ?? string.Empty));
			}
		}

		return headers;
	}

	/// <remarks>
	/// A seekable body is read from its start, as WebView2 does (apps commonly hand over a stream left at its end
	/// after writing), and its position is restored so the response stays usable. A non-seekable body is read from
	/// where it is. The stream is not disposed: the response still owns it.
	/// </remarks>
	private static async Task<byte[]> ReadBody(IRandomAccessStream content, Func<Stream, Stream, Task> copy, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var stream = content.AsStream();
		using var destination = new MemoryStream();

		if (stream.CanSeek)
		{
			var position = stream.Position;
			stream.Position = 0;
			try
			{
				await copy(stream, destination);
			}
			finally
			{
				stream.Position = position;
			}
		}
		else
		{
			await copy(stream, destination);
		}

		return destination.ToArray();
	}
}
