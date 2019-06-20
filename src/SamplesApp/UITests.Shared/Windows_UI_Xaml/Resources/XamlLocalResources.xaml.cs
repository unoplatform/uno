using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.Resources
{
	public sealed partial class XamlLocalResources : Page
	{
		public XamlLocalResources()
		{
			this.InitializeComponent();
		}
	}

	internal class CustomTemplateSelector : DataTemplateSelector
	{

	}
}
