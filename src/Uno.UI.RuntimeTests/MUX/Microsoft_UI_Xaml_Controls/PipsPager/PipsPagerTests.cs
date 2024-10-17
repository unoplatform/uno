// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPagerTests.cs, commit 2eebc34

using Common;
using MUXControlsTestApp.Utilities;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation;
using Private.Infrastructure;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests;
#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;
#endif
namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	[TestClass]
	[RequiresFullWindow]
	public class PipsPagerTests : MUXApiTestBase
	{
		[TestMethod]
		public void VerifyAutomationPeerBehavior()
		{
			RunOnUIThread.Execute(() =>
			{
				var pipsControl = new PipsPager();
				pipsControl.NumberOfPages = 5;
				Content = pipsControl;

				var peer = PipsPagerAutomationPeer.CreatePeerForElement(pipsControl);
				var selectionPeer = peer as ISelectionProvider;
				Verify.AreEqual(false, selectionPeer.CanSelectMultiple);
				Verify.AreEqual(true, selectionPeer.IsSelectionRequired);
			});
		}

		[TestMethod]
		public async Task VerifyPipsPagerButtonUIABehavior()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var pipsPager = new PipsPager();
				pipsPager.NumberOfPages = 5;
				Content = pipsPager;
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var rootPanel = VisualTreeHelper.GetChild(Content, 0) as StackPanel;
				var repeaterRootParent = VisualTreeHelper.GetChild(rootPanel, 1);
				ItemsRepeater repeater = null;
				while (repeater == null)
				{
					var nextChild = VisualTreeHelper.GetChild(repeaterRootParent, 0);
					repeater = nextChild as ItemsRepeater;
					repeaterRootParent = nextChild;
				}
				for (int i = 0; i < 5; i++)
				{
					var button = repeater.TryGetElement(i);
					Verify.IsNotNull(button);
					Verify.AreEqual(i + 1, button.GetValue(AutomationProperties.PositionInSetProperty));
					Verify.AreEqual(5, button.GetValue(AutomationProperties.SizeOfSetProperty));
				}
			});
		}

		[TestMethod]
		public async Task VerifyEmptyPagerDoesNotCrash()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				Content = new PipsPager();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				Verify.IsNotNull(Content);
			});
		}

		[TestMethod]
		public async Task VerifySelectedIndexChangedEventArgs()
		{
			PipsPager pager = null;
			var newIndex = -2;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				pager = new PipsPager();
				pager.SelectedIndexChanged += Pager_SelectedIndexChanged;
				Content = pager;

			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				VerifySelectionChanged(0);

				pager.NumberOfPages = 10;
				VerifySelectionChanged(0);

				pager.SelectedPageIndex = 9;
				VerifySelectionChanged(9);

				pager.SelectedPageIndex = 4;
				VerifySelectionChanged(4);
			});

			void Pager_SelectedIndexChanged(PipsPager sender, PipsPagerSelectedIndexChangedEventArgs args)
			{
				newIndex = sender.SelectedPageIndex;
			}

			void VerifySelectionChanged(int expectedNewIndex)
			{
				Verify.AreEqual(expectedNewIndex, newIndex, "Expected PreviousPageIndex:" + expectedNewIndex + ", actual: " + newIndex);
			}
		}
	}
}
