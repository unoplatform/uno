// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// BubblingUnitTests.h, BubblingUnitTests.cpp

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using static Uno.UI.Tests.Helpers.MuxVerify;
using static Uno.UI.Xaml.Input.XYFocusBubbling;

namespace Uno.UI.Tests.Windows_UI_Xaml.Input.Internal
{
	[TestClass]
	public class Given_XYFocusBubbling
	{
		[TestMethod]
		public void VerifyXYFocusPropertyRetrieval()
		{
			var element = new Control();

			var elementLeft = new Control();
			var elementRight = new Control();
			var elementUp = new Control();
			var elementDown = new Control();

			element.SetValue(UIElement.XYFocusLeftProperty, elementLeft);
			element.SetValue(UIElement.XYFocusRightProperty, elementRight);
			element.SetValue(UIElement.XYFocusDownProperty, elementDown);
			element.SetValue(UIElement.XYFocusUpProperty, elementUp);

			DependencyObject? retirevedElement = GetDirectionOverride(element, null, FocusNavigationDirection.Left);
			VerifyAreEqual(retirevedElement, elementLeft);

			retirevedElement = GetDirectionOverride(element, null, FocusNavigationDirection.Right);
			VerifyAreEqual(retirevedElement, elementRight);

			retirevedElement = GetDirectionOverride(element, null, FocusNavigationDirection.Up);
			VerifyAreEqual(retirevedElement, elementUp);

			retirevedElement = GetDirectionOverride(element, null, FocusNavigationDirection.Down);
			VerifyAreEqual(retirevedElement, elementDown);
		}

		[TestMethod]
		public void VerifyNullWhenXYFocusPropertyRetrievalFailed()
		{
			var element = new Control();

			DependencyObject? retirevedElement = GetDirectionOverride(element, null, FocusNavigationDirection.Left);
			Assert.IsNull(retirevedElement);
		}

		[TestMethod]
		public void VerifyCorrectOverrideChosenWhenTargetElementHasOverride()
		{
			var element = new Control();
			var candidate = new Control();
			var parent = new Control();
			var directionOverrideOfParent = new Control();
			var overrideElement = new Control();

			parent.AddChild(element);

			parent.SetValue(UIElement.XYFocusRightProperty, directionOverrideOfParent);

			element.SetValue(UIElement.XYFocusRightProperty, overrideElement);

			DependencyObject? retrievedElement = TryXYFocusBubble(element, candidate, null, FocusNavigationDirection.Right);
			VerifyAreEqual(retrievedElement, overrideElement);
		}

		[TestMethod]
		public void VerifyCorrectOverrideChosenWhenBubbling()
		{
			var element = new Control();
			var candidate = new Control();
			var parent = new Control();
			var directionOverrideOfParent = new Control();

			parent.AddChild(element);

			parent.SetValue(UIElement.XYFocusRightProperty, directionOverrideOfParent);

			DependencyObject? retrievedElement = TryXYFocusBubble(element, candidate, null, FocusNavigationDirection.Right);
			VerifyAreEqual(retrievedElement, directionOverrideOfParent);
		}

		[TestMethod]
		public void VerifyCandidateChosenWhenDescendant()
		{
			var element = new Control();
			var candidate = new Control();
			var parent = new Control();
			var directionOverrideOfParent = new Control();

			parent.AddChild(element);
			parent.AddChild(candidate);

			parent.SetValue(UIElement.XYFocusRightProperty, directionOverrideOfParent);

			DependencyObject? retrievedElement = TryXYFocusBubble(element, candidate, null, FocusNavigationDirection.Right);
			VerifyAreEqual(retrievedElement, candidate);
		}

		[TestMethod]
		public void VerifyNullWhenCandidateNull()
		{
			var element = new Control();
			DependencyObject? retrievedElement = TryXYFocusBubble(element, null, null, FocusNavigationDirection.Right);
			Assert.IsNull(retrievedElement);
		}

		[TestMethod]
		public void VerifyOverrideAncestorOfSearchRoot()
		{
			var element = new UIElement();
			var candidate = new UIElement();
			var parent = new Control();
			var directionOverrideOfParent = new UIElement();
			var overrideElement = new UIElement();
			var searchRoot = new UIElement();

			parent.AddChild(element);

			parent.SetValue(UIElement.XYFocusRightProperty, directionOverrideOfParent);

			element.SetValue(UIElement.XYFocusRightProperty, overrideElement);

			DependencyObject? retrievedElement = TryXYFocusBubble(element, candidate, searchRoot, FocusNavigationDirection.Right);
			VerifyAreEqual(retrievedElement, candidate);
		}

		[TestMethod]
		public void VerifyNonFocusableDirectionOverrideChosen()
		{
			var element = new UIElement();
			var elementLeft = new UIElement();
			element.SetValue(UIElement.XYFocusLeftProperty, elementLeft);

			DependencyObject? retrievedElement = GetDirectionOverride(element, null, FocusNavigationDirection.Left, true);
			VerifyAreEqual(retrievedElement, elementLeft);
		}
	}
}
