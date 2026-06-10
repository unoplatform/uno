using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	// Repro coverage for the ElementLevelTheme sample under a Dark application:
	//   Bug 1 — setting RequestedTheme on the root at runtime re-themes children but not the element's OWN
	//           background {ThemeResource} (ApplicationPageBackgroundThemeBrush: black in Dark, white in Light).
	//   Bug 2 — a TextBox in a Light-pinned subtree resolves TextControlBorderBrush (-> TextControlElevationBorderBrush,
	//           a LinearGradientBrush whose stops are baked per theme dictionary: ControlStrokeColorDefault is
	//           #0F000000 in Light, #12FFFFFF in Dark) to the Dark brush, so the border is invisible on the light card.
	[TestClass]
	[RunsOnUIThread]
	public class Given_ElementTheme_Resolution_Regression
	{
#if HAS_UNO
		private static readonly Color _darkPageBg = Color.FromArgb(0xFF, 0x20, 0x20, 0x20);  // SolidBackgroundFillColorBase (Dark)
		private static readonly Color _lightPageBg = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3); // SolidBackgroundFillColorBase (Light)
		private static readonly Color _lightStroke = Color.FromArgb(0x0F, 0x00, 0x00, 0x00); // ControlStrokeColorDefault (Light)
		private static readonly Color _darkStroke = Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF);  // ControlStrokeColorDefault (Dark)

		// PROBE: isolate the dictionary lookup from all element/walk/pin machinery. Query the app resources
		// directly with the requested-theme-for-subtree slot scoped to Dark then Light (mimicking
		// load-under-Dark then switch-to-Light) — the dictionary leaf reads the slot like WinUI's
		// EnsureActiveThemeDictionary (Resources.cpp:764-768). The slot only drives resolution on
		// enhanced-lifecycle targets, so native mobile is excluded.
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeMobile)]
		public async Task Probe_Direct_Dictionary_Theme_Lookup()
		{
			var originalTheme = Application.Current.RequestedTheme;
			var wasExplicit = Application.Current.IsThemeSetExplicitly;
			try
			{
				Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);
				await TestServices.WindowHelper.WaitForIdle();

				var key = new SpecializedResourceDictionary.ResourceKey("ApplicationPageBackgroundThemeBrush");

				object QueryUnderTheme(Microsoft.UI.Xaml.Theme theme)
				{
					using var themeScope = Uno.UI.Xaml.Core.CoreServices.Instance.ScopeRequestedThemeForSubTree(theme);
					Application.Current.Resources.TryGetValue(key, out var value, false);
					return value;
				}

				var dv = QueryUnderTheme(Microsoft.UI.Xaml.Theme.Dark);
				var lv = QueryUnderTheme(Microsoft.UI.Xaml.Theme.Light);
				var dv2 = QueryUnderTheme(Microsoft.UI.Xaml.Theme.Dark);
				var lv2 = QueryUnderTheme(Microsoft.UI.Xaml.Theme.Light);

				var dc = (dv as SolidColorBrush)?.Color;
				var lc = (lv as SolidColorBrush)?.Color;

				Assert.AreEqual(_darkPageBg, dc, "Dark-scoped query must return Dark value");
				Assert.AreEqual(_lightPageBg, lc, "Light-scoped query must return Light value (dictionary lookup must honor the scoped subtree theme)");
				// A second query of each must stay stable (guards against a materialization caching the wrong theme).
				Assert.AreEqual(_darkPageBg, (dv2 as SolidColorBrush)?.Color, "Dark-scoped re-query must stay Dark");
				Assert.AreEqual(_lightPageBg, (lv2 as SolidColorBrush)?.Color, "Light-scoped re-query must stay Light");
			}
			finally
			{
				RestoreTheme(originalTheme, wasExplicit);
			}
		}

		// Bug 1: the page in the sample sets its own RequestedTheme at runtime; its Background must follow.
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Runtime_Self_Theme_Change_Updates_Own_Background()
		{
			var originalTheme = Application.Current.RequestedTheme;
			var wasExplicit = Application.Current.IsThemeSetExplicitly;
			try
			{
				Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);
				await TestServices.WindowHelper.WaitForIdle();

				// Pin Dark explicitly (rather than relying on the inherited host theme) so the baseline is
				// deterministic; the real bug is that switching this element's OWN theme afterwards doesn't
				// re-resolve its own Background {ThemeResource}.
				var root = (Grid)XamlReader.Load(
					"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
					"RequestedTheme='Dark' Width='200' Height='120' Background='{ThemeResource ApplicationPageBackgroundThemeBrush}'>" +
					"<TextBlock Text='x' Foreground='{ThemeResource TextFillColorPrimaryBrush}' /></Grid>");

				await UITestHelper.Load(root);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(_darkPageBg, ((SolidColorBrush)root.Background).Color, "baseline (RequestedTheme=Dark) must be #FF202020");

				root.RequestedTheme = ElementTheme.Light;
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(ElementTheme.Light, root.ActualTheme, "root.ActualTheme");
				Assert.AreEqual(_lightPageBg, ((SolidColorBrush)root.Background).Color,
					"the element's OWN Background must re-resolve to the Light value (#FFF3F3F3).");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
				RestoreTheme(originalTheme, wasExplicit);
			}
		}

		// Bug 2a: TextBox directly under a parse-time Light pin (matches the existing CheckBox control test shape).
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_ParseTime_Light_Pin_Direct_TextBox_Border_Resolves_Light()
		{
			await AssertTextBoxBorderLight(nested: false);
		}

		// Bug 2b: TextBox nested under Default-theme intermediates inside the Light pin, matching the sample's
		// Light column (Light Border -> card Border (Default) -> StackPanel -> TextBox).
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_ParseTime_Light_Pin_Nested_TextBox_Border_Resolves_Light()
		{
			await AssertTextBoxBorderLight(nested: true);
		}

		private static async Task AssertTextBoxBorderLight(bool nested)
		{
			var originalTheme = Application.Current.RequestedTheme;
			var wasExplicit = Application.Current.IsThemeSetExplicitly;
			try
			{
				Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);
				await TestServices.WindowHelper.WaitForIdle();

				var lightRoot = new Border { RequestedTheme = ElementTheme.Light };
				var textBox = new TextBox { PlaceholderText = "Enter your name" };

				if (nested)
				{
					var card = new Border();                 // RequestedTheme Default -> inherits Light
					var panel = new StackPanel();
					panel.Children.Add(textBox);
					card.Child = panel;
					lightRoot.Child = card;
				}
				else
				{
					lightRoot.Child = textBox;
				}

				await UITestHelper.Load(lightRoot);
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(ElementTheme.Light, textBox.ActualTheme, "textBox.ActualTheme");

				var borderElement = textBox.FindVisualChildByName("BorderElement") as Border;
				Assert.IsNotNull(borderElement, "BorderElement template part not found.");
				Assert.AreEqual(ElementTheme.Light, borderElement.ActualTheme, "BorderElement.ActualTheme");

				var bottomStop = GetBottomStrokeColor(borderElement.BorderBrush);
				Assert.AreEqual(_lightStroke, bottomStop,
					$"TextControlBorderBrush must resolve the Light brush (stroke {_lightStroke}); {_darkStroke} means it resolved Dark. Got {bottomStop}.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
				RestoreTheme(originalTheme, wasExplicit);
			}
		}

		// The TextBox elevation border is a LinearGradientBrush; its bottom stop (ControlStrokeColorDefault) is the
		// theme discriminator. Fall back to a SolidColorBrush color for any non-gradient case.
		private static Color GetBottomStrokeColor(Brush brush)
		{
			if (brush is LinearGradientBrush gradient && gradient.GradientStops.Count > 0)
			{
				return gradient.GradientStops.OrderByDescending(s => s.Offset).First().Color;
			}

			return (brush as SolidColorBrush)?.Color ?? default;
		}

		private static void RestoreTheme(ApplicationTheme originalTheme, bool wasExplicit)
		{
			if (wasExplicit)
			{
				Application.Current.SetExplicitRequestedTheme(originalTheme);
			}
			else
			{
				Application.Current.SetExplicitRequestedTheme(null);
			}
		}
#endif
	}
}
