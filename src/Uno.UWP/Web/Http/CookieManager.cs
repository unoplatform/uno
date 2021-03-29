#if __WASM__
#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Uno.Foundation;
using Windows.Web.Http;

namespace Uno.Web.Http
{
	public class CookieManager
	{
		private const char ValueSeparator = '=';
		private const char AttributeSeparator = ';';

		private static readonly Lazy<CookieManager> _cookieManager = new Lazy<CookieManager>(() => new CookieManager());

		private CookieManager()
		{
		}

		public static CookieManager GetDefault() => _cookieManager.Value;

		public void SetCookie(SetCookieRequest cookie)
		{
			var httpCookie = new HttpCookie(cookie.Name, cookie.Domain ?? string.Empty, cookie.Path ?? string.Empty)
			{
				Secure = cookie.Secure,
				Expires = cookie.Expires,
				Value = cookie.Value,
			};
			var serializedCookie = httpCookie.ToString();

			if (cookie.MaxAge != null)
			{
				serializedCookie += $"; max-age={cookie.MaxAge.Value.ToString(CultureInfo.InvariantCulture)}";
			}
			if (cookie.SameSite != null)
			{
				serializedCookie += $"; samesite={cookie.SameSite.Value.ToString("g").ToLowerInvariant()}";
			}

			var escapedCookie = WebAssemblyRuntime.EscapeJs(serializedCookie);
			var jsInvoke = $"document.cookie = '{escapedCookie}'";
			WebAssemblyRuntime.InvokeJS(jsInvoke);
		}

		public void DeleteCookie(string name, string? path = null)
		{
			var setCookieRequest = new SetCookieRequest(name, string.Empty)
			{
				Expires = DateTimeOffset.MinValue,
				Path = path
			};

			SetCookie(setCookieRequest);
		}

		public Cookie? GetCookie(string name) => GetCookies().FirstOrDefault(c => c.Name == name);

		public Cookie[] GetCookies()
		{
			Cookie? ParseCookie(string cookieString)
			{
				cookieString = cookieString.Trim();
				var valueSeparatorIndex = cookieString.IndexOf('=');
				if (valueSeparatorIndex == -1)
				{
					return null;					
				}

				var name = cookieString.Substring(0, valueSeparatorIndex);
				var valueStartIndex = valueSeparatorIndex + 1;
				var value = cookieString.Substring(valueStartIndex, cookieString.Length - valueStartIndex);
				return new Cookie(name, value);
			}

			var cookies = WebAssemblyRuntime.InvokeJS("document.cookie");
			if (string.IsNullOrWhiteSpace(cookies))
			{
				return Array.Empty<Cookie>();
			}
		
			var cookieStrings = cookies.Split(";", StringSplitOptions.RemoveEmptyEntries);
			return cookieStrings
				.Select(part => ParseCookie(part))
				.Where(cookie => cookie != null)
				.OfType<Cookie>()
				.ToArray();
		}
	}
}
#endif
