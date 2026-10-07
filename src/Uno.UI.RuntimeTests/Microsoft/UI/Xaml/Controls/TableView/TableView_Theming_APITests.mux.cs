// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Theming_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewThemingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;
// Category 13 of the TableView API test plan: theming, resources, density and high contrast.
//
// Three dictionaries own distinct things, and a test that looks in the wrong one finds nothing:
//
//   * CommonStyles\TabularSurfaces_themeresources.xaml - the brush palette. Every TabularSurface*
//     key, in Default / Light / HighContrast. The canonical source.
//   * TableView\TableView_themeresources.xaml - the metric tokens ONLY: row heights, font sizes,
//     cell and header padding, the density variants, gripper width. Brushes are deliberately
//     excluded; mirroring them would collide on duplicate keys during theme-XBF emission.
//   * TableView\TableView.xaml (root) - last-resort re-resolving {ThemeResource} fallbacks for
//     hosts that do not merge TabularSurfaces.
//
// All of them reach a consumer through TabularControlsResources, which the test app must merge
// explicitly: App.xaml merges only XamlControlsResources. That is also why
// CommonStylesTests.VerifyAllThemesContainSameResourceKeys does NOT cover any of these keys -
// it builds a XamlControlsResources. VerifyTabularSurfaceKeysExistInEveryThemeDictionary below
// is the only parity guard these keys have.
[TestClass]
public class TableViewThemingTests : TableViewApiTestBase
{
	#region 13.1 Style resolution

	// TableView's own default style is already proven by VerifyTemplatePartsAfterTemplateApplication
	// in TableViewTests.cs - that test cannot pass unless the style applied. The value here is the
	// container types nothing else touches.
	//
	// Asserts the template AND a setter rather than just Style != null: a style can be attached and
	// still not be the intended one.
	[TestMethod]
	public async Task VerifyRowAndGroupHeaderDefaultStylesResolve()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
			if (row == null)
			{
				Verify.Fail("A grouped TableView should realize at least one TableViewRow.");
				return;
			}

			Verify.IsNotNull(row.Template, "TableViewRow must resolve its default ControlTemplate.");
			Verify.AreEqual(new Thickness(0), row.Padding,
				"TableViewRow's default style sets Padding to 0; the row itself adds no inset, cells carry the density padding.");

			var groupHeader = FindVisualChildrenByType<TableViewGroupHeader>(tableView).FirstOrDefault();
			if (groupHeader == null)
			{
				Verify.Fail("A grouped TableView should realize at least one TableViewGroupHeader.");
				return;
			}

			Verify.IsNotNull(groupHeader.Template, "TableViewGroupHeader must resolve its default ControlTemplate.");

