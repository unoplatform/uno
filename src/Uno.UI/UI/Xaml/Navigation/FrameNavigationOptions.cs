using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Navigation;

/// <summary>
/// Represents options for a frame navigation, including whether it is recorded in the navigation stack and what transition animation is used.
/// </summary>
public partial class FrameNavigationOptions
{
	/// <summary>
	/// Initializes a new instance of the FrameNavigationOptions class.
	/// </summary>
	public FrameNavigationOptions()
	{
	}

	/// <summary>
	/// Gets or sets a value that indicates the animated transition associated with the navigation.
	/// </summary>
	public NavigationTransitionInfo TransitionInfoOverride { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether navigation is recorded in the Frame's ForwardStack or BackStack.
	/// The default is true.
	/// </summary>
	public bool IsNavigationStackEnabled { get; set; } = true;
}
