#nullable enable

using System;
using CoreGraphics;
using UIKit;
using Windows.Foundation;

namespace Uno.UI.Runtime.AppleUIKit.Controls;

/// <summary>
/// Shows the iOS system text magnifier (<c>UITextLoupeSession</c>, iOS 17+) over the Skia-rendered text
/// while the caret or a selection handle is dragged.
/// </summary>
/// <remarks>
/// The loupe magnifies the root view, which hosts the Skia canvas, so it shows the real rendered text rather than
/// the invisible proxy's own copy of it. Points arrive in XamlRoot (= text input layer) coordinates.
/// </remarks>
internal sealed class InvisibleTextBoxLoupe
{
#if __TVOS__
	// tvOS has no touch text editing, and no UITextLoupeSession.
	public bool Begin(UIView textInputLayer, Point point, Rect caretRect) => false;

	public void Move(Point point, Rect caretRect) { }

	public void End() { }
#else
	private UITextLoupeSession? _session;
	private UIView? _view;
	private UIView? _layer;

	public bool Begin(UIView textInputLayer, Point point, Rect caretRect)
	{
		End();

		if (!OperatingSystem.IsIOSVersionAtLeast(17) || textInputLayer.Superview is not { } rootView)
		{
			return false;
		}

		_layer = textInputLayer;
		_view = rootView;
		_session = UITextLoupeSession.BeginLoupeSession(ToView(point), null, rootView);
		if (_session is null)
		{
			_layer = null;
			_view = null;
			return false;
		}

		Move(point, caretRect);
		return true;
	}

	public void Move(Point point, Rect caretRect)
	{
		if (_session is null || _view is null || _layer is null)
		{
			return;
		}

		var caret = _view.ConvertRectFromView(new CGRect(caretRect.X, caretRect.Y, caretRect.Width, caretRect.Height), _layer);
		_session.MoveToPoint(ToView(point), caret, true); // true: the loupe tracks the caret
	}

	public void End()
	{
		_session?.Invalidate();
		_session = null;
		_view = null;
		_layer = null;
	}

	private CGPoint ToView(Point point)
		=> _view is not null && _layer is not null
			? _view.ConvertPointFromView(new CGPoint(point.X, point.Y), _layer)
			: new CGPoint(point.X, point.Y);
#endif
}
