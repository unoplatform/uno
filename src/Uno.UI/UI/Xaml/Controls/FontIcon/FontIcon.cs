using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class FontIcon : IconElement
	{
		private readonly TextBlock _textBlock;

		public FontIcon()
		{
			_textBlock = new TextBlock();

			AddIconElementView(_textBlock);

			Loaded += FontIcon_Loaded;
		}

		private void FontIcon_Loaded(object sender, RoutedEventArgs e)
		{
			SynchronizeProperties();
		}

		private void SynchronizeProperties()
		{
			_textBlock.Text = Glyph;
			_textBlock.FontSize = FontSize;
			_textBlock.FontStyle = FontStyle;
			_textBlock.FontFamily = FontFamily;
			_textBlock.Foreground = Foreground;

			_textBlock.VerticalAlignment = VerticalAlignment.Center;
			_textBlock.HorizontalAlignment = HorizontalAlignment.Center;
			_textBlock.TextAlignment = Windows.UI.Xaml.TextAlignment.Center;
		}

		#region Glyph

		public string Glyph
		{
			get { return (string)this.GetValue(GlyphProperty); }
			set { this.SetValue(GlyphProperty, value); }
		}

		public static DependencyProperty GlyphProperty { get ; } =
			DependencyProperty.Register("Glyph", typeof(string), typeof(FontIcon), new FrameworkPropertyMetadata(null,
				(s, e) => ((FontIcon)s).OnGlyphChanged((string)e.NewValue)));

		private void OnGlyphChanged(string newValue)
		{
			if (_textBlock != null)
			{
				_textBlock.Text = newValue;
			}
		}

		#endregion

		#region FontFamily

		public FontFamily FontFamily
		{
			get { return (FontFamily)this.GetValue(FontFamilyProperty); }
			set { this.SetValue(FontFamilyProperty, value); }
		}

		public static DependencyProperty FontFamilyProperty { get ; } =
			DependencyProperty.Register(
				name: nameof(FontFamily),
				propertyType: typeof(FontFamily),
				ownerType: typeof(FontIcon),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: new FontFamily(Uno.UI.FeatureConfiguration.Font.SymbolsFont),
					propertyChangedCallback: (s, e) => ((FontIcon)s).OnFontFamilyChanged((FontFamily)e.NewValue)
				)
		);

		private void OnFontFamilyChanged(FontFamily newValue)
		{
			if (_textBlock != null)
			{
				_textBlock.FontFamily = newValue; 
			}
		}

		#endregion

		#region FontStyle

		public FontStyle FontStyle
		{
			get { return (FontStyle)GetValue(FontStyleProperty); }
			set { SetValue(FontStyleProperty, value); }
		}

		public static DependencyProperty FontStyleProperty { get ; } =
			DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(FontIcon), new FrameworkPropertyMetadata(FontStyle.Normal,
				(s, e) => ((FontIcon)s).OnFontStyleChanged((FontStyle)e.NewValue)));

		private void OnFontStyleChanged(FontStyle newValue)
		{
			if (_textBlock != null)
			{
				_textBlock.FontStyle = newValue;
			}
		}

		#endregion

		#region FontSize

		public double FontSize
		{
			get { return (double)this.GetValue(FontSizeProperty); }
			set { this.SetValue(FontSizeProperty, value); }
		}

		public static DependencyProperty FontSizeProperty { get ; } =
			DependencyProperty.Register("FontSize", typeof(double), typeof(FontIcon), new FrameworkPropertyMetadata(15.0,
				(s, e) => ((FontIcon)s).OnFontSizeChanged((double)e.NewValue)));

		private void OnFontSizeChanged(double newValue)
		{
			if (_textBlock != null)
			{
				_textBlock.FontSize = newValue;
			}
		}

		#endregion

		protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
		{
			var solidColorBrush = e.NewValue as SolidColorBrush;
			if (solidColorBrush != null && _textBlock != null)
			{
				_textBlock.Foreground = solidColorBrush;
			}
		}
	}
}
