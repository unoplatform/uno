using System;
using System.IO;

namespace Uno.UI.Tasks.ResourcesGenerator;

/// <summary>
/// Resolves the authored, project-relative path of a resource item, from which MRT
/// qualifiers (language, scale, ...) can be extracted.
/// </summary>
internal static class ResourceQualifierPathResolver
{
	/// <summary>
	/// Gets the path to parse qualifiers from, given a resource item's metadata.
	/// </summary>
	/// <remarks>
	/// Qualifiers may only be read from path segments the developer authored. Parsing an absolute
	/// path lets directories above the project contribute qualifiers, so a project sitting in a
	/// folder named `OS` (Ossetic) or `cs` (Czech) has that folder detected as its language.
	/// </remarks>
	public static string Resolve(
		string itemSpec,
		string link,
		string targetPath,
		string definingProjectDirectory,
		string projectDirectory)
	{
		if (!string.IsNullOrEmpty(link))
		{
			return link;
		}

		if (string.IsNullOrEmpty(itemSpec) || !Path.IsPathRooted(itemSpec))
		{
			// A relative item spec is authored as-is and already relative to the project.
			return itemSpec;
		}

		if (TryMakeRelative(itemSpec, definingProjectDirectory, out var relativePath)
			|| TryMakeRelative(itemSpec, projectDirectory, out relativePath))
		{
			return relativePath;
		}

		// AssignTargetPath collapses items living outside the project cone to their bare file name,
		// which carries no qualifier. Only trust TargetPath when it kept some directory structure.
		if (!string.IsNullOrEmpty(targetPath) && !string.IsNullOrEmpty(Path.GetDirectoryName(targetPath)))
		{
			return targetPath;
		}

		// No project-relative context available, parsing the full path is the best we can do.
		return itemSpec;
	}

	private static bool TryMakeRelative(string fullPath, string directory, out string relativePath)
	{
		if (!string.IsNullOrEmpty(directory))
		{
			var lastChar = directory[directory.Length - 1];
			var root = lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar
				? directory
				: directory + Path.DirectorySeparatorChar;

			if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
			{
				relativePath = fullPath.Substring(root.Length);
				return true;
			}
		}

		relativePath = null;
		return false;
	}
}
