using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_BackgroundSizing
{
	#region Property Default & Read/Write

	[TestMethod]
	public void When_Border_Default_BackgroundSizing()
	{
		var border = new Border();
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, border.BackgroundSizing);
	}

	[TestMethod]
	public void When_ContentPresenter_Default_BackgroundSizing()
	{
		var cp = new ContentPresenter();
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, cp.BackgroundSizing);
	}

	[TestMethod]
	public void When_Control_Default_BackgroundSizing()
	{
		var button = new Button();
		// Raw default before style application
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, (BackgroundSizing)button.GetValue(Control.BackgroundSizingProperty));
	}

	[TestMethod]
	public void When_Grid_Default_BackgroundSizing()
	{
		var grid = new Grid();
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, grid.BackgroundSizing);
	}

	[TestMethod]
	public void When_StackPanel_Default_BackgroundSizing()
	{
		var sp = new StackPanel();
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, sp.BackgroundSizing);
	}

	[TestMethod]
	public void When_RelativePanel_Default_BackgroundSizing()
	{
		var rp = new RelativePanel();
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, rp.BackgroundSizing);
	}

	[TestMethod]
	public void When_Control_BackgroundSizing_SetAndGet()
	{
		var button = new Button();
		button.BackgroundSizing = BackgroundSizing.OuterBorderEdge;
		Assert.AreEqual(BackgroundSizing.OuterBorderEdge, button.BackgroundSizing);

		button.BackgroundSizing = BackgroundSizing.InnerBorderEdge;
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, button.BackgroundSizing);
	}

	[TestMethod]
	public async Task When_Button_Style_Sets_BackgroundSizing()
	{
		// Verify that an explicitly applied style can set BackgroundSizing.
		// AccentButtonStyle is used here as a concrete style that sets OuterBorderEdge.
		var button = new Button
		{
			Style = (Style)Application.Current.Resources["AccentButtonStyle"],
			Content = "Test"
		};

		await UITestHelper.Load(button);

		Assert.AreEqual(BackgroundSizing.OuterBorderEdge, button.BackgroundSizing);
	}

	#endregion

	#region TemplateBinding Propagation

	[TestMethod]
	public async Task When_Button_BackgroundSizing_Propagates_To_ContentPresenter()
	{
		var button = new Button
		{
			BackgroundSizing = BackgroundSizing.OuterBorderEdge,
			Content = "Test"
		};

		await UITestHelper.Load(button);

		var cp = button.FindVisualChildByType<ContentPresenter>();
		Assert.IsNotNull(cp, "ContentPresenter should exist in Button template");
		Assert.AreEqual(BackgroundSizing.OuterBorderEdge, cp.BackgroundSizing);
	}

	[TestMethod]
	public async Task When_Button_BackgroundSizing_InnerBorderEdge_Propagates()
	{
		var button = new Button
		{
			BackgroundSizing = BackgroundSizing.InnerBorderEdge,
			Content = "Test"
		};

		await UITestHelper.Load(button);

		var cp = button.FindVisualChildByType<ContentPresenter>();
		Assert.IsNotNull(cp, "ContentPresenter should exist in Button template");
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, cp.BackgroundSizing);
	}

	[TestMethod]
	public async Task When_ToggleButton_BackgroundSizing_Propagates_To_ContentPresenter()
	{
		var toggleButton = new ToggleButton
		{
			BackgroundSizing = BackgroundSizing.OuterBorderEdge,
			Content = "Test"
		};

		await UITestHelper.Load(toggleButton);

		// WinUI ToggleButton template uses ContentPresenter with {TemplateBinding BackgroundSizing}
		var cp = toggleButton.FindVisualChildByType<ContentPresenter>();
		Assert.IsNotNull(cp, "ContentPresenter should exist in ToggleButton template");
		Assert.AreEqual(BackgroundSizing.OuterBorderEdge, cp.BackgroundSizing);
	}

	[TestMethod]
	public async Task When_BackgroundSizing_Changed_After_Load_Propagates()
	{
		var button = new Button
		{
			BackgroundSizing = BackgroundSizing.InnerBorderEdge,
			Content = "Test"
		};

		await UITestHelper.Load(button);

		var cp = button.FindVisualChildByType<ContentPresenter>();
		Assert.IsNotNull(cp);
		Assert.AreEqual(BackgroundSizing.InnerBorderEdge, cp.BackgroundSizing);

		// Change at runtime
		button.BackgroundSizing = BackgroundSizing.OuterBorderEdge;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(BackgroundSizing.OuterBorderEdge, cp.BackgroundSizing);
	}

	#endregion

	#region Visual Rendering

