using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the TimePicker.SelectedTimeChanged event.
/// </summary>
public partial class TimePickerValueChangedEventArgs
{
	internal TimePickerValueChangedEventArgs(TimeSpan oldTime, TimeSpan newTime)
	{
		OldTime = oldTime;
		NewTime = newTime;
	}

	/// <summary>
	/// Gets the time previously selected in the picker.
	/// </summary>
	public TimeSpan OldTime { get; }

	/// <summary>
	/// Gets the new time selected in the picker.
	/// </summary>
	public TimeSpan NewTime { get; }

}
