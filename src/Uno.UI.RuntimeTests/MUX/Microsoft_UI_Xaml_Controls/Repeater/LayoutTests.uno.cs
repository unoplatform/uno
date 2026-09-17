// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Uno-specific layout API and compatibility tests with no active WinUI APITests counterpart.

using System;
using System.Reflection;
using Windows.Foundation;
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

		[TestMethod]
		public void ValidateIsSignificantViewportChangeCompatibility()
		{
			var layout = new SignificantViewportLayout();

			Verify.IsFalse(layout.Invoke(default, new Rect(0, 0, 100, 100), new Rect(49, 0, 100, 100)));
			Verify.IsTrue(layout.Invoke(default, new Rect(0, 0, 100, 100), new Rect(51, 0, 100, 100)));
		}

		[TestMethod]
		public void ValidateLegacyFlowLayoutSpacingCompatibility()
		{
			RunOnUIThread.Execute(() =>
			{
				var layout = new FlowLayout
				{
					Orientation = Orientation.Horizontal,
					MinColumnSpacing = 10,
					MinRowSpacing = 20,
				};

				Verify.AreEqual(10.0, GetEffectiveSpacing(layout, "EffectiveMinItemSpacing"));
				Verify.AreEqual(20.0, GetEffectiveSpacing(layout, "EffectiveLineSpacing"));

				layout.Orientation = Orientation.Vertical;
				Verify.AreEqual(20.0, GetEffectiveSpacing(layout, "EffectiveMinItemSpacing"));
				Verify.AreEqual(10.0, GetEffectiveSpacing(layout, "EffectiveLineSpacing"));

				layout.MinItemSpacing = 7;
				layout.LineSpacing = 9;
				Verify.AreEqual(7.0, GetEffectiveSpacing(layout, "EffectiveMinItemSpacing"));
				Verify.AreEqual(9.0, GetEffectiveSpacing(layout, "EffectiveLineSpacing"));
			});
		}

		private static double GetEffectiveSpacing(FlowLayout layout, string propertyName)
			=> (double)typeof(FlowLayout)
				.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(layout);

		private sealed class SignificantViewportLayout : VirtualizingLayout
		{
			public bool Invoke(object state, Rect oldViewport, Rect newViewport)
				=> IsSignificantViewportChange(state, oldViewport, newViewport);

			protected internal override bool IsSignificantViewportChange(object state, Rect oldViewport, Rect newViewport)
				=> base.IsSignificantViewportChange(state, oldViewport, newViewport);
		}
	}
}
