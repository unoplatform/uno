// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference AnimatedIconSource.cpp & AnimatedIconSource.properties.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AnimatedIconSource : IconSource
	{
		public IconSource FallbackIconSource
		{
			get => (IconSource)GetValue(FallbackIconSourceProperty);
			set => SetValue(FallbackIconSourceProperty, value);
		}

		public static DependencyProperty FallbackIconSourceProperty { get; } =
			DependencyProperty.Register(nameof(FallbackIconSource), typeof(IconSource), typeof(AnimatedIconSource), new FrameworkPropertyMetadata(null, OnPropertyChanged));

		public bool MirroredWhenRightToLeft
		{
			get => (bool)GetValue(MirroredWhenRightToLeftProperty);
			set => SetValue(MirroredWhenRightToLeftProperty, value);
		}

		public static DependencyProperty MirroredWhenRightToLeftProperty { get; } =
			DependencyProperty.Register(nameof(MirroredWhenRightToLeft), typeof(bool), typeof(AnimatedIconSource), new FrameworkPropertyMetadata(false, OnPropertyChanged));

		public IAnimatedVisualSource2 Source
		{
			get => (IAnimatedVisualSource2)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register(nameof(Source), typeof(IAnimatedVisualSource2), typeof(AnimatedIconSource), new FrameworkPropertyMetadata(null, OnPropertyChanged));

#if !HAS_UNO_WINUI
		private
#endif
		protected override IconElement CreateIconElementCore()
		{
			AnimatedIcon animatedIcon = new AnimatedIcon();
			if (Source is { } source)
			{
				animatedIcon.Source = source;
			}
			if (FallbackIconSource is { } fallbackIconSource)
			{
				animatedIcon.FallbackIconSource = fallbackIconSource;
			}
			if (Foreground is { } newForeground)
			{
				animatedIcon.Foreground = newForeground;
			}
			animatedIcon.MirroredWhenRightToLeft = MirroredWhenRightToLeft;

			return animatedIcon;
		}

#if !HAS_UNO_WINUI
		private
#endif
		protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
		{
			if (iconSourceProperty == SourceProperty)
			{
				return AnimatedIcon.SourceProperty;
			}
			else if (iconSourceProperty == FallbackIconSourceProperty)
			{
				return AnimatedIcon.FallbackIconSourceProperty;
			}
			else if (iconSourceProperty == MirroredWhenRightToLeftProperty)
			{
				return AnimatedIcon.MirroredWhenRightToLeftProperty;
			}

			return base.GetIconElementPropertyCore(iconSourceProperty);
		}
	}
}
