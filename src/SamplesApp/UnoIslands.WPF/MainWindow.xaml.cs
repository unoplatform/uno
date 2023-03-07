using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Uno.UI.Skia.Platform;
using Uno.UI.XamlHost.Skia.Wpf;
using UnoIslands.Skia.Wpf;

namespace UnoIslands.WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		UnoXamlHost _host;

		public MainWindow()
		{
			InitializeComponent();

			DataContext = new MainWindowViewModel();

			_host = xamlHost;
		}

		private void contentLoaded_Checked(object sender, RoutedEventArgs e)
			=> ChangeLoadedState();

		private void contentLoaded_Unchecked(object sender, RoutedEventArgs e)
			=> ChangeLoadedState();

		private void ChangeLoadedState()
		{
			if (hostContainer is not null)
			{
				if (contentLoaded.IsChecked ?? false)
				{
					hostContainer.Content = _host;
				}
				else
				{
					hostContainer.Content = null;
				}
			}
		}
	}
}
