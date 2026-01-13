using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

internal class EditorConfigPathUpdaterMan(PathMapper mapper, bool isPacking)
{
	private const string _scheme = "uno-ws-pack://";
	private static readonly StringComparer _pathComparer = OperatingSystem.IsWindows()
		? StringComparer.OrdinalIgnoreCase
		: StringComparer.Ordinal;

	//private static readonly SearchValues<char> _pathInvalidChars = SearchValues.Create([..Path.GetInvalidPathChars(), ':']);
	//private static readonly SearchValues<char> _pathSeparators = SearchValues.Create(['\\', '/']);

	public async ValueTask<ImmutableHashSet<string>> MakeRelativeToAsync(string workingDir, string editorConfigPath, CancellationToken ct)
	{
		var updated = ImmutableHashSet.CreateBuilder<string>(_pathComparer);
		var output = new StringBuilder();
		foreach (var line in await File.ReadAllLinesAsync(editorConfigPath, ct))
		{
			var lineSpan = line.AsSpan().Trim();
			if (line is not [';', ..] and not ['#', ..] // Comments
				&& (TryUpdateSectionLine(workingDir, ref lineSpan, out var path)
					|| TryUpdateKeyValueLine(workingDir, ref lineSpan, out path)))
			{
				updated.Add(path.ToString());
				output.AppendLine(lineSpan.ToString());
			}
			else
			{
				output.AppendLine(line);
			}
		}

		if (updated.Count > 0)
		{
			await File.WriteAllTextAsync(editorConfigPath, output.ToString(), ct);
		}

		return updated.ToImmutable();
	}

	private bool TryUpdateSectionLine(string workingDir, ref ReadOnlySpan<char> line, out ReadOnlySpan<char> path)
	{
		if (line is not ['[', .., ']'])
		{
			path = null;
			return false;
		}

		var value = line[1..^1];
		if (TryUpdateValue(workingDir, value, out var updated))
		{
			path = value;
			var updatedLine = new Span<char>(new char[updated.Length + 2]) { [0] = '[', [^1] = ']' };
			updated.CopyTo(updatedLine[1..]);
			updatedLine[1..^1].Replace('\\', '/'); // For sections we always want to use '/' no matter the OS!

			line = updatedLine;
			return true;
		}
		else
		{
			path = null;
			return false;
		}
	}

	private bool TryUpdateKeyValueLine(string workingDir, ref ReadOnlySpan<char> line, out ReadOnlySpan<char> path)
	{
		var separator = line.IndexOf('=');
		if (separator < 0)
		{
			path = default;
			return false;
		}

		var valueStart = separator + 1;
		var value = line[valueStart..].TrimStart();
		if (TryUpdateValue(workingDir, value, out var updated))
		{
			path = value;
			var updatedLine = new Span<char>(new char[valueStart + 1 + updated.Length]);
			line[..valueStart].CopyTo(updatedLine);
			updatedLine[valueStart] = ' '; // Preserve at least one space after '='
			updated.CopyTo(updatedLine[(valueStart + 1)..]);

			line = updatedLine;
			return true;
		}
		else
		{
			path = null;
			return false;
		}
	}

	//private static bool TryUpdateValue(ReadOnlySpan<char> value, ReadOnlySpan<char> basePath, char separator, out Span<char> updated)
	//{
	//	if (Path.GetPathRoot(value) is not { Length: > 0 } root
	//		|| !Path.Exists(value.ToString()))
	//	{
	//		updated = default;
	//		return false;
	//	}

	//	updated = new Span<char>(new char[basePath.Length + value.Length]);
	//	basePath.CopyTo(updated);
	//	value.CopyTo(updated.Slice(basePath.Length));

	//	// Patch the root
	//	updated.Slice(basePath.Length, root.Length).ReplaceAny(_pathInvalidChars, '_');

	//	// Patch dir separator in the whole path (including the root which may contains a '\' (e.g. "C:\"))
	//	updated.Slice(basePath.Length).Replace('\\', separator);

	//	return true;
	//}

	private bool TryUpdateValue(string workingDir, ReadOnlySpan<char> value, out ReadOnlySpan<char> updated)
	{
		if (value.IsEmpty)
		{
			updated = default;
			return false;
		}

		if (isPacking)
		{
			var path = value.ToString();
			if (Path.IsPathRooted(path) && Path.Exists(Path.Combine(workingDir, value.ToString())))
			{
				var mapped = mapper(value);
				var result = new char[_scheme.Length + mapped.Length].AsSpan();

				_scheme.AsSpan().CopyTo(result);
				mapped.CopyTo(result.Slice(_scheme.Length));

				updated = result;
				return true;
			}
			else
			{
				updated = default;
				return false;
			}
		}
		else
		{
			if (value.StartsWith(_scheme.AsSpan()))
			{
				updated = mapper(value[_scheme.Length..]);
				return true;
			}
			else
			{
				updated = default;
				return false;
			}
		}


		//if (Path.GetPathRoot(value) is not { Length: > 0 } root
		//	|| !Path.Exists(value.ToString()))
		//{
		//	updated = default;
		//	return false;
		//}

		//updated = new Span<char>(new char[basePath.Length + value.Length]);
		//basePath.CopyTo(updated);
		//value.CopyTo(updated.Slice(basePath.Length));

		//// Patch the root
		//updated.Slice(basePath.Length, root.Length).ReplaceAny(_pathInvalidChars, '_');

		//// Patch dir separator in the whole path (including the root which may contains a '\' (e.g. "C:\"))
		//updated.Slice(basePath.Length).Replace('\\', separator);

		//return true;
	}
}

public delegate ReadOnlySpan<char> PathMapper(ReadOnlySpan<char> path);
