namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Contains values used as identifiers by ITransformProvider2.
/// </summary>
public partial class TransformPattern2Identifiers
{
	internal TransformPattern2Identifiers()
	{
	}

	/// <summary>
	/// Identifies the CanZoom automation property.
	/// </summary>
	public static AutomationProperty CanZoomProperty => new();

	/// <summary>
	/// Identifies the MaxZoom automation property.
	/// </summary>
	public static AutomationProperty MaxZoomProperty => new();

	/// <summary>
	/// Identifies the MinZoom automation property.
	/// </summary>
	public static AutomationProperty MinZoomProperty => new();

	/// <summary>
	/// Identifies the ZoomLevel automation property.
	/// </summary>
	public static AutomationProperty ZoomLevelProperty => new();
}
