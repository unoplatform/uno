using CheckBoxPointer.ViewModels;
using SampleControl.Presentation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SamplesApp
{
	public sealed partial class MainPage : Page
	{
		public LogViewModel Log { get; set; } = new LogViewModel();
		public MainPage()
		{
			this.InitializeComponent();
			this.Loaded += MainPage_Loaded;
		}

		private void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			Log.Init();
		}
	}
}
