using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Uno.UI.Samples.Helper;

#if !WINAPPSDK && !__ANDROID__ && !__IOS__ && !UNO_REFERENCE_API && !__MACOS__
using System.Windows;
using System.Windows.Controls;
#else
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.Samples.Controls
{
	public partial class StarStackPanel : Panel
	{
		public StarStackPanel()
		{

		}

		private double HorizontalTrim => Padding.Left + Padding.Right;
		private double VerticalTrim => Padding.Top + Padding.Bottom;
		private Point ChildrenOffset => new Point(Padding.Left, Padding.Top);

		protected override Size MeasureOverride(Size availableSize)
		{
			availableSize.Width -= HorizontalTrim;
			availableSize.Height -= VerticalTrim;

			var orientation = Orientation;
			var totalSize = new Size();
			double starTotal = 0;

			var children = Children
#if !WINAPPSDK && !__ANDROID__ && !__IOS__ && !__MACOS__ // Useless operator (==overhead on UI thread) for Jupiter platform
				.Cast<UIElement>()
#endif
				.OrderBy(GetPriority)
				.ToArray();

			// Calculate the interelement spacing if used (and more than one child)
			var totalInterElementSpacing = (double)Math.Max(InterElementSpacing * (children.Length - 1), 0);
			if (totalInterElementSpacing > 0)
			{
				if (Orientation == Orientation.Vertical)
				{
					var remainingHeight = Math.Max(availableSize.Height - totalInterElementSpacing, 0);
					availableSize = new Size(availableSize.Width, remainingHeight);
				}
				else
				{
					var remainingWidth = Math.Max(availableSize.Width - totalInterElementSpacing, 0);
					availableSize = new Size(remainingWidth, availableSize.Height);
				}
			}

			foreach (var child in children)
			{
				MeasureChild(child, availableSize, orientation, ref totalSize, ref starTotal);
			}

			// Compute leftover size for star children
			if (orientation == Orientation.Vertical)
			{
				availableSize.Height = Math.Max(availableSize.Height - totalSize.Height, 0);
			}
			else
			{
				availableSize.Width = Math.Max(availableSize.Width - totalSize.Width, 0);
			}

			MeasureStarChildren(availableSize, orientation, ref totalSize, starTotal, children);

			// re-introduce the interelement spacing
			if (orientation == Orientation.Vertical)
			{
				totalSize.Height += totalInterElementSpacing;
			}
			else
			{
				totalSize.Width += totalInterElementSpacing;
			}

			totalSize.Width += HorizontalTrim;
			totalSize.Height += VerticalTrim;

			return totalSize;
		}

		private void MeasureChild(UIElement child, Size availableSize, Orientation orientation, ref Size totalSize, ref double starTotal)
		{
			var sizeHint = GetChildSizeHint(child);

			if (GridLengthHelper2.GetIsStar(sizeHint))
			{
				starTotal += sizeHint.Value;
			}
			else if (GridLengthHelper2.GetIsAuto(sizeHint))
			{
				MesureChildAuto(child, orientation, availableSize, ref totalSize);
			}
			else if (GridLengthHelper2.GetIsAbsolute(sizeHint))
			{
				MesureChildAbsolute(child, orientation, availableSize, sizeHint, ref totalSize);
			}
		}

		private void MesureChildAbsolute(UIElement child, Orientation orientation, Size availableSize, GridLength sizeHint, ref Size totalSize)
		{
			if (orientation == Orientation.Vertical)
			{
				availableSize.Height = Math.Max(sizeHint.Value, 0);
			}
			else
			{
				availableSize.Width = Math.Max(sizeHint.Value, 0);
			}

#if __ANDROID__ || __IOS__ || __MACOS__
			var desiredSize = MeasureElement(child, availableSize);
#else
			child.Measure(availableSize);
			var desiredSize = child.DesiredSize;
#endif

			if (orientation == Orientation.Vertical)
			{
				desiredSize.Height = availableSize.Height;
			}
			else
			{
				desiredSize.Width = availableSize.Width;
			}

			AddToTotalSize(orientation, desiredSize, ref totalSize);
		}

		private void MesureChildAuto(UIElement child, Orientation orientation, Size availableSize, ref Size totalSize)
		{
			if (orientation == Orientation.Vertical)
			{
				availableSize.Height = Math.Max(availableSize.Height - totalSize.Height, 0);
			}
			else
			{
				availableSize.Width = Math.Max(availableSize.Width - totalSize.Width, 0);
			}

#if __ANDROID__ || __IOS__ || __MACOS__
			var desiredSize = MeasureElement(child, availableSize);
#else
			child.Measure(availableSize);
			var desiredSize = child.DesiredSize;
#endif

			AddToTotalSize(orientation, desiredSize, ref totalSize);
		}

		private void MeasureStarChildren(Size availableSize, Orientation orientation, ref Size totalSize, double starTotal, UIElement[] children)
		{
			if (starTotal != 0)
			{
				foreach (UIElement child in children)
				{
					var sizeHint = GetChildSizeHint(child);
					if (GridLengthHelper2.GetIsStar(sizeHint))
					{
						MesureChildStar(child, orientation, availableSize, sizeHint, starTotal, ref totalSize);
					}
				}
			}
		}

		private void MesureChildStar(UIElement child, Orientation orientation, Size availableSize, GridLength sizeHint, double starTotal, ref Size totalSize)
		{
			var portion = sizeHint.Value / starTotal;

			if (orientation == Orientation.Vertical)
			{
				availableSize.Height *= portion;
			}
			else
			{
				availableSize.Width *= portion;
			}

#if __ANDROID__ || __IOS__ || __MACOS__
			var desiredSize = MeasureElement(child, availableSize);
#else
			child.Measure(availableSize);
			var desiredSize = child.DesiredSize;
#endif

			AddToTotalSize(orientation, desiredSize, ref totalSize);
		}

		private static void AddToTotalSize(Orientation orientation, Size desiredSize, ref Size totalSize)
		{
			if (orientation == Orientation.Vertical)
			{
				totalSize.Height += desiredSize.Height;
				totalSize.Width = Math.Max(desiredSize.Width, totalSize.Width);
			}
			else
			{
				totalSize.Height = Math.Max(desiredSize.Height, totalSize.Height);
				totalSize.Width += desiredSize.Width;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			finalSize.Width -= HorizontalTrim;
			finalSize.Height -= VerticalTrim;

			var children = Children
#if !WINAPPSDK // Useless operator (==overhead on UI thread) for Jupiter platform
				.Cast<UIElement>()
#endif
				.ToArray(); // Materialize the list (prevent interop on Jupiter Platform)

			var orientation = Orientation;
			double totalLength = 0;
			double starTotal = 0;

			// Calculate the interelement spacing if used (and more than one child)
			var spacing = InterElementSpacing;
			var spacingCount = children.Length - 1;

			if (spacing > 0 && spacingCount > 0) // If spacing calculation required...
			{
				totalLength = InterElementSpacing * spacingCount; // Allocating spacings on used length

				// Protection against overflow
				totalLength =
					Orientation == Orientation.Horizontal
						? Math.Min(totalLength, finalSize.Width)
						: Math.Min(totalLength, finalSize.Height);

				// Recalculate real spacing after overflow protection
				spacing = totalLength / spacingCount;
			}

			var arrangeRecords = ComputeKnownSizesForArrange(children, orientation, ref finalSize, ref totalLength, ref starTotal);

			if (orientation == Orientation.Vertical)
			{
				totalLength = finalSize.Height - totalLength;
			}
			else
			{
				totalLength = finalSize.Width - totalLength;
			}

			var starRatio = (starTotal > 0) ?
				totalLength / starTotal :
				0;

			var arrangeSize = ArrangeChildren(arrangeRecords, orientation, finalSize, starRatio, spacing);

			arrangeSize.Width += HorizontalTrim;
			arrangeSize.Height += VerticalTrim;

			return arrangeSize;
		}

		private Record[] ComputeKnownSizesForArrange(UIElement[] children, Orientation orientation, ref Size finalSize, ref double totalLength, ref double starTotal)
		{
			var array = new Record[children.Length];

			for (var i = 0; i < array.Length; i++)
			{
				var child = children[i];

				array[i] = ComputeChildSize(child, orientation, ref finalSize, ref totalLength, ref starTotal);
			}

			return array;
		}

		private Record ComputeChildSize(UIElement child, Orientation orientation, ref Size finalSize, ref double totalLength, ref double starTotal)
		{
			var sizeHint = GetChildSizeHint(child);
			var size = new Size();

			if (GridLengthHelper2.GetIsStar(sizeHint))
			{
				starTotal += sizeHint.Value;
			}
			else if (GridLengthHelper2.GetIsAuto(sizeHint))
			{
				ComputeChildAutoSize(child, orientation, ref finalSize, ref totalLength, ref size);
			}
			else if (GridLengthHelper2.GetIsAbsolute(sizeHint))
			{
				ComputeChildAbsoluteSize(orientation, ref finalSize, ref totalLength, ref sizeHint, ref size);
			}

			return new Record
			{
				Child = child,
				Size = size,
				SizeHint = sizeHint,
			};
		}

		private void ComputeChildAutoSize(UIElement child, Orientation orientation, ref Size finalSize, ref double totalLength, ref Size size)
		{
#if __ANDROID__ || __IOS__ || __MACOS__
			var desiredSize = GetElementDesiredSize(child);
#else
			var desiredSize = child.DesiredSize;
#endif

			if (orientation == Orientation.Vertical)
			{
				size.Width = finalSize.Width;
				totalLength += size.Height = Math.Min(desiredSize.Height, Math.Max(finalSize.Height - totalLength, 0));
			}
			else
			{
				totalLength += size.Width = Math.Min(desiredSize.Width, Math.Max(finalSize.Width - totalLength, 0));
				size.Height = finalSize.Height;
			}
		}

		private static void ComputeChildAbsoluteSize(Orientation orientation, ref Size finalSize, ref double totalLength, ref GridLength sizeHint, ref Size size)
		{
			if (orientation == Orientation.Vertical)
			{
				size.Width = finalSize.Width;
				totalLength += size.Height = sizeHint.Value;
			}
			else
			{
				totalLength += size.Width = sizeHint.Value;
				size.Height = finalSize.Height;
			}
		}

		private Size ArrangeChildren(Record[] arrangeRecords, Orientation orientation, Size finalSize, double starRatio, double interElementSpacing)
		{
			double totalLength = 0;

			var nbRecords = arrangeRecords.Length;

			for (int i = 0; i < nbRecords; i++)
			{
				ArrangeChild(arrangeRecords[i], orientation, ref finalSize, starRatio, ref totalLength);

				if (i + 1 < nbRecords)
				{
					totalLength += interElementSpacing;
				}
			}

			if (orientation == Orientation.Vertical)
			{
				finalSize.Height = totalLength;
			}
			else
			{
				finalSize.Width = totalLength;
			}

			return finalSize;
		}

		private void ArrangeChild(Record record, Orientation orientation, ref Size finalSize, double starRatio, ref double totalLength)
		{
			var size = ComputeFinalChildSize(record, orientation, ref finalSize, starRatio);

			var rect = new Rect(ChildrenOffset, size);

			if (orientation == Orientation.Vertical)
			{
				rect.Y += totalLength;
				totalLength += size.Height;
			}
			else
			{
				rect.X += totalLength;
				totalLength += size.Width;
			}

#if __ANDROID__ || __IOS__ || __MACOS__
			ArrangeElement(record.Child, rect);
#else
			record.Child.Arrange(rect);
#endif
		}

		private static Size ComputeFinalChildSize(Record record, Orientation orientation, ref Size finalSize, double starRatio)
		{
			var size = record.Size;

			if (GridLengthHelper2.GetIsStar(record.SizeHint))
			{
				var portion = record.SizeHint.Value * starRatio;

				if (orientation == Orientation.Vertical)
				{
					size.Width = finalSize.Width;
					size.Height = portion;
				}
				else
				{
					size.Width = portion;
					size.Height = finalSize.Height;
				}
			}

			return size;
		}

		private static void InvalidateLayoutOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not StarStackPanel panel)
			{
				return;
			}

			panel.InvalidateMeasure();

#if !__ANDROID__ && !__IOS__ && !__MACOS__
			panel.InvalidateArrange();
#endif
		}

		private GridLength GetChildSizeHint(UIElement child)
		{
			if (child == null)
			{
				return default(GridLength);
			}

			var sizes = _sizes;

			if (sizes != null && sizes.Length > 0)
			{
				var index = Children.IndexOf(child);
				if (index >= 0 && sizes.Length > index)
				{
					return sizes[index];
				}
			}

			return GetSize(child);
		}

		#region Orientation DependencyProperty
		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		public static DependencyProperty OrientationProperty { get; } = DependencyProperty.Register
		(
			"Orientation",
			typeof(Orientation),
			typeof(StarStackPanel),
			new PropertyMetadata(Orientation.Vertical, InvalidateLayoutOnChanged)
		);
		#endregion

		#region Priority AttachedProperty
		public static int GetPriority(DependencyObject obj)
		{
			return (int)obj.GetValue(PriorityProperty);
		}

		public static void SetPriority(DependencyObject obj, int value)
		{
			obj.SetValue(PriorityProperty, value);
		}

		public static DependencyProperty PriorityProperty { get; } = DependencyProperty.RegisterAttached
		(
			"Priority",
			typeof(int),
			typeof(StarStackPanel),
			new PropertyMetadata(0)
		);
		#endregion

		#region Sizes DependencyProperty
		public static DependencyProperty SizesProperty { get; } = DependencyProperty.Register(
			"Sizes", typeof(string), typeof(StarStackPanel), new PropertyMetadata(null, HandleSizesChanged));

		public string Sizes
		{
			get { return (string)GetValue(SizesProperty); }
			set { SetValue(SizesProperty, value); }
		}

		private static void HandleSizesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var pnl = d as StarStackPanel;
			if (pnl == null)
			{
				return;
			}

			InvalidateLayoutOnChanged(d, e);

			var sizes = e.NewValue as string;
			if (string.IsNullOrWhiteSpace(sizes))
			{
				pnl._sizes = null;
			}
			else
			{
				pnl._sizes = ParseGridLength(sizes);
			}
		}

		private static readonly Regex GridLengthParsingRegex =
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
			new Regex(
				@"^(?:(?<stars>\d*(?:.\d*))\*)|(?<abs>\d+(?:.\d*))|(?<auto>Auto)|(?<star>\*)$",
#if WINAPPSDK
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
#else
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
#endif

		private static GridLength[] ParseGridLength(string s)
		{
			var parts = s.Split(new[] { ',' });

			var result = new List<GridLength>(parts.Length);

			foreach (var part in parts)
			{
				if (string.IsNullOrEmpty(part))
				{
					result.Add(GridLengthHelper2.FromValueAndType(0, GridUnitType.Auto));
					continue;
				}

				var match = GridLengthParsingRegex.Match(part);
				if (!match.Success)
				{
					throw new InvalidOperationException("Invalid value '" + part + "', unable to parse.");
				}

				var autoGroup = match.Groups["auto"];
				if (autoGroup.Success)
				{
					result.Add(GridLengthHelper2.FromValueAndType(0, GridUnitType.Auto));
					continue;
				}

				var starsGroup = match.Groups["stars"];
				if (starsGroup.Success)
				{
					var value =
						!string.IsNullOrWhiteSpace(starsGroup.Value)
							? double.Parse(starsGroup.Value, CultureInfo.InvariantCulture)
							: 1;
					result.Add(GridLengthHelper2.FromValueAndType(value, GridUnitType.Star));
					continue;
				}

				var starGroup = match.Groups["star"];
				if (starGroup.Success)
				{
					result.Add(GridLengthHelper2.FromValueAndType(1, GridUnitType.Star));
					continue;
				}

				var absGroup = match.Groups["abs"];
				if (absGroup.Success)
				{
					var value = double.Parse(absGroup.Value, CultureInfo.InvariantCulture);
					result.Add(GridLengthHelper2.FromValueAndType(value, GridUnitType.Pixel));
					continue;
				}

				throw new Exception("Unknown parsing error");
			}

			return result.ToArray();
		}

		private GridLength[] _sizes;

		#endregion

		#region InterElementSpacing DependencyProperty

		public static DependencyProperty InterElementSpacingProperty { get; } = DependencyProperty.Register(
			"InterElementSpacing", typeof(double), typeof(StarStackPanel), new PropertyMetadata((double)0.0, InvalidateLayoutOnChanged));

		public double InterElementSpacing
		{
			get { return (double)GetValue(InterElementSpacingProperty); }
			set { SetValue(InterElementSpacingProperty, value); }
		}
		#endregion

		#region Size AttachedProperty
		public static GridLength GetSize(DependencyObject obj)
		{
			return (GridLength)obj.GetValue(SizeProperty);
		}

		public static void SetSize(DependencyObject obj, GridLength value)
		{
			obj.SetValue(SizeProperty, value);
		}

		public static DependencyProperty SizeProperty { get; } = DependencyProperty.RegisterAttached
		(
			"Size",
			typeof(GridLength),
			typeof(StarStackPanel),
			new PropertyMetadata(GridLengthHelper2.Auto, HandleSizePropertyChanged)
		);

		private static void HandleSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is FrameworkElement element)
			{
				InvalidateLayoutOnChanged(element.Parent, default(DependencyPropertyChangedEventArgs));
			}
		}
		#endregion

		#region Padding DependencyProperty

		public Thickness Padding
		{
			get { return (Thickness)GetValue(PaddingProperty); }
			set { SetValue(PaddingProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Padding.  This enables animation, styling, binding, etc...
		public static DependencyProperty PaddingProperty { get; } =
			DependencyProperty.Register("Padding", typeof(Thickness), typeof(StarStackPanel), new PropertyMetadata(default(Thickness), InvalidateLayoutOnChanged));

		#endregion

		#region struct Record
		private struct Record
		{
			public UIElement Child;
			public Size Size;
			public GridLength SizeHint;
		}
		#endregion
	}
}
