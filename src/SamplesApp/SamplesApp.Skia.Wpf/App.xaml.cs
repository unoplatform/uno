using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Skia.Wpf;

namespace SamplesApp.Wpf
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : System.Windows.Application
	{
		private SamplesApp.App _app;

		public App()
		{
			SamplesApp.App.ConfigureLogging();

			var host = new WpfHost(Dispatcher, () => _app ??= new SamplesApp.App());

			host.Run();

			_app.MainWindowActivated += OnMainWindowActivated;
		}

		private void OnMainWindowActivated(object sender, System.EventArgs e)
		{
			var windowContent = Application.Current.Windows[0].Content;
			Assert.IsInstanceOfType(windowContent, typeof(System.Windows.UIElement));
			var windowContentAsUIElement = (System.Windows.UIElement)windowContent;
			Assert.IsTrue(windowContentAsUIElement.IsFocused);
		}
	}
}
