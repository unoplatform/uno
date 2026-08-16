using System;
using Windows.Foundation;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Input;

partial class PointerRoutedEventArgs : IHtmlHandleableRoutedEventArgs
{
	/// <inheritdoc />
	/// <remarks>Default value for pointers is <see cref="HtmlEventDispatchResult.StopPropagation"/>.</remarks>
	HtmlEventDispatchResult IHtmlHandleableRoutedEventArgs.HandledResult { get; set; } = HtmlEventDispatchResult.StopPropagation;

	internal static Point ToRelativePosition(Point absolutePosition, UIElement relativeTo)
	{
		if (relativeTo is null)
		{
			return absolutePosition;
		}

		relativeTo.TransformToVisual(null).TryTransformInverse(absolutePosition, out var relativePosition);
		return relativePosition;
	}
}
