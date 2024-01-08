#nullable enable

using Uno.UI.UI.Xaml.Controls.TestHooks;

namespace Microsoft.UI.Xaml.Controls;

partial class TimePicker : IDateTimePickerTestHooks
{
	object IDateTimePickerTestHooks.Header
	{
		get => Header;
		set => Header = value;
	}
}
