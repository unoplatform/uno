// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusSelectionUnitTests.h, FocusSelectionUnitTests.cpp

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using static Uno.UI.Tests.Helpers.MuxVerify;

namespace Uno.UI.Tests.Windows_UI_Xaml.Input.Internal
{
	[TestClass]
	public partial class Given_FocusSelection
	{
		private partial class View : FrameworkElement
		{
		}

		[TestMethod]
		public void ValidateShouldUpdateFocusWhenAllowFocusOnInteractionDisabled()
		{
			var elementA = new View();
			elementA.AllowFocusOnInteraction = false;
			var elementB = new View();
			elementB.AllowFocusOnInteraction = true;

			bool update = FocusSelection.ShouldUpdateFocus(elementA, FocusState.Pointer);
			Assert.IsFalse(update);

			//Even if the element is supposed to not allow focus, if the focus state is not a pointer, we should
			//still update focus
			update = FocusSelection.ShouldUpdateFocus(elementA, FocusState.Keyboard);
			Assert.IsTrue(update);

			//Regardless of FocusState, if we should allow focus, then we should update focus
			update = FocusSelection.ShouldUpdateFocus(elementB, FocusState.Pointer);
			Assert.IsTrue(update);
		}

		[TestMethod]
		public void ValidateNavigationDirection()
		{
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.GamepadDPadUp), FocusNavigationDirection.Up);
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.GamepadLeftThumbstickUp), FocusNavigationDirection.Up);
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.Up), FocusNavigationDirection.Up);

			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.GamepadDPadDown), FocusNavigationDirection.Down);
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.GamepadLeftThumbstickDown), FocusNavigationDirection.Down);
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.Down), FocusNavigationDirection.Down);

			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.GamepadDPadLeft), FocusNavigationDirection.Left);
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.GamepadLeftThumbstickLeft), FocusNavigationDirection.Left);
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.Left), FocusNavigationDirection.Left);

			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.GamepadDPadRight), FocusNavigationDirection.Right);
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.GamepadLeftThumbstickRight), FocusNavigationDirection.Right);
			VerifyAreEqual(FocusSelection.GetNavigationDirection(VirtualKey.Right), FocusNavigationDirection.Right);
		}

		[TestMethod]
		public void ValidateNavigationDirectionForKeyboardArrow()
		{
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.GamepadDPadUp), FocusNavigationDirection.None);
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.GamepadLeftThumbstickUp), FocusNavigationDirection.None);
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.Up), FocusNavigationDirection.Up);

			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.GamepadDPadDown), FocusNavigationDirection.None);
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.GamepadLeftThumbstickDown), FocusNavigationDirection.None);
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.Down), FocusNavigationDirection.Down);

			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.GamepadDPadLeft), FocusNavigationDirection.None);
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.GamepadLeftThumbstickLeft), FocusNavigationDirection.None);
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.Left), FocusNavigationDirection.Left);

			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.GamepadDPadRight), FocusNavigationDirection.None);
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.GamepadLeftThumbstickRight), FocusNavigationDirection.None);
			VerifyAreEqual(FocusSelection.GetNavigationDirectionForKeyboardArrow(VirtualKey.Right), FocusNavigationDirection.Right);
		}

		[TestMethod]
		public void ValidateTryDirectionalFocus()
		{
			var element = new Control();
			element.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

			var candidate = new Control();
			candidate.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Disabled;

			var focusManagerMock = new MockFocusManager();

			focusManagerMock.FindNextFocusResult = candidate;

			var result = new FocusMovementResult(true, false);

			focusManagerMock.SetFocusedElementResult = result;

			var info = FocusSelection.TryDirectionalFocus(focusManagerMock, FocusNavigationDirection.Right, element);
			Assert.IsTrue(info.Handled);
		}

		private class MockFocusManager : IFocusManager
		{
			public DependencyObject? FindNextFocusResult { get; set; }

			public FocusMovementResult? SetFocusedElementResult { get; set; }

			public DependencyObject? FindNextFocus(FindFocusOptions findFocusOptions, XYFocusOptions xyFocusOptions, DependencyObject? component = null, bool updateManifolds = true) =>
				FindNextFocusResult;

			public FocusMovementResult SetFocusedElement(FocusMovement movement) =>
				SetFocusedElementResult ?? new FocusMovementResult();
		}

		[TestMethod]
		public void ValidateTryDirectionalFocusMarksUnhandled()
		{
			Control element = new Control();
			element.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
			var focusManagerMock = new MockFocusManager();

			focusManagerMock.FindNextFocusResult = null;

			var info = FocusSelection.TryDirectionalFocus(focusManagerMock, FocusNavigationDirection.Right, element);
			Assert.IsFalse(info.Handled);
		}

		[TestMethod]
		public void ValidateThatNotHandledWhenModeInherited()
		{
			Control element = new Control();
			element.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Auto;
			var focusManagerMock = new MockFocusManager();

			var info = FocusSelection.TryDirectionalFocus(focusManagerMock, FocusNavigationDirection.Right, element);
			Assert.IsFalse(info.Handled);
		}

		[TestMethod]
		public void ValidateShouldNotBubbleWhenModeNone()
		{
			Control element = new Control();
			element.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Disabled;
			var focusManagerMock = new MockFocusManager();

			var info = FocusSelection.TryDirectionalFocus(focusManagerMock, FocusNavigationDirection.Right, element);
			Assert.IsFalse(info.Handled);
			Assert.IsFalse(info.ShouldBubble);
		}

		[TestMethod]
		public void ValidateNotHandledWhenNotUIElement()
		{
			Control element = new Control();
			element.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
			var focusManagerMock = new MockFocusManager();

			var info = FocusSelection.TryDirectionalFocus(focusManagerMock, FocusNavigationDirection.Right, element);
			Assert.IsFalse(info.Handled);
		}
	}
}
