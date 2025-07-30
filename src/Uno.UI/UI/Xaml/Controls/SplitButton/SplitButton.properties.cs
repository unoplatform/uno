// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SplitButton.properties.cpp, tag winui3/release/1.4.2

using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SplitButton
	{
		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public object CommandParameter
		{
			get { return (object)GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		public FlyoutBase Flyout
		{
			get { return (FlyoutBase)GetValue(FlyoutProperty); }
			set { SetValue(FlyoutProperty, value); }
		}

		public static DependencyProperty CommandProperty { get; } =
			DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SplitButton), new FrameworkPropertyMetadata(null, OnCommandChanged));

		public static DependencyProperty CommandParameterProperty { get; } =
			DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(SplitButton), new FrameworkPropertyMetadata(null, OnCommandParameterChanged));

		public static DependencyProperty FlyoutProperty { get; } =
			DependencyProperty.Register(nameof(Flyout), typeof(FlyoutBase), typeof(SplitButton), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.LogicalChild, OnFlyoutChanged));

		private static void OnCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (SplitButton)sender;
			owner.OnPropertyChanged(args);
		}

		private static void OnCommandParameterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (SplitButton)sender;
			owner.OnPropertyChanged(args);
		}


		private static void OnFlyoutChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (SplitButton)sender;
			owner.OnPropertyChanged(args);
		}
	}
}
