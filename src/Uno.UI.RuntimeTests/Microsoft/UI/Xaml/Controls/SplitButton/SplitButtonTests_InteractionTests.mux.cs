// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference InteractionTests/SplitButtonTests.cpp, tag winui3/release/1.4.2

#if !WINDOWS_UWP

using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Common;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;
using ToggleSplitButton = Microsoft.UI.Xaml.Controls.ToggleSplitButton;
using Uno.UI.Toolkit.DevTools.Input;

namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
{
	[TestClass]
	[RunsOnUIThread]
	public partial class SplitButtonTests
	{
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task BasicInteractionTest()
		{
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("TestSplitButton");

			TextBlock clickCountTextBlock = FindElementByName<TextBlock>("ClickCountTextBlock");
			TextBlock flyoutOpenedCountTextBlock = FindElementByName<TextBlock>("FlyoutOpenedCountTextBlock");
			TextBlock flyoutClosedCountTextBlock = FindElementByName<TextBlock>("FlyoutClosedCountTextBlock");

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			Verify.AreEqual("0", clickCountTextBlock.Text);
			// ClickPrimaryButton(splitButton);
			await ClickPrimaryButton(splitButton, mouse);
			Verify.AreEqual("1", clickCountTextBlock.Text);
			VerifyElementNotFound("TestFlyout");

			Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);
			// ClickSecondaryButton(splitButton);
			await ClickSecondaryButton(splitButton, mouse);
			Verify.AreEqual("1", flyoutOpenedCountTextBlock.Text);
			// VerifyElementFound("TestFlyout"); // Uno Specific: the flyout is not a part of the visual tree
			Verify.IsTrue(splitButton.Flyout.IsOpen);

			Verify.AreEqual("0", flyoutClosedCountTextBlock.Text);
			Log.Comment("Close flyout by clicking over the button");
			// splitButton.Click();
			await ClickPrimaryButton(splitButton, mouse);
			Verify.AreEqual("1", flyoutClosedCountTextBlock.Text);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task CommandTest()
		{
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("CommandSplitButton");

			CheckBox canExecuteCheckBox = FindElementByName<CheckBox>("CanExecuteCheckBox");
			TextBlock executeCountTextBlock = FindElementByName<TextBlock>("ExecuteCountTextBlock");

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			Log.Comment("Verify that the control starts out enabled");
			// Verify.AreEqual(ToggleState.On, canExecuteCheckBox.ToggleState);
			Verify.AreEqual(ToggleState.On, ((IToggleProvider)canExecuteCheckBox.GetAutomationPeer()).ToggleState);
			Verify.AreEqual(true, splitButton.IsEnabled);
			Verify.AreEqual("0", executeCountTextBlock.Text);

			Log.Comment("Click primary button to execute command");
			// ClickPrimaryButton(splitButton);
			await ClickPrimaryButton(splitButton, mouse);
			Verify.AreEqual("1", executeCountTextBlock.Text);

			Log.Comment("Click primary button with SPACE key to execute command");
			// ClickPrimaryButtonWithKey(splitButton, "SPACE");
			await KeyboardHelper.Space(splitButton);
			Verify.AreEqual("2", executeCountTextBlock.Text);

			Log.Comment("Click primary button with ENTER key to execute command");
			// ClickPrimaryButtonWithKey(splitButton, "ENTER");
			await KeyboardHelper.Enter(splitButton);
			Verify.AreEqual("3", executeCountTextBlock.Text);

			Log.Comment("Use invoke pattern to execute command");
			// splitButton.InvokeAndWait();
			splitButton.Invoke();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("4", executeCountTextBlock.Text);

			Log.Comment("Verify that setting CanExecute to false disables the primary button");
			// canExecuteCheckBox.Uncheck();
			canExecuteCheckBox.IsChecked = false;
			await WindowHelper.WaitForIdle();
			await ClickPrimaryButton(splitButton, mouse);
			Verify.AreEqual("4", executeCountTextBlock.Text);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task TouchTest()
		{
			// Uno Specific: close popups after the test
			using var _ = Disposable.Create(() => VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot));

			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("TestSplitButton");

			CheckBox simulateTouchCheckBox = FindElementByName<CheckBox>("SimulateTouchCheckBox");

			TextBlock clickCountTextBlock = FindElementByName<TextBlock>("ClickCountTextBlock");
			TextBlock flyoutOpenedCountTextBlock = FindElementByName<TextBlock>("FlyoutOpenedCountTextBlock");

			// Uno-specific
			splitButton.StartBringIntoView();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			// Uno Doc: this is not needed, we use an injected touch pointer
			Log.Comment("Check simulate touch mode checkbox");
			// simulateTouchCheckBox.Click(); // This conveniently moves the mouse over the checkbox so that it isn't over the split button yet
			simulateTouchCheckBox.ProgrammaticClick();
			await WindowHelper.WaitForIdle();

			Verify.AreEqual("0", clickCountTextBlock.Text);
			Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);

			Log.Comment("Click primary button in touch mode");
			// ClickPrimaryButton(splitButton);
			await ClickPrimaryButton(splitButton, finger);
			await WindowHelper.WaitForIdle();

			Verify.AreEqual("1", clickCountTextBlock.Text);
			Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);

			Log.Comment("Click secondary button in touch mode");
			await ClickSecondaryButton(splitButton, finger);

			Verify.AreEqual("1", clickCountTextBlock.Text);
			Verify.AreEqual("1", flyoutOpenedCountTextBlock.Text);

			Log.Comment("Close flyout by clicking over the button");
			// splitButton.Click();
			await ClickPrimaryButton(splitButton, finger);
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task AccessibilityTest()
		{
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("TestSplitButton");

			TextBlock clickCountTextBlock = FindElementByName<TextBlock>("ClickCountTextBlock");
			TextBlock flyoutOpenedCountTextBlock = FindElementByName<TextBlock>("FlyoutOpenedCountTextBlock");
			TextBlock flyoutClosedCountTextBlock = FindElementByName<TextBlock>("FlyoutClosedCountTextBlock");

			Log.Comment("Verify that SplitButton has no accessible children");
			// Verify.AreEqual(0, splitButton.Children.Count); // Uno Specific: SplitButton doesn't have a Children property

			Verify.AreEqual("0", clickCountTextBlock.Text);
			Log.Comment("Verify that invoking the SplitButton causes a click");
			// splitButton.InvokeAndWait();
			splitButton.Invoke();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", clickCountTextBlock.Text);

			Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);
			Log.Comment("Verify that expanding the SplitButton opens the flyout");
			// splitButton.ExpandAndWait();
			((IExpandCollapseProvider)splitButton.GetAutomationPeer()).Expand();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", flyoutOpenedCountTextBlock.Text);
			// Verify.AreEqual(ExpandCollapseState.Expanded, splitButton.ExpandCollapseState);
			Verify.AreEqual(ExpandCollapseState.Expanded, ((IExpandCollapseProvider)splitButton.GetAutomationPeer()).ExpandCollapseState);

			Verify.AreEqual("0", flyoutClosedCountTextBlock.Text);
			Log.Comment("Verify that collapsing the SplitButton closes the flyout");
			// splitButton.CollapseAndWait();
			((IExpandCollapseProvider)splitButton.GetAutomationPeer()).Collapse();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", flyoutClosedCountTextBlock.Text);
			// Verify.AreEqual(ExpandCollapseState.Collapsed, splitButton.ExpandCollapseState);
			Verify.AreEqual(ExpandCollapseState.Collapsed, ((IExpandCollapseProvider)splitButton.GetAutomationPeer()).ExpandCollapseState);
		}

		[TestMethod]
