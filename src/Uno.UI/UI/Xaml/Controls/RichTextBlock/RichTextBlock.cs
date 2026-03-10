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
using Microsoft.UI.Xaml.Automation.Peers;
using Uno;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	// MUX Reference RichTextBlock_Partial.cpp, tag winui3/release/1.4.2

	[ContentProperty(Name = nameof(Blocks))]
	public partial class RichTextBlock : FrameworkElement, IThemeChangeAware
	{
		private IDisposable _foregroundBrushChangedSubscription;

#if !__WASM__
		private bool _isPressed;
		private Range _selectionOnPointerPressed;
#endif

		private Hyperlink _hyperlinkOver;
		private bool _subscribeToPointerEvents;
		private Action _foregroundChanged;
		private Range _selection;

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
		partial void OnPointerReleasedForSelectionFlyout(PointerRoutedEventArgs e);

		public RichTextBlock()
		{
			IFrameworkElementHelper.Initialize(this);
			UpdateLastUsedTheme();

			Blocks = new BlockCollection();
			Blocks.SetParent(this);
			Blocks.VectorChanged += OnBlocksChanged;

			_hyperlinks.CollectionChanged += HyperlinksOnCollectionChanged;

			InitializePartial();
		}

		partial void InitializePartial();

		/// <summary>
		/// Gets the contents of the RichTextBlock.
		/// </summary>
		public BlockCollection Blocks { get; }

		internal override bool CanHaveChildren() => true;

		public new bool Focus(FocusState value) => base.Focus(value);

		internal override bool IsFocusable =>
			IsVisible() &&
			(IsTextSelectionEnabled || IsTabStop || FocusProperties.GetCaretBrowsingModeEnable()) &&
			AreAllAncestorsVisible();

		private void OnBlocksChanged(global::Windows.Foundation.Collections.IObservableVector<Block> sender, global::Windows.Foundation.Collections.IVectorChangedEventArgs e)
		{
			InvalidateBlockContent();
		}

		internal void InvalidateBlockContent()
		{
			UpdateHyperlinks();
			InvalidateRichTextBlock();
		}

		private void InvalidateRichTextBlock()
		{
			InvalidateRichTextBlockPartial();
			InvalidateMeasure();
		}

		partial void InvalidateRichTextBlockPartial();

		/// <summary>
		/// Gets the full plain text content of the RichTextBlock by concatenating all paragraph inlines.
		/// </summary>
		internal new string GetPlainText()
		{
			if (Blocks.Count == 0)
			{
				return string.Empty;
			}

			var parts = new List<string>();
			foreach (var block in Blocks)
			{
				if (block is Paragraph paragraph)
				{
					parts.Add(string.Concat(paragraph.Inlines.Select(InlineExtensions.GetText)));
				}
			}

			return string.Join("\r\n", parts);
		}

		#region Pointer events

#if !__WASM__
		private static bool SupportsSelection(PointerRoutedEventArgs args)
			=> args.Pointer.PointerDeviceType is PointerDeviceType.Mouse;
#endif

		private static readonly RightTappedEventHandler OnRightTapped = (object sender, RightTappedRoutedEventArgs e) =>
		{
			if (sender is not RichTextBlock that || !that.IsTextSelectionEnabled)
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
			if (sender is not RichTextBlock that)
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
				that.CompleteGesture();
			}
#if !__WASM__
			else if (that.IsTextSelectionEnabled && SupportsSelection(e))
			{
				var point = e.GetCurrentPoint(that);
#if __SKIA__
				var index = that.GetCharacterIndexAtPoint(point.Position, true);
#else
				var index = that.GetCharacterIndexAtPoint(point.Position);
#endif
				that._selectionOnPointerPressed = that.Selection;
				if (index >= 0)
				{
					that.Selection = new Range(index, index);
				}

				e.Handled = true;
#if __SKIA__
				if (!Internal.TextControlFlyoutHelper.IsOpen(that.ContextFlyout))
#endif
				{
					that.Focus(FocusState.Pointer);
				}

				that.CapturePointer(e.Pointer);
			}
#endif
		};

		private static readonly PointerEventHandler OnPointerReleased = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is not RichTextBlock that)
			{
				return;
			}

#if !__WASM__
			if (that._isPressed && that.IsTextSelectionEnabled && that.FindHyperlinkAt(e) is { })
			{
				that.Selection = new Range(0, 0);
			}

			that._isPressed = false;
