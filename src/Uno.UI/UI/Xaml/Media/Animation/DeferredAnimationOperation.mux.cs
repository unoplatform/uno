// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference DeferredAnimationOperation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class DeferredAnimationOperation
{
	internal DeferredAnimationOperation(
		(DependencyObject Object, DependencyProperty Property) target,
		object? value,
#if !HAS_UNO
		bool targetHasPeggedPeer,
#endif
		DeferredOperation operation,
		DependencyObject? sourceSetter
#if HAS_UNO
		, Action? reapplyThemeResourceSetter = null
#endif
		)
	{
		m_target = target;
#if !HAS_UNO
		m_targetHasPeggedPeer = targetHasPeggedPeer;
#endif
		m_operation = operation;
		m_sourceSetter = sourceSetter;
#if HAS_UNO
		m_reapplyThemeResourceSetter = reapplyThemeResourceSetter;
#endif

		if (value != DependencyProperty.UnsetValue)
		{
			m_vValue = value;
		}
	}

#if !HAS_UNO
	// TODO Uno: Uno doesn't peg managed peers.
	~CDeferredAnimationOperation()
	{
		if (m_target.first && m_targetHasPeggedPeer)
		{
			m_target.first->UnpegManagedPeer();
		}
	}
#endif

	internal void Execute()
	{
		switch (m_operation)
		{
			case DeferredOperation.Set:
				{
					SetAnimatedValue();
					break;
				}
			case DeferredOperation.Clear:
				{
					ClearAnimatedValue();
					break;
				}
			default:
				{
					throw new InvalidOperationException();
				}
		}
	}

	private void SetAnimatedValue()
	{
#if HAS_UNO
		// TODO Uno: WinUI's animated value carries its theme reference. Uno's {ThemeResource} setters bind instead, so
		// they are applied again to restore the binding.
		if (m_reapplyThemeResourceSetter is { } reapply)
		{
			reapply();
			return;
		}
#endif

		// This can cause a synchronous callout to user code.
		m_target.Object.SetValue(m_target.Property, m_vValue, DependencyPropertyValuePrecedences.Animations);
	}

	private void ClearAnimatedValue()
	{
		// This can cause a synchronous callout to user code.
		m_target.Object.ClearValue(m_target.Property, DependencyPropertyValuePrecedences.Animations);
	}
}
