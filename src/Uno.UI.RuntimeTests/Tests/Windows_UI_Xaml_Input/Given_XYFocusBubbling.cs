// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// BubblingUnitTests.h, BubblingUnitTests.cpp

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using static Uno.UI.Xaml.Input.XYFocusBubbling;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Input;

[TestClass]
[RunsOnUIThread]
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

		Assert.AreEqual(elementLeft, GetDirectionOverride(element, null, FocusNavigationDirection.Left));
		Assert.AreEqual(elementRight, GetDirectionOverride(element, null, FocusNavigationDirection.Right));
		Assert.AreEqual(elementUp, GetDirectionOverride(element, null, FocusNavigationDirection.Up));
		Assert.AreEqual(elementDown, GetDirectionOverride(element, null, FocusNavigationDirection.Down));
	}

	[TestMethod]
	public void VerifyNullWhenXYFocusPropertyRetrievalFailed()
	{
		var element = new Control();
		Assert.IsNull(GetDirectionOverride(element, null, FocusNavigationDirection.Left));
	}

	[TestMethod]
	public void VerifyCorrectOverrideChosenWhenTargetElementHasOverride()
	{
		var element = new Control();
		var candidate = new Control();
		var parent = new Grid();
		var directionOverrideOfParent = new Control();
		var overrideElement = new Control();

		parent.Children.Add(element);

		parent.SetValue(UIElement.XYFocusRightProperty, directionOverrideOfParent);
		element.SetValue(UIElement.XYFocusRightProperty, overrideElement);

		var retrieved = TryXYFocusBubble(element, candidate, null, FocusNavigationDirection.Right);
		Assert.AreEqual(overrideElement, retrieved);
	}

	[TestMethod]
	public void VerifyCorrectOverrideChosenWhenBubbling()
	{
		var element = new Control();
		var candidate = new Control();
		var parent = new Grid();
		var directionOverrideOfParent = new Control();

		parent.Children.Add(element);

		parent.SetValue(UIElement.XYFocusRightProperty, directionOverrideOfParent);

		var retrieved = TryXYFocusBubble(element, candidate, null, FocusNavigationDirection.Right);
		Assert.AreEqual(directionOverrideOfParent, retrieved);
	}

	[TestMethod]
	public void VerifyCandidateChosenWhenDescendant()
	{
		var element = new Control();
		var candidate = new Control();
		var parent = new Grid();
		var directionOverrideOfParent = new Control();

		parent.Children.Add(element);
		parent.Children.Add(candidate);

		parent.SetValue(UIElement.XYFocusRightProperty, directionOverrideOfParent);

		var retrieved = TryXYFocusBubble(element, candidate, null, FocusNavigationDirection.Right);
		Assert.AreEqual(candidate, retrieved);
	}

	[TestMethod]
	public void VerifyNullWhenCandidateNull()
	{
		var element = new Control();
		Assert.IsNull(TryXYFocusBubble(element, null, null, FocusNavigationDirection.Right));
	}

	[TestMethod]
	public void VerifyOverrideAncestorOfSearchRoot()
	{
		var element = new ContentControl();
		var candidate = new ContentControl();
		var parent = new Grid();
		var directionOverrideOfParent = new ContentControl();
		var overrideElement = new ContentControl();
		var searchRoot = new ContentControl();

		parent.Children.Add(element);

		parent.SetValue(UIElement.XYFocusRightProperty, directionOverrideOfParent);
		element.SetValue(UIElement.XYFocusRightProperty, overrideElement);

		var retrieved = TryXYFocusBubble(element, candidate, searchRoot, FocusNavigationDirection.Right);
		Assert.AreEqual(candidate, retrieved);
	}

	[TestMethod]
	public void VerifyNonFocusableDirectionOverrideChosen()
	{
		var element = new ContentControl();
		var elementLeft = new ContentControl();
		element.SetValue(UIElement.XYFocusLeftProperty, elementLeft);

		var retrieved = GetDirectionOverride(element, null, FocusNavigationDirection.Left, true);
		Assert.AreEqual(elementLeft, retrieved);
	}
}
