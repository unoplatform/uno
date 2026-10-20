using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Resources.Core;

public partial class ResourceCandidate
{
	private static readonly char[] _dotArray = new[] { '.' };

	internal ResourceCandidate(IReadOnlyList<ResourceQualifier> qualifiers, string valueAsString, string logicalPath)
	{
		Qualifiers = qualifiers;
		ValueAsString = valueAsString;
		LogicalPath = logicalPath;
	}

	public IReadOnlyList<ResourceQualifier> Qualifiers { get; }

	public string ValueAsString { get; }

	internal string LogicalPath { get; }

	public string GetQualifierValue(string qualifierName)
	{
		return Qualifiers.FirstOrDefault(qualifier => qualifier.QualifierName == qualifierName)?.QualifierValue;
	}

	internal static ResourceCandidate Parse(string fullPath, string relativePath)
	{
		var logicalPath = GetLogicalPath(relativePath);

		var directoryPart = Path.GetDirectoryName(relativePath) ?? string.Empty;
		var fileName = Path.GetFileName(relativePath);

		// MRT: bare BCP-47 language tags (e.g. `en-US`) are allowed only
		// as folder names. File-name segments must use the explicit
		// `lang-` / `language-` prefix. See ResourceQualifier.Parse.
		var folderQualifiers = directoryPart
			.Split(Path.DirectorySeparatorChar, '_')
			.Select(s => ResourceQualifier.Parse(s, allowBareLanguageTag: true));

		var fileQualifiers = fileName
			.Split('_', '.')
			.Select(s => ResourceQualifier.Parse(s, allowBareLanguageTag: false));

		var qualifiers = folderQualifiers
			.Concat(fileQualifiers)
			.Reverse()
			.Where(p => p != null)
			.ToArray();

		return new ResourceCandidate(qualifiers, fullPath, logicalPath);
	}

	private static string GetLogicalPath(string path)
	{
		var directoryNameWithoutQualifiers = (Path.GetDirectoryName(path) ?? string.Empty)
			.Split(new[] { Path.DirectorySeparatorChar })
			.Where(x => ResourceQualifier.Parse(x, allowBareLanguageTag: true) == null)
			.ToArray();

		var fileNameWithoutQualifiers = Path
			.GetFileName(path)
			.Split(_dotArray)
			.Where(x => ResourceQualifier.Parse(x, allowBareLanguageTag: false) == null);

		return Path.Combine(Path.Combine(directoryNameWithoutQualifiers), string.Join(".", fileNameWithoutQualifiers));
	}
}
