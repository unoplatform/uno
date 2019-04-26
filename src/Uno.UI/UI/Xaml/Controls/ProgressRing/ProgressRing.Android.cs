using Android.Graphics;
using Uno.Extensions;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Uno.UI;
using Android.Views;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Represents a control that indicates that an operation is ongoing. The typical visual appearance is a ring-shaped "spinner" that cycles an animation as progress continues.
	/// See https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.progressring
	/// </summary>

	public partial class ProgressRing : BindableProgressBar
	{
		public ProgressRing()
		{
			// This is required to have multiple ProgressBar with different colors. 
			// Without this, changing one drawable would change all drawables (because they all have the same constant state).
			// http://stackoverflow.com/questions/7979440/android-cloning-a-drawable-in-order-to-make-a-statelistdrawable-with-filters
			IndeterminateDrawable.Mutate();
		}

		private static void OnForegroundChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var progressRing = dependencyObject as ProgressRing;
			//We only support SolidColorBrush for now
			var foregroundColor = progressRing.SelectOrDefault(r => r.Foreground as SolidColorBrush);

			if (progressRing != null && foregroundColor != null)
			{
				progressRing.IndeterminateDrawable?.SetColorFilter(foregroundColor.Color, PorterDuff.Mode.SrcIn);
			}
		}

		private static void OnIsActiveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var progressRing = dependencyObject as ProgressRing;
			var isActive = args.NewValue as bool?;

			if (progressRing != null && isActive != null)
			{
				if (isActive.Value)
				{
					progressRing.Visibility = Visibility.Visible;
				}
				else
				{
					progressRing.Visibility = Visibility.Collapsed;
				}
			}
		}
		protected override void OnDraw(Android.Graphics.Canvas canvas)
		{
			base.OnDraw(canvas);
			if (!IsActive)
			{
				return;
			}

			// This is required for progress ring to visually update when inside transformed ancestor on hardware-accelerated devices
			((this as View).Parent as UnoViewGroup).InvalidateTransformedHierarchy();
		}
	}
}
