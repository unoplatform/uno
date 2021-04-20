#nullable enable

using Uno.Disposables;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls
{
	internal partial class SystemFocusVisual : Control
	{
		private SerialDisposable _focusedElementSubscriptions = new SerialDisposable();

		public SystemFocusVisual()
		{
			DefaultStyleKey = typeof(SystemFocusVisual);
		}

		public FrameworkElement FocusedElement
		{
			get => (FrameworkElement)GetValue(FocusedElementProperty);
			set => SetValue(FocusedElementProperty, value);
		}

		public static readonly DependencyProperty FocusedElementProperty =
			DependencyProperty.Register(
				nameof(FocusedElement),
				typeof(FrameworkElement),
				typeof(SystemFocusVisual),
				new PropertyMetadata(default, OnFocusedElementChanged));

		private static void OnFocusedElementChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var focusVisual = (SystemFocusVisual)dependencyObject;

			focusVisual._focusedElementSubscriptions.Disposable = null;

			if (args.NewValue is FrameworkElement element)
			{
				element.EnsureFocusVisualBrushDefaults();
				element.SizeChanged += focusVisual.FocusedElementSizeChanged;
				element.LayoutUpdated += focusVisual.FocusedElemenLayoutUpdated;

				focusVisual.SetLayoutProperties();

				focusVisual._focusedElementSubscriptions.Disposable = Disposable.Create(() =>
				{
					element.SizeChanged -= focusVisual.FocusedElementSizeChanged;
					element.LayoutUpdated -= focusVisual.FocusedElemenLayoutUpdated;
				});
			}
		}

		private void FocusedElemenLayoutUpdated(object sender, object e) => SetLayoutProperties();

		private void FocusedElementSizeChanged(object sender, SizeChangedEventArgs args) => SetLayoutProperties();

		private void SetLayoutProperties()
		{
			if (FocusedElement == null)
			{
				return;
			}

			Width = FocusedElement.ActualWidth;
			Height = FocusedElement.ActualHeight;
			var transformToRoot = FocusedElement.TransformToVisual(Windows.UI.Xaml.Window.Current.Content);
			var point = transformToRoot.TransformPoint(new Windows.Foundation.Point(0, 0));
			Canvas.SetLeft(this, point.X);
			Canvas.SetTop(this, point.Y);
		}
	}
}
