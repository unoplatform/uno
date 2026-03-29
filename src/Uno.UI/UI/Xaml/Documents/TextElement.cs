#if IS_UNIT_TESTS
#pragma warning disable CS0067
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.Disposables;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation.Peers;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#else
using Color = System.Drawing.Color;
#endif

#if __WASM__
using BaseClass = Microsoft.UI.Xaml.UIElement;
#else
using BaseClass = Microsoft.UI.Xaml.DependencyObject;
#endif

namespace Microsoft.UI.Xaml.Documents
{
	public abstract partial class TextElement : BaseClass, IThemeChangeAware
	{
		public TextElement()
		{
			SetDefaultForeground(ForegroundProperty);
#if !__WASM__
			InitializeBinder();
#endif
		}

		#region FontFamily Dependency Property
		public FontFamily FontFamily
		{
			get { return (FontFamily)this.GetValue(FontFamilyProperty); }
			set { this.SetValue(FontFamilyProperty, value); }
		}

		public static DependencyProperty FontFamilyProperty { get; } =
			DependencyProperty.Register(
				"FontFamily",
				typeof(FontFamily),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: FontFamily.Default,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) =>
					{
						var te = (TextElement)s;
						te.OnFontFamilyChanged();
						te.OnTextFormattingPropertyChanged("FontFamily", e.NewValue);
					}
				)
			);

		// While font family itself didn't change, OnFontFamilyChanged will invalidate whatever
		// needed for the rendering to happen correctly on the next frame.
		internal void OnFontLoaded() => OnFontFamilyChanged();

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

		public static DependencyProperty FontStyleProperty { get; } =
			DependencyProperty.Register(
				"FontStyle",
				typeof(FontStyle),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: FontStyle.Normal,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) =>
					{
						var te = (TextElement)s;
						te.OnFontStyleChanged();
						te.OnTextFormattingPropertyChanged("FontStyle", e.NewValue);
					}
				)
			);

		protected virtual void OnFontStyleChanged()
		{
			OnFontStyleChangedPartial();
		}

		partial void OnFontStyleChangedPartial();

		#endregion

		#region FontStretch Dependency Property

		public FontStretch FontStretch
		{
			get => GetFontStretchValue();
			set => SetFontStretchValue(value);
		}

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnFontStretchChanged), DefaultValue = FontStretch.Normal)]
		public static DependencyProperty FontStretchProperty { get; } = CreateFontStretchProperty();

		protected virtual void OnFontStretchChanged()
		{
			OnFontStretchChangedPartial();
			OnTextFormattingPropertyChanged("FontStretch", FontStretch);
		}

		partial void OnFontStretchChangedPartial();

		#endregion

		#region FontSize Dependency Property

		public double FontSize
		{
			get { return (double)this.GetValue(FontSizeProperty); }
			set { this.SetValue(FontSizeProperty, value); }
		}

		public static DependencyProperty FontSizeProperty { get; } =
			DependencyProperty.Register(
				"FontSize",
				typeof(double),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: 14.0,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) =>
					{
						var te = (TextElement)s;
						te.OnFontSizeChanged();
						te.OnTextFormattingPropertyChanged("FontSize", e.NewValue);
					}
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
			get
			{
				// MUX ref: In WinUI, Foreground is a TextFormatting storage group property.
				// GetValue calls EnsureTextFormatting which returns the inherited value when
				// Foreground is not set locally. Uno replicates this via TextFormatting.
				// WinUI ref: CDependencyObject::GetPropertyOffset (storage group path).
				if (IsPropertyDefault(ForegroundProperty))
				{
					EnsureTextFormatting(ForegroundProperty, forGetValue: true);
					if (((ITextFormattingOwner)this).TextFormatting?.Foreground is { } inherited)
					{
						return inherited;
					}
				}
				return (Brush)GetValue(ForegroundProperty);
			}
			set
			{
				if (value != null && !(value is SolidColorBrush))
				{
					throw new InvalidOperationException("Specified brush is not a SolidColorBrush");
				}

				SetValue(ForegroundProperty, value);
			}
		}

		public static DependencyProperty ForegroundProperty { get; } = DependencyProperty.Register(
			nameof(Foreground),
			typeof(Brush),
			typeof(TextElement),
			new FrameworkPropertyMetadata(
				defaultValue: SolidColorBrushHelper.Black,
				// MUX ref: TextElement_Foreground uses IsInherited in WinUI's DP system.
				// Restoring Inherits ensures run.Foreground returns the inherited theme foreground
				// from ancestor Control/ContentPresenter via DP Inherits, matching WinUI behavior.
				options: FrameworkPropertyMetadataOptions.Inherits,
				propertyChangedCallback: (instance, args) =>
				{
					var te = (TextElement)instance;
					te.OnForegroundChanged();
					te.OnTextFormattingPropertyChanged("Foreground", args.NewValue);
				}
		));

		protected virtual void OnForegroundChanged()
		{
			// TODO: This is missing listening for brush changes?
			OnForegroundChangedPartial();
		}

		partial void OnForegroundChangedPartial();

		#endregion

		#region FontWeight Dependency Property

		public FontWeight FontWeight
		{
			get { return (FontWeight)this.GetValue(FontWeightProperty); }
			set { this.SetValue(FontWeightProperty, value); }
		}

		public static DependencyProperty FontWeightProperty { get; } =
			DependencyProperty.Register(
				"FontWeight",
				typeof(FontWeight),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: FontWeights.Normal,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) =>
					{
						var te = (TextElement)s;
						te.OnFontWeightChanged();
						te.OnTextFormattingPropertyChanged("FontWeight", e.NewValue);
					}
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

		public static DependencyProperty CharacterSpacingProperty { get; } =
			DependencyProperty.Register(
				"CharacterSpacing",
				typeof(int),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) =>
					{
						var te = (TextElement)s;
						te.OnCharacterSpacingChanged();
						te.OnTextFormattingPropertyChanged("CharacterSpacing", e.NewValue);
					}
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
			get => GetTextDecorationsValue();
			set => SetTextDecorationsValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = TextDecorations.None, ChangedCallback = true, ChangedCallbackName = nameof(OnTextDecorationsChanged))]
		public static DependencyProperty TextDecorationsProperty { get; } = CreateTextDecorationsProperty();

		protected virtual void OnTextDecorationsChanged()
		{
			OnTextDecorationsChangedPartial();
			OnTextFormattingPropertyChanged("TextDecorations", TextDecorations);
		}

		partial void OnTextDecorationsChangedPartial();

		#endregion

		#region Language Dependency Property

		public string Language
		{
			get => (string)GetValue(LanguageProperty);
			set => SetValue(LanguageProperty, value);
		}

		// MUX ref: TextElement_Language has IsInheritedProperty + IsStorageGroup flags.
		// Hand-written to add OnTextFormattingPropertyChanged callback.
		public static DependencyProperty LanguageProperty { get; } =
			DependencyProperty.Register(
				nameof(Language),
				typeof(string),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					default(string),
					propertyChangedCallback: (s, e) =>
					{
						var te = (TextElement)s;
						te.OnTextFormattingPropertyChanged("Language", e.NewValue);
					}
				)
			);

		#endregion

		#region BaseLineAlignment Dependency Property

		public BaseLineAlignment BaseLineAlignment
		{
			get => (BaseLineAlignment)GetValue(BaseLineAlignmentProperty);
			set => SetValue(BaseLineAlignmentProperty, value);
		}

		public static DependencyProperty BaseLineAlignmentProperty { get; } =
			DependencyProperty.Register(
				"BaseLineAlignment",
				typeof(BaseLineAlignment),
				typeof(TextElement),
				new FrameworkPropertyMetadata(
					defaultValue: BaseLineAlignment.Baseline,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnBaseLineAlignmentChanged()
				)
			);

		protected virtual void OnBaseLineAlignmentChanged()
		{
			OnBaseLineAlignmentChangedPartial();
		}

		partial void OnBaseLineAlignmentChangedPartial();

		#endregion

		#region AllowFocusOnInteraction Dependency Property

		/// <summary>
		/// Identifies for the AllowFocusOnInteraction dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = true)]
		public static DependencyProperty AllowFocusOnInteractionProperty { get; } = CreateAllowFocusOnInteractionProperty();

		/// <summary>
		/// Gets or sets a value that indicates whether the element automatically gets focus when the user interacts with it.
		/// </summary>
		public bool AllowFocusOnInteraction
		{
			get => GetAllowFocusOnInteractionValue();
			set => SetAllowFocusOnInteractionValue(value);
		}

		#endregion

		private string _name;

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnNameChangedPartial(value);
				}
			}
		}

		// WASM specific as on WASM BaseClass is UIElement

