using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	partial class FlowLayout
	{
		private static void OnDependencyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
			=> ((FlowLayout)sender).OnPropertyChanged(args);

		#region LineAlignment - DP with common callback
		public static DependencyProperty LineAlignmentProperty { get; } = DependencyProperty.Register(
			"LineAlignment", typeof(FlowLayoutLineAlignment), typeof(FlowLayout), new FrameworkPropertyMetadata(default(FlowLayoutLineAlignment), OnDependencyPropertyChanged));

		public FlowLayoutLineAlignment LineAlignment
		{
			get { return (FlowLayoutLineAlignment)GetValue(LineAlignmentProperty); }
			set { SetValue(LineAlignmentProperty, value); }
		}
		#endregion

		#region MinColumnSpacing - DP with common callback
		public static DependencyProperty MinColumnSpacingProperty { get; } = DependencyProperty.Register(
			"MinColumnSpacing", typeof(double), typeof(FlowLayout), new FrameworkPropertyMetadata(default(double), OnDependencyPropertyChanged));

		public double MinColumnSpacing
		{
			get { return (double)GetValue(MinColumnSpacingProperty); }
			set { SetValue(MinColumnSpacingProperty, value); }
		}
		#endregion

		#region MinRowSpacing - DP with common callback
		public static DependencyProperty MinRowSpacingProperty { get; } = DependencyProperty.Register(
			"MinRowSpacing", typeof(double), typeof(FlowLayout), new FrameworkPropertyMetadata(default(double), OnDependencyPropertyChanged));

		public double MinRowSpacing
		{
			get { return (double)GetValue(MinRowSpacingProperty); }
			set { SetValue(MinRowSpacingProperty, value); }
		}
		#endregion

		#region Orientation - DP with common callback
		public static DependencyProperty OrientationProperty { get; } = DependencyProperty.Register(
			"Orientation", typeof(Orientation), typeof(FlowLayout), new FrameworkPropertyMetadata(default(Orientation), OnDependencyPropertyChanged));

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}
		#endregion
	}
}
