// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Private.Controls;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("SelectorBar")]
	public sealed partial class SelectorBarPage : TestPage
	{
		public SelectorBarPage()
		{
			LogController.InitializeLogging();
			this.InitializeComponent();

			navigateToSummary.Click += delegate { Frame.NavigateWithoutAnimation(typeof(SelectorBarSummaryPage), 0); };
			navigateToSample.Click += delegate { Frame.NavigateWithoutAnimation(typeof(SelectorBarSamplePage), 0); };
		}

		private void CmbSelectorBarOutputDebugStringLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//if (chkSelectorBar != null && chkSelectorBar.IsChecked == true)
			//{
			//	MUXControlsTestHooks.SetOutputDebugStringLevelForType(
			//		"SelectorBar",
			//		cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 1 || cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 2,
			//		cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 2);
			//}

			//if (chkItemsView != null && chkItemsView.IsChecked == true)
			//{
			//	MUXControlsTestHooks.SetOutputDebugStringLevelForType(
			//		"ItemsView",
			//		cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 1 || cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 2,
			//		cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 2);
			//}

			//if (chkItemContainer != null && chkItemContainer.IsChecked == true)
			//{
			//	MUXControlsTestHooks.SetOutputDebugStringLevelForType(
			//		"ItemContainer",
			//		cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 1 || cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 2,
			//		cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 2);
			//}

			//if (chkItemsRepeater != null && chkItemsRepeater.IsChecked == true)
			//{
			//	MUXControlsTestHooks.SetOutputDebugStringLevelForType(
			//		"ItemsRepeater",
			//		cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 1 || cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 2,
			//		cmbSelectorBarOutputDebugStringLevel.SelectedIndex == 2);
			//}
		}
	}
}
