using CoreAnimation;
using Foundation;
using Uno.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using CoreGraphics;
using System.Drawing;
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

		protected override void OnAttachedToView()
		{
			base.OnAttachedToView();

			SetNeedsUpdate();
		}

		internal override CGAffineTransform ToNativeTransform(CGSize size, bool withCenter = true)
		{
			var pivotX = withCenter ? CenterX : 0;
			var pivotY = withCenter ? CenterY : 0;

			CGAffineTransform transform = CGAffineTransform.MakeTranslation((nfloat)(pivotX), (nfloat)(pivotY));
			transform = CGAffineTransform.Rotate(transform, (nfloat)ToRadians(Angle));
			transform = CGAffineTransform.Translate(transform, -(nfloat)pivotX, -(nfloat)pivotY);
			return transform;
		}
	}
}

