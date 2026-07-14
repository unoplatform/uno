using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Internal;
using Microsoft.UI.Input;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Core.Scaling;
using Uno.UI.Xaml.Media;

#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	partial class RichTextBlock : UnicodeText.IFontCacheUpdateListener
	{
		internal const float CaretThickness = 1;

		private Action? _selectionHighlightColorChanged;
		private IDisposable? _selectionHighlightBrushChangedSubscription;
		private readonly VirtualKeyModifiers _platformCtrlKey = Uno.UI.Helpers.DeviceTargetHelper.PlatformCommandModifier;
		private readonly Dictionary<TextHighlighter, IDisposable> _textHighlighterDisposables = new();
		private bool _renderSelection;
		private bool _forceFocusedForContextFlyout;
		private bool _isSelectionFlyoutUpdateQueued;
		private PointerDeviceType _lastInputDeviceType;
		private Point _lastPointerPosition;

		/// <summary>
		/// Layout data for a single paragraph within the RichTextBlock.
		/// </summary>
		internal record struct ParagraphLayout(
			ParsedText ParsedText,
			float YOffset,
			int GlobalCharOffset,
			Size Size,
			Thickness Margin);

		private List<ParagraphLayout> _paragraphLayouts = new();
		private Size _lastMeasuredContentSize;
		private bool _isContentClippedByMaxLines;

		// Stage 9: the ported BlockLayout engine drives measure/arrange; the node tree is the
		// authoritative layout. _paragraphLayouts is rebuilt from the arranged tree and still
		// backs the render/selection path (replaced by the view layer in a later step).
		private BlockLayoutEngine? _blockLayout;
		private PageNode? _pageNode;
		private RichTextBlockBreak? _break;

		private protected override ContainerVisual CreateElementVisual() => new RichTextVisual(Compositor.GetSharedCompositor(), this);

		partial void InitializePartial()
		{
			((ObservableCollection<TextHighlighter>)TextHighlighters).CollectionChanged += OnTextHighlightersChanged;

			Tapped += static (s, e) => ((RichTextBlock)s).OnTapped(e);
			DoubleTapped += static (s, e) => ((RichTextBlock)s).OnDoubleTapped(e);
			KeyDown += static (s, e) => ((RichTextBlock)s).OnKeyDown(e);

			GotFocus += (_, _) =>
			{
				_forceFocusedForContextFlyout = false;
				UpdateSelectionRendering();
			};
			LostFocus += (_, _) =>
			{
				_forceFocusedForContextFlyout = ShouldForceFocusedVisualState();
				UpdateSelectionRendering();
			};
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();
#if DEBUG
			Visual.Comment = $"{Visual.Comment}#richtext";
#endif
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			_forceFocusedForContextFlyout = false;
			TextSelectionManager.Destroy(ref _pSelectionManager);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			RebuildBlockLayout();

			Size desiredSize = default;

			if (_pageNode is not null)
			{
				// Always use the RichTextBlockBreak object to retrieve the old page break. If the PageNode has not been
				// deleted it should be exactly the same as the page node's break.
				BlockNodeBreak? oldPageBreak = _break?.GetBlockBreak();

				// PageNode reads Padding and MaxLines off this owner; suppressTopMargin=true matches
				// CRichTextBlock::MeasureOverride. The engine handles padding internally, so the
				// desired size already includes it (no manual add like the old flat path).
				_pageNode.Measure(availableSize, (uint)MaxLines, 0f, false, false, true, null, out _);
				desiredSize = _pageNode.GetDesiredSize();

				// If PageNode bypasses Measure, its break will be exactly the same, and there's no need to notify
				// overflow elements. If it changed, update this container's break and set it, notifying overflows.
				// Ref-equality (not BlockNodeBreak.Equals) is intentional - see CRichTextBlock::MeasureOverride.
				var pageBreak = _pageNode.GetBreak();
				if (pageBreak is not null)
				{
					if (!ReferenceEquals(pageBreak, oldPageBreak))
					{
						SetBreak(new RichTextBlockBreak(pageBreak));
					}
				}
				else
				{
					SetBreak(null);
				}
			}

			if (GetUseLayoutRounding())
			{
				var plateauScale = RootScale.GetRasterizationScaleForElement(this);
				desiredSize.Width = ((int)Math.Ceiling(desiredSize.Width * plateauScale)) / plateauScale;
				desiredSize.Height = ((int)Math.Ceiling(desiredSize.Height * plateauScale)) / plateauScale;
			}

			return desiredSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Visual.Compositor.InvalidateRender(Visual);

			if (_pageNode is not null && !_pageNode.IsMeasureDirty())
			{
				_pageNode.Arrange(finalSize);
				PopulateLayoutsFromTree();
			}

			var result = base.ArrangeOverride(finalSize);
			UpdateIsTextTrimmed();
			return result;
		}

		// Mirrors CRichTextBlock::EnsureBlockLayout: create the layout engine + page node once.
		// Calling GetDefaultFontDetails warms the font cache and hooks async font-load invalidation
		// (the engine resolves fonts via BlockLayoutHelpers/FontDetailsCache).
		private void RebuildBlockLayout()
		{
			_ = GetDefaultFontDetails();

			_blockLayout ??= new BlockLayoutEngine(this);

			// Recreate the page node each measure so content/property changes (FontSize, Padding,
			// inlines, wrapping, MaxLines) are picked up — parity with the old re-parse-every-measure
			// path. (A future optimization can invalidate only on change.)
			_pageNode = (PageNode)_blockLayout.CreatePageNode(Blocks, this);

			// Rebuild the per-element view against the fresh page node and notify the manager so its
			// selection / text positions are recreated against the new view. When this control is linked
			// to an overflow chain the manager queries through the stable LinkedRichTextBlockView, so the
			// per-element view swap must not be reported as the manager's active view (that stays the linked
			// view); only swap the manager when standalone.
			var oldElementView = _pTextView;
			_pTextView = new Microsoft.UI.Xaml.Controls.Text.Core.RichTextBlockView(_pageNode, this);
			if (_pSelectionManager is null && (IsTextSelectionEnabled || ((ITextSelectionManagerOwner)this).IsHighContrast))
			{
				_pSelectionManager = TextSelectionManager.Create(this, Blocks.GetTextContainer(), this);
			}
			if (_pLinkedView is null)
			{
				_pSelectionManager?.TextViewChanged(oldElementView, _pTextView);
			}
		}

		// Ensures a block layout exists without discarding a measured one. The text-pointer and
		// hit-testing APIs query the arranged page node, so recreating it here would leave them
		// querying a node that is still measure/arrange dirty (the view then reports position 0).
		private void EnsureBlockLayout()
		{
			if (_pageNode is null)
			{
				RebuildBlockLayout();
			}
		}

		// Rebuilds _paragraphLayouts (render/selection data) from the arranged PageNode tree. The
		// engine is the authoritative layout; each ParagraphNode shares one ParsedText with its lines,
		// drawn once at the paragraph offset. Replaced by the view layer in a later step.
		private void PopulateLayoutsFromTree()
		{
			_paragraphLayouts.Clear();
			_isContentClippedByMaxLines = false;

			float accumHeight = 0;
			float maxWidth = 0;
			int globalCharOffset = 0;
			int blockIndex = 0;
			int blockCount = Blocks.Count;
			int totalLines = 0;

			for (var child = _pageNode?.GetFirstChild(); child is not null; child = child.GetNext())
			{
				var para = blockIndex < blockCount ? Blocks[blockIndex] as Paragraph : null;

				if (child is ParagraphNode paragraphNode &&
					para is not null &&
					paragraphNode.GetParsedText() is { } parsed)
				{
					var margin = para.Margin;
					var boxSize = paragraphNode.GetDesiredSize(); // content + margins
					var contentSize = new Size(
						Math.Max(0, boxSize.Width - margin.Left - margin.Right),
						Math.Max(0, boxSize.Height - margin.Top - margin.Bottom));

					_paragraphLayouts.Add(new ParagraphLayout(
						ParsedText: parsed,
						YOffset: accumHeight + (float)margin.Top,
						GlobalCharOffset: globalCharOffset,
						Size: contentSize,
						Margin: margin));

					accumHeight += (float)boxSize.Height;
					maxWidth = Math.Max(maxWidth, (float)boxSize.Width);
					totalLines += paragraphNode.LineCount;
				}

				if (para is not null)
				{
					globalCharOffset += GetParagraphTextLength(para);
					if (blockIndex < blockCount - 1)
					{
						// "\r\n" separator, same flat char-index space the text view maps against.
						globalCharOffset += Microsoft.UI.Xaml.Controls.Text.Core.RichTextBlockView.InterParagraphSeparatorLength;
					}
				}

				blockIndex++;
			}

			_isContentClippedByMaxLines = _break is not null || (MaxLines > 0 && totalLines >= MaxLines);
			_lastMeasuredContentSize = new Size(maxWidth, accumHeight);
		}

		private static int GetParagraphTextLength(Paragraph paragraph)
		{
			return string.Concat(paragraph.Inlines.Select(InlineExtensions.GetText)).Length;
		}

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

		internal void Draw(in Visual.PaintingSession session)
		{
			var canvas = session.Canvas;
			canvas.Save();
			canvas.Translate((float)Padding.Left, (float)Padding.Top);

			for (int p = 0; p < _paragraphLayouts.Count; p++)
			{
				var layout = _paragraphLayouts[p];
				canvas.Save();
				canvas.Translate((float)layout.Margin.Left, layout.YOffset);

				// Build highlighters for this paragraph (including selection)
				var paragraphHighlighters = GetParagraphHighlighters(layout, p);

				layout.ParsedText.Draw(session, null, paragraphHighlighters, compositionRange: null);

				canvas.Restore();
			}

			canvas.Restore();
		}

		private IEnumerable<TextHighlighter> GetParagraphHighlighters(ParagraphLayout layout, int paragraphIndex)
		{
			// Called once per paragraph on every paint: collect into a list instead of chaining
			// Enumerable.Append iterators, which allocates one wrapper per highlighter per frame.
			var result = new List<TextHighlighter>();

			// Apply TextHighlighters (translated to paragraph-local coordinates)
			foreach (var highlighter in TextHighlighters)
			{
				foreach (var range in highlighter.Ranges)
				{
					var globalStart = range.StartIndex;
					var globalEnd = globalStart + range.Length;
					var paraStart = layout.GlobalCharOffset;
					var paraEnd = paraStart + GetParagraphTextLengthFromLayout(paragraphIndex);

					if (globalEnd > paraStart && globalStart < paraEnd)
					{
						var localStart = Math.Max(0, globalStart - paraStart);
						var localEnd = Math.Min(paraEnd - paraStart, globalEnd - paraStart);
						result.Add(new TextHighlighter
						{
							Background = highlighter.Background,
							Foreground = highlighter.Foreground,
							Ranges =
							{
								new TextRange { StartIndex = localStart, Length = localEnd - localStart }
							}
						});
					}
				}
			}

			// Apply selection
			if (_renderSelection)
			{
				var selStart = Math.Min(Selection.start, Selection.end);
				var selEnd = Math.Max(Selection.start, Selection.end);
				var paraStart = layout.GlobalCharOffset;
				var paraEnd = paraStart + GetParagraphTextLengthFromLayout(paragraphIndex);

				if (selEnd > paraStart && selStart < paraEnd)
				{
					var localStart = Math.Max(0, selStart - paraStart);
					var localEnd = Math.Min(paraEnd - paraStart, selEnd - paraStart);
					result.Add(new TextHighlighter
					{
						Background = SelectionHighlightColor ?? DefaultBrushes.SelectionHighlightColor,
						Foreground = DefaultBrushes.SelectedTextForegroundColor,
						Ranges =
						{
							new TextRange { StartIndex = localStart, Length = localEnd - localStart }
						}
					});
				}
			}

			return result;
		}

		private int GetParagraphTextLengthFromLayout(int paragraphIndex)
		{
			if (paragraphIndex < Blocks.Count && Blocks[paragraphIndex] is Paragraph para)
			{
				return GetParagraphTextLength(para);
			}

			return 0;
		}

		// the entire body of the rich text block is considered hit-testable
		internal override bool HitTest(Point point)
		{
			var transform = GetTransform(this, (UIElement)this.GetParent());
			var success = Matrix3x2.Invert(transform, out var inverted);
			return success && inverted.Transform(LayoutSlotWithMarginsAndAlignments).Contains(point);
		}

		partial void OnIsTextSelectionEnabledChangedPartial()
		{
			UpdateSelectionRendering();
			if (IsTextSelectionEnabled)
			{
				EnsureContextMenuGesturesEnabled();
			}
		}

		private void UpdateSelectionRendering()
		{
			RenderSelection = IsTextSelectionEnabled && (IsFocused || _forceFocusedForContextFlyout);
		}

		private bool ShouldForceFocusedVisualState()
		{
			return TextControlFlyoutHelper.IsGettingFocus(SelectionFlyout, this)
				|| TextControlFlyoutHelper.IsGettingFocus(ContextFlyout, this);
		}

		internal void ForceFocusLoss()
		{
			_forceFocusedForContextFlyout = false;
			UpdateSelectionRendering();
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

		private void InvalidateInlineAndRequireRepaint()
		{
			Visual.Compositor.InvalidateRender(Visual);
		}

		partial void InvalidateRichTextBlockPartial()
		{
			InvalidateInlineAndRequireRepaint();
			// Master content/property changes invalidate the linked overflow chain so it re-measures
			// its slice from the (re)computed master break. Mirrors CRichTextBlock::InvalidateContentMeasure
			// driving CRichTextBlockOverflow::InvalidateAllOverflowContentMeasure down the chain.
			InvalidateOverflowChainContentMeasure();
		}
		partial void OnForegroundChangedPartial() => InvalidateInlineAndRequireRepaint();

		void UnicodeText.IFontCacheUpdateListener.Invalidate() => InvalidateMeasure();

		partial void OnSelectionChanged()
		{
			InvalidateInlineAndRequireRepaint();
			// Update SelectedText based on global selection
			var text = GetPlainText();
			var start = Math.Min(Selection.start, Selection.end);
			var end = Math.Max(Selection.start, Selection.end);
			start = Math.Clamp(start, 0, text.Length);
			end = Math.Clamp(end, 0, text.Length);
			SelectedText = text[start..end];
			RaiseSelectionChanged();
		}

		partial void UpdateIsTextTrimmed()
		{
			IsTextTrimmed = IsTextTrimmable && (
				_isContentClippedByMaxLines ||
				_lastMeasuredContentSize.Width + Padding.Left + Padding.Right > ActualWidth ||
				_lastMeasuredContentSize.Height + Padding.Top + Padding.Bottom > ActualHeight);
		}

		private int GetCharacterIndexAtPointSkia(Point point, bool extended)
		{
			// Adjust for padding
			var adjustedPoint = new Point(point.X - Padding.Left, point.Y - Padding.Top);

			for (int p = 0; p < _paragraphLayouts.Count; p++)
			{
				var layout = _paragraphLayouts[p];
				var paraTop = layout.YOffset;
				var paraBottom = paraTop + layout.Size.Height;

				if (adjustedPoint.Y >= paraTop && adjustedPoint.Y < paraBottom || (extended && p == _paragraphLayouts.Count - 1))
				{
					var localPoint = new Point(
						adjustedPoint.X - layout.Margin.Left,
						adjustedPoint.Y - layout.YOffset);
					var localIndex = layout.ParsedText.GetIndexAt(localPoint, false, extended);
					if (localIndex >= 0)
					{
						return layout.GlobalCharOffset + localIndex;
					}
				}
			}

			return extended && _paragraphLayouts.Count > 0
				? GetPlainText().Length
				: -1;
		}

		private Hyperlink? FindHyperlinkAtSkia(PointerRoutedEventArgs e)
		{
			var point = e.GetCurrentPoint(this).Position;
			var adjustedPoint = new Point(point.X - Padding.Left, point.Y - Padding.Top);

			for (int p = 0; p < _paragraphLayouts.Count; p++)
			{
				var layout = _paragraphLayouts[p];
				var paraTop = layout.YOffset;
				var paraBottom = paraTop + layout.Size.Height;

				if (adjustedPoint.Y >= paraTop && adjustedPoint.Y < paraBottom)
				{
					var localPoint = new Point(
						adjustedPoint.X - layout.Margin.Left,
						adjustedPoint.Y - layout.YOffset);
					return layout.ParsedText.GetHyperlinkAt(localPoint);
				}
			}

			return null;
		}

		private void OnKeyDown(KeyRoutedEventArgs args)
		{
			if (IsTextSelectionEnabled && _pSelectionManager is { } manager && _pTextView is { } view)
			{
				manager.OnKeyDown(this, args, view);
				return;
			}

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
			if (IsTextSelectionEnabled && _pSelectionManager is { } manager && _pTextView is { } view)
			{
				manager.OnTapped(this, e, view);
				return;
			}

			if (IsTextSelectionEnabled)
			{
				Selection = default;
			}
		}

		private void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			if (!IsTextSelectionEnabled)
			{
				return;
			}

			if (_pSelectionManager is { } manager && _pTextView is { } view)
			{
				manager.OnDoubleTapped(this, e, view);
				return;
			}

			var index = GetCharacterIndexAtPoint(e.GetPosition(this), true);
			if (index < 0)
			{
				return;
			}

			// Find which paragraph this index belongs to
			for (int p = 0; p < _paragraphLayouts.Count; p++)
			{
				var layout = _paragraphLayouts[p];
				var paraTextLen = GetParagraphTextLengthFromLayout(p);
				if (index >= layout.GlobalCharOffset && index < layout.GlobalCharOffset + paraTextLen)
				{
					var localIndex = index - layout.GlobalCharOffset;
					var chunk = layout.ParsedText.GetWordAt(localIndex, true);
					Selection = new Range(
						layout.GlobalCharOffset + chunk.start,
						layout.GlobalCharOffset + chunk.start + chunk.length);
					return;
				}
			}
		}

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush oldBrush, SolidColorBrush newBrush)
		{
			var newValue = newBrush ?? DefaultBrushes.SelectionHighlightColor;

			_selectionHighlightBrushChangedSubscription?.Dispose();
			_selectionHighlightBrushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _selectionHighlightColorChanged, () => InvalidateInlineAndRequireRepaint());
		}

		#region SelectionFlyout Support

		partial void OnPointerReleasedForSelectionFlyout(PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType is not PointerDeviceType.Mouse && IsTextSelectionEnabled)
			{
				QueueUpdateSelectionFlyoutVisibility(e.Pointer.PointerDeviceType, e.GetCurrentPoint(this).Position);
			}
		}

		private void QueueUpdateSelectionFlyoutVisibility(PointerDeviceType deviceType, Point position)
		{
			_lastInputDeviceType = deviceType;
			_lastPointerPosition = position;

			if (!_isSelectionFlyoutUpdateQueued)
			{
				_isSelectionFlyoutUpdateQueued = true;
				DispatcherQueue.TryEnqueue(() => UpdateSelectionFlyoutVisibility());
			}
		}

		private void UpdateSelectionFlyoutVisibility()
		{
			_isSelectionFlyoutUpdateQueued = false;

			if (SelectionFlyout is null || TextControlFlyoutHelper.IsOpen(ContextFlyout))
			{
				return;
			}

			var selectionLength = Math.Abs(Selection.end - Selection.start);

			if (_lastInputDeviceType is PointerDeviceType.Pen or PointerDeviceType.Touch && selectionLength > 0)
			{
				TextControlFlyoutHelper.ShowAt(SelectionFlyout, this, _lastPointerPosition, FlyoutShowMode.Transient);
			}
			else if (SelectionFlyout?.IsOpen == true)
			{
				SelectionFlyout.Hide();
			}

			_lastInputDeviceType = default;
		}

		#endregion

		#region ContextMenuOpening

		internal bool FireContextMenuOpeningEventSynchronously(Point point)
		{
			var rootPoint = TransformToVisual(null).TransformPoint(point);
			var args = new ContextMenuEventArgs(rootPoint.X, rootPoint.Y);
			ContextMenuOpening?.Invoke(this, args);
			return args.Handled;
		}

		#endregion

		#region TextHighlighter management

		private void OnTextHighlightersChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems is not null)
			{
				foreach (var item in e.OldItems)
				{
					if (item is TextHighlighter highlighter && _textHighlighterDisposables.Remove(highlighter, out var disposable))
					{
						disposable.Dispose();
					}
				}
			}

			if (e.NewItems is not null)
			{
				foreach (var item in e.NewItems)
				{
					if (item is TextHighlighter highlighter)
					{
						var composite = new CompositeDisposable();
						composite.Add(highlighter.RegisterDisposablePropertyChangedCallback(TextHighlighter.BackgroundProperty, (_, _) => InvalidateInlineAndRequireRepaint()));
						composite.Add(highlighter.RegisterDisposablePropertyChangedCallback(TextHighlighter.ForegroundProperty, (_, _) => InvalidateInlineAndRequireRepaint()));
						NotifyCollectionChangedEventHandler onCollectionChanged = (_, _) => InvalidateInlineAndRequireRepaint();
						((ObservableCollection<TextRange>)highlighter.Ranges).CollectionChanged += onCollectionChanged;
						composite.Add(Disposable.Create(() => ((ObservableCollection<TextRange>)highlighter.Ranges).CollectionChanged -= onCollectionChanged));
						_textHighlighterDisposables.Add(highlighter, composite);
					}
				}
			}

			InvalidateInlineAndRequireRepaint();
		}

		#endregion

		/// <summary>
		/// Skia Visual for RichTextBlock rendering.
		/// </summary>
		internal class RichTextVisual : ContainerVisual
		{
			private readonly WeakReference<RichTextBlock> _owner;

			public RichTextVisual(Compositor compositor, RichTextBlock owner) : base(compositor)
			{
				_owner = new WeakReference<RichTextBlock>(owner);
			}

			internal override void Paint(in PaintingSession session)
			{
				if (_owner.TryGetTarget(out var owner))
				{
					owner.Draw(in session);
				}
			}

			internal override bool CanPaint() => true;
		}
	}
}
