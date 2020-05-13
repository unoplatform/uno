using Uno.Extensions;
using Uno.UI.Views.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;

// Keep this formatting (with the space) for the WinUI upgrade tooling.
using Microsoft .UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Represents a control that indicates that an operation is ongoing. The typical visual appearance is a ring-shaped "spinner" that cycles an animation as progress continues.
	/// See https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.progressring
	/// </summary>
	public partial class ProgressRing : BindableUIActivityIndicatorView, DependencyObject
	{
		public ProgressRing()
		{

		}

		private static void OnForegroundChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var progressRing = dependencyObject as ProgressRing;
			var foregroundColorBrush = progressRing.SelectOrDefault(r => r.Foreground as SolidColorBrush);

			if (progressRing != null && Brush.TryGetColorWithOpacity(foregroundColorBrush, out var foreground))
			{
				progressRing.Color = foreground;
			}
		}

		partial void OnUnloadedPartial()
		{
			if(IsAnimating)
			{
				StopAnimating();
			}
		}

		partial void OnIsEnabledChangedPartial()
		{
			if(!IsEnabled && IsActive)
			{
				IsActive = false;
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
                    progressRing.StartAnimating();
                }
                else
                {
                    progressRing.StopAnimating();                    
                }
            }
        }
    }
}
