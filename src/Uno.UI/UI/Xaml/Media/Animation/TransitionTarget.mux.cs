// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TransitionTarget.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Numerics;
using Uno.Extensions;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class TransitionTarget
{
	// TODO Uno: ~CTransitionTarget only releases the transforms and unregisters from the DComp object registry.

	//------------------------------------------------------------------------
	//  Synopsis:
	//      Initializes the instance with instances of the leaves we might animate.
	//      The value of this class is in these being non-null and not settable,
	//      this enables consumers to rely on them being animateable.
	//------------------------------------------------------------------------
	internal TransitionTarget()
	{
		// transform
		CompositeTransform = new CompositeTransform();

		// clip transform
		ClipTransform = new CompositeTransform();
	}

	internal bool IsDirty() => m_currentDirtyMode != TransitionTargetDirtyMode.Dirty_None;

	internal void Clean() => m_currentDirtyMode = TransitionTargetDirtyMode.Dirty_None;

	//------------------------------------------------------------------------
	//  Synopsis: This RENDERCHANGEDPFN marks this TransitionTargets transform as
	//            dirty for rendering.
	//------------------------------------------------------------------------
	private static void NWSetTransformDirty(DependencyObject target)
	{
		var pThis = (TransitionTarget)target;
		// TODO Uno: Uno has no independent (DirtyFlags.Independent) ticks, so the mode is always updated.
		pThis.m_currentDirtyMode |= TransitionTargetDirtyMode.Dirty_Transform;
		pThis.NWPropagateDirtyFlag();
	}

	//------------------------------------------------------------------------
	//  Synopsis: This RENDERCHANGEDPFN marks this TransitionTargets clip as
	//            dirty for rendering.
	//------------------------------------------------------------------------
	private static void NWSetClipDirty(DependencyObject target)
	{
		var pThis = (TransitionTarget)target;
		pThis.m_currentDirtyMode |= TransitionTargetDirtyMode.Dirty_Clip;
		pThis.NWPropagateDirtyFlag();
	}

	//------------------------------------------------------------------------
	//  Synopsis: This RENDERCHANGEDPFN marks this TransitionTargets opacity as
	//            dirty for rendering.
	//------------------------------------------------------------------------
	private static void NWSetOpacityDirty(DependencyObject target)
	{
		var pThis = (TransitionTarget)target;
		pThis.m_currentDirtyMode |= TransitionTargetDirtyMode.Dirty_Opacity;
		pThis.NWPropagateDirtyFlag();
	}

	internal void NWSetPropertyDirtyOnTarget(UIElement pTarget)
	{
		if ((m_currentDirtyMode & TransitionTargetDirtyMode.Dirty_Transform) == TransitionTargetDirtyMode.Dirty_Transform)
		{
			pTarget.OnTransitionTargetTransformDirty();
		}

		if ((m_currentDirtyMode & TransitionTargetDirtyMode.Dirty_Clip) == TransitionTargetDirtyMode.Dirty_Clip)
		{
			pTarget.OnTransitionTargetClipDirty();
		}

		if ((m_currentDirtyMode & TransitionTargetDirtyMode.Dirty_Opacity) == TransitionTargetDirtyMode.Dirty_Opacity)
		{
			pTarget.OnTransitionTargetOpacityDirty();
		}
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns the transform determined by m_pClipTransform, m_ptClipTransformOrigin,
	//      and the bounding box representing a clip.
	//
	//------------------------------------------------------------------------
	internal Matrix3x2 GetClipTransform(Rect bounds)
	{
		var pMatrix = ClipTransform?.MatrixCore ?? Matrix3x2.Identity;
		if (!pMatrix.IsIdentity)
		{
			ApplyClipTransformOrigin(bounds, ref pMatrix);
		}

		return pMatrix;
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Applies m_ptClipTransformOrigin to the bounds param.
	//
	//------------------------------------------------------------------------
	private void ApplyClipTransformOrigin(Rect bounds, ref Matrix3x2 pMatrix)
	{
		// TODO: JCOMP: This math is duplicated in CUIElement::ApplyTransform.
		// TODO: JCOMP: The origin adjustment can be skipped if pMatrix is purely a translation.
		// Adjust for origin
		var clipTransformOrigin = ClipTransformOrigin;
		if (clipTransformOrigin.X != 0.0f || clipTransformOrigin.Y != 0.0f)
		{
			var rScaledX = (float)(bounds.Width * clipTransformOrigin.X);
			var rScaledY = (float)(bounds.Height * clipTransformOrigin.Y);

			// Move to the origin before our transform
			pMatrix = Matrix3x2.CreateTranslation(-1.0f * rScaledX, -1.0f * rScaledY) * pMatrix;

			// Move back after the transform has been applied
			pMatrix *= Matrix3x2.CreateTranslation(rScaledX, rScaledY);
		}
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Applies m_pClipTransform and m_ptClipTransformOrigin to the bounds param.
	//
	//------------------------------------------------------------------------
	internal void TransformBounds(ref Rect bounds)
	{
		var clipMatrix = ClipTransform?.MatrixCore ?? Matrix3x2.Identity;

		if (!clipMatrix.IsIdentity)
		{
			ApplyClipTransformOrigin(bounds, ref clipMatrix);

			bounds = clipMatrix.Transform(bounds);
		}
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Applies some specified bounds as a clip rect, if this transition
	//      target has a clip animation.
	//
	//------------------------------------------------------------------------
	internal void ApplyClip(Rect bounds, ref Rect pClipRect)
	{
		if (m_hasClipAnimation)
		{
			TransformBounds(ref bounds);
			pClipRect.Intersect(bounds);
		}
	}

	// TODO Uno: ApplyClip(XRECTF, TransformAndClipStack*) is not ported; Visual.TransitionClip carries the clip to the compositor.

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Determines whether the point is inside the implicit TransitionTarget clip.
	//
	//------------------------------------------------------------------------
	internal bool ClipToTransitionTarget(ref Rect bounds, Point target)
	{
		bool pHit;
		if (m_hasClipAnimation)
		{
			TransformBounds(ref bounds);
			pHit = bounds.Contains(target);
		}
		else
		{
			pHit = true;
		}

		return pHit;
	}

	// TODO Uno: ClipToTransitionTarget(XRECTF&, HitTestPolygon&, bool*) is not ported (no HitTestPolygon).

	internal void ReplaceTransform(CompositeTransform newTransform)
	{
		if (CompositeTransform != newTransform)
		{
			ClearValue(CompositeTransformProperty);
			MUX_ASSERT(CompositeTransform is null);
			SetValue(CompositeTransformProperty, newTransform);
			MUX_ASSERT(CompositeTransform == newTransform);  // Field was set by property system
		}
	}
}
