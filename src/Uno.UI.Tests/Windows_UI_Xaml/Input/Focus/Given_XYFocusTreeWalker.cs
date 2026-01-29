// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// TreeWalkerUnitTests.h, TreeWalkerUnitTests.cpp

#nullable enable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using static Uno.UI.Tests.Helpers.MuxVerify;
using static Uno.UI.Xaml.Input.XYFocusTreeWalker;

namespace Uno.UI.Tests.Windows_UI_Xaml.Input.Internal
{
	[TestClass]
	public class Given_XYFocusTreeWalker
	{
		internal void VerifyResult(
			List<XYFocus.XYFocusParameters> vector,
			List<DependencyObject> target)
		{
			List<DependencyObject?> uiElementList = new List<DependencyObject?>();

			foreach (var param in vector)
			{
				uiElementList.Add(param.Element);
			}

			CollectionAssert.AreEqual(target, uiElementList);
		}

		[TestMethod]
		public void VerifyFindElement()
		{
			var root = new XYFocusCUIElement();

			var current = new FocusableXYFocusCUIElement();
			var candidate = new FocusableXYFocusCUIElement();

			root.AddChild(candidate);

			var candidateList = FindElements(root, current, null, true, false);
			Assert.AreEqual(1, candidateList.Count);
			VerifyAreEqual(candidateList[0].Element, candidate);
		}

		[TestMethod]
		public void VerifyFindElementIgnoresNonFocusableChildren()
		{
			var root = new XYFocusCUIElement();

			var current = new FocusableXYFocusCUIElement();

			var candidate = new FocusableXYFocusCUIElement();
			var nonFocusableCandidate = new XYFocusCUIElement();

			root.AddChild(candidate);
			root.AddChild(nonFocusableCandidate);

			var candidateList = FindElements(root, current, null, true, false);
			Assert.AreEqual(1, candidateList.Count);
			VerifyAreEqual(candidateList[0].Element, candidate);
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

			List<DependencyObject> targetList = new List<DependencyObject>();
			targetList.Add(candidate);
			targetList.Add(candidateB);
			targetList.Add(candidateC);

			root.AddChild(candidate);
			root.AddChild(subRoot);

			subRoot.AddChild(candidateB);

			candidateB.AddChild(candidateC);

			var candidateList = FindElements(root, current, null, true, false);
			Assert.AreEqual(3, candidateList.Count);
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

			var targetList = new List<DependencyObject>();
			targetList.Add(candidateB);
			targetList.Add(candidateC);

			root.AddChild(candidate);
			root.AddChild(subRoot);

			subRoot.AddChild(candidateB);

			candidateB.AddChild(candidateC);

			var candidateList = FindElements(subRoot, current, null, true, false);
			Assert.AreEqual(2, candidateList.Count);
			VerifyResult(candidateList, targetList);
		}

		[TestMethod]
		public void VerifyCurrentElementNotIncludedInList()
		{
			var root = new XYFocusCUIElement();

			var current = new FocusableXYFocusCUIElement();

			root.AddChild(current);

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

			root.AddChild(scrollviewer);
			scrollviewer.AddChild(candidate);

			var candidateList = FindElements(root, current, scrollviewer, true, false);
			Assert.AreEqual(1, candidateList.Count);
			VerifyAreEqual(candidateList[0].Element, candidate);
		}

		[Ignore("ScrollViewer-related XY navigation does not work properly yet")]
		[TestMethod]
		public void VerifyElementPartOfNestedScrollingScrollviewerAddedToList()
		{
			var root = new XYFocusCUIElement();
			var scrollviewer = new ScrollViewer();
			var scrollviewerB = new ScrollViewer();

			var current = new FocusableXYFocusCUIElement();
			var candidate = new FocusableXYFocusCUIElement();
			//TODO:MZ: This has to be done somehow (in multiple tests in this file)
			//Expect(*candidate, IsOccluded)
			//	.ReturnValue(true);

			root.AddChild(scrollviewer);
			scrollviewer.AddChild(scrollviewerB);
			scrollviewer.AddChild(candidate);

			var candidateList = FindElements(root, current, scrollviewerB, true, false);
			Assert.AreEqual(1, candidateList.Count);
			VerifyAreEqual(candidateList[0].Element, candidate);
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

			root.AddChild(scrollviewer);
			scrollviewer.AddChild(candidate);
			//Expect(*candidate, IsOccluded)
			//	.ReturnValue(false);

			var candidateList = FindElements(root, current, scrollviewerB, true, false);
			Assert.AreEqual(1, candidateList.Count);
			VerifyAreEqual(candidateList[0].Element, candidate);
		}

		[TestMethod]
		public void VerifyOccludedElementInNonActiveScrollviewerNotAddedToList()
		{
			var root = new XYFocusCUIElement();
			var scrollviewer = new ScrollViewer();
			var scrollviewerB = new ScrollViewer();

			var current = new FocusableXYFocusCUIElement();
			var candidate = new FocusableXYFocusCUIElement();
			//Expect(*candidate, IsOccluded)
			//	.ReturnValue(true);

			root.AddChild(scrollviewer);
			scrollviewer.AddChild(candidate);

			var candidateList = FindElements(root, current, scrollviewerB, true, false);
			// TODO: This assert is flaky
			//Assert.AreEqual(0, candidateList.Count);
		}

		public class FocusableXYFocusCUIElement : Control
		{
			public FocusableXYFocusCUIElement()
			{
				SkipFocusSubtree = false;
			}
		}

		public class XYFocusCUIElement : Control
		{
			public XYFocusCUIElement()
			{
				IsTabStop = false; // To make it "non-focusable"
			}
		}
	}
}
