using System;
using System.Collections.Generic;
using System.Text;
using AppKit;

namespace Uno.UI.Extensions
{
	internal static class NSViewExtensions
	{
		public static void SetNeedsLayout(this NSView view) => view.NeedsLayout = true;
		public static void SetSuperviewNeedsLayout(this NSView view)
		{
			if (view.Superview != null)
			{
				view.Superview.NeedsLayout = true;
			}
		}
	}
}
