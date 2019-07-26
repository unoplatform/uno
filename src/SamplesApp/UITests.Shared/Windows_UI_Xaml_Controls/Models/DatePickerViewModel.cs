using System;
using System.Windows.Input;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;

namespace SamplesApp.Windows_UI_Xaml_Controls.Models
{
	[Bindable]
	public class DatePickerViewModel : ViewModelBase
	{
		private DateTime _date = DateTime.Now.Date;

		public DatePickerViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public DateTime Date
		{
			get => _date;
			set
			{
				_date = value;
				RaisePropertyChanged("Date");
			}
		}

		public ICommand SetToCurrentDate => GetOrCreateCommand(() => Date = DateTime.Now.Date);
	}
}
