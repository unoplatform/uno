using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Composition;

namespace Microsoft.UI.Private.Controls
{
	internal partial class RefreshInfoProviderImpl : IRefreshInfoProvider
	{
		// There is not a lot of value in antly firing the interaction ratio changed event as the
		// animations which are based off of it use the published composition property set which is
		// updated regularly. Instead we fire the event every 5th change to reduce overhead.
		private const int RAISE_INTERACTION_RATIO_CHANGED_FREQUENCY = 5;

		// When the user is close to a threshold point we want to make sure that we always raise
		// InteractionRatioChanged events so that we don't miss something important.
		private const double ALWAYS_RAISE_INTERACTION_RATIO_TOLERANCE = 0.05;

		// This is our private implementation of the IRefreshInfoProvider interface. It is contructed by
		// the ScrollViewerAdapter's Adapt method and returned as an instance of an IRefreshInfoProvider.
		// It is an InteractionTrackerOwner, the corresponding InteractionTracker is maintained in the Adapter.

		public RefreshInfoProviderImpl()
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		}

		~RefreshInfoProviderImpl()
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		}

		public RefreshInfoProviderImpl(RefreshPullDirection refreshPullDirection, Size refreshVisualizerSize, Compositor compositor)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			m_refreshPullDirection = refreshPullDirection;
			m_refreshVisualizerSize = refreshVisualizerSize;
			m_compositionProperties = compositor.CreatePropertySet();
		}

		private void UpdateIsInteractingForRefresh(bool value)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			bool isInteractingForRefresh = value && !m_peeking;
			if (isInteractingForRefresh != m_isInteractingForRefresh)
			{
				m_isInteractingForRefresh = isInteractingForRefresh;
				RaiseIsInteractingForRefreshChanged();
			}
		}

		/////////////////////////////////////////////////////
		///////   IInteractionTrackerOwnerOverrides  ////////
		/////////////////////////////////////////////////////
		private void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_FLT_FLT_FLT, METH_NAME, this, args.Position.X, args.Position.Y, args.Position.Z);
			switch (m_refreshPullDirection)
			{
				case RefreshPullDirection.TopToBottom:
					RaiseInteractionRatioChanged(m_refreshVisualizerSize.Height == 0 ? 1.0 : Math.Min(1.0, (double)-args.Position.Y / m_refreshVisualizerSize.Height));
					break;
				case RefreshPullDirection.BottomToTop:
					RaiseInteractionRatioChanged(m_refreshVisualizerSize.Height == 0 ? 1.0f : Math.Min(1.0, (double)args.Position.Y / m_refreshVisualizerSize.Height));
					break;
				case RefreshPullDirection.LeftToRight:
					RaiseInteractionRatioChanged(m_refreshVisualizerSize.Width == 0 ? 1.0f : Math.Min(1.0, (double)-args.Position.X / m_refreshVisualizerSize.Width));
					break;
				case RefreshPullDirection.RightToLeft:
					RaiseInteractionRatioChanged(m_refreshVisualizerSize.Width == 0 ? 1.0f : Math.Min(1.0, (double)args.Position.X / m_refreshVisualizerSize.Width));
					break;
				default:
					MUX_ASSERT(false);
					break;
			}
		}

		private void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId);
		}

		private void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId);
			UpdateIsInteractingForRefresh(true);
		}

		private void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());
			UpdateIsInteractingForRefresh(false);
		}

		private void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());
		}

		private void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());
		}

		/////////////////////////////////////////////////////
		////////////   IRefreshInfoProvider  ////////////////
		/////////////////////////////////////////////////////
		private void OnRefreshStarted()
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			RaiseRefreshStarted();
		}

		private void OnRefreshCompleted()
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			RaiseRefreshCompleted();
		}

		event_token InteractionRatioChanged(TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs>& handler)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "Add Handler");
			return m_InteractionRatioChangedEventSource.add(handler);
		}

		private void InteractionRatioChanged(event_token& token)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "Remove Handler");
			m_InteractionRatioChangedEventSource.remove(token);
		}

		event_token IsInteractingForRefreshChanged(TypedEventHandler<IRefreshInfoProvider, object>& handler)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "Add Handler");
			return m_IsInteractingForRefreshChangedEventSource.add(handler);
		}

		void IsInteractingForRefreshChanged(event_token& token)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "Remove Handler");
			m_IsInteractingForRefreshChangedEventSource.remove(token);
		}

		event_token RefreshStarted(TypedEventHandler<IRefreshInfoProvider, object>& handler)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "Add Handler");
			return m_RefreshStartedEventSource.add(handler);
		}

		void RefreshStarted(event_token& token)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "Remove Handler");
			m_RefreshStartedEventSource.remove(token);
		}

		event_token RefreshCompleted(TypedEventHandler<IRefreshInfoProvider, object>& handler)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "Add Handler");
			return m_RefreshCompletedEventSource.add(handler);
		}

		private void RefreshCompleted(event_token& token)
		{
			PTR_TRACE_INFO(null, TRACE_MSG_METH_STR, METH_NAME, this, "Remove Handler");
			m_RefreshCompletedEventSource.remove(token);
		}

		private double ExecutionRatio()
		{
			return m_executionRatio;
		}

		private string InteractionRatioCompositionProperty()
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			return m_interactionRatioCompositionProperty;
		}

		private CompositionPropertySet CompositionProperties()
		{
			return m_compositionProperties;
		}

		private bool IsInteractingForRefresh()
		{
			return m_isInteractingForRefresh;
		}

		/////////////////////////////////////////////////////
		///////////       Private Helpers       /////////////
		/////////////////////////////////////////////////////
		private void RaiseInteractionRatioChanged(double interactionRatio)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_DBL, METH_NAME, this, interactionRatio);

			m_compositionProperties.InsertScalar(m_interactionRatioCompositionProperty, (float)(interactionRatio));

			if (m_interactionRatioChangedCount == 0 || AreClose(interactionRatio, 0.0) || AreClose(interactionRatio, m_executionRatio))
			{
				if (m_InteractionRatioChangedEventSource)
				{
					var interactionRatioChangedArgs = new RefreshInteractionRatioChangedEventArgs(interactionRatio);
					m_InteractionRatioChangedEventSource(this, interactionRatioChangedArgs);
				}
				m_interactionRatioChangedCount = 1;
			}
			else if (m_interactionRatioChangedCount >= RAISE_INTERACTION_RATIO_CHANGED_FREQUENCY)
			{
				m_interactionRatioChangedCount = 0;
			}
			else
			{
				m_interactionRatioChangedCount++;
			}
		}

		private bool AreClose(double interactionRatio, double target)
		{
			return Math.Abs(interactionRatio - target) < ALWAYS_RAISE_INTERACTION_RATIO_TOLERANCE;
		}

		private void RaiseIsInteractingForRefreshChanged()
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			m_IsInteractingForRefreshChangedEventSource(this, null);
		}

		private void RaiseRefreshStarted()
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			m_RefreshStartedEventSource(this, null);
		}

		private void RaiseRefreshCompleted()
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
			m_RefreshCompletedEventSource(this, null);
		}

		private void SetPeekingMode(bool peeking)
		{
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, peeking);
			m_peeking = peeking;
		}
	}
}
