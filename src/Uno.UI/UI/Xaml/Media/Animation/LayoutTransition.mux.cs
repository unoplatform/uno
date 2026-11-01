// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutTransition.cpp and LayoutManager.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Transition
{
	// Uno: CLayoutManager's deferred transition state. Uno has no per-tree layout manager, so it is static.
	private static readonly List<UIElement> s_elementsWithDeferredTransitions = new();
	private static readonly List<UIElement> s_elementsEnteredTree = new();

	// Uno: CCoreServices::GetDefaultNavigationTransition, created the first time a page under a frame navigates.
	private static Transition? s_defaultNavigationTransition;

	/// <summary>
	/// Whether this transition runs for <paramref name="element"/> on <paramref name="transitionTrigger"/> (ITransitionPrivate::ParticipatesInTransition).
	/// </summary>
	/// <remarks>Transitions that keep using the <see cref="AttachToElement"/> path return false.</remarks>
	internal virtual bool ParticipatesInTransition(UIElement element, TransitionTrigger transitionTrigger) => false;

	/// <summary>
	/// Creates the storyboards running this transition on <paramref name="element"/> (ITransitionPrivate::CreateStoryboard).
	/// </summary>
	internal virtual void CreateStoryboard(UIElement element, Rect start, Rect destination, TransitionTrigger transitionTrigger, IList<Storyboard> storyboards)
	{
	}

	// MUX Reference CLayoutManager::RegisterElementForDeferrredTransition
	internal static void RegisterElementForDeferredTransition(UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		if (!pStorage.m_bRegisteredInLayoutManager)
		{
			s_elementsWithDeferredTransitions.Add(pTarget);
			pStorage.m_bRegisteredInLayoutManager = true;
		}
	}

	// MUX Reference CLayoutManager::UnregisterElementForDeferredTransition
	internal static void UnregisterElementForDeferredTransition(UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		if (pStorage.m_bRegisteredInLayoutManager && s_elementsWithDeferredTransitions.Count > 0)
		{
			if (s_elementsWithDeferredTransitions.Remove(pTarget))
			{
				pStorage.m_bRegisteredInLayoutManager = false;
			}
		}
	}

	/// <summary>
	/// Uno-specific stand-in for <c>m_enteredTreeCounter</c>: the Load trigger of an element that entered the live tree
	/// is processed once layout has run, like WinUI's <c>ProcessLayoutForTransition</c> during arrange.
	/// </summary>
	internal static void OnElementEnteredTree(UIElement pTarget) => s_elementsEnteredTree.Add(pTarget);

	//-------------------------------------------------------------------------
	//
	//  Synopsis:   Create all the layouttransitions that have been registered.
	//
	//  Groups all the elements (and their layoutrects) per layouttransition,
	//  raise the starting event and then setup the transition.
	//-------------------------------------------------------------------------
	// MUX Reference CLayoutManager::RealizeRegisteredLayoutTransitions
	/// <returns>Whether any transition was set up, so layout should run again.</returns>
	internal static bool RealizeRegisteredLayoutTransitions()
	{
		if (s_elementsEnteredTree.Count > 0)
		{
			var entered = s_elementsEnteredTree.ToArray();
			s_elementsEnteredTree.Clear();

			foreach (var element in entered)
			{
				if (element.IsActiveInVisualTree)
				{
					ProcessLoadTrigger(element);
				}
			}
		}

		if (s_elementsWithDeferredTransitions.Count == 0)
		{
			return false;
		}

		// we group all layouttransitions per registering layouttransition object
		var groups = new List<(Transition Transition, List<UIElement> Elements)>();
		// keep a list of targets that we are setting up transitions for
		var elementsTransitioned = new List<UIElement>();

		try
		{
			// fill the groups dictionary with layoutslots
			foreach (var element in s_elementsWithDeferredTransitions.ToArray())
			{
				if (element.GetLayoutTransitionStorage() is not { } pStorage)
				{
					continue;
				}

				if (pStorage.GetTrigger() != TransitionTrigger.NoTrigger)
				{
					// TODO Uno: layout slots are not collected, as staggering is not supported.
					foreach (var transition in pStorage.m_registeredTransitions)
					{
						GetOrCreateGroup(groups, transition).Add(element);
					}
				}

				// the call to unregister will remove from deferredstorage as well, which will be slow
				// since we _know_ we will clear out the deferred vector, lets skip that
				pStorage.m_bRegisteredInLayoutManager = false;

				pStorage.UnregisterElementForTransitions(element);   // note how the trigger remains untouched.
			}

			// now do the actual work of calling setuptransition for each element
			foreach (var (transition, elements) in groups)
			{
				foreach (var transitionedElement in elements)
				{
					// puts the target in a list so that we can later validate that storyboards were setup
					elementsTransitioned.Add(transitionedElement);

					// will create and call begin on storyboards
					SetupTransition(transitionedElement, transition);
				}
			}

			// iterate over elements that have been called for SetupTransition and make sure they are
			// actually with storyboard
			foreach (var transitionTarget in elementsTransitioned)
			{
				// if they did not give a storyboard, we should cancel the transition immediately
				ValidateTransitionWasSetup(transitionTarget);
			}
		}
		finally
		{
			s_elementsWithDeferredTransitions.Clear();
		}

		return true;

		static List<UIElement> GetOrCreateGroup(List<(Transition Transition, List<UIElement> Elements)> groups, Transition transition)
		{
			foreach (var group in groups)
			{
				if (group.Transition == transition)
				{
					return group.Elements;
				}
			}

			var elements = new List<UIElement>();
			groups.Add((transition, elements));
			return elements;
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis: Allows transitions to be registered
	//------------------------------------------------------------------------
	internal static void ProcessLoadTrigger(UIElement pTarget)
	{
		var applicableTransitions = new List<Transition>();
		var trigger = TransitionTrigger.Load;

		// register this transition instance. This will unregister any other transition already registered.
		// build up list of transition instances that will participate
		AppendAllTransitionsNoAddRefs(pTarget, trigger, applicableTransitions);

		// Uno: avoid allocating storage for the many elements that never transition.
		if (applicableTransitions.Count == 0 && !pTarget.HasLayoutTransitionStorage())
		{
			return;
		}

		// first thing that we need is to cancel transitions, so that we get the correct information inside of pStorage.
		// notice how we can make multiple calls to cancellation (each transition for this pTarget will hit this code)
		// but transitions that are being called are _current_ animations, while we are not setting up _future_ transitions.
		CancelTransitions(pTarget);

		// TODO Uno: the size, offset, opacity and scale snapshots are not kept, as Uno has no LayoutTransitionElement to apply them to.

		var pStorage = pTarget.EnsureLayoutTransitionStorage();
		pStorage.RegisterElementForTransitions(pTarget, applicableTransitions, trigger);  // register transitions to be called back later for setting up storyboard
	}

	internal static void ProcessUnloadTrigger(UIElement pTarget, LayoutTransitionStorage pStorage, List<Transition> applicableTransitions)
	{
		var trigger = TransitionTrigger.Unload;

		// first thing that we need is to cancel transitions, so that we get the correct information inside of pStorage.
		// notice how we can make multiple calls to cancellation (each transition for this pTarget will hit this code)
		// but transitions that are being called are _current_ animations, while we are not setting up _future_ transitions.
		CancelTransitions(pTarget);

		// TODO Uno: the size, offset, opacity and scale snapshots are not kept, as Uno has no LayoutTransitionElement to apply them to.

		pStorage.RegisterElementForTransitions(pTarget, applicableTransitions, trigger);  // register transitions to be called back later for setting up storyboard
	}

	//------------------------------------------------------------------------
	//  Synopsis: Creates and starts the animation for this transition.
	//------------------------------------------------------------------------
	private static void SetupTransition(UIElement pTarget, Transition pTransition)
	{
		if (pTarget.GetLayoutTransitionStorage() is not { } pStorage)
		{
			return;
		}

		// calls into transition instance to create a storyboard
		var storyboards = CreateStoryboards(pTransition, pTarget, pStorage);

		// TODO Uno: WinUI retargets the storyboards to a LayoutTransitionElement and staggers them through BeginTime.
		foreach (var pStoryboard in storyboards)
		{
			// manage storyboard instance - beginning and cancellation
			pStoryboard.LayoutTransitionTarget = pTarget;
			pStoryboard.Completed += OnTransitionCompleted;

			pStoryboard.Begin();
		}

		// not register them until the last moment, since cleaning up is hard
		foreach (var pStoryboard in storyboards)
		{
			pStorage.RegisterStoryboard(pStoryboard);
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis: Calls out to the managed peer to create a storyboard.
	//  Not batched up.
	//------------------------------------------------------------------------
	private static List<Storyboard> CreateStoryboards(Transition pTransition, UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		// Uno: start and destination are the current layout rect, as no layout snapshots are kept.
		var offset = pTarget.ActualOffset;
		var size = pTarget.ActualSize;
		var rect = new Rect(offset.X, offset.Y, size.X, size.Y);

		var storyboards = new List<Storyboard>();
		pTransition.CreateStoryboard(pTarget, rect, rect, pStorage.GetTrigger(), storyboards);
		return storyboards;
	}

	//------------------------------------------------------------------------
	//  Synopsis: determines whether this instance wants to participate in a transition.
	//  Calls out to mananged and thus should be used sparingly.
	//------------------------------------------------------------------------
	private bool ParticipateInTransitions(UIElement pTarget, TransitionTrigger trigger) => ParticipatesInTransition(pTarget, trigger);

	//------------------------------------------------------------------------
	//  Synopsis:   Handles the completed event of our transition.
	//              Is used by system (event) to reset element to its final
	//              position by removing all effects of a transition on the
	//              element. Will cancel the transition.
	//------------------------------------------------------------------------
	internal static void OnTransitionCompleted(object? pSender, object pEventArgs)
	{
		// scenario: completed events on multiple animations are being called
		// all animations have indeed already ended and thus this method
		// will call CancelTransitions which will have removed the
		// completedData. That is fine.
		if (pSender is not Storyboard { LayoutTransitionTarget: { } pTarget })
		{
			return;
		}

		if (!HasRunningTransitionAnimations(pTarget))
		{
			// no more running transitions. This was the last one to complete.

			// TODO Uno: WinUI restores the element's offset from the primary LayoutTransitionElement and, unless unloading,
			// invalidates its measure. Uno animates the element itself, so stopping the storyboards is enough.
			CancelTransitions(pTarget);  // note how this call will update the current offset and we are overwriting below.
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis: Cancels all the transitions.
	//------------------------------------------------------------------------
	internal static void CancelTransitions(UIElement pTarget)
	{
		var pStorage = pTarget.GetLayoutTransitionStorage();
		if (pStorage is null)
		{
			return;  // no storage, means per definition no transitions.
		}

		var trigger = pStorage.GetTrigger();

		// finally remove the storyboards
		pStorage.ClearStoryboards();
		pStorage.UnregisterElementForTransitions(pTarget);

		if (trigger == TransitionTrigger.Unload)
		{
			// unload target
			pStorage.RemoveUnloadedElement(pTarget);
		}
	}

	private static void ValidateTransitionWasSetup(UIElement pTarget)
	{
		var pStorage = pTarget.GetLayoutTransitionStorage();
		if (pStorage is null)
		{
			return;
		}

		var trigger = pStorage.GetTrigger();
		if (trigger != TransitionTrigger.NoTrigger)
		{
			if (pStorage.m_transitionStoryboards.Count == 0)
			{
				// so we had transitions that when asked for a storyboard, ended up returning nothing
				// we need to fully cancel
				CancelTransitions(pTarget);
			}
		}
	}

	//------------------------------------------------------------------------
	//  Helper method
	//
	//  Synopsis: Gets a transition through the parent.
	//  Currently supported:
	//   Itemscontrol. ChildTransition of an IC will be templatebound to the
	//      panel presenting elements. ChildTransition will not apply to
	//      individual elements.
	//   ContentControl. ChildTransition of a CC will be templatebound to the
	//      cp presenting the content. When switching template the first child
	//      of a cc will not inherit the childtransition. User is responsible
	//      for setting up a childtransition templatebinding in their own templates.
	//   Border. Works as one would expect.
	//------------------------------------------------------------------------
	private static TransitionCollection? GetInheritedChildTransition(UIElement? pTarget)
	{
		if (pTarget is null)
		{
			return null;
		}

		return (pTarget.GetParent() as UIElement)?.GetTransitionsForChildElementNoAddRef(pTarget);
	}

	//------------------------------------------------------------------------
	//  Synopsis: Gets the transition that should be used.
	//------------------------------------------------------------------------
	internal static void AppendAllTransitionsNoAddRefs(UIElement? pTarget, TransitionTrigger trigger, List<Transition> applicableTransitions)
	{
		if (pTarget is null)
		{
			return;
		}

		var pInheritedCollection = GetInheritedChildTransition(pTarget);

		var transitions = pTarget.Transitions;
		if (transitions is not null)
		{
			GetApplicableTransitionsNoAddRefs(transitions, pTarget, trigger, applicableTransitions);
		}

		// If we have a page and there are no transitions defined, then add the default navigation transition
		if (applicableTransitions.Count == 0 && pTarget is Page)
		{
			var defaultNavigationTransition = s_defaultNavigationTransition;
			if (defaultNavigationTransition is null)
			{
				// It can be expensive to create this transition if the dll isn't already loaded, so we will do a pre check
				// here to see if we really need this.  The page must be under a frame and we must be navigating from a
				// page.
				if (pTarget.GetParent() is ContentPresenter contentPresenter)
				{
					if (contentPresenter.GetParent() is Frame frame)
					{
						// Obviously if we have entries on the backstack then we are navigating (as opposed to being
						// the initial load, however, we also hit this code as we unload, but before it enters the
						// backstack.  This is different than when the transition is played.  By then the entry is in
						// the backstack.  So on our first transtion, at the time of unload of the "from" page, there
						// is no entry in the back stack.  Now, even though the transition on the "from" page is never
						// used (we use the navigating away from the "to" page instead), it never the less must have
						// a transition on it to even hit the transition code (where it will use the "to" page transtions).
						// So, if we are unloading, a page, we will also assume we are navigating.
						if (frame.BackStackDepth > 0 || trigger == TransitionTrigger.Unload)
						{
							defaultNavigationTransition = new NavigationThemeTransition();
							s_defaultNavigationTransition = defaultNavigationTransition;
						}
					}
				}
			}
			if (defaultNavigationTransition is not null)
			{
				var participate = defaultNavigationTransition.ParticipateInTransitions(pTarget, trigger);
				if (participate)
				{
					applicableTransitions.Add(defaultNavigationTransition);
				}
			}
		}

		if (pInheritedCollection is not null)
		{
			GetApplicableTransitionsNoAddRefs(pInheritedCollection, pTarget, trigger, applicableTransitions);
		}
	}

	// MUX Reference CTransitionCollection::GetApplicableTransitionsNoAddRefs
	private static void GetApplicableTransitionsNoAddRefs(TransitionCollection transitions, UIElement pTarget, TransitionTrigger trigger, List<Transition> applicableTransitions)
	{
		foreach (var transition in transitions)
		{
			if (transition is not null && transition.ParticipateInTransitions(pTarget, trigger))
			{
				applicableTransitions.Add(transition);
			}
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis: TRUE if this element is currently transitioning
	//------------------------------------------------------------------------
	internal static bool HasTransitionAnimations(UIElement? pTarget) =>
		pTarget is not null && pTarget.GetLayoutTransitionStorage() is { } storage && storage.m_transitionStoryboards.Count > 0;

	internal static bool HasRunningTransitionAnimations(UIElement pTarget)
	{
		if (pTarget.GetLayoutTransitionStorage() is { } pStorage)
		{
			foreach (var pSB in pStorage.m_transitionStoryboards)
			{
				if (pSB.GetCurrentState() == ClockState.Active)
				{
					return true;
				}
			}
		}

		return false;
	}

	internal static bool HasActiveTransition(UIElement? pTarget) =>
		pTarget is not null && pTarget.GetLayoutTransitionStorage() is { } storage && storage.GetTrigger() != TransitionTrigger.NoTrigger;

	internal static bool HasActiveTransition(UIElement? pTarget, TransitionTrigger trigger) =>
		pTarget is not null && pTarget.GetLayoutTransitionStorage() is { } storage && storage.GetTrigger() == trigger;
}
