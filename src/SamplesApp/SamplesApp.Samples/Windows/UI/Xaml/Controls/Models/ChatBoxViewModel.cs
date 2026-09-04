using System.Windows.Input;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.Models
{
	internal class ChatBoxViewModel : ViewModelBase
	{
		public ChatBoxViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_clickCount = 0;
		}

		public ICommand ToggleHeader => GetOrCreateCommand(ExecuteToggleHeader);

		private int _clickCount;

		public int ClickCount
		{
			get => _clickCount;
			set
			{
				_clickCount = value;
				RaisePropertyChanged();
			}
		}

		private void ExecuteToggleHeader()
		{
			ClickCount++;
		}
	}
}
