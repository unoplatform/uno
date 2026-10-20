#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Serves <see cref="WebViewVirtualHostMap.NativeScheme"/> requests for a native view: raises <c>WebResourceRequested</c>,
/// waits for its deferral, and falls back to the folder mapped to the host, then to a 404.
/// </summary>
/// <remarks>
/// Platform-neutral: the native scheme handler (WebKit's <c>WKURLSchemeHandler</c>) converts its task into a
/// <see cref="CustomSchemeRequest"/> and turns the returned snapshot into its native response.
/// </remarks>
internal sealed class CustomSchemeResourceHandler
{
	private const string ContentTypeHeader = "Content-Type";
	private const string ContentLengthHeader = "Content-Length";

	private readonly Func<string, CoreWebView2WebResourceContext, bool> _matchesFilter;
	private readonly Action<CoreWebView2WebResourceRequestedEventArgs> _raise;

	/// <param name="matchesFilter">Whether a native URL and context match one of the view's <c>WebResourceRequested</c> filters.</param>
	/// <param name="raise">Raises the view's <c>WebResourceRequested</c> event.</param>
	public CustomSchemeResourceHandler(
		Func<string, CoreWebView2WebResourceContext, bool> matchesFilter,
		Action<CoreWebView2WebResourceRequestedEventArgs> raise)
	{
		_matchesFilter = matchesFilter;
		_raise = raise;
	}

	/// <summary>
	/// The map handed to the view through <see cref="ISupportsCustomSchemeVirtualHosts.SetVirtualHostMap"/>.
	/// </summary>
	public WebViewVirtualHostMap? Map { get; set; }

	/// <summary>
	/// The base of relative <c>SetVirtualHostNameToFolderMapping</c> folders; <see cref="AppContext.BaseDirectory"/> when null.
	/// </summary>
	public string? FolderBasePath { get; set; }

