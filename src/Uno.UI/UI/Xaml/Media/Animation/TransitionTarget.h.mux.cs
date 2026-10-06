// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TransitionTarget.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

[Flags]
internal enum TransitionTargetDirtyMode
{
	Dirty_None = 0,
	Dirty_Transform = 1,
	Dirty_Clip = 2,
	Dirty_Opacity = 4,
	Dirty_All = 0x7
}

partial class TransitionTarget
{
	internal bool HasClipAnimation => m_hasClipAnimation;

	internal void SetHasClipAnimation() => m_hasClipAnimation = true;

	// TODO Uno: NeedsWUCOpacityExpression, EnsureWUCOpacityExpression, ClearWUCOpacityExpression, GetWUCOpacityExpression,
	// CleanupDeviceRelatedResourcesRecursive, ReleaseDCompResources and EnsureWUCAnimationStarted are not ported (no WUC expressions).

	// m_pxf, m_pClipTransform, m_opacity, m_ptRenderTransformOrigin and m_ptClipTransformOrigin are the
	// CompositeTransform, ClipTransform, Opacity, TransformOrigin and ClipTransformOrigin properties.

	private bool m_hasClipAnimation;

	private TransitionTargetDirtyMode m_currentDirtyMode = TransitionTargetDirtyMode.Dirty_All;
}
