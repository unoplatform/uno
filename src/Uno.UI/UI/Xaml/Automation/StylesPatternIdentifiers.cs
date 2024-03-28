namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by IStylesProvider.
/// </summary>
public partial class StylesPatternIdentifiers
{
	internal StylesPatternIdentifiers()
	{
	}

	/// <summary>
	/// Identifies the ExtendedProperties automation property.
	/// </summary>
	public static AutomationProperty ExtendedPropertiesProperty { get; } = new();

	/// <summary>
	/// Identifies the FillColor automation property.
	/// </summary>
	public static AutomationProperty FillColorProperty { get; } = new();

	/// <summary>
	/// Identifies the FillPatternColor automation property.
	/// </summary>
	public static AutomationProperty FillPatternColorProperty { get; } = new();

	/// <summary>
	/// Identifies the FillPatternStyle automation property.
	/// </summary>
	public static AutomationProperty FillPatternStyleProperty { get; } = new();

	/// <summary>
	/// Identifies the Shape automation property.
	/// </summary>
	public static AutomationProperty ShapeProperty { get; } = new();

	/// <summary>
	/// Identifies the StyleId automation property.
	/// </summary>
	public static AutomationProperty StyleIdProperty { get; } = new();

	/// <summary>
	/// Identifies the StyleName automation property.
	/// </summary>
	public static AutomationProperty StyleNameProperty { get; } = new();
}
