using System;
using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// SkewTransform: iOS part
	/// </summary>
	public partial class SkewTransform
	{
		partial void SetCenterY(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (View != null && !(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				Update();
			}
		}

		partial void SetCenterX(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (View != null && !(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				Update();
			}
		}

		partial void SetAngleX(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (View != null && !(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				Update();
			}
		}


		partial void SetAngleY(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (View != null && !(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				Update();
			}
		}

		internal override Matrix3x2 ToNativeTransform(Size size)
		{
			var centerPoint = new Vector2((float) CenterX, (float) CenterY);
			return Matrix3x2.CreateSkew((float)MathEx.ToRadians(AngleX), (float)MathEx.ToRadians(AngleY), centerPoint);
		}
	}
}

