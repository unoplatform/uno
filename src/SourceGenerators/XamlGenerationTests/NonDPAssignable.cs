using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace XamlGenerationTests.Shared
{
	public partial class NonDPAssignable : FrameworkElement
	{
		public Binding MyBinding { get; set; }

		public int MyProperty { get; set; }
	}
}
