using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using Uno;
using Uno.UI;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Extensions;

public static class TransformExtensions
{
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
