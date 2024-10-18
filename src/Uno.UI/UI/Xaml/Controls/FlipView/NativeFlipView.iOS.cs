using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CoreGraphics;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NativeFlipView : PagedCollectionView
	{
		public NativeFlipView()
		{
			ShowsHorizontalScrollIndicator = false;
		}

		public override void LayoutSubviews()
		{
			Debug.WriteLine("NativeFlipVIew In layout subviews " + Frame + " " + Bounds);
			base.LayoutSubviews();
		}

		public override CGRect Frame
		{
			get => base.Frame;
			set
			{
				base.Frame = value;
				Debug.WriteLine("NativeFlipVIew Setting frame " + Frame + " " + Bounds);
			}
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			Debug.WriteLine("NativeFlipVIew Size that fits " + Frame + " " + Bounds + " " + size);
			var res = base.SizeThatFits(size);
			Debug.WriteLine("NativeFlipVIew Size that fits " + res);
			return res;
		}
	}
}
