// MUX Reference dxaml/xcp/components/comptree/DCompTreeHost.cpp, inc/ImplicitAnimations.h

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Xaml.Core;

internal enum ImplicitAnimationType
{
	Show,
	Hide,
}

internal enum EffectiveVisibility
{
	Visible,
	Hidden,
}

internal readonly record struct EffectiveVisibilityInfo(EffectiveVisibility Vis);

internal sealed class ImplicitAnimationInfo
{
	public ImplicitAnimationInfo(ICompositionAnimationBase implicitAnimation) => ImplicitAnimation = implicitAnimation;

	/// <summary>The implicit animation. Always non-null.</summary>
	public ICompositionAnimationBase ImplicitAnimation { get; }

	/// <summary>Tracks animation completion. Non-null only while the animation is playing.</summary>
	public CompositionScopedBatch? ScopedBatch { get; set; }

	/// <summary>The object the animation was started on. Non-null only while the animation is playing.</summary>
	public CompositionObject? AnimationTarget { get; set; }

	public Windows.Foundation.TypedEventHandler<object, CompositionBatchCompletedEventArgs>? CompletedHandler { get; set; }

	/// <summary>True iff the implicit animation was set to a new value while the animation was playing.</summary>
	public bool PendingChange { get; set; }

	/// <summary>If <see cref="PendingChange"/>, the animation to switch to once the current one finishes.</summary>
	public ICompositionAnimationBase? PendingAnimation { get; set; }
}

/// <summary>
/// The implicit Show/Hide animation bookkeeping WinUI keeps on DCompTreeHost: effective visibility tracking of
/// elements with Show/Hide animations, and "keep visible" tracking of elements whose subtree plays a Hide animation.
/// </summary>
internal sealed class ImplicitShowHideAnimations
{
	// Weak, as WinUI only removes destroyed elements from these maps in the element's destructor.
	private readonly List<WeakReference<UIElement>> _effectiveVisibilityElements = new();
	private readonly List<WeakReference<UIElement>> _implicitHideAnimationElements = new();

	// Strong: an element kept visible for a Hide animation must survive until the animation completes.
	private readonly List<UIElement> _keepVisible = new();

	internal bool HasImplicitHideAnimations => _implicitHideAnimationElements.Count > 0;

	// Start/Stop tracking the effective visibility of this element.
	// Note carefully, effective visibility for implicit Show/Hide animations has special semantics.
	// See additional notes in UIElement.ComputeEffectiveVisibility();
	internal void SetTrackEffectiveVisibility(UIElement uiElement, bool track)
	{
		var isTracked = IndexOf(_effectiveVisibilityElements, uiElement) is var index && index >= 0;
		if (isTracked && !track)
		{
			_effectiveVisibilityElements.RemoveAt(index);
			uiElement.TrackedEffectiveVisibility = null;
		}
		else if (!isTracked && track)
		{
			_effectiveVisibilityElements.Add(new(uiElement));
			uiElement.TrackedEffectiveVisibility = uiElement.ComputeEffectiveVisibility();
		}
	}

	internal void SetHasImplicitHideAnimation(UIElement uiElement, bool hasAnimation)
	{
		var index = IndexOf(_implicitHideAnimationElements, uiElement);
		if (index >= 0 && !hasAnimation)
		{
			_implicitHideAnimationElements.RemoveAt(index);
		}
		else if (index < 0 && hasAnimation)
		{
			_implicitHideAnimationElements.Add(new(uiElement));
		}
	}

