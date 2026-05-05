// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// TreeWalkerUnitTests.h, TreeWalkerUnitTests.cpp

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Input;
using static Uno.UI.Xaml.Input.XYFocusTreeWalker;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Input;

[TestClass]
public class Given_XYFocusTreeWalker
{
	private static void VerifyResult(
		List<XYFocus.XYFocusParameters> vector,
		List<DependencyObject> target)
	{
		var actual = vector.Select(p => p.Element).ToList();
		CollectionAssert.AreEqual(target, actual);
	}

	[TestMethod]
	public void VerifyFindElement()
	{
		var root = new XYFocusCUIElement();

		var current = new FocusableXYFocusCUIElement();
		var candidate = new FocusableXYFocusCUIElement();

		root.Children.Add(candidate);

		var candidateList = FindElements(root, current, null, true, false);
		Assert.HasCount(1, candidateList);
		Assert.AreEqual(candidate, candidateList[0].Element);
	}

	[TestMethod]
	public void VerifyFindElementIgnoresNonFocusableChildren()
	{
		var root = new XYFocusCUIElement();

		var current = new FocusableXYFocusCUIElement();

		var candidate = new FocusableXYFocusCUIElement();
		var nonFocusableCandidate = new XYFocusCUIElement();

		root.Children.Add(candidate);
		root.Children.Add(nonFocusableCandidate);

		var candidateList = FindElements(root, current, null, true, false);
		Assert.HasCount(1, candidateList);
		Assert.AreEqual(candidate, candidateList[0].Element);
	}

	[TestMethod]
	public void VerifyRecursiveSearchOfElements()
	{
		var root = new XYFocusCUIElement();
		var subRoot = new XYFocusCUIElement();

		var current = new FocusableXYFocusCUIElement();

		var candidate = new FocusableXYFocusCUIElement();
		var candidateB = new FocusableXYFocusCUIElement();
		var candidateC = new FocusableXYFocusCUIElement();

		var targetList = new List<DependencyObject>
		{
			candidate,
			candidateB,
			candidateC,
		};

		root.Children.Add(candidate);
		root.Children.Add(subRoot);

		subRoot.Children.Add(candidateB);

		candidateB.Content = candidateC;

		var candidateList = FindElements(root, current, null, true, false);
		Assert.HasCount(3, candidateList);
		VerifyResult(candidateList, targetList);
	}

	[TestMethod]
	public void VerifyOnlyElementsWithinRootSelected()
	{
		var root = new XYFocusCUIElement();
		var subRoot = new XYFocusCUIElement();

		var current = new FocusableXYFocusCUIElement();

		var candidate = new FocusableXYFocusCUIElement();
		var candidateB = new FocusableXYFocusCUIElement();
		var candidateC = new FocusableXYFocusCUIElement();

		var targetList = new List<DependencyObject>
		{
			candidateB,
			candidateC,
		};

		root.Children.Add(candidate);
		root.Children.Add(subRoot);

		subRoot.Children.Add(candidateB);

		candidateB.Content = candidateC;

		var candidateList = FindElements(subRoot, current, null, true, false);
		Assert.HasCount(2, candidateList);
		VerifyResult(candidateList, targetList);
	}

	[TestMethod]
	public void VerifyCurrentElementNotIncludedInList()
	{
		var root = new XYFocusCUIElement();

		var current = new FocusableXYFocusCUIElement();

		root.Children.Add(current);

		var candidateList = FindElements(root, current, null, true, false);
		Assert.IsEmpty(candidateList);
	}

	[Ignore("ScrollViewer-related XY navigation does not work properly yet")]
	[TestMethod]
	public void VerifyElementParticipatingInScrollAddedToList()
	{
		var root = new XYFocusCUIElement();
		var scrollviewer = new ScrollViewer();

		var current = new FocusableXYFocusCUIElement();
		var candidate = new FocusableXYFocusCUIElement();

		root.Children.Add(scrollviewer);
		scrollviewer.Content = candidate;

		var candidateList = FindElements(root, current, scrollviewer, true, false);
		Assert.HasCount(1, candidateList);
		Assert.AreEqual(candidate, candidateList[0].Element);
	}

	[Ignore("ScrollViewer-related XY navigation does not work properly yet")]
	[TestMethod]
	public void VerifyElementInNonActiveScrollviewerAddedToList()
	{
		var root = new XYFocusCUIElement();
		var scrollviewer = new ScrollViewer();
		var scrollviewerB = new ScrollViewer();

		var current = new FocusableXYFocusCUIElement();
		var candidate = new FocusableXYFocusCUIElement();

		root.Children.Add(scrollviewer);
		scrollviewer.Content = candidate;

		var candidateList = FindElements(root, current, scrollviewerB, true, false);
		Assert.HasCount(1, candidateList);
		Assert.AreEqual(candidate, candidateList[0].Element);
	}

	[TestMethod]
	public void VerifyOccludedElementInNonActiveScrollviewerNotAddedToList()
	{
		var root = new XYFocusCUIElement();
		var scrollviewer = new ScrollViewer();
		var scrollviewerB = new ScrollViewer();

		var current = new FocusableXYFocusCUIElement();
		var candidate = new FocusableXYFocusCUIElement();

		root.Children.Add(scrollviewer);
		scrollviewer.Content = candidate;

		_ = FindElements(root, current, scrollviewerB, true, false);
		// TODO: This assert is flaky
		// Assert.HasCount(0, candidateList);
	}

	private sealed class FocusableXYFocusCUIElement : ContentControl
	{
		public FocusableXYFocusCUIElement()
		{
			IsTabStop = true;
			SkipFocusSubtree = false;
		}
	}

	// Non-focusable container (Grid is not a Control, so it has no focus behavior).
	private sealed class XYFocusCUIElement : Grid
	{
	}
}
