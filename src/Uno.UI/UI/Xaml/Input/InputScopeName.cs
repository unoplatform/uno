using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Input
{
	[ContentProperty(Name = nameof(NameValue))]
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
