namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify whether the area outside of a light-dismiss UI is darkened.
/// </summary>
public enum LightDismissOverlayMode
{
	/// <summary>
	/// The device-family the app is running on determines whether the area outside of a light-dismiss UI is darkened.
	/// </summary>
	Auto,

	/// <summary>
	/// The area outside of a light-dismiss UI is darkened for all device families.
	/// </summary>
	On,

	/// <summary>
	/// The area outside of a light-dismiss UI is not darkened for all device families.
	/// </summary>
	Off,
}
