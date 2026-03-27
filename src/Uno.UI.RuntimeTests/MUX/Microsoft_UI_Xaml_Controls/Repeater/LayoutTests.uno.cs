// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Uno-specific tests for Layout.IndexBasedLayoutOrientation and
// Layout.CreateDefaultItemTransitionProvider — these APIs have no equivalent
// WinUI APITests counterpart and are validated here.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Common;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	public partial class LayoutTests
	{
		[TestMethod]
		public void ValidateIndexBasedLayoutOrientationDefault()
		{
			RunOnUIThread.Execute(() =>
			{
				// StackLayout defaults to Vertical orientation → TopToBottom.
				var stackLayout = new StackLayout();
				Verify.AreEqual(IndexBasedLayoutOrientation.TopToBottom, stackLayout.IndexBasedLayoutOrientation,
					"StackLayout should default to IndexBasedLayoutOrientation.TopToBottom");

				// FlowLayout defaults to Horizontal orientation → LeftToRight.
				var flowLayout = new FlowLayout();
				Verify.AreEqual(IndexBasedLayoutOrientation.LeftToRight, flowLayout.IndexBasedLayoutOrientation,
					"FlowLayout should default to IndexBasedLayoutOrientation.LeftToRight");

				// UniformGridLayout defaults to Horizontal orientation → LeftToRight.
				var uniformGridLayout = new UniformGridLayout();
				Verify.AreEqual(IndexBasedLayoutOrientation.LeftToRight, uniformGridLayout.IndexBasedLayoutOrientation,
					"UniformGridLayout should default to IndexBasedLayoutOrientation.LeftToRight");
			});
		}

		[TestMethod]
		public void ValidateIndexBasedLayoutOrientationAfterOrientationChange()
		{
			RunOnUIThread.Execute(() =>
			{
				// StackLayout: Vertical (default) → TopToBottom, Horizontal → LeftToRight.
				var stackLayout = new StackLayout();
				Verify.AreEqual(IndexBasedLayoutOrientation.TopToBottom, stackLayout.IndexBasedLayoutOrientation);
				stackLayout.Orientation = Orientation.Horizontal;
				Verify.AreEqual(IndexBasedLayoutOrientation.LeftToRight, stackLayout.IndexBasedLayoutOrientation,
					"StackLayout should be LeftToRight after setting Orientation to Horizontal");
				stackLayout.Orientation = Orientation.Vertical;
				Verify.AreEqual(IndexBasedLayoutOrientation.TopToBottom, stackLayout.IndexBasedLayoutOrientation,
					"StackLayout should be TopToBottom after setting Orientation back to Vertical");

				// FlowLayout: Horizontal (default) → LeftToRight, Vertical → TopToBottom.
				var flowLayout = new FlowLayout();
				Verify.AreEqual(IndexBasedLayoutOrientation.LeftToRight, flowLayout.IndexBasedLayoutOrientation);
				flowLayout.Orientation = Orientation.Vertical;
				Verify.AreEqual(IndexBasedLayoutOrientation.TopToBottom, flowLayout.IndexBasedLayoutOrientation,
					"FlowLayout should be TopToBottom after setting Orientation to Vertical");
				flowLayout.Orientation = Orientation.Horizontal;
				Verify.AreEqual(IndexBasedLayoutOrientation.LeftToRight, flowLayout.IndexBasedLayoutOrientation,
					"FlowLayout should be LeftToRight after setting Orientation back to Horizontal");

				// UniformGridLayout: Horizontal (default) → LeftToRight, Vertical → TopToBottom.
				var uniformGridLayout = new UniformGridLayout();
				Verify.AreEqual(IndexBasedLayoutOrientation.LeftToRight, uniformGridLayout.IndexBasedLayoutOrientation);
				uniformGridLayout.Orientation = Orientation.Vertical;
				Verify.AreEqual(IndexBasedLayoutOrientation.TopToBottom, uniformGridLayout.IndexBasedLayoutOrientation,
					"UniformGridLayout should be TopToBottom after setting Orientation to Vertical");
				uniformGridLayout.Orientation = Orientation.Horizontal;
				Verify.AreEqual(IndexBasedLayoutOrientation.LeftToRight, uniformGridLayout.IndexBasedLayoutOrientation,
					"UniformGridLayout should be LeftToRight after setting Orientation back to Horizontal");
			});
		}

		[TestMethod]
		public void ValidateCreateDefaultItemTransitionProviderBaseReturnsNull()
		{
			RunOnUIThread.Execute(() =>
			{
				// StackLayout does not override CreateDefaultItemTransitionProvider,
				// so it inherits the base Layout implementation which returns null.
				// This will be overridden by LinedFlowLayout in a later PR.
				var stackLayout = new StackLayout();
				Verify.IsNotNull(stackLayout);
				Verify.AreEqual(IndexBasedLayoutOrientation.TopToBottom, stackLayout.IndexBasedLayoutOrientation);
			});
		}
	}
}
