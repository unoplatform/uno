﻿#pragma warning disable CS0109

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uno.Disposables;
using Uno.Extensions;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml;
using Uno.UI.DataBinding;
using System;
using Uno.UI;
using System.Collections;
using System.Diagnostics;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Windows.Foundation;
using Windows.UI.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno;
using Uno.Foundation.Logging;

using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;
using Uno.UI.Helpers;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Inlines))]
	public partial class TextBlock : DependencyObject, IThemeChangeAware
	{
		private InlineCollection _inlines;
		private string _inlinesText; // Text derived from the content of Inlines
		private IDisposable _foregroundBrushChangedSubscription;

#if !__WASM__
		// Used for text selection which is handled natively
		private bool _isPressed;
#endif

		private Hyperlink _hyperlinkOver;
		private bool _subscribeToPointerEvents;

		private Action _foregroundChanged;

		private Run _reusableRun;
		private bool _skipInlinesChangedTextSetter;
		private Range _selection;
		private Range _selectionOnPointerPressed;

		// end can be less than or equal to start when the selection starts ahead and then goes back
		// see the selection in TextBox.skia.cs for more info
		private Range Selection
		{
			get => _selection;
			set
			{
				_selection = value;

				OnSelectionChanged();
			}
		}

		partial void OnSelectionChanged();

#if !UNO_REFERENCE_API
		public TextBlock()
		{
			IFrameworkElementHelper.Initialize(this);
			UpdateLastUsedTheme();

			_hyperlinks.CollectionChanged += HyperlinksOnCollectionChanged;

			InitializeProperties();

			InitializePartial();
		}

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
#endif

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
#if __WASM__
					SetText(string.Empty); // To clean up the text that is set directly on the TextBlock's <p>
#endif
					UpdateInlines(Text);

					SetupInlines();
				}

				return _inlines;
			}
		}

		partial void SetupInlines();

		internal void InvalidateInlines(bool updateText)
		{
			if (updateText)
			{
				if (Inlines.Count == 1 && Inlines[0] is Run run)
				{
					_inlinesText = run.Text;
				}
				else
				{
					_inlinesText = string.Concat(Inlines.Select(InlineExtensions.GetText));
				}

				if (!_skipInlinesChangedTextSetter)
				{
					Text = _inlinesText;
				}

				UpdateHyperlinks();
				Inlines.InvalidatePreorderTree();
			}

			OnInlinesChangedPartial();
			InvalidateTextBlock();
		}

		partial void OnInlinesChangedPartial();

		#endregion

		#region FontStyle Dependency Property

		public FontStyle FontStyle
		{
			get => (FontStyle)GetValue(FontStyleProperty);
			set => SetValue(FontStyleProperty, value);
		}

		public static DependencyProperty FontStyleProperty { get; } =
			DependencyProperty.Register(
				"FontStyle",
				typeof(FontStyle),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontStyle.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
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

		#region FontStretch Dependency Property

		public FontStretch FontStretch
		{
			get => GetFontStretchValue();
			set => SetFontStretchValue(value);
		}

		[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnFontStretchChanged), DefaultValue = FontStretch.Normal, Options = FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure)]
		public static DependencyProperty FontStretchProperty { get; } = CreateFontStretchProperty();

		private void OnFontStretchChanged()
		{
			OnFontStretchChangedPartial();
			InvalidateTextBlock();
		}

		partial void OnFontStretchChangedPartial();

		#endregion

		#region TextWrapping Dependency Property

		public TextWrapping TextWrapping
		{
			get => (TextWrapping)GetValue(TextWrappingProperty);
			set => SetValue(TextWrappingProperty, value);
		}

		public static DependencyProperty TextWrappingProperty { get; } =
			DependencyProperty.Register(
				"TextWrapping",
				typeof(TextWrapping),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextWrapping.NoWrap,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
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
			get => (FontWeight)GetValue(FontWeightProperty);
			set => SetValue(FontWeightProperty, value);
		}

		public static DependencyProperty FontWeightProperty { get; } =
			DependencyProperty.Register(
				"FontWeight",
				typeof(FontWeight),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontWeights.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
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
#if __APPLE_UIKIT__
			new
#endif
			string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
					coerceValueCallback: CoerceText,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) =>
						((TextBlock)s).OnTextChanged((string)e.OldValue, (string)e.NewValue)
				)
			);

		internal static object CoerceText(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences _) =>
			baseValue is string
				? baseValue
				: string.Empty;

		protected virtual void OnTextChanged(string oldValue, string newValue)
		{
			UpdateInlines(newValue);

#if __SKIA__
			if (!IsTextBoxDisplay)
#endif
			{
				// On skia, we don't want to set the selection here in case TextBox is managing the selection.
				Selection = new Range(0, 0);
			}

			OnTextChangedPartial();
			InvalidateTextBlock();
		}

		partial void OnTextChangedPartial();

		#endregion

		#region FontFamily Dependency Property

