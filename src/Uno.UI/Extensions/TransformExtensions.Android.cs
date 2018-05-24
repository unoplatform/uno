using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using Uno;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	public static class TransformExtensions
	{
		internal static DependencyPropertyChangedEventArgs CreateInitialChangedEventArgs(this Transform transform, DependencyProperty property)
		{
			return new DependencyPropertyChangedEventArgs(
                property,
                null,
				DependencyPropertyValuePrecedences.DefaultValue,
				transform.GetValue(property),
				transform.GetCurrentHighestValuePrecedence(property)
			);
		}

		internal static Vector2 GetPivot(this Transform transform, Windows.Foundation.Size size, bool isBrush)
		{
			var center = transform.GetCenter() ?? Vector2.Zero;
			var vectorSize = new Vector2((float)size.Width, (float)size.Height);
			var vectorOrigin = new Vector2((float)transform.Origin.X, (float)transform.Origin.Y);

			// When used as UIElement.RenderTransform, CenterX/Y are interpreted as absolute pixel values; when used as ImageBrush.RelativeTransform,
			// they're interpreted as fractional values.
			var centerOffset = isBrush ?
				center * vectorSize :
				ViewHelper.LogicalToPhysicalPixels(center);

			var pivot = vectorOrigin * vectorSize + centerOffset;

			return pivot;
		}

		internal static Vector2? GetCenter(this Transform transform)
		{
			var asRotate = transform as RotateTransform;
			if (asRotate != null)
			{
				return new Vector2((float)asRotate.CenterX, (float)asRotate.CenterY);
			}

			var asComposite = transform as CompositeTransform;
			if (asComposite != null)
			{
				return new Vector2((float)asComposite.CenterX, (float)asComposite.CenterY);
			}

			var asScale = transform as ScaleTransform;
			if (asScale != null)
			{
				return new Vector2((float)asScale.CenterX, (float)asScale.CenterY);
			}

			var asSkew = transform as SkewTransform;
			if (asSkew != null)
			{
				return new Vector2((float)asSkew.CenterX, (float)asSkew.CenterY);
			}

			return null;
		}
	}
}
