using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Input
{
	public partial class InputScope
	{
		public InputScope()
		{
			Names = new List<InputScopeName>();
		}

		public IList<InputScopeName> Names { get; }

		internal InputScopeNameValue GetFirstInputScopeNameValue()
			=> Names.FirstOrDefault()?.NameValue ?? InputScopeNameValue.Default;
	}
}
