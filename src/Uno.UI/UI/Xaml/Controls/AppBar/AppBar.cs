#if __IOS__ || __ANDROID__
#define HAS_NATIVE_COMMANDBAR
#endif
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Controls;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class AppBar : ContentControl
#if HAS_NATIVE_COMMANDBAR
		, ICustomClippingElement
#endif
	{
		private double _compactHeight;
		private double _minimalHeight;
#if HAS_NATIVE_COMMANDBAR
		private bool _isNativeTemplate;
#endif

		public AppBar()
		{
			TemplateSettings = new AppBarTemplateSettings(this);

			SizeChanged += (s, e) => UpdateTemplateSettings();
		}

#region IsSticky

		public bool IsSticky
		{
			get => (bool)GetValue(IsStickyProperty);
			set => SetValue(IsStickyProperty, value);
		}

		public static DependencyProperty IsStickyProperty { get; } =
			DependencyProperty.Register(
				"IsSticky",
				typeof(bool),
				typeof(AppBar),
				new FrameworkPropertyMetadata(default(bool))
			);

#endregion

#region IsOpen

		public bool IsOpen
		{
			get => (bool)GetValue(IsOpenProperty);
			set => SetValue(IsOpenProperty, value);
		}

		public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(
			"IsOpen",
			typeof(bool),
			typeof(AppBar),
			new FrameworkPropertyMetadata(default(bool))
		);

#endregion

#region ClosedDisplayMode

		public AppBarClosedDisplayMode ClosedDisplayMode
		{
			get => (AppBarClosedDisplayMode)GetValue(ClosedDisplayModeProperty);
			set => SetValue(ClosedDisplayModeProperty, value);
		}

		public static DependencyProperty ClosedDisplayModeProperty { get; } =
			DependencyProperty.Register(
				"ClosedDisplayMode",
				typeof(AppBarClosedDisplayMode),
				typeof(AppBar),
				new FrameworkPropertyMetadata(AppBarClosedDisplayMode.Compact)
			);

#endregion

#region LightDismissOverlayMode

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
			set => SetValue(LightDismissOverlayModeProperty, value);
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
			DependencyProperty.Register(
				"LightDismissOverlayMode",
				typeof(LightDismissOverlayMode),
				typeof(AppBar),
				new FrameworkPropertyMetadata(default(LightDismissOverlayMode))
			);

#endregion

		public AppBarTemplateSettings TemplateSettings { get; }

		public event EventHandler<object> Closed;
		public event EventHandler<object> Opened;
		public event EventHandler<object> Closing;
		public event EventHandler<object> Opening;

		protected virtual void OnClosed(object e)
		{
			Closed?.Invoke(this, e);
		}

		protected virtual void OnOpened(object e)
		{
			Opened?.Invoke(this, e);
		}

		protected virtual void OnClosing(object e)
		{
			Closing?.Invoke(this, e);
		}

		protected virtual void OnOpening(object e)
		{
			Opening?.Invoke(this, e);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_compactHeight = ResourceResolver.ResolveTopLevelResourceDouble("AppBarThemeCompactHeight");
			_minimalHeight = ResourceResolver.ResolveTopLevelResourceDouble("AppBarThemeMinimalHeight");

			UpdateTemplateSettings();

#if HAS_NATIVE_COMMANDBAR
			 _isNativeTemplate = Uno.UI.Extensions.DependencyObjectExtensions
				 .FindFirstChild<NativeCommandBarPresenter>(this) != null;
#endif
		}

		private void UpdateTemplateSettings()
		{
			var contentHeight = (ContentTemplateRoot as FrameworkElement)?.ActualHeight ?? ActualHeight;
			TemplateSettings.ClipRect = new Windows.Foundation.Rect(0, 0, ActualWidth, contentHeight);

			var compactVerticalDelta = _compactHeight - contentHeight;
			TemplateSettings.CompactVerticalDelta = compactVerticalDelta;
			TemplateSettings.NegativeCompactVerticalDelta = -compactVerticalDelta;

			var minimalVerticalDelta = _minimalHeight - contentHeight;
			TemplateSettings.MinimalVerticalDelta = minimalVerticalDelta;
			TemplateSettings.NegativeMinimalVerticalDelta = -minimalVerticalDelta;

			TemplateSettings.HiddenVerticalDelta = -contentHeight;
			TemplateSettings.NegativeHiddenVerticalDelta = contentHeight;
		}

#if HAS_NATIVE_COMMANDBAR
		protected override Size MeasureOverride(Size availableSize)
		{
			if (_isNativeTemplate)
			{
				var size = base.MeasureOverride(availableSize);
				return size;
			}

			// On WinUI the CommandBar does not constraints its children (it only clips them)
			// (It's the responsibility of each child to constraint itself)
			// Note: This override is used only for the XAML command bar, not the native!
			var infinity = new Size(double.PositiveInfinity, double.PositiveInfinity);
			var result = base.MeasureOverride(infinity);

			var height = ClosedDisplayMode switch
			{
				AppBarClosedDisplayMode.Compact => _compactHeight,
				AppBarClosedDisplayMode.Minimal => _minimalHeight,
				_ => 0
			};

			return new Size(result.Width, height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var size = base.ArrangeOverride(finalSize);
			return size;
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => !_isNativeTemplate;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
#endif
	}
}
