using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
using UIView = AppKit.NSView;
#endif

namespace Windows.UI.Xaml
{
	public partial class ElementStub : FrameworkElement
	{
		public ElementStub()
		{
			Visibility = Visibility.Collapsed;
		}

		private UIView MaterializeContent()
		{
			var currentPosition = Superview?.Subviews.IndexOf(this) ?? -1;
			
			if (currentPosition != -1)
			{
				// Create the instance first so that x:Bind constructs can be picked up by the
				// Unload event of ElementStub. Not doing so does not fills up the generated variables
				// too late and Binding.Update() does not refresh the available x:Bind instances.
				var newContent = ContentBuilder();

				var currentSuperview = Superview;
				RemoveFromSuperview();

#if __IOS__
				currentSuperview?.InsertSubview(newContent, currentPosition);
				return newContent;				
#elif __MACOS__
				// macOS TODO
				currentSuperview.AddSubview(newContent, NSWindowOrderingMode.Above, currentSuperview.Subviews[Math.Max(0, currentPosition-1)]);
#endif
			}

			return null;
		}
	}
}
