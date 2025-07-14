// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedIcon.properties.cpp, commit f4d781d

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AnimatedIcon
	{
		public IconSource FallbackIconSource
		{
			get => (IconSource)GetValue(FallbackIconSourceProperty);
			set => SetValue(FallbackIconSourceProperty, value);
		}

		public static DependencyProperty FallbackIconSourceProperty { get; } =
			DependencyProperty.Register(nameof(FallbackIconSource), typeof(IconSource), typeof(AnimatedIcon), new FrameworkPropertyMetadata(null, OnFallbackIconSourcePropertyChanged));

		public bool MirroredWhenRightToLeft
		{
			get => (bool)GetValue(MirroredWhenRightToLeftProperty);
			set => SetValue(MirroredWhenRightToLeftProperty, value);
		}

		public static DependencyProperty MirroredWhenRightToLeftProperty { get; } =
			DependencyProperty.Register(nameof(MirroredWhenRightToLeft), typeof(bool), typeof(AnimatedIcon), new FrameworkPropertyMetadata(false, OnMirroredWhenRightToLeftPropertyChanged));

		public IAnimatedVisualSource2 Source
		{
			get => (IAnimatedVisualSource2)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register(nameof(Source), typeof(IAnimatedVisualSource2), typeof(AnimatedIcon), new FrameworkPropertyMetadata(null, OnSourcePropertyChanged));

		public static string GetState(DependencyObject @object)
		{
			return (string)@object.GetValue(StateProperty);
		}

		public static void SetState(DependencyObject @object, string value)
		{
			@object.SetValue(StateProperty, value);
		}

		public static DependencyProperty StateProperty { get; } =
			DependencyProperty.RegisterAttached("State", typeof(string), typeof(AnimatedIcon), new FrameworkPropertyMetadata(string.Empty, OnAnimatedIconStatePropertyChanged));

		private static void OnFallbackIconSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as AnimatedIcon;
			owner?.OnFallbackIconSourcePropertyChanged(args);
		}

		private static void OnMirroredWhenRightToLeftPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as AnimatedIcon;
			owner?.OnMirroredWhenRightToLeftPropertyChanged(args);
		}

		private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as AnimatedIcon;
			owner?.OnSourcePropertyChanged(args);
		}
	}
}
