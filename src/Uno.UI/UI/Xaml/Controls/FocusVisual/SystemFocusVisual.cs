#nullable enable

using System;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Numerics;
using Uno.Extensions;

namespace Uno.UI.Xaml.Controls;

internal partial class SystemFocusVisual : Control
{
#if __SKIA__
	private static readonly SkiaSharp.SKPath _spareRenderPath = new SkiaSharp.SKPath();
#endif
	private SerialDisposable _focusedElementSubscriptions = new SerialDisposable();

	public SystemFocusVisual()
	{
		DefaultStyleKey = typeof(SystemFocusVisual);
#if !__SKIA__
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
#endif
	}

	public UIElement? FocusedElement
	{
		get => (FrameworkElement?)GetValue(FocusedElementProperty);
		set => SetValue(FocusedElementProperty, value);
	}

	public static readonly DependencyProperty FocusedElementProperty =
		DependencyProperty.Register(
			nameof(FocusedElement),
			typeof(UIElement),
			typeof(SystemFocusVisual),
			new FrameworkPropertyMetadata(default, OnFocusedElementChanged));

	private static void OnFocusedElementChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var focusVisual = (SystemFocusVisual)dependencyObject;

		focusVisual._focusedElementSubscriptions.Disposable = null;

		if (args.NewValue is FrameworkElement element)
		{
#if __SKIA__
			if (element.XamlRoot?.VisualTree.ContentRoot.CompositionTarget is { } compositionTarget)
			{
				compositionTarget.FrameRendered += focusVisual.OnFrameRendered;
				focusVisual._focusedElementSubscriptions.Disposable = Disposable.Create(() =>
				{
					compositionTarget.FrameRendered -= focusVisual.OnFrameRendered;
				});
			}
#else
			element.SizeChanged += focusVisual.FocusedElementSizeChanged;
#if !UNO_HAS_ENHANCED_LIFECYCLE
			element.LayoutUpdated += focusVisual.FocusedElementLayoutUpdated;
#endif
			element.EffectiveViewportChanged += focusVisual.FocusedElementEffectiveViewportChanged;
			element.Unloaded += focusVisual.FocusedElementUnloaded;

			var visibilityToken = element.RegisterPropertyChangedCallback(VisibilityProperty, focusVisual.FocusedElementVisibilityChanged);

			focusVisual.AttachVisualPartial();

			focusVisual.SetLayoutProperties();
			var parentViewport = element.GetParentViewport(); // the parent Viewport is used, similar to PropagateEffectiveViewportChange
			focusVisual.ApplyClipping(parentViewport.Effective);

			focusVisual._focusedElementSubscriptions.Disposable = Disposable.Create(() =>
			{
				element.SizeChanged -= focusVisual.FocusedElementSizeChanged;
#if !UNO_HAS_ENHANCED_LIFECYCLE
				element.LayoutUpdated -= focusVisual.FocusedElementLayoutUpdated;
#endif
				element.EffectiveViewportChanged -= focusVisual.FocusedElementEffectiveViewportChanged;
				element.UnregisterPropertyChangedCallback(VisibilityProperty, visibilityToken);

				focusVisual.DetachVisualPartial();
			});
#endif
		}
	}

#if __SKIA__
	private void OnFrameRendered()
	{
		if (XamlRoot is null ||
			FocusedElement is null ||
			FocusedElement.Visibility == Visibility.Collapsed ||
			FocusedElement is Control { IsEnabled: false, AllowFocusWhenDisabled: false })
		{
			Visibility = Visibility.Collapsed;
			return;
		}

		var parentElement = VisualTreeHelper.GetParent(FocusedElement) as UIElement;
		if (parentElement is null)
		{
			Visibility = Visibility.Collapsed;
			return;
		}

		var transform = GetTransform(FocusedElement, XamlRoot.VisualTree.RootElement);

		FocusedElement.Visual.GetTotalClipPath(_spareRenderPath, true);
		var totalClipRect = _spareRenderPath.Bounds;
		var inverseMatrix = transform.Inverse();
		var topLeft = inverseMatrix.Transform(new Point(totalClipRect.Left, totalClipRect.Top));
		var topRight = inverseMatrix.Transform(new Point(totalClipRect.Right, totalClipRect.Top));
		var bottomLeft = inverseMatrix.Transform(new Point(totalClipRect.Left, totalClipRect.Bottom));
		var bottomRight = inverseMatrix.Transform(new Point(totalClipRect.Right, totalClipRect.Bottom));

		var minX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
		var maxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
		var minY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
		var maxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));

		var clipRect = new Rect(minX, minY, maxX - minX, maxY - minY);
		var layoutRect = new Rect(0, 0, FocusedElement.ActualSize.X, FocusedElement.ActualSize.Y);
		var left = Math.Max(clipRect.Left, layoutRect.Left);
		var top = Math.Max(clipRect.Top, layoutRect.Top);
		var right = Math.Min(clipRect.Right, layoutRect.Right);
		var bottom = Math.Min(clipRect.Bottom, layoutRect.Bottom);

		var translatedMatrix = new Matrix(Matrix3x2.CreateTranslation((float)left, (float)top) * transform);
		if ((RenderTransform as MatrixTransform)?.Matrix != translatedMatrix)
		{
			RenderTransform = new MatrixTransform { Matrix = translatedMatrix };
		}

		var newWidth = Math.Max(0, right - left);
		var newHeight = Math.Max(0, bottom - top);
		Width = newWidth;
		Height = newHeight;
		Visibility = newWidth <= 0 || newHeight <= 0 ? Visibility.Collapsed : Visibility.Visible;
	}
