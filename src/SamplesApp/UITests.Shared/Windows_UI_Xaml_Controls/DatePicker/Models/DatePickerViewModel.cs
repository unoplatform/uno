using System;
using System.Windows.Input;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models
{
	[Bindable]
	internal class DatePickerViewModel : ViewModelBase
	{
		private DateTimeOffset _date = DateTimeOffset.FromUnixTimeSeconds(1580655600); // 02/02/2020 @ 3:00pm (UTC) - Groundhog day!

		public DatePickerViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public DateTimeOffset Date
		{
			get => _date;
			set
			{
				_date = value;
				RaisePropertyChanged();
			}
		}

		public ICommand SetToCurrentDate => GetOrCreateCommand(() => Date = DateTimeOffset.Now);
	}
}