			// Hardcoded rather than looked up: TableViewGroupHeaderPadding is declared at the root
			// of TableView.xaml, the default-style page, which reaches a control through
			// DefaultStyleResourceUri and is never merged into Application.Current.Resources.
			Verify.AreEqual(new Thickness(8, 6, 16, 6), groupHeader.Padding,
				"TableViewGroupHeader's default style must take Padding from TableViewGroupHeaderPadding.");
		});
	}

	// Both types are MUX_INTERNAL but projected into the test app as Microsoft.UI.Private.Controls;
	// TableView_Sizing_APITests.cs already constructs a ResizeGripper the same way.
	[TestMethod]
	public async Task VerifySortIndicatorAndResizeGripperDefaultStylesResolve()
	{
		SortIndicator indicator = null;
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			indicator = new SortIndicator();
			gripper = new ResizeGripper { Width = 8, Height = 24 };

			var host = new StackPanel();
			host.Children.Add(indicator);
			host.Children.Add(gripper);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(indicator.Template,
				"SortIndicator must resolve its default ControlTemplate, otherwise the sort glyph renders nothing.");
			Verify.IsNotNull(gripper.Template,
				"ResizeGripper must resolve its default ControlTemplate, otherwise the resize affordance is invisible while still being draggable.");

			// A setter, not just the template: proves it is this style and not some other one.
			Verify.IsFalse(gripper.IsTabStop,
				"The default ResizeGripper style sets IsTabStop=False; the header cell is the keyboard target.");
			Verify.IsFalse(indicator.IsHitTestVisible,
				"The default SortIndicator style sets IsHitTestVisible=False; it is decorative.");
		});
	}

	#endregion

	#region 13.2 Theme resources

	// The single highest-value test in this category. A key present in Default but missing from
	// HighContrast is a CRASH at resolve time under that theme, and it is invisible in whichever
	// theme the author happened to be working in.
	//
	// Models CommonStylesTests.VerifyAllThemesContainSameResourceKeys, which does not reach these
	// keys at all - it builds a XamlControlsResources, and TableView's resources live in
	// TabularControlsResources. Covers the TableView* metric tokens in the same pass: same failure
	// mode, and no reason to walk the dictionaries twice.
	[TestMethod]
	public void VerifyTabularSurfaceKeysExistInEveryThemeDictionary()
	{
		RunOnUIThread.Execute(() =>
		{
			var resources = new TabularControlsResources();

			var themeDictionaries = new Dictionary<string, ResourceDictionary>();
			foreach (var themeName in new[] { "Default", "Light", "HighContrast" })
			{
				if (!resources.ThemeDictionaries.ContainsKey(themeName))
				{
					Verify.Fail($"TabularControlsResources must declare a '{themeName}' theme dictionary. " +
						"Once any merged dictionary declares Light, XAML stops treating Default as a Light fallback, " +
						"so a missing theme silently drops every key from that theme.");
					return;
				}

				var dictionary = resources.ThemeDictionaries[themeName] as ResourceDictionary;
				if (dictionary == null)
				{
					Verify.Fail($"The '{themeName}' entry in TabularControlsResources.ThemeDictionaries must be a ResourceDictionary.");
					return;
				}

				themeDictionaries[themeName] = dictionary;
				Log.Comment($"{themeName}: {dictionary.Keys.Count} keys.");
			}

			var defaultKeys = CollectKeys(themeDictionaries["Default"]);

			// Sanity floor: an empty Default dictionary would make every comparison below pass.
			Verify.IsGreaterThan(defaultKeys.Count, 0,
				"The Default theme dictionary must not be empty - TabularControlsResources failed to load its source.");

			foreach (var themeName in new[] { "Light", "HighContrast" })
			{
				var themeKeys = CollectKeys(themeDictionaries[themeName]);

				// Report names, not counts. A bare "counts differ" failure leaves the next person
				// diffing three dictionaries by hand.
				var missingFromTheme = defaultKeys.Except(themeKeys).OrderBy(key => key).ToList();
				var missingFromDefault = themeKeys.Except(defaultKeys).OrderBy(key => key).ToList();

				Verify.AreEqual(0, missingFromTheme.Count,
					$"Keys defined in Default but missing from {themeName}: {string.Join(", ", missingFromTheme)}");
				Verify.AreEqual(0, missingFromDefault.Count,
					$"Keys defined in {themeName} but missing from Default: {string.Join(", ", missingFromDefault)}");
			}

			// Both families must actually be present, or the parity check above is vacuously true
			// for whichever one failed to merge.
			Verify.IsTrue(defaultKeys.Any(key => key.StartsWith("TabularSurface", StringComparison.Ordinal)),
				"The brush palette from TabularSurfaces_themeresources.xaml must be part of TabularControlsResources.");
			Verify.IsTrue(defaultKeys.Contains("TableViewRowMinHeightCompact"),
				"The metric tokens from TableView_themeresources.xaml must be part of TabularControlsResources.");
		});
	}

	// Reasoned from the high-contrast requirement itself rather than a spec sentence: the dictionary
	// is written this way throughout today, so this pins an existing, deliberate property.
	//
	// A hardcoded colour under high contrast ignores the user's chosen scheme. For a user who
	// requires high contrast that is not cosmetic - it can make a column of text unreadable against
	// its own background.
	[TestMethod]
	public void VerifyHighContrastBrushesUseSystemColors()
	{
		RunOnUIThread.Execute(() =>
		{
			var resources = new TabularControlsResources();
			EnsureTabularControlsResources();
			var highContrast = resources.ThemeDictionaries["HighContrast"] as ResourceDictionary;
			if (highContrast == null)
			{
				Verify.Fail("TabularControlsResources must declare a HighContrast theme dictionary.");
				return;
			}

			// The system colours as the framework resolves them right now. Collected from the app,
			// not from the dictionary under test, so this is a genuine cross-source comparison.
			var systemColors = CollectSystemColors();
			Verify.IsGreaterThan(systemColors.Count, 0,
				"SystemColor* resources must resolve from the application resources. Without them this test " +
				"cannot tell a system colour from a literal, so it fails rather than silently passing.");

			var literalBrushes = new List<string>();
			int checkedBrushes = 0;

			foreach (var key in CollectKeys(highContrast))
			{
				// Only the canonical palette entries, which declare an explicit
				// Color="{ThemeResource SystemColor*}". The MUX_-prefixed keys are StaticResource
				// aliases; a StaticResource inside a theme dictionary is resolved when that
				// dictionary loads, so reading one off a detached TabularControlsResources yields
				// whichever sibling theme XAML happened to bind - an artifact of the read, not the
				// value a high-contrast user would see. Same for SortIndicatorForeground, which is
				// an alias to SystemColorWindowTextColorBrush.
				if (!key.StartsWith("TabularSurface", StringComparison.Ordinal))
				{
					continue;
				}

				if (!(highContrast[key] is SolidColorBrush brush))
				{
					continue;
				}

				checkedBrushes++;

				// Transparent is not a colour choice - it is the absence of one, and it is scheme
				// neutral, so it is never a high-contrast defect.
				if (brush.Color.A == 0)
				{
					continue;
				}

				if (!systemColors.Contains(brush.Color))
				{
					literalBrushes.Add($"{key} (#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2})");
				}
			}

			Verify.IsGreaterThan(checkedBrushes, 0,
				"The HighContrast dictionary must contain SolidColorBrush entries; finding none means it did not load.");

			Verify.AreEqual(0, literalBrushes.Count,
				"Every high-contrast brush must trace to a SystemColor* value. Hardcoded: " +
				string.Join(", ", literalBrushes));
		});
	}

	// One of very few exact values the spec states outright (TableView-dev-spec.md:176): the dark
	// TabularSurfaceGridLineBrush is #29FFFFFF and the C++ fallback uses the same 16% white.
	//
	// Pinning a literal colour is normally weak. Here the spec names it, and the test's real job is
	// the cross-source agreement: the two sources drifting apart gives a host that merges
	// TabularSurfaces a different gridline colour from one that does not, which is the same app
	// looking different depending on how it was packaged.
	[TestMethod]
	public void VerifyDarkGridLineBrushMatchesTheDocumentedValue()
	{
		RunOnUIThread.Execute(() =>
		{
			var resources = new TabularControlsResources();

			var dark = RequireBrush(resources, "Default", "TabularSurfaceGridLineBrush");
			var light = RequireBrush(resources, "Light", "TabularSurfaceGridLineBrush");
			if (dark == null || light == null)
			{
				return;
			}

			Verify.AreEqual(Color.FromArgb(0x29, 0xFF, 0xFF, 0xFF), dark.Color,
				"Dark TabularSurfaceGridLineBrush must be #29FFFFFF - 16% white, bumped from DividerStrokeColorDefault's " +
				"8.2% for readability over row banding. TableView.cpp's CreateGridLineFallbackBrush hardcodes the same value.");

			Verify.AreEqual(Color.FromArgb(0x29, 0x00, 0x00, 0x00), light.Color,
				"Light TabularSurfaceGridLineBrush must be #29000000, matching CreateGridLineFallbackBrush's light branch. " +
				"If these disagree, the gridline colour depends on whether the host merged TabularSurfaces.");
		});
	}

	// TableView-dev-spec.md:227-231 makes overriding a TabularSurface* key THE supported way to
	// restyle the table, in place of a monolithic style. If the control caches or hardcodes the
	// brush instead of resolving it, that story is fiction.
	//
	// The gridline brush is held in the per-instance resource cache, so override BEFORE load;
	// overriding afterwards tests the invalidation path, which is
	// VerifyThemeChangeAfterLoadReResolvesRowsAndHeaders' job.
	[TestMethod]
	[TestProperty("Ignore", "True")] // Product bug: LookupElementResource resolves merged theme dictionaries before app-scope keys, so app overrides lose. Re-enable with the lookup-precedence fix.
	[Ignore("WinUI ignores this test: Product bug: LookupElementResource resolves merged theme dictionaries before app-scope keys, so app overrides lose. Re-enable with the lookup-precedence fix.")]
	public async Task VerifyGridLineBrushOverrideChangesRenderedSeparators()
	{
		var expected = Color.FromArgb(0xFF, 0xFF, 0x00, 0x99);
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceGridLineBrush", new SolidColorBrush(expected));

			tableView = CreateThemingTable();
			Verify.AreEqual(TableViewGridLinesVisibility.All, tableView.GridLinesVisibility,
				"This test relies on the default including vertical lines; the per-cell separator is where the brush lands.");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
				if (row == null)
				{
					Verify.Fail("The TableView should realize at least one row.");
					return;
				}

				var wrapper = GetRowCell(row, 0);
				var brush = wrapper.BorderBrush as SolidColorBrush;
				if (brush == null)
				{
					Verify.Fail("A cell wrapper should carry a SolidColorBrush vertical separator when GridLinesVisibility includes Vertical.");
					return;
				}

				Verify.AreEqual(expected, brush.Color,
					"An app-scope TabularSurfaceGridLineBrush override must reach the rendered cell separator.");
			});
		}
		finally
		{
			// Application.Resources is UI-thread affine; a finally block runs on the test thread.
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceGridLineBrush"));
		}
	}

	// TableView-dev-spec.md:123 names both keys as part of what "the primitive owns", which makes
	// them a published surface rather than an internal detail. If they are inert a host cannot match
	// the separator to its design language.
	[TestMethod]
	[TestProperty("Ignore", "True")] // Product bug: ResizeGripper styling keys live in its default-style page, so app-scope overrides cannot win. Re-enable once they move to a themeresources dictionary.
	[Ignore("WinUI ignores this test: Product bug: ResizeGripper styling keys live in its default-style page, so app-scope overrides cannot win. Re-enable once they move to a themeresources dictionary.")]
	public async Task VerifyResizeGripperSeparatorResourcesApply()
	{
		var expectedColor = Color.FromArgb(0xFF, 0x00, 0xCC, 0x44);
		var expectedThickness = new Thickness(0, 0, 5, 0);
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("ResizeGripperSeparatorBrush", new SolidColorBrush(expectedColor));
			SetApplicationResource("ResizeGripperSeparatorThickness", expectedThickness);

			gripper = new ResizeGripper { Width = 8, Height = 24 };
			LoadContent(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				var brush = gripper.BorderBrush as SolidColorBrush;
				if (brush == null)
				{
					Verify.Fail("The default ResizeGripper style must set BorderBrush from ResizeGripperSeparatorBrush.");
					return;
				}

				Verify.AreEqual(expectedColor, brush.Color,
					"An app-scope ResizeGripperSeparatorBrush override must win over the primitive's own theme dictionary.");

				// The named state target from the template contract in ResizeGripper.idl. The
				// OrientationStates group applies the thickness, and DragOrientation defaults to
				// Horizontal, so the horizontal key is the one in play.
				var separator = gripper.FindVisualChildByName("Separator") as Border;
				if (separator == null)
				{
					Verify.Fail("The ResizeGripper template must contain the 'Separator' part its visual states target by name.");
					return;
				}

				Verify.AreEqual(expectedThickness, separator.BorderThickness,
					"An app-scope ResizeGripperSeparatorThickness override must reach the separator through the Horizontal visual state.");
			});
		}
		finally
		{
			// Application.Resources is UI-thread affine; a finally block runs on the test thread.
			RunOnUIThread.Execute(() =>
			{
				ClearApplicationResource("ResizeGripperSeparatorBrush");
				ClearApplicationResource("ResizeGripperSeparatorThickness");
			});
		}
	}

	// If the sort glyph cannot be themed it may end up invisible against a customised header
	// background.
	[TestMethod]
	public async Task VerifySortIndicatorForegroundResourceApplies()
	{
		var expected = Color.FromArgb(0xFF, 0x33, 0x66, 0xFF);
		SortIndicator indicator = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("SortIndicatorForeground", new SolidColorBrush(expected));

			indicator = new SortIndicator { Direction = SortIndicatorDirection.Ascending };
			LoadContent(indicator);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				var foreground = indicator.Foreground as SolidColorBrush;
				if (foreground == null)
				{
					Verify.Fail("The default SortIndicator style must set Foreground from SortIndicatorForeground.");
					return;
				}

				Verify.AreEqual(expected, foreground.Color,
					"An app-scope SortIndicatorForeground override must reach the indicator.");

				// The glyph template-binds Foreground, so the override has to survive the binding too.
				var glyph = indicator.FindVisualChildByName("GlyphIcon") as FontIcon;
				if (glyph == null)
				{
					Verify.Fail("The SortIndicator template must contain the GlyphIcon FontIcon part.");
					return;
				}

				var glyphForeground = glyph.Foreground as SolidColorBrush;
				Verify.IsNotNull(glyphForeground, "GlyphIcon must template-bind the control's Foreground.");
				Verify.AreEqual(expected, glyphForeground.Color,
					"The glyph itself must render with the overridden foreground, not just the control property.");
			});
		}
		finally
		{
			// Application.Resources is UI-thread affine; a finally block runs on the test thread.
			RunOnUIThread.Execute(() => ClearApplicationResource("SortIndicatorForeground"));
		}
	}

	// TableView-dev-spec.md:176 states values "re-resolve across theme (and high-contrast) changes".
	//
	// The per-instance resource cache is real and deliberate - density metrics and the gridline
	// brush are both cached - so this is the specific risk the invalidation path exists to cover,
	// not a hypothetical. Without it a table stays light-themed inside a dark app until recreated.
	[TestMethod]
	public async Task VerifyThemeChangeAfterLoadReResolvesRowsAndHeaders()
	{
		TableView tableView = null;
		Grid host = null;
		Color lightGridLine = default;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			host = new Grid { RequestedTheme = ElementTheme.Light };
			host.Children.Add(tableView);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			lightGridLine = RequireCellSeparatorColor(tableView, "in Light");

			Verify.AreEqual(Color.FromArgb(0x29, 0x00, 0x00, 0x00), lightGridLine,
				"The light-theme gridline must resolve to #29000000 before the theme switch, or the rest of this test proves nothing.");

			host.RequestedTheme = ElementTheme.Dark;
			host.UpdateLayout();
		});

		// A theme change reaches realized rows one full layout + idle pass after the property
		// changes, so settle twice.
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			host.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var darkGridLine = RequireCellSeparatorColor(tableView, "in Dark");

			Verify.AreNotEqual(lightGridLine, darkGridLine,
				"The gridline brush must change with the theme; an unchanged value means the per-instance resource cache " +
				"was not invalidated on the theme change.");
			Verify.AreEqual(Color.FromArgb(0x29, 0xFF, 0xFF, 0xFF), darkGridLine,
				"After switching to Dark the gridline must re-resolve to the dark TabularSurfaceGridLineBrush.");
		});
	}

	// Adopted from PR !15971489's SortIndicator_ThemeSwitchMidState, which has it as an interaction
	// test. Written here as an API test because both triggers are programmatic.
	//
	// Spec is silent on this - it pins robustness, not a stated contract - so treat a failure as a
	// real finding but expect a spec conversation with it.
	[TestMethod]
	public async Task VerifyThemeChangeDuringSortIndicatorTransitionIsCoherent()
	{
		SortIndicator indicator = null;
		Grid host = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			indicator = new SortIndicator();
			host = new Grid { RequestedTheme = ElementTheme.Light };
			host.Children.Add(indicator);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Both in the same turn, before the visual-state transition started by the first has any
			// chance to complete. That interleaving is the whole point of the test.
			indicator.Direction = SortIndicatorDirection.Descending;
			host.RequestedTheme = ElementTheme.Dark;
			host.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			host.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortIndicatorDirection.Descending, indicator.Direction,
				"The interrupted transition must leave the final direction intact.");

			var layoutRoot = indicator.FindVisualChildByName("LayoutRoot") as FrameworkElement;
			if (layoutRoot == null)
			{
				Verify.Fail("The SortIndicator template must contain the LayoutRoot part whose Opacity the control drives.");
				return;
			}

			Verify.AreEqual(1.0, layoutRoot.Opacity,
				"A sorted indicator must end fully opaque. A value between 0 and 1 means the transition was stranded " +
				"mid-animation by the theme change.");

			var glyph = indicator.FindVisualChildByName("GlyphIcon") as FontIcon;
			if (glyph == null)
			{
				Verify.Fail("The SortIndicator template must contain the GlyphIcon part.");
				return;
			}

			Verify.IsFalse(string.IsNullOrEmpty(glyph.Glyph),
				"The glyph must still be set after the interrupted transition.");

			var foreground = indicator.Foreground as SolidColorBrush;
			Verify.IsNotNull(foreground,
				"The indicator must still carry a resolved foreground brush after the theme change - a null brush renders nothing.");
		});
	}

	#endregion

	#region 13.3 Density

	// Density is one of the better-specified areas in the control: the presets are literal values in
	// TableView_themeresources.xaml rather than constants in code.
	//
	// Table-driven over all three values deliberately. The row's own Style carries a MinHeight
	// setter of 40, so if the density value stopped being applied as a local value, Standard would
	// keep passing and only Compact and Comfortable would fail.
	[TestMethod]
	public async Task VerifyRowMinHeightFollowsDensity()
	{
		foreach (var testCase in DensityCases)
		{
			TableView tableView = null;

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateThemingTable();
				tableView.Density = testCase.Density;
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
				if (row == null)
				{
					Verify.Fail($"The TableView should realize at least one row at Density={testCase.Density}.");
					return;
				}

				Verify.AreEqual(testCase.RowMinHeight, row.MinHeight,
					$"Density={testCase.Density} must give rows a MinHeight of {testCase.RowMinHeight}, from " +
					$"TableView_themeresources.xaml. A value of 40 here means the density metric is not being applied " +
					"as a local value and the style's own setter is winning.");
			});
		}
	}

	// TableView-dev-spec.md:83 states it directly: "Header cells use the density row minimum height,
	// so the header band matches body rows." Most visible at Compact, where the difference is 10px.
	[TestMethod]
	public async Task VerifyHeaderBandHeightFollowsDensity()
	{
		foreach (var testCase in DensityCases)
		{
			TableView tableView = null;

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateThemingTable();
				tableView.Density = testCase.Density;
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var headerCell = GetHeaderCell(tableView, 0);
				if (headerCell == null)
				{
					return;
				}

				Verify.AreEqual(testCase.RowMinHeight, headerCell.MinHeight,
					$"Density={testCase.Density} must give header cells the same MinHeight as body rows " +
					$"({testCase.RowMinHeight}), so the header band lines up with the grid it labels.");

				var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
				if (row == null)
				{
					Verify.Fail($"The TableView should realize at least one row at Density={testCase.Density}.");
					return;
				}

				Verify.AreEqual(row.MinHeight, headerCell.MinHeight,
					"Header cells and body rows must agree on height at every density.");
			});
		}
	}

	// Horizontal padding is identical across all three densities by design - density is vertical
	// only - so a test asserting horizontal change would be asserting a bug.
	[TestMethod]
	public async Task VerifyCellAndHeaderPaddingFollowDensity()
	{
		foreach (var testCase in DensityCases)
		{
			TableView tableView = null;

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateThemingTable();
				tableView.Density = testCase.Density;
				// Sortable headers add the sort indicator's width to their trailing padding; this
				// test is about density padding, so compare against an unsortable header.
				tableView.Columns[0].CanSort = false;
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var cellText = RequireFirstCellTextBlock(tableView);
				if (cellText == null)
				{
					return;
				}

				Verify.AreEqual(testCase.CellPadding, cellText.Padding,
					$"Density={testCase.Density} must give generated text cells a Padding of {testCase.CellPadding}. " +
					"Without it rows grow taller but text stays pinned to the top, so Comfortable buys empty space " +
					"instead of breathing room.");

				var headerPresenter = GetHeaderPresenter(tableView, 0);
				Verify.AreEqual(testCase.CellPadding, headerPresenter.Padding,
					$"Density={testCase.Density} must give header cells the same padding as body cells.");
			});
		}
	}

	// Distinct from the tests above, which set density before load. Without this, density applies
	// only to rows realized AFTER the change, so a table that has already rendered shows a mix of
	// densities as the user scrolls.
	[TestMethod]
	public async Task VerifyDensityChangeAfterLoadUpdatesRealizedRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			Verify.AreEqual(TableViewDensity.Standard, tableView.Density,
				"This test starts from the Standard default.");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		TableViewRow realizedRow = null;

		RunOnUIThread.Execute(() =>
		{
			realizedRow = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
			if (realizedRow == null)
			{
				Verify.Fail("The TableView should realize at least one row before the density change.");
				return;
			}

			Verify.AreEqual(40.0, realizedRow.MinHeight, "The row should start at the Standard metric.");

			tableView.Density = TableViewDensity.Compact;
			tableView.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowAfter = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
			if (rowAfter == null)
			{
				Verify.Fail("The TableView should still have a realized row after the density change.");
				return;
			}

			Verify.AreEqual(realizedRow, rowAfter,
				"The density change should update the existing row rather than force a rebuild.");
			Verify.AreEqual(30.0, rowAfter.MinHeight,
				"An already-realized row must pick up the Compact metric; otherwise the table shows a mix of densities as the user scrolls.");

			var cellText = RequireFirstCellTextBlock(tableView);
			if (cellText == null)
			{
				return;
			}

			Verify.AreEqual(new Thickness(8, 2, 8, 2), cellText.Padding,
				"An already-realized cell must pick up the Compact padding.");
		});
	}

	// The only test that can tell the difference between the resource and the C++ fallback constant:
	// every other density test passes identically either way, because the two agree
	// (TableView.cpp DensityRowMinHeightFallback mirrors TableView_themeresources.xaml).
	//
	// Also guards the cache - override before load, since the resolved value is cached per instance.
	[TestMethod]
	[TestProperty("Ignore", "True")] // Product bug: LookupElementResource resolves merged theme dictionaries before app-scope keys, so app overrides lose. Re-enable with the lookup-precedence fix.
	[Ignore("WinUI ignores this test: Product bug: LookupElementResource resolves merged theme dictionaries before app-scope keys, so app overrides lose. Re-enable with the lookup-precedence fix.")]
	public async Task VerifyDensityResourceOverrideWins()
	{
		const double overriddenMinHeight = 71.0;
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TableViewRowMinHeightCompact", overriddenMinHeight);

			tableView = CreateThemingTable();
			tableView.Density = TableViewDensity.Compact;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
				if (row == null)
				{
					Verify.Fail("The TableView should realize at least one row.");
					return;
				}

				Verify.AreEqual(overriddenMinHeight, row.MinHeight,
					"An app-scope TableViewRowMinHeightCompact override must win. A value of 30 here means the metric came " +
					"from the C++ fallback constant and the resource key is decorative.");
			});
		}
		finally
		{
			// Application.Resources is UI-thread affine; a finally block runs on the test thread.
			RunOnUIThread.Execute(() => ClearApplicationResource("TableViewRowMinHeightCompact"));
		}
	}

	// A negative test, and new relative to PR !15971489. The product states the exclusion as
	// deliberate (TableView.h:62: font sizes resolve from fixed, non-density-suffixed keys and are
	// kept out of the density cache "to avoid implying otherwise"), and TableView-dev-spec.md:215
	// scopes Density to "row min-height + built-in cell/header padding".
	//
	// If someone adds a density suffix to the font keys, Compact starts shrinking text - a
	// legibility regression that would read as intentional to a reviewer.
	[TestMethod]
	public async Task VerifyFontSizeDoesNotVaryWithDensity()
	{
		const double expectedFontSize = 14.0;

		foreach (var testCase in DensityCases)
		{
			TableView tableView = null;

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateThemingTable();
				tableView.Density = testCase.Density;
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var cellText = RequireFirstCellTextBlock(tableView);
				if (cellText == null)
				{
					return;
				}

				Verify.AreEqual(expectedFontSize, cellText.FontSize,
					$"Cell font size must stay {expectedFontSize} at Density={testCase.Density}: TableViewCellFontSize has no " +
					"density variants, and density is specified as row height and padding only.");

				var headerPresenter = GetHeaderPresenter(tableView, 0);
				Verify.AreEqual(expectedFontSize, headerPresenter.FontSize,
					$"Header font size must stay {expectedFontSize} at Density={testCase.Density}.");
			});
		}
	}

	#endregion
}

