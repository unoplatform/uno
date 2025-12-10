#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Uno.UI.SourceGenerators.Helpers;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal sealed record class XamlFileDefinition : IEquatable<XamlFileDefinition>, IComparable<XamlFileDefinition>, IXamlLocation
	{
		public XamlFileDefinition(string file, string link, string targetFilePath, string content, ImmutableArray<byte> checksum)
		{
			FilePath = file;
			SourceLink = link;
			TargetFilePath = targetFilePath;
			Content = content;

			UniqueID = SanitizedFileName + "_" + HashBuilder.Build(FilePath);

			Checksum = string.Concat(checksum.Select(c => c.ToString("x2", CultureInfo.InvariantCulture)));
		}

		private string SanitizedFileName => Path
			.GetFileNameWithoutExtension(FilePath)
			.Replace(" ", "_")
			.Replace(".", "_");

		public List<NamespaceDeclaration> Namespaces { get; } = [];
		public List<XamlObjectDefinition> Objects { get; } = [];

		public string FilePath { get; }

		public int LineNumber => 1;

		public int LinePosition => 1;

		public string Checksum { get; }

		public string SourceLink { get; }

		/// <summary>
		/// Exception set by the parser if any error occurred during parsing
		/// </summary>
		public ImmutableArray<XamlParsingException> ParsingErrors { get; init; } = ImmutableArray<XamlParsingException>.Empty;

		/// <summary>
		/// Provides the path to the file using an actual target path in the project
		/// </summary>
		public string TargetFilePath { get; }

		/// <summary>
		/// The actual content of the file (XAML)
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Unique and human-readable file ID, used to name generated file.
		/// </summary>
		public string UniqueID { get; }

		public bool Equals(XamlFileDefinition? other)
		{
			if (other is null)
			{
				return false;
			}

			return ReferenceEquals(this, other)
				|| string.Equals(UniqueID, other.UniqueID, StringComparison.InvariantCultureIgnoreCase);
		}

		public override int GetHashCode() => UniqueID != null
			? StringComparer.InvariantCultureIgnoreCase.GetHashCode(UniqueID)
			: 0;

		public int CompareTo(XamlFileDefinition? other)
			=> ReferenceEquals(this, other)
				? 0
				: other is null ? 1 : string.Compare(FilePath, other.FilePath, StringComparison.InvariantCultureIgnoreCase);
	}
}
