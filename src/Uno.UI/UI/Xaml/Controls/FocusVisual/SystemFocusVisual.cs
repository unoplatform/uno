#nullable enable

using System;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
#endif

namespace Uno.UI.Xaml.Controls;

internal partial class SystemFocusVisual : Control
{
	private SerialDisposable _focusedElementSubscriptions = new SerialDisposable();
	private Rect _lastRect = Rect.Empty;

	public SystemFocusVisual()
	{
		DefaultStyleKey = typeof(SystemFocusVisual);
		Microsoft.UI.Xaml.Window.Current.SizeChanged += WindowSizeChanged;
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
			element.EnsureFocusVisualBrushDefaults();
			element.SizeChanged += focusVisual.FocusedElementSizeChanged;
			element.LayoutUpdated += focusVisual.FocusedElementLayoutUpdated;
			element.Unloaded += focusVisual.FocusedElementUnloaded;

			var visibilityToken = element.RegisterPropertyChangedCallback(VisibilityProperty, focusVisual.FocusedElementVisibilityChanged);

			focusVisual.AttachVisualPartial();

			focusVisual._lastRect = Rect.Empty;
			focusVisual.SetLayoutProperties();

			focusVisual._focusedElementSubscriptions.Disposable = Disposable.Create(() =>
			{
				element.SizeChanged -= focusVisual.FocusedElementSizeChanged;
				element.LayoutUpdated -= focusVisual.FocusedElementLayoutUpdated;
				element.UnregisterPropertyChangedCallback(VisibilityProperty, visibilityToken);

				focusVisual.DetachVisualPartial();
			});
		}
	}

	partial void AttachVisualPartial();

	partial void DetachVisualPartial();

	partial void SetLayoutPropertiesPartial();

	private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e) => SetLayoutProperties();

	private void FocusedElementUnloaded(object sender, RoutedEventArgs e) => FocusedElement = null;

	private void FocusedElementVisibilityChanged(DependencyObject sender, DependencyProperty dp) => SetLayoutProperties();

	private void FocusedElementLayoutUpdated(object? sender, object e) => SetLayoutProperties();

	private void FocusedElementSizeChanged(object sender, SizeChangedEventArgs args) => SetLayoutProperties();

	private void SetLayoutProperties()
	{
		if (FocusedElement == null ||
			FocusedElement.Visibility == Visibility.Collapsed ||
			(FocusedElement is Control control && !control.IsEnabled && !control.AllowFocusWhenDisabled))
		{
			Visibility = Visibility.Collapsed;
			return;
		}

		Visibility = Visibility.Visible;

		var transformToRoot = FocusedElement.TransformToVisual(Microsoft.UI.Xaml.Window.Current.RootElement);
		var point = transformToRoot.TransformPoint(new Windows.Foundation.Point(0, 0));
		var newRect = new Rect(point.X, point.Y, FocusedElement.ActualSize.X, FocusedElement.ActualSize.Y);

		if (newRect != _lastRect)
		{
			Width = FocusedElement.ActualSize.X;
			Height = FocusedElement.ActualSize.Y;

			Canvas.SetLeft(this, point.X);
			Canvas.SetTop(this, point.Y);

			_lastRect = newRect;
		}

		SetLayoutPropertiesPartial();
	}
}
