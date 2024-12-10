using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Markup
{
	public sealed partial class ProvideValueTargetProperty
	{
		public Type DeclaringType { get; set; }

		public string Name { get; set; }

		public Type Type { get; set; }
	}
}
