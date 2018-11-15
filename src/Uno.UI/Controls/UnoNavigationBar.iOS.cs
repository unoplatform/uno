using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using UIKit;

namespace Uno.UI.Controls
{
	public partial class UnoNavigationBar : UINavigationBar
	{
		internal event Action SizeChanged;

		public override CGRect Frame
		{
			get => base.Frame;
			set
			{
				if(value != Frame)
				{
					base.Frame = value;
					SizeChanged?.Invoke();
				}
			}
		}

		public override CGRect Bounds
		{
			get => base.Bounds;
			set
			{
				if(value != Bounds)
				{
					base.Bounds = value;
					SizeChanged?.Invoke();
				}
			}
		}
	}
}
