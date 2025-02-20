using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.UITests.Helpers;
using Uno.UI.Common;
using System.Windows.Input;
using Windows.UI.Core;

using ICommand = System.Windows.Input.ICommand;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	internal class ContentControlTestViewModel : ViewModelBase
	{
		public string Property1 { get; set; } = "Property1";
		public string Property2 { get; set; } = "Property2";
		public string Property3 { get; set; } = "Property3";
		public string Property4 { get; set; } = "Property4";

		public ContentControlTestViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			SetChangeablePropertyToNull = new DelegateCommand(() => ChangeableProperty = null);

			FillItems();
		}

		public TestDataItem SampleData { get; set; } = new TestDataItem()
		{
			UpperString = "This string goes above - databound",
			LowerString = "This string goes below - databound"
		};

		private string _changeableProperty;

		public string ChangeableProperty
		{
			get { return _changeableProperty; }
			set { _changeableProperty = value; RaisePropertyChanged(); }
		}

		public ICommand SetChangeablePropertyToNull { get; }


		public class TestDataItem
		{
			public string UpperString { get; set; }
			public string LowerString { get; set; }

			public override string ToString()
			{
				return "TestDataItem: " + base.ToString();
			}
		}


		public sealed class Item
		{
			public string DisplayName { get; set; }
		}

		List<Item> _items = new List<Item>();
		public IEnumerable<Item> Items { get => _items; }

		private int _selectedIndex;
		public int SelectedIndex
		{
			get { return _selectedIndex; }
			set
			{
				if (_selectedIndex != value)
				{
					_selectedIndex = value;
					RaisePropertyChanged();
				}
			}
		}

		private void FillItems()
		{
			_items.Add(new Item { DisplayName = "Test1" });
			_items.Add(new Item { DisplayName = "Test2" });
			_items.Add(new Item { DisplayName = "Test3" });
		}
	}
}
