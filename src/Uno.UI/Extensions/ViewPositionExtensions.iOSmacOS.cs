#nullable disable

using System.Drawing;
using CoreGraphics;
using System;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
#endif

namespace Uno.UI.Extensions
{
	public static class UIViewPositioningExtensions
	{
		public static T IncrementX<T>(this T thisView, float delta) where T : _View
		{
			thisView.Frame = thisView.Frame.IncrementX((nfloat)delta);
			return thisView;
		}

		public static T IncrementY<T>(this T thisView, float delta) where T : _View
		{
			thisView.Frame = thisView.Frame.IncrementY((nfloat)delta);
			return thisView;
		}

		public static T SetX<T>(this T thisView, double value) where T : _View
		{
			return thisView.SetX((float)value);
		}

		public static T SetX<T>(this T thisView, float value) where T : _View
		{
			thisView.Frame = thisView.Frame.SetX((nfloat)value);
			return thisView;
		}

		public static T SetRight<T>(this T thisView, float value) where T : _View
		{
			thisView.Frame = thisView.Frame.SetRight((nfloat)value);
			return thisView;
		}

		public static T SetRightRespectWidth<T>(this T thisView, float value) where T : _View
		{
			thisView.Frame = thisView.Frame.SetRightRespectWidth((nfloat)value);
			return thisView;
		}

		public static T SetVerticalCenter<T>(this T thisView, float value) where T : _View
		{
			thisView.Frame = thisView.Frame.SetVerticalCenter((nfloat)value);
			return thisView;
		}

		public static T SetHorizontalCenter<T>(this T thisView, float value) where T : _View
		{
			thisView.Frame = thisView.Frame.SetHorizontalCenter((nfloat)value);
			return thisView;
		}

		public static T SetY<T>(this T thisView, nfloat value) where T : _View
		{
			thisView.Frame = thisView.Frame.SetY(value);
			return thisView;
		}
		public static T SetY<T>(this T thisView, double value) where T : _View
		{
			return thisView.SetY((float)value);
		}

		public static T SetBottom<T>(this T thisView, float value) where T : _View
		{
			thisView.Frame = thisView.Frame.SetBottom((nfloat)value);
			return thisView;
		}

		public static T IncrementHeight<T>(this T thisView, float delta) where T : _View
		{
			thisView.Frame = thisView.Frame.IncrementHeight((nfloat)delta);
			return thisView;
		}

		public static T IncrementWidth<T>(this T thisView, float delta) where T : _View
		{
			thisView.Frame = thisView.Frame.IncrementWidth((nfloat)delta);
			return thisView;
		}

		public static T SetWidth<T>(this T thisView, float value) where T : _View
		{
			thisView.Frame = thisView.Frame.SetWidth((nfloat)value);
			return thisView;
		}

		public static T SetWidth<T>(this T thisView, double value) where T : _View
		{
			return thisView.SetWidth((float)value);
		}

		public static T SetHeight<T>(this T thisView, float value) where T : _View
		{
			thisView.Frame = thisView.Frame.Copy().SetHeight((nfloat)value);
			return thisView;
		}
		public static T SetHeight<T>(this T thisView, double value) where T : _View
		{
			return thisView.SetHeight((float)value);
		}

		public static T SetSize<T>(this T thisView, SizeF value) where T : _View
		{
			thisView.Frame = new CGRect(thisView.Frame.X, thisView.Frame.Y, value.Width, value.Height);
			return thisView;
		}

		public static T SetSize<T>(this T thisView, float width, float height) where T : _View
		{
			thisView.Frame = new CGRect(thisView.Frame.X, thisView.Frame.Y, width, height);
			return thisView;
		}

		public static T SetFrame<T>(this T thisView, RectangleF frame) where T : _View
		{
			return thisView.SetFrame(frame.X, frame.Y, frame.Width, frame.Height);
		}

		public static T SetFrame<T>(this T thisView, float x, float y, float width, float height) where T : _View
		{
			thisView.Frame = new RectangleF(x, y, width, height);
			return thisView;
		}
	}
}

