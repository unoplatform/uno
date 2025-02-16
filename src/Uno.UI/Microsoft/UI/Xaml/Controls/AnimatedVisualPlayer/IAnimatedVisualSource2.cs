using System.Collections.Generic;
using Windows.UI;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// An animated Visual that can be used by other objects, such as an AnimatedIcon. Extends IAnimatedVisualSource.
/// </summary>
public partial interface IAnimatedVisualSource2 : IAnimatedVisualSource
{
	/// <summary>
	/// Gets a collection that provides a mapping of marker names to playback positions in the animation.
	/// </summary>
	public IReadOnlyDictionary<string, double> Markers { get; }

	/// <summary>
	/// Sets a color for the animated visual.
	/// </summary>
	/// <param name="propertyName">The property name of the color as defined in the JSON file for the animated icon.</param>
	/// <param name="value">The color value for the propertyName.</param>
	void SetColorProperty(string propertyName, Color value);
}
