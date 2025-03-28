using System;
using System.Collections.Generic;
using System.Text;
using Windows.Graphics.Display;
using AppKit;
using Windows.UI.Xaml.Media;
using Uno.Extensions;

namespace Uno.UI.Controls
{
	public class RootViewController : NSViewController
	{
		public RootViewController()
		{
		}

		public override void LoadView()
		{
			// Don't call base, we're loading the view without a nib
			// base.LoadView();
		}
	}
}
