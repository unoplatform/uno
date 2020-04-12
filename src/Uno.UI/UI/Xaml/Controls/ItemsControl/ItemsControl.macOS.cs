using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.UI;

using Foundation;
using AppKit;
using CoreGraphics;

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsControl : Control
	{
		partial void InitializePartial()
		{

		}

		partial void RequestLayoutPartial()
		{
			NeedsLayout = true;
		}

		partial void RemoveViewPartial(NSView current)
		{
			current.RemoveFromSuperview();
		}

		public override void Layout()
		{
			UpdateItemsIfNeeded();
			base.Layout();
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			UpdateItemsIfNeeded();
			return base.SizeThatFits(size);
		}
	}
}

