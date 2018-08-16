using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.XamlReaderTests
{
	public partial class NonDependencyPropertyAssignable : FrameworkElement
	{
		public int MyProperty { get; set; }

		public Binding MyBinding { get; set; }
	}
}
