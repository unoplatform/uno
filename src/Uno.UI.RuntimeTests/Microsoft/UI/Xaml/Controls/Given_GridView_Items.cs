using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
#if HAS_UNO
	[TestClass]
	[RunsOnUIThread]
#if !__APPLE_UIKIT__ && !__ANDROID__
	[Ignore]
#endif
	public class Given_GridView_Items
	{
#if !__ANDROID__
		[Ignore]
#endif
		[TestMethod]
		public async Task When_GridViewItems_WillRespect_ItemsWidth()
		{
			var gridView = new GridView
			{
				ItemContainerStyle = TestsResourceHelper.GetResource<Style>("MyGridViewItemStyle")
			};

			var gvi = new GridViewItem();
			var gvi2 = new GridViewItem();

			gridView.ItemsSource = new[] { gvi, gvi2 };

			var itemWrap = new ItemsWrapGrid()
			{
				MaximumRowsOrColumns = 2,
				Orientation = Orientation.Horizontal,
				ItemWidth = 30
			};

			var itemTemplate = new ItemsPanelTemplate(() => itemWrap);

			gridView.SetValue(ItemsControl.ItemsPanelProperty, itemTemplate);

			WindowHelper.WindowContent = gridView;
			await WindowHelper.WaitForLoaded(gridView);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(30, LayoutInformation.GetLayoutSlot(gvi2).Width);
		}

#if !__ANDROID__
		[Ignore]
#endif
		[TestMethod]
		public async Task When_GridViewItems_ItemsWidthChanges_Refresh_Layout()
		{
			var gridView = new GridView
			{
				ItemContainerStyle = TestsResourceHelper.GetResource<Style>("MyGridViewItemStyle")
			};

			var gvi = new GridViewItem();
			var gvi2 = new GridViewItem();

			gridView.ItemsSource = new[] { gvi, gvi2 };

			var itemWrap = new ItemsWrapGrid()
			{
				MaximumRowsOrColumns = 2,
				Orientation = Orientation.Horizontal,
				ItemWidth = 30
			};

			var itemTemplate = new ItemsPanelTemplate(() => itemWrap);

			gridView.SetValue(GridViewVariableSizeBehavior.ShouldResizeProperty, true);
			gridView.SetValue(ItemsControl.ItemsPanelProperty, itemTemplate);

			WindowHelper.WindowContent = gridView;
			await WindowHelper.WaitForLoaded(gridView);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(gridView.ActualWidth / 2, LayoutInformation.GetLayoutSlot(gvi2).Width, 1);
		}

#if !__APPLE_UIKIT__
		[Ignore]
#endif
		[TestMethod]
		public async Task When_GridViewItems_LayoutSlots()
		{
			var gridView = new GridView
			{
				ItemContainerStyle = TestsResourceHelper.GetResource<Style>("MyGridViewItemStyle")
			};

			var gvi = new GridViewItem();
			var gvi2 = new GridViewItem();

			gridView.ItemsSource = new[] { gvi, gvi2 };

			WindowHelper.WindowContent = gridView;
			await WindowHelper.WaitForLoaded(gridView);
			await WindowHelper.WaitForIdle();

			RectAssert.AreEqual(new Rect(0, 0, 100, 100), LayoutInformation.GetLayoutSlot(gvi));
			RectAssert.AreEqual(new Rect(0, 0, 100, 100), gvi.LayoutSlotWithMarginsAndAlignments);
			RectAssert.AreEqual(new Rect(0, 0, 100, 100), LayoutInformation.GetLayoutSlot(gvi2));
			RectAssert.AreEqual(new Rect(0, 0, 100, 100), gvi2.LayoutSlotWithMarginsAndAlignments);
		}
	}
	#region Helper Behaviour Class
	public class GridViewVariableSizeBehavior
	{
		public static bool GetShouldResize(DependencyObject obj) => (bool)obj.GetValue(ShouldResizeProperty);

		public static void SetShouldResize(DependencyObject obj, bool value) => obj.SetValue(ShouldResizeProperty, value);

		public static readonly DependencyProperty ShouldResizeProperty =
			DependencyProperty.RegisterAttached(
				"ShouldResize",
				typeof(bool),
				typeof(GridViewVariableSizeBehavior),
				new PropertyMetadata(false, OnShouldResizeChanged));

		private static void OnShouldResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is GridView gridView)
			{
				if (e.NewValue is bool shouldResize && shouldResize)
				{
					gridView.SizeChanged += GridView_SizeChanged;
				}
				else
				{
					gridView.SizeChanged -= GridView_SizeChanged;
				}
			}
		}

		private static void GridView_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (sender is GridView gridView && gridView?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
			{
				var width = (e.NewSize.Width - gridView.Padding.Left - gridView.Padding.Right) / Math.Max(wrapGrid.MaximumRowsOrColumns, 1);
				wrapGrid.ItemWidth = width;
			}
		}
	}
	#endregion
#endif
}
