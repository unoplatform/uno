using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents;
using SkiaSharp;
using Microsoft.UI.Composition;
using System.Numerics;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Microsoft.UI.Xaml.Media;
using Uno.UI;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Media;

#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	partial class TextBlock : FrameworkElement, IBlock
	{
		private readonly TextVisual _textVisual;
		private Action? _selectionHighlightColorChanged;
		private MenuFlyout? _contextMenu;
		private readonly Dictionary<ContextMenuItem, MenuFlyoutItem> _flyoutItems = new();

		public TextBlock()
		{
			SetDefaultForeground(ForegroundProperty);
			_textVisual = new TextVisual(Visual.Compositor, this);

			Visual.Children.InsertAtBottom(_textVisual);

			_hyperlinks.CollectionChanged += HyperlinksOnCollectionChanged;

			DoubleTapped += (s, e) => ((TextBlock)s).OnDoubleTapped(e);
			RightTapped += (s, e) => ((TextBlock)s).OnRightTapped(e);
			KeyDown += (s, e) => ((TextBlock)s).OnKeyDown(e);

			GotFocus += (_, _) => UpdateSelectionRendering();
			LostFocus += (_, _) => UpdateSelectionRendering();
		}

#if DEBUG
		private protected override void OnLoaded()
		{
			base.OnLoaded();
			_textVisual.Comment = $"{Visual.Comment}#text";
		}
#endif

		protected override Size MeasureOverride(Size availableSize)
		{
			var padding = Padding;
			var availableSizeWithoutPadding = availableSize.Subtract(padding).AtLeastZero();
			var defaultLineHeight = GetComputedLineHeight();
			var desiredSize = Inlines.Measure(availableSizeWithoutPadding, defaultLineHeight);

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

		partial void OnIsTextSelectionEnabledChangedPartial()
		{
			if (_inlines is { })
			{
				_inlines.FireDrawingEventsOnEveryRedraw = IsTextSelectionEnabled;
			}

			RecalculateSubscribeToPointerEvents();
			UpdateSelectionRendering();
		}

		private void UpdateSelectionRendering()
		{
			if (_inlines is { })
			{
				_inlines.RenderSelection = IsTextSelectionEnabled && (IsFocused || (_contextMenu?.IsOpen ?? false));
			}
		}

		private void ApplyFlowDirection(float width)
		{
			if (this.FlowDirection == FlowDirection.RightToLeft)
			{
				_textVisual.TransformMatrix = new Matrix4x4(new Matrix3x2(-1.0f, 0.0f, 0.0f, 1.0f, width, 0.0f));
			}
			else
			{
				_textVisual.TransformMatrix = Matrix4x4.Identity;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var padding = Padding;
			var availableSizeWithoutPadding = finalSize.Subtract(padding);
			var arrangedSizeWithoutPadding = Inlines.Arrange(availableSizeWithoutPadding);
			_textVisual.Size = new Vector2((float)arrangedSizeWithoutPadding.Width, (float)arrangedSizeWithoutPadding.Height);
			_textVisual.Offset = new Vector3((float)padding.Left, (float)padding.Top, 0);
			ApplyFlowDirection((float)finalSize.Width);

			var result = base.ArrangeOverride(finalSize);
			UpdateIsTextTrimmed();

			return result;
		}

		/// <summary>
		/// Gets the line height of the TextBlock either
		/// based on the LineHeight property or the default
		/// font line height.
		/// </summary>
		/// <returns>Computed line height</returns>
		internal float GetComputedLineHeight()
		{
			var lineHeight = LineHeight;
			if (!lineHeight.IsNaN() && lineHeight > 0)
			{
				return (float)lineHeight;
			}
			else
			{
				var font = FontDetailsCache.GetFont(FontFamily?.Source, (float)FontSize, FontWeight, FontStyle);
				return font.LineHeight;
			}
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			if (args.Property == FlowDirectionProperty)
			{
				ApplyFlowDirection((float)this.RenderSize.Width);
			}
		}

		private int GetCharacterIndexAtPoint(Point point, bool extended = false) => Inlines.GetIndexAt(point, false, extended);

		partial void OnInlinesChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		// Invalidate Inlines measure when any IBlock properties used during measuring change:

		partial void OnMaxLinesChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		partial void OnTextWrappingChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		partial void OnLineHeightChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		partial void OnLineStackingStrategyChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush)
		{
			Inlines.InvalidateMeasure();
		}

		void IBlock.Invalidate(bool updateText) => InvalidateInlines(updateText);

		partial void OnSelectionChanged()
			=> Inlines.Selection = (Math.Min(Selection.start, Selection.end), Math.Max(Selection.start, Selection.end));

		partial void SetupInlines()
		{
			_inlines.SelectionFound += t =>
			{
				var canvas = t.canvas;
				var rect = t.rect;
				canvas.DrawRect(new SKRect((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom), new SKPaint
				{
					Color = SelectionHighlightColor.Color.ToSKColor(),
					Style = SKPaintStyle.Fill
				});
			};

			_inlines.FireDrawingEventsOnEveryRedraw = IsTextSelectionEnabled;
			_inlines.RenderSelection = IsTextSelectionEnabled;
		}

		private void OnKeyDown(KeyRoutedEventArgs args)
		{
			switch (args.Key)
			{
				case VirtualKey.C when args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Control):
					CopySelectionToClipboard();
					args.Handled = true;
					break;
				case VirtualKey.A when args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Control):
					SelectAll();
					args.Handled = true;
					break;
			}
		}

		private void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			if (IsTextSelectionEnabled)
			{
				var nullableSpan = Inlines.GetRenderSegmentSpanAt(e.GetPosition(this), false);
				if (nullableSpan.HasValue)
				{
					Selection = new Range(Inlines.GetStartAndEndIndicesForSpan(nullableSpan.Value.span, false));
				}
			}
		}

		// TODO: remove this context menu when TextCommandBarFlyout is implemented
		private void OnRightTapped(RightTappedRoutedEventArgs e)
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

		public void CopySelectionToClipboard()
		{
			if (Selection.start != Selection.end)
			{
				var start = Math.Min(Selection.start, Selection.end);
				var end = Math.Max(Selection.start, Selection.end);
				var text = Text[start..end];
				var dataPackage = new DataPackage();
				dataPackage.SetText(text);
				Clipboard.SetContent(dataPackage);
			}
		}

		public void SelectAll() => Selection = new Range(0, Text.Length);

		// TODO: move to TextBlock.cs when we implement SelectionHighlightColor for the other platforms
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
			Brush.SetupBrushChanged(oldBrush, newBrush, ref _selectionHighlightColorChanged, () => OnSelectionHighlightColorChangedPartial(newBrush));
		}

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush);

		partial void UpdateIsTextTrimmed()
		{
			IsTextTrimmed = IsTextTrimmable && (
				(_textVisual.Size.X + Padding.Left + Padding.Right) > ActualWidth ||
				(_textVisual.Size.Y + Padding.Top + Padding.Bottom) > ActualHeight
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