internal static class TableViewThemingTestHelpers
{
	internal struct DensityCase
	{
		internal TableViewDensity Density;
		internal double RowMinHeight;
		internal Thickness CellPadding;
	}

	// The literal presets from TableView_themeresources.xaml, not from the code constants. They
	// agree today; VerifyDensityResourceOverrideWins is what proves the resource is the real source.
	internal static readonly DensityCase[] DensityCases = new[]
	{
		new DensityCase { Density = TableViewDensity.Compact, RowMinHeight = 30.0, CellPadding = new Thickness(8, 2, 8, 2) },
		new DensityCase { Density = TableViewDensity.Standard, RowMinHeight = 40.0, CellPadding = new Thickness(8, 4, 8, 4) },
		new DensityCase { Density = TableViewDensity.Comfortable, RowMinHeight = 48.0, CellPadding = new Thickness(8, 8, 8, 8) },
	};

	internal static TableView CreateThemingTable()
		=> CreateTableView(headers: new[] { "Name", "Role" });

	internal static TableView CreateGroupedThemingTable()
	{
		var tableView = CreateThemingTable();
		tableView.Height = 400;
		tableView.ItemsSource = TableViewSource.From(MakeItems()).GroupBy(item => (object)((Person)item).Role);
		return tableView;
	}

