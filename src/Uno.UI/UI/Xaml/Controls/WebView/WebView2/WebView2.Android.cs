﻿using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class WebView2 : ICustomClippingElement
{
	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

	// Force clipping, otherwise native WebView may exceed its bounds in some circumstances (eg when Xaml WebView is animated)
	bool ICustomClippingElement.ForceClippingToLayoutSlot => true;
}
