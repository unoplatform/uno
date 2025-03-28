#if HAS_UNO
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

using RefreshPullDirection = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshPullDirection;
using IAdapterAnimationHandler = Microsoft.UI.Private.Controls.IAdapterAnimationHandler;

namespace MUXControlsTestApp
{
	class AnimationHandler : IAdapterAnimationHandler
	{
		Compositor compositor;
		RefreshPullDirection refreshPullDirection;
		UIElement refreshVisualizer;
		Visual refreshVisualizerVisual;
		UIElement infoProvider;
		Visual infoProviderVisual;
		InteractionTracker interactionTracker;

		bool interactionAnimationNeedsUpdating = true;
		bool refreshRequestedAnimationNeedsUpdating = true;
		bool refreshCompletedAnimationNeedsUpdating = true;

		ExpressionAnimation refreshVisualizerVisualOffsetAnimation;
		ExpressionAnimation infoProviderOffsetAnimation;
		ScalarKeyFrameAnimation refreshVisualizerRefreshRequestedAnimation;
		ScalarKeyFrameAnimation infoProviderRefreshRequestedAnimation;
		ScalarKeyFrameAnimation refreshVisualizerRefreshCompletedAnimation;
		ScalarKeyFrameAnimation infoProviderRefreshCompletedAnimation;

		CompositionScopedBatch refreshCompletedScopedBatch;

		public AnimationHandler(UIElement container, RefreshPullDirection pullDirection)
		{
			if (container != null)
			{
				var vis = ElementCompositionPreview.GetElementVisual(container);
				compositor = vis.Compositor;
			}
			refreshPullDirection = pullDirection;
		}

		public void InteractionTrackerAnimation(UIElement refreshVisualizer, UIElement infoProvider, InteractionTracker interactionTracker)
		{
			ValidateAndStoreParameters(refreshVisualizer, infoProvider, interactionTracker);

			if (interactionTracker == null)
			{
				this.interactionTracker = null;
				return;
			}

			if ((refreshVisualizerVisualOffsetAnimation == null || infoProviderOffsetAnimation == null || interactionAnimationNeedsUpdating) && compositor != null)
			{
				switch (refreshPullDirection)
				{
					case RefreshPullDirection.TopToBottom:
						refreshVisualizerVisualOffsetAnimation = compositor.CreateExpressionAnimation(
							"-refreshVisualizerVisual.Size.Y + min(refreshVisualizerVisual.Size.Y, -interactionTracker.Position.Y)");

						infoProviderOffsetAnimation = compositor.CreateExpressionAnimation(
							"min(refreshVisualizerVisual.Size.Y, max(-interactionTracker.Position.Y, 0))");

						break;
					case RefreshPullDirection.BottomToTop:
						refreshVisualizerVisualOffsetAnimation = compositor.CreateExpressionAnimation(
							"refreshVisualizerVisual.Size.Y - min(refreshVisualizerVisual.Size.Y, interactionTracker.Position.Y)");

						infoProviderOffsetAnimation = compositor.CreateExpressionAnimation(
							"max(-refreshVisualizerVisual.Size.Y, min(-interactionTracker.Position.Y, 0))");

						break;
					case RefreshPullDirection.LeftToRight:
						refreshVisualizerVisualOffsetAnimation = compositor.CreateExpressionAnimation(
							"-refreshVisualizerVisual.Size.X + min(refreshVisualizerVisual.Size.X, -interactionTracker.Position.X)");

						infoProviderOffsetAnimation = compositor.CreateExpressionAnimation(
							"min(refreshVisualizerVisual.Size.X, max(-interactionTracker.Position.X, 0))");

						break;
					case RefreshPullDirection.RightToLeft:
						refreshVisualizerVisualOffsetAnimation = compositor.CreateExpressionAnimation(
							"refreshVisualizerVisual.Size.X - min(refreshVisualizerVisual.Size.X, interactionTracker.Position.X)");

						infoProviderOffsetAnimation = compositor.CreateExpressionAnimation(
							"max(-refreshVisualizerVisual.Size.X, min(-interactionTracker.Position.X, 0))");

						break;
				}

				refreshVisualizerVisualOffsetAnimation.SetReferenceParameter("refreshVisualizerVisual", refreshVisualizerVisual);
				refreshVisualizerVisualOffsetAnimation.SetReferenceParameter("interactionTracker", this.interactionTracker);

				infoProviderOffsetAnimation.SetReferenceParameter("refreshVisualizerVisual", refreshVisualizerVisual);
				infoProviderOffsetAnimation.SetReferenceParameter("infoProviderVisual", infoProviderVisual);
				infoProviderOffsetAnimation.SetReferenceParameter("interactionTracker", this.interactionTracker);

				interactionAnimationNeedsUpdating = false;
			}

			if (refreshVisualizerVisualOffsetAnimation != null && infoProviderOffsetAnimation != null)
			{
				if (PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone2))
				{
					refreshVisualizerVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
					infoProviderVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
				}
				else
				{
					refreshVisualizerVisual.Offset = new Vector3(0.0f);
					infoProviderVisual.Offset = new Vector3(0.0f);
				}

				string animatedProperty = GetAnimatedPropertyName();
				refreshVisualizerVisual.StartAnimation(animatedProperty, refreshVisualizerVisualOffsetAnimation);
				infoProviderVisual.StartAnimation(animatedProperty, infoProviderOffsetAnimation);
			}
		}

