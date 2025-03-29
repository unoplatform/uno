#if !HAS_UI_TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Uno.Disposables;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using CoreGraphics;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;
using UIKit;
using _Image = UIKit.UIImage;

namespace Microsoft.UI.Xaml.Controls;

partial class Border
{
	protected override void OnAfterArrange()
	{
		base.OnAfterArrange();
		BorderRenderer.Update();
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None && (!(Child is UIElement ue) || ue.RenderTransform == null);
	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
}
#endif