	// Every key in a dictionary, including its own merged dictionaries. A key nested in a merged
	// dictionary resolves exactly like a top-level one, so parity has to account for both.
	internal static HashSet<string> CollectKeys(ResourceDictionary dictionary)
	{
		var keys = new HashSet<string>(StringComparer.Ordinal);
		CollectKeysCore(dictionary, keys);
		return keys;
	}

	private static void CollectKeysCore(ResourceDictionary dictionary, HashSet<string> keys)
	{
		foreach (var key in dictionary.Keys)
		{
			if (key is string name)
			{
				keys.Add(name);
			}
		}

		foreach (var merged in dictionary.MergedDictionaries)
		{
			CollectKeysCore(merged, keys);
		}
	}

	// SystemColor* resources are framework-provided and do not appear when enumerating
	// Application.Current.Resources.Keys, so they have to be asked for by name. This is the full
	// set the high-contrast palette draws on.
	private static readonly string[] SystemColorNames = new[]
	{
		"SystemColorWindowColor",
		"SystemColorWindowTextColor",
		"SystemColorButtonFaceColor",
		"SystemColorButtonTextColor",
		"SystemColorHighlightColor",
		"SystemColorHighlightTextColor",
		"SystemColorHotlightColor",
		"SystemColorGrayTextColor",
		"SystemColorCaptionTextColor",
		"SystemColorActiveCaptionColor",
		"SystemColorBackgroundColor",
	};