		public void RefreshRequestedAnimation(UIElement refreshVisualizer, UIElement infoProvider, double executionRatio)
		{
			ValidateAndStoreParameters(refreshVisualizer, infoProvider, null);

			if ((refreshVisualizerRefreshRequestedAnimation == null || infoProviderRefreshRequestedAnimation == null || refreshRequestedAnimationNeedsUpdating) && compositor != null)
			{
				refreshVisualizerRefreshRequestedAnimation = compositor.CreateScalarKeyFrameAnimation();
				refreshVisualizerRefreshRequestedAnimation.Duration = TimeSpan.FromMilliseconds(100);
				infoProviderRefreshRequestedAnimation = compositor.CreateScalarKeyFrameAnimation();
				infoProviderRefreshRequestedAnimation.Duration = TimeSpan.FromMilliseconds(100);
				switch (refreshPullDirection)
				{
					case RefreshPullDirection.TopToBottom:
						refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, -(refreshVisualizerVisual.Size.Y * (1 - (float)executionRatio)));
						infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, refreshVisualizerVisual.Size.Y * (float)executionRatio);
						break;
					case RefreshPullDirection.BottomToTop:
						refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, refreshVisualizerVisual.Size.Y * (1 - (float)executionRatio));
						infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, -refreshVisualizerVisual.Size.Y * (float)executionRatio);
						break;
					case RefreshPullDirection.LeftToRight:
						refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, -(refreshVisualizerVisual.Size.X * (1 - (float)executionRatio)));
						infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, refreshVisualizerVisual.Size.X * (float)executionRatio);
						break;
					case RefreshPullDirection.RightToLeft:
						refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, refreshVisualizerVisual.Size.X * (1 - (float)executionRatio));
						infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, -refreshVisualizerVisual.Size.X * (float)executionRatio);
						break;
				}
				refreshRequestedAnimationNeedsUpdating = false;
			}

			if (refreshVisualizerRefreshRequestedAnimation != null && infoProviderRefreshRequestedAnimation != null)
			{
				string animatedProperty = GetAnimatedPropertyName();
				refreshVisualizerVisual.StartAnimation(animatedProperty, refreshVisualizerRefreshRequestedAnimation);
				infoProviderVisual.StartAnimation(animatedProperty, infoProviderRefreshRequestedAnimation);
			}
		}

		public void RefreshCompletedAnimation(UIElement refreshVisualizer, UIElement infoProvider)
		{
			ValidateAndStoreParameters(refreshVisualizer, infoProvider, null);

			if ((refreshVisualizerRefreshCompletedAnimation == null || infoProviderRefreshCompletedAnimation == null || refreshCompletedAnimationNeedsUpdating) && compositor != null)
			{
				refreshVisualizerRefreshCompletedAnimation = compositor.CreateScalarKeyFrameAnimation();
				refreshVisualizerRefreshCompletedAnimation.Duration = TimeSpan.FromMilliseconds(100);
				infoProviderRefreshCompletedAnimation = compositor.CreateScalarKeyFrameAnimation();
				infoProviderRefreshCompletedAnimation.Duration = TimeSpan.FromMilliseconds(100);
				infoProviderRefreshCompletedAnimation.InsertKeyFrame(1.0f, 0.0f);

				switch (refreshPullDirection)
				{
					case RefreshPullDirection.TopToBottom:
						refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, -(refreshVisualizerVisual.Size.Y));
						break;
					case RefreshPullDirection.BottomToTop:
						refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, refreshVisualizerVisual.Size.Y);
						break;
					case RefreshPullDirection.LeftToRight:
						refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, -(refreshVisualizerVisual.Size.X));
						break;
					case RefreshPullDirection.RightToLeft:
						refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, refreshVisualizerVisual.Size.X);
						break;
				}
				refreshCompletedAnimationNeedsUpdating = false;
			}

			if (compositor != null)
			{
				refreshCompletedScopedBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
				refreshCompletedScopedBatch.Completed += RefreshCompletedBatchCompleted;
			}

			if (refreshVisualizerRefreshCompletedAnimation != null && infoProviderRefreshCompletedAnimation != null)
			{
				string animatedProperty = GetAnimatedPropertyName();
				refreshVisualizerVisual.StartAnimation(animatedProperty, refreshVisualizerRefreshCompletedAnimation);
				infoProviderVisual.StartAnimation(animatedProperty, infoProviderRefreshCompletedAnimation);
			}

			if (refreshCompletedScopedBatch != null)
			{
				refreshCompletedScopedBatch.End();
			}
		}

		//PrivateHelpers
		private void ValidateAndStoreParameters(UIElement refreshVisualizer, UIElement infoProvider, InteractionTracker interactionTracker)
		{
			if (refreshVisualizer != null && this.refreshVisualizer != refreshVisualizer)
			{
				refreshVisualizerVisual = ElementCompositionPreview.GetElementVisual(refreshVisualizer);
				this.refreshVisualizer = refreshVisualizer;
				interactionAnimationNeedsUpdating = true;
				refreshRequestedAnimationNeedsUpdating = true;
				refreshCompletedAnimationNeedsUpdating = true;

				if (compositor == null)
				{
					compositor = refreshVisualizerVisual.Compositor;
				}

				if (PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone2))
				{
					ElementCompositionPreview.SetIsTranslationEnabled(refreshVisualizer, true);
				}
			}

			if (infoProvider != null && this.infoProvider != infoProvider)
			{
				infoProviderVisual = ElementCompositionPreview.GetElementVisual(infoProvider);
				this.infoProvider = infoProvider;
				interactionAnimationNeedsUpdating = true;
				refreshRequestedAnimationNeedsUpdating = true;
				refreshCompletedAnimationNeedsUpdating = true;

				if (compositor == null)
				{
					compositor = infoProviderVisual.Compositor;
				}

				if (PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone2))
				{
					ElementCompositionPreview.SetIsTranslationEnabled(infoProvider, true);
				}
			}

			if (interactionTracker != null && this.interactionTracker != interactionTracker)
			{
				this.interactionTracker = interactionTracker;
				interactionAnimationNeedsUpdating = true;
				refreshRequestedAnimationNeedsUpdating = true;
				refreshCompletedAnimationNeedsUpdating = true;
			}
		}

		private void RefreshCompletedBatchCompleted(Object sender, CompositionBatchCompletedEventArgs args)
		{
			InteractionTrackerAnimation(refreshVisualizer, infoProvider, interactionTracker);
		}

		private bool IsOrientationVertical()
		{
			return (refreshPullDirection == RefreshPullDirection.TopToBottom || refreshPullDirection == RefreshPullDirection.BottomToTop);
		}

		String GetAnimatedPropertyName()
		{
			String animatedProperty = "";
			if (PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone2))
			{
				animatedProperty += "Translation";
			}
			else
			{
				animatedProperty += "Offset";
			}

			if (IsOrientationVertical())
			{
				animatedProperty += ".Y";
			}
			else
			{
				animatedProperty += ".X";
			}

			return animatedProperty;
		}
	}
}
#endif
