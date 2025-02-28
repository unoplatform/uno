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
	internal class XamlFileDefinition : IEquatable<XamlFileDefinition>, IComparable<XamlFileDefinition>
	{
		public XamlFileDefinition(string file, string targetFilePath, string content, ImmutableArray<byte> checksum)
		{
			Namespaces = new List<NamespaceDeclaration>();
			Objects = new List<XamlObjectDefinition>();
			FilePath = file;
			TargetFilePath = targetFilePath;
			Content = content;

			UniqueID = SanitizedFileName + "_" + HashBuilder.Build(FilePath);

			Checksum = string.Concat(checksum.Select(c => c.ToString("x2", CultureInfo.InvariantCulture)));
		}

		private string SanitizedFileName => Path
			.GetFileNameWithoutExtension(FilePath)
			.Replace(" ", "_")
			.Replace(".", "_");

		public List<NamespaceDeclaration> Namespaces { get; private set; }
		public List<XamlObjectDefinition> Objects { get; private set; }

		public string FilePath { get; }

		public string Checksum { get; }

		public string? SourceLink { get; internal set; }

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

		public override bool Equals(object? obj)
		{
			if (obj is XamlFileDefinition xfd)
			{
				return Equals(xfd);
			}

			return false;
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
