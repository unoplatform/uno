using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.AppleUIKit.Hosting;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal sealed class UIKitNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _presenter;

	public UIKitNativeElementHostingExtension(ContentPresenter presenter)
	{
		_presenter = presenter;
	}

	private UIView? OverlayLayer => _presenter.XamlRoot is { } xamlRoot ? (XamlRootMap.GetHostForRoot(xamlRoot) as IAppleUIKitXamlRootHost)?.NativeOverlayLayer : null;

	public void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect)
	{
		if (content is UIView view)
		{
			view.Frame = arrangeRect.ToCGRect();
		}
	}

	public void AttachNativeElement(object content)
	{
		if (OverlayLayer is not { } overlayLayer)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Debug($"Unable to attach native element because overlay layer is not initialized.");
			}

			return;
		}

		if (content is UIView view)
		{
			overlayLayer.AddSubview(view);
		}
	}

	public void DetachNativeElement(object content)
	{
		if (OverlayLayer is not { })
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Debug($"Unable to attach native element because overlay layer is not initialized.");
			}

			return;
		}

		if (content is UIView view)
		{
			view.RemoveFromSuperview();
		}
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is UIView view)
		{
			view.Alpha = (float)opacity;
		}
	}

	public object? CreateSampleComponent(string text)
	{
		if (OverlayLayer is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Debug($"Unable to attach native element because overlay layer is not initialized.");
			}

			return null;
		}

		var button = new UIButton();
		button.SetTitle(text, UIControlState.Normal);

		return button;
	}

	public bool IsNativeElement(object content) => content is UIView;

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize)
	{
		if (content is UIView view)
		{
			var availablePhysical = availableSize.LogicalToPhysicalPixels();
			var size = view.SizeThatFits(new CoreGraphics.CGSize((int)availablePhysical.Width, (int)availablePhysical.Height));
			return new Size(size.Width, size.Height);
		}

		return default;
	}
}
