#if UNO_HAS_MANAGED_SCROLL_PRESENTER
#if __SKIA__
#nullable enable
using System;
using System.Numerics;
using Microsoft.UI.Composition;

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

			if (options.DisableAnimation)
			{
				visual.StopAnimation(nameof(Visual.AnchorPoint));
				visual.AnchorPoint = target;
			}
			else if (options is { LinearAnimationDuration: { Ticks: > 0 } duration })
			{
				var compositor = visual.Compositor;
				var easing = CompositionEasingFunction.CreateLinearEasingFunction(compositor);
				var animation = compositor.CreateVector2KeyFrameAnimation();
				animation.InsertKeyFrame(1.0f, target, easing);
				animation.Duration = duration;

				visual.StartAnimation(nameof(Visual.AnchorPoint), animation);
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
