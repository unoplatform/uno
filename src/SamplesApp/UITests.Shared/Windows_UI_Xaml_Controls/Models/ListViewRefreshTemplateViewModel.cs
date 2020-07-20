using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace Uno.UI.Samples.Presentation.SamplePages
{
	public class ListViewRefreshTemplateViewModel : ViewModelBase
	{
		private string _selectedItem;

		public ListViewRefreshTemplateViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			SampleItems = GetSampleItems();
		}

		public string SelectedItem
		{
			get => _selectedItem;
			set
			{
				_selectedItem = value;
				RaisePropertyChanged();
			}
		}

		public string Header => "My Header";

		public string Footer => "My Footer";

		public ItemViewModel[] SampleItems { get; }

		private ItemViewModel[] GetSampleItems()
		{
			return Enumerable
				.Range(1, 30)
				.Select(i => new ItemViewModel(i.ToString(CultureInfo.InvariantCulture), Dispatcher))
				.ToArray();
		}
	}
	public class ItemViewModel : ViewModelBase
	{
		private bool _isVisible;

		public ItemViewModel(string item, CoreDispatcher dispatcher) : base (dispatcher)
		{
		}

		public ICommand MakeVisible => GetOrCreateCommand(() => IsVisible = !IsVisible);

		public bool IsVisible
		{
			get => _isVisible;
			set
			{
				_isVisible = value;
				RaisePropertyChanged();
			}
		}
	}
}
