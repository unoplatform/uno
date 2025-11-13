#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
#if DEBUG
	[DebuggerDisplay("Type: {Member.Name} = {Value}")]
#endif
	internal sealed class XamlMemberDefinition : IXamlLocation
	{
		public XamlMemberDefinition(XamlMember xamlMember, int lineNumber, int linePosition, XamlObjectDefinition owner)
		{
			// TODO: Complete member initialization

			// If the DeclaringType is not properly resolved, we assume it should be the owner (xamlObject)
			if (xamlMember.DeclaringType == null
				&& xamlMember.PreferredXamlNamespace != XamlConstants.XamlXmlNamespace) // e.g., x:Class, x:Name
			{
				xamlMember = XamlMember.WithDeclaringType(xamlMember, owner.Type)
					?? throw new Exception($"Unable to find {xamlMember} on {owner.Type}");
			}

			Member = xamlMember;
			LineNumber = lineNumber;
			LinePosition = linePosition;
			Owner = owner;
		}

		public XamlMember Member { get; }

		public object? Value { get; set; }

		public List<XamlObjectDefinition> Objects { get; } = [];

		public int LineNumber { get; }

		public int LinePosition { get; }

		public XamlObjectDefinition Owner { get; }

		public string Key => Member.Name switch
		{
			"_UnknownContent" => Owner?.Key ?? "__",
			var name => $"{Owner?.Key ?? "_"}_{NamingHelper.GetShortName(name)}"
		};
	}
}
