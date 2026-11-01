#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml;

partial class UIElement
{
	private LayoutTransitionStorage? _layoutTransitionStorage;

	internal LayoutTransitionStorage? GetLayoutTransitionStorage() => _layoutTransitionStorage;

	internal bool HasLayoutTransitionStorage() => _layoutTransitionStorage is not null;

	internal LayoutTransitionStorage EnsureLayoutTransitionStorage() => _layoutTransitionStorage ??= new();

	/// <summary>
	/// The transitions a parent hands down to <paramref name="child"/> (ContentTransitions, ChildTransitions, ChildrenTransitions).
	/// </summary>
	internal virtual TransitionCollection? GetTransitionsForChildElementNoAddRef(UIElement child) => null;

	// MUX Reference CUIElementCollection::UnloadElement (UIElementCollection.cpp), tag winui3/release/2.5.1, commit ba3a8d59e
	private bool UnloadElement(UIElement pRemove)
	{
		// Uno: only pages, see Transition.OnElementEnteredTree.
		if (pRemove is not Page)
		{
			return false;
		}

		var bUnloaded = false;
		List<Transition> applicableUnloadTransitions = new();

		// TODO Uno: WinUI allows panels to 'ignore transitions' (CPanel::GetIsIgnoringTransitions).
		Transition.AppendAllTransitionsNoAddRefs(pRemove, TransitionTrigger.Unload, applicableUnloadTransitions);   // cache to pass into processUnload

		if (applicableUnloadTransitions.Count > 0 && // has unload transition
			pRemove.IsActiveInVisualTree) // is not itself already unloaded
		{
			var pStorage = pRemove.EnsureLayoutTransitionStorage();

			// we have left the tree, even though we might not yet have run leaveimpl.
			Transition.ProcessUnloadTrigger(pRemove, pStorage, applicableUnloadTransitions);

			if (Transition.HasActiveTransition(pRemove, TransitionTrigger.Unload))
			{
				bUnloaded = true;
			}
		}

		if (!bUnloaded && pRemove.GetLayoutTransitionStorage() is { } storage)
		{
			// did not unload the element. So it will just be removed
			// should stop current transition on it
			// this will as a side effect also set the correct currentInfo on
			// the pStorage
			storage.UnregisterElementForTransitions(pRemove);
		}

		return bUnloaded;
	}
}
