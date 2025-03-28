namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the direction that content is scaled.
/// </summary>
public enum StretchDirection
{
	/// <summary>
	/// The content scales upward only when it is smaller than the parent. If the content is larger, no scaling downward is performed.
	/// </summary>
	UpOnly = 0,

	/// <summary>
	/// The content scales downward only when it is larger than the parent. If the content is smaller, no scaling upward is performed.
	/// </summary>
	DownOnly = 1,

	/// <summary>
	/// The content stretches to fit the parent according to the Stretch property.
	/// </summary>
	Both = 2,
}
