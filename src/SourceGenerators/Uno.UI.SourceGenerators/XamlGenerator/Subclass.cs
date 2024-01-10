#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class Subclass
	{
		public Subclass(XamlMemberDefinition contentOwner, string returnType, string defaultBindMode, IXamlLocation? location)
		{
			ContentOwner = contentOwner;
			ReturnType = returnType;
			DefaultBindMode = defaultBindMode;
			Location = location;
		}

		public XamlMemberDefinition ContentOwner { get; }

		public string ReturnType { get; }

		public string DefaultBindMode { get; }

		public IXamlLocation? Location { get; }
	}
}
