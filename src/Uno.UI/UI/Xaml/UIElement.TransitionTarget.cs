#nullable enable

using System;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

partial class UIElement
{
	private TransitionTarget? _transitionTarget;

	/// <summary>
	/// The internal theme-animation layer (XamlOM UIElement.TransitionTarget). Null unless explicitly assigned.
	/// </summary>
	// TODO Uno: WinUI also creates it lazily when a dynamic timeline resolves a "(UIElement.TransitionTarget)" path.
	internal TransitionTarget? TransitionTarget
	{
		get => _transitionTarget;
		set
		{
			if (ReferenceEquals(_transitionTarget, value))
			{
				return;
			}

			_transitionTarget?.SetOwner(null);
			_transitionTarget = value;
			value?.SetOwner(this);

			// The property's own NWSetTransitionTargetDirty: every layer it contributes may have changed.
			OnTransitionTargetTransformDirty();
			OnTransitionTargetClipDirty();
			OnTransitionTargetOpacityDirty();
			value?.Clean();
		}
	}

	internal bool HasTransitionTarget() => _transitionTarget is not null;

	// MUX Reference uielement.cpp (CUIElement::NWSetTransitionTargetDirty), tag winui3/release/2.5.1, commit ba3a8d59e
	internal void NWSetTransitionTargetDirty()
	{
		if (_transitionTarget is { } transitionTarget)
		{
			transitionTarget.NWSetPropertyDirtyOnTarget(this);

			// No render walk consumes the dirty state later, the owner applies it right away.
			transitionTarget.Clean();
		}
	}

	internal void OnTransitionTargetTransformDirty()
	{
		if (_renderTransform is not null)
		{
			_renderTransform.UpdateTransitionTarget();
		}
		else if (_transitionTarget is not null)
		{
			_renderTransform = new NativeRenderTransformAdapter(this, RenderTransform, RenderTransformOrigin);
		}
	}

	internal void OnTransitionTargetClipDirty() => UpdateTransitionClip();

	internal void OnTransitionTargetOpacityDirty() => UpdateOpacity();

	// MUX Reference HWCompNodeWinRT.cpp (UpdateTransitionClipVisual), tag winui3/release/2.5.1, commit ba3a8d59e
	private void UpdateTransitionClip()
	{
		var visual = Visual;
		if (_transitionTarget is { HasClipAnimation: true } transitionTarget)
		{
			// An inset clip over the element bounds, so it tracks the visual's Size like WinUI's InsetClip.
			// TODO Uno: WinUI also transforms a self-applied layout clip with the clip transform; Uno intersects it untransformed.
			var clip = visual.TransitionClip ??= visual.Compositor.CreateInsetClip();
			var size = visual.Size;
			clip.TransformMatrix = transitionTarget.GetClipTransform(new Rect(0, 0, size.X, size.Y));
		}
		else if (visual.TransitionClip is not null)
		{
			visual.TransitionClip = null;
		}
	}

	// MUX Reference components/elements/UIElement.cpp (CUIElement::GetOpacityCombined), tag winui3/release/2.5.1, commit ba3a8d59e
	private double GetOpacityCombined()
		=> _transitionTarget is { } transitionTarget
			? Opacity * Math.Clamp(transitionTarget.Opacity, 0.0, 1.0)
			: Opacity;
}
