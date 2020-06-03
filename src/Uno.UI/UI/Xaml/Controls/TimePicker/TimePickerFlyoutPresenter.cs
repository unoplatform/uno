#if !NET461
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerFlyoutPresenter : FlyoutPresenter
	{
		public TimePickerFlyoutPresenter()
		{
			DefaultStyleKey = typeof(TimePickerFlyoutPresenter);
		}
	}
}
#endif
