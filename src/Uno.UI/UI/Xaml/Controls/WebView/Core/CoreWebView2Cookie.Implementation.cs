#nullable enable

using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// CoreWebView2Cookie implementation
/// </summary>
public partial class CoreWebView2Cookie
{
	private string? _name;
	private string? _value;
	private string? _domain;
	private string? _path;
	private double _expires;
	private bool _isHttpOnly;
	private CoreWebView2CookieSameSiteKind _sameSite;
	private bool _isSecure;
	private bool _isSession = true;

	/// <summary>
	/// Internal constructor for creating CoreWebView2Cookie instances.
	/// </summary>
	internal CoreWebView2Cookie(string name, string value, string domain, string path)
	{
		_name = name ?? throw new ArgumentNullException(nameof(name));
		_value = value ?? throw new ArgumentNullException(nameof(value));
		_domain = domain ?? throw new ArgumentNullException(nameof(domain));
		_path = path ?? throw new ArgumentNullException(nameof(path));
		_isSession = true;
		_sameSite = CoreWebView2CookieSameSiteKind.None;
	}
}
