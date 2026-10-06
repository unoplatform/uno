// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.cpp, tag winui3/release/2.5.1

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class ListViewBaseItemChrome
{
	// The "ListViewBaseItemRoundedChromeEnabled" theme resource value is used to turn on/off the rendering with rounded corners.
	// For performance reasons, the resource is only evaluated once. TAEF tests can invalidate the cache by calling TestServices::Utilities::DeleteResourceDictionaryCaches().
	internal bool IsRoundedListViewBaseItemChromeEnabled()
	{
		s_isRoundedListViewBaseItemChromeEnabled ??= IsRoundedListViewBaseItemChromeEnabledStatic();

		return s_isRoundedListViewBaseItemChromeEnabled.Value;
	}

	// Invoked when a TAEF test calls TestServices::Utilities::DeleteResourceDictionaryCaches().
	internal static void ClearIsRoundedListViewBaseItemChromeEnabledCache()
		=> s_isRoundedListViewBaseItemChromeEnabled = null;

	internal static bool IsRoundedListViewBaseItemChromeEnabledStatic()
	{
		if (IsRoundedListViewBaseItemChromeForced())
		{
			return true;
		}

		return DependencyProperty.GetBooleanThemeResourceValue("ListViewBaseItemRoundedChromeEnabled");
	}

	internal static bool IsRoundedListViewBaseItemChromeForced()
	{
		if (ListViewBaseItemChromeRuntimeFeatures.DenyRoundedListViewBaseItemChrome)
		{
			return false;
		}

		return ListViewBaseItemChromeRuntimeFeatures.ForceRoundedListViewBaseItemChrome;
	}
}
