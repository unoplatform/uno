// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using System;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Common;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{

	[TestClass]
	public class PagerControlTests : MUXApiTestBase
	{

		[TestMethod]
		public void VerifyAutomationPeerBehavior()
		{
			RunOnUIThread.Execute(() =>
			{
				var pagerControl = new PagerControl();
				pagerControl.NumberOfPages = 5;
				Content = pagerControl;

				var peer = PagerControlAutomationPeer.CreatePeerForElement(pagerControl);
				var selectionPeer = peer as ISelectionProvider;
				Verify.AreEqual(false, selectionPeer.CanSelectMultiple);
				Verify.AreEqual(true, selectionPeer.IsSelectionRequired);
				Verify.AreEqual(AutomationLandmarkType.Navigation, peer.GetLandmarkType());
			});
		}

#if !__WASM__ && !__ANDROID__// IdleSynchronizer.Wait(); is not supported on WASM
		[TestMethod]
		public void VerifyNumberPanelButtonUIABehavior()
		{
			RunOnUIThread.Execute(() => {
				var pagerControl = new PagerControl();
				pagerControl.NumberOfPages = 5;
				pagerControl.DisplayMode = PagerControlDisplayMode.ButtonPanel;
				Content = pagerControl;
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				var rootGrid = VisualTreeHelper.GetChild(Content, 0) as Grid;
				var repeater = VisualTreeHelper.GetChild(rootGrid, 2) as ItemsRepeater;

				for (int i = 0; i < 5; i++)
				{
					var button = repeater.TryGetElement(i);
					Verify.IsNotNull(button);
					Verify.AreEqual(i + 1, button.GetValue(AutomationProperties.PositionInSetProperty));
					Verify.AreEqual(5, button.GetValue(AutomationProperties.SizeOfSetProperty));
				}
			});
		}
#endif

		[TestMethod]
		[Ignore("ComboBox version of the control is slow on Android/iOS (issue #3144)")]
		public void VerifyComboBoxItemsListNormal()
		{
			PagerControl control = null;
			RunOnUIThread.Execute(() =>
			{
				control = new PagerControl();
				control.NumberOfPages = 5;
				control.DisplayMode = PagerControlDisplayMode.ComboBox;
				Content = control;
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(control.NumberOfPages, control.TemplateSettings.Pages.Count);
				for (int i = 0; i < control.NumberOfPages; i++)
				{
					Verify.AreEqual(i + 1, control.TemplateSettings.Pages[i]);
				}
				control.NumberOfPages = 100;
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(control.NumberOfPages, control.TemplateSettings.Pages.Count);
				for (int i = 0; i < control.NumberOfPages; i++)
				{
					Verify.AreEqual(i + 1, control.TemplateSettings.Pages[i]);
				}
			});
		}

		[TestMethod]
		[Ignore("ComboBox version of the control is slow on Android/iOS (issue #3144)")]
		public void VerifyComboBoxItemsInfiniteItems()
		{
			PagerControl control = null;
			RunOnUIThread.Execute(() =>
			{
				control = new PagerControl();
				control.NumberOfPages = 5;
				control.DisplayMode = PagerControlDisplayMode.ComboBox;
				Content = control;
				control.NumberOfPages = -1;
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(100, control.TemplateSettings.Pages.Count);
				for (int i = 0; i < 100; i++)
				{
					Verify.AreEqual(i + 1, control.TemplateSettings.Pages[i]);
				}
				control.NumberOfPages = 200;
				control.UpdateLayout();
				control.NumberOfPages = -1;
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(200, control.TemplateSettings.Pages.Count);
				for (int i = 0; i < 200; i++)
				{
					Verify.AreEqual(i + 1, control.TemplateSettings.Pages[i]);
				}
			});
		}

		[TestMethod]
		public void VerifyEmptyPagerDoesNotCrash()
		{
			RunOnUIThread.Execute(() =>
			{
				Content = new PagerControl();
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsNotNull(Content);
			});
		}

		[TestMethod]
		public void VerifySelectedIndexChangedEventArgs()
		{
			PagerControl pager = null;
			var previousIndex = -2;
			var newIndex = -2;
			RunOnUIThread.Execute(() =>
			{
				pager = new PagerControl();
				pager.SelectedIndexChanged += Pager_SelectedIndexChanged;
				Content = pager;

			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				VerifySelectionChanged(-1, 0);

				pager.NumberOfPages = 10;
				VerifySelectionChanged(-1, 0);

				pager.SelectedPageIndex = 9;
				VerifySelectionChanged(0, 9);

				pager.SelectedPageIndex = 4;
				VerifySelectionChanged(9, 4);
			});

			void Pager_SelectedIndexChanged(PagerControl sender, PagerControlSelectedIndexChangedEventArgs args)
			{
				previousIndex = args.PreviousPageIndex;
				newIndex = args.NewPageIndex;
			}

			void VerifySelectionChanged(int expectedPreviousIndex, int expectedNewIndex)
			{
				Verify.AreEqual(expectedPreviousIndex, previousIndex, "Expected PreviousPageIndex:" + expectedPreviousIndex + ", actual: " + previousIndex);
				Verify.AreEqual(expectedNewIndex, newIndex, "Expected PreviousPageIndex:" + expectedNewIndex + ", actual: " + newIndex);
			}
		}
	}
}
