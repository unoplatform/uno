using System;
using Windows.Foundation;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input;

partial class PointerRoutedEventArgs : IHtmlHandleableRoutedEventArgs
{
	/// <inheritdoc />
	/// <remarks>Default value for pointers is <see cref="HtmlEventDispatchResult.StopPropagation"/>.</remarks>
	HtmlEventDispatchResult IHtmlHandleableRoutedEventArgs.HandledResult { get; set; } = HtmlEventDispatchResult.StopPropagation;

	internal static Point ToRelativePosition(Point absolutePosition, UIElement relativeTo)
		=> relativeTo == null
			? absolutePosition
			: relativeTo.TransformToVisual(null).Inverse.TransformPoint(absolutePosition);
}
