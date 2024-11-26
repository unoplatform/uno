// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\TestUI\CommandBarFlyoutPage.xaml.cs, tag winui3/release/1.6.3, commit 66d24dfff3b2763ab3be096a2c7cbaafc81b31eb

using Common;
using System;
using System.Linq;
using Windows.Foundation.Metadata;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using MUXControlsTestApp.Utilities;

using CommandBarFlyout = Microsoft.UI.Xaml.Controls.CommandBarFlyout;
using CommandBarFlyoutCommandBar = Microsoft.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBar;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

[Sample("CommandBarFlyout", "WinUI")]
public sealed partial class CommandBarFlyoutPage : TestPage
{
	private Style animatedCommandBarFlyoutCommandBarStyle;

	private DispatcherTimer clearSecondaryCommandsTimer = new DispatcherTimer();
	private CommandBarFlyout clearSecondaryCommandsFlyout;

	private DispatcherTimer clearPrimaryCommandsTimer = new DispatcherTimer();
	private CommandBarFlyout clearPrimaryCommandsFlyout;

	private DispatcherTimer addFlyoutTimer = new DispatcherTimer();
	private MenuFlyout flyoutToAdd = new MenuFlyout();

	private DispatcherTimer dynamicLabelTimer = new DispatcherTimer();
	private DispatcherTimer dynamicVisibilityTimer = new DispatcherTimer();
	private DispatcherTimer dynamicWidthTimer = new DispatcherTimer();
	private DispatcherTimer dynamicCommandTimer = new DispatcherTimer();
	private AppBarButton dynamicLabelSecondaryCommand;
	private AppBarButton dynamicVisibilitySecondaryCommand;
	private FrameworkElement dynamicWidthOverflowContentRoot;
	private CommandBarFlyout dynamicCommandBarFlyout;
	private string originalLabelSecondaryCommand;
	private Visibility originalVisibilitySecondaryCommand;
	private double originalWidthOverflowContentRoot;
	private int dynamicLabelChangeCount;
	private int dynamicVisibilityChangeCount;
	private int dynamicWidthChangeCount;

