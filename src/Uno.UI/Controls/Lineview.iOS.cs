#nullable disable

using System;
using System.Drawing;
using Uno.UI.Views;
using Uno.UI;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif

namespace Uno.UI.Controls
{
	public partial class LineView : UIView
    {
        public static LineView CreateHorizontal(float width, UIColor color)
        {
            return CreateHorizontal(width, color, 1f);
        }

        public static LineView CreateHorizontal(float width, UIColor color, float thickness)
        {
            return new LineView(new CGRect(0, 0, width, ViewHelper.GetConvertedPixel(thickness)))
            {
                BackgroundColor = color
            };
        }

        public static LineView CreateVertical(float height, UIColor color)
        {
            return CreateVertical(height, color, 1f);
        }

        public static LineView CreateVertical(float height, UIColor color, float thickness)
        {
			return new LineView(new CGRect(0, 0, ViewHelper.GetConvertedPixel(thickness), height))
            {
                BackgroundColor = color
            };
        }

		private LineView(CGRect frame)
			: base(frame) 
        {
        }
    }
}

