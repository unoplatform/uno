using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public partial class NonDependencyPropertyAssignable : FrameworkElement
	{
		public int MyProperty { get; set; }

		public Binding MyBinding { get; set; }
	}
}
