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
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using Windows.UI.Xaml.Automation.Peers;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
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
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextElement)s).OnFontFamilyChanged()
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

		#region FontStretch Dependency Property

		public FontStretch FontStretch
		{
			get => GetFontStretchValue();
			set => SetFontStretchValue(value);
		}

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnFontStretchChanged), DefaultValue = FontStretch.Normal, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty FontStretchProperty { get; } = CreateFontStretchProperty();

		protected virtual void OnFontStretchChanged()
		{
			OnFontStretchChangedPartial();
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
			get => (Brush)GetValue(ForegroundProperty);
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
				options: FrameworkPropertyMetadataOptions.Inherits,
				propertyChangedCallback: (instance, args) => ((TextElement)instance).OnForegroundChanged()
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

		public static DependencyProperty CharacterSpacingProperty { get; } =
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
			get => GetTextDecorationsValue();
			set => SetTextDecorationsValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = TextDecorations.None, Options = FrameworkPropertyMetadataOptions.Inherits, ChangedCallback = true, ChangedCallbackName = nameof(OnTextDecorationsChanged))]
		public static DependencyProperty TextDecorationsProperty { get; } = CreateTextDecorationsProperty();

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

		public static DependencyProperty BaseLineAlignmentProperty { get; } =
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

		#region AllowFocusOnInteraction Dependency Property

		/// <summary>
		/// Identifies for the AllowFocusOnInteraction dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = true, Options = FrameworkPropertyMetadataOptions.Inherits)]
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
