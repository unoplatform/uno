using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;
using System;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.Models
{
	public class DateTimePickerViewModel : ViewModelBase
	{
		public DateTimePickerViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			m_Date = DateTimeOffset.Now.Date;
			m_Time = DateTimeOffset.Now.TimeOfDay;
		}

		private DateTimeOffset m_Date;

		public DateTimeOffset Date
		{
			get { return m_Date; }
			set
			{
				m_Date = value;
				RaisePropertyChanged();
			}
		}

		private TimeSpan m_Time;

		public TimeSpan Time
		{
			get { return m_Time; }
			set
			{
				m_Time = value;
				RaisePropertyChanged();
			}
		}
	}
}
