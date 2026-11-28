#nullable enable

using System.IO;

namespace Windows.ApplicationModel;

partial class Package
{
	/// <summary>
	/// The path of a file of the app's content: under <paramref name="installRoot"/> when the file is there,
	/// otherwise under the first additional content root that has it.
	/// </summary>
	/// <param name="installRoot">The folder the content is installed in.</param>
	/// <param name="relativePath">The file's path relative to a content root.</param>
	/// <returns>The path of the file, or its path under <paramref name="installRoot"/> when no root has it.</returns>
	internal static string ResolveContentPath(string installRoot, string relativePath)
	{
		var installed = Path.Combine(installRoot, relativePath);

		// No file-system access unless a host declared additional roots.
		if (Uno.WinRTFeatureConfiguration.Package.AdditionalContentRoots.Count == 0 || File.Exists(installed))
		{
			return installed;
		}

		foreach (var root in Uno.WinRTFeatureConfiguration.Package.AdditionalContentRoots)
		{
			var candidate = Path.Combine(root, relativePath);
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		return installed;
	}
}
