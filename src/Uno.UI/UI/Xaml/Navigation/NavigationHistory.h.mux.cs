// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\NavigationHistory.h, tag winui3/release/1.5.5, commit fd8e26f1d

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DirectUI;

partial class NavigationHistory
{
	private bool m_isNavigationPending;

	private bool m_isSetNavigationStatePending;

	// This can be NULL for cases where we skip navigating to current, when NULL it won't be added to the BackStack or ForwardStack.
	private PageStackEntry m_tpCurrentPageStackEntry;
	private PageStackEntry m_tpPendingPageStackEntry;

	private PageStackEntryTrackerCollection m_tpForwardStack;
	private PageStackEntryTrackerCollection m_tpBackStack;

	private Frame m_pIFrame;

	private NavigationMode m_navigationMode;

}
