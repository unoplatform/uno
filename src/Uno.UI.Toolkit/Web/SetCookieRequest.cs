#if __WASM__
#nullable enable


using System;

namespace Uno.UI.Toolkit.Web
{
	public class SetCookieRequest
    {
		public SetCookieRequest(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public string Name { get; }

		public string Value { get; }

		public string? Path { get; set; }

		public string? Domain { get; set; }

		public int? MaxAge { get; set; }

		public DateTimeOffset? Expires { get; set; }

		public bool Secure { get; set; }

		public CookieSameSite? SameSite { get; set; }
	}
}
#endif
