using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", IsManualTest = true)]
	public sealed partial class ListView_HorizontalScroll : UserControl
	{
		public MyListViewViewModel ViewModel { get; }

		public ListView_HorizontalScroll()
		{
			this.InitializeComponent();

			DataContext = ViewModel = new MyListViewViewModel();
		}
	}

	public class MyListViewViewModel : INotifyPropertyChanged
	{
		private List<MyListViewItem> items;

		public List<MyListViewItem> Items
		{
			get => items;
			set
			{
				if (items != value)
				{
					items = value;
					OnPropertyChanged(nameof(Items));
				}
			}
		}

		public MyListViewViewModel()
		{
			Items = new List<MyListViewItem>
			{
				new MyListViewItem() { Id = 1, Name = "Item A", Code = "A1", IsSelected = true, IsActive = true, Offset = 1 },
				new MyListViewItem() { Id = 2, Name = "Item B", Code = "B1", IsSelected = false, IsActive = true, Offset = 2 },
				new MyListViewItem() { Id = 3, Name = "Item C", Code = "C1", IsSelected = false, IsActive = true, Offset = 3 },
				new MyListViewItem() { Id = 4, Name = "Item D", Code = "D1", IsSelected = false, IsActive = true, Offset = 4 },
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class MyListViewItem
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Code { get; set; }
		public bool IsSelected { get; set; }
		public bool IsActive { get; set; }
		public int Offset { get; set; }

		public DateTime ItemDate => DateTime.UtcNow.AddHours(Offset);
	}
}
