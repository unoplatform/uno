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
			if (Windows.UI.Xaml.Window.Current.Bounds != default)
			{

			}
			sampleControl.DataContext = new SampleChooserViewModel(sampleControl);
		}
	}
}
