using System;
using System.Collections.Generic;
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
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_DataContext_Reference", viewModelType: typeof(DataContextReferenceViewModel), isManualTest: true)]
	public sealed partial class ListView_DataContext_Reference : UserControl
	{
		public ListView_DataContext_Reference()
		{
			this.InitializeComponent();
		}
	}

	public class DataContextReferenceViewModel
	{
		public ObservableCollection<Item> Items { get; } =
			new ObservableCollection<Item>(Enumerable.Range(0, 10).Select(x => new Item(x)));

		private ICommand _command;
		public ICommand Command
		{
			get
			{
				_command ??= new Command<Item>(ExecuteCommand);
				return _command;
			}
		}

		private void ExecuteCommand(Item item)
		{
			if (item is null)
			{
				Console.WriteLine("Item was null");
				return;
			}

			var newItem = item with { Name = $"Selected item {item.Index}" };

			Items.Remove(item);
			Items.Insert(newItem.Index, newItem);
		}
	}

	public record Item
	{
		public Item(int index)
		{
			Index = index;
			Name = $"Item {index}";
		}

		public int Index { get; init; }

		public string Name { get; init; }
	}

	public class Command<T> : ICommand
	{
		private readonly Action<T> _act;

		public Command(Action<T> act)
		{
			_act = act;
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter) => true;

		public void Execute(object parameter)
		{
			_act?.Invoke((T)parameter!);
		}

		public void UpdateCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
