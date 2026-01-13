//using System;
//using System.Collections.Immutable;
//using System.IO;

//namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

//internal static class EditorConfigPathUpdater
//{
//	private const string VirtualRoot = "/";
//	private static readonly StringComparison _pathComparison = OperatingSystem.IsWindows()
//		? StringComparison.OrdinalIgnoreCase
//		: StringComparison.Ordinal;

//	public static bool MakeRelativeTo(string editorConfigPath, string basePath, out ImmutableArray<string> replacedPaths)
//	{
//		ArgumentNullException.ThrowIfNull(editorConfigPath);

//		replacedPaths = ImmutableArray<string>.Empty;

//		if (!File.Exists(editorConfigPath))
//		{
//			return false;
//		}

//		var normalizedBase = NormalizeBase(basePath);
//		var lines = File.ReadAllLines(editorConfigPath);
//		var updated = false;
//		var replaced = ImmutableArray.CreateBuilder<string>();

//		for (var index = 0; index < lines.Length; index++)
//		{
//			var line = lines[index];
//			if (TryUpdateSectionLine(line, normalizedBase, replaced, out var sectionLine))
//			{
//				lines[index] = sectionLine;
//				updated = true;
//				continue;
//			}

//			if (TryUpdateKeyValueLine(line, normalizedBase, replaced, out var keyValueLine))
//			{
//				lines[index] = keyValueLine;
//				updated = true;
//			}
//		}

//		if (updated)
//		{
//			File.WriteAllLines(editorConfigPath, lines);
//			replacedPaths = replaced.ToImmutable();
//		}
//		else
//		{
//			replacedPaths = ImmutableArray<string>.Empty;
//		}

//		return updated;
//	}

//	private static string NormalizeBase(string basePath)
//		=> string.IsNullOrWhiteSpace(basePath) || basePath == VirtualRoot
//			? VirtualRoot
//			: Path.GetFullPath(basePath);

//	private static bool TryUpdateSectionLine(string line, string basePath, ImmutableArray<string>.Builder replacedPaths, out string updatedLine)
//	{
//		updatedLine = line;
//		var trimmed = line.Trim();
//		if (!trimmed.StartsWith("[", StringComparison.Ordinal) || !trimmed.EndsWith("]", StringComparison.Ordinal))
//		{
//			return false;
//		}

//		var openBracket = line.IndexOf('[');
//		var closeBracket = line.LastIndexOf(']');
//		if (openBracket < 0 || closeBracket <= openBracket)
//		{
//			return false;
//		}

//		var header = line.Substring(openBracket + 1, closeBracket - openBracket - 1);
//		if (!TryGetReplacement(header, basePath, isSection: true, out var replacement, out var recordedPath))
//		{
//			return false;
//		}

//		if (string.Equals(header, replacement, StringComparison.Ordinal))
//		{
//			return false;
//		}

//		updatedLine = line[..(openBracket + 1)] + replacement + line[closeBracket..];
//		replacedPaths.Add(recordedPath);
//		return true;
//	}

//	private static bool TryUpdateKeyValueLine(string line, string basePath, ImmutableArray<string>.Builder replacedPaths, out string updatedLine)
//	{
//		updatedLine = line;
//		var trimmed = line.Trim();
//		if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
//		{
//			return false;
//		}

//		var separatorIndex = line.IndexOf('=');
//		if (separatorIndex < 0)
//		{
//			return false;
//		}

//		var keyPart = line[..separatorIndex];
//		var valuePart = line[(separatorIndex + 1)..];
//		var trimmedValue = valuePart.Trim();
//		if (!TryGetReplacement(trimmedValue, basePath, isSection: false, out var replacement, out var recordedPath))
//		{
//			return false;
//		}

//		if (string.Equals(trimmedValue, replacement, _pathComparison))
//		{
//			return false;
//		}

