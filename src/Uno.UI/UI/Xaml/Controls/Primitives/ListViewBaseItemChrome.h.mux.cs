// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.h, tag winui3/release/2.5.1

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class ListViewBaseItemChrome
{
	private static bool? s_isRoundedListViewBaseItemChromeEnabled;

	// Used by DependencyProperty::GetDefaultValue
	internal static readonly CornerRadius s_defaultSelectionIndicatorCornerRadius = new(1.5);
	internal static readonly CornerRadius s_defaultCheckBoxCornerRadius = new(3.0);
	internal static readonly Thickness s_selectedBorderThicknessRounded = new(2.0);
	internal static readonly Thickness s_selectedBorderThickness = new(0.0);

	internal static float GetDefaultDisabledOpacity(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome ? 0.3f : 0.55f;

	internal static float GetDefaultDragOpacity() => 0.8f;

	internal static float GetDefaultListViewItemReorderHintOffset() => 10.0f;

	internal static float GetDefaultGridViewItemReorderHintOffset() => 16.0f;

	internal static float GetSelectedBorderThickness(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome ? 2.0f : 0.0f;

	internal static bool GetDefaultSelectionCheckMarkVisualEnabled() => true;

	internal static Thickness GetSelectedBorderXThickness(bool forRoundedListViewBaseItemChrome)
		=> forRoundedListViewBaseItemChrome ? s_selectedBorderThicknessRounded : s_selectedBorderThickness;
}

// TODO Uno: RuntimeEnabledFeatureDetector
internal static class ListViewBaseItemChromeRuntimeFeatures
{
	internal static bool ForceRoundedListViewBaseItemChrome { get; private set; }

	internal static bool DenyRoundedListViewBaseItemChrome { get; private set; }

	// Scopes must be disposed in reverse order of creation.
	internal static IDisposable Override(bool? forceRounded = null, bool? denyRounded = null)
	{
		var previousForce = ForceRoundedListViewBaseItemChrome;
		var previousDeny = DenyRoundedListViewBaseItemChrome;

		ForceRoundedListViewBaseItemChrome = forceRounded ?? previousForce;
		DenyRoundedListViewBaseItemChrome = denyRounded ?? previousDeny;
		ListViewBaseItemChrome.ClearIsRoundedListViewBaseItemChromeEnabledCache();

		return new Restore(previousForce, previousDeny);
	}

	private sealed class Restore : IDisposable
	{
		private readonly bool _force;
		private readonly bool _deny;
		private bool _disposed;

		public Restore(bool force, bool deny)
		{
			_force = force;
			_deny = deny;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			ForceRoundedListViewBaseItemChrome = _force;
			DenyRoundedListViewBaseItemChrome = _deny;
			ListViewBaseItemChrome.ClearIsRoundedListViewBaseItemChromeEnabledCache();
		}
	}
}