	public CommandBarFlyoutPage()
	{
		this.InitializeComponent();

		animatedCommandBarFlyoutCommandBarStyle = this.Resources["animatedCommandBarFlyoutCommandBarStyle"] as Style;

		dynamicLabelTimer.Tick += DynamicLabelTimer_Tick;
		dynamicVisibilityTimer.Tick += DynamicVisibilityTimer_Tick;
		dynamicWidthTimer.Tick += DynamicWidthTimer_Tick;
		dynamicCommandTimer.Tick += DynamicCommandTimer_Tick;

		clearSecondaryCommandsTimer.Interval = new TimeSpan(0, 0, 3 /*sec*/);
		clearSecondaryCommandsTimer.Tick += ClearSecondaryCommandsTimer_Tick;

		clearSecondaryCommandsTimer.Interval = new TimeSpan(0, 0, 3 /*sec*/);
		clearPrimaryCommandsTimer.Tick += ClearPrimaryCommandsTimer_Tick;

		addFlyoutTimer.Interval = new TimeSpan(0, 0, 1 /*sec*/);
		addFlyoutTimer.Tick += AddFlyoutTimer_Tick;

		flyoutToAdd.Items.Add(new MenuFlyoutItem() { Text = "This" });
		flyoutToAdd.Items.Add(new MenuFlyoutItem() { Text = "That" });

		UndoButton1.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton2.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton3.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton4.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton5.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton6.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton7.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton11.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton12.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton13.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton14.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });
		UndoButton15.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Z, Modifiers = VirtualKeyModifiers.Control });

		RedoButton1.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton2.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton3.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton4.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton5.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton6.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton7.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton11.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton12.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton13.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton14.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton15.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });
		RedoButton16.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.Y, Modifiers = VirtualKeyModifiers.Control });

		SelectAllButton1.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton2.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton3.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton4.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton5.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton6.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton7.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton11.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton12.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton13.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton14.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton15.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });
		SelectAllButton16.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.A, Modifiers = VirtualKeyModifiers.Control });

		FlyoutTarget1.ContextFlyout = Flyout1;
		FlyoutTarget2.ContextFlyout = Flyout2;
		FlyoutTarget3.ContextFlyout = Flyout3;
		FlyoutTarget4.ContextFlyout = Flyout4;
		FlyoutTarget5.ContextFlyout = Flyout5;
		FlyoutTarget6.ContextFlyout = Flyout6;
		FlyoutTarget7.ContextFlyout = Flyout7;
		FlyoutTarget11.ContextFlyout = Flyout11;
		FlyoutTarget12.ContextFlyout = Flyout12;
		FlyoutTarget13.ContextFlyout = Flyout13;
		FlyoutTarget14.ContextFlyout = Flyout14;
		FlyoutTarget15.ContextFlyout = Flyout15;
		FlyoutTarget16.ContextFlyout = Flyout16;

		Flyout1.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout2.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout3.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout4.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout5.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout6.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout7.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout11.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout12.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout13.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout14.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout15.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		Flyout16.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;

		Flyout16.Opening += (object sender, object args) =>
		{
			addFlyoutTimer.Start();
		};

		Flyout16.Closed += (object sender, object args) =>
		{
			UndoButton16.Visibility = Visibility.Visible;
			UndoButtonOverflow16.Visibility = Visibility.Collapsed;
			UndoButtonOverflow16.Flyout = null;

			StatusReportingTextBox.Text = string.Empty;
			ExtraInformationTextBox.Text = string.Empty;
		};
	}

	public void OnElementClicked(object sender, object args)
	{
		RecordEvent(sender, "clicked");
	}

	public void OnElementChecked(object sender, object args)
	{
		RecordEvent(sender, "checked");
	}

	public void OnElementUnchecked(object sender, object args)
	{
		RecordEvent(sender, "unchecked");
	}

	public void OnFlyoutOpened(object sender, object args)
	{
		IsFlyoutOpenCheckBox.IsChecked = true;

		CommandBarFlyout commandBarFlyout = sender as CommandBarFlyout;

		if (commandBarFlyout != null)
		{
			if ((bool)UseOverflowContentRootDynamicWidthCheckBox.IsChecked && commandBarFlyout.SecondaryCommands != null && commandBarFlyout.SecondaryCommands.Count > 0)
			{
				FrameworkElement secondaryCommandAsFE = commandBarFlyout.SecondaryCommands[0] as FrameworkElement;
				FrameworkElement overflowContentRoot = secondaryCommandAsFE.FindVisualParentByName("OverflowContentRoot");

				if (overflowContentRoot == null)
				{
					secondaryCommandAsFE.Loaded += SecondaryCommandAsFE_Loaded;
				}
				else
				{
					SetDynamicOverflowContentRoot(overflowContentRoot);
				}
			}

			if (animatedCommandBarFlyoutCommandBarStyle != null && (bool)UseAnimatedCommandBarFlyoutCommandBarStyleCheckBox.IsChecked && commandBarFlyout.PrimaryCommands != null && commandBarFlyout.PrimaryCommands.Count > 0)
			{
				FrameworkElement primaryCommandAsFE = commandBarFlyout.PrimaryCommands[0] as FrameworkElement;

				if (primaryCommandAsFE != null)
				{
					var commandBarFlyoutCommandBar = primaryCommandAsFE.FindVisualParentByType<CommandBarFlyoutCommandBar>();

					if (commandBarFlyoutCommandBar != null)
					{
						commandBarFlyoutCommandBar.Style = animatedCommandBarFlyoutCommandBarStyle;
					}
				}
			}
		}
	}

	private void SecondaryCommandAsFE_Loaded(object sender, RoutedEventArgs e)
	{
		FrameworkElement secondaryCommandAsFE = sender as FrameworkElement;

		secondaryCommandAsFE.Loaded -= SecondaryCommandAsFE_Loaded;

		FrameworkElement overflowContentRoot = secondaryCommandAsFE.FindVisualParentByName("OverflowContentRoot");

		SetDynamicOverflowContentRoot(overflowContentRoot);
	}

	public void OnFlyoutClosed(object sender, object args)
	{
		IsFlyoutOpenCheckBox.IsChecked = false;
		SetDynamicLabelSecondaryCommand(null);
		SetDynamicVisibilitySecondaryCommand(null);
		SetDynamicOverflowContentRoot(null);
		SetClearSecondaryCommandsFlyout(null);

		if ((bool)HideFlyoutOnFlyoutClosedCheckBox.IsChecked)
		{
			var commandBarFlyout = sender as CommandBarFlyout;

			if (commandBarFlyout != null)
			{
				commandBarFlyout.Hide();
			}
		}
	}

	private void RecordEvent(string eventString)
	{
		StatusReportingTextBox.Text = eventString;
	}

	private void RecordEvent(object sender, string eventString)
	{
		DependencyObject senderAsDO = sender as DependencyObject;

		if (senderAsDO != null)
		{
			RecordEvent(AutomationProperties.GetAutomationId(senderAsDO) + " " + eventString);
		}
	}

	private void OnFlyoutTarget1Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout1, FlyoutTarget1);
	}

	private void OnFlyoutTarget2Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout2, FlyoutTarget2);
	}

	private void OnFlyoutTarget3Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout3, FlyoutTarget3);
	}

	private void OnFlyoutTarget4Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout4, FlyoutTarget4);
	}

	private void OnFlyoutTarget5Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout5, FlyoutTarget5);
	}

	private void OnFlyoutTarget6Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout6, FlyoutTarget6, FlyoutShowMode.Standard);
	}

	private void OnFlyoutTarget7Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout7, FlyoutTarget7);
	}

	private void OnFlyoutTarget8Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout8, FlyoutTarget8);
	}

	private void OnFlyoutTarget9Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout9, FlyoutTarget9);
	}

	private void OnFlyoutTarget10Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout10, FlyoutTarget10);
	}

	private void OnFlyoutTarget11Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout11, FlyoutTarget11, FlyoutShowMode.Standard);
	}

	private void OnFlyoutTarget12Click(object sender, RoutedEventArgs e)
	{
		DispatcherTimer showFlyoutTimer = new();
		showFlyoutTimer.Interval = TimeSpan.FromSeconds(5);
		showFlyoutTimer.Tick += (object sender, object args) =>
		{
			ShowFlyoutAt(Flyout12, FlyoutTarget12, FlyoutShowMode.Standard);
			showFlyoutTimer.Stop();
		};
		showFlyoutTimer.Start();
	}

	private void OnFlyoutTarget13Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout13, FlyoutTarget13);
	}

	private void OnFlyoutTarget14Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout14, FlyoutTarget14);
	}

	private void OnFlyoutTarget15Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout15, FlyoutTarget15);
	}

	private void OnFlyoutTarget16Click(object sender, RoutedEventArgs e)
	{
		ShowFlyoutAt(Flyout16, FlyoutTarget16);
	}

	private void ShowFlyoutAt(FlyoutBase flyout, FrameworkElement targetElement, FlyoutShowMode showMode = FlyoutShowMode.Transient)
	{
		bool useSecondaryCommandDynamicLabel = (bool)UseSecondaryCommandDynamicLabelCheckBox.IsChecked;
		bool useSecondaryCommandDynamicVisibility = (bool)UseSecondaryCommandDynamicVisibilityCheckBox.IsChecked;
		bool clearSecondaryCommands = (bool)ClearSecondaryCommandsCheckBox.IsChecked;
		bool addPrimaryCommandDynamicallyCheckBox = (bool)AddPrimaryCommandDynamicallyCheckBox.IsChecked;
		bool clearPrimaryCommands = (bool)ClearPrimaryCommandsCheckBox.IsChecked;

		if (useSecondaryCommandDynamicLabel || useSecondaryCommandDynamicVisibility || addPrimaryCommandDynamicallyCheckBox || clearSecondaryCommands || clearPrimaryCommands)
		{
			CommandBarFlyout commandBarFlyout = flyout as CommandBarFlyout;

			if (commandBarFlyout != null)
			{
				if (commandBarFlyout.SecondaryCommands != null && commandBarFlyout.SecondaryCommands.Count > 0)
				{
					if (useSecondaryCommandDynamicLabel)
					{
						SetDynamicLabelSecondaryCommand(commandBarFlyout.SecondaryCommands[0] as AppBarButton);
					}

					if (useSecondaryCommandDynamicVisibility && commandBarFlyout.SecondaryCommands.Count > 4)
					{
						SetDynamicVisibilitySecondaryCommand(commandBarFlyout.SecondaryCommands[4] as AppBarButton);
					}

					if (clearSecondaryCommands)
					{
						SetClearSecondaryCommandsFlyout(commandBarFlyout);
					}
				}

				if (addPrimaryCommandDynamicallyCheckBox)
				{
					dynamicCommandBarFlyout = commandBarFlyout;
					SetDynamicPrimaryCommand();
				}

				if (clearPrimaryCommands && commandBarFlyout.PrimaryCommands != null && commandBarFlyout.PrimaryCommands.Count > 0)
				{
					SetClearPrimaryCommandsFlyout(commandBarFlyout);
				}
			}
		}

		flyout.ShowAt(targetElement, new FlyoutShowOptions { Placement = FlyoutPlacementMode.TopEdgeAlignedLeft, ShowMode = showMode });
	}

	private void SetClearSecondaryCommandsFlyout(CommandBarFlyout commandBarFlyout)
	{
		if (commandBarFlyout == null)
		{
			clearSecondaryCommandsFlyout = null;
			clearSecondaryCommandsTimer.Stop();
		}
		else
		{
			clearSecondaryCommandsFlyout = commandBarFlyout;
			clearSecondaryCommandsTimer.Start();
		}
	}

	private void SetClearPrimaryCommandsFlyout(CommandBarFlyout commandBarFlyout)
	{
		if (commandBarFlyout == null)
		{
			clearPrimaryCommandsFlyout = null;
			clearPrimaryCommandsTimer.Stop();
		}
		else
		{
			clearPrimaryCommandsFlyout = commandBarFlyout;
			clearPrimaryCommandsTimer.Start();
		}
	}

	private void SetDynamicLabelSecondaryCommand(AppBarButton appBarButton)
	{
		if (appBarButton == null)
		{
			if (dynamicLabelSecondaryCommand != null)
			{
				if (dynamicLabelSecondaryCommand.Label != originalLabelSecondaryCommand)
				{
					dynamicLabelSecondaryCommand.Label = originalLabelSecondaryCommand;
					SecondaryCommandDynamicLabelChangedCheckBox.IsChecked = true;
				}
				dynamicLabelSecondaryCommand = null;
				dynamicLabelTimer.Stop();
			}
		}
		else
		{
			if (dynamicLabelSecondaryCommand == null)
			{
				originalLabelSecondaryCommand = appBarButton.Label;
				dynamicLabelSecondaryCommand = appBarButton;
				dynamicLabelTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(DynamicLabelTimerIntervalTextBox.Text) /*msec*/);
				dynamicLabelChangeCount = int.Parse(DynamicLabelChangeCountTextBox.Text);
				dynamicLabelTimer.Start();
			}
		}
	}

	private void SetDynamicVisibilitySecondaryCommand(AppBarButton appBarButton)
	{
		if (appBarButton == null)
		{
			if (dynamicVisibilitySecondaryCommand != null)
			{
				if (dynamicVisibilitySecondaryCommand.Visibility != originalVisibilitySecondaryCommand)
				{
					dynamicVisibilitySecondaryCommand.Visibility = originalVisibilitySecondaryCommand;
					SecondaryCommandDynamicVisibilityChangedCheckBox.IsChecked = true;
				}
				dynamicVisibilitySecondaryCommand = null;
				dynamicVisibilityTimer.Stop();
			}
		}
		else
		{
			if (dynamicVisibilitySecondaryCommand == null)
			{
				originalVisibilitySecondaryCommand = appBarButton.Visibility;
				dynamicVisibilitySecondaryCommand = appBarButton;
				dynamicVisibilityTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(DynamicVisibilityTimerIntervalTextBox.Text) /*msec*/);
				dynamicVisibilityChangeCount = int.Parse(DynamicVisibilityChangeCountTextBox.Text);
				dynamicVisibilityTimer.Start();
			}
		}
	}

	private void SetDynamicOverflowContentRoot(FrameworkElement overflowContentRoot)
	{
		if (overflowContentRoot == null)
		{
			if (dynamicWidthOverflowContentRoot != null)
			{
				if (dynamicWidthOverflowContentRoot.ActualWidth != originalWidthOverflowContentRoot)
				{
					dynamicWidthOverflowContentRoot.Width = originalWidthOverflowContentRoot;
					OverflowContentRootDynamicWidthChangedCheckBox.IsChecked = true;
				}
				dynamicWidthOverflowContentRoot = null;
				dynamicWidthTimer.Stop();
			}
		}
		else if (dynamicWidthOverflowContentRoot == null)
		{
			if (overflowContentRoot.ActualWidth == 0.0)
			{
				overflowContentRoot.SizeChanged += OverflowContentRoot_SizeChanged;
			}
			else
			{
				originalWidthOverflowContentRoot = overflowContentRoot.ActualWidth;
				dynamicWidthOverflowContentRoot = overflowContentRoot;
				dynamicWidthTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(DynamicWidthTimerIntervalTextBox.Text) /*msec*/);
				dynamicWidthChangeCount = int.Parse(DynamicWidthChangeCountTextBox.Text);
				dynamicWidthTimer.Start();
			}
		}
	}

	private void OverflowContentRoot_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		FrameworkElement overflowContentRoot = sender as FrameworkElement;

		overflowContentRoot.SizeChanged -= OverflowContentRoot_SizeChanged;

		SetDynamicOverflowContentRoot(overflowContentRoot);
	}

	private void SetDynamicPrimaryCommand()
	{
		dynamicCommandTimer.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(DynamicLabelTimerIntervalTextBox.Text) /*msec*/);
		dynamicLabelChangeCount = int.Parse(DynamicLabelChangeCountTextBox.Text);
		dynamicCommandTimer.Start();
	}

	private void ClearSecondaryCommandsTimer_Tick(object sender, object e)
	{
		if (clearSecondaryCommandsFlyout != null)
		{
			clearSecondaryCommandsFlyout.SecondaryCommands.Clear();
		}
	}

	private void ClearPrimaryCommandsTimer_Tick(object sender, object e)
	{
		if (clearPrimaryCommandsFlyout != null)
		{
			clearPrimaryCommandsFlyout.PrimaryCommands.Clear();
		}
	}

	private void AddFlyoutTimer_Tick(object sender, object e)
	{
		UndoButtonOverflow16.SizeChanged += UndoButtonOverflow16_SizeChanged;

		UndoButton16.Visibility = Visibility.Collapsed;
		UndoButtonOverflow16.Visibility = Visibility.Visible;
		UndoButtonOverflow16.Flyout = flyoutToAdd;
		addFlyoutTimer.Stop();
	}

	private void DynamicLabelTimer_Tick(object sender, object e)
	{
		if (dynamicLabelSecondaryCommand != null)
		{
			if (dynamicLabelSecondaryCommand.Label == originalLabelSecondaryCommand)
			{
				// Testing dynamic label expansion
				dynamicLabelSecondaryCommand.Label += " with an expanded label";
			}
			else
			{
				// Testing dynamic label shrinkage
				dynamicLabelSecondaryCommand.Label = originalLabelSecondaryCommand;
			}

			SecondaryCommandDynamicLabelChangedCheckBox.IsChecked = true;

			if (--dynamicLabelChangeCount == 0)
			{
				dynamicLabelTimer.Stop();
			}
		}
	}

	private void DynamicVisibilityTimer_Tick(object sender, object e)
	{
		if (dynamicVisibilitySecondaryCommand != null)
		{
			dynamicVisibilitySecondaryCommand.Visibility = dynamicVisibilitySecondaryCommand.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

			SecondaryCommandDynamicVisibilityChangedCheckBox.IsChecked = true;

			if (--dynamicVisibilityChangeCount == 0)
			{
				dynamicVisibilityTimer.Stop();
			}
		}
	}

	private void DynamicWidthTimer_Tick(object sender, object e)
	{
		if (dynamicWidthOverflowContentRoot != null)
		{
			if (dynamicWidthOverflowContentRoot.ActualWidth == originalWidthOverflowContentRoot)
			{
				// Testing dynamic size expansion
				dynamicWidthOverflowContentRoot.Width = dynamicWidthOverflowContentRoot.ActualWidth + 24.0;
			}
			else
			{
				// Testing dynamic size shrinkage
				dynamicWidthOverflowContentRoot.Width = originalWidthOverflowContentRoot;
			}

			OverflowContentRootDynamicWidthChangedCheckBox.IsChecked = true;

			if (--dynamicWidthChangeCount == 0)
			{
				dynamicWidthTimer.Stop();
			}
		}
	}

	private void DynamicCommandTimer_Tick(object sender, object e)
	{
		dynamicCommandBarFlyout.PrimaryCommands.Add(new AppBarButton()
		{
			Content = new TextBlock() { Text = "Test" }
		});

		PrimaryCommandDynamicallyAddedCheckBox.IsChecked = true;

		if (--dynamicLabelChangeCount == 0)
		{
			dynamicCommandTimer.Stop();
		}
	}

	private void IsRTLCheckBox_Checked(object sender, RoutedEventArgs e)
	{
		FlowDirection = FlowDirection.RightToLeft;
	}

	private void IsRTLCheckBox_Unchecked(object sender, RoutedEventArgs e)
	{
		FlowDirection = FlowDirection.LeftToRight;
	}

	private void OnEditCommandCount6Click(object sender, RoutedEventArgs e)
	{
		var flyout6 = Flyout6 as CommandBarFlyout;

		if (flyout6.PrimaryCommands.Count() == 0)
		{
			flyout6.PrimaryCommands.Add(new AppBarButton()
			{
				Content = new TextBlock() { Text = "Test" }
			});
		}
		else
		{
			flyout6.PrimaryCommands.RemoveAt(0);
		}
	}

	private void UndoButtonOverflow16_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UndoButtonOverflow16.SizeChanged -= UndoButtonOverflow16_SizeChanged;

		// These visual elements don't have automation peers we can access, so we need to expose their bounds to the test the hard way.
		var undoButtonOverflow16ContentViewbox = UndoButtonOverflow16.FindVisualChildByName("ContentViewbox");
		var undoButtonOverflow16OverflowTextLabel = UndoButtonOverflow16.FindVisualChildByName("OverflowTextLabel");
		var redoButton16ContentViewbox = RedoButton16.FindVisualChildByName("ContentViewbox");

		var undoButtonOverflow16ContentViewboxBounds = undoButtonOverflow16ContentViewbox.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, undoButtonOverflow16ContentViewbox.ActualWidth, undoButtonOverflow16ContentViewbox.ActualHeight));
		var undoButtonOverflow16OverflowTextLabelBounds = undoButtonOverflow16OverflowTextLabel.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, undoButtonOverflow16ContentViewbox.ActualWidth, undoButtonOverflow16ContentViewbox.ActualHeight));
		var redoButton16ContentViewboxBounds = redoButton16ContentViewbox.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, undoButtonOverflow16ContentViewbox.ActualWidth, undoButtonOverflow16ContentViewbox.ActualHeight));

		ExtraInformationTextBox.Text = $"({undoButtonOverflow16ContentViewboxBounds.X}, {undoButtonOverflow16ContentViewboxBounds.Y}, {undoButtonOverflow16ContentViewboxBounds.Width}, {undoButtonOverflow16ContentViewboxBounds.Height}), ({undoButtonOverflow16OverflowTextLabelBounds.X}, {undoButtonOverflow16OverflowTextLabelBounds.Y}, {undoButtonOverflow16OverflowTextLabelBounds.Width}, {undoButtonOverflow16OverflowTextLabelBounds.Height}), ({redoButton16ContentViewboxBounds.X}, {redoButton16ContentViewboxBounds.Y}, {redoButton16ContentViewboxBounds.Width}, {redoButton16ContentViewboxBounds.Height})";
		RecordEvent("Undo with overflow visible");
	}
}
