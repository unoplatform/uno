using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml.ThemeResources
{
	[SampleControlInfo("XAML", controlName: nameof(AppXamlDefinedResources), description: "Validates the use of resources defined in App.xaml")]
	public sealed partial class AppXamlDefinedResources : UserControl
	{
		public AppXamlDefinedResources()
		{
			this.InitializeComponent();
		}
	}
}
