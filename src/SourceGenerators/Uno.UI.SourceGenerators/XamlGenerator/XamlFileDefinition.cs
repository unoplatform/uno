#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Uno.UI.SourceGenerators.Helpers;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class XamlFileDefinition : IEquatable<XamlFileDefinition>
	{
		public XamlFileDefinition(string file, string targetFilePath)
		{
			Namespaces = new List<NamespaceDeclaration>();
			Objects = new List<XamlObjectDefinition>();
			FilePath = file;
			TargetFilePath = targetFilePath;

			UniqueID = SanitizedFileName + "_" + HashBuilder.Build(FilePath);
		}

		private string SanitizedFileName => Path
			.GetFileNameWithoutExtension(FilePath)
			.Replace(" ", "_")
			.Replace(".", "_");

		public List<NamespaceDeclaration> Namespaces { get; private set; }
		public List<XamlObjectDefinition> Objects { get; private set; }

		public string FilePath { get; private set; }

		public string? SourceLink { get; internal set; }

		/// <summary>
		/// Provides the path to the file using an actual target path in the project
		/// </summary>
		public string TargetFilePath { get; }

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
				return ReferenceEquals(this, xfd)
					|| string.Equals(UniqueID, xfd.UniqueID, StringComparison.InvariantCultureIgnoreCase);
			}

			return false;
		}

		public override int GetHashCode() => (UniqueID != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(UniqueID) : 0);
	}
}
