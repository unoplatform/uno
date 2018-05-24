using Android.Graphics;
using Uno.UI;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Text;

namespace Windows.UI.Xaml.Media
{

	/// <summary>
	/// ScaleTransform : Android Part
	/// </summary>
	public partial class ScaleTransform
	{


		partial void SetScaleX(DependencyPropertyChangedEventArgs args)
		{
			if (View != null)
			{
				View.PivotX = (float)(Origin.X * View.MeasuredWidth) + ViewHelper.LogicalToPhysicalPixels(CenterX);
				View.PivotY = (float)(Origin.Y * View.MeasuredHeight) + ViewHelper.LogicalToPhysicalPixels(CenterY);
				View.ScaleX = (float)ScaleX;
			}
		}

		partial void SetScaleY(DependencyPropertyChangedEventArgs args)
		{
			if (View != null)
			{
				View.ScaleY = (float)ScaleY;
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

			SetScaleX(this.CreateInitialChangedEventArgs(ScaleXProperty));
			SetScaleY(this.CreateInitialChangedEventArgs(ScaleYProperty));
		}

		internal override Android.Graphics.Matrix ToNativeTransform(Android.Graphics.Matrix targetMatrix = null, Size size = new Size(), bool isBrush = false)
		{
			if (targetMatrix == null)
			{
				targetMatrix = new Android.Graphics.Matrix();
			}

			var pivot = this.GetPivot(size, isBrush);

			targetMatrix.PostScale((float)ScaleX, (float)ScaleY, (float)pivot.X, (float)pivot.Y);

			return targetMatrix;
		}

	}


}


