using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnoIslandsSamplesApp.Skia.Wpf
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();
			xamlHost.Loaded += XamlHost_Loaded;
			xamlHost.Loaded += XamlHost_LoadedAsync;
		}

		private void XamlHost_Loaded(object sender, RoutedEventArgs e)
		{
#if HAS_UNO
			// Assertion should NOT be in async void.
			// DON'T move this to XamlHost_LoadedAsync.
			Assert.IsNotNull(xamlHost.Child.XamlRoot?.VisualTree.ContentRoot.CompositionContent, "ContentIsland of the ContentRoot should have been set by now.");
#endif
		}

		private async void XamlHost_LoadedAsync(object sender, RoutedEventArgs e)
		{
			await SamplesApp.App.HandleRuntimeTests(string.Join(";", System.Environment.GetCommandLineArgs()));
		}
	}
}
