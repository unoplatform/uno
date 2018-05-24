using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
    /// <summary>
    /// ScaleTransform :  Based on the WinRT ScaleTransform
    /// https://msdn.microsoft.com/en-us/library/system.windows.media.skewtransform(v=vs.110).aspx
    /// </summary>
    public partial class SkewTransform : Transform
    {
        public double CenterY
        {
            get { return (double)this.GetValue(CenterYProperty); }
            set { this.SetValue(CenterYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CenterY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CenterYProperty =
            DependencyProperty.Register("CenterY", typeof(double), typeof(SkewTransform), new PropertyMetadata(0.0, OnCenterYChanged));
        private static void OnCenterYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var self = dependencyObject as SkewTransform;

            if (self != null)
            {
                self.SetCenterY(args);
            }
        }

        partial void SetCenterY(DependencyPropertyChangedEventArgs args);



        public double CenterX
        {
            get { return (double)this.GetValue(CenterXProperty); }
            set { this.SetValue(CenterXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CenterX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CenterXProperty =
            DependencyProperty.Register("CenterX", typeof(double), typeof(SkewTransform), new PropertyMetadata(0.0, OnCenterXChanged));
        private static void OnCenterXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var self = dependencyObject as SkewTransform;

            if (self != null)
            {
                self.SetCenterX(args);
            }
        }
        partial void SetCenterX(DependencyPropertyChangedEventArgs args);

        public double AngleX
        {
            get { return (double)this.GetValue(AngleXProperty); }
            set { this.SetValue(AngleXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AngleX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AngleXProperty =
            DependencyProperty.Register("AngleX", typeof(double), typeof(SkewTransform), new PropertyMetadata(0.0, OnAngleXChanged));
        private static void OnAngleXChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var self = dependencyObject as SkewTransform;

            if (self != null)
            {
                self.SetAngleX(args);
            }
        }
        partial void SetAngleX(DependencyPropertyChangedEventArgs args);




        public double AngleY
        {
            get { return (double)this.GetValue(AngleYProperty); }
            set { this.SetValue(AngleYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AngleY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AngleYProperty =
            DependencyProperty.Register("AngleY", typeof(double), typeof(SkewTransform), new PropertyMetadata(0.0, OnAngleYChanged));
        private static void OnAngleYChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var self = dependencyObject as SkewTransform;

            if (self != null)
            {
                self.SetAngleY(args);
            }
        }
        partial void SetAngleY(DependencyPropertyChangedEventArgs args);
    }


}


