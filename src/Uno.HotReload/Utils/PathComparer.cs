using System;
using System.Collections.Generic;
using System.IO;

namespace Uno.HotReload.Utils;

/// <summary>
/// Path comparison helpers used throughout the hot-reload pipeline.
/// </summary>
/// <remarks>
/// <para>
/// File paths that flow through the pipeline come from multiple sources with different
/// separator conventions: MSBuild items (<c>\</c>-native on Windows), file-system watcher
/// events (OS-native), direct RPC file-update requests (often <c>/</c>-normalized), tooling
/// that normalizes for JSON transport, etc. Roslyn's <c>Workspace</c> does NOT deduplicate
/// <see cref="Microsoft.CodeAnalysis.AdditionalDocument"/>s by normalized path — two entries
/// with <c>C:\Foo\Bar.xaml</c> and <c>C:/Foo/Bar.xaml</c> coexist, causing generators that
/// enumerate <c>AdditionalFiles</c> to see (and process) the same physical file twice.
/// </para>
/// <para>
/// All path-equality checks inside this assembly MUST go through these helpers so the same
/// physical file is treated as a single entry regardless of how the path was spelled at its
/// point of origin.
/// </para>
/// </remarks>
public static class PathComparer
{
	/// <summary>
	/// Case-agnostic string comparer on Windows, case-sensitive elsewhere. Retained for
	/// callers that need a <see cref="StringComparer"/> for dictionaries/sets where the
	/// keys have already been normalized via <see cref="Normalize"/>.
	/// </summary>
	public static readonly StringComparer Comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	/// <summary>
	/// <see cref="StringComparison"/> companion to <see cref="Comparer"/>. Safe for
	/// <c>StartsWith</c>/<c>EndsWith</c> checks ONLY when both operands have been
	/// pre-normalized via <see cref="Normalize"/>.
	/// </summary>
	public static readonly StringComparison Comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

	/// <summary>
	/// <see cref="IEqualityComparer{T}"/> that compares two paths as representing the same
	/// physical file regardless of separator style (<c>\</c> vs <c>/</c>) and, on Windows,
	/// casing. Use this wherever paths from different sources must be matched.
	/// </summary>
	public static readonly IEqualityComparer<string> PathEqualityComparer = new SeparatorAgnosticComparer();

	/// <summary>
	/// Returns <c>true</c> when both paths denote the same physical file, treating
	/// <c>/</c> and <c>\</c> as equivalent and, on Windows, ignoring casing. Either or
	/// both arguments may be <c>null</c>; two nulls are considered equal.
	/// </summary>
	public static bool PathEquals(string? left, string? right)
		=> PathEqualityComparer.Equals(left!, right!);

	/// <summary>
	/// Normalizes directory separators in <paramref name="path"/> to
	/// <see cref="Path.DirectorySeparatorChar"/>. Returns <paramref name="path"/> unchanged
	/// when it is <c>null</c>. No filesystem access is performed; relative paths, <c>.</c>
	/// and <c>..</c> segments are preserved as-is.
	/// </summary>
	public static string? Normalize(string? path)
	{
		if (path is null)
		{
			return null;
		}

		// On Windows, Path.DirectorySeparatorChar == '\' and AltDirectorySeparatorChar == '/'.
		// On Unix, Path.DirectorySeparatorChar == '/' and AltDirectorySeparatorChar == '/' (same).
		// The replace is therefore a no-op on Unix.
		return Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar
			? path
			: path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
	}

	private sealed class SeparatorAgnosticComparer : IEqualityComparer<string>
	{
		public bool Equals(string? x, string? y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x is null || y is null)
			{
				return false;
			}

			// Normalize separators before delegating to the underlying case-aware comparer.
			return Comparer.Equals(Normalize(x), Normalize(y));
		}

		public int GetHashCode(string obj)
			=> obj is null ? 0 : Comparer.GetHashCode(Normalize(obj)!);
	}
}