	// Do processing for implicit Show/Hide animations, done once per frame, just before the RenderWalk.
	// This function has two responsibilities:
	// 1) Recompute the effective visibility of each element with an implicit Show/Hide animation,
	//    and trigger the appropriate animation when effective visibility changes.
	// 2) For every element that's being "kept visible" due to implicit Hide animation running in it subtree,
	//    recompute if this element still needs to be kept visible, and clean it up if no longer being kept visible.
	internal void Update()
	{
		if (_effectiveVisibilityElements.Count == 0 && _keepVisible.Count == 0)
		{
			return;
		}

		// Uno TODO: WinUI first updates the effective visibility of modern panels hosting item containers with implicit
		// animations, and applies the item container policies below. Uno doesn't flag item containers yet.

		// Recompute effective visibility of each element with implicit Show/Hide animation
		foreach (var uiElement in GetAlive(_effectiveVisibilityElements))
		{
			var effectiveVisibility = ComputeEffectiveVisibility(uiElement);

			if (effectiveVisibility != uiElement.TrackedEffectiveVisibility)
			{
				uiElement.TrackedEffectiveVisibility = effectiveVisibility;

				var shouldPlay = uiElement.IsActiveInVisualTree;

				if (!shouldPlay && uiElement.GetParent() is null && uiElement is Popup popupNoRef)
				{
					// If the element is a parentless popup, it might be allowed to play animations even though it's not in the live tree.
					// (No parentless popup is considered "in the live tree".)
					var isPopupVisible = popupNoRef.IsOpen && popupNoRef.Visibility == Visibility.Visible;
					if ((isPopupVisible && effectiveVisibility.Vis == EffectiveVisibility.Visible)
						|| (!isPopupVisible && effectiveVisibility.Vis == EffectiveVisibility.Hidden))
					{
						shouldPlay = true;
					}
				}

				if (shouldPlay)
				{
					switch (effectiveVisibility.Vis)
					{
						case EffectiveVisibility.Visible:
							// We just transitioned to visible.  Cancel any ongoing Hide and request Show animation.
							if (uiElement.HasImplicitAnimation(ImplicitAnimationType.Show))
							{
								uiElement.CancelImplicitAnimation(ImplicitAnimationType.Hide);
								uiElement.SetImplicitAnimationRequested(ImplicitAnimationType.Show, true);
							}
							break;

						case EffectiveVisibility.Hidden:
							// We just transitioned to hidden.  Cancel any ongoing Show and request Hide animation.
							if (uiElement.HasImplicitAnimation(ImplicitAnimationType.Hide))
							{
								uiElement.CancelImplicitAnimation(ImplicitAnimationType.Show);
								uiElement.SetImplicitAnimationRequested(ImplicitAnimationType.Hide, true);
							}
							break;
					}
				}
			}
		}

		// Update the "keep visible" status for elements being kept visible due to implicit Hide animation running in their subtree.
		List<UIElement>? elementsToNotKeepVisible = null;
		foreach (var keepVisibleElement in _keepVisible)
		{
			var keepVisible = false;

			// Check for running ECP hide animations under the element
			// Note: We're not looking for any element with an ECP hide animation. It needs to be running.
			foreach (var hideElement in GetAlive(_effectiveVisibilityElements))
			{
				if (hideElement.IsImplicitAnimationRequested(ImplicitAnimationType.Hide) ||
					hideElement.IsImplicitAnimationPlaying(ImplicitAnimationType.Hide))
				{
					if (ShouldKeepVisible(keepVisibleElement, hideElement))
					{
						keepVisible = true;
						break;
					}
				}
			}

			if (!keepVisible)
			{
				(elementsToNotKeepVisible ??= new()).Add(keepVisibleElement);
			}
		}

		// Cleanup all the elements no longer being kept visible.
		if (elementsToNotKeepVisible is not null)
		{
			foreach (var element in elementsToNotKeepVisible)
			{
				element.SetKeepVisible(false);
			}
		}

		// WinUI plays requested animations from the CompNode walk that follows, once the visuals carry this frame's
		// properties. Uno pushes properties synchronously, so they are up to date here.
		foreach (var uiElement in GetAlive(_effectiveVisibilityElements))
		{
			uiElement.TriggerImplicitShowHideAnimations();
		}
	}

	// Helper function, computes effective visibility for the given UIElement.
	private static EffectiveVisibilityInfo ComputeEffectiveVisibility(UIElement uiElement)
	{
		var effectiveVisibility = uiElement.ComputeEffectiveVisibility();

		// Special case for popup here - we look at whether the popup is open as well.
		if (uiElement is Popup { IsOpen: false })
		{
			effectiveVisibility = effectiveVisibility with { Vis = EffectiveVisibility.Hidden };
		}

		return effectiveVisibility;
	}

	// Start/stop tracking this element for "keep visible" purposes.
	// The basic idea is, for any element with an implicit Hide animation running in its subtree,
	// we keep track of these elements, and every frame we evaluate whether or not it still has
	// any animations running in its subtree.  Once we reach a state of no animations running,
	// we can complete the action that was originally requested (finish Unloading, or Collapsing).
	internal void SetTrackKeepVisible(UIElement uiElement, bool track)
	{
		var index = _keepVisible.IndexOf(uiElement);
		if (track)
		{
			if (index < 0)
			{
				_keepVisible.Add(uiElement);
			}
		}
		else if (index >= 0)
		{
			_keepVisible.RemoveAt(index);
		}
	}

	// Return true if this element is being tracked for "keep visible" purposes
	internal bool IsKeepVisible(UIElement uiElement) => _keepVisible.Contains(uiElement);

	internal bool ShouldKeepVisible(UIElement uiElement)
	{
		foreach (var uiElementWithHideAnimation in GetAlive(_implicitHideAnimationElements))
		{
			if (ShouldKeepVisible(uiElement, uiElementWithHideAnimation))
			{
				return true;
			}
		}

		return false;
	}

	private static bool ShouldKeepVisible(UIElement uiElement, UIElement uiElementWithKeepAlive)
		// This element needs to be kept alive, or is the ancestor of an element that needs to be kept alive. Keep it visible.
		=> uiElementWithKeepAlive == uiElement || uiElementWithKeepAlive.HasAncestorIncludingPopups(uiElement);

	private static int IndexOf(List<WeakReference<UIElement>> list, UIElement element)
	{
		for (var i = 0; i < list.Count; i++)
		{
			if (list[i].TryGetTarget(out var target) && target == element)
			{
				return i;
			}
		}

		return -1;
	}

	// Snapshots the live elements (pruning collected ones): callers can change the tracked set while iterating.
	private static UIElement[] GetAlive(List<WeakReference<UIElement>> list)
	{
		if (list.Count == 0)
		{
			return Array.Empty<UIElement>();
		}

		List<UIElement> alive = new(list.Count);
		for (var i = list.Count - 1; i >= 0; i--)
		{
			if (list[i].TryGetTarget(out var target))
			{
				alive.Add(target);
			}
			else
			{
				list.RemoveAt(i);
			}
		}

		alive.Reverse();
		return alive.ToArray();
	}
}
