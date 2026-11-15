using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;
using System;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.Models
{
	internal class DateTimePickerViewModel : ViewModelBase
	{
		public DateTimePickerViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_date = DateTimeOffset.Now.Date;
			_time = DateTimeOffset.Now.TimeOfDay;
		}

		private DateTimeOffset _date;

		public DateTimeOffset Date
		{
			get => _date;
			set
			{
				_date = value;
				RaisePropertyChanged();
			}
		}

		private TimeSpan _time;

		public TimeSpan Time
		{
			get => _time;
			set
			{
				_time = value;
				RaisePropertyChanged();
			}
		}
	}
}
