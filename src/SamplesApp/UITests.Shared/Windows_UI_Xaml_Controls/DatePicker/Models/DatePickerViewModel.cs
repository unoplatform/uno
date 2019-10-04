using System;
using System.Windows.Input;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models
{
	[Bindable]
	public class DatePickerViewModel : ViewModelBase
	{
		private DateTimeOffset _date = DateTime.Now;

		public DatePickerViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public DateTimeOffset Date
		{
			get => _date;
			set
			{
				_date = value;
				RaisePropertyChanged("Date");
			}
		}

		public ICommand SetToCurrentDate => GetOrCreateCommand(() => Date = DateTime.Now);
	}
}
