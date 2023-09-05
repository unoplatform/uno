using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.UI;

using Foundation;
using AppKit;
using CoreGraphics;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsControl : Control
	{
		partial void InitializePartial()
		{

		}

		partial void RequestLayoutPartial()
		{
			InvalidateMeasure();
		}
	}
}

