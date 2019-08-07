using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Documents
{
	public partial class Hyperlink : Span
	{
		#region Static

		private static Brush _defaultForeground;
		private static Brush DefaultForeground
		{
			get
			{
				if (_defaultForeground == null)
				{
#if __IOS__ || __ANDROID__
					_defaultForeground = GetDefaultForeground();
#else
					_defaultForeground = null;
#endif
				}

				return _defaultForeground;
			}
		}

		#endregion

		public event TypedEventHandler<Hyperlink, HyperlinkClickEventArgs> Click;

#if !__WASM__
		public Hyperlink()
		{
			OnUnderlineStyleChanged();
			Foreground = DefaultForeground;
		}
#endif

		#region NavigateUri

		public Uri NavigateUri
		{
			get => (Uri)this.GetValue(NavigateUriProperty);
			set => this.SetValue(NavigateUriProperty, value);
		}

		public static DependencyProperty NavigateUriProperty { get; } =
			DependencyProperty.Register(
				"NavigateUri",
				typeof(Uri),
				typeof(Hyperlink),
				new FrameworkPropertyMetadata(
					defaultValue: default(Uri),
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((Hyperlink)s).OnNavigateUriChanged()
				)
			);

		protected virtual void OnNavigateUriChanged()
		{
			OnNavigateUriChangedPartial();
		}

		partial void OnNavigateUriChangedPartial();

		#endregion

		#region UnderlineStyle

		public UnderlineStyle UnderlineStyle
		{
			get => (UnderlineStyle)this.GetValue(UnderlineStyleProperty);
			set => this.SetValue(UnderlineStyleProperty, value);
		}

		internal static DependencyProperty UnderlineStyleProperty { get; } =
			DependencyProperty.Register(
				"UnderlineStyle",
				typeof(UnderlineStyle),
				typeof(Hyperlink),
				new FrameworkPropertyMetadata(
					defaultValue: UnderlineStyle.Single,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((Hyperlink)s).OnUnderlineStyleChanged()
				)
			);

		internal protected virtual void OnUnderlineStyleChanged()
		{
			TextDecorations = UnderlineStyle == UnderlineStyle.Single
				? Text.TextDecorations.Underline
				: Text.TextDecorations.None;
		}

		#endregion

		protected override void OnStyleChanged()
		{
			if (Style == null)
			{
				base.Style = Style.DefaultStyleForType(typeof(Hyperlink));
				base.Style.ApplyTo(this);
			}
		}

		#region Click


#if __IOS__ || __WASM__
		internal new bool IsPressed { get; private set; }
#else
		internal bool IsPressed { get; private set; }
#endif

		internal void OnPointerPressed(PointerRoutedEventArgs e)
		{
			if (!IsPressed)
			{
				this.SetValue(ForegroundProperty, GetPressedForeground(), DependencyPropertyValuePrecedences.Animations);
				e.Handled = true;
				IsPressed = true;
			}
		}

		internal void OnPointerReleased(PointerRoutedEventArgs e)
		{
			if (IsPressed)
			{
				this.ClearValue(ForegroundProperty, DependencyPropertyValuePrecedences.Animations);
				OnClick();
				e.Handled = true;
				IsPressed = false;
			}

		}

		internal void OnPointerCanceled(PointerRoutedEventArgs e)
		{
			if (IsPressed)
			{
				this.ClearValue(ForegroundProperty, DependencyPropertyValuePrecedences.Animations);
				e.Handled = true;
				IsPressed = false;
			}
		}

		private void OnClick()
		{
			Click?.Invoke(this, new HyperlinkClickEventArgs() { OriginalSource = this });

#if !__WASM__  // handled natively in WASM/Html
			if (NavigateUri != null)
			{
				Launcher.LaunchUriAsync(NavigateUri);
			}
#endif
		}

		private Brush GetPressedForeground()
		{
#if XAMARIN
			var normalColor = (Foreground as SolidColorBrush).ColorWithOpacity;
			var pressedColor = Color.FromArgb((byte)(normalColor.A / 2), normalColor.R, normalColor.G, normalColor.B);
			return new SolidColorBrush(pressedColor);
#else
			return null;
#endif
		}

#endregion
	}
}
