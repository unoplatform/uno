// MUX Reference dxaml/xcp/components/elements/UIElement.cpp, core/core/elements/uielement.cpp

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public partial class UIElement
{
	private ImplicitAnimationInfo? _implicitShowAnimationInfo;
	private ImplicitAnimationInfo? _implicitHideAnimationInfo;
	private bool _requestedPlayImplicitShow; // An implicit Show animation is requested to be triggered on the following frame
	private bool _requestedPlayImplicitHide; // An implicit Hide animation is requested to be triggered on the following frame

	// Children removed while a Hide animation plays in their subtree. WinUI keeps them in the collection's
	// unloading storage: still in the live tree and rendered, but no longer part of the children.
	private List<UIElement>? _implicitAnimationUnloadingChildren;

	private static ImplicitShowHideAnimations ImplicitShowHideAnimations => CoreServices.Instance.ImplicitShowHideAnimations;

	/// <summary>The effective visibility last computed for this element, while it is tracked.</summary>
	internal EffectiveVisibilityInfo? TrackedEffectiveVisibility { get; set; }

	// An implicit Show animation is defined (but not necessarily requested, or playing)
	internal bool HasImplicitShowAnimation => _implicitShowAnimationInfo is not null;

	// An implicit Hide animation is defined (but not necessarily requested, or playing)
	internal bool HasImplicitHideAnimation => _implicitHideAnimationInfo is not null;

	// Notes on flags and flow of Implicit Show/Hide animations
	// Implicit Show/Hide animations have several distinct "states":
	//
	// 1) Animation is Set:  This is done through ElementCompositionPreview API.  This just tells XAML there is an animation
	//    to be played at the appropriate time, but doesn't request the animation be played yet.  The triggering is done implicitly.
	//
	// 2) XAML tree changes are made.  For the most part this only results in dirtying the tree for render, with one notable exception:
	//    When elements are removed from the tree or Collapsed, if that element or one of its children has a Hide animation, the element
	//    gets special treatment as it must be kept visible in the rendering tree while these animations are playing.
	//
	// 3) Just before rendering, we re-evaluate the effective visibility of all elements with Show/Hide animations, as well
	//    as keep-visible elements.  Any animations that need to be started are requested, then played.

	// Helper method for setting/clearing implicit show/hide animation
	internal void SetImplicitShowHideAnimation(ImplicitAnimationType iaType, ICompositionAnimationBase? animation)
	{
		// First see if we have a currently set animation or not
		var info = GetImplicitAnimationInfo(iaType);

		if (info?.ImplicitAnimation != animation)
		{
			if (info is not null && IsImplicitAnimationPlaying(iaType))
			{
				// The behavior we want here is to continue playing the current animation until it completes, or is canceled.
				// We cannot replace our animation information until the current animation is done, so we set aside this
				// information and set the "pendingChange" flag to indicate we need to make this change later in CleanupImplicitAnimationOnCompletion().
				info.PendingChange = true;
				info.PendingAnimation = animation;
			}
			else
			{
				UpdateImplicitShowHideAnimationInfo(iaType, animation);
			}
		}
	}

	// Helper method for updating our internal info about an implicit show/hide animation in response to app setting/clearing it
	private void UpdateImplicitShowHideAnimationInfo(ImplicitAnimationType iaType, ICompositionAnimationBase? animation)
	{
		// Clear out whatever we have stored about this implicit animation.  We may re-populate later.
		var info = animation is null ? null : new ImplicitAnimationInfo(animation);
		if (iaType == ImplicitAnimationType.Show)
		{
			_implicitShowAnimationInfo = info;
		}
		else
		{
			_implicitHideAnimationInfo = info;
			ImplicitShowHideAnimations.SetHasImplicitHideAnimation(this, info is not null);
		}

		// Track the effective visibility of this element if it has an implicit animation, otherwise stop tracking.
		ImplicitShowHideAnimations.SetTrackEffectiveVisibility(this, HasImplicitShowAnimation || HasImplicitHideAnimation);
	}

	// Compute the effective visibility of this element for implicit Show/Hide animations
	internal EffectiveVisibilityInfo ComputeEffectiveVisibility()
	{
		// Do a walk up the tree evaluating rules that determine if this element is considered
		// Visible or Hidden from the perspective of "should this trigger an implicit Show/Hide animation".
		// Opacity and clipping are not part of this criteria.
		// For an element to have effective visibility of Visible, it must be in the live tree, and all ancestors
		// must be visible, and not in the process of being unloaded (the equivalent of in the live tree).

		// Rule #1:  Not in live tree == Hidden
		if (!IsActiveInVisualTree)
		{
			if (Visibility == Visibility.Visible && this.GetParent() is null && this is Popup { IsOpen: true })
			{
				// The exception is visible open parentless popups. They're counted as visible.
				// (The parentless popup itself is not counted as being "in the live tree".)
				return new(EffectiveVisibility.Visible);
			}

			return new(EffectiveVisibility.Hidden);
		}

		var result = EffectiveVisibility.Visible;
		var current = this;
		while (current is not null)
		{
			// Rule #2:  Visibility::Collapsed == Hidden
			if (current.Visibility != Visibility.Visible)
			{
				result = EffectiveVisibility.Hidden;
			}
			else if (current is Popup { IsOpen: false })
			{
				// Rule #2b:  Closed popup == Hidden
				result = EffectiveVisibility.Hidden;
			}

			var parent = current.GetParentFollowPopups();

			// Rule #3:  In Unloading storage == Hidden
			if (parent is not null && parent.IsUnloadingElement(current))
			{
				result = EffectiveVisibility.Hidden;
			}

			// Uno TODO: WinUI exempts children of item containers from implicit animations.

			current = parent;
		}

		return new(result);
	}

	// Popup content is parented to the PopupRoot; follow it back to the Popup that owns it instead.
	private UIElement? GetParentFollowPopups()
		=> this.GetParent() switch
		{
			PopupPanel popupPanel => popupPanel.Popup,
			UIElement parent => parent,
			_ => null,
		};

	internal bool HasAncestorIncludingPopups(UIElement ancestor)
	{
		for (var current = GetParentFollowPopups(); current is not null; current = current.GetParentFollowPopups())
		{
			if (current == ancestor)
			{
				return true;
			}
		}

		return false;
	}

	// Compute whether we should keep this element visible in the rendering tree while a Hide animation is playing.
	// The basic idea is best explained with a picture.  Consider this example subtree:
	//
	// K
	// |
	// E1
	// |
	// E2
	//
	// We compute the keep-visible state for K as follows:
	// -If K, E1, or E2 has an implicit Hide animation, K must be kept-visible.
	internal bool ComputeKeepVisible() => ImplicitShowHideAnimations.ShouldKeepVisible(this);

	// Set "keep visible" status for this element.  See notes above for more details.
	internal void SetKeepVisible(bool keepVisible)
	{
		// Start/Stop keep-visible tracking, done each frame in ImplicitShowHideAnimations.Update().
		ImplicitShowHideAnimations.SetTrackKeepVisible(this, keepVisible);

		if (keepVisible)
		{
			// Rendering skips collapsed elements unless they're kept visible for a Hide animation.
			Visual.IsVisible = true;
		}
		else
		{
			if (Visibility != Visibility.Visible)
			{
				Visual.IsVisible = false;
			}

			FlushPendingKeepVisibleOperations();
		}
	}

	// Returns true if this element is being kept visible for a Hide animation
	internal bool IsKeepVisible() => ImplicitShowHideAnimations.IsKeepVisible(this);

	private void FlushPendingKeepVisibleOperations()
	{
		if (this.GetParent() is UIElement parent && parent.IsUnloadingElement(this))
		{
			parent.RemoveUnloadedElement(this);
		}

		// Uno TODO: WinUI also finishes the removal of a popup's unloading child here.

		SetDirtyToRoot();
	}

	// Returns true if this element has the given type of implicit animation set.  Does not indicate if the animation is requested or running.
	internal bool HasImplicitAnimation(ImplicitAnimationType iaType)
		=> iaType == ImplicitAnimationType.Show ? HasImplicitShowAnimation : HasImplicitHideAnimation;

	// Retrieve data about this element's implicit Show/Hide animation, if any.
	// Note that having this information means there's an animation, but it's not necessarily playing.
	private ImplicitAnimationInfo? GetImplicitAnimationInfo(ImplicitAnimationType iaType)
		=> iaType == ImplicitAnimationType.Show ? _implicitShowAnimationInfo : _implicitHideAnimationInfo;

	// Returns true if the given implicit animation is actually running right now.
	internal bool IsImplicitAnimationPlaying(ImplicitAnimationType iaType)
		=> GetImplicitAnimationInfo(iaType)?.ScopedBatch is not null;

	// Requests the triggering of implicit Show/Hide animation
	internal void SetImplicitAnimationRequested(ImplicitAnimationType iaType, bool requested)
	{
		if (iaType == ImplicitAnimationType.Show)
		{
			_requestedPlayImplicitShow = requested;
		}
		else
		{
			_requestedPlayImplicitHide = requested;
		}

		if (requested)
		{
			// Force a frame if we're triggering the animation.
			SetDirtyToRoot();
		}
	}

	// Returns true if an implicit Show/Hide animation is requested.
	internal bool IsImplicitAnimationRequested(ImplicitAnimationType iaType)
		=> iaType == ImplicitAnimationType.Show ? _requestedPlayImplicitShow : _requestedPlayImplicitHide;

	// Helper function to actually start playing any requested Show/Hide animations
	internal void TriggerImplicitShowHideAnimations()
	{
		if (IsImplicitAnimationRequested(ImplicitAnimationType.Show))
		{
			PlayImplicitAnimation(_implicitShowAnimationInfo!, ImplicitAnimationType.Show);
		}
		else if (IsImplicitAnimationRequested(ImplicitAnimationType.Hide))
		{
			PlayImplicitAnimation(_implicitHideAnimationInfo!, ImplicitAnimationType.Hide);
		}
	}

	// Start playing an implicit Show/Hide animation, given the supplied info about the animation
	private void PlayImplicitAnimation(ImplicitAnimationInfo info, ImplicitAnimationType iaType)
	{
		// Don't play if the animation is already playing, eg if the element is first Collapsed, then removed
		if (!IsImplicitAnimationPlaying(iaType))
		{
			var handOffVisual = Visual;

			// Create a ScopedBatch which will let us know when the animation is complete.
			var scopedBatch = handOffVisual.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

			// Play the animation!
			if (SharedHelpers.IsAnimationsEnabled())
			{
				try
				{
					handOffVisual.StartAnimationGroup(info.ImplicitAnimation);
				}
				catch (ObjectDisposedException)
				{
					// The app disposed the animation out from under us.
				}
			}

			scopedBatch.End();

			// Listen for animation completion
			// Use weak reference in lambda to prevent leaking the element.
			WeakReference<UIElement> wrThis = new(this);
			void OnCompleted(object sender, CompositionBatchCompletedEventArgs args)
			{
				if (wrThis.TryGetTarget(out var spThis))
				{
					spThis.OnImplicitAnimationCompleted(iaType);
				}
			}

			info.CompletedHandler = OnCompleted;
			scopedBatch.Completed += info.CompletedHandler;

			info.ScopedBatch = scopedBatch;
			info.AnimationTarget = handOffVisual;
		}

		SetImplicitAnimationRequested(iaType, false);
	}

	// Cancel a requested or in-progress implicit Show/Hide animation, else do nothing
	internal void CancelImplicitAnimation(ImplicitAnimationType iaType)
	{
		if (HasImplicitAnimation(iaType))
		{
			SetImplicitAnimationRequested(iaType, false);

			if (IsImplicitAnimationPlaying(iaType))
			{
				var info = GetImplicitAnimationInfo(iaType)!;

				try
				{
					info.AnimationTarget!.StopAnimationGroup(info.ImplicitAnimation);
				}
				catch (ObjectDisposedException)
				{
					// The app disposed the animation out from under us.
				}

				CleanupImplicitAnimationOnCompletion(iaType);
			}
		}
	}

	// Event handler for completion of implicit Show/Hide animation
	private void OnImplicitAnimationCompleted(ImplicitAnimationType iaType)
	{
		CleanupImplicitAnimationOnCompletion(iaType);
		SetDirtyToRoot();
	}

	// Cleanup of implicit Show/Hide animation, run either after cancel, or after animation completion
	private void CleanupImplicitAnimationOnCompletion(ImplicitAnimationType iaType)
	{
		// Release the book-keeping entry for this animation
		if (GetImplicitAnimationInfo(iaType) is { ScopedBatch: { } scopedBatch } info)
		{
			scopedBatch.Completed -= info.CompletedHandler;
			info.CompletedHandler = null;
			info.ScopedBatch = null;
			info.AnimationTarget = null;

			if (info.PendingChange)
			{
				// The animation was changed by the app while an animation was playing.  Now that we're done playing
				// the animation, we can process the pending change by updating the info we have on this animation.
				UpdateImplicitShowHideAnimationInfo(iaType, info.PendingAnimation);
			}
		}
	}

	private static void SetDirtyToRoot() => CoreServices.RequestAdditionalFrame();

	// Called when this element leaves the live tree.
	private void OnLeaveForImplicitShowHideAnimations()
	{
		// This element is leaving the tree: finish unloading any child still kept around for its Hide animation.
		if (_implicitAnimationUnloadingChildren is { Count: > 0 } unloadingChildren)
		{
			foreach (var child in unloadingChildren.ToArray())
			{
				// Cancel any running implicit Hide animation, and finish unloading it if it's been kept visible for Hide animations.
				child.CancelImplicitAnimation(ImplicitAnimationType.Hide);
				child.SetKeepVisible(false);
			}
		}

		// Cancel any ongoing implicit Show animations as the element no longer renders.
		CancelImplicitAnimation(ImplicitAnimationType.Show);
		if (!IsKeepVisible())
		{
			CancelImplicitAnimation(ImplicitAnimationType.Hide);
		}
	}

	// Called when this element's Visibility changes while it is in the live tree.
	private void OnVisibilityChangedForImplicitShowHideAnimations(Visibility newValue)
	{
		if (newValue != Visibility.Visible
			&& (IsActiveInVisualTree || (this.GetParent() is null && this is Popup { IsOpen: true }))
			&& ImplicitShowHideAnimations.HasImplicitHideAnimations)
		{
			// If this element or one of its children has an implicit Hide animation,
			// keep it visible for rendering while the animation(s) play.
			if (ComputeKeepVisible())
			{
				SetKeepVisible(true);
			}
		}
	}

	internal bool IsUnloadingElement(UIElement child) => _implicitAnimationUnloadingChildren?.Contains(child) == true;

	// When a child whose subtree has an implicit Hide animation is removed, keep it in the live tree and rendered
	// until the animation completes (UIElementCollection unloading storage in WinUI).
	private bool TryMoveToImplicitAnimationUnloadingStorage(UIElement child)
	{
		if (!child.IsActiveInVisualTree
			|| !ImplicitShowHideAnimations.HasImplicitHideAnimations
			|| !child.ComputeKeepVisible())
		{
			return false;
		}

		child.SetKeepVisible(true);
		(_implicitAnimationUnloadingChildren ??= new()).Add(child);

		// Unloading elements render above the other children of a panel, and below the content of any other element,
		// so that an outgoing page sits under the incoming one.
		Visual.Children.Remove(child.Visual);
		if (this is Panel)
		{
			Visual.Children.InsertAtTop(child.Visual);
		}
		else
		{
			Visual.Children.InsertAtBottom(child.Visual);
		}

		return true;
	}

	// Finishes the removal of a child that was kept for its Hide animation.
	private void RemoveUnloadedElement(UIElement child)
	{
		if (_implicitAnimationUnloadingChildren?.Remove(child) == true)
		{
			CompleteChildRemoval(child);
		}
	}
}
