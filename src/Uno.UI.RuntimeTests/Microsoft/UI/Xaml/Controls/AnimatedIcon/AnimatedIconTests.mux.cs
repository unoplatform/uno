// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedIconTests.cs

using System;
using System.Threading.Tasks;
using Common;
using Microsoft.UI;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	public class AnimatedIconTests : MUXApiTestBase
	{
		[TestMethod]
		public async Task SettingStateOnParentPropagatesToChildAnimatedIcon()
		{
			AnimatedIcon animatedIcon = null;
			Grid parentGrid = null;
			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();
				parentGrid = new Grid();
				parentGrid.Children.Add(animatedIcon);
				AnimatedIcon.SetState(parentGrid, "Initial State");

				Content = parentGrid;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				string stateString = "Test State";
				AnimatedIcon.SetState(parentGrid, stateString);
				Verify.AreEqual(stateString, AnimatedIcon.GetState(animatedIcon));
			});
		}

		[TestMethod]
		public async Task SettingStateOnGrandParentPropagatesToGrandChildAnimatedIcon()
		{
			AnimatedIcon animatedIcon = null;
			Grid parentGrid = null;
			Grid grandParentGrid = null;
			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();
				parentGrid = new Grid();
				grandParentGrid = new Grid();
				parentGrid.Children.Add(animatedIcon);
				grandParentGrid.Children.Add(parentGrid);
				AnimatedIcon.SetState(grandParentGrid, "Initial State");

				Content = grandParentGrid;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				string stateString = "Test State";
				AnimatedIcon.SetState(grandParentGrid, stateString);
				Verify.AreEqual(stateString, AnimatedIcon.GetState(animatedIcon));
			});
		}

		[TestMethod]
		public async Task ChangingVisualTrees()
		{
			AnimatedIcon animatedIcon = null;
			Grid parentGrid = null;
			Grid grandParentGrid = null;
			Grid newParentGrid = null;
			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();
				parentGrid = new Grid();
				grandParentGrid = new Grid();
				newParentGrid = new Grid();
				parentGrid.Children.Add(animatedIcon);
				grandParentGrid.Children.Add(parentGrid);
				AnimatedIcon.SetState(parentGrid, "Initial State");

				Content = grandParentGrid;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				string stateString = "Test State";
				AnimatedIcon.SetState(parentGrid, stateString);
				Verify.AreEqual(stateString, AnimatedIcon.GetState(animatedIcon));

				parentGrid.Children.Clear();
				newParentGrid.Children.Add(animatedIcon);
				grandParentGrid.Children.Clear();
				grandParentGrid.Children.Add(newParentGrid);
				AnimatedIcon.SetState(newParentGrid, "Initial State");

				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				string state2String = "Test State2";
				AnimatedIcon.SetState(newParentGrid, state2String);
				Verify.AreEqual(state2String, AnimatedIcon.GetState(animatedIcon));

				string badStateString = "Bad State";
				AnimatedIcon.SetState(parentGrid, badStateString);
				Verify.AreNotEqual(badStateString, AnimatedIcon.GetState(animatedIcon));
			});
		}

		[TestMethod]
		public async Task SettingStateOnParentDoesNotPropagateToChildNonAnimatedIcon()
		{
			AnimatedIcon animatedIcon = null;
			Grid parentGrid = null;
			Grid childGrid = null;
			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();
				parentGrid = new Grid();
				childGrid = new Grid();
				parentGrid.Children.Add(childGrid);
				childGrid.Children.Add(animatedIcon);
				AnimatedIcon.SetState(parentGrid, "Initial State");

				Content = parentGrid;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				string stateString = "Test State";
				AnimatedIcon.SetState(parentGrid, stateString);
				Verify.AreNotEqual(stateString, AnimatedIcon.GetState(childGrid));
				Verify.AreEqual(stateString, AnimatedIcon.GetState(animatedIcon));
			});
		}

		[TestMethod]
		public async Task AddingAnimatedIconToGridWithoutAStateDoesNotPropogateState()
		{
			// This is not actually a desired behavior.  Ideally we would be able to set
			// the AnimatedIcon.State property on any ancestor of an animated icon at any
			// time and that would reach the icon. However this is challenging to do
			// efficiently so instead we require that the parent have an AnimatedIcon.State
			// value when the icon is loaded.
			AnimatedIcon animatedIcon = null;
			Grid parentGrid = null;
			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();
				parentGrid = new Grid();
				parentGrid.Children.Add(animatedIcon);

				Content = parentGrid;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				string stateString = "Test State";
				AnimatedIcon.SetState(parentGrid, stateString);
				Verify.AreNotEqual(stateString, AnimatedIcon.GetState(animatedIcon));
			});
		}

		[TestMethod]
		public async Task CanSetStateOnAnimatedIconDirectlyWithoutPropagationToChild()
		{
			AnimatedIcon animatedIcon = null;
			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();

				Content = animatedIcon;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				string stateString = "Test State";
				AnimatedIcon.SetState(animatedIcon, stateString);
				Verify.AreEqual(stateString, AnimatedIcon.GetState(animatedIcon));
				Verify.AreNotEqual(stateString, AnimatedIcon.GetState(VisualTreeHelper.GetChild(animatedIcon, 0)));
			});
		}

		[TestMethod]
		public async Task ForegroundInheritsFromParent()
		{
			AnimatedIcon animatedIcon = null;
			ContentControl parentContentControl = null;
			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();
				parentContentControl = new ContentControl();
				parentContentControl.Content = animatedIcon;

				Content = parentContentControl;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var foregroundBrush = new SolidColorBrush(Colors.Red);
				parentContentControl.Foreground = foregroundBrush;
				Verify.AreEqual(foregroundBrush, animatedIcon.Foreground);

				var newForegroundBrush = new SolidColorBrush(Colors.Blue);
				animatedIcon.Foreground = newForegroundBrush;
				Verify.AreEqual(newForegroundBrush, animatedIcon.Foreground);
			});
		}

		[TestMethod]
		public async Task CanChangeSourceAfterState()
		{
			AnimatedIcon animatedIcon = null;
			Grid parentGrid = null;
			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();
				parentGrid = new Grid();
				parentGrid.Children.Add(animatedIcon);

				Content = parentGrid;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				animatedIcon.Source = new AnimatedChevronDownSmallVisualSource();
				AnimatedIcon.SetState(parentGrid, "Normal");
				animatedIcon.Source = new AnimatedSettingsVisualSource();
				AnimatedIcon.SetState(parentGrid, "PointerOver");
				animatedIcon.Source = null;
				AnimatedIcon.SetState(parentGrid, "");
			});
		}

		[TestMethod]
		public async Task TransitionFallbackLogic()
		{
			AnimatedIcon animatedIcon = null;
			var layoutUpdatedEvent = new UnoAutoResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				animatedIcon = new AnimatedIcon();
				animatedIcon.Source = new MockIAnimatedIconSource2();

				Content = animatedIcon;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				animatedIcon.LayoutUpdated += AnimatedIcon_LayoutUpdated;
				AnimatedIcon.SetState(animatedIcon, "a");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				layoutUpdatedEvent.Reset();
				AnimatedIcon.SetState(animatedIcon, "b");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual("aTob_Start", AnimatedIconTestHooks.GetLastAnimationSegmentStart(animatedIcon));
				Verify.AreEqual("aTob_End", AnimatedIconTestHooks.GetLastAnimationSegmentEnd(animatedIcon));

				layoutUpdatedEvent.Reset();
				AnimatedIcon.SetState(animatedIcon, "c");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual("bToc_Start", AnimatedIconTestHooks.GetLastAnimationSegmentStart(animatedIcon));
				// bToc_End is undefined in MockIAnimatedIconSource2
				Verify.AreEqual("", AnimatedIconTestHooks.GetLastAnimationSegmentEnd(animatedIcon));

				layoutUpdatedEvent.Reset();
				AnimatedIcon.SetState(animatedIcon, "d");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				// cTod_Start is undefined in MockIAnimatedIconSource2
				Verify.AreEqual("", AnimatedIconTestHooks.GetLastAnimationSegmentStart(animatedIcon));
				Verify.AreEqual("cTod_End", AnimatedIconTestHooks.GetLastAnimationSegmentEnd(animatedIcon));

				layoutUpdatedEvent.Reset();
				AnimatedIcon.SetState(animatedIcon, "e");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				// dToe_Start and dToe_End are undefined in MockIAnimatedIconSource2, the first backup is dToe
				Verify.AreEqual("", AnimatedIconTestHooks.GetLastAnimationSegmentStart(animatedIcon));
				Verify.AreEqual("dToe", AnimatedIconTestHooks.GetLastAnimationSegmentEnd(animatedIcon));

				layoutUpdatedEvent.Reset();
				AnimatedIcon.SetState(animatedIcon, "f");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				// eTof_Start, eTof_End, and eTof are undefined in MockIAnimatedIconSource2, the second backup is f
				Verify.AreEqual("", AnimatedIconTestHooks.GetLastAnimationSegmentStart(animatedIcon));
				Verify.AreEqual("f", AnimatedIconTestHooks.GetLastAnimationSegmentEnd(animatedIcon));

				layoutUpdatedEvent.Reset();
				AnimatedIcon.SetState(animatedIcon, "b");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				// fTob_Start, fTob_End, fTob and b are all undefined in MockIAnimatedIconSource2, the third backup is any
				// marker which ends with the string "Tob_End"
				Verify.AreEqual("", AnimatedIconTestHooks.GetLastAnimationSegmentStart(animatedIcon));
				Verify.AreEqual("aTob_End", AnimatedIconTestHooks.GetLastAnimationSegmentEnd(animatedIcon));

				layoutUpdatedEvent.Reset();
				AnimatedIcon.SetState(animatedIcon, "0.12345");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				// bTo0.12345_Start, bTo0.12345_End, bTo0.12345, and 0.12345  are all undefined in MockIAnimatedIconSource2, and
				// there are no markers which end with the string "To0.12345_End" so finally we attempt to interpret the state as
				// a float to get the position to animate to.
				Verify.AreEqual("", AnimatedIconTestHooks.GetLastAnimationSegmentStart(animatedIcon));
				Verify.AreEqual("0.12345", AnimatedIconTestHooks.GetLastAnimationSegmentEnd(animatedIcon));

				layoutUpdatedEvent.Reset();
				AnimatedIcon.SetState(animatedIcon, "Failure");
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();
			await layoutUpdatedEvent.WaitOne();

			RunOnUIThread.Execute(() =>
			{
				// 0.12345ToFailure_Start, 0.12345ToFailure_End, 0.12345ToFailure, and Failure are all undefined in MockIAnimatedIconSource2, and
				// there are no markers which end with the string "ToFailure_End" and Failure is not a float, so we have failed to find a marker.
				Verify.AreEqual("", AnimatedIconTestHooks.GetLastAnimationSegmentStart(animatedIcon));
				Verify.AreEqual("0.0", AnimatedIconTestHooks.GetLastAnimationSegmentEnd(animatedIcon));
			});

			void AnimatedIcon_LayoutUpdated(object sender, object e)
			{
				layoutUpdatedEvent.Set();
			}
		}

		[TestMethod]
		public async Task ChangingSourcePropertyChangesRenderSize()
		{
			AnimatedIcon icon = null;
			RunOnUIThread.Execute(() =>
			{
				icon = new AnimatedIcon();
				Content = new StackPanel()
				{
					Children = { icon }
				};
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				// Icon height will be zero if the source property is not set.
				Verify.IsTrue(Math.Abs(icon.ActualHeight) < 0.1);
				icon.Source = new AnimatedChevronDownSmallVisualSource();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				// Icon will have a height if the AnimatedIcon also updated the visual tree to rerender.
				Verify.IsTrue(Math.Abs(icon.ActualHeight) > 10);
			});
		}
	}
}
