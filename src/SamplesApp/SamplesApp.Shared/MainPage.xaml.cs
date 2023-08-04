using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

			//sampleControl.DataContext = new SampleChooserViewModel(sampleControl);
		}

		private void myButton_Click(object sender, RoutedEventArgs e)
		{
			myButton.Content = "Clicked";

			var newWindow = new Window();
			newWindow.Content = new Button() { Content = "New window!" };
			newWindow.Activate();
		}
	}
}
