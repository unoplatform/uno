using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

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
