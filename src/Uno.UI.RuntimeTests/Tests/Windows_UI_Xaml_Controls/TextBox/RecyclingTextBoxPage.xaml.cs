using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.RuntimeTests.TextBoxPages
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class RecyclingTextBoxPage : Page
	{
		public RecyclingTextBoxPage()
		{
			this.InitializeComponent();

			DataContext = new RecyclingTextBoxPageItemViewModel();
		}
	}

	public class RecyclingTextBoxPageItemViewModel : INotifyPropertyChanged
	{
		private string _text = Guid.NewGuid().ToString();

		public event PropertyChangedEventHandler PropertyChanged;

		public string Text
		{
			get => _text; 
			set
			{
				_text = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
			}
		}
	}
}
