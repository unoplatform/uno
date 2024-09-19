using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameController;
using UIKit;
using Microsoft.UI.Xaml;


#if __IOS__
using CoreGraphics;
using _View = UIKit.UIView;
using _Controller = UIKit.UIViewController;
using _Responder = UIKit.UIResponder;
using _Color = UIKit.UIColor;
using _Event = UIKit.UIEvent;
using System.Security.Principal;
#elif __MACOS__
using Foundation;
using AppKit;
using CoreGraphics;
using _View = AppKit.NSView;
using _Controller = AppKit.NSViewController;
using _Responder = AppKit.NSResponder;
using _Color = AppKit.NSColor;
using _Event = AppKit.NSEvent;
#endif

namespace UIKit;

internal static class UIViewExtensions
{
	/// <summary>
	/// Finds the nearest view controller for this _View.
	/// </summary>
	/// <returns>A _ViewController instance, otherwise null.</returns>
	public static _Controller? FindViewController(this _View? view)
	{
		if (view?.NextResponder == null)
		{
			// Sometimes, a view is not part of the visual tree (or doesn't have a next responder) but is part of the logical tree.
			// Here, we substitute the view with the first logical parent that's part of the visual tree (or has a next responder).
			view = (view as DependencyObject)
				?.GetParents()
				.OfType<_View>()
				.Where(parent => parent.NextResponder != null)
				.FirstOrDefault();
		}

		_Responder? responder = view;

		do
		{
			if (responder is _View nativeView)
			{
				responder = nativeView.NextResponder;
			}
			else if (responder is _Controller controller)
			{
				return controller;
			}
			else
			{
				responder = null;
			}

		} while (responder != null);

		return null;
	}
}
