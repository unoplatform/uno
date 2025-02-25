#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Uno.UI.Helpers;

/// <summary>
/// Compares two absolute path strings for equality. Supports Windows, Unix, and UNC path formats. URIs are not supported.
/// </summary>
/// <remarks>
/// This will resolve "." and ".." segments and normalize slashes vs backslashes before doing the comparison.
/// Relative paths will always compare as unequal. Same for empty paths (C:\ or / on unix/max).
/// IMPORTANT: This comparer WILL NOT resolve symbolic links or check if the filesystem is case-sensitive.
/// It's just comparing the strings as paths, no I/O is performed.
/// </remarks>
internal class AbsolutePathComparer : IEqualityComparer<string?>
{
	private readonly bool _ignoreCase;

	public static readonly AbsolutePathComparer Comparer = new(ignoreCase: false);
	public static readonly AbsolutePathComparer ComparerIgnoreCase = new(ignoreCase: true);

	private enum PathType
	{
		Relative,
		Unix,
		Windows,
		UNC,
	}

	private AbsolutePathComparer(bool ignoreCase = true)
	{
		_ignoreCase = ignoreCase;
	}

	/// <summary>
	/// Determines if two path strings are equal.
	/// </summary>
	public bool Equals(string? x, string? y)
	{
		if (x == null || y == null)
		{
			return false;
		}

		var spanX = x.AsSpan();
		var spanY = y.AsSpan();

		// Parse each path into a drive (if any) and segments
		var (typeX, driveX, segmentsX) = ParsePath(spanX);
		var (typeY, driveY, segmentsY) = ParsePath(spanY);

		// Compare path types (e.g., relative, UNC, Windows, or Unix)
		if (typeX != typeY)
		{
			return false;
		}

		// Compare drive letters (e.g., "C:" vs "D:") or empty if UNC or no drive
		if (driveX != driveY)
		{
			return false;
		}

		// Compare the number of segments, ensure at least one segment
		if (segmentsX.Count == 0 || segmentsX.Count != segmentsY.Count)
		{
			return false;
		}

		// Compare each corresponding segment
		for (var i = 0; i < segmentsX.Count; i++)
		{
			var segX = segmentsX[i];
			var segY = segmentsY[i];

			if (!SegmentEquals(spanX, segX.start, segX.length, spanY, segY.start, segY.length))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Computes a hash code for the given path string.
	/// </summary>
	public int GetHashCode(string? path)
	{
		if (path is null || path.Length == 0)
		{
			return -1;
		}

		var (type, drive, segments) = ParsePath(path.AsSpan());

		var hash = 17;

		// Include the path type in the hash
		hash = unchecked(hash * 79 + (int)type);

		// Include the drive in the hash (no effect is drive is '\0')
		hash = unchecked(hash * 31 + NormalizeChar(drive));

		// Include each segment in the hash
		foreach (var (start, length) in segments)
		{
			for (var i = 0; i < length; i++)
			{
				var c = path[start + i];
				hash = unchecked(hash * 31 + NormalizeChar(c));
			}
			// Use a slash as a separator between segments
			hash = unchecked(hash * 31 + '/');
		}

		return hash;
	}

	/// <summary>
	/// Parses a path into an optional drive (e.g., "C:") and a list of segments.
	/// Also handles UNC paths that begin with "//" or "\\".
	/// </summary>
	private (PathType type, char drive, List<(int start, int length)>) ParsePath(ReadOnlySpan<char> path)
	{
		if (!IsPathFullyQualified(path))
		{
			return (PathType.Relative, '\0', []); // Only fully-qualified paths are supported
		}

		var segments = new List<(int start, int length)>();

		var type = PathType.Unix; // Assume Unix-style path
		var drive = '\0'; // '\0' means no drive letter
		var idx = 0;

		// 1) Check for UNC path (starts with "//" or "\\")
		if (path.Length >= 2 && IsPathSegmentSeparator(path[0]) && IsPathSegmentSeparator(path[1]))
		{
			// Skip the initial double slash
			type = PathType.UNC;
			idx = 2;
		}
		// 2) Otherwise, check for local drive (e.g., "C:")
		else if (path.Length >= 2 && path[1] == ':')
		{
			// Extract the drive letter advance to the next character after the colon
			type = PathType.Windows;
			drive = char.ToUpper(path[0], CultureInfo.InvariantCulture);
			idx = 2;
		}

		// Skip any additional slashes after drive or UNC prefix
		while (idx < path.Length && IsPathSegmentSeparator(path[idx]))
		{
			idx++;
		}

		// Parse segments until we reach the end
		while (idx < path.Length)
		{
			var segStart = idx;

			// Move until the next slash or end of string
			while (idx < path.Length && !IsPathSegmentSeparator(path[idx]))
			{
				idx++;
			}

			var segLength = idx - segStart;

			if (segLength == 1 && path[segStart] == '.')
			{
				// Ignore single-dot segments
			}
			else if (segLength == 2 && path[segStart] == '.' && path[segStart + 1] == '.')
			{
				// Pop the last segment for ".." if possible
				if (segments.Count > 0)
				{
					segments.RemoveAt(segments.Count - 1);
				}
			}
			else if (segLength > 0)
			{
				// Normal segment => record its start/length
				segments.Add((segStart, segLength));
			}

			// Skip any consecutive slashes
			while (idx < path.Length && IsPathSegmentSeparator(path[idx]))
			{
				idx++;
			}
		}

		return (type, drive, segments);
	}

	// Determines if the given path is fully qualified (e.g., "C:\foo", "\\server\share\foo", or "/foo").
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsPathFullyQualified(ReadOnlySpan<char> path)
	{
		if (path.Length == 0)
		{
			return false;
		}

		// Unix-like, UNC or non-drive specific path
		if (IsPathSegmentSeparator(path[0]))
		{
			return true;
		}

		// Drive letter (must be followed by a colon and a path separator) - must also be a valid drive letter
		if (path.Length >= 3 && path[1] == ':' && IsPathSegmentSeparator(path[2]) && char.IsLetter(path[0]))
		{
			return true;
		}

		// Relative path
		return false;
	}

	/// <summary>
	/// Compares two path segments [start..start+length] for equality (respecting or ignoring case).
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool SegmentEquals(ReadOnlySpan<char> a, int startA, int lenA, ReadOnlySpan<char> b, int startB, int lenB)
	{
		if (lenA != lenB)
		{
			return false; // Should never happen (already checked in the caller)
		}

		if (_ignoreCase)
		{
			for (var i = 0; i < lenA; i++)
			{
				if (char.ToLowerInvariant(a[startA + i]) != char.ToLowerInvariant(b[startB + i]))
				{
					return false;
				}
			}
		}
		else
		{
			for (var i = 0; i < lenA; i++)
			{
				if (a[startA + i] != b[startB + i])
				{
					return false;
				}
			}
		}

		return true;
	}

	/// <summary>
	/// Normalizes a character (e.g., converting it to lowercase if ignoring case).
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private char NormalizeChar(char c) => _ignoreCase ? char.ToLowerInvariant(c) : c;

	/// <summary>
	/// Returns true if the given character is a slash ('/' or '\').
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsPathSegmentSeparator(char c) => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
}
