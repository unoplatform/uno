﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\TestUI\CommandBarFlyoutMainPage.xaml.cs, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#if false
using MUXControlsTestHooks = Microsoft.UI.Private.Controls.MUXControlsTestHooks;
#endif

namespace MUXControlsTestApp;

[Sample("CommandBarFlyout", "WinUI")]
public sealed partial class CommandBarFlyoutMainPage : TestPage
{
	public CommandBarFlyoutMainPage()
	{
		this.InitializeComponent();
	}

	public void OnCommandBarFlyoutTestsClicked(object sender, object args)
	{
		var rootFrame = XamlRoot.Content as Frame;
		rootFrame.NavigateWithoutAnimation(typeof(CommandBarFlyoutPage), "CommandBarFlyout Tests");
	}

	public void OnTextCommandBarFlyoutTestsClicked(object sender, object args)
	{
		var rootFrame = XamlRoot.Content as Frame;
		rootFrame.NavigateWithoutAnimation(typeof(TextCommandBarFlyoutPage), "TextCommandBarFlyout Tests");
	}

	public void OnExtraCommandBarFlyoutTestsClicked(object sender, object args)
	{
		var rootFrame = XamlRoot.Content as Frame;
		rootFrame.NavigateWithoutAnimation(typeof(ExtraCommandBarFlyoutPage), "Extra CommandBarFlyout Tests");
	}
	private void CmbCommandBarFlyoutOutputDebugStringLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
#if false // Uno specific: Not needed.
		MUXControlsTestHooks.SetOutputDebugStringLevelForType(
            "CommandBarFlyout",
            cmbCommandBarFlyoutOutputDebugStringLevel.SelectedIndex == 1 || cmbCommandBarFlyoutOutputDebugStringLevel.SelectedIndex == 2,
            cmbCommandBarFlyoutOutputDebugStringLevel.SelectedIndex == 2);
#endif
	}
}
