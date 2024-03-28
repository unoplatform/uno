using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared
{
	public partial class XBindUserControl : UserControl
	{
		public XBindUserControl()
		{
		}

		public string Test { get; set; }

		public MyType TypeProperty { get; set; }

		public XBindViewModel ViewModel => new XBindViewModel();
	}

	public class MyType
	{
		public string Value { get; set; }
	}
}
