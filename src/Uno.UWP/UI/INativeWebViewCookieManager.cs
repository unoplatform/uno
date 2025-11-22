#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Interface for native WebView implementations that support cookie management.
/// </summary>
internal interface INativeWebViewCookieManager
{
	/// <summary>
	/// Gets a list of cookies matching the specific URI.
	/// </summary>
	/// <param name="uri">The URI of the cookies to get.</param>
	/// <returns>A list of cookies for the provided URI.</returns>
	Task<IReadOnlyList<CoreWebView2Cookie>> GetCookiesAsync(string uri);

	/// <summary>
	/// Adds or updates a cookie with the given cookie data.
	/// </summary>
	/// <param name="cookie">The cookie to add or update.</param>
	void AddOrUpdateCookie(CoreWebView2Cookie cookie);

	/// <summary>
	/// Deletes a cookie.
	/// </summary>
	/// <param name="cookie">The cookie to delete.</param>
	void DeleteCookie(CoreWebView2Cookie cookie);

	/// <summary>
	/// Deletes cookies with matching name and uri.
	/// </summary>
	/// <param name="name">The name of the cookies to delete.</param>
	/// <param name="uri">The URI of the cookies to delete.</param>
	void DeleteCookies(string name, string uri);

	/// <summary>
	/// Deletes cookies with matching name, domain, and path.
	/// </summary>
	/// <param name="name">The name of the cookies to delete.</param>
	/// <param name="domain">The domain of the cookies to delete.</param>
	/// <param name="path">The path of the cookies to delete.</param>
	void DeleteCookiesWithDomainAndPath(string name, string domain, string path);

	/// <summary>
	/// Deletes all cookies.
	/// </summary>
	void DeleteAllCookies();
}
