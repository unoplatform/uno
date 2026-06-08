// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\TestUI\ExtraCommandBarFlyoutPage.xaml.cs, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace MUXControlsTestApp;

[Sample("CommandBarFlyout", "WinUI")]
public sealed partial class ExtraCommandBarFlyoutPage : TestPage
{
	private int customButtonsFlyoutOpenCount = 0;

	public ExtraCommandBarFlyoutPage()
	{
		this.InitializeComponent();

		TextCommandBarContextFlyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
		TextCommandBarSelectionFlyout.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		TextBox1.ContextFlyout = TextCommandBarContextFlyout;
		RichTextBlock1.ContextFlyout = TextCommandBarContextFlyout;
		TextBox1.SelectionFlyout = TextCommandBarSelectionFlyout;
		RichTextBlock1.SelectionFlyout = TextCommandBarSelectionFlyout;
	}

	private void OnClearClipboardContentsClicked(object sender, object args)
	{
		Clipboard.Clear();
	}

	private void OnCountPopupsClicked(object sender, object args)
	{
		PopupCountTextBox.Text = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Count.ToString();
		CustomButtonsOpenCount.Text = customButtonsFlyoutOpenCount.ToString();
	}

	private void tbloaded(object sender, RoutedEventArgs e)
	{
		tb.ContextFlyout = new Microsoft.UI.Xaml.Controls.TextCommandBarFlyout();
		tb.ContextFlyout.Opening += ContextFlyout_Opening;
		tb.ContextFlyout.Closed += ContextFlyout_Closed;
	}

	private void tbunloaded(object sender, RoutedEventArgs e)
	{
		tb.ContextFlyout.Opening -= ContextFlyout_Opening;
	}

	private void ContextFlyout_Opening(object sender, object e)
	{
		customButtonsFlyoutOpenCount++;
		var flyout = (sender as Microsoft.UI.Xaml.Controls.TextCommandBarFlyout);
		flyout.PrimaryCommands.Add(new AppBarButton()
		{
			Content = new TextBlock() { Text = "Test" }
		});
	}

	private void ContextFlyout_Closed(object sender, object e)
	{
		customButtonsFlyoutOpenCount--;
	}
}
