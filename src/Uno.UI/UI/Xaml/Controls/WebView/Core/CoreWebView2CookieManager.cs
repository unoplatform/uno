#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Manages cookies for the WebView2 control.
/// </summary>
public partial class CoreWebView2CookieManager
{
	private readonly INativeWebView _nativeWebView;

	internal CoreWebView2CookieManager(INativeWebView nativeWebView)
	{
		_nativeWebView = nativeWebView ?? throw new ArgumentNullException(nameof(nativeWebView));
	}

	/// <summary>
	/// Gets a list of cookies matching the specific URI.
	/// </summary>
	/// <param name="uri">The URI of the cookies to get.</param>
	/// <returns>A list of cookies for the provided URI.</returns>
	public IAsyncOperation<IReadOnlyList<CoreWebView2Cookie>> GetCookiesAsync(string uri)
	{
		return AsyncOperation.FromTask(async ct =>
		{
			if (_nativeWebView is INativeWebViewCookieManager cookieManager)
			{
				return await cookieManager.GetCookiesAsync(uri);
			}

			return Array.Empty<CoreWebView2Cookie>();
		});
	}

	/// <summary>
	/// Creates a cookie object with a specified name, value, domain, and path.
	/// </summary>
	/// <param name="name">The name of the cookie.</param>
	/// <param name="value">The value of the cookie.</param>
	/// <param name="Domain">The domain for which the cookie applies.</param>
	/// <param name="Path">The path for which the cookie applies.</param>
	/// <returns>A new CoreWebView2Cookie object.</returns>
	public CoreWebView2Cookie CreateCookie(string name, string value, string Domain, string Path)
	{
		return new CoreWebView2Cookie(name, value, Domain, Path);
	}

	/// <summary>
	/// Creates a copy of a cookie.
	/// </summary>
	/// <param name="cookieParam">The cookie to copy.</param>
	/// <returns>A copy of the cookie.</returns>
	public CoreWebView2Cookie CopyCookie(CoreWebView2Cookie cookieParam)
	{
		if (cookieParam == null)
		{
			throw new ArgumentNullException(nameof(cookieParam));
		}

		var newCookie = new CoreWebView2Cookie(
			cookieParam.Name,
			cookieParam.Value,
			cookieParam.Domain,
			cookieParam.Path)
		{
			IsHttpOnly = cookieParam.IsHttpOnly,
			IsSecure = cookieParam.IsSecure,
			SameSite = cookieParam.SameSite
		};

		if (!cookieParam.IsSession)
		{
			newCookie.Expires = cookieParam.Expires;
		}

		return newCookie;
	}

	/// <summary>
	/// Adds or updates a cookie with the given cookie data. This will overwrite cookies with matching name, domain, and path.
	/// </summary>
	/// <param name="cookie">The cookie to add or update.</param>
	public void AddOrUpdateCookie(CoreWebView2Cookie cookie)
	{
		if (cookie == null)
		{
			throw new ArgumentNullException(nameof(cookie));
		}

		if (_nativeWebView is INativeWebViewCookieManager cookieManager)
		{
			cookieManager.AddOrUpdateCookie(cookie);
		}
	}

	/// <summary>
	/// Deletes a cookie.
	/// </summary>
	/// <param name="cookie">The cookie to delete.</param>
	public void DeleteCookie(CoreWebView2Cookie cookie)
	{
		if (cookie == null)
		{
			throw new ArgumentNullException(nameof(cookie));
		}

		if (_nativeWebView is INativeWebViewCookieManager cookieManager)
		{
			cookieManager.DeleteCookie(cookie);
		}
	}

	/// <summary>
	/// Deletes cookies with matching name and uri.
	/// </summary>
	/// <param name="name">The name of the cookies to delete.</param>
	/// <param name="uri">The URI of the cookies to delete.</param>
	public void DeleteCookies(string name, string uri)
	{
		if (_nativeWebView is INativeWebViewCookieManager cookieManager)
		{
			cookieManager.DeleteCookies(name, uri);
		}
	}

	/// <summary>
	/// Deletes cookies with matching name, domain, and path.
	/// </summary>
	/// <param name="name">The name of the cookies to delete.</param>
	/// <param name="Domain">The domain of the cookies to delete.</param>
	/// <param name="Path">The path of the cookies to delete.</param>
	public void DeleteCookiesWithDomainAndPath(string name, string Domain, string Path)
	{
		if (_nativeWebView is INativeWebViewCookieManager cookieManager)
		{
			cookieManager.DeleteCookiesWithDomainAndPath(name, Domain, Path);
		}
	}

	/// <summary>
	/// Deletes all cookies.
	/// </summary>
	public void DeleteAllCookies()
	{
		if (_nativeWebView is INativeWebViewCookieManager cookieManager)
		{
			cookieManager.DeleteAllCookies();
		}
	}
}
