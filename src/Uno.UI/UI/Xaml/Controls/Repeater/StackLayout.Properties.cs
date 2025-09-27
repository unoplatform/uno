using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	partial class StackLayout
	{
		private static void OnDependencyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
			=> ((StackLayout)sender).OnPropertyChanged(args);

		#region Orientation - DP with common callback
		public static DependencyProperty OrientationProperty { get; } = DependencyProperty.Register(
			"Orientation", typeof(Orientation), typeof(StackLayout), new FrameworkPropertyMetadata(default(Orientation), OnDependencyPropertyChanged));

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}
		#endregion

		#region Spacing - DP with common callback
		public static DependencyProperty SpacingProperty { get; } = DependencyProperty.Register(
			"Spacing", typeof(double), typeof(StackLayout), new FrameworkPropertyMetadata(default(double), OnDependencyPropertyChanged));

		public double Spacing
		{
			get { return (double)GetValue(SpacingProperty); }
			set { SetValue(SpacingProperty, value); }
		}
		#endregion

		#region DisableVirtualization
		public static DependencyProperty DisableVirtualizationProperty { get; } = DependencyProperty.Register(
			"DisableVirtualization ", typeof(bool), typeof(StackLayout), new FrameworkPropertyMetadata(default(bool), OnDependencyPropertyChanged));

		public bool DisableVirtualization
		{
			get { return (bool)GetValue(DisableVirtualizationProperty); }
			set { SetValue(DisableVirtualizationProperty, value); }
		}
		#endregion
	}
}
