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

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// ScaleTransform: iOS part
	/// </summary>
	public partial class ScaleTransform
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

		partial void SetScaleX(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (View != null && !(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				Update();
			}
		}

		partial void SetScaleY(DependencyPropertyChangedEventArgs args)
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

			var transform = CGAffineTransform.MakeIdentity();

			//Perform transformations about centre
			transform = CGAffineTransform.Translate(transform, (nfloat)pivotX, (nfloat)pivotY);

			//Apply transformations in order
			transform = CGAffineTransform.Scale(transform, (nfloat)ScaleX, (nfloat)ScaleY);

			//Unapply centering
			transform = CGAffineTransform.Translate(transform, -(nfloat)pivotX, -(nfloat)pivotY);

			return transform;
		}
	}
}

