using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Uno.Xaml;

namespace Windows.UI.Xaml.Markup.Reader
{
#if DEBUG
	[DebuggerDisplay("Type: {_xamlMember.Name} = {Value}")]
#endif
	internal class XamlMemberDefinition
	{
		private XamlMember _xamlMember;

		public XamlMemberDefinition(XamlMember xamlMember, int lineNumber, int linePosition, XamlObjectDefinition owner = null)
		{
			// TODO: Complete member initialization

			// If the DeclaringType is not properly resolved, we assume it should be the owner (xamlObject)
			if (xamlMember.DeclaringType == null
			&& owner != null
			&& xamlMember.PreferredXamlNamespace != XamlConstants.XamlXmlNamespace) // e.g., x:Class, x:Name
			{
				xamlMember = new XamlMember(xamlMember.Name, owner.Type, xamlMember.IsAttachable);
			}

			this._xamlMember = xamlMember;
			LineNumber = lineNumber;
			LinePosition = linePosition;
			Objects = new List<XamlObjectDefinition>();
			Owner = owner;
		}

		public XamlMember Member { get { return _xamlMember; } }

		public object Value { get; set; }

		public List<XamlObjectDefinition> Objects { get; private set; }

		public int LineNumber { get; private set; }

		public int LinePosition { get; set; }

		public XamlObjectDefinition Owner { get; }
	}
}
