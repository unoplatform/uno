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

	public CoreWebView2Cookie CreateCookie(string name, string value, string domain, string path)
		=> new CoreWebView2Cookie(name, value, domain, path);

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
		=> AsyncOperation.FromTask(ct => Native.GetCookiesAsync(uri, ct));

	public void AddOrUpdateCookie(CoreWebView2Cookie cookie)
	{
		if (cookie is null)
		{
			throw new ArgumentNullException(nameof(cookie));
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

	public void DeleteCookiesWithDomainAndPath(string name, string domain, string path)
		=> Native.DeleteCookiesWithDomainAndPath(name, domain, path);

	public void DeleteAllCookies() => Native.DeleteAllCookies();
}
