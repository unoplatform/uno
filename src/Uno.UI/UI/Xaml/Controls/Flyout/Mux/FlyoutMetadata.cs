// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\FlyoutMetadata.cpp, tag winui3/release/1.4.3, commit 685d2bf

#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace DirectUI;

// FlyoutMetadata is used to keep track of currently open flyout as well
// as stage flyouts while waiting for an open one to close.
// The instance on DXamlCore tracks the root flyout, of which there can
// be only 1 open at a time.
// However, another flyout can be opened if its placement target is within
// an already opened flyout presenter's tree.
// In those cases, instances of FlyoutMetadata are used to track the child
// and staged child flyouts for any open flyout.
internal class FlyoutMetadata
{
	private FlyoutBase? _openFlyout;
	private FrameworkElement? _openFlyoutPlacementTarget;

	private FlyoutBase? _stagedFlyout;
	private FrameworkElement? _stagedFlyoutTarget;

	internal void SetOpenFlyout(FlyoutBase? flyout, FrameworkElement? placementTarget)
	{
		_openFlyout = flyout;
		_openFlyoutPlacementTarget = placementTarget;
	}

	internal void GetOpenFlyout(out FlyoutBase? flyout, out FrameworkElement? placementTarget)
	{
		flyout = _openFlyout;
		placementTarget = _openFlyoutPlacementTarget;
	}

	internal void SetStagedFlyout(FlyoutBase? flyout, FrameworkElement? placementTarget)
	{
		// We should never set a staged flyout without an already
		// open flyout.
		MUX_ASSERT(flyout is null || _openFlyout is not null);

		_stagedFlyout = flyout;
		_stagedFlyoutTarget = placementTarget;
	}

	internal void GetStagedFlyout(out FlyoutBase? flyout, out FrameworkElement? placementTarget)
	{
		flyout = _stagedFlyout;
		placementTarget = _stagedFlyoutTarget;
	}
}
