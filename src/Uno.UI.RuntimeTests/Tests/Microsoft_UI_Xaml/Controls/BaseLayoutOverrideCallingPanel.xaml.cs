using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public partial class BaseLayoutOverrideCallingPanel : Panel
	{
		public BaseLayoutOverrideCallingPanel()
		{
			this.InitializeComponent();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var count = Children.Count;

			ComputeChildrenSize(availableSize, count, out _, out _, out var childSize);

			if (childSize.Width > 0 && childSize.Height > 0)
			{
				foreach (var child in Children)
				{
					child.Measure(childSize);
				}
			}

			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var count = Children.Count;

			ComputeChildrenSize(finalSize, count, out var columnCount, out _, out var childSize);

			var row = 0;
			var column = 0;
			for (var elementIndex = 0; elementIndex < count; elementIndex++)
			{
				var element = Children[elementIndex];

				var theRow = row;
				var theColumn = column;
				var theColumnSpan = 1;

				if (column + 1 == columnCount)
				{
					column = 0;
					row++;
				}
				else
				{
					column++;
				}

				var columnWidth = (float)childSize.Width;
				var rowHeight = (float)childSize.Height;

				element.Arrange(new Rect(theColumn * columnWidth, theRow * rowHeight, columnWidth * theColumnSpan, rowHeight));
			}

			return base.ArrangeOverride(finalSize);
		}

		private static void ComputeChildrenSize(Size targetSize, int childrenCount, out int columnCount, out int rowCount, out Size childSize)
		{
			childSize = new Size();

			if (childrenCount == 0)
			{
				columnCount = 0;
				rowCount = 0;
				return;
			}

			columnCount = Math.Max(1, (int)Math.Sqrt(childrenCount));
			rowCount = Math.Max(1, childrenCount / columnCount + (childrenCount % columnCount > 0 ? 1 : 0));
			childSize.Width = (float)(targetSize.Width / columnCount);
			childSize.Height = (float)(targetSize.Height / rowCount);
		}
	}
}
