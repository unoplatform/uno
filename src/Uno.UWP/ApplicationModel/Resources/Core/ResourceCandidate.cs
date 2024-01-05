#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Resources.Core
{
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

		public string? GetQualifierValue(string qualifierName)
		{
			return Qualifiers.FirstOrDefault(qualifier => qualifier.QualifierName == qualifierName)?.QualifierValue;
		}

		internal static ResourceCandidate Parse(string fullPath, string relativePath)
		{
			var logicalPath = GetLogicalPath(relativePath);

			ResourceQualifier[] qualifiers = relativePath
				.Split(Path.DirectorySeparatorChar, '_', '.')
				.Select(ResourceQualifier.Parse)
				.Reverse()
				.Where(p => p != null)
				.ToArray()!;

			return new ResourceCandidate(qualifiers, fullPath, logicalPath);
		}

		private static string GetLogicalPath(string path)
		{
			var directoryNameWithoutQualifiers = Path
				.GetDirectoryName(path)!
				.Split(new[] { Path.DirectorySeparatorChar })
				.Where(x => ResourceQualifier.Parse(x) == null)
				.ToArray();

			var fileNameWithoutQualifiers = Path
				.GetFileName(path)
				.Split(_dotArray)
				.Where(x => ResourceQualifier.Parse(x) == null);

			return Path.Combine(Path.Combine(directoryNameWithoutQualifiers), string.Join(".", fileNameWithoutQualifiers));
		}
	}
}
