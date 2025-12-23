using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Composition;
using System.Numerics;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.UI.Dispatching;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Core.Scaling;

#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	partial class TextBlock : FrameworkElement, IBlock
	{
		// The caret thickness is actually always 1-pixel wide regardless of how big the text is
		internal const float CaretThickness = 1;

		private Action? _selectionHighlightColorChanged;
		private MenuFlyout? _contextMenu;
		private IDisposable? _selectionHighlightBrushChangedSubscription;
		private readonly Dictionary<ContextMenuItem, MenuFlyoutItem> _flyoutItems = new();
		private readonly VirtualKeyModifiers _platformCtrlKey = OperatingSystem.IsMacOS() ? VirtualKeyModifiers.Windows : VirtualKeyModifiers.Control;
		private Size _lastInlinesArrangeWithPadding;

		private protected override ContainerVisual CreateElementVisual() => new TextVisual(Compositor.GetSharedCompositor(), this);

		private bool _renderSelection;
		private (int index, CompositionBrush brush)? _caretPaint;

		internal IParsedText ParsedText { get; private set; } = Microsoft.UI.Xaml.Documents.ParsedText.Empty;

		internal event Action? DrawingFinished;

		public TextBlock()
		{
			UpdateLastUsedTheme();

			_hyperlinks.CollectionChanged += HyperlinksOnCollectionChanged;

			Tapped += static (s, e) => ((TextBlock)s).OnTapped(e);
			DoubleTapped += static (s, e) => ((TextBlock)s).OnDoubleTapped(e);
			RightTapped += static (s, e) => ((TextBlock)s).OnRightTapped(e);
			KeyDown += static (s, e) => ((TextBlock)s).OnKeyDown(e);

			GotFocus += (_, _) => UpdateSelectionRendering();
			LostFocus += (_, _) => UpdateSelectionRendering();
		}

		internal TextBox? OwningTextBox { private get; init; }

#if DEBUG
		private protected override void OnLoaded()
		{
			base.OnLoaded();
			Visual.Comment = $"{Visual.Comment}#text";
		}
#endif

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
				IsTextBoxDisplay && (this as IDependencyObjectStoreProvider).Store.GetCurrentHighestValuePrecedence(TextAlignmentProperty) is DependencyPropertyValuePrecedences.DefaultValue ? null : TextAlignment,
				TextWrapping,
				InvalidateInlineAndRequireRepaint,
				out var desiredSize);

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
		}

		private void UpdateSelectionRendering()
		{
			if (OwningTextBox is null) // TextBox manages RenderSelection itself
			{
				RenderSelection = IsTextSelectionEnabled && (IsFocused || (_contextMenu?.IsOpen ?? false));
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
			var selection = (Math.Min(Selection.start, Selection.end), Math.Max(Selection.start, Selection.end), SelectionHighlightColor.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor()), DefaultBrushes.SelectedTextForegroundColor);
			ParsedText.Draw(
				session,
				_caretPaint is { } c ? (c.index, c.brush, CaretThickness) : null,
				_renderSelection ? selection : null);
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

		// TODO: remove this context menu when TextCommandBarFlyout is implemented
		private void OnRightTapped(RightTappedRoutedEventArgs e)
		{
			if (IsTextSelectionEnabled)
			{
				e.Handled = true;

				Focus(FocusState.Pointer);

				if (_contextMenu is null)
				{
					_contextMenu = new MenuFlyout();

					_flyoutItems.Add(ContextMenuItem.Copy, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_COPY"), Command = new StandardUICommand(StandardUICommandKind.Copy) { Command = new TextBlockCommand(CopySelectionToClipboard) } });
					_flyoutItems.Add(ContextMenuItem.SelectAll, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_SELECT_ALL"), Command = new StandardUICommand(StandardUICommandKind.SelectAll) { Command = new TextBlockCommand(SelectAll) } });
				}

				_contextMenu.Items.Clear();

				if (Selection.start != Selection.end)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Copy]);
				}
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.SelectAll]);

				_contextMenu.ShowAt(this, e.GetPosition(this));
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
				new FrameworkPropertyMetadata(default(string)));

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

		private sealed class TextBlockCommand(Action action) : ICommand
		{
			public bool CanExecute(object? parameter) => true;

			public void Execute(object? parameter) => action();

#pragma warning disable 67 // An event was declared but never used in the class in which it was declared.
			public event EventHandler? CanExecuteChanged;
#pragma warning restore 67 // An event was declared but never used in the class in which it was declared.
		}
	}
}
