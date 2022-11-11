// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CommandBarFlyoutCommandBarAutomationProperties.cpp, commit d2e60f8

using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class CommandBarFlyoutCommandBarAutomationProperties
{
	// Forward changes to CommandBarFlyoutCommandBarAutomationProperties.ControlType on to
	// AutomationProperties.ControlType.  We need to do this indirection since we can't
	// access types in XAML that we're interacting with via a known interface GUID.
	private static void OnControlTypePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		if (sender is UIElement senderAsUIE)
		{
			AutomationProperties.SetAutomationControlType(
				senderAsUIE,
				(AutomationControlType)args.NewValue);
		}
	}
}
