// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\APITests\CommandBarFlyoutTests.cs, tag winui3/release/1.6.3, commit 66d24dfff3b2763ab3be096a2c7cbaafc81b31eb

using Common;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using Private.Infrastructure;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

[TestClass]
public class CommandBarFlyoutTests : MUXApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies the CommandBarFlyout's default properties.")]
	public void VerifyFlyoutDefaultPropertyValues()
	{
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
	public async Task VerifyFlyoutCommandsArePropagatedToTheCommandBar()
	{
		CommandBarFlyout commandBarFlyout = null;
		Button commandBarFlyoutTarget = null;

		SetupCommandBarFlyoutTest(out commandBarFlyout, out commandBarFlyoutTarget);
		await OpenFlyout(commandBarFlyout, commandBarFlyoutTarget);

		RunOnUIThread.Execute(() =>
		{
			Popup flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(commandBarFlyout.XamlRoot).Last();
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

		await CloseFlyout(commandBarFlyout);
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
	public async Task VerifyCommandBarSizingPrimaryItemsLarger()
	{
		await VerifyCommandBarSizing(CommandBarSizingOptions.PrimaryItemsLarger);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that the main command bar sizes itself to be the size of the overflow popup when open if the primary items section width is smaller than the secondary items section width.")]
	public async Task VerifyCommandBarSizingSecondaryItemsLarger()
	{
		await VerifyCommandBarSizing(CommandBarSizingOptions.SecondaryItemsLarger);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that the command bar and overflow popup do not size themselves to be larger than the max width if a very wide AppBarButton is present.")]
	public async Task VerifyCommandBarSizingSecondaryItemsMaxWidth()
	{
		await VerifyCommandBarSizing(CommandBarSizingOptions.SecondaryItemsMaxWidth);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that the overflow popup does not size itself to be larger than its max height if a sufficiently large number of AppBarButtons are present.")]
	public async Task VerifyCommandBarSizingSecondaryItemsMaxHeight()
	{
		await VerifyCommandBarSizing(CommandBarSizingOptions.SecondaryItemsMaxHeight);
	}

	private async Task VerifyCommandBarSizing(CommandBarSizingOptions sizingOptions)
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

		await OpenFlyout(commandBarFlyout, commandBarFlyoutTarget);

		CommandBar commandBar = null;

		RunOnUIThread.Execute(() =>
		{
			Popup flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(commandBarFlyout.XamlRoot).Last();
			commandBar = TestUtilities.FindDescendents<CommandBar>(flyoutPopup).Single();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Pre-RS5, CommandBarFlyouts always open expanded, so to put us in a known good state,
			// we'll collapse the flyout before we do anything else.
			commandBar.IsOpen = false;
		});

		await TestServices.WindowHelper.WaitForIdle();

		double originalWidth = 0;
		double originalHeight = 0;

		RunOnUIThread.Execute(() =>
		{
			Popup flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(commandBar.XamlRoot).Last();
			commandBar = TestUtilities.FindDescendents<CommandBar>(flyoutPopup).Single();

			originalWidth = commandBar.ActualWidth;
			originalHeight = commandBar.ActualHeight;

			commandBar.IsOpen = true;
		});

		await TestServices.WindowHelper.WaitForIdle();

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

		await TestServices.WindowHelper.WaitForIdle();
		await CloseFlyout(commandBarFlyout);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that the overflow popup does not size itself to be larger than its max height if a sufficiently large number of AppBarButtons are present.")]
	public async Task VerifyPrimaryCommandsCanOverflowToSecondaryItemsControl()
	{
		CommandBarFlyout flyout = null;
		Button flyoutTarget = null;

		RunOnUIThread.Execute(() =>
		{
			flyout = new CommandBarFlyout() { Placement = FlyoutPlacementMode.Right };

			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());
			flyout.PrimaryCommands.Add(new AppBarButton());

			flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 21" });
			flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 22" });
			flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 23" });
			flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 24" });
			flyout.SecondaryCommands.Add(new AppBarButton() { Label = "Item 25" });

			flyoutTarget = new Button() { Content = "Click for flyout" };
			Content = flyoutTarget;
			Content.UpdateLayout();
		});

		await OpenFlyout(flyout, flyoutTarget);

		RunOnUIThread.Execute(() =>
		{
			Popup flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot).Last();
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

		await CloseFlyout(flyout);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that labels cause primary commmands to be wider than without, and to have their labels be visible.")]
	public async Task VerifyPrimaryCommandLabelsAffectLayout()
	{
		CommandBarFlyout flyout = null;
		Button flyoutTarget = null;
		AppBarButton button1 = null;
		AppBarButton button2 = null;

		RunOnUIThread.Execute(() =>
		{
			flyout = new CommandBarFlyout() { Placement = FlyoutPlacementMode.Right };

			button1 = new AppBarButton();
			button2 = new AppBarButton();

			flyout.PrimaryCommands.Add(button1);
			flyout.PrimaryCommands.Add(button2);

			flyoutTarget = new Button() { Content = "Click for flyout" };
			Content = flyoutTarget;
			Content.UpdateLayout();
		});

		await OpenFlyout(flyout, flyoutTarget);

		double originalWidth = 0;
		double originalHeight = 0;

		CommandBar commandBar = null;
		TextBlock button1TextLabel = null;
		TextBlock button2TextLabel = null;

		RunOnUIThread.Execute(() =>
		{
			button1TextLabel = TestUtilities.FindDescendents<TextBlock>(button1).Where(textBlock => textBlock.Name == "TextLabel").Single();
			button2TextLabel = TestUtilities.FindDescendents<TextBlock>(button2).Where(textBlock => textBlock.Name == "TextLabel").Single();

			Log.Comment("We expect the TextLabel template parts to be collapsed when the Label property is empty.");
			Verify.AreEqual(Visibility.Collapsed, button1TextLabel.Visibility);
			Verify.AreEqual(Visibility.Collapsed, button2TextLabel.Visibility);

			Popup flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot).Last();
			commandBar = TestUtilities.FindDescendents<CommandBar>(flyoutPopup).Single();

			originalWidth = commandBar.ActualWidth;
			originalHeight = commandBar.ActualHeight;
		});

		await CloseFlyout(flyout);

		RunOnUIThread.Execute(() =>
		{
			button1.Label = "Item 1";
		});

		await OpenFlyout(flyout, flyoutTarget);

		double finalWidth = 0;
		double finalHeight = 0;

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("We expect the TextLabel template parts to be visible when the Label property is set.");
			Verify.AreEqual(Visibility.Visible, button1TextLabel.Visibility);
			Verify.AreEqual(Visibility.Visible, button2TextLabel.Visibility);

			finalWidth = commandBar.ActualWidth;
			finalHeight = commandBar.ActualHeight;

			Log.Comment("We also expect the width and height of the AppBarButtons to be larger when the Label property is set on at least one primary command.");
			Verify.IsGreaterThan(finalWidth, originalWidth);
			Verify.IsGreaterThan(finalHeight, originalHeight);
		});

		await CloseFlyout(flyout);

		RunOnUIThread.Execute(() =>
		{
			button2.Label = "Item 2";
		});

		await OpenFlyout(flyout, flyoutTarget);

		RunOnUIThread.Execute(() =>
		{
			Log.Comment("Having all labels set should not make things any wider or taller than only some labels set.");
			Verify.AreEqual(Math.Round(finalWidth), Math.Round(commandBar.ActualWidth));
			Verify.AreEqual(Math.Round(finalHeight), Math.Round(commandBar.ActualHeight));
		});

		await CloseFlyout(flyout);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies that CommandBarFlyoutCommandBar can set its SystemBackdrop without being parented to the tree.")]
	public async Task VerifyDisconnectedFlyoutCommandBar()
	{
		CommandBarFlyoutCommandBar cbfcb = null;
		DesktopAcrylicBackdrop dba = null;

		RunOnUIThread.Execute(() =>
		{
			cbfcb = new CommandBarFlyoutCommandBar();
			dba = new DesktopAcrylicBackdrop();

			// cbfcb isn't in the tree yet. Setting this shouldn't crash. The parser will do this (set properties before inserting into tree).
			cbfcb.SystemBackdrop = dba;

			// dbfcb shouldn't crash when it's destroyed.
			cbfcb = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		});

		await TestServices.WindowHelper.WaitForIdle();
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

			undoButton.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
			redoButton.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
			selectAllButton.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });

			commandBarFlyoutTarget = new Button() { Content = "Click for flyout" };
			Content = commandBarFlyoutTarget;
			Content.UpdateLayout();
		});

		flyout = commandBarFlyout;
		flyoutTarget = commandBarFlyoutTarget;
	}

	private async Task OpenFlyout(CommandBarFlyout flyout, Button flyoutTarget)
	{
		Log.Comment("Opening flyout...");
		var openedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			flyout.Opened += (sender, args) => openedEvent.Set();
			flyout.ShowAt(flyoutTarget, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Transient });
		});

		await TestUtilities.WaitForEvent(openedEvent);
		await TestServices.WindowHelper.WaitForIdle();
		Log.Comment("Flyout opened.");
	}

	private async Task CloseFlyout(CommandBarFlyout flyout)
	{
		Log.Comment("Closing flyout...");
		var closedEvent = new UnoAutoResetEvent(false);

		RunOnUIThread.Execute(() =>
		{
			flyout.Closed += (sender, args) => closedEvent.Set();
			flyout.Hide();
		});

		await TestUtilities.WaitForEvent(closedEvent);
		await TestServices.WindowHelper.WaitForIdle();
		Log.Comment("Flyout closed.");
	}
}
