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
	[DebuggerDisplay("Type: {_xamlMember.Name} = {Value}")]
#endif
	internal class XamlMemberDefinition : IXamlLocation
	{
		private XamlMember _xamlMember;

		public XamlMemberDefinition(XamlMember xamlMember, int lineNumber, int linePosition, XamlObjectDefinition? owner = null)
		{
			// TODO: Complete member initialization

			// If the DeclaringType is not properly resolved, we assume it should be the owner (xamlObject)
			if (xamlMember.DeclaringType == null
				&& owner != null
				&& xamlMember.PreferredXamlNamespace != XamlConstants.XamlXmlNamespace) // e.g., x:Class, x:Name
			{
				xamlMember = XamlMember.WithDeclaringType(xamlMember, owner.Type)
					?? throw new Exception($"Unable to find {xamlMember} on {owner.Type}");
			}

			_xamlMember = xamlMember;
			LineNumber = lineNumber;
			LinePosition = linePosition;
			Objects = new List<XamlObjectDefinition>();
			Owner = owner;
		}

		public XamlMember Member => _xamlMember;

		public object? Value { get; set; }

		public List<XamlObjectDefinition> Objects { get; private set; }

		public int LineNumber { get; private set; }

		public int LinePosition { get; set; }

		public XamlObjectDefinition? Owner { get; }

		public string Key => Member.Name switch
		{
			"_UnknownContent" => Owner?.Key ?? "__",
			var name => $"{Owner?.Key ?? "_"}_{NamingHelper.GetShortName(name)}"
		};
	}
}
