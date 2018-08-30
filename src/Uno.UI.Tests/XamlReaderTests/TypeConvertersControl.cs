using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Uno.UI.Tests.XamlReaderTests
{
	public class TypeConvertersControl : FrameworkElement
	{
		public Type TypeProperty { get; set; }

		public Uri UriProperty { get; set; }
	}
}
