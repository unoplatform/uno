using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Composition;
using System.Numerics;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Core.Scaling;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Input;

#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	partial class TextBlock : FrameworkElement, IBlock, UnicodeText.IFontCacheUpdateListener
	{
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

		internal IParsedText ParsedText { get; private set; } = Microsoft.UI.Xaml.Documents.ParsedText.Empty;

		internal event Action? DrawingFinished;

		public TextBlock()
		{
			UpdateLastUsedTheme();

			_hyperlinks.CollectionChanged += HyperlinksOnCollectionChanged;
			((ObservableCollection<TextHighlighter>)TextHighlighters).CollectionChanged += OnTextHighlightersChanged;

			Tapped += static (s, e) => ((TextBlock)s).OnTapped(e);
			DoubleTapped += static (s, e) => ((TextBlock)s).OnDoubleTapped(e);
			KeyDown += static (s, e) => ((TextBlock)s).OnKeyDown(e);

			GotFocus += (_, _) => UpdateSelectionRendering();
			LostFocus += (_, _) => UpdateSelectionRendering();
		}

		internal TextBox? OwningTextBox { private get; init; }

		internal bool IsSpellCheckEnabled { get; set; }

		private protected override void OnLoaded()
		{
			base.OnLoaded();
#if DEBUG
			Visual.Comment = $"{Visual.Comment}#text";
#endif
			// Ensure the default ContextFlyout has its Opening event subscribed.
			// This is needed because default flyouts (via GetDefaultValue) don't trigger OnContextFlyoutChanged.
			EnsureContextFlyoutSubscription();
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

		private UnicodeText ParseText(Size availableSizeWithoutPadding, out Size size) =>
			new UnicodeText(
				availableSizeWithoutPadding,
				Inlines.TraversedTree.leafTree,
				GetDefaultFontDetails(),
				MaxLines,
				(float)LineHeight,
				LineStackingStrategy,
				FlowDirection,
				(OwningTextBox as IDependencyObjectStoreProvider)?.Store
					.GetCurrentHighestValuePrecedence(TextBox.TextAlignmentProperty) is DependencyPropertyValuePrecedences.DefaultValue
						? null
						: TextAlignment,
				TextWrapping,
				TextTrimming,
				IsSpellCheckEnabled,
				this,
				out size);

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
				RenderSelection = IsTextSelectionEnabled && (IsFocused || (ContextFlyout?.IsOpen ?? false));
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Visual.Compositor.InvalidateRender(Visual);
			var padding = Padding;
			var availableSizeWithoutPadding = finalSize.Subtract(padding);
			ParsedText = ParseText(availableSizeWithoutPadding, out var arrangedSize);

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
			ParsedText.Draw(
				session,
				_caretPaint is { } c ? (c.index, c.brush, CaretThickness) : null,
				highligherters);
			session.Canvas.Restore();
			DrawingFinished?.Invoke();
		}

		/// <summary>
		/// Gets the line height of the TextBlock either
		/// based on the LineHeight property or the default
		/// font line height.
		/// </summary>
		/// <returns>Computed line height</returns>
		private FontDetails GetDefaultFontDetails()
		{
			var (details, task) = FontDetailsCache.GetFont(FontFamily?.Source, (float)FontSize, FontWeight, FontStretch, FontStyle);
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

		private void OnTapped(TappedRoutedEventArgs _)
		{
			if (IsTextSelectionEnabled)
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

		#region SelectionFlyout Support
		// Ported from: microsoft-ui-xaml2/src/dxaml/xcp/core/text/common/TextSelectionManager.cpp (lines 3381-3420)
		// TextSelectionManager::UpdateSelectionFlyoutVisibility

		private PointerDeviceType _lastInputDeviceType;
		private Point _lastPointerPosition;
		private bool _isSelectionFlyoutUpdateQueued;

		// Track the flyout we've subscribed to for Opening event (to avoid double-subscription)
		private FlyoutBase? _subscribedContextFlyout;

		private bool HasSelectionFlyout() => SelectionFlyout is not null;

		private void EnsureContextFlyoutSubscription()
		{
			var currentFlyout = ContextFlyout;
			if (currentFlyout is not null && _subscribedContextFlyout != currentFlyout)
			{
				if (_subscribedContextFlyout is not null)
				{
					_subscribedContextFlyout.Opening -= OnContextFlyoutOpening;
				}
				currentFlyout.Opening += OnContextFlyoutOpening;
				_subscribedContextFlyout = currentFlyout;
			}
		}

		// Handle ContextFlyout/SelectionFlyout coordination - ContextFlyout takes priority
		private protected override void OnContextFlyoutChanged(FlyoutBase oldValue, FlyoutBase newValue)
		{
			base.OnContextFlyoutChanged(oldValue, newValue);

			if (oldValue is not null && oldValue == _subscribedContextFlyout)
			{
				oldValue.Opening -= OnContextFlyoutOpening;
				_subscribedContextFlyout = null;
			}
			if (newValue is not null)
			{
				newValue.Opening += OnContextFlyoutOpening;
				_subscribedContextFlyout = newValue;
			}
		}

		private void OnContextFlyoutOpening(object? sender, object e)
		{
			// Close SelectionFlyout when ContextFlyout opens (ContextFlyout takes priority)
			SelectionFlyout?.Hide();
		}

		/// <summary>
		/// Called from OnPointerReleased to queue SelectionFlyout visibility update for non-mouse input.
		/// </summary>
		partial void OnPointerReleasedForSelectionFlyout(PointerRoutedEventArgs e)
		{
			// Only show SelectionFlyout for touch/pen input, not mouse (matching WinUI behavior)
			if (e.Pointer.PointerDeviceType is not PointerDeviceType.Mouse && IsTextSelectionEnabled)
			{
				QueueUpdateSelectionFlyoutVisibility(e.Pointer.PointerDeviceType, e.GetCurrentPoint(this).Position);
			}
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

			if (!HasSelectionFlyout() || (ContextFlyout?.IsOpen ?? false))
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

				SelectionFlyout?.ShowAt(this, new FlyoutShowOptions
				{
					Position = position,
					ShowMode = showMode
				});
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
	}
}
