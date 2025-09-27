using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Represents the options used to show a flyout.
/// </summary>
public partial class FlyoutShowOptions
{
	/// <summary>
	/// Initializes a new instance of the FlyoutShowOptions class.
	/// </summary>
	public FlyoutShowOptions()
	{
	}

	/// <summary>
	/// Gets or sets the position where the flyout opens.
	/// </summary>
	public Point? Position { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates where the flyout is placed in relation to its target element.
	/// </summary>
	public FlyoutPlacementMode Placement { get; set; } = FlyoutPlacementMode.Auto;

	/// <summary>
	/// Gets or sets a value that indicates how the flyout behaves when opened.
	/// </summary>
	public FlyoutShowMode ShowMode { get; set; }
}