#if !__SKIA__
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task KeyboardTest()
		{
			// Uno Specific: close popups after the test
			using var _ = Disposable.Create(() => VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot));

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
			await KeyboardHelper.Space();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", clickCountTextBlock.Text);

			Verify.AreEqual("0", flyoutOpenedCountTextBlock.Text);
			Log.Comment("Verify that pressing alt-down on SplitButton opens the flyout");
			await KeyboardHelper.PressKeySequence("$d$_alt#$d$_down#$u$_down#$u$_alt");
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", flyoutOpenedCountTextBlock.Text);

			Verify.AreEqual("0", flyoutClosedCountTextBlock.Text);
			Log.Comment("Verify that pressing escape closes the flyout");
			await KeyboardHelper.Escape();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("1", flyoutClosedCountTextBlock.Text);

			Log.Comment("Verify that F4 opens the flyout");
			// splitButton.SetFocus();
			splitButton.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();
			// TextInput.SendText("{F4}");
			await KeyboardHelper.PressKeySequence("$d$_f4#$u$_f4");
			await WindowHelper.WaitForIdle();
			Verify.AreEqual("2", flyoutOpenedCountTextBlock.Text);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task ToggleTest()
		{
			// Uno Specific: close popups after the test
			using var _ = Disposable.Create(() => VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot));

			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			SplitButton splitButton = FindElementByName<SplitButton>("ToggleSplitButton");

			TextBlock toggleStateTextBlock = FindElementByName<TextBlock>("ToggleStateTextBlock");
			TextBlock toggleStateOnClickTextBlock = FindElementByName<TextBlock>("ToggleStateOnClickTextBlock");

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			Verify.AreEqual("Unchecked", toggleStateTextBlock.Text);
			Verify.AreEqual("Unchecked", toggleStateOnClickTextBlock.Text);

			Log.Comment("Click primary button to check button");
			// using (var toggleStateWaiter = new PropertyChangedEventWaiter(splitButton, Scope.Element, UIProperty.Get("Toggle.ToggleState")))
			// {
			//     ClickPrimaryButton(splitButton);
			//     Verify.IsTrue(toggleStateWaiter.TryWait(TimeSpan.FromSeconds(1)), "Waiting for the Toggle.ToggleState event should succeed");
			// }
			var peer = ((IToggleProvider)splitButton.GetAutomationPeer());
			Assert.AreEqual(ToggleState.Off, peer.ToggleState);
			peer.Toggle();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(ToggleState.On, peer.ToggleState);

			Verify.AreEqual("Checked", toggleStateTextBlock.Text);
			// Verify.AreEqual("Checked", toggleStateOnClickTextBlock.Text); // Uno TODO: WinUI expects OnClick to trigger, but there's no reason for it to.

			Log.Comment("Click primary button to uncheck button");
			// ClickPrimaryButton(splitButton);
			await ClickPrimaryButton(splitButton, mouse);

			Verify.AreEqual("Unchecked", toggleStateTextBlock.Text);
			Verify.AreEqual("Unchecked", toggleStateOnClickTextBlock.Text);

			Log.Comment("Clicking secondary button should not change toggle state");
			// ClickSecondaryButton(splitButton);
			await ClickSecondaryButton(splitButton, mouse);

			Verify.AreEqual("Unchecked", toggleStateTextBlock.Text);
			Verify.AreEqual("Unchecked", toggleStateOnClickTextBlock.Text);
		}

		[TestMethod]
		public async Task ToggleAccessibilityTest()
		{
			var splitButtonPage = new SplitButtonPage();
			WindowHelper.WindowContent = splitButtonPage;
			await WindowHelper.WaitForIdle();

			ToggleSplitButton toggleButton = FindElementByName<ToggleSplitButton>("ToggleSplitButton");

			TextBlock toggleStateTextBlock = FindElementByName<TextBlock>("ToggleStateTextBlock");

			Verify.AreEqual("Unchecked", toggleStateTextBlock.Text);
			// Verify.AreEqual(ToggleState.Off, toggleButton.ToggleState);
			Verify.AreEqual(ToggleState.Off, ((IToggleProvider)toggleButton.GetAutomationPeer()).ToggleState);

			Log.Comment("Verify that toggling the SplitButton works");
			// toggleButton.Toggle();
			((IToggleProvider)toggleButton.GetAutomationPeer()).Toggle();
			await WindowHelper.WaitForIdle();

			Verify.AreEqual("Checked", toggleStateTextBlock.Text);
			Verify.AreEqual(ToggleState.On, ((IToggleProvider)toggleButton.GetAutomationPeer()).ToggleState);
		}

		// Uno Specific: There's no SplitButton.Click, so we use our own implementation
		private async Task ClickPrimaryButton(SplitButton splitButton, IInjectedPointer pointer)
		{
			pointer.Press(splitButton.GetAbsoluteBounds().GetCenter());
			pointer.Release();
			await WindowHelper.WaitForIdle();
		}

		private async Task ClickSecondaryButton(SplitButton splitButton, IInjectedPointer pointer)
		{
			pointer.Press(splitButton.GetAbsoluteBounds().GetCenter().WithX(splitButton.GetAbsoluteBounds().Right - 2));
			pointer.Release();
			await WindowHelper.WaitForIdle();
		}

		private void VerifyElementNotFound(string name) => Verify.IsNull(FindElementById<UIElement>(name));

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
