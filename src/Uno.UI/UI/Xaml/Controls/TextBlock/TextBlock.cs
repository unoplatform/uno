#pragma warning disable CS0109

using System;
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
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno;
using Uno.Foundation.Logging;

using RadialGradientBrush = Microsoft.UI.Xaml.Media.RadialGradientBrush;
using Uno.UI.Helpers;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;
using Microsoft.UI.Composition;
using System.Numerics;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Core.Scaling;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Internal;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Inlines))]
	public partial class TextBlock : FrameworkElement, IThemeChangeAware, IBlock, UnicodeText.IFontCacheUpdateListener, ITextSelectionGripperHost
	{
		private InlineCollection _inlines;
		private string _inlinesText; // Text derived from the content of Inlines
		private IDisposable _foregroundBrushChangedSubscription;

		// Used for text selection which is handled natively
		private bool _isPressed;
		private Range _selectionOnPointerPressed; // stores the selection before a mouse press so that it's restored on pointer cancellation

		private Hyperlink _hyperlinkOver; // do not use: use HyperlinkOver instead
		private Hyperlink HyperlinkOver
		{
			get => _hyperlinkOver;
			set
			{
				if (_hyperlinkOver != value)
				{
					_hyperlinkOver = value;
					UpdateProtectedCursor();
				}
			}
		}

		private bool _subscribeToPointerEvents;

		private Action _foregroundChanged;

		private Run _reusableRun;
		private bool _skipInlinesChangedTextSetter;
		private Range _selection;

		// end can be less than or equal to start when the selection starts ahead and then goes back
		// see the selection in TextBox.skia.cs for more info
		internal Range Selection
		{
			get => _selection;
			set
			{
				if (_selection != value)
				{
					_selection = value;
					OnSelectionChanged();
				}
			}
		}

		partial void OnSelectionChanged();

		/// <summary>
		/// Called from OnPointerReleased to handle SelectionFlyout visibility updates.
		/// Implemented in TextBlock.skia.cs.
		/// </summary>
		partial void OnPointerReleasedForSelectionFlyout(PointerRoutedEventArgs e);

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
				Inlines.InvalidateTraversedTree();
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
			if (OwningTextBox is null)
#endif
			{
				// On skia, we don't want to set the selection here in case TextBox is managing the selection.
				Selection = new Range(0, 0);
			}

			OnTextChangedPartial();
			InvalidateTextBlock();

			// When a TextBlock with LiveSetting (Polite/Assertive) has its text changed,
			// raise LiveRegionChanged so screen readers announce the new content.
			// In WinUI3, the OS UIA framework monitors content changes on live region
			// elements automatically. We replicate that behavior here.
			if (AutomationProperties.GetLiveSetting(this) != AutomationLiveSetting.Off)
			{
				AutomationHelper.RaiseEventIfListener(this, AutomationEvents.LiveRegionChanged);
			}

			RaiseAutomationNameChangedIfNeeded(oldValue, newValue);
		}

		partial void OnTextChangedPartial();

		/// <summary>
		/// When the TextBlock's accessible name is derived from its <see cref="Text"/> (i.e. no
		/// explicit <see cref="AutomationProperties.NameProperty"/> overrides it), a runtime Text
		/// change also changes the accessible name. UI Automation must be notified so assistive
		/// technologies (and the semantic DOM on Skia-WASM) re-read the name; otherwise the
		/// accessibility tree keeps the stale name. In WinUI3 the OS UIA framework derives and
		/// re-evaluates the Name from the text automatically — we replicate that here.
		/// </summary>
		private void RaiseAutomationNameChangedIfNeeded(string oldValue, string newValue)
		{
#if __SKIA__
			// Only the accessible-name-from-Text case matters: an explicit AutomationProperties.Name
			// takes precedence in GetNameCore and is already routed via OnNamePropertyChanged, so
			// raising here would be redundant (and would report an unchanged name).
			if (!string.IsNullOrEmpty(AutomationProperties.GetName(this)))
			{
				return;
			}

			// AutomationProperties.LabeledBy also overrides the Text-derived name. When it's set,
			// the accessible name comes from the labeller (which didn't change with our Text), so
			// reporting (oldText -> newText) here would emit a bogus name-change notification.
			if (AutomationProperties.GetLabeledBy(this) is not null)
			{
				return;
			}

			var listener = AutomationPeer.AutomationPeerListener;
			if (listener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) != true)
			{
				return;
			}

			if (GetOrCreateAutomationPeer() is { } peer)
			{
				// Confirm the peer's resolved name truly tracks Text (e.g. ContentPresenter / inline
				// composition could divert it). If not, oldValue/newValue would not be valid old/new
				// accessible names and we'd emit a misleading event.
				var newName = peer.GetName() ?? string.Empty;
				var newText = newValue ?? string.Empty;
				if (!string.Equals(newName, newText, StringComparison.Ordinal))
				{
					return;
				}

				listener.NotifyPropertyChangedEvent(peer, AutomationElementIdentifiers.NameProperty, oldValue ?? string.Empty, newName);
			}
