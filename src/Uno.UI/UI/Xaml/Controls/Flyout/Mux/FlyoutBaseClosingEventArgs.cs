namespace Windows.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides data for the FlyoutBase.Closing event
/// </summary>
public partial class FlyoutBaseClosingEventArgs
{
	internal FlyoutBaseClosingEventArgs()
	{		
	}

	/// <summary>
	/// Gets or sets a value that indicates whether
	/// the flyout should be prevented from closing.
	/// </summary>
	public bool Cancel { get; set; }
}
