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
		}

		private async void XamlHost_Loaded(object sender, RoutedEventArgs e)
		{
#if HAS_UNO
			Assert.IsNotNull(xamlHost.Child.XamlRoot?.VisualTree.ContentRoot.CompositionContent, "ContentIsland of the ContentRoot should have been set by now.");
#endif
			await SamplesApp.App.HandleRuntimeTests(string.Join(";", System.Environment.GetCommandLineArgs()));
		}
	}
}
