using System;
using System.Drawing;
using System.Linq;
using Uno.Extensions;
using Uno.UI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Diagnostics;
using CoreGraphics;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class FlipViewItem : SelectorItem
	{
		public FlipViewItem()
		{
			DefaultStyleKey = typeof(FlipViewItem);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new FlipViewItemAutomationPeer(this);
		}

		protected override global::Windows.Foundation.Size MeasureOverride(global::Windows.Foundation.Size availableSize)
		{
			Debug.WriteLine("Measure " + availableSize);
			return base.MeasureOverride(availableSize);
		}

		protected override global::Windows.Foundation.Size ArrangeOverride(global::Windows.Foundation.Size finalSize)
		{
			Debug.WriteLine("Arrange " + finalSize);
			return base.ArrangeOverride(finalSize);
		}

		public override void LayoutSubviews()
		{
			Debug.WriteLine("In layout subviews " + Frame + " " + Bounds);
			base.LayoutSubviews();
		}

		public override CGRect Frame
		{
			get => base.Frame;
			set
			{
				base.Frame = value;
				Debug.WriteLine("Setting frame " + Frame + " " + Bounds);
			}
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			Debug.WriteLine("Size that fits " + Frame + " " + Bounds + " " + size);
			var res = base.SizeThatFits(size);
			Debug.WriteLine("Size that fits " + res);
			return res;
		}
	}
}
