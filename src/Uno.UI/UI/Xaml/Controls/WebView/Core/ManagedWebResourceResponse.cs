#nullable enable

using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// A platform-neutral web resource response, created by <see cref="CoreWebView2Environment.CreateWebResourceResponse"/>.
/// </summary>
/// <remarks>
/// It holds data only. A platform that serves custom responses translates it into its native response when it is
/// assigned to a request.
/// </remarks>
internal sealed class ManagedWebResourceResponse : INativeWebResourceResponse
{
	public ManagedWebResourceResponse(IRandomAccessStream? content, int statusCode, string? reasonPhrase, ManagedHttpResponseHeaders headers)
	{
		// WebView2 accepts a response without a body.
		Content = content!;
		StatusCode = statusCode;
		ReasonPhrase = reasonPhrase ?? string.Empty;
		Headers = headers;
	}

	public IRandomAccessStream Content { get; set; }

	public INativeHttpResponseHeaders Headers { get; }

	public int StatusCode { get; set; }

	public string ReasonPhrase { get; set; }
}