#if !__WASM__
		//UNO TODO: Implement GetOrCreateAutomationPeer on TextElement
		internal Automation.Peers.AutomationPeer GetOrCreateAutomationPeer()
		{
			return null;
		}

		//UNO TODO: Implement GetAccessKeyScopeOwner on TextElement
		internal DependencyObject GetAccessKeyScopeOwner()
		{
			return null;
		}
#endif

		partial void OnNameChangedPartial(string newValue);

		/// <summary>
		/// Retrieves the parent RichTextBox/CRichTextBlock/TextBlock.
		/// </summary>
		/// <returns>FrameworkElement or <see langword="null"/>.</returns>
		internal FrameworkElement GetContainingFrameworkElement()
		{
			var parent = this.GetParent();

			while (
				parent != null &&
				!(parent is RichTextBlock) &&
				!(parent is TextBlock))
			{
				parent = parent.GetParent();
			}

			return parent as FrameworkElement;
		}

		public void OnThemeChanged() => SetDefaultForeground(ForegroundProperty);

		private void SetDefaultForeground(DependencyProperty foregroundProperty)
		{
			if (this is Hyperlink hl)
			{
				// Hyperlink doesn't appear to inherit foreground from the parent.
				// So, we set this with ImplicitStyle precedence which is a higher precedence than Inheritance.
				hl.SetCurrentForeground();
			}

			((IDependencyObjectStoreProvider)this).Store.SetLastUsedTheme(Application.Current?.RequestedThemeForResources);
		}

		internal protected virtual List<AutomationPeer> AppendAutomationPeerChildren(int startPos, int endPos)
		{
			//return S_OK;
			return null;
		}
	}
}
