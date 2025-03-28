using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Windows_UI_Xaml_Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Template_Loading_DataContext_Page : Page
	{
		public Template_Loading_DataContext_Page()
		{
			this.InitializeComponent();
			DataContext = new ViewModel() { MyItemsSource = new[] { "Froot", "Veg" }, MyText = "Froot" };
		}

		private void AddBoundContentButton(object sender, object args)
		{
			SpawnedButtonHost.SpawnButton();
		}

		private class ViewModel : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public IEnumerable<string> MyItemsSource { get; set; }

			public string MyText
			{
				get { return _myText; }
				set
				{
					if (_myText != value)
					{
						_myText = value;
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MyText)));
					}
				}
			}
			string _myText;
		}
	}
}
