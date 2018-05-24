using System.Numerics;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// RotateTransform: iOS part
	/// </summary>
	public partial class RotateTransform
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

		partial void SetAngle(DependencyPropertyChangedEventArgs args)
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
			return Matrix3x2.CreateRotation((float) ToRadians(Angle), centerPoint);
		}
	}
}

