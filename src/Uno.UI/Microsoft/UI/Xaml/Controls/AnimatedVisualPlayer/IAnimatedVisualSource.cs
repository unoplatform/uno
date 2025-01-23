using Microsoft.UI.Composition;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// An animated Visual that can be used by other objects, such as an AnimatedVisualPlayer.
/// </summary>
public partial interface IAnimatedVisualSource
{
	/// <summary>
	/// Attempts to create an animated visual.
	/// </summary>
	/// <param name="compositor">The compositor for the animated visual.</param>
	/// <param name="diagnostics">The diagnostics information about the attempt to create an animated visual.</param>
	/// <returns>An animated visual that can be used by other objects.</returns>
	IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics);
}
