using System;
using Windows.Foundation;
using Windows.Web.Http.Headers;

namespace Windows.Web.Http;

public sealed partial class HttpRequestMessage : IDisposable, IStringable
{
	public HttpRequestMessage(HttpMethod method, Uri uri)
	{
		Method = method;
		RequestUri = uri;
		Headers = new HttpRequestHeaderCollection(this);
	}

	public HttpRequestMessage()
	{
		Method = new HttpMethod("GET");
	}

	public Uri? RequestUri { get; set; }
	public HttpRequestHeaderCollection Headers { get; } = null!; // TODO: This looks wrong.
}
