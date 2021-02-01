// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewTemplateSettings.cpp file from WinUI controls.
//

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
#endif

namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewTemplateSettings : DependencyObject
	{
		public Visibility BackButtonVisibility
		{
			get => (Visibility)GetValue(BackButtonVisibilityProperty);
			internal set => SetValue(BackButtonVisibilityProperty, value);
		}

		public Visibility LeftPaneVisibility
		{
			get
			{
				return (Visibility)GetValue(LeftPaneVisibilityProperty);
			}
			internal set => SetValue(LeftPaneVisibilityProperty, value);
		}

		public Visibility OverflowButtonVisibility
		{
			get
			{
				return (Visibility)GetValue(OverflowButtonVisibilityProperty);
			}
			internal set => SetValue(OverflowButtonVisibilityProperty, value);
		}

		public Visibility PaneToggleButtonVisibility
		{
			get
			{
				return (Visibility)GetValue(PaneToggleButtonVisibilityProperty);
			}
			internal set => SetValue(PaneToggleButtonVisibilityProperty, value);
		}

		public  bool SingleSelectionFollowsFocus
		{
			get
			{
				return (bool)GetValue(SingleSelectionFollowsFocusProperty);
			}
			internal set => SetValue(SingleSelectionFollowsFocusProperty, value);
		}

		public  double TopPadding
		{
			get
			{
				return (double)GetValue(TopPaddingProperty);
			}
			internal set => SetValue(TopPaddingProperty, value);
		}

		public Visibility TopPaneVisibility
		{
			get
			{
				return (Visibility)GetValue(TopPaneVisibilityProperty);
			}
			internal set => SetValue(TopPaneVisibilityProperty, value);
		}

		public static DependencyProperty BackButtonVisibilityProperty { get; } =
		DependencyProperty.Register(
			"BackButtonVisibility", typeof(Visibility), 
			typeof(NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Collapsed));

		public static DependencyProperty LeftPaneVisibilityProperty { get; } =
		DependencyProperty.Register(
			"LeftPaneVisibility", typeof(Visibility), 
			typeof(NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Visible));

		public static DependencyProperty OverflowButtonVisibilityProperty { get; } =
		DependencyProperty.Register(
			"OverflowButtonVisibility", typeof(Visibility), 
			typeof(NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Collapsed));

		public static DependencyProperty PaneToggleButtonVisibilityProperty { get; } =
		DependencyProperty.Register(
			"PaneToggleButtonVisibility", typeof(Visibility), 
			typeof(NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Visible));

		public static DependencyProperty SingleSelectionFollowsFocusProperty { get; } =
		DependencyProperty.Register(
			"SingleSelectionFollowsFocus", typeof(bool), 
			typeof(NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(false));

		public static DependencyProperty TopPaddingProperty { get; } =
		DependencyProperty.Register(
			"TopPadding", typeof(double), 
			typeof(NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(0.0));

		public static DependencyProperty TopPaneVisibilityProperty { get; } =
		DependencyProperty.Register(
			"TopPaneVisibility", typeof(Visibility), 
			typeof(NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Collapsed));
	}
}
