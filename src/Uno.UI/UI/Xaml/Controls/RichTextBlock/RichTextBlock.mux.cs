// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlock.cpp, tag winui3/release/2.4.0, commit e8442d07a
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
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
using Uno.UI.Xaml;
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
		private Task? _pendingFontLoad;
		private bool _forceFocusedForContextFlyout;
		private bool _isSelectionFlyoutUpdateQueued;
		private PointerDeviceType _lastInputDeviceType;
		private Point _lastPointerPosition;

		// Stage 9: the ported BlockLayout engine drives measure, arrange and drawing; the view answers
		// hit-tests and text bounds.
		private BlockLayoutEngine? _blockLayout;
		private PageNode? _pageNode;
		private RichTextBlockBreak? _break;

		internal PageNode? GetPageNode() => _pageNode;

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
		}

		// CRichTextBlock::EnterImpl
		internal override void EnterImpl(EnterParams @params, int depth)
		{
			base.EnterImpl(@params, depth);

			if (@params.IsLive)
			{
				// Upon entering the live tree, a parent or ancestor may have changed and inherited properties
				// should be considered dirty.
				InvalidateContent();
			}
		}

		// CRichTextBlock::LeaveImpl
		internal override void LeaveImpl(LeaveParams @params)
		{
			base.LeaveImpl(@params);

			if (@params.IsLive)
			{
				// Deleting TextSelectionManager here so that Popup can be removed while tree is still alive.
				TextSelectionManager.Destroy(ref _pSelectionManager);

				// Upon leaving the live tree, a parent or ancestor may have changed and inherited properties
				// should be considered dirty.
				InvalidateContent();
			}
		}

		// CRichTextBlock::OnChildDesiredSizeChanged
		private protected override void OnChildDesiredSizeChanged(UIElement child)
		{
			_pageNode?.OnChildDesiredSizeChanged(child);
			_isBreakValid = false;
			base.OnChildDesiredSizeChanged(child);
		}

		// Ensures the embedded UIElements of a RichTextBlock or RichTextBlockOverflow are measured: when the owner
		// is dirty itself, the base measure does not walk to dirty children. A size change invalidates the page
		// node through OnChildDesiredSizeChanged.
		internal static void MeasureDirtyEmbeddedElements(FrameworkElement owner)
		{
			foreach (var child in owner.GetChildren())
			{
				if (child.IsMeasureDirtyOrMeasureDirtyPath)
				{
					child.EnsureLayoutStorage();
					child.Measure(child.m_previousAvailableSize);
				}
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureDirtyEmbeddedElements(this);

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

				// CRichTextBlock renders the page node after arranging it; the lines it records are painted by the visual.
				_pageNode.Draw(false);
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

			// Since IsTextSelectionEnabled is true by default, the manager is created once here.
			if (_pSelectionManager is null && (IsTextSelectionEnabled || ((ITextSelectionManagerOwner)this).IsHighContrast))
			{
				CreateTextSelectionManager();
			}

			// The page node and the view are each created once, exactly as EnsureBlockLayout does.
			// Content and property changes go through InvalidateContent on the existing node rather
			// than a rebuild, which is what keeps the selection's text positions valid across a
			// re-measure (a fresh view makes the manager throw its selection away).
			_pageNode ??= (PageNode)_blockLayout.CreatePageNode(Blocks, this);

			// If there is a valid PageNode, there is content that will be laid out. Create a TextView.
			if (_pageNode is not null && _pTextView is null)
			{
				_pTextView = new Microsoft.UI.Xaml.Controls.Text.Core.RichTextBlockView(_pageNode, this);

				// If there is no linked view, TextSelectionManager needs to be set to the local view.
				if (_pSelectionManager is not null && _pLinkedView is null)
				{
					_pSelectionManager.TextViewChanged(null, _pTextView);
				}
			}
		}

		// CRichTextBlock::CreateTextSelectionManager
		private void CreateTextSelectionManager()
		{
			_pSelectionManager = TextSelectionManager.Create(this, Blocks.GetTextContainer(), this);

			// Set the selection manager with the linked view if one exists.
			if (_pLinkedView is not null)
			{
				_pSelectionManager.TextViewChanged(null, _pLinkedView);
			}
			else if (_pTextView is not null) // set to local view if exists
			{
				_pSelectionManager.TextViewChanged(null, _pTextView);
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

		// The length of the paragraph's text, summed over its leaf inlines rather than concatenated: painting and position
		// mapping ask for it per paragraph.
		internal static int GetParagraphTextLength(Paragraph paragraph)
		{
			var length = 0;
			foreach (var inline in paragraph.Inlines.TraversedTree.leafTree)
			{
				length += inline.GetText().Length;
			}

			return length;
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
				// Measure runs repeatedly while the shared font task is pending: register one continuation
				// per task, and hold the control weakly so an unloaded one is not kept alive by it.
				if (_pendingFontLoad != task)
				{
					_pendingFontLoad = task;
					var self = new WeakReference<RichTextBlock>(this);
					task.ContinueWith(_ => NativeDispatcher.Main.Enqueue(() =>
					{
						if (self.TryGetTarget(out var that) && that._pendingFontLoad == task)
						{
							that._pendingFontLoad = null;
							that.OnFontLoaded();
						}
					}));
				}

				return details;
			}
		}

		internal void Draw(in Visual.PaintingSession session)
			=> RichTextPagePainter.Paint(this, _pageNode, this, session);

		// Whether painting needs the paragraph highlights: text highlighters or a visible selection.
		internal bool HasParagraphHighlights => TextHighlighters.Count > 0 || (_renderSelection && Selection.start != Selection.end);

		// The highlights of one paragraph, from the TextHighlighters and the selection, which are in the control's flat index
		// space (a paragraph's characters, then "\r\n"). The engine ranges leave out an object at either end of a range.
		internal IReadOnlyList<TextHighlighter> GetParagraphHighlighters(int paragraphStart, int paragraphLength, UnicodeParagraphLayout layout)
		{
			// Allocated only for a paragraph that a range touches.
			List<TextHighlighter>? result = null;
			var paragraphEnd = paragraphStart + paragraphLength;

			void Add(int start, int end, Brush? background, Brush? foreground)
			{
				if (end > paragraphStart && start < paragraphEnd)
				{
					var engineStart = layout.ToEngineEnd(Math.Max(0, start - paragraphStart));
					var engineEnd = layout.ToEngineStart(Math.Min(paragraphLength, end - paragraphStart));
					if (engineEnd > engineStart)
					{
						(result ??= new()).Add(new TextHighlighter
						{
							Background = background,
							Foreground = foreground,
							Ranges = { new TextRange { StartIndex = engineStart, Length = engineEnd - engineStart } },
						});
					}
				}
			}

			foreach (var highlighter in TextHighlighters)
			{
				foreach (var range in highlighter.Ranges)
				{
					Add(range.StartIndex, range.StartIndex + range.Length, highlighter.Background, highlighter.Foreground);
				}
			}

			if (_renderSelection)
			{
				Add(
					Math.Min(Selection.start, Selection.end),
					Math.Max(Selection.start, Selection.end),
					SelectionHighlightColor ?? DefaultBrushes.SelectionHighlightColor,
					DefaultBrushes.SelectedTextForegroundColor);
			}

			return result ?? (IReadOnlyList<TextHighlighter>)Array.Empty<TextHighlighter>();
		}

		// the entire body of the rich text block is considered hit-testable
		internal override bool HitTest(Point point)
		{
			var transform = GetTransform(this, (UIElement)this.GetParent());
			var success = Matrix3x2.Invert(transform, out var inverted);
			return success && inverted.Transform(LayoutSlotWithMarginsAndAlignments).Contains(point);
		}

		// CRichTextBlock::OnContentChanged clears the selection through the manager, since the content
		// change may have invalidated the selection's start/end positions.
		partial void ClearSelectionOnContentChangedPartial()
		{
			if (IsSelectionEnabled() && _pSelectionManager?.GetTextSelection() is { } selection)
			{
				selection.Select(0, 0, TextGravity.LineForwardCharacterBackward);

				// WinUI derives SelectedText from the selection on demand. Uno caches it against the
				// flat selection, and going straight to the selection object skips the manager's
				// NotifySelectionChanged, so collapse the flat selection too.
				Selection = default;
			}
		}

		partial void OnIsTextSelectionEnabledChangedPartial()
		{
			UpdateSelectionRendering();
			if (IsTextSelectionEnabled)
			{
				EnsureContextMenuGesturesEnabled();

				// Selection went from disabled to enabled. Create selection manager if it doesn't exist.
				if (_pSelectionManager is null)
				{
					CreateTextSelectionManager();
				}
			}
			else if (_pSelectionManager is not null && !((ITextSelectionManagerOwner)this).IsHighContrast)
			{
				// CRichTextBlock drops the manager once neither selection nor the back plate needs it.
				TextSelectionManager.Destroy(ref _pSelectionManager);
				Selection = default;
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

		// CRichTextBlock::InvalidateRender - the overflow columns paint from this element's state too.
		private void InvalidateInlineAndRequireRepaint()
		{
			InvalidateSelectionRender();
			RichTextBlockOverflow.InvalidateAllOverflowRender(_pOverflowTarget);
		}

		// CRichTextBlock::InvalidateSelectionRender
		private void InvalidateSelectionRender() => Visual.Compositor.InvalidateRender(Visual);

		partial void InvalidateRichTextBlockPartial()
		{
			// CRichTextBlock::InvalidateContent — invalidate the block layout engine's content rather
			// than dropping the node, so the view (and the selection built against it) stays alive.
			_pageNode?.InvalidateContent();

			InvalidateInlineAndRequireRepaint();
			// Master content/property changes invalidate the linked overflow chain so it re-measures
			// its slice from the (re)computed master break. Mirrors CRichTextBlock::InvalidateContentMeasure
			// driving CRichTextBlockOverflow::InvalidateAllOverflowContentMeasure down the chain.
			InvalidateOverflowChainContentMeasure();
		}
		partial void OnForegroundChangedPartial() => InvalidateInlineAndRequireRepaint();

		// Bumped when a brush in the content changes; the cached layouts take the new brushes when they next draw.
		internal int ForegroundVersion { get; private set; }

		// A run's brush changed: repaint, without laying the content out again.
		internal void InvalidateForeground()
		{
			ForegroundVersion++;
			InvalidateInlineAndRequireRepaint();
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			// The overflows lay out and mirror by this element's direction (an inherited Run.FlowDirection change
			// doesn't reach them when there is no Run), and the cached layouts depend on it.
			if (args.Property == FlowDirectionProperty)
			{
				InvalidateContent();
			}
		}

		// A font the layout fell back from has loaded: the cached paragraph layouts are stale.
		void UnicodeText.IFontCacheUpdateListener.Invalidate() => InvalidateContent();

		partial void OnSelectionChanged()
		{
			InvalidateInlineAndRequireRepaint();
			SelectedText = GetSelectedText(Math.Min(Selection.start, Selection.end), Math.Max(Selection.start, Selection.end));
			RaiseSelectionChanged();
		}

		// The flat Selection counts a LineBreak as one character (RichTextBlockView.GetCharacterIndex), while the
		// container text spells it, like every paragraph end, as CRLF.
		private string GetSelectedText(int start, int end)
		{
			var builder = new StringBuilder();
			var flatIndex = 0;

			void AppendText(string text, int flatLength)
			{
				if (flatLength == text.Length)
				{
					var from = Math.Max(start, flatIndex) - flatIndex;
					var to = Math.Min(end, flatIndex + flatLength) - flatIndex;
					if (from < to)
					{
						builder.Append(text, from, to - from);
					}
				}
				else if (start <= flatIndex && flatIndex < end)
				{
					builder.Append(text);
				}

				flatIndex += flatLength;
			}

			void AppendInline(Inline inline)
			{
				switch (inline)
				{
					case Run run:
						var text = run.Text ?? string.Empty;
						AppendText(text, text.Length);
						break;
					case LineBreak:
						AppendText("\r\n", 1);
						break;
					case Span span:
						foreach (var child in span.Inlines)
						{
							AppendInline(child);
						}
						break;
				}
			}

			foreach (var paragraph in Blocks.OfType<Paragraph>())
			{
				foreach (var inline in paragraph.Inlines)
				{
					AppendInline(inline);
				}

				AppendText("\r\n", 2);
			}

			return builder.ToString();
		}

		//------------------------------------------------------------------------
		//
		//  CRichTextBlock::UpdateIsTextTrimmed
		//
		//------------------------------------------------------------------------
		partial void UpdateIsTextTrimmed()
		{
			if (HasOverflowContent)
			{
				IsTextTrimmed = true;
				return;
			}

			bool isTrimmed = false;

			for (var blockChild = _pageNode?.GetFirstChild(); blockChild is not null; blockChild = blockChild.GetNext())
			{
				if (blockChild is ParagraphNode paragraphNode && paragraphNode.GetHasTrimmedLine())
				{
					isTrimmed = true;
					break;
				}
			}

			IsTextTrimmed = isTrimmed;
		}

		// The flat index of the text position at a point, through the text view.
		private int GetCharacterIndexAtPointCore(Point point)
		{
			if (_pTextView is not { } view || _pageNode is null || _pageNode.IsMeasureDirty() || _pageNode.IsArrangeDirty())
			{
				return -1;
			}

			var position = view.PixelPositionToTextPosition(point, false, out _);
			return view.GetCharacterIndex((int)position);
		}

		// CRichTextBlock::HitTestLink: the link that contains the text position at the point.
		private Hyperlink? HitTestLink(Point point)
			=> _pTextView is { } view && _pageNode is { } page
				? HitTestLink(view, page, Blocks.GetTextContainer(), point)
				: null;

		internal static Hyperlink? HitTestLink(Microsoft.UI.Xaml.Controls.Text.Core.RichTextBlockView view, PageNode page, Microsoft.UI.Xaml.Documents.RichTextServices.ITextContainer? container, Point point)
		{
			if (container is null || page.IsMeasureDirty() || page.IsArrangeDirty())
			{
				return null;
			}

			var position = view.PixelPositionToTextPosition(point, false, out var gravity);
			var textPosition = new Microsoft.UI.Xaml.Controls.Text.Core.PlainTextPosition(container, position, gravity);
			if (!textPosition.GetLogicalParent(out var parent))
			{
				return null;
			}

			for (var element = parent; element is TextElement; element = element.GetParent() as DependencyObject)
			{
				if (element is Hyperlink hyperlink)
				{
					return hyperlink;
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

			// CRichTextBlock::OnDoubleTapped: word selection belongs to the selection manager.
			if (_pSelectionManager is { } manager && _pTextView is { } view)
			{
				manager.OnDoubleTapped(this, e, view);
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
			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				// Clear() raises Reset without OldItems: only what is still in the collection stays tracked.
				foreach (var disposable in _textHighlighterDisposables.Values)
				{
					disposable.Dispose();
				}

				_textHighlighterDisposables.Clear();

				foreach (var highlighter in TextHighlighters)
				{
					TrackTextHighlighter(highlighter);
				}
			}
			else
			{
				if (e.OldItems is not null)
				{
					foreach (var item in e.OldItems)
					{
						// A moved or duplicated instance is still in the collection.
						if (item is TextHighlighter highlighter
							&& !TextHighlighters.Contains(highlighter)
							&& _textHighlighterDisposables.Remove(highlighter, out var disposable))
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
							TrackTextHighlighter(highlighter);
						}
					}
				}
			}

			InvalidateInlineAndRequireRepaint();
		}

		private void TrackTextHighlighter(TextHighlighter highlighter)
		{
			if (_textHighlighterDisposables.ContainsKey(highlighter))
			{
				return;
			}

			var composite = new CompositeDisposable();
			composite.Add(highlighter.RegisterDisposablePropertyChangedCallback(TextHighlighter.BackgroundProperty, (_, _) => InvalidateInlineAndRequireRepaint()));
			composite.Add(highlighter.RegisterDisposablePropertyChangedCallback(TextHighlighter.ForegroundProperty, (_, _) => InvalidateInlineAndRequireRepaint()));
			NotifyCollectionChangedEventHandler onCollectionChanged = (_, _) => InvalidateInlineAndRequireRepaint();
			((ObservableCollection<TextRange>)highlighter.Ranges).CollectionChanged += onCollectionChanged;
			composite.Add(Disposable.Create(() => ((ObservableCollection<TextRange>)highlighter.Ranges).CollectionChanged -= onCollectionChanged));
			_textHighlighterDisposables.Add(highlighter, composite);
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
