#nullable enable

using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// CoreWebView2Cookie implementation - provides backing fields and implementation for cookie properties
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

#if __ANDROID__ || __IOS__ || __TVOS__ || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__
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

	/// <summary>
	/// Gets the name of the cookie.
	/// </summary>
	public string Name => _name ?? string.Empty;

	/// <summary>
	/// Gets or sets the value of the cookie.
	/// </summary>
	public string Value
	{
		get => _value ?? string.Empty;
		set => _value = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Gets the domain for the cookie.
	/// </summary>
	public string Domain => _domain ?? string.Empty;

	/// <summary>
	/// Gets the path for the cookie.
	/// </summary>
	public string Path => _path ?? string.Empty;

	/// <summary>
	/// Gets or sets the expiration date and time for the cookie as the number of seconds since the UNIX epoch.
	/// </summary>
	public double Expires
	{
		get => _expires;
		set
		{
			_expires = value;
			_isSession = false; // Setting an expiration makes it a persistent cookie
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the cookie is HttpOnly.
	/// </summary>
	public bool IsHttpOnly
	{
		get => _isHttpOnly;
		set => _isHttpOnly = value;
	}

	/// <summary>
	/// Gets or sets the SameSite attribute of the cookie.
	/// </summary>
	public CoreWebView2CookieSameSiteKind SameSite
	{
		get => _sameSite;
		set => _sameSite = value;
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the cookie is secure.
	/// </summary>
	public bool IsSecure
	{
		get => _isSecure;
		set => _isSecure = value;
	}

	/// <summary>
	/// Gets a value that indicates whether the cookie is a session cookie.
	/// </summary>
	public bool IsSession => _isSession;
#endif
}
