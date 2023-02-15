using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Android.Graphics.Drawables.Shapes;
using System.Linq;
using Uno.Disposables;
using Microsoft.UI.Xaml.Media;
using Uno.UI;

namespace Windows.UI.Xaml.Controls;

public partial class Border
{
	protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom, bool localIsLayoutRequested)
	{
		base.OnLayoutCore(changed, left, top, right, bottom, localIsLayoutRequested);

		UpdateBorder();
	}

	partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
	{
		if (previousValue != null)
		{
			RemoveView(previousValue);
		}

		if (newValue != null)
		{
			AddView(newValue);
		}
	}

	protected override void OnDraw(Android.Graphics.Canvas canvas)
	{
		AdjustCornerRadius(canvas, CornerRadius);
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => !(Child is UIElement ue) || ue.RenderTransform == null;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
}
