using Uno.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Documents;
using Android.Text;
using Android.Text.Style;
using Android.Widget;
using Android.Views;
using System.Collections.Specialized;
using Windows.UI.Xaml.Automation;
using Android.Graphics.Drawables;
using static Uno.UI.ViewHelper;
using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml.Markup;
using Uno;
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Windows.Foundation;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlock : FrameworkElement
	{
		#region Static

		private readonly static IEventProvider _frameworkElementTrace = Tracing.Get(FrameworkElement.TraceProvider.Id);
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private static readonly ILogger _log = typeof(TextBlock).Log();

		public new static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{7817A274-04FC-4CAA-B33F-066EECE9476A}");

			public const int TextBlock_MakeLayoutStart = 1;
			public const int TextBlock_MakeLayoutStop = 2;
		}

		private readonly static TextUtils.TruncateAt TruncateEnd = TextUtils.TruncateAt.End;
		private readonly static Java.Lang.String EmptyString = new Java.Lang.String();
		private static Java.Lang.Reflect.Constructor _maxLinedStaticLayout;
		private static Java.Lang.Object _textDirectionHeuristics;

		static TextBlock()
		{
			InitializeStaticLayoutInterop();
		}

		/// <summary>
		/// Finds a private constructor that allows for the specification of MaxLines. 
		/// </summary>
		/// <remarks>
		/// This code may fail in a newer version of android that
		/// does not provide this exact constructor, DO NOT REUSE AS-IS.
		/// </remarks>
		private static void InitializeStaticLayoutInterop()
		{
			if (_maxLinedStaticLayout == null)
			{
				var staticLayoutClass = Java.Lang.Class.ForName("android.text.StaticLayout");
				var ctors = staticLayoutClass.GetDeclaredConstructors();

				_maxLinedStaticLayout = ctors.Where(c => c.GetParameterTypes().Length == 13).First();
				_maxLinedStaticLayout.Accessible = true;
			}

			if (_textDirectionHeuristics == null)
			{
#if __ANDROID_18__
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
				{
					_textDirectionHeuristics = (Java.Lang.Object)TextDirectionHeuristics.FirststrongLtr;
				}
				else
#endif
				{
					// This is required because this class was not exposed until API 18 but available before.

					var textDirectionHeuristicsClass = Java.Lang.Class.ForName("android.text.TextDirectionHeuristics");
					var firstStrongField = textDirectionHeuristicsClass.GetField("FIRSTSTRONG_LTR");

					_textDirectionHeuristics = firstStrongField.Get(null);
				}
			}
		}

		#endregion

		/// <summary>
		/// The last build layout will be stored in this two fields.
		/// Both exists as most of the time, the measured layout size will be different
		/// from the imposed arrange size, particularly when a control is placed in a 
		/// StackPanel or a Grid.
		/// </summary>
		private LayoutBuilder _measureLayout, _arrangeLayout;

		private Java.Lang.ICharSequence _textFormatted;
		private TextPaint _paint;
		private TextUtils.TruncateAt _ellipsize;
		private Layout.Alignment _layoutAlignment;

		private void InitializePartial()
		{
			// This makes OnDraw to be called
			SetWillNotDraw(false);
		}

		protected sealed override string UIAutomationText => Text;

		#region Invalidate

		// Invalidate _textFormatted
		partial void OnInlinesChangedPartial() => _textFormatted = null;
		partial void OnTextChangedPartial()
		{
			_textFormatted = null;
		}

		// Invalidate _paint
		partial void OnFontWeightChangedPartial() => _paint = null;
		partial void OnFontStyleChangedPartial() => _paint = null;
		partial void OnFontFamilyChangedPartial() => _paint = null;
		partial void OnFontSizeChangedPartial() => _paint = null;
		partial void OnCharacterSpacingChangedPartial() => _paint = null;
		partial void OnForegroundChangedPartial() => _paint = null;

		// Invalidate _ellipsize
		partial void OnTextTrimmingChangedPartial() => _ellipsize = null;

		// Invalidate _layoutAlignment
		partial void OnTextAlignmentChangedPartial() => _layoutAlignment = null;

		#endregion

		#region Update

		private void Update()
		{
			UpdateLayoutAlignment();
			UpdateTextTrimming();
			UpdateTextFormatted();
			UpdatePaint();
		}

		private void UpdateLayoutAlignment()
		{
			if (_layoutAlignment != null)
			{
				return;
			}

			switch (TextAlignment)
			{
				case TextAlignment.Center:
					_layoutAlignment = Android.Text.Layout.Alignment.AlignCenter;
					break;

				case TextAlignment.Right:
					_layoutAlignment = Android.Text.Layout.Alignment.AlignOpposite;
					break;

				default:
				case TextAlignment.Left:
					_layoutAlignment = Android.Text.Layout.Alignment.AlignNormal;
					break;
			}
		}

		private void UpdateTextTrimming()
		{
			if (_ellipsize != null)
			{
				return;
			}

			switch (TextTrimming)
			{
				case TextTrimming.CharacterEllipsis:
				case TextTrimming.WordEllipsis: // For lack of true WordEllipsis support
					_ellipsize = TruncateEnd;
					break;
				case TextTrimming.Clip:
				case TextTrimming.None:
				default:
					_ellipsize = null;
					break;
			}
		}

		private void UpdatePaint()
		{
			if (_paint != null)
			{
				return;
			}

			_paint = TextPaintPool.GetPaint(
				FontWeight,
				FontStyle,
				FontFamily,
				FontSize,
				CharacterSpacing,
				(Foreground as SolidColorBrush)?.Color ?? Colors.Transparent,
				BaseLineAlignment.Baseline,
				TextDecorations
			);
		}

		private void UpdateTextFormatted()
		{
			if (_textFormatted != null)
			{
				return;
			}

			_textFormatted = GetTextFormatted();
		}

		private Java.Lang.ICharSequence GetTextFormatted()
		{
			if (Text == string.Empty)
			{
				return EmptyString;
			}
			else if (UseInlinesFastPath)
			{
				return new Java.Lang.String(Text);
			}
			else
			{
				var textFormatted = new UnoSpannableString(Text);
				foreach (var inline in GetEffectiveInlines())
				{
					textFormatted.SetPaintSpan(inline.inline.GetPaint(), inline.start, inline.end);
				}
				return textFormatted;
			}
		}

		#endregion

		#region Layout

		internal protected override void OnInvalidateMeasure()
		{
			base.OnInvalidateMeasure();

			// We want to invalidate both the layout and the rendering
			Invalidate();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			IDisposable measureActivity = null;

			if (_frameworkElementTrace.IsEnabled)
			{
				measureActivity = _frameworkElementTrace.WriteEventActivity(
					FrameworkElement.TraceProvider.FrameworkElement_MeasureStart,
					FrameworkElement.TraceProvider.FrameworkElement_MeasureStop,
					new object[] { GetType().ToString(), this.GetDependencyObjectId() }
				);
			}

			using (measureActivity)
			{
				Update();

				availableSize.Width -= Padding.Left + Padding.Right;
				availableSize.Height -= Padding.Top + Padding.Bottom;

				var measuredSize = UpdateLayout(ref _measureLayout, availableSize, false);

				measuredSize.Width += Padding.Left + Padding.Right;
				measuredSize.Height += Padding.Top + Padding.Bottom;

				return measuredSize;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			IDisposable arrangeActivity = null;

			if (_frameworkElementTrace.IsEnabled)
			{
				arrangeActivity = _frameworkElementTrace.WriteEventActivity(
					FrameworkElement.TraceProvider.FrameworkElement_ArrangeStart,
					FrameworkElement.TraceProvider.FrameworkElement_ArrangeStop,
					new object[] { GetType().ToString(), this.GetDependencyObjectId() }
				);
			}

			using (arrangeActivity)
			{
				finalSize.Width -= Padding.Left + Padding.Right;
				finalSize.Height -= Padding.Top + Padding.Bottom;

				var originalArrangeLayout = _arrangeLayout;

				// If the width is not the same, the wrapping/trimming may be different.
				var isSameWidth = _measureLayout.AvailableSize.Width == finalSize.Width;

				// If the requested height is the same
				var isSameHeight = _measureLayout.AvailableSize.Height == finalSize.Width;

				// If the measured height is exactly the same
				var isSameMeasuredHeight = _measureLayout.MeasuredSize.Height == finalSize.Height;

				// If the unbound requested height is below the arrange height. In this case, 
				// the rendered text height is below the arrange size, but since the text 
				// does not need the whole height to render completely, we can reuse the measured
				// layout as the arrangeLayout.
				var isSameUnboundHeight = _measureLayout.MeasuredSize.Height <= finalSize.Height && MaxLines == 0;

				//If the measure height is the arrange height.
				isSameHeight = isSameHeight || isSameMeasuredHeight || isSameUnboundHeight;

				if (isSameWidth && isSameHeight && _layoutAlignment == Android.Text.Layout.Alignment.AlignNormal)
				{
					// We can reuse the measure layout as the arrange layout, since it
					// renders as the same visible surface.
					_arrangeLayout = _measureLayout;
				}
				else
				{
					// The layout is different and needs to be rebuilt.
					UpdateLayout(ref _arrangeLayout, finalSize, exactWidth: true);
				}

				if (originalArrangeLayout != _arrangeLayout)
				{
					UpdateNativeTextBlockLayout();
				}

				finalSize.Width += Padding.Left + Padding.Right;
				finalSize.Height += Padding.Top + Padding.Bottom;

				return finalSize;
			}
		}

		private void UpdateNativeTextBlockLayout()
		{
			var padding = Padding;
			base.SetNativeTextBlockLayout(
				_arrangeLayout?.Layout,
				LogicalToPhysicalPixels(padding.Left),
				LogicalToPhysicalPixels(padding.Top)
			);
			Invalidate(); // This ensures that OnDraw() will be called, which is typically the case anyway after OnLayout() but not always (eg, if device is being unlocked).
		}

		/// <summary>
		/// Updates the specified layout using the current properties, and the specified new size.
		/// </summary>
		private Size UpdateLayout(ref LayoutBuilder layout, Size availableSize, bool exactWidth)
		{
			// Normally this is a no-op since it's called in Measure. We call it again to prevent a crash in the edge case where Text changes in between calling Measure and Arrange.
			Update();

			var newLayout = new LayoutBuilder(
				_textFormatted,
				_paint,
				_ellipsize,
				_layoutAlignment,
				TextWrapping,
				MaxLines,
				availableSize,
				exactWidth,
				(float)(LineHeight * ViewHelper.Scale),
				LineStackingStrategy,
				layout
			);
			if (!newLayout.Equals(layout))
			{
				layout = newLayout;

				using (
					_trace.WriteEventActivity(
						TraceProvider.TextBlock_MakeLayoutStart,
						TraceProvider.TextBlock_MakeLayoutStop
					)
				)
				{
					layout.Build();
				}
			}

			return layout.MeasuredSize;
		}

		/// <summary>
		/// A layout builder, used to perform change tracking and avoid creating the same layout multiple times.
		/// </summary>
		/// <remarks>
		/// This class is intentionally left here for easier 
		/// change tracking. This class can be moved later on with no other 
		/// changes.
		/// </remarks>
		private class LayoutBuilder : IEquatable<LayoutBuilder>
		{
			private readonly Java.Lang.ICharSequence _textFormatted;
			private readonly TextUtils.TruncateAt _ellipsize;
			private readonly Layout.Alignment _layoutAlignment;
			private readonly TextWrapping _textWrapping;
			private readonly int _maxLines;
			private readonly bool _exactWidth;
			private float _lineHeight;
			private LineStackingStrategy _lineStackingStrategy;
			private float _addedSpacing;
			private static BoringLayout.Metrics UnknownBoring = new BoringLayout.Metrics();
			private BoringLayout.Metrics _metrics = UnknownBoring;
			private TextPaint _paint;

			/// <summary>
			/// The size of this layout.
			/// </summary>
			public Size MeasuredSize { get; private set; }

			public Size AvailableSize { get; private set; }

			/// <summary>
			/// The layout to be drawn
			/// </summary>
			public Layout Layout { get; private set; }

			/// <summary>
			/// Builds a new layout with the specified parameters.
			/// </summary>
			public LayoutBuilder(
				Java.Lang.ICharSequence textFormatted,
				TextPaint paint,
				TextUtils.TruncateAt ellipsize,
				Layout.Alignment layoutAlignment,
				TextWrapping textWrapping,
				int maxLines,
				Size availableSize,
				bool exactWidth,
				float lineHeight,
				LineStackingStrategy lineStackingStrategy,
				LayoutBuilder existingBuilder
			)
			{
				_textFormatted = textFormatted;
				_paint = paint;
				_ellipsize = ellipsize;
				_layoutAlignment = layoutAlignment;
				_textWrapping = textWrapping;
				_maxLines = maxLines;
				AvailableSize = availableSize;
				_exactWidth = exactWidth;
				_lineHeight = lineHeight;
				_lineStackingStrategy = lineStackingStrategy;
				Layout = existingBuilder?.Layout;
				_metrics = existingBuilder?._metrics;
			}

			public bool Equals(LayoutBuilder other)
			{
				return ReferenceEquals(_textFormatted, other?._textFormatted)
					&& ReferenceEquals(_paint, other?._paint)
					&& ReferenceEquals(_ellipsize, other?._ellipsize)
					&& ReferenceEquals(_layoutAlignment, other?._layoutAlignment)
					&& _textWrapping == other?._textWrapping
					&& _maxLines == other?._maxLines
					&& AvailableSize == other?.AvailableSize
					&& _exactWidth == other?._exactWidth
					&& _lineHeight == other?._lineHeight
					&& _lineStackingStrategy == other?._lineStackingStrategy;
			}

			public void Build()
			{
				MeasuredSize = UpdateLayout(AvailableSize, _exactWidth);
			}

			/// <summary>
			/// Updates the current TextBlock layout to use the provided width and height.
			/// </summary>
			/// <param name="maxWidth">An optional width of the textblock</param>
			/// <param name="maxHeight">An optional height of the textblock</param>
			/// <param name="exactWidth">
			/// Specifies if the provided width must be used for the new layout, and 
			/// not "at most" of the widh.
			/// </param>
			/// <returns>The size of the new layout</returns>
			private Size UpdateLayout(Size availableSize, bool exactWidth = false)
			{
				var desiredWidth = int.MaxValue;

				var maxWidth = double.IsPositiveInfinity(availableSize.Width)
					? (int?)null
					: LogicalToPhysicalPixels(availableSize.Width);

				var maxHeight = double.IsPositiveInfinity(availableSize.Height)
					? (int?)null
					: LogicalToPhysicalPixels(availableSize.Height);

				if (maxWidth != null)
				{
					if (!exactWidth)
					{
						// The width of the textblock can't be larger that the maxWidth, otherwise
						// use the measured width of the text.
						desiredWidth = Math.Min(
							maxWidth.Value,
							(int)Math.Ceiling(Android.Text.Layout.GetDesiredWidth(_textFormatted, _paint))
						);
					}
					else
					{
						// The layout must be exactly this size. This case is present for the layout
						// created during the OnLayout, which states the size of the textblock explicitly.
						desiredWidth = maxWidth.Value;
					}

					MakeLayout(
						desiredWidth,
						maxLines: _maxLines != 0 ? _maxLines : int.MaxValue
					);
				}
				else
				{
					// No constraint, this is going to be a text on a single line.
					desiredWidth = (int)Math.Ceiling(Android.Text.Layout.GetDesiredWidth(_textFormatted, _paint));

					MakeLayout(desiredWidth);
				}

				int linecount = Layout.LineCount;
				var measuredHeight = Layout.GetLineTop(linecount);
				if (_lineHeight != 0 && _addedSpacing > 0)
				{
					// Unlike Windows, Android by default doesn't add spacing to final line. However Android seems to add (Top-Ascent) and 
					// (Bottom-Descent) as 'padding' above the top and below the bottom lines. As a 'safe' approach, we take the greater of the two values.
					var heightFromLineHeight = (int)(linecount * _lineHeight);
					measuredHeight = Math.Max(measuredHeight, heightFromLineHeight);
				}

				if (maxHeight != null && measuredHeight > maxHeight)
				{
					var lineAtHeight = Layout.GetLineForOffset(maxHeight.Value) + 1;
					measuredHeight = Layout.GetLineTop(lineAtHeight);

					// We don't want to display half of a line so we remove it, unless
					// there's only one line, which will be clipped.
					if (linecount > 1 && measuredHeight > maxHeight)
					{
						lineAtHeight = Math.Max(0, lineAtHeight - 1);
					}

					MakeLayout(
						desiredWidth,
						maxLines: lineAtHeight
					);
				}

				return new Size(PhysicalToLogicalPixels(Layout.Width), PhysicalToLogicalPixels(measuredHeight));
			}

			private float GetSpacingAdd(TextPaint paint)
			{
				if (_lineHeight == 0f)
				{
					return 0f;
				}
				
				// Use integer font metrics to match StaticLayout's usage and avoid pixel rounding errors
				var fmi = paint.GetFontMetricsInt();
				var baseLineHeight = fmi.Descent - fmi.Ascent;

				float spacingOffset = _lineHeight - baseLineHeight;
				if (_lineStackingStrategy == LineStackingStrategy.MaxHeight)
				{
					//If LineStackingStrategy is MaxHeight, don't apply negative spacing
					spacingOffset = Math.Max(0f, spacingOffset);
				}

				return spacingOffset;
			}

			private void MakeLayout(int width, int maxLines = int.MaxValue)
			{
				if (_textWrapping == TextWrapping.NoWrap)
				{
					maxLines = 1;
				}

				if (maxLines == 1)
				{
					_metrics = BoringLayout.IsBoring(_textFormatted, _paint, _metrics);

					if (_metrics != null)
					{
						if (Layout is BoringLayout boring)
						{
							Layout = boring.ReplaceOrMake(
								_textFormatted,
								_paint,
								width,
								_layoutAlignment,
								1,
								_addedSpacing = GetSpacingAdd(_paint),
								_metrics,
								true,
								_ellipsize,
								width
							);

							return;
						}
						else
						{
							Layout = new BoringLayout(
								_textFormatted,
								_paint,
								width,
								_layoutAlignment,
								1,
								_addedSpacing = GetSpacingAdd(_paint),
								_metrics,
								true,
								_ellipsize,
								width
							);

							return;
						}
					}
				}

				Layout = UnoStaticLayoutBuilder.Build(
						/*source:*/ _textFormatted,
						/*paint: */ _paint,
						/*outerwidth: */ width,
						/*align: */ _layoutAlignment,
						/*spacingmult:*/  1,
						/*spacingadd: */ _addedSpacing = GetSpacingAdd(_paint),
						/*includepad:*/  true,
						/*ellipsize: */ _ellipsize,
						/*ellipsizedWidth: */ width,
						/*maxLines: */ maxLines
					);
			}
		}

		#endregion

		#region Hyperlinks

		protected override bool NativeHitCheck()
		{
			return true;
		}

		private int GetCharacterIndexAtPoint(Point point)
		{
			point.X -= Padding.Left;
			point.Y -= Padding.Top;

			var physicalPoint = point.LogicalToPhysicalPixels();

			var layout = _arrangeLayout.Layout;
			var rect = new Android.Graphics.Rect(0, 0, layout.Width, layout.Height);
			if (rect.Contains((int)physicalPoint.X, (int)physicalPoint.Y))
			{
				int line = layout.GetLineForVertical((int)physicalPoint.Y);
				int offset = layout.GetOffsetForHorizontal(line, (int)physicalPoint.X);

				return offset;
			}

			return -1;
		}

		#endregion
	}
}
