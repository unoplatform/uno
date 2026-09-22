#nullable enable

using System;
using System.Globalization;
using System.Text;

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

	internal string ToSetCookieHeader()
	{
		var header = CreateCookieHeader(Value);
		if (IsSecure)
		{
			header.Append("; Secure");
		}
		if (IsHttpOnly)
		{
			header.Append("; HttpOnly");
		}
		header.Append("; SameSite=").Append(SameSite switch
		{
			CoreWebView2CookieSameSiteKind.None => "None",
			CoreWebView2CookieSameSiteKind.Lax => "Lax",
			CoreWebView2CookieSameSiteKind.Strict => "Strict",
			_ => throw new ArgumentOutOfRangeException(nameof(SameSite)),
		});
		if (!IsSession)
		{
			header.Append("; Expires=").Append(DateTimeOffset.UnixEpoch.AddSeconds(Expires).ToString("r", CultureInfo.InvariantCulture));
		}
		return header.ToString();
	}

	internal string ToSetCookieDeletionHeader()
	{
		var header = CreateCookieHeader(string.Empty).Append("; Max-Age=0");
		if (IsSecure)
		{
			header.Append("; Secure");
		}
		return header.ToString();
	}

	private StringBuilder CreateCookieHeader(string value)
	{
		var header = new StringBuilder().Append(Name).Append('=').Append(value);
		// A Domain attribute would turn a host-only cookie into a domain cookie.
		if (Domain.StartsWith('.'))
		{
			header.Append("; Domain=").Append(Domain);
		}
		return header.Append("; Path=").Append(Path);
	}

	internal static bool MatchesUri(string domain, string path, bool isSecure, Uri uri)
	{
		var matchesDomain = domain.StartsWith('.')
			? string.Equals(uri.Host, domain[1..], StringComparison.OrdinalIgnoreCase)
				|| uri.Host.EndsWith(domain, StringComparison.OrdinalIgnoreCase)
			: string.Equals(uri.Host, domain, StringComparison.OrdinalIgnoreCase);
		return matchesDomain
			&& (!isSecure || uri.Scheme == Uri.UriSchemeHttps)
			&& uri.AbsolutePath.StartsWith(path, StringComparison.Ordinal)
			&& (path.EndsWith('/') || uri.AbsolutePath.Length == path.Length || uri.AbsolutePath[path.Length] == '/');
	}
}