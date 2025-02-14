// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace MUXControlsTestApp
{
	public sealed partial class MenuBarPage : TestPage
	{
		public MenuBarPage()
		{
			this.InitializeComponent();

			FileItem.AccessKey = "A";
			NewItem.AccessKey = "B";

			var cutCommand = new StandardUICommand(StandardUICommandKind.Cut);
			cutCommand.ExecuteRequested += CutCommand_ExecuteRequested;
			CutItem.Command = cutCommand;

			var accelerator = new KeyboardAccelerator();
			accelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			accelerator.Key = Windows.System.VirtualKey.Z;
			UndoItem.KeyboardAccelerators.Add(accelerator);

			UndoItem.Click += UndoItem_Click;
		}

		private void UndoItem_Click(object sender, RoutedEventArgs e)
		{
			TextOutput.Text = "Undo Clicked";
		}

		private void CutCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			TextOutput.Text = "Cut Clicked";
		}

		private void AddMenuBarItem_Click(object sender, RoutedEventArgs e)
		{
			var item = new Microsoft.UI.Xaml.Controls.MenuBarItem();
			item.Title = "New Menu Bar Item";
			item.Name = "NewMenuBarItem"; // Uno specific: easier for tests to find by Name
			menuBar.Items.Add(item);
		}

		private void RemoveMenuBarItem_Click(object sender, RoutedEventArgs e)
		{
			var size = menuBar.Items.Count;
			menuBar.Items.RemoveAt(size - 1);
		}

		private void AddFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			var item = new MenuFlyoutItem();
			item.Text = "New Flyout Item";
			item.Name = "NewFlyoutItem"; // Uno specific: easier for tests to find by Name
			FileItem.Items.Add(item);
		}

		private void RemoveFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			var size = FileItem.Items.Count;
			FileItem.Items.RemoveAt(size - 1);
		}

		private void NewItem_Click(object sender, RoutedEventArgs e)
		{
			TextOutput.Text = "New Clicked";
		}

		private void TestFrameCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			UpdateTestFrameVisibility(Visibility.Visible);
		}

		private void TestFrameCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			UpdateTestFrameVisibility(Visibility.Collapsed);
		}

		private void AddMenuBarToEmptyMenuBarItem_Click(object sender, RoutedEventArgs e)
		{
			int eCount = EmptyMenuBar.Items.Count;
			Microsoft.UI.Xaml.Controls.MenuBarItem mainMenuBarHelp = new Microsoft.UI.Xaml.Controls.MenuBarItem();
			mainMenuBarHelp.Title = "Help" + eCount;
			mainMenuBarHelp.SetValue(AutomationProperties.NameProperty, "Help" + eCount);
			mainMenuBarHelp.SetValue(NameProperty, "Help" + eCount); // Uno specific: easier for tests to find by Name
			MenuFlyoutItem newFlyout = new MenuFlyoutItem()
			{
				Text = "Add" + eCount
			};
			// UIA Name for interaction test
			newFlyout.SetValue(AutomationProperties.NameProperty, "Add" + eCount);
			newFlyout.SetValue(NameProperty, "Add" + eCount); // Uno specific: easier for tests to find by Name
			mainMenuBarHelp.Items.Add(newFlyout);

			mainMenuBarHelp.Items.Add(new MenuFlyoutItem()
			{
				Text = "Remove" + eCount
			});
			EmptyMenuBar.Items.Add(mainMenuBarHelp);
		}

		private void RemoveItemsFromOneChildrenItem_Click(object sender, RoutedEventArgs e)
		{
			OneChildrenFlyoutMenuBarItem.Items.Clear();
		}

		private void UpdateTestFrameVisibility(Visibility? visibility = null)
		{
			if (visibility.HasValue && XamlRoot != null)
			{
				var testFrame = XamlRoot.Content as Frame;
				testFrame.Visibility = visibility.Value;
			}
		}
	}
}
