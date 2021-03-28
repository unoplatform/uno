#if __WASM__
#nullable enable

using System;
using System.Linq;
using System.Text;
using Uno.Foundation;

namespace Uno.UI.Toolkit.Web
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
			var builder = new StringBuilder();
			builder.Append(Uri.EscapeDataString(cookie.Name));
			builder.Append(ValueSeparator);
			builder.Append(Uri.EscapeDataString(cookie.Value));

			static void AppendAttribute(StringBuilder builder, string name, object? value)
			{
				if (value == null)
				{
					return;
				}
				builder.Append(AttributeSeparator);
				builder.Append(name);				
				builder.Append(ValueSeparator);
				builder.Append(value);
			}

			AppendAttribute(builder, "path", cookie.Path);
			AppendAttribute(builder, "domain", cookie.Domain);
			AppendAttribute(builder, "max-age", cookie.MaxAge?.ToString());
			AppendAttribute(builder, "expires", cookie.Expires?.ToString("r"));
			AppendAttribute(builder, "samesite", cookie.SameSite?.ToString("g")?.ToLowerInvariant());
			if (cookie.Secure)
			{
				builder.Append(AttributeSeparator);
				builder.Append("secure");
			}

			var escapedCookie = WebAssemblyRuntime.EscapeJs(builder.ToString());
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
			Cookie ParseCookie(string cookieString)
			{
				var cookieParts = cookieString.Split("=");
				var name = Uri.UnescapeDataString(cookieParts[0]);
				var value = Uri.UnescapeDataString(cookieParts[1]);
				return new Cookie(name, value);
			}

			var cookies = WebAssemblyRuntime.InvokeJS("document.cookie");
			if (string.IsNullOrWhiteSpace(cookies))
			{
				return Array.Empty<Cookie>();
			}
		
			var cookieStrings = cookies.Split(";", StringSplitOptions.RemoveEmptyEntries);
			return cookieStrings.Select(part => ParseCookie(part)).ToArray();
		}
	}
}
#endif
