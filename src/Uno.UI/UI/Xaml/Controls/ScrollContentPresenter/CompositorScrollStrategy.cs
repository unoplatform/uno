#if UNO_HAS_MANAGED_SCROLL_PRESENTER
#if __SKIA__
#nullable enable
using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	internal class CompositorScrollStrategy : IScrollStrategy
	{
		public static CompositorScrollStrategy Instance { get; } = new();

		private CompositorScrollStrategy() { }

		/// <inheritdoc />
		public void Initialize(ScrollContentPresenter presenter)
			=> presenter.Visual.Clip = presenter.Visual.Compositor.CreateInsetClip(0, 0, 0, 0);

		public void Update(UIElement view, double horizontalOffset, double verticalOffset, double zoom, ScrollOptions options)
		{
			var target = new Vector2((float)-horizontalOffset, (float)-verticalOffset);
			var visual = view.Visual;

			// No matter the `options.DisableAnimation`, if we have an animation running
			if (visual.TryGetAnimationController(nameof(Visual.AnchorPoint)) is { } controller
				// ... that is animating to (almost) the same target value
				&& Vector2.DistanceSquared(visual.AnchorPoint, target) < 4
				// ... and which is about to complete
				&& controller.Remaining < TimeSpan.FromMilliseconds(50))
			{
				// We keep the animation running, making sure that we are not abruptly stopping scrolling animation
				// due to completion of the inertia processor a bit earlier than the animation itself.
				return;
			}

			if (options is { DisableAnimation: true } or { IsInertial: true })
			{
				visual.StopAnimation(nameof(Visual.AnchorPoint));
				visual.AnchorPoint = target;
			}
			else
			{
				var compositor = visual.Compositor;
				var easing = CompositionEasingFunction.CreatePowerEasingFunction(compositor, CompositionEasingFunctionMode.Out, 10);
				var animation = compositor.CreateVector2KeyFrameAnimation();
				animation.InsertKeyFrame(1.0f, target, easing);
				animation.Duration = TimeSpan.FromSeconds(1);

				visual.StartAnimation(nameof(Visual.AnchorPoint), animation);
			}
		}
	}
}
#endif
#endif
