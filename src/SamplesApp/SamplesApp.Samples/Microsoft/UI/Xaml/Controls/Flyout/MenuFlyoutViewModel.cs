using System.Windows.Input;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace Uno.UI.Samples.Content.UITests.MenuFlyout
{
	internal class MenuFlyoutViewModel : ViewModelBase
	{
		private string _selectedOption = string.Empty;

		public MenuFlyoutViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public string SelectedOption
		{
			get => _selectedOption;
			private set
			{
				_selectedOption = value;
				RaisePropertyChanged();
			}
		}

		public bool IsOption2Visible { get; set; } = true;

		public ICommand SelectOption => GetOrCreateCommand<object>(param => SelectedOption = $"You selected: '{param}'");
	}
}
