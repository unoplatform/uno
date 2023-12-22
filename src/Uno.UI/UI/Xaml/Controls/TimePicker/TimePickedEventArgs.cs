using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the TimePicked event.
/// </summary>
public sealed partial class TimePickedEventArgs : DependencyObject
{
	/// <summary>
	/// Initializes a new instance of the TimePickedEventArgs class.
	/// </summary>
	public TimePickedEventArgs()
	{
	}

	internal TimePickedEventArgs(TimeSpan oldTime, TimeSpan newTime)
	{
		OldTime = oldTime;
		NewTime = newTime;
	}

	/// <summary>
	/// Gets the old time value.
	/// </summary>
	public TimeSpan OldTime { get; }

	/// <summary>
	/// Gets the time that was selected by the user.
	/// </summary>
	public TimeSpan NewTime { get; }
}
