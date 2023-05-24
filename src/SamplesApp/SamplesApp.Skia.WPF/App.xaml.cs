using Uno.UI.Skia;

namespace SamplesApp.WPF
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : System.Windows.Application
	{
		public App()
		{
			SamplesApp.App.ConfigureFilters();

			var host = new WpfHost(Dispatcher, () => new SamplesApp.App());

			host.Run();
		}
	}
}
