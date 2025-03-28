using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Shows an interface for selecting a date. Used as the content of DatePickerFlyout, and can also be used in other templates.
	/// </summary>
	public partial class DatePickerSelector : ContentControl
	{
		public DatePickerSelector()
		{
			DefaultStyleKey = typeof(DatePickerSelector);

			InitPartial();
		}
		//Properties defined in DependencyPropertyMixins

		partial void InitPartial();
	}
}
