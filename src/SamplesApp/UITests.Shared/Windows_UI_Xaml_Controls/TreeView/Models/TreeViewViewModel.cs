using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

using Uno;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Samples.UITests.Helpers;


namespace UITests.Shared.Windows_UI_Xaml_Controls.TreeView.Models
{
	[Bindable]
	public class Item
	{
		public string Name { get; }
		public List<Item> Children { get; }
		public Item(string name, List<Item> children)
		{
			Name = name;
			Children = children;
		}
	}

	public class TreeViewViewModel : ViewModelBase
	{
		public ObservableCollection<Item> Items = new ObservableCollection<Item>
		{
			new Item("Flavors", new List<Item>
			{
				new Item("Vanilla", null),
				new Item("Strawberry", null),
				new Item("Chocolate", null)
			}),
			new Item("Toppings", new List<Item>
			{
				new Item("Candy", new List<Item>
				{
					new Item("Chocolate", null),
					new Item("Mint", null),
					new Item("Sprinkles", null)
				}),
				new Item("Fruits", new List<Item>
				{
					new Item("Mango", null),
					new Item("Peach", null),
					new Item("Kiwi", null)
				}),
				new Item("Berries", new List<Item>
				{
					new Item("Strawberry", null),
					new Item("Blueberry", null),
					new Item("Blackberry",null)
				})
			})
		};

		public TreeViewViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

	}

}
