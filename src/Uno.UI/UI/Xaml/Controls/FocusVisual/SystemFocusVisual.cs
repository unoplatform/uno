#nullable enable

using System;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Numerics;


using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;

namespace Uno.UI.Xaml.Controls;

internal partial class SystemFocusVisual : Control
{
	private SerialDisposable _focusedElementSubscriptions = new SerialDisposable();
	private Rect _lastRect = Rect.Empty;

	public SystemFocusVisual()
	{
		DefaultStyleKey = typeof(SystemFocusVisual);
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

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

	internal void Redraw() => SetLayoutProperties();

	private static void OnFocusedElementChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var focusVisual = (SystemFocusVisual)dependencyObject;

		focusVisual._focusedElementSubscriptions.Disposable = null;

		if (args.NewValue is FrameworkElement element)
		{
			element.SizeChanged += focusVisual.FocusedElementSizeChanged;
#if !UNO_HAS_ENHANCED_LIFECYCLE
			element.LayoutUpdated += focusVisual.FocusedElementLayoutUpdated;
#endif
			element.EffectiveViewportChanged += focusVisual.FocusedElementEffectiveViewportChanged;
			element.Unloaded += focusVisual.FocusedElementUnloaded;

			var visibilityToken = element.RegisterPropertyChangedCallback(VisibilityProperty, focusVisual.FocusedElementVisibilityChanged);

			focusVisual.AttachVisualPartial();

			focusVisual._lastRect = Rect.Empty;
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
}
