#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.Web.WebView2.Core;

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

	public IAsyncOperation<IReadOnlyList<CoreWebView2Cookie>> GetCookiesAsync(string uri)
	{
		if (!string.IsNullOrEmpty(uri) && !Uri.TryCreate(uri, UriKind.Absolute, out _))
		{
			throw new ArgumentException("The cookie URI must be absolute or empty.", nameof(uri));
		}

		return AsyncOperation.FromTask(ct => Native.GetCookiesAsync(uri, ct));
	}

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

	public void DeleteCookie(CoreWebView2Cookie cookie)
	{
		if (cookie is null)
		{
			throw new ArgumentNullException(nameof(cookie));
		}

		Native.DeleteCookie(cookie);
	}

	public void DeleteCookies(string name, string? uri) => Native.DeleteCookies(name, uri);

	public void DeleteCookiesWithDomainAndPath(string name, string Domain, string Path)
		=> Native.DeleteCookiesWithDomainAndPath(name, Domain, Path);

	public void DeleteAllCookies() => Native.DeleteAllCookies();
}
