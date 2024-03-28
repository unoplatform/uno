using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Android.Views;
using Android.Views.Animations;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Controls;

namespace Uno.UI.Media
{
	partial class NativeRenderTransformAdapter
	{
		private static readonly float[] _identity =
		{
			1, 0, 0,
			0, 1, 0,
			0, 0, 1
		};

		/// <summary>
		/// Gets the native transform that can be applied by the parent ViewGroup for the given child view
		/// </summary>
		internal Android.Graphics.Matrix Matrix { get; } = new Android.Graphics.Matrix();

		partial void Initialized()
		{
			// Apply the transform as soon as its been declared
			Update();

			UpdateParent(null, Owner.Parent);
		}

		public void UpdateParent(object oldParent, object newParent)
		{
			// The only way to apply a matrix transform on Android is by using the "static transform" on the parent `GroupView`.
			// Here we register on the parent this RenderTransform for on of its children.

			(oldParent as BindableView)?.UnregisterChildTransform(this);

			if (newParent is BindableView newBindableParent)
			{
				newBindableParent.RegisterChildTransform(this);
			}
			else if (newParent != null && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error(
					$"A RenderTransform was set on '{Owner}', but its parent is not a BindableView ({newParent.GetType().Name}), "
					+ "so the transform won't have any effect.");
			}
		}

		partial void Apply(bool isSizeChanged, bool isOriginChanged)
		{
			if (Transform.IsAnimating)
			{
				// The transform is animating, so we disable the matrix "static transform" and let the animation apply its values
				Matrix.SetValues(_identity);
				if (isSizeChanged)
				{
					// Even if while animating, we must update the PivotX and PivotY
					AnimatorFactory.UpdatePivotWhileAnimating(
						Transform,
						CurrentSize.Width * CurrentOrigin.X,
						CurrentSize.Height * CurrentOrigin.Y);
				}
			}
			else if (CurrentSize.IsEmpty || CurrentSize.Width == 0 || CurrentSize.Width == 0)
			{
				// Element was not measured yet (or unloaded ?), as on Android the Matrix includes the center,
				// it's dependent of the element size, so we cannot get a valid transform matrix.
				// Instead we just make sure to reset all values.
				Matrix.SetValues(_identity);
				Owner.PivotX = 0;
				Owner.PivotY = 0;
			}
			else
			{
				// The element is measured and not animating, we can update the "static transform" matrix.
				var matrix = Transform.ToMatrix(CurrentOrigin, CurrentSize);

				Matrix.SetValues(new[]
				{
					matrix.M11, matrix.M21, ViewHelper.LogicalToPhysicalPixels(matrix.M31),
					matrix.M12, matrix.M22, ViewHelper.LogicalToPhysicalPixels(matrix.M32),
					0, 0, 1
				});
				Owner.PivotX = 0;
				Owner.PivotY = 0;

				if (!isSizeChanged)
				{
					// A property on the Transform was change, request a redraw to apply the updated matrix
					Owner.Invalidate();
				}
			}
		}

		partial void Cleanup()
		{
			(Owner.Parent as BindableView)?.UnregisterChildTransform(this);

			Owner.Invalidate();
		}
	}
}
