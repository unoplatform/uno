using System;

namespace Windows.UI.Xaml.Controls
{
	internal class CQuirksMode2
	{
		internal static bool QuirkUseLegacyWindows8UI() => false;
		internal static bool QuirkDateTimePickerDefaultsToCurrentDateOrTime() => false;
		internal static bool QuirkSuppressDateTimePickerNullVisualizations() => false;
		internal static bool QuirkShouldDateAndTimePickerFlyoutButtonHaveNoAccessibleName() => false;
	}
}
