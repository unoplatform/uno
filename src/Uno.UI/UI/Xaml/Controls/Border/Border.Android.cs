﻿using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Android.Graphics.Drawables.Shapes;
using System.Linq;
using Uno.Disposables;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Uno.UI.Helpers;

using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using Android.Views;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class Border
{
	protected override void OnDraw(ACanvas canvas) => AdjustCornerRadius(canvas, CornerRadius);

	bool ICustomClippingElement.AllowClippingToLayoutSlot => !(Child is UIElement ue) || ue.RenderTransform == null;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
}