	internal static HashSet<Color> CollectSystemColors()
	{
		var colors = new HashSet<Color>();

		foreach (var name in SystemColorNames)
		{
			if (!Application.Current.Resources.ContainsKey(name))
			{
				continue;
			}

			var value = Application.Current.Resources[name];
			if (value is Color color)
			{
				colors.Add(color);
			}
			else if (value is SolidColorBrush brush)
			{
				colors.Add(brush.Color);
			}

			var brushName = name + "Brush";
			if (Application.Current.Resources.ContainsKey(brushName) &&
				Application.Current.Resources[brushName] is SolidColorBrush namedBrush)
			{
				colors.Add(namedBrush.Color);
			}
		}

		return colors;
	}

	internal static SolidColorBrush RequireBrush(ResourceDictionary resources, string themeName, string key)
	{
		var themeDictionary = resources.ThemeDictionaries[themeName] as ResourceDictionary;
		if (themeDictionary == null)
		{
			Verify.Fail($"TabularControlsResources must declare a '{themeName}' theme dictionary.");
			return null;
		}

		if (!themeDictionary.ContainsKey(key))
		{
			Verify.Fail($"'{key}' must be defined in the '{themeName}' theme dictionary.");
			return null;
		}

		var brush = themeDictionary[key] as SolidColorBrush;
		if (brush == null)
		{
			Verify.Fail($"'{key}' in '{themeName}' must be a SolidColorBrush.");
			return null;
		}

		return brush;
	}

