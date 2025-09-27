namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the NumberBox.ValueChanged event.
/// </summary>
public partial class NumberBoxValueChangedEventArgs
{
	internal NumberBoxValueChangedEventArgs(double oldValue, double newValue)
	{
		OldValue = oldValue;
		NewValue = newValue;
	}

	/// <summary>
	/// Contains the old Value being replaced in a NumberBox.
	/// </summary>
	public double OldValue { get; }

	/// <summary>
	/// Contains the new Value to be set for a NumberBox.
	/// </summary>
	public double NewValue { get; }
}