	/// <summary>
	/// Produces the response for <paramref name="request"/>. Never throws but for cancellation: a failing handler
	/// becomes a 500 response.
	/// </summary>
	/// <remarks>
	/// Must be called on the UI thread, where <c>WebResourceRequested</c> is raised. The returned task completes
	/// when the handler's deferrals do, possibly on another thread.
	/// </remarks>
	public async Task<WebResourceResponseSnapshot> HandleAsync(CustomSchemeRequest request, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(request);

		try
		{
			var context = ClassifyResourceContext(request);

			if (_matchesFilter(request.Uri, context))
			{
				var args = new CustomSchemeWebResourceRequestedEventArgs(request, context);
				try
				{
					_raise(new CoreWebView2WebResourceRequestedEventArgs(args));
				}
				finally
				{
					args.OnHandlerReturned();
				}

				await args.Completion.WaitAsync(ct);

				if (args.Response is { } response)
				{
					return Normalize(await WebResourceResponseSnapshot.CreateAsync(response, ct), request.Uri);
				}
			}

			if (await TryServeFolderAsync(request, ct) is { } file)
			{
				return file;
			}

			return CreateTextResponse(404, "Not Found");
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to serve '{request.Uri}'.", e);
			}

			return CreateTextResponse(500, "Internal Server Error");
		}
	}

	/// <summary>
	/// Classifies a request for <c>WebResourceRequested</c> filters.
	/// </summary>
	/// <remarks>
	/// WebKit does not report what a request is for. The top-level document is the request whose URL is its own
	/// <paramref name="request"/>'s <see cref="CustomSchemeRequest.MainDocumentUri"/>; frames are recognized by an
	/// <c>Accept</c> header that prefers <c>text/html</c>; other requests are classified by <c>Accept</c>, then by
	/// file extension, and are never <see cref="CoreWebView2WebResourceContext.Document"/>.
	/// </remarks>
	internal static CoreWebView2WebResourceContext ClassifyResourceContext(CustomSchemeRequest request)
	{
		if (request.MainDocumentUri is { } main && IsSameDocument(request.Uri, main))
		{
			return CoreWebView2WebResourceContext.Document;
		}

		var accept = GetHeader(request.Headers, "Accept");
		if (accept is not null)
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

		var context = WebResourceContextHelper.DetermineResourceContext(StripFragment(request.Uri));
		if (context == CoreWebView2WebResourceContext.Document)
		{
			// Extension-less subresources (e.g. fetch("/api/data")) are not documents.
			return request.MainDocumentUri is null ? context : CoreWebView2WebResourceContext.Other;
		}

		return context;
	}

	/// <summary>
	/// Makes a snapshot safe to hand to WebKit: a valid status code, a <c>Content-Type</c>, and a
	/// <c>Content-Length</c> that matches the body.
	/// </summary>
	internal static WebResourceResponseSnapshot Normalize(WebResourceResponseSnapshot snapshot, string uri)
	{
		var headers = new List<KeyValuePair<string, string>>(snapshot.Headers.Count + 2);
		var hasContentType = false;

		foreach (var header in snapshot.Headers)
		{
			if (string.Equals(header.Key, ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
			{
				// A length that does not match the body stalls or truncates the load.
				continue;
			}

			if (string.Equals(header.Key, ContentTypeHeader, StringComparison.OrdinalIgnoreCase))
			{
				if (hasContentType || string.IsNullOrWhiteSpace(header.Value))
				{
					continue;
				}

				hasContentType = true;
			}

			headers.Add(header);
		}

		if (!hasContentType)
		{
			headers.Add(new(ContentTypeHeader, GetContentType(uri)));
		}

		headers.Add(new(ContentLengthHeader, snapshot.Body.Length.ToString(CultureInfo.InvariantCulture)));

		var statusCode = snapshot.StatusCode is >= 100 and <= 599 ? snapshot.StatusCode : 500;

		return snapshot with { StatusCode = statusCode, Headers = headers };
	}

	/// <summary>
	/// A MIME type for <paramref name="uri"/> from its extension; <c>application/octet-stream</c> when unknown.
	/// </summary>
	internal static string GetContentType(string uri)
	{
		var path = uri;
		if (System.Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
		{
			path = parsed.AbsolutePath;
		}

		if (path.EndsWith('/'))
		{
			return "text/html";
		}

		return Path.GetExtension(path).ToLowerInvariant() switch
		{
			".html" or ".htm" => "text/html",
			".js" or ".mjs" => "text/javascript",
			".css" => "text/css",
			".json" or ".map" => "application/json",
			".wasm" => "application/wasm",
			".svg" => "image/svg+xml",
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".gif" => "image/gif",
			".webp" => "image/webp",
			".ico" => "image/x-icon",
			".woff" => "font/woff",
			".woff2" => "font/woff2",
			".ttf" => "font/ttf",
			".otf" => "font/otf",
			".txt" => "text/plain",
			".xml" => "application/xml",
			".mp4" => "video/mp4",
			".webm" => "video/webm",
			".mp3" => "audio/mpeg",
			_ => "application/octet-stream",
		};
	}

	internal static WebResourceResponseSnapshot CreateTextResponse(int statusCode, string reasonPhrase)
	{
		var body = Encoding.UTF8.GetBytes(reasonPhrase);
		return new(
			statusCode,
			reasonPhrase,
			new KeyValuePair<string, string>[]
			{
				new(ContentTypeHeader, "text/plain; charset=utf-8"),
				new(ContentLengthHeader, body.Length.ToString(CultureInfo.InvariantCulture)),
			},
			body);
	}

	/// <summary>
	/// Resolves <paramref name="uri"/> to a file under <paramref name="root"/>, or null when it escapes the root.
	/// </summary>
	internal static string? ResolveFilePath(string root, string uri)
	{
		if (!System.Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
		{
			return null;
		}

		var relative = System.Uri.UnescapeDataString(parsed.AbsolutePath).TrimStart('/');
		if (relative.Length == 0 || relative.Contains('\0'))
		{
			return null;
		}

		var fullRoot = Path.GetFullPath(root);
		var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relative));
		var rootWithSeparator = Path.EndsInDirectorySeparator(fullRoot) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;

		return fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal) ? fullPath : null;
	}

	private async Task<WebResourceResponseSnapshot?> TryServeFolderAsync(CustomSchemeRequest request, CancellationToken ct)
	{
		if (Map is not { } map
			|| !System.Uri.TryCreate(request.Uri, UriKind.Absolute, out var uri)
			|| !map.TryGetFolder(uri.Host, out var folder)
			|| string.IsNullOrEmpty(folder)
			|| !(request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) || request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)))
		{
			return null;
		}

		var root = Path.IsPathRooted(folder) ? folder : Path.Combine(FolderBasePath ?? AppContext.BaseDirectory, folder);
		if (ResolveFilePath(root, request.Uri) is not { } path || !File.Exists(path))
		{
			return null;
		}

		var body = await File.ReadAllBytesAsync(path, ct);
		return Normalize(new WebResourceResponseSnapshot(200, "OK", Array.Empty<KeyValuePair<string, string>>(), body), request.Uri);
	}

	private static string? GetHeader(IReadOnlyList<KeyValuePair<string, string>> headers, string name)
	{
		foreach (var header in headers)
		{
			if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
			{
				return header.Value;
			}
		}

		return null;
	}

	private static bool IsSameDocument(string uri, string mainDocumentUri)
		=> string.Equals(StripFragment(uri), StripFragment(mainDocumentUri), StringComparison.Ordinal);

	private static string StripFragment(string uri)
	{
		var index = uri.IndexOf('#');
		return index < 0 ? uri : uri.Substring(0, index);
	}
}
