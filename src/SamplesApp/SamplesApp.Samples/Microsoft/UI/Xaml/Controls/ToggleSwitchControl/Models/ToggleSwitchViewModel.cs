using System.Windows.Input;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace SamplesApp.Windows_UI_Xaml_Controls.ToggleSwitchControl.Models
{
	internal class ToggleSwitchViewModel : ViewModelBase
	{
		private bool _isOn = true;
		private bool _isOn2 = true;

		public ToggleSwitchViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public bool IsOn
		{
			get => _isOn;
			set
			{
				_isOn = value;
				RaisePropertyChanged();
			}
		}

		public bool IsOn2
		{
			get => _isOn2;
			set
			{
				_isOn2 = value;
				RaisePropertyChanged();
			}
		}

		public ICommand Flip => GetOrCreateCommand(() => IsOn = !IsOn);
	}
}
