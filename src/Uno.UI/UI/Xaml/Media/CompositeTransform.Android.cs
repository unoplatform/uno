using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Android.Graphics;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// CompositeTransform : Android Part
	/// </summary>
	public partial class CompositeTransform
	{
		/// <summary>
		/// Set the view of the inner transform
		/// </summary>
		protected override void OnAttachedToView()
		{
			base.OnAttachedToView();
			_innerTransform.View = View;
		}

		internal override Android.Graphics.Matrix ToNativeTransform(Android.Graphics.Matrix targetMatrix = null, Windows.Foundation.Size size = default(Windows.Foundation.Size), bool isBrush = false)
		{
			if (targetMatrix == null)
			{
				targetMatrix = new Android.Graphics.Matrix();
			}

			var pivot = this.GetPivot(size, isBrush);

			//Apply transformations in order
			targetMatrix.PostScale((float)ScaleX, (float)ScaleY, (float)pivot.X, (float)pivot.Y);

			targetMatrix.PostSkew((float)SkewX, (float)SkewY, (float)pivot.X, (float)pivot.Y);

			targetMatrix.PostRotate((float)Rotation, (float)pivot.X, (float)pivot.Y);

			targetMatrix.PostTranslate((float)TranslateX, (float)TranslateY);

			return targetMatrix;
		}
	}
}

