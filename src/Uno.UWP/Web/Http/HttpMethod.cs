using Windows.Foundation;

namespace Windows.Web.Http;

public partial class HttpMethod : IStringable
{
	public HttpMethod(string method)
	{
		Method = method;
	}

	public string Method { get; }

	public static HttpMethod Delete { get; } = new("DELETE");

	public static HttpMethod Get { get; } = new("GET");

	public static HttpMethod Head { get; } = new("HEAD");

	public static HttpMethod Options { get; } = new("OPTIONS");

	public static HttpMethod Patch { get; } = new("PATCH");

	public static HttpMethod Post { get; } = new("POST");

	public static HttpMethod Put { get; } = new("PUT");

	public override string ToString()
		=> Method;
}
