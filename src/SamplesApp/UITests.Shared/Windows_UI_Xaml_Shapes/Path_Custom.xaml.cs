using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace UITests.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes")]
	public sealed partial class Path_Custom : Page
	{
		public Path_Custom()
		{
			this.InitializeComponent();
		}
	}

	//#if __WASM__
	//	[Uno.UI.Runtime.WebAssembly.HtmlElement("svg")]
	//#endif
	public partial class MyPath : Path
	{

	}
}
