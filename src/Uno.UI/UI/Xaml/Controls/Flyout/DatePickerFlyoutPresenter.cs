#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickerFlyoutPresenter : FlyoutPresenter
	{
		public DatePickerFlyoutPresenter()
		{
			DefaultStyleKey = typeof(DatePickerFlyoutPresenter);
		}
	}
}
#endif