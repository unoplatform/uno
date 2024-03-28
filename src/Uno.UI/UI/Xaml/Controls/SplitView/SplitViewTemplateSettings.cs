using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public sealed partial class SplitViewTemplateSettings : DependencyObject
	{
		internal SplitViewTemplateSettings()
		{
		}

		public static DependencyProperty CompactPaneGridLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(CompactPaneGridLength),
				typeof(GridLength),
				typeof(SplitViewTemplateSettings),
				new FrameworkPropertyMetadata(default(GridLength))
			);

		public GridLength CompactPaneGridLength
		{
			get => (GridLength)GetValue(CompactPaneGridLengthProperty);
			internal set => SetValue(CompactPaneGridLengthProperty, value);
		}

		public static DependencyProperty OpenPaneGridLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(OpenPaneGridLength),
				typeof(GridLength),
				typeof(SplitViewTemplateSettings),
				new FrameworkPropertyMetadata(default(GridLength), propertyChangedCallback: (dependencyObject, args) =>
				{

				}));

		public GridLength OpenPaneGridLength
		{
			get => (GridLength)GetValue(OpenPaneGridLengthProperty);
			internal set => SetValue(OpenPaneGridLengthProperty, value);
		}

		public static DependencyProperty NegativeOpenPaneLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(NegativeOpenPaneLength),
				typeof(double),
				typeof(SplitViewTemplateSettings),
				new FrameworkPropertyMetadata(default(GridLength))
			);

		public double NegativeOpenPaneLength
		{
			get => (double)GetValue(NegativeOpenPaneLengthProperty);
			internal set => SetValue(NegativeOpenPaneLengthProperty, value);
		}

		public static DependencyProperty NegativeOpenPaneLengthMinusCompactLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(NegativeOpenPaneLengthMinusCompactLength),
				typeof(double),
				typeof(SplitViewTemplateSettings),
				new FrameworkPropertyMetadata(default(GridLength))
			);

		public double NegativeOpenPaneLengthMinusCompactLength
		{
			get => (double)GetValue(NegativeOpenPaneLengthMinusCompactLengthProperty);
			internal set => SetValue(NegativeOpenPaneLengthMinusCompactLengthProperty, value);
		}

		public static DependencyProperty OpenPaneLengthMinusCompactLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(OpenPaneLengthMinusCompactLength),
				typeof(double),
				typeof(SplitViewTemplateSettings),
				new FrameworkPropertyMetadata(default(GridLength))
			);

		public double OpenPaneLengthMinusCompactLength
		{
			get => (double)GetValue(OpenPaneLengthMinusCompactLengthProperty);
			internal set => SetValue(OpenPaneLengthMinusCompactLengthProperty, value);
		}

		public static DependencyProperty OpenPaneLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(OpenPaneLength),
				typeof(double),
				typeof(SplitViewTemplateSettings),
				new FrameworkPropertyMetadata(default(GridLength))
			);

		public double OpenPaneLength
		{
			get => (double)GetValue(OpenPaneLengthProperty);
			internal set => SetValue(OpenPaneLengthProperty, value);
		}

		public static DependencyProperty CompactPaneLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(CompactPaneLength),
				typeof(double),
				typeof(SplitViewTemplateSettings),
				new FrameworkPropertyMetadata(default(GridLength))
			);

		public double CompactPaneLength
		{
			get => (double)GetValue(CompactPaneLengthProperty);
			internal set => SetValue(CompactPaneLengthProperty, value);
		}
	}
}
