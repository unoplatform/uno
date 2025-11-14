#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
#if DEBUG
	[DebuggerDisplay("Type: {Type.Name}")]
#endif
	internal sealed record XamlObjectDefinition : IXamlLocation
	{
		/// <summary>
		/// Creates a root object
		/// </summary>
		public XamlObjectDefinition(XamlXmlReader reader, XamlFileDefinition file)
		{
			FilePath = file.FilePath;
			LineNumber = reader.LineNumber;
			LinePosition = reader.LinePosition;
			Type = reader.Type;
		}

		/// <summary>
		/// Creates a child object
		/// </summary>
		public XamlObjectDefinition(XamlXmlReader reader, XamlObjectDefinition owner, List<NamespaceDeclaration>? namespaces = null)
		{
			FilePath = owner.FilePath;
			LineNumber = reader.LineNumber;
			LinePosition = reader.LinePosition;
			Type = reader.Type;
			Namespaces = namespaces;
		}

		/// <summary>
		/// Creates a virtual object definition (i.e. not from XamlXmlReader)
		/// </summary>
		public XamlObjectDefinition(XamlType type, XamlObjectDefinition owner)
		{
			FilePath = owner.FilePath;
			LineNumber = owner.LineNumber;
			LinePosition = owner.LinePosition;
			Type = type;
			Owner = owner;
		}

		public XamlType Type { get; init; }

		public List<XamlMemberDefinition> Members { get; } = [];

		public List<XamlObjectDefinition> Objects { get; } = [];

		public object? Value { get; set; }

		public string FilePath { get; }

		public int LineNumber { get; }

		public int LinePosition { get; set; }

		public XamlObjectDefinition? Owner { get; }

		public List<NamespaceDeclaration>? Namespaces { get; }

		private string? _key;
		public string Key => _key ??= BuildKey();

		private string BuildKey()
		{
			var owner = Owner;
			if (owner is null)
			{
				return $"__{NamingHelper.GetShortName(Type.Name)}";
			}

			var ownerMember = owner
				.Members
				.Select(member => (value: member, idx: member.Objects.IndexOf(this)))
				.FirstOrDefault(member => member.idx >= 0);
			if (ownerMember is { value: not null })
			{
				return $"{ownerMember.value.Key}Ξ{ownerMember.idx}_{NamingHelper.GetShortName(Type.Name)}";
			}

			var ownerObjectIdx = owner.Objects.IndexOf(this);
			if (ownerObjectIdx >= 0)
			{
				return $"{owner.Key}Λ{ownerObjectIdx}_{NamingHelper.GetShortName(Type.Name)}";
			}

			return $"{owner.Key}_ø_{NamingHelper.GetShortName(Type.Name)}"; // Should not happen
		}
	}
}
