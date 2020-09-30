namespace Uno.Web.Http
{
	/// <summary>
	/// Represents the valid options for the same-site
	/// attribute of HTTP cookies.
	/// </summary>
	public enum CookieSameSite
	{
		/// <summary>
		/// Cookies are not sent on normal cross-site subrequests (for example to load images or frames into a third party site),
		/// but are sent when a user is navigating to the origin site (i.e. when following a link).
		/// This is the default value when not specified.
		/// </summary>
		Lax,

		/// <summary>
		/// Cookies will only be sent in a first-party context and not be sent along with requests initiated by third party websites.
		/// </summary>
		Strict,

		/// <summary>
		/// Cookies will be sent in all contexts, i.e in responses to both first-party and cross-origin requests.If SameSite=None is set,
		/// the cookie Secure attribute must also be set (or the cookie will be blocked).
		/// </summary>
		None
	}
}