#if HAS_RENDER_TARGET_BITMAP
	// The border-area samples below expect alpha-blend results (a 50% red border over the parent's
	// blue or the element's green), whose 8-bit rounding differs slightly between rasterizers.
	// The band is intentionally wider than the 1-5 used for flat colors: the two modes differ by
	// ~127 counts in the green/blue channels, so it still cannot conflate InnerBorderEdge with
	// OuterBorderEdge. Flat, unblended samples keep a tight tolerance.
	private const int BlendedBorderTolerance = 20;

	[TestMethod]
	public async Task When_Border_InnerBorderEdge_Background_Not_Under_Border()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Lime),
			BorderBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
			BorderThickness = new Thickness(20),
			BackgroundSizing = BackgroundSizing.InnerBorderEdge
		};

		var root = new Grid
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Children = { border }
		};

		await UITestHelper.Load(root);

		var screenshot = await UITestHelper.ScreenShot(root);

		// With InnerBorderEdge, the green background does not extend under the border.
		// The semi-transparent red border blends with the blue parent background,
		// producing a pixel with no green component: ~(255, 128, 0, 127).
		ImageAssert.HasColorAt(screenshot, 5, 50, Color.FromArgb(255, 128, 0, 127), tolerance: BlendedBorderTolerance);
	}

	[TestMethod]
	public async Task When_Border_OuterBorderEdge_Background_Under_Border()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Lime),
			BorderBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
			BorderThickness = new Thickness(20),
			BackgroundSizing = BackgroundSizing.OuterBorderEdge
		};

		var root = new Grid
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Children = { border }
		};

		await UITestHelper.Load(root);

		var screenshot = await UITestHelper.ScreenShot(root);

		// With OuterBorderEdge, the green background extends under the border.
		// The semi-transparent red border blends with the green background,
		// producing a pixel with a meaningful green component: ~(255, 128, 127, 0).
		ImageAssert.HasColorAt(screenshot, 5, 50, Color.FromArgb(255, 128, 127, 0), tolerance: BlendedBorderTolerance);
	}

	[TestMethod]
	public async Task When_ContentPresenter_OuterBorderEdge_Visual()
	{
		var cp = new ContentPresenter
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Lime),
			BorderBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
			BorderThickness = new Thickness(20),
			BackgroundSizing = BackgroundSizing.OuterBorderEdge
		};

		var root = new Grid
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Children = { cp }
		};

		await UITestHelper.Load(root);

		var screenshot = await UITestHelper.ScreenShot(root);

		// With OuterBorderEdge, the green background extends under the border.
		// The semi-transparent red border blends with the green background,
		// producing a pixel with a meaningful green component: ~(255, 128, 127, 0).
		ImageAssert.HasColorAt(screenshot, 5, 50, Color.FromArgb(255, 128, 127, 0), tolerance: BlendedBorderTolerance);
	}

	[TestMethod]
	public async Task When_Grid_BackgroundSizing_OuterBorderEdge_Visual()
	{
		var grid = new Grid
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Lime),
			BorderBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
			BorderThickness = new Thickness(20),
			BackgroundSizing = BackgroundSizing.OuterBorderEdge
		};

		var root = new Grid
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Children = { grid }
		};

		await UITestHelper.Load(root);

		var screenshot = await UITestHelper.ScreenShot(root);

		// With OuterBorderEdge, the green background extends under the border.
		// The semi-transparent red border blends with the green background,
		// producing a pixel with a meaningful green component: ~(255, 128, 127, 0).
		ImageAssert.HasColorAt(screenshot, 5, 50, Color.FromArgb(255, 128, 127, 0), tolerance: BlendedBorderTolerance);
	}

	[TestMethod]
	public async Task When_Border_BackgroundSizing_With_CornerRadius()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Lime),
			BorderBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
			BorderThickness = new Thickness(20),
			CornerRadius = new CornerRadius(15),
			BackgroundSizing = BackgroundSizing.InnerBorderEdge
		};

		var root = new Grid
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Children = { border }
		};

		await UITestHelper.Load(root);

		var screenshot = await UITestHelper.ScreenShot(root);

		// At mid-height the left border is straight (the 15px corner arcs do not reach y=50),
		// so this samples the border area. With InnerBorderEdge the green background stops at the
		// inner edge, leaving the semi-transparent red border blended with the blue parent:
		// ~(255, 128, 0, 127). Under OuterBorderEdge it would blend with green instead
		// (~(255, 128, 127, 0)), so this assertion discriminates between the two modes.
		ImageAssert.HasColorAt(screenshot, 5, 50, Color.FromArgb(255, 128, 0, 127), tolerance: BlendedBorderTolerance);

		// Inner content area stays green (flat, unblended color).
		ImageAssert.HasColorAt(screenshot, 50, 50, Microsoft.UI.Colors.Lime, tolerance: 5);
	}

	[TestMethod]
	public async Task When_Border_BackgroundSizing_Dynamic_Change_Visual()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Lime),
			BorderBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
			BorderThickness = new Thickness(20),
			BackgroundSizing = BackgroundSizing.InnerBorderEdge
		};

		var root = new Grid
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Children = { border }
		};

		await UITestHelper.Load(root);

		var screenshotBefore = await UITestHelper.ScreenShot(root);

		// With InnerBorderEdge, the green background does not extend under the border.
		// The semi-transparent red border blends with the blue parent background,
		// producing a pixel with no green component: ~(255, 128, 0, 127).
		ImageAssert.HasColorAt(screenshotBefore, 5, 50, Color.FromArgb(255, 128, 0, 127), tolerance: BlendedBorderTolerance);

		// Switch to OuterBorderEdge — green background now extends under the border
		border.BackgroundSizing = BackgroundSizing.OuterBorderEdge;
		await TestServices.WindowHelper.WaitForIdle();

		var screenshotAfter = await UITestHelper.ScreenShot(root);

		// With OuterBorderEdge, the green background extends under the border.
		// The semi-transparent red border blends with the green background,
		// producing a pixel with a meaningful green component: ~(255, 128, 127, 0).
		ImageAssert.HasColorAt(screenshotAfter, 5, 50, Color.FromArgb(255, 128, 127, 0), tolerance: BlendedBorderTolerance);
	}
