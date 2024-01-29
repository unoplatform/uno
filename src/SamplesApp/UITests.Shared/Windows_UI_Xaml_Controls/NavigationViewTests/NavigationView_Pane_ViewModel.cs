using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;

namespace SamplesApp.Samples.Windows_UI_Xaml_Controls.NavigationViewTests
{
	internal class NavigationViewItemsViewModel : ViewModelBase
	{
		public NavigationViewItemsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			Items = new[]
			{
			  new NavigationViewItemViewModel("Play", "Play"),
			  new NavigationViewItemViewModel("Save", "Save")
			};
		}

		public IList<NavigationViewItemViewModel> Items { get; }

		NavigationViewItemViewModel _selectedItem;
		public NavigationViewItemViewModel SelectedItem
		{
			get => _selectedItem;
			set
			{
				_selectedItem = value;
				RaisePropertyChanged();
			}
		}
	}

	public class NavigationViewItemViewModel
	{
		public NavigationViewItemViewModel(string title, string symbol)
		{
			Title = title;
			Symbol = symbol;
		}

		public string Title { get; }
		public string Symbol { get; }
	}
}
