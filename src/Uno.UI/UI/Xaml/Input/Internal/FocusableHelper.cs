// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusableHelper.h, FocusableHelper.cpp

#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;

namespace Uno.UI.Xaml.Input
{
	internal class FocusableHelper : IFocusable
	{
		private readonly Hyperlink _hyperlink;

		internal FocusableHelper(Hyperlink hyperlink)
		{
			_hyperlink = hyperlink;
		}

		internal static IFocusable? GetIFocusableForDO(DependencyObject? dependencyObject) =>
			(dependencyObject as Hyperlink)?.GetIFocusable();

		internal static bool IsFocusableDO(DependencyObject cdo) => cdo is Hyperlink;

		internal static FrameworkElement? GetContainingFrameworkElementIfFocusable(DependencyObject dependencyObject)
		{
			if (dependencyObject is Hyperlink hyperlink)
			{
				var contentStart = hyperlink.ContentStart;
				var contentStartVisualParent = contentStart.VisualParent;
				if (contentStartVisualParent != null)
				{
					return contentStartVisualParent;
				}
				else
				{
					return hyperlink.GetContainingFrameworkElement();
				}
			}
			return dependencyObject as FrameworkElement;
		}

		public bool IsFocusable() => _hyperlink.IsFocusable();

		public int GetTabIndex() => _hyperlink.TabIndex;

		public DependencyProperty GetXYFocusDownPropertyIndex() =>
			Hyperlink.XYFocusDownProperty;

		public DependencyProperty GetXYFocusDownNavigationStrategyPropertyIndex() =>
			Hyperlink.XYFocusDownNavigationStrategyProperty;

		public DependencyProperty GetXYFocusLeftPropertyIndex() =>
			Hyperlink.XYFocusLeftProperty;

		public DependencyProperty GetXYFocusLeftNavigationStrategyPropertyIndex() =>
			Hyperlink.XYFocusLeftNavigationStrategyProperty;

		public DependencyProperty GetXYFocusRightPropertyIndex() =>
			Hyperlink.XYFocusRightProperty;

		public DependencyProperty GetXYFocusRightNavigationStrategyPropertyIndex() =>
			Hyperlink.XYFocusRightNavigationStrategyProperty;

		public DependencyProperty GetXYFocusUpPropertyIndex() =>
			Hyperlink.XYFocusUpProperty;

		public DependencyProperty GetXYFocusUpNavigationStrategyPropertyIndex() =>
			Hyperlink.XYFocusUpNavigationStrategyProperty;

		public DependencyProperty GetFocusStatePropertyIndex() =>
			Hyperlink.FocusStateProperty;
		public void OnGotFocus(RoutedEventArgs args) =>
			_hyperlink.OnGotFocus(args);
		public void OnLostFocus(RoutedEventArgs args) =>
			_hyperlink.OnLostFocus(args);
	}
}
