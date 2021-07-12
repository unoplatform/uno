#nullable enable
using System;
using System.Text;
using Windows.Foundation;

namespace Windows.Web.Http
{
	/// <summary>
	/// Provides a set of properties and methods to manage an HTTP cookie.
	/// </summary>
	public partial class HttpCookie : IStringable
	{
		private const char ValueSeparator = '=';
		private const string AttributeSeparator = "; ";

		/// <summary>
		/// Initializes a new instance of the HttpCookie class with a specified name, domain, and path.
		/// </summary>
		/// <param name="name">The name for the HttpCookie.</param>
		/// <param name="domain">The domain for which the HttpCookie is valid.</param>
		/// <param name="path">The URIs to which the HttpCookie applies.</param>
		/// <remarks>
		/// For parameter value limitations https://docs.microsoft.com/en-us/uwp/api/windows.web.http.httpcookie.-ctor?view=winrt-19041#remarks UWP docs.
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
		public string Domain { get; } = string.Empty;

		/// <summary>
		/// Get the token that represents the HttpCookie name.
		/// </summary>
		public string Name { get; } = string.Empty;

		/// <summary>
		/// Get the URI path component to which the HttpCookie applies.
		/// </summary>
		public string Path { get; } = string.Empty;

		/// <summary>
		/// Get or set the value for the HttpCookie.
		/// </summary>
		public string Value { get; set; } = string.Empty;

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
			static void AppendAttribute(StringBuilder builder, string name, string? value)
			{
				if (string.IsNullOrEmpty(value))
				{
					return;
				}

				builder.Append(AttributeSeparator);
				builder.Append(name);
				builder.Append(ValueSeparator);
				builder.Append(value);
			}

			var builder = new StringBuilder();
			builder.Append(Name);
			builder.Append(ValueSeparator);
			builder.Append(Value);
			AppendAttribute(builder, "Path", Path);
			AppendAttribute(builder, "Domain", Domain);
			if (Secure)
			{
				builder.Append(AttributeSeparator);
				builder.Append("Secure");
			}
			if (HttpOnly)
			{
				builder.Append(AttributeSeparator);
				builder.Append("HttpOnly");
			}
			AppendAttribute(builder, "Expires", Expires?.ToString("r"));
			return builder.ToString();
		}
	}
}
