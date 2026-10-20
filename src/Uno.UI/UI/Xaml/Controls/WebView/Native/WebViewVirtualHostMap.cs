#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Translates <c>https://&lt;virtual host&gt;/</c> URLs to and from <see cref="NativeScheme"/> URLs, for native web
/// views that cannot intercept <c>https</c> requests (WebKit) and serve virtual hosts through a custom scheme instead.
/// </summary>
/// <remarks>
/// <para>
/// A host is virtual when it was registered through <c>CoreWebView2.SetVirtualHostNameToFolderMapping</c>, or, while
/// <see cref="IncludeNonRoutableHosts"/> is true, when it is an IPv4 literal in the non-routable <c>0.0.0.0/8</c> range.
/// Blazor's WebView serves its app from such a host (<c>https://0.0.0.1/</c>, or <c>https://0.0.0.0/</c> behind
/// an AppContext switch) and intercepts its requests without registering a folder mapping.
/// </para>
/// <para>
/// Only <c>https</c> is mapped: one public scheme keeps the translation reversible. Everything after the scheme
/// (host, non-default port, path, query, fragment) is kept as is.
/// </para>
/// </remarks>
internal sealed class WebViewVirtualHostMap
{
	/// <summary>
	/// The scheme native views use for virtual hosts.
	/// </summary>
	/// <remarks>Open for review; kept in this one constant so it can change in one place.</remarks>
	internal const string NativeScheme = "uno-app";

	private const string PublicScheme = "https";

	private readonly object _gate = new();
	private readonly Dictionary<string, string?> _hosts = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Whether IPv4 hosts in <c>0.0.0.0/8</c> are virtual without being registered. Defaults to true.
	/// </summary>
	public bool IncludeNonRoutableHosts { get; set; } = true;

	/// <summary>
	/// Registers <paramref name="hostName"/> as virtual, optionally backed by <paramref name="folderPath"/>.
	/// </summary>
	public void SetHost(string hostName, string? folderPath = null)
	{
		ArgumentNullException.ThrowIfNull(hostName);

		lock (_gate)
		{
			_hosts[hostName] = folderPath;
		}
	}

	public bool RemoveHost(string hostName)
	{
		ArgumentNullException.ThrowIfNull(hostName);

		lock (_gate)
		{
			return _hosts.Remove(hostName);
		}
	}

	/// <summary>
	/// Gets the folder registered for <paramref name="hostName"/>; false for unregistered (including <c>0.0.0.0/8</c>) hosts.
	/// </summary>
	public bool TryGetFolder(string hostName, out string? folderPath)
	{
		lock (_gate)
		{
			return _hosts.TryGetValue(hostName, out folderPath);
		}
	}

	public bool IsVirtualHost(string? host)
	{
		if (string.IsNullOrEmpty(host))
		{
			return false;
		}

		lock (_gate)
		{
			if (_hosts.ContainsKey(host))
			{
				return true;
			}
		}

		return IncludeNonRoutableHosts && IsNonRoutableIPv4(host);
	}

	/// <summary>
	/// Whether <paramref name="uri"/> is a public (<c>https</c>) URL of a virtual host.
	/// </summary>
	public bool IsVirtual(Uri? uri)
		=> uri is { IsAbsoluteUri: true }
		&& string.Equals(uri.Scheme, PublicScheme, StringComparison.OrdinalIgnoreCase)
		&& IsVirtualHost(uri.Host);

	/// <summary>
	/// <c>https://&lt;virtual host&gt;/...</c> to <c>uno-app://&lt;virtual host&gt;/...</c>; any other URL is returned unchanged.
	/// </summary>
	public Uri ToNative(Uri uri)
		=> IsVirtual(uri) ? new Uri(NativeScheme + uri.AbsoluteUri.Substring(PublicScheme.Length)) : uri;

	/// <summary>
	/// The inverse of <see cref="ToNative(Uri)"/>. A <see cref="NativeScheme"/> URL whose host is not virtual is returned unchanged.
	/// </summary>
	public Uri ToPublic(Uri uri)
		=> IsNative(uri) ? new Uri(PublicScheme + uri.AbsoluteUri.Substring(NativeScheme.Length)) : uri;

	/// <summary>
	/// String form of <see cref="ToNative(Uri)"/>. Untranslated input, including non-URLs, comes back as the same string.
	/// </summary>
	public string ToNative(string uri)
		=> Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && IsVirtual(parsed)
			? SwapScheme(uri, PublicScheme, NativeScheme) ?? ToNative(parsed).AbsoluteUri
			: uri;

	/// <summary>
	/// String form of <see cref="ToPublic(Uri)"/>. Untranslated input, including non-URLs, comes back as the same string.
	/// </summary>
	public string ToPublic(string uri)
		=> Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && IsNative(parsed)
			? SwapScheme(uri, NativeScheme, PublicScheme) ?? ToPublic(parsed).AbsoluteUri
			: uri;

	/// <summary>
	/// Translates a <c>WebResourceRequested</c> filter such as <c>https://0.0.0.1/*</c>. Filters whose host is not a
	/// literal virtual host (wildcards included) are returned unchanged.
	/// </summary>
	public string ToNativeFilter(string pattern)
	{
		ArgumentNullException.ThrowIfNull(pattern);

		const string prefix = PublicScheme + "://";
		if (!pattern.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return pattern;
		}

		var authorityEnd = pattern.IndexOf('/', prefix.Length);
		var authority = authorityEnd < 0 ? pattern.Substring(prefix.Length) : pattern.Substring(prefix.Length, authorityEnd - prefix.Length);
		if (authority.AsSpan().IndexOfAny("*?[@") >= 0)
		{
			return pattern;
		}

		var portSeparator = authority.IndexOf(':');
		var host = portSeparator < 0 ? authority : authority.Substring(0, portSeparator);

		return IsVirtualHost(host) ? NativeScheme + pattern.Substring(PublicScheme.Length) : pattern;
	}

	/// <summary>
	/// Whether <paramref name="host"/> is a dotted-quad IPv4 literal in <c>0.0.0.0/8</c>.
	/// </summary>
	internal static bool IsNonRoutableIPv4(string host)
	{
		var parts = host.Split('.');
		if (parts.Length != 4)
		{
			return false;
		}

		foreach (var part in parts)
		{
			if (part.Length is 0 or > 3 || !uint.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value > 255)
			{
				return false;
			}
		}

		return uint.Parse(parts[0], CultureInfo.InvariantCulture) == 0;
	}

	private bool IsNative(Uri? uri)
		=> uri is { IsAbsoluteUri: true }
		&& string.Equals(uri.Scheme, NativeScheme, StringComparison.OrdinalIgnoreCase)
		&& IsVirtualHost(uri.Host);

	// Swaps the scheme in place so the rest of the string keeps its original form; null when it does not start with it.
	private static string? SwapScheme(string uri, string from, string to)
		=> uri.Length > from.Length && uri[from.Length] == ':' && uri.StartsWith(from, StringComparison.OrdinalIgnoreCase)
			? to + uri.Substring(from.Length)
			: null;
}
