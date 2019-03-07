using CoreAnimation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Uno.Extensions;
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

		protected override void OnAttachedToView()
		{
			base.OnAttachedToView();

			SetNeedsUpdate();
		}

		internal override CGAffineTransform ToNativeTransform(CGSize size, bool withCenter = true)
		{
			return CGAffineTransform.MakeTranslation((nfloat)X, (nfloat)Y);
		}
	}
}

