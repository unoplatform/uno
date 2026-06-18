#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Core;   // VisualTree
using Uno.UI.Xaml.Input;  // InputDeviceType
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TextBox_SoftwareKeyboard
{
	// Models the platform contract: a show populates OccludedRect (=> Visible),
	// a hide clears it — mirroring the real Win32 occlusion forwarding.
	private sealed class FakeInputPaneExtension : IInputPaneExtension
	{
		public int ShowCount { get; private set; }
		public int HideCount { get; private set; }

		public bool TryShow()
		{
			ShowCount++;
			InputPane.GetForCurrentView().OccludedRect = new Rect(0, 0, 100, 200);
			return true;
		}

		public bool TryHide()
		{
			HideCount++;
			InputPane.GetForCurrentView().OccludedRect = default;
			return true;
		}
	}

	[TestMethod]
	public void When_InputPane_Routes_To_Injected_Extension()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var inputPane = InputPane.GetForCurrentView();
		Assert.IsTrue(inputPane.TryShow());
		Assert.AreEqual(1, fake.ShowCount);
		Assert.IsTrue(inputPane.Visible);     // OccludedRect set by the fake
		Assert.IsTrue(inputPane.TryHide());
		Assert.AreEqual(1, fake.HideCount);
		Assert.IsFalse(inputPane.Visible);
	}

	[TestMethod]
	public async Task When_TouchFocus_Then_Keyboard_Shown()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);

		VisualTree.GetContentRootForElement(textBox)!.InputManager.LastInputDeviceType = InputDeviceType.Touch;
		textBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, fake.ShowCount);
	}

	[TestMethod]
	public async Task When_MouseFocus_Then_Keyboard_Not_Shown()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);

		VisualTree.GetContentRootForElement(textBox)!.InputManager.LastInputDeviceType = InputDeviceType.Mouse;
		textBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, fake.ShowCount);
	}

	[TestMethod]
	public async Task When_KeyboardFocus_Then_Keyboard_Not_Shown()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var textBox = new TextBox();
		await UITestHelper.Load(textBox);

		VisualTree.GetContentRootForElement(textBox)!.InputManager.LastInputDeviceType = InputDeviceType.Touch;
		textBox.Focus(FocusState.Keyboard); // not a pointer-driven focus
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, fake.ShowCount);
	}

	[TestMethod]
	public async Task When_Blur_To_NonText_Then_Keyboard_Hidden()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var textBox = new TextBox();
		var button = new Button() { Content = "x" };
		var panel = new StackPanel();
		panel.Children.Add(textBox);
		panel.Children.Add(button);
		await UITestHelper.Load(panel);

		VisualTree.GetContentRootForElement(textBox)!.InputManager.LastInputDeviceType = InputDeviceType.Touch;
		textBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(1, fake.ShowCount);

		button.Focus(FocusState.Pointer); // focus leaves the text box to a non-text control
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, fake.HideCount);
	}

	[TestMethod]
	public async Task When_Focus_Moves_Between_TextBoxes_Then_No_Flicker()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var tb1 = new TextBox();
		var tb2 = new TextBox();
		var panel = new StackPanel();
		panel.Children.Add(tb1);
		panel.Children.Add(tb2);
		await UITestHelper.Load(panel);

		var inputManager = VisualTree.GetContentRootForElement(tb1)!.InputManager;

		inputManager.LastInputDeviceType = InputDeviceType.Touch;
		tb1.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		inputManager.LastInputDeviceType = InputDeviceType.Touch;
		tb2.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		// tb2 took over before tb1's deferred hide ran => the keyboard is never hidden (no
		// flicker) and stays visible. The 2nd show is a no-op because the keyboard is already
		// visible (InputPane.TryShow short-circuits on Visible), so ShowCount stays at 1.
		Assert.AreEqual(0, fake.HideCount);
		Assert.IsTrue(InputPane.GetForCurrentView().Visible);
		Assert.AreEqual(1, fake.ShowCount);
	}

	[TestMethod]
	public async Task When_PasswordBox_TouchFocus_Then_Keyboard_Shown()
	{
		var fake = new FakeInputPaneExtension();
		using var _ = InputPane.SetExtensionForTesting(fake);

		var passwordBox = new PasswordBox();
		await UITestHelper.Load(passwordBox);

		VisualTree.GetContentRootForElement(passwordBox)!.InputManager.LastInputDeviceType = InputDeviceType.Touch;
		passwordBox.Focus(FocusState.Pointer);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, fake.ShowCount);
	}
}
