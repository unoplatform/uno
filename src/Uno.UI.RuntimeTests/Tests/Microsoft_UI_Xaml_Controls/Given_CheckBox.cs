using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
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
			var fill = (Rectangle)checkBox.FindVisualChildByName("NormalRectangle");
			Assert.IsNotNull(fill, "Could not find the CheckBox 'NormalRectangle' template part.");
			return fill.TransformToVisual(root).TransformPoint(new Point(fill.ActualWidth / 2, 3));
		}

		/// <summary>
		/// A checked+disabled CheckBox inside a disabled ListView must render with the same color as a
		/// standalone checked+disabled CheckBox. WinUI does not dim disabled item content; Uno used to
		/// additionally dim it by ListViewItemDisabledThemeOpacity (0.55) when IsEnabled was coerced false
		/// on the container by the disabled parent ListView, which double-dimmed the already-disabled glyph.
		/// </summary>
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Disabled_In_Disabled_ListView_Matches_Standalone()
		{
			var standalone = new CheckBox { Content = "Standalone", IsChecked = true, IsEnabled = false };

			var inListCheckBox = new CheckBox { Content = "InList", IsChecked = true, IsEnabled = false };
			var listView = new ListView { IsEnabled = false };
			listView.Items.Add(new ListViewItem { Content = inListCheckBox });

			var root = new StackPanel
			{
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

			// The in-list checkbox must match the standalone one (no extra 0.55 dim from the disabled ListView).
			ImageAssert.HasColorAt(bmp, (float)inListPoint.X, (float)inListPoint.Y, standaloneColor, tolerance: 8);
		}

		/// <summary>
		/// An individually disabled ListViewItem (inside an enabled ListView) must render its content with
		/// the same color as an enabled item: matching WinUI, which does not dim disabled item content.
		/// </summary>
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_ListViewItem_Locally_Disabled_Matches_Enabled()
		{
			var checkBoxInEnabledItem = new CheckBox { Content = "Enabled item", IsChecked = true, IsEnabled = false };
			var checkBoxInDisabledItem = new CheckBox { Content = "Disabled item", IsChecked = true, IsEnabled = false };

			var listView = new ListView();
			listView.Items.Add(new ListViewItem { Content = checkBoxInEnabledItem });
			listView.Items.Add(new ListViewItem { Content = checkBoxInDisabledItem, IsEnabled = false });
			var root = new StackPanel
			{
				RequestedTheme = ElementTheme.Dark,
				Background = new SolidColorBrush(Colors.Black),
				Children = { listView },
			};

			await UITestHelper.Load(root);
			await WindowHelper.WaitForLoaded(disabledItemCheckBox);
			await WindowHelper.WaitForIdle();

			var bmp = await UITestHelper.ScreenShot(root);

			var enabledPoint = GetCheckFillPoint(enabledItemCheckBox, root);
			var disabledPoint = GetCheckFillPoint(disabledItemCheckBox, root);

			var enabledColor = bmp.GetPixel((int)enabledPoint.X, (int)enabledPoint.Y);

			// Guard: ensure we sampled the rendered disabled fill, not the (black) background.
			ImageAssert.DoesNotHaveColorAt(bmp, (float)enabledPoint.X, (float)enabledPoint.Y, Colors.Black, tolerance: 12);

			// The locally-disabled item's content must match the enabled item's content (no opacity dim).
			ImageAssert.HasColorAt(bmp, (float)disabledPoint.X, (float)disabledPoint.Y, enabledColor, tolerance: 8);
		}
	}
}
