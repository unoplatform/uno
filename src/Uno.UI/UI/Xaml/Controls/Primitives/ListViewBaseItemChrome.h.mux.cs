// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.h, tag winui3/release/2.5.1

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class ListViewBaseItemChrome
{
	private static bool? s_isRoundedListViewBaseItemChromeEnabled;
}

// TODO Uno: RuntimeEnabledFeatureDetector
internal static class ListViewBaseItemChromeRuntimeFeatures
{
	internal static bool ForceRoundedListViewBaseItemChrome { get; private set; }

	internal static bool DenyRoundedListViewBaseItemChrome { get; private set; }

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
