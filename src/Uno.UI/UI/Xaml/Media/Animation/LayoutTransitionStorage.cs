// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutTransition.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Per-element state of the layout transitions (Load/Unload triggers) running on it.
/// </summary>
/// <remarks>
/// Uno only ports the trigger and storyboard bookkeeping: there are no LayoutTransitionElements,
/// so storyboards animate the element itself rather than a transition target layer.
/// </remarks>
internal sealed class LayoutTransitionStorage
{
	private TransitionTrigger m_trigger = TransitionTrigger.NoTrigger;

	internal readonly List<Transition> m_registeredTransitions = new();
	internal readonly List<Storyboard> m_transitionStoryboards = new();
	internal bool m_bRegisteredInLayoutManager;

	// Uno: WinUI keeps an unloading element in its parent's UIElementCollection unloading storage. Uno removes it
	// from its parent right away and only keeps its visual in the parent's visual until the transition ends.
	internal ContainerVisual? m_unloadingParentVisual;

	internal TransitionTrigger GetTrigger() => m_trigger;

	internal void SetTrigger(TransitionTrigger trigger) => m_trigger = trigger;

	//-------------------------------------------------------------------------
	//  Synopsis:   Registers a UIElement as needing for a transition.
	//  Usage:      Use to register the new transition to the workitems that layout
	//              will process.
	//  Note:       Calling this method replaces transitions that were previously registered.
	//-------------------------------------------------------------------------
	internal void RegisterElementForTransitions(UIElement pTarget, List<Transition> transitions, TransitionTrigger trigger)
	{
		UnregisterElementForTransitions(pTarget);

		// Check animations are disabled across the system
		var isAnimationEnabled = Uno.UI.Helpers.WinUI.SharedHelpers.IsAnimationsEnabled();

		// take refs of the new transitions
		if (transitions.Count > 0 &&           // only a transition if there are applicable transitions to handle the event
			isAnimationEnabled)                // Animations are turned off in the Control panel settings
		{
			Transition.RegisterElementForDeferredTransition(pTarget, this);    // see note in the else clause
			m_registeredTransitions.AddRange(transitions);   // todo: this inverts the order of transitions. Reason about whether that is important or not.

			SetTrigger(trigger);  // remember that these transitions were created because of this trigger
		}
		else
		{
			// NOTE: we do not unregister this element for deferred transitions. That means that an element might be in the layoutmanagers deferred list
			// more than once or in the wrong order. The realization logic has been hardened to deal with this.
			// I find the ordering of the deferred registration of low importance at the moment, but this might have to change
			// in the future.
			SetTrigger(TransitionTrigger.NoTrigger);
		}
	}

	//-------------------------------------------------------------------------
	//  Synopsis:   Removes all transitions that were registered to be realized.
	//
	//-------------------------------------------------------------------------
	internal void UnregisterElementForTransitions(UIElement pTarget)
	{
		if (m_bRegisteredInLayoutManager)
		{
			Transition.UnregisterElementForDeferredTransition(pTarget, this);
		}

		// clear out all earlier transitions that were registered for this element but not executed yet.
		m_registeredTransitions.Clear();
	}

	// MUX Reference CUIElementCollection::RemoveUnloadedElement (UIElementCollection.cpp)
	internal void RemoveUnloadedElement(UIElement pTarget)
	{
		if (m_unloadingParentVisual is { } parentVisual)
		{
			m_unloadingParentVisual = null;
			parentVisual.Children.Remove(pTarget.Visual);
		}
	}

	internal void RegisterStoryboard(Storyboard pStoryboard) => m_transitionStoryboards.Add(pStoryboard);

	//------------------------------------------------------------------------
	//  Synopsis: Cleans up the reference this storage keeps to a live storyboard
	//  by stopping.
	//  Note: does _not_ remove from storyboard vector (perf).
	//------------------------------------------------------------------------
	internal static void CleanupStoryboard(Storyboard pStoryboard)
	{
		pStoryboard.Completed -= Transition.OnTransitionCompleted;
		pStoryboard.LayoutTransitionTarget = null;

		// want to clear out the storyboard before stopping, since it has side effects
		// that are effected by having a valid instance in storage
		pStoryboard.Stop();   // will reset animated value to localvalue
	}

	//------------------------------------------------------------------------
	//  Synopsis: Cleans up the references to all storyboards and clears the list.
	//------------------------------------------------------------------------
	internal void ClearStoryboards()
	{
		foreach (var storyboard in m_transitionStoryboards)
		{
			CleanupStoryboard(storyboard);
		}
		m_transitionStoryboards.Clear();
		SetTrigger(TransitionTrigger.NoTrigger);
	}
}
