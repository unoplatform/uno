using System;
using System.Windows.Input;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

namespace SamplesApp.Windows_UI_Xaml_Controls.Models
{
	[Bindable]
	public class TimePickerViewModel : ViewModelBase
	{
		private TimeSpan _time = DateTime.Now.TimeOfDay;

		public TimePickerViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			SetToCurrentTime = CreateCommand(ExecuteSetToCurrentTime);
		}

		public TimeSpan Time
		{
			get => _time;
			set
			{
				_time = value;
				RaisePropertyChanged("Time");
			}
		}

		public ICommand SetToCurrentTime { get; }

		private void ExecuteSetToCurrentTime()
		{
			Time = DateTime.Now.TimeOfDay;
		}
	}
}
