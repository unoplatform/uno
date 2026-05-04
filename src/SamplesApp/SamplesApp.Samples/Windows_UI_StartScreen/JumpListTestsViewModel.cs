using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using UITests.Shared.Helpers;
using Uno.UI.Common;
using Uno.UI.Samples.UITests.Helpers;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace UITests.Shared.Windows_UI_StartScreen
{
	internal class JumpListTestsViewModel : ViewModelBase
	{
		private JumpList _jumpList;
		private JumpListItem _selectedItem;
		private ObservableCollection<JumpListItem> _items = new ObservableCollection<JumpListItem>();
		private NewJumpListItem _newItem = new NewJumpListItem();

		public JumpListTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			IsSupported = JumpList.IsSupported();
		}

		public bool IsSupported { get; }

		public bool IsLoaded => _jumpList != null;

		public bool ItemSelected => SelectedItem != null;

		public ObservableCollection<JumpListItem> Items
		{
			get => _items;
			private set
			{
				_items = value;
				RaisePropertyChanged();
			}
		}

		public JumpListItem SelectedItem
		{
			get => _selectedItem;
			set
			{
				_selectedItem = value;
				RaisePropertyChanged(nameof(ItemSelected));
			}
		}

		public NewJumpListItem NewItem
		{
			get => _newItem;
			set
			{
				_newItem = value;
				RaisePropertyChanged(nameof(NewItem));
			}
		}

		public ICommand AddItemCommand => GetOrCreateCommand(AddItemAsync);

		private async void AddItemAsync()
		{
			var item = JumpListItem.CreateWithArguments(NewItem.Arguments, NewItem.DisplayName);
			item.Description = NewItem.Description;
			item.Logo = new Uri(NewItem.Logo);
			_jumpList.Items.Add(item);
			NewItem = new NewJumpListItem();
			await _jumpList.SaveAsync();
			RefreshItems();
		}

		public ICommand RemoveItemCommand => GetOrCreateCommand(RemoveItemAsync);

		private async void RemoveItemAsync()
		{
			_jumpList.Items.Remove(SelectedItem);
			SelectedItem = null;
			await _jumpList.SaveAsync();
			RefreshItems();
		}

		public ICommand LoadCurrentCommand => GetOrCreateCommand(LoadCurrentAsync);

		private async void LoadCurrentAsync()
		{
			_jumpList = await JumpList.LoadCurrentAsync();
			RefreshItems();
			RaisePropertyChanged(nameof(IsLoaded));
		}

		private void RefreshItems()
		{
			Items = new ObservableCollection<JumpListItem>(_jumpList.Items);
		}

		public class NewJumpListItem : BindableBase
		{
			public string Arguments { get; set; } = "";

			public string DisplayName { get; set; } = "";

			public string Description { get; set; } = "";

			public string Logo { get; set; } = "ms-appx:///Assets/Icons/home.png";
		}
	}
}
