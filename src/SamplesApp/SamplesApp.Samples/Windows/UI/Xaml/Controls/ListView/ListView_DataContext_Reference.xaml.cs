using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample(
		"ListView",
		"ListView_DataContext_Reference",
		ViewModelType = typeof(DataContextReferenceViewModel),
		Description = "Display a ListView of items. Each row has a TextBox and a Click Button. \n" +
		"When Cliking on a row's button the item description will change into `Selected Item [Number]` where `Number` \n" +
		"is the row number of the item in question. This whole process should not fail.",
		IsManualTest = true)]
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
