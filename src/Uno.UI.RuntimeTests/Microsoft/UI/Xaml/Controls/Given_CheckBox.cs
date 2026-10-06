using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_CheckBox
	{
		// Samples the fill of the checkbox's check-background ("NormalRectangle"), in `root` coordinates.
		// The point is kept off the centered check glyph so we read the fill, not the glyph.
		private static Point GetCheckFillPoint(CheckBox checkBox, FrameworkElement root)
		{
			var fill = checkBox.FindVisualChildByName("NormalRectangle") as Rectangle;
			Assert.IsNotNull(fill, "Could not find the CheckBox 'NormalRectangle' template part.");
			return fill.TransformToVisual(root).TransformPoint(new Point(fill.ActualWidth / 2, 3));
		}

		/// <summary>
		/// A checked+disabled CheckBox inside a disabled ListView renders like a standalone one. The rounded
		/// chrome dims the item content (DisabledOpacity) only on a Disabled state change once the content
		/// exists, and an item that starts disabled enters that state before its content is created.
		/// </summary>
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Disabled_In_Disabled_ListView_Matches_Standalone()
		{
			var standalone = new CheckBox { Content = "Standalone", IsChecked = true, IsEnabled = false };

			var inListCheckBox = new CheckBox { Content = "InList", IsChecked = true, IsEnabled = false };
			var listView = new ListView { IsEnabled = false, ItemContainerTransitions = new() }; // WinUI's item entrance transition hides content right after load
			listView.Items.Add(new ListViewItem { Content = inListCheckBox });

			var root = new StackPanel
			{
				Width = 300, // RenderTargetBitmap on WinUI downscales captures wider than 4096 px
				RequestedTheme = ElementTheme.Dark,
				Background = new SolidColorBrush(Colors.Black),
				Children = { standalone, listView },
			};

			await UITestHelper.Load(root);
			await WindowHelper.WaitForLoaded(inListCheckBox);
			await WindowHelper.WaitForIdle();

			var bmp = await UITestHelper.ScreenShot(root);

			var standalonePoint = GetCheckFillPoint(standalone, root);
			var inListPoint = GetCheckFillPoint(inListCheckBox, root);

			var standaloneColor = bmp.GetPixel((int)standalonePoint.X, (int)standalonePoint.Y);

			// Guard: make sure we actually sampled the rendered disabled fill, not the (black) background.
			ImageAssert.DoesNotHaveColorAt(bmp, (float)standalonePoint.X, (float)standalonePoint.Y, Colors.Black, tolerance: 12);

			// The in-list checkbox must match the standalone one (no dim from the disabled ListView).
			ImageAssert.HasColorAt(bmp, (float)inListPoint.X, (float)inListPoint.Y, standaloneColor, tolerance: 8);
		}

		/// <summary>
		/// A ListViewItem disabled before it loads (inside an enabled ListView) renders its content like an
		/// enabled item, for the same reason as above.
		/// </summary>
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_ListViewItem_Locally_Disabled_Matches_Enabled()
		{
			var checkBoxInEnabledItem = new CheckBox { Content = "Enabled item", IsChecked = true, IsEnabled = false };
			var checkBoxInDisabledItem = new CheckBox { Content = "Disabled item", IsChecked = true, IsEnabled = false };

			var listView = new ListView { ItemContainerTransitions = new() }; // WinUI's item entrance transition hides content right after load
			listView.Items.Add(new ListViewItem { Content = checkBoxInEnabledItem });
			listView.Items.Add(new ListViewItem { Content = checkBoxInDisabledItem, IsEnabled = false });

			var root = new StackPanel
			{
				Width = 300, // RenderTargetBitmap on WinUI downscales captures wider than 4096 px
				RequestedTheme = ElementTheme.Dark,
				Background = new SolidColorBrush(Colors.Black),
				Children = { listView },
			};

			await UITestHelper.Load(root);
			await WindowHelper.WaitForLoaded(checkBoxInDisabledItem);
			await WindowHelper.WaitForIdle();

			var bmp = await UITestHelper.ScreenShot(root);

			var enabledPoint = GetCheckFillPoint(checkBoxInEnabledItem, root);
			var disabledPoint = GetCheckFillPoint(checkBoxInDisabledItem, root);

			var enabledColor = bmp.GetPixel((int)enabledPoint.X, (int)enabledPoint.Y);

			// Guard: ensure we sampled the rendered disabled fill, not the (black) background.
			ImageAssert.DoesNotHaveColorAt(bmp, (float)enabledPoint.X, (float)enabledPoint.Y, Colors.Black, tolerance: 12);

			// The locally-disabled item's content must match the enabled item's content (no opacity dim).
			ImageAssert.HasColorAt(bmp, (float)disabledPoint.X, (float)disabledPoint.Y, enabledColor, tolerance: 8);
		}

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_ListViewItem_Disabled_After_Load_Content_Dimmed()
		{
			var checkBox = new CheckBox { Content = "Item", IsChecked = true, IsEnabled = false };
			var item = new ListViewItem { Content = checkBox };
			var listView = new ListView { Items = { item }, ItemContainerTransitions = new() }; // WinUI's item entrance transition hides content right after load

			var root = new StackPanel
			{
				Width = 300, // RenderTargetBitmap on WinUI downscales captures wider than 4096 px
				RequestedTheme = ElementTheme.Dark,
				Background = new SolidColorBrush(Colors.Black),
				Children = { listView },
			};

			await UITestHelper.Load(root);
			await WindowHelper.WaitForLoaded(checkBox);
			await WindowHelper.WaitForIdle();

			var point = GetCheckFillPoint(checkBox, root);
			var enabled = await UITestHelper.ScreenShot(root);
			var enabledColor = enabled.GetPixel((int)point.X, (int)point.Y);
			ImageAssert.DoesNotHaveColorAt(enabled, (float)point.X, (float)point.Y, Colors.Black, tolerance: 12);

			item.IsEnabled = false;
			await WindowHelper.WaitForIdle();

			// Over black, the content Opacity of DisabledOpacity scales each channel.
			var opacity = ((ListViewItemPresenter)VisualTreeHelper.GetChild(item, 0)).DisabledOpacity;
			byte Dim(byte channel) => (byte)Math.Round(channel * opacity);
			var dimmed = Color.FromArgb(255, Dim(enabledColor.R), Dim(enabledColor.G), Dim(enabledColor.B));
			ImageAssert.HasColorAt(await UITestHelper.ScreenShot(root), (float)point.X, (float)point.Y, dimmed, tolerance: 8);

			item.IsEnabled = true;
			await WindowHelper.WaitForIdle();

			ImageAssert.HasColorAt(await UITestHelper.ScreenShot(root), (float)point.X, (float)point.Y, enabledColor, tolerance: 8);
		}
	}
}
