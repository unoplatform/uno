// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using AnimationContext = Microsoft/* UWP don't rename */.UI.Xaml.Controls.AnimationContext;
using ElementAnimator = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ElementAnimator;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public class ElementAnimatorDerived : ElementAnimator
	{
		public bool HasShowAnimationValue { get; set; }
		public bool HasHideAnimationValue { get; set; }
		public bool HasBoundsChangeAnimationValue { get; set; }

		public Action<UIElement, AnimationContext> StartShowAnimationFunc { get; set; }
		public Action<UIElement, AnimationContext> StartHideAnimationFunc { get; set; }
		public Action<UIElement, AnimationContext, Rect, Rect> StartBoundsChangeAnimationFunc { get; set; }

		protected override bool HasShowAnimationCore(UIElement element, AnimationContext context)
		{
			return HasShowAnimationValue;
		}

		protected override bool HasHideAnimationCore(UIElement element, AnimationContext context)
		{
			return HasHideAnimationValue;
		}

		protected override bool HasBoundsChangeAnimationCore(
			UIElement element,
			AnimationContext context,
			Rect oldBounds,
			Rect newBounds)
		{
			return HasBoundsChangeAnimationValue;
		}

		protected override void StartShowAnimation(UIElement element, AnimationContext context)
		{
			if (StartShowAnimationFunc != null)
			{
				StartShowAnimationFunc(element, context);
				OnShowAnimationCompleted(element);
			}
		}

		protected override void StartHideAnimation(UIElement element, AnimationContext context)
		{
			if (StartHideAnimationFunc != null)
			{
				StartHideAnimationFunc(element, context);
				OnHideAnimationCompleted(element);
			}
		}

		protected override void StartBoundsChangeAnimation(UIElement element, AnimationContext context, Rect oldBounds, Rect newBounds)
		{
			if (StartBoundsChangeAnimationFunc != null)
			{
				StartBoundsChangeAnimationFunc(element, context, oldBounds, newBounds);
				OnBoundsChangeAnimationCompleted(element);
			}
		}
	}
}
