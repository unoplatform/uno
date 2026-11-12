#nullable enable

using System.Net.Http;
using Microsoft.Identity.Client;

namespace Uno.UI.MSAL;

// MSAL's default factory sets HttpClientHandler.UseDefaultCredentials, which is not supported in the browser.
internal sealed class WasmHttpFactory : IMsalHttpClientFactory
{
	internal static readonly WasmHttpFactory Instance = new();

	private static readonly HttpClient _client = new();

	public HttpClient GetHttpClient() => _client;
}
