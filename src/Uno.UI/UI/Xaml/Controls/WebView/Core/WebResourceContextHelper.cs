#nullable enable

using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Shared helper for determining resource context from URLs.
/// </summary>
internal static class WebResourceContextHelper
{
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
