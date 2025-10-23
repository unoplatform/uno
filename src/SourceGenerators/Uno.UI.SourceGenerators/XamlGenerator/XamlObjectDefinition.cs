﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
#if DEBUG
	[DebuggerDisplay("Type: {_type.Name}")]
#endif
	internal class XamlObjectDefinition : IXamlLocation
	{
		private XamlType _type;

		public XamlObjectDefinition(XamlType type, int lineNumber, int linePosition, XamlObjectDefinition? owner, List<NamespaceDeclaration>? namespaces)
		{
			LineNumber = lineNumber;
			LinePosition = linePosition;
			_type = type;
			Owner = owner;
			Members = new List<XamlMemberDefinition>();
			Objects = new List<XamlObjectDefinition>();
			Namespaces = namespaces;
		}

		public XamlType Type { get { return _type; } }

		public List<XamlMemberDefinition> Members { get; private set; }

		public List<XamlObjectDefinition> Objects { get; private set; }

		public object? Value { get; set; }

		public int LineNumber { get; private set; }

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
