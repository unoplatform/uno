using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Transform: wasm part
	/// </summary>
	partial class Transform
	{
		internal virtual Matrix3x2 ToNativeTransform(Size size)
		{
			throw new NotImplementedException(nameof(ToNativeTransform) + " not implemented for " + this.GetType().ToString());
		}

		internal static double ToRadians(double angle) => MathEx.ToRadians(angle);

		protected void SetMatrix(Matrix3x2 m)
		{
			var matrix = $"matrix({m.M11.ToStringInvariant()},{m.M12.ToStringInvariant()},{m.M21.ToStringInvariant()},{m.M22.ToStringInvariant()},{m.M31.ToStringInvariant()},{m.M32.ToStringInvariant()})";
			View?.SetStyle("transform", matrix);
		}

		protected void Update()
		{
			SetMatrix(ToNativeTransform(GetViewSize(View)));
		}

		partial void OnDetachedFromViewPartial(UIElement view)
		{
			view.ResetStyle("transform");

			if (view is FrameworkElement fe)
			{
				fe.SizeChanged -= Fe_SizeChanged;
			}
		}

		partial void OnAttachedToViewPartial(UIElement view)
		{
			View?.SetStyle("transform-origin", "left top");
			if (view is FrameworkElement fe)
			{
				fe.SizeChanged += Fe_SizeChanged;
			}
		}

		private void Fe_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			Update();
		}

		/// <summary>
		/// Get size of the view before any transform is applied.
		/// </summary>
		protected static Size GetViewSize(UIElement view)
		{
			if (view is FrameworkElement fe)
			{
				return fe.RenderSize;
			}

			return Size.Empty;
		}
	}
}

