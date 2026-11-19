// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference XamlOptionalChanges_Partial.cpp, OptionalChangeState.h

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Settings;

public partial class XamlOptionalChanges
{
	private const int BitIndex_IconNoGridOptimization = 0;
	private const int BitIndex_OptimizeApplyStyles = 1;
	private const int BitIndex_DefaultStyleOptimizations = 2;
	private const int BitIndex_DeferContextFlyoutInit = 3;

	private const string LockedErrorMessage =
		"XamlOptionalChanges cannot be modified after XAML has been initialized. " +
		"Call EnableChange/DisableChange before Application.Start() or " +
		"WindowsXamlManager.InitializeForCurrentThread().";

	// No atomics: writes are serialized by the lock and complete before XAML init spawns threads that read.
	private static readonly global::System.Threading.Lock _gate = new();
	private static ulong _enabledChanges;
	private static bool _locked;

	internal XamlOptionalChanges()
	{
	}

	public static bool EnableChange(XamlChangeId changeId) => SetChange(changeId, enable: true);

	public static bool DisableChange(XamlChangeId changeId) => SetChange(changeId, enable: false);

	public static bool IsChangeEnabled(XamlChangeId changeId)
	{
		// The public method is safe to call at any time (before or after lock).
		var bit = GetBitIndex(changeId);
		return bit >= 0 && (_enabledChanges & (1UL << bit)) != 0;
	}

	public static bool Lock() => LockInternal();

	public static bool IsLocked() => IsLockedInternal();

	internal static bool IsIconNoGridOptimizationEnabled => IsOptionalChangeEnabled(BitIndex_IconNoGridOptimization);

	internal static bool IsOptimizeApplyStylesEnabled => IsOptionalChangeEnabled(BitIndex_OptimizeApplyStyles);

	internal static bool AreOptimizedStylesEnabled => IsOptionalChangeEnabled(BitIndex_DefaultStyleOptimizations);

	internal static bool IsDeferContextFlyoutInitEnabled => IsOptionalChangeEnabled(BitIndex_DeferContextFlyoutInit);

	internal static bool IsLockedInternal() => _locked;

	internal static bool LockInternal()
	{
		// Fast path: already locked (common case once XAML is initialized).
		if (_locked)
		{
			return false;
		}

		lock (_gate)
		{
			var performedLock = !_locked;
			_locked = true;
			return performedLock;
		}
	}

	internal static void ResetInternal()
	{
		lock (_gate)
		{
			_enabledChanges = 0;
			_locked = false;
		}
	}

	/// <summary>
	/// Test hook: sets a change regardless of the lock, returning its previous state.
	/// </summary>
	internal static bool SetChangeEnabledForTesting(XamlChangeId changeId, bool enabled)
	{
		var bit = GetBitIndex(changeId);
		if (bit < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(changeId));
		}

		lock (_gate)
		{
			var previous = (_enabledChanges & (1UL << bit)) != 0;
			_enabledChanges = enabled ? _enabledChanges | (1UL << bit) : _enabledChanges & ~(1UL << bit);
			return previous;
		}
	}

	private static bool SetChange(XamlChangeId changeId, bool enable)
	{
		var bit = GetBitIndex(changeId);
		bool wasLocked;
		var mutated = false;

		lock (_gate)
		{
			wasLocked = _locked;
			if (!wasLocked && bit >= 0 && bit < 64)
			{
				_enabledChanges = enable ? _enabledChanges | (1UL << bit) : _enabledChanges & ~(1UL << bit);
				mutated = true;
			}
		}

		if (wasLocked)
		{
			throw new InvalidOperationException(LockedErrorMessage);
		}

		return mutated;
	}

	// TODO Uno: WinUI fail-fasts when the platform queries a change before XAML init locked the set.
	// Uno skips that check, as unit tests create elements without Application.Start.
	private static bool IsOptionalChangeEnabled(int bitIndex) => (_enabledChanges & (1UL << bitIndex)) != 0;

	private static int GetBitIndex(XamlChangeId id) => id switch
	{
		XamlChangeId.IconNoGridOptimization => BitIndex_IconNoGridOptimization,
		XamlChangeId.OptimizeApplyStyles => BitIndex_OptimizeApplyStyles,
		XamlChangeId.DefaultStyleOptimizations => BitIndex_DefaultStyleOptimizations,
		XamlChangeId.DeferContextFlyoutInit => BitIndex_DeferContextFlyoutInit,
		_ => -1,
	};
}
