using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.GridPages
{
	partial class GridAdder : Panel
	{
		private bool _hasAddedGrid;
		private Grid _grid;
		public Grid AddedGrid => _grid;
		public Exception Exception { get; private set; }
		public bool WasArranged { get; private set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			try
			{
				if (!_hasAddedGrid)
				{
					_hasAddedGrid = true;
					_grid = new GridWithColumns();
					_grid.Visibility = Visibility.Collapsed;
					Children.Insert(0, _grid);
				}

				_grid.Measure(availableSize);
				return _grid.DesiredSize;
			}
			catch (Exception e)
			{
				Exception ??= e;
				throw;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			WasArranged = true;

			try
			{
				_grid.Visibility = Visibility.Visible;
				_grid.Arrange(new Rect(default, finalSize));
				return finalSize;
			}
			catch (Exception e)
			{
				Exception ??= e;
				throw;
			}
		}
	}
}
