namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IRangeValueProvider.
/// </summary>
public partial class RangeValuePatternIdentifiers
{
	/// <summary>
	/// Identifies the IsReadOnly automation property.
	/// </summary>
	public static AutomationProperty IsReadOnlyProperty { get; } = new AutomationProperty();

	/// <summary>
	/// Identifies the LargeChange automation property.
	/// </summary>
	public static AutomationProperty LargeChangeProperty { get; } = new AutomationProperty();

	/// <summary>
	/// Identifies the Maximum automation property.
	/// </summary>
	public static AutomationProperty MaximumProperty { get; } = new AutomationProperty();

	/// <summary>
	/// Identifies the Minimum automation property.
	/// </summary>
	public static AutomationProperty MinimumProperty { get; } = new AutomationProperty();

	/// <summary>
	/// Identifies the SmallChange automation property.
	/// </summary>
	public static AutomationProperty SmallChangeProperty { get; } = new AutomationProperty();

	/// <summary>
	/// Identifies the Value automation property.
	/// </summary>
	public static AutomationProperty ValueProperty { get; } = new AutomationProperty();
}
