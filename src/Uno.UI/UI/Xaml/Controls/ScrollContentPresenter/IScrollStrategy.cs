#if UNO_HAS_MANAGED_SCROLL_PRESENTER
#nullable enable

using System;
using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	internal interface IScrollStrategy
	{
		event EventHandler<StrategyUpdateEventArgs>? Updated;
		void Initialize(ScrollContentPresenter presenter);
		void Update(UIElement view, double horizontalOffset, double verticalOffset, double zoom, ScrollOptions options);
	}

	/// <summary>
	/// Options for the IScrollStrategy.Update
	/// </summary>
	/// <param name="DisableAnimation">Request to disable the animation.</param>
	/// <param name="LinearAnimationDuration">
	/// Requests to use a linear animation with a specific duration instead of the default animation strategy.
	/// This is for the for inertia processor with touch scrolling where the total duration is calculated based on the velocity.
	/// </param>
	internal record struct ScrollOptions(bool DisableAnimation = false, bool IsInertial = false, bool IsIntermediate = false);
	internal record StrategyUpdateEventArgs(double HorizontalOffset, double VerticalOffset, bool IsIntermediate = false);
}
#endif
