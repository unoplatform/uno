using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", IsManualTest = true)]
	public sealed partial class ListView_Focus : UserControl
	{
		public ListViewFocusViewModel ViewModel { get; }

		public ListView_Focus()
		{
			this.InitializeComponent();

			DataContext = ViewModel = new ListViewFocusViewModel();
			for (int i = 0; i < 3; i++)
			{
				var item = new ListViewFocusViewModelItem { Label = $"Test{i}" };
				ViewModel.Items.Add(item);
			}
			ViewModel.SelectedItem = ViewModel.Items[0];
		}
	}

	public class ListViewFocusViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public ListViewFocusViewModelItem First => Items.FirstOrDefault();

		public ObservableCollection<ListViewFocusViewModelItem> Items { get; } = new ObservableCollection<ListViewFocusViewModelItem>();

		public bool IsLoaded
		{
			get { return _isLoaded; }
			set
			{
				if (_isLoaded != value)
				{
					_isLoaded = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoaded)));
				}
			}
		}
		bool _isLoaded = false;

		public bool IsUserControlOpen
		{
			get { return _isUserControlOpen; }
			set
			{
				if (_isUserControlOpen != value)
				{
					_isUserControlOpen = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUserControlOpen)));
				}
			}
		}
		bool _isUserControlOpen = false;

		public ListViewFocusViewModelItem SelectedItem
		{
			get { return _selectedItem; }
			set
			{
				if (_selectedItem != value)
				{
					_selectedItem = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));

					if (value == null)
						ShowMessage();
				}
			}
		}
		ListViewFocusViewModelItem _selectedItem;

		private async void ShowMessage()
		{
			await new Windows.UI.Popups.MessageDialog("Null value set on TestViewModel").ShowAsync();
		}
	}

	public class ListViewFocusViewModelItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public DateTimeOffset Date
		{
			get { return _date; }
			set
			{
				if (value != _date)
				{
					_date = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Date)));
				}
			}
		}
		DateTimeOffset _date = DateTimeOffset.Now;

		public bool IsExpanded
		{
			get { return _isExpanded; }
			set
			{
				if (_isExpanded != value)
				{
					_isExpanded = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));

					if (value && Items.Count == 1)
					{
						Items.Clear();
						for (int i = 0; i < 3; i++)
						{
							var item = new ListViewFocusViewModelItem { Label = $"Test{i}" };
							item.Items.Add(new ListViewFocusViewModelItem { Label = "Loading..." });
							Items.Add(item);
						}
					}
				}
			}
		}
		bool _isExpanded = false;

		public ObservableCollection<ListViewFocusViewModelItem> Items { get; } = new ObservableCollection<ListViewFocusViewModelItem>();

		public string Label
		{
			get { return _label; }
			set
			{
				if (_label != value)
				{
					_label = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
				}
			}
		}
		string _label;

		public string Test1
		{
			get { return _test1; }
			set
			{
				if (_test1 != value)
				{
					_test1 = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Test1)));
				}
			}
		}
		string _test1;

		public string Test2
		{
			get { return _test2; }
			set
			{
				if (_test2 != value)
				{
					_test2 = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Test2)));
				}
			}
		}
		string _test2;
	}
}
