using Android.Graphics;
using Uno.UI;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// RotateTransform : Android Part
	/// </summary>
	public partial class RotateTransform
	{
		partial void OnOriginChanged(Foundation.Point origin)
		{
			Update();
		}

		partial void SetCenterX(DependencyPropertyChangedEventArgs args)
		{
			Update();
		}

		partial void SetCenterY(DependencyPropertyChangedEventArgs args)
		{
			Update();
		}

		partial void SetAngle(DependencyPropertyChangedEventArgs args)
		{
			Update();
		}

		protected override void Update()
		{
			base.Update();

			if(View != null)
			{
				View.PivotX = (float)(Origin.X * View.MeasuredWidth) + ViewHelper.LogicalToPhysicalPixels(CenterX);
				View.PivotY = (float)(Origin.Y * View.MeasuredHeight) + ViewHelper.LogicalToPhysicalPixels(CenterY);
				View.Rotation = (float)Angle;
			}
		}

		/// <summary>
		/// Apply Transform whem attached
		/// </summary>
		protected override void OnAttachedToView()
		{
			base.OnAttachedToView();

			SetCenterX(this.CreateInitialChangedEventArgs(CenterXProperty));
			SetCenterY(this.CreateInitialChangedEventArgs(CenterYProperty));
			SetAngle(this.CreateInitialChangedEventArgs(AngleProperty));
		}

        internal override Android.Graphics.Matrix ToNativeTransform(Android.Graphics.Matrix targetMatrix = null, Size size = default(Size), bool isBrush = false)
        {
            if (targetMatrix == null)
            {
                targetMatrix = new Android.Graphics.Matrix();
            }

			var pivot = this.GetPivot(size, isBrush);

            targetMatrix.PostRotate((float)Angle, (float)pivot.X, (float)pivot.Y);

            return targetMatrix;
        }

    }


}