#else
	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (XamlRoot is not null)
		{
			XamlRoot.Changed += XamlRootChanged;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (XamlRoot is not null)
		{
			XamlRoot.Changed -= XamlRootChanged;
		}
	}

	partial void AttachVisualPartial();

	partial void DetachVisualPartial();

	partial void SetLayoutPropertiesPartial();

	private void XamlRootChanged(object sender, XamlRootChangedEventArgs e) => SetLayoutProperties();

	private void FocusedElementUnloaded(object sender, RoutedEventArgs e) => FocusedElement = null;

	private void FocusedElementVisibilityChanged(DependencyObject sender, DependencyProperty dp) => SetLayoutProperties();

#if !UNO_HAS_ENHANCED_LIFECYCLE
	private void FocusedElementLayoutUpdated(object? sender, object e) => SetLayoutProperties();
#endif

	private void FocusedElementSizeChanged(object sender, SizeChangedEventArgs args) => SetLayoutProperties();

	private void FocusedElementEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
	{
		SetLayoutProperties();
		ApplyClipping(args.EffectiveViewport);
	}

	private void SetLayoutProperties()
	{
		if (XamlRoot is null ||
			FocusedElement is null ||
			FocusedElement.Visibility == Visibility.Collapsed ||
			(FocusedElement is Control control && !control.IsEnabled && !control.AllowFocusWhenDisabled))
		{
			Visibility = Visibility.Collapsed;
			return;
		}

		var parentElement = VisualTreeHelper.GetParent(FocusedElement) as UIElement;
		if (parentElement is null)
		{
			Visibility = Visibility.Collapsed;
			return;
		}

		Visibility = Visibility.Visible;

		// Use TransformToVisual to get the correct position accounting for all transforms including element's own.
		var transform = FocusedElement.TransformToVisual(XamlRoot.VisualTree.RootElement);
		RenderTransform = (MatrixTransform)transform;

		Width = FocusedElement.ActualSize.X;
		Height = FocusedElement.ActualSize.Y;

		SetLayoutPropertiesPartial();
	}

	private void ApplyClipping(Rect effectiveViewport)
	{
		if (FocusedElement is not FrameworkElement fe)
		{
			return;
		}

		var height = Height - fe.FocusVisualMargin.Top - fe.FocusVisualMargin.Bottom;
		var width = Width - fe.FocusVisualMargin.Left - fe.FocusVisualMargin.Right;

		RectangleGeometry clip;

		if (effectiveViewport.IsEmpty)
		{
			clip = new RectangleGeometry
			{
				Rect = new Rect(
					0,
					0,
					0,
					0
				)
			};
		}
		else
		{
			var clipTop = Math.Max(fe.FocusVisualMargin.Top, effectiveViewport.Top - fe.FocusVisualMargin.Top);
			var clipLeft = Math.Max(fe.FocusVisualMargin.Left, effectiveViewport.Left + fe.FocusVisualMargin.Left);
			var clipBottom = Math.Max(0, height - (effectiveViewport.Height + effectiveViewport.Top + fe.FocusVisualMargin.Bottom));
			var clipRight = Math.Max(0, width - (effectiveViewport.Width + effectiveViewport.Left + fe.FocusVisualMargin.Right));

			clip = new RectangleGeometry
			{
				Rect = new Rect(
					Math.Min(width, clipLeft),
					Math.Min(height, clipTop),
					Math.Max(0, width - clipRight - clipLeft),
					Math.Max(0, height - clipBottom - clipTop)
				)
			};
		}

		Clip = clip;
	}
#endif
}
