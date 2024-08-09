#nullable enable

using System;
using System.Collections.Generic;
using CoreAnimation;
using Foundation;
using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.UI.Composition
{
	public partial class SpriteVisual : ContainerVisual
	{
		partial void OnBrushChangedPartial(CompositionBrush? brush)
		{
			if (brush is CompositionColorBrush b)
			{
				NativeLayer.BackgroundColor = b.Color;
			}
			else
			{
				this.Log().Error($"The brush type {brush?.GetType()} is not supported for sprite visuals.");
			}
		}

		internal override bool StartAnimationCore(string propertyName, CompositionAnimation animation)
		{
			base.StartAnimationCore(propertyName, animation);

			switch (animation)
			{
				case ScalarKeyFrameAnimation kfa:
					AnimateKeyFrameAnimation(propertyName, kfa);
					return true;
			}

			return false;
		}

		private void AnimateKeyFrameAnimation(string propertyName, ScalarKeyFrameAnimation kfa)
		{
			switch (propertyName)
			{
				case nameof(RotationAngleInDegrees):
					var animations = new List<UnoCoreAnimation>();
					for (int i = 0; i < kfa.Keys.Length - 1; i++)
					{
						animations.Add(CreateCoreAnimation(NativeLayer, kfa.Keys[i], kfa.Keys[i + 1], "transform.rotation", value => new NSNumber(ToRadians(value))));
					}

					foreach (var a in animations)
					{
						a.Start();
					}
					break;
			}
		}

		private UnoCoreAnimation CreateCoreAnimation(
			CALayer layer,
			ScalarKeyFrameAnimation.KeyFrame from,
			ScalarKeyFrameAnimation.KeyFrame to,
			string property,
			Func<float, NSValue> nsValueConversion
		)
		{
			var timingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.Linear);

			return new UnoCoreAnimation(
				layer,
				property,
				from.Value,
				to.Value,
				from.NormalizedProgressKey,
				from.NormalizedProgressKey - to.NormalizedProgressKey,
				timingFunction,
				nsValueConversion,
				FinalizeAnimation,
				false
			);
		}

		private void FinalizeAnimation(UnoCoreAnimation.CompletedInfo info) { }
	}
}
