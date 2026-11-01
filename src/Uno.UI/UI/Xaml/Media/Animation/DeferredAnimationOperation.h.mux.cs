// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference DeferredAnimationOperation.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

// This is a wrapper object used to hold animation operations
// that we don't want to perform immediately. Used by Animations
// and VSM Setters for modifying the animated values of custom
// properties (since this can result in a call out to app code
// [note: it's not clear that this is actually a meaningful
// distinction anyway, because the call out to app code is going to
// happen sooner or later, but legacy...]). VSM Setters also use this
// mechanism to asynchronously modify a property's animated value,
// since it will be deferred to the next tick.
internal sealed partial class DeferredAnimationOperation
{
	internal enum DeferredOperation
	{
		Set,
		Clear
	}

	internal (DependencyObject Object, DependencyProperty Property) Target() => m_target;

	private readonly (DependencyObject Object, DependencyProperty Property) m_target;
	private readonly DependencyObject? m_sourceSetter;
	private readonly object? m_vValue;
	private readonly DeferredOperation m_operation;
#if !HAS_UNO
	private readonly bool m_targetHasPeggedPeer;
#endif
#if HAS_UNO
	private readonly Action? m_reapplyThemeResourceSetter;
#endif
}
