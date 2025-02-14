using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Uno.Xaml;

namespace Windows.UI.Xaml.Markup.Reader
{
	internal class XamlFileDefinition
	{
		public XamlFileDefinition()
		{
			Namespaces = new List<NamespaceDeclaration>();
			Objects = new List<XamlObjectDefinition>();
		}

		public List<NamespaceDeclaration> Namespaces { get; private set; }

		public List<XamlObjectDefinition> Objects { get; private set; }
	}
}
