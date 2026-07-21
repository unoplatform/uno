using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
#if HAS_UNO
using Uno.Helpers.Theming;
#endif
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.ViewManagement;
using static Private.Infrastructure.TestServices;
using MColors = Microsoft.UI.Colors;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_HighContrastAdjustment
{
	[TestMethod]
	public void When_Defaults_Match_WinUI()
	{
		Assert.AreEqual(ApplicationHighContrastAdjustment.Auto, Application.Current.HighContrastAdjustment);
		Assert.AreEqual(ElementHighContrastAdjustment.Application, new Grid().HighContrastAdjustment);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public void When_HighContrast_Scheme_Is_Read_Repeatedly()
	{
		var settings = new AccessibilitySettings();
		var expected = settings.HighContrastScheme;

		for (var i = 0; i < 100; i++)
		{
			Assert.AreEqual(expected, settings.HighContrastScheme);
		}
	}

	[TestMethod]
	public async Task When_Element_Value_Inherits()
	{
		var child = new Border();
		var root = new Grid
		{
			Width = 50,
			Height = 50,
			HighContrastAdjustment = ElementHighContrastAdjustment.None,
			Children = { child },
		};

		try
		{
			await UITestHelper.Load(root);
			Assert.AreEqual(ElementHighContrastAdjustment.None, child.HighContrastAdjustment);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

#if HAS_UNO
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Auto_Overrides_Combined_Opacity()
	{
		var originalAdjustment = Application.Current.HighContrastAdjustment;
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;

		var root = new Canvas
		{
			Width = 150,
			Height = 50,
			Background = new SolidColorBrush(MColors.White),
		};

		var auto = new Rectangle
		{
			Width = 50,
			Height = 50,
			Fill = new SolidColorBrush(MColors.Red),
			Opacity = 0.5,
			HighContrastAdjustment = ElementHighContrastAdjustment.Auto,
		};

		var none = new Rectangle
		{
			Width = 50,
			Height = 50,
			Fill = new SolidColorBrush(MColors.Red),
			Opacity = 0.5,
			HighContrastAdjustment = ElementHighContrastAdjustment.None,
		};
		Canvas.SetLeft(none, 50);

		var noneParent = new Grid
		{
			Width = 50,
			Height = 50,
			Opacity = 0.5,
			HighContrastAdjustment = ElementHighContrastAdjustment.None,
			Children =
			{
				new Rectangle
				{
					Fill = new SolidColorBrush(MColors.Red),
					Opacity = 0.5,
					HighContrastAdjustment = ElementHighContrastAdjustment.Auto,
				},
			},
		};
		Canvas.SetLeft(noneParent, 100);

		root.Children.Add(auto);
		root.Children.Add(none);
		root.Children.Add(noneParent);

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.Auto;
			await UITestHelper.Load(root);
			var screenshot = await UITestHelper.ScreenShot(root);

			ImageAssert.HasColorAt(screenshot, new(25, 25), MColors.Red, tolerance: 1);
			ImageAssert.HasColorAt(
				screenshot,
				new(75, 25),
				Color.FromArgb(255, 255, 127, 127),
				tolerance: 2);
			ImageAssert.HasColorAt(screenshot, new(125, 25), MColors.Red, tolerance: 1);
		}
		finally
		{
			Application.Current.HighContrastAdjustment = originalAdjustment;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Application_Adjustment_Changes_Opacity_Live()
	{
		var originalAdjustment = Application.Current.HighContrastAdjustment;
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;

		var rectangle = new Rectangle
		{
			Width = 50,
			Height = 50,
			Fill = new SolidColorBrush(MColors.Red),
			Opacity = 0.5,
		};
		var root = new Border
		{
			Width = 50,
			Height = 50,
			Background = new SolidColorBrush(MColors.White),
			Child = rectangle,
		};

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.None;
			await UITestHelper.Load(root);

			var disabled = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(
				disabled,
				new(25, 25),
				Color.FromArgb(255, 255, 127, 127),
				tolerance: 2);

			Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.Auto;
			await WindowHelper.WaitForIdle();

			var enabled = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(enabled, new(25, 25), MColors.Red, tolerance: 1);
		}
		finally
		{
			Application.Current.HighContrastAdjustment = originalAdjustment;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_HighContrast_Activates_On_Existing_Element_Updates_Opacity()
	{
		var originalAdjustment = Application.Current.HighContrastAdjustment;
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;

		var rectangle = new Rectangle
		{
			Width = 50,
			Height = 50,
			Fill = new SolidColorBrush(MColors.Red),
			Opacity = 0.5,
		};
		var root = new Border
		{
			Width = 50,
			Height = 50,
			Background = new SolidColorBrush(MColors.White),
			Child = rectangle,
		};

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = false;
			Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.Auto;
			await UITestHelper.Load(root);

			var before = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(
				before,
				new(25, 25),
				Color.FromArgb(255, 255, 127, 127),
				tolerance: 2);

			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			await WindowHelper.WaitForIdle();

			var after = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(after, new(25, 25), MColors.Red, tolerance: 1);
		}
		finally
		{
			Application.Current.HighContrastAdjustment = originalAdjustment;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Default_Adjustment_Element_Enters_Active_HighContrast_Renders_Opaque()
	{
		var originalAdjustment = Application.Current.HighContrastAdjustment;
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
		Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.Auto;

		var rectangle = new Rectangle
		{
			Width = 50,
			Height = 50,
			Fill = new SolidColorBrush(MColors.Red),
			Opacity = 0.5,
		};
		var root = new Border
		{
			Width = 50,
			Height = 50,
			Background = new SolidColorBrush(MColors.White),
			Child = rectangle,
		};

		try
		{
			await UITestHelper.Load(root);
			var screenshot = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(screenshot, new(25, 25), MColors.Red, tolerance: 1);
		}
		finally
		{
			Application.Current.HighContrastAdjustment = originalAdjustment;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Base_Theme_Changes_While_HighContrast_Active_Updates_Fallback_Variant()
	{
		var originalSystemTheme = SystemThemeHelper.SystemThemeOverride;
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var originalColors = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride;
		var dictionary = new ResourceDictionary
		{
			ThemeDictionaries =
			{
				["HighContrastWhite"] = new ResourceDictionary { ["Sentinel"] = "White" },
				["HighContrastBlack"] = new ResourceDictionary { ["Sentinel"] = "Black" },
			},
		};

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = null;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = null;
			SystemThemeHelper.SystemThemeOverride = SystemTheme.Dark;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("Black", dictionary["Sentinel"]);

			SystemThemeHelper.SystemThemeOverride = SystemTheme.Light;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("White", dictionary["Sentinel"]);
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = originalColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			SystemThemeHelper.SystemThemeOverride = originalSystemTheme;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_HighContrast_Has_No_Platform_Palette_Uses_Fallback_System_Colors()
	{
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var originalColors = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride;
		var dictionary = new ResourceDictionary();

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = null;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = "High Contrast Black";
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(MColors.Black, dictionary["SystemColorWindowColor"]);
			Assert.AreEqual(MColors.White, dictionary["SystemColorWindowTextColor"]);
			Assert.AreEqual(
				MColors.Black,
				((SolidColorBrush)dictionary["SystemColorWindowColorBrush"]).Color);
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = originalColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_HighContrast_System_Color_Changes_Update_Brush_In_Place()
	{
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var originalColors = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride;
		var dictionary = new ResourceDictionary();

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride =
				CreateSystemColors(MColors.Blue, MColors.Yellow);
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = "High Contrast #1";
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			await WindowHelper.WaitForIdle();

			var brush = (SolidColorBrush)dictionary["SystemColorWindowColorBrush"];
			Assert.AreEqual(MColors.Blue, brush.Color);

			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride =
				CreateSystemColors(MColors.Lime, MColors.Black);
			await WindowHelper.WaitForIdle();

			Assert.AreSame(brush, dictionary["SystemColorWindowColorBrush"]);
			Assert.AreEqual(MColors.Lime, brush.Color);
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = originalColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_HighContrast_Variant_Changes_ThemeDictionary()
	{
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var originalColors = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride;

		var dictionary = new ResourceDictionary
		{
			ThemeDictionaries =
			{
				["HighContrastWhite"] = new ResourceDictionary { ["Sentinel"] = "White" },
				["HighContrastBlack"] = new ResourceDictionary { ["Sentinel"] = "Black" },
				["HighContrastCustom"] = new ResourceDictionary { ["Sentinel"] = "Custom" },
				["HighContrast"] = new ResourceDictionary { ["Sentinel"] = "Generic" },
			},
		};

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride =
				CreateSystemColors(MColors.White, MColors.Black);
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			Assert.AreEqual("White", dictionary["Sentinel"]);

			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride =
				CreateSystemColors(MColors.Black, MColors.White);
			Assert.AreEqual("Black", dictionary["Sentinel"]);

			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride =
				CreateSystemColors(MColors.Gray, MColors.Blue);
			Assert.AreEqual("Custom", dictionary["Sentinel"]);

			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = null;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = "High Contrast #2";
			Assert.AreEqual("Custom", dictionary["Sentinel"]);
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = originalColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
		}
	}

	[TestMethod]
	public void When_HighContrast_Override_Changes_Settings_Event_Is_Raised()
	{
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var settings = new AccessibilitySettings();
		var raised = 0;

		void OnHighContrastChanged(AccessibilitySettings sender, object args) => raised++;

		try
		{
			settings.HighContrastChanged += OnHighContrastChanged;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = false;
			raised = 0;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;

			Assert.IsTrue(settings.HighContrast);
			Assert.AreEqual(1, raised);
		}
		finally
		{
			settings.HighContrastChanged -= OnHighContrastChanged;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Text_Adjustment_Uses_System_Backplate()
	{
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var originalColors = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride;

		var systemColors = new HighContrastSystemColors(
			ButtonFaceColor: MColors.Black,
			ButtonTextColor: MColors.White,
			GrayTextColor: MColors.Gray,
			HighlightColor: MColors.Blue,
			HighlightTextColor: MColors.Yellow,
			HotlightColor: MColors.Yellow,
			WindowColor: MColors.Black,
			WindowTextColor: MColors.White,
			ActiveCaptionColor: MColors.Black,
			BackgroundColor: MColors.Black,
			CaptionTextColor: MColors.White,
			InactiveCaptionColor: MColors.Black,
			InactiveCaptionTextColor: MColors.Gray,
			DisabledTextColor: MColors.Gray);

		var text = new TextBlock
		{
			Text = "MMMM",
			FontSize = 32,
			Foreground = new SolidColorBrush(MColors.Red),
			HighContrastAdjustment = ElementHighContrastAdjustment.Auto,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
		};

		var root = new Border
		{
			Width = 120,
			Height = 60,
			Background = new SolidColorBrush(MColors.Blue),
			Child = text,
		};

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = systemColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = "High Contrast Black";
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			await UITestHelper.Load(root);

			var adjusted = await UITestHelper.ScreenShot(root);
			Assert.IsTrue(CountPixels(adjusted, MColors.Black, tolerance: 2) > 500,
				"Auto must draw the system WindowColor backplate.");
			Assert.IsTrue(CountPixels(adjusted, MColors.White, tolerance: 2) > 20,
				"Auto must replace the text foreground with the system WindowTextColor.");

			text.HighContrastAdjustment = ElementHighContrastAdjustment.None;
			await WindowHelper.WaitForIdle();

			var unadjusted = await UITestHelper.ScreenShot(root);
			Assert.AreEqual(0, CountPixels(unadjusted, MColors.Black, tolerance: 2),
				"None must not draw the high-contrast backplate.");
			Assert.IsTrue(CountPixels(unadjusted, MColors.Red, tolerance: 2) > 20,
				"None must preserve the element foreground.");
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = originalColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_TextBox_Adjustment_Overrides_Background()
	{
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var originalColors = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride;

		var systemColors = new HighContrastSystemColors(
			ButtonFaceColor: MColors.Black,
			ButtonTextColor: MColors.White,
			GrayTextColor: MColors.Gray,
			HighlightColor: MColors.Blue,
			HighlightTextColor: MColors.Yellow,
			HotlightColor: MColors.Yellow,
			WindowColor: MColors.Lime,
			WindowTextColor: MColors.Black,
			ActiveCaptionColor: MColors.Black,
			BackgroundColor: MColors.Black,
			CaptionTextColor: MColors.White,
			InactiveCaptionColor: MColors.Black,
			InactiveCaptionTextColor: MColors.Gray,
			DisabledTextColor: MColors.Gray);

		var textBox = new TextBox
		{
			Width = 120,
			Height = 40,
			Background = new SolidColorBrush(MColors.Magenta),
			HighContrastAdjustment = ElementHighContrastAdjustment.Auto,
		};

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = systemColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = "High Contrast #1";
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = true;
			await UITestHelper.Load(textBox);

			var adjusted = await UITestHelper.ScreenShot(textBox);
			ImageAssert.HasColorAt(adjusted, new(60, 20), MColors.Lime, tolerance: 2);

			textBox.HighContrastAdjustment = ElementHighContrastAdjustment.None;
			await WindowHelper.WaitForIdle();

			var unadjusted = await UITestHelper.ScreenShot(textBox);
			ImageAssert.HasColorAt(unadjusted, new(60, 20), MColors.Magenta, tolerance: 2);
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSystemColorsOverride = originalColors;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}
	}

	private static int CountPixels(RawBitmap bitmap, Color expected, byte tolerance)
	{
		var count = 0;
		for (var y = 0; y < bitmap.Height; y++)
		{
			for (var x = 0; x < bitmap.Width; x++)
			{
				var actual = bitmap.GetPixel(x, y);
				if (Math.Abs(actual.R - expected.R) <= tolerance
					&& Math.Abs(actual.G - expected.G) <= tolerance
					&& Math.Abs(actual.B - expected.B) <= tolerance)
				{
					count++;
				}
			}
		}

		return count;
	}

	private static HighContrastSystemColors CreateSystemColors(Color window, Color windowText) =>
		new(
			ButtonFaceColor: window,
			ButtonTextColor: windowText,
			GrayTextColor: MColors.Gray,
			HighlightColor: MColors.Blue,
			HighlightTextColor: MColors.Yellow,
			HotlightColor: MColors.Yellow,
			WindowColor: window,
			WindowTextColor: windowText,
			ActiveCaptionColor: window,
			BackgroundColor: window,
			CaptionTextColor: windowText,
			InactiveCaptionColor: window,
			InactiveCaptionTextColor: MColors.Gray,
			DisabledTextColor: MColors.Gray);
#endif
}
