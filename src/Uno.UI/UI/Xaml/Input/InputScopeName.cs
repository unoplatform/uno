using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Input
{
	public partial class InputScopeName
	{
		public InputScopeName()
		{

		}

		public InputScopeName(InputScopeNameValue value)
		{
			NameValue = value;
		}
		public InputScopeNameValue NameValue { get; set; }
	}
}
