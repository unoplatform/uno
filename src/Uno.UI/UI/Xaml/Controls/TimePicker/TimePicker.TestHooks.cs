#nullable enable

using Uno.UI.Xaml.Controls.TestHooks;

namespace Windows.UI.Xaml.Controls;

partial class TimePicker : IDateTimePickerTestHooks
{
	object IDateTimePickerTestHooks.Header
	{
		get => Header;
		set => Header = value;
	}
}
