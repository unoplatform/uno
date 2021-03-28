using System;
using Windows.Foundation;

namespace Windows.Web.Http
{
	/// <summary>
	/// Provides a set of properties and methods to manage an HTTP cookie.
	/// </summary>
	public partial class HttpCookie : IStringable
	{
		/// <summary>
		/// Initializes a new instance of the HttpCookie class with a specified name, domain, and path.
		/// </summary>
		/// <param name="name">The name for the HttpCookie.</param>
		/// <param name="domain">The domain for which the HttpCookie is valid.</param>
		/// <param name="path">The URIs to which the HttpCookie applies.</param>
		/// <remarks>
		/// For parameter value limitations <see cref="https://docs.microsoft.com/en-us/uwp/api/windows.web.http.httpcookie.-ctor?view=winrt-19041#remarks">UWP docs.</see>
		/// </remarks>
		public HttpCookie(string name, string domain, string path)
		{
			Name = name;
			Domain = domain;
			Path = path;
		}

		/// <summary>
		/// Get the domain for which the HttpCookie is valid.
		/// </summary>
		public string Domain { get; }

		/// <summary>
		/// Get the token that represents the HttpCookie name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Get the URI path component to which the HttpCookie applies.
		/// </summary>
		public string Path { get; }

		/// <summary>
		/// Get or set the value for the HttpCookie.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Get or set the security level for the HttpCookie.
		/// </summary>
		public bool Secure { get; set; }

		/// <summary>
		/// Get or set a value that controls whether a script or other active content can access this HttpCookie.
		/// </summary>
		public bool HttpOnly { get; set; }

		/// <summary>
		/// Get or set the expiration date and time for the HttpCookie.
		/// </summary>
		public DateTimeOffset? Expires { get; set; }

		/// <summary>
		/// Rreturns a string that matches a Set-Cookie HTTP header suitable for including on a request message.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			throw new NotImplementedException();
		}
	}
}
