// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference LayoutTransition_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class Transition
{
	// TODO Uno: the other theme transitions (Pane, Popup, EdgeUI, Navigation) don't participate in layout transitions.
	internal virtual bool ParticipatesInTransition(UIElement element, TransitionTrigger transitionTrigger) => false;

	internal virtual void CreateStoryboard(
		UIElement element,
		Rect start,
		Rect destination,
		TransitionTrigger transitionTrigger,
		IList<Storyboard> storyboards,
		out TransitionParent parentForTransition)
		=> parentForTransition = TransitionParent.ParentToCommonParent;

	internal virtual bool GetIsStaggeringEnabled() => false;

	// ThemeGenerator::GetStaggerFunction for this transition's PVL storyboard, in milliseconds.
	internal virtual bool GetStaggerFunction(out double delay, out double delayReduce)
	{
		delay = 0;
		delayReduce = 0;
		return false;
	}

	// helper that checks for the contextprovider interface having been implemented and if so, calling it.
	private protected static void GetTransitionContext(
		UIElement element,
		out bool pRelevantInformation,
		out ThemeTransitionContext pContext)
	{
		pContext = ThemeTransitionContext.None;
		pRelevantInformation = false;

		if (element is ITransitionContextProvider pElementAsProvider)
		{
			int layoutTick = CoreImports.LayoutManager_GetLayoutTickForTransition(element);
			pContext = pElementAsProvider.GetCurrentTransitionContext(layoutTick);
			pRelevantInformation = true;
		}
	}

	private protected static bool GetSpeedOfChanges(UIElement element)
		=> element is ITransitionContextProvider pElementAsProvider && pElementAsProvider.IsCollectionMutatingFast();

	// helper that allow extra processing of the transition parent
	private protected static void SetTransitionParentForStickyHeaders(UIElement element, ref TransitionParent parentForTransition)
	{
		// transitions should not be aware of things like stickyheaders, however, this code already crept into addDeleteThemeTransition and
		// we need to escape the sticky header clip that is put on the panel. To limit the impact, i'm only changing the parent for
		// these headers.
		if (element.GetVirtualizationInformationOrNull() is { IsHeader: true })
		{
			parentForTransition = TransitionParent.ParentToGrandParent;
		}
	}

	private static void CoerceHeaderOffsets(UIElement element, ref Point sourceoffset, ref Point destoffset)
	{
		if (element.GetVirtualizationInformationOrNull() is { IsHeader: true } && element is ListViewBaseHeaderItem spHeader)
		{
			// We may have to correct the offsets
			double[] offsets = [sourceoffset.Y, destoffset.Y];
			spHeader.CoerceStickyHeaderOffsets(offsets.Length, offsets);
			sourceoffset.Y = offsets[0];
			destoffset.Y = offsets[1];
		}
	}

	private static void AddDeleteRepositionHelperLoad(
		UIElement element,
		Rect start,
		Rect destination,
		IList<Storyboard> storyboards)
	{
		Point sourceoffset = new(start.X - destination.X, start.Y - destination.Y);
		Point destoffset = new(0, 0);

		// configuration of this animation
		bool allowLoadUnload;
		bool scheduleAffectedFirst;   // pvl should define a begintime on the affected here
		bool reorderingContext = false;
		const long affectedDuration = 300; // todo: pvl might change, do I want to make yet another call to pvl to find out these times?
		bool mixedContext = false;

		GetTransitionContext(element, out var didContextCheck, out var context);

		CoerceHeaderOffsets(element, ref sourceoffset, ref destoffset);

		if (didContextCheck)
		{
			allowLoadUnload =
				context is ThemeTransitionContext.SingleAddList or
				ThemeTransitionContext.SingleDeleteList or
				ThemeTransitionContext.SingleAddGrid or
				ThemeTransitionContext.SingleDeleteGrid or
				ThemeTransitionContext.MixedOperationsList or
				ThemeTransitionContext.MixedOperationsGrid or
				ThemeTransitionContext.MultipleAddList or
				ThemeTransitionContext.MultipleDeleteList or
				ThemeTransitionContext.MultipleAddGrid or
				ThemeTransitionContext.MultipleDeleteGrid;

			mixedContext =
				context is ThemeTransitionContext.MixedOperationsList or
				ThemeTransitionContext.MixedOperationsGrid;

			scheduleAffectedFirst =
				context is ThemeTransitionContext.SingleAddList or
				ThemeTransitionContext.SingleAddGrid or
				ThemeTransitionContext.MultipleAddList or
				ThemeTransitionContext.MultipleReorderGrid or
				ThemeTransitionContext.MultipleReorderList or
				ThemeTransitionContext.MultipleAddGrid;

			reorderingContext =
				context is ThemeTransitionContext.SingleReorderList or
				ThemeTransitionContext.SingleReorderGrid or
				ThemeTransitionContext.MultipleReorderList or
				ThemeTransitionContext.MultipleReorderGrid;
		}
		else
		{
			// no context could be gotten, so we need sane defaults.
			// this is expected for non-moco scenarios, such as a regular stackpanel
			// in practise this means that these transitions should never be used outside of MoCo
			allowLoadUnload = true;
			scheduleAffectedFirst = true;
		}

		// prepare primary storyboard. We hold off on secondary storyboard since it is not always needed
		Storyboard spPrimaryStoryboard = new();
		storyboards.Add(spPrimaryStoryboard);

		if (allowLoadUnload)
		{
			long startOffset = scheduleAffectedFirst ? affectedDuration : 0;
			if (mixedContext)
			{
				global::System.Diagnostics.Debug.Assert(!scheduleAffectedFirst);
				// first need to work through deletes, then moves. Only then should adds occur
				startOffset = 2 * affectedDuration;
			}

			ThemeGenerator.AddTimelinesForThemeAnimation(
				ThemeGenerator.TAS_ADDTOGRID,
				ThemeGenerator.TA_ADDTOGRID_ADDED,
				element,
				false,
				sourceoffset,
				destoffset,
				startOffset,
				spPrimaryStoryboard.Children);
		}
		else if (reorderingContext)
		{
			// set the current transition location for this element
			sourceoffset = destoffset;

			ThemeGenerator.AddTimelinesForThemeAnimation(
				ThemeGenerator.TAS_ADDTOGRID,
				ThemeGenerator.TA_ADDTOGRID_ADDED,
				element,
				false,
				sourceoffset,
				destoffset,
				0,
				spPrimaryStoryboard.Children);
		}
	}

	private static void AddDeleteRepositionHelperUnload(
		UIElement element,
		Rect start,
		Rect destination,
		IList<Storyboard> storyboards)
	{
		Point sourceoffset = new(start.X - destination.X, start.Y - destination.Y);
		Point destoffset = new(0, 0);

		// configuration of this animation
		bool allowLoadUnload;
		bool scheduleAffectedFirst;   // pvl should define a begintime on the affected here
		const long affectedDuration = 300; // todo: pvl might change, do I want to make yet another call to pvl to find out these times?

		GetTransitionContext(element, out var didContextCheck, out var context);

		CoerceHeaderOffsets(element, ref sourceoffset, ref destoffset);

		if (didContextCheck)
		{
			allowLoadUnload =
				context is ThemeTransitionContext.SingleAddList or
				ThemeTransitionContext.SingleDeleteList or
				ThemeTransitionContext.SingleAddGrid or
				ThemeTransitionContext.SingleDeleteGrid or
				ThemeTransitionContext.MixedOperationsList or
				ThemeTransitionContext.MixedOperationsGrid or
				ThemeTransitionContext.MultipleAddList or
				ThemeTransitionContext.MultipleDeleteList or
				ThemeTransitionContext.MultipleAddGrid or
				ThemeTransitionContext.MultipleDeleteGrid;

			scheduleAffectedFirst =
				context is ThemeTransitionContext.SingleAddList or
				ThemeTransitionContext.SingleAddGrid or
				ThemeTransitionContext.MultipleAddList or
				ThemeTransitionContext.MultipleReorderGrid or
				ThemeTransitionContext.MultipleReorderList or
				ThemeTransitionContext.MultipleAddGrid;
		}
		else
		{
			// no context could be gotten, so we need sane defaults.
			// this is expected for non-moco scenarios, such as a regular stackpanel
			// in practise this means that these transitions should never be used outside of MoCo
			allowLoadUnload = true;
			scheduleAffectedFirst = true;
		}

		// prepare primary storyboard. We hold off on secondary storyboard since it is not always needed
		Storyboard spPrimaryStoryboard = new();
		storyboards.Add(spPrimaryStoryboard);

		if (allowLoadUnload)    // the reorder transition does no animation here
		{
			ThemeGenerator.AddTimelinesForThemeAnimation(
				ThemeGenerator.TAS_DELETEFROMGRID,
				ThemeGenerator.TA_DELETEFROMGRID_DELETED,
				element,
				false,
				sourceoffset,
				destoffset,
				scheduleAffectedFirst ? affectedDuration : 0,
				spPrimaryStoryboard.Children);
		}
	}

	private static void AddDeleteRepositionHelperReparentAndLayout(
		UIElement element,
		Rect start,
		Rect destination,
		IList<Storyboard> storyboards)
	{
		Point sourceoffset = new(start.X - destination.X, start.Y - destination.Y);
		Point destoffset = new(0, 0);

		// configuration of this animation
		bool scheduleAffectedFirst;   // pvl should define a begintime on the affected here
		bool addingContext;
		bool deletingContext = false;
		bool reorderingContext = false;
		bool mixedContext;
		const long loadDuration = 220;

		GetTransitionContext(element, out var didContextCheck, out var context);

		CoerceHeaderOffsets(element, ref sourceoffset, ref destoffset);

		if (didContextCheck)
		{
			mixedContext =
				context is ThemeTransitionContext.MixedOperationsGrid or
				ThemeTransitionContext.MixedOperationsList;

			scheduleAffectedFirst =
				context is ThemeTransitionContext.SingleAddList or
				ThemeTransitionContext.SingleAddGrid or
				ThemeTransitionContext.MultipleAddList or
				ThemeTransitionContext.MultipleReorderGrid or
				ThemeTransitionContext.MultipleReorderList or
				ThemeTransitionContext.MultipleAddGrid;

			addingContext =
				context is ThemeTransitionContext.SingleAddList or
				ThemeTransitionContext.SingleAddGrid or
				ThemeTransitionContext.MultipleAddList or
				ThemeTransitionContext.MultipleAddGrid;

			deletingContext =
				context is ThemeTransitionContext.SingleDeleteList or
				ThemeTransitionContext.SingleDeleteGrid or
				ThemeTransitionContext.MultipleDeleteList or
				ThemeTransitionContext.MultipleDeleteGrid;

			reorderingContext =
				context is ThemeTransitionContext.SingleReorderList or
				ThemeTransitionContext.SingleReorderGrid or
				ThemeTransitionContext.MultipleReorderList or
				ThemeTransitionContext.MultipleReorderGrid;
		}
		else
		{
			// no context could be gotten, so we need sane defaults.
			// this is expected for non-moco scenarios, such as a regular stackpanel
			// in practise this means that these transitions should never be used outside of MoCo
			scheduleAffectedFirst = true;
			addingContext = true;
			mixedContext = false;
		}

		// prepare primary storyboard. We hold off on secondary storyboard since it is not always needed
		Storyboard spPrimaryStoryboard = new();
		storyboards.Add(spPrimaryStoryboard);

		// TODO Uno: WinUI portals or crossfades a grid item that changes column or moves in a multiple operation, which draws the
		// element twice through two layout transition elements. Uno has one rendering of the element, so these get the straight move.

		int storyboardID;
		int targetID;
		if (addingContext)
		{
			storyboardID = ThemeGenerator.TAS_ADDTOGRID;
			targetID = ThemeGenerator.TA_ADDTOGRID_AFFECTED;
		}
		else if (deletingContext)
		{
			storyboardID = ThemeGenerator.TAS_DELETEFROMGRID;
			targetID = ThemeGenerator.TA_DELETEFROMGRID_REMAINING;
		}
		else if (reorderingContext)
		{
			storyboardID = ThemeGenerator.TAS_ADDTOGRID;
			targetID = ThemeGenerator.TA_ADDTOGRID_AFFECTED; // when this changes, make sure to match up with the storyboards used for the portalling!
		}
		else if (mixedContext)
		{
			// there is no PVL definition for how the affected should move in this case
			storyboardID = ThemeGenerator.TAS_DELETEFROMGRID;
			targetID = ThemeGenerator.TA_DELETEFROMGRID_REMAINING;
		}
		else
		{
			// we have defined sane behavior above. So if we have a different context, this is truly unsupported.
			throw new NotSupportedException();
		}

		// we are setting up storyboards for the affected elements. If they are caused by an add or a reorder, we want them to
		// first move (and then we will fade in the added elements). If they are caused by a delete, we want them to wait with the
		// move. If they are moved because of a mix of add/deletes, we want them to wait.
		ThemeGenerator.AddTimelinesForThemeAnimation(
			storyboardID,
			targetID,
			element,
			false /* onlyGenerateSteadyState */,
			sourceoffset,
			destoffset,
			(scheduleAffectedFirst || reorderingContext) && !mixedContext ? 0 : loadDuration,
			spPrimaryStoryboard.Children);
	}

	private protected static void AddDeleteRepositionStoryboardCreator(
		UIElement element,
		Rect start,
		Rect destination,
		TransitionTrigger transitionTrigger,
		IList<Storyboard> storyboards,
		out TransitionParent parentForTransition)
	{
		parentForTransition =
			transitionTrigger == TransitionTrigger.Reparent ?
			TransitionParent.ParentToRoot :
			TransitionParent.ParentToCommonParent;

		switch (transitionTrigger)
		{
			case TransitionTrigger.Load:
				AddDeleteRepositionHelperLoad(element, start, destination, storyboards);
				break;

			case TransitionTrigger.Unload:
				AddDeleteRepositionHelperUnload(element, start, destination, storyboards);
				break;

			case TransitionTrigger.Reparent:
			case TransitionTrigger.Layout:
				AddDeleteRepositionHelperReparentAndLayout(element, start, destination, storyboards);
				break;

			default:
				break;
		}
	}
}
