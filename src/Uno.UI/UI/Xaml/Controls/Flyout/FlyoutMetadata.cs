// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutMetadata.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using System.Diagnostics;

namespace Microsoft.UI.Xaml.Controls.Primitives;

// Storage class in DXamlcore to enforce one Flyout at a time requirement.
internal sealed class FlyoutMetadata
{
	private FlyoutBase m_tpOpenFlyout;
	private FrameworkElement m_tpOpenFlyoutPlacementTarget;
	private FlyoutBase m_tpStagedFlyout;
	private FrameworkElement m_tpStagedFlyoutTarget;

	internal void SetOpenFlyout(FlyoutBase pFlyout, FrameworkElement pPlacementTarget)
	{
		m_tpOpenFlyout = pFlyout;
		m_tpOpenFlyoutPlacementTarget = pPlacementTarget;
	}

	internal void GetOpenFlyout(out FlyoutBase ppFlyout, out FrameworkElement ppPlacementTarget)
	{
		ppFlyout = m_tpOpenFlyout;
		ppPlacementTarget = m_tpOpenFlyoutPlacementTarget;
	}

	internal void SetStagedFlyout(FlyoutBase pFlyout, FrameworkElement pPlacementTarget)
	{
		// We should never set a staged flyout without an already
		// open flyout.
		Debug.Assert(pFlyout is null || m_tpOpenFlyout is not null);

		m_tpStagedFlyout = pFlyout;
		m_tpStagedFlyoutTarget = pPlacementTarget;
	}

	internal void GetStagedFlyout(out FlyoutBase ppFlyout, out FrameworkElement ppPlacementTarget)
	{
		ppFlyout = m_tpStagedFlyout;
		ppPlacementTarget = m_tpStagedFlyoutTarget;
	}
}
