// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// IFocusable.h

using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Input
{
	internal interface IFocusable
	{
		bool IsFocusable();

		int GetTabIndex();

		DependencyProperty GetXYFocusDownPropertyIndex();

		DependencyProperty GetXYFocusDownNavigationStrategyPropertyIndex();

		DependencyProperty GetXYFocusLeftPropertyIndex();

		DependencyProperty GetXYFocusLeftNavigationStrategyPropertyIndex();

		DependencyProperty GetXYFocusRightPropertyIndex();

		DependencyProperty GetXYFocusRightNavigationStrategyPropertyIndex();

		DependencyProperty GetXYFocusUpPropertyIndex();

		DependencyProperty GetXYFocusUpNavigationStrategyPropertyIndex();

		DependencyProperty GetFocusStatePropertyIndex();

		void OnGotFocus(RoutedEventArgs args);

		void OnLostFocus(RoutedEventArgs args);
	}
}
