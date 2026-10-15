#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Runtime.Wayland;

// File lists exchanged as text/uri-list (RFC 2483) and x-special/gnome-copied-files (GNOME/GTK file managers).
internal static class WaylandFileUris
{
	internal const string UriListMimeType = "text/uri-list";
	internal const string GnomeCopiedFilesMimeType = "x-special/gnome-copied-files";

	/// <summary>Returns the local paths of the file URIs in a text/uri-list; other URIs and comments are skipped.</summary>
	internal static List<string> ParseUriList(string text)
	{
		var paths = new List<string>();
		// Some sources use bare LF or append a NUL terminator.
		foreach (var line in text.TrimEnd('\0').Split('\n'))
		{
			var uri = line.TrimEnd('\r');
			if (uri.Length > 0 && uri[0] != '#' && ToLocalPath(uri) is { } path)
			{
				paths.Add(path);
			}
		}
		return paths;
	}

	/// <summary>Returns the local paths of x-special/gnome-copied-files content: an action line ("copy" or "cut") followed by URIs.</summary>
	internal static List<string> ParseGnomeCopiedFiles(string text)
	{
		var newLine = text.IndexOf('\n');
		return newLine < 0 ? new List<string>() : ParseUriList(text[(newLine + 1)..]);
	}

	internal static string FormatUriList(IEnumerable<string> paths)
	{
		var builder = new StringBuilder();
		foreach (var path in paths)
		{
			builder.Append(ToFileUri(path)).Append("\r\n");
		}
		return builder.ToString();
	}

	/// <summary>The file URIs as plain text, one per line like Qt and GTK file managers expose their file lists as text.</summary>
	internal static string FormatUriText(IEnumerable<string> paths)
	{
		var builder = new StringBuilder();
		foreach (var path in paths)
		{
			if (builder.Length > 0)
			{
				builder.Append('\n');
			}
			builder.Append(ToFileUri(path));
		}
		return builder.ToString();
	}

	internal static string FormatGnomeCopiedFiles(IEnumerable<string> paths)
	{
		var builder = new StringBuilder("copy");
		foreach (var path in paths)
		{
			builder.Append('\n').Append(ToFileUri(path));
		}
		return builder.ToString();
	}

	internal static string ToFileUri(string path)
	{
		var builder = new StringBuilder("file://");
		foreach (var value in Encoding.UTF8.GetBytes(path))
		{
			if (value is (>= (byte)'a' and <= (byte)'z') or (>= (byte)'A' and <= (byte)'Z') or (>= (byte)'0' and <= (byte)'9')
				or (byte)'/' or (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~')
			{
				builder.Append((char)value);
			}
			else
			{
				builder.Append('%').Append(value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
			}
		}
		return builder.ToString();
	}

	internal static string? ToLocalPath(string uri)
	{
		if (!uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		var rest = uri["file:".Length..];
		if (rest.StartsWith("//", StringComparison.Ordinal))
		{
			// Authority form: only an empty host or localhost designates this machine.
			var slash = rest.IndexOf('/', 2);
			if (slash < 0)
			{
				return null;
			}
			var host = rest[2..slash];
			if (host.Length > 0 && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			rest = rest[slash..];
		}
		return rest.StartsWith('/') ? Uri.UnescapeDataString(rest) : null;
	}
}
