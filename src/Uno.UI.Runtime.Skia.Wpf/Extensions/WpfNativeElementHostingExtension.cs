#nullable enable

using System;
using System.Windows.Media;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;
using ContentPresenter = Windows.UI.Xaml.Controls.ContentPresenter;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.Wpf;

internal partial class WpfNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _presenter;

	public WpfNativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
	}

	private XamlRoot? XamlRoot => _presenter.XamlRoot;
	private WpfCanvas? OverlayLayer => _presenter.XamlRoot is { } xamlRoot ? WpfManager.XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer : null;

	public bool IsNativeElement(object content)
		=> content is System.Windows.UIElement;

	public void AttachNativeElement(object content)
	{
		if (OverlayLayer is { } layer
			&& content is System.Windows.FrameworkElement contentAsFE
			&& contentAsFE.Parent != layer)
		{
			layer.Children.Add(contentAsFE);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to attach native element {content} in {XamlRoot}.");
			}
		}
	}

	public void DetachNativeElement(object content)
	{
		if (OverlayLayer is { } layer
			&& content is System.Windows.FrameworkElement contentAsFE
			&& contentAsFE.Parent == layer)
		{
			layer.Children.Remove(contentAsFE);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to detach native element {content} in {XamlRoot}.");
			}
		}
	}

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		if (content is System.Windows.UIElement contentAsUIElement)
		{
			contentAsUIElement.Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
		}
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is System.Windows.UIElement contentAsUIElement)
		{
			contentAsUIElement.Opacity = opacity;
		}
	}

	public void ArrangeNativeElement(object content, Windows.Foundation.Rect arrangeRect, Windows.Foundation.Rect clipRect)
	{
		if (content is System.Windows.UIElement contentAsUIElement)
		{
			WpfCanvas.SetLeft(contentAsUIElement, arrangeRect.X);
			WpfCanvas.SetTop(contentAsUIElement, arrangeRect.Y);

			contentAsUIElement.Arrange(
				new(arrangeRect.X, arrangeRect.Y, arrangeRect.Width, arrangeRect.Height)
			);

			// no longer needed now that we draw everything above of the native elements but kept for reference
			// contentAsUIElement.Clip = new RectangleGeometry(new System.Windows.Rect(0, clipRect.Y, Math.Round(clipRect.Width), Math.Round(clipRect.Height)));
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to arrange native element {content} in {XamlRoot}.");
			}
		}
	}

	public Windows.Foundation.Size MeasureNativeElement(object content, Windows.Foundation.Size childMeasuredSize, Windows.Foundation.Size availableSize)
	{
		if (content is System.Windows.UIElement contentAsUIElement)
		{
			contentAsUIElement.Measure(new System.Windows.Size(availableSize.Width, availableSize.Height));
			return new Windows.Foundation.Size(contentAsUIElement.DesiredSize.Width, contentAsUIElement.DesiredSize.Height);
		}

		return Windows.Foundation.Size.Empty;
	}
}
