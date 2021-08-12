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

		/// <summary>
		/// Gets the cookie that will be set.
		/// </summary>
		public Cookie Cookie { get; }

		/// <summary>
		/// Gets or sets the path to which the cookie is scoped.
		/// </summary>
		public string? Path { get; set; }

		/// <summary>
		/// Gets or sets the domain to which the cookie is scoped.
		/// </summary>
		public string? Domain { get; set; }

		/// <summary>
		/// Gets or sets the maximum age of the cookie (in seconds).
		/// </summary>
		public int? MaxAge { get; set; }

		/// <summary>
		/// Gets or sets the expiration date of the cookie.
		/// </summary>
		public DateTimeOffset? Expires { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the cookie is secure.
		/// </summary>
		public bool Secure { get; set; }

		/// <summary>
		/// Gets or sets the same-site setting for the cookie.
		/// </summary>
		public CookieSameSite? SameSite { get; set; }
	}
}