	// Written directly into Application.Current.Resources, which standard XAML precedence looks up
	// before its own theme dictionaries and merged dictionaries - so this is the app-scope override
	// an app author writes.
	internal static void SetApplicationResource(string key, object value)
	{
		Application.Current.Resources[key] = value;
	}

	internal static void ClearApplicationResource(string key)
	{
		if (Application.Current.Resources.ContainsKey(key))
		{
			Application.Current.Resources.Remove(key);
		}
	}

	// The TextBlock TableViewTextColumn generates, which is where cell padding and font size land.
	internal static TextBlock RequireFirstCellTextBlock(TableView tableView)
	{
		var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
		if (row == null)
		{
			Verify.Fail("The TableView should realize at least one row.");
			return null;
		}

		return GetCellTextBlock(GetRowCell(row, 0));
	}

	internal static Color RequireCellSeparatorColor(TableView tableView, string context)
	{
		var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
		if (row == null)
		{
			Verify.Fail($"The TableView should realize at least one row ({context}).");
			return default;
		}

		var wrapper = GetRowCell(row, 0);
		var brush = wrapper.BorderBrush as SolidColorBrush;
		if (brush == null)
		{
			Verify.Fail($"A cell wrapper should carry a SolidColorBrush vertical separator ({context}).");
			return default;
		}

		return brush.Color;
	}
}
