using System;
using System.Windows.Input;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace UITests.Shared.Windows_UI_Xaml_Controls.CalendarDatePicker.Models
{
	[Bindable]
	public class CalendarDatePickerViewModel : ViewModelBase
	{
		private DateTimeOffset _date = new DateTimeOffset(2020, 05, 23, 12, 0, 0, TimeSpan.FromSeconds(0));

		public CalendarDatePickerViewModel(CoreDispatcher dispatcher) : base(dispatcher)
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
