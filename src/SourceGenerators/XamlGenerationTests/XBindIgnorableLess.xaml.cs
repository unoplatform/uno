using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests.Shared
{
	public partial class XBindIgnorableLessUserControl : UserControl
	{
		public XBindIgnorableLessUserControl()
		{
		}

		public string Test { get; set; }
	}
}
