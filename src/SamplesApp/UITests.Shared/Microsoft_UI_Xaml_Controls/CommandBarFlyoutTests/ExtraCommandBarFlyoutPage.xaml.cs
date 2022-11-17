// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MUXControlsTestApp
{
	[Sample("CommandBarFlyout", "WinUI")]
    public sealed partial class ExtraCommandBarFlyoutPage : TestPage
    {
        private int customButtonsFlyoutOpenCount = 0;

        public ExtraCommandBarFlyoutPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode", "BottomEdgeAlignedLeft"))
            {
                TextCommandBarContextFlyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
            }
            else
            {
                TextCommandBarContextFlyout.Placement = FlyoutPlacementMode.Top;
            }

            if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode", "TopEdgeAlignedLeft"))
            {
                TextCommandBarSelectionFlyout.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
            }
            else
            {
                TextCommandBarSelectionFlyout.Placement = FlyoutPlacementMode.Top;
            }

            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "ContextFlyout"))
            {
                TextBox1.ContextFlyout = TextCommandBarContextFlyout;
                RichTextBlock1.ContextFlyout = TextCommandBarContextFlyout;
            }

            try
            {
                if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.TextBox", "SelectionFlyout"))
                {
                    TextBox1.SelectionFlyout = TextCommandBarSelectionFlyout;
                }
            }
            catch (InvalidCastException)
            {
                // RS5 interfaces can change before release, so we need to make sure we don't crash if they do.
            }

            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.RichTextBlock", "SelectionFlyout"))
            {
                RichTextBlock1.SelectionFlyout = TextCommandBarSelectionFlyout;
            }
        }

        private void OnClearClipboardContentsClicked(object sender, object args)
        {
            Clipboard.Clear();
        }

        private void OnCountPopupsClicked(object sender, object args)
        {
            PopupCountTextBox.Text = VisualTreeHelper.GetOpenPopups(Window.Current).Count.ToString();
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
            flyout.PrimaryCommands.Add(new AppBarButton() {
                Content = new TextBlock() { Text = "Test" }
            });
        }

        private void ContextFlyout_Closed(object sender, object e)
        {
            customButtonsFlyoutOpenCount--;
        }
    }
}
