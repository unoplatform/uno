#pragma warning disable CS0109

#if !NET461
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
using Windows.UI.Input;
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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
			InvalidateTextBlock();
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

			if (this.ReadLocalValue(TextProperty) is UnsetValue)
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
#if __WASM__
		// As on wasm the TextElements are UIElement, when the hosting TextBlock will capture the pointer on Pressed,
		// the original source of the Release event will be this TextBlock (and we won't receive 'pointerup' nor 'click'
		// events on the Hyperlink itself - On FF we will still get the 'click').
		// To workaround that, we subscribe to the events directly on the Hyperlink, and make the Capture on this hyperlink.

		private void UpdateHyperlinks() { } // Events are subscribed in Hyperlink's ctor.

		internal static readonly PointerEventHandler OnPointerPressed = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is Hyperlink hyperlink
				&& e.GetCurrentPoint(hyperlink).Properties.IsLeftButtonPressed
				&& hyperlink.CapturePointer(e.Pointer))
			{
				hyperlink.SetPointerPressed(e.Pointer);
				e.Handled = true;
				// hyperlink.CompleteGesture(); No needs to complete the gesture as the TextBlock won't even receive the Pressed.
			}
		};

		internal static readonly PointerEventHandler OnPointerReleased = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is Hyperlink hyperlink
				&& hyperlink.IsCaptured(e.Pointer))
			{
				// Un UWP we don't get the Tapped event, so make sure to abort it
				(hyperlink.GetParent() as TextBlock)?.CompleteGesture();

				hyperlink.ReleasePointerPressed(e.Pointer);
			}

			// e.Handled = true; ==> On UWP the pointer released is **NOT** handled
		};

		internal static readonly PointerEventHandler OnPointerCaptureLost = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is Hyperlink hyperlink)
			{
				var handled = hyperlink.AbortPointerPressed(e.Pointer);

				e.Handled = handled;
			}
		};
#else
		private static readonly PointerEventHandler OnPointerPressed = (object sender, PointerRoutedEventArgs e) =>
		{
			if (!(sender is TextBlock that) || !that.HasHyperlink)
			{
				return;
			}

			var point = e.GetCurrentPoint(that);
			if (!point.Properties.IsLeftButtonPressed)
			{
				return;
			}

			var hyperlink = that.FindHyperlinkAt(point.Position);
			if (hyperlink is null)
			{
				return;
			}

			if (!that.CapturePointer(e.Pointer))
			{
				return;
			}

			hyperlink.SetPointerPressed(e.Pointer);
			e.Handled = true;
			that.CompleteGesture(); // Make sure to mute Tapped
		};

		private static readonly PointerEventHandler OnPointerReleased = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is TextBlock that
				&& that.IsCaptured(e.Pointer))
			{
				// On UWP we don't get the Tapped event, so make sure to abort it.
				that.CompleteGesture();

				// On UWP we don't get any CaptureLost, so make sure to manually release the capture silently
				that.ReleasePointerCapture(e.Pointer, muteEvent: true);

				// KNOWN ISSUE:
				// On UWP the 'click' event is raised **after** the PointerReleased ... but deferring the event on the Dispatcher
				// would move it after the PointerExited. So prefer to raise it before (actually like a Button).
				if (!(that.FindHyperlinkAt(e.GetCurrentPoint(that).Position)?.ReleasePointerPressed(e.Pointer) ?? false))
				{
					// We failed to find the hyperlink that made this capture but we ** silently ** removed the capture,
					// so we won't receive the CaptureLost. So make sure to AbortPointerPressed on the Hyperlink which made the capture.
					that.AbortHyperlinkCaptures(e.Pointer);
				}
			}

			// e.Handled = true; ==> On UWP the pointer released is **NOT** handled
		};

		private static readonly PointerEventHandler OnPointerCaptureLost = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is TextBlock that)
			{
				e.Handled = that.AbortHyperlinkCaptures(e.Pointer);
			}
		};

		private bool AbortHyperlinkCaptures(Pointer pointer)
		{
			var aborted = false;
			foreach (var hyperlink in _hyperlinks.ToList()) // .ToList() : for a strange reason on WASM the collection gets modified
			{
				aborted |= hyperlink.hyperlink.AbortPointerPressed(pointer);
			}

			return aborted;
		}

		private readonly List<(int start, int end, Hyperlink hyperlink)> _hyperlinks = new List<(int start, int end, Hyperlink hyperlink)>();

		private void UpdateHyperlinks()
		{
			if (UseInlinesFastPath) // i.e. no Inlines
			{
				if (HasHyperlink)
				{
					RemoveHandler(PointerPressedEvent, OnPointerPressed);
					RemoveHandler(PointerReleasedEvent, OnPointerReleased);
					RemoveHandler(PointerCaptureLostEvent, OnPointerCaptureLost);

					// Make sure to clear the pressed state of removed hyperlinks
					foreach (var hyperlink in _hyperlinks)
					{
						hyperlink.hyperlink.AbortAllPointerPressed();
					}

					_hyperlinks.Clear();
				}

				return;
			}

			var previousHasHyperlinks = HasHyperlink;
			var previousHyperLinks = _hyperlinks.Select(h => h.hyperlink).ToList();
			_hyperlinks.Clear();

			var start = 0;
			foreach (var inline in Inlines.SelectMany(InlineExtensions.Enumerate))
			{
				switch (inline)
				{
					case Hyperlink hyperlink:
						previousHyperLinks.Remove(hyperlink);
						_hyperlinks.Add((start, start + hyperlink.GetText().Length, hyperlink));
						break;
					case Span span:
						break;
					default: // Leaf node
						start += inline.GetText().Length;
						break;
				}
			}

			// Make sure to clear the pressed state of removed hyperlinks
			foreach (var removed in previousHyperLinks)
			{
				removed.AbortAllPointerPressed();
			}

			// Update events subscriptions if needed
			// Note: we subscribe to those events only if needed as they increase marshaling on Android and WASM
			if (HasHyperlink && !previousHasHyperlinks)
			{
				InsertHandler(PointerPressedEvent, OnPointerPressed);
				InsertHandler(PointerReleasedEvent, OnPointerReleased);
				InsertHandler(PointerCaptureLostEvent, OnPointerCaptureLost);
			}
			else if (!HasHyperlink && previousHasHyperlinks)
			{
				RemoveHandler(PointerPressedEvent, OnPointerPressed);
				RemoveHandler(PointerReleasedEvent, OnPointerReleased);
				RemoveHandler(PointerCaptureLostEvent, OnPointerCaptureLost);
			}
		}

		private bool HasHyperlink => _hyperlinks.Any();

		private Hyperlink FindHyperlinkAt(Point point)
		{
			var characterIndex = GetCharacterIndexAtPoint(point);
			var hyperlink = _hyperlinks
				.FirstOrDefault(h => h.start <= characterIndex && h.end > characterIndex)
				.hyperlink;

			return hyperlink;
		}
#endif
		#endregion

		private void InvalidateTextBlock()
		{
			InvalidateTextBlockPartial();
			this.InvalidateMeasure();
		}

		partial void InvalidateTextBlockPartial();

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TextBlockAutomationPeer(this);
		}

		public override string GetAccessibilityInnerText()
		{
			return Text;
		}

		// This approximates UWP behavior
		private protected override double GetActualWidth() => DesiredSize.Width;
		private protected override double GetActualHeight() => DesiredSize.Height;
	}
}
#endif
