#nullable enable

using System.Numerics;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Microsoft.UI.Private.Controls
{
	internal partial class ScrollViewerIRefreshInfoProviderDefaultAnimationHandler : IAdapterAnimationHandler
	{
		private static readonly TimeSpan REFRESH_ANIMATION_DURATION = TimeSpan.FromMilliseconds(100);
		private const double REFRESH_VISUALIZER_OVERPAN_RATIO = 0.4;

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

		private void InteractionTrackerAnimation(UIElement refreshVisualizer, UIElement infoProvider, InteractionTracker interactionTracker)
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
				}

				m_refreshVisualizerVisualOffsetAnimation.SetReferenceParameter("refreshVisualizerVisual", m_refreshVisualizerVisual);
				m_refreshVisualizerVisualOffsetAnimation.SetReferenceParameter("interactionTracker", m_interactionTracker);

				m_infoProviderOffsetAnimation.SetReferenceParameter("refreshVisualizerVisual", m_refreshVisualizerVisual);
				m_infoProviderOffsetAnimation.SetReferenceParameter("infoProviderVisual", m_infoProviderVisual);
				m_infoProviderOffsetAnimation.SetReferenceParameter("interactionTracker", m_interactionTracker);

				m_interactionAnimationNeedsUpdating = false;
			}

			if (m_refreshVisualizerVisualOffsetAnimation && m_infoProviderOffsetAnimation)
			{
				m_refreshVisualizerVisual.Offset = new Vector3(0.0f);
				m_infoProviderVisual.Offset = new Vector3(0.0f);
				if (SharedHelpers.IsRS2OrHigher())
				{
					m_refreshVisualizerVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
					m_infoProviderVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
				}

				string animatedProperty = getAnimatedPropertyName();

				m_refreshVisualizerVisual.StartAnimation(animatedProperty, m_refreshVisualizerVisualOffsetAnimation);
				m_infoProviderVisual.StartAnimation(animatedProperty, m_infoProviderOffsetAnimation);
			}
		}

		private void RefreshRequestedAnimation(UIElement refreshVisualizer, UIElement infoProvider, double executionRatio)
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
						m_refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, -(m_refreshVisualizerVisual.Size().x * (1 - (float)executionRatio)));
						m_infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size().x * (float)executionRatio);
						break;
					case RefreshPullDirection.RightToLeft:
						m_refreshVisualizerRefreshRequestedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size().x * (1 - (float)executionRatio));
						m_infoProviderRefreshRequestedAnimation.InsertKeyFrame(1.0f, -m_refreshVisualizerVisual.Size().x * (float)executionRatio);
						break;
					default:
						MUX_ASSERT(false);
				}
				m_refreshRequestedAnimationNeedsUpdating = false;
			}

			if (m_refreshVisualizerRefreshRequestedAnimation && m_infoProviderRefreshRequestedAnimation)
			{
				hstring animatedProperty = getAnimatedPropertyName();

				m_refreshVisualizerVisual.StartAnimation(animatedProperty, m_refreshVisualizerRefreshRequestedAnimation);
				m_infoProviderVisual.StartAnimation(animatedProperty, m_infoProviderRefreshRequestedAnimation);
			}
		}

		private void RefreshCompletedAnimation(UIElement refreshVisualizer, UIElement infoProvider)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			ValidateAndStoreParameters(refreshVisualizer, infoProvider, null);

			if ((m_refreshVisualizerRefreshCompletedAnimation == null || m_infoProviderRefreshCompletedAnimation == null || m_refreshCompletedAnimationNeedsUpdating) && m_compositor != null)
			{
				m_refreshVisualizerRefreshCompletedAnimation = m_compositor.CreateScalarKeyFrameAnimation();
				m_refreshVisualizerRefreshCompletedAnimation.Duration(REFRESH_ANIMATION_DURATION);
				m_infoProviderRefreshCompletedAnimation = m_compositor.CreateScalarKeyFrameAnimation();
				m_infoProviderRefreshCompletedAnimation.Duration(REFRESH_ANIMATION_DURATION);
				m_infoProviderRefreshCompletedAnimation.InsertKeyFrame(1.0f, 0.0f);

				switch (m_refreshPullDirection)
				{
					case RefreshPullDirection.TopToBottom:
						m_refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, -(m_refreshVisualizerVisual.Size().y));
						break;
					case RefreshPullDirection.BottomToTop:
						m_refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size().y);
						break;
					case RefreshPullDirection.LeftToRight:
						m_refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, -(m_refreshVisualizerVisual.Size().x));
						break;
					case RefreshPullDirection.RightToLeft:
						m_refreshVisualizerRefreshCompletedAnimation.InsertKeyFrame(1.0f, m_refreshVisualizerVisual.Size().x);
						break;
					default:
						MUX_ASSERT(false);
				}
				m_refreshCompletedAnimationNeedsUpdating = false;
			}

			if (m_compositor)
			{
				m_refreshCompletedScopedBatch = m_compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
				m_compositionScopedBatchCompletedEventToken = m_refreshCompletedScopedBatch.Completed({ this, &ScrollViewerIRefreshInfoProviderDefaultAnimationHandler.RefreshCompletedBatchCompleted });
			}

			if (m_refreshVisualizerRefreshCompletedAnimation && m_infoProviderRefreshCompletedAnimation)
			{
				hstring animatedProperty = getAnimatedPropertyName();

				m_refreshVisualizerVisual.StartAnimation(animatedProperty, m_refreshVisualizerRefreshCompletedAnimation);
				m_infoProviderVisual.StartAnimation(animatedProperty, m_infoProviderRefreshCompletedAnimation);
			}

			if (m_refreshCompletedScopedBatch)
			{
				m_refreshCompletedScopedBatch.End();
			}
		}

		//PrivateHelpers
		void ValidateAndStoreParameters(UIElement& refreshVisualizer, UIElement& infoProvider, InteractionTracker& interactionTracker)
		{
			if (refreshVisualizer && m_refreshVisualizer != refreshVisualizer)
			{

				m_refreshVisualizerVisual = ElementCompositionPreview.GetElementVisual(refreshVisualizer);
				m_refreshVisualizer = refreshVisualizer;
				m_interactionAnimationNeedsUpdating = true;
				m_refreshRequestedAnimationNeedsUpdating = true;
				m_refreshCompletedAnimationNeedsUpdating = true;

				if (SharedHelpers.IsRS2OrHigher())
				{
					ElementCompositionPreview.SetIsTranslationEnabled(refreshVisualizer, true);
					m_refreshVisualizerVisual.Properties().InsertVector3("Translation", { 0.0f, 0.0f, 0.0f });
				}

				if (!m_compositor)
				{
					m_compositor = m_refreshVisualizerVisual.Compositor();
				}
			}

			if (infoProvider && m_infoProvider != infoProvider)
			{
				m_infoProviderVisual = ElementCompositionPreview.GetElementVisual(infoProvider);
				m_infoProvider = infoProvider;
				m_interactionAnimationNeedsUpdating = true;
				m_refreshRequestedAnimationNeedsUpdating = true;
				m_refreshCompletedAnimationNeedsUpdating = true;

				if (SharedHelpers.IsRS2OrHigher())
				{
					ElementCompositionPreview.SetIsTranslationEnabled(infoProvider, true);
					m_infoProviderVisual.Properties().InsertVector3("Translation", { 0.0f, 0.0f, 0.0f });
				}

				if (!m_compositor)
				{
					m_compositor = m_infoProviderVisual.Compositor();
				}
			}

			if (interactionTracker && m_interactionTracker != interactionTracker)
			{
				m_interactionTracker = interactionTracker;
				m_interactionAnimationNeedsUpdating = true;
				m_refreshRequestedAnimationNeedsUpdating = true;
				m_refreshCompletedAnimationNeedsUpdating = true;
			}
		}

		void RefreshCompletedBatchCompleted(object& /*sender*/, CompositionBatchCompletedEventArgs& /*args*/)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			m_refreshCompletedScopedBatch.Completed(m_compositionScopedBatchCompletedEventToken);
			InteractionTrackerAnimation(m_refreshVisualizer, m_infoProvider, m_interactionTracker);
		}

		bool IsOrientationVertical()
		{
			return (m_refreshPullDirection == RefreshPullDirection.TopToBottom || m_refreshPullDirection == RefreshPullDirection.BottomToTop);
		}

		hstring getAnimatedPropertyName()
		{
			hstring animatedProperty = "";
			if (SharedHelpers.IsRS2OrHigher())
			{
				animatedProperty = (std.wstring)(animatedProperty) + "Translation";
			}
			else
			{
				animatedProperty = (std.wstring)(animatedProperty) + "Offset";
			}

			if (IsOrientationVertical())
			{
				animatedProperty = (std.wstring)(animatedProperty) + ".Y";
			}
			else
			{
				animatedProperty = (std.wstring)(animatedProperty) + ".X";
			}

			return animatedProperty;
		}

	}
}
