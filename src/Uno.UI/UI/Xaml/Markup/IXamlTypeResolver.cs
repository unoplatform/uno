using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Markup
{
	public partial interface IXamlTypeResolver
	{
		Type Resolve(string qualifiedTypeName);
	}
}
