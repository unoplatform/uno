using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.UI;

using Foundation;
using UIKit;
using CoreGraphics;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsControl : Control
	{
		partial void InitializePartial()
		{

		}

		partial void RequestLayoutPartial()
		{
			SetNeedsLayout();
		}
	}
}