#endif
		}

		#endregion

		#region FontFamily Dependency Property

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

		#region IsTextScaleFactorEnabled Dependency Property

		public bool IsTextScaleFactorEnabled
		{
			get => (bool)GetValue(IsTextScaleFactorEnabledProperty);
			set => SetValue(IsTextScaleFactorEnabledProperty, value);
		}

		public static DependencyProperty IsTextScaleFactorEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextScaleFactorEnabled),
				typeof(bool),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: true,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

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
			Brush Foreground
		{
			get => (Brush)GetValue(ForegroundProperty);
			set
			{
				if (value is SolidColorBrush || value is GradientBrush || value is RadialGradientBrush || value is null)
				{
					SetValue(ForegroundProperty, value);
				}
				else
				{
					throw new NotSupportedException("Only SolidColorBrush or GradientBrush's FallbackColor are supported.");
				}
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

#if !__SKIA__
		[NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
#endif
		public bool IsTextSelectionEnabled
		{
			get => (bool)GetValue(IsTextSelectionEnabledProperty);
			set => SetValue(IsTextSelectionEnabledProperty, value);
		}

#if !__SKIA__
		[NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
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
			UpdateProtectedCursor();
			OnIsTextSelectionEnabledChangedPartial();
		}

		// The cursor while hovering a hyperlink takes precedence over the text-selection I-beam,
		// matching WinUI where the innermost element wins the cursor resolution.
		private void UpdateProtectedCursor() =>
			ProtectedCursor = HyperlinkOver is not null
				? InputSystemCursor.Create(InputSystemCursorShape.Hand)
				: IsTextSelectionEnabled
					? InputSystemCursor.Create(InputSystemCursorShape.IBeam)
					: null;

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

		#region TextHighlighters

		public IList<TextHighlighter> TextHighlighters { get; } = new ObservableCollection<TextHighlighter>();

		#endregion

		#region DependencyProperty: IsTextTrimmed
		private TypedEventHandler<TextBlock, IsTextTrimmedChangedEventArgs> _isTextTrimmedChanged;

#if __SKIA__
		public event ContextMenuOpeningEventHandler ContextMenuOpening;
#endif

#if false || false || IS_UNIT_TESTS || false || false || __NETSTD_REFERENCE__
		[NotImplemented("IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
#endif
		public event TypedEventHandler<TextBlock, IsTextTrimmedChangedEventArgs> IsTextTrimmedChanged
		{
			add
			{
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

		private void UpdateInlines(string text)
		{
			if (UseInlinesFastPath)
			{
				return;
			}

			if (!this.IsDependencyPropertySet(TextProperty))
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

		// Ported from: TextSelectionManager.cpp OnRightTapped (lines 895-938)
		// WinUI focuses the TextBlock on right-tap so that when the context flyout
		// opens and steals focus, the LostFocus handler can set
		// _forceFocusedForContextFlyout, keeping the selection highlight visible.
		private static readonly RightTappedEventHandler OnRightTapped = (object sender, RightTappedRoutedEventArgs e) =>
		{
			if (sender is not TextBlock that || !that.IsTextSelectionEnabled)
			{
				return;
			}

			if (e.Handled)
			{
				return;
			}

#if __SKIA__
			if (!that.IsFocused && !Internal.TextControlFlyoutHelper.IsOpen(that.ContextFlyout))
			{
				that.Focus(FocusState.Pointer);
			}
#endif
		};

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

			that._isPressed = true;

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
			else if (that.IsTextSelectionEnabled && e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
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
				// Ported from: TextSelectionManager.cpp OnHolding/OnRightTapped
				// Don't take focus if the context flyout is open.
#if __SKIA__
				// A mouse interaction drops any touch grippers that were showing.
				that.HideGrippers();
				if (!Internal.TextControlFlyoutHelper.IsOpen(that.ContextFlyout))
#endif
				{
					that.Focus(FocusState.Pointer);
				}

				that.CapturePointer(e.Pointer);
			}
#if __SKIA__
			else if (that.IsTextSelectionEnabled && e.Pointer.PointerDeviceType is PointerDeviceType.Touch or PointerDeviceType.Pen)
			{
				// Touch/pen: remember the press for hold/tap detection on release. We don't select or
				// capture here, and we don't set Handled, so the Holding (long-press -> context menu)
				// and Tapped/Released gestures still fire. Selection happens in OnPointerReleasedForSelectionFlyout.
				that._lastPointerDownPoint = e.GetCurrentPoint(null);
				// Dismiss the selection flyout on press; the gesture re-shows it (tap) or yields to the context menu (hold).
				that.DismissSelectionFlyoutForPointerPress();
				if (!Internal.TextControlFlyoutHelper.IsOpen(that.ContextFlyout))
				{
					that.Focus(FocusState.Pointer);
				}
			}
#endif
		};

		private static readonly PointerEventHandler OnPointerReleased = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is not TextBlock that)
			{
				return;
			}

			if (that._isPressed && that.IsTextSelectionEnabled && that.FindHyperlinkAt(e) is { })
			{
				// if we release on a hyperlink, we don't select anything
				that.Selection = new Range(0, 0);
			}

			that._isPressed = false;

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

			// Modeled after WinUI TextSelectionManager.cpp UpdateSelectionFlyoutVisibility:
			// After pointer release, handle touch/pen selection and queue a SelectionFlyout visibility update.
			that.OnPointerReleasedForSelectionFlyout(e);
			e.Handled |= that.IsTextSelectionEnabled;
		};

		private static readonly PointerEventHandler OnPointerCaptureLost = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is TextBlock that)
			{
				that._isPressed = false;
				if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
				{
					that.Selection = that._selectionOnPointerPressed;
				}

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
			if (that.HyperlinkOver != hyperlink)
			{
				that.HyperlinkOver?.ReleasePointerOver(e.Pointer);
				that.HyperlinkOver = hyperlink;
				hyperlink?.SetPointerOver(e.Pointer);
			}

			if (that._isPressed && that.IsTextSelectionEnabled && e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
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
		};

		private static readonly PointerEventHandler OnPointerEntered = (sender, e) =>
		{
			if (sender is not TextBlock { HasHyperlink: true } that)
			{
				return;
			}

			// This assertion fails because we don't release pointer captures on PointerExited in InputManager
			// TODO: make it such that this assertion doesn't fail
			// global::System.Diagnostics.Debug.Assert(that.HyperlinkOver == null);

			var hyperlink = that.FindHyperlinkAt(e);

			that.HyperlinkOver = hyperlink;
			hyperlink?.SetPointerOver(e.Pointer);
		};

		private static readonly PointerEventHandler OnPointerExit = (sender, e) =>
		{
			if (sender is not TextBlock { HasHyperlink: true } that)
			{
				return;
			}

			that.HyperlinkOver?.ReleasePointerOver(e.Pointer);
			that.HyperlinkOver = null;
		};

		private bool AbortHyperlinkCaptures(Pointer pointer)
		{
			var aborted = false;
			foreach (var hyperlink in _hyperlinks.ToList()) // .ToList() : for a strange reason on WASM the collection gets modified
			{
				aborted |= hyperlink.AbortPointerPressed(pointer);
				aborted |= hyperlink.ReleasePointerOver(pointer);
			}

			aborted |= HyperlinkOver?.ReleasePointerOver(pointer) ?? false;
			HyperlinkOver = null;

			return aborted;
		}

		private readonly ObservableCollection<Hyperlink> _hyperlinks = new();

		private void HyperlinksOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RecalculateSubscribeToPointerEvents();

		private void RecalculateSubscribeToPointerEvents()
		{
			SubscribeToPointerEvents = HasHyperlink
				|| IsTextSelectionEnabled
				;
		}

		private void UpdateHyperlinks()
		{
			global::System.Diagnostics.Debug.Assert(HyperlinkOver is null || _hyperlinks.Count(h => h == HyperlinkOver) == 1);

			if (UseInlinesFastPath) // i.e. no Inlines
			{
				if (HasHyperlink)
				{
					// Make sure to clear the pressed state of removed hyperlinks
					foreach (var hyperlink in _hyperlinks)
					{
						hyperlink.AbortAllPointerState();
					}

					HyperlinkOver = null;
					_hyperlinks.Clear();
				}

				return;
			}

			HyperlinkOver = null;
			var previousHyperLinks = _hyperlinks.ToHashSet();
			_hyperlinks.Clear();
			foreach (var hyperlink in Inlines.TraversedTree.preorderTree.OfType<Hyperlink>())
			{
				_hyperlinks.Add(hyperlink);
				previousHyperLinks.Remove(hyperlink);
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

				global::System.Diagnostics.Debug.Assert(!(!hasHyperlink && HyperlinkOver is not null));

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
					InsertHandler(RightTappedEvent, OnRightTapped);
				}
				else
				{
					RemoveHandler(PointerPressedEvent, OnPointerPressed);
					RemoveHandler(PointerReleasedEvent, OnPointerReleased);
					RemoveHandler(PointerMovedEvent, OnPointerMoved);
					RemoveHandler(PointerEnteredEvent, OnPointerEntered);
					RemoveHandler(PointerExitedEvent, OnPointerExit);
					RemoveHandler(PointerCaptureLostEvent, OnPointerCaptureLost);
					RemoveHandler(RightTappedEvent, OnRightTapped);
				}
			}
		}

		private Hyperlink FindHyperlinkAt(PointerRoutedEventArgs e)
		{
#if __SKIA__
			return ParsedText.GetHyperlinkAt(e.GetCurrentPoint(this).Position);
#else
			return null;
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
#if __CROSSRUNTIME__
			// WinUI: CTextBlock::IsFocusable requires IsActive() (the element is in the live tree).
			IsActiveOrAttachedUnderActiveAncestor() &&
#endif
			IsVisible() &&
			// Uno-specific: On Android Skia, we force GetCaretBrowsingModeEnable so that TextBlocks can be navigated
			// with TalkBack. In this case, we want IsFocusable to be true for the TextBlock to be considered
			// by UnoExploreByTouchHelper.GetVisibleVirtualViews
			/*IsEnabled() &&*/ (IsTextSelectionEnabled || IsTabStop || FocusProperties.GetCaretBrowsingModeEnable()) &&
			AreAllAncestorsVisible();

		[SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used only by some platforms")]
		private bool IsTextTrimmable =>
			TextTrimming != TextTrimming.None ||
			MaxLines != 0;

		[SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used only by some platforms")]
		partial void UpdateIsTextTrimmed();

		// The way this works in WinUI is by the MarkInheritedPropertyDirty call in CFrameworkElement::NotifyThemeChangedForInheritedProperties
		// There is a special handling for Foreground specifically there.
		void IThemeChangeAware.OnThemeChanged() => OnForegroundChanged();

		internal record struct Range(int start, int end);

#nullable enable
		// The caret thickness is actually always 1-pixel wide regardless of how big the text is
		internal const float CaretThickness = 1;

		private Action? _selectionHighlightColorChanged;
		private IDisposable? _selectionHighlightBrushChangedSubscription;
		private readonly VirtualKeyModifiers _platformCtrlKey = Uno.UI.Helpers.DeviceTargetHelper.PlatformCommandModifier;
		private Size _lastInlinesArrangeWithPadding;
		private readonly Dictionary<TextHighlighter, IDisposable> _textHighlighterDisposables = new();

		private protected override ContainerVisual CreateElementVisual() => new TextVisual(Compositor.GetSharedCompositor(), this);

		private bool _renderSelection;
		private (int index, CompositionBrush brush)? _caretPaint;
		private bool _forceFocusedForContextFlyout;

		// Touch-selection grippers (knobs), driven by the shared TextSelectionGripperPresenter. Unlike
		// TextBox there is no caret/insertion point in a TextBlock, so the grippers only ever appear in
		// pairs around a non-empty selection (GripperMode is only ever Hidden or Both).
		private TextSelectionGripperPresenter? _gripperPresenter;
		private bool _grippersShown;
		private Microsoft.UI.Input.PointerPoint? _lastPointerDownPoint;

		private (Size availableSize, Size outSize, TextAlignment? alignment) _lastParsedTextCreationValues = (Size.Empty, Size.Empty, TextAlignment.Left);
		internal IParsedText ParsedText { get; private set; } = Microsoft.UI.Xaml.Documents.ParsedText.Empty;

		internal event EventHandler? DrawingFinished;

		public TextBlock()
		{
			UpdateLastUsedTheme();

			_hyperlinks.CollectionChanged += HyperlinksOnCollectionChanged;
			((ObservableCollection<TextHighlighter>)TextHighlighters).CollectionChanged += OnTextHighlightersChanged;

			Tapped += static (s, e) => ((TextBlock)s).OnTapped(e);
			DoubleTapped += static (s, e) => ((TextBlock)s).OnDoubleTapped(e);
			KeyDown += static (s, e) => ((TextBlock)s).OnKeyDown(e);

			GotFocus += (_, _) =>
			{
				_forceFocusedForContextFlyout = false;
				UpdateSelectionRendering();
			};
			LostFocus += (_, _) =>
			{
				_forceFocusedForContextFlyout = ShouldForceFocusedVisualState();
				UpdateSelectionRendering();

				if (!_forceFocusedForContextFlyout)
				{
					HideGrippers();
				}
			};
		}

		public static DependencyProperty SelectionFlyoutProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionFlyout), typeof(FlyoutBase), typeof(TextBlock),
				new FrameworkPropertyMetadata(default(FlyoutBase), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public FlyoutBase SelectionFlyout
		{
			get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
			set => SetValue(SelectionFlyoutProperty, value);
		}

		internal TextBox? OwningTextBox { get; init; }

		internal bool IsSpellCheckEnabled { get; set; }

		private protected override void OnLoaded()
		{
			base.OnLoaded();
#if DEBUG
			Visual.Comment = $"{Visual.Comment}#text";
#endif
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_forceFocusedForContextFlyout = false;
			HideGrippers();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var padding = Padding;
			var availableSizeWithoutPadding = availableSize.Subtract(padding).AtLeastZero();
			ParsedText = ParseText(availableSizeWithoutPadding, out var desiredSize);

			desiredSize = desiredSize.Add(padding);

			if (GetUseLayoutRounding())
			{
				// In order to prevent text clipping as a result of layout rounding at
				// scales other than 1.0x, the ceiling of the rescaled size is used.
				var plateauScale = RootScale.GetRasterizationScaleForElement(this);
				Size pageNodeSize = desiredSize;
				desiredSize.Width = ((int)Math.Ceiling(pageNodeSize.Width * plateauScale)) / plateauScale;
				desiredSize.Height = ((int)Math.Ceiling(pageNodeSize.Height * plateauScale)) / plateauScale;

				// LsTextLine is not aware of layoutround and uses baseline height to place the rendered text.
				// However, because the height of the *block is potentionally layoutround-ed up, we should adjust the
				// placement of text by the difference.  Horizontal adjustment is not of concern since
				// LsTextLine uses arranged size which is already layoutround-ed.
				//_layoutRoundingHeightAdjustment = desiredSize.Height - pageNodeSize.Height;
			}

			return desiredSize;
		}

		private UnicodeText ParseText(Size availableSizeWithoutPadding, out Size size)
		{
			var isTextBoxOwned = OwningTextBox is not null;
			var adjustedTextAlignment = GetAdjustedTextAlignment();
			var ret = new UnicodeText(
				availableSizeWithoutPadding,
				Inlines.TraversedTree.leafTree,
				GetDefaultFontDetails(),
				MaxLines,
				(float)LineHeight,
				LineStackingStrategy,
				FlowDirection,
				adjustedTextAlignment,
				TextWrapping,
				TextTrimming,
				IsSpellCheckEnabled,
				this,
				isTextBoxOwned,
				out size);

			if (isTextBoxOwned)
			{
				size.Width += CaretThickness;
			}

			_lastParsedTextCreationValues = (availableSizeWithoutPadding, size, adjustedTextAlignment);
			return ret;
		}

		private TextAlignment? GetAdjustedTextAlignment() =>
			(OwningTextBox as IDependencyObjectStoreProvider)?.Store
			.GetCurrentHighestValuePrecedence(TextBox.TextAlignmentProperty) is DependencyPropertyValuePrecedences
				.DefaultValue
				? null
				: TextAlignment;

		// the entire body of the text block is considered hit-testable
		internal override bool HitTest(Point point)
		{
			// This is equivalent to using TransformToVisual but without the unnecessary MatrixTransform allocation.
			var transform = GetTransform(this, (UIElement)this.GetParent());
			var success = Matrix3x2.Invert(transform, out var inverted);
			return success && inverted.Transform(LayoutSlotWithMarginsAndAlignments).Contains(point);
		}

		partial void OnIsTextSelectionEnabledChangedPartial()
		{
			RecalculateSubscribeToPointerEvents();
			UpdateSelectionRendering();

			if (!IsTextSelectionEnabled)
			{
				HideGrippers();
			}

			// Enable context menu gestures when text selection is enabled.
			// This ensures ContextRequested is raised for the default TextCommandBarFlyout.
			// We need to call this explicitly because TextBlock's default ContextFlyout is set via
			// GetDefaultValue (not via SetValue), which doesn't trigger OnContextFlyoutChanged.
			if (IsTextSelectionEnabled)
			{
				EnsureContextMenuGesturesEnabled();
			}
		}

		private void UpdateSelectionRendering()
		{
			if (OwningTextBox is null) // TextBox manages RenderSelection itself
			{
				RenderSelection = IsTextSelectionEnabled && (IsFocused || _forceFocusedForContextFlyout);
			}
		}

		// Ported from: TextSelectionManager.cpp ShouldForceFocusedVisualState (lines 3422-3428)
		private bool ShouldForceFocusedVisualState()
		{
			return TextControlFlyoutHelper.IsGettingFocus(SelectionFlyout, this)
				|| TextControlFlyoutHelper.IsGettingFocus(ContextFlyout, this);
		}

		// Ported from: TextSelectionManager.cpp ForceFocusLoss (lines 3430-3444)
		internal void ForceFocusLoss()
		{
			_forceFocusedForContextFlyout = false;
			UpdateSelectionRendering();
			HideGrippers();
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Visual.Compositor.InvalidateRender(Visual);
			var padding = Padding;
			var availableSizeWithoutPadding = finalSize.Subtract(padding);

			// There's no reason to re-parse the text if the available size hasn't changed since the last measure/arrange.
			// Note that MeasureOverride doesn't have these checks. If something in the text block has changed that would
			// require a re-parse, the ParseText call during the measure pass will catch it. There are no changes that
			// would require a re-parse that would invalidate arrange but not measure, except TextAlignment, which we explicitly check.
			var arrangedSize = _lastParsedTextCreationValues.outSize;
			if (_lastParsedTextCreationValues.availableSize != availableSizeWithoutPadding || _lastParsedTextCreationValues.alignment != GetAdjustedTextAlignment())
			{
				ParsedText = ParseText(availableSizeWithoutPadding, out arrangedSize);
			}

			_lastInlinesArrangeWithPadding = arrangedSize.Add(padding);

			var result = base.ArrangeOverride(finalSize);
			UpdateIsTextTrimmed();

			return result;
		}

		internal bool RenderSelection
		{
			set
			{
				if (_renderSelection != value)
				{
					_renderSelection = value;
					InvalidateInlineAndRequireRepaint();
				}
			}
		}

		internal (int index, CompositionBrush brush)? RenderCaret
		{
			set
			{
				if (_caretPaint != value)
				{
					_caretPaint = value;
					InvalidateInlineAndRequireRepaint();
				}
			}
		}

		internal void Draw(in Visual.PaintingSession session)
		{
			session.Canvas.Save();
			session.Canvas.Translate((float)Padding.Left, (float)Padding.Top);
			var highligherters = _renderSelection ? TextHighlighters.Append(new TextHighlighter
			{
				Background = SelectionHighlightColor,
				Foreground = DefaultBrushes.SelectedTextForegroundColor,
				Ranges =
				{
					new TextRange
					{
						StartIndex = Math.Min(Selection.start, Selection.end),
						Length = Math.Abs(Selection.start - Selection.end)
					}
				}
			}) : TextHighlighters;
			(int startIndex, int length)? compositionRange = null;
			if (OwningTextBox is { IsComposing: true, CompositionUnderlineLength: > 0 } owningTextBox)
			{
				compositionRange = (owningTextBox.CompositionUnderlineStart, owningTextBox.CompositionUnderlineLength);
			}
			ParsedText.Draw(
				session,
				_caretPaint is { } c ? (c.index, c.brush, CaretThickness) : null,
				highligherters,
				compositionRange);
			session.Canvas.Restore();
			DrawingFinished?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Gets the line height of the TextBlock either
		/// based on the LineHeight property or the default
		/// font line height.
		/// </summary>
		/// <returns>Computed line height</returns>
		private FontDetails GetDefaultFontDetails()
		{
			var scaledSize = Uno.UI.Xaml.Core.TextScaleHelper.GetScaledFontSize(FontSize, Uno.UI.Xaml.Core.CoreServices.Instance.FontScale, IsTextScaleFactorEnabled && !Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor);
			var (details, task) = FontDetailsCache.GetFont(FontFamily?.Source, (float)scaledSize, FontWeight, FontStretch, FontStyle);
			if (task.IsCompletedSuccessfully)
			{
				return task.Result;
			}
			else
			{
				task.ContinueWith(_ =>
				{
					NativeDispatcher.Main.Enqueue(OnFontLoaded);
				});
				return details;
			}
		}

		private int GetCharacterIndexAtPoint(Point point, bool extended = false) => ParsedText.GetIndexAt(point, false, extended);

		// Invalidate Inlines measure and repaint text when any IBlock properties used during measuring change:

		private void InvalidateInlineAndRequireRepaint()
		{
			Visual.Compositor.InvalidateRender(Visual);
		}

		partial void InvalidateTextBlockPartial() => InvalidateInlineAndRequireRepaint();
		partial void OnForegroundChangedPartial() => InvalidateInlineAndRequireRepaint();
		partial void OnInlinesChangedPartial() => InvalidateInlineAndRequireRepaint();
		partial void OnMaxLinesChangedPartial() => InvalidateInlineAndRequireRepaint();
		partial void OnTextWrappingChangedPartial() => InvalidateInlineAndRequireRepaint();
		partial void OnLineHeightChangedPartial() => InvalidateInlineAndRequireRepaint();
		partial void OnLineStackingStrategyChangedPartial() => InvalidateInlineAndRequireRepaint();
		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush) => InvalidateInlineAndRequireRepaint();

		private void OnTextHighlightersChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems is not null)
			{
				foreach (var item in e.OldItems)
				{
					if (item is TextHighlighter highlighter)
					{
						if (_textHighlighterDisposables.Remove(highlighter, out var disposable))
						{
							disposable.Dispose();
						}
					}
				}
			}
			if (e.NewItems is not null)
			{
				foreach (var item in e.NewItems)
				{
					if (item is TextHighlighter highlighter)
					{
						var backgroundDisposable = highlighter.RegisterDisposablePropertyChangedCallback(TextHighlighter.BackgroundProperty, OnTextHighlighterPropertyChanged);
						var foregroundDisposable = highlighter.RegisterDisposablePropertyChangedCallback(TextHighlighter.ForegroundProperty, OnTextHighlighterPropertyChanged);
						NotifyCollectionChangedEventHandler onCollectionChanged = (_, _) => InvalidateInlineAndRequireRepaint();
						var rangesDisposable = Disposable.Create(() => ((ObservableCollection<TextRange>)highlighter.Ranges).CollectionChanged -= onCollectionChanged);
						((ObservableCollection<TextRange>)highlighter.Ranges).CollectionChanged += onCollectionChanged;
						var disposable = new CompositeDisposable();
						disposable.Add(backgroundDisposable);
						disposable.Add(foregroundDisposable);
						disposable.Add(rangesDisposable);
						_textHighlighterDisposables.Add(highlighter, disposable);
					}
				}
			}

			InvalidateInlineAndRequireRepaint();
		}

		private void OnTextHighlighterPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			InvalidateInlineAndRequireRepaint();
		}

		void UnicodeText.IFontCacheUpdateListener.Invalidate() => InvalidateMeasure();

		void IBlock.Invalidate(bool updateText) => InvalidateInlineAndRequireRepaint();
		string IBlock.GetText() => Text;

		partial void OnSelectionChanged()
		{
			InvalidateInlineAndRequireRepaint();

			var start = Math.Min(Selection.start, Selection.end);
			var end = Math.Max(Selection.start, Selection.end);
			SelectedText = Text[start..end];

			if (start == end)
			{
				// No selection left to grab: drop the touch grippers.
				HideGrippers();
			}
		}

		partial void SetupInlines() => RenderSelection = IsTextSelectionEnabled;

		private void OnKeyDown(KeyRoutedEventArgs args)
		{
			switch (args.Key)
			{
				case VirtualKey.C when args.KeyboardModifiers.HasFlag(_platformCtrlKey):
					CopySelectionToClipboard();
					args.Handled = true;
					break;
				case VirtualKey.A when args.KeyboardModifiers.HasFlag(_platformCtrlKey):
					SelectAll();
					args.Handled = true;
					break;
			}
		}

		private void OnTapped(TappedRoutedEventArgs e)
		{
			// Touch tapping is owned by the pointer-released touch path
			if (IsTextSelectionEnabled && e.PointerDeviceType == PointerDeviceType.Mouse)
			{
				Selection = default;
			}
		}

		private void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			if (IsTextSelectionEnabled)
			{
				if (GetCharacterIndexAtPoint(e.GetPosition(this), true) is var index and > 1)
				{
					var chunk = ParsedText.GetWordAt(index, true);

					Selection = new Range(chunk.start, chunk.start + chunk.length);

					if (e.PointerDeviceType is not PointerDeviceType.Mouse && chunk.length > 0)
					{
						ShowGrippers();
					}
				}
			}
		}

		public void CopySelectionToClipboard()
		{
			if (Selection.start != Selection.end)
			{
				var text = SelectedText;
				var dataPackage = new DataPackage();
				dataPackage.SetText(text);
				Clipboard.SetContent(dataPackage);
			}
		}

		public void SelectAll() => Selection = new Range(0, Text.Length);

		// TODO: move to TextBlock.cs when we implement SelectionHighlightColor for the other platforms
		#region SelectionHighlightColor (DP)
		public SolidColorBrush SelectionHighlightColor
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
			set => SetValue(SelectionHighlightColorProperty, value);
		}

		public static DependencyProperty SelectionHighlightColorProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionHighlightColor),
				typeof(SolidColorBrush),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					DefaultBrushes.SelectionHighlightColor,
					propertyChangedCallback: (s, e) => ((TextBlock)s)?.OnSelectionHighlightColorChanged((SolidColorBrush)e.OldValue, (SolidColorBrush)e.NewValue)));

		private void OnSelectionHighlightColorChanged(SolidColorBrush? oldBrush, SolidColorBrush? newBrush)
		{
			oldBrush ??= DefaultBrushes.SelectionHighlightColor;
			newBrush ??= DefaultBrushes.SelectionHighlightColor;

			_selectionHighlightBrushChangedSubscription?.Dispose();
			_selectionHighlightBrushChangedSubscription = Brush.SetupBrushChanged(newBrush, ref _selectionHighlightColorChanged, () => OnSelectionHighlightColorChangedPartial(newBrush));
		}

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush);
		#endregion

		#region SelectedText (DP - readonly)
		public static DependencyProperty SelectedTextProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectedText), typeof(string),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(string.Empty));

		public string SelectedText
		{
			get => (string)this.GetValue(SelectedTextProperty);
			private set => this.SetValue(SelectedTextProperty, value);
		}
		#endregion

		partial void UpdateIsTextTrimmed()
		{
			IsTextTrimmed = IsTextTrimmable && (
				_lastInlinesArrangeWithPadding.Width > ActualWidth ||
				_lastInlinesArrangeWithPadding.Height > ActualHeight
			);
		}

		/// <summary>
		/// Returns a mask that represents the alpha channel of the text as a CompositionBrush.
		/// This brush can be used with CompositionMaskBrush or DropShadow.Mask to create shaped effects.
		/// </summary>
		/// <returns>A CompositionBrush representing the text as an alpha mask.</returns>
		public CompositionBrush GetAlphaMask()
		{
			var compositor = Compositor.GetSharedCompositor();
			var surface = new AlphaMaskSurface(compositor, Visual);
			var brush = compositor.CreateSurfaceBrush(surface);
			brush.Stretch = CompositionStretch.None;
			return brush;
		}

		#region SelectionFlyout Support
		// Ported from: microsoft-ui-xaml2/src/dxaml/xcp/core/text/common/TextSelectionManager.cpp (lines 3381-3420)
		// TextSelectionManager::UpdateSelectionFlyoutVisibility

		private PointerDeviceType _lastInputDeviceType;
		private Point _lastPointerPosition;
		private bool _isSelectionFlyoutUpdateQueued;

		private bool HasSelectionFlyout() => SelectionFlyout is not null;

		/// <summary>
		/// Called from OnPointerReleased to handle touch/pen tap selection and queue a SelectionFlyout
		/// visibility update. Mouse input is handled on the pressed/move side and ignored here (matching WinUI).
		/// </summary>
		partial void OnPointerReleasedForSelectionFlyout(PointerRoutedEventArgs e)
		{
			// Mouse doesn't get the gripper/selection-flyout treatment (matching WinUI behavior).
			if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse || !IsTextSelectionEnabled)
			{
				return;
			}

			var down = _lastPointerDownPoint;
			_lastPointerDownPoint = null;

			if (FindHyperlinkAt(e) is not null)
			{
				// Tapping a hyperlink: navigation is handled elsewhere, don't start a selection.
				return;
			}

			if (down is null)
			{
				return;
			}

			var touchHoldTime = e.GetCurrentPoint(null).Timestamp - down.Timestamp;
			if (touchHoldTime >= Microsoft.UI.Input.GestureRecognizer.HoldMinDelayMicroseconds)
			{
				// Long-press: the context menu was already opened through the Holding gesture
				// (ContextRequested).
				return;
			}

			TouchTap(e.GetCurrentPoint(this).Position);
			QueueUpdateSelectionFlyoutVisibility(e.Pointer.PointerDeviceType, e.GetCurrentPoint(this).Position);
		}

		private void QueueUpdateSelectionFlyoutVisibility(PointerDeviceType deviceType, Point position)
		{
			_lastInputDeviceType = deviceType;
			_lastPointerPosition = position;

			// Prevent duplicate queued updates (matching TextBox behavior)
			if (!_isSelectionFlyoutUpdateQueued)
			{
				_isSelectionFlyoutUpdateQueued = true;
				DispatcherQueue.TryEnqueue(() => UpdateSelectionFlyoutVisibility());
			}
		}

		private void UpdateSelectionFlyoutVisibility()
		{
			// Reset the queued flag
			_isSelectionFlyoutUpdateQueued = false;

			if (!HasSelectionFlyout() || TextControlFlyoutHelper.IsOpen(ContextFlyout))
			{
				return;
			}

			var selectionLength = Math.Abs(Selection.end - Selection.start);
			var showMode = FlyoutShowMode.Transient;
			var shouldShow = false;

			switch (_lastInputDeviceType)
			{
				case PointerDeviceType.Mouse:
					// Mouse doesn't show SelectionFlyout (matching WinUI behavior)
					shouldShow = false;
					break;
				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
					if (selectionLength > 0)
					{
						shouldShow = true;
						showMode = FlyoutShowMode.Transient;
					}
					break;
				default:
					shouldShow = false;
					break;
			}

			if (shouldShow)
			{
				// Get selection bounds and adjust flyout position (Y = top of selection)
				var position = _lastPointerPosition;

				var startIndex = Math.Min(Selection.start, Selection.end);
				var endIndex = Math.Max(Selection.start, Selection.end);
				var startRect = ParsedText.GetRectForIndex(startIndex);
				var endRect = ParsedText.GetRectForIndex(endIndex);
				var selectionTop = Math.Min(startRect.Top, endRect.Top);

				// Adjust for padding
				position = new Point(position.X, selectionTop + Padding.Top);

				if (SelectionFlyout is { } selectionFlyout)
				{
					TextControlFlyoutHelper.ShowAt(selectionFlyout, this, position, showMode);
				}
			}
			else
			{
				// Close SelectionFlyout if it's open and we shouldn't show it
				if (SelectionFlyout?.IsOpen == true)
				{
					SelectionFlyout.Hide();
				}
			}

			// Reset input device type after processing (matching WinUI behavior)
			_lastInputDeviceType = default;
		}
		#endregion

		#region Touch selection grippers
		// The gripper visuals and all the drag/positioning mechanics live in the shared
		// TextSelectionGripperPresenter (also used by TextBox). A TextBlock has no caret/insertion point,
		// so its GripperMode is only ever Hidden or Both (a non-empty selection).

		// Test hook: the pair of selection grippers when they are currently showing, otherwise null.
		internal (CaretWithStemAndThumb start, CaretWithStemAndThumb end)? SelectionGrippersForTesting
			=> _gripperPresenter?.VisibleGrippersForTesting;

		private void ShowGrippers()
		{
			if (!IsTextSelectionEnabled)
			{
				return;
			}

			_gripperPresenter ??= new TextSelectionGripperPresenter(this);
			_grippersShown = true;
			_gripperPresenter.Update();
		}

		private void HideGrippers()
		{
			_grippersShown = false;
			_gripperPresenter?.Hide();
		}

		// Touch tap handling: select the tapped word (and show the grippers), or keep an existing
		// selection if the tap landed inside it. The point is relative to the TextBlock.
		private void TouchTap(Point point)
		{
			var textPoint = new Point(point.X - Padding.Left, point.Y - Padding.Top);
			var index = Math.Max(0, ParsedText.GetIndexAt(textPoint, true, true));

			var selStart = Math.Min(Selection.start, Selection.end);
			var selEnd = Math.Max(Selection.start, Selection.end);
			if (selStart != selEnd && selStart <= index && index < selEnd)
			{
				// Tapped inside the current selection: keep it and re-show the grippers/flyout.
				ShowGrippers();
				return;
			}

			var chunk = ParsedText.GetWordAt(index, true);
			Selection = new Range(chunk.start, chunk.start + chunk.length);
			if (chunk.length > 0)
			{
				ShowGrippers();
			}
			else
			{
				HideGrippers();
			}
		}

		#region ITextSelectionGripperHost
		TextBlock ITextSelectionGripperHost.GripperTextSurface => this;

		Rect ITextSelectionGripperHost.GripperClipBounds => this.GetAbsoluteBoundsRect();

		GripperMode ITextSelectionGripperHost.GripperMode => _grippersShown ? GripperMode.Both : GripperMode.Hidden;

		int ITextSelectionGripperHost.SelectionLowerIndex => Math.Min(Selection.start, Selection.end);

		int ITextSelectionGripperHost.SelectionUpperIndex => Math.Max(Selection.start, Selection.end);

		void ITextSelectionGripperHost.SetGripperSelection(int start, int end) => Selection = new Range(start, end);

		// A TextBlock never reports GripperMode.EndOnly, so this is only a defensive fallback.
		void ITextSelectionGripperHost.MoveGripperCaret(int index) => Selection = new Range(index, index);

		// A TextBlock doesn't scroll its own content, so there's nothing to bring into view.
		void ITextSelectionGripperHost.ScrollForGripper(bool isEndGripper) { }

		void ITextSelectionGripperHost.OnGripperPressed() => DismissSelectionFlyoutForPointerPress();

		// Dismiss the selection flyout (the floating copy bar) when a touch interaction begins, so it
		// doesn't linger next to a context menu opened on the same gesture.
		internal void DismissSelectionFlyoutForPointerPress() => TextControlFlyoutHelper.CloseIfOpen(SelectionFlyout);

		void ITextSelectionGripperHost.RequestGripperContextMenu(PointerRoutedEventArgs args)
		{
			var contextArgs = new ContextRequestedEventArgs();
			contextArgs.SetGlobalPoint(args.GetCurrentPoint(null).Position);
			OnContextRequested(this, contextArgs);
		}

		// A TextBlock only ever shows Both-mode grippers over a real selection, so there's never a collapsed
		// caret to re-open the flyout over; allowEmptySelection is irrelevant here.
		void ITextSelectionGripperHost.QueueGripperSelectionFlyout(PointerRoutedEventArgs args, bool allowEmptySelection)
			=> QueueUpdateSelectionFlyoutVisibility(args.Pointer.PointerDeviceType, args.GetCurrentPoint(this).Position);

		// Both grippers sit on the current selection's edges, so tapping either one keeps the selection and
		// re-shows the grippers/flyout — there's no insertion handle (or editable caret) to re-place, so press
		// and anchorIndex are unused here.
		void ITextSelectionGripperHost.OnGripperTapped(Microsoft.UI.Input.PointerPoint press, int anchorIndex)
			=> ShowGrippers();
		#endregion
		#endregion

		/// <summary>
		/// Fires the ContextMenuOpening event synchronously and returns whether it was handled.
		/// </summary>
		/// <remarks>
		/// Ported from CTextBlock::FireContextMenuOpeningEventSynchronously (TextBlock.cpp:4107)
		/// and TextControlHelper::OnContextMenuOpeningHandler (TextControlHelper.h:10).
		///
		/// WinUI does this->TransformToRoot(point) then divides by rasterization scale.
		/// In Uno/Skia, TransformToVisual(null) already yields DIP coordinates.
		/// </remarks>
		internal bool FireContextMenuOpeningEventSynchronously(Point point)
		{
			// WinUI: TransformToRoot + pointerPosition /= zoomScale
			var rootPoint = TransformToVisual(null).TransformPoint(point);

			var args = new ContextMenuEventArgs(rootPoint.X, rootPoint.Y);
			ContextMenuOpening?.Invoke(this, args);
			return args.Handled;
		}
#nullable disable
	}
}
