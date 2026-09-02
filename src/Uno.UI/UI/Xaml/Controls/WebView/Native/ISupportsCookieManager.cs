#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebView providers that expose a first-class cookie store.
/// </summary>
internal interface ISupportsCookieManager
{
	Task<IReadOnlyList<CoreWebView2Cookie>> GetCookiesAsync(string uri, CancellationToken ct);

	void AddOrUpdateCookie(CoreWebView2Cookie cookie);

	void DeleteCookie(CoreWebView2Cookie cookie);

	void DeleteCookies(string name, string? uri);

	void DeleteCookiesWithDomainAndPath(string name, string domain, string path);

	void DeleteAllCookies();
}
