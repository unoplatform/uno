#if !__ANDROID__ && !__IOS__
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewerIRefreshInfoProviderDefaultAnimationHandler.cpp, commit 87ce7c0

using System;
using System.Numerics;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;
using RefreshPullDirection = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshPullDirection;

namespace Microsoft.UI.Private.Controls;

internal partial class ScrollViewerIRefreshInfoProviderDefaultAnimationHandler : IAdapterAnimationHandler
{
	private static readonly TimeSpan REFRESH_ANIMATION_DURATION = TimeSpan.FromMilliseconds(100);
	//private const double REFRESH_VISUALIZER_OVERPAN_RATIO = 0.4;

	// Implementors of the IAdapterAnimationHandler interface are responsible for implementing the
	// 3 well defined component level animations in a PTR scenario. The three animations involved
	// in PTR include the expression animation used to have the RefreshVisualizer and its
	// InfoProvider follow the users finger, the animation used to show the RefreshVisualizer
	// when a refresh is requested, and the animation used to hide the refreshVisualizer when the
	// refresh is completed.

	// The interaction tracker set up by the Adapter has to be assembled in a very particular way.
	// Factoring out this functionality is a way to expose the animation for
	// Alteration without having to expose the "delicate" interaction tracker.

	public ScrollViewerIRefreshInfoProviderDefaultAnimationHandler(UIElement container, RefreshPullDirection refreshPullDirection)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (container != null)
		{
			var containerVisual = ElementCompositionPreview.GetElementVisual(container);
			m_compositor = containerVisual.Compositor;
			containerVisual.Clip = m_compositor.CreateInsetClip(0.0f, 0.0f, 0.0f, 0.0f);
		}
		m_refreshPullDirection = refreshPullDirection;
	}

	~ScrollViewerIRefreshInfoProviderDefaultAnimationHandler()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_refreshCompletedScopedBatch != null)
		{
			m_compositionScopedBatchCompletedEventToken.Disposable = null;
		}
	}

	public void InteractionTrackerAnimation(UIElement refreshVisualizer, UIElement infoProvider, InteractionTracker interactionTracker)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		ValidateAndStoreParameters(refreshVisualizer, infoProvider, interactionTracker);

		if (interactionTracker == null)
		{
			m_interactionTracker = null;
			return;
		}

		if ((m_refreshVisualizerVisualOffsetAnimation == null || m_infoProviderOffsetAnimation == null || m_interactionAnimationNeedsUpdating) && m_compositor != null)
		{
			switch (m_refreshPullDirection)
			{
				case RefreshPullDirection.TopToBottom:
					m_refreshVisualizerVisualOffsetAnimation = m_compositor.CreateExpressionAnimation(
						"-refreshVisualizerVisual.Size.Y + min(refreshVisualizerVisual.Size.Y, -interactionTracker.Position.Y)");

					m_infoProviderOffsetAnimation = m_compositor.CreateExpressionAnimation(
						"min(refreshVisualizerVisual.Size.Y, max(-interactionTracker.Position.Y, -refreshVisualizerVisual.Size.Y * 0.4))");

					break;
				case RefreshPullDirection.BottomToTop:
					m_refreshVisualizerVisualOffsetAnimation = m_compositor.CreateExpressionAnimation(
						"refreshVisualizerVisual.Size.Y - min(refreshVisualizerVisual.Size.Y, interactionTracker.Position.Y)");

					m_infoProviderOffsetAnimation = m_compositor.CreateExpressionAnimation(
						"max(-refreshVisualizerVisual.Size.Y, min(-interactionTracker.Position.Y, refreshVisualizerVisual.Size.Y * 0.4))");

					break;
				case RefreshPullDirection.LeftToRight:
					m_refreshVisualizerVisualOffsetAnimation = m_compositor.CreateExpressionAnimation(
						"-refreshVisualizerVisual.Size.X + min(refreshVisualizerVisual.Size.X, -interactionTracker.Position.X)");

					m_infoProviderOffsetAnimation = m_compositor.CreateExpressionAnimation(
						"min(refreshVisualizerVisual.Size.X, max(-interactionTracker.Position.X, -refreshVisualizerVisual.Size.X * 0.4))");

					break;
				case RefreshPullDirection.RightToLeft:
					m_refreshVisualizerVisualOffsetAnimation = m_compositor.CreateExpressionAnimation(
						"refreshVisualizerVisual.Size.X - min(refreshVisualizerVisual.Size.X, interactionTracker.Position.X)");

					m_infoProviderOffsetAnimation = m_compositor.CreateExpressionAnimation(
						"max(-refreshVisualizerVisual.Size.X, min(-interactionTracker.Position.X, refreshVisualizerVisual.Size.X * 0.4))");

					break;
				default:
					MUX_ASSERT(false);
					break;
			}

			m_refreshVisualizerVisualOffsetAnimation.SetReferenceParameter("refreshVisualizerVisual", m_refreshVisualizerVisual);
			m_refreshVisualizerVisualOffsetAnimation.SetReferenceParameter("interactionTracker", m_interactionTracker);

			m_infoProviderOffsetAnimation.SetReferenceParameter("refreshVisualizerVisual", m_refreshVisualizerVisual);
			m_infoProviderOffsetAnimation.SetReferenceParameter("infoProviderVisual", m_infoProviderVisual);
			m_infoProviderOffsetAnimation.SetReferenceParameter("interactionTracker", m_interactionTracker);

			m_interactionAnimationNeedsUpdating = false;
		}

		if (m_refreshVisualizerVisualOffsetAnimation is not null && m_infoProviderOffsetAnimation is not null)
		{
			m_refreshVisualizerVisual.Offset = new Vector3(0.0f);
			m_infoProviderVisual.Offset = new Vector3(0.0f);
			if (SharedHelpers.IsRS2OrHigher())
			{
				m_refreshVisualizerVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
				m_infoProviderVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
			}

			string animatedProperty = GetAnimatedPropertyName();

			m_refreshVisualizerVisual.StartAnimation(animatedProperty, m_refreshVisualizerVisualOffsetAnimation);
			m_infoProviderVisual.StartAnimation(animatedProperty, m_infoProviderOffsetAnimation);
		}
	}

	public void RefreshRequestedAnimation(UIElement refreshVisualizer, UIElement infoProvider, double executionRatio)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		ValidateAndStoreParameters(refreshVisualizer, infoProvider, null);

		if ((m_refreshVisualizerRefreshRequestedAnimation == null || m_infoProviderRefreshRequestedAnimation == null || m_refreshRequestedAnimationNeedsUpdating) && m_compositor != null)
		{
			m_refreshVisualizerRefreshRequestedAnimation = m_compositor.CreateScalarKeyFrameAnimation();
			m_refreshVisualizerRefreshRequestedAnimation.Duration = REFRESH_ANIMATION_DURATION;
			m_infoProviderRefreshRequestedAnimation = m_compositor.CreateScalarKeyFrameAnimation();
			m_infoProviderRefreshRequestedAnimation.Duration = REFRESH_ANIMATION_DURATION;
			switch (m_refreshPullDirection)
			{
				case RefreshPullDirection.TopToBottom:
					m_refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, -(m_refreshVisualizerVisual.Size.Y * (1 - (float)executionRatio)));
					m_infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size.Y * (float)executionRatio);
					break;
				case RefreshPullDirection.BottomToTop:
					m_refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size.Y * (1 - (float)executionRatio));
					m_infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, -m_refreshVisualizerVisual.Size.Y * (float)executionRatio);
					break;
				case RefreshPullDirection.LeftToRight:
					m_refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, -(m_refreshVisualizerVisual.Size.X * (1 - (float)executionRatio)));
					m_infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size.X * (float)executionRatio);
					break;
				case RefreshPullDirection.RightToLeft:
					m_refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size.X * (1 - (float)executionRatio));
					m_infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, -m_refreshVisualizerVisual.Size.X * (float)executionRatio);
					break;
				default:
					MUX_ASSERT(false);
					break;
			}
			m_refreshRequestedAnimationNeedsUpdating = false;
		}

		if (m_refreshVisualizerRefreshRequestedAnimation is not null && m_infoProviderRefreshRequestedAnimation is not null)
		{
			string animatedProperty = GetAnimatedPropertyName();

			m_refreshVisualizerVisual.StartAnimation(animatedProperty, m_refreshVisualizerRefreshRequestedAnimation);
			m_infoProviderVisual.StartAnimation(animatedProperty, m_infoProviderRefreshRequestedAnimation);
		}
	}

	public void RefreshCompletedAnimation(UIElement refreshVisualizer, UIElement infoProvider)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		ValidateAndStoreParameters(refreshVisualizer, infoProvider, null);

		if ((m_refreshVisualizerRefreshCompletedAnimation == null || m_infoProviderRefreshCompletedAnimation == null || m_refreshCompletedAnimationNeedsUpdating) && m_compositor != null)
		{
			m_refreshVisualizerRefreshCompletedAnimation = m_compositor.CreateScalarKeyFrameAnimation();
			m_refreshVisualizerRefreshCompletedAnimation.Duration = REFRESH_ANIMATION_DURATION;
			m_infoProviderRefreshCompletedAnimation = m_compositor.CreateScalarKeyFrameAnimation();
			m_infoProviderRefreshCompletedAnimation.Duration = REFRESH_ANIMATION_DURATION;
			m_infoProviderRefreshCompletedAnimation.InsertKeyFrame(1.0f, 0.0f);

			switch (m_refreshPullDirection)
			{
				case RefreshPullDirection.TopToBottom:
					m_refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, -(m_refreshVisualizerVisual.Size.Y));
					break;
				case RefreshPullDirection.BottomToTop:
					m_refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size.Y);
					break;
				case RefreshPullDirection.LeftToRight:
					m_refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, -(m_refreshVisualizerVisual.Size.X));
					break;
				case RefreshPullDirection.RightToLeft:
					m_refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size.X);
					break;
				default:
					MUX_ASSERT(false);
					break;
			}
			m_refreshCompletedAnimationNeedsUpdating = false;
		}

		if (m_compositor is not null)
		{
			m_refreshCompletedScopedBatch = m_compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
			m_refreshCompletedScopedBatch.Completed += RefreshCompletedBatchCompleted;
			m_compositionScopedBatchCompletedEventToken.Disposable = Disposable.Create(() => m_refreshCompletedScopedBatch.Completed -= RefreshCompletedBatchCompleted);
		}

		if (m_refreshVisualizerRefreshCompletedAnimation is not null && m_infoProviderRefreshCompletedAnimation is not null)
		{
			string animatedProperty = GetAnimatedPropertyName();

			m_refreshVisualizerVisual.StartAnimation(animatedProperty, m_refreshVisualizerRefreshCompletedAnimation);
			m_infoProviderVisual.StartAnimation(animatedProperty, m_infoProviderRefreshCompletedAnimation);
		}

		if (m_refreshCompletedScopedBatch is not null)
		{
			m_refreshCompletedScopedBatch.End();
		}
	}

	//PrivateHelpers
	private void ValidateAndStoreParameters(UIElement refreshVisualizer, UIElement infoProvider, InteractionTracker interactionTracker)
	{
		if (refreshVisualizer is not null && m_refreshVisualizer != refreshVisualizer)
		{

			m_refreshVisualizerVisual = ElementCompositionPreview.GetElementVisual(refreshVisualizer);
			m_refreshVisualizer = refreshVisualizer;
			m_interactionAnimationNeedsUpdating = true;
			m_refreshRequestedAnimationNeedsUpdating = true;
			m_refreshCompletedAnimationNeedsUpdating = true;

			if (SharedHelpers.IsRS2OrHigher())
			{
				ElementCompositionPreview.SetIsTranslationEnabled(refreshVisualizer, true);
				m_refreshVisualizerVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
			}

			if (m_compositor is null)
			{
				m_compositor = m_refreshVisualizerVisual.Compositor;
			}
		}

		if (infoProvider is not null && m_infoProvider != infoProvider)
		{
			m_infoProviderVisual = ElementCompositionPreview.GetElementVisual(infoProvider);
			m_infoProvider = infoProvider;
			m_interactionAnimationNeedsUpdating = true;
			m_refreshRequestedAnimationNeedsUpdating = true;
			m_refreshCompletedAnimationNeedsUpdating = true;

			if (SharedHelpers.IsRS2OrHigher())
			{
				ElementCompositionPreview.SetIsTranslationEnabled(infoProvider, true);
				m_infoProviderVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
			}

			if (m_compositor is null)
			{
				m_compositor = m_infoProviderVisual.Compositor;
			}
		}

		if (interactionTracker is not null && m_interactionTracker != interactionTracker)
		{
			m_interactionTracker = interactionTracker;
			m_interactionAnimationNeedsUpdating = true;
			m_refreshRequestedAnimationNeedsUpdating = true;
			m_refreshCompletedAnimationNeedsUpdating = true;
		}
	}

	private void RefreshCompletedBatchCompleted(object sender, CompositionBatchCompletedEventArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		m_compositionScopedBatchCompletedEventToken.Disposable = null;
		InteractionTrackerAnimation(m_refreshVisualizer, m_infoProvider, m_interactionTracker);
	}

	private bool IsOrientationVertical()
	{
		return (m_refreshPullDirection == RefreshPullDirection.TopToBottom || m_refreshPullDirection == RefreshPullDirection.BottomToTop);
	}

	private string GetAnimatedPropertyName()
	{
		string animatedProperty = "";
		if (SharedHelpers.IsRS2OrHigher())
		{
			animatedProperty = (string)(animatedProperty) + "Translation";
		}
		else
		{
			animatedProperty = (string)(animatedProperty) + "Offset";
		}

		if (IsOrientationVertical())
		{
			animatedProperty = (string)(animatedProperty) + ".Y";
		}
		else
		{
			animatedProperty = (string)(animatedProperty) + ".X";
		}

		return animatedProperty;
	}

}
#endif
