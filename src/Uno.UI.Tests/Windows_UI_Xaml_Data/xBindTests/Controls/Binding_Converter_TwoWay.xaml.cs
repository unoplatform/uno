using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Binding_Converter_TwoWay : Page
	{
		public TestViewModel ViewModel { get; }

		public Binding_Converter_TwoWay()
		{
			this.InitializeComponent();

			DataContext = ViewModel = new TestViewModel() { ShowImages = true };

			//Loaded += MainPage_Loaded;
		}

		//private void MainPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		//{
		//}

		public class TestViewModel : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public bool ShowImages
			{
				get { return _showImages; }
				set
				{
					if (_showImages != value)
					{
						_showImages = value;
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowImages)));
					}
				}
			}
			bool _showImages = false;
		}
	}
}
