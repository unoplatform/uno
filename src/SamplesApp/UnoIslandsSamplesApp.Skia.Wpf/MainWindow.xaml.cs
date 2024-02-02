using System.Windows;

namespace UnoIslandsSamplesApp.Skia.Wpf
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();
			xamlHost.Loaded += XamlHost_Loaded;
		}

		private async void XamlHost_Loaded(object sender, RoutedEventArgs e) =>
			await SamplesApp.App.HandleRuntimeTests(string.Join(';', System.Environment.GetCommandLineArgs()));
	}
}
