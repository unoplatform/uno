// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewTemplateSettings.cpp file from WinUI controls.
//

namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewTemplateSettings : global::Windows.UI.Xaml.DependencyObject
	{
		public  global::Windows.UI.Xaml.Visibility BackButtonVisibility
		{
			get => (global::Windows.UI.Xaml.Visibility)GetValue(BackButtonVisibilityProperty);
			internal set => SetValue(BackButtonVisibilityProperty, value);
		}

		public  global::Windows.UI.Xaml.Visibility LeftPaneVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)GetValue(LeftPaneVisibilityProperty);
			}
			internal set => SetValue(LeftPaneVisibilityProperty, value);
		}

		public  global::Windows.UI.Xaml.Visibility OverflowButtonVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)GetValue(OverflowButtonVisibilityProperty);
			}
			internal set => SetValue(OverflowButtonVisibilityProperty, value);
		}

		public  global::Windows.UI.Xaml.Visibility PaneToggleButtonVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)GetValue(PaneToggleButtonVisibilityProperty);
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

		public  global::Windows.UI.Xaml.Visibility TopPaneVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)GetValue(TopPaneVisibilityProperty);
			}
			internal set => SetValue(TopPaneVisibilityProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty BackButtonVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BackButtonVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Collapsed));

		public static global::Windows.UI.Xaml.DependencyProperty LeftPaneVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LeftPaneVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Visible));

		public static global::Windows.UI.Xaml.DependencyProperty OverflowButtonVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"OverflowButtonVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Collapsed));

		public static global::Windows.UI.Xaml.DependencyProperty PaneToggleButtonVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PaneToggleButtonVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Visible));

		public static global::Windows.UI.Xaml.DependencyProperty SingleSelectionFollowsFocusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SingleSelectionFollowsFocus", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(false));

		public static global::Windows.UI.Xaml.DependencyProperty TopPaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TopPadding", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(0.0));

		public static global::Windows.UI.Xaml.DependencyProperty TopPaneVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TopPaneVisibility", typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewTemplateSettings), 
			new FrameworkPropertyMetadata(Visibility.Collapsed));
	}
}
