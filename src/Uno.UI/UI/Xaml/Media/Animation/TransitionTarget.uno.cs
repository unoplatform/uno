#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class TransitionTarget
{
	// WinUI's TransitionTarget is multi-parent shareable; here it serves a single element.
	private WeakReference<UIElement>? _owner;

	internal void SetOwner(UIElement? owner) => _owner = owner is null ? null : new WeakReference<UIElement>(owner);

	// Stands in for NWPropagateDirtyFlag reaching CUIElement::NWSetTransitionTargetDirty.
	private void NWPropagateDirtyFlag()
	{
		if (_owner is not null && _owner.TryGetTarget(out var owner))
		{
			owner.NWSetTransitionTargetDirty();
		}
	}

	private static void OnOpacityChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => NWSetOpacityDirty(sender);

	private static void OnTransformOriginChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => NWSetTransformDirty(sender);

	private static void OnClipTransformOriginChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => NWSetClipDirty(sender);

	private static void OnCompositeTransformChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var pThis = (TransitionTarget)sender;
		pThis._compositeTransformChangedRegistration?.Dispose();
		pThis._compositeTransformChangedRegistration = (args.NewValue as CompositeTransform)?.RegisterChanged(pThis.OnCompositeTransformValueChanged);

		NWSetTransformDirty(pThis);
	}

	private IDisposable? _compositeTransformChangedRegistration;

	private void OnCompositeTransformValueChanged(object? sender, EventArgs e) => NWSetTransformDirty(this);

	private static void OnClipTransformChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var pThis = (TransitionTarget)sender;
		pThis._clipTransformChangedRegistration?.Dispose();
		pThis._clipTransformChangedRegistration = (args.NewValue as CompositeTransform)?.RegisterChanged(pThis.OnClipTransformValueChanged);

		NWSetClipDirty(pThis);
	}

	private IDisposable? _clipTransformChangedRegistration;

	private void OnClipTransformValueChanged(object? sender, EventArgs e)
	{
		// CompositeTransform.cpp: any value set on the transition clip transform marks the target as clip-animated (sticky).
		// TODO Uno: WinUI also marks on a set that doesn't change the value; the transform's change notification only fires on actual changes.
		SetHasClipAnimation();
		NWSetClipDirty(this);
	}
}
