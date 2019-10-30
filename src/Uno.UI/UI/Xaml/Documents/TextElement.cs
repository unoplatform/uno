#if NET461
#pragma warning disable CS0067
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Uno.Disposables;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Uno.UI;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#elif __MACOS__
using View = Windows.UI.Xaml.UIElement;
using Color = Windows.UI.Color;
#else
using Color = System.Drawing.Color;
#endif

#if __WASM__
using BaseClass = Windows.UI.Xaml.UIElement;
#else
using BaseClass = Windows.UI.Xaml.DependencyObject;
#endif

namespace Windows.UI.Xaml.Documents
{
	public abstract partial class TextElement : BaseClass
	{
#if !__WASM__
		public TextElement()
		{
			InitializeBinder();
		}
#endif

		#region FontFamily Dependency Property
		public FontFamily FontFamily
		{
			get { return (FontFamily)this.GetValue(FontFamilyProperty); }
			set { this.SetValue(FontFamilyProperty, value); }
		}

		public static readonly DependencyProperty FontFamilyProperty =
			DependencyProperty.Register(
				"FontFamily",
				typeof(FontFamily),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: FontFamily.Default,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnFontFamilyChanged()
				)
			);

		protected virtual void OnFontFamilyChanged()
		{
			OnFontFamilyChangedPartial();
		}

		partial void OnFontFamilyChangedPartial();
		#endregion

		#region FontStyle Dependency Property

		public FontStyle FontStyle
		{
			get { return (FontStyle)this.GetValue(FontStyleProperty); }
			set { this.SetValue(FontStyleProperty, value); }
		}

		public static readonly DependencyProperty FontStyleProperty =
			DependencyProperty.Register(
				"FontStyle",
				typeof(FontStyle),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: Windows.UI.Text.FontStyle.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnFontStyleChanged()
				)
			);

		protected virtual void OnFontStyleChanged()
		{
			OnFontStyleChangedPartial();
		}

		partial void OnFontStyleChangedPartial();

#endregion

		#region FontSize Dependency Property

		public double FontSize
		{
			get { return (double)this.GetValue(FontSizeProperty); }
			set { this.SetValue(FontSizeProperty, value); }
		}

		public static readonly DependencyProperty FontSizeProperty =
			DependencyProperty.Register(
				"FontSize",
				typeof(double),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: (double)11,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnFontSizeChanged()
				)
			);

		protected virtual void OnFontSizeChanged()
		{
			OnFontSizeChangedPartial();
		}

		partial void OnFontSizeChangedPartial();

#endregion

		#region Foreground Dependency Property

		public Brush Foreground
		{
			get { return (Brush)this.GetValue(ForegroundProperty); }
			set
			{
				if (!(Foreground is SolidColorBrush))
				{
					throw new InvalidOperationException("Specified brush is not a SolidColorBrush");
				}

				this.SetValue(ForegroundProperty, value);
			}
		}

		public static readonly DependencyProperty ForegroundProperty =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Black,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnForegroundChanged()
				)
			);

		protected virtual void OnForegroundChanged()
		{
			OnForegroundChangedPartial();
		}

		partial void OnForegroundChangedPartial();

#endregion

		#region Style Dependency Property

		public Style Style
		{
			get { return (Style)this.GetValue(StyleProperty); }
			set { this.SetValue(StyleProperty, value); }
		}

		public static readonly DependencyProperty StyleProperty =
			DependencyProperty.Register(
				"Style",
				typeof(Style),
				typeof(TextElement),
				new PropertyMetadata(
					defaultValue: Style.DefaultStyleForType(typeof(TextElement)),
					propertyChangedCallback: (s, e) => ((TextElement)s).OnStyleChanged()
				)
			);

		protected virtual void OnStyleChanged()
		{
			if (Style == null)
			{
				Style = Style.DefaultStyleForType(typeof(TextElement));
				Style.ApplyTo(this);
			}

			OnStyleChangedPartial();
		}

		partial void OnStyleChangedPartial();

#endregion

		#region FontWeight Dependency Property

		public FontWeight FontWeight
		{
			get { return (FontWeight)this.GetValue(FontWeightProperty); }
			set { this.SetValue(FontWeightProperty, value); }
		}

		public static readonly DependencyProperty FontWeightProperty =
			DependencyProperty.Register(
				"FontWeight",
				typeof(FontWeight),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: FontWeights.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnFontWeightChanged()
				)
			);

		protected virtual void OnFontWeightChanged()
		{
			OnFontWeightChangedPartial();
		}

		partial void OnFontWeightChangedPartial();

#endregion

		#region CharacterSpacing Dependency Property

		public int CharacterSpacing
		{
			get => (int)GetValue(CharacterSpacingProperty);
			set => SetValue(CharacterSpacingProperty, value);
		}

		public static DependencyProperty CharacterSpacingProperty =
			DependencyProperty.Register(
				"CharacterSpacing",
				typeof(int),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnCharacterSpacingChanged()
				)
			);

		protected virtual void OnCharacterSpacingChanged()
		{
			OnCharacterSpacingChangedPartial();
		}

		partial void OnCharacterSpacingChangedPartial();

		#endregion

		#region TextDecorations

		public TextDecorations TextDecorations
		{
			get { return (TextDecorations)GetValue(TextDecorationsProperty); }
			set { SetValue(TextDecorationsProperty, value); }
		}

		public static DependencyProperty TextDecorationsProperty =
			DependencyProperty.Register(
				"TextDecorations",
				typeof(int),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: TextDecorations.None,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnTextDecorationsChanged()
				)
			);

		protected virtual void OnTextDecorationsChanged()
		{
			OnTextDecorationsChangedPartial();
		}

		partial void OnTextDecorationsChangedPartial();

		#endregion

		#region BaseLineAlignment Dependency Property

		public BaseLineAlignment BaseLineAlignment
		{
			get => (BaseLineAlignment)GetValue(BaseLineAlignmentProperty);
			set => SetValue(BaseLineAlignmentProperty, value);
		}

		public static DependencyProperty BaseLineAlignmentProperty =
			DependencyProperty.Register(
				"BaseLineAlignment",
				typeof(BaseLineAlignment),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: BaseLineAlignment.Baseline,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnBaseLineAlignmentChanged()
				)
			);

		protected virtual void OnBaseLineAlignmentChanged()
		{
			OnBaseLineAlignmentChangedPartial();
		}

		partial void OnBaseLineAlignmentChangedPartial();

		#endregion

#if !__WASM__ // WASM version is inheriting from UIElement, so it's already implementing it.
		public string Name { get; set; }
#endif
	}
}
