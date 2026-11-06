// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference layouttransition.h, LayoutTransition.cpp, LayoutManager.cpp (RealizeRegisteredLayoutTransitions), tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using DirectUI;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

internal enum TransitionTrigger
{
	NoTrigger,
	Load,
	Unload,
	Layout,
	Reparent,
}

internal enum TransitionParent
{
	ParentToRoot,
	ParentToCommonParent,
	ParentToGrandParent,
}

//------------------------------------------------------------------------
// Storage for the snapshot that a LayoutTransition takes between layout
// changes, and storage for the properties that drive the animation.
//------------------------------------------------------------------------
internal sealed class LayoutTransitionStorage
{
	// snapshot information
	internal Size m_currentSize;
	internal Point m_currentOffset;        // offset to either parent or plugin (it is relative to plugin after lefttree, but before enterimpl)
										   // these offsets are not kept up to date during animation but are grabbed when layout runs.
	internal Point m_nextGenerationOffset; // offset is updated in arrange, but is only valid on the next layout cycle
	internal Size m_nextGenerationSize;
	internal int m_nextGenerationCounter;  // the layout cycle in which the nextgeneration offset becomes valid.

	internal Rect m_arrangeOutput;

	// used for the animation
	internal Size m_sizeStart;
	internal Size m_sizeDestination;
	internal Point m_transformStart;
	internal Point m_transformDestination;
	internal float m_opacityStart = 1.0f;
	internal float m_scaleStart = 1.0f;

	internal readonly List<Storyboard> m_transitionStoryboards = new();  // the active storyboards
	internal readonly List<Transition> m_registeredTransitions = new();  // transition instances that have registered to start a transition for this uielement
	internal bool m_bRegisteredInLayoutManager;

	// CUIElement::m_enteredTreeCounter and m_leftTreeCounter live here so an element without transitions pays nothing for them.
	internal int m_enteredTreeCounter = Transition.EnteredInThisTick;
	internal int m_leftTreeCounter = 1;

	// UC_UnlinkContainer and ItemContainerGenerator.DeferredUnlinkingPayload.
	internal DeferredUnlinkActionPayload? m_deferredUnlinkingPayload;

	private TransitionTrigger m_trigger = TransitionTrigger.NoTrigger;

