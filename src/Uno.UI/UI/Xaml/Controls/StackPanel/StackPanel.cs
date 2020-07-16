using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using System.Collections.Specialized;
#if XAMARIN_ANDROID
using Android.Views;
#elif XAMARIN_IOS
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class StackPanel : Panel
	{

		#region Orientation DependencyProperty

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
		public static DependencyProperty OrientationProperty { get ; } =
			DependencyProperty.Register(
				"Orientation",
				typeof(Orientation),
				typeof(StackPanel),
				new FrameworkPropertyMetadata(Orientation.Vertical, (s, e) => ((StackPanel)s)?.OnOrientationChanged(e))
			);


		private void OnOrientationChanged(DependencyPropertyChangedEventArgs e)
		{
			this.InvalidateMeasure();
		}

		#endregion


		/// <summary>
		/// Gets or sets a uniform distance (in pixels) between stacked items. It is applied in the direction of the StackPanel's Orientation.
		/// </summary>
		public double Spacing
		{
			get => (double)GetValue(SpacingProperty);
			set => SetValue(SpacingProperty, value);
		}

		public static DependencyProperty SpacingProperty { get ; } =
			DependencyProperty.Register(
				name: "Spacing", 
				propertyType: typeof(double), 
				ownerType: typeof(StackPanel),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: 0.0,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				));

		protected override bool? IsWidthConstrainedInner(View requester)
		{
			if (requester != null && Orientation == Orientation.Horizontal)
			{
				return false;
			}

			return this.IsWidthConstrainedSimple();
		}

		protected override bool? IsHeightConstrainedInner(View requester)
		{
			if (requester != null && Orientation == Orientation.Vertical)
			{
				return false;
			}

			return this.IsHeightConstrainedSimple();
		}
	}
}
