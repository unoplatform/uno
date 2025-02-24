using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView;

[SampleControlInfo("ListView", "ListView_DeleteItem", isManualTest: true, description: "Tests that deleting an item from a ListView does not display weird information while the item is being removed.")]
public sealed partial class ListView_DeleteItem : UserControl
{
	private ViewModel _vm;

	public ListView_DeleteItem()
	{
		this.InitializeComponent();

		_vm = new ViewModel();

		DataContext = _vm;
	}

	private void Delete(object sender, RoutedEventArgs e)
	{
		_vm.Delete();
	}
}

public class ViewModel : INotifyPropertyChanged
{
	public ViewModel()
	{
		Items = new ObservableCollection<ListViewItem>();

		for (var i = 0; i < 10; i++)
		{
			Items.Add(new ListViewItem("Mock Item", $"#{i}"));
		}
	}

	public void Delete()
	{
		if (SelectedItem is { } selectedItem)
		{
			Items.Remove(selectedItem);
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged(string propertyName)
	=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public ListViewItem SelectedItem { get; set; }

	public ObservableCollection<ListViewItem> Items { get; }

	public class ListViewItem
	{
		public ListViewItem(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public string Name { get; }

		public string Value { get; }
	}
}
