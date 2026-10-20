#nullable enable

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Runs <see cref="ScriptBridgeWebViewCapabilities.InjectIntoHtml"/> on an HTML document held as bytes, for native
/// views that inject document-created scripts into the responses they serve (<see cref="WebViewScriptInjectionMode.BeforeParse"/>).
/// </summary>
/// <remarks>
/// The document is decoded the way a browser would pick its encoding (byte order mark, then the transport charset,
/// then a <c>&lt;meta&gt;</c> declaration in the first 1024 bytes, then UTF-8 if the bytes are valid UTF-8, else
/// windows-1252), and re-encoded as UTF-8 with a byte order mark. The mark outranks both the transport charset and
/// any <c>&lt;meta charset&gt;</c> the page declares, so the injected text cannot change how the page is decoded.
/// </remarks>
internal static partial class HtmlDocumentScriptInjector
{
	private const int MetaPrescanLength = 1024;

	private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

	/// <summary>
	/// The charset of the document <see cref="TryInject"/> produces.
	/// </summary>
	public const string OutputCharset = "utf-8";

	/// <summary>
	/// Decodes <paramref name="document"/>, lets <paramref name="injectIntoHtml"/> rewrite the text and encodes the result.
	/// </summary>
	/// <param name="transportCharset">The <c>charset</c> parameter of the response's <c>Content-Type</c>, if any.</param>
	/// <returns>false when the document's encoding is not available on this runtime; the document must then be served unchanged.</returns>
	public static bool TryInject(ReadOnlySpan<byte> document, string? transportCharset, Func<string, string> injectIntoHtml, out byte[] result)
	{
		ArgumentNullException.ThrowIfNull(injectIntoHtml);

		if (!TryDetectEncoding(document, transportCharset, out var encoding, out var preambleLength))
		{
			result = [];
			return false;
		}

		var html = encoding.GetString(document[preambleLength..]);
		var injected = injectIntoHtml(html);

		var output = new byte[Utf8Bom.Length + Encoding.UTF8.GetByteCount(injected)];
		Utf8Bom.CopyTo(output, 0);
		Encoding.UTF8.GetBytes(injected, 0, injected.Length, output, Utf8Bom.Length);

		result = output;
		return true;
	}

	/// <summary>
	/// Picks the encoding of an HTML document, following the order of the HTML encoding sniffing algorithm.
	/// </summary>
	/// <param name="preambleLength">The length of the byte order mark to skip, or 0.</param>
	internal static bool TryDetectEncoding(ReadOnlySpan<byte> document, string? transportCharset, out Encoding encoding, out int preambleLength)
	{
		if (document.StartsWith(Utf8Bom))
		{
			(encoding, preambleLength) = (new UTF8Encoding(false), 3);
			return true;
		}

		if (document.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE]))
		{
			(encoding, preambleLength) = (new UnicodeEncoding(bigEndian: false, byteOrderMark: false), 2);
			return true;
		}

		if (document.StartsWith((ReadOnlySpan<byte>)[0xFE, 0xFF]))
		{
			(encoding, preambleLength) = (new UnicodeEncoding(bigEndian: true, byteOrderMark: false), 2);
			return true;
		}

		preambleLength = 0;

		if (!string.IsNullOrWhiteSpace(transportCharset))
		{
			return TryGetEncoding(transportCharset, out encoding);
		}

		if (FindMetaCharset(document) is { } metaCharset)
		{
			return TryGetEncoding(metaCharset, out encoding);
		}

		encoding = IsValidUtf8(document) ? new UTF8Encoding(false) : Windows1252();
		return true;
	}

	/// <summary>
	/// Resolves an encoding label. The labels the HTML standard maps to windows-1252 (us-ascii and iso-8859-1
	/// included) fall back to Latin-1 when the runtime has no code page 1252, and a <c>&lt;meta&gt;</c> UTF-16 label
	/// means UTF-8, as in browsers.
	/// </summary>
	internal static bool TryGetEncoding(string label, out Encoding encoding)
	{
		var name = label.Trim().Trim('"', '\'').ToLowerInvariant();
		switch (name)
		{
			case "utf-8" or "utf8" or "unicode-1-1-utf-8":
				encoding = new UTF8Encoding(false);
				return true;
			case "us-ascii" or "ascii" or "iso-8859-1" or "iso8859-1" or "latin1" or "l1" or "windows-1252" or "cp1252" or "x-cp1252":
				encoding = Windows1252();
				return true;
		}

		try
		{
			encoding = Encoding.GetEncoding(name);
			return true;
		}
		catch (ArgumentException)
		{
			encoding = null!;
			return false;
		}
	}

	private static Encoding Windows1252()
	{
		try
		{
			return Encoding.GetEncoding(1252);
		}
		catch (Exception e) when (e is ArgumentException or NotSupportedException)
		{
			// Differs from windows-1252 only in 0x80-0x9F, which Latin-1 maps to C1 controls.
			return Encoding.Latin1;
		}
	}

	private static string? FindMetaCharset(ReadOnlySpan<byte> document)
	{
		var prescan = Encoding.Latin1.GetString(document[..Math.Min(document.Length, MetaPrescanLength)]);
		var match = MetaCharset().Match(prescan);
		if (!match.Success)
		{
			return null;
		}

		var charset = match.Groups["charset"].Value;
		return charset.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase) ? "utf-8" : charset;
	}

	private static bool IsValidUtf8(ReadOnlySpan<byte> document)
	{
		try
		{
			_ = new UTF8Encoding(false, throwOnInvalidBytes: true).GetCharCount(document);
			return true;
		}
		catch (DecoderFallbackException)
		{
			return false;
		}
	}

	// <meta charset="x"> and <meta http-equiv="Content-Type" content="text/html; charset=x">, attributes in any order.
	[GeneratedRegex(@"<meta\s[^>]*?charset\s*=\s*[""']?\s*(?<charset>[A-Za-z0-9_.:\-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex MetaCharset();
}
