// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using MUXControlsTestApp.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using CommandBarFlyout = Microsoft.UI.Xaml.Controls.CommandBarFlyout;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
    [TestClass]
    public class CommandBarFlyoutTests : MUXApiTestBase
    {
        [TestMethod]
        [TestProperty("Description", "Verifies the CommandBarFlyout's default properties.")]
        public void VerifyFlyoutDefaultPropertyValues()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled pre-RS2 because CommandBarFlyout is not supported pre-RS2");
                return;
            }

            RunOnUIThread.Execute(() =>
            {
                CommandBarFlyout commandBarFlyout = new CommandBarFlyout();
                Verify.IsNotNull(commandBarFlyout);

                Verify.IsNotNull(commandBarFlyout.PrimaryCommands);
                Verify.AreEqual(0, commandBarFlyout.PrimaryCommands.Count);
                Verify.IsNotNull(commandBarFlyout.SecondaryCommands);
                Verify.AreEqual(0, commandBarFlyout.SecondaryCommands.Count);
            });
        }

        [TestMethod]
        [TestProperty("Description", "Verifies that setting commands on CommandBarFlyout causes them to propagate down to its CommandBar.")]
        public void VerifyFlyoutCommandsArePropagatedToTheCommandBar()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled pre-RS2 because CommandBarFlyout is not supported pre-RS2");
                return;
            }

            CommandBarFlyout commandBarFlyout = null;
            Button commandBarFlyoutTarget = null;

            SetupCommandBarFlyoutTest(out commandBarFlyout, out commandBarFlyoutTarget);
            OpenFlyout(commandBarFlyout, commandBarFlyoutTarget);

            RunOnUIThread.Execute(() =>
            {
                Popup flyoutPopup = VisualTreeHelper.GetOpenPopups(Window.Current).Last();
                CommandBar commandBar = TestUtilities.FindDescendents<CommandBar>(flyoutPopup).Single();

                Verify.AreEqual(commandBarFlyout.PrimaryCommands.Count, commandBar.PrimaryCommands.Count);

                for (int i = 0; i < commandBarFlyout.PrimaryCommands.Count; i++)
                {
                    Verify.AreEqual(commandBarFlyout.PrimaryCommands[i], commandBar.PrimaryCommands[i]);
                }

                Verify.AreEqual(commandBarFlyout.SecondaryCommands.Count, commandBar.SecondaryCommands.Count);

                for (int i = 0; i < commandBarFlyout.SecondaryCommands.Count; i++)
                {
                    Verify.AreEqual(commandBarFlyout.SecondaryCommands[i], commandBar.SecondaryCommands[i]);
                }
            });

            CloseFlyout(commandBarFlyout);
        }

        private enum CommandBarSizingOptions
        {
            PrimaryItemsLarger,
            SecondaryItemsLarger,
            SecondaryItemsMaxWidth,
            SecondaryItemsMaxHeight,
        }

        [TestMethod]
        [TestProperty("Description", "Verifies that the overflow popup sizes itself to be the size of the main command bar if the primary items section width is larger than the secondary items section width.")]
        public void VerifyCommandBarSizingPrimaryItemsLarger()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled pre-RS2 because CommandBarFlyout is not supported pre-RS2");
                return;
            }

            VerifyCommandBarSizing(CommandBarSizingOptions.PrimaryItemsLarger);
        }

        [TestMethod]
        [TestProperty("Description", "Verifies that the main command bar sizes itself to be the size of the overflow popup when open if the primary items section width is smaller than the secondary items section width.")]
        public void VerifyCommandBarSizingSecondaryItemsLarger()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled pre-RS2 because CommandBarFlyout is not supported pre-RS2");
                return;
            }

            VerifyCommandBarSizing(CommandBarSizingOptions.SecondaryItemsLarger);
        }

        [TestMethod]
        [TestProperty("Description", "Verifies that the command bar and overflow popup do not size themselves to be larger than the max width if a very wide AppBarButton is present.")]
        public void VerifyCommandBarSizingSecondaryItemsMaxWidth()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled pre-RS2 because CommandBarFlyout is not supported pre-RS2");
                return;
            }

            VerifyCommandBarSizing(CommandBarSizingOptions.SecondaryItemsMaxWidth);
        }

        [TestMethod]
        [TestProperty("Description", "Verifies that the overflow popup does not size itself to be larger than its max height if a sufficiently large number of AppBarButtons are present.")]
        public void VerifyCommandBarSizingSecondaryItemsMaxHeight()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled pre-RS2 because CommandBarFlyout is not supported pre-RS2");
                return;
            }

            VerifyCommandBarSizing(CommandBarSizingOptions.SecondaryItemsMaxHeight);
        }

        private void VerifyCommandBarSizing(CommandBarSizingOptions sizingOptions)
        {
            CommandBarFlyout commandBarFlyout = null;
            Button commandBarFlyoutTarget = null;

            SetupCommandBarFlyoutTest(out commandBarFlyout, out commandBarFlyoutTarget);

            RunOnUIThread.Execute(() =>
            {
                switch (sizingOptions)
                {
                    case CommandBarSizingOptions.SecondaryItemsLarger:
                        commandBarFlyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item with a label wider than primary items" });
                        break;
                    case CommandBarSizingOptions.SecondaryItemsMaxWidth:
                        commandBarFlyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item with a really really really long label that will not fit in the space provided" });
                        break;
                    case CommandBarSizingOptions.SecondaryItemsMaxHeight:
                        for (int i = 0; i < 20; i++)
                        {
                            commandBarFlyout.SecondaryCommands.Add(new AppBarButton() { Label = "Do another thing" });
                        }
                        break;
                }
            });

            OpenFlyout(commandBarFlyout, commandBarFlyoutTarget);

            CommandBar commandBar = null;

            RunOnUIThread.Execute(() =>
            {
                Popup flyoutPopup = VisualTreeHelper.GetOpenPopups(Window.Current).Last();
                commandBar = TestUtilities.FindDescendents<CommandBar>(flyoutPopup).Single();
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                // Pre-RS5, CommandBarFlyouts always open expanded, so to put us in a known good state,
                // we'll collapse the flyout before we do anything else.
                commandBar.IsOpen = false;
            });

            IdleSynchronizer.Wait();

            double originalWidth = 0;
            double originalHeight = 0;

            RunOnUIThread.Execute(() =>
            {
                Popup flyoutPopup = VisualTreeHelper.GetOpenPopups(Window.Current).Last();
                commandBar = TestUtilities.FindDescendents<CommandBar>(flyoutPopup).Single();

                originalWidth = commandBar.ActualWidth;
                originalHeight = commandBar.ActualHeight;

                commandBar.IsOpen = true;
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                CommandBarOverflowPresenter overflowPresenter = TestUtilities.FindDescendents<CommandBarOverflowPresenter>(commandBar).Single();

                if (sizingOptions == CommandBarSizingOptions.PrimaryItemsLarger ||
                    sizingOptions == CommandBarSizingOptions.SecondaryItemsMaxHeight)
                {
                    Verify.AreEqual(originalWidth, commandBar.ActualWidth);
                    Verify.AreEqual(originalWidth, overflowPresenter.ActualWidth);
                }
                else
                {
                    Verify.IsLessThan(originalWidth, commandBar.ActualWidth);
                    Verify.AreEqual(overflowPresenter.ActualWidth, commandBar.ActualWidth);
                }

                Verify.AreEqual(originalHeight, commandBar.ActualHeight);

                if (sizingOptions == CommandBarSizingOptions.SecondaryItemsMaxWidth)
                {
                    Verify.AreEqual(commandBar.MaxWidth, commandBar.ActualWidth);
                }
                else if (sizingOptions == CommandBarSizingOptions.SecondaryItemsMaxHeight)
                {
                    Verify.AreEqual(overflowPresenter.MaxHeight, overflowPresenter.ActualHeight);
                }

                commandBar.IsOpen = false;
            });

            IdleSynchronizer.Wait();
            CloseFlyout(commandBarFlyout);
        }

        [TestMethod]
        [TestProperty("Description", "Verifies that the overflow popup does not size itself to be larger than its max height if a sufficiently large number of AppBarButtons are present.")]
        public void VerifyPrimaryCommandsCanOverflowToSecondaryItemsControl()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled pre-RS2 because CommandBarFlyout is not supported pre-RS2");
                return;
            }

            CommandBarFlyout flyout = null;
            Button flyoutTarget = null;

            RunOnUIThread.Execute(() =>
            {
                flyout = new CommandBarFlyout() { Placement = FlyoutPlacementMode.Right };

                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 1" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 2" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 3" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 4" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 5" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 6" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 7" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 8" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 9" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 10" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 11" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 12" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 13" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 14" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 15" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 16" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 17" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 18" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 19" });
                flyout.PrimaryCommands.Add(new AppBarButton() { Label = "Item 20" });

                flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 21" });
                flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 22" });
                flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 23" });
                flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 24" });
                flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 25" });

                flyoutTarget = new Button() { Content = "Click for flyout" };
                Content = flyoutTarget;
                Content.UpdateLayout();
            });

            OpenFlyout(flyout, flyoutTarget);

            RunOnUIThread.Execute(() =>
            {
                Popup flyoutPopup = VisualTreeHelper.GetOpenPopups(Window.Current).Last();
                CommandBar commandBar = TestUtilities.FindDescendents<CommandBar>(flyoutPopup).Single();

                IList<ItemsControl> itemsControls = TestUtilities.FindDescendents<ItemsControl>(commandBar);

                Log.Comment("We expect there to be 2 ItemsControls inside the CommandBar; {0} were found.", itemsControls.Count);
                Verify.AreEqual(2, itemsControls.Count);

                ItemsControl primaryItemsControl = itemsControls[0];
                ItemsControl secondaryItemsControl = itemsControls[1];

                Log.Comment("We expect there to be 9 items located inside the primary ItemsControl; {0} were found.", primaryItemsControl.Items.Count);
                Verify.AreEqual(9, primaryItemsControl.Items.Count);
                Log.Comment("We expect there to be 17 items located inside the secondary ItemsControl (16 + autogenerated separator); {0} were found.", secondaryItemsControl.Items.Count);
                Verify.AreEqual(17, secondaryItemsControl.Items.Count);
            });
            
            CloseFlyout(flyout);
        }

        private void SetupCommandBarFlyoutTest(out CommandBarFlyout flyout, out Button flyoutTarget)
        {
            CommandBarFlyout commandBarFlyout = null;
            Button commandBarFlyoutTarget = null;

            RunOnUIThread.Execute(() =>
            {
                commandBarFlyout = new CommandBarFlyout() { Placement = FlyoutPlacementMode.Right };

                commandBarFlyout.PrimaryCommands.Add(new AppBarButton() { Icon = new SymbolIcon(Symbol.Cut) });
                commandBarFlyout.PrimaryCommands.Add(new AppBarButton() { Icon = new SymbolIcon(Symbol.Copy) });
                commandBarFlyout.PrimaryCommands.Add(new AppBarButton() { Icon = new SymbolIcon(Symbol.Paste) });
                commandBarFlyout.PrimaryCommands.Add(new AppBarButton() { Icon = new SymbolIcon(Symbol.Bold) });
                commandBarFlyout.PrimaryCommands.Add(new AppBarButton() { Icon = new SymbolIcon(Symbol.Italic) });
                commandBarFlyout.PrimaryCommands.Add(new AppBarButton() { Icon = new SymbolIcon(Symbol.Underline) });

                AppBarButton undoButton = new AppBarButton() { Label = "Undo", Icon = new SymbolIcon(Symbol.Undo) };
                commandBarFlyout.SecondaryCommands.Add(undoButton);
                AppBarButton redoButton = new AppBarButton() { Label = "Redo", Icon = new SymbolIcon(Symbol.Redo) };
                commandBarFlyout.SecondaryCommands.Add(redoButton);
                AppBarButton selectAllButton = new AppBarButton() { Label = "Select all" };
                commandBarFlyout.SecondaryCommands.Add(selectAllButton);

                if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "KeyboardAccelerators"))
                {
                    undoButton.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
                    redoButton.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
                    selectAllButton.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
                }

                commandBarFlyoutTarget = new Button() { Content = "Click for flyout" };
                Content = commandBarFlyoutTarget;
                Content.UpdateLayout();
            });

            flyout = commandBarFlyout;
            flyoutTarget = commandBarFlyoutTarget;
        }

        private void OpenFlyout(CommandBarFlyout flyout, Button flyoutTarget)
        {
            Log.Comment("Opening flyout...");
            AutoResetEvent openedEvent = new AutoResetEvent(false);

            RunOnUIThread.Execute(() =>
            {
                flyout.Opened += (sender, args) => openedEvent.Set();

                if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Controls.Primitives.FlyoutShowMode"))
                {
                    flyout.ShowAt(flyoutTarget, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Transient });
                }
                else
                {
                    flyout.ShowAt(flyoutTarget);
                }
            });

            TestUtilities.WaitForEvent(openedEvent);
            IdleSynchronizer.Wait();
            Log.Comment("Flyout opened.");
        }

        private void CloseFlyout(CommandBarFlyout flyout)
        {
            Log.Comment("Closing flyout...");
            AutoResetEvent closedEvent = new AutoResetEvent(false);

            RunOnUIThread.Execute(() =>
            {
                flyout.Closed += (sender, args) => closedEvent.Set();
                flyout.Hide();
            });

            TestUtilities.WaitForEvent(closedEvent);
            IdleSynchronizer.Wait();
            Log.Comment("Flyout closed.");
        }
    }
}
