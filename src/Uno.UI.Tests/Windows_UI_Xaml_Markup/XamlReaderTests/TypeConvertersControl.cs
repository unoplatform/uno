using System;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public class TypeConvertersControl : FrameworkElement
	{
		public Type TypeProperty { get; set; }

		public Uri UriProperty { get; set; }
	}
}
