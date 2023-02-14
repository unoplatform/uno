using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using System.Linq;
using System.Drawing;
using Uno.Disposables;
using Microsoft.UI.Xaml.Media;
using Uno.UI;

using View = Microsoft.UI.Xaml.UIElement;
using Color = System.Drawing.Color;
using Microsoft.UI.Composition;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Border
	{
		partial void OnChildChangedPartial(View previousValue, View newValue)
		{
			if (previousValue != null)
			{
				RemoveChild(previousValue);
			}

			AddChild(newValue);
		}

		internal override void OnArrangeVisual(Rect rect, Rect? clip)
		{
			UpdateBorder();

			base.OnArrangeVisual(rect, clip);
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => !(Child is UIElement ue) || ue.RenderTransform == null;

		bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
	}
}
