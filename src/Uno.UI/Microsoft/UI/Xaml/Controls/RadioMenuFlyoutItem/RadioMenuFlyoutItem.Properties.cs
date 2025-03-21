// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RadioMenuFlyoutItem.properties.cpp, commit d6634d1bb82697c9b700fff5adf4e9484e0952c5
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class RadioMenuFlyoutItem
	{
		public static bool GetAreCheckStatesEnabled(DependencyObject obj) => (bool)obj.GetValue(AreCheckStatesEnabledProperty);
		public static void SetAreCheckStatesEnabled(DependencyObject obj, bool value) => obj.SetValue(AreCheckStatesEnabledProperty, value);

		public static DependencyProperty AreCheckStatesEnabledProperty { get; } =
			DependencyProperty.RegisterAttached("AreCheckStatesEnabled", typeof(bool), typeof(RadioMenuFlyoutItem), new FrameworkPropertyMetadata(false, OnAreCheckStatesEnabledPropertyChanged));

		public string GroupName
		{
			get { return (string)GetValue(GroupNameProperty); }
			set { SetValue(GroupNameProperty, value); }
		}

		public static DependencyProperty GroupNameProperty { get; } =
			DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(RadioMenuFlyoutItem), new FrameworkPropertyMetadata(string.Empty, (s, e) => (s as RadioMenuFlyoutItem)?.OnPropertyChanged(e)));

		public new bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public static new DependencyProperty IsCheckedProperty { get; } =
			DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(RadioMenuFlyoutItem), new FrameworkPropertyMetadata(false, (s, e) => (s as RadioMenuFlyoutItem)?.OnPropertyChanged(e)));
	}
}
