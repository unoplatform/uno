#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Shared helper for determining resource context from URLs.
/// </summary>
internal static class WebResourceContextHelper
{
	/// <summary>
	/// Determines the resource context of a request whose engine only reports its URL, headers and whether it loads
	/// the main frame (Android <c>shouldInterceptRequest</c>).
	/// </summary>
	/// <remarks>
	/// In order: a main-frame request is the top-level navigation, so <see cref="CoreWebView2WebResourceContext.Document"/>;
	/// then the <c>Sec-Fetch-Dest</c> header when the engine forwards it; then an <c>Accept</c> header that only
	/// navigations (<c>text/html</c>), stylesheets (<c>text/css</c>) or images (<c>image/</c>) send; then the URL extension.
	/// </remarks>
	public static CoreWebView2WebResourceContext DetermineResourceContext(string url, bool isForMainFrame, IReadOnlyDictionary<string, string>? headers)
	{
		if (isForMainFrame)
		{
			return CoreWebView2WebResourceContext.Document;
		}

		if (TryGetHeader(headers, "Sec-Fetch-Dest") is { } destination
			&& FromFetchDestination(destination) is { } fromDestination)
		{
			return fromDestination;
		}

		if (TryGetHeader(headers, "Accept") is { } accept)
		{
			if (accept.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
			{
				return CoreWebView2WebResourceContext.Document;
			}

			if (accept.StartsWith("text/css", StringComparison.OrdinalIgnoreCase))
			{
				return CoreWebView2WebResourceContext.Stylesheet;
			}

			if (accept.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			{
				return CoreWebView2WebResourceContext.Image;
			}
		}

		var fromUrl = DetermineResourceContext(url);

		// Without the main-frame flag or an Accept header, an extension-less URL is more likely an API call than a page.
		return fromUrl == CoreWebView2WebResourceContext.Document ? CoreWebView2WebResourceContext.Other : fromUrl;
	}

	private static CoreWebView2WebResourceContext? FromFetchDestination(string destination)
		=> destination.Trim().ToLowerInvariant() switch
		{
			"document" or "iframe" or "frame" or "embed" or "object" => CoreWebView2WebResourceContext.Document,
			"style" => CoreWebView2WebResourceContext.Stylesheet,
			"script" or "worker" or "sharedworker" or "serviceworker" => CoreWebView2WebResourceContext.Script,
			"image" => CoreWebView2WebResourceContext.Image,
			"font" => CoreWebView2WebResourceContext.Font,
			"audio" or "video" => CoreWebView2WebResourceContext.Media,
			"track" => CoreWebView2WebResourceContext.TextTrack,
			"manifest" => CoreWebView2WebResourceContext.Manifest,
			"report" => CoreWebView2WebResourceContext.CspViolationReport,
			// "empty" is fetch(), XMLHttpRequest, EventSource and WebSocket alike.
			"empty" => CoreWebView2WebResourceContext.Fetch,
			_ => null,
		};

	private static string? TryGetHeader(IReadOnlyDictionary<string, string>? headers, string name)
	{
		if (headers is null)
		{
			return null;
		}

		foreach (var header in headers)
		{
			if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(header.Value))
			{
				return header.Value;
			}
		}

		return null;
	}

	/// <summary>
	/// Determines the resource context based on URL file extension.
	/// </summary>
	public static CoreWebView2WebResourceContext DetermineResourceContext(string url)
	{
		var path = GetPathWithoutQuery(url);

		if (IsDocument(path))
		{
			return CoreWebView2WebResourceContext.Document;
		}
		if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
		{
			return CoreWebView2WebResourceContext.Stylesheet;
		}
		if (path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
		{
			return CoreWebView2WebResourceContext.Script;
		}
		if (IsImage(path))
		{
			return CoreWebView2WebResourceContext.Image;
		}
		if (IsFont(path))
		{
			return CoreWebView2WebResourceContext.Font;
		}
		if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
		{
			return CoreWebView2WebResourceContext.Fetch;
		}
		if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
		{
			return CoreWebView2WebResourceContext.XmlHttpRequest;
		}
		if (IsMedia(path))
		{
			return CoreWebView2WebResourceContext.Media;
		}

		return CoreWebView2WebResourceContext.Other;
	}

	private static string GetPathWithoutQuery(string url)
	{
		var path = url.ToLowerInvariant();
		var queryIndex = path.IndexOf('?');
		if (queryIndex >= 0)
		{
			path = path.Substring(0, queryIndex);
		}
		return path;
	}

	private static bool IsDocument(string path)
		=> path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith('/')
			|| !path.Contains('.');

	private static bool IsImage(string path)
		=> path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);

	private static bool IsFont(string path)
		=> path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".eot", StringComparison.OrdinalIgnoreCase);

	private static bool IsMedia(string path)
		=> path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
			|| path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
}
