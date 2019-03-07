using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Text;
using Android.Graphics;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// TranslateTransform : Android Part
	/// </summary>
	public partial class TranslateTransform
	{
		partial void SetX(DependencyPropertyChangedEventArgs args)
		{
			if (View != null)
			{
				View.TranslationX = ViewHelper.LogicalToPhysicalPixels(X);
			}
		}

		partial void SetY(DependencyPropertyChangedEventArgs args)
		{
			if (View != null)
			{
				View.TranslationY = ViewHelper.LogicalToPhysicalPixels(Y);
			}
		}

		/// <summary>
		/// Apply Transform whem attached
		/// </summary>
		protected override void OnAttachedToView()
		{
			base.OnAttachedToView();

			SetX(this.CreateInitialChangedEventArgs(XProperty));
			SetY(this.CreateInitialChangedEventArgs(YProperty));
		}


        internal override Android.Graphics.Matrix ToNativeTransform(Android.Graphics.Matrix targetMatrix = null, Size size = default(Size), bool isBrush = false)
        {
            if (targetMatrix == null)
            {
                targetMatrix = new Android.Graphics.Matrix();
            }

            targetMatrix.PostTranslate((float)X, (float)Y);

            return targetMatrix;
        }
    }
}