#endif

			if (that.IsCaptured(e.Pointer))
			{
				var hyperlink = that.FindHyperlinkAt(e);
				if (hyperlink is { })
				{
					that.CompleteGesture();
				}

				that.ReleasePointerCapture(e.Pointer.UniqueId, muteEvent: true);

				if (!(hyperlink?.ReleasePointerPressed(e.Pointer) ?? false))
				{
					that.AbortHyperlinkCaptures(e.Pointer);
				}
			}

			that.OnPointerReleasedForSelectionFlyout(e);
#if !__WASM__
			e.Handled |= that.IsTextSelectionEnabled;
#endif
		};

		private static readonly PointerEventHandler OnPointerCaptureLost = (object sender, PointerRoutedEventArgs e) =>
		{
			if (sender is RichTextBlock that)
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
			if (sender is not RichTextBlock that)
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
#if __SKIA__
				var index = that.GetCharacterIndexAtPoint(point.Position, true);
#else
				var index = that.GetCharacterIndexAtPoint(point.Position);
#endif
				if (index >= 0)
				{
					that.Selection = that.Selection with { end = index };
				}
			}
#endif
		};

		private static readonly PointerEventHandler OnPointerEntered = (sender, e) =>
		{
			if (sender is not RichTextBlock { HasHyperlink: true } that)
			{
				return;
			}

			var hyperlink = that.FindHyperlinkAt(e);
			that._hyperlinkOver = hyperlink;
			hyperlink?.SetPointerOver(e.Pointer);
		};

		private static readonly PointerEventHandler OnPointerExit = (sender, e) =>
		{
			if (sender is not RichTextBlock { HasHyperlink: true } that)
			{
				return;
			}

			that._hyperlinkOver?.ReleasePointerOver(e.Pointer);
			that._hyperlinkOver = null;
		};

		private bool AbortHyperlinkCaptures(Pointer pointer)
		{
			var aborted = false;
			foreach (var hyperlink in _hyperlinks.ToList())
			{
				aborted |= hyperlink.AbortPointerPressed(pointer);
				aborted |= hyperlink.ReleasePointerOver(pointer);
			}

			aborted |= _hyperlinkOver?.ReleasePointerOver(pointer) ?? false;
			_hyperlinkOver = null;

			return aborted;
		}

		private readonly ObservableCollection<Hyperlink> _hyperlinks = new();

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
			_hyperlinkOver = null;
			var previousHyperLinks = _hyperlinks.ToHashSet();
			_hyperlinks.Clear();

			foreach (var block in Blocks)
			{
				if (block is Paragraph paragraph)
				{
					foreach (var hyperlink in paragraph.Inlines.TraversedTree.preorderTree.OfType<Hyperlink>())
					{
						_hyperlinks.Add(hyperlink);
						previousHyperLinks.Remove(hyperlink);
					}
				}
			}

			foreach (var removed in previousHyperLinks)
			{
				removed.AbortAllPointerState();
			}
		}

		private bool HasHyperlink => _hyperlinks.Count > 0;

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
			return FindHyperlinkAtSkia(e);
#else
			return null;
#endif
		}

		#endregion

		protected override AutomationPeer OnCreateAutomationPeer() => new RichTextBlockAutomationPeer(this);

		public override string GetAccessibilityInnerText() => GetPlainText();

		private protected override double GetActualWidth() => DesiredSize.Width;
		private protected override double GetActualHeight() => DesiredSize.Height;

		internal override void UpdateThemeBindings(Data.ResourceUpdateReason updateReason)
		{
			base.UpdateThemeBindings(updateReason);
			UpdateLastUsedTheme();

			foreach (var block in Blocks)
			{
				if (block is Paragraph paragraph)
				{
					foreach (var inline in paragraph.Inlines)
					{
						((IDependencyObjectStoreProvider)inline).Store.UpdateResourceBindings(updateReason, resourceContextProvider: this);
					}
				}
			}
		}

		void IThemeChangeAware.OnThemeChanged() => OnForegroundChanged();

		/// <summary>
		/// Gets the character index at the given point, searching across all paragraphs.
		/// </summary>
		private int GetCharacterIndexAtPoint(Point point, bool extended = false)
		{
#if __SKIA__
			return GetCharacterIndexAtPointSkia(point, extended);
#else
			return -1;
#endif
		}

		/// <summary>
		/// Copies the current selection to the clipboard.
		/// </summary>
		public void CopySelectionToClipboard()
		{
			if (Selection.start != Selection.end)
			{
				var dataPackage = new global::Windows.ApplicationModel.DataTransfer.DataPackage();
				dataPackage.SetText(SelectedText);
				global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
			}
		}

		/// <summary>
		/// Selects all the content of the RichTextBlock.
		/// </summary>
		public void SelectAll() => Selection = new Range(0, GetPlainText().Length);

		internal record struct Range(int start, int end);
	}
}
