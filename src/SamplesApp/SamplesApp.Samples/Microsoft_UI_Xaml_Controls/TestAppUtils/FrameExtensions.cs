// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TestAppUtils/NavigateToTestCommand.cs

using System;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace MUXControlsTestApp;

static class FrameExtensions
{
	public static void NavigateWithoutAnimation(this Frame frame, Type sourcePageType)
	{
		if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo, Uno.UI"))
		{
			frame.Navigate(sourcePageType, new SuppressNavigationTransitionInfo());
		}
		else
		{
			frame.Navigate(sourcePageType);
		}
	}

	public static void NavigateWithoutAnimation(this Frame frame, Type sourcePageType, object parameter)
	{
		if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo"))
		{
			frame.Navigate(sourcePageType, parameter, new SuppressNavigationTransitionInfo());
		}
		else
		{
			frame.Navigate(sourcePageType, parameter);
		}
	}
}
