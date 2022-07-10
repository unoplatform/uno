using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Uno.UI.Skia.Platform;
using UnoIslands.Skia.Wpf;

namespace UnoIslands.WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private WpfHost _host;

		public MainWindow()
		{
			InitializeComponent();
			
			DataContext = new MainWindowViewModel();
		}
	}	
}
