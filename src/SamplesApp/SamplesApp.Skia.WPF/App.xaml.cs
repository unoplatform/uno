using Uno.UI.Skia;

namespace SamplesApp.WPF
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : System.Windows.Application
	{
		private readonly WpfHost _host;

		public App()
		{
			SamplesApp.App.ConfigureFilters();

			_host = new WpfHost(Dispatcher, () => new SamplesApp.App());
		}
	}
}
