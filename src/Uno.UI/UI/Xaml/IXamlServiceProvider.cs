using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	public partial interface IXamlServiceProvider
	{
		object GetService(Type type);
	}
}
