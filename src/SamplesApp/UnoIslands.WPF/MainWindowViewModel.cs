using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace UnoIslands.Skia.Wpf
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private DataItem _selectedItem = null;

		public MainWindowViewModel()
		{
			using var stream = typeof(MainWindowViewModel).Assembly.GetManifestResourceStream("UnoIslands.Skia.Wpf.TestData.json");
			using var textStream = new StreamReader(stream);
			Data = JsonConvert.DeserializeObject<DataItem[]>(textStream.ReadToEnd());
		}

		public DataItem[] Data { get; }

		public DataItem SelectedItem
		{
			get => _selectedItem;
			set
			{
				_selectedItem = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
