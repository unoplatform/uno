// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference InteractionTests/SplitButtonTests.cpp, tag winui3/release/1.4.2

#if !WINDOWS_UWP

using System;
using MUXControlsTestApp.Utilities;

using Windows.UI.Xaml.Controls;
using Common;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using MUXControlsTestApp;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;
using SplitButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.SplitButtonAutomationPeer;
using ToggleSplitButton = Microsoft.UI.Xaml.Controls.ToggleSplitButton;

namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
{
	[TestClass]
	public partial class SplitButtonTests
	{
		[TestMethod]
		public async void BasicInteractionTest()
		{
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("TestSplitButton");

			TextBlock clickCountTextBlock = FindElementByName<TextBlock>("ClickCountTextBlock");
			TextBlock flyoutOpenedCountTextBlock = FindElementByName<TextBlock>("FlyoutOpenedCountTextBlock");
			TextBlock flyoutClosedCountTextBlock = FindElementByName<TextBlock>("FlyoutClosedCountTextBlock");

			Verify.AreEqual("0", clickCountTextBlock.Text);
			// ClickPrimaryButton(splitButton);
			splitButton.OnClickPrimaryInternal();
			Verify.AreEqual("1", clickCountTextBlock.Text);
			VerifyElementNotFound("TestFlyout");

			Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);
			// ClickSecondaryButton(splitButton);
			splitButton.OnClickSecondaryInternal();
			Verify.AreEqual("1", flyoutOpenedCountTextBlock.Text);
			VerifyElementFound("TestFlyout");

			Verify.AreEqual("0", flyoutClosedCountTextBlock.Text);
			Log.Comment("Close flyout by clicking over the button");
			// splitButton.Click();
			splitButton.Invoke();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", flyoutClosedCountTextBlock.Text);
		}

		[TestMethod]
		public async void CommandTest()
		{
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("CommandSplitButton");

			CheckBox canExecuteCheckBox = FindElementByName<CheckBox>("CanExecuteCheckBox");
			TextBlock executeCountTextBlock = FindElementByName<TextBlock>("ExecuteCountTextBlock");

			Log.Comment("Verify that the control starts out enabled");
			// Verify.AreEqual(ToggleState.On, canExecuteCheckBox.ToggleState);
			Verify.AreEqual(ToggleState.On, canExecuteCheckBox.IsChecked);
			Verify.AreEqual(true, splitButton.IsEnabled);
			Verify.AreEqual("0", executeCountTextBlock.Text);

			Log.Comment("Click primary button to execute command");
			// ClickPrimaryButton(splitButton);
			splitButton.OnClickPrimaryInternal();
			Verify.AreEqual("1", executeCountTextBlock.Text);

			Log.Comment("Click primary button with SPACE key to execute command");
			// ClickPrimaryButtonWithKey(splitButton, "SPACE");
			KeyboardHelper.Space(splitButton);
			Verify.AreEqual("2", executeCountTextBlock.Text);

			Log.Comment("Click primary button with ENTER key to execute command");
			// ClickPrimaryButtonWithKey(splitButton, "ENTER");
			KeyboardHelper.Enter(splitButton);
			Verify.AreEqual("3", executeCountTextBlock.Text);

			Log.Comment("Use invoke pattern to execute command");
			// splitButton.InvokeAndWait();
			splitButton.Invoke();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("4", executeCountTextBlock.Text);

			// Uno Specific: programmatic clicking won't check for IsEnabled.
			// Log.Comment("Verify that setting CanExecute to false disables the primary button");
			// // canExecuteCheckBox.Uncheck();
			// canExecuteCheckBox.IsChecked = false;
			// await WindowHelper.WaitForIdle();
			// ClickPrimaryButton(splitButton);
			// Verify.AreEqual("4", executeCountTextBlock.Text);
		}

		[TestMethod]
		public async void TouchTest()
		{
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("TestSplitButton");

			CheckBox simulateTouchCheckBox = FindElementByName<CheckBox>("SimulateTouchCheckBox");

			TextBlock clickCountTextBlock = FindElementByName<TextBlock>("ClickCountTextBlock");
			TextBlock flyoutOpenedCountTextBlock = FindElementByName<TextBlock>("FlyoutOpenedCountTextBlock");

			Log.Comment("Check simulate touch mode checkbox");
			// simulateTouchCheckBox.Click(); // This conveniently moves the mouse over the checkbox so that it isn't over the split button yet
			simulateTouchCheckBox.ProgrammaticClick();
			await WindowHelper.WaitForIdle();

			Verify.AreEqual("0", clickCountTextBlock.Text);
			Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);

			Log.Comment("Click primary button to open flyout in touch mode");
			// ClickPrimaryButton(splitButton);
			splitButton.OnClickPrimaryInternal();

			Verify.AreEqual("0", clickCountTextBlock.Text);
			Verify.AreEqual("1", flyoutOpenedCountTextBlock.Text);

			Log.Comment("Close flyout by clicking over the button");
			// splitButton.Click();
			splitButton.Invoke();
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async void AccessibilityTest()
		{
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("TestSplitButton");

			TextBlock clickCountTextBlock = FindElementByName<TextBlock>("ClickCountTextBlock");
			TextBlock flyoutOpenedCountTextBlock = FindElementByName<TextBlock>("FlyoutOpenedCountTextBlock");
			TextBlock flyoutClosedCountTextBlock = FindElementByName<TextBlock>("FlyoutClosedCountTextBlock");

			Log.Comment("Verify that SplitButton has no accessible children");
			Verify.AreEqual(0, splitButton.GetChildren().Count);

			Verify.AreEqual("0", clickCountTextBlock.Text);
			Log.Comment("Verify that invoking the SplitButton causes a click");
			// splitButton.InvokeAndWait();
			splitButton.Invoke();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", clickCountTextBlock.Text);

			Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);
			Log.Comment("Verify that expanding the SplitButton opens the flyout");
			// splitButton.ExpandAndWait();
			((SplitButtonAutomationPeer)splitButton.GetAutomationPeer()).Expand();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", flyoutOpenedCountTextBlock.Text);
			// Verify.AreEqual(ExpandCollapseState.Expanded, splitButton.ExpandCollapseState);
			Verify.AreEqual(ExpandCollapseState.Expanded, ((SplitButtonAutomationPeer)splitButton.GetAutomationPeer()).ExpandCollapseState);

			Verify.AreEqual("0", flyoutClosedCountTextBlock.Text);
			Log.Comment("Verify that collapsing the SplitButton closes the flyout");
			// splitButton.CollapseAndWait();
			((SplitButtonAutomationPeer)splitButton.GetAutomationPeer()).Collapse();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", flyoutClosedCountTextBlock.Text);
			// Verify.AreEqual(ExpandCollapseState.Collapsed, splitButton.ExpandCollapseState);
			Verify.AreEqual(ExpandCollapseState.Collapsed, ((SplitButtonAutomationPeer)splitButton.GetAutomationPeer()).ExpandCollapseState);
		}

		[TestMethod]
        public async void KeyboardTest()
        {
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

            SplitButton splitButton = FindElementByName<SplitButton>("TestSplitButton");

            TextBlock clickCountTextBlock = FindElementByName<TextBlock>("ClickCountTextBlock");
            TextBlock flyoutOpenedCountTextBlock = FindElementByName<TextBlock>("FlyoutOpenedCountTextBlock");
            TextBlock flyoutClosedCountTextBlock = FindElementByName<TextBlock>("FlyoutClosedCountTextBlock");

            Verify.AreEqual("0", clickCountTextBlock.Text);
            Log.Comment("Verify that pressing Space on SplitButton causes a click");
            // splitButton.SetFocus();
			splitButton.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
            KeyboardHelper.Space();
			await WindowHelper.WaitForIdle();
            Verify.AreEqual("1", clickCountTextBlock.Text);

            Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);
            Log.Comment("Verify that pressing alt-down on SplitButton opens the flyout");
            // KeyboardHelper.PressDownModifierKey(ModifierKey.Alt);
            // KeyboardHelper.PressKey(Key.Down);
            // KeyboardHelper.ReleaseModifierKey(ModifierKey.Alt);
			splitButton.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(splitButton, VirtualKey.Down, VirtualKeyModifiers.Menu));
			await WindowHelper.WaitForIdle();
            Verify.AreEqual("1", flyoutOpenedCountTextBlock.Text);

            Verify.AreEqual("0", flyoutClosedCountTextBlock.Text);
            Log.Comment("Verify that pressing escape closes the flyout");
            KeyboardHelper.Escape();
			await WindowHelper.WaitForIdle();
            Verify.AreEqual("1", flyoutClosedCountTextBlock.Text);

            Log.Comment("Verify that F4 opens the flyout");
            // splitButton.SetFocus();
			splitButton.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
            // TextInput.SendText("{F4}");
			KeyboardHelper.PressKeySequence("$d$_f4#$u$_f4");
			await WindowHelper.WaitForIdle();
            Verify.AreEqual("2", flyoutOpenedCountTextBlock.Text);
        }

		private async Task InputHelperLeftClick(FrameworkElement result, Mouse mouse)
		{
			var position = result.GetAbsoluteBounds().GetCenter();
			mouse.Press(position);
			mouse.Release();
			await WindowHelper.WaitForIdle();
		}

		private async Task InputHelperMoveMouse(FrameworkElement result, int offsetX, int offsetY, Mouse mouse)
		{
			var position = result.GetAbsoluteBounds().GetLocation();
			mouse.MoveTo(position + new Point(offsetX, offsetY));
			await WindowHelper.WaitForIdle();
		}

		private void VerifyElementFound(string name) => Verify.IsNotNull(FindElementById<UIElement>(name));

		private void VerifyElementNotFound(string name) => Verify.IsNull(FindElementById<UIElement>(name));

		public static async Task VerifyAreEqualWithRetry(int maxRetries, Func<object> expectedFunc, Func<object> actualFunc, Func<Task> retryAction = null)
		{
			if (retryAction == null)
			{
				retryAction = async () =>
				{
					await Task.Delay(50);
				};
			}

			for (int retry = 0; retry <= maxRetries; retry++)
			{
				object expected = expectedFunc();
				object actual = actualFunc();
				if (Equals(expected, actual) || retry == maxRetries)
				{
					Log.Comment("Actual retry times: " + retry);
					Verify.AreEqual(expected, actual);
					return;
				}
				else
				{
					await retryAction();
				}
			}
		}

		private static T FindElementById<T>(string name) where T : UIElement => FindElementById<T>(WindowHelper.XamlRoot.VisualTree.RootElement, name);

		private static T FindElementById<T>(DependencyObject parent, string name) where T : UIElement
		{
			var count = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is FrameworkElement fe && fe.Name == name)
				{
					return fe as T;
				}
				else
				{
					var result = FindElementById<T>(child, name);
					if (result != null)
					{
						return result;
					}
				}
			}
			return null;
		}

		private static T FindElementByName<T>(string name) where T : UIElement => FindElementByName<T>(WindowHelper.XamlRoot.VisualTree.RootElement, name);

		private static T FindElementByName<T>(DependencyObject parent, string name) where T : UIElement
		{
			var count = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T t && AutomationProperties.GetName(child).Equals(name))
				{
					return t;
				}
				else
				{
					var result = FindElementByName<T>(child, name);
					if (result != null)
					{
						return result;
					}
				}
			}
			return null;
		}
	}
}
#endif
