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
	internal UnoExploreByTouchHelper ExploreByTouchHelper { get; }

	public UnoSKCanvasView(Context context, UIElement rootElement) : base(context)
	{
		ExploreByTouchHelper = new UnoExploreByTouchHelper(this, rootElement);
		ViewCompat.SetAccessibilityDelegate(this, new UnoExploreByTouchHelper(this, Microsoft.UI.Xaml.Window.CurrentSafe!.RootElement!));
	}

	protected override bool DispatchHoverEvent(MotionEvent? e)
	{
		return ExploreByTouchHelper.DispatchHoverEvent(e) ||
			base.DispatchHoverEvent(e);
	}

	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		return ExploreByTouchHelper.DispatchKeyEvent(e) ||
			base.DispatchKeyEvent(e);
	}

	protected override void OnFocusChanged(bool gainFocus, [GeneratedEnum] FocusSearchDirection direction, Rect? previouslyFocusedRect)
	{
		base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
		ExploreByTouchHelper.OnFocusChanged(gainFocus, (int)direction, previouslyFocusedRect);
	}
}
