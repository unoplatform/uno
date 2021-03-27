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
		public Subclass(XamlMemberDefinition contentOwner, string returnType)
		{
			ContentOwner = contentOwner;
			ReturnType = returnType;
		}


		public XamlMemberDefinition ContentOwner { get;}

		public string ReturnType { get; }
	}
}
