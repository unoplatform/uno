namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IRangeValueProvider.
/// </summary>
public partial class RangeValuePatternIdentifiers
{
	/// <summary>
	/// Identifies the IsReadOnly automation property.
	/// </summary>
	public static AutomationProperty IsReadOnlyProperty { get; } = new();

	/// <summary>
	/// Identifies the LargeChange automation property.
	/// </summary>
	public static AutomationProperty LargeChangeProperty { get; } = new();

	/// <summary>
	/// Identifies the Maximum automation property.
	/// </summary>
	public static AutomationProperty MaximumProperty { get; } = new();

	/// <summary>
	/// Identifies the Minimum automation property.
	/// </summary>
	public static AutomationProperty MinimumProperty { get; } = new();

	/// <summary>
	/// Identifies the SmallChange automation property.
	/// </summary>
	public static AutomationProperty SmallChangeProperty { get; } = new();

	/// <summary>
	/// Identifies the Value automation property.
	/// </summary>
	public static AutomationProperty ValueProperty { get; } = new();
}
