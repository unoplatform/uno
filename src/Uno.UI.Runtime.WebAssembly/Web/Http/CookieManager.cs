#nullable enable

using System;
using System.Globalization;
using System.Linq;
using Uno.Foundation;
using Windows.Web.Http;

namespace Uno.Web.Http
{
	/// <summary>
	/// Provides read-write access to browser Cookies in WebAssembly.
	/// </summary>
	public class CookieManager
	{
		private const char ValueSeparator = '=';
		private const char AttributeSeparator = ';';

		private static readonly Lazy<CookieManager> _cookieManager = new Lazy<CookieManager>(() => new CookieManager());

		private CookieManager()
		{
		}

		/// <summary>
		/// Retrieves the default instance of CookieManager.
		/// </summary>
		/// <returns>Cookie manager instance.</returns>
		public static CookieManager GetDefault() => _cookieManager.Value;

		/// <summary>
		/// Sets a cookie given attributes.
		/// </summary>
		/// <param name="request">Set cookie request</param>
		public void SetCookie(SetCookieRequest request)
		{
			var httpCookie = new HttpCookie(request.Cookie.Name, request.Domain ?? string.Empty, request.Path ?? string.Empty)
			{
				Secure = request.Secure,
				Expires = request.Expires,
				Value = request.Cookie.Value,
			};
			var serializedCookie = httpCookie.ToString();

			if (request.MaxAge != null)
			{
				serializedCookie += $"; max-age={request.MaxAge.Value.ToString(CultureInfo.InvariantCulture)}";
			}
			if (request.SameSite != null)
			{
				serializedCookie += $"; samesite={request.SameSite.Value.ToString("g").ToLowerInvariant()}";
			}

			var escapedCookie = WebAssemblyRuntime.EscapeJs(serializedCookie);
			var jsInvoke = $"document.cookie = '{escapedCookie}'";
			WebAssemblyRuntime.InvokeJS(jsInvoke);
		}

		/// <summary>
		/// Deletes a cookies by name.
		/// </summary>
		/// <param name="name">Name of the cookie.</param>
		/// <param name="domain">Domain of the cookie (optional).</param>
		/// <param name="path">Path of the cookie (optional).</param>
		public void DeleteCookie(string name, string? domain = null, string? path = null)
		{
			var setCookieRequest = new SetCookieRequest(new Cookie(name, string.Empty))
			{
				Expires = DateTimeOffset.MinValue,
				Path = path,
				Domain = domain
			};

			SetCookie(setCookieRequest);
		}

		/// <summary>
		/// Retrieves a cookie by name.
		/// </summary>
		/// <param name="name">Cookie name.</param>
		/// <returns>Cookie or null if not found.</returns>
		public Cookie? FindCookie(string name) => GetCookies().FirstOrDefault(c => c.Name == name);

		/// <summary>
		/// Gets array of currently active cookies.
		/// </summary>
		/// <returns>Active cookies.</returns>
		public Cookie[] GetCookies()
		{
			Cookie? ParseCookie(string cookieString)
			{
				cookieString = cookieString.Trim();
				var valueSeparatorIndex = cookieString.IndexOf(ValueSeparator);
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

			var cookieStrings = cookies.Split(new char[] { AttributeSeparator }, StringSplitOptions.RemoveEmptyEntries);
			return cookieStrings
				.Select(part => ParseCookie(part))
				.Where(cookie => cookie != null)
				.OfType<Cookie>()
				.ToArray();
		}
	}
}
