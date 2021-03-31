#if __WASM__
#nullable enable

using System;

namespace Uno.Web.Http
{
	/// <summary>
	/// Represents a request to set cookie.
	/// </summary>
	public class SetCookieRequest
	{
		/// <summary>
		/// Creates an instance of a request to set cookie.
		/// </summary>
		/// <param name="cookie">Cookie to set.</param>
		public SetCookieRequest(Cookie cookie)
		{
			Cookie = cookie;
		}

		public Cookie Cookie { get; }

		public string? Path { get; set; }

		public string? Domain { get; set; }

		public int? MaxAge { get; set; }

		public DateTimeOffset? Expires { get; set; }

		public bool Secure { get; set; }

		public CookieSameSite? SameSite { get; set; }
	}
}
#endif
