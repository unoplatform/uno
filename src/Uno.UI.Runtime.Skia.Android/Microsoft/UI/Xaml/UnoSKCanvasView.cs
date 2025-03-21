// #define FPS_DISPLAY

using System;
using Windows.Graphics.Display;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Autofill;
using Android.Views.InputMethods;
using AndroidX.Core.View;
using Microsoft.UI.Xaml;
using SkiaSharp;
using SkiaSharp.Views.Android;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class UnoSKCanvasView : SKGLSurfaceView
{
#if FPS_DISPLAY
	private long _counter;
	private DateTime _time = DateTime.UtcNow;
	private string _fpsText = "0";
#endif

	private SKPicture? _picture;

	internal UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	internal TextInputPlugin TextInputPlugin { get; }

	internal static UnoSKCanvasView? Instance { get; private set; }

	public UnoSKCanvasView(Context context) : base(context)
	{
		Instance = this;
		ExploreByTouchHelper = new UnoExploreByTouchHelper(this);
		TextInputPlugin = new TextInputPlugin(this);
		ViewCompat.SetAccessibilityDelegate(this, ExploreByTouchHelper);
		Focusable = true;
		FocusableInTouchMode = true;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			ImportantForAutofill = ImportantForAutofill.Yes;
		}

		SetWillNotDraw(false);

		PaintSurface += (_, e) =>
		{
			if (_picture != null)
			{
				e.Surface.Canvas.DrawPicture(_picture);
			}
		};
	}

	protected override void OnDraw(Canvas c)
	{
		base.OnDraw(c);

		var recorder = new SKPictureRecorder();
		var canvas = recorder.BeginRecording(new SKRect(-9999, -9999, 9999, 9999));
		using (new SKAutoCanvasRestore(canvas, true))
		{
			if (Microsoft.UI.Xaml.Window.CurrentSafe is { RootElement: { } root } window)
			{
				ExploreByTouchHelper.InvalidateRoot();

				canvas.Clear(SKColors.Transparent);
				var scale = DisplayInformation.GetForCurrentViewSafe()!.RawPixelsPerViewPixel;
				canvas.Scale((float)scale);
				var negativePath = SkiaRenderHelper.RenderRootVisualAndReturnNegativePath((int)window.Bounds.Width,
					(int)window.Bounds.Height, root.Visual, canvas);
				if (ApplicationActivity.Instance.NativeLayerHost is { } nativeLayerHost)
				{
					nativeLayerHost.Path = negativePath;
					nativeLayerHost.Invalidate();
				}

#if FPS_DISPLAY
				// This naively calculates the difference in time every 100 frames, so to get
				// a usable number, open a sample with a continuously-running animation.
				_counter++;
				if (_counter % 100 == 0)
				{
					var newTime = DateTime.UtcNow;
					_fpsText = $"{100 / (newTime - _time).TotalSeconds}";
					_time = newTime;
				}
				canvas.DrawText(
					_fpsText,
					(float)(window.Bounds.Width / 2),
					(float)(window.Bounds.Height / 2),
					new SKFont(SKTypeface.Default, size: 20F),
					new SKPaint { Color = SKColors.Red});
#endif
			}

			_picture = recorder.EndRecording();
		}
	}

	public override bool OnCheckIsTextEditor()
		// Required for the InputConnection to be created
		=> true;

	protected override bool DispatchHoverEvent(MotionEvent? e)
	{
		if (e is null)
		{
			return base.DispatchHoverEvent(e);
		}

		return ExploreByTouchHelper.DispatchHoverEvent(e) ||
			base.DispatchHoverEvent(e);
	}

	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		if (e is null)
		{
			return base.DispatchKeyEvent(e);
		}

		return ExploreByTouchHelper.DispatchKeyEvent(e) ||
			base.DispatchKeyEvent(e);
	}

	protected override void OnFocusChanged(bool gainFocus, [GeneratedEnum] FocusSearchDirection direction, Rect? previouslyFocusedRect)
	{
		base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);

		try
		{
			ExploreByTouchHelper.OnFocusChanged(gainFocus, (int)direction, previouslyFocusedRect);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(UnoSKCanvasView)}.{nameof(OnFocusChanged)} failed", e);
			}
		}
	}

	public override void OnProvideAutofillVirtualStructure(ViewStructure? structure, [GeneratedEnum] AutofillFlags flags)
	{
		base.OnProvideAutofillVirtualStructure(structure, flags);

		if (Build.VERSION.SdkInt < BuildVersionCodes.O)
		{
			return;
		}

		TextInputPlugin?.OnProvideAutofillVirtualStructure(structure);
	}

	public override void Autofill(SparseArray values)
	{
		var count = values.Size();
		for (int i = 0; i < count; i++)
		{
			var virtualId = values.KeyAt(i);
			if (AndroidSkiaTextBoxNotificationsProviderSingleton.Instance.LiveTextBoxesMap.TryGetValue(virtualId, out var textBox))
			{
				var autofillValue = (AutofillValue)values.ValueAt(i)!;
				textBox.Text = autofillValue.TextValue;
			}
		}
	}

	public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
		=> TextInputPlugin.OnCreateInputConnection(outAttrs!);
}
