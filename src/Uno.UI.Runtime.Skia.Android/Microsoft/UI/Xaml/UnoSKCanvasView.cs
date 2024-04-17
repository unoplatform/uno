using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.UI.Xaml;
using SkiaSharp.Views.Android;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class UnoSKCanvasView : SKCanvasView
{
	private readonly UnoExploreByTouchHelper _exploreByTouchHelper;

	public UnoSKCanvasView(Context context, UIElement rootElement) : base(context)
	{
		_exploreByTouchHelper = new UnoExploreByTouchHelper(this, rootElement);
		ViewCompat.SetAccessibilityDelegate(this, new UnoExploreByTouchHelper(this, Microsoft.UI.Xaml.Window.CurrentSafe!.RootElement!));
	}

	protected override bool DispatchHoverEvent(MotionEvent? e)
	{
		return _exploreByTouchHelper.DispatchHoverEvent(e) ||
			base.DispatchHoverEvent(e);
	}

	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		return _exploreByTouchHelper.DispatchKeyEvent(e) ||
			base.DispatchKeyEvent(e);
	}

	protected override void OnFocusChanged(bool gainFocus, [GeneratedEnum] FocusSearchDirection direction, Rect? previouslyFocusedRect)
	{
		base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
		_exploreByTouchHelper.OnFocusChanged(gainFocus, (int)direction, previouslyFocusedRect);
	}
}
