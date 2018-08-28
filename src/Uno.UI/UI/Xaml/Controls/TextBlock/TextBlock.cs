#pragma warning disable CS0109

#if !NET46
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Uno.Disposables;
using Uno.Extensions;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml;
using Uno.UI.DataBinding;
using System;
using Uno.UI;
using System.Collections;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Automation.Peers;

#if XAMARIN_IOS
using UIKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Text")]
	public partial class TextBlock : DependencyObject
	{
		private InlineCollection _inlines;
		private string _inlinesText; // Text derived from the content of Inlines

#if !__WASM__
		public TextBlock()
		{
			IFrameworkElementHelper.Initialize(this);
			InitializeProperties();

			InitializePartial();
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();			
			PointerPressed += OnPointerPressed;
			PointerReleased += OnPointerReleased;
			PointerCanceled += OnPointerCanceled;
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
			PointerPressed -= OnPointerPressed;
			PointerReleased -= OnPointerReleased;
			PointerCanceled -= OnPointerCanceled;
		}
#endif

		/// <summary>
		/// Calls On[Property]Changed for most DPs to ensure the values are correctly applied to the native control
		/// </summary>
		private void InitializeProperties()
		{
			OnForegroundChanged();
			OnFontFamilyChanged();
			OnFontWeightChanged();
			OnFontStyleChanged();
			OnFontSizeChanged();
			OnTextTrimmingChanged();
			OnTextWrappingChanged();
			OnMaxLinesChanged();
			OnTextAlignmentChanged();
			OnTextChanged(string.Empty, Text);
		}

#region Inlines

		/// <summary>
		/// Gets an InlineCollection containing the top-level Inline elements that comprise the contents of the TextBlock.
		/// </summary>
		/// <remarks>
		/// Accessing this property initializes an InlineCollection, whose content will be synchronized with the Text.
		/// This can have a significant impact on performance. Only access this property if absolutely necessary.
		/// </remarks>
		public InlineCollection Inlines
		{
			get
			{
				if (_inlines == null)
				{
					_inlines = new InlineCollection(this);
					UpdateInlines(Text);
				}

				return _inlines;
			}
		}

		internal void InvalidateInlines()
		{
			OnInlinesChanged();
		}

		private void OnInlinesChanged()
		{
			Text = _inlinesText = string.Concat(Inlines.Select(InlineExtensions.GetText));
			UpdateHyperlinks();

			OnInlinesChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnInlinesChangedPartial();

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
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontStyle.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnFontStyleChanged()
				)
			);

		private void OnFontStyleChanged()
		{
			OnFontStyleChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnFontStyleChangedPartial();

#endregion

#region TextWrapping Dependency Property

		public TextWrapping TextWrapping
		{
			get { return (TextWrapping)this.GetValue(TextWrappingProperty); }
			set { this.SetValue(TextWrappingProperty, value); }
		}

		public static readonly DependencyProperty TextWrappingProperty =
			DependencyProperty.Register(
				"TextWrapping",
				typeof(TextWrapping),
				typeof(TextBlock),
				new PropertyMetadata(
					defaultValue: TextWrapping.NoWrap,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnTextWrappingChanged()
				)
			);

		private void OnTextWrappingChanged()
		{
			OnTextWrappingChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnTextWrappingChangedPartial();

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
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontWeights.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnFontWeightChanged()
				)
			);

		private void OnFontWeightChanged()
		{
			OnFontWeightChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnFontWeightChangedPartial();

#endregion

#region Text Dependency Property

		public
#if XAMARIN_IOS
			new
#endif
		string Text
		{
			get { return (string)this.GetValue(TextProperty); }
			set { this.SetValue(TextProperty, value); }
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
					coerceValueCallback: CoerceText,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnTextChanged((string)e.OldValue, (string)e.NewValue)
				)
			);

		internal static object CoerceText(DependencyObject dependencyObject, object baseValue)
		{
			return baseValue is string
				? baseValue
				: string.Empty;
		}

		protected virtual void OnTextChanged(string oldValue, string newValue)
		{
			UpdateInlines(newValue);

			OnTextChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnTextChangedPartial();

#endregion

#region FontFamily Dependency Property

#if XAMARIN_IOS
		/// <summary>
		/// Supported font families: http://iosfonts.com/
		/// </summary>
#endif
		public FontFamily FontFamily
		{
			get { return (FontFamily)this.GetValue(FontFamilyProperty); }
			set { this.SetValue(FontFamilyProperty, value); }
		}

		public static readonly DependencyProperty FontFamilyProperty =
			DependencyProperty.Register(
				"FontFamily",
				typeof(FontFamily),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontFamily.Default,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnFontFamilyChanged()
				)
			);

		private void OnFontFamilyChanged()
		{
			OnFontFamilyChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnFontFamilyChangedPartial();

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
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: (double)11,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnFontSizeChanged()
				)
			);

		private void OnFontSizeChanged()
		{
			OnFontSizeChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnFontSizeChangedPartial();

#endregion

#region MaxLines Dependency Property

		public int MaxLines
		{
			get { return (int)this.GetValue(MaxLinesProperty); }
			set { this.SetValue(MaxLinesProperty, value); }
		}

		public static readonly DependencyProperty MaxLinesProperty =
			DependencyProperty.Register(
				"MaxLines",
				typeof(int),
				typeof(TextBlock),
				new PropertyMetadata(
					defaultValue: 0,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnMaxLinesChanged()
				)
			);

		private void OnMaxLinesChanged()
		{
			OnMaxLinesChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnMaxLinesChangedPartial();

#endregion

#region TextTrimming Dependency Property

		public TextTrimming TextTrimming
		{
			get { return (TextTrimming)this.GetValue(TextTrimmingProperty); }
			set { this.SetValue(TextTrimmingProperty, value); }
		}

		public static readonly DependencyProperty TextTrimmingProperty =
			DependencyProperty.Register(
				"TextTrimming",
				typeof(TextTrimming),
				typeof(TextBlock),
				new PropertyMetadata(
					defaultValue: TextTrimming.None,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnTextTrimmingChanged()
				)
			);

		private void OnTextTrimmingChanged()
		{
			OnTextTrimmingChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnTextTrimmingChangedPartial();

#endregion

#region Foreground Dependency Property

		public
#if __ANDROID_23__
		new
#endif
		Brush Foreground
		{
			get { return (Brush)this.GetValue(ForegroundProperty); }
			set
			{
				if (!(Foreground is SolidColorBrush))
				{
					throw new NotSupportedException();
				}

				this.SetValue(ForegroundProperty, value);
			}
		}

		public static readonly DependencyProperty ForegroundProperty =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Black,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnForegroundChanged()
				)
			);

		private void OnForegroundChanged()
		{
			OnForegroundChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnForegroundChangedPartial();

#endregion

#region TextAlignment Dependency Property

		public new TextAlignment TextAlignment
		{
			get { return (TextAlignment)this.GetValue(TextAlignmentProperty); }
			set { this.SetValue(TextAlignmentProperty, value); }
		}

		public static readonly DependencyProperty TextAlignmentProperty =
			DependencyProperty.Register(
				"TextAlignment",
				typeof(TextAlignment),
				typeof(TextBlock),
				new PropertyMetadata(
					defaultValue: TextAlignment.Left,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnTextAlignmentChanged()
				)
			);

		private void OnTextAlignmentChanged()
		{
			OnTextAlignmentChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnTextAlignmentChangedPartial();

#endregion

#region LineHeight Dependency Property

		public double LineHeight
		{
			get { return (double)GetValue(LineHeightProperty); }
			set { SetValue(LineHeightProperty, value); }
		}

		public static readonly DependencyProperty LineHeightProperty =
			DependencyProperty.Register("LineHeight", typeof(double), typeof(TextBlock), new PropertyMetadata(0d,
				propertyChangedCallback: (s, e) => ((TextBlock)s).OnLineHeightChanged())
			);

		private void OnLineHeightChanged()
		{
			OnLineHeightChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnLineHeightChangedPartial();

#endregion

#region LineStackingStrategy Dependency Property

		public LineStackingStrategy LineStackingStrategy
		{
			get { return (LineStackingStrategy)GetValue(LineStackingStrategyProperty); }
			set { SetValue(LineStackingStrategyProperty, value); }
		}

		public static readonly DependencyProperty LineStackingStrategyProperty =
			DependencyProperty.Register("LineStackingStrategy", typeof(LineStackingStrategy), typeof(TextBlock), new PropertyMetadata(LineStackingStrategy.MaxHeight,
				propertyChangedCallback: (s, e) => ((TextBlock)s).OnLineStackingStrategyChanged())
			);

		private void OnLineStackingStrategyChanged()
		{
			OnLineStackingStrategyChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnLineStackingStrategyChangedPartial();

#endregion

#region Padding Dependency Property

		public Thickness Padding
		{
			get { return (Thickness)this.GetValue(PaddingProperty); }
			set { this.SetValue(PaddingProperty, value); }
		}

		public static DependencyProperty PaddingProperty =
			DependencyProperty.Register(
				"Padding",
				typeof(Thickness),
				typeof(TextBlock),
				new PropertyMetadata((Thickness)Thickness.Empty, propertyChangedCallback: (s, e) => ((TextBlock)s).OnPaddingChanged())
			);

		private void OnPaddingChanged()
		{
			OnPaddingChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnPaddingChangedPartial();

#endregion

#region CharacterSpacing Dependency Property

		public int CharacterSpacing
		{
			get { return (int)this.GetValue(CharacterSpacingProperty); }
			set { this.SetValue(CharacterSpacingProperty, value); }
		}

		public static DependencyProperty CharacterSpacingProperty =
			DependencyProperty.Register(
				"CharacterSpacing",
				typeof(int),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnCharacterSpacingChanged()
				)
			);

		private void OnCharacterSpacingChanged()
		{
			OnCharacterSpacingChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnCharacterSpacingChangedPartial();

		#endregion

		#region TextDecorations

		public TextDecorations TextDecorations
		{
			get { return (TextDecorations)this.GetValue(TextDecorationsProperty); }
			set { this.SetValue(TextDecorationsProperty, value); }
		}

		public static DependencyProperty TextDecorationsProperty =
			DependencyProperty.Register(
				"TextDecorations",
				typeof(int),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextDecorations.None,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnTextDecorationsChanged()
				)
			);

		private void OnTextDecorationsChanged()
		{
			OnTextDecorationsChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnTextDecorationsChangedPartial();

		#endregion

		/// <summary>
		/// Gets whether the TextBlock is using the fast path in which Inlines
		/// have not been initialized and don't need to be synchronized.
		/// </summary>
		private bool UseInlinesFastPath => _inlines == null;

		/// <summary>
		/// Gets the inlines which affect the typography of the TextBlock.
		/// </summary>
		private IEnumerable<(Inline inline, int start, int end)> GetEffectiveInlines()
		{
			if (UseInlinesFastPath)
			{
				yield break;
			}

			var start = 0;
			foreach (var inline in Inlines.SelectMany(InlineExtensions.Enumerate))
			{
				if (inline.HasTypographicalEffectWithin(this))
				{
					yield return (inline, start, start + inline.GetText().Length);
				}

				if (inline is Run || inline is LineBreak)
				{
					start += inline.GetText().Length;
				}
			}
		}

		private void UpdateInlines(string text)
		{
			if (UseInlinesFastPath)
			{
				return;
			}

			if (this.ReadLocalValue(TextProperty) == DependencyProperty.UnsetValue)
			{
				Inlines.Clear();
				ClearTextPartial();
			}
			else if (text != _inlinesText)
			{
				// Inlines must be updated
				Inlines.Clear();
				ClearTextPartial();
				Inlines.Add(new Run { Text = text });
			}
		}

		partial void ClearTextPartial();

#region Hyperlinks

		private readonly List<(int start, int end, Hyperlink hyperlink)> _hyperlinks = new List<(int start, int end, Hyperlink hyperlink)>();
		private Hyperlink _pressedHyperlink;

		private void UpdateHyperlinks()
		{
			if (UseInlinesFastPath)
			{
				return;
			}

			_hyperlinks.Clear();

			var start = 0;
			foreach (var inline in Inlines.SelectMany(InlineExtensions.Enumerate))
			{
				switch (inline)
				{
					case Hyperlink hyperlink:
						_hyperlinks.Add((start, start + hyperlink.GetText().Length, hyperlink));
						break;
					case Span span:
						break;
					default: // Leaf node
						start += inline.GetText().Length;
						break;
				}
			}
		}

		private bool HasHyperlink => _hyperlinks.Any();

		private Hyperlink GetHyperlinkAtPoint(Point point)
		{
			if (!HasHyperlink)
			{
				return null;
			}

			var characterIndex = GetCharacterIndexAtPoint(point);
			return GetHyperlinkAtCharacterIndex(characterIndex);
		}
		
		private Hyperlink GetHyperlinkAtCharacterIndex(int characterIndex)
		{
			return _hyperlinks.FirstOrDefault(h => h.start <= characterIndex && h.end > characterIndex).hyperlink;
		}

		private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_pressedHyperlink = GetHyperlinkAtPoint(e.GetCurrentPoint(this).Position);
			_pressedHyperlink?.OnPointerPressed(e);
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_pressedHyperlink != null)
			{
				var releasedHyperlink = GetHyperlinkAtPoint(e.GetCurrentPoint(this).Position);
				if (releasedHyperlink == _pressedHyperlink)
				{
					_pressedHyperlink.OnPointerReleased(e);
				}
				else
				{
					_pressedHyperlink.OnPointerCanceled(e);
				}

				_pressedHyperlink = null;
			}
		}

		private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			if (_pressedHyperlink != null)
			{
				_pressedHyperlink.OnPointerCanceled(e);
				_pressedHyperlink = null;
			}
		}

		#endregion

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TextBlockAutomationPeer(this);
		}

		public override string GetAccessibilityInnerText()
		{
			return Text;
		}
	}
}
#endif
