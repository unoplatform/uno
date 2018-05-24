using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;
using Uno.Extensions.ValueType;
using Uno.Logging;

namespace Windows.UI.Xaml.Media
{

	/// <summary>
	/// TranslateTransform: iOS part
	/// </summary>
	public partial class TranslateTransform
	{
		partial void SetX(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (View != null && !(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				Update();
			}
		}

		partial void SetY(DependencyPropertyChangedEventArgs args)
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
			return Matrix3x2.CreateTranslation((float)X, (float)Y);
		}
	}
}