	internal void RegisterElementForTransitions(UIElement pTarget, List<Transition> transitions, TransitionTrigger trigger)
	{
		if (!pTarget.IsFirstArrangeDone)
		{
			return;
		}

		UnregisterElementForTransitions(pTarget);

		// take refs of the new transitions
		if (transitions.Count > 0 &&           // only a transition if there are applicable transitions to handle the event
			SharedHelpers.IsAnimationsEnabled()) // Animations are turned off in the Control panel settings
		{
			Transition.RegisterElementForDeferredTransition(pTarget, this);    // see note in the else clause
			m_registeredTransitions.AddRange(transitions);

			SetTrigger(trigger);  // remember that these transitions were created because of this trigger
		}
		else
		{
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

	internal void RegisterStoryboard(Storyboard pStoryboard)
	{
		if (m_transitionStoryboards.Count == 0)
		{
			Transition.ElementsWithTransitionStoryboards++;
		}

		m_transitionStoryboards.Add(pStoryboard);
	}

	internal void ClearStoryboards()
	{
		if (m_transitionStoryboards.Count > 0)
		{
			Transition.ElementsWithTransitionStoryboards--;
		}

		foreach (var storyboard in m_transitionStoryboards)
		{
			storyboard.Stop();
		}

		m_transitionStoryboards.Clear();
		SetTrigger(TransitionTrigger.NoTrigger);
	}

	internal TransitionTrigger GetTrigger() => m_trigger;

	internal void SetTrigger(TransitionTrigger trigger) => m_trigger = trigger;
}

public partial class Transition
{
	internal const int EnteredInThisTick = 0;
	internal const int LeftInThisTick = 0;

	private static readonly List<UIElement> s_elementsWithDeferredTransitions = new();
	private static short s_layoutCounter = 2;

	internal static int ElementsWithTransitionStoryboards { get; set; }

	// TODO Uno: WinUI animates a layout transition element, so the element itself never moves. Here the animation is on the
	// element's own visual, and it has to be taken back out of what coordinate queries and hit testing read.
	internal static Matrix4x4 GetTotalMatrixWithoutLayoutTransitions(UIElement element)
	{
		var total = element.Visual.TotalMatrix;

		for (var current = element; current is not null; current = current.GetParent() as UIElement)
		{
			if (current.m_pLayoutTransitionStorage is { m_transitionStoryboards.Count: > 0 } &&
				current.TransitionTarget is { CompositeTransform: { } transform } transitionTarget)
			{
				var currentTotal = current.Visual.TotalMatrix;
				var size = current.Visual.Size;
				var transitionMatrix = new Matrix4x4(transform.ToMatrix(transitionTarget.TransformOrigin, new Size(size.X, size.Y)));

				if (Matrix4x4.Invert(transitionMatrix, out var invertedTransition) && Matrix4x4.Invert(currentTotal, out var invertedTotal))
				{
					total = total * invertedTotal * invertedTransition * currentTotal;
				}
			}
		}

		return total;
	}

	// CLayoutManager::GetLayoutCounter: unlike UIElement.LayoutTick, this advances once per frame, after transitions are realized.
	internal static short GetLayoutCounter() => s_layoutCounter;

	private static short GetNextLayoutCounter() => s_layoutCounter == short.MaxValue ? (short)2 : (short)(s_layoutCounter + 1); // using 2 so we have room to always subtract one for setting enter to have already happened (virtualization)

	// xcpcore.cpp, end of the frame's layout: realize, and only then advance the counter.
	internal static void OnFrameLayoutCompleted()
	{
		if (s_elementsWithDeferredTransitions.Count > 0)
		{
			RealizeRegisteredLayoutTransitions();
		}

		s_layoutCounter = GetNextLayoutCounter();
	}

	internal static void RegisterElementForDeferredTransition(UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		if (!pStorage.m_bRegisteredInLayoutManager)
		{
			pStorage.m_bRegisteredInLayoutManager = true;
			s_elementsWithDeferredTransitions.Add(pTarget);
			Uno.UI.Xaml.Core.CoreServices.RequestAdditionalFrame();
		}
	}

	internal static void UnregisterElementForDeferredTransition(UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		pStorage.m_bRegisteredInLayoutManager = false;
		s_elementsWithDeferredTransitions.Remove(pTarget);
	}

	// CUIElement::EnsureLayoutTransitionStorage
	internal static LayoutTransitionStorage EnsureLayoutTransitionStorage(UIElement pTarget)
		=> pTarget.m_pLayoutTransitionStorage ??= new LayoutTransitionStorage();

	private static Rect GetActualRect(UIElement pTarget) => pTarget.LayoutSlotWithMarginsAndAlignments;

	//------------------------------------------------------------------------
	//  Synopsis: process and take current snapshot data in preparation of a layoutchange
	//------------------------------------------------------------------------
	internal static void OnLayoutChanging(UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		// we're running layout on this element, might want to update the magic number for enteredTreeCounter and leaveTreeCounter
		if (pStorage.m_enteredTreeCounter == EnteredInThisTick)
		{
			pStorage.m_enteredTreeCounter = s_layoutCounter;
		}
		if (pStorage.m_leftTreeCounter == LeftInThisTick)
		{
			pStorage.m_leftTreeCounter = s_layoutCounter;
		}

		// before layout starts (and new visualoffset and rendersizes are set), information that was
		// gathered during last layout cycle becomes the new assumed position of this element.
		if (s_layoutCounter >= pStorage.m_nextGenerationCounter)
		{
			pStorage.m_currentOffset = pStorage.m_nextGenerationOffset;
			pStorage.m_currentSize = pStorage.m_nextGenerationSize;
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis: Finish setup and register transition instances.
	//------------------------------------------------------------------------
	internal static void OnLayoutChanged(UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		var currentTrigger = pStorage.GetTrigger();
		var allowTransitions = GetAllowsTransitionsToRun(pTarget);

		// applies only on the very first creation of this element: the nextgeneration
		// counter is 0 means it has never run layout before.
		// In this case the 'next generation' informationset, is the set that should be used for
		// a potential load transition.
		if (pStorage.m_nextGenerationCounter == 0)
		{
			// never had layout before
			SetNextGenerationInformationFromLayout(pTarget, pStorage, GetNextLayoutCounter());
			pStorage.m_currentOffset = pStorage.m_nextGenerationOffset;
			pStorage.m_currentSize = pStorage.m_nextGenerationSize;
		}

		// regardless of whether we wish to animate, the destination should always be correct.
		// When a new slot comes in that does not exceed the configured delta, the transition destination
		// might not be updated. However, after the transition has finished we wish to make sure
		// we end up in the correct location as layed out right now.
		pStorage.m_arrangeOutput = GetActualRect(pTarget);

		if (currentTrigger != TransitionTrigger.Unload)   // no other transition possible when unloading
		{
			if (allowTransitions)
			{
				// before calling to Impl, make sure the current offset is correct.
				// we _only_ want to update the current offset if there is an active transition, otherwise
				// trust the last set information.
				// this is the version of m_currentOffset and m_currentSize that is used to trigger transitions.
				SetInformationFromAnimation(pTarget, pStorage, GetNextLayoutCounter());

				int layoutCounter = s_layoutCounter;
				int enterCounter = pStorage.m_enteredTreeCounter;
				int leftCounter = pStorage.m_leftTreeCounter;

				// note how unloading is not triggered from layout, so we only manage load and layout

				// load transition
				if (enterCounter == layoutCounter && leftCounter != layoutCounter)
				{
					ProcessLoadTrigger(pTarget, pStorage);
				}
				// layout transition
				else if (enterCounter < layoutCounter || (enterCounter == layoutCounter && leftCounter == layoutCounter))
				{
					ProcessLayoutTrigger(pTarget, pStorage);
				}
			}
			else if (!HasActiveTransition(pTarget))
			{
				// if we got layout while we were explicitly ignoring a transition, we want our next layout to
				// 'act' as though it originated from the newly set location.
				// This whole flag 'allow transitions to run' is really a hack, and just used by virtualizing panels.
				// It should be interpreted as 'we are correcting layout after a manipulation'.
				SetNextGenerationInformationFromLayout(pTarget, pStorage, s_layoutCounter);
			}
		}

		// call to update current information. If we have a transition at this point, it will use that information
		// otherwise use this layout information
		if (allowTransitions)
		{
			SetNextGenerationInformationFromLayout(pTarget, pStorage, GetNextLayoutCounter());
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis: Allows transitions to be registered
	//------------------------------------------------------------------------
	private static void ProcessLoadTrigger(UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		var newDestination = GetActualRect(pTarget);

		// register this transition instance. This will unregister any other transition already registered.
		// build up list of transition instances that will participate
		var applicableTransitions = AppendAllTransitionsNoAddRefs(pTarget, TransitionTrigger.Load);

		// first thing that we need is to cancel transitions, so that we get the correct information inside of pStorage.
		// notice how we can make multiple calls to cancellation (each transition for this pTarget will hit this code)
		// but transitions that are being called are _current_ animations, while we are not setting up _future_ transitions.
		CancelTransitions(pTarget);

		pStorage.m_sizeDestination = new Size(newDestination.Width, newDestination.Height);
		pStorage.m_transformDestination = new Point(newDestination.X, newDestination.Y);

		// setup start according to destination snapshot
		pStorage.m_sizeStart = pStorage.m_sizeDestination;
		pStorage.m_transformStart = pStorage.m_transformDestination;

		pStorage.m_opacityStart = 1.0f;    // probably animated
		pStorage.m_scaleStart = 1.0f;

		pStorage.RegisterElementForTransitions(pTarget, applicableTransitions, TransitionTrigger.Load);  // register transitions to be called back later for setting up storyboard
	}

	private static void ProcessLayoutTrigger(UIElement pTarget, LayoutTransitionStorage pStorage)
	{
		var newDestination = GetActualRect(pTarget);
		int layoutCounter = s_layoutCounter;
		var trigger = TransitionTrigger.Layout;
		const double rDelta = 5.0;
		bool exceededDelta;

		if (pStorage.m_enteredTreeCounter == layoutCounter && pStorage.m_leftTreeCounter == layoutCounter)
		{
			trigger = TransitionTrigger.Reparent;
		}

		// compare against the destination if we are in progress, otherwise against current situation
		if (HasTransitionAnimations(pTarget))
		{
			exceededDelta = Math.Abs(newDestination.Width - pStorage.m_sizeDestination.Width) > rDelta
				|| Math.Abs(newDestination.Height - pStorage.m_sizeDestination.Height) > rDelta
				|| Math.Abs(newDestination.X - pStorage.m_transformDestination.X) > rDelta
				|| Math.Abs(newDestination.Y - pStorage.m_transformDestination.Y) > rDelta;
		}
		else
		{
			exceededDelta = Math.Abs(newDestination.Width - pStorage.m_currentSize.Width) > rDelta
				|| Math.Abs(newDestination.Height - pStorage.m_currentSize.Height) > rDelta
				|| Math.Abs(newDestination.X - pStorage.m_currentOffset.X) > rDelta
				|| Math.Abs(newDestination.Y - pStorage.m_currentOffset.Y) > rDelta;
		}

		// if delta was not exceeded, current transitions will not be cancelled and their
		if (exceededDelta)
		{
			// register this transition instance. This will unregister any other transition already registered.
			// build up list of transition instances that will participate
			var applicableTransitions = AppendAllTransitionsNoAddRefs(pTarget, trigger);

			// first thing that we need is to cancel transitions, so that we get the correct information inside of pStorage.
			// notice how we can make multiple calls to cancellation (each transition for this pTarget will hit this code)
			// but transitions that are being called are _current_ animations, while we are not setting up _future_ transitions.
			// different than the other triggers, we only have to cancel if we are _truly_ setting up a transition (exceeded delta)
			CancelTransitions(pTarget);

			// setup start according to snapshot
			pStorage.m_sizeStart = pStorage.m_currentSize;
			pStorage.m_transformStart = pStorage.m_currentOffset;

			pStorage.m_sizeDestination = new Size(newDestination.Width, newDestination.Height);
			pStorage.m_transformDestination = new Point(newDestination.X, newDestination.Y);

			pStorage.m_opacityStart = 1.0f;
			pStorage.m_scaleStart = 1.0f;

			pStorage.RegisterElementForTransitions(pTarget, applicableTransitions, trigger);  // register transitions to be called back later for setting up storyboard
		}
		else if (pStorage.m_transitionStoryboards.Count == 0)
		{
			// so another layout has come in during this tick that did not exceed our delta. That means that we might have put back
			// the item. If for some reason we already had a layouttransition setup (to run) we should probably not execute it.
			// But on the other hand, if there is a running transition and all you are doing is somehow moving the element (on the target location)
			// by <exceededDelta there is no reason to cancel it. So we only care about not starting a new transition.
			pStorage.UnregisterElementForTransitions(pTarget);
		}
	}

	private static void ProcessUnloadTrigger(UIElement pTarget, LayoutTransitionStorage pStorage, List<Transition> applicableTransitions)
	{
		// transformStart will represent where we are on the screen right now
		// transformDest  will represent where layout things we are being deleted from
		// This is only important when we are in the middle of a transform and are being deleted.
		// You could imagine a transform wanting to fade out plus moving to that location.
		// (AddDeleteThemeTransition)

		if (s_layoutCounter >= pStorage.m_nextGenerationCounter)
		{
			pStorage.m_currentOffset = pStorage.m_nextGenerationOffset;
			pStorage.m_currentSize = pStorage.m_nextGenerationSize;
		}

		var newDestinationOffset = pStorage.m_currentOffset;
		var newDestinationSize = pStorage.m_currentSize;

		// first thing that we need is to cancel transitions, so that we get the correct information inside of pStorage.
		// notice how we can make multiple calls to cancellation (each transition for this pTarget will hit this code)
		// but transitions that are being called are _current_ animations, while we are not setting up _future_ transitions.
		CancelTransitions(pTarget);

		// setup start according to destination snapshot
		pStorage.m_sizeStart = pStorage.m_currentSize;
		pStorage.m_transformStart = pStorage.m_currentOffset;

		pStorage.m_sizeDestination = newDestinationSize;
		pStorage.m_transformDestination = newDestinationOffset;

		pStorage.m_opacityStart = 1.0f;
		pStorage.m_scaleStart = 1.0f;

		pStorage.RegisterElementForTransitions(pTarget, applicableTransitions, TransitionTrigger.Unload);  // register transitions to be called back later for setting up storyboard
	}

	// CUIElementCollection::UnloadElement: returns true when the element is kept alive until its unload transition completes.
	internal static bool UnloadElement(UIElementCollection collection, UIElement pRemove)
	{
		if (pRemove.m_pLayoutTransitionStorage is not { } pStorage)
		{
			return false;
		}

		// we allow panels to 'ignore transitions'.
		if (pRemove.GetParent() is Panel { IsIgnoringTransitions: false })
		{
			var applicableUnloadTransitions = AppendAllTransitionsNoAddRefs(pRemove, TransitionTrigger.Unload);

			if (applicableUnloadTransitions.Count > 0 && // has unload transition
				pRemove.IsActiveInVisualTree && // is not itself already unloaded
				pRemove.IsFirstArrangeDone) // has not been shutdown by parent tree
			{
				pStorage.m_leftTreeCounter = s_layoutCounter;
				// we have left the tree, even though we might not yet have run leaveimpl.
				ProcessUnloadTrigger(pRemove, pStorage, applicableUnloadTransitions);

				if (HasActiveTransition(pRemove, TransitionTrigger.Unload))
				{
					collection.AddUnloadingElement(pRemove);
					return true;
				}
			}
		}

		// did not unload the element. So it will just be removed
		// should stop current transition on it
		// this will as a side effect also set the correct currentInfo on
		// the pStorage
		pStorage.UnregisterElementForTransitions(pRemove);
		return false;
	}

	//-------------------------------------------------------------------------
	//  Synopsis:   Create all the layouttransitions that have been registered.
	//
	//  Groups all the elements (and their layoutrects) per layouttransition,
	//  raise the starting event and then setup the transition.
	//-------------------------------------------------------------------------
	private static void RealizeRegisteredLayoutTransitions()
	{
		// we group all layouttransitions per registering layouttransition object
		List<(Transition transition, List<(UIElement element, Rect rect)> elementRects)> groups = new();
		// keep a list of targets that we are setting up transitions for
		List<UIElement> elementsTransitioned = new();

		// fill the groups dictionary with layoutslots
		foreach (var element in s_elementsWithDeferredTransitions.ToArray())
		{
			var pStorage = element.m_pLayoutTransitionStorage!;

			if (pStorage.GetTrigger() != TransitionTrigger.NoTrigger)
			{
				// get the layoutslot as we will animate it
				var layoutSlot = new Rect(pStorage.m_transformStart, pStorage.m_sizeStart);

				// fill the groups
				foreach (var transition in pStorage.m_registeredTransitions)
				{
					var index = groups.FindIndex(g => ReferenceEquals(g.transition, transition));
					if (index < 0)
					{
						index = groups.Count;
						groups.Add((transition, new()));
					}
					// finally store the combination of the layoutrect and the element for later use
					groups[index].elementRects.Add((element, layoutSlot));
				}
			}

			// the call to unregister will remove from deferredstorage as well, which will be slow
			// since we _know_ we will clear out the deferred vector, lets skip that
			pStorage.m_bRegisteredInLayoutManager = false;

			pStorage.UnregisterElementForTransitions(element);   // note how the trigger remains untouched.
		}

		s_elementsWithDeferredTransitions.Clear();

		foreach (var (transition, elementRects) in groups)
		{
			// handle staggering
			double staggerDelay = 0;
			double staggerReduce = 1;
			var isStaggering = transition.GetIsStaggeringEnabled() && transition.GetStaggerFunction(out staggerDelay, out staggerReduce);

			// PVLStaggerFunction::GetTransitionDelays: the running time starts at the first delay.
			double runningTime = staggerDelay;

			// now do the actual work of calling setuptransition for each element
			// passing in the storyboard that we might have gotten from the aggregated starting event.
			foreach (var (transitionedElement, _) in elementRects)
			{
				// puts the target in a list so that we can later validate that storyboards were setup
				elementsTransitioned.Add(transitionedElement);

				// will create and call begin on storyboards
				SetupTransition(transitionedElement, transition, isStaggering ? runningTime : 0);

				staggerDelay *= staggerReduce;
				runningTime += staggerDelay;
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

	//------------------------------------------------------------------------
	//  Synopsis: Creates and starts the animation for this transition.
	//------------------------------------------------------------------------
	private static void SetupTransition(UIElement pTarget, Transition pTransition, double staggeredBeginMs)
	{
		var pStorage = pTarget.m_pLayoutTransitionStorage!;
		List<Storyboard> storyboards = new();

		// calls into transition instance to create a storyboard
		pTransition.CreateStoryboard(
			pTarget,
			new Rect(pStorage.m_transformStart, pStorage.m_sizeStart),
			new Rect(pStorage.m_transformDestination, pStorage.m_sizeDestination),
			pStorage.GetTrigger(),
			storyboards,
			out _);

		foreach (var pStoryboard in storyboards)
		{
			// TODO Uno: WinUI retargets the storyboard to a CLayoutTransitionElement which draws in place of the element. The element's
			// own TransitionTarget renders the same way without a second visual, so one element cannot show two storyboards at once.
			if (pStorage.m_transitionStoryboards.Count == 0)
			{
				var transitionTarget = pTarget.TransitionTarget ??= new TransitionTarget();
				var transform = transitionTarget.CompositeTransform!;

				// SetValue on the properties that are changed by a transition
				// this causes us to be able to easily stagger an animation by setting a begintime on it
				// NOTE: this means you are going to have to always animate to the destination value, even if you don't want to move it there
				transform.TranslateX = pStorage.m_transformStart.X - pStorage.m_transformDestination.X;
				transform.TranslateY = pStorage.m_transformStart.Y - pStorage.m_transformDestination.Y;
				transform.ScaleX = pStorage.m_scaleStart;
				transform.ScaleY = pStorage.m_scaleStart;

				// start off invisible
				transitionTarget.Opacity = pStorage.GetTrigger() == TransitionTrigger.Load && staggeredBeginMs > 0 ? 0.0 : pStorage.m_opacityStart;
			}

			// stagger
			// TODO Uno: Storyboard.BeginTime is not honored, so the stagger goes on its children.
			if (staggeredBeginMs > 0)
			{
				foreach (var child in pStoryboard.Children)
				{
					child.BeginTime = (child.BeginTime ?? TimeSpan.Zero) + TimeSpan.FromMilliseconds(staggeredBeginMs);
				}
			}

			// manage storyboard instance - beginning and cancellation
			pStoryboard.Completed += (sender, _) => OnTransitionCompleted(pTarget, (Storyboard)sender!);
			pStorage.RegisterStoryboard(pStoryboard);
			pStoryboard.Begin();
		}

		// let the world know that we've started a transition
		if (storyboards.Count > 0 && pTarget is ILayoutTransitionStoryboardNotification spNotification)
		{
			spNotification.NotifyLayoutTransitionStart();
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis:   Handles the completed event of our transition.
	//              Is used by system (event) to reset element to its final
	//              position by removing all effects of a transition on the
	//              element. Will cancel the transition.
	//------------------------------------------------------------------------
	private static void OnTransitionCompleted(UIElement pTarget, Storyboard pStoryboard)
	{
		if (pTarget.m_pLayoutTransitionStorage is not { } pStorage)
		{
			return;
		}

		// scenario: completed events on multiple animations are being called
		// all animations have indeed already ended and thus this method
		// will call CancelTransitions which will have removed the
		// completedData. That is fine.
		if (!pStorage.m_transitionStoryboards.Contains(pStoryboard))
		{
			return;
		}

		if (!HasRunningTransitionAnimations(pStorage))
		{
			// no more running transitions. This was the last one to complete.

			var wasUnload = pStorage.GetTrigger() == TransitionTrigger.Unload;

			CancelTransitions(pTarget);  // note how this call will update the current offset and we are overwriting below.

			if (!wasUnload)
			{
				// so: set current offset to the completed rect
				// so that we do not accidentally schedule a new LT.
				pStorage.m_currentOffset = pStorage.m_nextGenerationOffset = new Point(pStorage.m_arrangeOutput.X, pStorage.m_arrangeOutput.Y);
				pStorage.m_currentSize = pStorage.m_nextGenerationSize = new Size(pStorage.m_arrangeOutput.Width, pStorage.m_arrangeOutput.Height);
				pStorage.m_nextGenerationCounter = s_layoutCounter;
			}
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis: Cancels all the transitions.
	//------------------------------------------------------------------------
	internal static void CancelTransitions(UIElement pTarget)
	{
		if (pTarget.m_pLayoutTransitionStorage is not { } pStorage)
		{
			return;  // no storage, means per definition no transitions.
		}

		var trigger = pStorage.GetTrigger();
		var hadStoryboards = pStorage.m_transitionStoryboards.Count > 0;

		// before removing the brush, update the current information in storage
		SetInformationFromAnimation(pTarget, pStorage, GetNextLayoutCounter()); // note how removing the storyboard will reset the destinationelements values

		// finally remove the storyboards
		pStorage.ClearStoryboards();
		pStorage.UnregisterElementForTransitions(pTarget);

		if (hadStoryboards)
		{
			if (pTarget is ILayoutTransitionStoryboardNotification spNotification)
			{
				spNotification.NotifyLayoutTransitionEnd();
			}

			// cleans up what CleanupBrushRepresentations would have taken away with the brush.
			if (pTarget.TransitionTarget is { CompositeTransform: { } transform } transitionTarget)
			{
				transform.TranslateX = 0;
				transform.TranslateY = 0;
				transform.ScaleX = 1;
				transform.ScaleY = 1;
				transitionTarget.Opacity = 1;
			}
		}

		if (trigger == TransitionTrigger.Unload)
		{
			// unload target
			// parent might have been destroyed already. In that case this canceltransition has been triggered through the
			// destructor of its collection.
			if (pTarget.GetParent() is Panel pParent)
			{
				pParent.Children.RemoveUnloadedElement(pTarget);  // will set the lifecycle to unloaded
			}
		}
	}

	private static void ValidateTransitionWasSetup(UIElement pTarget)
	{
		if (pTarget.m_pLayoutTransitionStorage is not { } pStorage)
		{
			return;
		}

		if (pStorage.GetTrigger() != TransitionTrigger.NoTrigger && pStorage.m_transitionStoryboards.Count == 0)
		{
			// so we had transitions that when asked for a storyboard, ended up returning nothing
			// we need to fully cancel
			CancelTransitions(pTarget);
		}
	}

	//------------------------------------------------------------------------
	//  Synopsis: Gets the transition that should be used.
	//------------------------------------------------------------------------
	private static List<Transition> AppendAllTransitionsNoAddRefs(UIElement pTarget, TransitionTrigger trigger)
	{
		List<Transition> applicableTransitions = new();

		if (pTarget.GetParent() is Panel pVisualParent && pVisualParent.GetTransitionsForChildElementNoAddRef(pTarget) is { } pInheritedCollection)
		{
			foreach (var transition in pInheritedCollection)
			{
				if (transition.ParticipatesInTransition(pTarget, trigger))
				{
					applicableTransitions.Add(transition);
				}
			}
		}

		return applicableTransitions;
	}

	//------------------------------------------------------------------------
	//  Synopsis: TRUE if this element is currently transitioning
	//------------------------------------------------------------------------
	private static bool HasTransitionAnimations(UIElement pTarget)
		=> pTarget.m_pLayoutTransitionStorage is { m_transitionStoryboards.Count: > 0 };

	private static bool HasRunningTransitionAnimations(LayoutTransitionStorage pStorage)
	{
		foreach (var storyboard in pStorage.m_transitionStoryboards)
		{
			if (storyboard.State == Timeline.TimelineState.Active)
			{
				return true;
			}
		}

		return false;
	}

	// true if there is a transition set on the element (might not have started yet, so may not yet have a storyboard)
	private static bool HasActiveTransition(UIElement pTarget)
		=> pTarget.m_pLayoutTransitionStorage is { } pStorage && pStorage.GetTrigger() != TransitionTrigger.NoTrigger;

	private static bool HasActiveTransition(UIElement pTarget, TransitionTrigger trigger)
		=> pTarget.m_pLayoutTransitionStorage is { } pStorage && pStorage.GetTrigger() == trigger;

	// updates current location from pTarget to the storage with the always correct information from destinationElement
	private static void SetInformationFromAnimation(UIElement pTarget, LayoutTransitionStorage pStorage, int nextGenerationCounter)
	{
		// have a bcb, so use that instead of the m_rLayoutTransition values
		if (pStorage.m_transitionStoryboards.Count > 0 && pTarget.TransitionTarget?.CompositeTransform is { } transform)
		{
			pStorage.m_currentOffset = pStorage.m_nextGenerationOffset = new Point(
				pStorage.m_transformDestination.X + transform.TranslateX,
				pStorage.m_transformDestination.Y + transform.TranslateY);

			// the information is updated to the best of our current knowledge. Use this in the next
			// layoutcycle.
			pStorage.m_nextGenerationCounter = nextGenerationCounter;
		}
	}

	//---------------------------------------------------------------------------
	// Synopsis: Updates the next generation information. This information will
	// transition into the current information set on the next layout cycle.
	//
	// Note how information from the destination element trumps information from layout.
	//---------------------------------------------------------------------------
	private static void SetNextGenerationInformationFromLayout(UIElement pTarget, LayoutTransitionStorage pStorage, int nextGenerationCounter)
	{
		if (pStorage.m_transitionStoryboards.Count > 0 && pTarget.TransitionTarget?.CompositeTransform is { } transform)
		{
			pStorage.m_nextGenerationOffset = new Point(
				pStorage.m_transformDestination.X + transform.TranslateX,
				pStorage.m_transformDestination.Y + transform.TranslateY);
		}
		else
		{
			// not in a transition, so the offsets that are set by layout are correct
			var rect = GetActualRect(pTarget);
			pStorage.m_nextGenerationOffset = new Point(rect.X, rect.Y);

			// it seems counterintuitive not to update size always, but when an element has left the tree
			// and shutdown, we actually remove layoutstorage, so the sizes would return 0
			pStorage.m_nextGenerationSize = new Size(rect.Width, rect.Height);
		}
		// information-set has been updated, use it in the next layoutcyle.
		pStorage.m_nextGenerationCounter = nextGenerationCounter;
	}

	// true if this element is disabling transitions for its subtree
	private static bool GetAllowsTransitionsToRun(UIElement pTarget)
		=> pTarget.GetParent() is not Panel { IsIgnoringTransitions: true };
}
