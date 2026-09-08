#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Creates, adds or updates, gets, or deletes cookies for the current user profile.
/// </summary>
public partial class CoreWebView2CookieManager
{
	private readonly CoreWebView2 _owner;

	internal CoreWebView2CookieManager(CoreWebView2 owner)
	{
		_owner = owner;
	}

	private ISupportsCookieManager Native =>
		(_owner.NativeWebViewForCookies as ISupportsCookieManager)
		?? throw new NotSupportedException(
			"CoreWebView2.CookieManager is not supported on this platform.");

	/// <summary>
	/// Creates a cookie object with a specified name, value, domain, and path.
	/// </summary>
	/// <param name="name">The cookie name.</param>
	/// <param name="value">The cookie value.</param>
	/// <param name="Domain">The cookie domain.</param>
	/// <param name="Path">The cookie path.</param>
	/// <returns>The newly created cookie.</returns>
	public CoreWebView2Cookie CreateCookie(string name, string value, string Domain, string Path)
	{
		ValidateCookieIdentity(name, Domain, Path);
		return new CoreWebView2Cookie(name, value ?? throw new ArgumentNullException(nameof(value)), Domain, Path);
	}

	private static void ValidateCookieIdentity(string name, string domain, string path)
	{
		if (string.IsNullOrEmpty(name) || name.IndexOfAny([';', '\r', '\n']) >= 0)
		{
			throw new ArgumentException("Cookie names cannot be empty or contain separators.", nameof(name));
		}
		if (string.IsNullOrEmpty(domain) || domain.IndexOfAny([';', '\r', '\n']) >= 0)
		{
			throw new ArgumentException("A valid cookie domain is required.", nameof(domain));
		}
		if (string.IsNullOrEmpty(path) || !path.StartsWith('/') || path.IndexOfAny([';', '\r', '\n']) >= 0)
		{
			throw new ArgumentException("Cookie paths must start with '/'.", nameof(path));
		}
	}

	/// <summary>
	/// Creates a cookie whose parameters match those of the specified cookie.
	/// </summary>
	/// <param name="cookieParam">The cookie to copy.</param>
	/// <returns>A copy of the specified cookie.</returns>
	public CoreWebView2Cookie CopyCookie(CoreWebView2Cookie cookieParam)
	{
		if (cookieParam is null)
		{
			throw new ArgumentNullException(nameof(cookieParam));
		}

		return new CoreWebView2Cookie(cookieParam.Name, cookieParam.Value, cookieParam.Domain, cookieParam.Path)
		{
			Expires = cookieParam.Expires,
			IsHttpOnly = cookieParam.IsHttpOnly,
			IsSecure = cookieParam.IsSecure,
			SameSite = cookieParam.SameSite,
		};
	}

	/// <summary>
	/// Gets a list of cookies matching the specified URI.
	/// </summary>
	/// <param name="uri">
	/// The URI whose cookies are returned. If the value is null or empty, all cookies in the profile are returned.
	/// </param>
	/// <returns>An asynchronous operation that returns the matching cookies.</returns>
	public IAsyncOperation<IReadOnlyList<CoreWebView2Cookie>> GetCookiesAsync(string uri)
	{
		uri ??= string.Empty;
		if (uri.Length > 0 && !Uri.TryCreate(uri, UriKind.Absolute, out _))
		{
			throw new ArgumentException("The cookie URI must be absolute or empty.", nameof(uri));
		}

		return AsyncOperation.FromTask(ct => Native.GetCookiesAsync(uri, ct));
	}

	/// <summary>
	/// Adds or updates a cookie and may overwrite an existing cookie with the same name, domain, and path.
	/// </summary>
	/// <param name="cookie">The cookie to add or update.</param>
	public void AddOrUpdateCookie(CoreWebView2Cookie cookie)
	{
		if (cookie is null)
		{
			throw new ArgumentNullException(nameof(cookie));
		}
		ValidateCookieIdentity(cookie.Name, cookie.Domain, cookie.Path);
		if (cookie.Value is null || cookie.Value.IndexOfAny(['\r', '\n']) >= 0)
		{
			throw new ArgumentException("Cookie values cannot be null or contain line breaks.", nameof(cookie));
		}
		if (cookie.SameSite == CoreWebView2CookieSameSiteKind.None && !cookie.IsSecure)
		{
			throw new ArgumentException("SameSite=None cookies must also be secure.", nameof(cookie));
		}

		Native.AddOrUpdateCookie(cookie);
	}

	/// <summary>
	/// Deletes a cookie whose name, domain, and path match the specified cookie.
	/// </summary>
	/// <param name="cookie">The cookie to delete.</param>
	public void DeleteCookie(CoreWebView2Cookie cookie)
	{
		if (cookie is null)
		{
			throw new ArgumentNullException(nameof(cookie));
		}

		Native.DeleteCookie(cookie);
	}

	/// <summary>
	/// Deletes cookies with a matching name and URI.
	/// </summary>
	/// <param name="name">The required cookie name.</param>
	/// <param name="uri">The URI used to match the cookie domain and path.</param>
	public void DeleteCookies(string name, string? uri) => Native.DeleteCookies(name, uri);

	/// <summary>
	/// Deletes cookies with a matching name, domain, and path.
	/// </summary>
	/// <param name="name">The required cookie name.</param>
	/// <param name="Domain">The exact cookie domain.</param>
	/// <param name="Path">The exact cookie path.</param>
	public void DeleteCookiesWithDomainAndPath(string name, string Domain, string Path)
		=> Native.DeleteCookiesWithDomainAndPath(name, Domain, Path);

	/// <summary>
	/// Deletes all cookies in the current user profile.
	/// </summary>
	public void DeleteAllCookies() => Native.DeleteAllCookies();
}
