using SampleControl.Presentation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SamplesApp
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

			sampleControl.DataContext = new SampleChooserViewModel();

#if __MACOS__
			Content = new TextBlock() { Text= "Hello macOS!", FontSize = 72, Margin = new Thickness(12) };
#endif
		}
	}
}
