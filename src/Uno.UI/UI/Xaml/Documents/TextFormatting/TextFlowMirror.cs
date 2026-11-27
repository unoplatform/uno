#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// Maps between a text element's flow space and the physical space its text engine lays out and draws in.
/// </summary>
/// <remarks>
/// A text element's flow space is mirrored when its layout direction is RightToLeft, as in WinUI: x grows from its
/// right edge. A box [x, x + w] in a mirrored viewport of width V sits at [V - x - w, V - x].
/// </remarks>
internal readonly record struct TextFlowMirror(double ViewportWidth, bool IsMirrored)
{
	/// <summary>The mirror of an element laid out in direction <paramref name="flowDirection"/>.</summary>
	/// <remarks>The width is the one the element's composition flip mirrors around.</remarks>
	public static TextFlowMirror ForLayout(FrameworkElement owner, FlowDirection flowDirection)
		=> new(owner.RenderSize.Width, flowDirection == FlowDirection.RightToLeft);

	public double MapX(double x) => IsMirrored ? ViewportWidth - x : x;

	public (double Left, double Width) MapSpan(double left, double width)
		=> IsMirrored ? (ViewportWidth - left - width, width) : (left, width);

	/// <summary>
	/// Saves the session's state and, when mirrored, cancels the mirror, so physical coordinates draw where they read.
	/// The caller restores the session.
	/// </summary>
	public void PushCounterMirror(IDrawingSession session)
	{
		session.Save();
		if (IsMirrored)
		{
			session.Translate((float)ViewportWidth, 0);
			session.Scale(-1, 1);
		}
	}
}
