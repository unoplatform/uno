using CoreAnimation;
using CoreGraphics;
using Foundation;
using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UIKit;

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

        protected override void OnAttachedToView()
        {
            base.OnAttachedToView();

            SetNeedsUpdate();
        }

		internal override CGAffineTransform ToNativeTransform(CGSize size, bool withCenter = true)
		{
			var skew = CGAffineTransform.MakeIdentity();

			skew.yx = (float)Math.Tan(ToRadians(AngleY));
            skew.xy = (float)Math.Tan(ToRadians(AngleX));

			return skew;
		}
    }
}