#if __APPLE_UIKIT__
		/// <summary>
		/// Supported font families: http://iosfonts.com/
		/// </summary>
#endif
		public FontFamily FontFamily
		{
			get => (FontFamily)GetValue(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}

		public static DependencyProperty FontFamilyProperty { get; } =
			DependencyProperty.Register(
				"FontFamily",
				typeof(FontFamily),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontFamily.Default,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
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
			get => (double)GetValue(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}

		public static DependencyProperty FontSizeProperty { get; } =
			DependencyProperty.Register(
				"FontSize",
				typeof(double),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: 14.0,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
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
			get => (int)GetValue(MaxLinesProperty);
			set => SetValue(MaxLinesProperty, value);
		}

		public static DependencyProperty MaxLinesProperty { get; } =
			DependencyProperty.Register(
				"MaxLines",
				typeof(int),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
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
			get => (TextTrimming)GetValue(TextTrimmingProperty);
			set => SetValue(TextTrimmingProperty, value);
		}

		public static DependencyProperty TextTrimmingProperty { get; } =
			DependencyProperty.Register(
				"TextTrimming",
				typeof(TextTrimming),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextTrimming.None,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
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
#if __ANDROID__
		new
#endif
			Brush Foreground
		{
			get => (Brush)GetValue(ForegroundProperty);
			set
			{
#if !__WASM__
				if (value is SolidColorBrush || value is GradientBrush || value is RadialGradientBrush || value is null)
				{
					SetValue(ForegroundProperty, value);
				}
				else
				{
					throw new NotSupportedException("Only SolidColorBrush or GradientBrush's FallbackColor are supported.");
				}
#else
				SetValue(ForegroundProperty, value);
#endif
			}
		}

		public static DependencyProperty ForegroundProperty { get; } =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Black,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).Subscribe((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		private void Subscribe(Brush oldValue, Brush newValue)
		{
			var newOnInvalidateRender = _foregroundChanged ?? (() => OnForegroundChanged());

			_foregroundBrushChangedSubscription?.Dispose();
			_foregroundBrushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _foregroundChanged, newOnInvalidateRender);
		}

		private void OnForegroundChanged()
		{
			// The try-catch here is primarily for the benefit of Android. This callback is raised when (say) the brush color changes,
			// which may happen when the system theme changes from light to dark. For app-level resources, a large number of views may
			// be subscribed to changes on the brush, including potentially some that have been removed from the visual tree, collected
			// on the native side, but not yet collected on the managed side (for Xamarin targets).

			// On Android, in practice this could result in ObjectDisposedExceptions when calling RequestLayout(). The try/catch is to
			// ensure that callbacks are correctly raised for remaining views referencing the brush which *are* still live in the visual tree.
			try
			{
				OnForegroundChangedPartial();
				InvalidateTextBlock();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Failed to invalidate for brush changed: {e}");
				}
			}
		}

		partial void OnForegroundChangedPartial();

		#endregion

		#region IsTextSelectionEnabled Dependency Property

#if !__WASM__ && !__SKIA__
		[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
#endif
		public bool IsTextSelectionEnabled
		{
			get => (bool)GetValue(IsTextSelectionEnabledProperty);
			set => SetValue(IsTextSelectionEnabledProperty, value);
		}

#if !__WASM__ && !__SKIA__
		[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty IsTextSelectionEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextSelectionEnabled),
				typeof(bool),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: false,
					propertyChangedCallback: (s, _) => ((TextBlock)s).OnIsTextSelectionEnabledChanged()
				)
			);

		private void OnIsTextSelectionEnabledChanged()
		{
			ProtectedCursor = IsTextSelectionEnabled ? InputSystemCursor.Create(InputSystemCursorShape.IBeam) : null;
			OnIsTextSelectionEnabledChangedPartial();
		}

		partial void OnIsTextSelectionEnabledChangedPartial();

		#endregion

		#region TextAlignment Dependency Property

		public new TextAlignment TextAlignment
		{
			get => GetTextAlignmentValue();
			set => SetTextAlignmentValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = TextAlignment.Left, ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.AffectsArrange, ChangedCallbackName = nameof(OnTextAlignmentChanged))]
		public static DependencyProperty TextAlignmentProperty { get; } = CreateTextAlignmentProperty();

		private void OnTextAlignmentChanged()
		{
			HorizontalTextAlignment = TextAlignment;
			OnTextAlignmentChangedPartial();
			InvalidateTextBlock();
		}

		partial void OnTextAlignmentChangedPartial();

		#endregion

		#region HorizontalTextAlignment Dependency Property

		public new TextAlignment HorizontalTextAlignment
		{
			get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
			set => SetValue(HorizontalTextAlignmentProperty, value);
		}

		public static DependencyProperty HorizontalTextAlignmentProperty { get; } =
			DependencyProperty.Register(
				"HorizontalTextAlignment",
				typeof(TextAlignment),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextAlignment.Left,
					FrameworkPropertyMetadataOptions.AffectsArrange,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnHorizontalTextAlignmentChanged()
				)
			);

		// This property provides the same functionality as the TextAlignment property.
		// If both properties are set to conflicting values, the last one set is used.
		// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.textbox.horizontaltextalignment#remarks
		private void OnHorizontalTextAlignmentChanged() => TextAlignment = HorizontalTextAlignment;

		#endregion

		#region LineHeight Dependency Property

		public double LineHeight
		{
			get => (double)GetValue(LineHeightProperty);
			set => SetValue(LineHeightProperty, value);
		}

		public static DependencyProperty LineHeightProperty { get; } =
			DependencyProperty.Register(
				nameof(LineHeight),
				typeof(double),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					0d,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnLineHeightChanged()));

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
			get => (LineStackingStrategy)GetValue(LineStackingStrategyProperty);
			set => SetValue(LineStackingStrategyProperty, value);
		}

		public static DependencyProperty LineStackingStrategyProperty { get; } =
			DependencyProperty.Register(
				nameof(LineStackingStrategy),
				typeof(LineStackingStrategy),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					LineStackingStrategy.MaxHeight,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnLineStackingStrategyChanged()));

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
			get => (Thickness)GetValue(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}

		public static DependencyProperty PaddingProperty { get; } =
			DependencyProperty.Register(
				nameof(Padding),
				typeof(Thickness),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					(Thickness)Thickness.Empty,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnPaddingChanged()));

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
			get => (int)GetValue(CharacterSpacingProperty);
			set => SetValue(CharacterSpacingProperty, value);
		}

		public static DependencyProperty CharacterSpacingProperty { get; } =
			DependencyProperty.Register(
				nameof(CharacterSpacing),
				typeof(int),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
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
			get => (TextDecorations)GetValue(TextDecorationsProperty);
			set => SetValue(TextDecorationsProperty, value);
		}

		public static DependencyProperty TextDecorationsProperty { get; } =
			DependencyProperty.Register(
				nameof(TextDecorations),
				typeof(TextDecorations),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextDecorations.None,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
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

		#region DependencyProperty: IsTextTrimmed
		private TypedEventHandler<TextBlock, IsTextTrimmedChangedEventArgs> _isTextTrimmedChanged;

#if false || false || IS_UNIT_TESTS || false || false || __NETSTD_REFERENCE__
		[NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
#endif
		public event TypedEventHandler<TextBlock, IsTextTrimmedChangedEventArgs> IsTextTrimmedChanged
		{
			add
			{
#if __WASM__
				if (!_shouldUpdateIsTextTrimmed)
				{
					UpdateIsTextTrimmed();

					_shouldUpdateIsTextTrimmed = true;
				}
#endif

				_isTextTrimmedChanged += value;
			}
			remove
			{
				_isTextTrimmedChanged -= value;
			}
		}

#if false || false || IS_UNIT_TESTS || false || false || __NETSTD_REFERENCE__
		[NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty IsTextTrimmedProperty { get; } = DependencyProperty.Register(
			nameof(IsTextTrimmed),
			typeof(bool),
			typeof(TextBlock),
			new FrameworkPropertyMetadata(false, propertyChangedCallback: (s, e) => ((TextBlock)s).OnIsTextTrimmedChanged()));

#if false || false || IS_UNIT_TESTS || false || false || __NETSTD_REFERENCE__
		[NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
#endif
		public bool IsTextTrimmed
		{
			get
			{
#if __WASM__
				if (!_shouldUpdateIsTextTrimmed)
				{
					UpdateIsTextTrimmed();

					_shouldUpdateIsTextTrimmed = true;
				}
#endif

				return (bool)GetValue(IsTextTrimmedProperty);
			}

			private set => SetValue(IsTextTrimmedProperty, value);
		}

		private void OnIsTextTrimmedChanged()
		{
			OnIsTextTrimmedChangedPartial();
			_isTextTrimmedChanged?.Invoke(this, new());
		}

		partial void OnIsTextTrimmedChangedPartial();

		#endregion

		// While font family itself didn't change, OnFontFamilyChanged will invalidate whatever
		// needed for the rendering to happen correctly on the next frame.
		internal void OnFontLoaded() => OnFontFamilyChanged();

		/// <summary>
		/// Gets whether the TextBlock is using the fast path in which Inlines
		/// have not been initialized and don't need to be synchronized.
		/// </summary>
		private bool UseInlinesFastPath => _inlines == null;

#if __ANDROID__ || __WASM__
		/// <summary>
		/// Returns if the TextBlock is constrained by a maximum number of lines.
		/// </summary>
		private bool IsLayoutConstrainedByMaxLines => MaxLines > 0;
#endif

#if __ANDROID__ || __APPLE_UIKIT__
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
#endif
		private void UpdateInlines(string text)
		{
			if (UseInlinesFastPath)
			{
				return;
			}

			if (ReadLocalValue(TextProperty) == DependencyProperty.UnsetValue)
			{
				_skipInlinesChangedTextSetter = true;
				Inlines.Clear();
				_skipInlinesChangedTextSetter = false;
				ClearTextPartial();
			}
			else if (text != _inlinesText)
			{
				// Inlines must be updated
				_skipInlinesChangedTextSetter = true;

				if (Inlines.Count == 1 && Inlines[0] is Run run)
				{
					run.Text = text;
				}
				else
				{
					if (Inlines.Count > 0)
					{
						Inlines.Clear();
						ClearTextPartial();
					}

					(_reusableRun ??= new Run()).Text = text;

					Inlines.Add(_reusableRun);
				}

				_skipInlinesChangedTextSetter = false;
			}
		}

		partial void ClearTextPartial();

		#region pointer events
		// For pen and touch selection, we need to:
		// * Start selection using a long-press and/or double-tap (platform specific)
		// * Add visual anchor (platform specific) to edit selection (pointer pressed out of those anchors only scroll, tap would unselect)
		// * Prevent scrolling to kick-in when editing selection (i.e. disable the DirectManipulation)
		// * Automatic scroll (platform specific) when pointer is close to the edge of the parent ScrollViewer
		// * Show a magnifier when finger can hide the text being (un)selected (platform specific)
		private static bool SupportsSelection(PointerRoutedEventArgs args)
			=> args.Pointer.PointerDeviceType is PointerDeviceType.Mouse;

		private static readonly PointerEventHandler OnPointerPressed = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is not TextBlock that)
			{
				return;
			}

			if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
			{
				return;
			}

#if !__WASM__
			that._isPressed = true;
#endif

			if (that.FindHyperlinkAt(e) is Hyperlink hyperlink)
			{
				if (!that.CapturePointer(e.Pointer))
				{
					return;
				}

				hyperlink.SetPointerPressed(e.Pointer);
				e.Handled = true;
				that.CompleteGesture(); // Make sure to mute Tapped
			}
#if !__WASM__
			else if (that.IsTextSelectionEnabled && SupportsSelection(e))
			{
				var point = e.GetCurrentPoint(that);

#if __SKIA__ // GetCharacterIndexAtPoint returns -1 if point isn't on any char. For pointers, we still want to get the closest char
				var index = that.GetCharacterIndexAtPoint(point.Position, true);
#else // TODO: add an option to get the closest char to point
				var index = that.GetCharacterIndexAtPoint(point.Position);
#endif
				that._selectionOnPointerPressed = that.Selection;
				if (index >= 0) // should always be true if above TODO is addressed
				{
					that.Selection = new Range(index, index);
				}

				e.Handled = true;
				that.Focus(FocusState.Pointer);

				that.CapturePointer(e.Pointer);
			}
#endif
		};

		private static readonly PointerEventHandler OnPointerReleased = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is not TextBlock that)
			{
				return;
			}

#if !__WASM__
			if (that._isPressed && that.IsTextSelectionEnabled && that.FindHyperlinkAt(e) is { })
			{
				// if we release on a hyperlink, we don't select anything
				that.Selection = new Range(0, 0);
			}

			that._isPressed = false;
#endif

			if (that.IsCaptured(e.Pointer))
			{
				var hyperlink = that.FindHyperlinkAt(e);
				// On UWP we don't get the Tapped event if we tapped a hyperlink, so make sure to abort it.
				if (hyperlink is { })
				{
					that.CompleteGesture();
				}

				// On UWP we don't get any CaptureLost, so make sure to manually release the capture silently
				that.ReleasePointerCapture(e.Pointer.UniqueId, muteEvent: true);

				// KNOWN ISSUE:
				// On UWP the 'click' event is raised **after** the PointerReleased ... but deferring the event on the Dispatcher
				// would move it after the PointerExited. So prefer to raise it before (actually like a Button).
				if (!(hyperlink?.ReleasePointerPressed(e.Pointer) ?? false))
				{
					// We failed to find the hyperlink that made this capture but we ** silently ** removed the capture,
					// so we won't receive the CaptureLost. So make sure to AbortPointerPressed on the Hyperlink which made the capture.
					that.AbortHyperlinkCaptures(e.Pointer);
				}
			}

#if !__WASM__
			e.Handled |= that.IsTextSelectionEnabled;
#endif
		};

		private static readonly PointerEventHandler OnPointerCaptureLost = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is TextBlock that)
			{
#if !__WASM__
				that._isPressed = false;
				if (SupportsSelection(e))
				{
					that.Selection = that._selectionOnPointerPressed;
				}
#endif

				e.Handled = that.AbortHyperlinkCaptures(e.Pointer);
			}
		};

		private static readonly PointerEventHandler OnPointerMoved = (sender, e) =>
		{
			if (sender is not TextBlock that)
			{
				return;
			}

			var hyperlink = that.FindHyperlinkAt(e);
			if (that._hyperlinkOver != hyperlink)
			{
				that._hyperlinkOver?.ReleasePointerOver(e.Pointer);
				that._hyperlinkOver = hyperlink;
				hyperlink?.SetPointerOver(e.Pointer);
			}

#if !__WASM__
			if (that._isPressed && that.IsTextSelectionEnabled && SupportsSelection(e))
			{
				var point = e.GetCurrentPoint(that);
#if __SKIA__ // GetCharacterIndexAtPoint returns -1 if point isn't on any char. For pointers, we still want to get the closest char
				var index = that.GetCharacterIndexAtPoint(point.Position, true);
#else // TODO: add an option to get the closest char to point
				var index = that.GetCharacterIndexAtPoint(point.Position);
#endif
				if (index >= 0) // should always be true if above TODO is addressed
				{
					that.Selection = that.Selection with { end = index };
				}
			}
#endif
		};

		private static readonly PointerEventHandler OnPointerEntered = (sender, e) =>
		{
			if (sender is not TextBlock { HasHyperlink: true } that)
			{
				return;
			}

			// This assertion fails because we don't release pointer captures on PointerExited in InputManager
			// TODO: make it such that this assertion doesn't fail
			// global::System.Diagnostics.Debug.Assert(that._hyperlinkOver == null);

			var hyperlink = that.FindHyperlinkAt(e);

			that._hyperlinkOver = hyperlink;
			hyperlink?.SetPointerOver(e.Pointer);
		};

		private static readonly PointerEventHandler OnPointerExit = (sender, e) =>
		{
			if (sender is not TextBlock { HasHyperlink: true } that)
			{
				return;
			}

			that._hyperlinkOver?.ReleasePointerOver(e.Pointer);
			that._hyperlinkOver = null;
		};

		private bool AbortHyperlinkCaptures(Pointer pointer)
		{
			var aborted = false;
			foreach (var hyperlink in _hyperlinks.ToList()) // .ToList() : for a strange reason on WASM the collection gets modified
			{
				aborted |= hyperlink.hyperlink.AbortPointerPressed(pointer);
				aborted |= hyperlink.hyperlink.ReleasePointerOver(pointer);
			}

			aborted |= _hyperlinkOver?.ReleasePointerOver(pointer) ?? false;
			_hyperlinkOver = null;

			return aborted;
		}

		private readonly ObservableCollection<(int start, int end, Hyperlink hyperlink)> _hyperlinks = new();

		private void HyperlinksOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RecalculateSubscribeToPointerEvents();

		private void RecalculateSubscribeToPointerEvents()
		{
			SubscribeToPointerEvents = HasHyperlink
#if !__WASM__
				|| IsTextSelectionEnabled
#endif
				;
		}

		private void UpdateHyperlinks()
		{
			global::System.Diagnostics.Debug.Assert(_hyperlinkOver is null || _hyperlinks.Where(h => h.hyperlink == _hyperlinkOver).Count() == 1);

			if (UseInlinesFastPath) // i.e. no Inlines
			{
				if (HasHyperlink)
				{
					// Make sure to clear the pressed state of removed hyperlinks
					foreach (var hyperlink in _hyperlinks)
					{
						hyperlink.hyperlink.AbortAllPointerState();
					}

					_hyperlinkOver = null;
					_hyperlinks.Clear();
				}

				return;
			}

			var previousHyperLinks = _hyperlinks.SelectToList(hyperlink => hyperlink.hyperlink);

			_hyperlinkOver = null;
			_hyperlinks.Clear();

			var start = 0;
			foreach (var inline in Inlines.PreorderTree)
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
				removed.AbortAllPointerState();
			}
		}

		private bool HasHyperlink
		{
			get
			{
				var hasHyperlink = _hyperlinks.Count > 0;

				global::System.Diagnostics.Debug.Assert(!(!hasHyperlink && _hyperlinkOver is { }));

				return hasHyperlink;
			}
		}

		private bool SubscribeToPointerEvents
		{
			get => _subscribeToPointerEvents;
			set
			{
				if (_subscribeToPointerEvents == value)
				{
					return;
				}

				_subscribeToPointerEvents = value;

				// Update events subscriptions if needed
				// Note: we subscribe to those events only if needed as they increase marshaling on Android and WASM
				if (value)
				{
					InsertHandler(PointerPressedEvent, OnPointerPressed);
					InsertHandler(PointerReleasedEvent, OnPointerReleased);
					InsertHandler(PointerMovedEvent, OnPointerMoved);
					InsertHandler(PointerEnteredEvent, OnPointerEntered);
					InsertHandler(PointerExitedEvent, OnPointerExit);
					InsertHandler(PointerCaptureLostEvent, OnPointerCaptureLost);
				}
				else
				{
					RemoveHandler(PointerPressedEvent, OnPointerPressed);
					RemoveHandler(PointerReleasedEvent, OnPointerReleased);
					RemoveHandler(PointerMovedEvent, OnPointerMoved);
					RemoveHandler(PointerEnteredEvent, OnPointerEntered);
					RemoveHandler(PointerExitedEvent, OnPointerExit);
					RemoveHandler(PointerCaptureLostEvent, OnPointerCaptureLost);
				}
			}
		}

		private Hyperlink FindHyperlinkAt(PointerRoutedEventArgs e)
		{
#if __WASM__
			var point = e.GetCurrentPoint(null).Position;
			var elementInCoordinate = WindowManagerInterop.TryGetElementInCoordinate(point) as TextElement;
			while (elementInCoordinate is not null)
			{
				if (elementInCoordinate is Hyperlink)
				{
					break;
				}

				elementInCoordinate = elementInCoordinate.GetParent() as TextElement;
			}

			if (elementInCoordinate is Hyperlink hyperlink)
			{
				foreach (var candidate in _hyperlinks)
				{
					if (hyperlink == candidate.hyperlink)
					{
						return hyperlink;
					}
				}
			}

			return null;
#else
			var point = e.GetCurrentPoint(this).Position;
			var characterIndex = GetCharacterIndexAtPoint(point);
			var hyperlink = _hyperlinks
				.FirstOrDefault(h => h.start <= characterIndex && h.end > characterIndex)
				.hyperlink;

			return hyperlink;
#endif
		}
		#endregion

		private void InvalidateTextBlock()
		{
			InvalidateTextBlockPartial();
			InvalidateMeasure();
		}

		partial void InvalidateTextBlockPartial();

		protected override AutomationPeer OnCreateAutomationPeer() => new TextBlockAutomationPeer(this);

		public override string GetAccessibilityInnerText() => Text;

		// This approximates UWP behavior
		private protected override double GetActualWidth() => DesiredSize.Width;
		private protected override double GetActualHeight() => DesiredSize.Height;

		internal override void UpdateThemeBindings(Data.ResourceUpdateReason updateReason)
		{
			base.UpdateThemeBindings(updateReason);

			UpdateLastUsedTheme();

			if (_inlines is not null)
			{
				foreach (var inline in _inlines)
				{
					((IDependencyObjectStoreProvider)inline).Store.UpdateResourceBindings(updateReason, resourceContextProvider: this);
				}
			}
		}

		internal override bool CanHaveChildren() => true;

		public new bool Focus(FocusState value) => base.Focus(value);

		internal override bool IsFocusable =>
			/*IsActive() &&*/ //TODO Uno: No concept of IsActive in Uno yet.
			IsVisible() &&
			// Uno-specific: On Android Skia, we force GetCaretBrowsingModeEnable so that TextBlocks can be navigated
			// with TalkBack. In this case, we want IsFocusable to be true for the TextBlock to be considered
			// by UnoExploreByTouchHelper.GetVisibleVirtualViews
			/*IsEnabled() &&*/ (IsTextSelectionEnabled || IsTabStop || FocusProperties.GetCaretBrowsingModeEnable()) &&
			AreAllAncestorsVisible();

		private record struct Range(int start, int end)
		{
			public Range((int start, int end) tuple) : this(tuple.start, tuple.end)
			{
			}
		}

		[SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used only by some platforms")]
		private bool IsTextTrimmable =>
			TextTrimming != TextTrimming.None ||
			MaxLines != 0;

		[SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used only by some platforms")]
		partial void UpdateIsTextTrimmed();

		// The way this works in WinUI is by the MarkInheritedPropertyDirty call in CFrameworkElement::NotifyThemeChangedForInheritedProperties
		// There is a special handling for Foreground specifically there.
		void IThemeChangeAware.OnThemeChanged() => OnForegroundChanged();
	}
}
