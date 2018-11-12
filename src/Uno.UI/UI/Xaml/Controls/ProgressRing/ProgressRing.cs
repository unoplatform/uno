#if XAMARIN || __WASM__
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
    public partial class ProgressRing
    {
#if !NETSTANDARD2_0 && !__MACOS__
#region Foreground

		/// <summary>
		/// Gets or sets a brush that describes the foreground color (only SolidColorBrush is supported for the moment) 
		/// </summary>
		public
#if __ANDROID_23__
        new
#endif
        Brush Foreground
        {
            get { return (Brush)this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(Brush), typeof(ProgressRing), new PropertyMetadata(SolidColorBrushHelper.Black, OnForegroundChanged));

#endregion
#endif

#region IsActive

        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="ProgressRing"/> is showing progress.
        /// </summary>
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(ProgressRing), new PropertyMetadata(false, OnIsActiveChanged));

#endregion
    }
}
#endif
