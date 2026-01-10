using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.Resources
{
	[Sample("XAML", "XamlGlobalResources", description: "Using a resource defined in Global.Xaml's Resources (Pink).")]
	public sealed partial class XamlGlobalResources : Page
	{
		public XamlGlobalResources()
		{
			this.InitializeComponent();
		}
	}
}
