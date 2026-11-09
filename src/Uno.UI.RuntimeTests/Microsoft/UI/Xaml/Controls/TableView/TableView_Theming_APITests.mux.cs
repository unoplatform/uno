// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Theming_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
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
// Four dictionaries own distinct things, and a test that looks in the wrong one finds nothing:
//
//   * CommonStyles\TabularSurfaces_themeresources.xaml - the brush palette. Every TabularSurface*
//     key, in Default / Light / HighContrast. The canonical source.
//   * TableView\TableView_themeresources.xaml - the metric tokens ONLY: row heights, font sizes,
//     cell and header padding, the density variants, gripper width. Brushes are deliberately
//     excluded; mirroring them would collide on duplicate keys during theme-XBF emission.
//   * ResizeGripper\ResizeGripper_themeresources.xaml - the gripper's own styling keys
//     (ResizeGripperSeparator*), so a gripper resolves them without TableView.
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
			Verify.AreEqual(new Thickness(0, 0, 0, 1), row.BorderThickness,
				"TableViewRow's default style sets BorderThickness to 0,0,0,1, the bottom grid line the default GridLinesVisibility keeps.");

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
			Verify.AreEqual(new Thickness(8, 8, 12, 8), groupHeader.Padding,
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
				// Color="{ThemeResource SystemColor*}". SortIndicatorForeground is a StaticResource
				// alias to SystemColorWindowTextColorBrush; a StaticResource inside a theme
				// dictionary is resolved when that dictionary loads, so reading one off a detached
				// TabularControlsResources yields whichever sibling theme XAML happened to bind - an
				// artifact of the read, not the value a high-contrast user would see.
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

	// Values mirror Common_themeresources_any.xaml.
	[TestMethod]
	public void VerifyGridLineAndOuterBorderBrushesMatchFluentStrokeTokens()
	{
		RunOnUIThread.Execute(() =>
		{
			var resources = new TabularControlsResources();

			var dark = RequireBrush(resources, "Default", "TabularSurfaceGridLineBrush");
			var light = RequireBrush(resources, "Light", "TabularSurfaceGridLineBrush");
			var darkBorder = RequireBrush(resources, "Default", "TabularSurfaceOuterBorderBrush");
			var lightBorder = RequireBrush(resources, "Light", "TabularSurfaceOuterBorderBrush");
			var darkHeader = RequireBrush(resources, "Default", "TabularSurfaceHeaderSeparatorBrush");
			var lightHeader = RequireBrush(resources, "Light", "TabularSurfaceHeaderSeparatorBrush");
			if (dark == null || light == null || darkBorder == null || lightBorder == null || darkHeader == null || lightHeader == null)
			{
				return;
			}

			Verify.AreEqual(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF), dark.Color,
				"Dark TabularSurfaceGridLineBrush must be DividerStrokeColorDefault (#15FFFFFF).");
			Verify.AreEqual(Color.FromArgb(0x0F, 0x00, 0x00, 0x00), light.Color,
				"Light TabularSurfaceGridLineBrush must be DividerStrokeColorDefault (#0F000000).");
			Verify.AreEqual(Color.FromArgb(0x19, 0x00, 0x00, 0x00), darkBorder.Color,
				"Dark TabularSurfaceOuterBorderBrush must be CardStrokeColorDefault (#19000000).");
			Verify.AreEqual(Color.FromArgb(0x0F, 0x00, 0x00, 0x00), lightBorder.Color,
				"Light TabularSurfaceOuterBorderBrush must be CardStrokeColorDefault (#0F000000).");
			Verify.AreEqual(dark.Color, darkHeader.Color, "Dark header lines must match the body grid lines (DividerStrokeColorDefault).");
			Verify.AreEqual(light.Color, lightHeader.Color, "Light header lines must match the body grid lines (DividerStrokeColorDefault).");
		});
	}

	[TestMethod]
	public async Task VerifyOuterBorderAndHeaderRuleUseTheirOwnBrushes()
	{
		var border = Color.FromArgb(0xFF, 0x20, 0x40, 0xC0);
		var gridLine = Color.FromArgb(0xFF, 0xC0, 0x40, 0x20);
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceOuterBorderBrush", new SolidColorBrush(border));
			SetApplicationResource("TabularSurfaceHeaderSeparatorBrush", new SolidColorBrush(gridLine));

			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(border, (tableView.BorderBrush as SolidColorBrush)?.Color ?? default,
					"The outer border must come from TabularSurfaceOuterBorderBrush.");

				var headerRow = tableView.FindVisualChildByName("PART_HeaderRow") as Border;
				Verify.IsNotNull(headerRow, "The template must contain PART_HeaderRow.");
				Verify.AreEqual(gridLine, (headerRow?.BorderBrush as SolidColorBrush)?.Color ?? default,
					"The header bottom rule must come from TabularSurfaceHeaderSeparatorBrush.");
			});
		}
		finally
		{
			RunOnUIThread.Execute(() =>
			{
				ClearApplicationResource("TabularSurfaceOuterBorderBrush");
				ClearApplicationResource("TabularSurfaceHeaderSeparatorBrush");
			});
		}
	}

	// TableView-dev-spec.md:227-231 makes overriding a TabularSurface* key THE supported way to
	// restyle the table, in place of a monolithic style. If the control caches or hardcodes the
	// brush instead of resolving it, that story is fiction.
	//
	// The gridline brush is held in the per-instance resource cache, so override BEFORE load;
	// overriding afterwards tests the invalidation path, which is
	// VerifyThemeChangeAfterLoadReResolvesRowsAndHeaders' job.
	[TestMethod]
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

	[TestMethod]
	public async Task VerifyHeaderGridLinesShowWhateverTheBodyGridLines()
	{
		var separatorColor = Color.FromArgb(0xFF, 0x12, 0x9E, 0x5A);
		var cases = new[]
		{
			TableViewGridLinesVisibility.None,
			TableViewGridLinesVisibility.Horizontal,
			TableViewGridLinesVisibility.Vertical,
			TableViewGridLinesVisibility.All,
		};

		TableView tableView = null;
		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceHeaderSeparatorBrush", new SolidColorBrush(separatorColor));

			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			foreach (var body in cases)
			{
				RunOnUIThread.Execute(() =>
				{
					tableView.GridLinesVisibility = body;
					Content.UpdateLayout();
				});

				await TestServices.WindowHelper.WaitForIdle();

				RunOnUIThread.Execute(() =>
				{
					var context = $"body {body}";
					bool bodyVertical = body == TableViewGridLinesVisibility.All || body == TableViewGridLinesVisibility.Vertical;
					bool bodyHorizontal = body == TableViewGridLinesVisibility.All || body == TableViewGridLinesVisibility.Horizontal;

					var separator = GetHeaderCell(tableView, 0).FindVisualChildByName("PART_GridLine") as Border;
					if (separator == null)
					{
						Verify.Fail("Every header cell must contain its separator.");
						return;
					}

					Verify.AreEqual(Visibility.Visible, separator.Visibility,
						$"The non-terminal header separator is always shown ({context}).");
					Verify.AreEqual(VerticalAlignment.Stretch, separator.VerticalAlignment, "Header separators are full height.");
					Verify.AreEqual(separatorColor, (separator.Background as SolidColorBrush)?.Color ?? default,
						"Header separators paint TabularSurfaceHeaderSeparatorBrush.");

					var headerRow = tableView.FindVisualChildByName("PART_HeaderRow") as Border;
					Verify.IsNotNull(headerRow, "The template must contain PART_HeaderRow.");
					if (headerRow != null)
					{
						Verify.IsTrue(headerRow.BorderThickness.Bottom > 0.0, $"The header bottom rule is always shown ({context}).");
						Verify.AreEqual(separatorColor, (headerRow.BorderBrush as SolidColorBrush)?.Color ?? default,
							"The header bottom rule paints TabularSurfaceHeaderSeparatorBrush.");
					}

					var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
					if (row == null)
					{
						Verify.Fail("The TableView should realize at least one row.");
						return;
					}

					Verify.AreEqual(bodyHorizontal, row.BorderThickness.Bottom > 0.0,
						$"Row dividers must follow GridLinesVisibility ({context}).");
					Verify.AreEqual(bodyVertical, GetRowCell(row, 0).BorderThickness.Right > 0.0,
						$"Body column separators must follow GridLinesVisibility ({context}).");
				});
			}
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceHeaderSeparatorBrush"));
		}
	}

	[TestMethod]
	public async Task VerifyTransparentHeaderSeparatorBrushHidesHeaderLines()
	{
		var transparent = Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF);
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceHeaderSeparatorBrush", new SolidColorBrush(transparent));

			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				var separator = GetHeaderCell(tableView, 0).FindVisualChildByName("PART_GridLine") as Border;
				Verify.IsNotNull(separator, "Every header cell must contain its separator.");
				Verify.AreEqual(transparent, (separator?.Background as SolidColorBrush)?.Color ?? default,
					"Overriding TabularSurfaceHeaderSeparatorBrush with Transparent must hide the header separators.");

				var headerRow = tableView.FindVisualChildByName("PART_HeaderRow") as Border;
				Verify.IsNotNull(headerRow, "The template must contain PART_HeaderRow.");
				Verify.AreEqual(transparent, (headerRow?.BorderBrush as SolidColorBrush)?.Color ?? default,
					"Overriding TabularSurfaceHeaderSeparatorBrush with Transparent must hide the header bottom rule.");
			});
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceHeaderSeparatorBrush"));
		}
	}

	[TestMethod]
	public async Task VerifyGridLineBrushAppOverrideWinsInsideElementTheme()
	{
		var expected = Color.FromArgb(0xFF, 0x66, 0x22, 0xAA);
		TableView tableView = null;
		Grid host = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceGridLineBrush", new SolidColorBrush(expected));

			tableView = CreateThemingTable();
			host = new Grid { RequestedTheme = ElementTheme.Light };
			host.Children.Add(tableView);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				var actual = RequireCellSeparatorColor(tableView, "with an app override inside a Light subtree");

				Verify.AreEqual(expected, actual,
					"A direct app-scope TabularSurfaceGridLineBrush override must win even when the table is inside an element-scoped theme.");
			});
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceGridLineBrush"));
		}
	}

	[TestMethod]
	public async Task VerifyElementThemeDictionaryBeatsFartherApplicationResource()
	{
		var fartherAppColor = Color.FromArgb(0xFF, 0x66, 0x22, 0xAA);
		var nearerElementThemeColor = Color.FromArgb(0xFF, 0x11, 0xCC, 0x77);
		TableView tableView = null;
		Grid host = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceGridLineBrush", new SolidColorBrush(fartherAppColor));

			tableView = CreateThemingTable();
			host = new Grid { RequestedTheme = ElementTheme.Light };
			host.Resources = new ResourceDictionary();
			host.Resources.ThemeDictionaries["Light"] = new ResourceDictionary
			{
				["TabularSurfaceGridLineBrush"] = new SolidColorBrush(nearerElementThemeColor),
			};
			host.Children.Add(tableView);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				var actual = RequireCellSeparatorColor(tableView, "with a nearer element theme dictionary and a farther app resource");

				Verify.AreEqual(nearerElementThemeColor, actual,
					"A nearer element-scoped ThemeDictionaries entry must win over a farther plain Application.Resources entry.");
			});
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceGridLineBrush"));
		}
	}

	// Framework order is own entry, then MergedDictionaries last-to-first. An own entry that is the
	// same object as an EARLIER (non-winning) merged entry must still be own; deciding own-ness by
	// "matches any merged entry" would skip it and return the winning merged entry instead.
	[TestMethod]
	public async Task VerifyOwnEntryBeatsMergedEntryEvenWhenAnotherMergedEntryMatches()
	{
		var ownColor = Color.FromArgb(0xFF, 0x11, 0xAA, 0x55);
		var winningMergedColor = Color.FromArgb(0xFF, 0xAA, 0x33, 0x11);
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			var ownBrush = new SolidColorBrush(ownColor);
			tableView = CreateThemingTable();
			var host = new Grid();
			host.Resources = new ResourceDictionary { ["TabularSurfaceGridLineBrush"] = ownBrush };
			host.Resources.MergedDictionaries.Add(new ResourceDictionary { ["TabularSurfaceGridLineBrush"] = ownBrush });
			host.Resources.MergedDictionaries.Add(new ResourceDictionary { ["TabularSurfaceGridLineBrush"] = new SolidColorBrush(winningMergedColor) });
			host.Children.Add(tableView);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var actual = RequireCellSeparatorColor(tableView, "with an own entry shared by a non-winning merged dictionary");
			Verify.AreEqual(ownColor, actual,
				"An own ResourceDictionary entry must win over MergedDictionaries, even when an earlier merged dictionary holds the same object.");
		});
	}

	// Under Dark the framework picks ThemeDictionaries["Dark"] ahead of ["Default"], per element.
	// The differing "Default" entry stops a lookup that only probes "Default" from passing.
	[TestMethod]
	public async Task VerifyElementDarkThemeDictionaryOverrideWins()
	{
		var darkColor = Color.FromArgb(0xFF, 0x22, 0x88, 0xDD);
		var defaultColor = Color.FromArgb(0xFF, 0xDD, 0x44, 0x11);
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			tableView = CreateThemingTable();
			var host = new Grid { RequestedTheme = ElementTheme.Dark };
			host.Resources = new ResourceDictionary();
			host.Resources.ThemeDictionaries["Dark"] = new ResourceDictionary
			{
				["TabularSurfaceGridLineBrush"] = new SolidColorBrush(darkColor),
			};
			host.Resources.ThemeDictionaries["Default"] = new ResourceDictionary
			{
				["TabularSurfaceGridLineBrush"] = new SolidColorBrush(defaultColor),
			};
			host.Children.Add(tableView);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(ElementTheme.Dark, tableView.ActualTheme, "Precondition: the table must be in a Dark subtree.");

			var actual = RequireCellSeparatorColor(tableView, "with an element-scoped Dark theme dictionary");
			Verify.AreEqual(darkColor, actual,
				"Under Dark, an ancestor's ThemeDictionaries[\"Dark\"] entry must win over its [\"Default\"] entry and the TableView default.");
		});
	}

	[TestMethod]
	public async Task VerifyGridLineBrushSameColorOverrideKeepsObjectAndOpacity()
	{
		var themeColor = Color.FromArgb(0x0F, 0x00, 0x00, 0x00);
		SolidColorBrush expectedBrush = null;
		TableView tableView = null;
		Grid host = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			expectedBrush = new SolidColorBrush(themeColor) { Opacity = 0.42 };
			SetApplicationResource("TabularSurfaceGridLineBrush", expectedBrush);

			tableView = CreateThemingTable();
			host = new Grid { RequestedTheme = ElementTheme.Light };
			host.Children.Add(tableView);

			LoadContent(host);
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

				Verify.AreEqual(themeColor, brush.Color,
					"The override deliberately uses the same color as the Light theme default.");
				Verify.AreEqual(expectedBrush.Opacity, brush.Opacity,
					"A direct app-scope brush override must not be discarded just because its Color equals the theme default.");
				Verify.AreSame(expectedBrush, brush,
					"The rendered separator must keep the exact app-scope brush object so non-color brush properties are preserved.");
			});
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceGridLineBrush"));
		}
	}

	// TableView-dev-spec.md:123 names both keys as part of what "the primitive owns", which makes
	// them a published surface rather than an internal detail. If they are inert a host cannot match
	// the separator to its design language.
	[TestMethod]
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

	[TestMethod]
	public async Task VerifyResizeGripperSeparatorShowsOnlyWhileDragging()
	{
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			gripper = new ResizeGripper { Width = 8, Height = 24 };
			LoadContent(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var separator = gripper.FindVisualChildByName("Separator") as Border;
			if (separator == null)
			{
				Verify.Fail("The ResizeGripper template must contain the 'Separator' part its visual states target by name.");
				return;
			}

			Verify.AreEqual(separator, VisualTreeHelper.GetChild(gripper, 0),
				"'Separator' must be the template root.");
			Verify.IsNull(separator.Child, "The separator Border must not wrap another element.");
			Verify.IsNotNull(separator.Background, "A null Background would stop the gripper from hit-testing.");

			var rest = separator.BorderBrush as SolidColorBrush;
			Verify.IsTrue(rest != null && rest.Color.A == 0, "The separator must be hidden in the Normal state.");

			VisualStateManager.GoToState(gripper, "PointerOver", false);
			var hover = separator.BorderBrush as SolidColorBrush;
			Verify.IsTrue(hover != null && hover.Color.A == 0, "Hover must not draw the separator.");

			VisualStateManager.GoToState(gripper, "Pressed", false);
			Verify.AreEqual(gripper.BorderBrush, separator.BorderBrush,
				"The Pressed state must show the separator with the gripper's BorderBrush.");

			VisualStateManager.GoToState(gripper, "Normal", false);
			rest = separator.BorderBrush as SolidColorBrush;
			Verify.IsTrue(rest != null && rest.Color.A == 0, "Returning to Normal must hide the separator again.");
		});
	}

	[TestMethod]
	public void VerifyResizeGripperDefaultSeparatorBrushesResolve()
	{
		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			var resources = new TabularControlsResources();

			var expectedLight = RequireThemeColor("Light", "ControlStrongStrokeColorDefault");
			var expectedDark = RequireThemeColor("Default", "ControlStrongStrokeColorDefault");
			Verify.AreNotEqual(expectedLight, expectedDark,
				"ControlStrongStrokeColorDefault must differ between Light and Dark, or a swap could not be detected.");

			var light = RequireBrush(resources, "Light", "ResizeGripperSeparatorBrush");
			Verify.AreEqual(expectedLight, light.Color,
				"The Light ResizeGripperSeparatorBrush must be the neutral ControlStrongStrokeColorDefault.");

			var dark = RequireBrush(resources, "Default", "ResizeGripperSeparatorBrush");
			Verify.AreEqual(expectedDark, dark.Color,
				"The Dark ResizeGripperSeparatorBrush must be the neutral ControlStrongStrokeColorDefault.");

			var accents = new[] { "SystemAccentColorDark1", "SystemAccentColorLight2" }
				.Select(RequireApplicationColor).ToList();
			Verify.IsFalse(accents.Contains(light.Color) || accents.Contains(dark.Color),
				"The gripper separator must not use an accent colour.");

			var highContrast = RequireBrush(resources, "HighContrast", "ResizeGripperSeparatorBrush");
			var systemColors = CollectSystemColors();
			Verify.IsGreaterThan(systemColors.Count, 0,
				"SystemColor* resources must resolve from the application resources before validating HighContrast.");
			Verify.IsTrue(systemColors.Contains(highContrast.Color),
				"The HighContrast default ResizeGripperSeparatorBrush must resolve through a system colour, not a hardcoded accent or cross-dictionary StaticResource alias.");
		});
	}

	[TestMethod]
	public void VerifySelectionIndicatorBrushFollowsAccentFillTiers()
	{
		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			var resources = new TabularControlsResources();

			var expectedLight = RequireApplicationColor("SystemAccentColorDark1");
			var expectedDark = RequireApplicationColor("SystemAccentColorLight2");
			Verify.AreNotEqual(expectedLight, expectedDark,
				"SystemAccentColorDark1 and SystemAccentColorLight2 must differ, or a Light/Dark swap could not be detected.");

			Verify.AreEqual(expectedLight, RequireBrush(resources, "Light", "TabularSurfaceSelectionIndicatorBrush").Color,
				"The Light TabularSurfaceSelectionIndicatorBrush must follow AccentFillColorDefaultBrush's Light tier, SystemAccentColorDark1.");
			Verify.AreEqual(expectedDark, RequireBrush(resources, "Default", "TabularSurfaceSelectionIndicatorBrush").Color,
				"The Default/Dark TabularSurfaceSelectionIndicatorBrush must follow AccentFillColorDefaultBrush's Dark tier, SystemAccentColorLight2.");

			// Unread by the control: header text inherits the row foreground.
			foreach (var themeName in new[] { "Default", "Light", "HighContrast" })
			{
				var dictionary = (ResourceDictionary)resources.ThemeDictionaries[themeName];
				Verify.IsFalse(dictionary.ContainsKey("TabularSurfaceHeaderForegroundBrush"),
					$"{themeName} must not declare the unused TabularSurfaceHeaderForegroundBrush.");
				Verify.IsTrue(dictionary.ContainsKey("TabularSurfaceHeaderSeparatorBrush"),
					$"{themeName} must keep TabularSurfaceHeaderSeparatorBrush.");
			}
		});
	}

	[TestMethod]
	public void VerifyHeaderPointerStateBrushesResolveInEveryTheme()
	{
		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			var resources = new TabularControlsResources();

			foreach (var themeName in new[] { "Default", "Light" })
			{
				Verify.AreEqual(
					RequireBrush(resources, themeName, "TabularSurfaceRowBackgroundPointerOverBrush").Color,
					RequireBrush(resources, themeName, "TabularSurfaceHeaderBackgroundPointerOverBrush").Color,
					$"'{themeName}' header pointer-over fill must match the row pointer-over fill.");
				Verify.AreEqual(
					RequireBrush(resources, themeName, "TabularSurfaceRowBackgroundPressedBrush").Color,
					RequireBrush(resources, themeName, "TabularSurfaceHeaderBackgroundPressedBrush").Color,
					$"'{themeName}' header pressed fill must match the row pressed fill.");

				// Header text inherits TableView.Foreground (TabularSurfaceRowForegroundBrush); the
				// state keys only apply in High Contrast but must still match it elsewhere.
				var restForeground = RequireBrush(resources, themeName, "TabularSurfaceRowForegroundBrush").Color;
				Verify.AreEqual(restForeground,
					RequireBrush(resources, themeName, "TabularSurfaceHeaderForegroundPointerOverBrush").Color,
					$"'{themeName}' header text must keep its rest colour on hover.");
				Verify.AreEqual(restForeground,
					RequireBrush(resources, themeName, "TabularSurfaceHeaderForegroundPressedBrush").Color,
					$"'{themeName}' header text must keep its rest colour when pressed.");
			}

			var systemColors = CollectSystemColors();
			Verify.IsGreaterThan(systemColors.Count, 0,
				"SystemColor* resources must resolve from the application resources before validating HighContrast.");

			var highlight = RequireBrush(resources, "HighContrast", "TabularSurfaceRowBackgroundPointerOverBrush").Color;
			var highlightText = RequireBrush(resources, "HighContrast", "TabularSurfaceRowForegroundPointerOverBrush").Color;
			foreach (var state in new[] { "PointerOver", "Pressed" })
			{
				var background = RequireBrush(resources, "HighContrast", $"TabularSurfaceHeaderBackground{state}Brush").Color;
				var foreground = RequireBrush(resources, "HighContrast", $"TabularSurfaceHeaderForeground{state}Brush").Color;
				Verify.IsTrue(systemColors.Contains(background) && systemColors.Contains(foreground),
					$"HighContrast header {state} brushes must resolve through system colours.");
				Verify.AreEqual(highlight, background, $"HighContrast header {state} fill must be SystemColorHighlightColor.");
				Verify.AreEqual(highlightText, foreground, $"HighContrast header {state} text must be SystemColorHighlightTextColor.");
			}
		});
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

			Verify.AreEqual(Color.FromArgb(0x0F, 0x00, 0x00, 0x00), lightGridLine,
				"The light-theme gridline must resolve to #0F000000 before the theme switch, or the rest of this test proves nothing.");

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
			Verify.AreEqual(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF), darkGridLine,
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

	// In High Contrast every state below paints an opaque SystemColorHighlightColor fill that a
	// text backplate fights.
	[TestMethod]
	public async Task VerifyPointerOverAndPressedRowsSuppressHighContrastBackplate()
	{
		foreach (var state in new[] { "PointerOver", "Pressed", "Selected", "SelectedPointerOver", "SelectedPressed" })
		{
			TableView tableView = null;
			TableViewRow row = null;

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateThemingTable();
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				row = GetRealizedRows(tableView).FirstOrDefault();
				if (row == null)
				{
					Verify.Fail("The TableView should realize at least one row.");
					return;
				}

				Verify.IsTrue(VisualStateManager.GoToState(row, state, false), $"The row template must define the {state} state.");
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var presenter = row?.FindVisualChildByName("PART_CellForegroundPresenter") as ContentPresenter;
				if (presenter == null)
				{
					Verify.Fail("The row template must contain PART_CellForegroundPresenter.");
					return;
				}

				Verify.AreEqual(ElementHighContrastAdjustment.None, presenter.HighContrastAdjustment,
					$"The {state} state must turn off the High Contrast text backplate.");
			});
		}
	}

	[TestMethod]
	public async Task VerifyPointerOverAndPressedGroupHeadersSuppressHighContrastBackplate()
	{
		foreach (var state in new[] { "PointerOver", "Pressed" })
		{
			TableView tableView = null;
			TableViewGroupHeader groupHeader = null;

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateGroupedThemingTable();
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				groupHeader = FindVisualChildrenByType<TableViewGroupHeader>(tableView).FirstOrDefault();
				if (groupHeader == null)
				{
					Verify.Fail("A grouped TableView should realize at least one TableViewGroupHeader.");
					return;
				}

				Verify.IsTrue(VisualStateManager.GoToState(groupHeader, state, false), $"The group header template must define the {state} state.");
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				foreach (var partName in new[] { "PART_ContentPresenter", "PART_ExpanderIcon" })
				{
					var part = groupHeader?.FindVisualChildByName(partName) as UIElement;
					if (part == null)
					{
						Verify.Fail($"The group header template must contain {partName}.");
						return;
					}

					Verify.AreEqual(ElementHighContrastAdjustment.None, part.HighContrastAdjustment,
						$"The {state} state must turn off the High Contrast text backplate on {partName}.");
				}
			});
		}
	}

	[TestMethod]
	public async Task VerifyHighContrastHoveredAndPressedHeadersSuppressBackplate()
	{
		foreach (var state in new[] { "HighContrastPointerOver", "HighContrastPressed" })
		{
			TableView tableView = null;
			TableViewColumnHeader header = null;

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateThemingTable();
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				header = RequireColumnHeader(tableView, 0);
				Verify.IsTrue(VisualStateManager.GoToState(header, state, false), $"The header template must define the {state} state.");
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var presenter = header?.FindVisualChildByName("PART_ContentPresenter") as UIElement;
				if (presenter == null)
				{
					Verify.Fail("The header template must contain PART_ContentPresenter.");
					return;
				}

				Verify.AreEqual(ElementHighContrastAdjustment.None, presenter.HighContrastAdjustment,
					$"The {state} state must turn off the High Contrast text backplate on the header content.");
			});
		}
	}

	[TestMethod]
	public async Task VerifyDefaultGroupHeaderCountInheritsForegroundThatFollowsStates()
	{
		TableView tableView = null;
		TableViewGroupHeader groupHeader = null;
		TextBlock count = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceGroupHeaderCountOpacity", 0.5);

			tableView = CreateGroupedThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		Color PresenterColor() =>
			((groupHeader.FindVisualChildByName("PART_ContentPresenter") as ContentPresenter)?.Foreground as SolidColorBrush)?.Color ?? default;
		Color CountColor() => (count.Foreground as SolidColorBrush)?.Color ?? default;

		try
		{
			RunOnUIThread.Execute(() =>
			{
				groupHeader = FindVisualChildrenByType<TableViewGroupHeader>(tableView).FirstOrDefault();
				count = groupHeader?.FindVisualChildByName("TableViewGroupHeaderItemCountText") as TextBlock;
				if (count == null)
				{
					Verify.Fail("The default group header content must contain the item-count TextBlock.");
					return;
				}

				Verify.AreEqual(DependencyProperty.UnsetValue, count.ReadLocalValue(TextBlock.ForegroundProperty),
					"The count must inherit Foreground rather than hold its own brush.");
				Verify.AreEqual(0.5, count.Opacity, "The count must be dimmed with TabularSurfaceGroupHeaderCountOpacity.");
				Verify.AreEqual(PresenterColor(), CountColor(), "At rest the count must use the header's Foreground.");

				var contentPresenter = groupHeader.FindVisualChildByName("PART_ContentPresenter") as ContentPresenter;
				var key = contentPresenter == null ? null :
					FindVisualChildrenByType<TextBlock>(contentPresenter).FirstOrDefault(t => t != count);
				Verify.IsNotNull(key, "The default group header content must contain the key TextBlock.");
				Verify.AreEqual(DependencyProperty.UnsetValue, key?.ReadLocalValue(TextBlock.FontWeightProperty),
					"Key text weight must come from a style, not a local value.");
				Verify.AreEqual((ushort)600, key?.FontWeight.Weight ?? (ushort)0,
					"Key text must be SemiBold (BodyStrongTextBlockStyle equivalent).");
				Verify.AreEqual(TextWrapping.NoWrap, key?.TextWrapping ?? TextWrapping.Wrap, "Key text stays on one line.");

				Verify.IsTrue(VisualStateManager.GoToState(groupHeader, "PointerOver", false), "PointerOver must exist.");
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(PresenterColor(), CountColor(), "PointerOver must recolor the count with the key text.");
				Verify.IsTrue(VisualStateManager.GoToState(groupHeader, "Disabled", false), "Disabled must exist.");
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(PresenterColor(), CountColor(), "Disabled must give the count the same disabled brush as the key text.");
				Verify.IsTrue(VisualStateManager.GoToState(groupHeader, "Normal", false), "Normal must exist.");
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() => Verify.AreEqual(PresenterColor(), CountColor(),
				"Returning to Normal must restore the rest brush."));
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceGroupHeaderCountOpacity"));
		}
	}

	[TestMethod]
	public async Task VerifyDisabledGroupHeaderCountIsNotDimmedTwice()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceGroupHeaderCountOpacity", 0.5);

			tableView = CreateGroupedThemingTable();
			tableView.IsEnabled = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		TableViewGroupHeader header = null;
		TextBlock Count() => header?.FindVisualChildByName("TableViewGroupHeaderItemCountText") as TextBlock;

		// IsEnabledChanged is raised asynchronously; the header's own handler was attached first.
		using (var enabledChanged = new global::System.Threading.AutoResetEvent(false))
		{
			try
			{
				RunOnUIThread.Execute(() =>
				{
					header = FindVisualChildrenByType<TableViewGroupHeader>(tableView).FirstOrDefault();
					var count = Count();
					if (count == null)
					{
						Verify.Fail("The default group header content must contain the item-count TextBlock.");
						return;
					}

					Verify.AreEqual(1.0, count.Opacity, "A header loaded disabled must not dim its count on top of the disabled brush.");
					header.IsEnabledChanged += (s, e) =>
					{
						try { enabledChanged.Set(); } catch (ObjectDisposedException) { }
					};
					tableView.IsEnabled = true;
				});

				Verify.IsTrue(enabledChanged.WaitOne(TimeSpan.FromSeconds(5)), "IsEnabledChanged must be raised.");
				await TestServices.WindowHelper.WaitForIdle();

				RunOnUIThread.Execute(() =>
				{
					Verify.AreEqual(0.5, Count().Opacity, "Enabling must restore TabularSurfaceGroupHeaderCountOpacity.");
					tableView.IsEnabled = false;
				});

				Verify.IsTrue(enabledChanged.WaitOne(TimeSpan.FromSeconds(5)), "IsEnabledChanged must be raised.");
				await TestServices.WindowHelper.WaitForIdle();

				RunOnUIThread.Execute(() => Verify.AreEqual(1.0, Count().Opacity,
					"Disabling must not dim the count on top of the disabled brush."));
			}
			finally
			{
				RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceGroupHeaderCountOpacity"));
			}
		}
	}

	[TestMethod]
	public async Task VerifyDisabledGroupHeaderCountFollowsContentTemplateChanges()
	{
		TableView tableView = null;
		DataTemplate customTemplate = null;
		const string CustomTemplateXaml =
			@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
							xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
				  <StackPanel Orientation=""Horizontal"">
					  <TextBlock Text=""{Binding KeyText}"" />
					  <TextBlock x:Name=""TableViewGroupHeaderItemCountText"" Text=""{Binding ItemCountText}"" Opacity=""0.25"" />
				  </StackPanel>
			  </DataTemplate>";

		List<TextBlock> Counts() => FindVisualChildrenByType<TableViewGroupHeader>(tableView)
			.Select(header => header.FindVisualChildByName("TableViewGroupHeaderItemCountText") as TextBlock)
			.ToList();

		void VerifyCounts(double expected, string message)
		{
			var counts = Counts();
			Verify.IsGreaterThan(counts.Count, 0, "Precondition: group headers are realized.");
			foreach (var count in counts)
			{
				if (count == null)
				{
					Verify.Fail("Every group header must contain the item-count TextBlock.");
					return;
				}
				Verify.AreEqual(expected, count.Opacity, message);
			}
		}

		try
		{
			RunOnUIThread.Execute(() =>
			{
				EnsureTabularControlsResources();
				SetApplicationResource("TabularSurfaceGroupHeaderCountOpacity", 0.5);
				customTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(CustomTemplateXaml);

				tableView = CreateGroupedThemingTable();
				tableView.IsEnabled = false;
				LoadContent(tableView);
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				VerifyCounts(1.0, "Precondition: a disabled default count is not dimmed twice.");
				tableView.GroupHeaderTemplate = customTemplate;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				VerifyCounts(0.25, "An app template's same-named TextBlock must keep its own Opacity.");
				tableView.GroupHeaderTemplate = null;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() => VerifyCounts(1.0,
				"A default count realized again while disabled must not be dimmed twice."));
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceGroupHeaderCountOpacity"));
		}
	}

	[TestMethod]
	public async Task VerifyGroupHeaderCountFollowsAppForegroundStyle()
	{
		var white = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			tableView = CreateGroupedThemingTable();
			tableView.RequestedTheme = ElementTheme.Light;
			var style = new Style(typeof(TableViewGroupHeader));
			style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x5F, 0xB8))));
			style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(white)));
			tableView.Resources[typeof(TableViewGroupHeader)] = style;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var groupHeader = FindVisualChildrenByType<TableViewGroupHeader>(tableView).FirstOrDefault();
			var count = groupHeader?.FindVisualChildByName("TableViewGroupHeaderItemCountText") as TextBlock;
			if (count == null)
			{
				Verify.Fail("The default group header content must contain the item-count TextBlock.");
				return;
			}

			Verify.AreEqual(white, (count.Foreground as SolidColorBrush)?.Color ?? default,
				"An app's header Foreground must reach the count so it stays readable on the app's Background.");
		});
	}

	#endregion

	#region 13.3 Density

	// Density is one of the better-specified areas in the control: the presets are literal values in
	// TableView_themeresources.xaml rather than constants in code.
	//
	// The row Style has no MinHeight setter, so each preset must reach the row from code.
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
					"TableView_themeresources.xaml, applied by the row in code.");
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

	[TestMethod]
	[TestProperty("Description", "Verifies header cells are rounded only while keyboard-focused, so their system focus visual is rounded without clipping.")]
	public async Task VerifyHeaderCellCornerRadiusMatchesControlCornerRadius()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await VerifyRoundedOnlyWhileKeyboardFocused(tableView,
			() => GetHeaderCell(tableView, 0) as TableViewColumnHeader, () => GetHeaderCell(tableView, 1), "header cell");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header Style's CornerRadius is kept while the header is keyboard-focused.")]
	public async Task VerifyHeaderStyleCornerRadiusSurvivesKeyboardFocus()
	{
		TableView tableView = null;
		var styleRadius = new CornerRadius(7);

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			var style = new Style(typeof(TableViewColumnHeader));
			style.Setters.Add(new Setter(Control.CornerRadiusProperty, styleRadius));
			tableView.ColumnHeaderStyle = style;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			Verify.AreNotEqual(styleRadius, tableView.CornerRadius, "Precondition: the style radius differs from the table's.");
			Verify.AreEqual(styleRadius, header.CornerRadius, "Precondition: the style radius reaches the header.");
			Verify.IsTrue(header.Focus(FocusState.Keyboard), "Precondition: the header takes keyboard focus.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			Verify.AreEqual(FocusState.Keyboard, header.FocusState, "Precondition: the header is keyboard-focused.");
			Verify.AreEqual(styleRadius, header.CornerRadius, "A keyboard-focused header must keep its Style's CornerRadius.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies data cells are rounded only while keyboard-focused, so their system focus visual is rounded without clipping.")]
	public async Task VerifyCellCornerRadiusMatchesControlCornerRadius()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await VerifyRoundedOnlyWhileKeyboardFocused(tableView,
			() => GetRowCell(FindVisualChildrenByType<TableViewRow>(tableView).First(), 0),
			() => GetHeaderCell(tableView, 0), "cell");
	}

	// A rounded Grid clips its children and hit-testing, so it may round only while keyboard-focused.
	private static CornerRadius RadiusOf(FrameworkElement element)
		=> element is Control control ? control.CornerRadius : ((Grid)element).CornerRadius;

	private static async Task VerifyRoundedOnlyWhileKeyboardFocused(
		TableView tableView, Func<FrameworkElement> getTarget, Func<UIElement> getOther, string what)
	{
		var expected = new CornerRadius(0);

		RunOnUIThread.Execute(() =>
		{
			var row = FindVisualChildrenByType<TableViewRow>(tableView).FirstOrDefault();
			Verify.IsNotNull(row, "The TableView should realize at least one row.");
			expected = row.CornerRadius;
			Verify.AreNotEqual(new CornerRadius(0), expected, "The row style should round row focus.");

			var target = getTarget();
			Verify.IsNotNull(target, $"The {what} should exist.");
			Verify.IsTrue(target.UseSystemFocusVisuals, $"The {what} should use the system focus visual.");
			Verify.AreEqual(new CornerRadius(0), RadiusOf(target),
				$"An unfocused {what} must stay square: a rounded root clips its children.");
			target.IsTabStop = true;
			Verify.IsTrue(target.Focus(FocusState.Keyboard), $"Precondition: the {what} takes keyboard focus.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var target = getTarget();
			Verify.IsTrue(ReferenceEquals(target, FocusManager.GetFocusedElement(target.XamlRoot)),
				$"Precondition: keyboard focus stays on the {what}.");
			Verify.AreEqual(FocusState.Keyboard, target.FocusState, $"Precondition: the {what} is keyboard-focused.");
			Verify.AreEqual(expected, RadiusOf(target),
				$"A keyboard-focused {what} must be rounded like the rows' focus rect.");
			Verify.IsTrue(getOther().Focus(FocusState.Programmatic), "Precondition: focus moves elsewhere.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var target = getTarget();
			Verify.AreEqual(new CornerRadius(0), RadiusOf(target), $"The {what} must drop its radius when it loses focus.");
			target.IsTabStop = true;
			Verify.IsTrue(target.Focus(FocusState.Pointer), $"Precondition: the {what} takes pointer focus.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(new CornerRadius(0), RadiusOf(getTarget()),
				$"Pointer focus shows no focus rect, so the {what} must stay square.");
			Verify.IsTrue(getOther().Focus(FocusState.Programmatic), "Precondition: focus moves elsewhere.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var target = getTarget();
			target.IsTabStop = true;
			Verify.IsTrue(target.Focus(FocusState.Programmatic), $"Precondition: the {what} takes programmatic focus.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// XAML coerces programmatic focus to Keyboard or Pointer from the content root's last
			// input device, which earlier tests or the harness may have left as keyboard; the
			// focus rect, and so the radius, follows the coerced state.
			var target = getTarget();
			var state = target.FocusState;
			Log.Comment($"Programmatic focus on the {what} was coerced to {state}.");
			Verify.IsTrue(state == FocusState.Keyboard || state == FocusState.Pointer,
				$"Precondition: programmatic focus on the {what} is coerced to Keyboard or Pointer.");
			Verify.AreEqual(state == FocusState.Keyboard ? expected : new CornerRadius(0), RadiusOf(target),
				$"Programmatic focus shows the focus rect only after keyboard input, so the {what} is rounded only then.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies group headers carry ControlCornerRadius so their system focus visual is rounded.")]
	public async Task VerifyGroupHeaderCornerRadiusMatchesRowCornerRadius()
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
			var groupHeader = FindVisualChildrenByType<TableViewGroupHeader>(tableView).FirstOrDefault();
			Verify.IsNotNull(row, "A grouped TableView should realize at least one row.");
			Verify.IsNotNull(groupHeader, "A grouped TableView should realize at least one group header.");
			Verify.IsTrue(groupHeader.UseSystemFocusVisuals, "Group headers should use the system focus visual.");
			Verify.AreNotEqual(new CornerRadius(0), row.CornerRadius, "The row style should round row focus.");
			Verify.AreEqual(row.CornerRadius, groupHeader.CornerRadius,
				"Group headers must carry ControlCornerRadius so the keyboard focus rect is rounded like the rows'.");
		});
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

	[TestMethod]
	public async Task VerifyHeaderFontWeightDefaultIsSemiBold()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual((ushort)600, GetHeaderPresenter(tableView, 0).FontWeight.Weight,
				"TableViewHeaderFontWeight defaults to SemiBold.");
		});
	}

	// Override before load: the resolved weight is cached per instance.
	[TestMethod]
	public async Task VerifyHeaderFontWeightResourceOverrideApplies()
	{
		TableView appScoped = null;
		TableView elementScoped = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TableViewHeaderFontWeight", Microsoft.UI.Text.FontWeights.Normal);

			appScoped = CreateThemingTable();
			LoadContent(appScoped);
		});

		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual((ushort)400, GetHeaderPresenter(appScoped, 0).FontWeight.Weight,
					"An app-scope TableViewHeaderFontWeight override must win over the SemiBold default.");
			});
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TableViewHeaderFontWeight"));
		}

		RunOnUIThread.Execute(() =>
		{
			elementScoped = CreateThemingTable();
			elementScoped.Resources["TableViewHeaderFontWeight"] = Microsoft.UI.Text.FontWeights.Bold;
			LoadContent(elementScoped);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual((ushort)700, GetHeaderPresenter(elementScoped, 0).FontWeight.Weight,
				"A TableViewHeaderFontWeight in the TableView's own Resources must win.");
		});
	}

	// FontWeight comes back as a fresh box per lookup, so the own-entry check must compare it by
	// value; otherwise the APP-theme dictionary's entry reads as an own entry and wins. Both themes
	// run so one of them always differs from the app theme.
	[TestMethod]
	public async Task VerifyHeaderFontWeightThemeDictionaryOverrideFollowsElementTheme()
	{
		var cases = new[]
		{
			(Theme: ElementTheme.Light, Weight: (ushort)700),
			(Theme: ElementTheme.Dark, Weight: (ushort)300),
		};

		foreach (var testCase in cases)
		{
			TableView tableView = null;

			RunOnUIThread.Execute(() =>
			{
				EnsureTabularControlsResources();
				tableView = CreateThemingTable();
				tableView.RequestedTheme = testCase.Theme;
				tableView.Resources.ThemeDictionaries["Light"] = new ResourceDictionary
				{
					["TableViewHeaderFontWeight"] = Microsoft.UI.Text.FontWeights.Bold,
				};
				tableView.Resources.ThemeDictionaries["Dark"] = new ResourceDictionary
				{
					["TableViewHeaderFontWeight"] = Microsoft.UI.Text.FontWeights.Light,
				};
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(testCase.Theme, tableView.ActualTheme, "Precondition: RequestedTheme picks the table's theme.");
				Verify.AreEqual(testCase.Weight, GetHeaderPresenter(tableView, 0).FontWeight.Weight,
					$"Under {testCase.Theme}, the TableView's ThemeDictionaries[\"{testCase.Theme}\"] TableViewHeaderFontWeight must win.");
			});
		}
	}

	#endregion

	#region 13.4 Column header style

	private static TableViewColumnHeader RequireColumnHeader(TableView tableView, int index)
	{
		var header = GetHeaderCell(tableView, index) as TableViewColumnHeader;
		Verify.IsNotNull(header, $"Header {index} should be a TableViewColumnHeader.");
		return header;
	}

	private static string CurrentState(TableViewColumnHeader header, string groupName)
	{
		var root = VisualTreeHelper.GetChild(header, 0) as FrameworkElement;
		Verify.IsNotNull(root, "The header template should have applied.");
		var group = VisualStateManager.GetVisualStateGroups(root).FirstOrDefault(g => g.Name == groupName);
		Verify.IsNotNull(group, $"The header template should declare {groupName}.");
		return group.CurrentState?.Name;
	}

	[TestMethod]
	[TestProperty("Description", "Verifies TableView.ColumnHeaderStyle reaches every header, TableViewColumn.HeaderStyle wins for its column, and a style Padding/MinHeight beats the density values.")]
	public async Task VerifyColumnHeaderStyleApplies()
	{
		TableView tableView = null;
		SolidColorBrush tableBrush = null;
		SolidColorBrush columnBrush = null;
		var stylePadding = new Thickness(20, 1, 20, 1);

		RunOnUIThread.Execute(() =>
		{
			tableBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
			columnBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
			tableView = CreateThemingTable();
			var tableStyle = new Style(typeof(TableViewColumnHeader));
			tableStyle.Setters.Add(new Setter(Control.BackgroundProperty, tableBrush));
			tableStyle.Setters.Add(new Setter(Control.FontWeightProperty, Microsoft.UI.Text.FontWeights.Bold));
			tableStyle.Setters.Add(new Setter(Control.PaddingProperty, stylePadding));
			tableStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 60.0));
			tableView.ColumnHeaderStyle = tableStyle;

			var columnStyle = new Style(typeof(TableViewColumnHeader));
			columnStyle.Setters.Add(new Setter(Control.BackgroundProperty, columnBrush));
			tableView.Columns[1].HeaderStyle = columnStyle;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var first = RequireColumnHeader(tableView, 0);
			Verify.IsTrue(ReferenceEquals(tableBrush, first.Background), "ColumnHeaderStyle's Background setter must reach the header.");
			Verify.AreEqual((ushort)700, GetHeaderPresenter(tableView, 0).FontWeight.Weight,
				"ColumnHeaderStyle's FontWeight setter must reach the header text.");
			Verify.AreEqual(stylePadding, first.Padding, "A style Padding must win over the density padding.");
			Verify.AreEqual(60.0, first.MinHeight, "A style MinHeight must win over the density row height.");

			var second = RequireColumnHeader(tableView, 1);
			Verify.IsTrue(ReferenceEquals(tableView.Columns[1], second.Column), "A header's Column must be the column it represents.");
			Verify.IsTrue(ReferenceEquals(columnBrush, second.Background), "TableViewColumn.HeaderStyle must win over ColumnHeaderStyle.");
			Verify.AreEqual((ushort)600, GetHeaderPresenter(tableView, 1).FontWeight.Weight,
				"A column style without a FontWeight setter keeps the default SemiBold.");
			Verify.AreEqual(40.0, second.MinHeight, "Without a style MinHeight the header follows the density row height.");

			tableView.ColumnHeaderStyle = null;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(ReferenceEquals(tableBrush, RequireColumnHeader(tableView, 0).Background),
				"Clearing ColumnHeaderStyle must restyle the realized headers.");
			Verify.AreEqual((ushort)600, GetHeaderPresenter(tableView, 0).FontWeight.Weight,
				"Clearing ColumnHeaderStyle must restore the default header weight.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies header Styles that only inherit setters through BasedOn a ContentControl or Control Style apply, and their BasedOn Padding beats the density padding.")]
	public async Task VerifyBasedOnOnlyColumnHeaderStyleApplies()
	{
		TableView tableView = null;
		SolidColorBrush tableBrush = null;
		SolidColorBrush columnBrush = null;
		var stylePadding = new Thickness(20, 1, 20, 1);
		bool loaded = false;

		// A checked build can stop on a framework assert while loading; fail instead of stalling.
		using var watchdog = new global::System.Threading.Timer(_ =>
		{
			if (!global::System.Threading.Volatile.Read(ref loaded))
			{
				Environment.FailFast("VerifyBasedOnOnlyColumnHeaderStyleApplies: the TableView did not load within 60 seconds.");
			}
		}, null, TimeSpan.FromSeconds(60), global::System.Threading.Timeout.InfiniteTimeSpan);

		RunOnUIThread.Execute(() =>
		{
			tableBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
			columnBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
			var tableBase = new Style(typeof(ContentControl));
			tableBase.Setters.Add(new Setter(Control.BackgroundProperty, tableBrush));
			tableBase.Setters.Add(new Setter(Control.PaddingProperty, stylePadding));
			var columnBase = new Style(typeof(Control));
			columnBase.Setters.Add(new Setter(Control.BackgroundProperty, columnBrush));

			tableView = CreateThemingTable();
			tableView.ColumnHeaderStyle = new Style(typeof(TableViewColumnHeader)) { BasedOn = tableBase };
			tableView.Columns[1].HeaderStyle = new Style(typeof(TableViewColumnHeader)) { BasedOn = columnBase };
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();
		global::System.Threading.Volatile.Write(ref loaded, true);

		RunOnUIThread.Execute(() => tableView.Density = TableViewDensity.Compact);
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var first = RequireColumnHeader(tableView, 0);
			Verify.IsTrue(ReferenceEquals(tableBrush, first.Background), "A BasedOn Background setter must reach the header.");
			Verify.AreEqual(stylePadding, first.Padding, "A BasedOn Padding must win over the density padding.");

			var second = RequireColumnHeader(tableView, 1);
			Verify.IsTrue(ReferenceEquals(columnBrush, second.Background), "TableViewColumn.HeaderStyle's BasedOn Background must win over ColumnHeaderStyle.");
			Verify.AreEqual(30.0, second.MinHeight, "Without a style MinHeight the header follows the Compact row height.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a UIElement column Header moves to the new header when the header band is rebuilt.")]
	public async Task VerifyUIElementHeaderSurvivesHeaderRebuild()
	{
		TableView tableView = null;
		Border headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			headerElement = new Border { Width = 20, Height = 12, Background = new SolidColorBrush(Microsoft.UI.Colors.Blue) };
			tableView.Columns[0].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => tableView.ColumnHeaderStyle = new Style(typeof(TableViewColumnHeader)));
		await TestServices.WindowHelper.WaitForIdle();
		RunOnUIThread.Execute(() => tableView.Density = TableViewDensity.Compact);
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			Verify.IsTrue(ReferenceEquals(headerElement, header.Content), "The rebuilt header must show the column's UIElement Header.");

			DependencyObject current = headerElement;
			while (current != null && !ReferenceEquals(current, header))
			{
				current = VisualTreeHelper.GetParent(current);
			}
			Verify.IsNotNull(current, "The UIElement Header must be parented under the rebuilt header.");
			Verify.AreEqual(20.0, headerElement.ActualWidth, "The UIElement Header must be laid out in the rebuilt header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a UIElement column Header moves to the new header band when the TableView is re-templated.")]
	public async Task VerifyUIElementHeaderSurvivesRetemplate()
	{
		TableView tableView = null;
		Border headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			headerElement = new Border { Width = 20, Height = 12, Background = new SolidColorBrush(Microsoft.UI.Colors.Blue) };
			tableView.Columns[0].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				@"<ControlTemplate
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					xmlns:tabular='using:Microsoft.UI.Xaml.Controls.Tabular'
					TargetType='tabular:TableView'>
					<Grid>
						<Grid.RowDefinitions>
							<RowDefinition Height='Auto' />
							<RowDefinition Height='*' />
						</Grid.RowDefinitions>
						<Border x:Name='PART_HeaderRow'>
							<tabular:TableViewCellsPanel x:Name='PART_HeaderHost' HorizontalAlignment='Left' />
						</Border>
						<ItemsRepeater x:Name='PART_RowsRepeater' Grid.Row='1'>
							<ItemsRepeater.Layout>
								<StackLayout Orientation='Vertical' />
							</ItemsRepeater.Layout>
						</ItemsRepeater>
					</Grid>
				</ControlTemplate>");
			tableView.UpdateLayout();
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => VerifyHostedUnderHeader(tableView, 0, headerElement, "after re-templating"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a column with a UIElement Header can move from one TableView to another.")]
	public async Task VerifyUIElementHeaderSurvivesMoveToAnotherTableView()
	{
		TableView source = null;
		TableView target = null;
		StackPanel host = null;
		Border headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			source = CreateThemingTable();
			headerElement = new Border { Width = 20, Height = 12, Background = new SolidColorBrush(Microsoft.UI.Colors.Blue) };
			source.Columns[0].Header = headerElement;
			host = new StackPanel();
			host.Children.Add(source);
			LoadContent(host);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = source.Columns[0];
			source.Columns.RemoveAt(0);
			target = new TableView();
			target.Columns.Add(column);
			// Templating the target rebuilds its headers before the source's queued rebuild runs.
			host.Children.Add(target);
			host.UpdateLayout();
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => VerifyHostedUnderHeader(target, 0, headerElement, "after the move"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies focus inside a UIElement column Header returns to that header after the header band is rebuilt.")]
	public async Task VerifyFocusInUIElementHeaderIsRestoredAfterRebuild()
	{
		TableView tableView = null;
		CheckBox headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			headerElement = new CheckBox { MinWidth = 0 };
			tableView.Columns[0].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.IsTrue(headerElement.Focus(FocusState.Keyboard), "Precondition: the header CheckBox takes focus."));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => tableView.Density = TableViewDensity.Compact);
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			DependencyObject current = FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;
			Log.Comment($"Focused after rebuild: {current}");
			while (current != null && !ReferenceEquals(current, header))
			{
				current = VisualTreeHelper.GetParent(current);
			}
			Verify.IsNotNull(current, "Focus must return to the rebuilt header of the column that held it.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a replaced UIElement column Header can be re-parented right away, before the header band rebuilds.")]
	public async Task VerifyReplacedUIElementHeaderCanBeReparentedImmediately()
	{
		TableView tableView = null;
		StackPanel root = null;
		Border headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			headerElement = new Border { Width = 20, Height = 12, Background = new SolidColorBrush(Microsoft.UI.Colors.Blue) };
			tableView.Columns[0].Header = headerElement;
			root = new StackPanel();
			root.Children.Add(tableView);
			LoadContent(root);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Columns[0].Header = "Name";
			var other = new StackPanel();
			root.Children.Add(other);
			other.Children.Add(headerElement);
			Verify.IsTrue(ReferenceEquals(other, VisualTreeHelper.GetParent(headerElement)), "The old Header must move to the new panel.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.AreEqual("Name", (RequireColumnHeader(tableView, 0).Content as TextBlock)?.Text,
			"The header band must show the new Header."));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies focus inside a UIElement column Header follows the column when it moves within the same TableView.")]
	public async Task VerifyFocusInUIElementHeaderFollowsColumnMove()
	{
		TableView tableView = null;
		CheckBox headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			headerElement = new CheckBox { MinWidth = 0 };
			tableView.Columns[0].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.IsTrue(headerElement.Focus(FocusState.Keyboard), "Precondition: the header CheckBox takes focus."));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			tableView.Columns.RemoveAt(0);
			tableView.Columns.Insert(1, column);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 1);
			Verify.IsTrue(ReferenceEquals(headerElement, header.Content), "Precondition: the moved column's header hosts its Header.");
			DependencyObject current = FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;
			Log.Comment($"Focused after the move: {current}");
			while (current != null && !ReferenceEquals(current, header))
			{
				current = VisualTreeHelper.GetParent(current);
			}
			Verify.IsNotNull(current, "Focus must follow the moved column to its rebuilt header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing a column whose UIElement Header holds focus moves focus to the nearest remaining header, never onto the removed one.")]
	public async Task VerifyFocusInRemovedColumnHeaderMovesToNearestHeader()
	{
		TableView tableView = null;
		CheckBox headerElement = null;
		var removedHeaderFocused = false;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			headerElement = new CheckBox { MinWidth = 0 };
			tableView.Columns[0].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.IsTrue(headerElement.Focus(FocusState.Keyboard), "Precondition: the header CheckBox takes focus."));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var removedHeader = RequireColumnHeader(tableView, 0);
			EventHandler<GettingFocusEventArgs> handler = (s, args) =>
			{
				if (ReferenceEquals(args.NewFocusedElement, removedHeader))
				{
					removedHeaderFocused = true;
				}
			};
			FocusManager.GettingFocus += handler;
			try
			{
				tableView.Columns.RemoveAt(0);
			}
			finally
			{
				FocusManager.GettingFocus -= handler;
			}
			Verify.IsFalse(removedHeaderFocused, "Focus must not move onto the removed column's header.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			DependencyObject focused = FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;
			Log.Comment($"Focused after the removal: {focused}");
			Verify.IsTrue(focused != null && IsUnder(focused, header), "Focus must move to the nearest remaining header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies replacing the Columns vector while a header holds keyboard focus keeps focus on a header of the table.")]
	public async Task VerifyHeaderFocusStaysInHeaderBandWhenColumnsVectorReplaced()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.IsTrue(RequireColumnHeader(tableView, 1).Focus(FocusState.Keyboard), "Precondition: the second header takes focus."));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// A vector no table is subscribed to, holding columns no table owns.
			var donor = new TableView();
			var replacement = donor.GetValue(TableView.ColumnsProperty);
			donor.SetValue(TableView.ColumnsProperty, new TableView().GetValue(TableView.ColumnsProperty));
			var columns = (IList<TableViewColumn>)replacement;
			columns.Add(MakeTextColumn("Name"));
			columns.Add(MakeTextColumn("Role"));

			tableView.SetValue(TableView.ColumnsProperty, replacement);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var focused = FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;
			Log.Comment($"Focused after the replacement: {focused}");
			var header = FindVisualChildrenByType<TableViewColumnHeader>(tableView).FirstOrDefault(h => focused != null && IsUnder(focused, h));
			Verify.IsNotNull(header, "Focus must stay on a header of the table.");
			Verify.AreEqual(FocusState.Keyboard, (focused as UIElement)?.FocusState ?? FocusState.Unfocused, "Focus keeps its keyboard state.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies focus in the header of a column that is moved or re-added with no other header to hold focus returns to that column's header.")]
	[TestProperty("data:Change", "{MoveOnlyColumn, ClearAndReAddLast}")]
	[DataRow("MoveOnlyColumn")]
	[DataRow("ClearAndReAddLast")]
	public async Task VerifyHeaderFocusReturnsWhenNoOtherHeaderCanHoldIt(string changeRow)
	{
		var change = changeRow.Trim();
		var focusedIndex = change == "ClearAndReAddLast" ? 1 : 0;
		TableView tableView = null;
		CheckBox headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = change == "MoveOnlyColumn" ? CreateTableView(headers: new[] { "Name" }) : CreateThemingTable();
			headerElement = new CheckBox { MinWidth = 0 };
			tableView.Columns[focusedIndex].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.IsTrue(headerElement.Focus(FocusState.Keyboard), "Precondition: the header CheckBox takes focus."));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var columns = tableView.Columns.ToList();
			if (change == "MoveOnlyColumn")
			{
				tableView.Columns.RemoveAt(0);
				tableView.Columns.Insert(0, columns[0]);
			}
			else
			{
				tableView.Columns.Clear();
				foreach (var column in columns)
				{
					tableView.Columns.Add(column);
				}
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, focusedIndex);
			var focused = FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;
			Log.Comment($"{change}: focused {focused}");
			Verify.IsTrue(focused != null && IsUnder(focused, header), $"{change}: focus must return to the column's header.");
			Verify.AreEqual(FocusState.Keyboard, ((UIElement)focused).FocusState, $"{change}: keyboard focus must stay keyboard focus.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies focus in the header of the only column moves into the body when that column is removed.")]
	public async Task VerifyFocusInOnlyRemovedColumnHeaderMovesToBody()
	{
		TableView tableView = null;
		CheckBox headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name" });
			headerElement = new CheckBox { MinWidth = 0 };
			tableView.Columns[0].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.IsTrue(headerElement.Focus(FocusState.Keyboard), "Precondition: the header CheckBox takes focus."));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => tableView.Columns.RemoveAt(0));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var focused = FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;
			Log.Comment($"Focused after the removal: {focused}");
			Verify.IsTrue(focused != null && IsUnder(focused, GetRowsRepeater(tableView)), "Focus must move into the table body.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies focus in a header stays in the header band when the columns are replaced by new instances.")]
	[TestProperty("data:Change", "{ClearAndAddNew, ReplaceOnlyColumnWithoutRows}")]
	[DataRow("ClearAndAddNew")]
	[DataRow("ReplaceOnlyColumnWithoutRows")]
	public async Task VerifyHeaderFocusStaysInHeaderBandWhenColumnsReplaced(string changeRow)
	{
		var change = changeRow.Trim();
		TableView tableView = null;
		CheckBox headerElement = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = change == "ReplaceOnlyColumnWithoutRows"
				? CreateTableView(new List<Person>(), headers: new[] { "Name" })
				: CreateThemingTable();
			headerElement = new CheckBox { MinWidth = 0 };
			tableView.Columns[0].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.IsTrue(headerElement.Focus(FocusState.Keyboard), "Precondition: the header CheckBox takes focus."));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			if (change == "ClearAndAddNew")
			{
				tableView.Columns.Clear();
				tableView.Columns.Add(MakeTextColumn("Name"));
				tableView.Columns.Add(MakeTextColumn("Role"));
			}
			else
			{
				tableView.Columns[0] = MakeTextColumn("Name");
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var focused = FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;
			Log.Comment($"{change}: focused {focused}");
			Verify.IsTrue(focused != null && IsUnder(focused, RequireColumnHeader(tableView, 0)), $"{change}: focus must move to the first header of the new columns.");
			Verify.AreEqual(FocusState.Keyboard, ((UIElement)focused).FocusState, $"{change}: keyboard focus must stay keyboard focus.");
		});
	}


	[TestMethod]
	[TestProperty("Description", "Verifies changing a string Header keeps the header's content and tooltip until the header band rebuilds.")]
	public async Task VerifyStringHeaderChangeKeepsHeaderContentUntilRebuild()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			tableView.Columns[0].HeaderToolTip = "Tip";
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			Verify.IsNotNull(ToolTipService.GetToolTip(header), "Precondition: the header has a tooltip.");
			tableView.Columns[0].Header = "Renamed";
			Verify.IsNotNull(header.Content, "A string Header change must not blank the header before the rebuild.");
			Verify.IsNotNull(ToolTipService.GetToolTip(header), "A string Header change must not drop the tooltip before the rebuild.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.AreEqual("Renamed", (RequireColumnHeader(tableView, 0).Content as TextBlock)?.Text,
			"The header band must show the new Header."));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header rebuild started from app code while another rebuild releases headers keeps UIElement Headers hosted.")]
	public async Task VerifyReentrantHeaderRebuildKeepsUIElementHeader()
	{
		TableView tableView = null;
		CheckBox headerElement = null;
		bool reentered = false;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			headerElement = new CheckBox { MinWidth = 0 };
			tableView.Columns[0].Header = headerElement;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => Verify.IsTrue(headerElement.Focus(FocusState.Keyboard), "Precondition: the header CheckBox takes focus."));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			EventHandler<GettingFocusEventArgs> handler = (s, args) =>
			{
				if (!reentered && ReferenceEquals(args.OldFocusedElement, headerElement))
				{
					reentered = true;
					// Rebuilds the header band synchronously, from inside the outer rebuild.
					tableView.CanUserResizeColumns = !tableView.CanUserResizeColumns;
				}
			};
			FocusManager.GettingFocus += handler;
			try
			{
				tableView.Density = TableViewDensity.Compact;
			}
			finally
			{
				FocusManager.GettingFocus -= handler;
			}
			Verify.IsTrue(reentered, "Precondition: releasing the focused Header re-entered the rebuild.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			Verify.IsTrue(ReferenceEquals(headerElement, header.Content), "The header must show the column's UIElement Header.");
			Verify.IsTrue(IsUnder(headerElement, header), "The UIElement Header must be parented under the rebuilt header.");
		});
	}

	private static bool IsUnder(DependencyObject element, DependencyObject ancestor)
	{
		for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
		{
			if (ReferenceEquals(current, ancestor))
			{
				return true;
			}
		}
		return false;
	}

	private static void VerifyHostedUnderHeader(TableView tableView, int index, FrameworkElement headerElement, string when)
	{
		var header = RequireColumnHeader(tableView, index);
		Verify.IsTrue(ReferenceEquals(headerElement, header.Content), $"The header must show the column's UIElement Header {when}.");

		DependencyObject current = headerElement;
		while (current != null && !ReferenceEquals(current, header))
		{
			current = VisualTreeHelper.GetParent(current);
		}
		Verify.IsNotNull(current, $"The UIElement Header must be parented under the header {when}.");
		Verify.AreEqual(20.0, headerElement.ActualWidth, $"The UIElement Header must be laid out {when}.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a disabled TableView puts its headers in Disabled, dimming the header text and sort glyph.")]
	public async Task VerifyDisabledHeaderDimsText()
	{
		var disabled = Color.FromArgb(0xFF, 0x20, 0x40, 0x60);
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceRowForegroundDisabledBrush", new SolidColorBrush(disabled));
			tableView = CreateThemingTable();
			tableView.Columns[0].CanSort = true;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() =>
			{
				Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending), "Precondition: the column sorts.");
				tableView.IsEnabled = false;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var header = RequireColumnHeader(tableView, 0);
				Verify.AreEqual("Disabled", CurrentState(header, "CommonStates"), "A disabled table's header must be Disabled.");
				Verify.AreEqual(disabled, (GetHeaderPresenter(tableView, 0).Foreground as SolidColorBrush)?.Color ?? default,
					"Disabled must dim the header text.");
				var glyph = header.FindVisualChildByName("TableViewSortIndicator") as Control;
				Verify.IsNotNull(glyph, "A sorted column must host the sort glyph.");
				Verify.AreEqual(disabled, (glyph?.Foreground as SolidColorBrush)?.Color ?? default, "Disabled must dim the sort glyph.");
			});
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceRowForegroundDisabledBrush"));
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies disabling a header in its High Contrast hover state keeps the disabled text and glyph brush, and re-enabling drops it.")]
	public async Task VerifyDisablingHighContrastHoveredHeaderKeepsDisabledForeground()
	{
		var disabled = Color.FromArgb(0xFF, 0x20, 0x40, 0x60);
		TableView tableView = null;
		TableViewColumnHeader header = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			SetApplicationResource("TabularSurfaceRowForegroundDisabledBrush", new SolidColorBrush(disabled));
			tableView = CreateThemingTable();
			tableView.Columns[0].CanSort = true;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			RunOnUIThread.Execute(() => Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending), "Precondition: the column sorts."));
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				header = RequireColumnHeader(tableView, 0);
				// The hovered High Contrast states, entered directly: a test cannot switch the system theme.
				Verify.IsTrue(VisualStateManager.GoToState(header, "PointerOver", false), "PointerOver must exist.");
				Verify.IsTrue(VisualStateManager.GoToState(header, "HighContrastPointerOver", false), "HighContrastPointerOver must exist.");
				header.IsEnabled = false;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual("Disabled", CurrentState(header, "CommonStates"), "Precondition: the disabled header is Disabled.");
				Verify.AreEqual("HighContrastNormal", CurrentState(header, "HighContrastStates"), "Precondition: the hover highlight is gone.");
				Verify.AreEqual(disabled, (GetHeaderPresenter(tableView, 0).Foreground as SolidColorBrush)?.Color ?? default,
					"Leaving the High Contrast hover state must not drop the disabled text brush.");
				var glyph = header.FindVisualChildByName("TableViewSortIndicator") as Control;
				Verify.IsNotNull(glyph, "A sorted column must host the sort glyph.");
				Verify.AreEqual(disabled, (glyph?.Foreground as SolidColorBrush)?.Color ?? default,
					"Leaving the High Contrast hover state must not drop the disabled glyph brush.");
				header.IsEnabled = true;
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual("Normal", CurrentState(header, "CommonStates"), "Precondition: the re-enabled header is Normal.");
				Verify.AreNotEqual(disabled, (GetHeaderPresenter(tableView, 0).Foreground as SolidColorBrush)?.Color ?? default,
					"Re-enabling must drop the disabled text brush.");
			});
		}
		finally
		{
			RunOnUIThread.Execute(() => ClearApplicationResource("TabularSurfaceRowForegroundDisabledBrush"));
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a disabled header's sort glyph follows the header text when its theme brush re-resolves without a visual state change.")]
	public async Task VerifyDisabledSortGlyphFollowsTextAcrossThemeChange()
	{
		TableView tableView = null;
		TableViewColumnHeader header = null;
		Color before = default;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			tableView.RequestedTheme = ElementTheme.Light;
			tableView.Columns[0].CanSort = true;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending), "Precondition: the column sorts.");
			tableView.IsEnabled = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			header = RequireColumnHeader(tableView, 0);
			Verify.AreEqual("Disabled", CurrentState(header, "CommonStates"), "Precondition: a disabled table's header is Disabled.");
			before = (GetHeaderPresenter(tableView, 0).Foreground as SolidColorBrush)?.Color ?? default;

			// Re-resolves the header's ThemeResources without re-running its visual states, as a
			// High Contrast toggle does.
			header.RequestedTheme = ElementTheme.Dark;
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var text = (GetHeaderPresenter(tableView, 0).Foreground as SolidColorBrush)?.Color ?? default;
			Verify.AreNotEqual(before, text, "Precondition: the disabled text brush differs between Light and Dark.");
			var glyph = header.FindVisualChildByName("TableViewSortIndicator") as Control;
			Verify.IsNotNull(glyph, "A sorted column must host the sort glyph.");
			Verify.AreEqual(text, (glyph?.Foreground as SolidColorBrush)?.Color ?? default,
				"The disabled sort glyph must keep the header text's brush after it re-resolves.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header Style with an incompatible TargetType is ignored instead of breaking the header band.")]
	public async Task VerifyIncompatibleColumnHeaderStyleIsIgnored()
	{
		TableView tableView = null;
		SolidColorBrush brush = null;

		RunOnUIThread.Execute(() =>
		{
			brush = new SolidColorBrush(Microsoft.UI.Colors.Red);
			var buttonStyle = new Style(typeof(Button));
			buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, brush));
			tableView = CreateThemingTable();
			tableView.ColumnHeaderStyle = buttonStyle;
			tableView.Columns[1].HeaderStyle = buttonStyle;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var index in new[] { 0, 1 })
			{
				var header = RequireColumnHeader(tableView, index);
				Verify.IsNull(header.Style, $"Header {index} must not take a Style targeting another type.");
				Verify.IsFalse(ReferenceEquals(brush, header.Background), $"Header {index} must keep its default look.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header Style whose BasedOn targets an incompatible type is ignored instead of breaking the header band.")]
	public async Task VerifyColumnHeaderStyleWithIncompatibleBasedOnIsIgnored()
	{
		TableView tableView = null;
		SolidColorBrush brush = null;

		RunOnUIThread.Execute(() =>
		{
			brush = new SolidColorBrush(Microsoft.UI.Colors.Red);
			var headerStyle = new Style(typeof(TableViewColumnHeader)) { BasedOn = new Style(typeof(Button)) };
			headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, brush));
			tableView = CreateThemingTable();
			tableView.ColumnHeaderStyle = headerStyle;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var index in new[] { 0, 1 })
			{
				var header = RequireColumnHeader(tableView, index);
				Verify.IsNull(header.Style, $"Header {index} must not take a Style based on another type.");
				Verify.IsFalse(ReferenceEquals(brush, header.Background), $"Header {index} must keep its default look.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header Style whose BasedOn chain loops is ignored instead of breaking the header band.")]
	public async Task VerifyColumnHeaderStyleWithBasedOnCycleIsIgnored()
	{
		TableView tableView = null;
		SolidColorBrush brush = null;

		RunOnUIThread.Execute(() =>
		{
			brush = new SolidColorBrush(Microsoft.UI.Colors.Red);
			var first = new Style(typeof(TableViewColumnHeader));
			var second = new Style(typeof(TableViewColumnHeader)) { BasedOn = first };
			first.BasedOn = second;
			first.Setters.Add(new Setter(Control.BackgroundProperty, brush));
			tableView = CreateThemingTable();
			tableView.ColumnHeaderStyle = first;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var index in new[] { 0, 1 })
			{
				var header = RequireColumnHeader(tableView, index);
				Verify.IsNull(header.Style, $"Header {index} must not take a Style whose BasedOn chain loops.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a header Style without a TargetType, alone or as a BasedOn, is ignored instead of breaking the header band.")]
	public async Task VerifyUntypedColumnHeaderStyleIsIgnored()
	{
		TableView tableView = null;
		SolidColorBrush tableBrush = null;
		SolidColorBrush columnBrush = null;

		RunOnUIThread.Execute(() =>
		{
			tableBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
			columnBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
			var untyped = new Style();
			untyped.Setters.Add(new Setter(Control.BackgroundProperty, tableBrush));
			var untypedBase = new Style();
			untypedBase.Setters.Add(new Setter(Control.BackgroundProperty, columnBrush));
			tableView = CreateThemingTable();
			tableView.ColumnHeaderStyle = untyped;
			tableView.Columns[1].HeaderStyle = new Style(typeof(TableViewColumnHeader)) { BasedOn = untypedBase };
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var index in new[] { 0, 1 })
			{
				var header = RequireColumnHeader(tableView, index);
				Verify.IsNull(header.Style, $"Header {index} must not take a Style chain without a TargetType.");
				Verify.IsFalse(ReferenceEquals(tableBrush, header.Background) || ReferenceEquals(columnBrush, header.Background),
					$"Header {index} must keep its default look.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies header Styles targeting TableViewColumnHeader's framework base types are applied.")]
	public async Task VerifyColumnHeaderStyleTargetingBaseTypesApplies()
	{
		TableView tableView = null;
		Style tableStyle = null;
		Style columnStyle = null;
		SolidColorBrush tableBrush = null;
		SolidColorBrush columnBrush = null;

		RunOnUIThread.Execute(() =>
		{
			tableBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
			columnBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
			// Framework types: their TargetType names come back as freshly allocated strings.
			tableStyle = new Style(typeof(ContentControl));
			tableStyle.Setters.Add(new Setter(Control.BackgroundProperty, tableBrush));
			columnStyle = new Style(typeof(Control));
			columnStyle.Setters.Add(new Setter(Control.BackgroundProperty, columnBrush));
			tableView = CreateThemingTable();
			tableView.ColumnHeaderStyle = tableStyle;
			tableView.Columns[1].HeaderStyle = columnStyle;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var first = RequireColumnHeader(tableView, 0);
			Verify.IsTrue(ReferenceEquals(tableStyle, first.Style), "A ContentControl Style must apply to the header.");
			Verify.IsTrue(ReferenceEquals(tableBrush, first.Background), "The ContentControl Style's setter must reach the header.");
			var second = RequireColumnHeader(tableView, 1);
			Verify.IsTrue(ReferenceEquals(columnStyle, second.Style), "A Control Style must apply to the header.");
			Verify.IsTrue(ReferenceEquals(columnBrush, second.Background), "The Control Style's setter must reach the header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a disabled header keeps one glyph Foreground binding across visual state updates instead of rebinding each time.")]
	public async Task VerifyDisabledSortGlyphBindingIsNotRecreated()
	{
		TableView tableView = null;
		Control glyph = null;
		BindingExpression binding = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			tableView.Columns[0].CanSort = true;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending), "Precondition: the column sorts.");
			tableView.IsEnabled = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			glyph = RequireColumnHeader(tableView, 0).FindVisualChildByName("TableViewSortIndicator") as Control;
			Verify.IsNotNull(glyph, "Precondition: a sorted column hosts the sort glyph.");
			binding = glyph.GetBindingExpression(Control.ForegroundProperty);
			Verify.IsNotNull(binding, "Precondition: the disabled glyph is bound to the header text.");
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the sort direction changes.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(ReferenceEquals(glyph, RequireColumnHeader(tableView, 0).FindVisualChildByName("TableViewSortIndicator")),
				"Precondition: the sort change updates the realized header in place.");
			Verify.IsTrue(ReferenceEquals(binding, glyph.GetBindingExpression(Control.ForegroundProperty)),
				"A state update must keep the glyph's existing binding.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the default header template provides its parts and hosts the sort glyph and resize gripper in them.")]
	public async Task VerifyColumnHeaderTemplateParts()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			var presenter = header.FindVisualChildByName("PART_ContentPresenter") as ContentPresenter;
			Verify.IsNotNull(presenter, "PART_ContentPresenter should exist.");
			Verify.IsTrue(ReferenceEquals(presenter, GetHeaderPresenter(tableView, 0)), "The content presenter should be the header's first ContentPresenter.");
			Verify.IsNotNull(header.FindVisualChildByName("PART_GridLine") as Border, "PART_GridLine should exist.");

			var indicatorHost = header.FindVisualChildByName("PART_SortIndicatorHost") as Border;
			Verify.IsNotNull(indicatorHost, "PART_SortIndicatorHost should exist.");
			Verify.IsTrue(indicatorHost.Child is SortIndicator, "A sortable header should host its sort glyph in PART_SortIndicatorHost.");

			var gripperHost = header.FindVisualChildByName("PART_ResizeGripperHost") as Border;
			Verify.IsNotNull(gripperHost, "PART_ResizeGripperHost should exist.");
			Verify.IsTrue(gripperHost.Child is ResizeGripper, "A resizable header should host its gripper in PART_ResizeGripperHost.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies SortStates follow the column's sort direction and show the glyph only while sorted.")]
	public async Task VerifyColumnHeaderSortStates()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		void VerifyState(string expected, Visibility hostVisibility, string context)
		{
			RunOnUIThread.Execute(() =>
			{
				var header = RequireColumnHeader(tableView, 0);
				Verify.AreEqual(expected, CurrentState(header, "SortStates"), $"SortStates ({context}).");
				var host = (UIElement)header.FindVisualChildByName("PART_SortIndicatorHost");
				Verify.AreEqual(hostVisibility, host.Visibility, $"Sort glyph visibility ({context}).");
				Verify.AreEqual("Unsorted", CurrentState(RequireColumnHeader(tableView, 1), "SortStates"),
					$"The other column stays Unsorted ({context}).");
			});
		}

		VerifyState("Unsorted", Visibility.Collapsed, "initial");

		RunOnUIThread.Execute(() => Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending)));
		await TestServices.WindowHelper.WaitForIdle();
		VerifyState("SortAscending", Visibility.Visible, "ascending");

		RunOnUIThread.Execute(() => Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending)));
		await TestServices.WindowHelper.WaitForIdle();
		VerifyState("SortDescending", Visibility.Visible, "descending");

		RunOnUIThread.Execute(() => Verify.IsTrue(tableView.ClearSort()));
		await TestServices.WindowHelper.WaitForIdle();
		VerifyState("Unsorted", Visibility.Collapsed, "cleared");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies header typography comes from the header Style, not from local values written by the control.")]
	public async Task VerifyColumnHeaderTypographyHasNoLocalValues()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateThemingTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = RequireColumnHeader(tableView, 0);
			var presenter = GetHeaderPresenter(tableView, 0);
			var text = presenter.Content as TextBlock;
			Verify.IsNotNull(text, "A plain string header should render through a TextBlock.");

			foreach (var (element, fontSize, fontWeight, name) in new (DependencyObject, DependencyProperty, DependencyProperty, string)[]
			{
				(header, Control.FontSizeProperty, Control.FontWeightProperty, "header"),
				(presenter, ContentPresenter.FontSizeProperty, ContentPresenter.FontWeightProperty, "content presenter"),
				(text, TextBlock.FontSizeProperty, TextBlock.FontWeightProperty, "header text"),
			})
			{
				Verify.AreEqual(DependencyProperty.UnsetValue, element.ReadLocalValue(fontSize), $"The {name} must not carry a local FontSize.");
				Verify.AreEqual(DependencyProperty.UnsetValue, element.ReadLocalValue(fontWeight), $"The {name} must not carry a local FontWeight.");
			}

			Verify.AreEqual((ushort)600, text.FontWeight.Weight, "The default header style keeps SemiBold.");
			Verify.AreEqual(14.0, text.FontSize, "The default header style keeps TableViewHeaderFontSize.");
		});
	}

	#endregion

	#region 13.6 Row banding under an element theme

	// The subtree runs opposite to the app theme so an Application-theme resolution is detectable.
	[TestMethod]
	public async Task VerifyAlternatingRowBackgroundFollowsElementThemeOppositeToApp()
	{
		const string appDictionaryXaml =
			@"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
								  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
				<ResourceDictionary.ThemeDictionaries>
				  <ResourceDictionary x:Key=""Light"">
					<SolidColorBrush x:Key=""TvBandLiteral"" Color=""#FF112233""/>
					<SolidColorBrush x:Key=""TvBandNestedThemeColor"" Color=""{ThemeResource SubtleFillColorSecondary}""/>
				  </ResourceDictionary>
				  <ResourceDictionary x:Key=""Dark"">
					<SolidColorBrush x:Key=""TvBandLiteral"" Color=""#FF445566""/>
					<SolidColorBrush x:Key=""TvBandNestedThemeColor"" Color=""{ThemeResource SubtleFillColorSecondary}""/>
				  </ResourceDictionary>
				</ResourceDictionary.ThemeDictionaries>
			  </ResourceDictionary>";

		ResourceDictionary appDictionary = null;
		StackPanel root = null;
		ElementTheme elementTheme = ElementTheme.Default;
		ElementTheme appTheme = ElementTheme.Default;
		var cases = new[] { "Direct", "Setter", "Literal", "NestedThemeColor" };

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();
			appTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
			elementTheme = appTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
			Log.Comment($"Application theme {appTheme}; window-root theme {elementTheme}.");

			appDictionary = (ResourceDictionary)Microsoft.UI.Xaml.Markup.XamlReader.Load(appDictionaryXaml);
			Application.Current.Resources.MergedDictionaries.Add(appDictionary);

			root = (StackPanel)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				$@"<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
							  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
							  xmlns:tv=""using:Microsoft.UI.Xaml.Controls.Tabular""
							  RequestedTheme=""{elementTheme}"">
					<StackPanel.Resources>
					  <Style x:Key=""BandStyle"" TargetType=""tv:TableView"">
						<Setter Property=""AlternatingRowBackground"" Value=""{{ThemeResource SubtleFillColorSecondaryBrush}}""/>
					  </Style>
					</StackPanel.Resources>
					<Border x:Name=""Reference"" Width=""10"" Height=""10"" Background=""{{ThemeResource SubtleFillColorSecondaryBrush}}""/>
					<tv:TableView x:Name=""Direct"" Width=""300"" Height=""120"" AlternatingRowBackground=""{{ThemeResource SubtleFillColorSecondaryBrush}}""/>
					<tv:TableView x:Name=""Setter"" Width=""300"" Height=""120"" Style=""{{StaticResource BandStyle}}""/>
					<tv:TableView x:Name=""Literal"" Width=""300"" Height=""120"" AlternatingRowBackground=""{{ThemeResource TvBandLiteral}}""/>
					<tv:TableView x:Name=""NestedThemeColor"" Width=""300"" Height=""120"" AlternatingRowBackground=""{{ThemeResource TvBandNestedThemeColor}}""/>
				  </StackPanel>");

			foreach (var name in cases)
			{
				var tableView = (TableView)root.FindName(name);
				tableView.Columns.Add(MakeTextColumn("Name"));
				tableView.ItemsSource = MakeItems();
			}

			LoadContent(root);
		});

		try
		{
			foreach (var theme in new[] { elementTheme, appTheme })
			{
				RunOnUIThread.Execute(() => root.RequestedTheme = theme);
				await TestServices.WindowHelper.WaitForIdle();
				RunOnUIThread.Execute(() => root.UpdateLayout());
				await TestServices.WindowHelper.WaitForIdle();

				RunOnUIThread.Execute(() =>
				{
					var expected = ((SolidColorBrush)((Border)root.FindName("Reference")).Background).Color;
					var expectedLiteral = theme == ElementTheme.Dark
						? Color.FromArgb(0xFF, 0x44, 0x55, 0x66)
						: Color.FromArgb(0xFF, 0x11, 0x22, 0x33);

					foreach (var name in cases)
					{
						var tableView = (TableView)root.FindName(name);
						Verify.AreEqual(theme, tableView.ActualTheme, $"Precondition: {name} runs under {theme}.");
						var oddRow = GetProjectedRow(tableView, 1);
						var actual = (oddRow?.Background as SolidColorBrush)?.Color;
						var want = name == "Literal" ? expectedLiteral : expected;
						Log.Comment($"{theme} {name}: band {actual}, expected {want}.");
						Verify.AreEqual(want, actual, $"Under {theme}, the {name} banding pattern must paint the {theme} color on odd rows.");
						Verify.IsNull(GetProjectedRow(tableView, 0)?.ReadLocalValue(Control.BackgroundProperty) as Brush,
							$"Even rows stay unbanded without RowBackground ({name}).");
					}
				});
			}
		}
		finally
		{
			RunOnUIThread.Execute(() => Application.Current.Resources.MergedDictionaries.Remove(appDictionary));
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

	internal static Color RequireApplicationColor(string key)
	{
		if (!Application.Current.Resources.ContainsKey(key) ||
			!(Application.Current.Resources[key] is Color color))
		{
			Verify.Fail($"'{key}' must resolve to a Color from the application resources.");
			return default;
		}

		return color;
	}

	internal static Color RequireThemeColor(string themeName, string key)
	{
		var pending = new Stack<ResourceDictionary>();
		pending.Push(Application.Current.Resources);
		while (pending.Count > 0)
		{
			var dictionary = pending.Pop();
			if (dictionary.ThemeDictionaries.TryGetValue(themeName, out var theme) &&
				theme is ResourceDictionary themeDictionary &&
				// Uno specific: the plain overload falls back to the active theme's system resources.
				themeDictionary.TryGetValue(key, out var value, shouldCheckSystem: false) &&
				value is Color color)
			{
				return color;
			}

			foreach (var merged in dictionary.MergedDictionaries)
			{
				pending.Push(merged);
			}
		}

		Verify.Fail($"'{key}' must resolve to a Color in an application '{themeName}' theme dictionary.");
		return default;
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
