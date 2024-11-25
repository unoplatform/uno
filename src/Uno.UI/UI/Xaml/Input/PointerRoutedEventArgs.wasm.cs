using System;
using Windows.Foundation;
using Uno.UI.Xaml.Input;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Input;

partial class PointerRoutedEventArgs : IHtmlHandleableRoutedEventArgs
{
	/// <inheritdoc />
	/// <remarks>Default value for pointers is <see cref="HtmlEventDispatchResult.StopPropagation"/>.</remarks>
	HtmlEventDispatchResult IHtmlHandleableRoutedEventArgs.HandledResult
	{
		get => CoreArgs.DispatchResult;
		set => CoreArgs.DispatchResult = value;
	}

	partial void InitPartial()
	{
		((IHtmlHandleableRoutedEventArgs)this).HandledResult = HtmlEventDispatchResult.StopPropagation;
	}

	public void PreventDefault()
	{
		((IHtmlHandleableRoutedEventArgs)this).HandledResult |= HtmlEventDispatchResult.PreventDefault;
	}

	internal static Point ToRelativePosition(Point absolutePosition, UIElement relativeTo)
		=> relativeTo == null
			? absolutePosition
			: relativeTo.TransformToVisual(null).Inverse.TransformPoint(absolutePosition);
}
