using System;

namespace Windows.UI.Xaml.Controls;

public partial class TimePickerSelectedValueChangedEventArgs
{
	internal TimePickerSelectedValueChangedEventArgs()
	{
	}

	public TimeSpan? OldTime { get; internal set; }

	public TimeSpan? NewTime { get; internal set; }
}
