using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace UnoIslands.Skia.Wpf
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private PersonViewModel _selectedItem = null;

		public MainWindowViewModel()
		{
			using var stream = typeof(MainWindowViewModel).Assembly.GetManifestResourceStream("UnoIslands.Skia.Wpf.TestData.json");
			using var textStream = new StreamReader(stream);
			Data = JsonConvert.DeserializeObject<PersonViewModel[]>(textStream.ReadToEnd());
		}

		public PersonViewModel[] Data { get; }

		public PersonViewModel SelectedItem
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
