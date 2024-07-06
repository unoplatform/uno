namespace Microsoft.UI.Xaml;

internal readonly struct PropertyChangedParams
{
	public readonly DependencyProperty Property { get; }
	public readonly object OldValue { get; }
	public readonly object NewValue { get; }

	public PropertyChangedParams(DependencyProperty property, object oldValue, object newValue)
	{
		Property = property;
		OldValue = oldValue;
		NewValue = newValue;
	}
}
