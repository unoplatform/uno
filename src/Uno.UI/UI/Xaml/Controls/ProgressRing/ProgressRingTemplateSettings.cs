using Microsoft.UI.Xaml;

namespace Uno.UI.Controls.Legacy.Primitives;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources
/// when defining templates for a ProgressRing control. Not intended for general use.
/// </summary>
public partial class ProgressRingTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the template-defined diameter of the "Ellipse" element that is animated in a templated ProgressRing.
	/// </summary>
	public double EllipseDiameter { get; set; }

	/// <summary>
	/// Gets the template-defined offset position of the "Ellipse" element that is animated in a templated ProgressRing.
	/// </summary>
	public Thickness EllipseOffset { get; set; }

	/// <summary>
	/// Gets the maximum bounding size of the progress ring as rendered.
	/// </summary>
	public double MaxSideLength { get; set; }
}
