#nullable enable

using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.Wpf;

internal partial class WpfNativeElementHostingExtension : INativeElementHostingExtension
{
	public WpfNativeElementHostingExtension()
	{
	}

	internal static WpfCanvas? GetOverlayLayer(XamlRoot xamlRoot) =>
		WpfManager.XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

	public bool IsNativeElement(object content)
		=> content is System.Windows.UIElement;

	public void AttachNativeElement(object owner, object content)
	{
		if (owner is XamlRoot xamlRoot
			&& GetOverlayLayer(xamlRoot) is { } layer
			&& content is System.Windows.FrameworkElement contentAsFE
			&& contentAsFE.Parent != layer)
		{
			layer.Children.Add(contentAsFE);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to attach native element {content} in {owner}.");
			}
		}
	}

	public void DetachNativeElement(object owner, object content)
	{
		if (owner is XamlRoot xamlRoot
			&& GetOverlayLayer(xamlRoot) is { } layer
			&& content is System.Windows.FrameworkElement contentAsFE
			&& contentAsFE.Parent == layer)
		{
			layer.Children.Remove(contentAsFE);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to detach native element {content} in {owner}.");
			}
		}
	}

	public bool IsNativeElementAttached(object owner, object nativeElement) =>
		nativeElement is System.Windows.FrameworkElement contentAsFE
			&& owner is XamlRoot xamlRoot
			&& GetOverlayLayer(xamlRoot) is { } layer
			&& contentAsFE.Parent == layer;

	public void ChangeNativeElementVisibility(object owner, object content, bool visible)
	{
		if (content is System.Windows.UIElement contentAsUIElement)
		{
			contentAsUIElement.Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
		}
	}
	public void ChangeNativeElementOpacity(object owner, object content, double opacity)
	{
		if (content is System.Windows.UIElement contentAsUIElement)
		{
			contentAsUIElement.Opacity = opacity;
		}
	}

	public void ArrangeNativeElement(object owner, object content, Windows.Foundation.Rect arrangeRect, Windows.Foundation.Rect? clip)
	{
		if (content is System.Windows.UIElement contentAsUIElement)
		{
			WpfCanvas.SetLeft(contentAsUIElement, arrangeRect.X);
			WpfCanvas.SetTop(contentAsUIElement, arrangeRect.Y);

			contentAsUIElement.Arrange(
				new(arrangeRect.X, arrangeRect.Y, arrangeRect.Width, arrangeRect.Height)
			);

			if (clip is { } c)
			{
				contentAsUIElement.Clip = new RectangleGeometry(new System.Windows.Rect(0, c.Y, 9999, Math.Round(c.Height)));
			}
			else
			{
				contentAsUIElement.Clip = null;
			}
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to arrange native element {content} in {owner}.");
			}
		}
	}

	public Windows.Foundation.Size MeasureNativeElement(object owner, object content, Windows.Foundation.Size childMeasuredSize, Windows.Foundation.Size availableSize)
	{
		if (content is System.Windows.UIElement contentAsUIElement)
		{
			contentAsUIElement.Measure(new System.Windows.Size(availableSize.Width, availableSize.Height));
			return new Windows.Foundation.Size(contentAsUIElement.DesiredSize.Width, contentAsUIElement.DesiredSize.Height);
		}

		return Windows.Foundation.Size.Empty;
	}

	public object CreateSampleComponent(string text)
	{
		return new Button
		{
			Width = 100,
			Height = 100,
			Content = new Viewbox
			{
				Child = new StackPanel
				{
					Children =
					{
						new TextBlock
						{
							Text = text
						},
						new Path
						{
							// A star
							Data = Geometry.Parse("M 17.416,32.25L 32.910,32.25L 38,18L 43.089,32.25L 58.583,32.25L 45.679,41.494L 51.458,56L 38,48.083L 26.125,56L 30.597,41.710L 17.416,32.25 Z"),
							Stretch = Stretch.Uniform,
							Stroke = Brushes.Red
						}
					}
				}
			}
		};
	}
}
