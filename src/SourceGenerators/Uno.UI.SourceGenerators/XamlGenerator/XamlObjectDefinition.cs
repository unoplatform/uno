#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
#if DEBUG
	[DebuggerDisplay("Type: {_type.Name}")]
#endif
    internal class XamlObjectDefinition
	{
		private XamlType _type;

		public XamlObjectDefinition(XamlType type, int lineNumber, int linePosition, XamlObjectDefinition? owner)
		{
			LineNumber = lineNumber;
			LinePosition = linePosition;
			_type = type;
			Owner = owner;
			Members = new List<XamlMemberDefinition>();
			Objects = new List<XamlObjectDefinition>();
		}

		public XamlType Type { get { return _type; } }

		public List<XamlMemberDefinition> Members { get; private set; }

		public List<XamlObjectDefinition> Objects { get; private set; }

		public object? Value { get; set; }

		public int LineNumber { get; private set; }

		public int LinePosition { get; set; }

		public XamlObjectDefinition? Owner { get; private set; }
	}

}
