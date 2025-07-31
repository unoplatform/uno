// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshInfoProviderImpl.cpp, commit de78834

using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Private.Controls;

internal partial class RefreshInfoProviderImpl : IRefreshInfoProvider, IInteractionTrackerOwner
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

	//~RefreshInfoProviderImpl()
	//{
	//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
	//}

	public RefreshInfoProviderImpl(RefreshPullDirection refreshPullDirection, Size refreshVisualizerSize, Compositor compositor)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		m_refreshPullDirection = refreshPullDirection;
		m_refreshVisualizerSize = refreshVisualizerSize;
		m_compositionProperties = compositor.CreatePropertySet();
	}

	internal void UpdateIsInteractingForRefresh(bool value)
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
	public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
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

	public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId);
	}

	public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId);
		UpdateIsInteractingForRefresh(true);
	}

	public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());
		UpdateIsInteractingForRefresh(false);
	}

	public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());
	}

	public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, args.RequestId());
	}

	/////////////////////////////////////////////////////
	////////////   IRefreshInfoProvider  ////////////////
	/////////////////////////////////////////////////////
	public void OnRefreshStarted()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		RaiseRefreshStarted();
	}

	public void OnRefreshCompleted()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		RaiseRefreshCompleted();
	}

	public double ExecutionRatio => m_executionRatio;

	public string InteractionRatioCompositionProperty => m_interactionRatioCompositionProperty;

	public CompositionPropertySet CompositionProperties => m_compositionProperties;

	public bool IsInteractingForRefresh => m_isInteractingForRefresh;

	/////////////////////////////////////////////////////
	///////////       Private Helpers       /////////////
	/////////////////////////////////////////////////////
	private void RaiseInteractionRatioChanged(double interactionRatio)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_DBL, METH_NAME, this, interactionRatio);

		m_compositionProperties.InsertScalar(m_interactionRatioCompositionProperty, (float)(interactionRatio));

		if (m_interactionRatioChangedCount == 0 || AreClose(interactionRatio, 0.0) || AreClose(interactionRatio, m_executionRatio))
		{
			if (InteractionRatioChanged is not null)
			{
				var interactionRatioChangedArgs = new RefreshInteractionRatioChangedEventArgs(interactionRatio);
				InteractionRatioChanged?.Invoke(this, interactionRatioChangedArgs);
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
		IsInteractingForRefreshChanged?.Invoke(this, null);
	}

	private void RaiseRefreshStarted()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		RefreshStarted?.Invoke(this, null);
	}

	private void RaiseRefreshCompleted()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		RefreshCompleted?.Invoke(this, null);
	}

	internal void SetPeekingMode(bool peeking)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, peeking);
		m_peeking = peeking;
	}
}