#endif

	#endregion

	#region Issue #7192 Repro

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/7192")]
	public async Task When_ToggleButton_With_CornerRadius_And_Border()
	{
		var grid = new Grid
		{
			Width = 300,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
			}
		};

		var toggle1 = new ToggleButton
		{
			Content = "RGB",
			BorderThickness = new Thickness(1),
			IsChecked = true,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			IsThreeState = false,
			CornerRadius = new CornerRadius(2, 0, 0, 2)
		};
		Grid.SetColumn(toggle1, 0);

		var toggle2 = new ToggleButton
		{
			Content = "HSV",
			BorderThickness = new Thickness(1),
			Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			IsThreeState = false,
			CornerRadius = new CornerRadius(0, 2, 2, 0)
		};
		Grid.SetColumn(toggle2, 1);

		grid.Children.Add(toggle1);
		grid.Children.Add(toggle2);

		await UITestHelper.Load(grid);

		// Both ToggleButtons should render with valid dimensions
		Assert.IsTrue(toggle1.ActualWidth > 0, "toggle1 ActualWidth should be > 0");
		Assert.IsTrue(toggle1.ActualHeight > 0, "toggle1 ActualHeight should be > 0");
		Assert.IsTrue(toggle2.ActualWidth > 0, "toggle2 ActualWidth should be > 0");
		Assert.IsTrue(toggle2.ActualHeight > 0, "toggle2 ActualHeight should be > 0");

		// Verify BackgroundSizing can be set on ToggleButton and propagates
		// WinUI ToggleButton template uses ContentPresenter with {TemplateBinding BackgroundSizing}
		toggle1.BackgroundSizing = BackgroundSizing.OuterBorderEdge;
		await TestServices.WindowHelper.WaitForIdle();

		var cp = toggle1.FindVisualChildByType<ContentPresenter>();
		Assert.IsNotNull(cp, "ContentPresenter should exist in ToggleButton template");
		Assert.AreEqual(BackgroundSizing.OuterBorderEdge, cp.BackgroundSizing,
			"BackgroundSizing should propagate from ToggleButton to template ContentPresenter");
	}

	#endregion
}
