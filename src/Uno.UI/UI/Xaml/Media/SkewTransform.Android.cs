using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Text;
using Android.Graphics;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// SkewTransform : Android Part
	/// </summary>
	public partial class SkewTransform
	{
		partial void SetAngleX(DependencyPropertyChangedEventArgs args)
		{
			Update();
		}

		partial void SetAngleY(DependencyPropertyChangedEventArgs args)
		{
			Update();
		}

		protected override void Update()
		{
			base.Update();

			if (View != null)
			{
				View.PivotX = (float)(Origin.X * View.Width) + ViewHelper.LogicalToPhysicalPixels(CenterX);
				View.PivotY = (float)(Origin.Y * View.Height) + ViewHelper.LogicalToPhysicalPixels(CenterY);
				View.RotationY = (float)AngleY;
				View.RotationX = (float)AngleX;
			}
		}

		/// <summary>
		/// Apply Transform when attached
		/// </summary>
		protected override void OnAttachedToView()
		{
			base.OnAttachedToView();

			SetCenterX(this.CreateInitialChangedEventArgs(CenterXProperty));
			SetCenterY(this.CreateInitialChangedEventArgs(CenterYProperty));

			SetAngleX(this.CreateInitialChangedEventArgs(AngleXProperty));
			SetAngleY(this.CreateInitialChangedEventArgs(AngleYProperty));
		}

		internal override Android.Graphics.Matrix ToNativeTransform(Android.Graphics.Matrix targetMatrix = null, Size size = default(Size), bool isBrush = false)
		{
			if (targetMatrix == null)
			{
				targetMatrix = new Android.Graphics.Matrix();
			}

			var pivot = this.GetPivot(size, isBrush);

			targetMatrix.PostSkew((float)AngleX, (float)AngleY, (float)pivot.X, (float)pivot.Y);

			return targetMatrix;
		}
	}
}


