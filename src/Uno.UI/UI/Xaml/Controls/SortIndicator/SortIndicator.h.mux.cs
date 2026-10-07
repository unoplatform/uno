// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SortIndicator\SortIndicator.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls;

partial class SortIndicator
{
	// SortIndicator();

	// IFrameworkElement overrides
	// void OnApplyTemplate();

	// IUIElement overrides
	// winrt::AutomationPeer OnCreateAutomationPeer();

	// void OnDirectionPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// private:
	// void UpdateVisualState(bool useTransitions);
	private FrameworkElement? m_layoutRoot;
	private FontIcon? m_glyphIcon;
}
