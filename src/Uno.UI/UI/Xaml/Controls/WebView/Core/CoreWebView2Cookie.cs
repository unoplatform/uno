using System;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2Cookie
{
	private double _expires = -1d;

	internal CoreWebView2Cookie(string name, string value, string domain, string path)
	{
		Name = name;
		Value = value;
		Domain = domain;
		Path = path;
	}

	public string Domain { get; }

	public double Expires
	{
		get => _expires;
		set
		{
			if (double.IsNaN(value) || double.IsInfinity(value) || (value < 0d && value != -1d))
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Expires must be -1 for a session cookie or a non-negative Unix timestamp.");
			}

			_expires = value;
		}
	}

	public bool IsHttpOnly { get; set; }

	public bool IsSecure { get; set; }

	public bool IsSession => Expires == -1d;

	public string Name { get; }

	public string Path { get; }

	public CoreWebView2CookieSameSiteKind SameSite { get; set; } = CoreWebView2CookieSameSiteKind.Lax;

	public string Value { get; set; }
}