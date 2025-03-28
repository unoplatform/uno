using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.Resources
{
	[SampleControlInfo("XAML", "XamlGlobalResources", description: "Using a resource defined in Global.Xaml's Resources (Pink).")]
	public sealed partial class XamlGlobalResources : Page
	{
		public XamlGlobalResources()
		{
			this.InitializeComponent();
		}
	}
}