//		var leadingWhitespaceLength = valuePart.Length - valuePart.TrimStart().Length;
//		var trailingWhitespaceLength = valuePart.Length - valuePart.TrimEnd().Length;
//		var leadingWhitespace = leadingWhitespaceLength > 0 ? valuePart[..leadingWhitespaceLength] : string.Empty;
//		var trailingWhitespace = trailingWhitespaceLength > 0 ? valuePart[^trailingWhitespaceLength..] : string.Empty;

//		updatedLine = keyPart + "=" + leadingWhitespace + replacement + trailingWhitespace;
//		replacedPaths.Add(recordedPath);
//		return true;
//	}

//	private static bool TryGetReplacement(string rawValue, string basePath, bool isSection, out string replacement, out string recordedPath)
//	{
//		replacement = rawValue;
//		recordedPath = rawValue;
//		var normalizedKey = NormalizeKey(rawValue);
//		if (normalizedKey.Length == 0)
//		{
//			return false;
//		}

//		if (!TryResolveFileSystemPath(normalizedKey, basePath, out var resolvedPath, out var pathKind))
//		{
//			return false;
//		}

//		var convertedValue = basePath == VirtualRoot
//			? GetPackageRelativePath(resolvedPath)
//			: resolvedPath;

//		replacement = isSection ? NormalizeSectionValue(convertedValue) : convertedValue;
//		recordedPath = pathKind == PathKind.Directory ? EnsureDirectorySuffix(replacement) : replacement;
//		return true;
//	}

//	private static string NormalizeKey(string value)
//	{
//		var trimmed = value.Trim().Trim('"');
//		return trimmed.Replace('\\', '/');
//	}

//	private static string NormalizeSectionValue(string value)
//		=> value.Replace('\\', '/');

//	private static bool TryResolveFileSystemPath(string normalizedKey, string basePath, out string resolvedPath, out PathKind pathKind)
//	{
//		var candidate = ToCurrentOsPath(normalizedKey);
//		if (Path.IsPathRooted(candidate))
//		{
//			return TryResolveExisting(candidate, out resolvedPath, out pathKind);
//		}

//		if (basePath == VirtualRoot)
//		{
//			resolvedPath = candidate;
//			pathKind = PathKind.File;
//			return false;
//		}

//		var combined = Path.Combine(basePath, candidate);
//		return TryResolveExisting(combined, out resolvedPath, out pathKind);
//	}

//	private static bool TryResolveExisting(string path, out string resolvedPath, out PathKind pathKind)
//	{
//		var cleaned = Path.TrimEndingDirectorySeparator(path);
//		var fullPath = Path.GetFullPath(cleaned);
//		if (Directory.Exists(fullPath))
//		{
//			resolvedPath = fullPath;
//			pathKind = PathKind.Directory;
//			return true;
//		}

//		if (File.Exists(fullPath))
//		{
//			resolvedPath = fullPath;
//			pathKind = PathKind.File;
//			return true;
//		}

//		resolvedPath = fullPath;
//		pathKind = PathKind.File;
//		return false;
//	}

//	private static string ToCurrentOsPath(string normalizedKey)
//		=> normalizedKey.Replace('/', Path.DirectorySeparatorChar);

//	private static string EnsureDirectorySuffix(string value)
//	{
//		if (value.EndsWith('/') || value.EndsWith('\\'))
//		{
//			return value;
//		}

//		var separator = value.Contains('/') ? '/' : Path.DirectorySeparatorChar;
//		return value + separator;
//	}

//	private static string GetPackageRelativePath(string fullPath)
//	{
//		var root = Path.GetPathRoot(fullPath);
//		var relative = string.IsNullOrEmpty(root)
//			? Path.GetFileName(fullPath)
//			: Path.GetRelativePath(root!, fullPath);

//		relative = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

//		if (!string.IsNullOrWhiteSpace(root))
//		{
//			var sanitizedRoot = root!
//				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
//				.Replace(':', '_')
//				.Replace('\\', '_')
//				.Replace('/', '_');

//			relative = Path.Combine(sanitizedRoot, relative ?? string.Empty);
//		}

//		return relative.Replace(':', '_').Replace('\\', '/');
//	}

//	private enum PathKind
//	{
//		File,
//		Directory
//	}
//}
