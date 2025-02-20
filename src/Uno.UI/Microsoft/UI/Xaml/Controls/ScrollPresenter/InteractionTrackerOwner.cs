// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

// Class that handles the InteractionTracker callbacks. Has a weak reference back to the ScrollPresenter so it can be garbage-collected, 
// since the InteractionTracker keeps a strong reference to this object.
internal sealed partial class InteractionTrackerOwner : IInteractionTrackerOwner
{
	private WeakReference<ScrollPresenter> m_owner;

	public InteractionTrackerOwner(ScrollPresenter scrollPresenter)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(scrollPresenter, TRACE_MSG_METH_PTR, METH_NAME, this, scrollPresenter);

		m_owner = new WeakReference<ScrollPresenter>(scrollPresenter);
	}

	//~InteractionTrackerOwner()
	//{
	//	 SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);

	//	m_owner = null;
	//}

	#region IInteractionTrackerOwner
	public void ValuesChanged(
		InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
	{
		if (m_owner is null)
		{
			return;
		}

		if (m_owner.TryGetTarget(out var rawOwner))
		{
			var scrollPresenter = rawOwner;
			if (scrollPresenter is not null)
			{
				scrollPresenter.ValuesChanged(args);
			}
		}
	}

	public void RequestIgnored(
		InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
	{
		if (m_owner is null)
		{
			return;
		}

		if (m_owner.TryGetTarget(out var rawOwner))
		{
			var scrollPresenter = rawOwner;
			if (scrollPresenter is not null)
			{
				scrollPresenter.RequestIgnored(args);
			}
		}
	}

	public void InteractingStateEntered(
		InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
	{
		if (m_owner is null)
		{
			return;
		}

		if (m_owner.TryGetTarget(out var rawOwner))
		{
			var scrollPresenter = rawOwner;
			if (scrollPresenter is not null)
			{
				scrollPresenter.InteractingStateEntered(args);
			}
		}
	}

	public void InertiaStateEntered(
		InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
	{
		if (m_owner is null)
		{
			return;
		}

		if (m_owner.TryGetTarget(out var rawOwner))
		{
			var scrollPresenter = rawOwner;
			if (scrollPresenter is not null)
			{
				scrollPresenter.InertiaStateEntered(args);
			}
		}
	}

	public void IdleStateEntered(
		InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
	{
		if (m_owner is null)
		{
			return;
		}

		if (m_owner.TryGetTarget(out var rawOwner))
		{
			var scrollPresenter = rawOwner;
			if (scrollPresenter is not null)
			{
				scrollPresenter.IdleStateEntered(args);
			}
		}
	}

	public void CustomAnimationStateEntered(
		InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
	{
		if (m_owner is null)
		{
			return;
		}

		if (m_owner.TryGetTarget(out var rawOwner))
		{
			var scrollPresenter = rawOwner;
			if (scrollPresenter is not null)
			{
				scrollPresenter.CustomAnimationStateEntered(args);
			}
		}
	}
	#endregion
}
