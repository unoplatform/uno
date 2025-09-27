﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Private.Controls;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Media.Animation;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;

namespace MUXControlsTestApp;

[Sample("Scrolling")]
public sealed partial class ScrollViewPage : TestPage
{
	public ScrollViewPage()
	{
		LogController.InitializeLogging();
		this.InitializeComponent();

		navigateToSimpleContents.Click += delegate { Frame.NavigateWithoutAnimation(typeof(ScrollViewsWithSimpleContentsPage), 0); };
		navigateToDynamic.Click += delegate { Frame.NavigateWithoutAnimation(typeof(ScrollViewDynamicPage), 0); };
		navigateToScrollControllers.Click += delegate { Frame.NavigateWithoutAnimation(typeof(ScrollViewWithScrollControllersPage), 0); };
		navigateToRTL.Click += delegate { Frame.NavigateWithoutAnimation(typeof(ScrollViewWithRTLFlowDirectionPage), 0); };
		navigateToKeyboardAndGamepadNavigation.Click += delegate { Frame.NavigateWithoutAnimation(typeof(ScrollViewKeyboardAndGamepadNavigationPage), 0); };
		navigateToBringIntoView.Click += delegate { Frame.NavigateWithoutAnimation(typeof(ScrollViewBringIntoViewPage), 0); };
		navigateToBlank.Click += delegate { Frame.NavigateWithoutAnimation(typeof(ScrollViewBlankPage), 0); };
	}

	private void CmbScrollViewOutputDebugStringLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		//MUXControlsTestHooks.SetOutputDebugStringLevelForType(
		//	"ScrollView",
		//	cmbScrollViewOutputDebugStringLevel.SelectedIndex == 1 || cmbScrollViewOutputDebugStringLevel.SelectedIndex == 2,
		//	cmbScrollViewOutputDebugStringLevel.SelectedIndex == 2);

		//MUXControlsTestHooks.SetOutputDebugStringLevelForType(
		//	"ScrollPresenter",
		//	cmbScrollViewOutputDebugStringLevel.SelectedIndex == 1 || cmbScrollViewOutputDebugStringLevel.SelectedIndex == 2,
		//	cmbScrollViewOutputDebugStringLevel.SelectedIndex == 2);
	}
}
