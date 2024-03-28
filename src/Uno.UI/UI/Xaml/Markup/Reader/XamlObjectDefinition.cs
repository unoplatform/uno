using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Xaml;

namespace Windows.UI.Xaml.Markup.Reader
{
#if DEBUG
	[DebuggerDisplay("Type: {Type.Name}")]
#endif
	internal class XamlObjectDefinition
	{
		public XamlObjectDefinition(XamlType type, int lineNumber, int linePosition, XamlObjectDefinition owner)
		{
			LineNumber = lineNumber;
			LinePosition = linePosition;
			Type = type;
			Owner = owner;
			Members = new List<XamlMemberDefinition>();
			Objects = new List<XamlObjectDefinition>();
		}

		public XamlType Type { get; }

		public List<XamlMemberDefinition> Members { get; }

		public List<XamlObjectDefinition> Objects { get; }

		public object Value { get; set; }

		public int LineNumber { get; }

		public int LinePosition { get; set; }

		public XamlObjectDefinition Owner { get; }
	}

}
