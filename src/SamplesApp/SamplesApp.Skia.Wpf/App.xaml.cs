using Uno.UI.Runtime.Skia.Wpf;

namespace SamplesApp.Wpf
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : System.Windows.Application
	{
		public App()
		{
			SamplesApp.App.ConfigureLogging();

			var host = new WpfHost(Dispatcher, () => new SamplesApp.App());

			host.Run();
		}
	}
}
