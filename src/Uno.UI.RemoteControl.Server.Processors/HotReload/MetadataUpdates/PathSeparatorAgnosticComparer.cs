using System;
using System.Collections.Generic;
using System.IO;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

internal class PathSeparatorAgnosticComparer : IEqualityComparer<string>
{
	private static readonly bool _ignoreCase = OperatingSystem.IsWindows();

	public static PathSeparatorAgnosticComparer Instance { get; } = new();

	/// <inheritdoc />
	public bool Equals(string? x, string? y)
	{
		if (ReferenceEquals(x, y))
		{
			return true;
		}

		if (x is null || y is null || x.Length != y.Length)
		{
			return false;
		}

		for (var i = 0; i < x.Length; i++)
		{
			if (NormalizeChar(x[i]) != NormalizeChar(y[i]))
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc />
	public int GetHashCode(string obj)
	{
		if (obj is null)
		{
			throw new ArgumentNullException(nameof(obj));
		}

		var hash = new HashCode();
		foreach (var ch in obj)
		{
			hash.Add(NormalizeChar(ch));
		}

		return hash.ToHashCode();
	}

	private static char NormalizeChar(char value)
	{
		if (value == Path.DirectorySeparatorChar || value == Path.AltDirectorySeparatorChar)
		{
			return '/';
		}

		return _ignoreCase ? char.ToUpperInvariant(value) : value;
	}
}
