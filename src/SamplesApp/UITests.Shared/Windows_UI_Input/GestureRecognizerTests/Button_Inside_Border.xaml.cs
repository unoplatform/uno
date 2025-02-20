using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Collections.ObjectModel;

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[Sample("Gesture Recognizer", Name = "Button_Inside_Border")]
	public sealed partial class Button_Inside_Border : UserControl
	{
		public ObservableCollection<DataRow> YourDataCollection { get; set; } = new ObservableCollection<DataRow>();
		private int _rows = 1;

		public Button_Inside_Border()
		{
			this.InitializeComponent();
			listView.ItemsSource = YourDataCollection;
		}

		private void Border_Tapped(object sender, RoutedEventArgs e) => YourDataCollection.Add(new DataRow { RowNumber = _rows++, CallbackName = nameof(Border_Tapped), ElementName = sender.GetType().Name, EventArgsHashCode = e.GetHashCode().ToString("X8"), OriginalSource = e.OriginalSource.GetType().Name });

		private void Button_Tapped(object sender, TappedRoutedEventArgs e) => YourDataCollection.Add(new DataRow { RowNumber = _rows++, CallbackName = nameof(Button_Tapped), ElementName = sender.GetType().Name, EventArgsHashCode = e.GetHashCode().ToString("X8"), OriginalSource = e.OriginalSource.GetType().Name });

		private void Border_RightTapped(object sender, RoutedEventArgs e) => YourDataCollection.Add(new DataRow { RowNumber = _rows++, CallbackName = nameof(Border_RightTapped), ElementName = sender.GetType().Name, EventArgsHashCode = e.GetHashCode().ToString("X8"), OriginalSource = e.OriginalSource.GetType().Name });

		private void Button_RightTapped(object sender, RoutedEventArgs e) => YourDataCollection.Add(new DataRow { RowNumber = _rows++, CallbackName = nameof(Button_RightTapped), ElementName = sender.GetType().Name, EventArgsHashCode = e.GetHashCode().ToString("X8"), OriginalSource = e.OriginalSource.GetType().Name });

		private void Border_Holding(object sender, RoutedEventArgs e) => YourDataCollection.Add(new DataRow { RowNumber = _rows++, CallbackName = nameof(Border_Holding), ElementName = sender.GetType().Name, EventArgsHashCode = e.GetHashCode().ToString("X8"), OriginalSource = e.OriginalSource.GetType().Name });

		private void Button_Holding(object sender, RoutedEventArgs e) => YourDataCollection.Add(new DataRow { RowNumber = _rows++, CallbackName = nameof(Button_Holding), ElementName = sender.GetType().Name, EventArgsHashCode = e.GetHashCode().ToString("X8"), OriginalSource = e.OriginalSource.GetType().Name });

		public class DataRow
		{
			public int RowNumber { get; set; }
			public string CallbackName { get; set; }
			public string ElementName { get; set; }
			public string EventArgsHashCode { get; set; }
			public string OriginalSource { get; set; }
		}
	}
}
